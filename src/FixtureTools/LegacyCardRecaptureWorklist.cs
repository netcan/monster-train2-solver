using System.Globalization;
using System.Text;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static partial class CardCoverageInventory
{
    private sealed record LegacyPlay(string Id, string Name, string Archive, int[] Actions);
    private sealed record LegacyRecapture(string Archive, string Sha256, string Scenario, LegacyPlay[] Cards, int Plays, int Turns);

    private static void WriteLegacyRecaptureWorklist(string workspace, Card[] eligible, int settledBattles)
    {
        var missing = eligible.Where(card => card.DirectPlays == 0).ToDictionary(card => card.Id, StringComparer.Ordinal);
        string fixtures = Path.Combine(workspace, "tests", "fixtures");
        var catalog = new FixtureCatalog(fixtures);
        var manifest = File.ReadLines(Path.Combine(fixtures, "manifest.tsv")).Skip(1)
            .Select(line => line.Split('\t')).ToDictionary(row => row[0], StringComparer.Ordinal);
        string oldCatalog = Path.Combine(workspace, "tests", "catalogs", "card-catalog.mt2f");
        Require(Hash(oldCatalog) == CatalogHash, "Original legacy card catalog hash changed.");
        using var oldDefinitions = FixtureDocument.Read(oldCatalog);
        var legacyCards = Array(oldDefinitions.RootElement, "Cards").Select(value => new Card(value))
            .ToDictionary(card => card.Id, StringComparer.Ordinal);
        AttachKillTriggerDefinitions(oldDefinitions.RootElement, legacyCards);

        var candidates = ReadLegacyPlays(Path.Combine(fixtures, "inventory", "card-play-evidence.tsv"), missing)
            .GroupBy(play => play.Archive, StringComparer.Ordinal).ToArray();
        var worklist = new List<LegacyRecapture>();
        foreach (var group in candidates)
        {
            string path = catalog.Resolve(group.Key);
            string[] registration = manifest[group.Key];
            long expectedBytes = long.Parse(registration[4], CultureInfo.InvariantCulture);
            Require(new FileInfo(path).Length == expectedBytes && Hash(path).Equals(registration[5], StringComparison.OrdinalIgnoreCase),
                "Legacy archive integrity failed: " + group.Key);
            using var fixture = FixtureDocument.Read(path);
            FixtureValue root = fixture.RootElement;
            if (Text(root, "GameModuleMvid") != GameModule || BattleCounters.Any(name =>
                !root.TryGetProperty(name, out var counter) || counter.ValueKind != FixtureKind.Number || counter.GetInt32() != 0))
                continue;

            FixtureValue[] actions = Array(root, "Actions"), turns = Array(root, "Turns");
            if (!IsContinuousLegacyBattle(actions, turns)) continue;
            LegacyPlay[] cards = group.OrderBy(play => play.Name, StringComparer.Ordinal).ThenBy(play => play.Id, StringComparer.Ordinal).ToArray();
            foreach (LegacyPlay card in cards)
            foreach (int index in card.Actions)
            {
                Require(index >= 0 && index < actions.Length, "Legacy original witness index is outside " + group.Key);
                FixtureValue record = actions[index];
                FixtureValue before = record.GetProperty("Before");
                FixtureValue context = before.GetProperty("Spawn").GetProperty("Train").GetProperty("Context");
                int instance = record.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
                FixtureValue rule = Array(before.GetProperty("PlayRules"), "Cards").FirstOrDefault(value => Text(value, "DataId") == card.Id);
                Require(FindCardId(context, instance) == card.Id && MatchesOriginalPlay(legacyCards[card.Id], rule, context, instance),
                    $"Legacy witness differs from its original definition in {group.Key} action {index}.");
            }
            worklist.Add(new LegacyRecapture("tests/fixtures/" + registration[7], registration[5], Text(root, "ModifierScenario"),
                cards, actions.Length, turns.Length));
        }

        var output = new StringBuilder();
        Row(output, "SourceVersion", "TargetVersion", "Archive", "ArchiveSHA256", "ModifierScenario", "UsefulCards", "Plays", "EndTurns",
            "AssetKeys", "DataIds", "LegacyActionIndices");
        foreach (LegacyRecapture battle in worklist.OrderByDescending(item => item.Cards.Length)
            .ThenBy(item => item.Plays + item.Turns).ThenBy(item => item.Archive, StringComparer.Ordinal))
        {
            Row(output, "2.2.1", CurrentVersion, battle.Archive, battle.Sha256, battle.Scenario,
                battle.Cards.Length.ToString(CultureInfo.InvariantCulture), battle.Plays.ToString(CultureInfo.InvariantCulture),
                battle.Turns.ToString(CultureInfo.InvariantCulture), string.Join(',', battle.Cards.Select(card => card.Name)),
                string.Join(',', battle.Cards.Select(card => card.Id)),
                string.Join('|', battle.Cards.Select(card => string.Join(',', card.Actions))));
        }
        int usefulCards = worklist.SelectMany(battle => battle.Cards).Select(card => card.Id).Distinct().Count();
        Console.Error.WriteLine($"CARD-LEGACY-RECAPTURE-WORKLIST {CurrentVersion} currentSettledBattles={settledBattles} " +
            $"missingDefinitionMatch={missing.Count} candidateArchives={candidates.Length} continuousArchives={worklist.Count} usefulCards={usefulCards}. " +
            "Legacy observations rank recapture priority. Current coverage requires fresh native evidence.");
        Console.Write(output.ToString());
    }

    private static IEnumerable<LegacyPlay> ReadLegacyPlays(string path, IReadOnlyDictionary<string, Card> missing)
    {
        using var lines = File.ReadLines(path).GetEnumerator();
        Require(lines.MoveNext() && lines.Current == "\"DataId\"\t\"AssetKey\"\t\"Archive\"\t\"MatchingDefinitionPlays\"\t\"OtherRecordedPlays\"\t\"MatchingActionIndices\"\t\"OtherActionIndices\"",
            "Unknown legacy card-play evidence header.");
        var seen = new HashSet<(string, string)>();
        while (lines.MoveNext())
        {
            string[] row = lines.Current.Split('\t');
            Require(row.Length == 7 && row.All(cell => cell.Length >= 2 && cell[0] == '"' && cell[^1] == '"'),
                "Invalid legacy card-play evidence row.");
            string id = row[0][1..^1];
            if (!missing.TryGetValue(id, out Card? card) || row[3] == "\"0\"") continue;
            string archive = row[2][1..^1];
            int[] actions = row[5][1..^1].Split(',').Select(index => int.Parse(index, NumberStyles.None, CultureInfo.InvariantCulture)).ToArray();
            Require(seen.Add((id, archive)) && actions.Distinct().Count() == actions.Length &&
                int.Parse(row[3][1..^1], NumberStyles.None, CultureInfo.InvariantCulture) == actions.Length,
                "Invalid or duplicate legacy direct-play coordinates in " + archive);
            yield return new LegacyPlay(id, card.Name, archive, actions);
        }
    }

    private static bool IsContinuousLegacyBattle(FixtureValue[] actions, FixtureValue[] turns)
    {
        if (actions.Length == 0 || turns.Length == 0) return false;
        static int DecisionTurn(FixtureValue row) => row.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32();
        int actionIndex = 0;
        FixtureValue prior = default;
        bool terminal = false;
        foreach (FixtureValue turn in turns)
        {
            int number = DecisionTurn(turn);
            while (actionIndex < actions.Length && DecisionTurn(actions[actionIndex]) <= number)
                if (!Consume(actions[actionIndex++])) return false;
            if (!Consume(turn)) return false;
        }
        while (actionIndex < actions.Length)
            if (!Consume(actions[actionIndex++])) return false;
        return terminal;

        bool Consume(FixtureValue record)
        {
            if (terminal || !AcceptedPlay(record)) return false;
            FixtureValue before = record.GetProperty("Before");
            if (prior.ValueKind == FixtureKind.Undefined ? before.GetProperty("Spawn").GetProperty("Turn").GetInt32() != 0 :
                !SameLegacyDecision(prior, before, "$")) return false;
            prior = record.GetProperty("Actual");
            terminal = record.GetProperty("ActualOutcome").GetInt32() is (int)RoomOutcome.BattleWon or (int)RoomOutcome.PlayerDefeated;
            return true;
        }
    }

    private static bool SameLegacyDecision(FixtureValue left, FixtureValue right, string path)
    {
        if (left.ContentEquals(right)) return true;
        if (left.ValueKind != right.ValueKind) return false;
        if (left.ValueKind == FixtureKind.Array)
            return left.GetArrayLength() == right.GetArrayLength() &&
                left.EnumerateArray().Zip(right.EnumerateArray()).All(pair => SameLegacyDecision(pair.First, pair.Second, path + "[]"));
        if (left.ValueKind != FixtureKind.Object) return false;

        // The same UI fields are projected out of settled comparisons in BattleStateKeys.
        bool queryFrame = path is "$.Spawn.Train.Context.QueryFrame" or "$.Spawn.Train.Rooms[].Context.QueryFrame";
        bool spawnPoints = path is "$.Spawn.Train.Context.SpawnPoints" or "$.Spawn.Train.Rooms[].Context.SpawnPoints";
        bool Include(FixtureProperty property) => !(path == "$" && property.Name == "SelectedRoom") &&
            !(queryFrame && property.Name == "SelectedRoomIndex") && !(spawnPoints && property.Name == "NextPreviewCopyId");
        var expected = left.EnumerateObject().Where(Include).ToDictionary(property => property.Name, property => property.Value);
        var observed = right.EnumerateObject().Where(Include).ToDictionary(property => property.Name, property => property.Value);
        return expected.Count == observed.Count && expected.All(property => observed.TryGetValue(property.Key, out var other) &&
            SameLegacyDecision(property.Value, other, path + "." + property.Key));
    }
}
