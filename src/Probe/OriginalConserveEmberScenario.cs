using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalConserveEmberScenario
    {
        internal const string Name = "original-conserve-ember-relic";
        internal const string AssetKey = "ConserveEmber";
        internal const string DataId = "3e200d1f-7ba9-4a6e-a09d-5625f25b01ac";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }

        internal sealed class EnergyRemoval
        {
            public int Turn { get; set; }
            public bool Placement { get; set; }
            public bool Preview { get; set; }
            public int RemainingPlayerEnergy { get; set; }
            public int PlayerEnergyBefore { get; set; }
            public int PlayerEnergyAfter { get; set; }
            public int RemovedEnergy { get; set; }
            public int Notifications { get; set; }
            public bool Completed { get; set; }
        }

        internal static readonly List<EnergyRemoval> EnergyRemovals = new List<EnergyRemoval>();
        private static EnergyRemoval? currentRemoval;
        private static ManualLogSource? logger;

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("ConserveEmber must be acquired before native battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == DataId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == DataId))
                throw new InvalidOperationException("ConserveEmber is already present.");
            logger = log;
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-CONSERVE-EMBER-ACQUIRED unchanged ConserveEmber; native starting deck, Balance, Pyre, boss and waves retained.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            SaveManager save = managers.GetSaveManager();
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0 ||
                save.GetCollectedRelics().Count(item => item.GetRelicDataID() == DataId) != 1)
                throw new InvalidOperationException("ConserveEmber missed the native deployment boundary.");
            CheckOriginal(save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == DataId));
            Prepared = true;
            log.LogInfo("ORIGINAL-CONSERVE-EMBER-PREPARED bank deployment and turn one, then spend positive-cost native cards while retaining at least one energy; " +
                "initial=" + managers.GetPlayerManager().GetEnergy() + ", maximum=" + save.GetBalanceData().GetMaxEnergy() + ".");
        }

        // The independent fixture checker duplicates this policy from the current
        // decision alone. No native counters, recorded plays or future states enter it.
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (source.Spawn.Turn < 2 || source.Energy <= 1 || source.PlayRules == null) return null;
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source, card =>
            {
                CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card.DataId);
                return rule != null && rule.CostType == "Default" && rule.Cost > 0 && rule.Cost < source.Energy;
            });
        }

        private static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            if (relic.GetID() != DataId || relic.name != AssetKey || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != "RelicEffectConserveEnergyEndOfTurn" ||
                effect.GetParamInt() != 0 || effect.GetParamInt2() != 0 || effect.GetParamFloat() != 0 ||
                effect.GetParamBool() || effect.GetParamBool2() || effect.GetParamBool3() || effect.GetParamBool4() ||
                effect.GetParamSourceTeam() != Team.Type.None || effect.GetEffectConditions().Count != 0 ||
                effect.GetParamCardUpgradeData() != null || effect.GetParamCardFilter() != null ||
                effect.GetParamMutatorData() != null || (effect.GetParamStatusEffects()?.Length ?? 0) != 0)
                throw new InvalidOperationException("Original ConserveEmber identity or effect payload changed.");
        }

        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.GetEnergyToRemoveAtTurnEnd))]
        private static class EnergyRemovalPatch
        {
            private static void Prefix(int remainingPlayerEnergy, out EnergyRemoval? __state)
            {
                __state = null;
                if (!Enabled || !Prepared || FullBattleTrace.Active == null) return;
                try
                {
                    AllGameManagers managers = AllGameManagers.Instance!;
                    __state = new EnergyRemoval
                    {
                        Turn = managers.GetCombatManager()!.GetTurnCount(),
                        Placement = managers.GetCombatManager()!.IsPlacementPhase,
                        Preview = managers.GetSaveManager().PreviewMode,
                        RemainingPlayerEnergy = remainingPlayerEnergy,
                        PlayerEnergyBefore = managers.GetPlayerManager().GetEnergy()
                    };
                    EnergyRemovals.Add(__state);
                    currentRemoval = __state;
                }
                catch (Exception error) { FullBattleTrace.Active.CaptureFailure(error); }
            }

            private static void Postfix(int __result, EnergyRemoval? __state)
            {
                if (__state == null) return;
                currentRemoval = null;
                try
                {
                    __state.RemovedEnergy = __result;
                    __state.PlayerEnergyAfter = AllGameManagers.Instance!.GetPlayerManager().GetEnergy();
                    __state.Completed = true;
                    logger?.LogInfo("ORIGINAL-CONSERVE-EMBER-REMOVAL turn=" + __state.Turn +
                        " remaining=" + __state.RemainingPlayerEnergy + " removed=" + __result +
                        " notifications=" + __state.Notifications + " preview=" + __state.Preview + ".");
                }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }

        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggered))]
        private static class NotifyPatch
        {
            private static void Prefix(IRelicEffect triggeredEffect)
            {
                if (currentRemoval != null && triggeredEffect is RelicEffectConserveEnergyEndOfTurn)
                    currentRemoval.Notifications++;
            }
        }
    }
}
