using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalWaxenSpikeChecks
{
    private const string CardId = "8d291f9a-30a1-4a6d-8d9e-6dc2df4432d1";
    private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";

    internal static int[] CardIds(FixtureValue root) => root.GetProperty("Actions").EnumerateArray()
        .Select(action => action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).ToArray();

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        for (int index = 0; index < ids.Length; index++)
        {
            if (context.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") != 0 ||
                !context.Cards.Hand.Any(card => card.InstanceId == ids[index])) continue;
            var action = new PlayCardAction(ids[index], index == 1 ? 1 : 0, playerPosition: index < 2 ? 0 : -1);
            if (BattleActionModel.PlayCard(source, action).Supported) return action;
        }
        return null;
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 103 && root.GetProperty("ModifierScenario").GetString() == "original-waxen-spike" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Missing completed original Waxen Spike witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        int[] ids = CardIds(root);
        Require(actions.Length == 3 && ids.Length == 3, "Expected target/control Stewards and one original Waxen Spike play.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(Before(0).Spawn.Train.Context!.FindCard(ids[0])!.DataId == StewardId &&
            Before(1).Spawn.Train.Context!.FindCard(ids[1])!.DataId == StewardId &&
            Before(2).Spawn.Train.Context!.FindCard(ids[2])!.DataId == CardId,
            "The controlled action sequence did not deploy both Stewards before Waxen Spike.");

        BattleTurnState before = Before(2), after = After(2);
        CombatUnit targetBefore = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]);
        CombatUnit targetAfter = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == targetBefore.Id);
        CombatUnit controlBefore = before.Spawn.Train.Rooms[1].Units.Single(unit => unit.SpawnerCardId == ids[1]);
        CombatUnit controlAfter = after.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == controlBefore.Id);
        CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[2])!;
        CardSourceTrait[] traits = card.MaskDescriptor!.SourceTraits!.ToArray();
        CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == CardId);
        PlayCardAction action = actions[2].GetProperty("Action").Deserialize<PlayCardAction>()!;
        int paid = before.Energy;
        int lastPlayedCost = after.Spawn.Train.Context!.FindCard(ids[2])!.LastPlayedCost;
        Require(card.DataId == CardId && traits.Length == 2 &&
            traits[0].Type == "CardTraitScalingUpgradeUnitAttack" && traits[0].Int == 4 && traits[0].Int2 == 0 && traits[0].Int3 == 0 &&
            !traits[0].Bool && traits[0].Str == "" && traits[0].Float == 1 && traits[0].Scaling &&
            traits[0].TrackedValue == "PlayedCost" && traits[0].EntryDuration == "ThisBattle" &&
            traits[1].Type == "CardTraitExhaustState" && traits[1].Int == 0 && traits[1].Int2 == 0 && traits[1].Int3 == 0 &&
            !traits[1].Bool && traits[1].Str == "" && traits[1].Float == 0 && !traits[1].Scaling &&
            traits[1].TrackedValue == "PlayedCost" && traits[1].EntryDuration == "ThisBattle" &&
            rule.AssetKey == "WaxenSpike" && rule.CardType == "Spell" &&
            rule.Cost == 0 && rule.CostType == "ConsumeRemainingEnergy" && rule.ExternalInteractions.Count == 0 &&
            rule.Effects.Select(effect => effect.Type).SequenceEqual(["UnitUpgrade", "AddStatus"]) &&
            rule.Effects[0].Target == "Room" && rule.Effects[0].Upgrade?.AssetKey == "XCostBurnoutEnhance" &&
            rule.Effects[0].Lifetime == "TemporaryUntilEndOfBattle" && rule.Effects[1].Target == "Room" &&
            rule.Effects[1].Statuses.Single() is { Id: "ephemeral", Stacks: 4 },
            "Original Waxen Spike identity, X-cost definition, scaling traits or ordered effects differ.");
        CardUpgradeModifier scaledUpgrade = targetAfter.Modifiers!.Upgrades.Single(upgrade =>
            upgrade.DataId == rule.Effects[0].Upgrade!.DataId);
        int expectedAttack = targetBefore.Attack + traits[0].Int * paid;
        Require(action.CardInstanceId == ids[2] && action.RoomIndex == 0 && paid > 0 && after.Energy == 0 && lastPlayedCost == paid &&
            targetAfter.Attack == expectedAttack && scaledUpgrade.Stats.Damage == traits[0].Int * paid &&
            targetAfter.Status("ephemeral")?.Stacks == 4 && controlAfter.Status("ephemeral") == null &&
            controlAfter.Attack == controlBefore.Attack && controlAfter.Modifiers!.Upgrades.Count == controlBefore.Modifiers!.Upgrades.Count &&
            after.Spawn.Train.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(item => item.InstanceId == ids[2]),
            "Waxen Spike did not apply 4 attack per Ember, spend all Ember, grant Ephemeral only on its room or Exhaust itself.");

        BattleActionResult replay = BattleActionModel.PlayCard(before, action);
        Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(after),
            "The recorded Waxen Spike action does not replay to the native state: " + replay.Reason);
        Require(BattleActionModel.EnumerateActions(before).SupportedCandidates.Any(candidate => candidate.Action.CardInstanceId == ids[2] &&
            candidate.Action.RoomIndex == 0), "Action search did not enumerate the native Waxen Spike play.");
        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-WAXEN-SPIKE PASS: all-Ember X payment, 4 attack per Ember, room-local Ephemeral, untouched control floor and Exhaust routing.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new System.InvalidOperationException(message); }
}
