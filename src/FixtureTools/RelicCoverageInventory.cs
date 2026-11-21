using System.Globalization;
using System.Text;
using MonsterTrain2Poju.Fixtures;

internal static class RelicCoverageInventory
{
    private const string CatalogPath = "2.3.1/relic-catalog.mt2f";
    private const string CatalogHash = "86ff34d5ffc0c70b443c0adfb9a59b9a042bf96d761779e86df9f9850faaed2d";

    private sealed record Witness(string Archive, string Snapshot, int OwnerIndex);

    private sealed class Relic(FixtureValue definition)
    {
        internal readonly string Id = Text(definition, "Id");
        internal readonly string AssetKey = Text(definition, "AssetKey");
        internal readonly bool PlacementExcluded = definition.GetProperty("DisallowedInPlacementPhase").GetBoolean();
        internal readonly string[] EffectTypes = definition.GetProperty("Effects").EnumerateArray().Select(effect => Text(effect, "RuntimeType")).ToArray();
        internal int ObservedBattles;
        internal int MatchedBattles;
        internal Witness? IdentityWitness;
        internal Witness? RosterWitness;

        internal bool MatchesRoster(FixtureValue owner) => Text(owner, "AssetKey") == AssetKey &&
            owner.TryGetProperty("IsCovenant", out FixtureValue covenant) && covenant.ValueKind == FixtureKind.False &&
            owner.TryGetProperty("DisallowedInPlacementPhase", out FixtureValue placement) &&
            placement.ValueKind == (PlacementExcluded ? FixtureKind.True : FixtureKind.False) &&
            Array(owner, "EffectTypes").Select(value => value.GetString()).SequenceEqual(EffectTypes, StringComparer.Ordinal);
    }

    internal static void RunCurrent(string workspace, string output, bool check)
    {
        string directory = Game231FixtureCatalog.DirectoryFor(workspace);
        var registrations = Game231FixtureCatalog.Load(directory);
        if (!registrations.TryGetValue(CatalogPath, out Game231FixtureRegistration? catalog) || catalog.Sha256 != CatalogHash)
            throw new InvalidDataException("Current relic catalog manifest differs from the pinned mechanism inventory source.");
        using var document = FixtureDocument.Read(catalog.Verify(directory));
        FixtureValue root = document.RootElement;
        if (root.GetProperty("Schema").GetInt32() != 9 || Text(root, "GameVersion") != Game231FixtureCatalog.Version ||
            Text(root, "GameModuleMvid") != Game231FixtureCatalog.Module ||
            !root.GetProperty("FrameBefore").ContentEquals(root.GetProperty("FrameAfter")) ||
            !root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")))
            throw new InvalidDataException("Current relic catalog version, frame or RNG differs.");
        var relics = root.GetProperty("Relics").EnumerateArray()
            .Where(value => Text(value, "Collection") == "collectableRelicDatas")
            .Select(value => new Relic(value)).ToDictionary(relic => relic.Id, StringComparer.Ordinal);
        if (relics.Count != 236 || relics.Values.Any(relic => !Guid.TryParse(relic.Id, out _) ||
            string.IsNullOrEmpty(relic.AssetKey) || relic.EffectTypes.Length == 0 || relic.EffectTypes.Any(string.IsNullOrEmpty)))
            throw new InvalidDataException("Expected 236 unique original collectible relic definitions.");

        int battles = 0, stage = 0, snapshots = 0;
        foreach (Game231FixtureRegistration archive in registrations.Values)
        {
            using var fixture = FixtureDocument.Read(archive.Verify(directory));
            FixtureValue battle = fixture.RootElement;
            if (Game231FixtureCatalog.Classify(archive, battle) != Game231FixtureKind.SettledBattle) continue;
            battles++;
            if (archive.Stage) stage++;
            var observed = new Dictionary<Relic, bool>();
            foreach (string sequence in new[] { "Actions", "Turns" })
            {
                int index = -1;
                foreach (FixtureValue record in battle.GetProperty(sequence).EnumerateArray())
                {
                    index++;
                    foreach (string side in new[] { "Before", "Actual" })
                    {
                        if (!record.TryGetProperty(side, out FixtureValue state) || state.ValueKind != FixtureKind.Object) continue;
                        FixtureValue context = state.GetProperty("Spawn").GetProperty("Train").GetProperty("Context");
                        snapshots++;
                        int ownerIndex = -1;
                        foreach (FixtureValue owner in Array(context, "Relics"))
                        {
                            ownerIndex++;
                            if (!relics.TryGetValue(Text(owner, "DataId"), out Relic? relic) ||
                                observed.TryGetValue(relic, out bool matched) && matched) continue;
                            bool matches = relic.MatchesRoster(owner);
                            observed[relic] = matches;
                            if (relic.IdentityWitness == null || matches && relic.RosterWitness == null)
                            {
                                var witness = new Witness(archive.Path, $"{sequence}[{index}].{side}", ownerIndex);
                                relic.IdentityWitness ??= witness;
                                if (matches) relic.RosterWitness ??= witness;
                            }
                        }
                    }
                }
            }
            foreach ((Relic relic, bool matched) in observed)
            {
                relic.ObservedBattles++;
                if (matched) relic.MatchedBattles++;
            }
        }

        var report = new StringBuilder();
        Row(report, "GameVersion", "GameModuleMvid", "DataId", "AssetKey", "Observation", "ObservedBattles", "RosterMatchedBattles",
            "WitnessArchive", "WitnessSnapshot", "WitnessOwnerIndex");
        foreach (Relic relic in relics.Values.OrderBy(relic => relic.Id, StringComparer.Ordinal))
        {
            Witness? witness = relic.RosterWitness ?? relic.IdentityWitness;
            string observation = relic.MatchedBattles > 0 ? "ordered-roster-match" : relic.ObservedBattles > 0 ? "identity-observed" : "no-active-observation";
            Row(report, Game231FixtureCatalog.Version, Game231FixtureCatalog.Module, relic.Id, relic.AssetKey, observation,
                relic.ObservedBattles.ToString(CultureInfo.InvariantCulture), relic.MatchedBattles.ToString(CultureInfo.InvariantCulture),
                witness?.Archive ?? "", witness?.Snapshot ?? "", witness?.OwnerIndex.ToString(CultureInfo.InvariantCulture) ?? "");
        }
        output = Path.GetFullPath(output);
        string content = report.ToString();
        if (check)
        {
            if (!File.Exists(output) || File.ReadAllText(output) != content)
                throw new InvalidDataException("Current relic observations are missing or stale: " + output);
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            File.WriteAllText(output, content, new UTF8Encoding(false));
        }
        int rosterMatches = relics.Values.Count(relic => relic.MatchedBattles > 0);
        int identityOnly = relics.Values.Count(relic => relic.ObservedBattles > 0 && relic.MatchedBattles == 0);
        Console.WriteLine($"RELIC-OBSERVATION {Game231FixtureCatalog.Version} {(check ? "CHECK" : "WRITE")} PASS registered={registrations.Count} settledBattles={battles} stageBattles={stage} decisionSnapshots={snapshots}");
        Console.WriteLine($"collectible={relics.Count} orderedRosterMatch={rosterMatches} identityObservedOnly={identityOnly} noActiveObservation={relics.Count - rosterMatches - identityOnly} report={output}");
    }

    private static string Text(FixtureValue value, string name) =>
        value.ValueKind == FixtureKind.Object && value.TryGetProperty(name, out FixtureValue text) && text.ValueKind == FixtureKind.String ? text.GetString()! : "";

    private static IEnumerable<FixtureValue> Array(FixtureValue value, string name) =>
        value.ValueKind == FixtureKind.Object && value.TryGetProperty(name, out FixtureValue array) && array.ValueKind == FixtureKind.Array ? array.EnumerateArray() : [];

    private static void Row(StringBuilder output, params string[] cells) => output.Append(string.Join('\t', cells.Select(cell =>
        "\"" + cell.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\""))).Append('\n');
}
