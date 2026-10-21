using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // A candidate seed, scored and replayed by the same bounded planner as any
    // other branch. Uses captured definitions and visible units, not card names.
    public static class BattleHeuristicPolicy
    {
        public static SupportedActionCandidate? Choose(BattleTurnState source,
            out IReadOnlyList<UnsupportedActionAttempt> unsupported, int preferredDeploymentRoom = 2)
        {
            var omitted = new List<UnsupportedActionAttempt>();
            unsupported = omitted;
            CombatContext? context = source.Spawn.Train.Context;
            if (context == null || source.PlayRules == null) return null;
            var choices = context.Cards.Hand.Select(card =>
            {
                CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(rule => rule.DataId == card.DataId);
                CardInstanceState? instance = context.FindCard(card.InstanceId);
                if (rule != null && instance != null) rule = CardModifierModel.Resolve(rule, instance);
                return (card, rule);
            }).Where(choice => choice.rule != null).OrderByDescending(choice => Priority(choice.rule!)).ToArray();
            bool checkedAbilities = false;
            foreach (var choice in choices)
            {
                if (!checkedAbilities && choice.rule!.Effect != "SpawnMonster")
                {
                    checkedAbilities = true;
                    SupportedActionCandidate? ability = Ability();
                    if (ability != null) return ability;
                }
                // Rank visible targets before applying effects. This seed only
                // needs its first legal successor; evaluating every placement
                // first consumed most of an interactive planning budget.
                foreach (PlayCardAction action in Targets(choice.card, choice.rule!)
                    .Where(action => TargetPriority(action, choice.rule!) > -1e5)
                    .OrderByDescending(action => TargetPriority(action, choice.rule!)))
                {
                    BattleActionResult result = BattleActionModel.PlayCard(source, action);
                    if (result.Supported) return new SupportedActionCandidate(action, result);
                    if (result.Rejection == ActionRejection.Unsupported)
                        omitted.Add(new UnsupportedActionAttempt(action, result.Reason ?? "Unsupported strength-seed action."));
                }
                // Multi-stage choices use the ordinary complete branch
                // enumerator; the simple target seed never invents choices.
                if (choice.rule!.Effects.Any(effect => effect.Recursion != null || effect.SpikeRecipe != null))
                {
                    BattleActionEnumeration actions = BattleActionModel.EnumerateActions(source, choice.card.InstanceId, includeAbilities: false);
                    omitted.AddRange(actions.UnsupportedAttempts);
                    var selected = actions.SupportedCandidates.Where(candidate => TargetPriority(candidate.Action, choice.rule!) > -1e5)
                        .OrderByDescending(candidate => TargetPriority(candidate.Action, choice.rule!)).FirstOrDefault();
                    if (selected != null) return selected;
                }
            }
            return checkedAbilities ? null : Ability();

            IEnumerable<PlayCardAction> Targets(CardToken card, CardPlayRule rule)
            {
                bool targeted = CardSpellModel.RequiresUnitTarget(rule.Effects.Concat(rule.OnCastEffects).ToArray());
                foreach (RoomCombatState room in source.Spawn.Train.Rooms)
                {
                    int players = room.Units.Count(unit => unit.Team == CombatTeam.Player);
                    int first = rule.Effect == "SpawnMonster" ? 0 : -1;
                    int last = rule.Effect == "SpawnMonster" ? players : -1;
                    foreach (int target in targeted ? room.Units.Where(unit => unit.Health > 0).Select(unit => unit.Id) : new[] { 0 })
                    for (int position = first; position <= last; position++)
                        yield return new PlayCardAction(card.InstanceId, room.RoomIndex, position, target);
                }
            }
            SupportedActionCandidate? Ability()
            {
                BattleActionEnumeration abilities = UnitAbilityModel.EnumerateActivations(source);
                omitted.AddRange(abilities.UnsupportedAttempts);
                return abilities.SupportedCandidates.FirstOrDefault();
            }
            double Priority(CardPlayRule rule)
            {
                if (rule.CardType == "Spell" && rule.Cost == 0 && rule.CostType != "ConsumeRemainingEnergy" &&
                    rule.Effects.Any(effect => effect.Type is "Draw" or "DrawType")) return 1700;
                if (rule.SpawnUnit is CombatUnit unit)
                    return 1000 + (unit.Modifiers?.IsChampion == true ? 500 : 0) + unit.Attack * 2 + unit.Health;
                if (rule.CostType == "ConsumeRemainingEnergy" && rule.Effects.Any(effect =>
                    effect.AllowPlayer && effect.Statuses.Any(status => status.DisplayCategory == "Positive"))) return 900;
                return 100;
            }
            double TargetPriority(PlayCardAction action, CardPlayRule rule)
            {
                RoomCombatState room = source.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
                if (rule.SpawnUnit is CombatUnit unit)
                {
                    CombatUnit[] allies = room.Units.Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0).ToArray();
                    bool tank = unit.Health > allies.Select(unit => unit.Health).DefaultIfEmpty(0).Max();
                    return -Math.Abs(action.RoomIndex - preferredDeploymentRoom) * 50 + (tank ? -1 : 1) * action.PlayerPosition +
                        (tank && allies.Any(unit => unit.Modifiers?.IsChampion == true) ? 100 + unit.Health * 2 : 0);
                }
                CombatUnit? target = room.Units.FirstOrDefault(unit => unit.Id == action.TargetUnitId);
                if (target == null) return room.Units.Count(unit => unit.Team == CombatTeam.Enemy) * 25 + action.RoomIndex;
                bool harmful = rule.Effects.Any(effect => effect.Type is "Damage" or "Kill" ||
                    effect.Statuses.Any(status => status.DisplayCategory == "Negative"));
                if (harmful != (target.Team == CombatTeam.Enemy)) return -1e6;
                if (!harmful && rule.Effects.Any(effect => effect.Type == "FloorRearrange"))
                    return target.Health + Math.Max(0, target.Status("armor")?.Stacks ?? 0);
                return harmful ? 400 - target.Health : 150 + target.Attack + (target.Modifiers?.IsChampion == true ? 500 : 0);
            }
        }
    }
}
