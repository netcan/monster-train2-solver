using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardSpellModel
    {
        public static RoomCombatResult Apply(RoomCombatState source, IReadOnlyList<CardActionEffect> effects, int targetId, int sourceCardId = 0,
            int? playerCapacity = null, int? enemyCapacity = null)
        {
            string? error = RoomCombatModel.Validate(source);
            if (error != null) return Unsupported(error);
            if (source.Context == null || effects.Count == 0 || effects[0].Target != "DropTargetCharacter")
                return Unsupported("A targeted spell requires context and an initial drop target.");
            CombatUnit? initial = source.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (initial == null) return Unsupported("The spell target is missing.");
            foreach (CardActionEffect effect in effects)
            {
                if (effect.Target != "DropTargetCharacter" && effect.Target != "LastTargetedCharacters")
                    return Unsupported("Unimplemented spell targeting " + effect.Target);
                if (effect.Type != "Damage" && effect.Type != "AddStatus" && effect.Type != "FloorRearrange")
                    return Unsupported("Unimplemented spell effect " + effect.Type);
                if (effect.Value < 0 || effect.Type == "FloorRearrange" && effect.Value > 1)
                    return Unsupported("Invalid spell effect value.");
                if (effect.Type == "AddStatus" && effect.Statuses.Count != 1)
                    return Unsupported("Random/multiple status selection is not implemented.");
                foreach (CombatStatus status in effect.Statuses)
                {
                    var test = Copy(initial, initial.Health, initial.Statuses.Where(item => item.Id != status.Id).Concat(new[] { status }).ToArray());
                    error = RoomCombatModel.Validate(new RoomCombatState(source.RoomIndex, source.Deployment,
                        new[] { test }, Array.Empty<string>(), source.Context));
                    if (error != null) return Unsupported(error);
                }
            }
            CardActionEffect first = effects[0];
            if (initial.Team == CombatTeam.Enemy && !first.AllowEnemy || initial.Team == CombatTeam.Player && !first.AllowPlayer)
                return Unsupported("The target team is excluded by the spell definition.");
            RoomCombatState state = source;
            foreach (CardActionEffect effect in effects)
            {
                CombatUnit? target = state.Units.FirstOrDefault(unit => unit.Id == targetId);
                // LastTargetedCharacters drops killed units instead of selecting another front unit.
                if (target == null || target.Team == CombatTeam.Enemy && !effect.AllowEnemy ||
                    target.Team == CombatTeam.Player && !effect.AllowPlayer) continue;
                if (effect.Type == "Damage")
                {
                    RoomCombatResult damage = RoomCombatModel.ApplyCardDamage(state, targetId, effect.Value, sourceCardId);
                    if (!damage.Supported || damage.Outcome == RoomOutcome.BattleWon || damage.Outcome == RoomOutcome.PlayerDefeated) return damage;
                    state = damage.State!;
                }
                else if (effect.Type == "AddStatus")
                {
                    CombatStatus added = effect.Statuses[0];
                    if (target.StatusImmunities.Contains(added.Id) || target.Statuses.Any(status => status.Id == "immune")) continue;
                    CombatStatus? existing = target.Statuses.FirstOrDefault(status => status.Id == added.Id);
                    int count = Math.Min(9999, (existing?.Stacks ?? 0) + added.Stacks);
                    CombatContext context = state.Context!;
                    if (context.Statistics != null)
                        context = context.WithStatistics(context.Statistics.Increment(sourceCardId, "AnyStatusEffectStacksAdded", count - (existing?.Stacks ?? 0)));
                    CombatUnit modified = Copy(target, target.Health, target.Statuses.Where(status => status.Id != added.Id)
                        .Concat(new[] { (existing ?? added).WithStacks(count) }).ToArray());
                    state = new RoomCombatState(state.RoomIndex, state.Deployment,
                        state.Units.Select(unit => unit.Id == targetId ? modified : unit).ToArray(), state.ExternalInteractions, context, state.Preview);
                }
                else if (!target.Statuses.Any(status => status.Id == "immobile"))
                {
                    var team = state.Units.Where(unit => unit.Team == target.Team).ToList();
                    team.Remove(target); team.Insert(effect.Value == 0 ? 0 : team.Count, target);
                    CombatUnit[] enemies = target.Team == CombatTeam.Enemy ? team.ToArray() : state.Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
                    CombatUnit[] players = target.Team == CombatTeam.Player ? team.ToArray() : state.Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
                    state = new RoomCombatState(state.RoomIndex, state.Deployment, enemies.Concat(players).ToArray(), state.ExternalInteractions, state.Context, state.Preview);
                }
            }
            return new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        }

        internal static CombatUnit Copy(CombatUnit unit, int health, IReadOnlyList<CombatStatus> statuses) =>
            new CombatUnit(unit.Id, unit.AssetKey, unit.Team, unit.BaseAttack, health, unit.MaxHealth, unit.CanAttack,
                unit.IsPyre, unit.EndsBattleOnDeath, statuses, unit.Triggers, unit.SpawnerCardId, unit.Size, unit.StatusImmunities, unit.Subtypes, unit.Modifiers);
        private static RoomCombatResult Unsupported(string reason) =>
            new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), reason);
    }
}
