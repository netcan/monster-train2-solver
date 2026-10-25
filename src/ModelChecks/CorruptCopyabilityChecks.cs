using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CorruptCopyabilityChecks
{
    private const string Reason = "Copyability changes from card trait upgrades are not modeled.";
    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var source = before.Spawn.Train.Context!.FindCard(15)!;
        var copy = after.Spawn.Train.Context!.FindCard(27)!;
        Require(rows.Length == 5 && before.Spawn.Turn == 1 && after.Spawn.Turn == 1 &&
            !last.GetProperty("IsEndTurn").GetBoolean() && source.CanBeCopied == true && copy.CanBeCopied == true &&
            source.DataId == "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b" && copy.DataId == source.DataId &&
            source.Permanent.Upgrades.Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait =>
                trait.RuntimeType == "CardTraitCorruptState" && trait.DeclaredName == trait.RuntimeType) == true) &&
            before.Spawn.Train.Context.NextCardId == 27 && after.Spawn.Train.Context.NextCardId == 28 &&
            after.Spawn.Train.Context.Cards.Hand.Select(card => card.InstanceId).SequenceEqual(new[] {27, 22}),
            "Missing the original upgraded Corrupt source and generated hand copy.");
        Console.WriteLine("CORRUPT-COPYABILITY PASS:5 native inputs/full successors/RNG; upgraded Corrupt trait retains native copyability and generated-card identity; immutable parents.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        NativeBranchObservationChecks.Pair(source, replay);
        Console.WriteLine("CORRUPT-COPYABILITY-PAIR PASS:original rejection and independently repaired forecast preserved.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
