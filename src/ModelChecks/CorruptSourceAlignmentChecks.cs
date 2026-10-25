using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CorruptSourceAlignmentChecks
{
    private const string Reason = "Positive Corrupt Restricted requires captured base source parameters.";
    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var card = before.Spawn.Train.Context!.FindCard(16)!;
        var mask = card.MaskDescriptor!;
        Require(rows.Length == 20 && before.Spawn.Turn == 3 &&
            card.DataId == "096def92-500b-4b1f-89f7-43e96aa1e54a" &&
            mask.BaseTraits.Count == 2 && mask.SourceTraits!.Count == 1 &&
            mask.BaseTraits.Any(trait => trait.RuntimeType == "CardTraitRetain") &&
            mask.SourceTraits[0].Type == CardCorruptRestrictedModel.Trait && mask.SourceTraits[0].Int == 1 &&
            card.Permanent.Upgrades.Any(upgrade => upgrade.AssetKey == "TraitRetain") &&
            before.Spawn.Train.Context.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 3 &&
            after.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 2 &&
            before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == 12).Health == 120 &&
            after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == 12) is { Health: 135, MaxHealth: 135 },
            "Missing the native positive Corrupt Restricted source with an installed Holdover trait.");
        Console.WriteLine("CORRUPT-SOURCE-ALIGNMENT PASS:20 native inputs/full successors/RNG; unrelated Holdover base-state entry preserves authored requirement1, corruption3->2 and health120->135; immutable parents.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        NativeBranchObservationChecks.Pair(source, replay);
        Console.WriteLine("CORRUPT-SOURCE-ALIGNMENT-PAIR PASS:original rejection and independent repaired forecast preserved.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
