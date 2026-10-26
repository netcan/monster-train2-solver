using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Native Echo copies the unscaled effects and the spell's paid cost. The
    // detached copy scales its own upgrade at replay, without exhausting again.
    internal static class EchoWaxenSpikeModel
    {
        internal const string DataId = "8d291f9a-30a1-4a6d-8d9e-6dc2df4432d1";
        private static readonly string[] Stats = { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" };
        internal static bool Known(CardInstanceState card, IReadOnlyList<CardActionEffect> effects)
        {
            if (card.DataId != DataId || card.MaskDescriptor == null || card.ExternalInteractions.Count != 0 ||
                card.Permanent.Upgrades.Count != 0 || card.Temporary.Upgrades.Any(upgrade => !CardJuiceModel.KnownTitanCurse(upgrade)) ||
                card.DamageScalingTraits?.Count is > 0 || card.StatusScalingTraits?.Count is > 0 ||
                card.CapacityScalingTraits?.Count is > 0 || card.ForgeCountScalingTraits?.Count is > 0 || card.TrainHealScalingTraits?.Count is > 0 ||
                Stats.Any(stat => card.Permanent.Offsets.Value(stat) != 0 || card.Temporary.Offsets.Value(stat) != 0) ||
                card.UnitUpgradeScalingTraits?.Count != 1 || effects.Count != 2) return false;
            var traits = CardExhaustModel.RefreshTraits(card).CombinedTraits;
            var scaling = card.UnitUpgradeScalingTraits[0];
            bool Trait(CardTraitValue trait, string type, int amount) => trait.RuntimeType == type && trait.DeclaredName == type &&
                trait.ParamInt == amount && trait.DefinitionParamInt == amount && trait.Removable && trait.DefinitionRemovable == true &&
                trait.StackMode == 0 && !trait.TemporaryReplacement && trait.ParameterUpgradeCastEffects.Count == 0;
            return traits?.Count == 2 && Trait(traits[0], "CardTraitScalingUpgradeUnitAttack", 4) &&
                Trait(traits[1], "CardTraitExhaustState", 0) && scaling.Stat == "Damage" && scaling.AmountPerStat == 4 &&
                scaling.Restriction == 0 && scaling.Query is { Type: "PlayedCost", Duration: "ThisBattle", VariableCost: true,
                    SourceRawCost: 0, CurrentCostSourceIsSpell: true, CurrentCostCardModifiersOnly: false,
                    TargetTeam: "None", SpecificCardDataId: null, SubtypeIsNone: true, TypeMatchDataIds: null, StatusIds: null } &&
                scaling.Query.SubtypeMatchDataIds?.Count is not > 0 &&
                effects[0] is { Type: "UnitUpgrade", Target: "Room", Value: 0, ScaleUpgrade: false,
                    Lifetime: "TemporaryUntilEndOfBattle", Upgrade: not null } effect &&
                effect.Upgrade.DataId == "31243b24-f327-42c8-84ad-b014e6fec675" && effect.Upgrade.AssetKey == "XCostBurnoutEnhance" &&
                Stats.All(stat => effect.Upgrade.Stats.Value(stat) == 0) &&
                effect.Upgrade is { Statuses.Count: 0, UnhealedHealth: 0, DamageBuff: 0, AbilityUpgrade: null, ExternalInteractions.Count: 0 } &&
                effect.Upgrade.TriggerUpgrades?.Count is not > 0 &&
                effects[1] is { Type: "AddStatus", Target: "Room", Statuses.Count: 1 } status &&
                status.Statuses[0].Id == "ephemeral" && status.Statuses[0].Stacks == 4;
        }

        internal static bool KnownRelics(CombatContext context) => context.Relics?.All(KnownRelic) != false;
        private static bool KnownRelic(CombatRelicState relic)
        {
            if (relic.EffectTypes.Count == 0) return true;
            if (relic.IsCovenant != false) return false;
            // This sealed interface only changes draft rarity; no card callback.
            if (relic.DataId == "8f46fde9-0c75-49ba-9f58-3063864f81bf" && relic.AssetKey == "FutureDraftsAreRares" &&
                relic.EffectTypes.SequenceEqual(new[] { "RelicEffectOverrideCardDraftRarity" })) return true;
            bool first = relic.DataId == "780f977e-4007-45ae-900f-c8929b2bf000" && relic.AssetKey == "BossEmber";
            bool second = relic.DataId == "6eef1e2d-3bd5-4f04-bd88-d0c24d5eb691" && relic.AssetKey == "BossEmber2";
            if (!(first || second) || relic.DisallowedInPlacementPhase != false ||
                !relic.EffectTypes.SequenceEqual(new[] { RelicResourceModel.Energy, RelicResourceModel.PyreHealth,
                    RelicResourceModel.PyreAttack, RelicResourceModel.DeploymentEnergy }) ||
                relic.ResourceEffects?.Count != 4 || RelicResourceModel.Validate(relic) != null) return false;
            return relic.ResourceEffects.All(effect => effect.Conditions.Count == 0 && !effect.CanApplyInPreviewMode &&
                effect.RequiredMutatorId == null && effect.ParamMinInt == 0) &&
                relic.ResourceEffects.Select(effect => effect.ParamInt).SequenceEqual(new[] { 1, 30, 10, 1 }) &&
                relic.ResourceEffects.Select(effect => effect.ParamInt2).SequenceEqual(second ? new[] { 0, 1, 1, 1 } : new[] { 0, 0, 0, 0 });
        }
    }
}
