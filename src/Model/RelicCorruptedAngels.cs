using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicDrawAndEnergyRule
    {
        public CardUpgradeMaskRule? TriggerFilter { get; }
        public CardUpgradeMaskRule? DrawFilter { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public RelicDrawAndEnergyRule(CardUpgradeMaskRule? triggerFilter, CardUpgradeMaskRule? drawFilter,
            IReadOnlyList<string> externalInteractions)
        { TriggerFilter = triggerFilter; DrawFilter = drawFilter; ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray()); }
    }

    internal static class RelicCorruptedAngelsModel
    {
        internal const string DataId = "3b0913dd-03e9-46d3-a3ff-b86cfc12d224";
        internal const string Effect = "RelicEffectDrawCardAndGainEmberOnDrawType";
        private static readonly string[] Excluded = {
            "913694e9-bace-4480-8691-24f699ae583e", "186180e9-94e9-47d0-b69c-865b9a6d27a4",
            "21038774-c0a4-413c-ab7e-27e458228e5f", "13981930-b732-4c42-9bd3-1950d7a553b5",
            "a27a19b7-9c21-48c3-85b7-5e4b643740be", "17fef621-2bb7-4847-b41d-52a89795c784",
            "f94572f9-d280-4990-a112-2c94f8fd7ede", "62cc5532-5fa0-4e66-9fec-85be573af359",
            "2785cc8e-d0e8-46c0-84ab-9c1d602525d4", "fbf74ec4-2781-4b99-b6f7-b7d75236da60",
            "ed50a054-2de3-47b8-8419-204f72e75fbd", "3b603b7f-300c-4022-9fc2-275448446e58",
            "4d1f27af-3122-4a08-87e3-5ef5a14ced94" };
        internal static bool Enabled(CombatContext context) => context.Relics?.Any(owner => owner.EffectTypes.Contains(Effect)) == true;
        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect) =>
            owner.DataId == DataId && owner.AssetKey == "CorruptedAngelsArtifact" && owner.IsCovenant == false &&
            owner.DisallowedInPlacementPhase == false && owner.EffectTypes.SequenceEqual(new[] { Effect }) &&
            owner.ResourceEffects?.Count == 1 && effect.EffectIndex == 0 && effect.Type == Effect &&
            effect.ParamInt == 1 && effect.ParamInt2 == 1 && effect.Amount == 1 && effect.ParamMinInt == 3 &&
            effect.ParamFloat == 0 && !effect.CanApplyInPreviewMode && effect.RequiredMutatorId == null && effect.Conditions.Count == 0 &&
            effect.DrawAndEnergy?.ExternalInteractions.Count == 0 && OriginalFilter(effect.DrawAndEnergy.TriggerFilter, true) &&
            OriginalFilter(effect.DrawAndEnergy.DrawFilter, false) ? null : "Missing or changed original CorruptedAngelsArtifact draw/energy definition.";

        private static bool Empty<T>(UpgradeMaskContent<T> content, int required = 0) => content.Required.Count == 0 &&
            content.Excluded.Count == 0 && content.RequiredOperator == required && content.ExcludedOperator == 0;
        private static bool OriginalFilter(CardUpgradeMaskRule? mask, bool trigger) => mask != null &&
            mask.AssetKey == (trigger ? "OnlyScourgeAndBlight" : "ExcludeScourgeAndBlight") &&
            mask.CardType == (trigger ? "Blight" : "Invalid") && mask.AdditionalCardTypes.SequenceEqual(trigger ? new[] { "Junk" } : Array.Empty<string>()) &&
            Empty(mask.Rarities, 1) && Empty(mask.Subtypes) && Empty(mask.Statuses) && Empty(mask.Traits) && Empty(mask.Effects) &&
            Empty(mask.LinkedClans) && Empty(mask.Upgrades) && mask.AllowedPools.Count == 0 &&
            (trigger ? mask.DisallowedPools.Count == 0 : mask.DisallowedPools.Count == 1 && mask.DisallowedPools[0].SequenceEqual(Excluded)) &&
            mask.RequiredSizes.Count == 0 && mask.ExcludedSizes.Count == 0 && mask.MinCost == 0 && mask.MaxCost == 99 && mask.TargetMode == 0 &&
            !mask.ExcludeNonAttackingMonsters && !mask.RequireXCost && !mask.ExcludeXCost && !mask.ExcludeIfHasUnitAbility &&
            !mask.ExcludeIfHasGraftedEquipment && !mask.ExcludeIfHasAnyUpgrades && !mask.ExcludeIfHasNoUpgrades;

        // Native ICardDrawnRelicEffect ignores CanApplyInPreviewMode. The original
        // filters even accept null; match count advances when DrawSpecific refuses.
        internal static RelicFirstCardCostResult Apply(CombatContext source, int cardId, out bool applied,
            BattlePlayRules? definitions = null, bool placement = false)
        {
            applied = false; CombatContext context = source;
            foreach (CombatRelicState owner in source.Relics ?? Array.Empty<CombatRelicState>())
            foreach (RelicResourceEffect effect in owner.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
            {
                if (effect.Type != Effect) continue;
                string? error = Validate(owner, effect); if (error != null) return new(source, error);
                if (placement && owner.DisallowedInPlacementPhase == true) continue;
                if (source.Relics!.Any(item => item.EffectTypes.Contains(RelicRandomDrawCostModel.Effect)))
                    return new(source, "Combined random-cost and Corrupted Angels draw callback ordering needs native evidence.");
                CardInstanceState? card = cardId == 0 ? null : context.FindCard(cardId);
                if (cardId != 0 && card?.MaskDescriptor == null) return new(source, "Corrupted Angels requires captured source-card masks.");
                RelicDrawAndEnergyRule rule = effect.DrawAndEnergy!;
                if (!CardUpgradeMaskModel.FilterCard(rule.TriggerFilter!, card == null ? null : CardBranchMaskModel.Resolve(card))) continue;
                if (context.Cards.ExternalInteractions.Count > 0) return new(source, string.Join("; ", context.Cards.ExternalInteractions));
                if (context.Cards.BonusDraw?.Listeners.Count > 0 || context.Cards.NextDrawUpgrades?.Count > 0)
                    return new(source, "Corrupted Angels with pending draw upgrades requires separate native evidence.");
                // Both candidate copies are taken before the first nested draw.
                CardToken[] candidates = context.Cards.Draw.Reverse().Concat(context.Cards.Discard.Reverse()).ToArray();
                foreach (CardToken candidate in candidates)
                {
                    CardInstanceState? instance = context.FindCard(candidate.InstanceId);
                    if (instance?.MaskDescriptor == null) return new(source, "Corrupted Angels requires candidate-card masks.");
                    if (!CardUpgradeMaskModel.FilterCard(rule.DrawFilter!, CardBranchMaskModel.Resolve(instance))) continue;
                    TypedDrawResult draw = NextDrawUpgradeModel.DrawSpecific(context, candidate.InstanceId, 0, definitions);
                    if (!draw.Supported) return new(source, draw.UnsupportedReason);
                    context = draw.Context!;
                    break;
                }
                error = EnergyModel.Validate(context); if (error != null) return new(source, error);
                context = EnergyModel.Apply(context, "GainEnergy", effect.Amount); applied = true;
            }
            return new(context);
        }

        // Ordinary hand draws must interleave native callbacks with each primary
        // draw. Extra draws do not consume the fixed primary-loop count.
        internal static TypedDrawResult DrawHand(CombatContext source, int handSize, BattlePlayRules? definitions = null)
        {
            if (handSize < 0) return new(null, Array.Empty<int>(), "Invalid hand size.");
            if (source.Cards.Hand.Count == source.MaxHandSize ||
                source.Cards.Hand.Count + source.Cards.Draw.Count + source.Cards.Discard.Count == 0)
                return new(source, Array.Empty<int>());
            if (source.Cards.BonusDraw?.Listeners.Count > 0 || source.Cards.NextDrawUpgrades?.Count > 0)
                return new(null, Array.Empty<int>(), "Corrupted Angels with pending draw upgrades requires separate native evidence.");
            TypedDrawResult result = NextDrawUpgradeModel.DrawCards(source, Math.Max(0, unchecked(handSize + source.Cards.DrawModifier)), 0, definitions);
            if (!result.Supported) return result;
            CardCycleState cards = result.Context!.Cards;
            return new(result.Context.WithCards(new(cards.Hand, cards.Draw, cards.Discard, cards.Rng, 0, cards.ExternalInteractions,
                cards.BonusDraw, cards.NextDrawUpgrades, cards.NextUpgradeReferenceId)), result.DrawnCardIds);
        }
    }
}
