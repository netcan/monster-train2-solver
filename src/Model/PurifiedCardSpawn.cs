using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class PurifiedCardSpawnModel
    {
        internal const string Interaction = "Purified card";

        internal static bool SupportsMask(CardInstanceState instance) =>
            instance.MaskDescriptor is { Purified: true, PermanentGraft: false, GraftedEquipment: null } mask &&
            mask.Definition.DataId == instance.DataId && mask.Definition.CardType == "Monster" &&
            mask.Definition.IsSpawner && mask.Definition.HasSpawnData && !mask.Definition.AuthoredGraft && !mask.Definition.AuthoredUnitAbility &&
            mask.Definition.IsChampionCard == false && mask.Definition.HasStartingUpgrades == false &&
            mask.Definition.MainEffectNames.Count == 1 && mask.Definition.MainEffectNames[0] == "CardEffectSpawnMonster" &&
            mask.Definition.StartingStatuses.Count == 1 && mask.Definition.StartingStatuses[0].Id == "ephemeral" &&
            mask.BaseTraits.Count == 0 && mask.AuthoredCastEffects.Count == 0 && instance.GraftedEquipmentCardId == null &&
            instance.Permanent.MergedEquipment == null &&
            !instance.Permanent.Upgrades.Concat(instance.Temporary.Upgrades).Any(upgrade => upgrade.AbilityUpgrade != null);

        internal static CombatUnit? Project(CardPlayRule rule, CardInstanceState instance)
        {
            CombatUnit? unit = rule.SpawnUnit;
            if (!instance.ExternalInteractions.Contains(Interaction) || !SupportsMask(instance) || instance.DataId != rule.DataId ||
                rule.Effect != "SpawnMonster" || rule.Summon != null || rule.Effects.Count != 0 || rule.OnCastEffects.Count != 0 ||
                rule.HasKillTriggers || rule.HasSpareTriggers || rule.HasUnsupportedUnplayedTriggers ||
                rule.OnForgeEffects?.Count > 0 || rule.OnUnplayedNegativeEffects.Count != 0 || rule.Equipment != null ||
                unit is not { Ability: null, NextTriggerId: 1, Modifiers: { Graft: null, RoomResources: null,
                    RoomStatuses: null, RoomTriggerCounts: null, TroopAura: null, Sentry: null } } ||
                unit.Triggers.Count != 1 || unit.Triggers[0].Kind != "OnHit" || unit.Triggers[0].Effects.Count != 1 ||
                unit.Triggers[0].Effects[0].Action is not { Type: "AddStatus", Statuses.Count: 1 } effect ||
                effect.Statuses[0].Id != "ephemeral" || unit.StatusImmunities.Count != 0 ||
                unit.StatusRegistry?.Count != 1 || unit.StatusRegistry[0].Id != "ephemeral" ||
                unit.StatusDictionary is not { Slots.Count: 1, FreeSlots.Count: 0 } || unit.StatusDictionary.Slots[0] != "ephemeral")
                return null;

            // Only the original birth template is filtered. The caller still
            // resolves owned modifiers and the common spawn phases afterwards.
            return new CombatUnit(unit.Id, unit.AssetKey, unit.Team, unit.BaseAttack, unit.Health, unit.MaxHealth,
                unit.CanAttack, unit.IsPyre, unit.EndsBattleOnDeath, Array.Empty<CombatStatus>(),
                spawnerCardId: unit.SpawnerCardId, size: unit.Size, statusImmunities: unit.StatusImmunities,
                subtypes: unit.Subtypes, modifiers: unit.Modifiers, isBoss: unit.IsBoss, lastAttackerId: unit.LastAttackerId,
                statusRegistry: Array.Empty<CombatStatus>(), equipmentCards: unit.EquipmentCards, nextTriggerId: 0,
                statusDictionary: new StatusDictionaryState(Array.Empty<string?>(), Array.Empty<int>()), abilityRules: unit.AbilityRules,
                hordeDefinition: unit.HordeDefinition, isSpawning: unit.IsSpawning, sacrificeCardId: unit.SacrificeCardId,
                deathState: unit.DeathState, bumpRules: unit.BumpRules, lastFeederId: unit.LastFeederId,
                moonPhaseMask: unit.MoonPhaseMask, damageHistory: unit.DamageHistory);
        }
    }
}
