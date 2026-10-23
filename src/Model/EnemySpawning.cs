using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class EnemyBirthStatAdjustment
    {
        public int Attack { get; }
        public int Health { get; }
        public IReadOnlyList<string> CovenantIds { get; }
        public EnemyBirthStatAdjustment(int attack, int health, IReadOnlyList<string> covenantIds)
        { Attack = attack; Health = health; CovenantIds = Array.AsReadOnly(covenantIds.ToArray()); }
    }

    public sealed class EnemyDefinition
    {
        public CombatUnit Unit { get; }
        public bool Ascends { get; }
        public bool Loops { get; }
        public bool CompanionBoss { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public EnemyBirthStatAdjustment? BirthStats { get; }
        // Archive hydration uses the sole public constructor. Keep the older
        // entry point for the existing friend assemblies that capture units.
        internal EnemyDefinition(CombatUnit unit, bool ascends, bool loops, IReadOnlyList<string> externalInteractions, bool companionBoss = false)
            : this(unit, ascends, loops, externalInteractions, companionBoss, null) { }
        public EnemyDefinition(CombatUnit unit, bool ascends, bool loops, IReadOnlyList<string> externalInteractions, bool companionBoss,
            EnemyBirthStatAdjustment? birthStats)
        { Unit = unit; Ascends = ascends; Loops = loops; ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray()); CompanionBoss = companionBoss; BirthStats = birthStats; }
        internal CombatUnit Create(int id, bool applyBirthStats = true) => new CombatUnit(id, Unit.AssetKey, CombatTeam.Enemy,
            checked(Unit.BaseAttack + (applyBirthStats ? BirthStats?.Attack ?? 0 : 0)), checked(Unit.Health + (applyBirthStats ? BirthStats?.Health ?? 0 : 0)),
            checked(Unit.MaxHealth + (applyBirthStats ? BirthStats?.Health ?? 0 : 0)), Unit.CanAttack, false, Unit.EndsBattleOnDeath,
            Unit.Statuses, Unit.Triggers, size: Unit.Size, statusImmunities: Unit.StatusImmunities, subtypes: Unit.Subtypes,
            modifiers: applyBirthStats && BirthStats != null && BirthStats.Attack != 0 ? Unit.Modifiers!.WithDamageBuff(checked(Unit.Modifiers.DamageBuff + BirthStats.Attack)) : Unit.Modifiers,
            isBoss: Unit.IsBoss, lastAttackerId: Unit.LastAttackerId.HasValue ? 0 : null, statusRegistry: Unit.StatusRegistry, equipmentCards: Unit.EquipmentCards, nextTriggerId: Unit.NextTriggerId, ability: Unit.Ability, statusDictionary: Unit.StatusDictionary, abilityRules: Unit.AbilityRules, hordeDefinition: Unit.HordeDefinition, isSpawning: Unit.IsSpawning, sacrificeCardId: Unit.SacrificeCardId, deathState: Unit.DeathState, bumpRules: Unit.BumpRules, lastFeederId: Unit.LastFeederId, moonPhaseMask: Unit.MoonPhaseMask);
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
        public OuterBossProgress? OuterBoss { get; }
        public bool ShouldSerializeOuterBoss() => OuterBoss != null;
        public OuterBossSpawnDefinition? QueuedOuterBoss { get; }
        public bool ShouldSerializeQueuedOuterBoss() => QueuedOuterBoss != null;
        public TrueFinalBossSpawnDefinition? QueuedTrueFinalBoss { get; }
        public bool ShouldSerializeQueuedTrueFinalBoss() => QueuedTrueFinalBoss != null;
        public TrueFinalBossPhaseFlags? TrueFinalBoss { get; }
        public bool ShouldSerializeTrueFinalBoss() => TrueFinalBoss != null;
        public EnemySpawnState(TrainCombatState train, IReadOnlyList<EnemyWave> waves,
            IReadOnlyList<int> selectedGroups, int phase, bool looping, UnityRng rng, int nextUnitId,
            IReadOnlyList<EnemyDefinition> treasures, int treasuresRemaining, bool treasureEnabled,
            int firstTreasureTurn, int firstTreasureRoom, int turn, IReadOnlyList<string> externalInteractions,
            bool canonicalDecisionReferences = false, IReadOnlyList<int>? pendingDestroyedUnitIds = null,
            OuterBossProgress? outerBoss = null, OuterBossSpawnDefinition? queuedOuterBoss = null,
            TrueFinalBossSpawnDefinition? queuedTrueFinalBoss = null, TrueFinalBossPhaseFlags? trueFinalBoss = null)
        {
            Train = train; Waves = Array.AsReadOnly(waves.ToArray()); SelectedGroups = Array.AsReadOnly(selectedGroups.ToArray());
            Phase = phase; Looping = looping; Rng = rng; NextUnitId = nextUnitId;
            Treasures = Array.AsReadOnly(treasures.ToArray()); TreasuresRemaining = treasuresRemaining;
            TreasureEnabled = treasureEnabled; FirstTreasureTurn = firstTreasureTurn; FirstTreasureRoom = firstTreasureRoom;
            Turn = turn; ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
            CanonicalDecisionReferences = canonicalDecisionReferences;
            PendingDestroyedUnitIds = pendingDestroyedUnitIds == null ? null : Array.AsReadOnly(pendingDestroyedUnitIds.ToArray());
            RoomCombatState? bossRoom = outerBoss == null ? null : train.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == outerBoss.UnitId));
            OuterBoss = bossRoom == null ? null : outerBoss!.WithRoom(bossRoom.RoomIndex);
            QueuedOuterBoss = OuterBoss == null ? queuedOuterBoss : null;
            QueuedTrueFinalBoss = OuterBoss == null ? queuedTrueFinalBoss : null;
            TrueFinalBoss = trueFinalBoss;
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
            string? identityError = UnitIdentityModel.Validate(context, source.Train.Rooms.SelectMany(room => room.Units), nextId);
            if (identityError != null) return Unsupported(identityError);
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
                if (wave.Candidates[selected].Units.Count > 0 && context?.QueryFrame?.SelectedRoomIndex != null)
                    context = context.WithQueryFrame(context.QueryFrame.With(selectedRoomIndex: 0));
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
                    UnitIdentityAllocation allocated = UnitIdentityModel.Allocate(context, context?.NextUnitId ?? nextId);
                    if (!allocated.Supported) return Unsupported(allocated.UnsupportedReason!);
                    nextId = allocated.NextUnitId; context = allocated.Context;
                    CombatUnit unit = AbilityLifecycleModel.SuppressAtSpawn(definition.Create(allocated.UnitId), context);
                    Insert(rooms[roomIndex], unit);
                    movement.Add(new EnemyMovement(unit.Id, 1, definition.Ascends, definition.Loops, definition.CompanionBoss));
                    error = Initialize(unit, roomIndex);
                    if (error != null) return Unsupported(error);
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
                        source.Train.Rooms[pyre].ExternalInteractions, context, source.Train.Rooms[pyre].Preview,
                        source.Train.Rooms[pyre].ActiveCompanionBossIds);
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
                    if (context?.QueryFrame?.SelectedRoomIndex != null)
                        context = context.WithQueryFrame(context.QueryFrame.With(selectedRoomIndex: eligible[floor.Value]));
                    RngDraw chosen = rng.Range(0, source.Treasures.Count); rng = chosen.State;
                    EnemyDefinition definition = source.Treasures[chosen.Value];
                    string? error = Validate(definition, context);
                    if (error != null) return Unsupported(error);
                    string? abilityError = AbilityLifecycleModel.SpawnError(definition.Unit, context);
                    if (abilityError != null) return Unsupported(abilityError);
                    UnitIdentityAllocation allocated = UnitIdentityModel.Allocate(context, context?.NextUnitId ?? nextId);
                    if (!allocated.Supported) return Unsupported(allocated.UnsupportedReason!);
                    nextId = allocated.NextUnitId; context = allocated.Context;
                    CombatUnit unit = AbilityLifecycleModel.SuppressAtSpawn(definition.Create(allocated.UnitId), context);
                    Insert(rooms[eligible[floor.Value]], unit);
                    movement.Add(new EnemyMovement(unit.Id, 1, definition.Ascends, definition.Loops, definition.CompanionBoss));
                    error = Initialize(unit, eligible[floor.Value]);
                    if (error != null) return Unsupported(error);
                    error = SpawnTriggers(unit.Id);
                    if (error != null) return Unsupported(error);
                    treasureRemaining--;
                }
            }
            return Finish();

            string? Initialize(CombatUnit unit, int index)
            {
                if (context?.SpawnPoints != null)
                {
                    var physicalRoom = source.Train.Rooms[index];
                    var scope = new RoomCombatState(index, physicalRoom.Deployment, rooms[index].Where(actor => actor.Id != unit.Id).ToArray(),
                        physicalRoom.ExternalInteractions, context, physicalRoom.Preview, physicalRoom.ActiveCompanionBossIds);
                    var born = BattleSpawnPointModel.Birth(context.SpawnPoints, scope, unit,
                        context.SpawnPoints.FirstEmpty(index, unit.Team), shift: false);
                    if (!born.Supported) return born.Error;
                    context = context.WithSpawnPoints(born.State!);
                    rooms[index] = BattleSpawnPointModel.Order(context.SpawnPoints, index, rooms[index]).ToList();
                }
                if (context?.AbilityCardCache != null && unit.Ability?.CardCreation is CardCreationRule creation)
                {
                    AbilityCardResult cached = AbilityCardModel.Get(context, creation);
                    if (!cached.Supported) return cached.UnsupportedReason;
                    context = cached.Context;
                }
                RoomCombatState original = source.Train.Rooms[index];
                if (context?.Enchantments?.AutomaticLifecycle == true)
                    context = EnchantmentWorldModel.Rebase(new TrainCombatState(source.Train.Rooms.Select((room, position) =>
                        new RoomCombatState(position, room.Deployment, rooms[position], room.ExternalInteractions, context,
                            room.Preview, room.ActiveCompanionBossIds)).ToArray(),
                        movement, source.Train.EnemySlotsPerRoom, context)).Context;
                string? error = StatusCallbackModel.Initialize(new RoomCombatState(index, original.Deployment, rooms[index],
                    original.ExternalInteractions, context, original.Preview, original.ActiveCompanionBossIds),
                    unit, unit.StatusRegistry ?? unit.Statuses, callbacks,
                    changed => rooms[index][rooms[index].FindIndex(actor => actor.Id == unit.Id)] = changed,
                    ImportRoom);
                if (error != null) return error;
                RoomCombatResult relics = RelicSpawnStatusModel.CharacterAdded(new RoomCombatState(index, original.Deployment,
                    rooms[index], original.ExternalInteractions, context, original.Preview, original.ActiveCompanionBossIds), unit.Id, 0, true);
                if (!relics.Supported) return relics.UnsupportedReason;
                ImportRoom(relics.State!); callbacks.AddRange(relics.PendingCallbacks);
                RoomCombatState attachmentRoom = new RoomCombatState(index, original.Deployment, rooms[index],
                    original.ExternalInteractions, context, original.Preview, original.ActiveCompanionBossIds);
                RoomCombatState? attached = RoomAttachmentUnitModel.ApplyToNewCharacter(attachmentRoom, unit.Id, out error);
                if (error != null || attached == null) return error ?? "Room attachment enemy-birth synchronization failed.";
                attached = EnchantmentWorldModel.Sync(attached);
                context = attached.Context; rooms[index] = attached.Units.ToList();
                if (context?.QueryFrame?.MoonPhase is int moon &&
                    MoonPhaseStatusModel.IsTracking(context, rooms.SelectMany(room => room)))
                {
                    int liveIndex = rooms[index].FindIndex(actor => actor.Id == unit.Id);
                    // Native heroes initialize off-train, before their physical
                    // placement. Room overrides enter understanding only during
                    // the later global/resource refresh, not this spawn boundary.
                    if (liveIndex >= 0)
                    {
                        CombatUnit initialized = MoonPhaseStatusModel.WithPhase(rooms[index][liveIndex], moon).WithMoonPhaseMask(moon);
                        rooms[index][liveIndex] = initialized;
                        // Setup queues understanding callbacks off-train. They
                        // join the shared queue before any group OnSpawn runs.
                        string gained = moon == 2 ? "OnMoonLit" : "OnMoonShade";
                        if (moon is 1 or 2 && initialized.Triggers.Any(trigger => trigger.Kind == gained))
                            callbacks.Add(new RoomCombatModel.QueuedCharacterTrigger(index, initialized, gained));
                    }
                }
                return null;
            }

            void ImportRoom(RoomCombatState changed)
            {
                context = changed.Context; rooms[changed.RoomIndex] = changed.Units.ToList();
                // A spawn/status callback can update aura caches on a different floor.
                // Keep every local room synchronized before the final train rebase.
                if (context?.Enchantments?.AutomaticLifecycle == true)
                    foreach (RoomCombatState shared in context.Enchantments.Rooms)
                        if (shared.RoomIndex != changed.RoomIndex) rooms[shared.RoomIndex] = shared.Units.ToList();
            }

            string? DrainCallbacks()
            {
                string? error = null;
                bool drained = RoomCombatModel.DrainCharacterQueue(callbacks, queued =>
                {
                    RoomCombatState original = source.Train.Rooms[queued.RoomIndex];
                    var room = EnchantmentWorldModel.Refresh(new RoomCombatState(queued.RoomIndex, original.Deployment, rooms[queued.RoomIndex],
                        original.ExternalInteractions, context, original.Preview, original.ActiveCompanionBossIds));
                    RoomCombatResult result = RoomCombatModel.ApplyQueuedCharacterTrigger(room, queued, callbacks.Add);
                    if (!result.Supported) { error = result.UnsupportedReason; return false; }
                    ImportRoom(result.State!);
                    if (Terminal(result.Outcome)) outcome = result.Outcome;
                    return true;
                }, queued =>
                {
                    RoomCombatState original = source.Train.Rooms[queued.RoomIndex];
                    RoomCombatResult result = RoomCombatModel.SettleQueuedSpawner(new RoomCombatState(queued.RoomIndex,
                        original.Deployment, rooms[queued.RoomIndex], original.ExternalInteractions, context, original.Preview,
                        original.ActiveCompanionBossIds), queued.Unit);
                    if (!result.Supported) { error = result.UnsupportedReason; return false; }
                    ImportRoom(result.State!);
                    return true;
                }, () =>
                {
                    RoomCombatState original = source.Train.Rooms[0];
                    RoomCombatResult updated = EnchantmentWorldModel.CompleteQueuedRemovals(new RoomCombatState(original.RoomIndex,
                        original.Deployment, rooms[0], original.ExternalInteractions, context, original.Preview,
                        original.ActiveCompanionBossIds), callbacks.Add);
                    if (!updated.Supported) { error = updated.UnsupportedReason; return false; }
                    ImportRoom(updated.State!); return true;
                }, () => context, message => error = message);
                callbacks.Clear();
                return drained ? null : error;
            }

            string? SpawnTriggers(int unitId)
            {
                int index = Array.FindIndex(rooms, room => room.Any(unit => unit.Id == unitId));
                if (index < 0) return null; // An earlier group trigger can remove a later entrant.
                RoomCombatState original = source.Train.Rooms[index];
                RoomCombatResult relics = RelicSpawnStatusModel.CharacterAdded(new RoomCombatState(index, original.Deployment,
                    rooms[index], original.ExternalInteractions, context, original.Preview, original.ActiveCompanionBossIds), unitId, 0, false);
                if (!relics.Supported) return relics.UnsupportedReason;
                ImportRoom(EnchantmentWorldModel.Sync(relics.State!)); callbacks.AddRange(relics.PendingCallbacks);
                TrainCombatState addedTrain = new TrainCombatState(source.Train.Rooms.Select((room, position) =>
                    new RoomCombatState(position, room.Deployment, rooms[position], room.ExternalInteractions, context,
                        room.Preview, room.ActiveCompanionBossIds)).ToArray(), movement, source.Train.EnemySlotsPerRoom, context);
                RelicSummonDamageResult summonRelics = RelicSummonDamageModel.CharacterAdded(addedTrain, unitId);
                if (!summonRelics.Supported) return summonRelics.UnsupportedReason;
                if (summonRelics.TargetUnitIds.Count != 0)
                    return "Enemy birth unexpectedly applied player-only summon damage.";
                context = summonRelics.State!.Context;
                // OnSpawn enters the already populated global queue. Callbacks generated by
                // initial callbacks therefore follow this phase, rather than running before it.
                foreach (string phaseKind in new[] { "OnSpawn", "OnUnscaledSpawn", "OnSpawnNotFromCard", "AfterSpawnEnchant" })
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
                    room.Deployment, rooms[index], room.ExternalInteractions, context, room.Preview,
                    room.ActiveCompanionBossIds)).ToArray();
                var alive = new HashSet<int>(states.SelectMany(room => room.Units).Select(unit => unit.Id));
                var train = EnchantmentWorldModel.Rebase(new TrainCombatState(states, movement.Where(rule => alive.Contains(rule.UnitId)).ToArray(),
                    source.Train.EnemySlotsPerRoom, context));
                if (source.CanonicalDecisionReferences) train = TrainCombatModel.ProcessRemovals(train);
                return new EnemySpawnResult(new EnemySpawnState(train, source.Waves, groups, phase, source.Looping,
                    rng, context?.NextUnitId ?? nextId, source.Treasures, treasureRemaining, source.TreasureEnabled, source.FirstTreasureTurn,
                    source.FirstTreasureRoom, source.Turn, source.ExternalInteractions, source.CanonicalDecisionReferences, source.PendingDestroyedUnitIds,
                    source.OuterBoss, source.QueuedOuterBoss, source.QueuedTrueFinalBoss, source.TrueFinalBoss), outcome);
            }
        }

        private static void Insert(List<CombatUnit> room, CombatUnit unit)
        {
            int playerIndex = room.FindIndex(existing => existing.Team == CombatTeam.Player);
            room.Insert(playerIndex < 0 ? room.Count : playerIndex, unit);
        }
        internal static string? ValidateBirthStats(EnemyDefinition definition, CombatContext? context)
        {
            if (definition.BirthStats is { } stats &&
                (!TrueFinalBossCovenantModel.MatchesOrigins(context, stats) || definition.Unit.Modifiers == null ||
                    (long)definition.Unit.BaseAttack + stats.Attack < 0 || (long)definition.Unit.BaseAttack + stats.Attack > int.MaxValue ||
                    (long)definition.Unit.Health + stats.Health <= 0 || (long)definition.Unit.Health + stats.Health > int.MaxValue ||
                    (long)definition.Unit.MaxHealth + stats.Health <= 0 || (long)definition.Unit.MaxHealth + stats.Health > int.MaxValue ||
                    (long)definition.Unit.Modifiers.DamageBuff + stats.Attack is < int.MinValue or > int.MaxValue))
                return "Enemy birth stats require a matching calibrated original Covenant and bounded native stats.";
            return null;
        }
        private static string? Validate(EnemyDefinition definition, CombatContext? context)
        {
            string? statsError = ValidateBirthStats(definition, context);
            if (statsError != null) return statsError;
            // Hero Setup runs before EnterTower. Native Conduit's stacks-added
            // callback dereferences the physical room even for a starting status.
            if (definition.Unit.Status("conduit")?.Stacks > 0)
                return "Enemy starting Conduit applies stacks off-train; a post-entry status callback is required.";
            return RoomCombatModel.Validate(new RoomCombatState(0, false, new[] { definition.Unit }, definition.ExternalInteractions, context));
        }
        private static bool Terminal(RoomOutcome outcome) => outcome == RoomOutcome.BattleWon ||
            outcome == RoomOutcome.PlayerDefeated || outcome == RoomOutcome.Stalemate;
        private static EnemySpawnResult Unsupported(string reason) => new EnemySpawnResult(null, RoomOutcome.Unsupported, reason);
    }
}
