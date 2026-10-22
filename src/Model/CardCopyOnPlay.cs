using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardCopyOnPlayModel
    {
        public const string Trait = "CardTraitCopyOnPlay";
        public const string UpgradeId = "a75fc092-ca72-476b-b518-01e9ecb8d6a3";
        public static bool Present(CardInstanceState card) => card.MaskDescriptor != null &&
            (card.MaskDescriptor.BaseTraits.Concat(card.MaskDescriptor.TemporaryTraits).Any(trait => trait.RuntimeType == Trait) ||
             card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true) ||
             card.MaskDescriptor.PermanentReplacements.Any(rule => rule.OldName == Trait || rule.NewRuntimeType == Trait));

        public static string? Validate(CardInstanceState card)
        {
            if (!Present(card)) return null;
            var active = CardExhaustModel.RefreshTraits(card).CombinedTraits!.ToArray();
            var traits = active.Where(trait => trait.RuntimeType == Trait).ToArray();
            if (traits.Length == 0) return null;
            if (card.MaskDescriptor!.SourceTraits == null || card.MaskDescriptor.SourceTraits.Count != 0 ||
                card.MaskDescriptor.BaseTraits.Any(trait => trait.RuntimeType is not (Trait or "CardTraitExhaustState")) ||
                card.MaskDescriptor.TemporaryTraits.Count != 0 || card.MaskDescriptor.PermanentReplacements.Count != 0)
                return "Copy-on-play currently requires an original spell without authored traits or other installed trait state.";
            if (card.MaskDescriptor!.Definition.CardType != "Spell" || card.GraftedEquipmentCardId.HasValue ||
                !card.CanBeCopied.HasValue || traits.Length != 1 || traits[0].DeclaredName != Trait || traits[0].ParamInt != 1 ||
                traits[0].StackMode != 1 || traits[0].CopyOnPlayUpgrade is not CardUpgradeModifier upgrade ||
                upgrade.DataId != "513224dd-223f-48ed-bf0a-afd486aef98b" || upgrade.ExternalInteractions.Count > 0 ||
                upgrade.Lifecycle?.AddedTraits.Count != 1 || upgrade.Lifecycle.AddedTraits[0].RuntimeType != "CardTraitEphemeral" ||
                !upgrade.Lifecycle.RemovedRuntimeTypes.SequenceEqual(new[] { Trait }) ||
                !upgrade.Lifecycle.ReplacedAssets.SequenceEqual(new[] { "CreateSpellCopyWithPurge" }))
                return "Copy-on-play requires the original single-copy spell upgrade and captured purge/replacement definition.";
            if (CardExhaustModel.RefreshTraits(card).CombinedTraits!.Any(trait => trait.RuntimeType is
                "CardTraitEphemeral" or "CardTraitSelfPurge" or "CardTraitPersistent" or "CardTraitInfusion" or
                "CardTraitScalingAddCards" or "CardTraitScalingAddEnergy" or CardRetainModel.Trait or CardMoonUpgradeModel.Trait))
                return "Copy-on-play combined discard callbacks require native ordering calibration.";
            if (card.Temporary.Upgrades.Any(upgrade => upgrade.RemoveOnDiscard))
                return "Copy-on-play with discard-removing temporary upgrades requires native ordering calibration.";
            int consumeIndex = Array.FindIndex(active, trait => trait.RuntimeType == "CardTraitExhaustState");
            if (consumeIndex >= Array.FindIndex(active, trait => trait.RuntimeType == Trait))
                return "Copy-on-play requires Consume to precede the copy callback, as in the original calibrated upgrade order.";
            return null;
        }

        internal static CardGenerationResult Discarded(CombatContext source, CardInstanceState card)
        {
            string? error = Validate(card);
            if (error != null) return new CardGenerationResult(null, Array.Empty<CardToken>(), error);
            var trait = CardExhaustModel.RefreshTraits(card).CombinedTraits!.SingleOrDefault(item => item.RuntimeType == Trait);
            if (trait == null || card.CanBeCopied != true || source.Cards.Hand.Count >= source.MaxHandSize)
                return new CardGenerationResult(source, Array.Empty<CardToken>());
            var creation = new CardCreationRule(card.DataId, CardModifiers.Empty(), card.EffectCounters,
                card.ExternalInteractions, card.DamageScalingTraits, card.StatusScalingTraits, card.UnitUpgradeScalingTraits,
                card.CapacityScalingTraits, card.EquippedUnitId,
                card.MaskDescriptor!.WithBaseTraits(Array.Empty<CardTraitValue>()), card.ForgeCountScalingTraits,
                card.CanBeCopied, card.TrainHealScalingTraits);
            return CardGenerationModel.Apply(source, new CardGenerationRule("HandPile", 1, new[] { creation },
                copyModifiers: true, upgrade: trait.CopyOnPlayUpgrade, deterministicPoolSelection: true,
                requireCopyableSource: true, permanent: false), card.InstanceId);
        }
    }
}
