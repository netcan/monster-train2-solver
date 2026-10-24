using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HeavenfallFocusChecks
{
    internal static void Prefix(FixtureValue root)
    {
        Require(root.GetProperty("NativeWon").ValueKind == FixtureKind.Null && root.GetProperty("Mismatches").GetInt32() == 1 &&
            new[] { "CaptureFailures", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0),
            "This retained prefix must preserve the original failed-run counters; it is not a completed battle acceptance.");
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length == 19 && root.GetProperty("Turns").GetArrayLength() == 2, "Missing original nineteen-action native prefix.");
        BattleTurnState witness = actions[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        PlayCardAction play = actions[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardInstanceState card = witness.Spawn.Train.Context!.FindCard(play.CardInstanceId)!;
        MoonCovenantCompositionChecks.Startup(root, witness.Spawn.Train.Context!, 3);
        Require(card.DataId == "8757357b-c36f-476d-88de-b9a481d89fcb" && play.RoomIndex == 2 && witness.MoonPhase == "Full" &&
            card.Permanent.Upgrades.Where(upgrade => upgrade.Lifecycle?.Triggers.Count > 0).Select(upgrade => upgrade.AssetKey)
                .SequenceEqual(new[] { "HeavenfallAll_Upgrade", "HeavenfallRoom_Upgrade" }) &&
            witness.Spawn.Train.Context.QueryFrame!.SelectedRoomIndex == 2 &&
            witness.Spawn.Train.Context.RoomCapacities!.Single(room => room.RoomIndex == 2).Attachments!.Single().MoonPhase!.AdditionalMask == 1 &&
            witness.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Any(unit => unit.Status("armor")?.Stacks == 25),
            "Witness must retain original Tower-then-Room triggers, selected Eclipse floor and remote armor notification.");
        foreach (FixtureValue action in actions)
        {
            BattleTurnState before = action.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = action.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction input = action.GetProperty("Action").Deserialize<PlayCardAction>()!;
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                BattleActionResult result = BattleActionModel.PlayCard(before, input);
                Require(result.Supported && result.Outcome == RoomOutcome.Exchanged, result.Reason ?? "Native prefix action failed.");
                string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(result.State!), BattleTurnChecks.Comparable(actual));
                Require(difference == null, "Native action/complete RNG differs at " + action.GetProperty("Index").GetInt32() + ": " + difference);
            }
            Replay();
            if (action.GetProperty("Index").GetInt32() == 18) Parallel.For(0, 16, _ => Replay());
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Prefix branch mutated its parent.");
        }
        BattleTurnState after = actions[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(after.SelectedRoom == 1 && after.Spawn.Train.Context!.FindCard(play.CardInstanceId)!.PlayedRoomUnitIds!
            .SequenceEqual(witness.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Select(unit => unit.Id).OrderBy(id => id)) &&
            after.Spawn.Train.Context.CardInstances!.Where(instance => instance.DataId == "87e45427-921e-45d3-b4c4-9c156a83cedf")
                .All(instance => instance.Permanent.Upgrades.Single().AssetKey == "GravityAscend_Upgrade"),
            "Remote focus must refresh both Gravity cards and recache the final trigger's actual room occupants.");
        Console.WriteLine("HEAVENFALL-FOCUS-PREFIX PASS actions=19 completeStateAndRng=true immutableWitnessBranches=16 sourceBattleAccepted=false");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
