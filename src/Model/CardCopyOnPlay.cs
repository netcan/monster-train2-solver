using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardCopyOnPlayModel
    {
        public const string Trait = "CardTraitCopyOnPlay";
        public const string UpgradeId = "a75fc092-ca72-476b-b518-01e9ecb8d6a3";
        public const string UndyingSpellId = "7168ed36-de03-41db-a36f-79b54b121341";
        public const string PermafrostUpgradeId = "e0af4fe5-75c0-4017-ae5e-5a87099aa6c5";
        public static bool Present(CardInstanceState card) => card.MaskDescriptor != null &&
            (card.MaskDescriptor.BaseTraits.Concat(card.MaskDescriptor.TemporaryTraits).Any(trait => trait.RuntimeType == Trait) ||
             card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true) ||
             card.MaskDescriptor.PermanentReplacements.Any(rule => rule.OldName == Trait || rule.NewRuntimeType == Trait));

        private static bool SupportedSourceTraits(CardMaskDescriptor descriptor)
        {
            if (descriptor.SourceTraits == null) return false;
            if (descriptor.SourceTraits.Count == 0) return true;
            if (SupportedUndyingSource(descriptor)) return true;
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

        private static bool SupportedUndyingSource(CardMaskDescriptor descriptor)
        {
            if (descriptor.Definition.DataId != UndyingSpellId || !descriptor.Definition.XCost ||
                descriptor.SourceTraits?.Count != 2 || descriptor.BaseTraits.Count < 2) return false;
            for (int index = 0; index < 2; index++)
            {
                CardSourceTrait authored = descriptor.SourceTraits[index];
                CardTraitValue runtime = descriptor.BaseTraits[index];
                string type = index == 0 ? "CardTraitExhaustState" : "CardTraitScalingAddStatusEffect";
                if (authored.Type != type || authored.Int != 1 || authored.Int2 != 0 || authored.Int3 != 0 ||
                    authored.Bool || authored.Str != "" || authored.Float != 0 || authored.Scaling ||
                    authored.TrackedValue != "PlayedCost" || authored.EntryDuration != "ThisTurn" ||
                    authored.StatusQueryTeam != null || authored.StatusQueryStatuses != null || authored.HandCostTargetType != null ||
                    authored.DeploymentDrawFlag != null || authored.SpecificCardDataId != null || authored.MoonPhaseUpgrade != null ||
                    authored.KillTargetTeam != null || authored.KillTargetSubtype != null || authored.QueryCardType != null ||
                    (index == 0 ? authored.StatusEffectIds != null : authored.StatusEffectIds?.SequenceEqual(new[] { "undying" }) != true) ||
                    runtime.RuntimeType != type || runtime.DeclaredName != type || runtime.ParamInt != 1 || runtime.DefinitionParamInt != 1 ||
                    runtime.StackMode != 0 || !runtime.Removable || runtime.DefinitionRemovable != true || runtime.TemporaryReplacement)
                    return false;
            }
            return true;
        }

        internal static bool SupportedPermafrost(CardInstanceState card)
        {
            if (card.MaskDescriptor is not { } descriptor || !SupportedUndyingSource(descriptor)) return false;
            bool Ordinary(CardTraitValue trait) => trait.RuntimeType == "CardTraitPermafrost" &&
                trait.DeclaredName == trait.RuntimeType && trait.ParamInt == 0 && trait.DefinitionParamInt == 0 &&
                trait.StackMode == 0 && trait.Removable && trait.DefinitionRemovable == true && !trait.TemporaryReplacement;
            var installed = descriptor.BaseTraits.Where(trait => trait.RuntimeType == "CardTraitPermafrost").ToArray();
            var upgrades = card.Permanent.Upgrades.Where(upgrade => upgrade.DataId == PermafrostUpgradeId).ToArray();
            if (installed.Length != 1 || !Ordinary(installed[0]) || upgrades.Length != 1) return false;
            var upgrade = upgrades[0];
            return upgrade.AssetKey == "TraitPermafrost" && !upgrade.RemoveOnDiscard && !upgrade.Unique &&
                !upgrade.ExcludeFromClones && upgrade.UnhealedHealth == 0 && upgrade.DamageBuff == 0 &&
                !upgrade.RestrictSizeToRoomCapacity && !upgrade.MagicPowerTraitScalingOnly && upgrade.AbilityUpgrade == null &&
                (upgrade.TriggerUpgrades?.Count ?? 0) == 0 && upgrade.Statuses.Count == 0 &&
                !CardModifierModel.UpgradeInteractions(upgrade).Any() &&
                upgrade.Stats.Cost == -1 && upgrade.Stats.XCost == 1 &&
                new[] { "Damage", "Health", "Heal", "Size", "EquipmentLimit", "UpgradeSlotCount" }
                    .All(stat => upgrade.Stats.Value(stat) == 0) &&
                upgrade.Lifecycle is { } lifecycle && lifecycle.AddedTraits.Count == 1 && Ordinary(lifecycle.AddedTraits[0]) &&
                lifecycle.RemovedRuntimeTypes.Count == 0 && lifecycle.ReplacedAssets.Count == 0 &&
                lifecycle.Triggers.Count == 0 && !lifecycle.AvoidClobbering && lifecycle.TraitsModified.Count == 0;
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
                card.MaskDescriptor.BaseTraits.Any(trait => trait.RuntimeType is not (Trait or "CardTraitExhaustState") &&
                    !(SupportedUndyingSource(card.MaskDescriptor) && trait.RuntimeType == "CardTraitScalingAddStatusEffect") &&
                    !(trait.RuntimeType == "CardTraitPermafrost" && SupportedPermafrost(card))) ||
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
                card.MaskDescriptor!.WithBaseTraits(card.MaskDescriptor.BaseTraits.Take(card.MaskDescriptor.SourceTraits!.Count).ToArray()), card.ForgeCountScalingTraits,
                card.CanBeCopied, card.TrainHealScalingTraits);
            return CardGenerationModel.Apply(source, new CardGenerationRule("HandPile", 1, new[] { creation },
                copyModifiers: true, upgrade: trait.CopyOnPlayUpgrade, deterministicPoolSelection: true,
                requireCopyableSource: true, permanent: false), card.InstanceId);
        }
    }
}
