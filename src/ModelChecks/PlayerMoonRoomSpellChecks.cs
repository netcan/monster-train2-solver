using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PlayerMoonRoomSpellChecks
{
    internal static void TurnBoundary(FixtureValue root)
    {
        Require(root.GetProperty("Boundary").GetString() == "PlayerPublishedEndTurnAfterDrawAndTriggerQueue" &&
            root.GetProperty("SourceRunCompleted").GetBoolean() == false, "Missing retained failed-run boundary provenance.");
        BattleTurnState before = root.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = root.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 0 && actual.Spawn.Turn == 1 && before.Spawn.Train.Context!.QueryFrame!.SelectedRoomIndex == 2 &&
            actual.Spawn.Train.Context!.QueryFrame!.SelectedRoomIndex == 0 && before.MoonPhase == "New" && actual.MoonPhase == "Full",
            "Missing original deployment, phase advance and native enemy focus controls.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        void Replay()
        {
            BattleTurnResult next = BattleTurnModel.EndTurn(before);
            Require(next.Supported && next.Outcome == RoomOutcome.Exchanged, next.UnsupportedReason ?? "Player turn boundary failed.");
            string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(next.State!), BattleTurnChecks.Comparable(actual));
            Require(difference == null, "Complete player turn successor/RNG differs: " + difference);
        }
        Replay(); Parallel.For(0, 16, _ => Replay());
        Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Player turn boundary mutated its parent.");
        CombatContext handBefore = root.GetProperty("HandBefore").Deserialize<CombatContext>()!;
        CombatContext handAfter = root.GetProperty("HandAfter").Deserialize<CombatContext>()!;
        CombatContext refreshed = CardMoonUpgradeModel.RefreshHand(handBefore, out string? error);
        Require(error == null && ModelJson.Difference(JsonSerializer.Serialize(refreshed, ModelJson.Options),
            JsonSerializer.Serialize(handAfter, ModelJson.Options)) == null, "Native queued post-draw hand callback differs: " + error);
        CardInstanceState[] gravity = handAfter.CardInstances!.Where(card => card.DataId == "87e45427-921e-45d3-b4c4-9c156a83cedf" &&
            handAfter.Cards.Hand.Any(token => token.InstanceId == card.InstanceId)).ToArray();
        Require(gravity.Length > 0 && gravity.All(card => card.Permanent.Upgrades.Any(upgrade => upgrade.AssetKey == "GravityAscend_Upgrade") &&
            card.Permanent.Upgrades.All(upgrade => upgrade.AssetKey != "GravityDescend_Upgrade")), "Retained Gravity did not restore unmasked Full.");
        Console.WriteLine("PLAYER-MOON-ROOM-TURN PASS completeEndTurnAndRng=true retainedGravity=newToFull nativeQueuedHandContext=true immutableBranches=16 sourceBattleAccepted=false");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
