using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardChooseDiscardRule
    {
        public int Condition { get; }
        public bool ExcludeEphemeralCopies { get; }
        public bool GainEmberForCost { get; }
        public bool HasReplacementPool { get; }

        public CardChooseDiscardRule(int condition, bool excludeEphemeralCopies, bool gainEmberForCost,
            bool hasReplacementPool)
        { Condition = condition; ExcludeEphemeralCopies = excludeEphemeralCopies;
            GainEmberForCost = gainEmberForCost; HasReplacementPool = hasReplacementPool; }
    }

    public sealed class CardChooseDiscardResult
    {
        public CombatContext? Context { get; }
        public IReadOnlyList<CardPileState>? OtherPiles { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Context != null;

        internal CardChooseDiscardResult(CombatContext? context, IReadOnlyList<CardPileState>? otherPiles,
            string? unsupportedReason = null)
        { Context = context; OtherPiles = otherPiles; UnsupportedReason = unsupportedReason; }
    }

    public static class CardChooseDiscardModel
    {
        public static string? Validate(CardActionEffect effect, CombatContext? context)
        {
            CardChooseDiscardRule? rule = effect.ChooseDiscard;
            if (rule == null) return "Missing choose-discard parameters.";
            if (effect.Value is < 0 or > 3)
                return "Unmodeled choose-discard mode " + effect.Value + ".";
            if (rule.Condition != 0 && rule.Condition != 5)
                return "Unmodeled choose-discard condition " + rule.Condition + ".";
            if (rule.HasReplacementPool) return "Choose-discard replacement pools require card generation.";
            if (effect.Target is not ("Hand" or "Room")) return "Unmodeled choose-discard target.";
            if (effect.Range != null) return "Choose-discard does not support integer ranges.";
            if (context?.CardRegistry == null) return "Choose-discard requires captured card instances.";
            if (rule.GainEmberForCost && context.Relics?.Any(relic =>
                relic.EffectTypes.Contains("RelicEffectAdditionalSmeltEmber")) == true)
                return "Additional smelt Ember relic effects require native calibration.";
            if (effect.Value is 2 or 3)
            {
                string? forgeError = ForgePointsModel.Validate(context);
                if (forgeError != null) return forgeError;
            }
            if ((effect.Value is 1 or 2 or 3) && rule.GainEmberForCost)
            {
                string? energyError = EnergyModel.Validate(context);
                if (energyError != null) return energyError;
            }
            return null;
        }

        public static IReadOnlyList<int> EligibleCards(CombatContext context, CardActionEffect effect,
            int sourceCardId, out string? error)
        {
            error = Validate(effect, context);
            if (error != null) return Array.Empty<int>();
            string? sourceDataId = context.FindCard(sourceCardId)?.DataId;
            if (effect.ChooseDiscard!.ExcludeEphemeralCopies && sourceDataId == null)
            {
                error = "Choose-discard requires its source card identity.";
                return Array.Empty<int>();
            }
            var eligible = new List<int>();
            foreach (CardToken token in context.Cards.Hand.Where(card => card.InstanceId != sourceCardId))
            {
                CardInstanceState? instance = context.FindCard(token.InstanceId);
                if (instance?.MaskDescriptor == null)
                {
                    error = "Choose-discard requires branch-owned traits for card " + token.InstanceId + ".";
                    return Array.Empty<int>();
                }
                CardUpgradeMaskCard card;
                try { card = CardBranchMaskModel.Resolve(instance); }
                catch (InvalidOperationException exception)
                {
                    error = "Choose-discard cannot resolve card " + token.InstanceId + ": " + exception.Message;
                    return Array.Empty<int>();
                }
                if (card.Traits.Contains("CardTraitInfusion") ||
                    effect.Value == 0 && card.Traits.Contains("CardTraitPersistent") ||
                    effect.ChooseDiscard!.ExcludeEphemeralCopies && token.DataId == sourceDataId &&
                    card.Traits.Contains("CardTraitEphemeral")) continue;
                eligible.Add(token.InstanceId);
            }
            return Array.AsReadOnly(eligible.ToArray());
        }

        public static CardChooseDiscardResult Apply(CombatContext source, CardActionEffect effect, int sourceCardId,
            int effectIndex, BattlePlayRules? definitions, int roomIndex, int? selectedCardId)
        {
            IReadOnlyList<int> eligible = EligibleCards(source, effect, sourceCardId, out string? error);
            if (error != null) return Unsupported(error);
            if (eligible.Count == 0)
                return selectedCardId.HasValue ? Unsupported("Choose-discard has no eligible selected card.") :
                    new CardChooseDiscardResult(source, source.OtherPiles);
            if (!selectedCardId.HasValue || !eligible.Contains(selectedCardId.Value))
                return Unsupported("Choose-discard requires one eligible selected card.");
            CombatContext context = source;
            if (effect.Value == 2)
            {
                if (context.Enchantments != null || context.RoomCapacities?.FirstOrDefault(room =>
                    room.RoomIndex == roomIndex)?.Attachments?.Count > 0)
                    return Unsupported("Room smelt modifiers require native room-state calibration.");
                int forge = 0;
                int ember = 0;
                int[] consumedIds = context.Cards.Hand.Where(card => card.InstanceId != sourceCardId &&
                    card.InstanceId != selectedCardId.Value).Reverse().Select(card => card.InstanceId).ToArray();
                foreach (int id in consumedIds)
                {
                    CardInstanceState? instance = context.FindCard(id);
                    if (instance?.MaskDescriptor == null) return Unsupported("Missing consumed card cost metadata.");
                    CardUpgradeMaskCard card = CardBranchMaskModel.Resolve(instance);
                    int bonus = RefineBonus(instance, card, out string? refineError);
                    if (refineError != null) return Unsupported(refineError);
                    int rawCost = instance.RawCostOverride ?? instance.MaskDescriptor.BaseCost;
                    int amount = unchecked(rawCost + bonus);
                    if (amount < 0) return Unsupported("Negative smelt cost requires native calibration.");
                    if (amount > 0)
                    {
                        forge = unchecked(forge + amount);
                        if (effect.ChooseDiscard!.GainEmberForCost) ember = unchecked(ember + amount);
                    }
                    HandRemovalResult consumed = HandRemovalModel.Apply(context, 1, sourceCardId, effectIndex,
                        definitions, context.OtherPiles, selectedCardIds: [id], effectCounterType: null);
                    if (!consumed.Supported) return Unsupported(consumed.UnsupportedReason!);
                    context = consumed.Context!;
                }
                if (forge > 0)
                {
                    ForgePointsResult points = ForgePointsModel.Apply(context, forge);
                    if (!points.Supported) return Unsupported(points.UnsupportedReason!);
                    context = points.Context!;
                }
                if (ember > 0) context = EnergyModel.Apply(context, "GainEnergy", ember);
                return new CardChooseDiscardResult(context, context.OtherPiles);
            }
            if (effect.Value is 1 or 3)
            {
                CardInstanceState instance = context.FindCard(selectedCardId.Value)!;
                if (instance.MaskDescriptor == null) return Unsupported("Missing selected card cost metadata.");
                CardUpgradeMaskCard card = CardBranchMaskModel.Resolve(instance);
                int bonus = RefineBonus(instance, card, out string? refineError);
                if (refineError != null) return Unsupported(refineError);
                if (context.Enchantments != null || context.RoomCapacities?.FirstOrDefault(room =>
                    room.RoomIndex == roomIndex)?.Attachments?.Count > 0)
                    return Unsupported("Room smelt modifiers require native room-state calibration.");
                int amount = unchecked((instance.RawCostOverride ?? instance.MaskDescriptor.BaseCost) + bonus);
                if (amount < 0) return Unsupported("Negative smelt cost requires native calibration.");
                if (effect.Value == 3 && amount > 0)
                {
                    ForgePointsResult forge = ForgePointsModel.Apply(context, amount);
                    if (!forge.Supported) return Unsupported(forge.UnsupportedReason!);
                    context = forge.Context!;
                }
                if (effect.ChooseDiscard!.GainEmberForCost && amount > 0)
                    context = EnergyModel.Apply(context, "GainEnergy", amount);
            }
            HandRemovalResult removed = HandRemovalModel.Apply(context, effect.Value == 0 ? 0 : 1,
                sourceCardId, effectIndex, definitions, context.OtherPiles, selectedCardIds: [selectedCardId.Value],
                effectCounterType: null);
            return removed.Supported ? new CardChooseDiscardResult(removed.Context, removed.OtherPiles) :
                Unsupported(removed.UnsupportedReason!);
        }

        private static int RefineBonus(CardInstanceState instance, CardUpgradeMaskCard card, out string? error)
        {
            error = null;
            if (!card.Traits.Contains("CardTraitRefine")) return 0;
            if (instance.Permanent.Upgrades.Concat(instance.Temporary.Upgrades).Any(upgrade =>
                upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == "CardTraitRefine") == true))
            {
                error = "Upgrade-added Refine requires captured trait resource identities.";
                return 0;
            }
            CardTraitValue[] traits = CardOwnedMaskModel.Resolve(CardBranchMaskModel.OwnedState(instance))
                .State.Traits.Composition.CombinedTraits?.Where(trait =>
                    trait.RuntimeType == "CardTraitRefine").ToArray() ?? Array.Empty<CardTraitValue>();
            if (traits.Length != 1 || !traits[0].HasData)
            {
                error = "Multiple or anonymous Refine traits require captured trait resource identities.";
                return 0;
            }
            return traits[0].ParamInt;
        }

        private static CardChooseDiscardResult Unsupported(string reason) => new(null, null, reason);
    }
}
