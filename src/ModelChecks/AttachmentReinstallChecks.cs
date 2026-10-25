using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AttachmentReinstallChecks
{
    private const string Reason = "Replacing an identical room attachment must remove and reinstall its character upgrades.";
    private const string Upgrade = "f93ac9f7-45d2-47a4-a159-a1b326bd047f";
    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, Historical);
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var oldRoom = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 2);
        var newRoom = actual.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 2);
        var oldHero = Hero(before); var newHero = Hero(actual);
        Require(rows.Length == 37 && before.Spawn.Turn == 8 &&
            oldRoom.Attachments!.Single().DataId == newRoom.Attachments!.Single().DataId &&
            newRoom.AttachmentsAddedFromCards == oldRoom.AttachmentsAddedFromCards + 1 &&
            oldHero.Modifiers!.Upgrades!.Count(upgrade => upgrade.DataId == Upgrade) == 1 &&
            newHero.Modifiers!.Upgrades!.Select(upgrade => upgrade.DataId).SequenceEqual(
                oldHero.Modifiers.Upgrades.Where(upgrade => upgrade.DataId != Upgrade).Select(upgrade => upgrade.DataId).Append(Upgrade)) &&
            newHero.Health == oldHero.Health && newHero.Attack == oldHero.Attack,
            "Identical attachment replacement must append the fresh upgrade without duplicating its stats.");
        Console.WriteLine("ATTACHMENT-REINSTALL PASS:37 native inputs/full successors/RNG; identical payload gets fresh upgrade order and unchanged stats; immutable parents.");
    }
    private static CombatUnit Hero(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 2);
    private static void Historical(BattleTurnState predicted, BattleTurnState actual)
    {
        var oldOrder = Hero(predicted).Modifiers!.Upgrades!.Select(upgrade => upgrade.DataId).ToArray();
        var newOrder = Hero(actual).Modifiers!.Upgrades!.Select(upgrade => upgrade.DataId).ToArray();
        Require(oldOrder[^2] == Upgrade && newOrder[^1] == Upgrade &&
            oldOrder.Where(id => id != Upgrade).SequenceEqual(newOrder.Where(id => id != Upgrade)),
            "Wrong original attachment upgrade-order mismatch.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        NativeBranchObservationChecks.Pair(source, replay, originalPredictionPresent: true);
        Console.WriteLine("ATTACHMENT-REINSTALL-PAIR PASS:original upgrade-order mismatch and independent repaired forecast preserved.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
