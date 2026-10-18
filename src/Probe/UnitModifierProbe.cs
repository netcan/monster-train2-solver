using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitModifierProbe
    {
        internal static UnitModifiers Capture(CharacterState unit)
        {
            object state = AccessTools.Property(typeof(CharacterState), "PrimaryStateInformation").GetValue(unit);
            int rawSize = (int)AccessTools.Property(state.GetType(), "Size").GetValue(state);
            var health = (Dictionary<string, int>)AccessTools.Field(state.GetType(), "maxHpFromUpgrades").GetValue(state);
            CardState? spawner = unit.GetSpawnerCard();
            RoomStateSelfDamagePerMagicPowerModifier[] mageblade = unit.GetRoomStateModifiers<RoomStateSelfDamagePerMagicPowerModifier>().ToArray();
            if (mageblade.Length > 1) throw new InvalidOperationException("Multiple Mageblade attack caches were attached to one unit.");
            CardUpgradeState[] upgrades = unit.GetAppliedCardUpgrades().ToArray();
            RoomState? room = unit.GetCurrentRoom();
            var roomAttachmentUpgrades = new HashSet<string>(room == null ? Array.Empty<string>() :
                room.Attachments.Where(attachment => attachment.IsActive && attachment.CurrentlyImpactsCharacter(unit))
                    .SelectMany(attachment => attachment.UpgradeStates.Select(item => item.Item1.GetCardUpgradeDataId())));
            // Replacement statuses mask both public getters; retain primary fields for later removal.
            int rawAttack = (int)AccessTools.Property(state.GetType(), "AttackDamage").GetValue(state);
            int rawBuff = (int)AccessTools.Property(state.GetType(), "DamageBuff").GetValue(state);
            return new UnitModifiers(rawAttack, unit.GetAttackDamageAddedFromUpgrades(), rawBuff,
                rawSize, unit.GetEquipmentLimit(), unit.GetCanBeHealed(), unit.GetIsClone(),
                upgrades.Select(upgrade =>
                {
                    bool roomAttachmentUpgrade = roomAttachmentUpgrades.Contains(upgrade.GetCardUpgradeDataId());
                    return FullBattleTrace.Active?.CaptureAppliedUpgrade(upgrade, roomAttachmentUpgrade) ??
                        CardModifierProbe.Upgrade(upgrade, rejectFilters: false, filtersAlreadyApplied: true,
                            roomAttachmentUnitUpgrade: roomAttachmentUpgrade);
                }).ToArray(), health.OrderBy(item => item.Key)
                    .Select(item => new StatisticCount(FullBattleTrace.Active?.UpgradeKey(item.Key) ?? item.Key, item.Value)).ToArray(),
                spawner?.GetSpawnCharacterData() == null || spawner.GetSpawnCharacterData() == unit.GetSourceCharacterData(),
                RelicProbe.HasStatusAttackHistory(AllGameManagers.Instance!) ? (int?)AccessTools.Property(state.GetType(), "DamageBuffFromStatusEffects").GetValue(state) : null,
                mageblade.SingleOrDefault()?.AdditionalDamagePerMagicPower, RelicProbe.StatusAttackModifiers(AllGameManagers.Instance!), unit.IsChampion());
        }
        internal static UnitModifiers Definition(CharacterData data) => new UnitModifiers(data.GetAttackDamage(), 0, 0,
            data.GetSize(), data.GetEquipmentLimit(), data.GetCanBeHealed(), false, System.Array.Empty<CardUpgradeModifier>(),
            damageBuffFromStatusEffects: RelicProbe.HasStatusAttackHistory(AllGameManagers.Instance!) ? 0 : (int?)null,
            statusAttackModifiers: RelicProbe.StatusAttackModifiers(AllGameManagers.Instance!), isChampion: data.IsChampion());
    }
}
