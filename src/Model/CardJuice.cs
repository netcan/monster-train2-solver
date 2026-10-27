using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class CardJuiceModel
    {
        internal const string Trait = "CardTraitJuice";
        internal const string Upgrade = "0fc9f6e7-df71-4045-9867-67453769adf8";
        internal const string TripleTrait = "CardTraitJuice3";
        internal const string TripleUpgrade = "2f066286-dbef-449b-aa5f-b8acdcdce7aa";
        private static bool Juice(string type) => type is Trait or TripleTrait;
        internal static bool Present(CardInstanceState card) => card.MaskDescriptor != null &&
            (card.MaskDescriptor.BaseTraits.Concat(card.MaskDescriptor.TemporaryTraits).Any(trait => Juice(trait.RuntimeType)) ||
                card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade =>
                    upgrade.Lifecycle?.AddedTraits.Any(trait => Juice(trait.RuntimeType)) == true)) ||
            card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.DataId == TripleUpgrade);
        internal static bool KnownTripleUpgrade(CardUpgradeModifier upgrade) => KnownJuiceUpgrade(upgrade, true);
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
        private static bool KnownConsumedCleanseCurse(CardInstanceState card) =>
            card.Permanent.Upgrades.Count == 2 && KnownCleanseConsume(card.Permanent.Upgrades[1]) &&
            card.PlayCount == 1 && card.ExhaustState is { PlayCountModifier: 0, BaseTraits.Count: 1, UpgradeTrait: null } exhausted &&
            exhausted.BaseTraits[0].TraitIndex == 1 && exhausted.BaseTraits[0].IsExhausted &&
            exhausted.BaseTraits[0].ExhaustedCardId == card.InstanceId &&
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
        internal static string? Validate(CardInstanceState card)
        {
            if (!Present(card)) return null;
            if (card.MaskDescriptor == null) return "Juice requires captured card traits.";
            if (card.DataId == LazarusJuiceSpikeModel.DataId)
                return LazarusJuiceSpikeModel.Known(card) ? null : "Juice with this card or ordered scaling traits requires native calibration.";
            // Original Lifesteal/Scorch,Ritual of Battle,and calibrated merchant owners.
            // Ordered scaling callbacks and other owners remain explicit gaps.
            var refreshed = CardExhaustModel.RefreshTraits(card);
            var traits = refreshed.CombinedTraits!;
            bool triple = traits.Any(trait => trait.RuntimeType == TripleTrait);
            if (triple && KnownTripleStarstruck(card, refreshed)) return null;
            if (triple && card.DataId == "d4366b7c-588c-49aa-b2c6-2aeb30b0e01b" &&
                traits.Count is 1 or 2 && traits[0].RuntimeType == TripleTrait && traits[0].DeclaredName == TripleTrait && Ordinary(traits[0], 3) &&
                card.Permanent.Upgrades.Count >= 1 && KnownTripleUpgrade(card.Permanent.Upgrades[0]) &&
                (traits.Count == 1 && card.Permanent.Upgrades.Count == 1 ||
                    traits.Count == 2 && traits[1].RuntimeType == "CardTraitExhaustState" &&
                    traits[1].DeclaredName == "CardTraitExhaustState" && Ordinary(traits[1], 0) &&
                    card.Permanent.Upgrades.Count == 2 && KnownCleanseConsume(card.Permanent.Upgrades[1])) &&
                (card.Temporary.Upgrades.Count <= 1 && card.Temporary.Upgrades.All(LazarusJuiceSpikeModel.KnownConduit) ||
                    KnownConsumedCleanseCurse(card)) &&
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
            if ((!fracture && !justCause && !magicJuice && !unnamedTome && card.DataId is not ("3020f56f-71a5-4155-8bed-48ada7e7e0de" or "f9f20326-dccb-44af-894b-467534afab4a")) ||
                card.StatusScalingTraits?.Count > 0 ||
                traits.Count(trait => trait.RuntimeType == Trait) != 1)
                return "Juice with this card or ordered scaling traits requires native calibration.";
            return null;
        }
        private static bool Ordinary(CardTraitValue trait, int parameter) =>
            trait.DefinitionParamInt == parameter && trait.ParamInt == parameter && trait.Removable &&
            trait.DefinitionRemovable == true && trait.StackMode == 0 && !trait.TemporaryReplacement;
        internal static int Apply(int stacks) => unchecked(stacks + Math.Max(stacks, 0));
        internal static int Apply(CardInstanceState owner, int stacks) =>
            CardExhaustModel.RefreshTraits(owner).CombinedTraits!.Any(trait => trait.RuntimeType == TripleTrait)
                ? unchecked(stacks + Math.Max(stacks, 0) * 2) : Apply(stacks);
    }
}
