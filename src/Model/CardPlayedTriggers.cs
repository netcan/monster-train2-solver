using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardPlayedQueueEntry
    {
        public int RoomIndex { get; }
        public CombatUnit Actor { get; }
        public string Kind { get; }
        public int ParamInt { get; }
        public int ParamInt2 { get; }
        public string? ParamString { get; }
        public CombatUnit? OverrideTarget { get; }
        public CombatUnit? DyingCharacter { get; }
        public bool CanFireTriggers { get; }
        public int TriggerCount { get; }
        public CardPlayedQueueEntry(int roomIndex, CombatUnit actor, string kind, int paramInt, int paramInt2, string? paramString,
            CombatUnit? overrideTarget, CombatUnit? dyingCharacter, bool canFireTriggers, int triggerCount)
        { RoomIndex = roomIndex; Actor = actor; Kind = kind; ParamInt = paramInt; ParamInt2 = paramInt2; ParamString = paramString;
            OverrideTarget = overrideTarget; DyingCharacter = dyingCharacter; CanFireTriggers = canFireTriggers; TriggerCount = triggerCount; }
        internal RoomCombatModel.QueuedCharacterTrigger ToQueued() => new RoomCombatModel.QueuedCharacterTrigger(RoomIndex,
            Actor, Kind, paramInt: ParamInt, paramInt2: ParamInt2, paramString: ParamString, overrideTarget: OverrideTarget,
            dyingCharacter: DyingCharacter, canFireTriggers: CanFireTriggers, triggerCount: TriggerCount,
            admission: RoomCombatModel.CharacterTriggerAdmission.Accepted);
    }
    internal static class CardPlayedTriggerModel
    {
        internal static TrainCombatResult Rally(TrainCombatState source, CombatTeam team, IReadOnlyList<int> cachedUnitIds,
            IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger>? preceding = null)
            => Apply(source, team, cachedUnitIds, "CardMonsterPlayed", preceding);
        internal static TrainCombatResult Spell(TrainCombatState source, CombatTeam team, IReadOnlyList<int> cachedUnitIds,
            IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger>? preceding = null)
            => Apply(source, team, cachedUnitIds, "CardSpellPlayed", preceding);
        internal static TrainCombatResult Ability(TrainCombatState source, CombatTeam team, IReadOnlyList<int> cachedUnitIds,
            int activatorUnitId, IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger>? preceding = null)
        {
            string? error = TrainCombatModel.Validate(source);
            if (error != null) return new TrainCombatResult(null, RoomOutcome.Unsupported, System.Array.Empty<RoomCombatResult>(), error);
            bool incant = RelicModel.AbilitiesTriggerIncant(source.Context);
            var cached = new HashSet<int>(cachedUnitIds);
            var queue = preceding?.ToList() ?? new List<RoomCombatModel.QueuedCharacterTrigger>();
            // Native admits both callbacks per actor, then drains the whole manager.
            // The activator's Own callback does not require the played-room cache.
            foreach (var entry in source.Rooms.SelectMany(room => room.Units.Where(unit => unit.Team == team &&
                    unit.Health > 0 && unit.DeathState?.IsDestroyed != true).Select(unit => new { room.RoomIndex, Unit = unit }))
                .OrderBy(entry => entry.Unit.Id))
            {
                if (entry.Unit.Id == activatorUnitId)
                    queue.Add(new RoomCombatModel.QueuedCharacterTrigger(entry.RoomIndex, entry.Unit, "OnOwnAbilityActivated"));
                if (incant && cached.Contains(entry.Unit.Id))
                    queue.Add(new RoomCombatModel.QueuedCharacterTrigger(entry.RoomIndex, entry.Unit, "CardSpellPlayed"));
            }
            return TrainCombatModel.ApplyCharacterQueue(source, queue);
        }
        private static TrainCombatResult Apply(TrainCombatState source, CombatTeam team, IReadOnlyList<int> cachedUnitIds, string kind,
            IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger>? preceding)
        {
            string? error = TrainCombatModel.Validate(source);
            if (error != null) return new TrainCombatResult(null, RoomOutcome.Unsupported, System.Array.Empty<RoomCombatResult>(), error);
            var cached = new HashSet<int>(cachedUnitIds);
            // CardState caches the original room's living, non-spawning actors.
            // Movement keeps membership, while deaths and newly created actors do not.
            var queue = preceding?.ToList() ?? new List<RoomCombatModel.QueuedCharacterTrigger>();
            queue.AddRange(source.Rooms.SelectMany(room => room.Units.Where(unit => unit.Team == team && unit.Health > 0 && unit.DeathState?.IsDestroyed != true && cached.Contains(unit.Id))
                .Select(unit => new RoomCombatModel.QueuedCharacterTrigger(room.RoomIndex, unit, kind)))
                .OrderBy(item => item.Unit.Id));
            return TrainCombatModel.ApplyCharacterQueue(source, queue);
        }
    }
}
