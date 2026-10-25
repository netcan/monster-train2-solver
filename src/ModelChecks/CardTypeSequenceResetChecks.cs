using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CardTypeSequenceResetChecks
{
    private const string Reason = "Start-turn context reset loses the three-card-type sequence reset before boss handling.";
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, Historical);
        NativeBranchObservationChecks.Replay(replay, Reason);
        NativeBranchObservationChecks.Pair(source, replay, originalPredictionPresent: true);
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(rows.Length == 9 && rows[^1].GetProperty("IsEndTurn").GetBoolean() &&
            before.Spawn.Turn == 1 && after.Spawn.Turn == 2 && Types(before).SequenceEqual(["Monster", "Spell"]) &&
            Types(after).Count == 0 && !before.Spawn.Train.Movement.Any(rule => rule.CompanionBoss) && before.Spawn.OuterBoss == null,
            "Missing the native ordinary-battle first nonempty sequence reset.");
        Console.WriteLine("CARD-TYPE-SEQUENCE-RESET PASS:9 independent paired native inputs/full state/RNG; two played types cleared at next turn without boss actions; original mismatch and immutable parents preserved.");
    }
    private static IReadOnlyList<string> Types(BattleTurnState state) => state.Spawn.Train.Context!.Relics!
        .Single(relic => relic.DataId == RelicCardTypeSequenceModel.DataId).ResourceEffects!
        .Single(effect => effect.Type == RelicCardTypeSequenceModel.Effect).CardTypeSequence!.PlayedTypes;
    private static void Historical(BattleTurnState predicted, BattleTurnState actual)
    {
        Require(Types(predicted).SequenceEqual(["Monster", "Spell"]) && Types(actual).Count == 0,
            "Original type-sequence reset mismatch was overwritten.");
        var node = JsonSerializer.SerializeToNode(predicted, ModelJson.Options)!;
        var relic = node["Spawn"]!["Train"]!["Context"]!["Relics"]!.AsArray()
            .Single(item => item!["DataId"]!.GetValue<string>() == RelicCardTypeSequenceModel.DataId)!;
        relic["ResourceEffects"]!.AsArray().Single(item => item!["Type"]!.GetValue<string>() == RelicCardTypeSequenceModel.Effect)!
            ["CardTypeSequence"]!["PlayedTypes"] = new JsonArray();
        Require(NativeBranchObservationChecks.Key(node.Deserialize<BattleTurnState>(ModelJson.Options)!) ==
            NativeBranchObservationChecks.Key(actual), "Historical mismatch extends beyond the stale sequence list.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
