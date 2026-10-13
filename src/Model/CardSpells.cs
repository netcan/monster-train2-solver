using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class SpellCastCheck
    {
        public bool CanPlay { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => UnsupportedReason == null;
        public UnityRng? BattleRngAfterTests { get; }
        internal SpellCastCheck(bool canPlay, string? error = null, UnityRng? battleRngAfterTests = null)
        { CanPlay = canPlay; UnsupportedReason = error; BattleRngAfterTests = battleRngAfterTests; }
    }

    public sealed class SpellTargetCollection
    {
        public int EffectIndex { get; }
        public IReadOnlyList<int> UnitIds { get; }
        internal SpellTargetCollection(int effectIndex, IReadOnlyList<int> unitIds)
        { EffectIndex = effectIndex; UnitIds = Array.AsReadOnly(unitIds.ToArray()); }
    }

    public sealed class TrainSpellResult
    {
        public TrainCombatState? State { get; }
        public RoomOutcome Outcome { get; }
        public IReadOnlyList<CombatEvent> Events { get; }
        public string? UnsupportedReason { get; }
        public IReadOnlyList<SpellTargetCollection> TargetCollections { get; }
        public IReadOnlyList<CardPileState>? OtherPiles { get; }
        public IReadOnlyList<CombatUnit> RetainedUnits { get; }
        public BattlePlayRules? PlayRules { get; }
        internal IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> PendingCallbacks { get; }
        public bool Supported => State != null;
        internal TrainSpellResult(TrainCombatState? state, RoomOutcome outcome, IReadOnlyList<CombatEvent> events, string? error = null,
            IReadOnlyList<SpellTargetCollection>? collections = null, IReadOnlyList<CardPileState>? otherPiles = null,
            IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger>? pendingCallbacks = null, IReadOnlyList<CombatUnit>? retainedUnits = null,
            BattlePlayRules? playRules = null)
        { State = state; Outcome = outcome; Events = Array.AsReadOnly(events.ToArray()); UnsupportedReason = error;
            TargetCollections = Array.AsReadOnly((collections ?? Array.Empty<SpellTargetCollection>()).ToArray());
            RetainedUnits = Array.AsReadOnly((retainedUnits ?? Array.Empty<CombatUnit>()).ToArray());
            OtherPiles = otherPiles == null ? null : Array.AsReadOnly(otherPiles.ToArray());
            PlayRules = playRules;
            PendingCallbacks = Array.AsReadOnly((pendingCallbacks ?? Array.Empty<RoomCombatModel.QueuedCharacterTrigger>()).ToArray()); }
    }

    public static class CardSpellModel
    {
        internal static string? UnisolatedUiRangeReason(BattleTurnState state) => !state.UiRngIsolated &&
            state.PlayRules?.Cards.Any(card => card.Effects.Concat(card.OnCastEffects).Concat(card.OnUnplayedNegativeEffects)
                .Any(effect => QuantityTest(effect) && effect.Range != null && effect.Tests?.ShouldTest != false)) == true
                ? "Vanilla UI quantity tests consume Battle RNG at uncaptured frame boundaries; isolate UI RNG before battle simulation." : null;
        public static bool RequiresUnitTarget(IReadOnlyList<CardActionEffect> effects) =>
            effects.Any(effect => effect.Type != "HandUpgrade" && effect.Target == "DropTargetCharacter");

        // Native casting tests all requested effects against the unchanged pre-cast state.
        // Any success permits the cast, unless an explicitly mandatory effect fails.
        public static SpellCastCheck TestPlay(RoomCombatState source, IReadOnlyList<CardActionEffect> effects, int targetId,
            int sourceCardId = 0)
            => TestPlayCore(SingleRoom(source), source.RoomIndex, effects, targetId, fullTrain: false, sourceCardId: sourceCardId);

        public static SpellCastCheck TestPlay(TrainCombatState source, int roomIndex, IReadOnlyList<CardActionEffect> effects, int targetId,
            BattlePlayRules? definitions = null, int selfUnitId = 0, int targetSpawnPointIndex = -1, int sourceCardId = 0)
            => TestPlayCore(source, roomIndex, effects, targetId, fullTrain: true, definitions, selfUnitId, targetSpawnPointIndex, sourceCardId);

        private static SpellCastCheck TestPlayCore(TrainCombatState source, int roomIndex, IReadOnlyList<CardActionEffect> effects,
            int targetId, bool fullTrain, BattlePlayRules? definitions = null, int selfUnitId = 0, int targetSpawnPointIndex = -1,
            int sourceCardId = 0)
        {
            string? error = Validate(source, roomIndex, effects, targetId, fullTrain, selfUnitId, targetSpawnPointIndex, sourceCardId);
            if (error != null) return new SpellCastCheck(false, error);
            int sourceAttackUnitId = selfUnitId > 0 ? selfUnitId : targetId;
            if (effects.Any(effect => effect.Type == "DamagePerSourceAttack") &&
                (sourceAttackUnitId <= 0 || source.Rooms.SelectMany(item => item.Units).All(unit => unit.Id != sourceAttackUnitId)))
                return new SpellCastCheck(false, "Source-attack damage requires a captured caster unit.");
            RoomCombatState room = source.Rooms.Single(item => item.RoomIndex == roomIndex);
            CombatUnit? initial = room.Units.FirstOrDefault(unit => unit.Id == targetId);
            int dropPosition = DropPosition(room, initial);
            IReadOnlyList<int> last = Array.Empty<int>();
            bool passed = false;
            UnityRng testingRng = source.Context!.BattleRng;
            for (int index = 0; index < effects.Count; index++)
            {
                CardActionEffect effect = effects[index];
                if (effect.Tests?.ShouldTest == false) continue;
                CardTargets targets = Collect(source, roomIndex, effect, last, initial, dropPosition, index, definitions,
                    isTesting: true, selfUnitId: selfUnitId, sourceCardId: sourceCardId);
                if (!targets.Supported) return new SpellCastCheck(false, targets.UnsupportedReason);
                if (!CardTargetModel.IsRandom(effect.Target))
                {
                    string? damageTargetError = DamagePerHpTargetError(source, effect, targets, selfUnitId);
                    if (damageTargetError != null) return new SpellCastCheck(false, damageTargetError);
                }
                Remember(effect, index, targets, ref last);
                string? targetError = AttackTestError(source, roomIndex, effect);
                if (targetError != null) return new SpellCastCheck(false, targetError);
                CardActionEffect tested = effect;
                if (QuantityTest(effect) && effect.Type != "DamagePerSourceAttack") tested = Sample(effect, ref testingRng);
                int testCount = TestCount(source, effect, targets);
                if (testCount < 0) return new SpellCastCheck(false, "All-status adjustment requires complete propagation metadata.");
                bool valid = effect.Type == "AbilityTeleport"
                    ? UnitTeleportModel.CanPlay(source, selfUnitId, roomIndex, targetSpawnPointIndex)
                    : PassesTest(tested, testCount, source.Context!.AllScenarioBossesDead == true, source.Context, room.Preview, room,
                        PyreAlive(source, fullTrain), sourceCardId) && PassesTargetSubtype(source, effect, targets);
                if (!valid && effect.Tests?.FailToCast == true) return new SpellCastCheck(false, battleRngAfterTests: testingRng);
                passed |= valid;
            }
            return new SpellCastCheck(passed, battleRngAfterTests: testingRng);
        }

        public static RoomCombatResult Apply(RoomCombatState source, IReadOnlyList<CardActionEffect> effects, int targetId, int sourceCardId = 0,
            int? playerCapacity = null, int? enemyCapacity = null, BattlePlayRules? definitions = null,
            Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null)
        {
            TrainSpellResult result = ApplyCore(SingleRoom(source), source.RoomIndex, effects, targetId, sourceCardId,
                playerCapacity, enemyCapacity, definitions, fullTrain: false, refreshPyre: refreshPyre);
            return new RoomCombatResult(result.State?.Rooms.Single(), result.Outcome, 0, result.Events.ToList(), result.UnsupportedReason);
        }

        public static TrainSpellResult Apply(TrainCombatState source, int roomIndex, IReadOnlyList<CardActionEffect> effects,
            int targetId, int sourceCardId = 0, BattlePlayRules? definitions = null, IReadOnlyList<CardPileState>? otherPiles = null, int selfUnitId = 0)
            => ApplyCore(source, roomIndex, effects, targetId, sourceCardId, null, null, definitions, fullTrain: true, otherPiles, selfUnitId);

        internal static TrainSpellResult ApplyForCardPlay(TrainCombatState source, int roomIndex, IReadOnlyList<CardActionEffect> effects,
            int targetId, int sourceCardId, BattlePlayRules definitions, IReadOnlyList<CardPileState>? otherPiles, int selfUnitId = 0,
            int targetSpawnPointIndex = -1, IReadOnlyList<CardRecursionSelection>? recursionSelections = null, bool onCastEffects = false,
            bool onUnplayedNegativeEffects = false)
            => ApplyCore(source, roomIndex, effects, targetId, sourceCardId, null, null, definitions, true, otherPiles,
                selfUnitId, deferFinalCallbacks: true, targetSpawnPointIndex: targetSpawnPointIndex,
                recursionSelections: recursionSelections, onCastEffects: onCastEffects,
                onUnplayedNegativeEffects: onUnplayedNegativeEffects);

        internal static TrainSpellResult ApplyNativeDamageStep(TrainCombatState source, int roomIndex, int targetId, int damage,
            int sourceCardId, IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> pendingDeaths, int attackerUnitId = 0,
            Action<RoomCombatModel.QueuedCharacterTrigger>? enqueueCharacterTrigger = null,
            Func<TrainCombatState, TrainCombatState?>? drainQueuedTriggers = null,
            Func<int, CombatUnit?>? getRetainedUnit = null)
        {
            TrainCombatState state = source;
            RelicPyreAttackRefreshResult Refresh(CombatContext context, bool placement) => RefreshTrain(ref state, context, placement);
            RoomCombatState? initial = source.Rooms.FirstOrDefault(room => room.RoomIndex == roomIndex);
            RoomCombatState? attackerRoom = attackerUnitId > 0
                ? source.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == attackerUnitId)) : null;
            CombatUnit? attacker = attackerRoom?.Units.FirstOrDefault(unit => unit.Id == attackerUnitId);
            bool externalAttacker = attacker != null && attackerRoom!.RoomIndex != roomIndex;
            if (initial == null || source.Context?.SpawnPoints == null || !source.Context.NextUnitId.HasValue ||
                attackerUnitId > 0 && attacker == null ||
                pendingDeaths.Any(queued => !source.Rooms.Any(room => room.RoomIndex == queued.RoomIndex) ||
                queued.Kind != "OnDeath" || !queued.DeferUntilRemoval || queued.Unit.Health > 0 || queued.Unit.Id <= 0 ||
                queued.Unit.DeathState?.HasFinishedDying != true || queued.Unit.Id >= source.Context.NextUnitId ||
                source.Rooms.Any(room => room.Units.Any(unit => unit.Id == queued.Unit.Id))) ||
                pendingDeaths.Select(queued => queued.Unit.Id).Distinct().Count() != pendingDeaths.Count)
                return UnsupportedTrain("Invalid global spell damage phase.");
            RoomCombatResult FinishQueue(RoomCombatState frame)
            {
                state = ReplaceRoom(state, frame);
                if (drainQueuedTriggers != null)
                {
                    TrainCombatState? drainedState = drainQueuedTriggers(state);
                    if (drainedState == null)
                        return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, [], "Queued spell callbacks failed before deferred deaths.");
                    state = drainedState;
                }
                var queue = pendingDeaths.ToList();
                var events = new List<CombatEvent>();
                RoomOutcome outcome = RoomOutcome.Exchanged;
                string? error = null;
                RoomCombatState RestoreUnfinished(RoomCombatResult result) => new RoomCombatState(result.State!.RoomIndex,
                    result.State.Deployment, result.State.Units.Concat(result.RetainedUnits.Where(unit => unit.Health <= 0 &&
                        unit.DeathState?.HasFinishedDying == false)).ToArray(), result.State.ExternalInteractions, result.State.Context,
                    result.State.Preview, result.State.ActiveCompanionBossIds);
                bool drained = RoomCombatModel.DrainCharacterQueue(queue, queued =>
                {
                    RoomCombatState room = state.Rooms.Single(item => item.RoomIndex == queued.RoomIndex);
                    var fired = RoomCombatModel.ApplyQueuedCharacterTrigger(room, queued, queue.Add, refreshPyre: Refresh);
                    if (!fired.Supported) { error = fired.UnsupportedReason; return false; }
                    state = ReplaceRoom(state, RestoreUnfinished(fired)); events.AddRange(fired.Events);
                    if (fired.Outcome != RoomOutcome.Exchanged) outcome = fired.Outcome;
                    return true;
                }, queued =>
                {
                    RoomCombatState room = state.Rooms.Single(item => item.RoomIndex == queued.RoomIndex);
                    bool lastInTeam = !queue.Any(item => item.Kind == "OnDeath" && item.Unit.Team == queued.Unit.Team &&
                        item.Unit.Id > queued.Unit.Id);
                    var returned = lastInTeam ? RoomCombatModel.SettleQueuedSpawnerAndCenter(room, queued.Unit, refreshPyre: Refresh) :
                        RoomCombatModel.SettleQueuedSpawner(room, queued.Unit, refreshPyre: Refresh);
                    if (!returned.Supported) { error = returned.UnsupportedReason; return false; }
                    state = ReplaceRoom(state, RestoreUnfinished(returned)); events.AddRange(returned.Events);
                    return true;
                }, () =>
                {
                    TrainCombatResult updated = EnchantmentWorldModel.UpdateAll(state, queue.Add);
                    if (!updated.Supported) { error = updated.UnsupportedReason; return false; }
                    state = updated.State!; return true;
                }, () => state.Context, message => error = message);
                CombatUnit? updatedAttacker = externalAttacker
                    ? state.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == attackerUnitId) ?? getRetainedUnit?.Invoke(attackerUnitId)
                    : null;
                if (externalAttacker && updatedAttacker == null)
                    error ??= "External spell attacker was lost while draining deferred deaths.";
                return new RoomCombatResult(drained && error == null ? state.Rooms.Single(room => room.RoomIndex == roomIndex) : null,
                    drained && error == null ? outcome : RoomOutcome.Unsupported, 0, events, error,
                    retainedUnits: updatedAttacker == null ? null : [updatedAttacker]);
            }
            var applied = RoomCombatModel.ApplyNativeCardDamageAfterTraits(initial, targetId, damage, sourceCardId,
                attackerUnitId,
                pendingDeaths.Count == 0 ? null : (Func<RoomCombatState, RoomCombatResult>)FinishQueue, refreshPyre: Refresh,
                externalActor: externalAttacker ? attacker : null,
                externalActorRoomIndex: externalAttacker ? attackerRoom!.RoomIndex : -1,
                enqueueCharacterTrigger: externalAttacker ? enqueueCharacterTrigger : null);
            if (!applied.Supported) return UnsupportedTrain(applied.UnsupportedReason!);
            state = ReplaceRoom(state, applied.State!);
            var retainedUnits = applied.RetainedUnits.ToList();
            if (externalAttacker)
            {
                CombatUnit? updatedAttacker = retainedUnits.FirstOrDefault(unit => unit.Id == attackerUnitId);
                if (updatedAttacker == null)
                    return UnsupportedTrain("Native spell damage did not retain its remote source actor.");
                state = ReplaceExternalActor(state, attackerRoom!.RoomIndex, updatedAttacker);
                retainedUnits.RemoveAll(unit => unit.Id == attackerUnitId);
                if (!ActorIsLive(updatedAttacker)) retainedUnits.Add(updatedAttacker);
            }
            return new TrainSpellResult(state, applied.Outcome, applied.Events, otherPiles: state.Context!.OtherPiles,
                pendingCallbacks: applied.PendingCallbacks, retainedUnits: retainedUnits);
        }

        private static RelicPyreAttackRefreshResult RefreshTrain(ref TrainCombatState state, CombatContext context, bool placement)
        {
            TrainCombatResult changed = RelicResourceModel.RefreshPyreAttack(WithContext(state, context), placement);
            if (!changed.Supported) return new RelicPyreAttackRefreshResult(null, null, changed.UnsupportedReason);
            state = changed.State!;
            return new RelicPyreAttackRefreshResult(state.Context, state.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.IsPyre));
        }

        private static TrainSpellResult ApplyCore(TrainCombatState source, int roomIndex, IReadOnlyList<CardActionEffect> effects,
            int targetId, int sourceCardId, int? playerCapacity, int? enemyCapacity, BattlePlayRules? definitions, bool fullTrain,
            IReadOnlyList<CardPileState>? otherPiles = null, int selfUnitId = 0, bool deferFinalCallbacks = false,
            Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null, int targetSpawnPointIndex = -1,
            IReadOnlyList<CardRecursionSelection>? recursionSelections = null, bool onCastEffects = false,
            bool onUnplayedNegativeEffects = false)
        {
            string? selectionError = CardRecursionModel.ValidateSelections(effects, recursionSelections);
            if (selectionError != null) return UnsupportedTrain(selectionError);
            string? error = Validate(source, roomIndex, effects, targetId, fullTrain, selfUnitId, targetSpawnPointIndex, sourceCardId);
            if (error != null) return UnsupportedTrain(error);
            int sourceAttackUnitId = selfUnitId > 0 ? selfUnitId : targetId;
            if (effects.Any(effect => effect.Type == "DamagePerSourceAttack") &&
                (sourceAttackUnitId <= 0 || source.Rooms.SelectMany(item => item.Units).All(unit => unit.Id != sourceAttackUnitId)))
                return UnsupportedTrain("Source-attack damage requires a captured caster unit.");
            RoomCombatState selected = source.Rooms.Single(item => item.RoomIndex == roomIndex);
            CombatUnit? initial = selected.Units.FirstOrDefault(unit => unit.Id == targetId);
            int dropPosition = DropPosition(selected, initial);
            TrainCombatState state = WithContext(source, source.Context!);
            IReadOnlyList<int> last = Array.Empty<int>();
            var events = new List<CombatEvent>();
            var collections = new List<SpellTargetCollection>();
            var pendingDeadRooms = new Dictionary<int, int>();
            var positions = CardTargetModel.Positions(state);
            int? focusedRoom = roomIndex;
            RoomOutcome outcome = RoomOutcome.Exchanged;
            bool bossDead = source.Context!.AllScenarioBossesDead == true;
            CardInstanceState? resolvingCard = source.Context.FindCard(sourceCardId);
            CardPileState[]? piles = (otherPiles ?? source.Context.OtherPiles)?.ToArray();
            if (piles != null) state = WithContext(state, state.Context!.WithOtherPiles(piles));
            var deferredExhaustion = new HashSet<int>();
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            var retained = new Dictionary<int, CombatUnit>();
            string? routingError = null;
            RelicPyreAttackRefreshResult Refresh(CombatContext context, bool placement)
            {
                if (!fullTrain && refreshPyre != null)
                {
                    RelicPyreAttackRefreshResult outer = refreshPyre(context, placement);
                    if (outer.Supported) state = WithContext(state, outer.Context!);
                    return outer;
                }
                TrainCombatResult changed = RelicResourceModel.RefreshPyreAttack(WithContext(state, context), placement);
                if (!changed.Supported) return new RelicPyreAttackRefreshResult(null, null, changed.UnsupportedReason);
                state = changed.State!;
                return new RelicPyreAttackRefreshResult(state.Context, state.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.IsPyre));
            }
            for (int index = 0; index < effects.Count; index++)
            {
                CardActionEffect effect = effects[index];
                // Each effect reads the card's live modifiers, including earlier hand upgrades.
                if (definitions != null && sourceCardId > 0 && state.Context!.CardInstances != null)
                {
                    CardInstanceState? card = state.Context.FindCard(sourceCardId) ?? resolvingCard;
                    CardPlayRule? rule = definitions.Cards.FirstOrDefault(item => item.DataId == card?.DataId);
                    CardPlayRule? resolvedRule = card == null || rule == null ? null : CardModifierModel.Resolve(rule, card);
                    IReadOnlyList<CardActionEffect>? sourceEffects = onUnplayedNegativeEffects ? rule?.OnUnplayedNegativeEffects :
                        onCastEffects ? rule?.OnCastEffects : rule?.Effects;
                    IReadOnlyList<CardActionEffect>? resolvedEffects = onUnplayedNegativeEffects ? resolvedRule?.OnUnplayedNegativeEffects :
                        onCastEffects ? resolvedRule?.OnCastEffects : resolvedRule?.Effects;
                    if (sourceEffects == null || resolvedEffects == null || resolvedEffects.Count != effects.Count)
                        return UnsupportedTrain("Missing live spell effect definition.");
                    // These native effects read GetUnmodifiedIntInRange, so card damage upgrades do not change their multipliers.
                    effect = index < sourceEffects.Count && (effect.Type == "DamagePerSourceAttack" || effect.Type == "DamagePerHp")
                        ? sourceEffects[index] : resolvedEffects[index];
                    resolvingCard = card;
                }
                if (effect.Target == "FrontInRoomAndRoomAbove" && !focusedRoom.HasValue)
                    return UnsupportedTrain("Room-and-above selection requires uncaptured trigger focus rules.");
                int collectionRoom = effect.Target == "FrontInRoomAndRoomAbove" ? focusedRoom!.Value : roomIndex;
                CardTargets targets = Collect(state, collectionRoom, effect, last, initial, dropPosition, index, definitions, isTesting: true,
                    pendingDeadRooms: pendingDeadRooms, positions: positions, selfUnitId: selfUnitId, sourceCardId: sourceCardId);
                if (!targets.Supported) return UnsupportedTrain(targets.UnsupportedReason!);
                string? damageTargetError = null;
                if (!CardTargetModel.IsRandom(effect.Target))
                {
                    damageTargetError = DamagePerHpTargetError(state, effect, targets, selfUnitId);
                    if (damageTargetError != null) return UnsupportedTrain(damageTargetError);
                }
                Remember(effect, index, targets, ref last);
                // Runtime tests every effect, even if its initial casting test was disabled.
                string? targetError = AttackTestError(state, collectionRoom, effect);
                if (targetError != null) return UnsupportedTrain(targetError);
                CardActionEffect tested = effect;
                if (QuantityTest(effect) && effect.Type != "DamagePerSourceAttack")
                {
                    UnityRng testingRng = state.Context!.BattleRng;
                    tested = Sample(effect, ref testingRng);
                    state = WithContext(state, state.Context.WithBattleRng(testingRng));
                }
                int testCount = TestCount(state, effect, targets);
                if (testCount < 0) return UnsupportedTrain("All-status adjustment requires complete propagation metadata.");
                bool passes = effect.Type == "AbilityTeleport"
                    ? UnitTeleportModel.CanPlay(state, selfUnitId, roomIndex, targetSpawnPointIndex)
                    : PassesTest(tested, testCount, bossDead, state.Context, selected.Preview,
                        state.Rooms.Single(room => room.RoomIndex == roomIndex), PyreAlive(state, fullTrain), sourceCardId) &&
                        PassesTargetSubtype(state, effect, targets);
                if (!passes)
                {
                    if (index == 0 && CardTargetModel.IsRandom(effect.Target) && targets.UnitIds.Count > 0 && effect.Tests?.CancelSubsequent != true &&
                        effects.Skip(1).Any(next => next.Target.Contains("LastTargeted")))
                        return UnsupportedTrain("A skipped first random effect retains uncaptured test-stream target history.");
                    if (effect.Tests?.CancelSubsequent == true) break;
                    continue;
                }
                targets = Collect(state, collectionRoom, effect, last, initial, dropPosition, index, definitions,
                    pendingDeadRooms: pendingDeadRooms, positions: positions, selfUnitId: selfUnitId, sourceCardId: sourceCardId);
                if (!targets.Supported) return UnsupportedTrain(targets.UnsupportedReason!);
                damageTargetError = DamagePerHpTargetError(state, effect, targets, selfUnitId);
                if (damageTargetError != null) return UnsupportedTrain(damageTargetError);
                collections.Add(new SpellTargetCollection(index, targets.UnitIds));
                Remember(effect, index, targets, ref last);
                if (targets.BattleRng.HasValue)
                    state = WithContext(state, state.Context!.WithBattleRng(targets.BattleRng.Value));
                if ((effect.Type == "AddStatus" || effect.Type == "MultiplyStatus" || effect.Type == "SpawnOrStatus") && effect.Statuses.Count > 1)
                {
                    // Native chooses one status for the whole effect, including an empty collection.
                    RngDraw chosen = state.Context!.BattleRng.Range(0, effect.Statuses.Count);
                    state = WithContext(state, state.Context.WithBattleRng(chosen.State));
                    effect = new CardActionEffect(effect.Type, effect.Target, effect.Value, effect.AllowEnemy, effect.AllowPlayer,
                        new[] { effect.Statuses[chosen.Value] }, effect.Upgrade, effect.Lifetime, effect.Tests, effect.Range, effect.Filters,
                        effect.Generation, effect.OnlyIfNoEnemies, effect.CooldownParameter, effect.AbilityChange,
                        effect.CopyHeroStats, effect.ScaleUpgrade, effect.DoNotFullyRemove, effect.TargetSubtype, effect.TypedDraw, effect.NextDrawUpgrade, effect.RequiredGold, effect.GoldCostRequired,
                        effect.UseCardTraits, effect.RequireEquipment, effect.UseSelectedRoom, effect.CountStatusOnlyOnTargets,
                        effect.SpawnFallbackUnit, effect.DisallowStatusStackModifiers, effect.StatusAdjustmentFilter,
                        effect.DamagePerHpLastAttacker, effect.DamagePerHpIncludeArmor, effect.RoomAttachment, effect.Recursion);
                }
                if (effect.Type == "Recursion")
                {
                    IReadOnlyList<int>? selectedCardIds = recursionSelections?.FirstOrDefault(selection =>
                        selection.EffectIndex == index)?.CardInstanceIds;
                    CardRecursionResult recursion = CardRecursionModel.Apply(state.Context!.Cards, piles,
                        state.Context.BattleRng, effect, definitions, sourceCardId, state.Context.MaxHandSize,
                        selectedCardIds, state.Context.CardRegistry);
                    if (!recursion.Supported) return UnsupportedTrain(recursion.UnsupportedReason!);
                    BattleStatistics? statistics = state.Context.Statistics;
                    foreach (int cardId in recursion.RestoredCardIds.Where(id =>
                        recursion.Cards!.Hand.Any(card => card.InstanceId == id)))
                        statistics = statistics?.Increment(cardId, "TimesDrawn");
                    CombatContext changed = state.Context.WithCards(recursion.Cards!);
                    if (recursion.OtherPiles != null) changed = changed.WithOtherPiles(recursion.OtherPiles);
                    changed = changed.WithBattleRng(recursion.BattleRng!.Value).WithStatistics(statistics);
                    foreach (CardInstanceState changedCard in recursion.ChangedCards)
                        changed = changed.WithCard(changedCard);
                    foreach (int restoredCardId in recursion.RestoredCardIds)
                    {
                        CardInstanceState? restoredCard = changed.FindCard(restoredCardId);
                        if (restoredCard?.ExhaustState != null)
                            changed = changed.WithCard(CardExhaustModel.ResetTraits(restoredCard));
                    }
                    state = WithContext(state, changed);
                    if (recursion.OtherPiles != null) piles = recursion.OtherPiles.ToArray();
                    continue;
                }
                if (effect.Type == "SpawnOrStatus")
                {
                    RoomPlayRule? spawnRule = definitions?.Rooms.FirstOrDefault(item => item.RoomIndex == collectionRoom);
                    if (spawnRule == null) return UnsupportedTrain("Spawn-or-status requires the selected room rule.");
                    if (!spawnRule.Enabled || spawnRule.SummonBlocked || spawnRule.IsPyre) continue;
                    if (targets.UnitIds.Count == 0)
                    {
                        if (!fullTrain || definitions == null || effect.SpawnFallbackUnit == null)
                            return UnsupportedTrain("Spawn-or-status fallback requires the complete birth catalog.");
                        CardInstanceState? sourceCard = state.Context!.FindCard(sourceCardId);
                        CardPlayRule? birthRule = definitions.Cards.FirstOrDefault(item => item.DataId == sourceCard?.DataId);
                        if (birthRule == null) return UnsupportedTrain("Spawn-or-status requires its live source card definition.");
                        RoomCombatState birthRoom = state.Rooms.Single(item => item.RoomIndex == collectionRoom);
                        var additionalStatus = new SpawnStatusApplication(effect.Statuses[0], sourceCardId,
                            !effect.DisallowStatusStackModifiers, "CardEffectSpawnMonsterOrAddStatusEffect");
                        UnitBirthResult born = UnitBirthModel.SpawnWithStatus(birthRoom, birthRule.WithSpawn(effect.SpawnFallbackUnit),
                            0, 0, new CombatStatus("cardless", 1, triggerVfxEnemy: false, triggerVfxPlayer: false,
                                stackable: false, hidden: false, displayCategory: "Persistent"), additionalStatus);
                        if (!born.Supported) return UnsupportedTrain(born.Result.UnsupportedReason!);
                        state = ReplaceRoom(state, born.Result.State!);
                        events.AddRange(born.Result.Events);
                        if (born.Result.Outcome != RoomOutcome.Exchanged) outcome = born.Result.Outcome;
                        bossDead |= outcome == RoomOutcome.BattleWon;
                        positions = CardTargetModel.Positions(state);
                        if (state.Context!.OtherPiles != null) piles = state.Context.OtherPiles.ToArray();
                        continue;
                    }
                }
                if (effect.Type == "AddStatusPerOtherEffect")
                {
                    CombatStatus counted = effect.Statuses[1];
                    IEnumerable<CombatUnit> countedUnits = effect.CountStatusOnlyOnTargets
                        ? state.Rooms.SelectMany(room => room.Units).Where(unit => targets.UnitIds.Contains(unit.Id))
                        : state.Rooms.Single(room => room.RoomIndex == collectionRoom).Units;
                    int stackCount = 0;
                    unchecked
                    {
                        foreach (CombatUnit unit in countedUnits) stackCount += unit.Status(counted.Id)?.Stacks ?? 0;
                        stackCount *= effect.Value;
                    }
                    effect = new CardActionEffect("AddStatus", effect.Target, 0, effect.AllowEnemy, effect.AllowPlayer,
                        new[] { effect.Statuses[0].WithStacks(stackCount) }, effect.Upgrade, effect.Lifetime, effect.Tests,
                        effect.Range, effect.Filters, effect.Generation, effect.OnlyIfNoEnemies, effect.CooldownParameter,
                        effect.AbilityChange, effect.CopyHeroStats, effect.ScaleUpgrade, effect.DoNotFullyRemove,
                        effect.TargetSubtype, effect.TypedDraw, effect.NextDrawUpgrade, effect.RequiredGold,
                        effect.GoldCostRequired, effect.UseCardTraits, effect.RequireEquipment, effect.UseSelectedRoom);
                }
                if (effect.Type == ForgePointsModel.Effect && selected.Preview) continue;
                UnityRng effectRng = state.Context!.BattleRng;
                if (effect.Type != "CopyUnits" && effect.Type != "Bump" && effect.Type != "DiscardHand" && effect.Type != "Generate" && effect.Type != "AdjustCapacity" && effect.Type != "AttachRoomCapacity" && effect.Type != "ScheduleNextDrawUpgrade" && !AbilityCooldownModel.IsEffect(effect.Type) && !AbilityLifecycleModel.IsEffect(effect.Type)) effect = Sample(effect, ref effectRng);
                state = WithContext(state, state.Context.WithBattleRng(effectRng));
                int damageAttackerId = selfUnitId;
                int damageOwnerCardId = sourceCardId;
                int damageValue = effect.Value;
                if (effect.Type == "DamagePerSourceAttack")
                {
                    int casterId = selfUnitId > 0 ? selfUnitId : targetId;
                    CombatUnit? caster = state.Rooms.SelectMany(item => item.Units).FirstOrDefault(unit => unit.Id == casterId);
                    if (caster == null) return UnsupportedTrain("Source-attack caster left the captured train before damage resolution.");
                    damageValue = unchecked(effect.Value * RelicResourceModel.AttackDamage(caster, state.Context));
                    if (effect.UseCardTraits) damageAttackerId = 0;
                    else damageOwnerCardId = caster.SpawnerCardId > 0 ? caster.SpawnerCardId : sourceCardId;
                }
                else if (effect.Type == "DamagePerHp")
                {
                    if (targets.UnitIds.Count == 0) continue;
                    int selectedId = targets.UnitIds[0];
                    RoomCombatState? selectedRoom = state.Rooms.FirstOrDefault(item => item.Units.Any(unit => unit.Id == selectedId));
                    CombatUnit? selectedUnit = selectedRoom?.Units.FirstOrDefault(unit => unit.Id == selectedId);
                    if (selectedRoom == null || selectedUnit == null)
                        return UnsupportedTrain("Damage-per-HP target left the captured train before damage resolution.");
                    int armor = effect.DamagePerHpIncludeArmor ? selectedUnit.Status("armor")?.Stacks ?? 0 : 0;
                    int multiplier = effect.Value;
                    int perHpDamage;
                    unchecked { perHpDamage = (selectedUnit.Health + armor) * multiplier; }
                    bool damagesEnemy = selectedUnit.Team == CombatTeam.Player;
                    if (perHpDamage <= 0) targets = new CardTargets(Array.Empty<int>());
                    else if (effect.DamagePerHpLastAttacker)
                    {
                        CombatUnit? sourceUnit = selfUnitId > 0
                            ? state.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == selfUnitId)
                            : null;
                        if (selfUnitId <= 0) targets = new CardTargets(Array.Empty<int>());
                        else if (sourceUnit == null)
                            return UnsupportedTrain("Damage-per-HP last-attacker source left the captured train before damage resolution.");
                        else if (!sourceUnit.LastAttackerId.HasValue)
                            return UnsupportedTrain("Damage-per-HP last-attacker targeting requires a captured source attacker reference.");
                        else if (sourceUnit.LastAttackerId.Value == 0) targets = new CardTargets(Array.Empty<int>());
                        else
                        {
                            CombatUnit? attacker = state.Rooms.SelectMany(room => room.Units)
                                .FirstOrDefault(unit => unit.Id == sourceUnit.LastAttackerId.Value);
                            if (attacker == null)
                                return UnsupportedTrain("Damage-per-HP last attacker is missing from the captured train.");
                            damagesEnemy = attacker.Team == CombatTeam.Enemy;
                            targets = effect.Filters?.IgnoreBosses == true && attacker.IsBoss == true
                                ? new CardTargets(Array.Empty<int>()) : new CardTargets(new[] { attacker.Id });
                        }
                    }
                    else
                    {
                        var front = new CardActionEffect("Damage", "FrontInRoom", perHpDamage,
                            damagesEnemy, !damagesEnemy, [], filters: effect.Filters);
                        CardTargets damageTargets = Collect(state, selectedRoom.RoomIndex, front, Array.Empty<int>(), selectedUnit,
                            DropPosition(selectedRoom, selectedUnit), index: 0, definitions: definitions, positions: positions,
                            pendingDeadRooms: pendingDeadRooms, selfUnitId: selfUnitId);
                        if (!damageTargets.Supported) return UnsupportedTrain(damageTargets.UnsupportedReason!);
                        targets = damageTargets;
                    }
                    damageValue = perHpDamage;
                    effect = new CardActionEffect("Damage", "FrontInRoom", perHpDamage,
                        damagesEnemy, !damagesEnemy, [], range: null);
                }
                if (effect.Type == "RewardGold")
                {
                    // Native previews test the effect but skip its iterator body.
                    if (!selected.Preview)
                    {
                        TrainCombatResult gold = GoldModel.Adjust(state, effect.Value, isReward: true);
                        if (!gold.Supported) return UnsupportedTrain(gold.UnsupportedReason!);
                        state = gold.State!;
                        if (effect.Value != 0 && !fullTrain && refreshPyre != null &&
                            !state.Rooms.SelectMany(room => room.Units).Any(unit => unit.IsPyre))
                        {
                            RelicPyreAttackRefreshResult refreshed = Refresh(state.Context!, selected.Deployment);
                            if (!refreshed.Supported) return UnsupportedTrain(refreshed.UnsupportedReason!);
                            state = WithContext(state, refreshed.Context!);
                        }
                    }
                    continue;
                }
                if (effect.Type == "CopyUnits")
                {
                    if (!fullTrain || definitions == null) return UnsupportedTrain("Paid copying requires the complete train and birth catalog.");
                    UnitCopyResult copied = UnitCopyModel.ApplyWithPending(state, roomIndex, targets.UnitIds, effect.Value, definitions.UnitCopyCatalog, callbacks,
                        heroBirth: effect.AllowEnemy && !effect.AllowPlayer, copyHeroStats: effect.CopyHeroStats == true);
                    if (!copied.Supported) return UnsupportedTrain(copied.UnsupportedReason!);
                    state = copied.State!; callbacks.Clear(); callbacks.AddRange(copied.PendingCallbacks); events.AddRange(copied.Events);
                    if (copied.Outcome != RoomOutcome.Exchanged) outcome = copied.Outcome;
                    positions = CardTargetModel.Positions(state);
                    if (state.Context!.OtherPiles != null) piles = state.Context.OtherPiles.ToArray();
                    continue;
                }
                if (effect.Type == "Bump")
                {
                    if (!fullTrain || definitions == null) return UnsupportedTrain("Bump requires the complete train and room rules.");
                    BumpResult bumped = BumpModel.Apply(state, targets.UnitIds, effect.Value, definitions.Rooms, sourceCardId, callbacks);
                    if (!bumped.Supported) return UnsupportedTrain(bumped.UnsupportedReason!);
                    state = bumped.State!; callbacks.Clear(); callbacks.AddRange(bumped.PendingCallbacks); events.AddRange(bumped.Events);
                    definitions = definitions.WithRooms(bumped.RoomRules);
                    foreach (CombatUnit unit in bumped.RetainedUnits) retained[unit.Id] = unit;
                    if (bumped.Outcome == RoomOutcome.BattleWon || bumped.Outcome == RoomOutcome.PlayerDefeated) outcome = bumped.Outcome;
                    positions = CardTargetModel.Positions(state);
                    if (state.Context!.OtherPiles != null) piles = state.Context.OtherPiles.ToArray();
                    continue;
                }
                if (effect.Type == "AbilityTeleport")
                {
                    if (!fullTrain || definitions == null || targets.UnitIds.Count != 1)
                        return UnsupportedTrain("Unit teleport requires one activating actor, the complete train and room rules.");
                    int actorId = targets.UnitIds[0];
                    RoomCombatState? actorRoom = state.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == actorId));
                    CombatUnit? actor = actorRoom?.Units.FirstOrDefault(unit => unit.Id == actorId);
                    if (actorRoom == null || actor == null)
                        return UnsupportedTrain("Unit teleport actor left the captured train before movement.");
                    if (actorRoom.RoomIndex != roomIndex)
                    {
                        var destination = new SpawnPointReference(roomIndex, actor.Team, targetSpawnPointIndex);
                        BumpResult teleported = BumpModel.Apply(state, [actorId], roomIndex - actorRoom.RoomIndex,
                            definitions.Rooms, sourceCardId, callbacks, destination);
                        if (!teleported.Supported) return UnsupportedTrain(teleported.UnsupportedReason!);
                        state = teleported.State!; callbacks.Clear(); callbacks.AddRange(teleported.PendingCallbacks);
                        events.AddRange(teleported.Events); definitions = definitions.WithRooms(teleported.RoomRules);
                        foreach (CombatUnit unit in teleported.RetainedUnits) retained[unit.Id] = unit;
                        if (teleported.Outcome == RoomOutcome.BattleWon || teleported.Outcome == RoomOutcome.PlayerDefeated)
                            outcome = teleported.Outcome;
                        positions = CardTargetModel.Positions(state);
                        if (state.Context!.OtherPiles != null) piles = state.Context.OtherPiles.ToArray();
                        continue;
                    }
                }
                if (effect.Type == "AdjustCapacity")
                {
                    RoomCapacityResult capacity = RoomCapacityModel.Apply(state.Context!, roomIndex, effect, sourceCardId);
                    if (!capacity.Supported) return UnsupportedTrain(capacity.UnsupportedReason!);
                    state = WithContext(state, capacity.Context!); continue;
                }
                if (effect.Type == "AttachRoomCapacity")
                {
                    RoomCapacityResult attachment = RoomCapacityModel.Attach(state.Context!, roomIndex, effect.RoomAttachment!);
                    if (!attachment.Supported) return UnsupportedTrain(attachment.UnsupportedReason!);
                    state = WithContext(state, attachment.Context!); continue;
                }
                if (effect.Type == "DrawNextTurn")
                {
                    state = WithContext(state, state.Context!.WithCards(BonusDrawModel.Schedule(state.Context.Cards,
                        BonusDrawState.CardKey(sourceCardId, index), effect.Value, effect.Upgrade)));
                    continue;
                }
                if (effect.Type == "ScheduleNextDrawUpgrade")
                {
                    if (!fullTrain && refreshPyre != null && Math.Abs(effect.RequiredGold) > 0)
                    {
                        // SpendGold signals before the next-draw listener is registered.
                        CombatContext paid = state.Context!.WithGold(Math.Max(0, unchecked(state.Context.Gold - Math.Abs(effect.RequiredGold))));
                        RelicPyreAttackRefreshResult refreshed = Refresh(paid, selected.Deployment);
                        if (!refreshed.Supported) return UnsupportedTrain(refreshed.UnsupportedReason!);
                        state = WithContext(state, NextDrawUpgradeModel.Schedule(refreshed.Context!, effect.NextDrawUpgrade!, requiredGold: 0));
                    }
                    else
                    {
                        TrainCombatResult scheduled = NextDrawUpgradeModel.Schedule(state, effect.NextDrawUpgrade!, effect.RequiredGold);
                        if (!scheduled.Supported) return UnsupportedTrain(scheduled.UnsupportedReason!);
                        state = scheduled.State!;
                    }
                    continue;
                }
                if (effect.Type == "DrawType")
                {
                    TypedDrawResult draw = TypedDrawModel.Draw(state.Context!, effect.TypedDraw!, effect.Value, sourceCardId, definitions);
                    if (!draw.Supported) return UnsupportedTrain(draw.UnsupportedReason!);
                    state = WithContext(state, draw.Context!);
                    continue;
                }
                if (EnergyModel.IsEffect(effect.Type))
                {
                    state = WithContext(state, EnergyModel.Apply(state.Context!, effect.Type, effect.Value));
                    continue;
                }
                if (effect.Type == ForgePointsModel.Effect)
                {
                    ForgePointsResult changed = ForgePointsModel.Apply(state.Context!, effect.Value, sourceCardId,
                        placement: selected.Deployment, preview: selected.Preview);
                    if (!changed.Supported) return UnsupportedTrain(changed.UnsupportedReason!);
                    state = WithContext(state, changed.Context!);
                    continue;
                }
                if (effect.Type == "Generate")
                {
                    CardGenerationResult generated = CardGenerationModel.Apply(state.Context!, effect.Generation!, sourceCardId);
                    if (!generated.Supported) return UnsupportedTrain(generated.UnsupportedReason!);
                    state = WithContext(state, generated.Context!);
                    continue;
                }
                if (effect.Type == "RandomDiscard")
                {
                    HandRemovalResult discarded = RandomDiscardModel.Apply(state.Context!, effect.Value, sourceCardId,
                        index, definitions, piles);
                    if (!discarded.Supported) return UnsupportedTrain(discarded.UnsupportedReason!);
                    state = WithContext(state, discarded.Context!);
                    piles = discarded.OtherPiles?.ToArray() ?? piles;
                    continue;
                }
                if (effect.Type == "DiscardHand")
                {
                    int[] pendingCards = source.Rooms.SelectMany(room => room.Units).Where(unit => pendingDeadRooms.ContainsKey(unit.Id) &&
                        unit.SpawnerCardId > 0 && piles?.Any(pile => pile.Name == "Standby" && pile.Cards.Any(card => card.InstanceId == unit.SpawnerCardId)) == true)
                        .Select(unit => unit.SpawnerCardId).ToArray();
                    bool runsQueue = state.Rooms.Single(room => room.RoomIndex == roomIndex).Units.Count > 0 || pendingDeadRooms.Values.Contains(roomIndex);
                    HandRemovalResult removed = HandRemovalModel.Apply(state.Context!, effect.Value, sourceCardId, index, definitions, piles,
                        pendingCards, runsQueue);
                    if (!removed.Supported) return UnsupportedTrain(removed.UnsupportedReason!);
                    piles = removed.OtherPiles?.ToArray();
                    state = WithContext(state, removed.Context!);
                    foreach (int id in pendingCards.Where(id => piles?.Any(pile => pile.Name == "Exhausted" && pile.Cards.Any(card => card.InstanceId == id)) == true))
                        deferredExhaustion.Remove(id);
                    if (removed.RemovedCards.Count > 0 && (effect.Value == 1 || pendingCards.Length > 0 && runsQueue)) DrainDeaths();
                    continue;
                }
                if (effect.Type == "Draw")
                {
                    CombatContext context = state.Context!;
                    // The native -1 mode uses max hand size, despite its starting-hand description.
                    int count = effect.Value == -1 ? Math.Max(0, context.MaxHandSize - context.Cards.Hand.Count + 1) : effect.Value;
                    TypedDrawResult drawn = NextDrawUpgradeModel.DrawCards(context, count, sourceCardId, definitions);
                    if (!drawn.Supported) return UnsupportedTrain(drawn.UnsupportedReason!);
                    state = WithContext(state, drawn.Context!);
                    continue;
                }
                if (effect.Type == "Sacrifice" || effect.Type == "Kill")
                {
                    if (selfUnitId > 0 && targets.UnitIds.Contains(selfUnitId))
                    {
                        RoomCombatState? activatorRoom = state.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == selfUnitId));
                        CombatUnit? activator = activatorRoom?.Units.FirstOrDefault(unit => unit.Id == selfUnitId) ?? retained.GetValueOrDefault(selfUnitId);
                        if (activator != null)
                        {
                            callbacks.Add(new RoomCombatModel.QueuedCharacterTrigger(activatorRoom?.RoomIndex ?? roomIndex,
                                activator, "OnOwnAbilityActivated"));
                            DrainDeaths();
                            if (routingError != null) return UnsupportedTrain(routingError);
                        }
                    }
                    foreach (int id in targets.UnitIds)
                    {
                        int targetRoomIndex = state.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == id))?.RoomIndex ??
                            pendingDeadRooms.GetValueOrDefault(id, roomIndex);
                        CombatUnit? target = state.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == id) ??
                            retained.GetValueOrDefault(id);
                        if (target == null) continue;
                        CombatUnit staged = Copy(target, 0, target.Statuses).WithSacrifice(sourceCardId);
                        if (selfUnitId > 0)
                        {
                            RoomCombatState? actorRoom = state.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == selfUnitId));
                            CombatUnit? activator = actorRoom?.Units.FirstOrDefault(unit => unit.Id == selfUnitId) ?? retained.GetValueOrDefault(selfUnitId);
                            if (activator != null)
                            {
                                callbacks.Add(new RoomCombatModel.QueuedCharacterTrigger(actorRoom?.RoomIndex ?? targetRoomIndex,
                                    activator, "OnKill", overrideTarget: staged));
                                DrainDeaths();
                                if (routingError != null) return UnsupportedTrain(routingError);
                                target = state.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == id) ??
                                    retained.GetValueOrDefault(id) ?? staged;
                            }
                        }
                        RoomCombatState deathRoom = state.Rooms.Single(room => room.RoomIndex == targetRoomIndex);
                        RoomCombatResult sacrificed = RoomCombatModel.QueueSacrifice(deathRoom, target, sourceCardId, callbacks.Add,
                            doNotFullyRemove: effect.DoNotFullyRemove == true, refreshPyre: Refresh);
                        if (!sacrificed.Supported) return UnsupportedTrain(sacrificed.UnsupportedReason!);
                        state = ReplaceRoom(state, sacrificed.State!);
                        retained[id] = target;
                        events.AddRange(sacrificed.Events);
                        if (sacrificed.Outcome == RoomOutcome.BattleWon || sacrificed.Outcome == RoomOutcome.PlayerDefeated)
                            outcome = sacrificed.Outcome;
                        if (state.Context!.OtherPiles != null) piles = state.Context.OtherPiles.ToArray();
                        DrainDeaths();
                        if (routingError != null) return UnsupportedTrain(routingError);
                        focusedRoom = null;
                    }
                    continue;
                }
                if (effect.Type == "HandUpgrade")
                {
                    RoomCombatResult upgraded = HandUpgradeModel.Apply(state.Rooms.Single(item => item.RoomIndex == roomIndex),
                        effect.Upgrade!, effect.Lifetime, definitions);
                    if (!upgraded.Supported) return UnsupportedTrain(upgraded.UnsupportedReason!);
                    state = ReplaceRoom(state, upgraded.State!);
                    events.AddRange(upgraded.Events);
                    continue;
                }
                if (effect.Type == "AttackWithUnit")
                {
                    if (targets.UnitIds.Count == 0) continue;
                    RoomCombatState? firstRoom = state.Rooms.FirstOrDefault(item => item.Units.Any(unit => unit.Id == targets.UnitIds[0]));
                    if (!effect.UseSelectedRoom && firstRoom == null) continue;
                    // Native Pyre targeting moves room selection to the Pyre floor before this effect runs.
                    int selectedRoomForEffect = effect.Target == "Pyre" && firstRoom != null ? firstRoom.RoomIndex : roomIndex;
                    int attackRoom = effect.UseSelectedRoom ? selectedRoomForEffect : firstRoom!.RoomIndex;
                    foreach (int actorId in targets.UnitIds)
                    {
                        CombatUnit? actor = state.Rooms.SelectMany(item => item.Units).FirstOrDefault(unit => unit.Id == actorId);
                        if (actor == null) continue;
                        if (effect.RequireEquipment)
                        {
                            if (actor.EquipmentCards == null)
                                return UnsupportedTrain("Equipment-gated unit attack requires captured equipment state.");
                            if (actor.EquipmentCards.Count == 0) continue;
                        }
                        TrainSpellResult turn = ApplyUnitAttackTurn(state, attackRoom, actorId, Refresh);
                        if (!turn.Supported) return UnsupportedTrain(turn.UnsupportedReason!);
                        state = turn.State!;
                        events.AddRange(turn.Events);
                        foreach (CombatUnit unit in turn.RetainedUnits) retained[unit.Id] = unit;
                        callbacks.AddRange(turn.PendingCallbacks);
                        if (turn.Outcome == RoomOutcome.BattleWon || turn.Outcome == RoomOutcome.PlayerDefeated)
                        {
                            if (outcome != RoomOutcome.Exchanged && outcome != turn.Outcome)
                                return UnsupportedTrain("Conflicting terminal results in one spell are not modeled.");
                            outcome = turn.Outcome;
                            bossDead |= outcome == RoomOutcome.BattleWon;
                        }
                        if (state.Context!.OtherPiles != null) piles = state.Context.OtherPiles.ToArray();
                        positions = CardTargetModel.Positions(state);
                        if (callbacks.Count > 0) DrainDeaths();
                        if (routingError != null) return UnsupportedTrain(routingError);
                    }
                    continue;
                }
                // Keep this effect's collection fixed; triggers may kill a later target.
                for (int targetIndex = 0; targetIndex < targets.UnitIds.Count; targetIndex++)
                {
                    if (effect.Type == "AttachEquipment" && targetIndex > 0) break;
                    // Native status application runs backwards; damage/healing/upgrades run forwards.
                    int id = targets.UnitIds[(effect.Type == "AddStatus" || effect.Type == "MultiplyStatus" ||
                        effect.Type == "MultiplyAllPositiveStatusEffects" || effect.Type == "AdjustAllStatusEffects" || effect.Type == "SpawnOrStatus" ||
                        AbilityCooldownModel.IsEffect(effect.Type) || AbilityLifecycleModel.IsEffect(effect.Type))
                        ? targets.UnitIds.Count - 1 - targetIndex : targetIndex];
                    RoomCombatState? targetRoom = state.Rooms.FirstOrDefault(item => item.Units.Any(unit => unit.Id == id));
                    CombatUnit? target = targetRoom?.Units.FirstOrDefault(unit => unit.Id == id);
                    if (target == null) continue;
                    if (effect.Type == "RemoveStatus")
                    {
                        RoomCombatResult removed = StatusRemovalModel.Effect(targetRoom!, id, effect, sourceCardId);
                        if (!removed.Supported) return UnsupportedTrain(removed.UnsupportedReason!);
                        state = ReplaceRoom(state, removed.State!); callbacks.AddRange(removed.PendingCallbacks); events.AddRange(removed.Events);
                        if (target.Team == CombatTeam.Player && removed.PendingCallbacks.Any(item => item.Kind == "OnDeath" && item.Unit.Id == id))
                        {
                            pendingDeadRooms[id] = targetRoom!.RoomIndex;
                            if (target.SpawnerCardId > 0 && !targetRoom.Preview) deferredExhaustion.Add(target.SpawnerCardId);
                        }
                        if (removed.Outcome != RoomOutcome.Exchanged) outcome = removed.Outcome;
                        if (state.Context!.OtherPiles != null) piles = state.Context.OtherPiles.ToArray();
                        if (StatusRemovalModel.RequiresSacrifice(removed)) { DrainDeaths(); focusedRoom = null; }
                        continue;
                    }
                    if (AbilityLifecycleModel.IsEffect(effect.Type))
                    {
                        RoomCombatResult changed = AbilityLifecycleModel.Apply(targetRoom!, id, effect.AbilityChange!, effect.Type == "RemoveAbility", deferCallbacks: true);
                        if (!changed.Supported) return UnsupportedTrain(changed.UnsupportedReason!);
                        state = ReplaceRoom(state, changed.State!); callbacks.AddRange(changed.PendingCallbacks);
                        continue;
                    }
                    if (AbilityCooldownModel.IsEffect(effect.Type))
                    {
                        RoomCombatResult adjusted = AbilityCooldownModel.Apply(targetRoom!, id, effect, sourceCardId);
                        if (!adjusted.Supported) return UnsupportedTrain(adjusted.UnsupportedReason!);
                        state = ReplaceRoom(state, adjusted.State!); callbacks.AddRange(adjusted.PendingCallbacks);
                        continue;
                    }
                    if (effect.Type == "BuffEquipment")
                    {
                        string? equipmentError = BuffEquipmentCards(ref state, id, sourceCardId, effect, targetRoom!.Preview,
                            callbacks, events, ref outcome, Refresh);
                        if (equipmentError != null) return UnsupportedTrain(equipmentError);
                        if (state.Context!.OtherPiles != null) piles = state.Context.OtherPiles.ToArray();
                        if (outcome == RoomOutcome.BattleWon) bossDead = true;
                        continue;
                    }
                    if ((effect.Type == "FloorRearrange" || effect.Type == "AbilityTeleport") && pendingDeadRooms.Count > 0)
                        return UnsupportedTrain("Rearranging a floor with pending death positions is not modeled.");
                    if (effect.Type == "AttachEquipment" || effect.Type == "RemoveEquipment")
                    {
                        if (definitions == null) return UnsupportedTrain("Equipment requires play definitions.");
                        if (effect.Type == "AttachEquipment" && target.EquipmentCards?.Contains(sourceCardId) == true) continue;
                        RoomCombatResult equipment = RoomCombatModel.ApplyEquipment(targetRoom!, id, effect.Type == "AttachEquipment" ? sourceCardId : 0,
                            definitions, effect.Type == "RemoveEquipment", deferAbilityCallbacks: true, refreshPyre: Refresh);
                        if (!equipment.Supported) return UnsupportedTrain(equipment.UnsupportedReason!);
                        state = ReplaceRoom(state, equipment.State!); events.AddRange(equipment.Events); callbacks.AddRange(equipment.PendingCallbacks);
                        if (equipment.Outcome == RoomOutcome.BattleWon || equipment.Outcome == RoomOutcome.PlayerDefeated) outcome = equipment.Outcome;
                        if (state.Context!.OtherPiles != null) piles = state.Context.OtherPiles.ToArray();
                        if (effect.Type == "AttachEquipment") DrainDeaths();
                        continue;
                    }
                    int? scaledDamage = null;
                    bool nativeDamagePhase = IsDamage(effect) && targetRoom!.Context?.SpawnPoints != null && !targetRoom.Preview;
                    // Native calculates trait damage and defensive status focus before its trigger
                    // queue drains the previous victim's standby return. Preserve that value once.
                    if (IsDamage(effect))
                    {
                        DamageScalingResult scaled = DamageScalingModel.Apply(state.Context, damageOwnerCardId, sourceCardId,
                            Math.Max(0, damageValue));
                        if (!scaled.Supported) return UnsupportedTrain(scaled.UnsupportedReason!);
                        state = WithContext(state, scaled.Context!);
                        scaledDamage = Math.Max(0, scaled.Damage);
                        FocusDamageStatuses(target, targetRoom!, scaledDamage.Value);
                        if (!nativeDamagePhase || callbacks.Any(queued => queued.Kind != "OnDeath" || !queued.DeferUntilRemoval)) DrainDeaths();
                        // Draining a prior death updates shared statistics; the next target must read that new context.
                        targetRoom = state.Rooms.Single(room => room.RoomIndex == targetRoom!.RoomIndex);
                    }
                    RoomPlayRule? capacity = definitions?.Rooms.FirstOrDefault(item => item.RoomIndex == targetRoom!.RoomIndex);
                    RoomCombatState? damageActorRoom = damageAttackerId > 0
                        ? state.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == damageAttackerId)) : null;
                    CombatUnit? damageActor = damageActorRoom?.Units.FirstOrDefault(unit => unit.Id == damageAttackerId);
                    if (damageAttackerId > 0 && damageActor == null)
                        return UnsupportedTrain("Spell damage source left the captured train before damage resolution.");
                    CombatUnit? externalDamageActor = damageActorRoom?.RoomIndex != targetRoom!.RoomIndex ? damageActor : null;
                    var priorDeaths = nativeDamagePhase ? callbacks.ToArray() : Array.Empty<RoomCombatModel.QueuedCharacterTrigger>();
                    if (nativeDamagePhase) callbacks.Clear();
                    TrainSpellResult? nativeApplied = nativeDamagePhase ? ApplyNativeDamageStep(state, targetRoom!.RoomIndex, id,
                        scaledDamage!.Value, sourceCardId, priorDeaths, damageAttackerId,
                        enqueueCharacterTrigger: externalDamageActor == null ? null : callbacks.Add,
                        drainQueuedTriggers: externalDamageActor == null ? null : frame =>
                        {
                            state = frame;
                            DrainDeaths();
                            return routingError == null ? state : null;
                        },
                        getRetainedUnit: retained.GetValueOrDefault) : null;
                    if (nativeApplied != null && !nativeApplied.Supported) return UnsupportedTrain(nativeApplied.UnsupportedReason!);
                    RoomCombatResult applied = nativeApplied != null
                        ? new RoomCombatResult(nativeApplied.State!.Rooms.Single(room => room.RoomIndex == targetRoom!.RoomIndex),
                            nativeApplied.Outcome, 0, nativeApplied.Events.ToList(), pendingCallbacks: nativeApplied.PendingCallbacks,
                            retainedUnits: nativeApplied.RetainedUnits)
                        : ApplyOne(targetRoom!, effect, target, sourceCardId,
                        RoomCapacityModel.Maximum(state.Context, targetRoom!.RoomIndex, CombatTeam.Player) ?? capacity?.PlayerCapacity ?? playerCapacity,
                        RoomCapacityModel.Maximum(state.Context, targetRoom!.RoomIndex, CombatTeam.Enemy) ?? capacity?.EnemyCapacity ?? enemyCapacity,
                        deferSpawnerExhaustion: piles != null, scaledDamage: scaledDamage, selfUnitId: damageAttackerId,
                        sourceHasDuality: selfUnitId > 0 && state.Rooms.SelectMany(room => room.Units)
                            .Any(unit => unit.Id == selfUnitId && unit.Status("duality")?.Stacks > 0), refreshPyre: Refresh,
                        externalActor: externalDamageActor, externalActorRoomIndex: damageActorRoom?.RoomIndex ?? -1,
                        enqueueCharacterTrigger: externalDamageActor == null ? null : callbacks.Add);
                    if (!applied.Supported) return UnsupportedTrain(applied.UnsupportedReason!);
                    state = nativeApplied?.State ?? ReplaceRoom(state, applied.State!);
                    if (externalDamageActor != null)
                    {
                        CombatUnit? updatedActor = applied.RetainedUnits.FirstOrDefault(unit => unit.Id == externalDamageActor.Id);
                        if (nativeApplied == null)
                        {
                            if (updatedActor == null)
                                return UnsupportedTrain("Spell damage did not retain its remote source actor.");
                            state = ReplaceExternalActor(state, damageActorRoom!.RoomIndex, updatedActor);
                            if (!ActorIsLive(updatedActor)) retained[updatedActor.Id] = updatedActor;
                        }
                    }
                    foreach (CombatUnit unit in applied.RetainedUnits.Where(unit =>
                        externalDamageActor == null || unit.Id != externalDamageActor.Id))
                        retained[unit.Id] = unit;
                    callbacks.AddRange(applied.PendingCallbacks);
                    foreach (var removed in priorDeaths)
                    {
                        pendingDeadRooms.Remove(removed.Unit.Id);
                        deferredExhaustion.Remove(removed.Unit.SpawnerCardId);
                    }
                    if (state.Context!.OtherPiles != null) piles = state.Context.OtherPiles.ToArray();
                    events.AddRange(applied.Events);
                    if ((effect.Type == "FloorRearrange" || effect.Type == "AbilityTeleport") &&
                        target.Status("immobile") == null && state.Context!.SpawnPoints?.Units.SingleOrDefault(unit => unit.UnitId == target.Id)?.OuterBoss != true)
                    {
                        BattleSpawnPoints? physical = state.Context!.SpawnPoints;
                        RoomCombatState rearrangeRoom = targetRoom!;
                        CombatUnit[] orderedTeam = (physical == null ? rearrangeRoom.Units :
                            BattleSpawnPointModel.Order(physical, rearrangeRoom.RoomIndex, rearrangeRoom.Units)).ToArray();
                        CombatUnit[] beforeTeam = orderedTeam.Where(unit => unit.Team == target.Team && unit.Health > 0 &&
                            unit.DeathState?.IsDestroyed != true && physical?.Units.SingleOrDefault(point => point.UnitId == unit.Id)?.OuterBoss != true).ToArray();
                        int oldIndex = Array.FindIndex(beforeTeam, unit => unit.Id == target.Id);
                        int goal;
                        if (effect.Type == "AbilityTeleport")
                        {
                            if (physical == null || !UnitTeleportModel.TryGoalIndex(physical, target.Id, rearrangeRoom.RoomIndex,
                                targetSpawnPointIndex, out goal)) return UnsupportedTrain("Unit teleport lost its selected spawn-point destination.");
                        }
                        else goal = effect.Value == 0 ? 0 : beforeTeam.Length - 1;
                        int movementGoal = goal >= 0 && goal < beforeTeam.Length ? goal : oldIndex;
                        int direction = oldIndex < movementGoal ? 1 : -1;
                        if (oldIndex >= 0 && physical != null)
                        {
                            for (int position = oldIndex; position != movementGoal; position += direction)
                            {
                                int firstId = beforeTeam[position].Id, secondId = beforeTeam[position + direction].Id;
                                SpawnPointReference? first = physical.Units.Single(unit => unit.UnitId == firstId).Current;
                                SpawnPointReference? second = physical.Units.Single(unit => unit.UnitId == secondId).Current;
                                if (first == null || second == null || first.RoomIndex != rearrangeRoom.RoomIndex ||
                                    second.RoomIndex != rearrangeRoom.RoomIndex || first.Team != target.Team || second.Team != target.Team)
                                    return UnsupportedTrain("Floor rearrangement requires the current physical points for every shifted unit.");
                                BattleSpawnPointResult swapped = BattleSpawnPointModel.Apply(physical, rearrangeRoom, "Swap", target.Team,
                                    index: first.Index, targetIndex: second.Index);
                                if (!swapped.Supported) return UnsupportedTrain(swapped.Error!);
                                physical = swapped.State!;
                            }
                            if (!ReferenceEquals(physical, state.Context!.SpawnPoints))
                            {
                                state = WithContext(state, state.Context.WithSpawnPoints(physical));
                                RoomCombatState currentRoom = state.Rooms.Single(room => room.RoomIndex == rearrangeRoom.RoomIndex);
                                state = ReplaceRoom(state, new RoomCombatState(currentRoom.RoomIndex, currentRoom.Deployment,
                                    BattleSpawnPointModel.Order(physical, currentRoom.RoomIndex, currentRoom.Units),
                                    currentRoom.ExternalInteractions, state.Context, currentRoom.Preview, currentRoom.ActiveCompanionBossIds));
                            }
                        }
                        int callbackGoal = movementGoal;
                        var shiftedIds = new List<int> { target.Id };
                        direction = oldIndex < callbackGoal ? 1 : -1;
                        for (int position = oldIndex; oldIndex >= 0 && position != callbackGoal; position += direction)
                            shiftedIds.Add(beforeTeam[position + direction].Id);
                        foreach (int shiftedId in shiftedIds)
                        {
                            CombatUnit shifted = state.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == shiftedId)
                                ?? retained.GetValueOrDefault(shiftedId) ?? beforeTeam.Single(unit => unit.Id == shiftedId);
                            if (shiftedId != target.Id || oldIndex != movementGoal)
                            {
                                callbacks.Add(new RoomCombatModel.QueuedCharacterTrigger(rearrangeRoom.RoomIndex, shifted, "OnShift"));
                                DrainDeaths();
                            }
                            shifted = state.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == shiftedId)
                                ?? retained.GetValueOrDefault(shiftedId) ?? shifted;
                            foreach (CombatUnit guard in state.Rooms.Single(room => room.RoomIndex == rearrangeRoom.RoomIndex).Units
                                .Where(unit => unit.Team != shifted.Team))
                                callbacks.Add(new RoomCombatModel.QueuedCharacterTrigger(rearrangeRoom.RoomIndex, guard, "OnSentry",
                                    overrideTarget: shifted, paramString: ""));
                            DrainDeaths();
                        }
                        DrainDeaths();
                        TrainCombatResult rearrangedOrder = EnchantmentWorldModel.UpdateAll(state);
                        if (!rearrangedOrder.Supported) return UnsupportedTrain(rearrangedOrder.UnsupportedReason!);
                        state = rearrangedOrder.State!;
                    }
                    if ((effect.Type == "Heal" && effect.Value >= 0 || effect.Type == "UnitUpgrade" || effect.Type == "BuffEquipment") && target.Triggers.Any(trigger => trigger.Kind == "OnHeal" &&
                        (!trigger.Once || !trigger.HasTriggered) && (!targetRoom!.Deployment || trigger.SkipDuringDeployment != true) &&
                        (trigger.IgnoreSilence || target.Statuses.All(status => status.Id != "silenced"))))
                        focusedRoom = null; // Trigger notification suppression/focus is not captured yet.
                    if (IsDamage(effect) && !applied.State!.Units.Any(unit => unit.Id == id))
                    {
                        pendingDeadRooms[id] = targetRoom!.RoomIndex;
                        if (!nativeDamagePhase && piles != null && target.SpawnerCardId > 0) deferredExhaustion.Add(target.SpawnerCardId);
                    }
                    else if (piles != null && target.SpawnerCardId > 0 && !applied.State!.Units.Any(unit => unit.Id == id))
                        RouteDeadCard(target.SpawnerCardId);
                    CombatUnit? afterTarget = applied.State!.Units.FirstOrDefault(unit => unit.Id == id);
                    bool upgradeApplied = (effect.Type == "UnitUpgrade" || effect.Type == "BuffEquipment") && (afterTarget == null ||
                        afterTarget.Modifiers!.Upgrades.Count > target.Modifiers!.Upgrades.Count);
                    if (effect.Type == "Heal" && effect.Value >= 0 && target.Modifiers?.CanBeHealed == true || upgradeApplied) DrainDeaths();
                    if (effect.Type == "BuffHealth" && effect.Value > 0 && target.Modifiers?.CanBeHealed == true &&
                        HealingModel.ModifiedAmount(effect.Value, target.Statuses, fromMaxHealthChange: true) >= 0) DrainDeaths();
                    if (effect.Type == "DebuffHealth" && afterTarget == null)
                    {
                        DrainDeaths();
                        focusedRoom = null; // Sacrifice/removal notification focus is not captured yet.
                    }
                    if (effect.Type == "FloorRearrange" || effect.Type == "AbilityTeleport")
                        positions = CardTargetModel.Positions(state);
                    if (applied.Outcome == RoomOutcome.BattleWon || applied.Outcome == RoomOutcome.PlayerDefeated)
                    {
                        if (outcome != RoomOutcome.Exchanged && outcome != applied.Outcome)
                            return UnsupportedTrain("Conflicting terminal results in one spell are not modeled.");
                        outcome = applied.Outcome;
                        bossDead |= outcome == RoomOutcome.BattleWon;
                    }
                }
                if (effect.Type == "AddStatus" || effect.Type == "MultiplyStatus" ||
                    effect.Type == "MultiplyAllPositiveStatusEffects" || effect.Type == "AdjustAllStatusEffects" || effect.Type == "SpawnOrStatus") DrainDeaths();
            }
            // Natural play records TimesPlayed before OnCast/unit-play callbacks
            // drain the final queue. Direct effect callers request a settled result.
            // Settled deaths can still have outstanding spawner routing, even
            // when the callback list is empty. Finish that work at this boundary.
            if (!deferFinalCallbacks || callbacks.Count == 0) DrainDeaths();
            if (routingError != null) return UnsupportedTrain(routingError);
            state = WithContext(state, (piles == null ? state.Context! : state.Context!.WithOtherPiles(piles)).AfterCardEffects());
            return new TrainSpellResult(state, outcome, events, collections: collections, otherPiles: piles,
                pendingCallbacks: deferFinalCallbacks ? callbacks : null, retainedUnits: retained.Values.ToArray(), playRules: definitions);

            void RouteDeadCard(int cardId)
            {
                CombatContext current = state.Context!;
                if (current.CardInstances != null && !current.CardInstances.Any(card => card.InstanceId == cardId) &&
                    (current.KillCamActivated == true || current.CardRegistry?.Any(card => card.InstanceId == cardId) == true))
                {
                    deferredExhaustion.Remove(cardId);
                    return;
                }
                CardPileState? standby = piles!.FirstOrDefault(pile => pile.Name == "Standby");
                CardPileState? exhausted = piles!.FirstOrDefault(pile => pile.Name == "Exhausted");
                CardToken? token = standby?.Cards.FirstOrDefault(card => card.InstanceId == cardId);
                if (token == null && exhausted?.Cards.Any(card => card.InstanceId == cardId) == true) return;
                if (standby == null || exhausted == null || token == null)
                {
                    routingError = "Missing dead spawner routing during the trigger queue.";
                    return;
                }
                piles = piles!.Select(pile => pile == standby ? CardPileModel.Remove(pile, cardId) :
                    pile == exhausted ? CardPileModel.Add(exhausted, token) : pile).ToArray();
                state = WithContext(state, state.Context!.WithOtherPiles(piles));
                if (deferredExhaustion.Remove(cardId)) state = WithContext(state, state.Context!.WithStatistics(
                    state.Context.LiveStatistics?.Increment(cardId, "TimesExhausted", requireTrackedCard: state.Context.CardInstances?.Count == 0)));
            }

            void DrainDeaths()
            {
                string? callbackError = null;
                bool drained = RoomCombatModel.DrainCharacterQueue(callbacks, queued =>
                {
                    RoomCombatState room = state.Rooms.Single(item => item.RoomIndex == queued.RoomIndex);
                    RoomCombatResult fired = RoomCombatModel.ApplyQueuedCharacterTrigger(room, queued, callbacks.Add, refreshPyre: Refresh);
                    if (!fired.Supported) { callbackError = fired.UnsupportedReason; return false; }
                    foreach (CombatUnit unit in fired.RetainedUnits) retained[unit.Id] = unit;
                    state = ReplaceRoom(state, fired.State!); events.AddRange(fired.Events);
                    if (fired.Outcome == RoomOutcome.BattleWon || fired.Outcome == RoomOutcome.PlayerDefeated) outcome = fired.Outcome;
                    return true;
                }, queued =>
                {
                    RoomCombatState room = state.Rooms.Single(item => item.RoomIndex == queued.RoomIndex);
                    bool lastPhysicalDeath = queued.CompletePhysicalRemovalAfterQueue && !callbacks.Any(item =>
                        item.CompletePhysicalRemovalAfterQueue && item.Kind == "OnDeath" && item.Unit.Team == queued.Unit.Team && item.Unit.Id > queued.Unit.Id);
                    RoomCombatResult returned = lastPhysicalDeath ? RoomCombatModel.SettleQueuedSpawnerAndCenter(room, queued.Unit, refreshPyre: Refresh) :
                        RoomCombatModel.SettleQueuedSpawner(room, queued.Unit, queued.PreservePhysicalPoint, refreshPyre: Refresh);
                    if (!returned.Supported) { callbackError = returned.UnsupportedReason; return false; }
                    state = ReplaceRoom(state, returned.State!); return true;
                }, () =>
                {
                    TrainCombatResult updated = EnchantmentWorldModel.UpdateAll(state, callbacks.Add);
                    if (!updated.Supported) { callbackError = updated.UnsupportedReason; return false; }
                    state = updated.State!; return true;
                }, () => state.Context, message => callbackError = message);
                callbacks.Clear();
                if (!drained) routingError = callbackError ?? "Status callback queue failed.";
                if (state.Context!.OtherPiles != null) piles = state.Context.OtherPiles.ToArray();
                if (piles != null)
                    foreach (int id in pendingDeadRooms.Keys)
                    {
                        CombatUnit? dead = source.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == id);
                        if (dead?.SpawnerCardId > 0) RouteDeadCard(dead.SpawnerCardId);
                    }
                if (pendingDeadRooms.Count > 0) focusedRoom = pendingDeadRooms.Values.Last();
                pendingDeadRooms.Clear(); positions = CardTargetModel.Positions(state);
            }

            void FocusDamageStatuses(CombatUnit target, RoomCombatState room, int damage)
            {
                damage = Math.Max(0, damage);
                foreach (string id in new[] { "pyregel", "damage shield", "armor", "fragile" })
                {
                    CombatStatus? status = target.Statuses.FirstOrDefault(item => item.Id == id);
                    if (status == null || room.Deployment && status.SkipDuringDeployment) continue;
                    bool triggered = id == "pyregel" ? status.Stacks * status.ParamInt != 0 : damage > 0;
                    if (!triggered) continue;
                    bool? focus = status.RemoveWhenTriggered && (!room.Deployment || status.RemoveDuringDeployment) ? true :
                        target.Team == CombatTeam.Enemy ? status.TriggerVfxEnemy : status.TriggerVfxPlayer;
                    if (focus != false) focusedRoom = focus == true ? room.RoomIndex : (int?)null;
                    damage = id == "pyregel" ? checked(damage + status.Stacks * status.ParamInt) :
                        id == "damage shield" ? 0 : id == "armor" ? Math.Max(0, damage - status.Stacks * status.ParamInt) : damage;
                }
            }
        }

        private static string? Validate(TrainCombatState source, int roomIndex, IReadOnlyList<CardActionEffect> effects,
            int targetId, bool fullTrain, int selfUnitId = 0, int targetSpawnPointIndex = -1, int sourceCardId = 0)
        {
            if (fullTrain)
            {
                string? error = TrainCombatModel.Validate(source);
                if (error != null) return error;
            }
            RoomCombatState? selected = source.Rooms.FirstOrDefault(room => room.RoomIndex == roomIndex);
            if (selected == null) return "The selected spell room does not exist.";
            if (source.Context == null) return "A spell requires shared battle context.";
            if (effects.Any(effect => effect.Type == "RandomDiscard"))
            {
                if (effects.Any(effect => effect.Type == "RandomDiscard" && (effect.Value < 0 || effect.Range?.Min < 0)))
                    return "Negative random discard counts are not modeled.";
                if (!RandomDiscardModel.EligibleCount(source.Context, sourceCardId, out string? discardError).HasValue)
                    return discardError;
            }
            if (effects.Any(effect => effect.Type == "AbilityTeleport") &&
                (!fullTrain || selfUnitId <= 0 || effects.Any(effect => effect.Type == "AbilityTeleport" && effect.Target != "Self") ||
                 source.Context.SpawnPoints == null))
                return "Unit ability teleport requires its activating actor and captured train spawn points.";
            if (!fullTrain && effects.Any(effect => CardTargetModel.IsCrossRoom(effect.Target)))
                return "Cross-room spells require the complete train state.";
            string? validation = Validate(selected, effects, targetId);
            if (validation != null) return validation;
            if (CardTargetModel.IsRandom(effects[0].Target))
            {
                CardTargets candidates = CardTargetModel.RandomCandidates(source, roomIndex, effects[0]);
                if (!candidates.Supported) return candidates.UnsupportedReason;
                CombatUnit[] possible = source.Rooms.SelectMany(room => room.Units).Where(unit => candidates.UnitIds.Contains(unit.Id)).ToArray();
                foreach (CardActionEffect effect in effects.Skip(1))
                {
                    if (effect.Target == "DropTargetCharacter" && effect.Tests?.ShouldTest != false) break;
                    if (!(effects[0].Target == "RandomFromAnyRoom" && effect.Target == "StrongestLastTargetedCharactersRoom" ||
                        AttackChange(effect) && effect.Target.Contains("LastTargeted")) || effect.Tests?.ShouldTest == false || effect.Tests?.FailToCast != true) continue;
                    var outcomes = new List<bool>();
                    foreach (CombatUnit unit in possible)
                    {
                        CardTargets testTargets = CardTargetModel.Collect(source, roomIndex, effect, new[] { unit.Id }, isTesting: true);
                        if (!testTargets.Supported) return testTargets.UnsupportedReason;
                        int count = TestCount(source, effect, testTargets);
                        if (count < 0) return "All-status adjustment requires complete propagation metadata.";
                        outcomes.Add(PassesTest(effect, count, source.Context.AllScenarioBossesDead == true));
                    }
                    if (outcomes.Distinct().Count() > 1) return "Mandatory last-target tests after random selection depend on the auxiliary test stream.";
                }
            }
            return null;
        }

        private static TrainCombatState SingleRoom(RoomCombatState room) => new TrainCombatState(new[] { room },
            Array.Empty<EnemyMovement>(), 1, room.Context);

        internal static TrainCombatState WithContext(TrainCombatState source, CombatContext context) => new TrainCombatState(
            source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment, room.Units,
                room.ExternalInteractions, context, room.Preview, room.ActiveCompanionBossIds)).ToArray(), source.Movement, source.EnemySlotsPerRoom, context);

        private static TrainCombatState ReplaceRoom(TrainCombatState source, RoomCombatState changed)
        {
            RoomCombatState[] rooms = source.Rooms.Select(room => room.RoomIndex == changed.RoomIndex ? changed : room).ToArray();
            var alive = new HashSet<int>(rooms.SelectMany(room => room.Units).Select(unit => unit.Id));
            return WithContext(new TrainCombatState(rooms, source.Movement.Where(move => alive.Contains(move.UnitId)).ToArray(),
                source.EnemySlotsPerRoom, changed.Context), changed.Context!);
        }

        private static bool ActorIsLive(CombatUnit unit) => unit.Health > 0 && unit.DeathState?.IsDestroyed != true &&
            unit.DeathState?.IsDespawned != true;

        private static TrainCombatState ReplaceExternalActor(TrainCombatState source, int roomIndex, CombatUnit actor)
        {
            CombatContext context = source.Context!;
            bool live = ActorIsLive(actor);
            RoomCombatState[] rooms = source.Rooms.Select(room =>
            {
                if (room.RoomIndex != roomIndex) return new RoomCombatState(room.RoomIndex, room.Deployment, room.Units,
                    room.ExternalInteractions, context, room.Preview, room.ActiveCompanionBossIds);
                CombatUnit[] units = room.Units.Select(unit => unit.Id == actor.Id && live ? actor : unit)
                    .Where(unit => unit.Id != actor.Id || live).ToArray();
                return new RoomCombatState(room.RoomIndex, room.Deployment, units, room.ExternalInteractions, context,
                    room.Preview, room.ActiveCompanionBossIds);
            }).ToArray();
            var activeIds = rooms.SelectMany(room => room.Units).Select(unit => unit.Id).ToHashSet();
            return WithContext(new TrainCombatState(rooms, source.Movement.Where(move => activeIds.Contains(move.UnitId)).ToArray(),
                source.EnemySlotsPerRoom, context), context);
        }

        private static TrainSpellResult ApplyUnitAttackTurn(TrainCombatState source, int roomIndex, int actorId,
            Func<CombatContext, bool, RelicPyreAttackRefreshResult> refreshPyre)
        {
            RoomCombatState? attackRoom = source.Rooms.FirstOrDefault(room => room.RoomIndex == roomIndex);
            RoomCombatState? actorRoom = source.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == actorId));
            CombatUnit? actor = actorRoom?.Units.FirstOrDefault(unit => unit.Id == actorId);
            if (attackRoom == null || actorRoom == null || actor == null)
                return UnsupportedTrain("Unit attack requires a live actor and attack room.");
            bool external = actorRoom.RoomIndex != attackRoom.RoomIndex;
            RoomCombatResult turn = RoomCombatModel.ApplyUnitTurn(attackRoom, actorId, refreshPyre,
                ignoreAttackPrevention: true, externalActor: external ? actor : null);
            if (!turn.Supported) return UnsupportedTrain(turn.UnsupportedReason!);

            TrainCombatState changed;
            CombatUnit? updatedActor = null;
            if (!external) changed = ReplaceRoom(source, turn.State!);
            else
            {
                updatedActor = turn.RetainedUnits.FirstOrDefault(unit => unit.Id == actorId);
                if (updatedActor == null)
                    return UnsupportedTrain("External unit attack did not retain the actor's updated state.");
                CombatContext context = turn.State!.Context!;
                RoomCombatState[] rooms = source.Rooms.Select(room =>
                {
                    if (room.RoomIndex == attackRoom.RoomIndex) return turn.State;
                    if (room.RoomIndex != actorRoom.RoomIndex) return new RoomCombatState(room.RoomIndex, room.Deployment,
                        room.Units, room.ExternalInteractions, context, room.Preview, room.ActiveCompanionBossIds);
                    bool actorAlive = updatedActor.Health > 0 && updatedActor.DeathState?.IsDestroyed != true &&
                        updatedActor.DeathState?.IsDespawned != true;
                    CombatUnit[] units = room.Units.Select(unit => unit.Id == actorId && actorAlive ? updatedActor : unit)
                        .Where(unit => unit.Id != actorId || actorAlive).ToArray();
                    return new RoomCombatState(room.RoomIndex, room.Deployment, units, room.ExternalInteractions, context,
                        room.Preview, room.ActiveCompanionBossIds);
                }).ToArray();
                var aliveIds = new HashSet<int>(rooms.SelectMany(room => room.Units).Select(unit => unit.Id));
                changed = WithContext(new TrainCombatState(rooms, source.Movement.Where(move => aliveIds.Contains(move.UnitId)).ToArray(),
                    source.EnemySlotsPerRoom, context), context);
            }
            IReadOnlyList<CombatUnit> retained = turn.RetainedUnits.Where(unit => !external || unit.Id != actorId ||
                updatedActor!.Health <= 0 || updatedActor.DeathState?.IsDestroyed == true || updatedActor.DeathState?.IsDespawned == true).ToArray();
            return new TrainSpellResult(changed, turn.Outcome, turn.Events, otherPiles: changed.Context!.OtherPiles,
                pendingCallbacks: turn.PendingCallbacks, retainedUnits: retained);
        }

        private static string? Validate(RoomCombatState source, IReadOnlyList<CardActionEffect> effects, int targetId)
        {
            string? error = RoomCombatModel.Validate(source);
            if (error != null) return error;
            if (source.Context == null || effects.Count == 0) return "A spell requires context and effects.";
            if (RequiresUnitTarget(effects) && !source.Units.Any(unit => unit.Id == targetId)) return "The spell target is missing.";
            if (effects[0].Tests?.ShouldTest == false && effects.Skip(1).Any(effect => effect.Target.Contains("LastTargeted")))
                return "Casting with an untested first effect requires uncaptured target history.";
            if (CardTargetModel.IsRandom(effects[0].Target) && effects[0].AllowEnemy && effects[0].AllowPlayer && effects.Skip(1).Any(effect =>
                effect.Tests?.ShouldTest != false && effect.Tests?.FailToCast == true && effect.Target == "LastTargetedCharacters" &&
                (effects[0].AllowEnemy && !effect.AllowEnemy || effects[0].AllowPlayer && !effect.AllowPlayer)))
                return "Mandatory last-target team tests after random selection depend on the auxiliary test stream.";
            foreach (CardActionEffect effect in effects)
            {
                string? filterError = effect.Target == "Pyre" ? null : effect.Filters?.Validate();
                if (filterError != null) return filterError;
                if (effect.Range != null && effect.Type != "CopyUnits" && effect.Type != "Bump" && effect.Type != "DiscardHand" && effect.Type != "RandomDiscard" && effect.Type != "Generate" && effect.Type != "AdjustCapacity" && effect.Type != "AttachRoomCapacity" && effect.Type != "ScheduleNextDrawUpgrade" &&
                    !AbilityLifecycleModel.IsEffect(effect.Type))
                {
                    if (effect.Type == "AdjustAllStatusEffects") return "Status adjustment effects use a fixed integer value, not an integer range.";
                    if (!EnergyModel.IsEffect(effect.Type) && effect.Type != ForgePointsModel.Effect && !new[] { "Damage", "DamagePerHp", "Heal", "AddStatus", "MultiplyStatus", "MultiplyAllPositiveStatusEffects", "BuffAttack", "DebuffAttack", "BuffHealth", "DebuffHealth", "Draw", "DrawType", "DrawNextTurn" }.Contains(effect.Type))
                        return "Unmodeled range consumer " + effect.Type;
                    string? rangeError = effect.Range.Validate();
                    if (rangeError != null) return rangeError;
                }
                if (effect.Type == "RewardGold")
                {
                    if (!effect.GoldCostRequired.HasValue) return "Missing native gold cost requirement.";
                    if (effect.Range != null) return "Gold rewards with setup-time range initialization require captured runtime effect amounts.";
                    if (source.Context.Resources == null) return "Gold rewards require captured Pyre and gold resource rules.";
                    continue;
                }
                if (effect.Type == "HandUpgrade")
                {
                    if (effect.Upgrade == null) return "Missing hand upgrade definition.";
                    if (effect.Target != "Hand" && effect.Target != "Room") return "Unmodeled hand upgrade target collection.";
                    continue;
                }
                if (effect.Type == "Recursion")
                {
                    string? recursionError = CardRecursionModel.Validate(effect, source.Context.OtherPiles, source.Context.Cards);
                    if (recursionError != null) return recursionError;
                    continue;
                }
                if (!CardOnlyPileTarget(effect) && !CardTargetModel.Supports(effect.Target)) return "Unimplemented spell targeting " + effect.Target;
                if (effect.Type == "ScheduleNextDrawUpgrade")
                {
                    if (effect.NextDrawUpgrade == null) return "Missing next-draw upgrade definition.";
                    if (source.Context.Cards.NextDrawUpgrades == null) return "Missing captured next-draw upgrade queue.";
                    if (!source.Context.Cards.NextUpgradeReferenceId.HasValue) return "Missing captured next-draw upgrade reference allocation.";
                    if (effect.NextDrawUpgrade.Upgrade.ExternalInteractions.Count > 0)
                        return string.Join("; ", effect.NextDrawUpgrade.Upgrade.ExternalInteractions);
                    continue;
                }
                if (effect.Type == "DrawType")
                {
                    if (effect.TypedDraw == null) return "Missing typed-draw definition.";
                    continue;
                }
                if (effect.Type == "RemoveStatus")
                {
                    string? removalError = StatusRemovalModel.Validate(effect, source);
                    if (removalError != null) return removalError;
                    continue;
                }
                if (effect.Type == "Sacrifice" && !effect.DoNotFullyRemove.HasValue)
                    return "Missing native do-not-fully-remove sacrifice parameter.";
                if (AbilityLifecycleModel.IsEffect(effect.Type))
                {
                    if (effect.AbilityChange == null) return "Missing lifecycle effect definition.";
                    continue;
                }
                if (AbilityCooldownModel.IsEffect(effect.Type))
                {
                    if (!effect.CooldownParameter.HasValue) return "Missing ability cooldown parameter.";
                    if (effect.Type == "ResetCooldown" && source.Context?.StatusRules.All(rule => rule.Id != "cooldown") != false)
                        return "Missing cooldown status definition.";
                    continue;
                }
                if (effect.Type == "AdjustCapacity")
                {
                    string? capacityError = RoomCapacityModel.Validate(source.Context);
                    if (capacityError != null) return capacityError;
                    continue;
                }
                if (effect.Type == "AttachRoomCapacity")
                {
                    if (effect.RoomAttachment == null || effect.RoomAttachment.AttachmentType != 0)
                        return "Missing supported primary capacity attachment definition.";
                    string? capacityError = RoomCapacityModel.Validate(source.Context);
                    if (capacityError != null) return capacityError;
                    continue;
                }
                if (effect.Type == "DrawNextTurn")
                {
                    if (source.Context?.Cards.BonusDraw == null) return "Missing bonus-draw state.";
                    string? bonusError = BonusDrawModel.Validate(source.Context.Cards);
                    if (bonusError != null) return bonusError;
                    if (effect.Upgrade?.ExternalInteractions.Count > 0) return string.Join("; ", effect.Upgrade.ExternalInteractions);
                    continue;
                }
                if (EnergyModel.IsEffect(effect.Type))
                {
                    string? energyError = EnergyModel.Validate(source.Context);
                    if (energyError != null) return energyError;
                    continue;
                }
                if (effect.Type == ForgePointsModel.Effect)
                {
                    string? forgeError = ForgePointsModel.Validate(source.Context);
                    if (forgeError != null) return forgeError;
                    continue;
                }
                if (!new[] { "CopyUnits", "Bump", "AbilityTeleport", "AttackWithUnit", "Damage", "DamagePerSourceAttack", "DamagePerHp", "Heal", "Sacrifice", "Kill", "AddStatus", "MultiplyStatus", "MultiplyAllPositiveStatusEffects", "AdjustAllStatusEffects", "AddStatusPerOtherEffect", "SpawnOrStatus", "FloorRearrange", "UnitUpgrade", "BuffEquipment", "RemoveUnitUpgrade", "AttachEquipment", "RemoveEquipment", "BuffAttack", "DebuffAttack", "BuffHealth", "DebuffHealth", "Draw", "DiscardHand", "RandomDiscard", "Generate" }.Contains(effect.Type))
                    return "Unimplemented spell effect " + effect.Type;
                if (effect.Type == "Generate" && effect.Generation == null) return "Missing generated card rules.";
                if ((effect.Type == "UnitUpgrade" || effect.Type == "BuffEquipment" || effect.Type == "RemoveUnitUpgrade") && effect.Upgrade == null)
                    return "Missing unit upgrade definition.";
                if (effect.Type == "BuffEquipment" && effect.Lifetime != "TemporaryUntilUnitDeath")
                    return "Equipment buffs require the native unit-death lifetime.";
                if (effect.Value < 0 && !AttackChange(effect) && effect.Type != ForgePointsModel.Effect && !new[] { "CopyUnits", "Bump", "Damage", "DamagePerSourceAttack", "DamagePerHp", "Heal", "AddStatus", "AddStatusPerOtherEffect", "SpawnOrStatus", "MultiplyAllPositiveStatusEffects", "AdjustAllStatusEffects", "BuffHealth", "DebuffHealth", "Draw", "DiscardHand", "Generate" }.Contains(effect.Type) ||
                    effect.Type == "FloorRearrange" && effect.Value > 1) return "Invalid spell effect value.";
                if (effect.Type == "BuffHealth" && effect.Lifetime != "" && effect.Lifetime != "TemporaryUntilEndOfBattle" &&
                    effect.Lifetime != "TemporaryUntilUnitDeath") return "Unmodeled maximum-health buff lifetime.";
                if (effect.Type == "FloorRearrange" && effect.Target != "DropTargetCharacter") return "Floor rearrangement requires a drop target.";
                if (effect.Type == "AdjustAllStatusEffects" && (!effect.StatusAdjustmentFilter.HasValue || effect.StatusAdjustmentFilter < 0 || effect.StatusAdjustmentFilter > 1))
                    return "Unmodeled all-status adjustment filter.";
                if ((effect.Type == "AddStatus" || effect.Type == "MultiplyStatus") && effect.Statuses.Count == 0)
                    return "A status effect requires at least one status definition.";
                if (effect.Type == "AddStatusPerOtherEffect" && effect.Statuses.Count < 2)
                    return "A per-status effect requires both applied and counted status definitions.";
                if (effect.Type == "SpawnOrStatus" && (effect.Statuses.Count == 0 || effect.SpawnFallbackUnit == null))
                    return "Spawn-or-status requires a status and fallback unit definition.";
                foreach (CombatStatus status in effect.Statuses)
                {
                    // This unit validates a definition, not captured live attack history.
                    // Retain the context for status rules while satisfying its cache guard.
                    var test = new CombatUnit(0, "status-validation", CombatTeam.Player, 0, 1, 1, false, false, false, new[] { status },
                        modifiers: new UnitModifiers(0, 0, 0, 1, 1, true, false, Array.Empty<CardUpgradeModifier>(),
                            damageBuffFromStatusEffects: 0));
                    error = RoomCombatModel.Validate(new RoomCombatState(source.RoomIndex, source.Deployment,
                        new[] { test }, Array.Empty<string>(), source.Context, source.Preview, source.ActiveCompanionBossIds));
                    if (error != null) return error;
                }
            }
            return null;
        }

        private static CardTargets Collect(TrainCombatState state, int roomIndex, CardActionEffect effect, IReadOnlyList<int> last,
            CombatUnit? initial, int dropPosition, int index, BattlePlayRules? definitions, bool isTesting = false,
            IReadOnlyDictionary<int, int>? pendingDeadRooms = null, IReadOnlyDictionary<int, int>? positions = null, int selfUnitId = 0,
            int sourceCardId = 0)
        {
            if (effect.Type == "Recursion")
            {
                IReadOnlyList<int> cardIds = CardRecursionModel.EligibleCards(state.Context!.Cards,
                    state.Context.OtherPiles, effect, definitions, sourceCardId, out string? error,
                    state.Context.CardRegistry);
                return new CardTargets(Array.Empty<int>(), error, cardIds: cardIds);
            }
            if (effect.Type == "SpawnOrStatus")
            {
                RoomCombatState? room = state.Rooms.FirstOrDefault(item => item.RoomIndex == roomIndex);
                RoomPlayRule? rule = definitions?.Rooms.FirstOrDefault(item => item.RoomIndex == roomIndex);
                if (room == null || rule == null) return new CardTargets(Array.Empty<int>(), "Spawn-or-status requires the selected room rule.");
                if (rule.IsPyre || !rule.Enabled || rule.SummonBlocked) return new CardTargets(Array.Empty<int>());
                if (effect.Filters == null) return new CardTargets(Array.Empty<int>(), "Spawn-or-status requires its subtype filter metadata.");
                IEnumerable<CombatUnit> candidates = room.Units.Where(unit => unit.Team == CombatTeam.Player);
                if (positions != null)
                    candidates = candidates.OrderBy(unit => positions.TryGetValue(unit.Id, out int position) ? position : int.MaxValue);
                foreach (CombatUnit unit in candidates)
                {
                    if (unit.IsPyre || unit.Health <= 0 && (unit.Status("undying")?.Stacks ?? 0) <= 0 ||
                        unit.DeathState?.IsBeingRemoved == true || unit.DeathState?.IsDestroyed == true || unit.DeathState?.IsDespawned == true)
                        continue;
                    if (effect.Filters.Subtype.Length > 0 && !unit.Subtypes.Contains(effect.Filters.Subtype)) continue;
                    if (!unit.IsBoss.HasValue) return new CardTargets(Array.Empty<int>(), "Spawn-or-status requires captured boss classification.");
                    if (unit.IsBoss.Value) continue;
                    return new CardTargets(new[] { unit.Id });
                }
                if (state.Context?.SpawnPoints == null)
                    return new CardTargets(Array.Empty<int>(), "Spawn-or-status fallback requires captured spawn points.");
                return new CardTargets(Array.Empty<int>());
            }
            return effect.Type == "HandUpgrade" && effect.Target == "Hand" || CardOnlyPileTarget(effect)
                ? new CardTargets(Array.Empty<int>()) : CardTargetModel.Collect(state, roomIndex, effect, last, initial?.Team, dropPosition,
                    index == 0, isTesting, definitions?.Rooms.FirstOrDefault(room => room.IsPyre)?.RoomIndex, pendingDeadRooms, positions, selfUnitId);
        }

        private static void Remember(CardActionEffect effect, int index, CardTargets targets, ref IReadOnlyList<int> last)
        {
            if (effect.Type == "SpawnOrStatus") return;
            if (index == 0 || effect.Target == "DropTargetCharacter") last = targets.UnitIds;
        }

        private static int DropPosition(RoomCombatState source, CombatUnit? initial) => initial == null ? -1 :
            source.Units.Where(unit => unit.Team == initial.Team).TakeWhile(unit => unit.Id != initial.Id).Count();

        private static bool PassesTest(CardActionEffect effect, int count, bool bossDead = false, CombatContext? context = null, bool preview = false,
            RoomCombatState? room = null, bool? pyreAlive = null, int sourceCardId = 0)
        {
            if (effect.Type == "RewardGold") return context != null && (pyreAlive ?? context.EnergyState?.PyreAlive) == true &&
                (effect.Value >= 0 || effect.GoldCostRequired == false || context.Gold >= unchecked(-effect.Value));
            if (effect.Type == "AdjustCapacity") return !preview && !bossDead && RoomCapacityModel.Test(room, effect);
            if (effect.Type == "AttachRoomCapacity") return !preview && !bossDead && RoomCapacityModel.TestAttachment(room, effect);
            if (effect.Type == ForgePointsModel.Effect) return ForgePointsModel.Test(pyreAlive ?? context?.EnergyState?.PyreAlive);
            if (EnergyModel.IsEffect(effect.Type)) return !preview && !bossDead && EnergyModel.Test(context!, effect.Type);
            if (preview && (effect.Type == "Draw" || effect.Type == "DrawType" || effect.Type == "ScheduleNextDrawUpgrade" || effect.Type == "DrawNextTurn" || effect.Type == "DiscardHand" || effect.Type == "RandomDiscard" || effect.Type == "Generate")) return false;
            if (bossDead && !(effect.Tests?.CanPlayAfterBossDead ?? (effect.Type != "HandUpgrade" && effect.Type != "Draw" && effect.Type != "DrawType" && effect.Type != "ScheduleNextDrawUpgrade" && effect.Type != "DrawNextTurn" && effect.Type != "DiscardHand" && effect.Type != "RandomDiscard" && effect.Type != "Generate"))) return false;
            if (effect.Type == "RandomDiscard") return context != null &&
                RandomDiscardModel.EligibleCount(context, sourceCardId, out _) is int eligible && eligible > 0;
            if (effect.Type == "Generate" && effect.Generation?.RequireHandSpace == true && context != null && context.Cards.Hand.Count >= context.MaxHandSize) return false;
            switch (effect.Type)
            {
                case "RemoveStatus": return string.IsNullOrEmpty(effect.Filters?.Subtype) || count > 0;
                case "Draw": return context != null && context.Cards.Hand.Count - 1 < context.MaxHandSize;
                case "ScheduleNextDrawUpgrade": return context != null && (Math.Abs(effect.RequiredGold) == 0 || context.Gold >= Math.Abs(effect.RequiredGold));
                case "Damage": return effect.Value >= 0 && (effect.Range == null || effect.Range.Max > 0) &&
                    (effect.Target != "DropTargetCharacter" || count > 0);
                case "DamagePerSourceAttack": return count > 0;
                case "DamagePerHp": return count > 0;
                case "Heal": return effect.Target == "Room" || count > 0;
                case "Sacrifice": return count > 0;
                case "Kill": return effect.Tests?.MustHaveValidTargets != true || count > 0;
                case "Recursion": return count > 0 &&
                    (effect.Recursion?.SelectionMode is "ChooseToHand" or "ChooseToDeck" || effect.Value > 0);
                case "AddStatus":
                case "MultiplyStatus": return effect.Tests?.StrictTargets != true && effect.Target != "DropTargetCharacter" || count > 0;
                case "MultiplyAllPositiveStatusEffects": return effect.Target != "DropTargetCharacter" || count > 0;
                case "AdjustAllStatusEffects": return effect.Tests?.StrictTargets != true || count > 0;
                case "AddStatusPerOtherEffect": return count > 0;
                case "SpawnOrStatus": return count > 0 || CanSpawnFallback(context, room, effect);
                case "FloorRearrange":
                case "CopyUnits":
                case "BuffAttack":
                case "DebuffAttack":
                case "UnitUpgrade":
                case "BuffEquipment":
                case "AttachEquipment":
                case "RemoveEquipment": return count > 0;
                default: return true;
            }
        }

        private static bool PassesTargetSubtype(TrainCombatState source, CardActionEffect effect, CardTargets targets)
        {
            if (effect.Type != "Kill" || effect.TargetSubtype.Length == 0) return true;
            CombatUnit[] units = source.Rooms.SelectMany(room => room.Units).ToArray();
            return targets.UnitIds.All(id => units.Any(unit => unit.Id == id && unit.Subtypes.Contains(effect.TargetSubtype)));
        }

        private static bool? PyreAlive(TrainCombatState state, bool fullTrain)
        {
            CombatUnit? pyre = state.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.IsPyre);
            return pyre != null ? pyre.Health > 0 : fullTrain ? false : state.Context?.EnergyState?.PyreAlive;
        }

        private static bool CanSpawnFallback(CombatContext? context, RoomCombatState? room, CardActionEffect effect)
        {
            if (context?.SpawnPoints == null || room == null || effect.SpawnFallbackUnit == null ||
                room.Units.Any(unit => unit.IsPyre) || context.RoomCapacities?.FirstOrDefault(item => item.RoomIndex == room.RoomIndex)?.IsPyre == true)
                return false;
            return context.SpawnPoints.FirstEmpty(room.RoomIndex, CombatTeam.Player) >= 0;
        }

        private static string? BuffEquipmentCards(ref TrainCombatState state, int targetId, int sourceCardId,
            CardActionEffect effect, bool preview, List<RoomCombatModel.QueuedCharacterTrigger> callbacks,
            List<CombatEvent> events, ref RoomOutcome outcome,
            Func<CombatContext, bool, RelicPyreAttackRefreshResult> refreshPyre)
        {
            RoomCombatState? initialRoom = state.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == targetId));
            CombatUnit? target = initialRoom?.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (target?.EquipmentCards == null || state.Context?.CardInstances == null)
                return "Equipment buffs require captured equipped-card ownership state.";
            if (effect.Upgrade == null) return "Equipment buff is missing its upgrade definition.";
            if (effect.Upgrade.ExternalInteractions.Count > 0)
                return string.Join("; ", effect.Upgrade.ExternalInteractions);
            bool persistent = !preview && state.Context.IsolatedBattlePreview != true;
            foreach (int equipmentId in target.EquipmentCards.ToArray())
            {
                if (equipmentId == sourceCardId) continue;
                CardInstanceState? card = state.Context!.CardInstances?.FirstOrDefault(item => item.InstanceId == equipmentId);
                if (card == null) return "Equipment buff could not resolve an attached card instance.";
                CardUpgradeModifier unitUpgrade = effect.Upgrade.WithSharedReference(null);
                if (persistent)
                {
                    CardUpgradeModifier? existingEquipmentUpgrade = effect.ScaleUpgrade
                        ? card.Permanent.Upgrades.FirstOrDefault(item => item.DataId == effect.Upgrade.DataId) : null;
                    CardUpgradeModifier cardUpgrade = existingEquipmentUpgrade == null
                        ? CardMagicPowerModel.Scale(unitUpgrade, CardMagicPowerModel.Multiplier(card))
                        : unitUpgrade;
                    CardInstanceState upgraded = card.WithPermanentUpgrade(cardUpgrade, effect.ScaleUpgrade);
                    if (effect.ScaleUpgrade)
                        unitUpgrade = upgraded.Permanent.Upgrades.First(item => item.DataId == effect.Upgrade.DataId);
                    else unitUpgrade = cardUpgrade;
                    state = WithContext(state, state.Context!.WithCard(upgraded));
                }

                RoomCombatState? room = state.Rooms.FirstOrDefault(item => item.Units.Any(unit => unit.Id == targetId));
                CombatUnit? liveTarget = room?.Units.FirstOrDefault(unit => unit.Id == targetId);
                if (room == null || liveTarget?.Modifiers == null)
                    return "Equipment buff host left the modeled room before all attached equipment resolved.";
                bool hasUpgrade = liveTarget.Modifiers.Upgrades.Any(item => item.DataId == effect.Upgrade.DataId);
                bool scaleUnit = persistent && effect.ScaleUpgrade;
                if (scaleUnit && hasUpgrade)
                    unitUpgrade = effect.Upgrade.WithSharedReference(null);
                RoomCombatResult applied = UnitModifierModel.Apply(room, targetId, unitUpgrade,
                    "TemporaryUntilUnitDeath", sourceCardId: 0, scaleUpgrade: scaleUnit, refreshPyre: refreshPyre,
                    accumulateScaledUpgradeRecord: false);
                if (!applied.Supported) return applied.UnsupportedReason;
                state = ReplaceRoom(state, applied.State!);
                callbacks.AddRange(applied.PendingCallbacks);
                events.AddRange(applied.Events);
                if (applied.Outcome == RoomOutcome.BattleWon || applied.Outcome == RoomOutcome.PlayerDefeated)
                    outcome = applied.Outcome;
            }
            return null;
        }

        private static bool AttackChange(CardActionEffect effect) => effect.Type == "BuffAttack" || effect.Type == "DebuffAttack";
        private static bool CardOnlyPileTarget(CardActionEffect effect) =>
            effect.Type == "RandomDiscard" && effect.Target == "Hand" || effect.Type == "Recursion" || CardOnlyDrawTarget(effect);
        private static bool CardOnlyDrawTarget(CardActionEffect effect) =>
            (effect.Type == "DrawType" || effect.Type == "ScheduleNextDrawUpgrade") &&
            (effect.Target == "DrawPile" || effect.Target == "Hand" || effect.Target == "LastDrawnCard");
        private static bool IsDamage(CardActionEffect effect) => effect.Type == "Damage" || effect.Type == "DamagePerSourceAttack";
        private static bool QuantityTest(CardActionEffect effect) => IsDamage(effect) || effect.Type == "Draw";
        private static CardActionEffect Sample(CardActionEffect effect, ref UnityRng rng)
        {
            if (effect.Range == null) return effect;
            RngDraw draw = effect.Range.Sample(rng); rng = draw.State;
            return new CardActionEffect(effect.Type, effect.Target, draw.Value, effect.AllowEnemy, effect.AllowPlayer,
                effect.Statuses, effect.Upgrade, effect.Lifetime, effect.Tests, effect.Range, effect.Filters, effect.Generation,
                effect.OnlyIfNoEnemies, effect.CooldownParameter, effect.AbilityChange, effect.CopyHeroStats, effect.ScaleUpgrade,
                effect.DoNotFullyRemove, effect.TargetSubtype, effect.TypedDraw, effect.NextDrawUpgrade, effect.RequiredGold, effect.GoldCostRequired,
                effect.UseCardTraits, effect.RequireEquipment, effect.UseSelectedRoom, effect.CountStatusOnlyOnTargets,
                effect.SpawnFallbackUnit, effect.DisallowStatusStackModifiers, effect.StatusAdjustmentFilter,
                effect.DamagePerHpLastAttacker, effect.DamagePerHpIncludeArmor, effect.RoomAttachment, effect.Recursion);
        }
        private static int TestCount(TrainCombatState state, CardActionEffect effect, CardTargets targets)
        {
            if (effect.Type == "Recursion") return targets.CardIds.Count;
            if (effect.Type == "AdjustAllStatusEffects")
            {
                if (effect.StatusAdjustmentFilter == 0 && effect.Statuses.Count == 0) return 0;
                CombatUnit[] targetUnits = state.Rooms.SelectMany(room => room.Units).Where(unit => targets.UnitIds.Contains(unit.Id)).ToArray();
                if (effect.StatusAdjustmentFilter == 1)
                foreach (CombatUnit unit in targetUnits)
                foreach (CombatStatus status in unit.Statuses.Where(status => status.Stacks > 0))
                {
                    if (!status.Propagatable.HasValue || status.Propagatable.Value &&
                        !(unit.Team == CombatTeam.Enemy ? status.ExcludeHeroPropagation.HasValue : status.ExcludeMonsterPropagation.HasValue))
                        return -1;
                }
                return targetUnits.Count(unit => unit.Statuses.Any(status =>
                    status.Stacks > 0 && StatusAdjustmentMatches(unit, status, effect, allowEmptyList: false)));
            }
            if (effect.Type == "RemoveStatus" && !string.IsNullOrEmpty(effect.Filters?.Subtype))
                return state.Rooms.SelectMany(room => room.Units).Count(unit =>
                    targets.UnitIds.Contains(unit.Id) && unit.Subtypes.Contains(effect.Filters!.Subtype));
            if (effect.Type == "RemoveEquipment")
                return state.Rooms.SelectMany(room => room.Units).Count(unit => targets.UnitIds.Contains(unit.Id) && unit.EquipmentCards?.Count > 0);
            return !AttackChange(effect) ? targets.UnitIds.Count : state.Rooms.SelectMany(room => room.Units)
                .Count(unit => targets.UnitIds.Contains(unit.Id) && (unit.IsPyre || unit.CanAttack));
        }
        private static bool StatusAdjustmentMatches(CombatUnit unit, CombatStatus status, CardActionEffect effect, bool allowEmptyList)
        {
            if (effect.StatusAdjustmentFilter == 0)
                return effect.Statuses.Count == 0 ? allowEmptyList : effect.Statuses.Any(item => item.Id == status.Id);
            if (effect.StatusAdjustmentFilter != 1 || status.Propagatable != true) return false;
            return unit.Team == CombatTeam.Enemy ? status.ExcludeHeroPropagation == false : status.ExcludeMonsterPropagation == false;
        }
        private static string? DamagePerHpTargetError(TrainCombatState state, CardActionEffect effect, CardTargets targets, int selfUnitId)
        {
            if (effect.Type != "DamagePerHp" || !effect.DamagePerHpLastAttacker || targets.UnitIds.Count == 0) return null;
            int targetId = targets.UnitIds[0];
            RoomCombatState? room = state.Rooms.FirstOrDefault(item => item.Units.Any(unit => unit.Id == targetId));
            CombatUnit? target = room?.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (room == null || target == null) return "Damage-per-HP target left the captured train before target validation.";
            if (selfUnitId <= 0) return null;
            CombatUnit? source = state.Rooms.SelectMany(item => item.Units).FirstOrDefault(unit => unit.Id == selfUnitId);
            if (source == null) return "Damage-per-HP last-attacker source left the captured train before target validation.";
            if (!source.LastAttackerId.HasValue)
                return "Damage-per-HP last-attacker targeting requires a captured source attacker reference.";
            int attackerId = source.LastAttackerId.Value;
            if (attackerId == 0) return null;
            CombatUnit? attacker = state.Rooms.SelectMany(item => item.Units)
                .FirstOrDefault(unit => unit.Id == attackerId);
            return attacker == null ? "Damage-per-HP last attacker is missing from the captured train." : null;
        }
        private static string? AttackTestError(TrainCombatState state, int roomIndex, CardActionEffect effect)
        {
            if (!AttackChange(effect) || !CardTargetModel.IsRandom(effect.Target)) return null;
            CardTargets candidates = CardTargetModel.RandomCandidates(state, roomIndex, effect);
            if (!candidates.Supported) return candidates.UnsupportedReason;
            bool[] outcomes = state.Rooms.SelectMany(room => room.Units).Where(unit => candidates.UnitIds.Contains(unit.Id))
                .Select(unit => unit.IsPyre || unit.CanAttack).Distinct().ToArray();
            return outcomes.Length > 1 ? "Random attack-change tests depend on the auxiliary test stream." : null;
        }

        private static RoomCombatResult ApplyOne(RoomCombatState state, CardActionEffect effect, CombatUnit target,
            int sourceCardId, int? playerCapacity, int? enemyCapacity, bool deferSpawnerExhaustion = false, int? scaledDamage = null, int selfUnitId = 0,
            bool sourceHasDuality = false, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            CombatUnit? externalActor = null, int externalActorRoomIndex = -1,
            Action<RoomCombatModel.QueuedCharacterTrigger>? enqueueCharacterTrigger = null)
        {
            if (IsDamage(effect)) return scaledDamage.HasValue
                ? RoomCombatModel.ApplyCardDamageAfterTraits(state, target.Id, scaledDamage.Value, sourceCardId,
                    deferSpawnerExhaustion, selfUnitId, refreshPyre: refreshPyre, externalActor: externalActor,
                    externalActorRoomIndex: externalActorRoomIndex, enqueueCharacterTrigger: enqueueCharacterTrigger)
                : RoomCombatModel.ApplyCardDamage(state, target.Id, Math.Max(0, effect.Value), sourceCardId, deferSpawnerExhaustion, selfUnitId, refreshPyre: refreshPyre);
            if (effect.Type == "Heal") return effect.Value < 0 ? Unchanged(state) : RoomCombatModel.ApplyCardHeal(state, target.Id, effect.Value, refreshPyre: refreshPyre);
            if (AttackChange(effect)) return UnitAttackModel.Apply(state, target.Id, effect.Value, effect.Type == "DebuffAttack");
            if (effect.Type == "BuffHealth" || effect.Type == "DebuffHealth")
                return UnitHealthModel.Apply(state, target.Id, effect.Value, effect.Type == "DebuffHealth", effect.Lifetime, refreshPyre: refreshPyre);
            if (effect.Type == "UnitUpgrade" || effect.Type == "BuffEquipment" || effect.Type == "RemoveUnitUpgrade")
                return UnitModifierModel.Apply(state, target.Id, effect.Upgrade!, effect.Lifetime,
                    effect.Type == "RemoveUnitUpgrade", target.Team == CombatTeam.Player ? playerCapacity : enemyCapacity,
                    sourceCardId, scaleUpgrade: effect.ScaleUpgrade, refreshPyre: refreshPyre);
            if (effect.Type == "AdjustAllStatusEffects")
            {
                int filter = effect.StatusAdjustmentFilter!.Value;
                var adjusted = new List<CombatStatus>();
                foreach (CombatStatus status in target.Statuses)
                {
                    if (status.Stacks <= 0) continue;
                    if (filter == 0)
                    {
                        if (effect.Statuses.Count > 0 && !effect.Statuses.Any(item => item.Id == status.Id)) continue;
                    }
                    else
                    {
                        if (!status.Propagatable.HasValue)
                            return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                                "All-status adjustment requires captured propagatable-status metadata.");
                        if (!status.Propagatable.Value) continue;
                        bool? excluded = target.Team == CombatTeam.Enemy ? status.ExcludeHeroPropagation : status.ExcludeMonsterPropagation;
                        if (!excluded.HasValue)
                            return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                                "All-status adjustment requires captured team-propagation metadata.");
                        if (excluded.Value) continue;
                    }
                    adjusted.Add(status);
                }
                var events = new List<CombatEvent>();
                var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
                var retained = new List<CombatUnit>();
                var dispatches = new List<CharacterTriggerDispatch>();
                RoomOutcome outcome = RoomOutcome.Exchanged;
                foreach (CombatStatus status in adjusted)
                {
                    RoomCombatResult applied;
                    if (effect.Value >= 0)
                    {
                        if (filter == 0 && sourceHasDuality && status.Id != "armor" && status.DisplayCategory == null)
                            return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, events,
                                "Duality status adjustment requires a captured display category.");
                        bool doubleForDuality = filter == 0 && sourceHasDuality && (status.Id == "armor" ||
                            status.DisplayCategory == "Positive" || status.DisplayCategory == "Negative");
                        applied = StatusApplicationModel.Apply(state, target.Id, status.WithStacks(effect.Value), sourceCardId,
                            overrideImmunity: effect.Target == "Pyre", fromEffectType: "CardEffectAdjustAllStatusEffects",
                            doubleForDuality: doubleForDuality);
                    }
                    else
                    {
                        if (effect.Value == int.MinValue)
                            return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, events,
                                "The all-status removal amount exceeds the modeled signed range.");
                        applied = StatusRemovalModel.Remove(state, target.Id, status.Id, -effect.Value, sourceCardId);
                    }
                    if (!applied.Supported) return applied;
                    state = applied.State!;
                    events.AddRange(applied.Events);
                    callbacks.AddRange(applied.PendingCallbacks);
                    retained.AddRange(applied.RetainedUnits);
                    dispatches.AddRange(applied.Dispatches);
                    if (applied.Outcome != RoomOutcome.Exchanged)
                    {
                        if (outcome != RoomOutcome.Exchanged && outcome != applied.Outcome)
                            return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, events,
                                "Conflicting terminal results in one status adjustment effect are not modeled.");
                        outcome = applied.Outcome;
                    }
                }
                return new RoomCombatResult(state, outcome, 0, events, pendingCallbacks: callbacks,
                    retainedUnits: retained, dispatches: dispatches);
            }
            if (effect.Type == "MultiplyAllPositiveStatusEffects")
            {
                var events = new List<CombatEvent>();
                var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
                var retained = new List<CombatUnit>();
                var dispatches = new List<CharacterTriggerDispatch>();
                RoomOutcome outcome = RoomOutcome.Exchanged;
                foreach (CombatStatus status in target.Statuses)
                {
                    if (status.Id == "horde") continue;
                    if (status.DisplayCategory == null)
                        return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, events,
                            "Positive-status multiplication requires captured status display categories.");
                    if (status.DisplayCategory != "Positive") continue;
                    int multiplied;
                    int added;
                    unchecked
                    {
                        multiplied = status.Stacks * effect.Value;
                        added = multiplied - status.Stacks;
                    }
                    if (multiplied <= 0 || added <= 0) continue;
                    RoomCombatResult applied = StatusApplicationModel.Apply(state, target.Id, status.WithStacks(added), sourceCardId,
                        overrideImmunity: effect.Target == "Pyre", fromEffectType: "CardEffectMultiplyAllStatusEffects");
                    if (!applied.Supported) return applied;
                    state = applied.State!;
                    events.AddRange(applied.Events);
                    callbacks.AddRange(applied.PendingCallbacks);
                    retained.AddRange(applied.RetainedUnits);
                    dispatches.AddRange(applied.Dispatches);
                    if (applied.Outcome != RoomOutcome.Exchanged)
                    {
                        if (outcome != RoomOutcome.Exchanged && outcome != applied.Outcome)
                            return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, events,
                                "Conflicting terminal results in one status multiplication effect are not modeled.");
                        outcome = applied.Outcome;
                    }
                }
                return new RoomCombatResult(state, outcome, 0, events, pendingCallbacks: callbacks,
                    retainedUnits: retained, dispatches: dispatches);
            }
            if (effect.Type == "AddStatus" || effect.Type == "MultiplyStatus" || effect.Type == "SpawnOrStatus")
            {
                if (effect.Type != "SpawnOrStatus" && effect.Value != 0)
                {
                    // Chance is evaluated for every collected target before status immunity is checked.
                    RngDraw chance = state.Context!.BattleRng.Range(0, 100);
                    state = new RoomCombatState(state.RoomIndex, state.Deployment, state.Units, state.ExternalInteractions,
                        state.Context.WithBattleRng(chance.State), state.Preview, state.ActiveCompanionBossIds);
                    if (chance.Value >= effect.Value) return Unchanged(state);
                }
                if (effect.Type == "MultiplyStatus")
                {
                    CombatStatus status = effect.Statuses[0];
                    int current = target.Status(status.Id)?.Stacks ?? 0;
                    int multiplied;
                    int added;
                    unchecked
                    {
                        multiplied = current * status.Stacks;
                        added = multiplied - current;
                    }
                    if (multiplied <= 0) return Unchanged(state);
                    return StatusApplicationModel.Apply(state, target.Id, status.WithStacks(added), sourceCardId,
                        overrideImmunity: effect.Target == "Pyre", fromEffectType: "CardEffectMultiplyStatusEffect");
                }
                return StatusApplicationModel.Apply(state, target.Id, effect.Statuses[0], sourceCardId,
                    overrideImmunity: effect.Target == "Pyre", fromEffectType: effect.Type == "SpawnOrStatus"
                        ? "CardEffectSpawnMonsterOrAddStatusEffect" : null,
                    allowStackModifiers: effect.Type != "SpawnOrStatus" || !effect.DisallowStatusStackModifiers);
            }
            if (effect.Type == "AbilityTeleport") return Unchanged(state);
            if (target.Statuses.Any(status => status.Id == "immobile")) return Unchanged(state);
            var team = state.Units.Where(unit => unit.Team == target.Team).ToList();
            if (state.Context?.SpawnPoints != null)
            {
                UnitSpawnPointState reference = state.Context.SpawnPoints.Units.Single(unit => unit.UnitId == target.Id);
                if (reference.OuterBoss) return Unchanged(state);
                if (reference.Current == null) return new RoomCombatResult(null, RoomOutcome.Unsupported, 0,
                    new List<CombatEvent>(), "Floor rearrangement requires the target's physical point.");
                var moved = BattleSpawnPointModel.Apply(state.Context.SpawnPoints, state, "Rearrange", target.Team,
                    index: reference.Current.Index, targetIndex: effect.Value == 0 ? 0 : team.Count - 1);
                if (!moved.Supported) return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), moved.Error!);
                var context = state.Context.WithSpawnPoints(moved.State!);
                return Unchanged(new RoomCombatState(state.RoomIndex, state.Deployment,
                    BattleSpawnPointModel.Order(context.SpawnPoints, state.RoomIndex, state.Units), state.ExternalInteractions, context,
                    state.Preview, state.ActiveCompanionBossIds));
            }
            team.Remove(target); team.Insert(effect.Value == 0 ? 0 : team.Count, target);
            CombatUnit[] enemies = target.Team == CombatTeam.Enemy ? team.ToArray() : state.Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
            CombatUnit[] players = target.Team == CombatTeam.Player ? team.ToArray() : state.Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
            return Unchanged(new RoomCombatState(state.RoomIndex, state.Deployment, enemies.Concat(players).ToArray(),
                state.ExternalInteractions, state.Context, state.Preview, state.ActiveCompanionBossIds));
        }

        internal static CombatUnit Copy(CombatUnit unit, int health, IReadOnlyList<CombatStatus> statuses) =>
            new CombatUnit(unit.Id, unit.AssetKey, unit.Team, unit.BaseAttack, health, unit.MaxHealth, unit.CanAttack,
                unit.IsPyre, unit.EndsBattleOnDeath, statuses, unit.Triggers, unit.SpawnerCardId, unit.Size, unit.StatusImmunities, unit.Subtypes, unit.Modifiers, unit.IsBoss, unit.LastAttackerId, unit.StatusRegistry, unit.EquipmentCards, unit.NextTriggerId, unit.Ability, unit.StatusDictionary, unit.AbilityRules, unit.HordeDefinition, unit.IsSpawning, unit.SacrificeCardId, unit.DeathState, bumpRules: unit.BumpRules);
        private static RoomCombatResult Unchanged(RoomCombatState state) => new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static TrainSpellResult UnsupportedTrain(string reason) => new TrainSpellResult(null, RoomOutcome.Unsupported,
            Array.Empty<CombatEvent>(), reason);
    }
}
