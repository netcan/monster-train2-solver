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
        internal SpellCastCheck(bool canPlay, string? error = null)
        { CanPlay = canPlay; UnsupportedReason = error; }
    }

    public sealed class TrainSpellResult
    {
        public TrainCombatState? State { get; }
        public RoomOutcome Outcome { get; }
        public IReadOnlyList<CombatEvent> Events { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        internal TrainSpellResult(TrainCombatState? state, RoomOutcome outcome, IReadOnlyList<CombatEvent> events, string? error = null)
        { State = state; Outcome = outcome; Events = Array.AsReadOnly(events.ToArray()); UnsupportedReason = error; }
    }

    public static class CardSpellModel
    {
        public static bool RequiresUnitTarget(IReadOnlyList<CardActionEffect> effects) =>
            effects.Any(effect => effect.Type != "HandUpgrade" && effect.Target == "DropTargetCharacter");

        // Native casting tests all requested effects against the unchanged pre-cast state.
        // Any success permits the cast, unless an explicitly mandatory effect fails.
        public static SpellCastCheck TestPlay(RoomCombatState source, IReadOnlyList<CardActionEffect> effects, int targetId)
            => TestPlayCore(SingleRoom(source), source.RoomIndex, effects, targetId, fullTrain: false);

        public static SpellCastCheck TestPlay(TrainCombatState source, int roomIndex, IReadOnlyList<CardActionEffect> effects, int targetId)
            => TestPlayCore(source, roomIndex, effects, targetId, fullTrain: true);

        private static SpellCastCheck TestPlayCore(TrainCombatState source, int roomIndex, IReadOnlyList<CardActionEffect> effects,
            int targetId, bool fullTrain)
        {
            string? error = Validate(source, roomIndex, effects, targetId, fullTrain);
            if (error != null) return new SpellCastCheck(false, error);
            RoomCombatState room = source.Rooms.Single(item => item.RoomIndex == roomIndex);
            CombatUnit? initial = room.Units.FirstOrDefault(unit => unit.Id == targetId);
            int dropPosition = DropPosition(room, initial);
            IReadOnlyList<int> last = Array.Empty<int>();
            bool passed = false;
            for (int index = 0; index < effects.Count; index++)
            {
                CardActionEffect effect = effects[index];
                if (effect.Tests?.ShouldTest == false) continue;
                CardTargets targets = Collect(source, roomIndex, effect, last, initial, dropPosition, index, isTesting: true);
                if (!targets.Supported) return new SpellCastCheck(false, targets.UnsupportedReason);
                Remember(effect, index, targets, ref last);
                bool valid = PassesTest(effect, targets.UnitIds.Count, source.Context!.AllScenarioBossesDead == true);
                if (!valid && effect.Tests?.FailToCast == true) return new SpellCastCheck(false);
                passed |= valid;
            }
            return new SpellCastCheck(passed);
        }

        public static RoomCombatResult Apply(RoomCombatState source, IReadOnlyList<CardActionEffect> effects, int targetId, int sourceCardId = 0,
            int? playerCapacity = null, int? enemyCapacity = null, BattlePlayRules? definitions = null)
        {
            TrainSpellResult result = ApplyCore(SingleRoom(source), source.RoomIndex, effects, targetId, sourceCardId,
                playerCapacity, enemyCapacity, definitions, fullTrain: false);
            return new RoomCombatResult(result.State?.Rooms.Single(), result.Outcome, 0, result.Events.ToList(), result.UnsupportedReason);
        }

        public static TrainSpellResult Apply(TrainCombatState source, int roomIndex, IReadOnlyList<CardActionEffect> effects,
            int targetId, int sourceCardId = 0, BattlePlayRules? definitions = null)
            => ApplyCore(source, roomIndex, effects, targetId, sourceCardId, null, null, definitions, fullTrain: true);

        private static TrainSpellResult ApplyCore(TrainCombatState source, int roomIndex, IReadOnlyList<CardActionEffect> effects,
            int targetId, int sourceCardId, int? playerCapacity, int? enemyCapacity, BattlePlayRules? definitions, bool fullTrain)
        {
            string? error = Validate(source, roomIndex, effects, targetId, fullTrain);
            if (error != null) return UnsupportedTrain(error);
            RoomCombatState selected = source.Rooms.Single(item => item.RoomIndex == roomIndex);
            CombatUnit? initial = selected.Units.FirstOrDefault(unit => unit.Id == targetId);
            int dropPosition = DropPosition(selected, initial);
            TrainCombatState state = WithContext(source, source.Context!);
            IReadOnlyList<int> last = Array.Empty<int>();
            var events = new List<CombatEvent>();
            RoomOutcome outcome = RoomOutcome.Exchanged;
            bool bossDead = source.Context!.AllScenarioBossesDead == true;
            CardInstanceState? resolvingCard = source.Context.FindCard(sourceCardId);
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
                CardTargets targets = Collect(state, roomIndex, effect, last, initial, dropPosition, index, isTesting: true);
                if (!targets.Supported) return UnsupportedTrain(targets.UnsupportedReason!);
                Remember(effect, index, targets, ref last);
                // Runtime tests every effect, even if its initial casting test was disabled.
                if (!PassesTest(effect, targets.UnitIds.Count, bossDead))
                {
                    if (index == 0 && effect.Target == "RandomInRoom" && targets.UnitIds.Count > 0 && effect.Tests?.CancelSubsequent != true &&
                        effects.Skip(1).Any(next => next.Target.Contains("LastTargeted")))
                        return UnsupportedTrain("A skipped first random effect retains uncaptured test-stream target history.");
                    if (effect.Tests?.CancelSubsequent == true) break;
                    continue;
                }
                targets = Collect(state, roomIndex, effect, last, initial, dropPosition, index);
                if (!targets.Supported) return UnsupportedTrain(targets.UnsupportedReason!);
                Remember(effect, index, targets, ref last);
                if (targets.BattleRng.HasValue)
                    state = WithContext(state, state.Context!.WithBattleRng(targets.BattleRng.Value));
                if (effect.Type == "AddStatus" && effect.Statuses.Count > 1)
                {
                    // Native chooses one status for the whole effect, including an empty collection.
                    RngDraw chosen = state.Context!.BattleRng.Range(0, effect.Statuses.Count);
                    state = WithContext(state, state.Context.WithBattleRng(chosen.State));
                    effect = new CardActionEffect(effect.Type, effect.Target, effect.Value, effect.AllowEnemy, effect.AllowPlayer,
                        new[] { effect.Statuses[chosen.Value] }, effect.Upgrade, effect.Lifetime, effect.Tests);
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
                    RoomPlayRule? capacity = definitions?.Rooms.FirstOrDefault(item => item.RoomIndex == targetRoom!.RoomIndex);
                    RoomCombatResult applied = ApplyOne(targetRoom!, effect, target, sourceCardId,
                        capacity?.PlayerCapacity ?? playerCapacity, capacity?.EnemyCapacity ?? enemyCapacity);
                    if (!applied.Supported) return UnsupportedTrain(applied.UnsupportedReason!);
                    state = ReplaceRoom(state, applied.State!);
                    events.AddRange(applied.Events);
                    if (applied.Outcome == RoomOutcome.BattleWon || applied.Outcome == RoomOutcome.PlayerDefeated)
                    {
                        if (outcome != RoomOutcome.Exchanged && outcome != applied.Outcome)
                            return UnsupportedTrain("Conflicting terminal results in one spell are not modeled.");
                        outcome = applied.Outcome;
                        bossDead |= outcome == RoomOutcome.BattleWon;
                    }
                }
            }
            return new TrainSpellResult(state, outcome, events);
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
            return Validate(selected, effects, targetId);
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
            if (effects[0].Target == "RandomInRoom" && effects[0].AllowEnemy && effects[0].AllowPlayer && effects.Skip(1).Any(effect =>
                effect.Tests?.ShouldTest != false && effect.Tests?.FailToCast == true && effect.Target == "LastTargetedCharacters" &&
                (effects[0].AllowEnemy && !effect.AllowEnemy || effects[0].AllowPlayer && !effect.AllowPlayer)))
                return "Mandatory last-target team tests after random selection depend on the auxiliary test stream.";
            foreach (CardActionEffect effect in effects)
            {
                if (effect.Type == "HandUpgrade")
                {
                    if (effect.Upgrade == null) return "Missing hand upgrade definition.";
                    if (effect.Target != "Hand" && effect.Target != "Room") return "Unmodeled hand upgrade target collection.";
                    continue;
                }
                if (!CardTargetModel.Supports(effect.Target)) return "Unimplemented spell targeting " + effect.Target;
                if (!new[] { "Damage", "Heal", "AddStatus", "FloorRearrange", "UnitUpgrade", "RemoveUnitUpgrade" }.Contains(effect.Type))
                    return "Unimplemented spell effect " + effect.Type;
                if ((effect.Type == "UnitUpgrade" || effect.Type == "RemoveUnitUpgrade") && effect.Upgrade == null)
                    return "Missing unit upgrade definition.";
                if (effect.Value < 0 || effect.Type == "FloorRearrange" && effect.Value > 1) return "Invalid spell effect value.";
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
            CombatUnit? initial, int dropPosition, int index, bool isTesting = false) => effect.Type == "HandUpgrade" && effect.Target == "Hand"
                ? new CardTargets(Array.Empty<int>()) : CardTargetModel.Collect(state, roomIndex, effect, last, initial?.Team, dropPosition, index == 0, isTesting);

        private static void Remember(CardActionEffect effect, int index, CardTargets targets, ref IReadOnlyList<int> last)
        {
            if (index == 0 || effect.Target == "DropTargetCharacter") last = targets.UnitIds;
        }

        private static int DropPosition(RoomCombatState source, CombatUnit? initial) => initial == null ? -1 :
            source.Units.Where(unit => unit.Team == initial.Team).TakeWhile(unit => unit.Id != initial.Id).Count();

        private static bool PassesTest(CardActionEffect effect, int count, bool bossDead = false)
        {
            if (bossDead && !(effect.Tests?.CanPlayAfterBossDead ?? effect.Type != "HandUpgrade")) return false;
            switch (effect.Type)
            {
                case "Damage": return effect.Value >= 0 && (effect.Target != "DropTargetCharacter" || count > 0);
                case "Heal": return effect.Target == "Room" || count > 0;
                case "AddStatus": return effect.Tests?.StrictTargets != true && effect.Target != "DropTargetCharacter" || count > 0;
                case "FloorRearrange":
                case "UnitUpgrade": return count > 0;
                default: return true;
            }
        }

        private static RoomCombatResult ApplyOne(RoomCombatState state, CardActionEffect effect, CombatUnit target,
            int sourceCardId, int? playerCapacity, int? enemyCapacity)
        {
            if (effect.Type == "Damage") return RoomCombatModel.ApplyCardDamage(state, target.Id, effect.Value, sourceCardId);
            if (effect.Type == "Heal") return RoomCombatModel.ApplyCardHeal(state, target.Id, effect.Value);
            if (effect.Type == "UnitUpgrade" || effect.Type == "RemoveUnitUpgrade")
                return UnitModifierModel.Apply(state, target.Id, effect.Upgrade!, effect.Lifetime,
                    effect.Type == "RemoveUnitUpgrade", target.Team == CombatTeam.Player ? playerCapacity : enemyCapacity);
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
                CombatStatus added = effect.Statuses[0];
                if (target.StatusImmunities.Contains(added.Id) || target.Statuses.Any(status => status.Id == "immune")) return Unchanged(state);
                CombatStatus? existing = target.Statuses.FirstOrDefault(status => status.Id == added.Id);
                int count = Math.Min(9999, (existing?.Stacks ?? 0) + added.Stacks);
                CombatContext context = state.Context!;
                if (context.Statistics != null)
                    context = context.WithStatistics(context.LiveStatistics!.Increment(sourceCardId, "AnyStatusEffectStacksAdded",
                        count - (existing?.Stacks ?? 0), requireTrackedCard: context.CardInstances?.Count == 0));
                CombatUnit modified = Copy(target, target.Health, target.Statuses.Where(status => status.Id != added.Id)
                    .Concat(new[] { (existing ?? added).WithStacks(count) }).ToArray());
                return Unchanged(new RoomCombatState(state.RoomIndex, state.Deployment,
                    state.Units.Select(unit => unit.Id == target.Id ? modified : unit).ToArray(), state.ExternalInteractions, context, state.Preview));
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
                unit.IsPyre, unit.EndsBattleOnDeath, statuses, unit.Triggers, unit.SpawnerCardId, unit.Size, unit.StatusImmunities, unit.Subtypes, unit.Modifiers);
        private static RoomCombatResult Unchanged(RoomCombatState state) => new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static TrainSpellResult UnsupportedTrain(string reason) => new TrainSpellResult(null, RoomOutcome.Unsupported,
            Array.Empty<CombatEvent>(), reason);
    }
}
