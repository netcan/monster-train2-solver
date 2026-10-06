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

    public static class CardSpellModel
    {
        public static bool RequiresUnitTarget(IReadOnlyList<CardActionEffect> effects) =>
            effects.Any(effect => effect.Type != "HandUpgrade" && effect.Target == "DropTargetCharacter");

        // Native casting tests all requested effects against the unchanged pre-cast state.
        // Any success permits the cast, unless an explicitly mandatory effect fails.
        public static SpellCastCheck TestPlay(RoomCombatState source, IReadOnlyList<CardActionEffect> effects, int targetId)
        {
            string? error = Validate(source, effects, targetId);
            if (error != null) return new SpellCastCheck(false, error);
            CombatUnit? initial = source.Units.FirstOrDefault(unit => unit.Id == targetId);
            int dropPosition = DropPosition(source, initial);
            IReadOnlyList<int> last = Array.Empty<int>();
            bool passed = false;
            for (int index = 0; index < effects.Count; index++)
            {
                CardActionEffect effect = effects[index];
                if (effect.Tests?.ShouldTest == false) continue;
                CardTargets targets = Collect(source, effect, last, initial, dropPosition, index);
                if (!targets.Supported) return new SpellCastCheck(false, targets.UnsupportedReason);
                Remember(effect, index, targets, ref last);
                bool valid = PassesTest(effect, targets.UnitIds.Count);
                if (!valid && effect.Tests?.FailToCast == true) return new SpellCastCheck(false);
                passed |= valid;
            }
            return new SpellCastCheck(passed);
        }

        public static RoomCombatResult Apply(RoomCombatState source, IReadOnlyList<CardActionEffect> effects, int targetId, int sourceCardId = 0,
            int? playerCapacity = null, int? enemyCapacity = null, BattlePlayRules? definitions = null)
        {
            string? error = Validate(source, effects, targetId);
            if (error != null) return Unsupported(error);
            CombatUnit? initial = source.Units.FirstOrDefault(unit => unit.Id == targetId);
            int dropPosition = DropPosition(source, initial);
            RoomCombatState state = source;
            IReadOnlyList<int> last = Array.Empty<int>();
            var events = new List<CombatEvent>();
            for (int index = 0; index < effects.Count; index++)
            {
                CardActionEffect effect = effects[index];
                // Each effect reads the card's live modifiers, including earlier hand upgrades.
                if (definitions != null && sourceCardId > 0 && state.Context!.CardInstances != null)
                {
                    CardInstanceState? card = state.Context.CardInstances.FirstOrDefault(item => item.InstanceId == sourceCardId);
                    CardPlayRule? rule = definitions.Cards.FirstOrDefault(item => item.DataId == card?.DataId);
                    if (card == null || rule == null || rule.Effects.Count != effects.Count)
                        return Unsupported("Missing live spell effect definition.");
                    effect = CardModifierModel.Resolve(rule, card).Effects[index];
                }
                CardTargets targets = Collect(state, effect, last, initial, dropPosition, index);
                if (!targets.Supported) return Unsupported(targets.UnsupportedReason!);
                Remember(effect, index, targets, ref last);
                // Runtime tests every effect, even if its initial casting test was disabled.
                if (!PassesTest(effect, targets.UnitIds.Count))
                {
                    if (effect.Tests?.CancelSubsequent == true) break;
                    continue;
                }
                if (effect.Type == "HandUpgrade")
                {
                    RoomCombatResult upgraded = HandUpgradeModel.Apply(state, effect.Upgrade!, effect.Lifetime, definitions);
                    if (!upgraded.Supported) return upgraded;
                    state = upgraded.State!;
                    events.AddRange(upgraded.Events);
                    continue;
                }
                // Keep this effect's collection fixed; triggers may kill a later target.
                for (int targetIndex = 0; targetIndex < targets.UnitIds.Count; targetIndex++)
                {
                    int id = targets.UnitIds[targetIndex];
                    CombatUnit? target = state.Units.FirstOrDefault(unit => unit.Id == id);
                    if (target == null) continue;
                    RoomCombatResult applied = ApplyOne(state, effect, target, sourceCardId, playerCapacity, enemyCapacity);
                    if (!applied.Supported) return applied;
                    state = applied.State!;
                    events.AddRange(applied.Events);
                    if (applied.Outcome == RoomOutcome.BattleWon || applied.Outcome == RoomOutcome.PlayerDefeated)
                    {
                        // The native StopCombat entry can be sampled while a spell is resolving.
                        // Settled post-kill continuation is not yet modeled; reject partial states.
                        if (targets.UnitIds.Skip(targetIndex + 1).Any(next => state.Units.Any(unit => unit.Id == next)))
                            return Unsupported("Terminal group damage with remaining live targets is not implemented.");
                        error = TerminalTailError(state, effects, index + 1, last, initial, dropPosition);
                        if (error != null) return Unsupported(error);
                        return new RoomCombatResult(state, applied.Outcome, 0, events);
                    }
                }
            }
            return new RoomCombatResult(state, RoomOutcome.Exchanged, 0, events);
        }

        private static string? Validate(RoomCombatState source, IReadOnlyList<CardActionEffect> effects, int targetId)
        {
            string? error = RoomCombatModel.Validate(source);
            if (error != null) return error;
            if (source.Context == null || effects.Count == 0) return "A spell requires context and effects.";
            if (RequiresUnitTarget(effects) && !source.Units.Any(unit => unit.Id == targetId)) return "The spell target is missing.";
            if (effects[0].Tests?.ShouldTest == false && effects.Skip(1).Any(effect => effect.Target.Contains("LastTargeted")))
                return "Casting with an untested first effect requires uncaptured target history.";
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
                if (effect.Type == "AddStatus" && effect.Statuses.Count != 1) return "Random/multiple status selection is not implemented.";
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

        private static CardTargets Collect(RoomCombatState state, CardActionEffect effect, IReadOnlyList<int> last,
            CombatUnit? initial, int dropPosition, int index) => effect.Type == "HandUpgrade" && effect.Target == "Hand"
                ? new CardTargets(Array.Empty<int>()) : CardTargetModel.Collect(state, effect, last, initial?.Team, dropPosition, index == 0);

        private static void Remember(CardActionEffect effect, int index, CardTargets targets, ref IReadOnlyList<int> last)
        {
            if (index == 0 || effect.Target == "DropTargetCharacter") last = targets.UnitIds;
        }

        private static int DropPosition(RoomCombatState source, CombatUnit? initial) => initial == null ? -1 :
            source.Units.Where(unit => unit.Team == initial.Team).TakeWhile(unit => unit.Id != initial.Id).Count();

        private static string? TerminalTailError(RoomCombatState state, IReadOnlyList<CardActionEffect> effects, int start,
            IReadOnlyList<int> last, CombatUnit? initial, int dropPosition)
        {
            for (int index = start; index < effects.Count; index++)
            {
                CardActionEffect effect = effects[index];
                CardTargets targets = Collect(state, effect, last, initial, dropPosition, index);
                if (!targets.Supported) return targets.UnsupportedReason;
                Remember(effect, index, targets, ref last);
                if (!PassesTest(effect, targets.UnitIds.Count))
                {
                    if (effect.Tests?.CancelSubsequent == true) break;
                    continue;
                }
                if (effect.Type == "HandUpgrade" || targets.UnitIds.Any(id => state.Units.Any(unit => unit.Id == id)))
                    return "Terminal spell continuation with remaining live effects is not implemented.";
            }
            return null;
        }

        private static bool PassesTest(CardActionEffect effect, int count)
        {
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
                CombatStatus added = effect.Statuses[0];
                if (target.StatusImmunities.Contains(added.Id) || target.Statuses.Any(status => status.Id == "immune")) return Unchanged(state);
                CombatStatus? existing = target.Statuses.FirstOrDefault(status => status.Id == added.Id);
                int count = Math.Min(9999, (existing?.Stacks ?? 0) + added.Stacks);
                CombatContext context = state.Context!;
                if (context.Statistics != null)
                    context = context.WithStatistics(context.Statistics.Increment(sourceCardId, "AnyStatusEffectStacksAdded", count - (existing?.Stacks ?? 0)));
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
        private static RoomCombatResult Unsupported(string reason) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), reason);
    }
}
