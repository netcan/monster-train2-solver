using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class ElixirRecipe
    {
        public CardCreationRule Creation { get; }
        public CardUpgradeModifier? BaseUpgrade { get; }
        public IReadOnlyList<CardUpgradeModifier> Upgrades { get; }

        public ElixirRecipe(CardCreationRule creation, CardUpgradeModifier? baseUpgrade,
            IReadOnlyList<CardUpgradeModifier> upgrades)
        {
            Creation = creation;
            BaseUpgrade = baseUpgrade;
            Upgrades = Array.AsReadOnly(upgrades.ToArray());
        }
    }

    public sealed class ElixirCatalog
    {
        public IReadOnlyList<ElixirRecipe> Recipes { get; }

        public ElixirCatalog(IReadOnlyList<ElixirRecipe> recipes) =>
            Recipes = Array.AsReadOnly(recipes.ToArray());
    }

    public sealed class MixRuntimeState
    {
        public string? LastCardDataId { get; }
        public string? LastUpgradeDataId { get; }

        public MixRuntimeState(string? lastCardDataId, string? lastUpgradeDataId)
        {
            LastCardDataId = lastCardDataId;
            LastUpgradeDataId = lastUpgradeDataId;
        }
    }

    public sealed class ElixirDraftOption
    {
        public ElixirRecipe Recipe { get; }
        public CardUpgradeModifier? Upgrade { get; }

        public ElixirDraftOption(ElixirRecipe recipe, CardUpgradeModifier? upgrade)
        { Recipe = recipe; Upgrade = upgrade; }
    }

    public sealed class ElixirDraftOptions
    {
        public IReadOnlyList<ElixirDraftOption> Offers { get; }
        public UnityRng Rng { get; }

        public ElixirDraftOptions(IReadOnlyList<ElixirDraftOption> offers, UnityRng rng)
        { Offers = Array.AsReadOnly(offers.ToArray()); Rng = rng; }
    }

    public sealed class ElixirMixSelection
    {
        public int EffectIndex { get; }
        public int MixNumber { get; }
        public int ChoiceIndex { get; }

        public ElixirMixSelection(int effectIndex, int mixNumber, int choiceIndex)
        { EffectIndex = effectIndex; MixNumber = mixNumber; ChoiceIndex = choiceIndex; }
    }

    public sealed class ElixirMixPlan
    {
        public CombatContext? Context { get; }
        public IReadOnlyList<int> Amounts { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Context != null;
        internal ElixirMixPlan(CombatContext? context, IReadOnlyList<int> amounts, string? unsupportedReason = null)
        { Context = context; Amounts = Array.AsReadOnly(amounts.ToArray()); UnsupportedReason = unsupportedReason; }
    }

    public static class ElixirMixModel
    {
        public static ElixirMixPlan Plan(CombatContext source, int baseAmount)
        {
            if (baseAmount < 1) return new ElixirMixPlan(null, Array.Empty<int>(), "Mix requires a positive base choice count.");
            if (source.Relics?.Any(relic => relic.EffectTypes.Contains(RelicResourceModel.AdjustMixAmount)) != true)
                return new ElixirMixPlan(source, new[] { baseAmount });
            string? error = RelicModel.Validate(source);
            if (error != null) return new ElixirMixPlan(null, Array.Empty<int>(), error);
            CombatContext context = source;
            int count = 1;
            foreach (int relicIndex in Enumerable.Range(0, context.Relics!.Count))
            {
                CombatRelicState owner = context.Relics[relicIndex];
                for (int ruleIndex = 0; ruleIndex < (owner.ResourceEffects?.Count ?? 0); ruleIndex++)
                {
                    RelicResourceEffect rule = context.Relics[relicIndex].ResourceEffects![ruleIndex];
                    if (rule.Type != RelicResourceModel.AdjustMixAmount) continue;
                    RelicConditionEvaluation evaluation = RelicConditionModel.Evaluate(context, rule.Conditions);
                    if (!evaluation.Supported) return new ElixirMixPlan(null, Array.Empty<int>(), evaluation.UnsupportedReason);
                    context = evaluation.Context;
                    if (!evaluation.Passed) continue;
                    count = unchecked(count + rule.ParamInt2);
                    RelicConditionRecord record = RelicConditionModel.Record(context, rule.Conditions);
                    if (!record.Supported) return new ElixirMixPlan(null, Array.Empty<int>(), record.UnsupportedReason);
                    context = record.Context;
                    RelicResourceEffect[] rules = context.Relics![relicIndex].ResourceEffects!.ToArray();
                    rules[ruleIndex] = rule.WithConditions(record.Conditions);
                    CombatRelicState[] owners = context.Relics.ToArray();
                    owners[relicIndex] = owners[relicIndex].WithResourceEffects(rules);
                    context = context.WithRelics(owners);
                }
            }
            RelicResourceEffect[] adjustments = context.Relics!.SelectMany(relic =>
                relic.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
                .Where(rule => rule.Type == RelicResourceModel.AdjustMixAmount).ToArray();
            var amounts = new int[Math.Max(0, count)];
            for (int mixNumber = 0; mixNumber < amounts.Length; mixNumber++)
            {
                int amount = mixNumber == 0 ? baseAmount : 0;
                if (mixNumber > 0)
                    foreach (RelicResourceEffect rule in adjustments)
                        if (rule.ParamMinInt != 0) amount = rule.ParamMinInt;
                foreach (RelicResourceEffect rule in adjustments)
                    amount = unchecked(amount + rule.Amount);
                amounts[mixNumber] = Math.Max(amount, 1);
            }
            return new ElixirMixPlan(context, amounts);
        }

        public static CardGenerationResult Apply(CombatContext source, ElixirCatalog? catalog, int amount, int choiceIndex)
        {
            if (catalog == null || source.MixState == null || source.OtherPiles == null || source.CardInstances == null ||
                source.CardRegistry == null || amount < 1)
                return Unsupported("Mix requires the original elixir catalog, selection state and complete card piles.");
            CardToken? current = source.Cards.Hand.FirstOrDefault(card =>
                catalog.Recipes.Any(recipe => recipe.Creation.DataId == card.DataId));
            if (current == null && source.Cards.Hand.Count >= source.MaxHandSize)
                return new CardGenerationResult(source, Array.Empty<CardToken>());
            ElixirRecipe? recipe = current == null ? null : catalog.Recipes.First(item => item.Creation.DataId == current.DataId);
            ElixirDraftOptions draft = recipe == null
                ? NewDraft(catalog, source.BattleRng, amount, source.MixState.LastCardDataId)
                : UpgradeDraft(recipe, source.BattleRng, amount);
            if (choiceIndex < 0 || choiceIndex >= draft.Offers.Count)
                return Unsupported("Mix draft selection is missing or outside its offers.");
            ElixirDraftOption chosen = draft.Offers[choiceIndex];
            CombatContext context = source.WithBattleRng(draft.Rng);
            int sourceCardId = 0;
            if (current != null)
            {
                CardPileState? standby = context.OtherPiles!.FirstOrDefault(pile => pile.Name == "Standby");
                if (standby == null) return Unsupported("Mix upgrade requires the Standby pile.");
                context = context.WithCards(new CardCycleState(context.Cards.Hand.Where(card => card.InstanceId != current.InstanceId).ToArray(),
                    context.Cards.Draw, context.Cards.Discard, context.Cards.Rng, context.Cards.DrawModifier,
                    context.Cards.ExternalInteractions, context.Cards.BonusDraw, context.Cards.NextDrawUpgrades,
                    context.Cards.NextUpgradeReferenceId, context.Cards.DeckDrawDisabledThisTurn));
                context = context.WithOtherPiles(context.OtherPiles!.Select(pile => pile.Name == "Standby"
                    ? CardPileModel.Add(pile, current) : pile).ToArray());
                sourceCardId = current.InstanceId;
            }
            var rule = new CardGenerationRule("HandPile", 1, new[] { chosen.Recipe.Creation },
                copyModifiers: current != null, upgrade: current == null ? chosen.Upgrade : null,
                deterministicPoolSelection: true, upgradeIsPermanent: true);
            CardGenerationResult generated = CardGenerationModel.Apply(context, rule, sourceCardId);
            if (!generated.Supported) return generated;
            context = generated.Context!;
            if (current != null && chosen.Upgrade != null)
            {
                CardInstanceState card = context.FindCard(generated.AddedCards.Single().InstanceId)!;
                string? error = CardExhaustModel.PermanentUpgradeError(card, chosen.Upgrade);
                if (error != null) return Unsupported(error);
                context = CardMagicPowerModel.Refresh(context,
                    card.WithPermanentUpgrade(chosen.Upgrade.WithSharedReference(null), false), out CardInstanceState refreshed);
                context = context.WithCard(refreshed);
            }
            CardInstanceState completed = context.FindCard(generated.AddedCards.Single().InstanceId)!;
            if (completed.MaskDescriptor != null)
            {
                string[] castEffects = completed.MaskDescriptor.AuthoredCastEffects.Concat(completed.Permanent.Upgrades
                    .GroupBy(upgrade => upgrade.DataId, StringComparer.Ordinal).Select(group => group.First())
                    .SelectMany(upgrade => upgrade.Lifecycle?.Triggers ?? Array.Empty<CardLifecycleTrigger>())
                    .Where(trigger => trigger.Type == "OnCast").SelectMany(trigger => trigger.Effects)).ToArray();
                context = context.WithCard(completed.WithMaskDescriptor(
                    completed.MaskDescriptor.WithAuthoredCastEffects(castEffects)));
            }
            return new CardGenerationResult(context.WithMixState(new MixRuntimeState(
                current == null ? chosen.Recipe.Creation.DataId : source.MixState.LastCardDataId,
                chosen.Upgrade?.DataId)), generated.AddedCards);
        }

        private static CardGenerationResult Unsupported(string reason) =>
            new CardGenerationResult(null, Array.Empty<CardToken>(), reason);

        public static ElixirDraftOptions NewDraft(ElixirCatalog catalog, UnityRng rng, int amount,
            string? lastCardDataId)
        {
            if (amount < 1) throw new ArgumentOutOfRangeException(nameof(amount));
            var candidates = catalog.Recipes.ToList();
            if (candidates.Count > amount)
            {
                int prior = candidates.FindIndex(recipe => recipe.Creation.DataId == lastCardDataId);
                if (prior >= 0) candidates.RemoveAt(prior);
            }
            var selected = new List<ElixirRecipe>();
            while (candidates.Count > 0 && selected.Count < amount)
            {
                RngDraw draw = rng.Range(0, candidates.Count);
                rng = draw.State;
                selected.Add(candidates[draw.Value]);
                candidates.RemoveAt(draw.Value);
            }
            var usedUpgrades = new HashSet<string>(StringComparer.Ordinal);
            var offers = new List<ElixirDraftOption>();
            foreach (ElixirRecipe recipe in selected)
            {
                CardUpgradeModifier? upgrade = recipe.BaseUpgrade;
                if (upgrade == null)
                {
                    CardUpgradeModifier[] available = recipe.Upgrades
                        .Where(item => !usedUpgrades.Contains(item.DataId)).ToArray();
                    if (available.Length > 0)
                    {
                        RngDraw draw = rng.Range(0, available.Length);
                        rng = draw.State;
                        upgrade = available[draw.Value];
                        usedUpgrades.Add(upgrade.DataId);
                    }
                }
                offers.Add(new ElixirDraftOption(recipe, upgrade));
            }
            return new ElixirDraftOptions(offers, rng);
        }

        public static ElixirDraftOptions UpgradeDraft(ElixirRecipe recipe, UnityRng rng, int amount)
        {
            if (amount < 1) throw new ArgumentOutOfRangeException(nameof(amount));
            var candidates = recipe.Upgrades.ToList();
            var offers = new List<ElixirDraftOption>();
            while (candidates.Count > 0 && offers.Count < amount)
            {
                RngDraw draw = rng.Range(0, candidates.Count);
                rng = draw.State;
                CardUpgradeModifier upgrade = candidates[draw.Value];
                offers.Add(new ElixirDraftOption(recipe, upgrade));
                candidates.RemoveAt(draw.Value);
            }
            return new ElixirDraftOptions(offers, rng);
        }
    }
}
