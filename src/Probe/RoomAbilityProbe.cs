using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RoomAbilityProbe
    {
        internal static RoomAbilityState? Definition(CardUpgradeData upgrade)
        {
            CardData ability = upgrade.GetRoomAbilityUpgrade();
            return ability == null ? null : new RoomAbilityState(ability.GetID(), ability.GetCooldownAtSpawn(),
                ability.GetCooldownAfterActivated(), CardGenerationProbe.Creation(ability));
        }
        internal static RoomAbilityState? Capture(TrainRoomAttachmentState attachment)
        {
            CardData? ability = attachment.UpgradeStates.Select(upgrade => upgrade.Item2.GetRoomAbilityUpgrade()).FirstOrDefault(card => card != null);
            return !attachment.HasRoomAbility || ability == null ? null : new RoomAbilityState(ability.GetID(),
                attachment.CurrentAbilityCooldown, ability.GetCooldownAfterActivated(), CardGenerationProbe.Creation(ability));
        }
        internal static bool IsCorruption(RoomModifierData modifier) => modifier.GetRoomStateModifierClassName() == "RoomStateCorruptionModifier";
        internal static RoomAttachmentCorruption? Corruption(IEnumerable<RoomModifierData> modifiers)
        {
            RoomModifierData[] values = modifiers.Where(IsCorruption).ToArray();
            return values.Length == 0 ? null : new RoomAttachmentCorruption(values.Sum(value => value.GetParamInt()), values.Sum(value => value.GetParamInt2()));
        }
    }
}
