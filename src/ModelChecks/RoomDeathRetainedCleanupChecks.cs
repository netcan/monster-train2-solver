using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RoomDeathRetainedCleanupChecks
{
    private const string Reason = "Observed room death retained actor survives canonical point cleanup.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, (historical, actual) =>
        {
            var old = historical.Spawn.Train.Context!;
            var native = actual.Spawn.Train.Context!;
            var corpse = old.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == 4);
            Require(corpse.Unit.AssetKey == "MonsterPricklyPuffball" && corpse.Unit.Health == 0 &&
                corpse.Unit.DeathState is { IsDestroyed: true, HasFinishedDying: true } &&
                native.Enchantments!.RetainedUnits.All(actor => actor.Unit.Id != 4) &&
                old.NextUnitId == 34 && native.NextUnitId == 34 &&
                old.Enchantments.RetainedUnits.Count == native.Enchantments.RetainedUnits.Count + 1,
                "Lost the original dead actor retained after its physical slot was compacted.");
        }, RoomOutcome.BattleWon);
        Require(rows.Length == 26 && rows[^1].GetProperty("IsEndTurn").GetBoolean(),
            "Incomplete natural second-battle terminal prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var context = actual.Spawn.Train.Context!;
        Require(before.Spawn.Turn == 4 && context.SummonCatalog != null &&
            context.NextUnitId == 34 && context.SpawnPoints!.Units.Single(unit => unit.UnitId == 4).Current == null &&
            context.SpawnPoints.Groups.All(group => !group.Occupants.Contains(4)) &&
            context.Enchantments!.RetainedUnits.Any(actor => actor.Unit.Id == 32 && actor.Preview &&
                actor.Unit.AssetKey == "MonsterFunguy") &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 33).AssetKey == "MonsterFunguy" &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 70,
            "Missing observed preview/live births, canonical removal or actual Pyre result.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason, boundaryOutcome: RoomOutcome.BattleWon);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string nativeKey = NativeBranchObservationChecks.Key(actual, terminal: true);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && result.Outcome == RoomOutcome.BattleWon &&
                NativeBranchObservationChecks.Key(result.State!, terminal: true) == nativeKey &&
                JsonSerializer.Serialize(before, ModelJson.Options) == parent,
                "Terminal birth ownership changed complete state/RNG or its immutable parent.");
        });
        Console.WriteLine("ROOM-DEATH-RETAINED-CLEANUP PASS:26 native inputs/full state/RNG, victory/Pyre70, " +
            "original stale corpse retained as evidence, preview32/live33 identities preserved, four immutable branches; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
