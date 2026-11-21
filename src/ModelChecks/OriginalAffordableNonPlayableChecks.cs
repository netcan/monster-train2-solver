using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalAffordableNonPlayableChecks
{
    internal static void Native(FixtureValue root)
    {
        const string dataId = "0127e3c2-b60d-4ac3-acf7-6395d44eb5ef";
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("Boundary").GetString() == "NativeOriginalAffordableNonPlayable" &&
            root.GetProperty("GameVersion").GetString() == "2.3.1" && root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("DataId").GetString() == dataId && root.GetProperty("OriginalCost").GetInt32() == 1 && root.GetProperty("CostType").GetString() == "NonPlayable",
            "Missing original 2.3.1 Shattering Darkshard evidence.");
        int cardId = root.GetProperty("CardId").GetInt32();
        FixtureValue unavailable = root.GetProperty("Unavailable");
        FixtureValue affordable = root.GetProperty("Affordable");
        BattleTurnState low = unavailable.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState lowActual = unavailable.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        BattleTurnState before = affordable.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = affordable.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = affordable.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == dataId);
        Require(low.Spawn.Turn == 0 && before.Spawn.Turn == 0 && before.ForgePoints == 0 &&
            rule.AssetKey == "ShatteringDarkshard" && rule.CostType == "NonPlayable" && rule.Cost == 1 &&
            action.CardInstanceId == cardId && before.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == cardId && card.DataId == dataId),
            "Missing unchanged original direct summon input.");
        BattleActionResult denied = BattleActionModel.PlayCard(low, action);
        Require(low.Energy == 0 && !unavailable.GetProperty("CanPlay").GetBoolean() &&
            unavailable.GetProperty("SelectionError").GetString() == "InsufficientEnergy" && denied.Rejection == ActionRejection.Illegal &&
            denied.Reason == "Insufficient energy." && !BattleActionModel.EnumerateActions(low, cardId).HasUnsupportedAttempts &&
            BattleTurnChecks.Comparable(low) == BattleTurnChecks.Comparable(lowActual), "Zero Ember must refuse the original paid play without changing state.");
        CombatUnit unit = actual.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex).Units.Single(actor => actor.SpawnerCardId == cardId);
        Require(before.Energy == 1 && actual.Energy == 0 && affordable.GetProperty("Accepted").GetBoolean() &&
            affordable.GetProperty("SelectionError").GetString() == "None" && unit.AssetKey == "MonsterShatteringDarkshard" &&
            unit.Health == 10 && unit.MaxHealth == 10 && unit.Attack == 0 && unit.Ability?.DataId == "0a24d29d-d009-43c0-8d98-2eb6ee90fe28" &&
            actual.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Any(card => card.InstanceId == cardId),
            "Paid original play must spend one Ember, move the original to Standby and summon its exact ability-bearing unit.");
        string parent = BattleTurnChecks.Comparable(before);
        BattleActionResult paid = BattleActionModel.PlayCard(before, action);
        Require(paid.Supported && paid.Outcome == RoomOutcome.Exchanged, "Original affordable play remains unsupported: " + paid.Reason);
        string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(paid.State!), BattleTurnChecks.Comparable(actual));
        Require(difference == null && BattleTurnChecks.Comparable(before) == parent &&
            BattleActionModel.EnumerateSupportedPlays(before, cardId).Any(play => play.RoomIndex == action.RoomIndex && play.PlayerPosition == action.PlayerPosition),
            "Native paid successor or search frontier differs: " + difference);
        Console.WriteLine("ORIGINAL-AFFORDABLE-NONPLAYABLE PASS: zero-Ember refusal and one-Ember paid original summon, standby, ability initialization and full successor equality.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidDataException(message); }
}
