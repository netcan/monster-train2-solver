using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class StartingDualityChecks
{
    internal static void Native(FixtureValue root)
    {
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        Require(root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Native battle did not settle.");
        int births = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
            if (!card.Permanent.Upgrades.Any(upgrade => upgrade.Statuses.Any(status => status.Id == "duality")) ||
                !card.Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == "SoldierOfFortune")) continue;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatUnit unit = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == card.InstanceId);
            Require(unit.Status("duality")?.Stacks == 1 && unit.Status("armor")?.Stacks == 30 && unit.Status("avarice")?.Stacks == 30,
                "Native Duality birth must apply the temporary fifteen-stack armor/avarice at thirty.");
            BattleActionResult predicted = BattleActionModel.PlayCard(before, action);
            Require(predicted.Supported && predicted.Outcome == row.GetProperty("ActualOutcome").Deserialize<RoomOutcome>() &&
                BattleTurnChecks.Comparable(predicted.State!) == BattleTurnChecks.Comparable(actual),
                "Starting Duality birth differs from the complete native action/context/RNG.");
            births++;
        }
        Require(births > 0, "Missing native starting-Duality interaction witness.");
        Console.WriteLine($"STARTING-DUALITY PASS: {births} natural upgraded births, permanent Duality before temporary armor/avarice and complete native action/context/RNG.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
