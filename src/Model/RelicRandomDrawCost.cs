using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicRandomDrawCostRule
    {
        public int Maximum { get; }
        public bool Bias { get; }
        public int ParamInt3 { get; }
        public bool ParamBool2 { get; }
        public bool ParamBool3 { get; }
        public bool ParamBool4 { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public RelicRandomDrawCostRule(int maximum, bool bias, int paramInt3, bool paramBool2, bool paramBool3,
            bool paramBool4, IReadOnlyList<string> externalInteractions)
        { Maximum = maximum; Bias = bias; ParamInt3 = paramInt3; ParamBool2 = paramBool2; ParamBool3 = paramBool3;
            ParamBool4 = paramBool4; ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray()); }
    }

    internal static class RelicRandomDrawCostModel
    {
        internal const string Effect = "RelicEffectRandomCardCostOnDraw";
        internal const string DataId = "834f4c02-2987-469c-8634-eac97f2f67c8";
        internal const string AssetKey = "RandomCardCostOnDraw";
        internal static string? Validate(CombatRelicState owner, RelicResourceEffect resource)
        {
            RelicRandomDrawCostRule? rule = resource.RandomDrawCost;
            return owner.DataId == DataId && owner.AssetKey == AssetKey && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
                owner.EffectTypes.SequenceEqual(new[] { Effect, RelicResourceModel.Draw }) && resource.EffectIndex == 0 && resource.Type == Effect &&
                resource.ParamInt == 3 && resource.ParamInt2 == 0 && resource.ParamMinInt == 0 && resource.ParamFloat == 0 && resource.Amount == 3 &&
                !resource.CanApplyInPreviewMode && resource.RequiredMutatorId == null && resource.Conditions.Count == 0 &&
                rule?.Maximum == 4 && rule.Bias && rule.ParamInt3 == 0 && !rule.ParamBool2 && !rule.ParamBool3 && !rule.ParamBool4 &&
                rule.ExternalInteractions.Count == 0 && owner.ResourceEffects?.Count == 2 && owner.ResourceEffects[1].Type == RelicResourceModel.Draw &&
                owner.ResourceEffects[1].ParamInt == 2 && owner.ResourceEffects[1].Amount == 2 && owner.ResourceEffects[1].Conditions.Count == 0
                ? null : "Missing or changed original RandomCardCostOnDraw definition and +2 draw consumer.";
        }

        // RelicManager does not consult CanApplyInPreviewMode for ICardDrawnRelicEffect.
        // Champion/null exits precede the random draw; full reduction follows it.
        internal static RelicFirstCardCostResult Apply(CombatContext source, int cardId, out bool applied, bool placement = false)
        {
            applied = false; CombatContext context = source;
            foreach (CombatRelicState owner in source.Relics ?? Array.Empty<CombatRelicState>())
            foreach (RelicResourceEffect resource in owner.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
            {
                if (resource.Type != Effect) continue;
                string? error = Validate(owner, resource); if (error != null) return new(source, error);
                if (placement && owner.DisallowedInPlacementPhase == true || cardId == 0) continue;
                CardInstanceState? card = context.FindCard(cardId);
                if (card?.MaskDescriptor?.Definition.IsChampionCard == null) return new(source, "Random draw cost requires captured champion classification.");
                if (card.MaskDescriptor.Definition.IsChampionCard.Value) continue;
                error = CardModifierModel.UnsupportedReason(card); if (error != null) return new(source, error);
                int baseline = card.RawCostOverride ?? card.MaskDescriptor.BaseCost;
                int cost = CardModifierModel.UpgradedStat(baseline, "Cost", true, card.Permanent, card.Temporary);
                var random = context.BattleRng.Range(resource.ParamMinInt, resource.RandomDrawCost!.Maximum + 2);
                int target = random.Value >= resource.RandomDrawCost.Maximum ? 1 + random.Value - resource.RandomDrawCost.Maximum : random.Value;
                context = context.WithBattleRng(random.State);
                bool Full(CardModifiers modifiers) => new[] { modifiers.Offsets.Cost }.Concat(modifiers.Upgrades.Select(upgrade => upgrade.Stats.Cost)).Any(value => Math.Abs(value) >= 99);
                if (Full(card.Temporary) | Full(card.Permanent)) target = 0;
                else if (cost < baseline) target = Math.Max(0, unchecked(target + cost - baseline));
                if (target != cost)
                {
                    CardStatModifier stats = new CardStatModifier(cost: target - cost);
                    var lifecycle = new CardLifecycleUpgrade(new CardMaskUpgrade("", stats, Array.Empty<UpgradeMaskStatus>(), false, false, false, false),
                        "", false, true, Array.Empty<CardTraitValue>(), Array.Empty<string>(), false, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<CardLifecycleTrigger>());
                    var upgrade = new CardUpgradeModifier("", "", stats, Array.Empty<CombatStatus>(), true, false, false, 0, 0,
                        Array.Empty<string>(), maskMetadata: new CardUpgradeMaskMetadata(Array.Empty<UpgradeMaskStatus>(), false, false, false, false), lifecycle: lifecycle);
                    context = context.WithCard(card.WithModifiers(card.Permanent, UnitModifierModel.Add(card.Temporary, upgrade)));
                }
                applied = true;
            }
            return new(context);
        }

        internal static CombatContext Drawn(CombatContext source, IEnumerable<int> ids, out string? error)
        {
            error = null; CombatContext context = source;
            foreach (int id in ids)
            {
                var result = Apply(context, id, out _);
                if (!result.Supported) { error = result.UnsupportedReason; return source; }
                context = result.Context;
            }
            return context;
        }
    }
}
