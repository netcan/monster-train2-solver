using System.Security.Cryptography;
using System.Text;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BossCoverageInventory
{
    private sealed class Evidence
    {
        internal readonly SortedSet<string> Battles = new(StringComparer.Ordinal);
        internal readonly SortedSet<string> Pairs = new(StringComparer.Ordinal);
    }

    internal static void Run(string workspace, bool check)
    {
        string catalogPath = Path.Combine(workspace, "tests/catalogs/boss-rule-catalog.mt2f");
        string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(catalogPath))).ToLowerInvariant();
        if (hash != "80459451514dde9b2313d31c7c27ca812ce055ea43c8c8c11fbfc0b756526f65")
            throw new InvalidOperationException("Boss inventory requires its reviewed complete native catalog.");
        using var catalog = FixtureDocument.Read(catalogPath);
        FixtureValue root = catalog.RootElement;
        if (root.GetProperty("Schema").GetInt32() != 3 || root.GetProperty("CharacterCatalogScope").GetString() != "AllGameData")
            throw new InvalidOperationException("Incomplete native boss catalog.");
        var scenarios = root.GetProperty("BossScenarios").EnumerateArray().Where(row =>
            Text(row, "AssetKey").StartsWith("Level3", StringComparison.Ordinal) ||
            Text(row, "AssetKey").StartsWith("Level6", StringComparison.Ordinal) ||
            Text(row, "AssetKey").StartsWith("Level8", StringComparison.Ordinal)).ToArray();
        var evidence = scenarios.ToDictionary(row => Text(row, "AssetKey"), _ => new Evidence(), StringComparer.Ordinal);
        string directory = Path.Combine(workspace, "tests/fixtures");
        var fixtures = new FixtureCatalog(directory);
        string[] archives = fixtures.Paths.Keys.Where(name => name.StartsWith("full-battle-", StringComparison.Ordinal)).ToArray();
        var registered = archives.ToHashSet(StringComparer.Ordinal);
        foreach (string archive in archives)
        {
            using var document = FixtureDocument.Read(fixtures.Resolve(archive));
            FixtureValue battle = document.RootElement;
            string scenario = Scenario(battle);
            if (!evidence.TryGetValue(scenario, out Evidence? entry) || !Accepted(battle)) continue;
            entry.Battles.Add(archive);
            if (!archive.EndsWith("-search.mt2f", StringComparison.Ordinal) || !Player(battle, requirePublication: true)) continue;
            string replayName = archive[..^"-search.mt2f".Length] + "-replay.mt2f";
            if (!registered.Contains(replayName)) continue;
            using var replay = FixtureDocument.Read(fixtures.Resolve(replayName));
            if (Scenario(replay.RootElement) == scenario && Accepted(replay.RootElement) && Player(replay.RootElement, requirePublication: false))
                entry.Pairs.Add(archive);
        }
        var tsv = new StringBuilder("scenario\touter_boss\tdefinition_gate\tgrowth_signature\tcurated_battles\tcurated_player_pairs\tplayer_sources\n");
        var table = new StringBuilder("| Original scenario | Model definition gate | Curated battles | Curated player pairs |\n| --- | --- | ---: | ---: |\n");
        foreach (FixtureValue scenario in scenarios)
        {
            string name = Text(scenario, "AssetKey"), boss = Text(scenario, "OuterBoss");
            Evidence entry = evidence[name];
            string gate = OuterBossDefinitions.Supports(boss) ? "allowed" : "guarded";
            string[] growth = scenario.GetProperty("EnemyRelics").EnumerateArray()
                .SelectMany(relic => relic.GetProperty("Effects").EnumerateArray())
                .SelectMany(effect => effect.GetProperty("Triggers").EnumerateArray())
                .SelectMany(trigger => trigger.GetProperty("Effects").EnumerateArray().Select(effect =>
                    Text(trigger, "Kind") + ":" + Text(effect, "Type") + ":" + effect.GetProperty("Int").GetInt32())).ToArray();
            string pairSources = entry.Pairs.Count == 0 ? "-" : string.Join(';', entry.Pairs);
            tsv.AppendLine($"{name}\t{boss}\t{gate}\t{string.Join(';', growth)}\t{entry.Battles.Count}\t{entry.Pairs.Count}\t{pairSources}");
            table.AppendLine($"| {name} | {gate} | {entry.Battles.Count} | {entry.Pairs.Count} |");
        }
        int allowed = scenarios.Count(row => OuterBossDefinitions.Supports(Text(row, "OuterBoss")));
        int paired = evidence.Values.Count(entry => entry.Pairs.Count > 0);
        string report = "# Original outer-boss coverage\n\n" +
            $"The frozen complete native catalog contains {scenarios.Length} ordinary base-game outer-boss scenarios at Rings3/6/8. " +
            $"The model definition gate allows {allowed}; {paired} scenarios have curated player-source/replay metadata.\n\n" +
            "The gate is an identity filter. Allowed definitions can still reject uncalibrated payloads or owners. " +
            "Battle counts require settled terminal effects and all four failure counters zero. Pair counts additionally require " +
            "registered source/replay archives, actual player assembly metadata and a positive source publication count. " +
            "This inventory reads prior curated evidence; it does not rerun native play or independent forecast checks. " +
            "Controlled selections and losses retain their original fixture boundaries and do not establish natural victories.\n\n" + table +
            "\nThe TSV retains native identities, growth signatures and exact player-source archives. " +
            "True-final-boss routing, Titan Trial substitutions and Endless/test scenarios have their own acceptance scopes.\n";
        report += SoulSavior(workspace);
        Write(Path.Combine(directory, "inventory", "boss-inventory.tsv"), tsv.ToString(), check);
        Write(Path.Combine(directory, "inventory", "boss-coverage.md"), report, check);
        Console.WriteLine($"BOSS-INVENTORY {(check ? "CHECK" : "WRITE")} PASS: {scenarios.Length} original scenarios/{allowed} allowed definition gates/{paired} curated player-pair scenarios; metadata only, no battle regression.");
    }

    private static string SoulSavior(string workspace)
    {
        const string rulePath = "2.3.1/rule-catalog.mt2f";
        const string ruleHash = "7ae35691d7d504f1513790709f3816bd252472c82de2f9d2a1bc2c9760b35ef9";
        string directory = Game231FixtureCatalog.DirectoryFor(workspace);
        var registrations = Game231FixtureCatalog.Load(directory);
        Game231FixtureRegistration ruleCatalog = registrations[rulePath];
        if (ruleCatalog.Sha256 != ruleHash) throw new InvalidOperationException("Unexpected Soul Savior definition catalog hash.");
        using var catalog = FixtureDocument.Read(ruleCatalog.Verify(directory));
        FixtureValue source = catalog.RootElement;
        if (Text(source, "GameModuleMvid") != Game231FixtureCatalog.Module)
            throw new InvalidOperationException("Unexpected Soul Savior definition module.");
        var bosses = source.GetProperty("BossScenarios").EnumerateArray().Where(row =>
            Text(row, "AssetKey").StartsWith("SoulSavior_", StringComparison.Ordinal))
            .ToDictionary(row => Text(row, "AssetKey"), row => Text(row, "OuterBoss"), StringComparer.Ordinal);
        var recorded = bosses.Keys.ToDictionary(key => key, _ => 0, StringComparer.Ordinal);
        foreach (Game231FixtureRegistration archive in registrations.Values.Where(row =>
            row.Path.StartsWith("mechanics/", StringComparison.Ordinal)))
        {
            using var document = FixtureDocument.Read(archive.Verify(directory));
            FixtureValue root = document.RootElement;
            FixtureValue receipt = root.TryGetProperty("OriginalUnrulyBattle", out FixtureValue witness) && witness.ValueKind == FixtureKind.Object
                ? witness : root.TryGetProperty("OriginalSpurnedBattle", out witness) ? witness : default;
            if (receipt.ValueKind != FixtureKind.Object) continue;
            string scenario = Text(receipt, "Scenario");
            if (bosses.TryGetValue(scenario, out string? expected) && Text(receipt, "BossKey") == expected &&
                Game231FixtureCatalog.Classify(archive, root) == Game231FixtureKind.SettledBattle)
                recorded[scenario]++;
        }
        var table = new StringBuilder("\n| Original scenario | Model definition gate | Settled recordings |\n| --- | --- | ---: |\n");
        int allowed = 0, scoped = 0;
        foreach (var (scenario, boss) in bosses.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            bool supported = OuterBossDefinitions.Supports(boss);
            allowed += supported ? 1 : 0;
            bool encounterScoped = OuterBossDefinitions.UsesEncounterScopedCasting(boss);
            scoped += encounterScoped ? 1 : 0;
            string gate = supported ? "allowed" : encounterScoped ? "scoped" : "guarded";
            table.AppendLine($"| {scenario} | {gate} | {recorded[scenario]} |");
        }
        int observed = recorded.Values.Count(battles => battles > 0);
        Console.WriteLine($"SOUL-SAVIOR-INVENTORY: {bosses.Count} original scenarios/{allowed} general definition gates/{scoped} encounter-scoped casting definitions/{observed} settled recorded scenarios.");
        return "\n## Soul Savior\n\n" +
            $"The pinned {Game231FixtureCatalog.Version} catalog contains {bosses.Count} original encounters. " +
            $"{allowed} definitions use the general model gate, {scoped} use encounter-specific casting validation, " +
            $"and {observed} encounters have settled recordings. " +
            "The registered fixtures use original TestScenario selections and can include defeats.\n" + table;
    }

    private static string Scenario(FixtureValue battle)
    {
        if (battle.TryGetProperty("NativeRunInput", out FixtureValue input) && input.ValueKind == FixtureKind.Object)
            return Text(input, "Scenario");
        return battle.TryGetProperty("PlayerAdvisor", out FixtureValue player) && player.ValueKind == FixtureKind.Object &&
            player.TryGetProperty("Overlay", out FixtureValue overlay) && overlay.ValueKind == FixtureKind.Object ? Text(overlay, "Scenario") : "";
    }
    private static bool Accepted(FixtureValue battle) =>
        battle.TryGetProperty("TerminalEffectsSettled", out FixtureValue terminal) && terminal.GetBoolean() &&
        new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name =>
            battle.TryGetProperty(name, out FixtureValue value) && value.GetInt32() == 0);
    private static bool Player(FixtureValue battle, bool requirePublication) =>
        battle.TryGetProperty("PlayerAdvisor", out FixtureValue player) && player.ValueKind == FixtureKind.Object &&
        Text(player, "Assembly") == "MonsterTrain2Poju.Advisor" &&
        (!requirePublication || player.GetProperty("Overlay").GetProperty("Published").GetInt32() > 0);
    private static string Text(FixtureValue value, string key) =>
        value.TryGetProperty(key, out FixtureValue field) && field.ValueKind == FixtureKind.String ? field.GetString()! : "";
    private static void Write(string path, string content, bool check)
    {
        static string Normalize(string value) => value.Replace("\r\n", "\n");
        if (check)
        {
            if (!File.Exists(path) || Normalize(File.ReadAllText(path)) != Normalize(content))
                throw new InvalidOperationException("Boss inventory is stale: " + path);
        }
        else File.WriteAllText(path, Normalize(content), new UTF8Encoding(false));
    }
}
