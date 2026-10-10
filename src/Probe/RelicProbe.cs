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
            Current(managers).Any(relic => relic.GetEffects().Any(effect => effect is RelicEffectModifyTriggerCount));
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
                return new CombatRelicState(relic.GetRelicDataID(), relic.GetAssetName(), effects.Select(effect => effect.GetType().Name).ToArray(),
                    statuses.Length == 0 ? null : statuses, relic is CovenantState,
                    relic.DisallowedInPlacementPhase, cardModifiers?.Length > 0 ? cardModifiers : null);
            }).ToArray();
        }

        private static RelicSpawnStatus SpawnStatus(IRelicEffect effect, int index)
        {
            object Field(string name) => AccessTools.Field(effect.GetType(), name).GetValue(effect);
            var conditions = ((System.Collections.Generic.IEnumerable<RelicEffectCondition>)Field("_effectConditions")).Select(condition => {
                object C(string name) => AccessTools.Field(typeof(RelicEffectCondition), name).GetValue(condition);
                bool trackCount = (bool)C("paramTrackTriggerCount");
                var input = new CardStatistics.StatValueData {
                    trackedValue = (CardStatistics.TrackedValueType)C("paramTrackedValue"),
                    entryDuration = (CardStatistics.EntryDuration)C("paramEntryDuration"),
                    cardTypeTarget = (CardStatistics.CardTypeTarget)C("paramCardType"),
                    paramSubtype = SubtypeManager.GetSubtypeData((string)C("paramSubtype")) };
                CardStatisticQuery query = trackCount ? new CardStatisticQuery(input.trackedValue.ToString(), input.entryDuration.ToString()) :
                    DamageScalingProbe.Query(input, 0, false);
                return new RelicConditionState(query, trackCount, Convert.ToInt32(C("paramComparator")), (int)C("paramInt"),
                    (bool)C("allowMultipleTriggersPerDuration"), (bool)C("triggered"), (int)C("valueAtLastTrigger"), (int)C("durationTriggerCount"));
            }).ToArray();
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
