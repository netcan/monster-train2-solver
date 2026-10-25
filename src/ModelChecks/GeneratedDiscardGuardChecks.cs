using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class GeneratedDiscardGuardChecks
{
    private const string Reason = "Generated Persistent elixir discard guards differ from native capture.";
    private static readonly string[] Guards = ["CardTraitPersistent Discard callback", "CardTraitPersistent discard routing"];

    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        var first = NativeBranchObservationChecks.Replay(source, Reason);
        var second = NativeBranchObservationChecks.Replay(replay, Reason);
        Require(first.Length == 8 && second.Length == first.Length &&
            source.GetProperty("Request").ContentEquals(replay.GetProperty("Request")), "Missing independent eight-input Mix observation.");
        for (int index = 0; index < first.Length; index++)
        {
            foreach (string field in new[] { "Before", "Actual" })
            {
                var a = first[index].GetProperty(field).Deserialize<BattleTurnState>()!;
                var b = second[index].GetProperty(field).Deserialize<BattleTurnState>()!;
                Require(NativeBranchObservationChecks.Key(a) == NativeBranchObservationChecks.Key(b) &&
                    a.ExternalInteractions.SequenceEqual(b.ExternalInteractions), "Native state/guard identity changed at " + index);
            }
            foreach (string field in new[] { "Action", "IsEndTurn", "Boundary" })
                Require(first[index].GetProperty(field).ContentEquals(second[index].GetProperty(field)), "Independent Mix input changed.");
        }
        var before = second[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = second[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var oldForecast = first[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!;
        var forecast = second[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!;
        var action = second[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(before.ExternalInteractions.Count == 0 && oldForecast.ExternalInteractions.Count == 0 &&
            actual.ExternalInteractions.SequenceEqual(Guards) && forecast.ExternalInteractions.SequenceEqual(Guards) &&
            action.MixSelections is [{ EffectIndex: 0, MixNumber: 0, ChoiceIndex: 0 }],
            "Preserve the missing historical generated-card guards and their repaired forecast.");
        Console.WriteLine("GENERATED-DISCARD-GUARD-PAIR PASS:8 native inputs/full successors/RNG; Persistent elixir guard introduction matches; historical omission and independent repair preserved.");
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
