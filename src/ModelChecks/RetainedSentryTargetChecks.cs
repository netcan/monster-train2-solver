using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RetainedSentryTargetChecks
{
    private const string Reason = "Observed retained enchantment destruction state";

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, Historical);
        Require(rows.Length == 11, "Changed natural Sentry/removal prefix.");
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit champion = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 2);
        Require(champion.Modifiers?.IsChampion == true && champion.Health == 15 && action.CardInstanceId == 36 &&
            action.TargetUnitId == 3 && action.RoomIndex == 2,
            "Changed natural lethal shift and later Sentry target.");
        Removed(actual);
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 8, _ =>
        {
            BattleActionResult result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Parallel lethal shift differs from the complete native state/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Queued Sentry replay mutated its parent.");
        if (replay is FixtureValue second)
        {
            NativeBranchObservationChecks.Replay(second, Reason, Historical);
            NativeBranchObservationChecks.Pair(source, second, originalPredictionPresent: true);
        }
        Console.WriteLine("RETAINED-SENTRY-TARGET PASS: eleven native inputs/full state/RNG, lethal equipment shift then Sentry keeps completed destruction, nonlethal prefix and eight immutable parallel branches; independentPair=" + replay.HasValue);
    }

    private static void Historical(BattleTurnState old, BattleTurnState actual)
    {
        EnchantmentRetainedUnit retained = old.Spawn.Train.Context!.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == 2);
        Require(retained.RoomIndex == 2 && retained.Unit.Health == 0 && retained.Unit.DeathState?.IsDestroyed == false,
            "Missing original stale queued target counterexample.");
        Removed(actual);
    }

    private static void Removed(BattleTurnState actual)
    {
        EnchantmentRetainedUnit retained = actual.Spawn.Train.Context!.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == 2);
        Require(retained.RoomIndex == -1 && retained.Unit.Health == 0 && retained.Unit.DeathState is
            { IsDestroyed: true, HasFinishedDying: true, IsBeingRemoved: true } &&
            actual.Spawn.Train.Rooms.All(room => room.Units.All(unit => unit.Id != 2)),
            "Removed champion was restored by the later Sentry callback.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
