using System.Globalization;
using System.Text;
using MonsterTrain2Poju.Fixtures;

internal static class RelicCoverageInventory
{
    private const string PreviousVersion = "2.2.1";
    private const string PreviousModule = "8fb07b96-f4db-4d2b-884d-c00536d6ccf4";
    private const string PreviousCatalogHash = "bdca7705ebd9d42e7d4d48370ba829e99133adfc047e82b7203736abb0394fd2";
    private const string CurrentCatalogHash = "86ff34d5ffc0c70b443c0adfb9a59b9a042bf96d761779e86df9f9850faaed2d";

    private sealed record Witness(string Archive, string Snapshot, int OwnerIndex);
    private sealed record DefinitionChange(Relic Current, Relic? Previous, IReadOnlyList<string> Fields);

    private sealed class Relic(FixtureValue definition)
    {
        internal readonly FixtureValue Definition = definition;
        internal readonly string Id = Text(definition, "Id");
        internal readonly string AssetKey = Text(definition, "AssetKey");
        internal readonly bool PlacementExcluded = definition.GetProperty("DisallowedInPlacementPhase").GetBoolean();
        internal readonly string[] EffectTypes = definition.GetProperty("Effects").EnumerateArray().Select(effect => Text(effect, "RuntimeType")).ToArray();
        internal string EffectSequence => string.Join(",", EffectTypes);
        internal int ObservedBattles;
        internal int MatchedBattles;
        internal Witness? IdentityWitness;
        internal Witness? RosterWitness;
        internal Witness? BestWitness => RosterWitness ?? IdentityWitness;
        internal string Observation => MatchedBattles > 0 ? "ordered-roster-match" :
            ObservedBattles > 0 ? "identity-observed" : "no-active-observation";

        internal bool MatchesRoster(FixtureValue owner) => Text(owner, "AssetKey") == AssetKey &&
            owner.TryGetProperty("IsCovenant", out FixtureValue covenant) && covenant.ValueKind == FixtureKind.False &&
            owner.TryGetProperty("DisallowedInPlacementPhase", out FixtureValue placement) &&
            placement.ValueKind == (PlacementExcluded ? FixtureKind.True : FixtureKind.False) &&
            Array(owner, "EffectTypes").Select(value => value.GetString()).SequenceEqual(EffectTypes, StringComparer.Ordinal);
    }

    internal static void RunCurrent(string workspace, string output, bool check, bool includeWorklist = false)
    {
        string directory = Game231FixtureCatalog.DirectoryFor(workspace);
        var registrations = Game231FixtureCatalog.Load(directory);
        var previous = ReadCatalog(directory, registrations, PreviousVersion, PreviousModule, PreviousCatalogHash, 224);
        var relics = ReadCatalog(directory, registrations, Game231FixtureCatalog.Version, Game231FixtureCatalog.Module, CurrentCatalogHash, 236);
        List<DefinitionChange> changes = CompareCatalogs(previous, relics);

        int battles = 0, stage = 0, snapshots = 0;
        foreach (Game231FixtureRegistration archive in registrations.Values)
        {
            if (archive.Path == CatalogPath(PreviousVersion) || archive.Path == CatalogPath(Game231FixtureCatalog.Version)) continue;
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
            Row(report, [Game231FixtureCatalog.Version, Game231FixtureCatalog.Module, relic.Id, relic.AssetKey, .. ObservationCells(relic)]);
        }
        var priorityReport = new StringBuilder();
        Row(priorityReport, "PreviousGameVersion", "CurrentGameVersion", "DataId", "AssetKey", "PreviousAssetKey",
            "DefinitionChange", "ChangedFields", "PreviousEffectTypes", "CurrentEffectTypes", "CurrentRosterObservation",
            "CurrentObservedBattles", "CurrentRosterMatchedBattles", "CurrentWitnessArchive", "CurrentWitnessSnapshot", "CurrentWitnessOwnerIndex");
        foreach (DefinitionChange change in changes)
        {
            Relic relic = change.Current;
            Relic? prior = change.Previous;
            Row(priorityReport, [PreviousVersion, Game231FixtureCatalog.Version, relic.Id, relic.AssetKey, prior?.AssetKey ?? "",
                prior == null ? "added" : "changed", string.Join(",", change.Fields), string.Join(",", prior?.EffectTypes ?? []),
                string.Join(",", relic.EffectTypes), .. ObservationCells(relic)]);
        }
        output = Path.GetFullPath(output);
        string results = Path.Combine(Path.GetFullPath(workspace), "results");
        string priorityOutput = Path.Combine(results, "relic-priority-version-delta-" + Game231FixtureCatalog.Version + ".tsv");
        string worklistOutput = Path.Combine(results, "relic-effect-worklist-" + Game231FixtureCatalog.Version + ".tsv");
        if (string.Equals(output, priorityOutput, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Relic observations and version differences require distinct output paths.", nameof(output));
        if (includeWorklist && string.Equals(output, worklistOutput, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Relic observations and the effect worklist require distinct output paths.", nameof(output));
        WriteReport(output, report, check);
        WriteReport(priorityOutput, priorityReport, check);
        if (includeWorklist) WriteEffectWorklist(relics.Values, worklistOutput, check);
        int rosterMatches = relics.Values.Count(relic => relic.MatchedBattles > 0);
        int identityOnly = relics.Values.Count(relic => relic.ObservedBattles > 0 && relic.MatchedBattles == 0);
        Console.WriteLine($"RELIC-OBSERVATION {Game231FixtureCatalog.Version} {(check ? "CHECK" : "WRITE")} PASS registered={registrations.Count} settledBattles={battles} stageBattles={stage} decisionSnapshots={snapshots}");
        Console.WriteLine($"collectible={relics.Count} orderedRosterMatch={rosterMatches} identityObservedOnly={identityOnly} noActiveObservation={relics.Count - rosterMatches - identityOnly} report={output}");
        int added = changes.Count(change => change.Previous == null);
        int priorityMatches = changes.Count(change => change.Current.MatchedBattles > 0);
        int priorityIdentityOnly = changes.Count(change => change.Current.ObservedBattles > 0 && change.Current.MatchedBattles == 0);
        Console.WriteLine($"priority={changes.Count} added={added} changed={changes.Count - added} orderedRosterMatch={priorityMatches} identityObservedOnly={priorityIdentityOnly} noActiveObservation={changes.Count - priorityMatches - priorityIdentityOnly} report={priorityOutput}");
    }

    private static void WriteEffectWorklist(IEnumerable<Relic> relics, string output, bool check)
    {
        Relic[] observed = relics.Where(relic => relic.RosterWitness != null).ToArray();
        var groups = relics.Where(relic => relic.RosterWitness == null)
            .GroupBy(relic => relic.EffectSequence, StringComparer.Ordinal).Select(group => new
        {
            Effects = group.Key,
            Gaps = group.OrderBy(relic => relic.AssetKey, StringComparer.Ordinal).ThenBy(relic => relic.Id, StringComparer.Ordinal).ToArray(),
            Reference = observed.Select(relic => new
            {
                Relic = relic,
                ExactOrder = relic.EffectSequence == group.Key,
                SharedTypes = group.First().EffectTypes.Intersect(relic.EffectTypes, StringComparer.Ordinal).ToArray()
            }).Where(peer => peer.SharedTypes.Length > 0).OrderByDescending(peer => peer.ExactOrder)
                .ThenByDescending(peer => peer.SharedTypes.Length).ThenByDescending(peer => peer.Relic.MatchedBattles)
                .ThenBy(peer => peer.Relic.Id, StringComparer.Ordinal).FirstOrDefault()
        })
            .OrderByDescending(group => group.Reference != null).ThenByDescending(group => group.Gaps.Length)
            .ThenBy(group => group.Effects, StringComparer.Ordinal).ToArray();

        var report = new StringBuilder();
        Row(report, "GameVersion", "GameModuleMvid", "OrderedEffectTypes", "MissingRosterCount", "MissingRosterDataIds",
            "MissingRosterAssetKeys", "ReferenceMatch", "SharedEffectTypes", "ReferenceDataId", "ReferenceAssetKey", "ReferenceArchive", "ReferenceSnapshot", "ReferenceOwnerIndex");
        foreach (var group in groups)
        {
            var reference = group.Reference;
            Witness? witness = reference?.Relic.RosterWitness;
            string match = reference == null ? "" : reference.ExactOrder ? "ordered-effect-types" : "shared-effect-types";
            Row(report, Game231FixtureCatalog.Version, Game231FixtureCatalog.Module, group.Effects,
                group.Gaps.Length.ToString(CultureInfo.InvariantCulture), string.Join(",", group.Gaps.Select(relic => relic.Id)),
                string.Join(",", group.Gaps.Select(relic => relic.AssetKey)), match, string.Join(",", reference?.SharedTypes ?? []),
                reference?.Relic.Id ?? "", reference?.Relic.AssetKey ?? "",
                witness?.Archive ?? "", witness?.Snapshot ?? "", witness?.OwnerIndex.ToString(CultureInfo.InvariantCulture) ?? "");
        }

        WriteReport(output, report, check);
        Console.WriteLine($"RELIC-EFFECT-WORKLIST {Game231FixtureCatalog.Version} {(check ? "CHECK" : "WRITE")} PASS groups={groups.Length} " +
            $"withRosterReference={groups.Count(group => group.Reference != null)} missingRoster={groups.Sum(group => group.Gaps.Length)} report={output}");
    }

    private static string CatalogPath(string version) => version + "/relic-catalog.mt2f";

    private static Dictionary<string, Relic> ReadCatalog(string directory,
        IReadOnlyDictionary<string, Game231FixtureRegistration> registrations, string version, string module, string hash, int count)
    {
        if (!registrations.TryGetValue(CatalogPath(version), out Game231FixtureRegistration? catalog) || catalog.Sha256 != hash)
            throw new InvalidDataException("Relic comparison catalog manifest changed for " + version);
        using var document = FixtureDocument.Read(catalog.Verify(directory));
        FixtureValue root = document.RootElement;
        if (root.GetProperty("Schema").GetInt32() != 9 || Text(root, "GameVersion") != version ||
            Text(root, "GameModuleMvid") != module ||
            !root.GetProperty("FrameBefore").ContentEquals(root.GetProperty("FrameAfter")) ||
            !root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")))
            throw new InvalidDataException("Relic catalog version, frame or RNG differs for " + version);
        var relics = root.GetProperty("Relics").EnumerateArray()
            .Where(value => Text(value, "Collection") == "collectableRelicDatas")
            .Select(value => new Relic(value)).ToDictionary(relic => relic.Id, StringComparer.Ordinal);
        if (relics.Count != count || relics.Values.Any(relic => !Guid.TryParse(relic.Id, out _) ||
            string.IsNullOrEmpty(relic.AssetKey) || relic.EffectTypes.Length == 0 || relic.EffectTypes.Any(string.IsNullOrEmpty)))
            throw new InvalidDataException($"Expected {count} unique original collectible relic definitions for {version}.");
        return relics;
    }

    private static List<DefinitionChange> CompareCatalogs(Dictionary<string, Relic> previous, Dictionary<string, Relic> current)
    {
        if (previous.Keys.Any(id => !current.ContainsKey(id)))
            throw new InvalidDataException("A pinned older collectible relic ID is missing from the current catalog.");
        var changes = new List<DefinitionChange>();
        foreach (Relic relic in current.Values.OrderBy(relic => relic.AssetKey, StringComparer.Ordinal).ThenBy(relic => relic.Id, StringComparer.Ordinal))
        {
            if (!previous.TryGetValue(relic.Id, out Relic? prior))
            {
                changes.Add(new DefinitionChange(relic, null, []));
                continue;
            }
            var paths = new List<string>();
            CollectChangedFields(prior.Definition, relic.Definition, "", paths);
            if (paths.Count > 0) changes.Add(new DefinitionChange(relic, prior, paths));
        }
        if (changes.Count(change => change.Previous == null) != 12 || changes.Count(change => change.Previous != null) != 13)
            throw new InvalidDataException("Expected 12 additions and 13 changed collectible relic definitions between the pinned releases.");
        return changes;
    }

    private static void CollectChangedFields(FixtureValue before, FixtureValue after, string path, List<string> differences)
    {
        if (before.ContentEquals(after)) return;
        if (before.ValueKind == FixtureKind.Object && after.ValueKind == FixtureKind.Object)
        {
            var oldFields = before.EnumerateObject().ToDictionary(field => field.Name, field => field.Value, StringComparer.Ordinal);
            var newFields = after.EnumerateObject().ToDictionary(field => field.Name, field => field.Value, StringComparer.Ordinal);
            foreach (string field in oldFields.Keys.Union(newFields.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal))
            {
                string fieldPath = path.Length == 0 ? field : path + "." + field;
                if (oldFields.TryGetValue(field, out FixtureValue oldValue) && newFields.TryGetValue(field, out FixtureValue newValue))
                    CollectChangedFields(oldValue, newValue, fieldPath, differences);
                else
                    differences.Add(fieldPath);
            }
        }
        else if (before.ValueKind == FixtureKind.Array && after.ValueKind == FixtureKind.Array)
        {
            var oldItems = before.EnumerateArray().ToArray();
            var newItems = after.EnumerateArray().ToArray();
            if (oldItems.Length != newItems.Length) differences.Add(path + ".Length");
            for (int index = 0; index < Math.Min(oldItems.Length, newItems.Length); index++)
                CollectChangedFields(oldItems[index], newItems[index], path + "[" + index.ToString(CultureInfo.InvariantCulture) + "]", differences);
        }
        else
            differences.Add(path);
    }

    private static string[] ObservationCells(Relic relic)
    {
        Witness? witness = relic.BestWitness;
        return [relic.Observation, relic.ObservedBattles.ToString(CultureInfo.InvariantCulture),
            relic.MatchedBattles.ToString(CultureInfo.InvariantCulture), witness?.Archive ?? "", witness?.Snapshot ?? "",
            witness?.OwnerIndex.ToString(CultureInfo.InvariantCulture) ?? ""];
    }

    private static void WriteReport(string output, StringBuilder report, bool check)
    {
        string content = report.ToString();
        if (check)
        {
            if (!File.Exists(output) || File.ReadAllText(output) != content)
                throw new InvalidDataException("Current relic report is missing or stale: " + output);
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            File.WriteAllText(output, content, new UTF8Encoding(false));
        }
    }

    private static string Text(FixtureValue value, string name) =>
        value.ValueKind == FixtureKind.Object && value.TryGetProperty(name, out FixtureValue text) && text.ValueKind == FixtureKind.String ? text.GetString()! : "";

    private static IEnumerable<FixtureValue> Array(FixtureValue value, string name) =>
        value.ValueKind == FixtureKind.Object && value.TryGetProperty(name, out FixtureValue array) && array.ValueKind == FixtureKind.Array ? array.EnumerateArray() : [];

    private static void Row(StringBuilder output, params string[] cells) => output.Append(string.Join('\t', cells.Select(cell =>
        "\"" + cell.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\""))).Append('\n');
}
