using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicSuppressExhaustRule
    {
        public int Chance { get; }
        public string CardType { get; }
        public bool TestPassed { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public RelicSuppressExhaustRule(int chance, string cardType, bool testPassed, IReadOnlyList<string> externalInteractions)
        { Chance = chance; CardType = cardType; TestPassed = testPassed; ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray()); }
        internal RelicSuppressExhaustRule WithPassed(bool value) => new(Chance, CardType, value, ExternalInteractions);
    }
    internal static class RelicSuppressExhaustModel
    {
        internal const string Effect = "RelicEffectSuppressExhaust";
        internal const string DataId = "51d95691-d59e-42f1-84ba-8c530743df69";
        internal const string AssetKey = "ChanceToNotConsume";
        internal const string Trait = "CardTraitExhaustState";
        internal static string? Validate(CombatRelicState owner, RelicResourceEffect resource) =>
            owner.DataId == DataId && owner.AssetKey == AssetKey && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
            owner.EffectTypes.SequenceEqual(new[] { Effect }) && resource.EffectIndex == 0 && resource.Type == Effect &&
            resource.ParamInt == 50 && resource.Amount == 50 && resource.ParamInt2 == 0 && resource.ParamMinInt == 0 && resource.ParamFloat == 0 &&
            !resource.CanApplyInPreviewMode && resource.RequiredMutatorId == null && resource.Conditions.Count == 0 &&
            resource.SuppressExhaust?.Chance == 50 && resource.SuppressExhaust.CardType == "Spell" && resource.SuppressExhaust.ExternalInteractions.Count == 0
                ? null : "Missing or changed original ChanceToNotConsume definition or initialized state.";
        internal static RelicFirstCardCostResult PreDiscard(CombatContext source, int cardId, bool wasPlayed,
            out bool passed, out string? suppression, string? existingSuppression = null)
        {
            passed = false; suppression = existingSuppression; CombatContext context = source;
            for (int index = 0; index < (source.Relics?.Count ?? 0); index++)
            {
                CombatRelicState owner = context.Relics![index]; if (!owner.EffectTypes.Contains(Effect)) continue;
                RelicResourceEffect? resource = owner.ResourceEffects?.SingleOrDefault(rule => rule.Type == Effect);
                if (resource == null) return new(source, "Missing original exhaust-suppression state.");
                string? error = Validate(owner, resource); if (error != null) return new(source, error);
                CardInstanceState? card = context.FindCard(cardId);
                if (card?.MaskDescriptor == null) return new(source, "Suppression requires captured card type and traits.");
                IReadOnlyList<string> traits = CardBranchMaskModel.Resolve(card).Traits; bool hit = false;
                if (wasPlayed && traits.Contains(Trait) && card.MaskDescriptor.Definition.CardType == resource.SuppressExhaust!.CardType &&
                    !traits.Contains("CardTraitInfusion") && !traits.Contains("CardTraitPersistent"))
                {
                    var random = context.BattleRng.Range(0, 100); context = context.WithBattleRng(random.State);
                    hit = random.Value < resource.SuppressExhaust.Chance; if (hit) suppression = Trait;
                }
                context = Replace(context, index, resource, resource.SuppressExhaust!.WithPassed(hit)); passed |= hit;
            }
            return new(context);
        }
        internal static bool Test(CombatContext context) => context.Relics?.Any(owner => owner.ResourceEffects?.Any(resource =>
            resource.Type == Effect && resource.SuppressExhaust?.TestPassed == true) == true) == true;
        // The body is unconditional; RelicManager gates it with Test and preview.
        internal static RelicFirstCardCostResult Apply(CombatContext source, out string? suppression, string? existingSuppression = null)
        {
            suppression = existingSuppression; CombatContext context = source;
            for (int index = 0; index < (source.Relics?.Count ?? 0); index++)
            {
                CombatRelicState owner = context.Relics![index]; if (!owner.EffectTypes.Contains(Effect)) continue;
                RelicResourceEffect? resource = owner.ResourceEffects?.SingleOrDefault(rule => rule.Type == Effect);
                if (resource == null) return new(source, "Missing original exhaust-suppression state.");
                string? error = Validate(owner, resource); if (error != null) return new(source, error);
                suppression = Trait; context = Replace(context, index, resource, resource.SuppressExhaust!.WithPassed(false));
            }
            return new(context);
        }
        internal static RelicFirstCardCostResult Discarded(CombatContext source, out string? suppression, string? existingSuppression = null, bool preview = false)
        {
            suppression = existingSuppression;
            return preview || !Test(source) ? new(source) : Apply(source, out suppression, existingSuppression);
        }
        private static CombatContext Replace(CombatContext context, int index, RelicResourceEffect resource, RelicSuppressExhaustRule rule)
        {
            CombatRelicState[] owners = context.Relics!.ToArray();
            owners[index] = owners[index].WithResourceEffects(owners[index].ResourceEffects!
                .Select(item => item.EffectIndex == resource.EffectIndex ? item.WithSuppressExhaust(rule) : item).ToArray());
            return context.WithRelics(owners);
        }
    }
}
