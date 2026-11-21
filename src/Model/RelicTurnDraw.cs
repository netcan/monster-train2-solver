using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class RelicTurnDrawModel
    {
        public const string FullHealth = "RelicEffectAdditionalDrawOnTowerFullHealth";
        public const string Opening = "RelicEffectDrawToMaxStartOfCombat";

        // Both native Test methods update the registered mutable value and return
        // false. Updating the value must not depend on executing an Apply body.
        public static RelicResourceResult BeforeDraw(CombatContext source, int pyreHealth,
            bool placement = false, bool preview = false, TrainCombatState? train = null, int? onlyOwner = null, int? onlyEffect = null)
        {
            if (source.Relics == null) return new RelicResourceResult(null, 0, "Missing turn-draw relic state.");
            var maximum = source.Relics.Any(owner => owner.EffectTypes.Contains(FullHealth))
                ? RelicResourceModel.GetPyreMaxHealth(source) : new RelicResourceResult(source, 0);
            if (!maximum.Supported) return maximum;
            var context = source;
            for (int i = 0; i < source.Relics.Count; i++)
            for (int j = 0; j < (context.Relics![i].ResourceEffects?.Count ?? 0); j++)
            {
                var owner = context.Relics![i]; var effect = owner.ResourceEffects![j];
                if (onlyOwner.HasValue && i != onlyOwner || onlyEffect.HasValue && effect.EffectIndex != onlyEffect) continue;
                if (effect.Type == RelicRelentlessDrawModel.Effect)
                {
                    if (train == null) return new RelicResourceResult(null, 0, "Relentless draw requires the complete train.");
                    var refreshed = RelicRelentlessDrawModel.RefreshOne(CardSpellModel.WithContext(train, context), i, effect.EffectIndex, placement, preview);
                    if (!refreshed.Supported) return refreshed;
                    context = refreshed.Context!;
                    continue;
                }
                if (effect.Type != FullHealth || placement && owner.DisallowedInPlacementPhase == true || preview && !effect.CanApplyInPreviewMode) continue;
                if (effect.Conditions.Count > 0) return new RelicResourceResult(null, 0, "Conditional full-health draw requires its native Test condition.");
                float value = pyreHealth == maximum.Value ? effect.ParamInt : 0;
                if (effect.DrawValue != value)
                {
                    var effects = owner.ResourceEffects.ToArray(); effects[j] = effect.WithDrawValue(value);
                    var owners = context.Relics.ToArray(); owners[i] = owner.WithResourceEffects(effects); context = context.WithRelics(owners);
                }
            }
            return new RelicResourceResult(context, 0);
        }

        public static RelicResourceResult PostOpening(CombatContext source, bool placement = false,
            bool preview = false, BattlePlayRules? definitions = null)
        {
            var context = source; int total = 0;
            foreach (var owner in source.Relics ?? Array.Empty<CombatRelicState>())
            foreach (var effect in owner.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
            {
                if (effect.Type != Opening) continue;
                if (!owner.DisallowedInPlacementPhase.HasValue || effect.Conditions.Count != 0)
                    return new RelicResourceResult(null, 0, "Missing or conditional original post-opening draw definition.");
                if (owner.DisallowedInPlacementPhase == placement || preview && !effect.CanApplyInPreviewMode) continue;
                var starting = RelicResourceModel.GetDrawCount(context);
                if (!starting.Supported) return starting;
                int count = context.MaxHandSize - starting.Value;
                if (count <= 0) continue;
                var drawn = NextDrawUpgradeModel.DrawCards(context, count, 0, definitions);
                if (!drawn.Supported) return new RelicResourceResult(null, 0, drawn.UnsupportedReason);
                context = drawn.Context!; total += drawn.DrawnCardIds.Count;
            }
            return new RelicResourceResult(context, total);
        }
    }
}
