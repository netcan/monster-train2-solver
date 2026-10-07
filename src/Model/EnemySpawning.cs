using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class EnemyDefinition
    {
        public CombatUnit Unit { get; }
        public bool Ascends { get; }
        public bool Loops { get; }
        public bool CompanionBoss { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public EnemyDefinition(CombatUnit unit, bool ascends, bool loops, IReadOnlyList<string> externalInteractions, bool companionBoss = false)
        { Unit = unit; Ascends = ascends; Loops = loops; ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray()); CompanionBoss = companionBoss; }
        internal CombatUnit Create(int id) => new CombatUnit(id, Unit.AssetKey, CombatTeam.Enemy,
            Unit.BaseAttack, Unit.Health, Unit.MaxHealth, Unit.CanAttack, false, Unit.EndsBattleOnDeath,
            Unit.Statuses, Unit.Triggers, size: Unit.Size, statusImmunities: Unit.StatusImmunities, subtypes: Unit.Subtypes, modifiers: Unit.Modifiers, isBoss: Unit.IsBoss, lastAttackerId: Unit.LastAttackerId.HasValue ? 0 : null, statusRegistry: Unit.StatusRegistry, equipmentCards: Unit.EquipmentCards, nextTriggerId: Unit.NextTriggerId, ability: Unit.Ability, statusDictionary: Unit.StatusDictionary, abilityRules: Unit.AbilityRules, hordeDefinition: Unit.HordeDefinition, isSpawning: Unit.IsSpawning, sacrificeCardId: Unit.SacrificeCardId, deathState: Unit.DeathState);
    }

    public sealed class EnemyGroup
    {
        // Authored order; the native spawner iterates this list backwards.
        public IReadOnlyList<EnemyDefinition> Units { get; }
        public EnemyGroup(IReadOnlyList<EnemyDefinition> units) { Units = Array.AsReadOnly(units.ToArray()); }
    }

    public sealed class EnemyWave
    {
        public IReadOnlyList<EnemyGroup> Candidates { get; }
        public EnemyWave(IReadOnlyList<EnemyGroup> candidates) { Candidates = Array.AsReadOnly(candidates.ToArray()); }
    }

    public sealed class EnemySpawnState
    {
        public TrainCombatState Train { get; }
        public IReadOnlyList<EnemyWave> Waves { get; }
        public IReadOnlyList<int> SelectedGroups { get; }
        public int Phase { get; }
        public bool Looping { get; }
        public UnityRng Rng { get; }
        public int NextUnitId { get; }
        public IReadOnlyList<EnemyDefinition> Treasures { get; }
        public int TreasuresRemaining { get; }
        public bool TreasureEnabled { get; }
        public int FirstTreasureTurn { get; }
        public int FirstTreasureRoom { get; }
        public int Turn { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public bool CanonicalDecisionReferences { get; }
        // Unity objects marked Destroyed but still awaiting the frame-end flush.
        public IReadOnlyList<int>? PendingDestroyedUnitIds { get; }
        public EnemySpawnState(TrainCombatState train, IReadOnlyList<EnemyWave> waves,
            IReadOnlyList<int> selectedGroups, int phase, bool looping, UnityRng rng, int nextUnitId,
            IReadOnlyList<EnemyDefinition> treasures, int treasuresRemaining, bool treasureEnabled,
            int firstTreasureTurn, int firstTreasureRoom, int turn, IReadOnlyList<string> externalInteractions,
            bool canonicalDecisionReferences = false, IReadOnlyList<int>? pendingDestroyedUnitIds = null)
        {
            Train = train; Waves = Array.AsReadOnly(waves.ToArray()); SelectedGroups = Array.AsReadOnly(selectedGroups.ToArray());
            Phase = phase; Looping = looping; Rng = rng; NextUnitId = nextUnitId;
            Treasures = Array.AsReadOnly(treasures.ToArray()); TreasuresRemaining = treasuresRemaining;
            TreasureEnabled = treasureEnabled; FirstTreasureTurn = firstTreasureTurn; FirstTreasureRoom = firstTreasureRoom;
            Turn = turn; ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
            CanonicalDecisionReferences = canonicalDecisionReferences;
            PendingDestroyedUnitIds = pendingDestroyedUnitIds == null ? null : Array.AsReadOnly(pendingDestroyedUnitIds.ToArray());
        }
    }

    public sealed class EnemySpawnResult
    {
        public EnemySpawnState? State { get; }
        public RoomOutcome Outcome { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        internal EnemySpawnResult(EnemySpawnState? state, RoomOutcome outcome, string? reason = null)
        { State = state; Outcome = outcome; UnsupportedReason = reason; }
    }

    public static class EnemySpawningModel
    {
        public static EnemySpawnResult Spawn(EnemySpawnState source, bool includeTreasure)
        {
            if (source.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", source.ExternalInteractions));
            if (source.Phase < 0 || source.Waves.Count == 0 || source.SelectedGroups.Count != source.Waves.Count)
                return Unsupported("Invalid spawn pattern phase or group cache.");
            if (source.Train.Rooms.Count < 2 || source.Train.EnemySlotsPerRoom < 1 ||
                source.Train.Rooms.Where((room, index) => room.RoomIndex != index).Any())
                return Unsupported("Invalid train for spawning.");
            var rooms = source.Train.Rooms.Select(room => room.Units.ToList()).ToArray();
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            var movement = source.Train.Movement.ToList();
            int[] groups = source.SelectedGroups.ToArray();
            int phase = source.Phase, nextId = source.NextUnitId, treasureRemaining = source.TreasuresRemaining;
            UnityRng rng = source.Rng;
            CombatContext? context = source.Train.Context;
            RoomOutcome outcome = RoomOutcome.Cleared;
            int pyre = rooms.Length - 1;
            if (source.Looping || phase < source.Waves.Count)
            {
                int waveIndex = phase % source.Waves.Count;
                EnemyWave wave = source.Waves[waveIndex];
                int selected = groups[waveIndex];
                if (selected < 0)
                {
                    if (wave.Candidates.Count == 0) return Unsupported("Empty spawn group pool.");
                    RngDraw choice = rng.Range(0, wave.Candidates.Count); rng = choice.State;
                    selected = groups[waveIndex] = choice.Value;
                }
                if (selected >= wave.Candidates.Count) return Unsupported("Invalid cached spawn group.");
                bool enteredPyre = false;
                var entered = new List<int>();
                foreach (EnemyDefinition definition in wave.Candidates[selected].Units.Reverse())
                {
                    string? error = Validate(definition, context);
                    if (error != null) return Unsupported(error);
                    int roomIndex = 0;
                    while (roomIndex <= pyre && rooms[roomIndex].Count(unit => unit.Team == CombatTeam.Enemy)
                        >= source.Train.EnemySlotsPerRoom) roomIndex++;
                    if (roomIndex > pyre) return Unsupported("No enemy spawn point remains in the train.");
                    string? abilityError = AbilityLifecycleModel.SpawnError(definition.Unit, context);
                    if (abilityError != null) return Unsupported(abilityError);
                    CombatUnit unit = AbilityLifecycleModel.SuppressAtSpawn(definition.Create(nextId++), context);
                    Insert(rooms[roomIndex], unit);
                    error = Initialize(unit, roomIndex);
                    if (error != null) return Unsupported(error);
                    movement.Add(new EnemyMovement(unit.Id, 1, definition.Ascends, definition.Loops, definition.CompanionBoss));
                    enteredPyre |= roomIndex == pyre;
                    entered.Add(unit.Id);
                }
                // Native creates and enters the entire chosen group before any OnSpawn runs.
                foreach (int unitId in entered)
                {
                    string? error = SpawnTriggers(unitId);
                    if (error != null) return Unsupported(error);
                    if (Terminal(outcome)) return Finish();
                }
                if (enteredPyre)
                {
                    var room = new RoomCombatState(pyre, source.Train.Rooms[pyre].Deployment, rooms[pyre],
                        source.Train.Rooms[pyre].ExternalInteractions, context);
                    RoomCombatResult combat = RoomCombatModel.Resolve(room);
                    if (!combat.Supported) return Unsupported(combat.UnsupportedReason!);
                    rooms[pyre] = combat.State!.Units.ToList(); context = combat.State.Context; outcome = combat.Outcome;
                    if (Terminal(outcome)) return Finish();
                }
                phase++;
            }
            if (includeTreasure && source.TreasureEnabled && treasureRemaining > 0 &&
                source.Turn >= source.FirstTreasureTurn && phase > 1 && phase < source.Waves.Count && source.Treasures.Count > 0)
            {
                int[] eligible = Enumerable.Range(Math.Max(0, source.FirstTreasureRoom),
                    Math.Max(0, pyre - Math.Max(0, source.FirstTreasureRoom))).Where(index =>
                    rooms[index].Count(unit => unit.Team == CombatTeam.Enemy) < source.Train.EnemySlotsPerRoom).ToArray();
                if (eligible.Length > 0)
                {
                    RngDraw floor = rng.Range(0, eligible.Length); rng = floor.State;
                    RngDraw chosen = rng.Range(0, source.Treasures.Count); rng = chosen.State;
                    EnemyDefinition definition = source.Treasures[chosen.Value];
                    string? error = Validate(definition, context);
                    if (error != null) return Unsupported(error);
                    string? abilityError = AbilityLifecycleModel.SpawnError(definition.Unit, context);
                    if (abilityError != null) return Unsupported(abilityError);
                    CombatUnit unit = AbilityLifecycleModel.SuppressAtSpawn(definition.Create(nextId++), context);
                    Insert(rooms[eligible[floor.Value]], unit);
                    error = Initialize(unit, eligible[floor.Value]);
                    if (error != null) return Unsupported(error);
                    movement.Add(new EnemyMovement(unit.Id, 1, definition.Ascends, definition.Loops, definition.CompanionBoss));
                    error = SpawnTriggers(unit.Id);
                    if (error != null) return Unsupported(error);
                    treasureRemaining--;
                }
            }
            return Finish();

            string? Initialize(CombatUnit unit, int index)
            {
                if (context?.AbilityCardCache != null && unit.Ability?.CardCreation is CardCreationRule creation)
                {
                    AbilityCardResult cached = AbilityCardModel.Get(context, creation);
                    if (!cached.Supported) return cached.UnsupportedReason;
                    context = cached.Context;
                }
                RoomCombatState original = source.Train.Rooms[index];
                return StatusCallbackModel.Initialize(new RoomCombatState(index, original.Deployment, rooms[index],
                    original.ExternalInteractions, context, original.Preview), unit, unit.StatusRegistry ?? unit.Statuses, callbacks,
                    changed => rooms[index][rooms[index].FindIndex(actor => actor.Id == unit.Id)] = changed);
            }

            string? DrainCallbacks()
            {
                string? error = null;
                bool drained = RoomCombatModel.DrainCharacterQueue(callbacks, queued =>
                {
                    RoomCombatState original = source.Train.Rooms[queued.RoomIndex];
                    var room = new RoomCombatState(queued.RoomIndex, original.Deployment, rooms[queued.RoomIndex],
                        original.ExternalInteractions, context, original.Preview);
                    RoomCombatResult result = RoomCombatModel.ApplyQueuedCharacterTrigger(room, queued, callbacks.Add);
                    if (!result.Supported) { error = result.UnsupportedReason; return false; }
                    rooms[queued.RoomIndex] = result.State!.Units.ToList(); context = result.State.Context;
                    if (Terminal(result.Outcome)) outcome = result.Outcome;
                    return true;
                }, queued =>
                {
                    RoomCombatState original = source.Train.Rooms[queued.RoomIndex];
                    RoomCombatResult result = RoomCombatModel.SettleQueuedSpawner(new RoomCombatState(queued.RoomIndex,
                        original.Deployment, rooms[queued.RoomIndex], original.ExternalInteractions, context, original.Preview), queued.Unit);
                    if (!result.Supported) { error = result.UnsupportedReason; return false; }
                    rooms[queued.RoomIndex] = result.State!.Units.ToList(); context = result.State.Context;
                    return true;
                });
                callbacks.Clear();
                return drained ? null : error;
            }

            string? SpawnTriggers(int unitId)
            {
                int index = Array.FindIndex(rooms, room => room.Any(unit => unit.Id == unitId));
                if (index < 0) return null; // An earlier group trigger can remove a later entrant.
                // OnSpawn enters the already populated global queue. Callbacks generated by
                // initial callbacks therefore follow this phase, rather than running before it.
                foreach (string phaseKind in new[] { "OnSpawn", "OnUnscaledSpawn", "OnSpawnNotFromCard" })
                {
                    CombatUnit? actor = rooms[index].FirstOrDefault(unit => unit.Id == unitId);
                    if (actor == null) break;
                    callbacks.Add(new RoomCombatModel.QueuedCharacterTrigger(index, actor, phaseKind));
                    string? error = DrainCallbacks();
                    if (error != null) return error;
                    if (Terminal(outcome)) break;
                }
                return null;
            }

            EnemySpawnResult Finish()
            {
                RoomCombatState[] states = source.Train.Rooms.Select((room, index) => new RoomCombatState(index,
                    room.Deployment, rooms[index], room.ExternalInteractions, context)).ToArray();
                var alive = new HashSet<int>(states.SelectMany(room => room.Units).Select(unit => unit.Id));
                var train = new TrainCombatState(states, movement.Where(rule => alive.Contains(rule.UnitId)).ToArray(),
                    source.Train.EnemySlotsPerRoom, context);
                if (source.CanonicalDecisionReferences) train = TrainCombatModel.ProcessRemovals(train);
                return new EnemySpawnResult(new EnemySpawnState(train, source.Waves, groups, phase, source.Looping,
                    rng, nextId, source.Treasures, treasureRemaining, source.TreasureEnabled, source.FirstTreasureTurn,
                    source.FirstTreasureRoom, source.Turn, source.ExternalInteractions, source.CanonicalDecisionReferences, source.PendingDestroyedUnitIds), outcome);
            }
        }

        private static void Insert(List<CombatUnit> room, CombatUnit unit)
        {
            int playerIndex = room.FindIndex(existing => existing.Team == CombatTeam.Player);
            room.Insert(playerIndex < 0 ? room.Count : playerIndex, unit);
        }
        private static string? Validate(EnemyDefinition definition, CombatContext? context) =>
            RoomCombatModel.Validate(new RoomCombatState(0, false, new[] { definition.Unit }, definition.ExternalInteractions, context));
        private static bool Terminal(RoomOutcome outcome) => outcome == RoomOutcome.BattleWon ||
            outcome == RoomOutcome.PlayerDefeated || outcome == RoomOutcome.Stalemate;
        private static EnemySpawnResult Unsupported(string reason) => new EnemySpawnResult(null, RoomOutcome.Unsupported, reason);
    }
}
