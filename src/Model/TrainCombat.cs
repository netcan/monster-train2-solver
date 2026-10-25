using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class EnemyMovement
    {
        public int UnitId { get; }
        public int Speed { get; }
        public bool Ascends { get; }
        public bool Loops { get; }
        public bool CompanionBoss { get; }
        // Intrinsic speed before movement statuses. The native default is one floor.
        public EnemyMovement(int unitId, int speed, bool ascends, bool loops, bool companionBoss = false)
        { UnitId = unitId; Speed = speed; Ascends = ascends; Loops = loops; CompanionBoss = companionBoss; }
    }

    public sealed class TrainCombatState
    {
        public IReadOnlyList<RoomCombatState> Rooms { get; }
        public IReadOnlyList<EnemyMovement> Movement { get; }
        public int EnemySlotsPerRoom { get; }
        public CombatContext? Context { get; }
        public TrainCombatState(IReadOnlyList<RoomCombatState> rooms, IReadOnlyList<EnemyMovement> movement,
            int enemySlotsPerRoom, CombatContext? context = null)
        {
            RoomCombatState[] orderedRooms = rooms.OrderBy(room => room.RoomIndex).ToArray();
            // Native capture enumerates movement by current floor/front-to-back order,
            // including new lower-floor waves before surviving enemies upstairs.
            var positions = orderedRooms.SelectMany(room => room.Units.Where(unit => unit.Team == CombatTeam.Enemy))
                .Select((unit, index) => new { unit.Id, Index = index }).GroupBy(unit => unit.Id)
                .ToDictionary(group => group.Key, group => group.First().Index);
            Movement = Array.AsReadOnly(movement.OrderBy(rule => positions.TryGetValue(rule.UnitId, out int position) ? position : int.MaxValue).ToArray());
            EnemySlotsPerRoom = enemySlotsPerRoom;
            Context = context;
            var liveUnits = orderedRooms.SelectMany(room => room.Units).GroupBy(unit => unit.Id)
                .ToDictionary(group => group.Key, group => group.First());
            int[] companionBosses = Movement.Where(rule => rule.CompanionBoss).Select(rule => rule.UnitId).Distinct()
                .Where(id => liveUnits.TryGetValue(id, out CombatUnit? unit) && unit.IsBoss == true &&
                    (unit.Health > 0 || unit.Status("undying")?.Stacks > 0)).ToArray();
            IReadOnlyList<int>? roster = companionBosses.Length == 0
                ? orderedRooms.Length > 0 && orderedRooms.Any(room => room.ActiveCompanionBossIds != null)
                    ? Array.Empty<int>() : null
                : Array.AsReadOnly(companionBosses);
            Rooms = Array.AsReadOnly(orderedRooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units, room.ExternalInteractions, room.Context, room.Preview, roster)).ToArray());
        }
    }

    public sealed class TrainCombatResult
    {
        public TrainCombatState? State { get; }
        public RoomOutcome Outcome { get; }
        public IReadOnlyList<RoomCombatResult> RoomResults { get; }
        public IReadOnlyList<int> DestroyedRoomIndexes { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        internal TrainCombatResult(TrainCombatState? state, RoomOutcome outcome,
            IReadOnlyList<RoomCombatResult> roomResults, string? reason = null, IReadOnlyList<int>? destroyedRoomIndexes = null)
        { State = state; Outcome = outcome; RoomResults = Array.AsReadOnly(roomResults.ToArray());
            UnsupportedReason = reason; DestroyedRoomIndexes = Array.AsReadOnly((destroyedRoomIndexes ?? Array.Empty<int>()).Distinct().ToArray()); }
    }

    public static class TrainCombatModel
    {
        // Canonical decision captures normalize references to retired actors.
        // Actual Unity destruction additionally clears a retired owner's own fields.
        // Random-aura revival uses an explicit native protocol which awaits dissolve
        // callbacks before the existing turn-end queue flush; raw frame timing is not
        // inferred from IsDestroyed. Terminal combat skips that native queue flush.
        internal static TrainCombatState ProcessRemovals(TrainCombatState source)
        {
            var activeIds = new HashSet<int>(source.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id));
            return ClearRemovedReferences(source, activeIds);
        }

        internal static TrainCombatState CompleteFrameRemovals(TrainCombatState source,
            IReadOnlyCollection<int>? completedDestructionIds = null)
        {
            source = ProcessRemovals(source);
            BattleSpawnPoints? points = source.Context?.SpawnPoints;
            if (points == null) return source;
            var active = source.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id).ToHashSet();
            // MonsterManager destroys temporary player births on preview reset.
            // Their frame completion is separate from real combat deaths, which
            // remain in the managers' removal lists until ProcessRemovals.
            if (completedDestructionIds != null)
                completedDestructionIds = completedDestructionIds.Concat(points.Units.Where(point => point.SpawnedInPreview &&
                    !active.Contains(point.UnitId) && source.Context!.Enchantments?.RetainedUnits.Any(actor =>
                        actor.Unit.Id == point.UnitId && actor.Unit.Team == CombatTeam.Player && actor.Unit.DeathState?.IsDestroyed == true) == true)
                    .Select(point => point.UnitId)).Distinct().ToArray();
            // LastFeeder is a Unity object reference. It becomes null after its
            // removed owner's frame destruction, even when the food survives.
            // Keep explicitly retained objects whose destruction has not finished.
            var destroyedFeeders = points.Units.Where(unit => source.Context!.QueryFrame?.RunningCombat != false &&
                !active.Contains(unit.UnitId) &&
                (completedDestructionIds == null || completedDestructionIds.Contains(unit.UnitId)) &&
                source.Context!.Enchantments?.RetainedUnits.Any(actor => actor.Unit.Id == unit.UnitId &&
                    actor.Unit.DeathState?.IsDestroyed != true) != true).Select(unit => unit.UnitId).ToHashSet();
            // Canonical decisions retire both owner pointers, matching the capture's
            // logical-removal boundary independently of Unity's dissolve timing.
            // Slot occupants are separate: terminal Pyres and DoNotFullyRemove deaths
            // can still occupy a group slot after their owner's pointers are cleared.
            // In-flight card/room transitions keep their raw last-known points.
            var retained = points.Units.Select(unit => active.Contains(unit.UnitId) ? unit :
                new UnitSpawnPointState(unit.UnitId, null, null, unit.OuterBoss, unit.SpawnedInPreview)).ToArray();
            var usedCopies = retained.SelectMany(unit => new[] { unit.Current, unit.LastKnown })
                .Where(point => point?.PreviewCopyId != null).Select(point => point!.PreviewCopyId!.Value).ToHashSet();
            CombatContext context = source.Context!.WithSpawnPoints(points.WithState(points.Groups.Where(group =>
                !points.NextPreviewCopyId.HasValue || !group.PreviewCopyId.HasValue || usedCopies.Contains(group.PreviewCopyId.Value)).ToArray(), retained));
            if (context.Enchantments?.AutomaticLifecycle == true && context.QueryFrame?.RunningCombat != false)
            {
                EnchantmentWorld world = context.Enchantments;
                // The removal signal still uses the bound source. Unity invalidates
                // that reference when destruction completes at the frame boundary.
                context = context.WithEnchantments(new EnchantmentWorld(world.Rooms.Select(room => room.WithUnits(
                        room.Units.Select(unit => unit.WithoutDestroyedFeeder(destroyedFeeders)).ToArray())).ToArray(),
                    world.Movement, world.EnemySlotsPerRoom,
                    world.RetainedUnits.Select(actor => new EnchantmentRetainedUnit(
                        (completedDestructionIds != null && !completedDestructionIds.Contains(actor.Unit.Id) ? actor.Unit :
                        AfterDestruction(actor.Unit).WithoutRemovedAttacker(new HashSet<int>()).WithTriggers(actor.Unit.Triggers.Select(trigger =>
                            trigger.WithEffects(trigger.Effects.Select(effect => actor.Unit.DeathState?.IsDestroyed == true && effect.Enchantment != null
                                ? effect.WithEnchantment(new EnchantmentRule(effect.Enchantment.Targeting, effect.Enchantment.StatusPool,
                                    effect.Enchantment.State, false, effect.Enchantment.HasParentCard)) : effect).ToArray())).ToArray()))
                            .WithoutDestroyedFeeder(destroyedFeeders),
                        actor.RoomIndex, actor.Preview)).ToArray(), world.EnchanterIds, world.AllowUpdates, world.Updating,
                    world.Preview, world.TestRng, true, world.TemporaryPreviewUnitIds));
            }
            if (destroyedFeeders.Count > 0)
                source = new TrainCombatState(source.Rooms.Select(room => room.WithUnits(room.Units.Select(unit =>
                    unit.WithoutDestroyedFeeder(destroyedFeeders)).ToArray())).ToArray(), source.Movement,
                    source.EnemySlotsPerRoom, source.Context);
            return CardSpellModel.WithContext(source, context);
        }

        private static CombatUnit AfterDestruction(CombatUnit unit)
        {
            if (unit.DeathState?.IsDestroyed != true) return unit;
            unit = unit.AfterDestruction();
            // A living unit can vanish without a death signal. Its statistics
            // listeners persist through the raw room result, then OnDestroy
            // clears them together with the other frame-completed references.
            if (unit.DeathState is { IsDespawned: true } death)
                unit = unit.WithDeathState(new UnitDeathState(death.HasFinishedDying, death.IsBeingRemoved,
                    false, null, death.IsSacrifice, death.StatisticsListenerOnce.HasValue ? false : (bool?)null,
                    death.IsDespawned, death.IsDestroyed));
            return unit;
        }

        private static TrainCombatState ClearRemovedReferences(TrainCombatState source, HashSet<int> activeIds)
        {
            CombatContext? context = source.Context;
            // Native card caches hold weak references to living retained actors
            // too. A preview-born enemy can have no room or point and still be
            // alive after HeroManager restores its primary state.
            var cacheIds = activeIds.Concat(context?.Enchantments?.RetainedUnits
                .Where(actor => actor.Unit.Health > 0 && actor.Unit.DeathState?.IsDestroyed != true)
                .Select(actor => actor.Unit.Id) ?? Array.Empty<int>()).ToHashSet();
            if (context?.LastSpawnedUnitId > 0 && !activeIds.Contains(context.LastSpawnedUnitId.Value)) context = context.WithLastSpawned(0);
            foreach (CardInstanceState card in context?.CardRegistry ?? context?.CardInstances ?? Array.Empty<CardInstanceState>())
            {
                CardInstanceState updated = card;
                if (card.EquippedUnitId > 0 && !activeIds.Contains(card.EquippedUnitId.Value)) updated = updated.WithEquippedUnit(0);
                if (card.RawPlayedRoomUnitIds?.Any(id => !cacheIds.Contains(id)) == true)
                    updated = updated.WithRoomCacheState(card.PlayedRoomUnitIds!.Where(cacheIds.Contains).ToArray(),
                        card.RawPlayedRoomUnitIds.Where(cacheIds.Contains).ToArray());
                if (!ReferenceEquals(updated, card)) context = context!.WithCard(updated);
            }
            if (context?.Enchantments?.AutomaticLifecycle == true)
            {
                EnchantmentWorld world = context.Enchantments;
                context = context.WithEnchantments(new EnchantmentWorld(world.Rooms, world.Movement, world.EnemySlotsPerRoom,
                    world.RetainedUnits.Select(actor => new EnchantmentRetainedUnit(actor.Unit.WithoutRemovedAttacker(activeIds),
                        actor.RoomIndex, actor.Preview)).ToArray(), world.EnchanterIds, world.AllowUpdates, world.Updating,
                    world.Preview, world.TestRng, true, world.TemporaryPreviewUnitIds));
            }
            return EnchantmentWorldModel.Rebase(new TrainCombatState(source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units.Select(unit => unit.WithoutRemovedCompanion(activeIds).WithoutRemovedAttacker(activeIds).WithTriggers(unit.Triggers.Select(trigger =>
                    trigger.WithEffects(trigger.Effects.Select(effect => effect.Summon?.FirstSpawnedUnitId > 0 &&
                        !activeIds.Contains(effect.Summon.FirstSpawnedUnitId) ? effect.WithSummon(effect.Summon.WithFirstSpawned(0)) : effect).ToArray())).ToArray())).ToArray(),
                room.ExternalInteractions, context, room.Preview, room.ActiveCompanionBossIds)).ToArray(),
                source.Movement, source.EnemySlotsPerRoom, context));
        }

        public static TrainCombatResult EndTurnPreHandDiscard(TrainCombatState source, CombatTeam team)
            => CharacterPhase(source, team, "EndTurnPreHandDiscard");

        public static TrainCombatResult PreCombat(TrainCombatState source, CombatTeam team)
            => CharacterPhase(source, team, "PreCombat");

        private static TrainCombatResult CharacterPhase(TrainCombatState source, CombatTeam team, string kind)
        {
            // These two passive phases never dispatch PostCombat, movement or
            // attacks. Retain their deferred original boss effects while still
            // validating all statuses and captured canonical health links.
            string? error = Validate(source, linkedHealthOnly: true);
            if (error != null || team != CombatTeam.Player && team != CombatTeam.Enemy)
                return Unsupported(error ?? "Invalid combat team.");
            RoomCombatState[] rooms = source.Rooms.ToArray();
            CombatContext? context = source.Context;
            var results = new List<RoomCombatResult>();
            var queue = new List<RoomCombatModel.QueuedCharacterTrigger>();
            RoomOutcome outcome = RoomOutcome.Exchanged;
            // Unit identities are assigned at creation. Native active character lists append
            // at creation and keep that order when units change floor or physical position.
            CombatUnit[] candidates = rooms.SelectMany(room => room.Units).Where(unit => unit.Team == team).ToArray();
            if (candidates.Any(unit => unit.Status("purify")?.Stacks > 0) && context?.PurifyBlockedTriggers == null)
                return Unsupported("Purify requires captured trigger queue restrictions.");
            // Native queues the entire team before running its first callback.
            // Later purification cannot cancel an already accepted team phase.
            int[] actors = candidates.Where(unit => unit.Status("purify")?.Stacks <= 0 || unit.Status("purify") == null ||
                    !context!.PurifyBlockedTriggers!.Contains(kind))
                .OrderBy(unit => unit.Id).Select(unit => unit.Id).ToArray();
            foreach (int actor in actors)
            {
                int index = Array.FindIndex(rooms, room => room.Units.Any(unit => unit.Id == actor));
                if (index < 0) continue; // An earlier queued trigger may remove a later actor.
                RoomCombatResult result = RoomCombatModel.ApplyCharacterPhase(WithContext(rooms[index], context), actor, kind, queue.Add,
                    refreshPyre: (changed, placement) => RelicResourceModel.RefreshPyreAttack(rooms, changed, placement));
                if (!result.Supported) return Unsupported(result.UnsupportedReason!);
                rooms[index] = result.State!; context = result.State!.Context; results.Add(result);
                SetCompanionBosses(rooms, result.State!.ActiveCompanionBossIds);
                if (Terminal(result.Outcome))
                { outcome = result.Outcome; break; }
            }
            // Ordinary callbacks drain first; damage deaths then enter native unit removal.
            string? queueError = null;
            bool drained = RoomCombatModel.DrainCharacterQueue(queue, queued =>
            {
                bool tower = NeedsTowerTargets(rooms[queued.RoomIndex]) || NeedsTowerTargets(queued.Unit);
                var targets = tower ? TowerTargets(rooms[queued.RoomIndex], rooms) : Array.Empty<(int RoomIndex, CombatUnit Unit)>();
                RoomCombatResult result = RoomCombatModel.ApplyQueuedCharacterTrigger(WithContext(rooms[queued.RoomIndex], context), queued, queue.Add,
                    refreshPyre: (changed, placement) => RelicResourceModel.RefreshPyreAttack(rooms, changed, placement),
                    externalTargets: tower ? targets : null);
                if (!result.Supported) { queueError = result.UnsupportedReason; return false; }
                rooms[queued.RoomIndex] = result.State!; context = result.State!.Context; results.Add(result);
                if (tower) ApplyTowerUpdates(rooms, targets, result.RetainedUnits);
                SetCompanionBosses(rooms, result.State!.ActiveCompanionBossIds);
                if (Terminal(result.Outcome)) outcome = result.Outcome;
                return true;
            }, queued =>
            {
                RoomCombatResult result = RoomCombatModel.SettleQueuedSpawner(WithContext(rooms[queued.RoomIndex], context), queued.Unit,
                    refreshPyre: (changed, placement) => RelicResourceModel.RefreshPyreAttack(rooms, changed, placement));
                if (!result.Supported) { queueError = result.UnsupportedReason; return false; }
                rooms[queued.RoomIndex] = result.State!; context = result.State!.Context; results.Add(result);
                SetCompanionBosses(rooms, result.State!.ActiveCompanionBossIds);
                return true;
            }, () =>
            {
                TrainCombatResult updated = EnchantmentWorldModel.UpdateAll(Freeze(source, rooms, context), queue.Add);
                if (!updated.Supported) { queueError = updated.UnsupportedReason; return false; }
                rooms = updated.State!.Rooms.ToArray(); context = updated.State.Context; results.AddRange(updated.RoomResults);
                return true;
            }, () => context, message => queueError = message);
            if (!drained) return Unsupported(queueError ?? "Character removal queue failed.");
            return new TrainCombatResult(Freeze(source, rooms, context), outcome, results);
        }

        public static TrainCombatResult ResolveCombat(TrainCombatState source)
        {
            string? error = Validate(source) ?? RelicEndCombatModel.Validate(source.Context);
            if (error != null) return Unsupported(error);
            int[] removedFromDeck = (source.Context?.OtherPiles ?? Array.Empty<CardPileState>())
                .Where(pile => pile.Name == "Purged").SelectMany(pile => pile.Cards)
                .Select(card => card.InstanceId).Distinct().ToArray();
            RoomCombatState[] rooms = source.Rooms.ToArray();
            CombatContext? context = source.Context;
            var results = new List<RoomCombatResult>();
            for (int index = rooms.Length - 1; index >= 0; index--)
            {
                int pyreRoomIndex = rooms.Length - 1;
                bool targetsPyre = index != pyreRoomIndex && rooms[index].Units.Any(unit =>
                    unit.Triggers.Any(trigger => trigger.Effects.Any(effect =>
                        effect.Type is "CardEffectPyreDamageIfNoPlayerUnits" or "CardEffectHealTrain")));
                CombatUnit? externalPyre = targetsPyre ? rooms[pyreRoomIndex].Units.FirstOrDefault(unit => unit.IsPyre) : null;
                if (targetsPyre && externalPyre == null) return Unsupported("Conditional Pyre damage requires a Pyre unit.");
                bool requiresTowerTargets = NeedsTowerTargets(rooms[index]);
                (int RoomIndex, CombatUnit Unit)[] externalTargets = requiresTowerTargets
                    ? TowerTargets(rooms[index], rooms, externalPyre?.Id ?? 0) : Array.Empty<(int, CombatUnit)>();
                RoomCombatResult result = RoomCombatModel.Resolve(WithContext(rooms[index], context),
                    refreshPyre: (changed, placement) => RelicResourceModel.RefreshPyreAttack(rooms, changed, placement),
                    externalPyre: externalPyre, externalPyreRoomIndex: externalPyre == null ? -1 : pyreRoomIndex,
                    externalTargets: requiresTowerTargets ? externalTargets : null);
                if (!result.Supported) return Unsupported("Train combat floor " + index + ": " + result.UnsupportedReason);
                rooms[index] = result.State!; results.Add(result);
                context = result.State!.Context;
                if (externalPyre != null)
                {
                    CombatUnit changedPyre = result.RetainedUnits.Single(unit => unit.Id == externalPyre.Id);
                    rooms[pyreRoomIndex] = rooms[pyreRoomIndex].WithUnits(rooms[pyreRoomIndex].Units.Select(unit =>
                        unit.Id == changedPyre.Id ? changedPyre : unit).Where(unit => unit.Health > 0).ToArray());
                }
                if (requiresTowerTargets) ApplyTowerUpdates(rooms, externalTargets, result.RetainedUnits);
                // A lower-room kill preview can update effect objects owned by
                // an already resolved upper room. Preserve those shared objects
                // before collecting that room again as a later Tower target.
                if (context?.Enchantments is EnchantmentWorld observed)
                    rooms = rooms.Select(room => room.WithUnits(room.Units.Select(unit =>
                        EnchantmentWorldModel.RestorePreviewEffects(unit, observed, false)).ToArray())).ToArray();
                SetCompanionBosses(rooms, result.State!.ActiveCompanionBossIds);
                if (Terminal(result.Outcome))
                    return new TrainCombatResult(Freeze(source, rooms, context), result.Outcome, results);
                if (context?.SpawnPoints != null)
                {
                    foreach (CombatTeam team in new[] { CombatTeam.Enemy, CombatTeam.Player })
                    {
                        var centered = BattleSpawnPointModel.Apply(context.SpawnPoints!, WithContext(rooms[index], context), "Compact", team);
                        if (!centered.Supported) return Unsupported("After train combat floor " + index + ": " + centered.Error);
                        context = context.WithSpawnPoints(centered.State!);
                    }
                    rooms[index] = WithContext(rooms[index], context);
                }
            }
            return FinishCombat(source, rooms, context, RoomOutcome.Cleared, results, removedFromDeck);
        }

        private static TrainCombatResult FinishCombat(TrainCombatState source, RoomCombatState[] rooms,
            CombatContext? context, RoomOutcome outcome, IReadOnlyList<RoomCombatResult> results,
            IReadOnlyCollection<int> removedFromDeck)
        {
            if (context != null)
            {
                context = RelicEndCombatModel.Apply(context, out string? error, removedFromDeck);
                if (error != null) return Unsupported(error);
                // The live hand display refreshes Rail Spike's statistic after the
                // full train settles, rather than inside an individual room.
                if (outcome == RoomOutcome.Cleared && !rooms.Any(room => room.Preview) && context.Statistics?.StoredCards != null &&
                    context.Cards.Hand.Any(token => CardDiscardDrawModel.Present(context.FindCard(token.InstanceId))))
                    context = context.WithStatistics(context.Statistics.RefreshOwnedCards(context.Cards.Hand.Concat(context.Cards.Draw)
                        .Concat(context.Cards.Discard).Concat(context.OtherPiles?.SelectMany(pile => pile.Cards) ?? Array.Empty<CardToken>()).Select(token => token.InstanceId)));
            }
            return new TrainCombatResult(Freeze(source, rooms, context), outcome, results);
        }

        public static TrainCombatResult Ascend(TrainCombatState source)
            => Ascend(source, null, false);

        internal static TrainCombatResult Ascend(TrainCombatState source, int? onlyUnitId, bool forceLoop)
        {
            string? error = Validate(source);
            if (error != null) return Unsupported(error);
            var rooms = source.Rooms.Select(room => room.Units.ToList()).ToArray();
            int pyre = rooms.Length - 1;
            // A pre-combat Bump can already have placed enemies in the Pyre room.
            // Native movement resolves that room even without a new floor entry.
            bool enteredPyre = rooms[pyre].Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0);
            int lastMovementSource = -1, lastPyreEntrySource = enteredPyre ? pyre : -1;
            var moved = new HashSet<int>();
            var movements = source.Movement.ToDictionary(rule => rule.UnitId);
            var looped = new List<int>();
            var shifted = new List<(int Destination, int UnitId)>();
            var movementResults = new List<RoomCombatResult>();
            var destroyedRooms = new List<int>();
            CombatContext? movementContext = source.Context;
            if (source.Rooms.SelectMany(room => room.Units).Any(unit => unit.Team == CombatTeam.Enemy))
            {
                TrainCombatResult disabled = EnchantmentWorldModel.SetAllowUpdates(source, false);
                if (!disabled.Supported) return disabled;
                movementContext = disabled.State!.Context;
            }
            // Reserve destinations from the top down; each enemy moves only once.
            for (int index = pyre; index >= 0; index--)
            {
                int? destroyThroughRoom = null;
                int highestRoomIndexMovedInto = -1;
                foreach (CombatUnit enemy in rooms[index].Where(unit => unit.Team == CombatTeam.Enemy).ToArray())
                {
                    // HeroManager.AddMovableCharactersToList excludes outer bosses;
                    // their own action phases choose and move their reserved anchors.
                    if (enemy.BumpRules?.OuterBoss == true) continue;
                    if (!rooms[index].Any(unit => unit.Id == enemy.Id)) continue;
                    if (onlyUnitId.HasValue && enemy.Id != onlyUnitId) continue;
                    if (!moved.Add(enemy.Id)) continue;
                    if (!movements.TryGetValue(enemy.Id, out EnemyMovement? rule))
                        return Unsupported("Missing enemy movement rule for " + enemy.Id);
                    int speed = rule.Ascends ? rule.Speed : 0;
                    if (enemy.Statuses.Any(status => status.Id == "rooted" || status.Id == "immobile")) speed = 0;
                    else if (speed > 0 && enemy.Statuses.Any(status => status.Id == "haste"))
                        speed = Math.Max(1, pyre - 1 - index);
                    CombatUnit arriving = enemy;
                    if (speed != 1)
                    {
                        CombatStatus[] statuses = enemy.Statuses.Select(status =>
                            (status.Id == "rooted" || status.Id == "haste" || status.Id == "immobile") &&
                            status.RemoveWhenTriggered && (!source.Rooms[index].Deployment || status.RemoveDuringDeployment)
                                ? status.WithStacks(status.Stacks - 1) : status).Where(status => status.Stacks > 0).ToArray();
                        arriving = new CombatUnit(enemy.Id, enemy.AssetKey, enemy.Team, enemy.BaseAttack, enemy.Health,
                            enemy.MaxHealth, enemy.CanAttack, enemy.IsPyre, enemy.EndsBattleOnDeath, statuses, enemy.Triggers, enemy.SpawnerCardId, enemy.Size, enemy.StatusImmunities, enemy.Subtypes, enemy.Modifiers, enemy.IsBoss, enemy.LastAttackerId, enemy.StatusRegistry, enemy.EquipmentCards, enemy.NextTriggerId, enemy.Ability, enemy.StatusDictionary, enemy.AbilityRules, enemy.HordeDefinition, enemy.IsSpawning, enemy.SacrificeCardId, enemy.DeathState, bumpRules: enemy.BumpRules, lastFeederId: enemy.LastFeederId, moonPhaseMask: enemy.MoonPhaseMask);
                    }
                    int destination = Math.Max(0, Math.Min(pyre, index + speed));
                    if ((destination == pyre || forceLoop) && rule.Loops && !enemy.Statuses.Any(status => status.Id == "relentless"))
                        destination = 0;
                    if (destination == index)
                    {
                        if (movementContext?.SpawnPoints != null)
                        {
                            var reference = movementContext.SpawnPoints.Units.Single(unit => unit.UnitId == enemy.Id).Current;
                            int empty = movementContext.SpawnPoints.FirstEmpty(index, CombatTeam.Enemy);
                            if (reference != null && empty >= 0 && empty < reference.Index)
                            {
                                var scope = new RoomCombatState(index, source.Rooms[index].Deployment, rooms[index],
                                    source.Rooms[index].ExternalInteractions, movementContext, source.Rooms[index].Preview,
                                    source.Rooms[index].ActiveCompanionBossIds);
                                var shiftedPoint = BattleSpawnPointModel.Apply(movementContext.SpawnPoints, scope, "Set", CombatTeam.Enemy,
                                    enemy.Id, target: new SpawnPointReference(index, CombatTeam.Enemy, empty));
                                if (!shiftedPoint.Supported) return Unsupported(shiftedPoint.Error!);
                                movementContext = movementContext.WithSpawnPoints(shiftedPoint.State!);
                            }
                        }
                        rooms[index][rooms[index].IndexOf(enemy)] = arriving;
                        continue;
                    }
                    while (destination <= pyre && rooms[destination].Count(unit => unit.Team == CombatTeam.Enemy)
                        >= source.EnemySlotsPerRoom) destination++;
                    if (destination > pyre) return Unsupported("No enemy spawn point remains in the train.");
                    if (movementContext?.SpawnPoints != null)
                    {
                        int empty = movementContext.SpawnPoints.FirstEmpty(destination, CombatTeam.Enemy);
                        var scope = new RoomCombatState(index, source.Rooms[index].Deployment, rooms[index],
                            source.Rooms[index].ExternalInteractions, movementContext, source.Rooms[index].Preview,
                            source.Rooms[index].ActiveCompanionBossIds);
                        var movedPoint = BattleSpawnPointModel.Apply(movementContext.SpawnPoints, scope, "Set", CombatTeam.Enemy,
                            enemy.Id, target: new SpawnPointReference(destination, CombatTeam.Enemy, empty));
                        if (!movedPoint.Supported) return Unsupported(movedPoint.Error!);
                        movementContext = movementContext.WithSpawnPoints(movedPoint.State!);
                    }
                    rooms[index].Remove(enemy);
                    int playerIndex = rooms[destination].FindIndex(unit => unit.Team == CombatTeam.Player);
                    rooms[destination].Insert(playerIndex < 0 ? rooms[destination].Count : playerIndex, arriving);
                    if (destination > index && enemy.Status("relentless") != null)
                        destroyThroughRoom = destination;
                    if (destination < index && rule.Loops && enemy.Status("relentless") == null) looped.Add(enemy.Id);
                    else
                    {
                        shifted.Add((destination, enemy.Id));
                        highestRoomIndexMovedInto = Math.Max(highestRoomIndexMovedInto, destination);
                    }
                    enteredPyre |= destination == pyre;
                    if (destination == pyre) lastPyreEntrySource = index;
                }
                if (destroyThroughRoom.HasValue)
                {
                    for (int floor = index; floor < destroyThroughRoom.Value; floor++)
                    {
                        if (destroyedRooms.Contains(floor)) continue;
                        TrainCombatState current = CurrentMovementState();
                        TrainCombatResult destroyed = DestroyAscendedRoom(current, floor);
                        if (!destroyed.Supported) return destroyed;
                        movementResults.AddRange(destroyed.RoomResults);
                        destroyedRooms.Add(floor);
                        if (Terminal(destroyed.Outcome))
                            return new TrainCombatResult(destroyed.State, destroyed.Outcome, movementResults,
                                destroyedRoomIndexes: destroyedRooms);
                        rooms = destroyed.State!.Rooms.Select(room => room.Units.ToList()).ToArray();
                        movementContext = destroyed.State.Context;
                    }
                }
                if (highestRoomIndexMovedInto >= 0 && !source.Rooms[index].Preview &&
                    movementContext?.QueryFrame?.SelectedRoomIndex != null)
                {
                    movementContext = movementContext.WithQueryFrame(movementContext.QueryFrame.With(
                        selectedRoomIndex: highestRoomIndexMovedInto));
                    lastMovementSource = index;
                }
            }
            RoomCombatState[] next = source.Rooms.Select((room, index) => new RoomCombatState(room.RoomIndex,
                room.Deployment, BattleSpawnPointModel.Order(movementContext?.SpawnPoints, index, rooms[index]), room.ExternalInteractions,
                movementContext, room.Preview, room.ActiveCompanionBossIds)).ToArray();
            TrainCombatState moving = EnchantmentWorldModel.Rebase(new TrainCombatState(next, source.Movement, source.EnemySlotsPerRoom, movementContext));
            // DoAscension reserves all destinations first, then compacts physical
            // enemy points. A looping enemy can reserve a slot behind units that
            // subsequently leave its destination; that reservation is not final.
            if (moving.Context?.SpawnPoints != null && !source.Rooms.Any(room => room.Preview))
            {
                CombatContext compactContext = moving.Context;
                foreach (RoomCombatState room in moving.Rooms.Reverse())
                {
                    // A companion-only DoAscension updates the moved actor's
                    // destination; unrelated source-floor actors keep their slots.
                    if (onlyUnitId.HasValue && !room.Units.Any(unit => unit.Id == onlyUnitId.Value)) continue;
                    var compact = BattleSpawnPointModel.Apply(compactContext.SpawnPoints!, WithContext(room, compactContext), "Compact", CombatTeam.Enemy);
                    if (!compact.Supported) return Unsupported(compact.Error!);
                    compactContext = compactContext.WithSpawnPoints(compact.State!);
                }
                moving = Freeze(moving, moving.Rooms.ToArray(), compactContext);
            }
            moving = Freeze(moving, moving.Rooms.ToArray(), moving.Context);
            RoomAttachmentUnitSyncResult attachmentSync = RoomAttachmentUnitModel.SynchronizeMovement(source, moving);
            if (!attachmentSync.Supported) return Unsupported(attachmentSync.UnsupportedReason!);
            moving = attachmentSync.State!;
            if (looped.Count > 0)
            {
                var callbacks = looped.Select(id => (id, "OnTrainRoomLoop")).Concat(looped.SelectMany(id =>
                    new[] { (id, "PostAscension"), (id, "OnShift") })).ToArray();
                TrainCombatResult fired = ApplyMovementTriggers(moving, callbacks);
                if (!fired.Supported) return fired;
                movementResults.AddRange(fired.RoomResults);
                if (Terminal(fired.Outcome)) return new TrainCombatResult(fired.State, fired.Outcome, movementResults,
                    destroyedRoomIndexes: destroyedRooms);
                moving = fired.State!;
            }
            // HeroManager reserves every destination first, then drains each
            // destination's movement queue from the top down. Pyre combat can
            // stop the coroutine before lower-floor PostAscension callbacks.
            foreach (var group in shifted.GroupBy(item => item.Destination)
                .Where(group => !enteredPyre || group.Key == pyre).OrderByDescending(group => group.Key))
            {
                TrainCombatResult fired = ApplyMovementTriggers(moving, group.SelectMany(item =>
                    new[] { (item.UnitId, "PostAscension"), (item.UnitId, "OnShift") }).ToArray());
                if (!fired.Supported) return fired;
                movementResults.AddRange(fired.RoomResults);
                if (Terminal(fired.Outcome)) return new TrainCombatResult(fired.State, fired.Outcome, movementResults,
                    destroyedRoomIndexes: destroyedRooms);
                moving = fired.State!;
            }
            next = moving.Rooms.ToArray();
            // Enemies that reach the Pyre fight immediately during ascension, within this same turn.
            // CombatManager.IsCombatInRoomDisabledForFinalBoss suppresses the
            // ascension tower pass when every living enemy has Life Link.
            bool pairedAngelArrival = shifted.Any(arrival => arrival.Destination == pyre && next[pyre].Units.Any(unit =>
                unit.Id == arrival.UnitId && OuterBossDefinitions.IsAngelDuoInner(unit.AssetKey) &&
                unit.BumpRules?.PairedBossUnitId > 0 && next.SelectMany(room => room.Units).Any(partner =>
                    partner.Id == unit.BumpRules.PairedBossUnitId && partner.Health > 0)));
            if (enteredPyre && !pairedAngelArrival && next[pyre].Units.Any(unit => unit.Team == CombatTeam.Enemy &&
                unit.Health > 0 && unit.Status("lifelink") == null))
            {
                TrainCombatResult enabled = EnchantmentWorldModel.SetAllowUpdates(moving, true);
                if (!enabled.Supported) return enabled;
                // Tower attacks retain HeroManager's last selected floor.
                RoomCombatState pyreRoom = enabled.State!.Rooms[pyre];
                bool requiresTowerTargets = NeedsTowerTargets(pyreRoom);
                (int RoomIndex, CombatUnit Unit)[] externalTargets = requiresTowerTargets
                    ? TowerTargets(pyreRoom, next) : Array.Empty<(int, CombatUnit)>();
                RoomCombatResult result = RoomCombatModel.Resolve(pyreRoom,
                    refreshPyre: (changed, placement) => RelicResourceModel.RefreshPyreAttack(next, changed, placement),
                    updateSelectedRoom: false, externalTargets: requiresTowerTargets ? externalTargets : null);
                if (!result.Supported) return Unsupported(result.UnsupportedReason!);
                next[pyre] = result.State!;
                if (requiresTowerTargets) ApplyTowerUpdates(next, externalTargets, result.RetainedUnits);
                movementResults.Add(result);
                moving = Freeze(enabled.State, next, result.State!.Context);
                // Native resolves tower combat within the top-down movement loop. Later
                // lower-floor movement selects its destination after tower PostCombat callbacks.
                if (!Terminal(result.Outcome) && lastMovementSource >= 0 && lastMovementSource < lastPyreEntrySource &&
                    movementContext?.QueryFrame?.SelectedRoomIndex is int laterSelection && moving.Context?.QueryFrame != null)
                    moving = Freeze(moving, moving.Rooms.ToArray(), moving.Context.WithQueryFrame(
                        moving.Context.QueryFrame.With(selectedRoomIndex: laterSelection)));
                if (moving.Context?.Enchantments?.AutomaticLifecycle != true &&
                    !shifted.Any(item => item.Destination != pyre))
                    return new TrainCombatResult(moving, result.Outcome, movementResults,
                        destroyedRoomIndexes: destroyedRooms);
                if (Terminal(result.Outcome)) return new TrainCombatResult(moving, result.Outcome, movementResults,
                    destroyedRoomIndexes: destroyedRooms);
                TrainCombatResult disabled = EnchantmentWorldModel.SetAllowUpdates(moving, false);
                if (!disabled.Supported) return disabled;
                moving = disabled.State!;
            }
            foreach (var group in shifted.GroupBy(item => item.Destination)
                .Where(group => enteredPyre && group.Key != pyre).OrderByDescending(group => group.Key))
            {
                TrainCombatResult fired = ApplyMovementTriggers(moving, group.SelectMany(item =>
                    new[] { (item.UnitId, "PostAscension"), (item.UnitId, "OnShift") }).ToArray());
                if (!fired.Supported) return fired;
                movementResults.AddRange(fired.RoomResults);
                if (Terminal(fired.Outcome)) return new TrainCombatResult(fired.State, fired.Outcome, movementResults,
                    destroyedRoomIndexes: destroyedRooms);
                moving = fired.State!;
            }
            if (source.Rooms.SelectMany(room => room.Units).Any(unit => unit.Team == CombatTeam.Enemy))
            {
                TrainCombatResult enabled = EnchantmentWorldModel.SetAllowUpdates(moving, true);
                if (!enabled.Supported) return enabled;
                TrainCombatResult orderChanged = EnchantmentWorldModel.UpdateAll(enabled.State!);
                if (!orderChanged.Supported) return orderChanged;
                moving = orderChanged.State!;
                movementResults.AddRange(enabled.RoomResults); movementResults.AddRange(orderChanged.RoomResults);
            }
            return new TrainCombatResult(moving, RoomOutcome.Cleared, movementResults,
                destroyedRoomIndexes: destroyedRooms);

            TrainCombatState CurrentMovementState()
            {
                RoomCombatState[] currentRooms = source.Rooms.Select((room, roomIndex) => new RoomCombatState(room.RoomIndex,
                    room.Deployment, BattleSpawnPointModel.Order(movementContext?.SpawnPoints, roomIndex, rooms[roomIndex]),
                    room.ExternalInteractions, movementContext, room.Preview, room.ActiveCompanionBossIds)).ToArray();
                TrainCombatState current = EnchantmentWorldModel.Rebase(new TrainCombatState(currentRooms,
                    source.Movement, source.EnemySlotsPerRoom, movementContext));
                return Freeze(current, current.Rooms.ToArray(), current.Context);
            }
        }

        internal static TrainCombatResult DestroyAscendedRoom(TrainCombatState source, int roomIndex)
        {
            RoomCombatState? originalRoom = source.Rooms.FirstOrDefault(room => room.RoomIndex == roomIndex);
            if (originalRoom == null) return Unsupported("Missing room destroyed by relentless ascension.");
            TrainCombatState current = source;
            var results = new List<RoomCombatResult>();
            foreach (CombatUnit listed in originalRoom.Units.ToArray())
            {
                int currentRoomIndex = Array.FindIndex(current.Rooms.ToArray(), room => room.Units.Any(unit => unit.Id == listed.Id));
                if (currentRoomIndex < 0) continue;
                CombatUnit? actor = current.Rooms[currentRoomIndex].Units.FirstOrDefault(unit => unit.Id == listed.Id);
                if (actor == null || actor.Health <= 0 || actor.DeathState?.IsDestroyed == true || actor.BumpRules?.OuterBoss == true)
                    continue;
                CombatStatus? undying = actor.RegisteredStatus("undying");
                if (undying?.Stacks > 0)
                {
                    RoomCombatResult removed = StatusRemovalModel.Remove(WithContext(current.Rooms[currentRoomIndex], current.Context),
                        actor.Id, "undying", undying.Stacks);
                    if (!removed.Supported) return Unsupported(removed.UnsupportedReason!);
                    results.Add(removed);
                    current = ReplaceRoom(current, removed.State!);
                    if (removed.PendingCallbacks.Count > 0)
                    {
                        TrainCombatResult callbacks = ApplyCharacterQueue(current, removed.PendingCallbacks.ToList());
                        if (!callbacks.Supported) return callbacks;
                        results.AddRange(callbacks.RoomResults);
                        current = callbacks.State!;
                        if (Terminal(callbacks.Outcome))
                            return new TrainCombatResult(current, callbacks.Outcome, results);
                    }
                    currentRoomIndex = Array.FindIndex(current.Rooms.ToArray(), room => room.Units.Any(unit => unit.Id == listed.Id));
                    if (currentRoomIndex < 0) continue;
                    actor = current.Rooms[currentRoomIndex].Units.FirstOrDefault(unit => unit.Id == listed.Id);
                    if (actor == null || actor.Health <= 0 || actor.DeathState?.IsDestroyed == true) continue;
                }
                var queue = new List<RoomCombatModel.QueuedCharacterTrigger>();
                RoomCombatResult sacrificed = RoomCombatModel.QueueSacrifice(
                    WithContext(current.Rooms[currentRoomIndex], current.Context), actor, 0, queue.Add);
                if (!sacrificed.Supported) return Unsupported(sacrificed.UnsupportedReason!);
                results.Add(sacrificed);
                current = ReplaceRoom(current, sacrificed.State!);
                if (queue.Count > 0)
                {
                    TrainCombatResult callbacks = ApplyCharacterQueue(current, queue);
                    if (!callbacks.Supported) return callbacks;
                    results.AddRange(callbacks.RoomResults);
                    current = callbacks.State!;
                    if (Terminal(callbacks.Outcome))
                        return new TrainCombatResult(current, callbacks.Outcome, results);
                }
            }
            return new TrainCombatResult(current, RoomOutcome.Exchanged, results);
        }

        private static TrainCombatState ReplaceRoom(TrainCombatState source, RoomCombatState replacement)
        {
            RoomCombatState[] rooms = source.Rooms.ToArray();
            int index = Array.FindIndex(rooms, room => room.RoomIndex == replacement.RoomIndex);
            if (index < 0) throw new InvalidOperationException("A room result references a missing train room.");
            rooms[index] = replacement;
            return Freeze(source, rooms, replacement.Context);
        }

        internal static TrainCombatResult ProcessDelayedStatusRemovalsAfterCardPlayed(TrainCombatState source)
        {
            TrainCombatState current = source;
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            foreach (RoomCombatState initialRoom in source.Rooms)
            foreach (CombatUnit initialUnit in initialRoom.Units)
            foreach (CombatStatus status in initialUnit.Statuses.Where(status => status.MarkedForRemovalAfterCardPlayed).ToArray())
            {
                RoomCombatState room = current.Rooms.Single(item => item.RoomIndex == initialRoom.RoomIndex);
                CombatUnit? unit = room.Units.FirstOrDefault(item => item.Id == initialUnit.Id);
                if (unit?.Status(status.Id) == null) continue;
                RoomCombatResult removed = StatusRemovalModel.Remove(WithContext(room, current.Context), unit.Id, status.Id, -1);
                if (!removed.Supported) return new TrainCombatResult(null, RoomOutcome.Unsupported, [], removed.UnsupportedReason);
                current = ReplaceRoom(current, removed.State!);
                callbacks.AddRange(removed.PendingCallbacks);
            }
            return callbacks.Count == 0 ? new TrainCombatResult(current, RoomOutcome.Exchanged, []) :
                ApplyCharacterQueue(current, callbacks);
        }

        internal static TrainCombatResult ApplyMovementTriggers(TrainCombatState source, IReadOnlyList<(int UnitId, string Kind)> triggers)
        {
            var queue = new List<RoomCombatModel.QueuedCharacterTrigger>();
            foreach (var trigger in triggers)
            {
                int index = source.Rooms.ToList().FindIndex(room => room.Units.Any(unit => unit.Id == trigger.UnitId));
                if (index >= 0) queue.Add(new RoomCombatModel.QueuedCharacterTrigger(index,
                    source.Rooms[index].Units.First(unit => unit.Id == trigger.UnitId), trigger.Kind));
                if (index >= 0 && trigger.Kind == "OnShift") QueueSentries(source.Rooms[index], trigger.UnitId, queue);
            }
            return ApplyCharacterQueue(source, queue, centerRelicDeaths: false);
        }

        public static TrainCombatResult Sentry(TrainCombatState source, int movedUnitId)
        {
            string? error = Validate(source);
            if (error != null) return Unsupported(error);
            RoomCombatState? room = source.Rooms.FirstOrDefault(item => item.Units.Any(unit => unit.Id == movedUnitId));
            if (room == null) return Unsupported("Sentry requires the moved character in its destination room.");
            var queue = new List<RoomCombatModel.QueuedCharacterTrigger>();
            QueueSentries(room, movedUnitId, queue);
            return ApplyCharacterQueue(source, queue);
        }

        private static void QueueSentries(RoomCombatState room, int movedUnitId, List<RoomCombatModel.QueuedCharacterTrigger> queue)
        {
            CombatUnit moved = room.Units.Single(unit => unit.Id == movedUnitId);
            foreach (CombatUnit actor in room.Units.Where(unit => unit.Team != moved.Team))
                queue.Add(new RoomCombatModel.QueuedCharacterTrigger(room.RoomIndex, actor, "OnSentry", overrideTarget: moved));
        }

        internal static TrainCombatResult ApplyCharacterQueue(TrainCombatState source,
            List<RoomCombatModel.QueuedCharacterTrigger> queue, bool centerRelicDeaths = true)
        {
            RoomCombatState[] rooms = source.Rooms.ToArray(); CombatContext? context = source.Context;
            var results = new List<RoomCombatResult>(); RoomOutcome outcome = RoomOutcome.Exchanged; string? error = null;
            bool drained = RoomCombatModel.DrainCharacterQueue(queue, queued =>
            {
                int roomPosition = Array.FindIndex(rooms, room => room.RoomIndex == queued.RoomIndex);
                if (roomPosition < 0) { error = "Character trigger room is not present in train state."; return false; }
                int pyrePosition = Array.FindIndex(rooms, room => room.Units.Any(unit => unit.IsPyre));
                bool adjustsPyre = queued.Unit.Triggers.Where(trigger => trigger.Kind == queued.Kind).Any(trigger =>
                    trigger.Effects.Any(effect => effect.Type == "CardEffectPermanentlyAdjustMaxPyreHealth"));
                CombatUnit? externalPyre = adjustsPyre && pyrePosition >= 0 && pyrePosition != roomPosition
                    ? rooms[pyrePosition].Units.Single(unit => unit.IsPyre) : null;
                bool tower = NeedsTowerTargets(rooms[roomPosition]) || NeedsTowerTargets(queued.Unit);
                var targets = tower ? TowerTargets(rooms[roomPosition], rooms, externalPyre?.Id ?? 0)
                    : Array.Empty<(int RoomIndex, CombatUnit Unit)>();
                RoomCombatResult fired = RoomCombatModel.ApplyQueuedCharacterTrigger(WithContext(rooms[roomPosition], context), queued, queue.Add,
                    refreshPyre: (changed, placement) => RelicResourceModel.RefreshPyreAttack(rooms, changed, placement),
                    externalPyre: externalPyre, externalPyreRoomIndex: externalPyre == null ? -1 : rooms[pyrePosition].RoomIndex,
                    externalTargets: tower ? targets : null);
                if (!fired.Supported) { error = fired.UnsupportedReason; return false; }
                rooms[roomPosition] = fired.State!; context = fired.State!.Context; results.Add(fired);
                if (externalPyre != null)
                {
                    CombatUnit changed = fired.RetainedUnits.Single(unit => unit.Id == externalPyre.Id);
                    rooms[pyrePosition] = rooms[pyrePosition].WithUnits(rooms[pyrePosition].Units.Select(unit =>
                        unit.Id == changed.Id ? changed : unit).ToArray());
                }
                if (tower) ApplyTowerUpdates(rooms, targets, fired.RetainedUnits);
                SetCompanionBosses(rooms, fired.State!.ActiveCompanionBossIds);
                if (Terminal(fired.Outcome)) outcome = fired.Outcome;
                return true;
            }, queued =>
            {
                int roomPosition = Array.FindIndex(rooms, room => room.RoomIndex == queued.RoomIndex);
                if (roomPosition < 0) { error = "Character removal room is not present in train state."; return false; }
                RoomCombatResult returned;
                // Ascension orders the room before dispatching PostAscension. Damage
                // from that queue removes its victims without another order pass.
                if (!centerRelicDeaths && queued.RelicDamageRemoval)
                    returned = RoomCombatModel.SettleQueuedSpawner(WithContext(rooms[roomPosition], context), queued.Unit,
                        refreshPyre: (changed, placement) => RelicResourceModel.RefreshPyreAttack(rooms, changed, placement));
                else if (queued.RelicDamageRemoval && queued.CompletePhysicalRemovalAfterQueue && context?.SpawnPoints != null)
                {
                    bool multipleDeaths = queue.Count(item => item.RelicDamageRemoval && item.CompletePhysicalRemovalAfterQueue &&
                        item.Kind == "OnDeath" && item.RoomIndex == queued.RoomIndex && item.Unit.Team == queued.Unit.Team) > 1;
                    UnitSpawnPointState? point = context.SpawnPoints.Units.FirstOrDefault(item => item.UnitId == queued.Unit.Id);
                    SpawnPointGroupState? group = point?.Current == null ? null :
                        context.SpawnPoints.SelectedGroup(queued.RoomIndex, queued.Unit.Team);
                    bool vacancyBefore = !multipleDeaths && point?.Current != null && point.Current.Index > 0 &&
                        group?.Occupants.Take(point.Current.Index).Any(id => id == 0) == true;
                    if (vacancyBefore)
                    {
                        RoomCombatState currentRoom = WithContext(rooms[roomPosition], context);
                        CombatUnit[] retainedDeaths = queue.Where(item => item.Kind == "OnDeath" &&
                                item.RoomIndex == queued.RoomIndex && item.Unit.Team == queued.Unit.Team)
                            .Select(item => item.Unit).GroupBy(item => item.Id).Select(group => group.Last())
                            .Where(item => !currentRoom.Units.Any(unit => unit.Id == item.Id)).ToArray();
                        if (retainedDeaths.Length > 0)
                            currentRoom = new RoomCombatState(currentRoom.RoomIndex, currentRoom.Deployment,
                                currentRoom.Units.Concat(retainedDeaths).ToArray(), currentRoom.ExternalInteractions,
                                currentRoom.Context, currentRoom.Preview, currentRoom.ActiveCompanionBossIds);
                        BattleSpawnPointResult compacted = BattleSpawnPointModel.Apply(context.SpawnPoints, currentRoom,
                            "Compact", queued.Unit.Team);
                        if (!compacted.Supported) { error = compacted.Error; return false; }
                        context = context.WithSpawnPoints(compacted.State!);
                    }
                    bool lastPhysicalDeath = multipleDeaths && !queue.Any(item => item.RelicDamageRemoval &&
                        item.CompletePhysicalRemovalAfterQueue && item.Kind == "OnDeath" && item.RoomIndex == queued.RoomIndex &&
                        item.Unit.Team == queued.Unit.Team && item.Unit.Id > queued.Unit.Id);
                    returned = lastPhysicalDeath
                        ? RoomCombatModel.SettleQueuedSpawnerAndCenter(WithContext(rooms[roomPosition], context), queued.Unit,
                            refreshPyre: (changed, placement) => RelicResourceModel.RefreshPyreAttack(rooms, changed, placement))
                        : RoomCombatModel.SettleQueuedSpawner(WithContext(rooms[roomPosition], context), queued.Unit,
                            refreshPyre: (changed, placement) => RelicResourceModel.RefreshPyreAttack(rooms, changed, placement));
                }
                else
                {
                    bool lastPhysicalDeath = queued.CompletePhysicalRemovalAfterQueue && !queue.Any(item => item.Kind == "OnDeath" &&
                        item.Unit.Team == queued.Unit.Team && item.Unit.Id > queued.Unit.Id);
                    returned = lastPhysicalDeath
                        ? RoomCombatModel.SettleQueuedSpawnerAndCenter(WithContext(rooms[roomPosition], context), queued.Unit,
                            refreshPyre: (changed, placement) => RelicResourceModel.RefreshPyreAttack(rooms, changed, placement))
                        : RoomCombatModel.SettleQueuedSpawner(WithContext(rooms[roomPosition], context), queued.Unit,
                        refreshPyre: (changed, placement) => RelicResourceModel.RefreshPyreAttack(rooms, changed, placement));
                }
                if (!returned.Supported) { error = returned.UnsupportedReason; return false; }
                rooms[roomPosition] = returned.State!; context = returned.State!.Context; results.Add(returned);
                SetCompanionBosses(rooms, returned.State!.ActiveCompanionBossIds); return true;
            }, () =>
            {
                TrainCombatResult updated = EnchantmentWorldModel.UpdateAll(Freeze(source, rooms, context), queue.Add);
                if (!updated.Supported) { error = updated.UnsupportedReason; return false; }
                rooms = updated.State!.Rooms.ToArray(); context = updated.State.Context; results.AddRange(updated.RoomResults);
                return true;
            }, () => context, message => error = message);
            if (!drained) return Unsupported(error ?? "Movement callback queue failed.");
            TrainCombatResult sentries = SentryAttackModel.UpdateAll(Freeze(source, rooms, context));
            return sentries.Supported ? new TrainCombatResult(sentries.State,
                sentries.Outcome == RoomOutcome.Exchanged ? outcome : sentries.Outcome, results.Concat(sentries.RoomResults).ToArray()) : sentries;
        }

        private static void SetCompanionBosses(RoomCombatState[] rooms, IReadOnlyList<int>? ids)
        {
            if (ids == null)
            {
                for (int index = 0; index < rooms.Length; index++) rooms[index] = rooms[index].WithCompanionBosses(null);
                return;
            }
            int[] companionIds = ids.Distinct().ToArray();
            var activeIds = companionIds.ToHashSet();
            bool singleSurvivor = companionIds.Length == 1;
            for (int index = 0; index < rooms.Length; index++)
            {
                RoomCombatState room = rooms[index];
                CombatUnit[] units = room.Units.Select(unit => activeIds.Contains(unit.Id)
                    ? unit.WithEndsBattleOnDeath(singleSurvivor) : unit).ToArray();
                rooms[index] = new RoomCombatState(room.RoomIndex, room.Deployment, units,
                    room.ExternalInteractions, room.Context, room.Preview, companionIds);
            }
        }

        internal static TrainCombatResult ResolvePyreCombat(TrainCombatState source)
        {
            string? error = Validate(source);
            if (error != null) return Unsupported(error);
            RoomCombatState[] rooms = source.Rooms.ToArray();
            int pyre = rooms.Length - 1;
            bool external = NeedsTowerTargets(rooms[pyre]);
            var targets = external ? TowerTargets(rooms[pyre], rooms) : Array.Empty<(int RoomIndex, CombatUnit Unit)>();
            RoomCombatResult result = RoomCombatModel.Resolve(rooms[pyre],
                refreshPyre: (changed, placement) => RelicResourceModel.RefreshPyreAttack(rooms, changed, placement),
                updateSelectedRoom: false, externalTargets: external ? targets : null);
            if (!result.Supported) return Unsupported(result.UnsupportedReason!);
            rooms[pyre] = result.State!;
            if (external) ApplyTowerUpdates(rooms, targets, result.RetainedUnits);
            return new TrainCombatResult(Freeze(source, rooms, result.State!.Context), result.Outcome, new[] { result });
        }

        private static (int RoomIndex, CombatUnit Unit)[] TowerTargets(RoomCombatState activeRoom,
            IReadOnlyList<RoomCombatState> rooms, int excludedUnitId = 0) => rooms
            .Where(room => room.RoomIndex != activeRoom.RoomIndex)
            .SelectMany(room => room.Units.Where(unit => unit.Id != excludedUnitId)
                .Select(unit => (room.RoomIndex, unit))).ToArray();

        private static bool NeedsTowerTargets(RoomCombatState room) => room.Units.Any(NeedsTowerTargets);

        private static bool NeedsTowerTargets(CombatUnit unit) => unit.Triggers.SelectMany(trigger => trigger.Effects)
            .Any(effect => effect.Action?.Target is "Tower" or "FrontInAllRooms" || effect.UnitUpgrade?.Target == "Tower");

        private static void ApplyTowerUpdates(RoomCombatState[] rooms,
            IReadOnlyList<(int RoomIndex, CombatUnit Unit)> targets, IReadOnlyList<CombatUnit> retained)
        {
            if (targets.Count == 0) return;
            var targetIds = targets.Select(target => target.Unit.Id).ToHashSet();
            var changed = retained.Where(unit => targetIds.Contains(unit.Id)).ToDictionary(unit => unit.Id);
            for (int index = 0; index < rooms.Length; index++)
            {
                RoomCombatState room = rooms[index];
                if (!room.Units.Any(unit => targetIds.Contains(unit.Id))) continue;
                rooms[index] = room.WithUnits(room.Units.Select(unit => changed.TryGetValue(unit.Id, out CombatUnit? updated)
                    ? updated : unit).Where(unit => unit.DeathState?.IsDestroyed != true).ToArray());
            }
        }

        private static RoomCombatState WithContext(RoomCombatState room, CombatContext? context) =>
            EnchantmentWorldModel.Refresh(new RoomCombatState(room.RoomIndex, room.Deployment, room.Units,
                room.ExternalInteractions, context, room.Preview, room.ActiveCompanionBossIds));

        private static TrainCombatState Freeze(TrainCombatState source, RoomCombatState[] rooms, CombatContext? context)
        {
            rooms = rooms.Select(room => WithContext(room, context)).ToArray();
            var alive = new HashSet<int>(rooms.SelectMany(room => room.Units).Select(unit => unit.Id));
            return new TrainCombatState(rooms.Select(room => WithContext(room, context)).ToArray(),
                source.Movement.Concat((context?.Enchantments?.Movement ?? Array.Empty<EnemyMovement>())
                    .Where(rule => !source.Movement.Any(old => old.UnitId == rule.UnitId)))
                    .Where(rule => alive.Contains(rule.UnitId)).ToArray(), source.EnemySlotsPerRoom, context);
        }

        private static bool Terminal(RoomOutcome outcome) => outcome == RoomOutcome.BattleWon ||
            outcome == RoomOutcome.PlayerDefeated || outcome == RoomOutcome.Stalemate;

        internal static string? Validate(TrainCombatState source, bool linkedHealthOnly = false)
        {
            string? relicError = RelicModel.Validate(source.Context);
            if (relicError != null) return relicError;
            string? identityError = UnitIdentityModel.Validate(source.Context, source.Rooms.SelectMany(room => room.Units));
            if (identityError != null) return identityError;
            if (source.Context != null && AbilityCardModel.Validate(source.Context) is string cacheError) return cacheError;
            if (source.Rooms.Count < 2 || source.EnemySlotsPerRoom < 1 ||
                source.Rooms.Where((room, index) => room.RoomIndex != index).Any())
                return "Train rooms must be contiguous and include a Pyre room.";
            if (source.Rooms.SelectMany(room => room.Units).GroupBy(unit => unit.Id).Any(group => group.Count() > 1))
                return "Train unit identities must be unique.";
            if (source.Movement.GroupBy(rule => rule.UnitId).Any(group => group.Count() > 1))
                return "Enemy movement identities must be unique.";
            foreach (RoomCombatState room in source.Rooms)
            {
                string? error = RoomCombatModel.Validate(room, linkedHealthOnly: linkedHealthOnly);
                if (error != null) return error;
            }
            return null;
        }

        private static TrainCombatResult Unsupported(string error) => new TrainCombatResult(null,
            RoomOutcome.Unsupported, Array.Empty<RoomCombatResult>(), error);
    }
}
