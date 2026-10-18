using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicOpeningDrawRule
    {
        public CardUpgradeMaskRule? FirstMask { get; }
        public CardUpgradeMaskRule? SecondMask { get; }
        public bool ExpandDraw { get; }
        public CardUpgradeModifier? Upgrade { get; }
        public RelicOpeningDrawRule(CardUpgradeMaskRule? firstMask, CardUpgradeMaskRule? secondMask, bool expandDraw, CardUpgradeModifier? upgrade)
        { FirstMask = firstMask; SecondMask = secondMask; ExpandDraw = expandDraw; Upgrade = upgrade; }
    }
    public static class RelicOpeningDrawModel
    {
        public const string Draw = "RelicEffectDrawOnPreOpeningDraw";
        public const string Cascade = "RelicEffectDrawOnPreOpeningDrawCascade";
        public static bool IsEffect(string type) => type == Draw || type == Cascade;
        public static RelicResourceResult Apply(CombatContext source, bool placement = true, bool preview = false)
        {
            var context = source; int selected = 0;
            foreach (var owner in source.Relics ?? Array.Empty<CombatRelicState>())
            foreach (var effect in owner.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
            {
                if (!IsEffect(effect.Type)) continue;
                var result = ApplyOne(context, owner.DataId, effect.EffectIndex, placement, preview);
                if (!result.Supported) return result;
                context = result.Context!; selected += result.Value;
            }
            return new RelicResourceResult(context, selected);
        }
        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect)
        {
            if (effect.OpeningDraw == null || !owner.DisallowedInPlacementPhase.HasValue || effect.ParamInt < 0 ||
                effect.Conditions.Count != 0 || effect.RequiredMutatorId != null) return "Missing or unsupported original pre-opening draw definition.";
            var upgrade = effect.OpeningDraw.Upgrade;
            if (upgrade != null && (CardModifierModel.UpgradeInteractions(upgrade).Any() || upgrade.Unique ||
                upgrade.Lifecycle is CardLifecycleUpgrade lifecycle && (lifecycle.AddedTraits.Count > 0 || lifecycle.RemovedRuntimeTypes.Count > 0 ||
                    lifecycle.ReplacedAssets.Count > 0 || lifecycle.Triggers.Count > 0 || lifecycle.TraitsModified.Count > 0)))
                return "Pre-opening draw requires a captured numeric, non-unique temporary upgrade.";
            return null;
        }
        public static RelicResourceResult ApplyOne(CombatContext source, string ownerId, int effectIndex,
            bool placement = true, bool preview = false)
        {
            var owners = source.Relics?.Where(owner => owner.DataId == ownerId).ToArray();
            if (owners?.Length != 1) return Fail("Pre-opening draw requires one captured owner.");
            var owner = owners[0]; var effects = owner.ResourceEffects?.Where(effect => effect.EffectIndex == effectIndex && IsEffect(effect.Type)).ToArray();
            if (effects?.Length != 1) return Fail("Missing original pre-opening draw effect index.");
            var effect = effects[0]; string? error = Validate(owner, effect);
            if (error != null) return Fail(error);
            if (owner.DisallowedInPlacementPhase == placement || preview && !effect.CanApplyInPreviewMode || effect.ParamInt <= 0)
                return new RelicResourceResult(source, 0);
            if (source.Cards.ExternalInteractions.Count > 0 || source.Cards.NextDrawUpgrades?.Count > 0)
                return Fail("Unmodeled pre-opening draw callback/queued-upgrade interactions.");
            var rule = effect.OpeningDraw!; var context = source; int selectedCount = 0;
            int first = DrawGroup(rule.FirstMask, effect.ParamInt, out error);
            if (error != null) return Fail(error);
            int secondCount = effect.Type == Cascade ? effect.ParamInt - first : effect.ParamInt2;
            if (secondCount > 0) DrawGroup(rule.SecondMask, secondCount, out error);
            if (error != null) return Fail(error);
            return new RelicResourceResult(context, selectedCount);

            int DrawGroup(CardUpgradeMaskRule? mask, int amount, out string? failure)
            {
                failure = null;
                var candidates = new System.Collections.Generic.List<CardToken>();
                foreach (var token in context.Cards.Draw)
                {
                    if (mask == null) { candidates.Add(token); continue; }
                    var card = context.FindCard(token.InstanceId);
                    if (card?.MaskDescriptor == null) { failure = "Opening draw requires captured owned-card mask metadata."; return 0; }
                    if (CardUpgradeMaskModel.FilterCard(mask, CardBranchMaskModel.Resolve(card))) candidates.Add(token);
                }
                int count = Math.Min(amount, candidates.Count);
                if (count <= 0) return 0;
                if (rule.ExpandDraw) context = context.WithCards(Cycle(context.Cards, modifier: unchecked(context.Cards.DrawModifier + count)));
                for (int i = 0; i < count; i++)
                {
                    var choice = context.Cards.Rng.Range(0, candidates.Count); var token = candidates[choice.Value]; candidates.RemoveAt(choice.Value);
                    context = context.WithCards(Cycle(context.Cards, rng: choice.State)); selectedCount++;
                    if (rule.Upgrade != null)
                    {
                        var card = context.FindCard(token.InstanceId);
                        if (card == null) { failure = "Opening draw upgrade target was not captured."; return count; }
                        context = context.WithCard(card.WithModifiers(card.Permanent, UnitModifierModel.Add(card.Temporary, rule.Upgrade)));
                    }
                    // Native selects and upgrades before DrawSpecificCard rejects a full hand.
                    if (context.Cards.Hand.Count >= context.MaxHandSize) continue;
                    var cycle = context.Cards;
                    context = context.WithCards(new CardCycleState(new[] { token }.Concat(cycle.Hand).ToArray(),
                        cycle.Draw.Where(item => item.InstanceId != token.InstanceId).ToArray(), cycle.Discard, cycle.Rng, cycle.DrawModifier,
                        cycle.ExternalInteractions, cycle.BonusDraw, cycle.NextDrawUpgrades, cycle.NextUpgradeReferenceId));
                    context = context.WithStatistics(context.Statistics?.Increment(token.InstanceId, "TimesDrawn"));
                    context = ConduitModel.Drawn(context, new[] { token.InstanceId });
                    context = RelicRandomDrawCostModel.Drawn(context, new[] { token.InstanceId }, out failure);
                    if (failure != null) return count;
                }
                return count;
            }
        }
        private static CardCycleState Cycle(CardCycleState source, UnityRng? rng = null, int? modifier = null) =>
            new CardCycleState(source.Hand, source.Draw, source.Discard, rng ?? source.Rng, modifier ?? source.DrawModifier,
                source.ExternalInteractions, source.BonusDraw, source.NextDrawUpgrades, source.NextUpgradeReferenceId);
        private static RelicResourceResult Fail(string error) => new RelicResourceResult(null, 0, error);
    }
}
