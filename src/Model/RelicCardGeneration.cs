using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class RelicCardGenerationModel
    {
        public const string AfterDraw = "RelicEffectAddBattleCardToHand";
        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect)
        {
            var rule = effect.CardGeneration;
            if (!owner.DisallowedInPlacementPhase.HasValue || rule == null || effect.Conditions.Count != 0 ||
                effect.RequiredMutatorId != null || rule.Destination != "HandPile" || rule.Count != 1 || rule.CopyModifiers ||
                rule.SkipDuplicateInHand || rule.RequireHandSpace || rule.DeterministicPoolSelection || rule.UpgradeIsPermanent ||
                rule.RequireCopyableSource || rule.IgnoreTemporaryModifiers || rule.DiscardCopyUpgrades.Count != 0)
                return "Missing or unsupported after-draw relic generation definition.";
            return null;
        }
        public static CardGenerationResult Apply(CombatContext source, bool placement = false, bool preview = false)
        {
            var context = source; var added = new System.Collections.Generic.List<CardToken>();
            foreach (var owner in source.Relics ?? Array.Empty<CombatRelicState>())
            foreach (var effect in owner.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
            {
                if (effect.Type != AfterDraw) continue;
                var result = ApplyOne(context, owner.DataId, effect.EffectIndex, placement, preview);
                if (!result.Supported) return result;
                context = result.Context!; added.AddRange(result.AddedCards);
            }
            return new CardGenerationResult(context, added);
        }
        public static CardGenerationResult ApplyOne(CombatContext source, string ownerId, int effectIndex,
            bool placement = false, bool preview = false)
        {
            var owners = source.Relics?.Where(owner => owner.DataId == ownerId).ToArray();
            if (owners?.Length != 1) return Fail("After-draw generation requires one captured original owner.");
            var owner = owners[0];
            var effects = owner.ResourceEffects?.Where(effect => effect.EffectIndex == effectIndex && effect.Type == AfterDraw).ToArray();
            if (effects?.Length != 1) return Fail("Missing after-draw relic effect index.");
            var effect = effects[0]; string? error = Validate(owner, effect);
            if (error != null) return Fail(error);
            if (placement && owner.DisallowedInPlacementPhase == true || preview && !effect.CanApplyInPreviewMode)
                return new CardGenerationResult(source, Array.Empty<CardToken>());
            // TestEffect draws before AddNewCard tests capacity; full hands still
            // consume the native pool selection. Generation applies temporary upgrades.
            return CardGenerationModel.Apply(source, effect.CardGeneration!);
        }
        private static CardGenerationResult Fail(string error) => new CardGenerationResult(null, Array.Empty<CardToken>(), error);
    }
}
