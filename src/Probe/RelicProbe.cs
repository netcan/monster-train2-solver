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
            Current(managers).Any(relic => relic.GetEffects().Any(effect => effect is RelicEffectModifyStatusMagnitude ||
                effect is RelicEffectPreventStatusEffectDecay decay && decay.GetStatusEffects().Any(status => status.statusId == "buff")));
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
            bool captureCardModifiers = RelicCardModifierProbe.Enabled ||
                Environment.GetEnvironmentVariable("MT2_PROBE_CARDLESS_RELIC_UPGRADES") == "1";
            return relics.Select(relic => {
                var effects = relic.GetEffects().ToArray();
                var statuses = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectAddStatusEffectOnSpawn).Select(item => SpawnStatus(item.Effect, item.Index)).ToArray();
                var cardModifiers = captureCardModifiers ? effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectAddTempUpgrade).Select(item => RelicCardModifierProbe.Definition(item.Effect, item.Index)).ToArray() : null;
                var triggers = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectAddTrigger).Select(item => AddedTriggers(item.Effect, item.Index)).ToArray();
                var stackAdditions = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectAddStatusEffectOnStatusApplied).Select(item => StatusStacks(item.Effect, item.Index)).ToArray();
                var magnitudes = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectModifyStatusMagnitude).Select(item => StatusMagnitude(item.Effect, item.Index)).ToArray();
                var decays = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => item.Effect is RelicEffectPreventStatusEffectDecay).Select(item => StatusDecay(item.Effect, item.Index)).ToArray();
                var resources = effects.Select((effect, index) => new { Effect = effect, Index = index })
                    .Where(item => RelicResourceModel.IsEffect(item.Effect.GetType().Name))
                    .Select(item => RelicResourceProbe.Definition(item.Effect, item.Index, managers)).ToArray();
                return new CombatRelicState(relic.GetRelicDataID(), relic.GetAssetName(), effects.Select(effect => effect.GetType().Name).ToArray(),
                    statuses.Length == 0 ? null : statuses, relic is CovenantState,
                    relic.DisallowedInPlacementPhase, cardModifiers?.Length > 0 ? cardModifiers : null,
                    triggers.Length > 0 ? triggers : null, stackAdditions.Length > 0 ? stackAdditions : null,
                    magnitudes.Length > 0 ? magnitudes : null, decays.Length > 0 ? decays : null,
                    resources.Length > 0 ? resources : null);
            }).ToArray();
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
                    .Select(character => character == null ? "<null>" : character.name).ToArray(),
                RelicCardModifierProbe.Conditions(effect), interactions);
        }

        private static RelicSpawnStatus SpawnStatus(IRelicEffect effect, int index)
        {
            object Field(string name) => AccessTools.Field(effect.GetType(), name).GetValue(effect);
            var conditions = RelicCardModifierProbe.Conditions(effect);
            Team.Type team = (Team.Type)Field("targetTeam");
            var subtype = (SubtypeData)Field("characterSubtype");
            bool allowFromCard = (bool)Field("allowFromCard"), onlyFromCard = (bool)Field("onlyAllowedFromCard");
            return new RelicSpawnStatus(index, team.HasFlag(Team.Type.Monsters), team.HasFlag(Team.Type.Heroes),
                ((StatusEffectStackData[])Field("statusEffects")).Select(status => BattleActionProbe.Status(status.statusId, status.count)).ToArray(),
                subtype?.Key, subtype == null || subtype.IsNone, subtype?.IsPyre == true,
                ((SubtypeData[])Field("excludeCharacterSubtypes")).Select(item => item.Key).ToArray(),
                (bool)Field("restrictToRoom") ? (int?)Field("restrictedRoomIndex") : null, (int)Field("hpPercentAsStacks"),
                onlyFromCard ? "OnlyFromCard" : allowFromCard ? "" : "NoCard", (bool)Field("requireGraft"),
                ((List<CharacterData>)Field("characterFilter")).Select(character => character == null ? "<null>" : character.name).ToArray(), conditions);
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
