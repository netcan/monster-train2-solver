using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicAttachmentReplacementChecks
{
    private const string Reason = "Incomplete original start-of-combat room attachment state.";
    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var first = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 2);
        var second = after.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 2);
        var card = before.Spawn.Train.Context.FindCard(9)!;
        var action = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(rows.Length == 20 && before.Spawn.Turn == 3 && action.CardInstanceId == 9 && action.RoomIndex == 2 &&
            first.AttachmentsAddedFromCards == 0 && second.AttachmentsAddedFromCards == 1 &&
            first.Attachments!.Count == 1 && second.Attachments!.Count == 1 &&
            first.Attachments[0].DataId == "f93ac9f7-45d2-47a4-a159-a1b326bd047f" &&
            card.DataId == "6bc6afc8-a113-4f03-b090-d4436f41d822" && second.Attachments[0].DataId == card.DataId &&
            second.Attachments[0].AssetKey == "FightClub" &&
            before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2).Units.Single(unit => unit.Id == 5).Attack == 41 &&
            after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2).Units.Single(unit => unit.Id == 5).Attack == 41,
            "Missing the native card replacing the original relic attachment and its creation ordinal.");
        Console.WriteLine("RELIC-ATTACHMENT-REPLACEMENT PASS:20 native inputs/full successors/RNG; card-owned FightClub replaces relic slot with creation ordinal1 and preserves 41 attack; immutable parents.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        NativeBranchObservationChecks.Pair(source, replay);
        Console.WriteLine("RELIC-ATTACHMENT-REPLACEMENT-PAIR PASS:original rejection and independent repaired forecast preserved.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
