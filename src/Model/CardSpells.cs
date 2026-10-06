using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardSpellModel
    {
        public static bool RequiresUnitTarget(IReadOnlyList<CardActionEffect> effects) =>
            effects.Any(effect => effect.Type != "HandUpgrade" && effect.Target == "DropTargetCharacter");
        public static RoomCombatResult Apply(RoomCombatState source, IReadOnlyList<CardActionEffect> effects, int targetId, int sourceCardId = 0,
            int? playerCapacity = null, int? enemyCapacity = null, BattlePlayRules? definitions = null)
        {
            string? error = RoomCombatModel.Validate(source);
            if (error != null) return Unsupported(error);
            if (source.Context == null || effects.Count == 0)
                return Unsupported("A spell requires context and effects.");
            CombatUnit? initial = source.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (RequiresUnitTarget(effects) && initial == null) return Unsupported("The spell target is missing.");
            if (!RequiresUnitTarget(effects) && effects.Any(effect => effect.Type != "HandUpgrade"))
                return Unsupported("A unit effect requires an initial drop target.");
            if (effects[0].Target == "Room" && effects.Any(effect => effect.Type != "HandUpgrade" && effect.Target == "LastTargetedCharacters"))
                return Unsupported("Room-wide last targets are not implemented.");
            foreach (CardActionEffect effect in effects)
            {
                if (effect.Type == "HandUpgrade")
                {
                    if (effect.Upgrade == null) return Unsupported("Missing hand upgrade definition.");
                    if (effect.Target != "Hand" && effect.Target != "Room") return Unsupported("Unmodeled hand upgrade target collection.");
                    continue;
                }
                if (effect.Target != "DropTargetCharacter" && effect.Target != "LastTargetedCharacters")
                    return Unsupported("Unimplemented spell targeting " + effect.Target);
                if (effect.Type != "Damage" && effect.Type != "Heal" && effect.Type != "AddStatus" && effect.Type != "FloorRearrange" &&
                    effect.Type != "UnitUpgrade" && effect.Type != "RemoveUnitUpgrade")
                    return Unsupported("Unimplemented spell effect " + effect.Type);
                if ((effect.Type == "UnitUpgrade" || effect.Type == "RemoveUnitUpgrade") && effect.Upgrade == null)
                    return Unsupported("Missing unit upgrade definition.");
                if (effect.Value < 0 || effect.Type == "FloorRearrange" && effect.Value > 1)
                    return Unsupported("Invalid spell effect value.");
                if (effect.Type == "AddStatus" && effect.Statuses.Count != 1)
                    return Unsupported("Random/multiple status selection is not implemented.");
                foreach (CombatStatus status in effect.Statuses)
                {
                    var test = Copy(initial!, initial!.Health, initial.Statuses.Where(item => item.Id != status.Id).Concat(new[] { status }).ToArray());
                    error = RoomCombatModel.Validate(new RoomCombatState(source.RoomIndex, source.Deployment,
                        new[] { test }, Array.Empty<string>(), source.Context));
                    if (error != null) return Unsupported(error);
                }
            }
            CardActionEffect? first = effects.FirstOrDefault(effect => effect.Type != "HandUpgrade");
            if (first != null && (initial!.Team == CombatTeam.Enemy && !first.AllowEnemy || initial!.Team == CombatTeam.Player && !first.AllowPlayer))
                return Unsupported("The target team is excluded by the spell definition.");
            RoomCombatState state = source;
            int lastTargetId = 0;
            int dropPosition = initial == null ? -1 : source.Units.Where(unit => unit.Team == initial.Team)
                .TakeWhile(unit => unit.Id != initial.Id).Count();
            for (int index = 0; index < effects.Count; index++)
            {
                CardActionEffect effect = effects[index];
                // Native GetParamInt reads the card's current modifiers when each effect executes.
                if (definitions != null && sourceCardId > 0 && state.Context!.CardInstances != null)
                {
                    CardInstanceState? card = state.Context.CardInstances.FirstOrDefault(item => item.InstanceId == sourceCardId);
                    CardPlayRule? rule = definitions.Cards.FirstOrDefault(item => item.DataId == card?.DataId);
                    if (card == null || rule == null || rule.Effects.Count != effects.Count)
                        return Unsupported("Missing live spell effect definition.");
                    effect = CardModifierModel.Resolve(rule, card).Effects[index];
                }
                if (effect.Type == "HandUpgrade")
                {
                    RoomCombatResult upgraded = HandUpgradeModel.Apply(state, effect.Upgrade!, effect.Lifetime, definitions);
                    if (!upgraded.Supported) return upgraded;
                    state = upgraded.State!;
                    continue;
                }
                // A drop target refers to the chosen spawn point. Rearrangement can change its occupant.
                CombatUnit? target = effect.Target == "DropTargetCharacter" ? state.Units.Where(unit => unit.Team == initial!.Team)
                    .ElementAtOrDefault(dropPosition) : state.Units.FirstOrDefault(unit => unit.Id == lastTargetId);
                if (effect.Target == "DropTargetCharacter") lastTargetId = target?.Id ?? 0;
                // LastTargetedCharacters drops killed units instead of selecting another front unit.
                if (target == null || target.Team == CombatTeam.Enemy && !effect.AllowEnemy ||
                    target.Team == CombatTeam.Player && !effect.AllowPlayer)
                {
                    if (effect.Target == "DropTargetCharacter") lastTargetId = 0;
                    continue;
                }
                if (effect.Type == "Damage")
                {
                    RoomCombatResult damage = RoomCombatModel.ApplyCardDamage(state, target.Id, effect.Value, sourceCardId);
                    if (!damage.Supported || damage.Outcome == RoomOutcome.BattleWon || damage.Outcome == RoomOutcome.PlayerDefeated) return damage;
                    state = damage.State!;
                }
                else if (effect.Type == "Heal")
                {
                    RoomCombatResult healed = RoomCombatModel.ApplyCardHeal(state, target.Id, effect.Value);
                    if (!healed.Supported) return healed;
                    state = healed.State!;
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
                        state.Units.Select(unit => unit.Id == target.Id ? modified : unit).ToArray(), state.ExternalInteractions, context, state.Preview);
                }
                else if (effect.Type == "UnitUpgrade" || effect.Type == "RemoveUnitUpgrade")
                {
                    RoomCombatResult upgraded = UnitModifierModel.Apply(state, target.Id, effect.Upgrade!, effect.Lifetime,
                        effect.Type == "RemoveUnitUpgrade", target.Team == CombatTeam.Player ? playerCapacity : enemyCapacity);
                    if (!upgraded.Supported || upgraded.Outcome == RoomOutcome.BattleWon || upgraded.Outcome == RoomOutcome.PlayerDefeated) return upgraded;
                    state = upgraded.State!;
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
