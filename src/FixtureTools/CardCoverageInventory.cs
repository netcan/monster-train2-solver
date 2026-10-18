using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MonsterTrain2Poju.Fixtures;

internal static class CardCoverageInventory
{
    private const string GameModule = "8fb07b96-f4db-4d2b-884d-c00536d6ccf4";
    private const string CatalogHash = "bba66c4ea6255bd16909cefc2ed61883b24f7048ce00216e995c811a8e7e1edb";
    private sealed class Card(FixtureValue definition)
    {
        internal readonly FixtureValue Definition = definition;
        internal string Id => Text(Definition, "Id");
        internal string Name => Text(Definition, "AssetKey");
        internal string Type => Text(Definition, "Type");
        internal bool Logbook => Flag(Definition, "AppearsInLogbook");
        internal string Role => Logbook ? "logbook" : Flag(Definition, "IsUnitAbility") ? "unit-ability" :
            Flag(Definition, "IsRoomAbility") ? "room-ability" : "other-definition";
        internal readonly SortedDictionary<string, Plays> Evidence = new(StringComparer.Ordinal);
        internal readonly SortedSet<string> Gaps = new(StringComparer.Ordinal);
        internal readonly SortedSet<string> Mechanisms = new(StringComparer.Ordinal);
        internal string Coverage = "unreviewed";
        internal int DirectPlays => Evidence.Values.Sum(value => value.Direct.Count);
        internal int OtherPlays => Evidence.Values.Sum(value => value.Other.Count);
    }
    private sealed class Plays
    {
        internal readonly List<int> Direct = [];
        internal readonly List<int> Other = [];
    }

    internal static void Run(string workspace, bool check)
    {
        workspace = Path.GetFullPath(workspace);
        string fixtureDirectory = Path.Combine(workspace, "tests", "fixtures");
        string catalogPath = Path.Combine(workspace, "tests", "catalogs", "card-catalog.mt2f");
        Require(Hash(catalogPath) == CatalogHash, "Original card catalog hash changed; review its provenance before updating the inventory.");
        using var catalog = FixtureDocument.Read(catalogPath);
        FixtureValue root = catalog.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && Text(root, "Scope") == "AllGameData" &&
            Text(root, "GameModuleMvid") == GameModule && Flag(root, "BranchCardMasksEnabled") && Flag(root, "DecisionUnchanged") &&
            root.GetProperty("FrameBefore").ContentEquals(root.GetProperty("FrameAfter")) &&
            root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")), "Native catalog identity/state/RNG/frame guard failed.");
        Card[] cards = root.GetProperty("Cards").EnumerateArray().Select(value => new Card(value)).OrderBy(card => card.Name, StringComparer.Ordinal).ToArray();
        var byId = cards.ToDictionary(card => card.Id, StringComparer.Ordinal);
        Require(cards.Length == 668 && cards.Count(card => card.Logbook) == 563, "Incomplete original card or logbook catalog.");
        using (var calibration = FixtureDocument.Read(Path.Combine(fixtureDirectory, "card-upgrade-mask-calibration.mt2f")))
        {
            FixtureValue[] facts = calibration.RootElement.GetProperty("CardData").EnumerateArray().ToArray();
            Require(facts.Length == cards.Length, "Accepted original definition calibration has a different scope.");
            foreach (FixtureValue fact in facts)
                Require(byId.TryGetValue(Text(fact, "DataId"), out Card? card) &&
                    Equivalent(card.Definition.GetProperty("Mask"), fact, legacyMask: true), "Catalog differs from accepted raw definition facts: " + Text(fact, "DataId"));
        }

        string[] manifest = File.ReadAllLines(Path.Combine(fixtureDirectory, "manifest.tsv"));
        Require(manifest[0] == "archive\tsource\tsource_bytes\tsource_sha256\tarchive_bytes\tarchive_sha256\tunique_nodes", "Unknown fixture manifest schema.");
        var mechanisms = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var excluded = new SortedDictionary<string, string>(StringComparer.Ordinal);
        int indexed = 0, actionCount = 0, nonCatalogActions = 0, unresolvedActions = 0;
        foreach (string line in manifest.Skip(1).Order(StringComparer.Ordinal))
        {
            string[] row = line.Split('\t');
            Require(row.Length == 7 && Path.GetFileName(row[0]) == row[0], "Invalid fixture manifest row.");
            string archive = row[0];
            if (!archive.StartsWith("full-battle-", StringComparison.Ordinal)) continue;
            string path = Path.Combine(fixtureDirectory, archive);
            Require(new FileInfo(path).Length == long.Parse(row[4], CultureInfo.InvariantCulture) && Hash(path).Equals(row[5], StringComparison.OrdinalIgnoreCase), "Fixture manifest integrity failed: " + archive);
            using var fixture = FixtureDocument.Read(path);
            FixtureValue battle = fixture.RootElement;
            Require(Text(battle, "GameModuleMvid") == GameModule, "Fixture has a different game module: " + archive);
            string[] counters = ["CaptureFailures", "Mismatches", "Unsupported", "Pending"];
            string[] counterErrors = counters.Select(name => !battle.TryGetProperty(name, out FixtureValue value) || value.ValueKind != FixtureKind.Number ? name + "=absent" :
                value.GetInt32() != 0 ? name + "=" + value.GetInt32().ToString(CultureInfo.InvariantCulture) : "").Where(error => error != "").ToArray();
            if (counterErrors.Length > 0)
            {
                excluded.Add(archive, string.Join(";", counterErrors));
                continue;
            }
            indexed++;
            if (!battle.TryGetProperty("Actions", out FixtureValue actions)) continue;
            int index = -1;
            foreach (FixtureValue record in actions.EnumerateArray())
            {
                index++;
                if (!record.TryGetProperty("Predicted", out FixtureValue predicted) || !Flag(predicted, "Supported") ||
                    !record.TryGetProperty("Actual", out FixtureValue actual) || actual.ValueKind != FixtureKind.Object ||
                    !record.TryGetProperty("Difference", out FixtureValue difference) || difference.ValueKind != FixtureKind.Null) continue;
                actionCount++;
                FixtureValue before = record.GetProperty("Before"), action = record.GetProperty("Action");
                FixtureValue context = before.GetProperty("Spawn").GetProperty("Train").GetProperty("Context");
                string? id = FindCardId(context, action.GetProperty("CardInstanceId").GetInt32());
                if (id == null) { unresolvedActions++; continue; }
                if (!before.TryGetProperty("PlayRules", out FixtureValue rules) || rules.ValueKind != FixtureKind.Object) { unresolvedActions++; continue; }
                FixtureValue rule = rules.GetProperty("Cards").EnumerateArray().FirstOrDefault(value => Text(value, "DataId") == id);
                if (rule.ValueKind != FixtureKind.Object) { unresolvedActions++; continue; }
                foreach (string primitive in Primitives(rule)) mechanisms.TryAdd(primitive, archive);
                if (!byId.TryGetValue(id, out Card? card)) { nonCatalogActions++; continue; }
                if (!card.Evidence.TryGetValue(archive, out Plays? plays)) card.Evidence.Add(archive, plays = new());
                FixtureValue original = card.Definition.GetProperty("Rule");
                bool matches = original.ValueKind == FixtureKind.Object && Equivalent(original, rule, ruleRoot: true) &&
                    MatchesSourceMetadata(card.Definition, context, action.GetProperty("CardInstanceId").GetInt32());
                (matches ? plays.Direct : plays.Other).Add(index);
            }
        }
        foreach (Card card in cards)
        {
            FixtureValue rule = card.Definition.GetProperty("Rule");
            if (rule.ValueKind != FixtureKind.Object || Text(card.Definition, "ProjectionError") != "")
            {
                card.Gaps.Add("definition-projection-error");
                continue;
            }
            CollectGuards(rule, "$", card.Gaps);
            string pipeline = Text(rule, "Effect");
            bool unsupported = Strings(rule, "ExternalInteractions").Any(value => value.StartsWith("Unimplemented play effect ", StringComparison.Ordinal));
            if (Flag(card.Definition, "IsRoomAbility"))
            {
                unsupported = true;
                card.Gaps.Add("room-ability-activation-not-modeled");
            }
            if (Flag(card.Definition, "IsUnitAbility") && pipeline != "Spell" && !(pipeline == "Null" && Array(rule, "Effects").Length == 0))
            {
                unsupported = true;
                card.Gaps.Add("unit-ability-pipeline-not-modeled:" + pipeline);
            }
            bool missingEvidence = false;
            foreach (string primitive in Primitives(rule))
            {
                if (mechanisms.TryGetValue(primitive, out string? archive)) card.Mechanisms.Add(primitive + "=" + archive);
                else { missingEvidence = true; card.Gaps.Add("no-indexed-play-chain:" + primitive); }
            }
            card.Coverage = unsupported ? "unsupported" : card.Gaps.Any(gap => !gap.StartsWith("no-indexed-play-chain:", StringComparison.Ordinal)) ? "partial" :
                card.DirectPlays > 0 ? "direct-native-play" : missingEvidence ? "unreviewed" : "mechanism-only";
        }

        var inventory = new StringBuilder();
        Row(inventory, "DataId", "AssetKey", "CardType", "Role", "InLogbook", "CountsTowardsMastery", "Rarity", "LinkedClan", "SpawnCharacter",
            "MainEffects", "Traits", "CardTriggers", "Coverage", "MatchingDefinitionPlays", "OtherRecordedPlays", "DirectFixtures", "OtherFixtures", "MechanismEvidence", "KnownGaps", "ProjectionError");
        foreach (Card card in cards)
            Row(inventory, card.Id, card.Name, card.Type, card.Role, card.Logbook.ToString().ToLowerInvariant(), Flag(card.Definition, "CountsTowardsMastery").ToString().ToLowerInvariant(),
                Text(card.Definition, "Rarity"), Text(card.Definition, "LinkedClan"), Text(card.Definition, "SpawnCharacter"),
                Join(Array(card.Definition, "Effects").Select(effect => Text(effect, "Type"))), Join(Array(card.Definition, "Traits").Select(trait => Text(trait, "Type"))),
                Join(Array(card.Definition, "Triggers").Select(trigger => Text(trigger, "Kind"))), card.Coverage,
                card.DirectPlays.ToString(CultureInfo.InvariantCulture), card.OtherPlays.ToString(CultureInfo.InvariantCulture),
                Join(card.Evidence.Where(pair => pair.Value.Direct.Count > 0).Select(pair => pair.Key)), Join(card.Evidence.Where(pair => pair.Value.Other.Count > 0).Select(pair => pair.Key)),
                Join(card.Mechanisms), Join(card.Gaps), Text(card.Definition, "ProjectionError"));
        var evidence = new StringBuilder();
        Row(evidence, "DataId", "AssetKey", "Archive", "MatchingDefinitionPlays", "OtherRecordedPlays", "MatchingActionIndices", "OtherActionIndices");
        foreach (Card card in cards)
        foreach ((string archive, Plays plays) in card.Evidence)
            Row(evidence, card.Id, card.Name, archive, plays.Direct.Count.ToString(CultureInfo.InvariantCulture), plays.Other.Count.ToString(CultureInfo.InvariantCulture),
                string.Join(",", plays.Direct), string.Join(",", plays.Other));
        string report = Report(cards, indexed, excluded, actionCount, nonCatalogActions, unresolvedActions);
        foreach ((string name, string content) in new[] { ("card-inventory.tsv", inventory.ToString()), ("card-play-evidence.tsv", evidence.ToString()), ("card-coverage.md", report) })
        {
            string path = Path.Combine(fixtureDirectory, name);
            if (check) Require(File.Exists(path) && File.ReadAllText(path) == content, "Card inventory is stale: " + name);
            else File.WriteAllText(path, content, new UTF8Encoding(false));
        }
        Console.WriteLine($"CARD-INVENTORY {(check ? "CHECK" : "WRITE")} PASS: {cards.Length} definitions/{cards.Count(card => card.Logbook)} logbook entries; {indexed} indexed battles/{actionCount} native actions; {cards.Count(card => card.DirectPlays > 0)} definition-matched card identities; {excluded.Count} excluded captures; no battle regression executed.");
    }

    private static string? FindCardId(FixtureValue context, int instanceId)
    {
        foreach (string name in new[] { "CardInstances", "CardRegistry", "AbilityCardCache" })
        foreach (FixtureValue card in Array(context, name))
            if (card.TryGetProperty("InstanceId", out FixtureValue id) && id.GetInt32() == instanceId) return Text(card, "DataId");
        if (context.TryGetProperty("Cards", out FixtureValue cycle))
        foreach (string pile in new[] { "Hand", "Draw", "Discard" })
        foreach (FixtureValue card in Array(cycle, pile))
            if (card.GetProperty("InstanceId").GetInt32() == instanceId) return Text(card, "DataId");
        return null;
    }

    private static bool MatchesSourceMetadata(FixtureValue definition, FixtureValue context, int instanceId)
    {
        // Rule projections do not retain every raw trait parameter. Until those
        // parameters have their own original-card witnesses, do not promote them.
        FixtureValue[] traits = Array(definition, "Traits");
        if (traits.Any(trait => Text(trait, "Type") != "CardTraitSelfPurge")) return false;
        foreach (string source in new[] { "CardInstances", "CardRegistry" })
        foreach (FixtureValue instance in Array(context, source))
        {
            if (instance.GetProperty("InstanceId").GetInt32() != instanceId) continue;
            if (!instance.TryGetProperty("MaskDescriptor", out FixtureValue mask) || mask.ValueKind != FixtureKind.Object) continue;
            if (!mask.TryGetProperty("Definition", out FixtureValue sourceDefinition) || sourceDefinition.ValueKind != FixtureKind.Object ||
                Text(sourceDefinition, "DataId") != Text(definition, "Id") || Text(sourceDefinition, "CardType") != Text(definition, "Type") ||
                !Strings(sourceDefinition, "MainEffectNames").SequenceEqual(Array(definition, "Effects").Select(effect => Text(effect, "Type")))) return false;
            if (!mask.TryGetProperty("BaseTraits", out FixtureValue baseTraits) || baseTraits.ValueKind != FixtureKind.Array) continue;
            FixtureValue[] bases = baseTraits.EnumerateArray().ToArray();
            if (!bases.Select(trait => Text(trait, "RuntimeType")).SequenceEqual(traits.Select(trait => Text(trait, "Type")))) return false;
            for (int index = 0; index < traits.Length; index++)
                if (!bases[index].TryGetProperty("DefinitionParamInt", out FixtureValue value) ||
                    !value.ContentEquals(traits[index].GetProperty("Int"))) return false;
            return true;
        }
        return false; // Legacy schemas without base-trait evidence stay unverified.
    }

    private static IEnumerable<string> Primitives(FixtureValue rule)
    {
        yield return "pipeline:" + Text(rule, "Effect");
        foreach (string name in new[] { "Effects", "OnCastEffects", "OnUnplayedNegativeEffects" })
        foreach (FixtureValue effect in Array(rule, name)) yield return "effect:" + Text(effect, "Type");
    }

    private static void CollectGuards(FixtureValue value, string path, SortedSet<string> guards)
    {
        if (value.ValueKind == FixtureKind.Object)
        foreach (FixtureProperty property in value.EnumerateObject())
        {
            string child = path + "." + property.Name;
            if (property.Name.EndsWith("Interactions", StringComparison.Ordinal) && property.Value.ValueKind == FixtureKind.Array)
                foreach (FixtureValue guard in property.Value.EnumerateArray()) guards.Add(child + ":" + guard.GetString());
            else if (property.Name == "Type" && property.Value.ValueKind == FixtureKind.String && property.Value.GetString() == "Unsupported") guards.Add(child + ":unsupported-effect");
            else if (property.Name == "HasUnsupportedUnplayedTriggers" && property.Value.ValueKind == FixtureKind.True) guards.Add(child);
            else CollectGuards(property.Value, child, guards);
        }
        else if (value.ValueKind == FixtureKind.Array)
        {
            int index = 0;
            foreach (FixtureValue element in value.EnumerateArray()) CollectGuards(element, path + "[" + index++ + "]", guards);
        }
    }

    // Only these root fields describe decision-context eligibility/order rather than
    // card definitions. Missing null properties are equivalent across capture schemas;
    // missing non-null fields, effects, numbers and ordered arrays never are.
    private static bool Equivalent(FixtureValue left, FixtureValue right, bool ruleRoot = false, bool legacyMask = false)
    {
        if (left.ValueKind != right.ValueKind) return false;
        if (left.ValueKind == FixtureKind.Object)
        {
            bool Include(FixtureProperty property) => property.Value.ValueKind != FixtureKind.Null &&
                !(legacyMask && property.Name == "FromPermanentUpgrade" && property.Value.ValueKind == FixtureKind.False) &&
                !(ruleRoot && property.Name is "ForgeEligible" or "UnplayedNegativeOrder");
            var a = left.EnumerateObject().Where(Include).ToDictionary(property => property.Name, property => property.Value, StringComparer.Ordinal);
            var b = right.EnumerateObject().Where(Include).ToDictionary(property => property.Name, property => property.Value, StringComparer.Ordinal);
            return a.Count == b.Count && a.All(pair => b.TryGetValue(pair.Key, out FixtureValue other) && Equivalent(pair.Value, other, legacyMask: legacyMask));
        }
        if (left.ValueKind == FixtureKind.Array)
            return left.GetArrayLength() == right.GetArrayLength() && left.EnumerateArray().Zip(right.EnumerateArray()).All(pair => Equivalent(pair.First, pair.Second, legacyMask: legacyMask));
        if (left.ValueKind == FixtureKind.Number) return left.GetDouble() == right.GetDouble();
        return left.ContentEquals(right);
    }

    private static string Report(Card[] cards, int indexed, SortedDictionary<string, string> excluded, int actions, int nonCatalog, int unresolved)
    {
        var text = new StringBuilder();
        text.Append("# Original card coverage inventory\n\n");
        text.Append("Generated by `scripts/Get-CardCoverageInventory.ps1`; inputs are the frozen native catalog and manifest-verified battle archives. This indexes existing evidence and does not replay battles or run regression.\n\n");
        text.Append("The catalog/model projection is from 2026-10-18 (model revision `e2c3bff`). See [catalog provenance](../catalogs/README.md). Keep the snapshot date in mind after model changes; regenerate the native catalog when projections change.\n\n");
        text.Append("## Scope\n\n| Native role | Definitions |\n| --- | ---: |\n");
        foreach (var group in cards.GroupBy(card => card.Role).OrderBy(group => group.Key, StringComparer.Ordinal)) text.Append($"| {group.Key} | {group.Count()} |\n");
        text.Append($"| **All definitions** | **{cards.Length}** |\n\n");
        text.Append($"The 563 native logbook entries are the primary card-inventory denominator, not the 668 internal definitions. Logbook visibility does not imply collectible/reward-pool membership; generated, event and non-mastery cards are included. Native mastery eligibility is a separate recorded predicate ({cards.Count(card => Flag(card.Definition, "CountsTowardsMastery"))} definitions). Abilities and other hidden definitions stay in the inventory.\n\n");
        text.Append("## Evidence levels\n\n| Classification | Logbook | All definitions | Meaning |\n| --- | ---: | ---: | --- |\n");
        foreach ((string level, string meaning) in new[] {
            ("direct-native-play", "No captured projection guards; at least one successful native play has the same captured definition projection."),
            ("mechanism-only", "No captured projection guards; its main pipeline/effect types occur in indexed native played chains. The original card itself is not directly accepted."),
            ("partial", "Projection contains explicit play, upgrade, discard, consume or nested interaction guards. See each row."),
            ("unsupported", "The current definition/activation pipeline is explicitly unimplemented."),
            ("unreviewed", "Projection failed or a required primitive has no indexed played-chain witness; support is unknown.") })
            text.Append($"| {level} | {cards.Count(card => card.Logbook && card.Coverage == level)} | {cards.Count(card => card.Coverage == level)} | {meaning} |\n");
        text.Append("\nThese are evidence labels, not whole-card completion claims. A directly matched play proves that captured path; it does not prove every target, upgrade, relic combination, trigger or future branch. Indirect witnesses may execute an effect with no target; presence in a played chain is not a positive test of every parameter. The definition projection is conservative but is not a full raw-asset checksum.\n\n");
        text.Append("Definition matching compares all captured rule fields recursively, including source-unit statistics, statuses, triggers, ability metadata, effect parameters and nested creation rules. Only root `ForgeEligible` and `UnplayedNegativeOrder` are excluded because they depend on the decision context. Missing null fields are tolerated for older schemas; missing non-null fields are not. Direct evidence also requires captured instance base-trait metadata. This initial matcher accepts only trait-free definitions or SelfPurge with matching native definition parameters; other trait parameters need separate original-card calibration and are not promoted from rule equality alone. A shared DataId alone never establishes direct coverage. Upgrades on an instance do not change the original definition, but authored definition changes do. Other recorded plays can be authored variants or older/incomplete projections; they are not automatically classified as original-card evidence.\n\n");
        text.Append($"Across all classifications, {cards.Count(card => card.DirectPlays > 0)} identities ({cards.Count(card => card.Logbook && card.DirectPlays > 0)} logbook) have at least one definition-matched play. Partial cards retain their matched-play evidence without losing their guard classification.\n\n");
        text.Append("## Reproducible files\n\n- [Per-card inventory](card-inventory.tsv): identity, type/role, native effects/traits, evidence level, direct/other plays, primitive witnesses and exact projection guard paths.\n- [Played-card evidence](card-play-evidence.tsv): one row per original identity/archive, with zero-based indices into that archive's `Actions` array.\n- [Fixture manifest](manifest.tsv): archive byte counts and SHA-256; every indexed full battle is checked against it.\n\n");
        text.Append($"Indexed {indexed} full battles and {actions} native actions whose captured prediction is supported, actual state exists and difference is null. {nonCatalog} actions use scenario-only identities; {unresolved} actions lack a resolvable card/rule and are not counted as card evidence. Excluded captures with missing/nonzero original counters: {excluded.Count}. Accepted component fixtures are used for catalog-fact validation, not counted as native card plays. No current simulation checks are rerun by this indexer.\n\n");
        text.Append("## Largest recorded guard groups\n\nCounts are per logbook identity and can overlap. Conditional upgrade/discard guards do not imply every normal play fails.\n\n| Guard | Logbook identities |\n| --- | ---: |\n");
        foreach (var group in cards.Where(card => card.Logbook).SelectMany(card => card.Gaps.Where(gap => !gap.StartsWith("no-indexed-play-chain:", StringComparison.Ordinal)).Select(gap => (card.Id, Gap: gap.Contains(':') ? gap[(gap.IndexOf(':') + 1)..] : gap)))
            .Distinct().GroupBy(pair => pair.Gap).OrderByDescending(group => group.Count()).ThenBy(group => group.Key, StringComparer.Ordinal).Take(15))
            text.Append($"| {Escape(group.Key)} | {group.Count()} |\n");
        text.Append("\n## Explicitly unsupported logbook entries\n\n| Asset key | Main effects |\n| --- | --- |\n");
        foreach (Card card in cards.Where(card => card.Logbook && card.Coverage == "unsupported"))
            text.Append($"| `{Escape(card.Name)}` | {Escape(Join(Array(card.Definition, "Effects").Select(effect => Text(effect, "Type"))))} |\n");
        if (excluded.Count > 0)
        {
            text.Append("\n## Excluded capture records\n\nThese may have separate later-model acceptance, but the indexer does not reinterpret their captured failure counters.\n\n");
            foreach ((string archive, string reason) in excluded) text.Append($"- `{archive}`: {reason}.\n");
        }
        text.Append("\n## Refresh\n\n```powershell\npwsh -NoProfile -File scripts/Get-CardCoverageInventory.ps1\npwsh -NoProfile -File scripts/Get-CardCoverageInventory.ps1 -Check -SkipBuild\n```\n\n`-Check` only verifies catalog facts, manifest hashes and deterministic inventory output. It does not run the full model regression. New original-card acceptance should add a native witness with positive/control paths and independent replay before increasing evidence claims.\n");
        return text.ToString();
    }

    private static FixtureValue[] Array(FixtureValue value, string name) => value.ValueKind == FixtureKind.Object && value.TryGetProperty(name, out FixtureValue items) && items.ValueKind == FixtureKind.Array ? items.EnumerateArray().ToArray() : [];
    private static IEnumerable<string> Strings(FixtureValue value, string name) => Array(value, name).Select(item => item.GetString()!);
    private static bool Flag(FixtureValue value, string name) => value.ValueKind == FixtureKind.Object && value.TryGetProperty(name, out FixtureValue flag) && flag.ValueKind == FixtureKind.True;
    private static string Text(FixtureValue value, string name) => value.ValueKind == FixtureKind.Object && value.TryGetProperty(name, out FixtureValue text) && text.ValueKind == FixtureKind.String ? text.GetString()! : "";
    private static string Join(IEnumerable<string> values) => string.Join(";", values.Distinct().Order(StringComparer.Ordinal));
    private static string Hash(string path) { using var stream = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant(); }
    private static void Row(StringBuilder output, params string[] cells) => output.Append(string.Join('\t', cells.Select(cell => "\"" + cell.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\""))).Append('\n');
    private static string Escape(string value) => value.Replace("|", "\\|").Replace("\n", " ").Replace("\r", " ");
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
