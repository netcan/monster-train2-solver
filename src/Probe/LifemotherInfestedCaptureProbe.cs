using System;
using System.Linq;
using MonsterTrain2Poju.Model;
using DLC = ShinyShoe.DLC;

namespace MonsterTrain2Poju.Probe
{
    internal static class LifemotherInfestedCaptureProbe
    {
        private const string Scenario = "SoulSavior_R5_BossBattle_Lifemother_Infested";
        private const string OwnerKey = "Boss_Railforged_Lifemother_Infested";
        private const string OwnerId = "72f9f4e3-df23-4954-99e6-9040ab573590";
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");

        internal static bool Supports(SaveManager? save, CharacterData data)
        {
            if (save is not { InTestScenario: false } || typeof(CardState).Module.ModuleVersionId != Module ||
                save.GetAscensionLevel() != 0 || save.GetCovenants().Count != 0 || !save.IsDlcInstalled(DLC.Railforged) ||
                save.GetCurrentScenarioData() is not ScenarioData scenario)
                return false;
            return MatchesDefinition(scenario, data);
        }

        internal static bool MatchesDefinition(ScenarioData scenario, CharacterData data)
        {
            if (scenario.name != Scenario ||
                !ReferenceEquals(data, scenario.GetOuterTrainBoss(false)) ||
                !ReferenceEquals(data, scenario.GetOuterTrainBoss(true)) ||
                scenario.GetEnemyRelicData() is not [var owner] || owner.name != OwnerKey || owner.GetID() != OwnerId ||
                owner.GetEffects().Count != 4 || owner.GetEffects().Any(effect => effect.GetEffectClassName() != nameof(RelicEffectNull)))
                return false;

            return data.GetAssetKey() == OuterBossDefinitions.LifemotherInfested &&
                data.GetAttackDamage() == 40 && data.GetHealth() == 9000 && data.GetSize() == 5 && data.GetCanAttack() &&
                (int)data.GetValidBossAttackPhase() == -1 &&
                data.GetStartingStatusEffects() is [{ statusId: "trample", count: 1 }] &&
                data.GetTriggers() is [var death, var turn] &&
                StockTrigger(death, CharacterTriggerData.Trigger.OnAnyUnitDeathOnFloor) &&
                death.GetEffects() is [var grant] && grant.GetEffectStateName() == nameof(CardEffectAddStatusEffect) &&
                SelfEffect(grant, Team.Type.Monsters) &&
                grant.GetParamStatusEffects() is [{ statusId: "hivemind", count: 2 }] &&
                StockTrigger(turn, CharacterTriggerData.Trigger.OnTeamTurnBegin) &&
                turn.GetEffects() is [var remove] && remove.GetEffectStateName() == nameof(CardEffectRemoveStatusEffect) &&
                SelfEffect(remove, Team.Type.Heroes | Team.Type.Monsters) &&
                remove.GetParamStatusEffects() is [{ statusId: "dazed", count: 5 }];
        }

        private static bool StockTrigger(CharacterTriggerData trigger, CharacterTriggerData.Trigger kind) =>
            trigger.GetTrigger() == kind && !trigger.GetTriggerOnce() && !trigger.GetHideVisualAndIgnoreSilence() &&
            !trigger.GetOnlyTriggerIfEquipped() && trigger.GetTriggerAtThreshold() == 0 && trigger.GetRemoveOnRelentlessChange() &&
            trigger.GetRequiredStatusEffects().Count == 0 && trigger.GetRequiredStatusEffectsForDyingCharacter().Count == 0;

        private static bool SelfEffect(CardEffectData effect, Team.Type team) =>
            effect.GetTargetMode() == TargetMode.Self && effect.GetTargetTeamType() == team &&
            effect.GetParamInt() == 0 && !effect.GetUseIntRange() && !effect.GetUseStatusEffectStackMultiplier() &&
            !effect.GetUseHealthMissingStackMultiplier() && !effect.GetUseMagicPowerMultiplier();
    }
}
