using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RoomSlayStatusChecks
{
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        const string reason = "Unsupported triggered all-status adjustment definition.";
        var rows = NativeBranchObservationChecks.Replay(source, reason);
        Require(rows.Length == 1 && !rows[0].GetProperty("IsEndTurn").GetBoolean(), "Missing isolated champion deployment.");
        var action = rows[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var actual = rows[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var champion = actual.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.SpawnerCardId == 4);
        var effect = champion.Triggers.Where(trigger => trigger.Kind == "OnKill").SelectMany(trigger => trigger.Effects)
            .Single(effect => effect.Type == "CardEffectAdjustAllStatusEffects").Action;
        Require(action.CardInstanceId == 4 && action.RoomIndex == 0 && effect is { Target: "Room", Value: 1,
            AllowPlayer: true, AllowEnemy: false, StatusAdjustmentFilter: 1 } && effect.Statuses.Count == 0 &&
            effect.Tests?.StrictTargets == true, "Lost the natural rank-two champion's room positive-status slay signature.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("ROOM-SLAY-STATUS PASS: native rank-two champion deployment/full state/RNG; " +
            "original rejection preserved; slay execution requires separate combat evidence; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
