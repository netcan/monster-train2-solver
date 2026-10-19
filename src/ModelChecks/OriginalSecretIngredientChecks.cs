using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalSecretIngredientChecks
{
    private const string CardDataId = "f4cf7469-4d84-40e9-94f6-6b743973f2ad";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-secret-ingredient-mix" &&
            root.GetProperty("OriginalSecretIngredientPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Missing completed original Secret Ingredient battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        FixtureValue[] plays = actions.Where(entry =>
        {
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            return before.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == CardDataId;
        }).ToArray();
        Require(plays.Length >= 1, "Expected an original Secret Ingredient play.");

        FixtureValue entry = plays[0];
        BattleTurnState stateBefore = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState stateAfter = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction played = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext beforeContext = stateBefore.Spawn.Train.Context!;
        CombatContext afterContext = stateAfter.Spawn.Train.Context!;
        CardInstanceState card = beforeContext.FindCard(played.CardInstanceId)!;
        CardPlayRule rule = stateBefore.PlayRules!.Cards.Single(candidate => candidate.DataId == CardDataId);
        CardActionEffect mix = rule.Effects.Single();
        ElixirCatalog catalog = stateBefore.PlayRules.Elixirs!;
        ElixirMixPlan plan = ElixirMixModel.Plan(beforeContext, mix.Value);
        string? mixedCardId = afterContext.MixState?.LastCardDataId;
        string? mixedUpgradeId = afterContext.MixState?.LastUpgradeDataId;
        var selectedRecipe = catalog.Recipes.SingleOrDefault(recipe => recipe.Creation.DataId == mixedCardId);

        Require(card.MaskDescriptor!.SourceTraits is { Count: 0 } && rule.AssetKey == "SecretIngredient" &&
            rule.CardType == "Spell" && rule.Cost == 1 && rule.CostType == "Default" && rule.Effects.Count == 1 &&
            mix.Type == "Mix" && mix.Value == 2 && plan.Supported && plan.Amounts.Count > 0 &&
            played.CardInstanceId == card.InstanceId && played.MixSelections.Count == plan.Amounts.Count &&
            played.MixSelections.Select(selection => selection.EffectIndex).All(index => index == 0) &&
            played.MixSelections.Select(selection => selection.MixNumber).SequenceEqual(Enumerable.Range(0, plan.Amounts.Count)) &&
            played.MixSelections.All(selection => selection.ChoiceIndex is >= 0 and < 2) &&
            stateBefore.Energy - stateAfter.Energy == rule.Cost && selectedRecipe != null && mixedUpgradeId != null &&
            selectedRecipe.Upgrades.Any(upgrade => upgrade.DataId == mixedUpgradeId) &&
            afterContext.Cards.Hand.Count(token => token.DataId == mixedCardId) == 1,
            "Secret Ingredient's original identity, one-effect Mix rules, selected catalog elixir/upgrade, or energy payment changed.");

        BattleActionResult replay = BattleActionModel.PlayCard(stateBefore, played);
        Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(stateAfter),
            "Original Secret Ingredient does not replay to native state: " + replay.Reason);
        PlayCardAction[] candidates = BattleActionModel.EnumerateActions(stateBefore).SupportedCandidates
            .Where(candidate => candidate.Action.CardInstanceId == played.CardInstanceId &&
                candidate.Action.RoomIndex == played.RoomIndex).Select(candidate => candidate.Action).ToArray();
        bool SameSelections(PlayCardAction candidate) => candidate.MixSelections.Count == played.MixSelections.Count &&
            candidate.MixSelections.Zip(played.MixSelections).All(pair => pair.First.EffectIndex == pair.Second.EffectIndex &&
                pair.First.MixNumber == pair.Second.MixNumber && pair.First.ChoiceIndex == pair.Second.ChoiceIndex);
        Require(candidates.Any(SameSelections) &&
            candidates.SelectMany(candidate => candidate.MixSelections).Where(selection => selection.MixNumber == 0)
                .Select(selection => selection.ChoiceIndex).Distinct().Order().SequenceEqual(new[] { 0, 1 }) &&
            mixedCardId != null && mixedUpgradeId != null,
            "Action enumeration omitted the native Secret Ingredient draft or its Mix state.");

        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-SECRET-INGREDIENT PASS: replayed the unchanged original Mix spell and enumerated both first-draft choices.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
