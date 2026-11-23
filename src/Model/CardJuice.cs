using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class CardJuiceModel
    {
        internal const string Trait = "CardTraitJuice";
        internal const string Upgrade = "0fc9f6e7-df71-4045-9867-67453769adf8";
        internal const string TripleTrait = "CardTraitJuice3";
        internal const string TripleUpgrade = "2f066286-dbef-449b-aa5f-b8acdcdce7aa";
        internal const string QuadrupleTrait = "CardTraitJuice4";
        internal static bool IsTrait(string type) => type is Trait or TripleTrait or QuadrupleTrait;
        internal static int Multiplier(string type) => type switch
        { Trait => 2, TripleTrait => 3, QuadrupleTrait => 4, _ => throw new ArgumentException("Unknown Juice trait.", nameof(type)) };
        internal static int StatusBonus(int stacks, int multiplier) => unchecked(Math.Max(stacks, 0) * (multiplier - 1));
        internal static int UpgradeStacks(int stacks, int multiplier) => Math.Max(unchecked(stacks * multiplier), 0);
        private static bool Juice(string type) => IsTrait(type);
        internal static bool Present(CardInstanceState card) => card.MaskDescriptor != null &&
            (card.MaskDescriptor.BaseTraits.Concat(card.MaskDescriptor.TemporaryTraits).Any(trait => Juice(trait.RuntimeType)) ||
                card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade =>
                    upgrade.Lifecycle?.AddedTraits.Any(trait => Juice(trait.RuntimeType)) == true)) ||
            card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.DataId == TripleUpgrade);
        internal static bool KnownTripleUpgrade(CardUpgradeModifier upgrade) => KnownJuiceUpgrade(upgrade, true);
        // Recognize a fully represented mechanism, independently of content IDs.
        // Cast actions keep their ordinary dispatcher validation. Parameter-
        // upgrade cast effects and other ordered scaling remain guarded below.
        internal static bool SimpleUpgrade(CardUpgradeModifier upgrade) =>
            upgrade.Stats.Damage == 0 && upgrade.Stats.Health == 0 && upgrade.Stats.Cost == 0 &&
            upgrade.Stats.Heal == 0 && upgrade.Stats.Size == 0 && upgrade.Stats.XCost == 0 &&
            upgrade.Stats.EquipmentLimit == 0 && upgrade.Stats.UpgradeSlotCount == 0 &&
            upgrade.Statuses.Count == 0 && upgrade.UnhealedHealth == 0 && upgrade.DamageBuff == 0 &&
            upgrade.AbilityUpgrade == null && upgrade.TriggerUpgrades?.Count is not > 0 &&
            upgrade.SelfAttackRules?.Count is not > 0 && !upgrade.Unique && !upgrade.RemoveOnDiscard &&
            !upgrade.RestrictSizeToRoomCapacity && !upgrade.MagicPowerTraitScalingOnly &&
            upgrade.EquipmentSourceCardId == null && upgrade.EquipmentSourceUpgradeIndex == null &&
            upgrade.CloneDamageBase == null && upgrade.CloneHealBase == null &&
            upgrade.ExternalInteractions.All(value => value == "Upgrade traits") &&
            upgrade.Lifecycle is { AddedTraits.Count: 1, RemovedRuntimeTypes.Count: 0,
                ReplacedAssets.Count: 0 } lifecycle &&
            IsTrait(lifecycle.AddedTraits[0].RuntimeType) &&
            lifecycle.AddedTraits[0].DeclaredName == lifecycle.AddedTraits[0].RuntimeType &&
            (lifecycle.AvoidClobbering
                ? lifecycle.TraitsModified.Count == 1 && lifecycle.TraitsModified[0] == lifecycle.AddedTraits[0].DeclaredName
                : lifecycle.TraitsModified.Count == 0) &&
            Ordinary(lifecycle.AddedTraits[0], lifecycle.AddedTraits[0].DefinitionParamInt) &&
            lifecycle.AddedTraits[0].ParameterUpgradeCastEffects.Count == 0 &&
            lifecycle.Triggers.All(trigger => trigger.Type == "OnCast" && trigger.ExternalInteractions.Count == 0 &&
                trigger.Effects.Count == trigger.Actions.Count &&
                trigger.Effects.All(effect => effect == "CardEffectAddStatusEffect") &&
                trigger.Actions.All(action => action.Type == "AddStatus"));
        private static bool SimpleStatusOwner(CardInstanceState card, CardTraitCompositionState traits)
        {
            var descriptor = card.MaskDescriptor!;
            bool KnownEffect(string effect) => effect is "CardEffectAddStatusEffect" or
                "CardEffectAddTempCardUpgradeToNextDrawnCard" or "CardEffectDraw" or "CardEffectDrawType";
            var mainEffects = descriptor.Definition.MainEffectNames;
            // The native unit-upgrade callback does not change unit stats for
            // Juice. The subsequent status effect uses the ordinary multiplier.
            bool upgradeThenStatus = mainEffects.Count == 2 &&
                mainEffects[0] == "CardEffectAddTempCardUpgradeToUnits" &&
                mainEffects[1] == "CardEffectAddStatusEffect";
            bool orderedStatus = OrderedCorruptionStatus(card, traits);
            if (descriptor.Definition.DataId != card.DataId || descriptor.Definition.CardType != "Spell" ||
                descriptor.Definition.XCost || descriptor.Definition.IsSpawner || descriptor.Definition.HasSpawnData ||
                descriptor.Definition.AuthoredGraft || descriptor.PermanentGraft || descriptor.GraftedEquipment != null ||
                descriptor.AuthoredCastEffects.Any(effect => !KnownEffect(effect)) ||
                descriptor.PermanentReplacements.Count != 0 ||
                (!orderedStatus && descriptor.SourceTraits is not { Count: 0 }) || descriptor.KillTriggers?.Count is > 0 ||
                descriptor.SpareTriggers?.Count is > 0 || mainEffects.Count == 0 ||
                !upgradeThenStatus && mainEffects.Any(effect => !KnownEffect(effect)) ||
                card.Permanent.Upgrades.Count != 1 || !SimpleUpgrade(card.Permanent.Upgrades[0]) ||
                card.Temporary.Upgrades.Any(upgrade => !SimpleStatusPayload(upgrade)) ||
                (!orderedStatus && card.StatusScalingTraits?.Count is > 0) ||
                card.DamageScalingTraits?.Count is > 0 || card.UnitUpgradeScalingTraits?.Count is > 0 ||
                card.TrainHealScalingTraits?.Count is > 0 || card.CapacityScalingTraits?.Count is > 0 ||
                card.ForgeCountScalingTraits?.Count is > 0) return false;
            var juice = traits.CombinedTraits!.Where(trait => IsTrait(trait.RuntimeType)).ToArray();
            var added = card.Permanent.Upgrades[0].Lifecycle!.AddedTraits[0];
            return juice.Length == 1 && juice[0].RuntimeType == added.RuntimeType &&
                juice[0].DeclaredName == added.DeclaredName && Ordinary(juice[0], added.DefinitionParamInt) &&
                (orderedStatus || traits.CombinedTraits.All(trait => IsTrait(trait.RuntimeType) ||
                    trait.RuntimeType == "CardTraitFreeze" && trait.DeclaredName == trait.RuntimeType && Ordinary(trait, 0)));
        }

        private static bool OrderedCorruptionStatus(CardInstanceState card, CardTraitCompositionState traits)
        {
            const string scalingTrait = "CardTraitScalingAddStatusEffect";
            const string exhaustTrait = "CardTraitExhaustState";
            var descriptor = card.MaskDescriptor!;
            if (descriptor.Definition.MainEffectNames.Count != 1 || descriptor.Definition.MainEffectNames[0] != "CardEffectAddStatusEffect" ||
                descriptor.AuthoredCastEffects.Any(effect => effect != "CardEffectAddStatusEffect") ||
                descriptor.SourceTraits is not { Count: 2 } source || source[1].Type != exhaustTrait ||
                source[0] is not { Type: scalingTrait, Int2: 0, Bool: false, Scaling: true,
                    TrackedValue: "CorruptionInTargetRoom", EntryDuration: "ThisTurn", StatusEffectIds.Count: 1 } ||
                card.StatusScalingTraits is not { Count: 1 } scaling ||
                scaling[0] is not { Query: { Type: "CorruptionInTargetRoom", Duration: "ThisTurn", VariableCost: false },
                    Filter: 0, OnlyWhenSourceZero: false } ||
                scaling[0].StacksPerStat != source[0].Int || !scaling[0].StatusIds.SequenceEqual(source[0].StatusEffectIds!) ||
                !scaling[0].StatusIds.All(RoomCombatModel.KnowsStatus) ||
                traits.CombinedTraits is not { Count: 3 or 4 } combined)
                return false;
            // StatusScalingModel applies the captured scaling before Juice.
            // Only admit this composition when the native trait order agrees.
            return Match(combined[0], scalingTrait, source[0].Int) && Match(combined[1], exhaustTrait, 0) &&
                Match(combined[2], QuadrupleTrait, 0) &&
                (combined.Count == 3 || Match(combined[3], "CardTraitFreeze", 0));

            bool Match(CardTraitValue trait, string type, int parameter) =>
                trait.RuntimeType == type && trait.DeclaredName == type && Ordinary(trait, parameter) &&
                trait.ParameterUpgradeCastEffects.Count == 0;
        }
        private static bool SimpleStatusPayload(CardUpgradeModifier upgrade) =>
            upgrade.Stats.Damage == 0 && upgrade.Stats.Health == 0 && upgrade.Stats.Cost == 0 && upgrade.Stats.Heal == 0 &&
            upgrade.Stats.Size == 0 && upgrade.Stats.XCost == 0 && upgrade.Stats.EquipmentLimit == 0 && upgrade.Stats.UpgradeSlotCount == 0 &&
            upgrade.UnhealedHealth == 0 && upgrade.DamageBuff == 0 && upgrade.ExternalInteractions.Count == 0 &&
            upgrade.AbilityUpgrade == null && upgrade.TriggerUpgrades?.Count is not > 0 && upgrade.SelfAttackRules?.Count is not > 0 &&
            upgrade.Lifecycle is { AddedTraits.Count: 0, RemovedRuntimeTypes.Count: 0, TraitsModified.Count: 0,
                ReplacedAssets.Count: 0, Triggers.Count: 0, AvoidClobbering: false } &&
            upgrade.Statuses.All(status => RoomCombatModel.KnowsStatus(status.Id));
        private static bool KnownJuiceUpgrade(CardUpgradeModifier upgrade, bool triple) => upgrade.DataId == (triple ? TripleUpgrade : Upgrade) &&
            upgrade.AssetKey == (triple ? "TraitJuice3" : "TraitJuice") && upgrade.Stats.Damage == 0 && upgrade.Stats.Health == 0 &&
            upgrade.Stats.Cost == 0 && upgrade.Stats.Heal == 0 && upgrade.Stats.Size == 0 && upgrade.Stats.XCost == 0 &&
            upgrade.Stats.EquipmentLimit == 0 && upgrade.Stats.UpgradeSlotCount == 0 && upgrade.Statuses.Count == 0 &&
            upgrade.UnhealedHealth == 0 && upgrade.DamageBuff == 0 && upgrade.AbilityUpgrade == null &&
            upgrade.TriggerUpgrades?.Count > 0 != true && upgrade.Lifecycle is { AddedTraits.Count: 1, RemovedRuntimeTypes.Count: 0, Triggers.Count: 0 } lifecycle &&
            lifecycle.AddedTraits[0].RuntimeType == (triple ? TripleTrait : Trait) &&
            lifecycle.AddedTraits[0].DeclaredName == lifecycle.AddedTraits[0].RuntimeType &&
            Ordinary(lifecycle.AddedTraits[0], triple ? 3 : 2);
        internal static bool KnownTitanCurse(CardUpgradeModifier upgrade) =>
            upgrade.DataId == "f37fb903-28d6-4d49-b50e-9226f4854816" && upgrade.AssetKey == "TitanCurse_Upgrade" &&
            upgrade.Stats.Cost == 1 && upgrade.Stats.Damage == 0 && upgrade.Stats.Health == 0 && upgrade.Stats.Heal == 0 &&
            upgrade.Stats.Size == 0 && upgrade.Stats.XCost == 0 && upgrade.Stats.EquipmentLimit == 0 && upgrade.Stats.UpgradeSlotCount == 0 &&
            upgrade.Statuses.Count == 0 && upgrade.UnhealedHealth == 0 && upgrade.DamageBuff == 0 && upgrade.ExternalInteractions.Count == 0 &&
            upgrade.TriggerUpgrades?.Count > 0 != true && upgrade.AbilityUpgrade == null &&
            upgrade.Lifecycle is { AddedTraits.Count: 0, RemovedRuntimeTypes.Count: 0, Triggers.Count: 0 };
        private static bool KnownInherentXUpgrade(CardUpgradeModifier upgrade) =>
            upgrade.DataId == "62ea77cc-4f97-476f-9ccc-a23e28bc788d" && upgrade.AssetKey == "TraitInherentSpellReduceCost" &&
            upgrade.Stats.Cost == -1 && upgrade.Stats.XCost == 1 && upgrade.Stats.Damage == 0 && upgrade.Stats.Health == 0 &&
            upgrade.Stats.Heal == 0 && upgrade.Stats.Size == 0 && upgrade.Stats.EquipmentLimit == 0 && upgrade.Stats.UpgradeSlotCount == 0 &&
            upgrade.Statuses.Count == 0 && upgrade.ExternalInteractions.Count == 0 && upgrade.UnhealedHealth == 0 && upgrade.DamageBuff == 0 &&
            upgrade.AbilityUpgrade == null && upgrade.TriggerUpgrades?.Count > 0 != true &&
            upgrade.Lifecycle is { AddedTraits.Count: 1, RemovedRuntimeTypes.Count: 0, Triggers.Count: 0 } lifecycle &&
            lifecycle.AddedTraits[0].RuntimeType == "CardTraitIntrinsicState" &&
            lifecycle.AddedTraits[0].DeclaredName == "CardTraitIntrinsicState" && Ordinary(lifecycle.AddedTraits[0], 0);
        private static bool CleansePower(CardStatModifier stats) => stats.Damage == 20 && stats.Heal == 20 &&
            stats.Health == 0 && stats.Cost == 0 && stats.Size == 0 && stats.XCost == 0 &&
            stats.EquipmentLimit == 0 && stats.UpgradeSlotCount == 0;
        private static bool KnownCleanseConsume(CardUpgradeModifier upgrade) =>
            upgrade.DataId == "83e2c6c5-8ebf-4160-8818-fdae82d2abfe" && upgrade.AssetKey == "ChaosScepter" &&
            CleansePower(upgrade.Stats) && upgrade.CloneDamageBase == 20 && upgrade.CloneHealBase == 20 &&
            upgrade.Statuses.Count == 0 && upgrade.UnhealedHealth == 0 && upgrade.DamageBuff == 0 &&
            !upgrade.RemoveOnDiscard && !upgrade.Unique && !upgrade.ExcludeFromClones &&
            upgrade.AbilityUpgrade == null && upgrade.TriggerUpgrades?.Count is not > 0 && upgrade.ExternalInteractions.Count == 0 &&
            upgrade.Lifecycle is { AssetKey: "ChaosScepter", AddedTraits.Count: 1, RemovedRuntimeTypes.Count: 0,
                TraitsModified.Count: 0, ReplacedAssets.Count: 0, Triggers.Count: 0, OriginalDamage: 20, OriginalHeal: 20 } lifecycle &&
            lifecycle.Values.DataId == upgrade.DataId && CleansePower(lifecycle.Values.Stats) && lifecycle.Values.Statuses.Count == 0 &&
            lifecycle.AddedTraits[0].RuntimeType == "CardTraitExhaustState" &&
            lifecycle.AddedTraits[0].DeclaredName == "CardTraitExhaustState" && Ordinary(lifecycle.AddedTraits[0], 0);
        internal static bool KnownTripleCleanseCopySource(CardInstanceState card) =>
            card.DataId == "d4366b7c-588c-49aa-b2c6-2aeb30b0e01b" && card.CanBeCopied == true &&
            card.MaskDescriptor is { SourceTraits.Count: 0, BaseTraits.Count: 2, TemporaryTraits.Count: 0,
                PermanentReplacements.Count: 0 } &&
            card.Permanent.Upgrades.Count == 2 && KnownTripleUpgrade(card.Permanent.Upgrades[0]) &&
            KnownCleanseConsume(card.Permanent.Upgrades[1]) && card.Temporary.Upgrades.Count <= 1 &&
            card.Temporary.Upgrades.All(LazarusJuiceSpikeModel.KnownConduit) && Validate(card) == null;
        private static bool KnownTripleCleanseCopy(CardInstanceState card, IReadOnlyList<CardTraitValue> traits) =>
            card.DataId == "d4366b7c-588c-49aa-b2c6-2aeb30b0e01b" && card.PlayCount == 0 && card.CanBeCopied == true &&
            card.Permanent.Upgrades.Count == 2 && card.MaskDescriptor is { BaseTraits.Count: 2, TemporaryTraits.Count: 0,
                PermanentReplacements.Count: 0, SourceTraits.Count: 0 } &&
            traits.Count == 3 && traits[2].RuntimeType == "CardTraitEphemeral" && card.Temporary.Upgrades.Count is 1 or 2 &&
            RelicCopyCardOnCardPlayModel.IsFirstSpellEphemeralUpgrade(card.Temporary.Upgrades[0]) &&
            card.Temporary.Upgrades[0].Lifecycle is
            {
                AddedTraits: [{ RuntimeType: "CardTraitEphemeral", DeclaredName: "CardTraitEphemeral", ParamInt: 0,
                    DefinitionParamInt: 0, StackMode: 0, Removable: false, DefinitionRemovable: false,
                    TemporaryReplacement: false, HasData: true }],
                TraitsModified.Count: 0, ReplacedAssets.Count: 0, AvoidClobbering: false,
            } &&
            card.Temporary.Upgrades.Skip(1).All(LazarusJuiceSpikeModel.KnownConduit);
        private static bool KnownCleanseCurse(CardInstanceState card, bool allowUnplayed) =>
            card.Permanent.Upgrades.Count == 2 && KnownCleanseConsume(card.Permanent.Upgrades[1]) &&
            card.ExhaustState is { PlayCountModifier: 0, BaseTraits.Count: 1, UpgradeTrait: null } exhausted &&
            exhausted.BaseTraits[0].TraitIndex == 1 &&
            (card.PlayCount == 1 && exhausted.BaseTraits[0].IsExhausted && exhausted.BaseTraits[0].ExhaustedCardId == card.InstanceId ||
                allowUnplayed && card.PlayCount == 0 && !exhausted.BaseTraits[0].IsExhausted && exhausted.BaseTraits[0].ExhaustedCardId == null) &&
            card.Temporary.Upgrades.Count == 1 && KnownTitanCurse(card.Temporary.Upgrades[0]) &&
            card.Temporary.Upgrades[0] is { Unique: false, RemoveOnDiscard: false, ExcludeFromClones: false,
                RestrictSizeToRoomCapacity: false, MagicPowerTraitScalingOnly: false, CloneDamageBase: null, CloneHealBase: null,
                Lifecycle.ReplacedAssets.Count: 0, Lifecycle.TraitsModified.Count: 0 };
        private static bool KnownStarstruckReduceCost(CardUpgradeModifier upgrade) =>
            upgrade.DataId == "413e67f5-63a0-4ccc-9763-19251392cda0" && upgrade.AssetKey == "ReduceCost" &&
            upgrade.Stats.Cost == -1 && upgrade.Stats.XCost == 1 && upgrade.Stats.Damage == 0 &&
            upgrade.Stats.Health == 0 && upgrade.Stats.Heal == 0 && upgrade.Stats.Size == 0 &&
            upgrade.Stats.EquipmentLimit == 0 && upgrade.Stats.UpgradeSlotCount == 0 && upgrade.Statuses.Count == 0 &&
            !upgrade.RemoveOnDiscard && !upgrade.Unique && !upgrade.ExcludeFromClones &&
            upgrade.UnhealedHealth == 0 && upgrade.DamageBuff == 0 && upgrade.ExternalInteractions.Count == 0 &&
            upgrade.AbilityUpgrade == null && upgrade.TriggerUpgrades?.Count is not > 0 &&
            upgrade.Lifecycle is { AssetKey: "ReduceCost", AddedTraits.Count: 0, RemovedRuntimeTypes.Count: 0,
                TraitsModified.Count: 0, ReplacedAssets.Count: 0, Triggers.Count: 0 };
        private static bool KnownStarstruckTemporary(CardInstanceState card)
        {
            var conduit = card.Temporary.Upgrades.Where(LazarusJuiceSpikeModel.KnownConduit).ToArray();
            return conduit.Length <= 3 && conduit.Select(upgrade => upgrade.DataId).Distinct(StringComparer.Ordinal).Count() == conduit.Length &&
                card.Temporary.Upgrades.All(upgrade => KnownTitanCurse(upgrade) || LazarusJuiceSpikeModel.KnownConduit(upgrade));
        }
        private static bool PowerstoneStats(CardStatModifier stats) => stats.Damage == 10 && stats.Heal == 10 &&
            stats.Health == 0 && stats.Cost == 0 && stats.Size == 0 && stats.XCost == 0 &&
            stats.EquipmentLimit == 0 && stats.UpgradeSlotCount == 0;
        private static bool KnownPyregelPowerstone(CardUpgradeModifier upgrade) =>
            upgrade.DataId == "571cd1d6-6959-48ae-831d-86f07de8329e" && upgrade.AssetKey == "Powerstone" &&
            PowerstoneStats(upgrade.Stats) && upgrade.CloneDamageBase == 10 && upgrade.CloneHealBase == 10 &&
            upgrade.Statuses.Count == 0 && upgrade.ExternalInteractions.Count == 0 && upgrade.DamageBuff == 0 &&
            upgrade.UnhealedHealth == 0 && !upgrade.Unique && !upgrade.RemoveOnDiscard && !upgrade.ExcludeFromClones &&
            !upgrade.RestrictSizeToRoomCapacity && !upgrade.MagicPowerTraitScalingOnly &&
            upgrade.EquipmentSourceCardId == null && upgrade.EquipmentSourceUpgradeIndex == null &&
            upgrade.AbilityUpgrade == null && upgrade.TriggerUpgrades?.Count is not > 0 &&
            upgrade.Lifecycle is { AssetKey: "Powerstone", Unique: false, RemoveOnDiscard: false,
                AddedTraits.Count: 0, RemovedRuntimeTypes.Count: 0, TraitsModified.Count: 0,
                ReplacedAssets.Count: 0, Triggers.Count: 0, OriginalDamage: 10, OriginalHeal: 10 } lifecycle &&
            lifecycle.Values.DataId == upgrade.DataId && PowerstoneStats(lifecycle.Values.Stats) &&
            lifecycle.Values.Statuses.Count == 0;
        private static bool KnownTripleStarstruck(CardInstanceState card, CardTraitCompositionState traits) =>
            card.DataId == "753f20a2-c625-47b3-be4b-8600b673841e" &&
            traits.CombinedTraits is { Count: 2 } combined &&
            combined[0].RuntimeType == CardMoonUpgradeModel.Trait && combined[1].RuntimeType == TripleTrait &&
            combined[1].DeclaredName == TripleTrait && Ordinary(combined[1], 3) &&
            card.Permanent.Upgrades.Count >= 1 && KnownTripleUpgrade(card.Permanent.Upgrades[0]) &&
            (card.Permanent.Upgrades.Count is 1 or 2 && card.Permanent.Upgrades.Skip(1).All(upgrade =>
                upgrade.DataId == "12793a9c-fae4-42ab-80bb-c3274e681c19" && CardMoonUpgradeModel.KnownPayload(upgrade)) ||
                card.Permanent.Upgrades.Count is 2 or 3 && KnownStarstruckReduceCost(card.Permanent.Upgrades[1]) &&
                card.Permanent.Upgrades.Skip(2).All(upgrade => upgrade.DataId == "12793a9c-fae4-42ab-80bb-c3274e681c19" &&
                    CardMoonUpgradeModel.KnownPayload(upgrade))) &&
            KnownStarstruckTemporary(card) &&
            CardMoonUpgradeModel.Validate(card) == null &&
            card.DamageScalingTraits?.Count is not > 0 && card.StatusScalingTraits?.Count is not > 0 &&
            card.UnitUpgradeScalingTraits?.Count is not > 0 && card.TrainHealScalingTraits?.Count is not > 0 &&
            card.CapacityScalingTraits?.Count is not > 0 && card.ForgeCountScalingTraits?.Count is not > 0;
        internal static string? Validate(CardInstanceState card, bool allowUnplayedCleanseCurse = false)
        {
            if (!Present(card)) return null;
            if (card.MaskDescriptor == null) return "Juice requires captured card traits.";
            if (card.DataId == LazarusJuiceSpikeModel.DataId)
                return LazarusJuiceSpikeModel.Known(card) ? null : "Juice with this card or ordered scaling traits requires native calibration.";
            // Original Lifesteal/Scorch,Ritual of Battle,and calibrated merchant owners.
            // Ordered scaling callbacks and other owners remain explicit gaps.
            var refreshed = CardExhaustModel.RefreshTraits(card);
            var traits = refreshed.CombinedTraits!;
            if (SimpleStatusOwner(card, refreshed)) return null;
            if (traits.Any(trait => trait.RuntimeType == QuadrupleTrait))
                return "Juice4 requires a captured simple status owner; ordered callbacks remain unsupported.";
            bool triple = traits.Any(trait => trait.RuntimeType == TripleTrait);
            if (triple && KnownTripleStarstruck(card, refreshed)) return null;
            bool copiedCleanse = triple && KnownTripleCleanseCopy(card, traits);
            if (copiedCleanse) traits = traits.Take(2).ToArray();
            if (triple && card.DataId == "d4366b7c-588c-49aa-b2c6-2aeb30b0e01b" &&
                traits.Count is 1 or 2 && traits[0].RuntimeType == TripleTrait && traits[0].DeclaredName == TripleTrait && Ordinary(traits[0], 3) &&
                card.Permanent.Upgrades.Count >= 1 && KnownTripleUpgrade(card.Permanent.Upgrades[0]) &&
                (traits.Count == 1 && card.Permanent.Upgrades.Count == 1 ||
                    traits.Count == 2 && traits[1].RuntimeType == "CardTraitExhaustState" &&
                    traits[1].DeclaredName == "CardTraitExhaustState" && Ordinary(traits[1], 0) &&
                    card.Permanent.Upgrades.Count == 2 && KnownCleanseConsume(card.Permanent.Upgrades[1])) &&
                (copiedCleanse || card.Temporary.Upgrades.Count <= 1 && card.Temporary.Upgrades.All(LazarusJuiceSpikeModel.KnownConduit) ||
                    KnownCleanseCurse(card, allowUnplayedCleanseCurse)) &&
                card.MaskDescriptor.SourceTraits?.Count is not > 0 &&
                card.DamageScalingTraits?.Count is not > 0 && card.StatusScalingTraits?.Count is not > 0 &&
                card.UnitUpgradeScalingTraits?.Count is not > 0 && card.TrainHealScalingTraits?.Count is not > 0 &&
                card.CapacityScalingTraits?.Count is not > 0 && card.ForgeCountScalingTraits?.Count is not > 0)
                return null;
            if (triple || card.DataId == "ab3c2bfa-0ad4-4be0-a050-293f4866bd65")
            {
                bool inherent = !triple && traits.Count == 4 && traits[3].RuntimeType == "CardTraitIntrinsicState" &&
                    traits[3].DeclaredName == traits[3].RuntimeType && Ordinary(traits[3], 0) &&
                    card.Permanent.Upgrades.Select(upgrade => upgrade.DataId).SequenceEqual(new[] { Upgrade, "62ea77cc-4f97-476f-9ccc-a23e28bc788d" }) &&
                    KnownJuiceUpgrade(card.Permanent.Upgrades[0], false) && KnownInherentXUpgrade(card.Permanent.Upgrades[1]) &&
                    card.Temporary.Upgrades.All(KnownTitanCurse);
                bool original = traits.Count == 3 &&
                    card.Permanent.Upgrades.Select(upgrade => upgrade.DataId).SequenceEqual(new[] { "6497cd22-8c83-4b13-8578-269f4387250d", triple ? TripleUpgrade : Upgrade }) &&
                    KnownJuiceUpgrade(card.Permanent.Upgrades[1], triple) && card.Temporary.Upgrades.All(KnownTitanCurse);
                bool spike = card.DataId == "ab3c2bfa-0ad4-4be0-a050-293f4866bd65" && (inherent || original) &&
                    traits[0].RuntimeType == "CardTraitScalingAddStatusEffect" && traits[0].DeclaredName == traits[0].RuntimeType && Ordinary(traits[0], 4) &&
                    traits[1].RuntimeType == "CardTraitExhaustState" && Ordinary(traits[1], 0) &&
                    traits[2].RuntimeType == (triple ? TripleTrait : Trait) && traits[2].DeclaredName == traits[2].RuntimeType && Ordinary(traits[2], triple ? 3 : 2) &&
                    card.StatusScalingTraits is { Count: 1 } scaling && scaling[0].Query.Type == "PlayedCost" &&
                    scaling[0].Query.Duration == "ThisBattle" && scaling[0].Query.VariableCost &&
                    scaling[0].StacksPerStat == 4 && !scaling[0].OnlyWhenSourceZero && scaling[0].Filter == 0 &&
                    scaling[0].StatusIds.SequenceEqual(new[] { "valor" });
                return spike ? null : triple ? "Triple Juice with this card or ordered scaling traits requires native calibration." :
                    "Juice with this card or ordered scaling traits requires native calibration.";
            }
            bool fracture = card.DataId == "b197fdec-a77d-49f5-bc05-6233b7ce01c7" && traits.Count == 2 &&
                traits[0].RuntimeType == "CardTraitCorruptState" && Ordinary(traits[0], 0) &&
                traits[1].RuntimeType == Trait && Ordinary(traits[1], 2) &&
                card.Permanent.Upgrades.Any(upgrade => upgrade.DataId == "83a6c9ec-0e40-48e6-8a4c-695a68b7e990") &&
                card.Permanent.Upgrades.Any(upgrade => upgrade.DataId == Upgrade);
            bool justCause = card.DataId == "c8b39244-83c1-4eb3-b474-8898226d4280" && traits.Count == 3 &&
                traits[0].RuntimeType == "CardTraitCorruptState" && Ordinary(traits[0], 0) &&
                traits[1].RuntimeType == "CardTraitExhaustState" && Ordinary(traits[1], 0) &&
                traits[2].RuntimeType == Trait && Ordinary(traits[2], 2) &&
                card.Permanent.Upgrades.Select(upgrade => upgrade.DataId).SequenceEqual(new[] {
                    "83a6c9ec-0e40-48e6-8a4c-695a68b7e990", "fb416958-813a-435b-b7ea-e428127ece20", Upgrade });
            bool magicJuice = card.DataId == "9ea70171-8afd-4fab-ad54-7ff5c8b478b8" && traits.Count == 2 &&
                traits[0].RuntimeType == "CardTraitExhaustState" && Ordinary(traits[0], 0) &&
                traits[1].RuntimeType == Trait && Ordinary(traits[1], 2) &&
                card.Permanent.Upgrades.Select(upgrade => upgrade.DataId).SequenceEqual(new[] { Upgrade });
            bool unnamedTome = card.DataId == "561f7a74-93a3-438f-9bfd-fbc84d979bdd" &&
                (traits.Count == 3 || traits.Count == 4 && traits[3].RuntimeType == "CardTraitFreeze" &&
                    traits[3].DeclaredName == "CardTraitFreeze" && Ordinary(traits[3], 0)) &&
                traits[0].RuntimeType == "CardTraitPermafrost" && Ordinary(traits[0], 0) &&
                traits[1].RuntimeType == "CardTraitExhaustState" && Ordinary(traits[1], 0) &&
                traits[2].RuntimeType == Trait && Ordinary(traits[2], 2) &&
                card.Permanent.Upgrades.Select(upgrade => upgrade.DataId).SequenceEqual(new[] { Upgrade });
            bool pyregel = card.DataId == "fb0683b1-20ac-4dd2-b580-14b502e182f4" && traits.Count == 1 &&
                traits[0].RuntimeType == Trait && traits[0].DeclaredName == Trait && Ordinary(traits[0], 2) &&
                card.Permanent.Upgrades.Count is 1 or 2 && KnownJuiceUpgrade(card.Permanent.Upgrades[0], false) &&
                (card.Permanent.Upgrades.Count == 1 || KnownPyregelPowerstone(card.Permanent.Upgrades[1])) &&
                card.Temporary.Upgrades.Count == 0 && card.MaskDescriptor.SourceTraits?.Count is not > 0 &&
                card.DamageScalingTraits?.Count is not > 0 && card.UnitUpgradeScalingTraits?.Count is not > 0 &&
                card.TrainHealScalingTraits?.Count is not > 0 && card.CapacityScalingTraits?.Count is not > 0 &&
                card.ForgeCountScalingTraits?.Count is not > 0;
            if ((!fracture && !justCause && !magicJuice && !unnamedTome && !pyregel && card.DataId is not ("3020f56f-71a5-4155-8bed-48ada7e7e0de" or "f9f20326-dccb-44af-894b-467534afab4a")) ||
                card.StatusScalingTraits?.Count > 0 ||
                traits.Count(trait => trait.RuntimeType == Trait) != 1)
                return "Juice with this card or ordered scaling traits requires native calibration.";
            return null;
        }
        private static bool Ordinary(CardTraitValue trait, int parameter) =>
            trait.DefinitionParamInt == parameter && trait.ParamInt == parameter && trait.Removable &&
            trait.DefinitionRemovable == true && trait.StackMode == 0 && !trait.TemporaryReplacement;
        internal static int Apply(int stacks) => unchecked(stacks + StatusBonus(stacks, 2));
        internal static CardUpgradeModifier OnCardBeingUpgraded(System.Collections.Generic.IEnumerable<CardTraitValue> traits,
            int ownerCardId, int upgradingSourceCardId, CardUpgradeModifier upgrade)
        {
            // Native ignores the target card: the callback belongs to the
            // source card. Clamp separately after each ordered trait callback.
            if (upgradingSourceCardId <= 0 || upgradingSourceCardId != ownerCardId) return upgrade;
            foreach (var trait in traits)
                if (IsTrait(trait.RuntimeType))
                    upgrade = upgrade.MapStatusStacks(count => UpgradeStacks(count, Multiplier(trait.RuntimeType)));
            return upgrade;
        }
        internal static int Apply(CardInstanceState owner, int stacks)
        {
            var traits = CardExhaustModel.RefreshTraits(owner).CombinedTraits!.Where(trait => IsTrait(trait.RuntimeType)).ToArray();
            // Existing calibrated paths have one active Juice callback too.
            foreach (var trait in traits) stacks = unchecked(stacks + StatusBonus(stacks, Multiplier(trait.RuntimeType)));
            return stacks;
        }
    }
}
