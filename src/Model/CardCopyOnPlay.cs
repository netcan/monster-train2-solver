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

        private static bool SupportedSourceTraits(CardMaskDescriptor descriptor)
        {
            if (descriptor.SourceTraits == null) return false;
            if (descriptor.SourceTraits.Count == 0) return true;
            if (descriptor.SourceTraits.Count != 1 || descriptor.BaseTraits.Count == 0) return false;
            CardSourceTrait authored = descriptor.SourceTraits[0];
            CardTraitValue runtime = descriptor.BaseTraits[0];
            return authored.Type == "CardTraitExhaustState" && authored.Int == 0 && authored.Int2 == 0 && authored.Int3 == 0 &&
                !authored.Bool && authored.Str == "" && authored.Float == 0 && !authored.Scaling &&
                authored.TrackedValue == "SubtypeInDeck" && authored.EntryDuration == "ThisTurn" &&
                authored.StatusQueryTeam == null && authored.StatusQueryStatuses == null && authored.HandCostTargetType == null &&
                authored.DeploymentDrawFlag == null && authored.SpecificCardDataId == null && authored.StatusEffectIds == null &&
                authored.MoonPhaseUpgrade == null && authored.KillTargetTeam == null && authored.KillTargetSubtype == null && authored.QueryCardType == null &&
                runtime.RuntimeType == authored.Type && runtime.DeclaredName == authored.Type && runtime.DefinitionParamInt == 0 &&
                runtime.ParamInt == 0 && runtime.StackMode == 0 && runtime.Removable && runtime.DefinitionRemovable == true && !runtime.TemporaryReplacement;
        }

        private static bool BoxSelfCopy(CardInstanceState card) => card.DataId == "f0ff6411-5c58-4b69-b025-0f6e4a52fca7" &&
            card.Temporary.Upgrades.Count(upgrade => upgrade.DataId == "80a5edf4-6dc8-41a7-ab4e-9d0e914e0848" &&
                upgrade.AssetKey == "AddEphemeral" && !upgrade.RemoveOnDiscard && upgrade.ExternalInteractions.Count == 0 &&
                upgrade.Lifecycle?.AddedTraits.Count == 1 && upgrade.Lifecycle.AddedTraits[0].RuntimeType == "CardTraitEphemeral" &&
                upgrade.Lifecycle.AddedTraits[0].DeclaredName == "CardTraitEphemeral" && upgrade.Lifecycle.AddedTraits[0].ParamInt == 0 &&
                upgrade.Lifecycle.AddedTraits[0].DefinitionParamInt == 0 && upgrade.Lifecycle.AddedTraits[0].StackMode == 0 &&
                !upgrade.Lifecycle.AddedTraits[0].Removable && upgrade.Lifecycle.RemovedRuntimeTypes.Count == 0 &&
                upgrade.Lifecycle.ReplacedAssets.Count == 0) == 1;

        public static bool SupportsTerminalPurge(CardInstanceState card)
        {
            if (card.MaskDescriptor is not CardMaskDescriptor descriptor || descriptor.Definition.CardType != "Spell" ||
                !SupportedSourceTraits(descriptor) || descriptor.BaseTraits.Count > 1 ||
                descriptor.BaseTraits.Any(trait => trait.RuntimeType != "CardTraitExhaustState" || trait.DeclaredName != "CardTraitExhaustState" ||
                    trait.ParamInt != 0 || trait.StackMode != 0) ||
                descriptor.TemporaryTraits.Count != 0 || descriptor.PermanentReplacements.Count != 0 ||
                card.GraftedEquipmentCardId.HasValue || Present(card)) return false;
            var upgrades = card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).ToArray();
            var purge = card.Temporary.Upgrades.Where(upgrade => upgrade.DataId == "513224dd-223f-48ed-bf0a-afd486aef98b" &&
                upgrade.AssetKey == "CopyOnPlayTraitUpgrade").ToArray();
            if (purge.Length != 1 || upgrades.Any(upgrade => upgrade.RemoveOnDiscard || upgrade.ExternalInteractions.Count != 0) ||
                purge[0].Lifecycle is not { } lifecycle || lifecycle.AddedTraits.Count != 1 ||
                lifecycle.AddedTraits[0].RuntimeType != "CardTraitEphemeral" || lifecycle.AddedTraits[0].DeclaredName != "CardTraitEphemeral" ||
                lifecycle.AddedTraits[0].ParamInt != 0 || lifecycle.AddedTraits[0].StackMode != 0 ||
                !lifecycle.RemovedRuntimeTypes.SequenceEqual(new[] { Trait }) ||
                !lifecycle.ReplacedAssets.SequenceEqual(new[] { "CreateSpellCopyWithPurge" })) return false;
            var traits = CardExhaustModel.RefreshTraits(card).CombinedTraits!.ToArray();
            return traits.Count(trait => trait.RuntimeType == "CardTraitEphemeral") == 1 &&
                traits.Count(trait => trait.RuntimeType == "CardTraitExhaustState") <= 1 &&
                traits.All(trait => trait.RuntimeType is "CardTraitEphemeral" or "CardTraitExhaustState") &&
                Array.FindIndex(traits, trait => trait.RuntimeType == "CardTraitExhaustState") <
                    Array.FindIndex(traits, trait => trait.RuntimeType == "CardTraitEphemeral");
        }

        public static string? Validate(CardInstanceState card)
        {
            if (!Present(card)) return null;
            var active = CardExhaustModel.RefreshTraits(card).CombinedTraits!.ToArray();
            var traits = active.Where(trait => trait.RuntimeType == Trait).ToArray();
            if (traits.Length == 0) return null;
            if (!SupportedSourceTraits(card.MaskDescriptor!) ||
                card.MaskDescriptor.BaseTraits.Any(trait => trait.RuntimeType is not (Trait or "CardTraitExhaustState")) ||
                card.MaskDescriptor.TemporaryTraits.Count > 1 || card.MaskDescriptor.TemporaryTraits.Any(trait =>
                    trait.RuntimeType != "CardTraitFreeze" || trait.DeclaredName != "CardTraitFreeze" || trait.ParamInt != 0 ||
                    trait.DefinitionParamInt != 0 || trait.StackMode != 0 || !trait.Removable || trait.DefinitionRemovable != true ||
                    trait.TemporaryReplacement) || card.MaskDescriptor.PermanentReplacements.Count != 0)
                return "Copy-on-play requires no authored traits or one ordinary authored Consume, optionally with one ordinary temporary Freeze.";
            if (card.MaskDescriptor!.Definition.CardType != "Spell" || card.GraftedEquipmentCardId.HasValue ||
                !card.CanBeCopied.HasValue || traits.Length != 1 || traits[0].DeclaredName != Trait || traits[0].ParamInt != 1 ||
                traits[0].StackMode != 1 || traits[0].CopyOnPlayUpgrade is not CardUpgradeModifier upgrade ||
                upgrade.DataId != "513224dd-223f-48ed-bf0a-afd486aef98b" || upgrade.ExternalInteractions.Count > 0 ||
                upgrade.Lifecycle?.AddedTraits.Count != 1 || upgrade.Lifecycle.AddedTraits[0].RuntimeType != "CardTraitEphemeral" ||
                !upgrade.Lifecycle.RemovedRuntimeTypes.SequenceEqual(new[] { Trait }) ||
                !upgrade.Lifecycle.ReplacedAssets.SequenceEqual(new[] { "CreateSpellCopyWithPurge" }))
                return "Copy-on-play requires the original single-copy spell upgrade and captured purge/replacement definition.";
            if (CardExhaustModel.RefreshTraits(card).CombinedTraits!.Any(trait => trait.RuntimeType == "CardTraitEphemeral" && !BoxSelfCopy(card) || trait.RuntimeType is
                "CardTraitSelfPurge" or "CardTraitPersistent" or "CardTraitInfusion" or
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
            if (!BoxSelfCopy(card) && CardExhaustModel.RefreshTraits(card).CombinedTraits!.Any(trait => trait.RuntimeType == "CardTraitEphemeral"))
                return new CardGenerationResult(null, Array.Empty<CardToken>(), "Copy-on-play combined discard callbacks require native ordering calibration.");
            var creation = new CardCreationRule(card.DataId, CardModifiers.Empty(), card.EffectCounters,
                card.ExternalInteractions, card.DamageScalingTraits, card.StatusScalingTraits, card.UnitUpgradeScalingTraits,
                card.CapacityScalingTraits, card.EquippedUnitId,
                // AddNewCardCopy starts from CardData's authored traits. Traits
                // installed by copied upgrades are rebuilt by the add pipeline.
                card.MaskDescriptor!.WithBaseTraits(card.MaskDescriptor.SourceTraits!.Count == 0 ? Array.Empty<CardTraitValue>() :
                    new[] { card.MaskDescriptor.BaseTraits[0] }), card.ForgeCountScalingTraits,
                card.CanBeCopied, card.TrainHealScalingTraits);
            return CardGenerationModel.Apply(source, new CardGenerationRule("HandPile", 1, new[] { creation },
                copyModifiers: true, upgrade: trait.CopyOnPlayUpgrade, deterministicPoolSelection: true,
                requireCopyableSource: true, permanent: false), card.InstanceId);
        }
    }
}
