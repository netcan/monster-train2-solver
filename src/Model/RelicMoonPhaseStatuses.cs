using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicMoonPhaseStatusRule
    {
        public string SourceTeam { get; }
        public string TargetMode { get; }
        public IReadOnlyList<CombatStatus> Statuses { get; }
        public string? RequiredSubtype { get; }
        public bool DoNotAnimateCamera { get; }
        public int MoonPhaseMask { get; }

        public RelicMoonPhaseStatusRule(string sourceTeam, string targetMode,
            IReadOnlyList<CombatStatus> statuses, string? requiredSubtype,
            bool doNotAnimateCamera, int moonPhaseMask)
        {
            SourceTeam = sourceTeam;
            TargetMode = targetMode;
            Statuses = Array.AsReadOnly(statuses.ToArray());
            RequiredSubtype = requiredSubtype;
            DoNotAnimateCamera = doNotAnimateCamera;
            MoonPhaseMask = moonPhaseMask;
        }
    }

    internal sealed class RelicMoonPhaseStatusResult
    {
        internal TrainCombatState? State { get; }
        internal string? UnsupportedReason { get; }
        internal bool Supported => State != null;
        internal RelicMoonPhaseStatusResult(TrainCombatState? state, string? error = null)
        { State = state; UnsupportedReason = error; }
    }

    internal static class RelicMoonPhaseStatusModel
    {
        internal const string Effect = "RelicEffectAddStatusEffectOnMoonPhase";
        internal const string DataId = "e013b17d-753b-47b8-a837-02af85addb4b";
        internal const string AssetKey = "MoonPhaseAddArmorToSilent";
        private const string SilentSubtype = "SubtypesData_Silent";

        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == Effect).Select(item => item.index).ToArray();
            if (indices.Length == 0)
                return relic.ResourceEffects?.Any(rule => rule.MoonPhaseStatus != null) == true
                    ? "Unexpected relic moon-phase status rules." : null;
            if (relic.DataId != DataId || relic.AssetKey != AssetKey || relic.IsCovenant != false ||
                relic.DisallowedInPlacementPhase != false || !indices.SequenceEqual(new[] { 0 }) ||
                relic.EffectTypes.Count != 1 || relic.ResourceEffects == null || relic.ResourceEffects.Count != 1)
                return "Unmodeled native relic moon-phase status owner.";
            return Validate(relic, relic.ResourceEffects[0]);
        }

        internal static string? Validate(CombatRelicState relic, RelicResourceEffect resource)
        {
            RelicMoonPhaseStatusRule? rule = resource.MoonPhaseStatus;
            if (resource.Type != Effect || resource.EffectIndex != 0 || rule == null ||
                resource.Conditions.Count != 0 || resource.RequiredMutatorId != null ||
                rule.SourceTeam != "Monsters" || rule.TargetMode != "Tower" ||
                rule.RequiredSubtype != SilentSubtype || !rule.DoNotAnimateCamera || rule.MoonPhaseMask != 1 ||
                rule.Statuses.Count != 1 || rule.Statuses[0].Id != "armor" || rule.Statuses[0].Stacks != 10 ||
                rule.Statuses[0].ParamInt != 1 || !rule.Statuses[0].RemoveWhenTriggered ||
                rule.Statuses[0].RemoveStackAtEnd || rule.Statuses[0].RemoveAllAtEnd ||
                rule.Statuses[0].RemoveAfterPostCombat || rule.Statuses[0].PreventRemovalDuringRelentless ||
                rule.Statuses[0].SkipDuringDeployment || !rule.Statuses[0].RemoveDuringDeployment ||
                rule.Statuses[0].RemoveWhenTriggeredAfterCardPlayed || rule.Statuses[0].RemoveAtEndOfTurnIfTriggered ||
                rule.Statuses[0].MarkedForRemovalAfterCardPlayed || rule.Statuses[0].TriggerVfxEnemy != true ||
                rule.Statuses[0].TriggerVfxPlayer != true || rule.Statuses[0].Stackable != true ||
                rule.Statuses[0].Hidden != false || rule.Statuses[0].DisplayCategory != "Persistent" ||
                rule.Statuses[0].Propagatable != false || rule.Statuses[0].ExcludeHeroPropagation != false ||
                rule.Statuses[0].ExcludeMonsterPropagation != false)
                return "Unmodeled native relic moon-phase status rule.";
            return null;
        }

        internal static string? ValidateTransition(TrainCombatState source, int phase, bool preview, bool placement)
        {
            if (source.Context?.Relics == null) return null;
            foreach (CombatRelicState relic in source.Context.Relics)
            {
                string? error = Validate(relic);
                if (error != null) return error;
                if (relic.DisallowedInPlacementPhase == true && placement) continue;
                foreach (RelicResourceEffect resource in relic.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
                {
                    RelicMoonPhaseStatusRule? rule = resource.MoonPhaseStatus;
                    if (resource.Type != Effect || rule == null || preview && !resource.CanApplyInPreviewMode ||
                        (rule.MoonPhaseMask & phase) == 0) continue;
                    foreach (RoomCombatState room in source.Rooms)
                    {
                        if (room.ExternalInteractions.Count != 0 && room.Units.Any(unit => IsTarget(unit, rule)))
                            return "Moon-phase status with room external interactions is not modeled.";
                        foreach (CombatUnit unit in room.Units.Where(unit => IsTarget(unit, rule)))
                        {
                            if (unit.Triggers.Any(trigger => trigger.Kind is "OnStatusEffectChanged" or "OnArmorAdded" or "OnNewStatusEffectAdded"))
                                return "Moon-phase status target callbacks are not modeled.";
                        }
                    }
                }
            }
            return null;
        }

        internal static RelicMoonPhaseStatusResult Apply(TrainCombatState source, int phase,
            bool preview = false, bool placement = false)
        {
            string? error = ValidateTransition(source, phase, preview, placement);
            if (error != null) return new RelicMoonPhaseStatusResult(null, error);
            if (source.Context?.Relics == null) return new RelicMoonPhaseStatusResult(source);
            TrainCombatState state = source;
            foreach (CombatRelicState relic in source.Context.Relics)
            {
                if (relic.DisallowedInPlacementPhase == true && placement) continue;
                foreach (RelicResourceEffect resource in relic.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
                {
                    RelicMoonPhaseStatusRule? rule = resource.MoonPhaseStatus;
                    if (resource.Type != Effect || rule == null || preview && !resource.CanApplyInPreviewMode ||
                        (rule.MoonPhaseMask & phase) == 0) continue;
                    int[] targetIds = state.Rooms.OrderBy(room => room.RoomIndex).SelectMany(room => room.Units
                        .Where(unit => IsTarget(unit, rule)).Select(unit => unit.Id)).ToArray();
                    var rooms = state.Rooms.ToArray();
                    var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
                    foreach (int targetId in targetIds)
                    {
                        int roomIndex = Array.FindIndex(rooms, room => room.Units.Any(unit => unit.Id == targetId));
                        if (roomIndex < 0) continue;
                        RoomCombatState room = rooms[roomIndex];
                        var scoped = new RoomCombatState(room.RoomIndex, room.Deployment, room.Units,
                            room.ExternalInteractions, state.Context, room.Preview, room.ActiveCompanionBossIds);
                        foreach (CombatStatus status in rule.Statuses)
                        {
                            RoomCombatResult applied = StatusApplicationModel.ApplyRetained(scoped, targetId, status, 0);
                            if (!applied.Supported) return new RelicMoonPhaseStatusResult(null, applied.UnsupportedReason);
                            scoped = applied.State!;
                            callbacks.AddRange(applied.PendingCallbacks);
                        }
                        rooms[roomIndex] = scoped;
                        state = CardSpellModel.WithContext(new TrainCombatState(rooms, state.Movement,
                            state.EnemySlotsPerRoom, state.Context), scoped.Context!);
                    }
                    if (callbacks.Count > 0)
                    {
                        TrainCombatResult drained = TrainCombatModel.ApplyCharacterQueue(state, callbacks);
                        if (!drained.Supported) return new RelicMoonPhaseStatusResult(null, drained.UnsupportedReason);
                        state = drained.State!;
                    }
                }
            }
            return new RelicMoonPhaseStatusResult(state);
        }

        private static bool IsTarget(CombatUnit unit, RelicMoonPhaseStatusRule rule) =>
            rule.SourceTeam == "Monsters" && unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0 &&
            unit.DeathState?.IsDestroyed != true && unit.Status("untouchable") == null &&
            unit.Subtypes.Contains(rule.RequiredSubtype!, StringComparer.Ordinal);
    }
}
