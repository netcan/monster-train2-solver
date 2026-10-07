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
        public bool Supported => State != null;
        internal TrainSpellResult(TrainCombatState? state, RoomOutcome outcome, IReadOnlyList<CombatEvent> events, string? error = null,
            IReadOnlyList<SpellTargetCollection>? collections = null, IReadOnlyList<CardPileState>? otherPiles = null)
        { State = state; Outcome = outcome; Events = Array.AsReadOnly(events.ToArray()); UnsupportedReason = error;
            TargetCollections = Array.AsReadOnly((collections ?? Array.Empty<SpellTargetCollection>()).ToArray());
            OtherPiles = otherPiles == null ? null : Array.AsReadOnly(otherPiles.ToArray()); }
    }

    public static class CardSpellModel
    {
        internal static string? UnisolatedUiRangeReason(BattleTurnState state) => !state.UiRngIsolated &&
            state.PlayRules?.Cards.Any(card => card.Effects.Any(effect => QuantityTest(effect) && effect.Range != null && effect.Tests?.ShouldTest != false)) == true
                ? "Vanilla UI quantity tests consume Battle RNG at uncaptured frame boundaries; isolate UI RNG before battle simulation." : null;
        public static bool RequiresUnitTarget(IReadOnlyList<CardActionEffect> effects) =>
            effects.Any(effect => effect.Type != "HandUpgrade" && effect.Target == "DropTargetCharacter");

        // Native casting tests all requested effects against the unchanged pre-cast state.
        // Any success permits the cast, unless an explicitly mandatory effect fails.
        public static SpellCastCheck TestPlay(RoomCombatState source, IReadOnlyList<CardActionEffect> effects, int targetId)
            => TestPlayCore(SingleRoom(source), source.RoomIndex, effects, targetId, fullTrain: false);

        public static SpellCastCheck TestPlay(TrainCombatState source, int roomIndex, IReadOnlyList<CardActionEffect> effects, int targetId,
            BattlePlayRules? definitions = null)
            => TestPlayCore(source, roomIndex, effects, targetId, fullTrain: true, definitions);

        private static SpellCastCheck TestPlayCore(TrainCombatState source, int roomIndex, IReadOnlyList<CardActionEffect> effects,
            int targetId, bool fullTrain, BattlePlayRules? definitions = null)
        {
            string? error = Validate(source, roomIndex, effects, targetId, fullTrain);
            if (error != null) return new SpellCastCheck(false, error);
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
                CardTargets targets = Collect(source, roomIndex, effect, last, initial, dropPosition, index, definitions, isTesting: true);
                if (!targets.Supported) return new SpellCastCheck(false, targets.UnsupportedReason);
                Remember(effect, index, targets, ref last);
                string? targetError = AttackTestError(source, roomIndex, effect);
                if (targetError != null) return new SpellCastCheck(false, targetError);
                CardActionEffect tested = effect;
                if (QuantityTest(effect)) tested = Sample(effect, ref testingRng);
                bool valid = PassesTest(tested, TestCount(source, effect, targets), source.Context!.AllScenarioBossesDead == true, source.Context, room.Preview);
                if (!valid && effect.Tests?.FailToCast == true) return new SpellCastCheck(false, battleRngAfterTests: testingRng);
                passed |= valid;
            }
            return new SpellCastCheck(passed, battleRngAfterTests: testingRng);
        }

        public static RoomCombatResult Apply(RoomCombatState source, IReadOnlyList<CardActionEffect> effects, int targetId, int sourceCardId = 0,
            int? playerCapacity = null, int? enemyCapacity = null, BattlePlayRules? definitions = null)
        {
            TrainSpellResult result = ApplyCore(SingleRoom(source), source.RoomIndex, effects, targetId, sourceCardId,
                playerCapacity, enemyCapacity, definitions, fullTrain: false);
            return new RoomCombatResult(result.State?.Rooms.Single(), result.Outcome, 0, result.Events.ToList(), result.UnsupportedReason);
        }

        public static TrainSpellResult Apply(TrainCombatState source, int roomIndex, IReadOnlyList<CardActionEffect> effects,
            int targetId, int sourceCardId = 0, BattlePlayRules? definitions = null, IReadOnlyList<CardPileState>? otherPiles = null)
            => ApplyCore(source, roomIndex, effects, targetId, sourceCardId, null, null, definitions, fullTrain: true, otherPiles);

        private static TrainSpellResult ApplyCore(TrainCombatState source, int roomIndex, IReadOnlyList<CardActionEffect> effects,
            int targetId, int sourceCardId, int? playerCapacity, int? enemyCapacity, BattlePlayRules? definitions, bool fullTrain,
            IReadOnlyList<CardPileState>? otherPiles = null)
        {
            string? error = Validate(source, roomIndex, effects, targetId, fullTrain);
            if (error != null) return UnsupportedTrain(error);
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
            string? routingError = null;
            for (int index = 0; index < effects.Count; index++)
            {
                CardActionEffect effect = effects[index];
                // Each effect reads the card's live modifiers, including earlier hand upgrades.
                if (definitions != null && sourceCardId > 0 && state.Context!.CardInstances != null)
                {
                    CardInstanceState? card = state.Context.FindCard(sourceCardId) ?? resolvingCard;
                    CardPlayRule? rule = definitions.Cards.FirstOrDefault(item => item.DataId == card?.DataId);
                    if (card == null || rule == null || rule.Effects.Count != effects.Count)
                        return UnsupportedTrain("Missing live spell effect definition.");
                    effect = CardModifierModel.Resolve(rule, card).Effects[index];
                    resolvingCard = card;
                }
                if (effect.Target == "FrontInRoomAndRoomAbove" && !focusedRoom.HasValue)
                    return UnsupportedTrain("Room-and-above selection requires uncaptured trigger focus rules.");
                int collectionRoom = effect.Target == "FrontInRoomAndRoomAbove" ? focusedRoom!.Value : roomIndex;
                CardTargets targets = Collect(state, collectionRoom, effect, last, initial, dropPosition, index, definitions, isTesting: true,
                    pendingDeadRooms: pendingDeadRooms, positions: positions);
                if (!targets.Supported) return UnsupportedTrain(targets.UnsupportedReason!);
                Remember(effect, index, targets, ref last);
                // Runtime tests every effect, even if its initial casting test was disabled.
                string? targetError = AttackTestError(state, collectionRoom, effect);
                if (targetError != null) return UnsupportedTrain(targetError);
                CardActionEffect tested = effect;
                if (QuantityTest(effect))
                {
                    UnityRng testingRng = state.Context!.BattleRng;
                    tested = Sample(effect, ref testingRng);
                    state = WithContext(state, state.Context.WithBattleRng(testingRng));
                }
                if (!PassesTest(tested, TestCount(state, effect, targets), bossDead, state.Context, selected.Preview))
                {
                    if (index == 0 && CardTargetModel.IsRandom(effect.Target) && targets.UnitIds.Count > 0 && effect.Tests?.CancelSubsequent != true &&
                        effects.Skip(1).Any(next => next.Target.Contains("LastTargeted")))
                        return UnsupportedTrain("A skipped first random effect retains uncaptured test-stream target history.");
                    if (effect.Tests?.CancelSubsequent == true) break;
                    continue;
                }
                targets = Collect(state, collectionRoom, effect, last, initial, dropPosition, index, definitions,
                    pendingDeadRooms: pendingDeadRooms, positions: positions);
                if (!targets.Supported) return UnsupportedTrain(targets.UnsupportedReason!);
                collections.Add(new SpellTargetCollection(index, targets.UnitIds));
                Remember(effect, index, targets, ref last);
                if (targets.BattleRng.HasValue)
                    state = WithContext(state, state.Context!.WithBattleRng(targets.BattleRng.Value));
                if (effect.Type == "AddStatus" && effect.Statuses.Count > 1)
                {
                    // Native chooses one status for the whole effect, including an empty collection.
                    RngDraw chosen = state.Context!.BattleRng.Range(0, effect.Statuses.Count);
                    state = WithContext(state, state.Context.WithBattleRng(chosen.State));
                    effect = new CardActionEffect(effect.Type, effect.Target, effect.Value, effect.AllowEnemy, effect.AllowPlayer,
                        new[] { effect.Statuses[chosen.Value] }, effect.Upgrade, effect.Lifetime, effect.Tests, effect.Range, effect.Filters);
                }
                UnityRng effectRng = state.Context!.BattleRng;
                if (effect.Type != "DiscardHand" && effect.Type != "Generate") effect = Sample(effect, ref effectRng);
                state = WithContext(state, state.Context.WithBattleRng(effectRng));
                if (effect.Type == "Generate")
                {
                    CardGenerationResult generated = CardGenerationModel.Apply(state.Context!, effect.Generation!, sourceCardId);
                    if (!generated.Supported) return UnsupportedTrain(generated.UnsupportedReason!);
                    state = WithContext(state, generated.Context!);
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
                    CardCycleResult drawn = CardCycleModel.DrawCards(context.Cards, count, context.MaxHandSize, sourceCardId);
                    if (!drawn.Supported) return UnsupportedTrain(drawn.UnsupportedReason!);
                    BattleStatistics? statistics = context.Statistics;
                    foreach (CardToken card in drawn.State!.Hand.Take(drawn.State.Hand.Count - context.Cards.Hand.Count).Reverse())
                        statistics = statistics?.Increment(card.InstanceId, "TimesDrawn");
                    state = WithContext(state, context.WithCards(drawn.State).WithStatistics(statistics));
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
                // Keep this effect's collection fixed; triggers may kill a later target.
                for (int targetIndex = 0; targetIndex < targets.UnitIds.Count; targetIndex++)
                {
                    // Native status application runs backwards; damage/healing/upgrades run forwards.
                    int id = targets.UnitIds[effect.Type == "AddStatus" ? targets.UnitIds.Count - 1 - targetIndex : targetIndex];
                    RoomCombatState? targetRoom = state.Rooms.FirstOrDefault(item => item.Units.Any(unit => unit.Id == id));
                    CombatUnit? target = targetRoom?.Units.FirstOrDefault(unit => unit.Id == id);
                    if (target == null) continue;
                    if (effect.Type == "FloorRearrange" && pendingDeadRooms.Count > 0)
                        return UnsupportedTrain("Rearranging a floor with pending death positions is not modeled.");
                    int? scaledDamage = null;
                    // Native calculates trait damage and defensive status focus before its trigger
                    // queue drains the previous victim's standby return. Preserve that value once.
                    if (effect.Type == "Damage")
                    {
                        DamageScalingResult scaled = DamageScalingModel.Apply(state.Context, sourceCardId, sourceCardId, Math.Max(0, effect.Value));
                        if (!scaled.Supported) return UnsupportedTrain(scaled.UnsupportedReason!);
                        state = WithContext(state, scaled.Context!);
                        scaledDamage = Math.Max(0, scaled.Damage);
                        FocusDamageStatuses(target, targetRoom!, scaledDamage.Value); DrainDeaths();
                        // Draining a prior death updates shared statistics; the next target must read that new context.
                        targetRoom = state.Rooms.Single(room => room.RoomIndex == targetRoom!.RoomIndex);
                    }
                    RoomPlayRule? capacity = definitions?.Rooms.FirstOrDefault(item => item.RoomIndex == targetRoom!.RoomIndex);
                    RoomCombatResult applied = ApplyOne(targetRoom!, effect, target, sourceCardId,
                        capacity?.PlayerCapacity ?? playerCapacity, capacity?.EnemyCapacity ?? enemyCapacity,
                        deferSpawnerExhaustion: piles != null, scaledDamage: scaledDamage);
                    if (!applied.Supported) return UnsupportedTrain(applied.UnsupportedReason!);
                    state = ReplaceRoom(state, applied.State!);
                    if (state.Context!.OtherPiles != null) piles = state.Context.OtherPiles.ToArray();
                    events.AddRange(applied.Events);
                    if ((effect.Type == "Heal" && effect.Value >= 0 || effect.Type == "UnitUpgrade") && target.Triggers.Any(trigger => trigger.Kind == "OnHeal" &&
                        (!trigger.Once || !trigger.HasTriggered) && (!targetRoom!.Deployment || trigger.SkipDuringDeployment != true) &&
                        (trigger.IgnoreSilence || target.Statuses.All(status => status.Id != "silenced"))))
                        focusedRoom = null; // Trigger notification suppression/focus is not captured yet.
                    if (effect.Type == "Damage" && !applied.State!.Units.Any(unit => unit.Id == id))
                    {
                        pendingDeadRooms[id] = targetRoom!.RoomIndex;
                        if (piles != null && target.SpawnerCardId > 0) deferredExhaustion.Add(target.SpawnerCardId);
                    }
                    else if (piles != null && target.SpawnerCardId > 0 && !applied.State!.Units.Any(unit => unit.Id == id))
                        RouteDeadCard(target.SpawnerCardId);
                    CombatUnit? afterTarget = applied.State!.Units.FirstOrDefault(unit => unit.Id == id);
                    bool upgradeApplied = effect.Type == "UnitUpgrade" && (afterTarget == null ||
                        afterTarget.Modifiers!.Upgrades.Count > target.Modifiers!.Upgrades.Count);
                    if (effect.Type == "Heal" && effect.Value >= 0 && target.Modifiers?.CanBeHealed == true || upgradeApplied) DrainDeaths();
                    if (effect.Type == "BuffHealth" && effect.Value > 0 && target.Modifiers?.CanBeHealed == true &&
                        HealingModel.ModifiedAmount(effect.Value, target.Statuses, fromMaxHealthChange: true) >= 0) DrainDeaths();
                    if (effect.Type == "DebuffHealth" && afterTarget == null)
                    {
                        DrainDeaths();
                        focusedRoom = null; // Sacrifice/removal notification focus is not captured yet.
                    }
                    if (effect.Type == "FloorRearrange") positions = CardTargetModel.Positions(state);
                    if (applied.Outcome == RoomOutcome.BattleWon || applied.Outcome == RoomOutcome.PlayerDefeated)
                    {
                        if (outcome != RoomOutcome.Exchanged && outcome != applied.Outcome)
                            return UnsupportedTrain("Conflicting terminal results in one spell are not modeled.");
                        outcome = applied.Outcome;
                        bossDead |= outcome == RoomOutcome.BattleWon;
                    }
                }
                if (effect.Type == "AddStatus") DrainDeaths();
            }
            DrainDeaths(); // The card's final played callbacks finish the remaining death queue.
            if (routingError != null) return UnsupportedTrain(routingError);
            state = WithContext(state, (piles == null ? state.Context! : state.Context!.WithOtherPiles(piles)).AfterCardEffects());
            return new TrainSpellResult(state, outcome, events, collections: collections, otherPiles: piles);

            void RouteDeadCard(int cardId)
            {
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
            int targetId, bool fullTrain)
        {
            if (fullTrain)
            {
                string? error = TrainCombatModel.Validate(source);
                if (error != null) return error;
            }
            RoomCombatState? selected = source.Rooms.FirstOrDefault(room => room.RoomIndex == roomIndex);
            if (selected == null) return "The selected spell room does not exist.";
            if (source.Context == null) return "A spell requires shared battle context.";
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
                    bool[] outcomes = possible.Select(unit => PassesTest(effect, TestCount(source, effect,
                        CardTargetModel.Collect(source, roomIndex, effect, new[] { unit.Id }, isTesting: true)),
                        source.Context.AllScenarioBossesDead == true)).Distinct().ToArray();
                    if (outcomes.Length > 1) return "Mandatory last-target tests after random selection depend on the auxiliary test stream.";
                }
            }
            return null;
        }

        private static TrainCombatState SingleRoom(RoomCombatState room) => new TrainCombatState(new[] { room },
            Array.Empty<EnemyMovement>(), 1, room.Context);

        internal static TrainCombatState WithContext(TrainCombatState source, CombatContext context) => new TrainCombatState(
            source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment, room.Units,
                room.ExternalInteractions, context, room.Preview)).ToArray(), source.Movement, source.EnemySlotsPerRoom, context);

        private static TrainCombatState ReplaceRoom(TrainCombatState source, RoomCombatState changed)
        {
            RoomCombatState[] rooms = source.Rooms.Select(room => room.RoomIndex == changed.RoomIndex ? changed : room).ToArray();
            var alive = new HashSet<int>(rooms.SelectMany(room => room.Units).Select(unit => unit.Id));
            return WithContext(new TrainCombatState(rooms, source.Movement.Where(move => alive.Contains(move.UnitId)).ToArray(),
                source.EnemySlotsPerRoom, changed.Context), changed.Context!);
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
                string? filterError = effect.Filters?.Validate();
                if (filterError != null) return filterError;
                if (effect.Range != null && effect.Type != "DiscardHand" && effect.Type != "Generate")
                {
                    if (!new[] { "Damage", "Heal", "AddStatus", "BuffAttack", "DebuffAttack", "BuffHealth", "DebuffHealth", "Draw" }.Contains(effect.Type))
                        return "Unmodeled range consumer " + effect.Type;
                    string? rangeError = effect.Range.Validate();
                    if (rangeError != null) return rangeError;
                }
                if (effect.Type == "HandUpgrade")
                {
                    if (effect.Upgrade == null) return "Missing hand upgrade definition.";
                    if (effect.Target != "Hand" && effect.Target != "Room") return "Unmodeled hand upgrade target collection.";
                    continue;
                }
                if (!CardTargetModel.Supports(effect.Target)) return "Unimplemented spell targeting " + effect.Target;
                if (!new[] { "Damage", "Heal", "AddStatus", "FloorRearrange", "UnitUpgrade", "RemoveUnitUpgrade", "BuffAttack", "DebuffAttack", "BuffHealth", "DebuffHealth", "Draw", "DiscardHand", "Generate" }.Contains(effect.Type))
                    return "Unimplemented spell effect " + effect.Type;
                if (effect.Type == "Generate" && effect.Generation == null) return "Missing generated card rules.";
                if ((effect.Type == "UnitUpgrade" || effect.Type == "RemoveUnitUpgrade") && effect.Upgrade == null)
                    return "Missing unit upgrade definition.";
                if (effect.Value < 0 && !AttackChange(effect) && !new[] { "Damage", "Heal", "AddStatus", "BuffHealth", "DebuffHealth", "Draw", "DiscardHand", "Generate" }.Contains(effect.Type) ||
                    effect.Type == "FloorRearrange" && effect.Value > 1) return "Invalid spell effect value.";
                if (effect.Type == "BuffHealth" && effect.Lifetime != "" && effect.Lifetime != "TemporaryUntilEndOfBattle" &&
                    effect.Lifetime != "TemporaryUntilUnitDeath") return "Unmodeled maximum-health buff lifetime.";
                if (effect.Type == "FloorRearrange" && effect.Target != "DropTargetCharacter") return "Floor rearrangement requires a drop target.";
                if (effect.Type == "AddStatus" && effect.Statuses.Count == 0) return "A status effect requires at least one status definition.";
                foreach (CombatStatus status in effect.Statuses)
                {
                    var test = new CombatUnit(0, "status-validation", CombatTeam.Player, 0, 1, 1, false, false, false, new[] { status });
                    error = RoomCombatModel.Validate(new RoomCombatState(source.RoomIndex, source.Deployment,
                        new[] { test }, Array.Empty<string>(), source.Context));
                    if (error != null) return error;
                }
            }
            return null;
        }

        private static CardTargets Collect(TrainCombatState state, int roomIndex, CardActionEffect effect, IReadOnlyList<int> last,
            CombatUnit? initial, int dropPosition, int index, BattlePlayRules? definitions, bool isTesting = false,
            IReadOnlyDictionary<int, int>? pendingDeadRooms = null, IReadOnlyDictionary<int, int>? positions = null) => effect.Type == "HandUpgrade" && effect.Target == "Hand"
                ? new CardTargets(Array.Empty<int>()) : CardTargetModel.Collect(state, roomIndex, effect, last, initial?.Team, dropPosition,
                    index == 0, isTesting, definitions?.Rooms.FirstOrDefault(room => room.IsPyre)?.RoomIndex, pendingDeadRooms, positions);

        private static void Remember(CardActionEffect effect, int index, CardTargets targets, ref IReadOnlyList<int> last)
        {
            if (index == 0 || effect.Target == "DropTargetCharacter") last = targets.UnitIds;
        }

        private static int DropPosition(RoomCombatState source, CombatUnit? initial) => initial == null ? -1 :
            source.Units.Where(unit => unit.Team == initial.Team).TakeWhile(unit => unit.Id != initial.Id).Count();

        private static bool PassesTest(CardActionEffect effect, int count, bool bossDead = false, CombatContext? context = null, bool preview = false)
        {
            if (preview && (effect.Type == "Draw" || effect.Type == "DiscardHand" || effect.Type == "Generate")) return false;
            if (bossDead && !(effect.Tests?.CanPlayAfterBossDead ?? (effect.Type != "HandUpgrade" && effect.Type != "Draw" && effect.Type != "DiscardHand" && effect.Type != "Generate"))) return false;
            if (effect.Type == "Generate" && effect.Generation?.RequireHandSpace == true && context != null && context.Cards.Hand.Count >= context.MaxHandSize) return false;
            switch (effect.Type)
            {
                case "Draw": return context != null && context.Cards.Hand.Count - 1 < context.MaxHandSize;
                case "Damage": return effect.Value >= 0 && (effect.Range == null || effect.Range.Max > 0) &&
                    (effect.Target != "DropTargetCharacter" || count > 0);
                case "Heal": return effect.Target == "Room" || count > 0;
                case "AddStatus": return effect.Tests?.StrictTargets != true && effect.Target != "DropTargetCharacter" || count > 0;
                case "FloorRearrange":
                case "BuffAttack":
                case "DebuffAttack":
                case "UnitUpgrade": return count > 0;
                default: return true;
            }
        }

        private static bool AttackChange(CardActionEffect effect) => effect.Type == "BuffAttack" || effect.Type == "DebuffAttack";
        private static bool QuantityTest(CardActionEffect effect) => effect.Type == "Damage" || effect.Type == "Draw";
        private static CardActionEffect Sample(CardActionEffect effect, ref UnityRng rng)
        {
            if (effect.Range == null) return effect;
            RngDraw draw = effect.Range.Sample(rng); rng = draw.State;
            return new CardActionEffect(effect.Type, effect.Target, draw.Value, effect.AllowEnemy, effect.AllowPlayer,
                effect.Statuses, effect.Upgrade, effect.Lifetime, effect.Tests, effect.Range, effect.Filters, effect.Generation);
        }
        private static int TestCount(TrainCombatState state, CardActionEffect effect, CardTargets targets) => !AttackChange(effect) ? targets.UnitIds.Count :
            state.Rooms.SelectMany(room => room.Units).Count(unit => targets.UnitIds.Contains(unit.Id) && unit.CanAttack);
        private static string? AttackTestError(TrainCombatState state, int roomIndex, CardActionEffect effect)
        {
            if (!AttackChange(effect) || !CardTargetModel.IsRandom(effect.Target)) return null;
            CardTargets candidates = CardTargetModel.RandomCandidates(state, roomIndex, effect);
            if (!candidates.Supported) return candidates.UnsupportedReason;
            bool[] outcomes = state.Rooms.SelectMany(room => room.Units).Where(unit => candidates.UnitIds.Contains(unit.Id))
                .Select(unit => unit.CanAttack).Distinct().ToArray();
            return outcomes.Length > 1 ? "Random attack-change tests depend on the auxiliary test stream." : null;
        }

        private static RoomCombatResult ApplyOne(RoomCombatState state, CardActionEffect effect, CombatUnit target,
            int sourceCardId, int? playerCapacity, int? enemyCapacity, bool deferSpawnerExhaustion = false, int? scaledDamage = null)
        {
            if (effect.Type == "Damage") return scaledDamage.HasValue
                ? RoomCombatModel.ApplyCardDamageAfterTraits(state, target.Id, scaledDamage.Value, sourceCardId, deferSpawnerExhaustion)
                : RoomCombatModel.ApplyCardDamage(state, target.Id, Math.Max(0, effect.Value), sourceCardId, deferSpawnerExhaustion);
            if (effect.Type == "Heal") return effect.Value < 0 ? Unchanged(state) : RoomCombatModel.ApplyCardHeal(state, target.Id, effect.Value);
            if (AttackChange(effect)) return UnitAttackModel.Apply(state, target.Id, effect.Value, effect.Type == "DebuffAttack");
            if (effect.Type == "BuffHealth" || effect.Type == "DebuffHealth")
                return UnitHealthModel.Apply(state, target.Id, effect.Value, effect.Type == "DebuffHealth", effect.Lifetime);
            if (effect.Type == "UnitUpgrade" || effect.Type == "RemoveUnitUpgrade")
                return UnitModifierModel.Apply(state, target.Id, effect.Upgrade!, effect.Lifetime,
                    effect.Type == "RemoveUnitUpgrade", target.Team == CombatTeam.Player ? playerCapacity : enemyCapacity, sourceCardId);
            if (effect.Type == "AddStatus")
            {
                if (effect.Value != 0)
                {
                    // Chance is evaluated for every collected target before status immunity is checked.
                    RngDraw chance = state.Context!.BattleRng.Range(0, 100);
                    state = new RoomCombatState(state.RoomIndex, state.Deployment, state.Units, state.ExternalInteractions,
                        state.Context.WithBattleRng(chance.State), state.Preview);
                    if (chance.Value >= effect.Value) return Unchanged(state);
                }
                return StatusApplicationModel.Apply(state, target.Id, effect.Statuses[0], sourceCardId, overrideImmunity: effect.Target == "Pyre");
            }
            if (target.Statuses.Any(status => status.Id == "immobile")) return Unchanged(state);
            var team = state.Units.Where(unit => unit.Team == target.Team).ToList();
            team.Remove(target); team.Insert(effect.Value == 0 ? 0 : team.Count, target);
            CombatUnit[] enemies = target.Team == CombatTeam.Enemy ? team.ToArray() : state.Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
            CombatUnit[] players = target.Team == CombatTeam.Player ? team.ToArray() : state.Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
            return Unchanged(new RoomCombatState(state.RoomIndex, state.Deployment, enemies.Concat(players).ToArray(), state.ExternalInteractions, state.Context, state.Preview));
        }

        internal static CombatUnit Copy(CombatUnit unit, int health, IReadOnlyList<CombatStatus> statuses) =>
            new CombatUnit(unit.Id, unit.AssetKey, unit.Team, unit.BaseAttack, health, unit.MaxHealth, unit.CanAttack,
                unit.IsPyre, unit.EndsBattleOnDeath, statuses, unit.Triggers, unit.SpawnerCardId, unit.Size, unit.StatusImmunities, unit.Subtypes, unit.Modifiers, unit.IsBoss, unit.LastAttackerId, unit.StatusRegistry);
        private static RoomCombatResult Unchanged(RoomCombatState state) => new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static TrainSpellResult UnsupportedTrain(string reason) => new TrainSpellResult(null, RoomOutcome.Unsupported,
            Array.Empty<CombatEvent>(), reason);
    }
}
