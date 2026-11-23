using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class CardFullCostReductionModel
    {
        internal const string Trait = "CardTraitResetFullCostReductionUpgrades";
        internal const string FecundityId = "16e75365-ef56-4525-95c1-3937844aad49";
        internal const string FecundityCopyId = "e24a2df4-2814-4f57-a8ae-a256fe706cd1";
        internal const string FreeUpgradeId = RelicRandomHandUpgradeModel.UpgradeId;

        internal static bool OriginalTraitUpgrade(string dataId, string assetKey, CardLifecycleUpgrade lifecycle) =>
            dataId == FecundityId && assetKey == "InscryptionEventFecundity_Upgrade" &&
            lifecycle.AddedTraits is [{ RuntimeType: Trait, DeclaredName: Trait, DefinitionParamInt: 0, ParamInt: 0 }] &&
            lifecycle.RemovedRuntimeTypes.Count == 0 &&
            lifecycle.Triggers is [{ Type: "OnCast", Effects: ["CardEffectAddUpgradedCopy"], Actions: [var action] }] &&
            action is { Type: "GenerateUpgradedCopy", Target: "Self", Generation: { Count: 1, CopyModifiers: true,
                Upgrade: { DataId: FecundityCopyId, AssetKey: "InscryptionEventFecundity2_Upgrade",
                    Stats.Cost: 1, Lifecycle.ReplacedAssets: ["InscryptionEventFecundity_Upgrade"] } } };

        internal static bool HasOriginalTrait(CardUpgradeModifier upgrade) =>
            upgrade.Lifecycle != null && OriginalTraitUpgrade(upgrade.DataId, upgrade.AssetKey, upgrade.Lifecycle);

        internal static bool HasOriginalTrait(CardInstanceState card) =>
            card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(HasOriginalTrait);

        internal static CardGenerationRule? ResolveCopy(CardInstanceState? source, CardGenerationRule rule)
        {
            if (source?.MaskDescriptor is not { Definition: { CardType: "Monster", XCost: false,
                    IsChampionCard: false, HasStartingUpgrades: false, MainEffectNames: ["CardEffectSpawnMonster"] },
                    SourceTraits: [], BaseTraits: [{ RuntimeType: Trait, DeclaredName: Trait }], TemporaryTraits: [],
                    AuthoredCastEffects: ["CardEffectAddUpgradedCopy"], Purified: false, PermanentGraft: false,
                    PermanentReplacements: [], KillTriggers: null, SpareTriggers: null } mask ||
                source is not { CanBeCopied: true, GraftedEquipmentCardId: null, EquippedUnitId: null or 0,
                    Permanent: { Upgrades: [var owner], PersistentHealth: 0, BoxedCards: null, MergedEquipment: null },
                    Temporary: { Upgrades.Count: 0, PersistentHealth: 0, BoxedCards: null, MergedEquipment: null } } ||
                source.EffectCounters?.Count > 0 || !HasOriginalTrait(owner) ||
                rule is not { Destination: "HandPile", Count: 1, Pool: [], CopyModifiers: true,
                    IgnoreTemporaryModifiers: false, DeterministicPoolSelection: true, RequireCopyableSource: true,
                    UpgradeIsPermanent: false, Permanent: null, SkipDuplicateInHand: false, RequireHandSpace: false,
                    SelectedPoolIndex: null, PoolEligibility: null, ChampionCardDataIds: null,
                    OptionalUpgradeFilters: null, OnlyUnequippedTargets: null, IgnoreIfEphemeral: null,
                    ExternalInteractions: [], DiscardCopyUpgrades: [],
                    Upgrade: { DataId: FecundityCopyId, AssetKey: "InscryptionEventFecundity2_Upgrade",
                        Unique: false, RemoveOnDiscard: false, ExcludeFromClones: false, UnhealedHealth: 0,
                        DamageBuff: 0, Statuses: [], TriggerUpgrades: null, AbilityUpgrade: null,
                        ExternalInteractions: ["Upgrade replacements"],
                        Stats: { Damage: 0, Health: 0, Cost: 1, Heal: 0, Size: 0, XCost: -1,
                            EquipmentLimit: 0, UpgradeSlotCount: 0 },
                        Lifecycle: { ReplacedAssets: ["InscryptionEventFecundity_Upgrade"], AddedTraits: [],
                            RemovedRuntimeTypes: [], TraitsModified: [], Triggers: [], AvoidClobbering: false } } })
                return null;

            // Build the original creation descriptor without the replaced upgrade's trait and callback.
            var descriptor = new CardMaskDescriptor(mask.Definition, mask.BaseCost, System.Array.Empty<CardTraitValue>(),
                System.Array.Empty<string>(), sourceTraits: mask.SourceTraits);
            var creation = new CardCreationRule(source.DataId, CardModifiers.Empty(), null, System.Array.Empty<string>(),
                equippedUnitId: source.EquippedUnitId, maskDescriptor: descriptor, forgeCountScalingTraits: source.ForgeCountScalingTraits,
                canBeCopied: source.CanBeCopied);
            return new CardGenerationRule(rule.Destination, rule.Count, new[] { creation }, copyModifiers: true,
                upgrade: rule.Upgrade, deterministicPoolSelection: true, requireCopyableSource: true);
        }

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
