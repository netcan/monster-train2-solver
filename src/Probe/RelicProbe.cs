using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicProbe
    {
        private static EquipmentTriggerDefinition[]? equipmentDefinitions;
        internal static bool HasTriggerCounts(AllGameManagers managers) =>
            Current(managers).Any(relic => relic.GetEffects().Any(effect => effect is RelicEffectModifyTriggerCount || effect is RelicEffectAddTrigger));
        internal static bool HasStatusAttackHistory(AllGameManagers managers) =>
            PyreResourceRefreshScenario.Enabled || Current(managers).Any(relic => relic.GetEffects().Any(effect => effect is RelicEffectModifyStatusMagnitude ||
                effect is RelicEffectAdjustAttackByStatusEffect ||
                effect is RelicEffectPreventStatusEffectDecay decay && decay.GetStatusEffects().Any(status => status.statusId == "buff")));
        internal static RelicStatusAttackModifier[] StatusAttackModifiers(AllGameManagers managers) =>
            Current(managers).SelectMany(owner => owner.GetEffects().Select((effect, index) => (owner, effect, index)))
                .Where(item => item.effect is RelicEffectAdjustAttackByStatusEffect)
                .Select(item => {
                    object Field(string name) => AccessTools.Field(item.effect.GetType(), name).GetValue(item.effect);
                    return new RelicStatusAttackModifier(item.index, (string)Field("statusId"),
                        ((Team.Type)Field("team")).ToString(), (int)Field("multiplier"));
                }).ToArray();
        private static List<RelicState> Current(AllGameManagers managers)
        {
            var relics = new List<RelicState>();
            AccessTools.Method(typeof(RelicManager), "GetCurrentRelics").Invoke(managers.GetRelicManager(), new object[] { relics });
            return relics;
        }
        internal static CombatRelicState[] Capture(AllGameManagers managers)
        {
            // Includes hero blessings, covenants, mutators, Pyre artifacts and souls,
            // in the exact order searched by GetRelicEffect<T>.
            var relics = Current(managers);
            return relics.Select(relic => {
                var effects = relic.GetEffects().ToArray();
                var statuses = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectAddStatusEffectOnSpawn || item.Effect is RelicEffectRemoveStatusEffectOnSpawn ||
                        item.Effect.GetType().Name == RelicModel.AddStatusOnTeamSpawn)
                    .Select(item => SpawnStatus(item.Effect, item.Index)).ToArray();
                var roomEntryStatuses = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectAddStatusEffectOnEnterRoomIndex)
                    .Select(item => RoomEntryStatus(item.Effect, item.Index)).ToArray();
                var corruptionStatuses = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectAddStatusEffectOnCorruptionAdded)
                    .Select(item => CorruptionStatus(item.Effect, item.Index)).ToArray();
                var cardModifiers = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectAddTempUpgrade).Select(item => RelicCardModifierProbe.Definition(item.Effect, item.Index)).ToArray();
                var cardTypeCosts = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectModifyCardTypeCost).Select(item => {
                        var data = (RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(item.Effect);
                        SubtypeData? subtype = data.GetParamCharacterSubtype();
                        return new RelicCardTypeCost(item.Index, data.GetParamCardType().ToString(), data.GetParamInt(),
                            data.GetParamBool(), subtype == null || subtype.IsNone ? null : subtype.Key,
                            data.GetEffectConditions().Count);
                    }).ToArray();
                var cardHealthModifiers = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectModifyCardHealth).Select(item => {
                        object Field(string name) => AccessTools.Field(item.Effect.GetType(), name).GetValue(item.Effect);
                        var subtype = (SubtypeData)Field("characterSubtype");
                        return new RelicCardHealthModifier(item.Index, (string)Field("characterName"),
                            subtype == null || subtype.IsNone ? "" : subtype.Key, (int)Field("healthModifier"),
                            (int)Field("healthMultiplier"), Field("cardType").ToString(), (bool)Field("multiplyByUpgradeCount"),
                            RelicCardModifierProbe.Conditions(item.Effect));
                    }).ToArray();
                var cardAttackModifiers = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectModifyCardAttack).Select(item => {
                        object Field(string name) => AccessTools.Field(item.Effect.GetType(), name).GetValue(item.Effect);
                        var subtype = (SubtypeData)Field("_characterSubtype");
                        var filter = (CardUpgradeMaskData)Field("_cardFilter");
                        return new RelicCardAttackModifier(item.Index, (string)Field("_characterName"),
                            subtype == null || subtype.IsNone ? "" : subtype.Key, (int)Field("_attackModifier"),
                            (float)Field("_attackMultiplier"), (int)Field("_unmodifiedAttackMultiplier"),
                            Field("_cardType").ToString(), (bool)Field("_multiplyByUpgradeCount"),
                            (bool)Field("_multiplyByRelicCount"), (bool)Field("_excludeChampions"),
                            (bool)Field("_excludeStartRelics"), filter == null ? null : CardUpgradeMaskProbe.Rule(filter),
                            (bool)Field("_multiplyByRelicCount") ? CountRoles(managers) : null);
                    }).ToArray();
                var triggers = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectAddTrigger).Select(item => AddedTriggers(item.Effect, item.Index)).ToArray();
                var unitUpgradeTriggers = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectAddRemoveTempUpgradeOnUnitTrigger)
                    .Select(item => UnitUpgradeTrigger(item.Effect, item.Index)).ToArray();
                var stackAdditions = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectAddStatusEffectOnStatusApplied).Select(item => StatusStacks(item.Effect, item.Index)).ToArray();
                var magnitudes = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectModifyStatusMagnitude).Select(item => StatusMagnitude(item.Effect, item.Index)).ToArray();
                var decays = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectPreventStatusEffectDecay).Select(item => StatusDecay(item.Effect, item.Index)).ToArray();
                var resources = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => RelicResourceModel.IsEffect(item.Effect.GetType().Name))
                    .Select(item => RelicResourceProbe.Definition(item.Effect, item.Index, managers)).ToArray();
                var forgePointModifiers = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect.GetType().Name == ForgePointsModel.AdditionalForgeRelic)
                    .Select(item => new RelicForgePointModifier(item.Index,
                        ((RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(item.Effect)).GetParamInt(),
                        item.Effect.CanApplyInPreviewMode, RelicCardModifierProbe.Conditions(item.Effect))).ToArray();
                var magicPower = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectModifyCardMagicalPower || item.Effect is RelicEffectModifyCardHealingPower)
                    .Select(item => new RelicMagicPowerModifier(item.Index,
                        ((RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(item.Effect)).GetParamInt(),
                        RelicCardModifierProbe.Conditions(item.Effect))).ToArray();
                var cardPlayMagicPower = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectModifyCardMagicPowerOnCardPlayed)
                    .Select(item => {
                        var data = (RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(item.Effect);
                        return new RelicCardPlayMagicPowerModifier(item.Index, data.GetParamInt(),
                            data.GetTraits().Select(trait => trait.GetTraitStateName()).ToArray(),
                            RelicCardModifierProbe.Conditions(item.Effect));
                    }).ToArray();
                var cardDrawOnCardPlay = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectCardDrawOnCardPlay)
                    .Select(item => {
                        var data = (RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(item.Effect);
                        SubtypeData? subtype = data.GetParamCharacterSubtype();
                        string? requiredSubtype = subtype == null || subtype.IsNone ? null : subtype.Key;
                        return new RelicCardDrawOnCardPlayModifier(item.Index, data.GetParamInt(),
                            data.GetParamCardType().ToString(), RelicCardModifierProbe.Conditions(item.Effect),
                            requiredSubtype: requiredSubtype, requiredTraits: data.GetTraits().Select(trait => trait.GetTraitStateName()).ToArray());
                    }).ToArray();
                var cardPlayRoomCapacity = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectModifyRoomCapacityOnCardPlay)
                    .Select(item => {
                        var data = (RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(item.Effect);
                        return new RelicCardPlayRoomCapacityModifier(item.Index, data.GetParamInt(), data.GetParamCardType().ToString());
                    }).ToArray();
                var roomAttachmentReplications = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectFirstRoomCardIsPlayedOnAllFloors)
                    .Select(item => {
                        var data = (RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(item.Effect);
                        return new RelicRoomAttachmentReplication(item.Index, data.GetParamInt2(),
                            RelicCardModifierProbe.Conditions(item.Effect));
                    }).ToArray();
                var moonPhaseHeals = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectHealOnMoonPhase)
                    .Select(item => {
                        object Field(string name) => AccessTools.Field(item.Effect.GetType(), name).GetValue(item.Effect);
                        Team.Type team = (Team.Type)Field("targetTeam");
                        var subtype = (SubtypeData)Field("characterSubtype");
                        var data = (RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(item.Effect);
                        return new RelicMoonPhaseHealRule(item.Index, team == Team.Type.None || team.HasFlag(Team.Type.Monsters),
                            team == Team.Type.None || team.HasFlag(Team.Type.Heroes), Field("targetMode").ToString(),
                            subtype == null || subtype.IsNone ? null : subtype.Key, (int)Field("healthAmount"),
                            (int)Field("moonPhaseMask"), data.GetEffectConditions().Count);
                    }).ToArray();
                return new CombatRelicState(relic.GetRelicDataID(), relic.GetAssetName(), effects.Select(effect => effect.GetType().Name).ToArray(),
                    statuses.Length == 0 ? null : statuses, relic is CovenantState,
                    relic.DisallowedInPlacementPhase, cardModifiers?.Length > 0 ? cardModifiers : null,
                    triggers.Length > 0 ? triggers : null, stackAdditions.Length > 0 ? stackAdditions : null,
                    magnitudes.Length > 0 ? magnitudes : null, decays.Length > 0 ? decays : null,
                    resources.Length > 0 ? resources : null, magicPower.Length > 0 ? magicPower : null,
                    cardPlayMagicPower.Length > 0 ? cardPlayMagicPower : null,
                    roomEntryStatuses.Length > 0 ? roomEntryStatuses : null,
                    unitUpgradeTriggers.Length > 0 ? unitUpgradeTriggers : null,
                    forgePointModifiers.Length > 0 ? forgePointModifiers : null,
                    cardHealthModifiers.Length > 0 ? cardHealthModifiers : null,
                    cardAttackModifiers.Length > 0 ? cardAttackModifiers : null,
                    cardDrawOnCardPlay.Length > 0 ? cardDrawOnCardPlay : null,
                    corruptionStatuses.Length > 0 ? corruptionStatuses : null,
                    cardTypeCosts.Length > 0 ? cardTypeCosts : null,
                    effects.Any(effect => effect is RelicEffectAdjustAttackByStatusEffect) ? StatusAttackModifiers(managers) : null,
                    cardPlayRoomCapacity.Length > 0 ? cardPlayRoomCapacity : null,
                    moonPhaseHeals.Length > 0 ? moonPhaseHeals : null,
                    roomAttachmentReplications.Length > 0 ? roomAttachmentReplications : null);
            }).ToArray();
        }

        private static RelicCountRole[] CountRoles(AllGameManagers managers)
        {
            var save = managers.GetSaveManager(); var collected = save.GetCollectedRelics();
            return Current(managers).Select(owner => new RelicCountRole(owner.GetRelicDataID(), collected.Contains(owner), owner.IsBossGivenRelic(), owner.IsSoulSaviorScalingRelic(),
                (save.GetMainClass()?.IsStarterRelic(owner.GetRelicDataID()) ?? false) || (save.GetSubClass()?.IsStarterRelic(owner.GetRelicDataID()) ?? false))).ToArray();
        }
        private static RelicUnitUpgradeTrigger UnitUpgradeTrigger(IRelicEffect effect, int index)
        {
            object Field(string name) => AccessTools.Field(effect.GetType(), name).GetValue(effect);
            var data = (CardUpgradeData?)Field("upgradeData");
            CardUpgradeModifier? upgrade = null;
            if (data != null)
            {
                var state = new CardUpgradeState(); state.Setup(data);
                upgrade = CardModifierProbe.Upgrade(state, false, filtersAlreadyApplied: true);
            }
            Team.Type sourceTeam = (Team.Type)Field("sourceTeam");
            var subtype = (SubtypeData)Field("characterSubtype");
            return new RelicUnitUpgradeTrigger(index, ((CharacterTriggerData.Trigger)Field("trigger")).ToString(),
                sourceTeam.HasFlag(Team.Type.Monsters), sourceTeam.HasFlag(Team.Type.Heroes), subtype?.Key,
                subtype == null || subtype.IsNone, (bool)Field("shouldRemove"), Convert.ToInt32(Field("upgradeLifetime")), upgrade);
        }

        private static RelicRoomEntryStatus RoomEntryStatus(IRelicEffect effect, int index)
        {
            object Field(string name) => AccessTools.Field(effect.GetType(), name).GetValue(effect);
            Team.Type team = (Team.Type)Field("targetTeamType");
            bool monsters = team.HasFlag(Team.Type.Monsters), heroes = team.HasFlag(Team.Type.Heroes);
            if (team == Team.Type.None) { monsters = true; heroes = true; }
            return new RelicRoomEntryStatus(index, (int)Field("targetRoomIndex"), monsters, heroes,
                (bool)Field("firstWaveOnly"), ((StatusEffectStackData[])Field("statusEffects"))
                    .Select(status => BattleActionProbe.Status(status.statusId, status.count)).ToArray());
        }

        private static RelicCorruptionStatus CorruptionStatus(IRelicEffect effect, int index)
        {
            var data = (RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(effect);
            Team.Type team = data.GetParamSourceTeam();
            if (team == Team.Type.None) team = Team.Type.Heroes | Team.Type.Monsters;
            return new RelicCorruptionStatus(index, data.GetTargetMode().ToString(),
                team.HasFlag(Team.Type.Monsters), team.HasFlag(Team.Type.Heroes), data.GetParamBool(),
                effect.CanApplyInPreviewMode, data.GetEffectConditions().Count > 0,
                data.GetParamStatusEffects().Select(status => BattleActionProbe.Status(status.statusId, status.count)).ToArray());
        }

        private static RelicStatusDecay StatusDecay(IRelicEffect effect, int index)
        {
            var data = (RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(effect);
            if (!effect.CanApplyInPreviewMode) throw new InvalidOperationException("Unexpected status decay preview restriction.");
            return new RelicStatusDecay(index, (int)data.GetParamSourceTeam(),
                data.GetParamStatusEffects().Select(status => status.statusId).ToArray(), RelicCardModifierProbe.Conditions(effect));
        }

        private static RelicStatusMagnitude StatusMagnitude(IRelicEffect effect, int index)
        {
            var data = (RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(effect);
            return new RelicStatusMagnitude(index, (int)data.GetParamSourceTeam(),
                data.GetParamStatusEffects().FirstOrDefault()?.statusId ?? "", data.GetParamInt(), RelicCardModifierProbe.Conditions(effect));
        }

        private static RelicStatusStacks StatusStacks(IRelicEffect effect, int index)
        {
            object Field(string name) => AccessTools.Field(effect.GetType(), name).GetValue(effect);
            return new RelicStatusStacks(index, Field("team").ToString(), (string)Field("statusId"),
                (int)Field("additionalStacks"), Convert.ToInt32(Field("optionalAddConditions")), RelicCardModifierProbe.Conditions(effect));
        }

        private static RelicCharacterTriggers AddedTriggers(IRelicEffect effect, int index)
        {
            object Field(string name) => AccessTools.Field(effect.GetType(), name).GetValue(effect);
            Team.Type team = (Team.Type)Field("sourceTeam");
            var subtype = (SubtypeData)Field("characterSubtype");
            var interactions = new List<string>();
            var data = (List<CharacterTriggerData>?)Field("triggerDataList");
            CombatTrigger[] triggers = (data ?? new List<CharacterTriggerData>())
                .Select(trigger => EnemySpawningProbe.TriggerDefinition(trigger, interactions)).ToArray();
            return new RelicCharacterTriggers(index, team.HasFlag(Team.Type.Monsters), team.HasFlag(Team.Type.Heroes),
                triggers, (bool)Field("noCountModifiersAllowed"), subtype?.Key, subtype == null || subtype.IsNone,
                subtype?.IsPyre == true, ((List<CharacterData>)Field("characterFilter"))
                    .Where(character => character != null).Select(character => character.GetAssetKey()).ToArray(),
                RelicCardModifierProbe.Conditions(effect), interactions);
        }

        private static RelicSpawnStatus SpawnStatus(IRelicEffect effect, int index)
        {
            object Field(string name) => AccessTools.Field(effect.GetType(), name).GetValue(effect);
            var conditions = RelicCardModifierProbe.Conditions(effect);
            bool applyToTeam = effect.GetType().Name == RelicModel.AddStatusOnTeamSpawn;
            Team.Type team = (Team.Type)Field("targetTeam");
            var subtype = (SubtypeData)Field("characterSubtype");
            bool allowFromCard = (bool)Field("allowFromCard"), onlyFromCard = (bool)Field("onlyAllowedFromCard");
            bool removal = effect.GetType().Name == RelicModel.RemoveStatusOnSpawn;
            return new RelicSpawnStatus(index, team.HasFlag(Team.Type.Monsters), team.HasFlag(Team.Type.Heroes),
                ((StatusEffectStackData[])Field("statusEffects")).Select(status => BattleActionProbe.Status(status.statusId, status.count)).ToArray(),
                subtype?.Key, subtype == null || subtype.IsNone, subtype?.IsPyre == true,
                ((SubtypeData[])Field("excludeCharacterSubtypes")).Select(item => item.Key).ToArray(),
                (bool)Field("restrictToRoom") ? (int?)Field("restrictedRoomIndex") : null, applyToTeam || removal ? 0 : (int)Field("hpPercentAsStacks"),
                onlyFromCard ? "OnlyFromCard" : allowFromCard ? "" : "NoCard", !applyToTeam && !removal && (bool)Field("requireGraft"),
                applyToTeam || removal ? System.Array.Empty<string>() : ((List<CharacterData>)Field("characterFilter"))
                    .Select(character => character == null ? "<null>" : character.name).ToArray(), conditions, applyToTeam);
        }

        internal static TriggerCountState? TriggerCounts(AllGameManagers managers)
        {
            if (!HasTriggerCounts(managers)) return null;
            RelicManager manager = managers.GetRelicManager();
            var modifiers = (Dictionary<CharacterTriggerData.Trigger, int>)AccessTools.Field(typeof(RelicManager), "triggerFireCountModifiers").GetValue(manager);
            var excluded = (List<CardTriggerType>)AccessTools.Field(typeof(RelicManager), "excludedCardTriggersFromCountModifiers").GetValue(manager);
            equipmentDefinitions ??= managers.GetSaveManager().GetAllGameData().GetAllCardData().Where(card => card != null)
                .GroupBy(card => card.GetID()).Select(group => group.First()).Select(card => new EquipmentTriggerDefinition(card.GetID(),
                    card.GetEffects().Where(effect => effect.GetEffectStateName() == "CardEffectAttachEquipment" && effect.GetParamCardUpgradeData() != null)
                        .SelectMany(effect => effect.GetParamCardUpgradeData().GetCharacterTriggerUpgrades()).Select(trigger => trigger.GetTrigger().ToString())
                        .Distinct().ToArray())).OrderBy(card => card.DataId, StringComparer.Ordinal).ToArray();
            return new TriggerCountState(modifiers.Select(item => new TriggerCountModifier(item.Key.ToString(), item.Value)).ToArray(),
                Enum.GetValues(typeof(CharacterTriggerData.Trigger)).Cast<CharacterTriggerData.Trigger>()
                    .Where(kind => manager.HasTriggerCountModifierRelicEffect(kind, Team.Type.Heroes)).Select(kind => kind.ToString()).ToArray(),
                excluded.Select(kind => kind.ToString()).ToArray(), equipmentDefinitions);
        }
    }
}
