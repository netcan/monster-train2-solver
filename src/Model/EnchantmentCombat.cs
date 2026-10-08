using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Each runtime effect owns its maps; immutable copies never share mutable lifecycle state.
    public sealed class EnchantmentRule
    {
        public CardActionEffect Targeting { get; }
        public IReadOnlyList<CombatStatus> StatusPool { get; }
        public EnchantmentState State { get; }
        public bool Bound { get; }
        public bool HasParentCard { get; }
        public EnchantmentRule(CardActionEffect targeting, IReadOnlyList<CombatStatus> statusPool,
            EnchantmentState? state = null, bool bound = false, bool hasParentCard = false)
        { Targeting = targeting; StatusPool = Array.AsReadOnly(statusPool.ToArray()); State = state ?? new EnchantmentState();
            Bound = bound; HasParentCard = hasParentCard; }
        internal EnchantmentRule WithState(EnchantmentState state) => new EnchantmentRule(Targeting, StatusPool, state, Bound, HasParentCard);
    }

    public sealed class EnchantmentRetainedUnit
    {
        public CombatUnit Unit { get; }
        public int RoomIndex { get; }
        public bool Preview { get; }
        public EnchantmentRetainedUnit(CombatUnit unit, int roomIndex, bool preview)
        { Unit = unit; RoomIndex = roomIndex; Preview = preview; }
    }

    // The native room manager guards the whole train, not each aura or each room.
    // Retained actors preserve map keys after movement, removal and destruction.
    public sealed class EnchantmentCombatState
    {
        public TrainCombatState Train { get; }
        public IReadOnlyList<EnchantmentRetainedUnit> RetainedUnits { get; }
        public IReadOnlyList<int> EnchanterIds { get; }
        public bool AllowUpdates { get; }
        public bool Updating { get; }
        public bool Preview { get; }
        public UnityRng TestRng { get; }
        public EnchantmentCombatState(TrainCombatState train, IReadOnlyList<EnchantmentRetainedUnit> retainedUnits,
            IReadOnlyList<int> enchanterIds, bool allowUpdates, bool updating, bool preview, UnityRng testRng)
        {
            Train = train; RetainedUnits = Array.AsReadOnly(retainedUnits.ToArray());
            EnchanterIds = Array.AsReadOnly(enchanterIds.ToArray()); AllowUpdates = allowUpdates; Updating = updating;
            Preview = preview; TestRng = testRng;
            int[] ids = train.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id).Concat(retainedUnits.Select(unit => unit.Unit.Id)).ToArray();
            if (ids.Distinct().Count() != ids.Length) throw new ArgumentException("Live and retained enchantment identities must be unique.");
        }
    }

    public sealed class EnchantmentCallback
    {
        public int RoomIndex { get; }
        public int UnitId { get; }
        public string Kind { get; }
        public int ParamInt { get; }
        public int ParamInt2 { get; }
        public string? ParamString { get; }
        public int? OverrideTargetId { get; }
        public int? DyingCharacterId { get; }
        public bool CanFireTriggers { get; }
        public EnchantmentCallback(int roomIndex, int unitId, string kind, int paramInt, int paramInt2, string? paramString,
            int? overrideTargetId, int? dyingCharacterId, bool canFireTriggers)
        { RoomIndex = roomIndex; UnitId = unitId; Kind = kind; ParamInt = paramInt; ParamInt2 = paramInt2;
            ParamString = paramString; OverrideTargetId = overrideTargetId; DyingCharacterId = dyingCharacterId; CanFireTriggers = canFireTriggers; }
        internal static EnchantmentCallback From(RoomCombatModel.QueuedCharacterTrigger item) => new EnchantmentCallback(
            item.RoomIndex, item.Unit.Id, item.Kind, item.ParamInt, item.ParamInt2, item.ParamString,
            item.OverrideTarget?.Id, item.DyingCharacter?.Id, item.CanFireTriggers);
    }

    public sealed class EnchantmentCombatResult
    {
        public EnchantmentCombatState? State { get; }
        public IReadOnlyList<EnchantmentRequest> Requests { get; }
        public IReadOnlyList<EnchantmentCallback> Callbacks { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        internal IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> PendingCallbacks { get; }
        internal EnchantmentCombatResult(EnchantmentCombatState? state, IReadOnlyList<EnchantmentRequest> requests,
            IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> callbacks, string? error = null)
        { State = state; Requests = Array.AsReadOnly(requests.ToArray()); PendingCallbacks = Array.AsReadOnly(callbacks.ToArray());
            Callbacks = Array.AsReadOnly(callbacks.Select(EnchantmentCallback.From).ToArray()); UnsupportedReason = error; }
    }

    public static class EnchantmentCombatModel
    {
        public static EnchantmentCombatResult UpdateAll(EnchantmentCombatState source) => new Engine(source).All();
        public static EnchantmentCombatResult UpdateEffect(EnchantmentCombatState source, int sourceId, int triggerIndex, int effectIndex)
            => new Engine(source).One(sourceId, triggerIndex, effectIndex);
        public static EnchantmentCombatState PrepareForPreview(EnchantmentCombatState source, int unitId)
        {
            CombatUnit Prepare(CombatUnit unit) => unit.Id != unitId ? unit : unit.WithTriggers(unit.Triggers.Select(trigger =>
                trigger.WithEffects(trigger.Effects.Select(effect => effect.Enchantment == null ? effect : effect.WithEnchantment(
                    effect.Enchantment.WithState(EnchantmentLifecycleModel.PrepareForPreview(effect.Enchantment.State)))).ToArray())).ToArray());
            var train = new TrainCombatState(source.Train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units.Select(Prepare).ToArray(), room.ExternalInteractions, room.Context, room.Preview)).ToArray(),
                source.Train.Movement, source.Train.EnemySlotsPerRoom, source.Train.Context);
            return new EnchantmentCombatState(train, source.RetainedUnits.Select(actor => new EnchantmentRetainedUnit(Prepare(actor.Unit),
                actor.RoomIndex, actor.Preview)).ToArray(), source.EnchanterIds, source.AllowUpdates, source.Updating, source.Preview, source.TestRng);
        }

        // Native UpdateEnchantments only queues status callbacks. Its caller decides when to drain.
        public static EnchantmentCombatResult Drain(EnchantmentCombatResult result)
        {
            if (!result.Supported || result.PendingCallbacks.Count == 0) return result;
            // Room engines do not yet refresh the full aura world in the middle of child
            // effects. Reject those paths rather than returning a stale but supported train.
            foreach (var callback in result.PendingCallbacks)
            {
                CombatUnit unit = result.State!.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == callback.Unit.Id) ?? callback.Unit;
                if (unit.Triggers.Where(trigger => trigger.Kind == callback.Kind).SelectMany(trigger => trigger.Effects)
                    .Any(effect => effect.Type != "CardEffectRewardGold"))
                    return new EnchantmentCombatResult(null, result.Requests, result.PendingCallbacks,
                        "Aura callbacks that change actors/cards require automatic combat lifecycle integration.");
            }
            TrainCombatResult drained = TrainCombatModel.ApplyCharacterQueue(result.State!.Train, result.PendingCallbacks.ToList());
            if (!drained.Supported) return new EnchantmentCombatResult(null, result.Requests, result.PendingCallbacks, drained.UnsupportedReason);
            var state = result.State;
            return new EnchantmentCombatResult(new EnchantmentCombatState(drained.State!, state.RetainedUnits,
                state.EnchanterIds, state.AllowUpdates, state.Updating, state.Preview, state.TestRng), result.Requests,
                Array.Empty<RoomCombatModel.QueuedCharacterTrigger>());
        }

        private sealed class Engine
        {
            private EnchantmentCombatState state;
            private readonly List<EnchantmentRequest> requests = new List<EnchantmentRequest>();
            private readonly List<RoomCombatModel.QueuedCharacterTrigger> callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            private string? error;
            internal Engine(EnchantmentCombatState source) { state = source; }
            internal EnchantmentCombatResult All() { UpdateAll(); return Finish(); }
            internal EnchantmentCombatResult One(int id, int trigger, int effect) { Update(id, trigger, effect); return Finish(); }
            private EnchantmentCombatResult Finish() => new EnchantmentCombatResult(error == null ? state : null, requests, callbacks, error);

            private IEnumerable<EnchantmentRetainedUnit> Actors => state.Train.Rooms.SelectMany(room => room.Units.Select(unit =>
                new EnchantmentRetainedUnit(unit, room.RoomIndex, room.Preview))).Concat(state.RetainedUnits);
            private EnchantmentRetainedUnit Find(int id) => Actors.Single(actor => actor.Unit.Id == id);
            private EnchantmentActor Actor(EnchantmentRetainedUnit item)
            {
                CombatUnit unit = item.Unit;
                return new EnchantmentActor(unit.Id, item.RoomIndex, unit.Team == CombatTeam.Enemy,
                    unit.Health > 0 && unit.DeathState?.IsDespawned != true, unit.DeathState?.IsDestroyed == true,
                    unit.Status("dormant") != null && unit.Status("spark") == null,
                    unit.Status("muted") != null, unit.Status("silenced") != null, unit.Status("duality") != null,
                    unit.Status("undying") != null, item.Preview);
            }
            private void UpdateAll()
            {
                if (!state.AllowUpdates || state.Updating || error != null) return;
                Set(updating: true);
                int[] sourceRooms = state.Train.Rooms.Where(room => room.Units.Any(unit => state.EnchanterIds.Contains(unit.Id)))
                    .Select(room => room.RoomIndex).ToArray();
                if (sourceRooms.Length > 1 && state.Train.Context?.SpawnPoints == null)
                { error = "Cross-room enchantment source order requires captured physical points."; Set(updating: false); return; }
                var positions = new List<EnchantmentSourcePosition>();
                foreach (RoomCombatState room in state.Train.Rooms)
                    foreach (CombatTeam team in new[] { CombatTeam.Enemy, CombatTeam.Player })
                    {
                        int rank = 0;
                        foreach (CombatUnit unit in room.Units.Where(unit => unit.Team == team))
                        {
                            var group = state.Train.Context?.SpawnPoints?.Group(room.RoomIndex, team);
                            int index = group == null ? rank : Array.IndexOf(group.Occupants.ToArray(), unit.Id);
                            if (index < 0) { error = "Missing physical point for enchantment source collection."; Set(updating: false); return; }
                            positions.Add(new EnchantmentSourcePosition(unit.Id, team, room.RoomIndex, index)); rank++;
                        }
                    }
                int[] sources = EnchantmentSourceOrderModel.Collect(state.Train.Rooms.Count, positions);
                foreach (int id in sources)
                {
                    if (!state.EnchanterIds.Contains(id)) continue;
                    CombatUnit unit = Find(id).Unit;
                    for (int trigger = 0; trigger < unit.Triggers.Count; trigger++)
                        for (int effect = 0; effect < unit.Triggers[trigger].Effects.Count; effect++)
                            if (unit.Triggers[trigger].Effects[effect].Type == "CardEffectEnchant") Update(id, trigger, effect);
                    if (error != null) break;
                }
                Set(updating: false);
                // Deathwish affects combat and must not silently become a no-op.
                if (state.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Triggers.Any(trigger =>
                    trigger.Kind == "OnDeathwish" || trigger.Kind == "OnDeathwishLost")))
                    error = "Deathwish after enchantments is not modeled.";
            }
            private EnchantmentRule Rule(int id, int trigger, int effect) => Find(id).Unit.Triggers[trigger].Effects[effect].Enchantment
                ?? throw new InvalidOperationException("Missing persistent enchantment definition.");
            private void Store(int id, int triggerIndex, int effectIndex, EnchantmentState value)
            {
                CombatUnit unit = Find(id).Unit;
                var triggers = unit.Triggers.ToArray(); var effects = triggers[triggerIndex].Effects.ToArray();
                effects[effectIndex] = effects[effectIndex].WithEnchantment(Rule(id, triggerIndex, effectIndex).WithState(value));
                triggers[triggerIndex] = triggers[triggerIndex].WithEffects(effects); Replace(unit.WithTriggers(triggers));
            }
            private void Update(int id, int trigger, int effect)
            {
                if (error != null) return;
                EnchantmentRule? rule = Find(id).Unit.Triggers[trigger].Effects[effect].Enchantment;
                if (rule == null) { error = "Missing persistent enchantment definition."; return; }
                if (!rule.Bound) return;
                if (state.Train.Context == null) { error = "Enchantment updates require shared combat context."; return; }
                if (state.Train.Rooms.Any(item => item.ExternalInteractions.Count != 0))
                { error = "Enchantment updates require modeled room/relic interactions."; return; }
                if (Actors.Any(actor => actor.Unit.Status("purify") != null))
                { error = "Purified enchantment status targets are not modeled."; return; }
                if (rule.StatusPool.Any(status => status.Id == "horde"))
                { error = "Horde aura births/casualties require automatic combat lifecycle integration."; return; }
                int room = Find(id).RoomIndex;
                // Status selection precedes target collection in the native effect.
                var input = new EnchantmentInput("Update", id, true, state.Preview, state.Train.Context.BattleRng, state.TestRng,
                    rule.StatusPool.Select(status => new EnchantmentStatus(status.Id, status.Stacks, status.DisplayCategory)).ToArray(),
                    Array.Empty<int>(), Actors.Select(Actor).ToArray());
                var selected = EnchantmentLifecycleModel.Begin(rule.State, input);
                UnityRng targetRng = state.Preview ? selected.TestRng : selected.BattleRng;
                CardTargets targets = new CardTargets(Array.Empty<int>());
                if (rule.StatusPool.Count > 0 && state.Train.Rooms.Any(item => item.RoomIndex == room))
                {
                    TrainCombatState targeting = WithContext(state.Train, state.Train.Context.WithBattleRng(targetRng));
                    targets = CardTargetModel.Collect(targeting, room, rule.Targeting, Array.Empty<int>(), selfUnitId: id, ignorePyre: rule.HasParentCard);
                    if (!targets.Supported) { error = targets.UnsupportedReason; return; }
                }
                // Re-plan with the collected identities without drawing the status pool twice.
                var planInput = new EnchantmentInput("Update", id, true, state.Preview, selected.BattleRng, selected.TestRng,
                    selected.State.CachedStatus == null ? Array.Empty<EnchantmentStatus>() : new[] { selected.State.CachedStatus },
                    targets.UnitIds, Actors.Select(Actor).ToArray());
                var plan = EnchantmentLifecycleModel.Begin(rule.State, planInput);
                Store(id, trigger, effect, plan.State);
                UnityRng battle = selected.BattleRng, test = selected.TestRng;
                if (targets.BattleRng.HasValue) { if (state.Preview) test = targets.BattleRng.Value; else battle = targets.BattleRng.Value; }
                Set(train: WithContext(state.Train, state.Train.Context.WithBattleRng(battle)), testRng: test);
                foreach (int targetId in plan.TargetOrder)
                {
                    EnchantmentActor actor = Actor(Find(targetId));
                    EnchantmentRequest? request = EnchantmentLifecycleModel.Next(Rule(id, trigger, effect).State, actor,
                        state.Preview, plan.SourceDuality, out EnchantmentState atCall);
                    Store(id, trigger, effect, atCall);
                    if (request != null)
                    {
                        requests.Add(request);
                        if (!Status(request, rule)) return;
                    }
                    // The foreach retains its actor reference and original skip decision through the API call.
                    Store(id, trigger, effect, EnchantmentLifecycleModel.Complete(Rule(id, trigger, effect).State, actor, state.Preview));
                }
            }
            private bool Status(EnchantmentRequest request, EnchantmentRule rule)
            {
                EnchantmentRetainedUnit actor = Find(request.UnitId);
                RoomCombatState? liveRoom = state.Train.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == request.UnitId));
                var room = liveRoom ?? new RoomCombatState(actor.RoomIndex, false, new[] { actor.Unit }, Array.Empty<string>(), state.Train.Context, actor.Preview);
                RoomCombatResult changed = request.Operation == "Add"
                    ? StatusApplicationModel.ApplyRetained(room, request.UnitId, rule.StatusPool.First(status => status.Id == request.StatusId).WithStacks(request.Count), 0)
                    : StatusRemovalModel.Remove(room, request.UnitId, request.StatusId, request.Count);
                if (!changed.Supported) { error = changed.UnsupportedReason; return false; }
                CombatUnit? target = changed.State!.Units.FirstOrDefault(unit => unit.Id == request.UnitId);
                if (target == null) target = changed.PendingCallbacks.LastOrDefault(item => item.Unit.Id == request.UnitId)?.Unit;
                if (target == null) { error = "Status removal lost its retained enchantment target."; return false; }
                Replace(target);
                Set(train: WithContext(state.Train, changed.State.Context)); callbacks.AddRange(changed.PendingCallbacks);
                bool registered = actor.Unit.RegisteredStatus(request.StatusId) != null;
                bool removed = (actor.Unit.RegisteredStatus(request.StatusId)?.Stacks ?? 0) > (target.RegisteredStatus(request.StatusId)?.Stacks ?? 0);
                bool added = request.Operation == "Add" && changed.PendingCallbacks.Count > 0;
                if ((added || request.Operation == "Remove" && registered && removed) &&
                    (request.StatusId == "silenced" || request.StatusId == "muted" || request.StatusId == "spark" && target.Status("dormant") != null)) UpdateAll();
                return error == null;
            }
            private void Replace(CombatUnit value)
            {
                TrainCombatState train = new TrainCombatState(state.Train.Rooms.Select(room => new RoomCombatState(room.RoomIndex,
                    room.Deployment, room.Units.Select(unit => unit.Id == value.Id ? value : unit).ToArray(), room.ExternalInteractions,
                    room.Context, room.Preview)).ToArray(), state.Train.Movement, state.Train.EnemySlotsPerRoom, state.Train.Context);
                Set(train: train, retained: state.RetainedUnits.Select(actor => actor.Unit.Id == value.Id
                    ? new EnchantmentRetainedUnit(value, actor.RoomIndex, actor.Preview) : actor).ToArray());
            }
            private void Set(TrainCombatState? train = null, IReadOnlyList<EnchantmentRetainedUnit>? retained = null,
                bool? updating = null, UnityRng? testRng = null) => state = new EnchantmentCombatState(train ?? state.Train,
                    retained ?? state.RetainedUnits, state.EnchanterIds, state.AllowUpdates, updating ?? state.Updating, state.Preview, testRng ?? state.TestRng);
            private static TrainCombatState WithContext(TrainCombatState train, CombatContext? context) => new TrainCombatState(
                train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment, room.Units, room.ExternalInteractions,
                    context, room.Preview)).ToArray(), train.Movement, train.EnemySlotsPerRoom, context);
        }
    }
}
