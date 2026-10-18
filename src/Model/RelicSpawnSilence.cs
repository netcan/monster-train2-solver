using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicSpawnSilenceModel
    {
        internal const string Effect = "RelicEffectChanceAddEffectOnSpawn";
        internal const string DataId = "be9b4dd1-c0ba-41bd-a276-108ca7dad0fa";
        internal const string AssetKey = "ChanceToSilence";
        internal const string ExcludedSubtype = "SubtypesData_TreasureCollector";

        internal static CombatStatus Muted(int stacks) => new("muted", stacks, 1, removeStackAtEnd: true,
            removeAfterPostCombat: true, triggerVfxEnemy: false, triggerVfxPlayer: false, stackable: true,
            hidden: false, displayCategory: "Negative", propagatable: true,
            excludeHeroPropagation: false, excludeMonsterPropagation: true);

        private static bool OriginalStatus(CombatStatus status) => status.Id == "muted" && status.Stacks == 1 && status.ParamInt == 1 &&
            !status.RemoveWhenTriggered && status.RemoveStackAtEnd && !status.RemoveAllAtEnd && status.RemoveAfterPostCombat &&
            !status.PreventRemovalDuringRelentless && !status.SkipDuringDeployment && !status.RemoveDuringDeployment &&
            !status.RemoveWhenTriggeredAfterCardPlayed && !status.RemoveAtEndOfTurnIfTriggered && !status.MarkedForRemovalAfterCardPlayed &&
            status.TriggerVfxEnemy == false && status.TriggerVfxPlayer == false && status.Stackable == true && status.Hidden == false &&
            status.DisplayCategory == "Negative" && status.Propagatable == true && status.ExcludeHeroPropagation == false && status.ExcludeMonsterPropagation == true;

        internal static string? Validate(CombatRelicState owner)
        {
            RelicAfterSpawnChanceRule? rule = owner.AfterSpawnChanceRules?.FirstOrDefault();
            return owner.DataId == DataId && owner.AssetKey == AssetKey && owner.IsCovenant == false &&
                owner.DisallowedInPlacementPhase == false && owner.EffectTypes.SequenceEqual(new[] { Effect }) &&
                owner.AfterSpawnChanceRules?.Count == 1 && rule != null && rule.EffectIndex == 0 &&
                rule.ChancePercent == 100 && rule.RoomRestriction == 0 && rule.TargetTeam == "Heroes" &&
                rule.HasRequiredSubtype && rule.RequiredSubtypeIsNone && rule.RequiredSubtype == "SubtypesData_None" &&
                rule.ExcludedSubtypes.SequenceEqual(new[] { ExcludedSubtype }) && rule.ConditionCount == 0 && rule.CanApplyInPreviewMode &&
                rule.CardEffectCount == 1 && rule.CardEffectHasOnlyCapturedPayload && rule.CardEffectPayloadIssues.Count == 0 &&
                rule.CardEffect == "CardEffectAddStatusEffect" && rule.CardEffectTarget == "Self" && rule.CardEffectTeam == "Heroes" &&
                rule.CardEffectTargetCardType == "Spell" && rule.CardEffectParamMultiplier == 0f && rule.CardEffectParamInt == 100 &&
                rule.CardEffectAdditionalParamInt == 0 && !rule.CardEffectUsesRange && !rule.CardEffectParamBool && !rule.CardEffectParamBool2 &&
                rule.StatusEffects.Count == 1 && OriginalStatus(rule.StatusEffects[0])
                ? null : "Missing or changed original ChanceToSilence definition.";
        }

        internal static bool Eligible(CombatUnit actor, string trigger) => trigger == "OnSpawn" &&
            actor.Team == CombatTeam.Enemy && actor.DeathState?.IsDestroyed != true && actor.Status("untouchable") == null &&
            !actor.Subtypes.Contains(ExcludedSubtype);

        internal static RoomCombatResult ApplyOne(RoomCombatState source, int actorId, CombatRelicState owner)
        {
            string? error = Validate(owner);
            CombatUnit? actor = source.Units.FirstOrDefault(unit => unit.Id == actorId);
            if (error != null || source.Context == null || actor == null)
                return Fail(error ?? "Spawn silence requires its retained actor and combat context.");
            if (actor.Health <= 0) return Fail("Dead self-target spawn silence requires separate native evidence.");
            if (source.Preview && source.Context.IsolatedBattlePreview != true)
                return Fail("Spawn silence preview requires an isolated RNG stream.");
            // The outer 100% Test skips RNG. CardEffectAddStatusEffect still
            // draws once per target for its nonzero (100) inner chance value.
            RngDraw draw = source.Context.BattleRng.Range(0, 100);
            source = source.WithContext(source.Context.WithBattleRng(draw.State));
            return StatusApplicationModel.ApplyRetained(source, actorId, owner.AfterSpawnChanceRules![0].StatusEffects[0], 0,
                fromEffectType: "CardEffectAddStatusEffect", doubleForDuality: actor.Status("duality")?.Stacks > 0);
        }

        internal static TrainCombatResult ApplySelected(TrainCombatState source, int actorId)
        {
            CombatRelicState? owner = source.Context?.Relics?.SingleOrDefault(item => item.DataId == DataId);
            RoomCombatState? room = source.Rooms.FirstOrDefault(item => item.Units.Any(unit => unit.Id == actorId));
            if (owner == null || room == null) return new(null, RoomOutcome.Unsupported, [], "Missing spawn-silence actor or owner.");
            RoomCombatResult applied = ApplyOne(room, actorId, owner);
            if (!applied.Supported) return new(null, RoomOutcome.Unsupported, [applied], applied.UnsupportedReason);
            TrainCombatState state = source;
            RelicDamageQueueModel.Import(ref state, applied.State!);
            UnitCloneResult drained = UnitCloneModel.Drain(new(state, actorId, applied.Outcome, null,
                applied.Events, [], applied.PendingCallbacks));
            return new(drained.State, drained.Outcome, [applied], drained.UnsupportedReason);
        }

        private static RoomCombatResult Fail(string error) => new(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
    }
}
