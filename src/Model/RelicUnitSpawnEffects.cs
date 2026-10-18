using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicUnitSpawnEffectRule
    {
        public string Team { get; }
        public string Subtype { get; }
        public CardActionEffect Action { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public IReadOnlyList<RelicStartTurnEffectTriggerMetadata> UnusedTriggers { get; }
        public RelicUnitSpawnEffectRule(string team, string subtype, CardActionEffect action,
            IReadOnlyList<string> externalInteractions, IReadOnlyList<RelicStartTurnEffectTriggerMetadata> unusedTriggers)
        { Team = team; Subtype = subtype; Action = action;
            ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
            UnusedTriggers = Array.AsReadOnly(unusedTriggers.ToArray()); }
    }

    internal static class RelicUnitSpawnEffectModel
    {
        internal const string Effect = "RelicEffectAddEffectOnUnitSpawn";
        internal const string DataId = "17ee5d9c-4b87-4250-ad3b-dbe49d118e01";
        internal const string AssetKey = "IncantTriggeredByUnitAbilities";
        internal const string Subtype = "SubtypesData_Whelp";
        internal static string? Validate(CombatRelicState owner, RelicResourceEffect resource)
        {
            RelicUnitSpawnEffectRule? rule = resource.UnitSpawnEffect;
            RelicConditionState? condition = resource.Conditions.Count == 1 ? resource.Conditions[0] : null;
            if (owner.DataId != DataId || owner.AssetKey != AssetKey || owner.IsCovenant != false ||
                owner.DisallowedInPlacementPhase != false || !owner.EffectTypes.SequenceEqual(new[] { Effect }) ||
                resource.EffectIndex != 0 || resource.Type != Effect || resource.ParamInt != 2 || resource.Amount != 2 ||
                resource.ParamInt2 != 0 || resource.ParamMinInt != 0 || resource.ParamFloat != 0 || resource.CanApplyInPreviewMode ||
                resource.RequiredMutatorId != null || condition?.TrackTriggerCount != true || condition.AllowMultiple ||
                condition.Comparator != 6 || condition.Value != 1 || condition.Query.Duration != "ThisTurn" ||
                condition.Query.Type != "SubtypeInDeck" || !condition.Query.SubtypeIsNone || rule?.Team != "Monsters" ||
                rule.Subtype != Subtype || rule.ExternalInteractions.Count != 0 || !Action(rule.Action, "Tower") ||
                rule.UnusedTriggers.Count != 1)
                return "Missing or changed original Whelp spawn cooldown definition.";
            RelicStartTurnEffectTriggerMetadata trigger = rule.UnusedTriggers[0];
            return trigger.Kind == "OnSpawn" && !trigger.Once && !trigger.IgnoreSilence && !trigger.OnlyIfEquipped &&
                trigger.Effects.Count == 1 && Action(trigger.Effects[0], "Room")
                ? null : "Changed unused original Whelp trigger metadata.";
        }
        private static bool Action(CardActionEffect action, string target) => action.Type == "AddStatus" && action.Target == target &&
            action.AllowPlayer && !action.AllowEnemy && action.Value == 0 && action.Range == null && action.Upgrade == null &&
            action.Statuses.Count == 1 && action.Statuses[0].Id == "cooldown" && action.Statuses[0].Stacks == -2 &&
            action.Filters?.Health == "Both" && !action.Filters.IgnoreBosses && action.Filters.Subtype.Length == 0 &&
            action.Filters.RequiredStatuses.Count == 0 && action.Filters.ExcludedStatuses.Count == 0 && action.Filters.ExcludedSubtypes.Count == 0 &&
            action.Tests?.ShouldTest == true && !action.Tests.StrictTargets && !action.Tests.FailToCast && !action.Tests.CancelSubsequent &&
            !action.Tests.MustHaveValidTargets && action.Tests.CanPlayAfterBossDead == true;

        internal static bool Test(CombatUnit? actor, string trigger) => trigger == "OnSpawn" && actor?.Team == CombatTeam.Player && actor.Subtypes.Contains(Subtype);
        internal static RelicConditionEvaluation Check(CombatContext source, CombatUnit? actor, string trigger, bool preview)
        {
            CombatRelicState? owner = source.Relics?.SingleOrDefault(item => item.DataId == DataId);
            if (owner == null) return new(source, false);
            RelicResourceEffect resource = owner.ResourceEffects!.Single(item => item.Type == Effect);
            string? error = Validate(owner, resource); if (error != null) return new(source, false, error);
            if (preview) return new(source, false);
            RelicConditionEvaluation condition = RelicConditionModel.Evaluate(source, resource.Conditions);
            return !condition.Supported || !condition.Passed ? condition : new(condition.Context, Test(actor, trigger));
        }
        internal static TrainCombatResult Apply(TrainCombatState source, int actorId, string trigger, bool managed)
        {
            CombatRelicState? owner = source.Context?.Relics?.SingleOrDefault(item => item.DataId == DataId);
            if (owner == null) return new(source, RoomOutcome.Exchanged, []);
            RelicResourceEffect resource = owner.ResourceEffects!.Single(item => item.Type == Effect);
            string? error = Validate(owner, resource); if (error != null) return Fail(error);
            CombatUnit? actor = source.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == actorId);
            RoomCombatState? room = source.Rooms.FirstOrDefault(item => item.Units.Any(unit => unit.Id == actorId));
            if (managed)
            {
                RelicConditionEvaluation checkedEffect = Check(source.Context!, actor, trigger, room?.Preview == true);
                if (!checkedEffect.Supported) return Fail(checkedEffect.UnsupportedReason!);
                source = CardSpellModel.WithContext(source, checkedEffect.Context);
                if (!checkedEffect.Passed) return new(source, RoomOutcome.Exchanged, []);
            }
            if (room == null) return Fail("Spawn effects require the native actor's room.");
            if (source.Rooms.SelectMany(item => item.Units).Any(unit => unit.Health <= 0))
                return Fail("Retained dead targets in Whelp spawn effects require native evidence.");
            TrainSpellResult applied = CardSpellModel.Apply(source, room.RoomIndex, [resource.UnitSpawnEffect!.Action], 0,
                selfUnitId: actorId, otherPiles: source.Context?.OtherPiles);
            if (!applied.Supported) return Fail(applied.UnsupportedReason!);
            if (applied.Outcome != RoomOutcome.Exchanged) return Fail("Unmodeled terminal Whelp spawn callback.");
            CombatContext context = applied.State!.Context!;
            // ApplyEffects invokes one post-effects callback for the sequence;
            // the relic manager separately invokes the same notification again.
            for (int notification = 0; notification < (managed ? 2 : 1); notification++)
            {
                CombatRelicState current = context.Relics!.Single(item => item.DataId == DataId);
                RelicResourceEffect effect = current.ResourceEffects!.Single(item => item.Type == Effect);
                RelicConditionRecord recorded = RelicConditionModel.Record(context, effect.Conditions);
                if (!recorded.Supported) return Fail(recorded.UnsupportedReason!);
                context = recorded.Context.WithRelics(recorded.Context.Relics!.Select(item => item.DataId == DataId
                    ? current.WithResourceEffects([effect.WithConditions(recorded.Conditions)]) : item).ToArray());
            }
            return new(CardSpellModel.WithContext(applied.State, context), RoomOutcome.Exchanged, []);
        }
        private static TrainCombatResult Fail(string error) => new(null, RoomOutcome.Unsupported, [], error);
    }
}
