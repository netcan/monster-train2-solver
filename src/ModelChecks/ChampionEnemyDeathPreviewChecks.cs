using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ChampionEnemyDeathPreviewChecks
{
    private const string Reason = "Native end-turn preview summon allocation differs after decision9.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, (old, actual) =>
        {
            Require(old.Spawn.NextUnitId == 14 && actual.Spawn.NextUnitId == 13 &&
                old.Spawn.Train.Context!.NextUnitId == 14 && actual.Spawn.Train.Context!.NextUnitId == 13 &&
                old.Spawn.Train.Context.SpawnPoints!.Units.Single(point => point.UnitId == 13) is
                    { SpawnedInPreview: true, Current: null, LastKnown: null } &&
                actual.Spawn.Train.Context.SpawnPoints!.Units.All(point => point.UnitId < 13),
                "Lost original unobserved preview13 allocation.");
            JsonNode normalized = JsonNode.Parse(NativeBranchObservationChecks.Key(old))!;
            normalized["NextUnitId"] = 13;
            normalized["Context"]!["NextUnitId"] = 13;
            var points = normalized["Context"]!["SpawnPoints"]!["Units"]!.AsArray();
            points.Remove(points.Single(point => point!["UnitId"]!.GetValue<int>() == 13));
            string difference = ModelJson.Difference(normalized.ToJsonString(), NativeBranchObservationChecks.Key(actual))!;
            Require(difference == null, "Historical difference exceeds unobserved preview allocation: " + difference);
        });
        Require(rows.Length == 9 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Incomplete original second-battle prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 1 && before.Spawn.Train.Context is
            { NextUnitId: 9, SummonCatalog: null, Enchantments.AutomaticLifecycle: true } &&
            actual.Spawn.Turn == 2 && actual.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Single(unit => unit.Id == 12) is { AssetKey: "TreasureBagLvl1Gold", Health: 1 },
            "Missing original champion, next wave and live treasure boundary.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Champion enemy-death preview differs from full native state/forecast/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Champion preview mutated its parent.");
        var temporary = new CombatUnit(13, "MonsterFunguy", CombatTeam.Player, 3, 8, 8, true, false, false, []);
        foreach (string control in new[] { "Original", "Card", "Asset", "Stacks", "Observer" })
        {
            var node = JsonSerializer.SerializeToNode(actual, ModelJson.Options)!;
            var context = node["Spawn"]!["Train"]!["Context"]!;
            var champion = node["Spawn"]!["Train"]!["Rooms"]!.AsArray().SelectMany(room => room!["Units"]!.AsArray())
                .Single(unit => unit!["Id"]!.GetValue<int>() == 2)!;
            var action = champion["Triggers"]![0]!["Effects"]![0]!["Action"]!;
            if (control == "Card") champion["SpawnerCardId"] = 0;
            if (control == "Asset") champion["AssetKey"] = "uncalibrated-champion";
            if (control == "Stacks") { action["Value"] = 3; action["Statuses"]![0]!["Stacks"] = 3; }
            if (control == "Observer") context["SummonCatalog"] = JsonSerializer.SerializeToNode(new TriggeredSummonCatalog([], [], []), ModelJson.Options);
            var controlled = node.Deserialize<BattleTurnState>(ModelJson.Options)!;
            var primary = controlled.Spawn.Train.Context!;
            var result = PreviewBirthModel.ForgetUnobservedPlayers(controlled.Spawn.Train, primary.WithNextUnitId(14),
                13, primary.NextCardId, [temporary], out int[] forgotten);
            Require(result.NextUnitId == (control == "Original" ? 13 : 14) &&
                forgotten.SequenceEqual(control == "Original" ? new[] { 13 } : Array.Empty<int>()),
                "Unknown or explicitly observed preview scope lost its identity: " + control);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("CHAMPION-ENEMY-DEATH-PREVIEW PASS:9 native inputs/full state/forecast/RNG, unobserved two-stack Fun Guy allocation, four immutable branches/four retention controls; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
