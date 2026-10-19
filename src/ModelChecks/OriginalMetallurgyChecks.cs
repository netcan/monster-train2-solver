using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalMetallurgyChecks
{
    private const string CardId = "bdfec381-a336-4b7d-b08a-c10c7ce231c0";
    private const string OneCostId = "fb0683b1-20ac-4dd2-b580-14b502e182f4";
    private const string TwoCostId = "25f84315-da10-4915-aa5d-e3f1a38124b4";

    internal static int[] CardIds(FixtureValue root)
    {
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        BattleTurnState before = actions[1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        CombatContext context = before.Spawn.Train.Context!;
        return new[]
        {
            actions[0].GetProperty("Action").GetProperty("CardInstanceId").GetInt32(),
            actions[1].GetProperty("Action").GetProperty("CardInstanceId").GetInt32(),
            context.Cards.Hand.Single(card => card.DataId == OneCostId).InstanceId,
            context.Cards.Hand.Single(card => card.DataId == TwoCostId).InstanceId
        };
    }

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
        {
            var summon = new PlayCardAction(ids[0], 0, playerPosition: 0);
            if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
        }
        if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0 && context.Cards.Hand.Any(card => card.InstanceId == ids[1]))
        {
            var cast = new PlayCardAction(ids[1], 0,
                recursionSelections: [new CardRecursionSelection(1, [ids[3]])]);
            return BattleActionModel.PlayCard(source, cast).Supported ? cast : null;
        }
        return null;
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 103 && root.GetProperty("ModifierScenario").GetString() == "original-metallurgy" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Missing completed original Metallurgy witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        int[] ids = CardIds(root);
        Require(actions.Length == 2 && ids.Length == 4, "Expected one Train Steward deployment and one original Metallurgy play.");
        BattleTurnState before = actions[1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = actions[1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = actions[1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext input = before.Spawn.Train.Context!;
        CombatContext output = actual.Spawn.Train.Context!;
        CombatUnit hostBefore = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]);
        CombatUnit hostAfter = actual.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == hostBefore.Id);
        CardToken source = input.Cards.Hand.Single(card => card.InstanceId == action.CardInstanceId);
        CardToken kept = input.Cards.Hand.Single(card => card.InstanceId == ids[2]);
        CardToken consumed = input.Cards.Hand.Single(card => card.InstanceId == ids[3]);
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == CardId);
        CardActionEffect armor = rule.Effects[0];
        CardActionEffect chooseDiscard = rule.Effects[1];
        int cost = input.FindCard(consumed.InstanceId)!.RawCostOverride ?? input.FindCard(consumed.InstanceId)!.MaskDescriptor!.BaseCost;

        Require(source.DataId == CardId && action.CardInstanceId == ids[1] && action.RoomIndex == 0 &&
            action.RecursionSelections.Count == 1 && action.RecursionSelections[0].EffectIndex == 1 &&
            action.RecursionSelections[0].CardInstanceIds.SequenceEqual(new[] { ids[3] }) &&
            kept.DataId == OneCostId && consumed.DataId == TwoCostId && cost == 2 &&
            rule.AssetKey == "Metallurgy" && rule.CardType == "Spell" && rule.Cost == 1 && rule.ExternalInteractions.Count == 0 &&
            rule.Effects.Count == 2 && armor.Type == "AddStatus" && armor.Target == "Room" && armor.AllowPlayer && !armor.AllowEnemy &&
            armor.Statuses.Single() is { Id: "armor", Stacks: 4 } &&
            chooseDiscard.Type == "ChooseDiscard" && chooseDiscard.Target == "Hand" && chooseDiscard.Value == 3 &&
            chooseDiscard.ChooseDiscard?.Condition == 5 && chooseDiscard.ChooseDiscard.GainEmberForCost &&
            !chooseDiscard.ChooseDiscard.HasReplacementPool && hostBefore.Status("armor") == null && hostAfter.Status("armor")?.Stacks == 4 &&
            input.QueryFrame?.ForgePoints == 0 && output.QueryFrame?.ForgePoints == 2 &&
            output.QueryFrame?.Energy == input.QueryFrame?.Energy - 1 + cost &&
            output.Cards.Hand.Any(card => card.InstanceId == ids[2]) &&
            output.OtherPiles?.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[3]) == true &&
            output.Cards.Discard.Any(card => card.InstanceId == ids[1]),
            "Original Metallurgy identity, armor, keep-one selection, ConsumeAndForge payout or card routing differs.");

        BattleActionResult replay = BattleActionModel.PlayCard(before, action);
        Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(actual),
            "The recorded Metallurgy choice does not replay to native state: " + replay.Reason);
        int[] eligible = CardChooseDiscardModel.EligibleCards(input, chooseDiscard, action.CardInstanceId, out string? error).ToArray();
        SupportedActionCandidate[] choices = BattleActionModel.EnumerateActions(before).SupportedCandidates
            .Where(candidate => candidate.Action.CardInstanceId == action.CardInstanceId &&
                candidate.Action.RoomIndex == action.RoomIndex && candidate.Action.RecursionSelections.Count == 1)
            .ToArray();
        Require(error == null && eligible.Length == 2 && eligible.Contains(ids[2]) && eligible.Contains(ids[3]) &&
            choices.Select(candidate => candidate.Action.RecursionSelections[0].CardInstanceIds.Single()).SequenceEqual(eligible),
            "Metallurgy search did not enumerate both legal consume-card choices in hand order.");
        foreach (SupportedActionCandidate choice in choices)
        {
            int consumedId = choice.Action.RecursionSelections[0].CardInstanceIds.Single();
            int retainedId = consumedId == ids[2] ? ids[3] : ids[2];
            CombatContext branch = choice.Result.State!.Spawn.Train.Context!;
            int branchCost = input.FindCard(consumedId)!.RawCostOverride ?? input.FindCard(consumedId)!.MaskDescriptor!.BaseCost;
            Require(branch.QueryFrame?.ForgePoints == branchCost && branch.Cards.Hand.Any(card => card.InstanceId == retainedId) &&
                branch.OtherPiles?.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == consumedId) == true,
                "A Metallurgy search branch consumed the wrong card or awarded the wrong Forge value.");
        }

        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-METALLURGY PASS: original armor 4 plus native ConsumeAndForge target selection, Ember/Forge payout and both search branches.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new System.InvalidOperationException(message); }
}
