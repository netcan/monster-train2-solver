using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class CardFullCostReductionModel
    {
        internal const string Trait = "CardTraitResetFullCostReductionUpgrades";
        internal const string FecundityId = "16e75365-ef56-4525-95c1-3937844aad49";
        internal const string FreeUpgradeId = RelicRandomHandUpgradeModel.UpgradeId;

        internal static bool OriginalTraitUpgrade(string dataId, string assetKey, CardLifecycleUpgrade lifecycle) =>
            dataId == FecundityId && assetKey == "InscryptionEventFecundity_Upgrade" &&
            lifecycle.AddedTraits is [{ RuntimeType: Trait, DeclaredName: Trait, DefinitionParamInt: 0, ParamInt: 0 }] &&
            lifecycle.RemovedRuntimeTypes.Count == 0 &&
            lifecycle.Triggers is [{ Type: "OnCast", Effects: ["CardEffectAddUpgradedCopy"], Actions: [var action] }] &&
            action is { Type: "GenerateUpgradedCopy", Target: "Self", Generation: { Count: 1, CopyModifiers: true,
                Upgrade: { DataId: "e24a2df4-2814-4f57-a8ae-a256fe706cd1", AssetKey: "InscryptionEventFecundity2_Upgrade",
                    Stats.Cost: 1, Lifecycle.ReplacedAssets: ["InscryptionEventFecundity_Upgrade"] } } };

        internal static bool HasOriginalTrait(CardUpgradeModifier upgrade) =>
            upgrade.Lifecycle != null && OriginalTraitUpgrade(upgrade.DataId, upgrade.AssetKey, upgrade.Lifecycle);

        internal static bool HasOriginalTrait(CardInstanceState card) =>
            card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(HasOriginalTrait);

        internal static CardUpgradeModifier Resolve(CombatContext context, CardInstanceState target,
            CardInstanceState? upgradingCard, CardUpgradeModifier upgrade, out string? error)
        {
            error = null;
            CardInstanceState owner = upgradingCard ?? target;
            if (upgrade.Stats.Cost != -99 || owner.MaskDescriptor == null ||
                !CardBranchMaskModel.Resolve(owner).Traits.Contains(Trait))
                return upgrade;

            if (upgrade.DataId != FreeUpgradeId || upgrade.AssetKey != "FreeFromBlessing" ||
                !HasOriginalTrait(owner) ||
                target.MaskDescriptor is not { Definition: { CardType: "Monster", XCost: false } } mask ||
                mask.Definition.MainEffectNames is not ["CardEffectSpawnMonster"] ||
                CardBranchMaskModel.Resolve(target).Traits.Any(trait => trait != Trait) ||
                target.Permanent.Upgrades.Concat(target.Temporary.Upgrades).Any(item =>
                    item.Lifecycle?.Triggers.Count > 0 && !HasOriginalTrait(item)))
            {
                error = "Full cost reduction requires the observed original upgrade callback and supported unit cost.";
                return upgrade;
            }

            int cost = CardHandCostModel.Resolve(context, target, mask.BaseCost, mask.Definition.CardType);
            CardStatModifier values = upgrade.Stats;
            return upgrade.WithStats(new CardStatModifier(values.Damage, values.Health, -cost, values.Heal,
                values.Size, values.XCost, values.EquipmentLimit, values.UpgradeSlotCount));
        }
    }
}
