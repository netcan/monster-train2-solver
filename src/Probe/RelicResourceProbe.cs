using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    // These observations contain raw balance/save/definition inputs as well as
    // independent native results. Acquisitions precede battle/card projection.
    internal static class RelicResourceProbe
    {
        internal sealed class ResourceSnapshot
        {
            public BattleResourceState Resources { get; set; } = null!;
            public CombatRelicState[] Relics { get; set; } = null!;
            public int Energy { get; set; }
            public int Draw { get; set; }
            public int PyreHP { get; set; }
            public int MaxHP { get; set; }
            public int PyreAttack { get; set; }
            public int EnergyModification { get; set; }
            public int DeploymentModification { get; set; }
            public string Phase { get; set; } = "";
            public string MoonPhase { get; set; } = "";
            public int Turn { get; set; }
            public bool Preview { get; set; }
            public int Gold { get; set; }
            public int Capacity { get; set; }
        }

        internal sealed class Operation
        {
            public string Kind { get; set; } = "";
            public string OwnerDataId { get; set; } = "";
            public int EffectIndex { get; set; } = -1;
            public bool Deployment { get; set; }
            public int Notifications { get; set; }
            public ResourceSnapshot Before { get; set; } = null!;
            public ResourceSnapshot? After { get; set; }
        }

        internal static readonly List<Operation> Operations = new List<Operation>();
        private static Operation? energyOperation;
        private static bool dragonHoardResourceObserved;

        private static RelicState[] Current(AllGameManagers managers)
        {
            var relics = new List<RelicState>();
            AccessTools.Method(typeof(RelicManager), "GetCurrentRelics").Invoke(managers.GetRelicManager(), new object[] { relics });
            return relics.ToArray();
        }

        internal static BattleResourceState? CaptureResources(AllGameManagers managers)
        {
            bool dragonHoardCards = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .SelectMany(card => card.GetEffectStates())
                .Any(effect => effect.GetCardEffect() is IDragonsHoardCardEffect ||
                    effect.GetCardEffect() is CardEffectRewardGoldPerDragonHoard);
            dragonHoardResourceObserved |= dragonHoardCards;
            if (!OriginalResourceRelicBatchScenario.Enabled && !PyreResourceRefreshScenario.Enabled && !OriginalFloorCapacityRelicScenario.Enabled &&
                !OriginalMoonPhaseEnergyRelicScenario.Enabled &&
                !Current(managers).Any(relic => relic.GetEffects().Any(effect => RelicResourceModel.IsEffect(effect.GetType().Name))) &&
                !dragonHoardResourceObserved)
                return null;
            SaveManager save = managers.GetSaveManager();
            BalanceData balance = save.GetBalanceData();
            CharacterData character = save.GetActiveSavePyreCharacterData();
            if (character == null) throw new InvalidOperationException("Missing native resource Pyre definition.");
            PyreHeartData pyre = character.GetPyreHeartData();
            var rawSave = (SaveData)AccessTools.Property(typeof(SaveManager), "ActiveSaveData").GetValue(save);
            if (rawSave == null) throw new InvalidOperationException("Missing native raw resource save.");
            return new BattleResourceState(balance.GetStartOfTurnEnergy(), balance.GetStartOfDeploymentPhaseEnergy(),
                balance.GetStartOfTurnCards(), balance.GetMaxEnergy(), balance.GetMaxHandSize(), character.GetID(),
                pyre.GetAttack(), rawSave.GetMaxTowerHP(), save.GetRelicPersistentState()?.GetPyreDamageIncrease() ?? 0,
                rawSave.GetTowerModifiedBaseAttack(),
                ((int[])AccessTools.Field(typeof(PyreHeartData), "upgradeAttack").GetValue(pyre)).ToArray(),
                ((int[])AccessTools.Field(typeof(PyreHeartData), "upgradeHP").GetValue(pyre)).ToArray(),
                rawSave.GetMutatorIDs().ToArray(),
                ((MutableRules)AccessTools.Field(typeof(SaveManager), "mutableRules").GetValue(save))
                    .GetRule(MutableRules.Condition.GoldRewardMultiplier, 1f),
                baseFloorCapacity: balance.GetCharacterCapacityPerFloor(), dragonsHoardCap: save.GetDragonsHoardCap());
        }

        internal static RelicResourceEffect Definition(IRelicEffect effect, int index, AllGameManagers managers)
        {
            var data = (RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(effect);
            string type = effect.GetType().Name;
            int amount;
            float? value = null;
            int? registration = null;
            float? capacityValue = null;
            int? capacityRegistration = null;
            MutatorData? required = null;
            if (effect is RelicEffectChangeStartingHandSize draw)
            {
                value = draw.Value;
                amount = UnityEngine.Mathf.RoundToInt(draw.Value);
                var rules = (MutableRules?)AccessTools.Field(typeof(SaveManager), "mutableRules").GetValue(managers.GetSaveManager());
                if (rules != null)
                {
                    var map = (Dictionary<MutableRules.Condition, List<IBalanceEditingRelicEffect>>)
                        AccessTools.Field(typeof(MutableRules), "ruleMap").GetValue(rules);
                    if (map.TryGetValue(MutableRules.Condition.ChangeStartingHandSize, out var list))
                    {
                        int order = list.FindIndex(item => ReferenceEquals(item, effect));
                        if (order >= 0) registration = order;
                    }
                }
            }
            else if (effect is RelicEffectChangeFloorCapacity capacity)
            {
                capacityValue = capacity.Value;
                amount = UnityEngine.Mathf.RoundToInt(capacity.Value);
                var rules = (MutableRules?)AccessTools.Field(typeof(SaveManager), "mutableRules").GetValue(managers.GetSaveManager());
                if (rules != null)
                {
                    var map = (Dictionary<MutableRules.Condition, List<IBalanceEditingRelicEffect>>)
                        AccessTools.Field(typeof(MutableRules), "ruleMap").GetValue(rules);
                    if (map.TryGetValue(MutableRules.Condition.AdditionalCharactersPerFloor, out var list))
                    {
                        int order = list.FindIndex(item => ReferenceEquals(item, effect));
                        if (order >= 0) capacityRegistration = order;
                    }
                }
            }
            else if (effect is RelicEffectIncreasePyreAttack || effect is RelicEffectIncreasePyreHealth)
            {
                amount = (int)AccessTools.Field(effect.GetType(), effect is RelicEffectIncreasePyreAttack ? "_attackAmount" : "_healthAmount").GetValue(effect);
                required = (MutatorData?)AccessTools.Field(effect.GetType(), "_requiredMutator").GetValue(effect);
            }
            else
                amount = (int)AccessTools.Field(effect.GetType(), "energyChange").GetValue(effect);
            return new RelicResourceEffect(index, type, data.GetParamInt(), data.GetParamInt2(), amount,
                effect.CanApplyInPreviewMode, required?.GetID(), RelicCardModifierProbe.Conditions(effect), value, registration,
                capacityValue, capacityRegistration);
        }

        internal static ResourceSnapshot Capture()
        {
            AllGameManagers managers = AllGameManagers.Instance!;
            SaveManager save = managers.GetSaveManager();
            CombatManager? combat = managers.GetCombatManager();
            return new ResourceSnapshot
            {
                Resources = CaptureResources(managers)!, Relics = RelicProbe.Capture(managers),
                Energy = managers.GetPlayerManager().GetEnergy(), Draw = save.GetStartOfTurnCards(),
                PyreHP = save.GetTowerHP(), MaxHP = save.GetMaxTowerHP(), PyreAttack = save.GetBasePyreAttack(),
                EnergyModification = managers.GetRelicManager().GetEnergyPerTurnModification(),
                DeploymentModification = managers.GetRelicManager().GetEnergyPerDeploymentPhaseModification(),
                Phase = combat?.GetCombatPhase().ToString() ?? "UNUSED", Turn = combat?.GetTurnCount() ?? 0,
                MoonPhase = managers.GetPlayerManager().CurrentMoonPhase.ToString(),
                Preview = save.PreviewMode, Gold = save.GetGold(), Capacity = save.GetCharacterCapacityPerFloor()
            };
        }

        private static IEnumerator WrapEnergy(IEnumerator native, IRelicEffect effect, bool deployment, string kind = "Energy")
        {
            Operation? record = null;
            try
            {
                var managers = AllGameManagers.Instance!;
                RelicState owner = Current(managers).Single(relic => relic.GetEffects().Any(item => ReferenceEquals(item, effect)));
                record = new Operation { Kind = kind, OwnerDataId = owner.GetRelicDataID(), Deployment = deployment,
                    EffectIndex = owner.GetEffects().FindIndex(item => ReferenceEquals(item, effect)), Before = Capture() };
                Operations.Add(record);
                energyOperation = record;
            }
            catch (Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
            // The callback's body executes in MoveNext, not its iterator factory.
            while (native.MoveNext()) yield return native.Current;
            energyOperation = null;
            if (record != null)
            {
                try { record.After = Capture(); }
                catch (Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
            }
        }

        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.AddRelic))]
        private static class AcquirePatch
        {
            private static void Prefix(RelicData relicData, out Operation? __state)
            {
                __state = null;
                if (!(OriginalResourceRelicBatchScenario.Enabled && OriginalResourceRelicBatchScenario.Artifacts.Any(item => item.Id == relicData.GetID())) &&
                    !(PyreResourceRefreshScenario.Enabled && PyreResourceRefreshScenario.Artifacts.Any(item => item.Id == relicData.GetID())) &&
                    !(OriginalFloorCapacityRelicScenario.Enabled && OriginalFloorCapacityRelicScenario.Artifacts.Any(item => item.Id == relicData.GetID())) &&
                    !(OriginalMoonPhaseEnergyRelicScenario.Enabled && relicData.GetID() == OriginalMoonPhaseEnergyRelicScenario.DataId)) return;
                try
                {
                    __state = new Operation { Kind = "Acquire", OwnerDataId = relicData.GetID(), Before = Capture() };
                    Operations.Add(__state);
                }
                catch (Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
            }
            private static void Postfix(Operation? __state)
            {
                if (__state == null) return;
                try { __state.After = Capture(); }
                catch (Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
            }
        }

        [HarmonyPatch(typeof(RelicEffectModifyEnergy), nameof(RelicEffectModifyEnergy.ApplyEffectTurnPhaseTiming))]
        private static class EnergyPatch
        {
            private static void Postfix(RelicEffectModifyEnergy __instance, ref IEnumerator __result)
            { if (OriginalResourceRelicBatchScenario.Prepared || PyreResourceRefreshScenario.Prepared || OriginalFloorCapacityRelicScenario.Prepared) __result = WrapEnergy(__result, __instance, false); }
        }

        [HarmonyPatch(typeof(RelicEffectModifyEnergyDuringDeployment), nameof(RelicEffectModifyEnergyDuringDeployment.ApplyEffectTurnPhaseTiming))]
        private static class DeploymentPatch
        {
            private static void Postfix(RelicEffectModifyEnergyDuringDeployment __instance, ref IEnumerator __result)
            { if (OriginalResourceRelicBatchScenario.Prepared || PyreResourceRefreshScenario.Prepared || OriginalFloorCapacityRelicScenario.Prepared) __result = WrapEnergy(__result, __instance, true); }
        }

        [HarmonyPatch(typeof(RelicEffectModifyEnergyOnMoonPhase), nameof(RelicEffectModifyEnergyOnMoonPhase.ApplyEffectMoonPhase))]
        private static class MoonPhaseEnergyPatch
        {
            private static void Postfix(RelicEffectModifyEnergyOnMoonPhase __instance, ref IEnumerator __result)
            {
                if (OriginalMoonPhaseEnergyRelicScenario.Prepared)
                    __result = WrapEnergy(__result, __instance, false, "MoonPhaseEnergy");
            }
        }

        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggered))]
        private static class NotifyPatch
        {
            private static void Prefix(IRelicEffect triggeredEffect)
            {
                if (energyOperation != null && RelicResourceModel.IsEffect(triggeredEffect.GetType().Name))
                    energyOperation.Notifications++;
            }
        }
    }
}
