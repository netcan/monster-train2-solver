using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BoneDogPreviewLeakChecks
{
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length == 36 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native mismatch boundary.");
        var before = rows[^1].GetProperty("Root").Deserialize<BattleTurnState>()!;
        var historical = rows[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        RelicConditionState Condition(BattleTurnState state) => state.Spawn.Train.Context!.Relics!
            .Single(owner => owner.DataId == RelicPyreResurrectionModel.DataId).ResourceEffects!.Single().Conditions.Single();
        Require(!Condition(historical).Triggered && Condition(actual) is { Triggered: true, ValueAtLastTrigger: 1, DurationTriggerCount: 1 } &&
            before.Spawn.Train.Context!.QueryFrame!.PyreResurrectionCount == 0 &&
            actual.Spawn.Train.Context!.QueryFrame!.PyreResurrectionCount == 0, "Missing native preview-only condition difference.");
        CombatContext sourceContext = before.Spawn.Train.Context!;
        CombatContext observedContext = actual.Spawn.Train.Context!;
        CombatContext carried = RelicPyreResurrectionModel.CarryPreviewConditions(sourceContext, observedContext);
        Require(carried.QueryFrame!.PyreResurrectionCount == 0 && carried.Relics!
            .Single(owner => owner.DataId == RelicPyreResurrectionModel.DataId).ResourceEffects!.Single().Conditions.Single().Triggered &&
            JsonSerializer.Serialize(carried.Relics.Where(owner => owner.DataId != RelicPyreResurrectionModel.DataId), ModelJson.Options) ==
            JsonSerializer.Serialize(sourceContext.Relics!.Where(owner => owner.DataId != RelicPyreResurrectionModel.DataId), ModelJson.Options),
            "Preview carry altered the run flag or unrelated relics.");
        CombatContext absent = sourceContext.WithRelics(sourceContext.Relics!.Where(owner => owner.DataId != RelicPyreResurrectionModel.DataId).ToArray());
        Require(ReferenceEquals(absent, RelicPyreResurrectionModel.CarryPreviewConditions(absent, observedContext)), "Absent relic changed the context.");
        CombatContext consumed = sourceContext.WithQueryFrame(sourceContext.QueryFrame!.With(pyreResurrectionCount: 1));
        Require(RelicPyreResurrectionModel.CarryPreviewConditions(consumed, observedContext).QueryFrame!.PyreResurrectionCount == 1,
            "Preview carry reset the consumed run flag.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Native successor differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated its parent.");
        if (replay.HasValue)
        {
            Native(replay.Value);
            var repeated = replay.Value.GetProperty("RollingDecisions").EnumerateArray().ToArray();
            Require(repeated.Length == rows.Length, "Independent replay changed input count.");
            for (int i = 0; i < rows.Length; i++)
            {
                Require(rows[i].GetProperty("Action").ContentEquals(repeated[i].GetProperty("Action")) &&
                    rows[i].GetProperty("IsEndTurn").GetBoolean() == repeated[i].GetProperty("IsEndTurn").GetBoolean(),
                    "Independent replay changed input " + i);
                foreach (string field in new[] { "Root", "Actual" })
                    Require(NativeBranchObservationChecks.Key(rows[i].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                        NativeBranchObservationChecks.Key(repeated[i].GetProperty(field).Deserialize<BattleTurnState>()!),
                        "Independent replay changed " + field + " at " + i);
            }
        }
        Console.WriteLine("BONE-DOG-PREVIEW-LEAK PASS: native end-turn full state/RNG, preview condition without consuming run flag, four immutable branches; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
