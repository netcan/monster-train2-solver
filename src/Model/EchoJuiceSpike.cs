using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class EchoJuiceSpikeModel
    {
        private static readonly string[] Stats = { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" };
        internal static bool Known(CardInstanceState card, IReadOnlyList<CardActionEffect> effects)
        {
            if (card.DataId != "ab3c2bfa-0ad4-4be0-a050-293f4866bd65" || CardJuiceModel.Validate(card) != null ||
                card.ExternalInteractions.Count != 0 || card.Permanent.Upgrades.Count != 2 ||
                !CardJuiceModel.KnownTripleUpgrade(card.Permanent.Upgrades[1]) ||
                card.Temporary.Upgrades.Count != 1 || !CardJuiceModel.KnownTitanCurse(card.Temporary.Upgrades[0]) ||
                Stats.Any(stat => card.Permanent.Offsets.Value(stat) != 0 || card.Temporary.Offsets.Value(stat) != 0) ||
                card.DamageScalingTraits?.Count is > 0 || card.UnitUpgradeScalingTraits?.Count is > 0 ||
                card.CapacityScalingTraits?.Count is > 0 || card.ForgeCountScalingTraits?.Count is > 0 ||
                card.TrainHealScalingTraits?.Count is > 0 || card.StatusScalingTraits?.Count != 1 || effects.Count != 1)
                return false;
            CardUpgradeModifier heal = card.Permanent.Upgrades[0];
            if (heal.DataId != "6497cd22-8c83-4b13-8578-269f4387250d" || heal.AssetKey != "TraitHealPyrePerEmber" || Stats.Any(stat => heal.Stats.Value(stat) != 0) ||
                heal.Statuses.Count != 0 || heal.ExternalInteractions.Count != 0 || heal.AbilityUpgrade != null ||
                heal.TriggerUpgrades?.Count is > 0 || heal.UnhealedHealth != 0 || heal.DamageBuff != 0 ||
                heal.Lifecycle is not { AddedTraits.Count: 0, RemovedRuntimeTypes.Count: 0, Triggers.Count: 1 } lifecycle ||
                lifecycle.Triggers[0].Type != "OnCast" || lifecycle.Triggers[0].ExternalInteractions.Count != 0 ||
                !KnownOnCast(lifecycle.Triggers[0].Actions)) return false;
            var scaling = card.StatusScalingTraits[0];
            return scaling.Query is { Type: "PlayedCost", Duration: "ThisBattle", VariableCost: true,
                SourceRawCost: 0, CurrentCostSourceIsSpell: true, CurrentCostCardModifiersOnly: false,
                TargetTeam: "Heroes", SpecificCardDataId: null, SubtypeIsNone: true, TypeMatchDataIds: null, StatusIds: null } &&
                scaling.Query.SubtypeMatchDataIds?.Count is not > 0 &&
                effects[0] is { Type: "AddStatus", Target: "DropTargetCharacter", Value: 0, Statuses.Count: 1,
                    AllowEnemy: true, AllowPlayer: true, Upgrade: null, Generation: null, Range: null } effect &&
                effect.Statuses[0].Id == "valor" && effect.Statuses[0].Stacks == 0;
        }

        internal static bool KnownOnCast(IReadOnlyList<CardActionEffect> effects) => effects.Count == 1 &&
            effects[0] is { Type: "HealTrainPerEmber", Target: "Pyre", Value: 1, Statuses.Count: 0,
                AllowEnemy: true, AllowPlayer: true, Upgrade: null, Generation: null, Range: null,
                SpikeRecipe: null, Recursion: null };

        internal static bool KnownRelics(CombatContext context) => context.Relics?.All(relic =>
        {
            if (EchoWaxenSpikeModel.KnownRelic(relic)) return true;
            if (relic.IsCovenant != false || relic.DisallowedInPlacementPhase != false) return false;
            if (relic.DataId == "46a9ebcf-ed66-4e63-bc87-9431751cd4d1" && relic.AssetKey == "Enemy_CorruptSeraph_BuffAllies" &&
                relic.EffectTypes.SequenceEqual(new[] { RelicModel.AddCardsNull })) return true;
            if (relic.DataId == "3203c2d3-58f3-4f38-888e-886d8faae758" && relic.AssetKey == "ExtraUnitUpgrades" &&
                relic.EffectTypes.SequenceEqual(new[] { "RelicEffectAddUnitUpgradeSlot" })) return true;
            if (relic.DataId == "c97500a9-b0a4-4c18-b203-b9f4447c84c3" && relic.AssetKey == "ManyBuffonAngelEquip" &&
                relic.EffectTypes.SequenceEqual(new[] { RelicModel.AddTrigger }) && RelicCharacterTriggerModel.Validate(relic) == null &&
                relic.TriggerAdditions is { Count: 1 } additions && additions[0].Triggers.Count == 1 &&
                additions[0].Triggers[0].Kind == "OnEquipmentAdded") return true;
            return relic.DataId == "83116c76-f5cc-4912-8a56-872731393975" && relic.AssetKey == "MoreDeploymentPhaseEmber" &&
                relic.EffectTypes.SequenceEqual(new[] { RelicResourceModel.DeploymentEnergy }) && RelicResourceModel.Validate(relic) == null &&
                relic.ResourceEffects is { Count: 1 } resources && resources[0] is { EffectIndex: 0, ParamInt: 3, ParamInt2: 0,
                    ParamMinInt: 0, CanApplyInPreviewMode: false, RequiredMutatorId: null, Conditions.Count: 0 };
        }) != false;
    }
}
