using System;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class IndulgentTrampleCaptureProbe
    {
        internal const string GorgeScenario = "SoulSavior_R2_BossBattle_FavoredChild_1";
        internal const string CapacityScenario = "SoulSavior_R2_BossBattle_FavoredChild_2";
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");

        internal static bool Supports(ScenarioData? scenario, CharacterData data) =>
            scenario != null && typeof(CardState).Module.ModuleVersionId == Module &&
            ReferenceEquals(data, scenario.GetOuterTrainBoss(false)) &&
            ReferenceEquals(data, scenario.GetOuterTrainBoss(true)) &&
            MatchesScenarioOwner(scenario, data.GetAssetKey()) && MatchesDefinition(data);

        internal static bool MatchesScenarioOwner(ScenarioData scenario, string assetKey) =>
            scenario.GetEnemyRelicData() is [var owner] && assetKey switch
            {
                OuterBossDefinitions.IndulgentGorgeOnSlay => scenario.name == GorgeScenario &&
                    owner.GetID() == OuterBossDefinitions.IndulgentGorgeOwnerId &&
                    owner.name == OuterBossDefinitions.IndulgentGorgeOwnerKey && owner.GetEffects().Count == 0,
                OuterBossDefinitions.IndulgentReduceCapacity => scenario.name == CapacityScenario &&
                    owner.GetID() == OuterBossDefinitions.IndulgentCapacityOwnerId &&
                    owner.name == OuterBossDefinitions.IndulgentCapacityOwnerKey &&
                    owner.GetEffects() is [var description] && description.GetEffectClassName() == nameof(RelicEffectNull),
                _ => false
            };

        internal static bool MatchesDefinition(CharacterData? data)
        {
            if (data == null || data.GetHealth() != 1100 || data.GetSize() != 5 || !data.GetCanAttack() ||
                (int)data.GetValidBossAttackPhase() != -1 ||
                data.GetStartingStatusEffects() is not [{ statusId: "trample", count: 1 }] ||
                data.GetTriggers() is not [var feed, var additional] ||
                !StockTrigger(feed, CharacterTriggerData.Trigger.OnFeed) ||
                feed.GetEffects() is not [var grant] || !RageGrant(grant))
                return false;

            return data.GetAssetKey() switch
            {
                OuterBossDefinitions.IndulgentGorgeOnSlay => data.GetAttackDamage() == 13 &&
                    StockTrigger(additional, CharacterTriggerData.Trigger.OnKill) &&
                    additional.GetEffects() is [var replay] && FeedReplay(replay, 1),
                OuterBossDefinitions.IndulgentReduceCapacity => data.GetAttackDamage() == 12 &&
                    StockTrigger(additional, CharacterTriggerData.Trigger.PostCombat) &&
                    additional.GetEffects() is [var capacity, var replay] && CapacityReduction(capacity) && FeedReplay(replay, -1),
                _ => false
            };
        }

        private static bool StockTrigger(CharacterTriggerData trigger, CharacterTriggerData.Trigger kind) =>
            trigger.GetTrigger() == kind && !trigger.GetTriggerOnce() && !trigger.GetHideVisualAndIgnoreSilence() &&
            !trigger.GetOnlyTriggerIfEquipped() && trigger.GetTriggerAtThreshold() == 0 &&
            trigger.GetRequiredStatusEffects().Count == 0 && trigger.GetRequiredStatusEffectsForDyingCharacter().Count == 0;

        internal static bool RageGrant(CardEffectData effect) =>
            effect.GetEffectStateName() == nameof(CardEffectAddStatusEffect) && UnscaledEffect(effect, TargetMode.Self) &&
            effect.GetParamInt() == 0 && effect.GetParamStatusEffects() is [{ statusId: "buff", count: 3 }];

        internal static bool FeedReplay(CardEffectData effect, int value) =>
            effect.GetEffectStateName() == nameof(CardEffectPlayUnitTrigger) && UnscaledEffect(effect, TargetMode.Self) &&
            effect.GetParamInt() == value && effect.GetParamTrigger() == CharacterTriggerData.Trigger.OnFeed;

        private static bool CapacityReduction(CardEffectData effect) =>
            effect.GetEffectStateName() == nameof(CardEffectAdjustRoomCapacity) && UnscaledEffect(effect, TargetMode.Room) &&
            effect.GetParamInt() == -1;

        private static bool UnscaledEffect(CardEffectData effect, TargetMode target) =>
            effect.GetTargetMode() == target && effect.GetTargetTeamType() == (Team.Type.Heroes | Team.Type.Monsters) &&
            !effect.GetUseIntRange();
    }
}
