using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicAfterSpawnChanceRule
    {
        public int EffectIndex { get; }
        public int ChancePercent { get; }
        public int RoomRestriction { get; }
        public string TargetTeam { get; }
        public bool HasRequiredSubtype { get; }
        public bool RequiredSubtypeIsNone { get; }
        public string? RequiredSubtype { get; }
        public IReadOnlyList<string> ExcludedSubtypes { get; }
        public int ConditionCount { get; }
        public bool CanApplyInPreviewMode { get; }
        public int CardEffectCount { get; }
        public bool CardEffectHasOnlyCapturedPayload { get; }
        public IReadOnlyList<string> CardEffectPayloadIssues { get; }
        public string CardEffect { get; }
        public string CardEffectTarget { get; }
        public string CardEffectTeam { get; }
        public string CardEffectTargetCardType { get; }
        public float CardEffectParamMultiplier { get; }
        public int CardEffectParamInt { get; }
        public int CardEffectAdditionalParamInt { get; }
        public bool CardEffectUsesRange { get; }
        public bool CardEffectParamBool { get; }
        public bool CardEffectParamBool2 { get; }
        public IReadOnlyList<CombatStatus> StatusEffects { get; }

        public RelicAfterSpawnChanceRule(int effectIndex, int chancePercent, int roomRestriction,
            string targetTeam, bool hasRequiredSubtype, bool requiredSubtypeIsNone, string? requiredSubtype,
            IReadOnlyList<string> excludedSubtypes, int conditionCount, bool canApplyInPreviewMode, int cardEffectCount,
            bool cardEffectHasOnlyCapturedPayload, IReadOnlyList<string> cardEffectPayloadIssues,
            string cardEffect, string cardEffectTarget, string cardEffectTeam, string cardEffectTargetCardType,
            float cardEffectParamMultiplier, int cardEffectParamInt,
            int cardEffectAdditionalParamInt, bool cardEffectUsesRange, bool cardEffectParamBool, bool cardEffectParamBool2,
            IReadOnlyList<CombatStatus> statusEffects)
        {
            EffectIndex = effectIndex; ChancePercent = chancePercent; RoomRestriction = roomRestriction;
            TargetTeam = targetTeam; HasRequiredSubtype = hasRequiredSubtype;
            RequiredSubtypeIsNone = requiredSubtypeIsNone; RequiredSubtype = requiredSubtype;
            ExcludedSubtypes = Array.AsReadOnly(excludedSubtypes.ToArray()); ConditionCount = conditionCount;
            CanApplyInPreviewMode = canApplyInPreviewMode; CardEffectCount = cardEffectCount;
            CardEffectHasOnlyCapturedPayload = cardEffectHasOnlyCapturedPayload;
            CardEffectPayloadIssues = Array.AsReadOnly(cardEffectPayloadIssues.ToArray()); CardEffect = cardEffect;
            CardEffectTarget = cardEffectTarget; CardEffectTeam = cardEffectTeam;
            CardEffectTargetCardType = cardEffectTargetCardType; CardEffectParamMultiplier = cardEffectParamMultiplier;
            CardEffectParamInt = cardEffectParamInt;
            CardEffectAdditionalParamInt = cardEffectAdditionalParamInt; CardEffectUsesRange = cardEffectUsesRange;
            CardEffectParamBool = cardEffectParamBool; CardEffectParamBool2 = cardEffectParamBool2;
            StatusEffects = Array.AsReadOnly(statusEffects.ToArray());
        }
    }

    internal static class RelicAfterSpawnChanceStatusModel
    {
        internal const string Effect = "RelicEffectChanceAddEffectAfterSpawn";
        internal const string DataId = "98989cbb-c729-4f87-8240-08f448db96d8";
        internal const string AssetKey = "ChanceForSpellWeakness";

        internal static CombatStatus SpellWeakness(int stacks) => new("spell weakness", stacks,
            removeDuringDeployment: true, triggerVfxEnemy: false, triggerVfxPlayer: false,
            stackable: true, hidden: false, displayCategory: "Negative", propagatable: true,
            excludeHeroPropagation: false, excludeMonsterPropagation: true,
            removeWhenTriggeredAfterCardPlayed: true);

        internal static bool IsOriginalSpellWeakness(CombatStatus status, int stacks)
        {
            CombatStatus expected = SpellWeakness(stacks);
            return status.Id == expected.Id && status.Stacks == expected.Stacks && status.ParamInt == expected.ParamInt &&
                status.RemoveWhenTriggered == expected.RemoveWhenTriggered && status.RemoveStackAtEnd == expected.RemoveStackAtEnd &&
                status.RemoveAllAtEnd == expected.RemoveAllAtEnd && status.RemoveAfterPostCombat == expected.RemoveAfterPostCombat &&
                status.PreventRemovalDuringRelentless == expected.PreventRemovalDuringRelentless &&
                status.SkipDuringDeployment == expected.SkipDuringDeployment && status.RemoveDuringDeployment == expected.RemoveDuringDeployment &&
                status.RemoveWhenTriggeredAfterCardPlayed == expected.RemoveWhenTriggeredAfterCardPlayed &&
                status.RemoveAtEndOfTurnIfTriggered == expected.RemoveAtEndOfTurnIfTriggered &&
                status.MarkedForRemovalAfterCardPlayed == expected.MarkedForRemovalAfterCardPlayed &&
                status.TriggerVfxEnemy == expected.TriggerVfxEnemy && status.TriggerVfxPlayer == expected.TriggerVfxPlayer &&
                status.Stackable == expected.Stackable && status.Hidden == expected.Hidden &&
                status.DisplayCategory == expected.DisplayCategory && status.Propagatable == expected.Propagatable &&
                status.ExcludeHeroPropagation == expected.ExcludeHeroPropagation &&
                status.ExcludeMonsterPropagation == expected.ExcludeMonsterPropagation;
        }

        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => new { type, index })
                .Where(item => item.type == Effect).Select(item => item.index).ToArray();
            if (!indices.SequenceEqual((relic.AfterSpawnChanceRules ?? Array.Empty<RelicAfterSpawnChanceRule>())
                .Select(rule => rule.EffectIndex))) return "Missing or unordered native after-spawn chance payload.";
            if (indices.Length == 0) return null;
            if (relic.DataId != DataId || relic.AssetKey != AssetKey || relic.EffectTypes.Count != 1 ||
                relic.IsCovenant != false || relic.DisallowedInPlacementPhase != false)
                return "Unmodeled native after-spawn chance effect owner.";
            RelicAfterSpawnChanceRule rule = relic.AfterSpawnChanceRules!.Single();
            return rule.EffectIndex == 0 && rule.ChancePercent == 50 && rule.RoomRestriction == 0 &&
                rule.TargetTeam == "Heroes" && rule.HasRequiredSubtype && rule.RequiredSubtypeIsNone &&
                rule.RequiredSubtype == "SubtypesData_None" && rule.ExcludedSubtypes.Count == 0 && rule.ConditionCount == 0 &&
                !rule.CanApplyInPreviewMode && rule.CardEffectCount == 1 && rule.CardEffectHasOnlyCapturedPayload &&
                rule.CardEffectPayloadIssues.Count == 0 && rule.CardEffectTargetCardType == "Spell" &&
                rule.CardEffectParamMultiplier == 0f &&
                rule.CardEffect == "CardEffectAddStatusEffect" &&
                rule.CardEffectTarget == "Self" && rule.CardEffectTeam == "Heroes" &&
                rule.CardEffectParamInt == 0 && rule.CardEffectAdditionalParamInt == 0 &&
                !rule.CardEffectUsesRange && !rule.CardEffectParamBool && !rule.CardEffectParamBool2 &&
                rule.StatusEffects.Count == 1 && IsOriginalSpellWeakness(rule.StatusEffects[0], 1)
                    ? null : "Unmodeled native after-spawn chance effect payload.";
        }

        internal static RoomCombatResult ApplyAfterEntry(RoomCombatResult triggered,
            RoomCombatModel.QueuedCharacterTrigger queued, Action<RoomCombatModel.QueuedCharacterTrigger> enqueue)
        {
            if (!triggered.Supported) return triggered;
            RoomCombatState state = triggered.State!;
            CombatContext? context = state.Context;
            CombatRelicState[] owners = context?.Relics?.Where(relic => relic.EffectTypes.Contains(Effect)).ToArray()
                ?? Array.Empty<CombatRelicState>();
            if (owners.Length == 0 || state.Preview || queued.Kind != "AfterSpawnEnchant" || state.RoomIndex != queued.RoomIndex)
                return triggered;
            foreach (CombatRelicState owner in owners)
            {
                string? error = Validate(owner);
                if (error != null) return Fail(error);
            }

            CombatUnit? actor = state.Units.FirstOrDefault(unit => unit.Id == queued.Unit.Id);
            if (actor == null || actor.DeathState?.IsDestroyed == true || actor.Team != CombatTeam.Enemy ||
                actor.Status("untouchable") != null) return triggered;

            var events = triggered.Events.ToList();
            var retained = triggered.RetainedUnits.ToDictionary(unit => unit.Id);
            foreach (CombatRelicState owner in owners)
            {
                RelicAfterSpawnChanceRule rule = owner.AfterSpawnChanceRules!.Single();
                RngDraw draw = state.Context!.BattleRng.Range(0, 100);
                state = WithContext(state, state.Context.WithBattleRng(draw.State));
                if (draw.Value >= rule.ChancePercent) continue;
                foreach (CombatStatus status in rule.StatusEffects)
                {
                    RoomCombatResult applied = StatusApplicationModel.ApplyRetained(state, actor.Id,
                        status.WithStacks(status.Stacks), 0);
                    if (!applied.Supported) return applied;
                    state = applied.State!;
                    events.AddRange(applied.Events);
                    foreach (RoomCombatModel.QueuedCharacterTrigger callback in applied.PendingCallbacks) enqueue(callback);
                    foreach (CombatUnit unit in applied.RetainedUnits) retained[unit.Id] = unit;
                    actor = state.Units.FirstOrDefault(unit => unit.Id == actor.Id) ?? actor;
                }
            }
            return new RoomCombatResult(state, triggered.Outcome, triggered.Rounds, events,
                pendingCallbacks: triggered.PendingCallbacks, retainedUnits: retained.Values.ToArray(),
                dispatches: triggered.Dispatches);
        }

        private static RoomCombatState WithContext(RoomCombatState state, CombatContext context) =>
            new(state.RoomIndex, state.Deployment, state.Units, state.ExternalInteractions, context, state.Preview,
                state.ActiveCompanionBossIds);

        private static RoomCombatResult Fail(string error) =>
            new(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
    }
}
