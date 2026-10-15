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
        private static bool trainHealResourceObserved;

        private static RelicState[] Current(AllGameManagers managers)
        {
            var relics = new List<RelicState>();
            AccessTools.Method(typeof(RelicManager), "GetCurrentRelics").Invoke(managers.GetRelicManager(), new object[] { relics });
            return relics.ToArray();
        }

        internal static BattleResourceState? CaptureResources(AllGameManagers managers)
        {
            // Relics can be acquired before battle creates the card manager.
            // Resource balance/save inputs already exist; there are no live cards
            // to inspect for Hoard or train-healing effects at that boundary.
            CardManager? cards = managers.GetCardManager();
            CardEffectState[] cardEffects = cards == null ? Array.Empty<CardEffectState>() :
                cards.GetAllCards(new List<CardState>()).SelectMany(card => card.GetEffectStates()).ToArray();
            bool dragonHoardCards = cardEffects
                .Any(effect => effect.GetCardEffect() is IDragonsHoardCardEffect ||
                    effect.GetCardEffect() is CardEffectRewardGoldPerDragonHoard);
            bool trainHealCards = cardEffects.Any(effect => effect.GetCardEffect() is CardEffectHealTrain);
            dragonHoardResourceObserved |= dragonHoardCards;
            trainHealResourceObserved |= trainHealCards;
            if (!OriginalResourceRelicBatchScenario.Enabled && !PyreResourceRefreshScenario.Enabled && !OriginalFloorCapacityRelicScenario.Enabled &&
                !OriginalMoonPhaseEnergyRelicScenario.Enabled &&
                !Current(managers).Any(relic => relic.GetEffects().Any(effect => RelicResourceModel.IsEffect(effect.GetType().Name))) &&
                !dragonHoardResourceObserved && !trainHealResourceObserved)
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
                baseFloorCapacity: balance.GetCharacterCapacityPerFloor(), dragonsHoardCap: save.GetDragonsHoardCap(),
                towerHealingDisabled: save.GetRelicModifiedRules().GetTowerHealingDisabled());
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
            CardGenerationRule? generation = null;
            RelicUnitResourceRule? unitTrigger = null;
            RelicOpeningDrawRule? openingDraw = null;
            if (effect is RelicEffectDrawOnPreOpeningDraw)
            {
                var upgradeData = data.GetParamCardUpgradeData(); CardUpgradeModifier? upgrade = null;
                if (upgradeData != null) { var state = new CardUpgradeState(); state.Setup(upgradeData); upgrade = CardModifierProbe.Upgrade(state); }
                var first = data.GetParamCardFilter(); var second = data.GetParamCardFilterSecondary();
                openingDraw = new RelicOpeningDrawRule(first == null ? null : CardUpgradeMaskProbe.Rule(first),
                    second == null ? null : CardUpgradeMaskProbe.Rule(second), data.GetParamBool(), upgrade);
                amount = data.GetParamInt();
            }
            else if (RelicUnitResourceModel.IsEffect(type))
            {
                var subtype = data.GetParamCharacterSubtype();
                unitTrigger = new RelicUnitResourceRule(data.GetParamTrigger().ToString(), data.GetParamSourceTeam().ToString(),
                    effect is RelicEffectEmberOnUnitTrigger && subtype != null && !subtype.IsNone ? subtype.Key : null,
                    effect is RelicEffectGoldOnTrigger ? data.GetParamStatusEffects().Select(status => status.statusId).ToArray() : Array.Empty<string>());
                amount = data.GetParamInt();
            }
            else if (effect is RelicEffectAddBattleCardToHand)
            {
                var upgradeData = data.GetParamCardUpgradeData();
                CardUpgradeModifier? upgrade = null;
                if (upgradeData != null) { var state = new CardUpgradeState(); state.Setup(upgradeData); upgrade = CardModifierProbe.Upgrade(state); }
                generation = new CardGenerationRule("HandPile", 1,
                    (data.GetParamCardPool()?.GetFilteredChoices() ?? new List<CardData>()).Select(CardGenerationProbe.Creation).ToArray(), upgrade: upgrade);
                amount = data.GetParamInt();
            }
            else if (effect is RelicEffectDrawToMaxStartOfCombat)
                amount = data.GetParamInt();
            else if (effect is RelicEffectChangeStartingHandSize || effect is RelicEffectAdditionalDrawOnTowerFullHealth)
            {
                var draw = (IBalanceEditingRelicEffect)effect;
                value = draw.Value;
                amount = effect is RelicEffectAdditionalDrawOnTowerFullHealth ? data.GetParamInt() : UnityEngine.Mathf.RoundToInt(draw.Value);
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
            else if (effect is RelicEffectAdditionalSmeltEmber)
                amount = (int)AccessTools.Field(effect.GetType(), "_additionalSmeltEmber").GetValue(effect);
            else if (effect is RelicEffectAdjustMixAmount)
                amount = (int)AccessTools.Field(effect.GetType(), "additionalAmount").GetValue(effect);
            else if (effect is RelicEffectConserveForgePoints)
                amount = (int)AccessTools.Field(effect.GetType(), "_value").GetValue(effect);
            else if (effect is RelicEffectModifyMaxCorruption || effect is RelicEffectModifyCorruptionStartOfCombat)
                amount = (int)AccessTools.Field(effect.GetType(), "corruptionChange").GetValue(effect);
            else if (effect is RelicEffectAddPermanentCorruptionStartOfCombat)
                amount = (int)AccessTools.Field(effect.GetType(), "addPermanentAmount").GetValue(effect);
            else if (effect is RelicEffectConserveEnergyEndOfTurn conserve)
            {
                IConserveEnergyEndOfTurnRelicEffect.ConserveParamaters parameters = conserve.GetConserveParameters();
                if (parameters.Mode != IConserveEnergyEndOfTurnRelicEffect.ConserveMode.All || parameters.Amount.HasValue)
                    throw new InvalidOperationException("Unexpected native end-of-turn conservation mode.");
                amount = 0;
            }
            else
                amount = (int)AccessTools.Field(effect.GetType(), "energyChange").GetValue(effect);
            return new RelicResourceEffect(index, type, data.GetParamInt(), data.GetParamInt2(), amount,
                effect.CanApplyInPreviewMode, required?.GetID(), RelicCardModifierProbe.Conditions(effect), value, registration,
                capacityValue, capacityRegistration, data.GetParamMinInt(),
                effect is RelicEffectAddPermanentCorruptionStartOfCombat ? data.GetParamFloat() : 0, generation, unitTrigger, openingDraw);
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
                    !(OriginalMoonPhaseEnergyRelicScenario.Enabled && relicData.GetID() == OriginalMoonPhaseEnergyRelicScenario.DataId) &&
                    !(OriginalStartCombatEnergyScenario.Enabled && relicData.GetID() == OriginalStartCombatEnergyScenario.DataId)) return;
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

        [HarmonyPatch(typeof(RelicEffectModifyEnergyStartOfCombat), nameof(RelicEffectModifyEnergyStartOfCombat.ApplyEffectStartOfBattle))]
        private static class StartCombatEnergyPatch
        {
            private static void Postfix(RelicEffectModifyEnergyStartOfCombat __instance, ref IEnumerator __result)
            {
                if (OriginalStartCombatEnergyScenario.Acquired)
                    __result = WrapEnergy(__result, __instance, AllGameManagers.Instance!.GetCombatManager()!.IsPlacementPhase, "StartCombatEnergy");
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
