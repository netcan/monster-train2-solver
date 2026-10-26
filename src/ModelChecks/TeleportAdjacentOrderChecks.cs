using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TeleportAdjacentOrderChecks
{
    private const string Reason = "Observed spawn order differs after native unit ability.";
    private static int[] Players(BattleTurnState state) => state.Spawn.Train.Rooms.Single(r => r.RoomIndex == 1)
        .Units.Where(u => u.Team == CombatTeam.Player).Select(u => u.Id).ToArray();

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, (old, actual) =>
            Require(Players(old).SequenceEqual(new[] { 5, 21, 4 }) &&
                Players(actual).SequenceEqual(new[] { 21, 4, 5 }), "Lost historical teleport order discrepancy."));
        Require(rows.Length == 19, "Wrong native teleport prefix.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(action.CardInstanceId == 26 && action.ActivatorUnitId == 21 && action.RoomIndex == 1 &&
            Players(before).SequenceEqual(new[] { 4, 5, 21 }), "Wrong multi-slot teleport input.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string native = NativeBranchObservationChecks.Key(rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && result.Outcome == RoomOutcome.Exchanged &&
                NativeBranchObservationChecks.Key(result.State!) == native &&
                JsonSerializer.Serialize(before, ModelJson.Options) == parent,
                "Teleport changed its native successor, RNG, or immutable parent.");
        });
        Console.WriteLine("TELEPORT-ADJACENT-ORDER PASS:19 native inputs/full state/RNG, retained two-slot order discrepancy and four immutable parallel branches; independentPair=" + replay.HasValue);
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
