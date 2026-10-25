using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PyrePaymentWorldChecks
{
    private const string Reason = "Pyre health payment is overwritten by the stale enchantment world.";
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, Historical);
        NativeBranchObservationChecks.Replay(replay, Reason);
        NativeBranchObservationChecks.Pair(source, replay, originalPredictionPresent: true);
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(rows.Length == 12 && !rows[^1].GetProperty("IsEndTurn").GetBoolean() &&
            before.Spawn.Turn == 1 && before.Energy == 0 && after.Energy == 0 &&
            before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.DataId == "5839cfaa-4784-4ec7-99f0-3dda42b939dc" &&
            Pyre(before).Health == 75 && Pyre(after).Health == 74 &&
            after.Spawn.Train.Context!.Enchantments!.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 74,
            "Missing the native nonlethal Ice Spear payment and shared aura-world update.");
        Console.WriteLine("PYRE-PAYMENT-WORLD PASS:12 independent paired native inputs/full state/RNG; unaffordable spell spends one Pyre HP in live and enchantment worlds; original mismatch and immutable parents retained.");
    }
    private static CombatUnit Pyre(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre);
    private static void Historical(BattleTurnState predicted, BattleTurnState actual)
    {
        Require(Pyre(predicted).Health == 75 && Pyre(actual).Health == 74, "Original payment mismatch was overwritten.");
        var node = JsonSerializer.SerializeToNode(predicted, ModelJson.Options)!;
        var train = node["Spawn"]!["Train"]!;
        foreach (var rooms in new[] { train["Rooms"]!, train["Context"]!["Enchantments"]!["Rooms"]! })
        {
            var pyre = rooms.AsArray().SelectMany(room => room!["Units"]!.AsArray())
                .Single(unit => unit!["IsPyre"]!.GetValue<bool>())!;
            Require(pyre["Health"]!.GetValue<int>() == 75, "Historical world payment changed.");
            pyre["Health"] = 74;
        }
        Require(NativeBranchObservationChecks.Key(node.Deserialize<BattleTurnState>(ModelJson.Options)!) ==
            NativeBranchObservationChecks.Key(actual), "Historical difference extends beyond the lost live/world Pyre HP payment.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
