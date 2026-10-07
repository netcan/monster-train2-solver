using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CompanionBossModel
    {
        public static TrainCombatResult Resolve(EnemySpawnState source, bool preCombat)
        {
            string? error = TrainCombatModel.Validate(source.Train);
            if (error != null || source.ExternalInteractions.Count > 0)
                return Unsupported(error ?? string.Join("; ", source.ExternalInteractions));
            var liveIds = source.Train.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id).ToHashSet();
            if (source.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.LastAttackerId > 0 && !liveIds.Contains(unit.LastAttackerId.Value)))
                return Unsupported("Companion phase inputs require settled destroyed-character references.");
            if (!preCombat || source.Phase < source.Waves.Count) return Match(source.Train);
            var companions = source.Train.Movement.Where(rule => rule.CompanionBoss).Select(rule => rule.UnitId).ToHashSet();
            CombatUnit[] bosses = source.Train.Rooms.SelectMany(room => room.Units).Where(unit => companions.Contains(unit.Id)).ToArray();
            if (bosses.Length == 0) return Match(source.Train);
            if (bosses.Length > 1 || source.Train.Rooms.SelectMany(room => room.Units).Count(unit => unit.IsBoss == true) > 1)
                return Unsupported("Paired companion Boss death and Pyre routing remain unmodeled.");
            CombatUnit boss = bosses[0];
            if (boss.Status("relentless") != null) return Match(source.Train);
            CombatStatus? relentless = source.Train.Context?.StatusRules.FirstOrDefault(rule => rule.Id == "relentless");
            if (relentless == null) return Unsupported("Companion transition requires the native relentless status definition.");
            TrainCombatResult moved = TrainCombatModel.Ascend(source.Train, boss.Id, true);
            if (!moved.Supported || Terminal(moved.Outcome)) return moved;
            TrainCombatState train = moved.State!;
            int index = train.Rooms.ToList().FindIndex(room => room.Units.Any(unit => unit.Id == boss.Id));
            if (index < 0) return Unsupported("A companion Boss disappeared before its native phase transition.");
            RoomCombatResult added = StatusApplicationModel.Apply(train.Rooms[index], boss.Id, relentless.WithStacks(1));
            if (!added.Supported) return Unsupported(added.UnsupportedReason!);
            CombatUnit changed = RemoveTriggers(added.State!.Units.First(unit => unit.Id == boss.Id));
            RoomCombatState[] rooms = train.Rooms.Select((room, roomIndex) => new RoomCombatState(room.RoomIndex, room.Deployment,
                roomIndex == index ? added.State.Units.Select(unit => unit.Id == changed.Id ? changed : unit).ToArray() : room.Units,
                room.ExternalInteractions, added.State.Context, room.Preview)).ToArray();
            train = new TrainCombatState(rooms, train.Movement, train.EnemySlotsPerRoom, added.State.Context);
            // AddStatusEffect queues callbacks; the native RemoveDeadCharacters drains
            // them only after RemoveTriggersOnRelentlessChange has removed the gates.
            TrainCombatResult settled = TrainCombatModel.ApplyCharacterQueue(train, added.PendingCallbacks.ToList());
            return settled.Supported ? new TrainCombatResult(settled.State, settled.Outcome,
                moved.RoomResults.Concat(settled.RoomResults).ToArray()) : settled;
        }

        public static CombatUnit RemoveTriggers(CombatUnit source) => new CombatUnit(source.Id, source.AssetKey, source.Team,
            source.BaseAttack, source.Health, source.MaxHealth, source.CanAttack, source.IsPyre, source.EndsBattleOnDeath,
            source.Statuses, source.Triggers.Where(trigger => !trigger.RemoveOnRelentlessChange).ToArray(), source.SpawnerCardId,
            source.Size, source.StatusImmunities, source.Subtypes, source.Modifiers, source.IsBoss, source.LastAttackerId,
            source.StatusRegistry, source.EquipmentCards, source.NextTriggerId, source.Ability, source.StatusDictionary, source.AbilityRules, source.HordeDefinition, source.IsSpawning, source.SacrificeCardId, source.DeathState);
        private static TrainCombatResult Match(TrainCombatState source) => new TrainCombatResult(source,
            RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>());
        private static bool Terminal(RoomOutcome outcome) => outcome == RoomOutcome.BattleWon ||
            outcome == RoomOutcome.PlayerDefeated || outcome == RoomOutcome.Stalemate;
        private static TrainCombatResult Unsupported(string reason) => new TrainCombatResult(null,
            RoomOutcome.Unsupported, Array.Empty<RoomCombatResult>(), reason);
    }
}
