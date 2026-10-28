using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UntouchableTroopAuraChecks
{
    internal static void Run(FixtureValue fixture)
    {
        FixtureValue[] actions = fixture.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length == 48, "Expected the observed original action prefix.");
        FixtureValue observed = actions[47];
        BattleTurnState before = observed.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = observed.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = observed.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit oldTarget = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId);
        CombatUnit newTarget = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId);
        CardToken card = before.Spawn.Train.Context!.Cards.Hand.Single(item => item.InstanceId == action.CardInstanceId);
        Require(before.PlayRules!.Cards.Single(item => item.DataId == card.DataId).AssetKey == "StasisCrystal" &&
            oldTarget.AssetKey == "MonsterFunguy" && oldTarget.Status("horde")?.Stacks == 4 &&
            oldTarget.Health == 19 && oldTarget.MaxHealth == 28 && newTarget.Health == 16 && newTarget.MaxHealth == 16 &&
            newTarget.Status("untouchable") != null, "Native troop-aura counterexample changed.");
        string original = JsonSerializer.Serialize(before);
        BattleActionResult rejected = BattleActionModel.PlayCard(before, action);
        Require(!rejected.Supported && rejected.Reason == "Untouchable troop-aura target removal requires native calibration." &&
            JsonSerializer.Serialize(before) == original, "Uncalibrated targetability change was not rejected without mutation.");
        FixtureValue previous = actions[46];
        BattleTurnState priorRoot = previous.GetProperty("Before").Deserialize<BattleTurnState>()!;
        PlayCardAction priorAction = previous.GetProperty("Action").Deserialize<PlayCardAction>()!;
        BattleActionResult prior = BattleActionModel.PlayCard(priorRoot, priorAction);
        using var keys = new BattleStateKeys();
        Require(prior.Supported && keys.Key(prior.State!) == keys.Key(previous.GetProperty("Actual").Deserialize<BattleTurnState>()!),
            "The preceding independently observed action was incorrectly blocked or changed.");
        Console.WriteLine("UNTOUCHABLE-TROOP-AURA PASS: native counterexample rejected; prior action and immutable root retained.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
