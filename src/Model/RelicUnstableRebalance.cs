using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicUnstableRebalanceModifier
    {
        public int EffectIndex { get; }
        public int SourceTeam { get; }
        public int AdditionalProcDamage { get; }
        public int AdditionalExplosionDamage { get; }
        public int AdditionalGrowthRate { get; }
        public bool RequireOnlyOneUnitWithUnstable { get; }
        public string StatusId { get; }
        public int StatusStacks { get; }

        public RelicUnstableRebalanceModifier(int effectIndex, int sourceTeam, int additionalProcDamage,
            int additionalExplosionDamage, int additionalGrowthRate, bool requireOnlyOneUnitWithUnstable,
            string statusId, int statusStacks)
        {
            EffectIndex = effectIndex;
            SourceTeam = sourceTeam;
            AdditionalProcDamage = additionalProcDamage;
            AdditionalExplosionDamage = additionalExplosionDamage;
            AdditionalGrowthRate = additionalGrowthRate;
            RequireOnlyOneUnitWithUnstable = requireOnlyOneUnitWithUnstable;
            StatusId = statusId;
            StatusStacks = statusStacks;
        }
    }

    public static class RelicUnstableRebalanceModel
    {
        public const string Effect = "RelicEffectRebalanceUnstable";
        public const string UncontrolledGroupId = "4c737e1e-7e5c-40ee-87a3-944859784283";
        public const string UncontrolledGroupKey = "UncontrolledGroup";

        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => new { type, index })
                .Where(item => item.type == Effect).Select(item => item.index).ToArray();
            RelicUnstableRebalanceModifier[] rules = (relic.UnstableRebalances ??
                Array.Empty<RelicUnstableRebalanceModifier>()).ToArray();
            if (!indices.SequenceEqual(rules.Select(rule => rule.EffectIndex)))
                return "Missing or unordered unstable-rebalance definitions on " + relic.AssetKey + ".";
            foreach (RelicUnstableRebalanceModifier rule in rules)
            {
                if (rule.SourceTeam < 0 || rule.SourceTeam > byte.MaxValue || string.IsNullOrEmpty(rule.StatusId) ||
                    rule.StatusStacks != 1 || rule.StatusId != "unstable")
                    return "Malformed unstable-rebalance definition on " + relic.AssetKey + ".";
                if (rule.AdditionalProcDamage != 0)
                    return "Unmodeled unstable proc-damage or growth hook on " + relic.AssetKey + ".";
                bool uncontrolledGroup = relic.DataId == UncontrolledGroupId && relic.AssetKey == UncontrolledGroupKey;
                if (rule.AdditionalGrowthRate != 0 && !uncontrolledGroup)
                    return "Unmodeled unstable proc-damage or growth hook on " + relic.AssetKey + ".";
                if (uncontrolledGroup && (!relic.EffectTypes.SequenceEqual(new[] { Effect, RelicModel.Null }) ||
                    rules.Length != 1 || rule.EffectIndex != 0 || rule.SourceTeam != byte.MaxValue ||
                    rule.AdditionalExplosionDamage != 0 || rule.AdditionalGrowthRate != 5 ||
                    rule.RequireOnlyOneUnitWithUnstable))
                    return "Unmodeled UncontrolledGroup payload.";
            }
            return null;
        }

        public static int GetAdditionalGrowthRate(CombatContext? context, CombatUnit unstableUnit,
            IReadOnlyList<CombatUnit> roomUnits)
        {
            if (context?.Relics == null) return 0;
            int total = 0;
            int nativeTeam = unstableUnit.Team == CombatTeam.Player ? 2 : 1;
            foreach (CombatRelicState relic in context.Relics)
            {
                string? error = Validate(relic);
                if (error != null) throw new InvalidOperationException(error);
                foreach (RelicUnstableRebalanceModifier rule in relic.UnstableRebalances ??
                    Array.Empty<RelicUnstableRebalanceModifier>())
                {
                    if (rule.SourceTeam != 0 && (rule.SourceTeam & nativeTeam) == 0) continue;
                    if (rule.RequireOnlyOneUnitWithUnstable && roomUnits.Any(candidate =>
                        candidate.Id != unstableUnit.Id && candidate.Team == unstableUnit.Team &&
                        (candidate.Status("unstable")?.Stacks ?? 0) > 0 && candidate.DeathState?.IsDestroyed != true &&
                        (candidate.DeathState?.HasFinishedDying != true && candidate.Health > 0 ||
                            (candidate.Status("undying")?.Stacks ?? 0) > 0)))
                        continue;
                    total = unchecked(total + rule.AdditionalGrowthRate);
                }
            }
            return total;
        }

        public static int GetAdditionalExplosionDamagePerStack(CombatContext? context, CombatUnit unstableUnit,
            IReadOnlyList<CombatUnit> roomUnits)
        {
            if (context?.Relics == null) return 0;
            int total = 0;
            int nativeTeam = unstableUnit.Team == CombatTeam.Player ? 2 : 1;
            foreach (CombatRelicState relic in context.Relics)
            {
                string? error = Validate(relic);
                if (error != null) throw new InvalidOperationException(error);
                foreach (RelicUnstableRebalanceModifier rule in relic.UnstableRebalances ??
                    Array.Empty<RelicUnstableRebalanceModifier>())
                {
                    if (rule.SourceTeam != 0 && (rule.SourceTeam & nativeTeam) == 0) continue;
                    if (rule.RequireOnlyOneUnitWithUnstable && roomUnits.Any(candidate =>
                        candidate.Id != unstableUnit.Id && candidate.Team == unstableUnit.Team &&
                        (candidate.Status("unstable")?.Stacks ?? 0) > 0 && candidate.DeathState?.IsDestroyed != true &&
                        (candidate.DeathState?.HasFinishedDying != true && candidate.Health > 0 ||
                            (candidate.Status("undying")?.Stacks ?? 0) > 0)))
                        continue;
                    total = unchecked(total + rule.AdditionalExplosionDamage);
                }
            }
            return total;
        }
    }

    public sealed class UnstableGrowthResult
    {
        public TrainCombatState? State { get; }
        public RoomOutcome Outcome { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        internal UnstableGrowthResult(TrainCombatState? state, RoomOutcome outcome, string? error = null)
        { State = state; Outcome = outcome; UnsupportedReason = error; }
    }

    public static class UnstableGrowthModel
    {
        public static UnstableGrowthResult StartTurn(TrainCombatState source)
        {
            if (source.Context == null) return Unsupported("Unstable growth requires shared combat context.");
            string? relicError = RelicModel.Validate(source.Context.Relics);
            if (relicError != null) return Unsupported(relicError);
            if (source.Context.Relics?.Any(relic => relic.UnstableRebalances?.Any(rule =>
                rule.AdditionalGrowthRate != 0) == true) != true)
                return new UnstableGrowthResult(source, RoomOutcome.Exchanged);
            RoomCombatState[] rooms = source.Rooms.ToArray();
            CombatContext? context = source.Context;
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            foreach (RoomCombatState originalRoom in source.Rooms.OrderByDescending(room => room.RoomIndex))
            {
                int roomIndex = Array.FindIndex(rooms, room => room.RoomIndex == originalRoom.RoomIndex);
                foreach (CombatTeam team in new[] { CombatTeam.Player, CombatTeam.Enemy })
                {
                    int[] unitIds = rooms[roomIndex].Units.Where(unit => unit.Team == team)
                        .Select(unit => unit.Id).ToArray();
                    foreach (int unitId in unitIds)
                    {
                        RoomCombatState room = WithContext(rooms[roomIndex], context);
                        CombatUnit? unit = room.Units.FirstOrDefault(candidate => candidate.Id == unitId);
                        CombatStatus? unstable = unit?.Status("unstable");
                        if (unit == null || unit.Health <= 0 || unit.DeathState?.IsDestroyed == true ||
                            unit.DeathState?.IsDespawned == true || unstable == null || unstable.Stacks <= 0 ||
                            unit.Status("blastresistant")?.Stacks > 0)
                            continue;
                        if (unstable.Stacks >= unit.Health)
                            return Unsupported("Lethal Unstable start-of-turn sacrifice is outside the accepted growth witness.");
                        int growth = RelicUnstableRebalanceModel.GetAdditionalGrowthRate(context, unit, room.Units);
                        if (growth <= 0) continue;
                        RoomCombatResult added = StatusApplicationModel.ApplyRetained(room, unit.Id,
                            unstable.WithStacks(growth), 0);
                        if (!added.Supported) return Unsupported(added.UnsupportedReason!);
                        rooms[roomIndex] = added.State!;
                        context = added.State!.Context;
                        callbacks.AddRange(added.PendingCallbacks);
                    }
                }
            }
            TrainCombatState grown = new TrainCombatState(rooms.Select(room => WithContext(room, context)).ToArray(),
                source.Movement, source.EnemySlotsPerRoom, context);
            if (callbacks.Count == 0) return new UnstableGrowthResult(grown, RoomOutcome.Exchanged);
            TrainCombatResult drained = TrainCombatModel.ApplyCharacterQueue(grown, callbacks);
            return drained.Supported
                ? new UnstableGrowthResult(drained.State, drained.Outcome)
                : Unsupported(drained.UnsupportedReason!);
        }

        private static RoomCombatState WithContext(RoomCombatState room, CombatContext? context) =>
            new RoomCombatState(room.RoomIndex, room.Deployment, room.Units, room.ExternalInteractions,
                context, room.Preview, room.ActiveCompanionBossIds);

        private static UnstableGrowthResult Unsupported(string reason) =>
            new UnstableGrowthResult(null, RoomOutcome.Unsupported, reason);
    }
}
