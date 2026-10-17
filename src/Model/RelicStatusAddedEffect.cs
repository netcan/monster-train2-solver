using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicStatusAddedEffectModel
    {
        internal const string OwnerId = "274bc27a-9be2-4908-aa84-641944188c08";
        internal const string OwnerKey = "PropagateOnStatusEffectApplied";

        internal static bool HasListener(CombatContext? context) => context?.Relics?.Any(relic =>
            relic.TriggerAdditions?.Any(rule => rule.StatusAddedEffect != null) == true) == true;

        internal static string? Validate(CombatRelicState relic, RelicCharacterTriggers rule)
        {
            RelicStatusAddedEffect? action = rule.StatusAddedEffect;
            if (relic.DataId != OwnerId || relic.AssetKey != OwnerKey || relic.EffectTypes.Count != 1 ||
                relic.EffectTypes[0] != RelicModel.AddEffectOnStatusAdded || rule.EffectIndex != 0 ||
                relic.IsCovenant != false || relic.DisallowedInPlacementPhase != false || action == null ||
                !rule.SourcePlayer || !rule.SourceEnemy || !rule.SubtypeIsNone || rule.CharacterAssetKeys.Count != 0 ||
                rule.Conditions.Count != 0 || action.UniqueRequiredStatusCount != 3 || !action.TriggerAtExactCount ||
                action.OuterTargetMode != "Room" || !action.CanApplyInPreviewMode ||
                action.CardEffectType != "CardEffectAdjustAllStatusEffects" || action.TargetMode != "Self" ||
                !action.TargetHeroes || !action.TargetMonsters || action.Adjustment != 2 || action.StatusFilter != 1 ||
                action.RequireValidTargets || action.StatusIds.Count != 0)
                return "Unmodeled PropagateOnStatusEffectApplied definition.";
            return null;
        }

        internal static RoomCombatResult ApplyAfterTrigger(RoomCombatResult source,
            RoomCombatModel.QueuedCharacterTrigger queued)
        {
            if (!source.Supported || queued.Kind != "OnNewStatusEffectAdded" || source.State?.Context?.Relics == null)
                return source;
            RoomCombatState state = source.State;
            var events = source.Events.ToList();
            var callbacks = source.PendingCallbacks.ToList();
            var retained = source.RetainedUnits.ToList();
            RoomOutcome outcome = source.Outcome;

            for (int relicIndex = 0; relicIndex < state.Context!.Relics!.Count; relicIndex++)
            {
                CombatRelicState relic = state.Context.Relics[relicIndex];
                foreach (RelicCharacterTriggers rule in relic.TriggerAdditions ?? System.Array.Empty<RelicCharacterTriggers>())
                {
                    RelicStatusAddedEffect? action = rule.StatusAddedEffect;
                    if (action == null) continue;
                    string? error = Validate(relic, rule);
                    if (error != null) return Unsupported(error);
                    RelicConditionEvaluation condition = RelicConditionModel.Evaluate(state.Context!, rule.Conditions);
                    if (!condition.Supported) return Unsupported(condition.UnsupportedReason!);
                    state = state.WithContext(condition.Context);
                    if (!condition.Passed || state.Preview && !action.CanApplyInPreviewMode ||
                        relic.DisallowedInPlacementPhase == true && state.Deployment ||
                        action.UniqueRequiredStatusCount > queued.ParamInt ||
                        action.TriggerAtExactCount && action.UniqueRequiredStatusCount != queued.ParamInt)
                        continue;

                    CombatUnit? actor = state.Units.FirstOrDefault(unit => unit.Id == queued.Unit.Id);
                    if (actor == null || actor.Health <= 0 ||
                        actor.Team == CombatTeam.Player && !rule.SourcePlayer ||
                        actor.Team == CombatTeam.Enemy && !rule.SourceEnemy ||
                        !rule.SubtypeIsNone && !actor.Subtypes.Contains(rule.RequiredSubtype!) ||
                        !action.TargetHeroes && actor.Team == CombatTeam.Enemy ||
                        !action.TargetMonsters && actor.Team == CombatTeam.Player)
                        continue;

                    CombatStatus[] statuses = actor.Statuses.Where(status => status.Stacks > 0).ToArray();
                    foreach (CombatStatus status in statuses)
                    {
                        if (!status.Propagatable.HasValue)
                            return Unsupported("All-status adjustment requires captured propagatable-status metadata.");
                        if (!status.Propagatable.Value) continue;
                        bool? excluded = actor.Team == CombatTeam.Enemy ? status.ExcludeHeroPropagation : status.ExcludeMonsterPropagation;
                        if (!excluded.HasValue)
                            return Unsupported("All-status adjustment requires captured team-propagation metadata.");
                        if (excluded.Value) continue;

                        RoomCombatResult applied = StatusApplicationModel.ApplyRetained(state, actor.Id,
                            status.WithStacks(action.Adjustment), 0, fromEffectType: action.CardEffectType);
                        if (!applied.Supported) return applied;
                        state = applied.State!;
                        events.AddRange(applied.Events);
                        callbacks.AddRange(applied.PendingCallbacks);
                        retained.AddRange(applied.RetainedUnits);
                        if (applied.Outcome != RoomOutcome.Exchanged) outcome = applied.Outcome;
                    }
                }
            }

            return new RoomCombatResult(state, outcome, source.Rounds, events, pendingCallbacks: callbacks,
                retainedUnits: retained, dispatches: source.Dispatches);
        }

        private static RoomCombatResult Unsupported(string reason) => new(null, RoomOutcome.Unsupported, 0,
            new List<CombatEvent>(), reason);
    }
}
