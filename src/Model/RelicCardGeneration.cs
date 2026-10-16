using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class RelicCardGenerationModel
    {
        public const string AfterDraw = "RelicEffectAddBattleCardToHand";
        public const string StartOfBattle = "RelicEffectAddCardsStartOfBattle";
        private const string PartFactory = "623ce968-aae5-4b07-b0c8-071f315031b7";
        private const string GraftedCardStats = "Unmodeled unit card statistics";
        private const string GraftedEquipment = "Grafted equipment";
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
        internal static string? ValidateStartOfBattle(CombatRelicState owner, RelicResourceEffect effect)
        {
            var rule = effect.CardGeneration;
            if (owner.DataId != "623ce968-aae5-4b07-b0c8-071f315031b7" || owner.AssetKey != "PartFactory" ||
                owner.IsCovenant != false || owner.DisallowedInPlacementPhase != false || effect.EffectIndex != 0 ||
                effect.Conditions.Count != 0 || effect.RequiredMutatorId != null || rule == null ||
                rule.Destination != "HandPile" || rule.Count != 1 || rule.Pool.Count == 0 || rule.Permanent != false ||
                rule.Pool.Any(creation => !IsPartFactoryGraftedCard(creation)) ||
                rule.Upgrade != null || rule.UpgradeIsPermanent || rule.SkipDuplicateInHand || rule.RequireHandSpace ||
                rule.CopyModifiers || rule.IgnoreTemporaryModifiers || rule.DiscardCopyUpgrades.Count != 0 ||
                rule.DeterministicPoolSelection || rule.SelectedPoolIndex.HasValue || rule.ExternalInteractions.Count != 0)
                return "Missing or unsupported native start-of-battle PartFactory generation definition.";
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
        public static CardGenerationResult ApplyStartOfBattle(CombatContext source, string ownerId, int effectIndex, int selectedPoolIndex)
        {
            var owners = source.Relics?.Where(owner => owner.DataId == ownerId).ToArray();
            if (owners?.Length != 1) return Fail("Start-of-battle generation requires one captured original owner.");
            var owner = owners[0];
            var effects = owner.ResourceEffects?.Where(effect => effect.EffectIndex == effectIndex && effect.Type == StartOfBattle).ToArray();
            if (effects?.Length != 1) return Fail("Missing original start-of-battle generation effect index.");
            var effect = effects[0]; string? error = ValidateStartOfBattle(owner, effect);
            if (error != null) return Fail(error);
            return CardGenerationModel.ApplyPartFactory(source, effect.CardGeneration!.WithSelectedPoolIndex(selectedPoolIndex));
        }

        internal static bool IsPartFactoryGraftedCard(CardCreationRule creation) =>
            creation.ExternalInteractions.SequenceEqual(new[] { GraftedCardStats }) &&
            creation.StartingModifiers.ExternalInteractions.SequenceEqual(new[] { GraftedEquipment });

        internal static bool IsPartFactoryGraftedCard(CardInstanceState card) =>
            card.ExternalInteractions.SequenceEqual(new[] { GraftedCardStats }) &&
            card.Permanent.ExternalInteractions.SequenceEqual(new[] { GraftedEquipment }) &&
            card.Temporary.ExternalInteractions.Count == 0;

        internal static bool CanDeferUnplayedGraftedCard(CombatContext? context, CardInstanceState card,
            IEnumerable<CombatUnit> currentRoomUnits)
        {
            if (context?.Relics?.Any(owner => owner.DataId == PartFactory && owner.ResourceEffects?.Any(effect =>
                effect.Type == StartOfBattle && effect.CardGeneration?.Pool.Any(rule => rule.DataId == card.DataId) == true) == true) != true ||
                !IsPartFactoryGraftedCard(card) || currentRoomUnits.Any(unit => unit.SpawnerCardId == card.InstanceId) ||
                context.OtherPiles?.Any(pile => pile.Name == "Standby" && pile.Cards.Any(token => token.InstanceId == card.InstanceId)) == true)
                return false;
            return context.Cards.Hand.Concat(context.Cards.Draw).Concat(context.Cards.Discard)
                .Any(token => token.InstanceId == card.InstanceId) ||
                context.OtherPiles?.Any(pile => pile.Name != "Standby" && pile.Cards.Any(token => token.InstanceId == card.InstanceId)) == true;
        }
        private static CardGenerationResult Fail(string error) => new CardGenerationResult(null, Array.Empty<CardToken>(), error);
    }
}
