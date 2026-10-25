using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class StatusCallbackRoomSyncChecks
{
    // The request's initial diagnosis is retained as immutable evidence.
    private const string Reason = "Summon played-room cache includes the newly spawned unit.";
    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, Historical);
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var context = actual.Spawn.Train.Context!;
        var card = context.FindCard(19)!;
        Require(rows.Length == 6 && card.DataId == "3a8240bd-8b96-4d6e-b2d1-49ff8b5d8022" &&
            card.PlayedRoomUnitIds!.Order().SequenceEqual(new[] { 2, 3, 4, 8, 11 }) &&
            card.RawPlayedRoomUnitIds!.Order().SequenceEqual(card.PlayedRoomUnitIds.Order()) &&
            context.Enchantments!.RetainedUnits.Any(actor => actor.Unit.Id == 13 && actor.Unit.Health == 5 &&
                actor.Unit.Team == CombatTeam.Enemy && actor.Unit.DeathState?.IsDestroyed == false),
            "Missing the dead-preview enemy exclusion and its later restored primary state.");
        Console.WriteLine("STATUS-CALLBACK-ROOM-SYNC PASS:6 native inputs/full successors/RNG; status callbacks retain preview deaths and valid cache references; immutable parents.");
    }
    private static void Historical(BattleTurnState predicted, BattleTurnState actual)
    {
        var oldCache = predicted.Spawn.Train.Context!.FindCard(19)!.PlayedRoomUnitIds!;
        var nativeCache = actual.Spawn.Train.Context!.FindCard(19)!.PlayedRoomUnitIds!;
        Require(oldCache.Contains(13) && !nativeCache.Contains(13) &&
            oldCache.Where(id => id != 13).Order().SequenceEqual(nativeCache.Order()),
            "Wrong historical stale enemy snapshot/cache mismatch.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        NativeBranchObservationChecks.Pair(source, replay, originalPredictionPresent: true);
        Console.WriteLine("STATUS-CALLBACK-ROOM-SYNC-PAIR PASS:original full cache mismatch and independent repaired forecast preserved.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
