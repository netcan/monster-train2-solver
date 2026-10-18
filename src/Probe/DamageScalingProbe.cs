using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DamageScalingProbe
    {
        internal static bool Known(string name) => name == "CardTraitShowCardTargets" || name == "CardTraitReturnToHandEquipment" || name == "CardTraitScalingAdjustCapacity" || name == "CardTraitSelfPurge" || name == "CardTraitInfusion" || name == "CardTraitPersistent" || name == "CardTraitScalingAddDamage" || name == "CardTraitScalingAddStatusEffect" ||
            name == "CardTraitScalingAddForged" || name == "CardTraitScalingHealTrain" || name == "CardTraitFreeze" ||
            (name == "CardTraitScalingFeederRules" || name == "CardTraitDistinct" || name == CardCorruptRestrictedModel.Trait || name == CardStatusDamageModel.Trait || name == CardSpecificDamageModel.Trait || name == CardScaledEnergyModel.Trait || name == CardStatusRemovalScalingModel.Trait || name == CardEffectApplicationsModel.Trait) && CardBranchMaskProbe.Enabled ||
            (name == "CardTraitDamageOverflow" || name == "CardTraitPermafrost" || name == "CardTraitEphemeral" || name == CardPileDrawModel.Trait || name == CardBuffDamageModel.Trait || name == CardPileStatusModel.Trait || name == CardDrawUpgradeScalingModel.Trait || name == CardMoonAdvanceScalingModel.Trait || name == CardDiscardDrawModel.Trait) && CardBranchMaskProbe.Enabled ||
            (name == "CardTraitStrongerMagicPower" || name == CardMoonUpgradeModel.Trait || name == "CardTraitExhaustState" || name == "CardTraitIgnoreArmor" || name == "CardTraitUnpurgable" || name == "CardTraitUnplayable" || name == "CardTraitRefine" || name == CardHandCostModel.Trait || name == CardPlayRangeModel.Trait || name == "CardTraitDrawInDeploymentPhase") && CardBranchMaskProbe.Enabled || UnitUpgradeScalingProbe.Known(name);
        internal static ScalingDamageTrait[]? Capture(CardState card)
        {
            ScalingDamageTrait[] traits = card.GetTraitStates().OfType<CardTraitScalingAddDamage>().Select(trait =>
                new ScalingDamageTrait(Query(trait.StatValueData, (int)AccessTools.Field(typeof(CardState), "cost").GetValue(card),
                    card.IsConsumeRemainingEnergyCostType()), trait.GetParamInt(), trait.GetParamFloat(), trait.GetParamBool())).ToArray();
            return traits.Length == 0 ? null : traits;
        }
        internal static ScalingDamageTrait[]? Creation(CardData card)
        {
            ScalingDamageTrait[] traits = card.GetTraits().Where(data => data.GetTraitStateName() == "CardTraitScalingAddDamage").Select(data =>
            {
                // Setup only copies immutable parameters; it does not register or notify the game.
                var trait = new CardTraitScalingAddDamage(); trait.Setup(data, CardState.None);
                return new ScalingDamageTrait(Query(trait.StatValueData, card.GetCost(), card.GetCostType() == CardData.CostType.ConsumeRemainingEnergy),
                    trait.GetParamInt(), trait.GetParamFloat(), trait.GetParamBool());
            }).ToArray();
            return traits.Length == 0 ? null : traits;
        }
        internal static CardStatisticQuery Query(CardStatistics.StatValueData data, int rawCost, bool variable)
        {
            // Masks cover every static definition, including cards that future effects may generate.
            IReadOnlyList<CardData> definitions = AllGameManagers.Instance!.GetSaveManager().GetAllGameData().GetAllCardData();
            string[]? typeMask = data.cardTypeTarget == CardStatistics.CardTypeTarget.Any ? null : definitions
                .Where(card => card.GetCardType().ToString() == data.cardTypeTarget.ToString()).Select(card => card.GetID()).ToArray();
            string[] subtypeMask = data.paramSubtype == null ? Array.Empty<string>() : definitions.Where(card =>
                card.GetSpawnCharacterData()?.GetSubtypes().Contains(data.paramSubtype) == true).Select(card => card.GetID()).ToArray();
            return new CardStatisticQuery(data.trackedValue.ToString(), data.entryDuration.ToString(), typeMask, subtypeMask,
                data.paramSubtype == null || data.paramSubtype.IsNone, data.paramSubtype?.Key, data.paramCardData?.GetID(), rawCost, variable,
                data.paramTeamType.ToString(), CurrentCostModifiersOnly(data.cardState),
                data.cardState?.GetCardType() == CardType.Spell,
                data.trackedValue == CardStatistics.TrackedValueType.StatusEffectCountInTargetRoom ?
                    data.paramStatusEffects.Select(status => status.statusId).ToArray() : null);
        }

        internal static bool CurrentCostModifiersOnly(CardState? card)
        {
            if (card == null || card.HasModifyCostEffect()) return false;
            static bool Overrides(CardTraitState trait, string methodName) => trait.GetType().GetMethods()
                .Any(method => method.Name == methodName && method.DeclaringType != typeof(CardTraitState));
            if (card.GetTraitStates().Any(trait => Overrides(trait, nameof(CardTraitState.GetModifiedCost)))) return false;
            AllGameManagers? managers = AllGameManagers.Instance;
            if (managers == null) return false;
            CardManager? cardManager = managers.GetCardManager();
            if (cardManager == null) return false;
            if (cardManager.GetHand().SelectMany(item => item.GetTraitStates())
                .Any(trait => Overrides(trait, nameof(CardTraitState.GetHandModifiedCost)))) return false;
            RoomManager? roomManager = managers.GetRoomManager();
            if (roomManager == null) return false;
            RoomState? room = roomManager.GetRoom(roomManager.GetSelectedRoom());
            if (room == null) return true;
            var characters = new List<CharacterState>();
            room.AddCharactersToList(characters, Team.Type.Heroes | Team.Type.Monsters, allowDead: true);
            IRoomStateCostModifier[] attachmentModifiers = room
                .GetRoomStateModifiersFromTrainRoomAttachments<IRoomStateCostModifier>(Team.Type.Heroes | Team.Type.Monsters).ToArray();
            return !characters.SelectMany(character => character.GetRoomStateModifiers<IRoomStateCostModifier>()).Any() &&
                attachmentModifiers.All(modifier => modifier is RoomStateSpellCostModifier spellCost && spellCost.GetParamInt() == -1);
        }
    }
}
