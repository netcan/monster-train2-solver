using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalEmbeddedRoomChecks
{
    internal static void Native(FixtureValue root)
    {
        FixtureValue owner = root.GetProperty("Definition");
        Require(Text(root, "Boundary") == "OriginalEmbeddedRoom" && Text(root, "GameVersion") == Game231FixtureCatalog.Version &&
            Text(root, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(owner, "Id") == RelicModel.AlwaysEmbeddedRoomsOwnerId &&
            Text(owner, "AssetKey") == RelicModel.AlwaysEmbeddedRoomsOwnerKey &&
            Text(owner, "Effect") == RelicModel.AlwaysEmbeddedRoomsEffect &&
            !owner.GetProperty("DisallowInDeploymentPhase").GetBoolean(), "Expected the pinned authored embedded-room mutator.");

        var before = root.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var action = root.GetProperty("Action").Deserialize<PlayCardAction>()!;
        BattleActionResult played = BattleActionModel.PlayCard(before, action);
        Require(played.Supported, played.Reason ?? "The first original attachment must be legal.");
        BattleTurnState after = RecordedBattleChecks.RecordedBoundary(played.State!, root.GetProperty("After"),
            terminal: false, "original embedded room placement", trackPreviewPosition: true);
        Require(before.Energy - after.Energy == Number(root, "PaidCost"), "Original attachment Ember must match.");

        int replacement = Number(root, "ReplacementCardId");
        var floors = root.GetProperty("Installed").EnumerateArray().ToArray();
        var restrictions = root.GetProperty("Permissions").EnumerateArray().ToDictionary(row => Number(row, "Floor"));
        Require(floors.Length == 3 && restrictions.Count == floors.Length, "Expected three native attachment floors.");
        foreach (FixtureValue row in floors)
        {
            int floor = Number(row, "Floor");
            RoomCapacityState initial = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == floor);
            RoomCapacityState final = after.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == floor);
            Require(!final.IsPyre && final.PlayerMaximum == initial.PlayerMaximum + 4 &&
                final.PlayerMaximum == Number(row, "Capacity") && final.AttachmentsAddedFromCards == 1 && Number(row, "Count") == 1 &&
                row.GetProperty("Types").EnumerateArray().Single().GetString() == "Primary_Embedded" &&
                final.Attachments?.Single().AttachmentType == 1, $"Floor {floor} must preserve the first room as embedded.");
            var rejected = BattleActionModel.PlayCard(after, new PlayCardAction(replacement, floor));
            Require(!restrictions[floor].GetProperty("Allowed").GetBoolean() && rejected.Rejection == ActionRejection.Illegal,
                $"A second original room attachment must be illegal on floor {floor}.");
        }
        Console.WriteLine("NATIVE-EMBEDDED-ROOM PASS: exact paid successor, three embedded rooms and three replacement gates.");
    }

    private static int Number(FixtureValue row, string name) => row.GetProperty(name).GetInt32();
    private static string? Text(FixtureValue row, string name) => row.GetProperty(name).GetString();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}
