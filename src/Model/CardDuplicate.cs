using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardDuplicateModel
    {
        public static IReadOnlyList<int> EligibleCards(CombatContext context, CardActionEffect effect,
            int sourceCardId, out string? error)
        {
            error = null;
            var result = new List<int>();
            foreach (CardToken token in context.Cards.Hand)
            {
                if (token.InstanceId == sourceCardId) continue;
                CardInstanceState? card = context.FindCard(token.InstanceId);
                if (card?.MaskDescriptor?.Definition.IsChampionCard == null || card.DataId != token.DataId || !card.CanBeCopied.HasValue)
                { error = "Hand duplication requires captured card identity, champion classification and copyability."; return Array.Empty<int>(); }
                try
                {
                    if (card.MaskDescriptor.Definition.IsChampionCard.Value ||
                        effect.CardFilter != null && !CardUpgradeMaskModel.FilterCard(effect.CardFilter, CardBranchMaskModel.Resolve(card))) continue;
                    if (card.CanBeCopied.Value) result.Add(token.InstanceId);
                }
                catch (InvalidOperationException failure) { error = failure.Message; return Array.Empty<int>(); }
            }
            return Array.AsReadOnly(result.ToArray());
        }

        public static CardGenerationResult Apply(CombatContext source, CardActionEffect effect, int sourceCardId, int? selectedCardId)
        {
            IReadOnlyList<int> eligible = EligibleCards(source, effect, sourceCardId, out string? error);
            if (error != null) return Unsupported(error);
            if (!selectedCardId.HasValue || !eligible.Contains(selectedCardId.Value))
                return Unsupported("Hand duplication requires one eligible selected card.");
            CardInstanceState card = source.FindCard(selectedCardId.Value)!;
            if (card.MaskDescriptor!.Definition.HasStartingUpgrades != false)
                return Unsupported("Hand duplication of cards with authored starting upgrades requires a definition creation template.");
            if (effect.Upgrade?.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == "CardTraitEphemeral") == true &&
                CardBranchMaskModel.Resolve(card).Traits.Contains("CardTraitSelfPurge"))
                return Unsupported("Ephemeral copies of SelfPurge cards require native repeated-purge pile calibration.");
            CardMaskDescriptor creationMask = card.MaskDescriptor;
            if (creationMask.SourceTraits?.Count == 0 && creationMask.BaseTraits.Any(trait => trait.RuntimeType == CardRetainModel.Trait))
            {
                CardUpgradeModifier? retain = card.Permanent.Upgrades.Count == 1 ? card.Permanent.Upgrades[0] : null;
                if (creationMask.BaseTraits.Count != 1 || creationMask.TemporaryTraits.Count != 0 || creationMask.PermanentReplacements.Count != 0 ||
                    retain?.DataId != "f43e7cdb-fa8d-4760-88bf-c6f34f72d918" || retain.AssetKey != "TraitRetain" ||
                    retain.Lifecycle?.AddedTraits.Count != 1 || retain.Lifecycle.AddedTraits[0] is not
                        { RuntimeType: "CardTraitRetain", DeclaredName: "CardTraitRetain", ParamInt: 0, DefinitionParamInt: 0,
                            Removable: true, DefinitionRemovable: true, StackMode: 0, TemporaryReplacement: false, HoldoverPending: false } ||
                    retain.Lifecycle.RemovedRuntimeTypes.Count != 0 || retain.Lifecycle.TraitsModified.Count != 0 ||
                    retain.Lifecycle.ReplacedAssets.Count != 0 || retain.ExternalInteractions.Count != 0 ||
                    CardRetainModel.Validate(card) != null)
                    return Unsupported("Hand duplication of installed Retain requires its calibrated authored creation traits.");
                // AddNewCardCopy creates the authored card before applying the
                // optional Ephemeral upgrade and copying the parent's Retain.
                creationMask = creationMask.WithBaseTraits(Array.Empty<CardTraitValue>());
            }
            var creation = new CardCreationRule(card.DataId, CardModifiers.Empty(), card.EffectCounters,
                card.ExternalInteractions, card.DamageScalingTraits, card.StatusScalingTraits,
                card.UnitUpgradeScalingTraits, card.CapacityScalingTraits, card.EquippedUnitId,
                creationMask, card.ForgeCountScalingTraits, card.CanBeCopied, card.TrainHealScalingTraits);
            // Native ignores ParamInt (Mass Production supplies 3): one selected
            // card creates exactly one temporary copy, with no pool RNG draw.
            var rule = new CardGenerationRule("HandPile", 1, new[] { creation }, copyModifiers: true,
                upgrade: effect.Upgrade, deterministicPoolSelection: true, requireCopyableSource: true, upgradeIsPermanent: true, permanent: false,
                optionalUpgradeFilters: effect.UpgradeFilters);
            return CardGenerationModel.Apply(source, rule, card.InstanceId);
        }
        private static CardGenerationResult Unsupported(string error) => new(null, Array.Empty<CardToken>(), error);
    }
}
