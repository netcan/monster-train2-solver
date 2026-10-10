using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicCardModifierScenario
    {
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (!RelicCardModifierProbe.Enabled || Prepared) return;
            var save = managers.GetSaveManager();
            var original = save.GetAllGameData().GetAllCollectableRelicData().Single(relic => relic.name == "ReduceStarterCost");
            var cards = managers.GetCardManager()!;
            var owned = cards.GetAllCards(new List<CardState>());
            // The generation fixture creates copies of its casting spell. Keep
            // that authored test spell paid after the unchanged original relic.
            var generators = owned.Where(card => card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectAddBattleCard"))
                .Select(card => card.GetCardDataID()).Distinct().ToArray();
            foreach (string id in generators)
            {
                var definition = save.GetAllGameData().FindCardData(id)!;
                AccessTools.Field(typeof(CardData), "cost").SetValue(definition, 2);
                foreach (var card in owned.Where(card => card.GetCardDataID() == id)) AccessTools.Field(typeof(CardState), "cost").SetValue(card, 2);
            }
            save.AddRelic(original);
            var filters = original.GetEffects().Single().GetParamCardUpgradeData().GetFilters();
            var eligible = owned.Where(card => filters.All(filter => filter.FilterCard(card, managers.GetRelicManager()))).Take(2).ToArray();
            if (eligible.Length != 2) throw new InvalidOperationException("Relic scenario lacks eligible repeated/reset owned cards.");
            CaptureReady = true;
            foreach (var card in eligible)
            {
                managers.GetRelicManager().ApplyCardStateModifiers(card, resetTempCardModifiers: false);
                managers.GetRelicManager().ApplyCardStateModifiers(card);
            }
            if (Environment.GetEnvironmentVariable("MT2_PROBE_CONDITIONAL_BATTLE_RELIC_UPGRADES") == "1")
                InstallOncePerTurnCondition(managers, eligible[0]);
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("RELIC-CARD-MODIFIERS-PREPARED unchanged original ReduceStarterCost, native acquire/repeat/reset and battle-generated cards; conditional=" +
                (Environment.GetEnvironmentVariable("MT2_PROBE_CONDITIONAL_BATTLE_RELIC_UPGRADES") == "1") + ".");

            static void InstallOncePerTurnCondition(AllGameManagers managers, CardState sample)
            {
                var current = new List<RelicState>();
                AccessTools.Method(typeof(RelicManager), "GetCurrentRelics").Invoke(managers.GetRelicManager(), new object[] { current });
                RelicEffectAddTempUpgrade effect = current.Single(relic => relic.GetAssetName() == "ReduceStarterCost")
                    .GetEffects().OfType<RelicEffectAddTempUpgrade>().Single();
                var condition = new RelicEffectCondition();
                void Set(string name, object value) => AccessTools.Field(typeof(RelicEffectCondition), name).SetValue(condition, value);
                Set("paramTrackedValue", CardStatistics.TrackedValueType.TimesPlayed);
                Set("paramCardType", CardStatistics.CardTypeTarget.Any);
                Set("paramTrackTriggerCount", true);
                Set("paramEntryDuration", CardStatistics.EntryDuration.ThisTurn);
                Set("paramComparator", RelicEffectCondition.Comparator.LessThan);
                Set("paramInt", 1);
                Set("allowMultipleTriggersPerDuration", true);
                ((List<RelicEffectCondition>)AccessTools.Field(typeof(RelicEffectBase), "_effectConditions").GetValue(effect)).Add(condition);
                managers.GetRelicManager().ApplyCardStateModifiers(sample, resetTempCardModifiers: false);
                managers.GetRelicManager().ApplyCardStateModifiers(sample, resetTempCardModifiers: false);
            }
        }
    }
}
