using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalTomeOfContractsChecks
{
    private const string CardId = "2a6c6ce2-f2c7-4be6-8417-8d8e72b82673";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-tome-of-contracts" &&
            root.GetProperty("OriginalTomeOfContractsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Tome of Contracts witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        FixtureValue action = actions.Single(row => Plays(row));
        BattleTurnState before = action.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = action.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == CardId);
        int cardId = action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
        int targetId = action.GetProperty("Action").GetProperty("TargetUnitId").GetInt32();
        CombatUnit targetBefore = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == targetId);
        CombatUnit targetAfter = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == targetId);
        FixtureValue effect = root.GetProperty("AbilityCooldownEffects").EnumerateArray().Single(row =>
            row.GetProperty("SourceCardId").GetInt32() == cardId &&
            row.GetProperty("Effect").GetProperty("Type").GetString() == "AdjustAbilityCooldown");
        Require(actions.Count(row => Plays(row)) == 1 && rule.AssetKey == "TomeOfContracts" && rule.Cost == 1 &&
            rule.Effects.Count == 1 && rule.Effects[0].Type == "AdjustAbilityCooldown" && rule.Effects[0].Value == -1 &&
            rule.Effects[0].CooldownParameter == false && rule.Effects[0].Target == "DropTargetCharacter" &&
            rule.ExternalInteractions.Count == 0 && before.Energy - after.Energy == 1 &&
            targetBefore.Ability is { HasAbility: true, Cooldown: 3 } && targetAfter.Ability is { HasAbility: true, Cooldown: 2 } &&
            effect.GetProperty("CardPlaying").GetBoolean() && effect.GetProperty("Targets").Deserialize<int[]>()!.SequenceEqual(new[] { targetId }) &&
            after.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == cardId),
            "Original Tome of Contracts must pay one Ember, reduce the selected friendly ability cooldown from three to two, and Exhaust.");
        AbilityCooldownChecks.Native(root);
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-TOME-OF-CONTRACTS PASS: unchanged 1-cost original reduces a selected friendly ability cooldown from three to two and Exhausts.");
    }

    private static bool Plays(FixtureValue action)
    {
        int instanceId = action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
        return action.GetProperty("Before").GetProperty("Spawn").GetProperty("Train").GetProperty("Context")
            .GetProperty("CardRegistry").EnumerateArray().Any(card =>
                card.GetProperty("InstanceId").GetInt32() == instanceId && card.GetProperty("DataId").GetString() == CardId);
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
