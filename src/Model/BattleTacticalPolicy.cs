using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Optional long-horizon seed. Preserve tower attack, build one combat floor,
    // and spend on equipment/armor before X buffs and free unit abilities.
    public static class BattleTacticalPolicy
    {
        public static SupportedActionCandidate? Choose(BattleTurnState source,
            out IReadOnlyList<UnsupportedActionAttempt> unsupported, int preferredDeploymentRoom = 2)
        {
            var omitted = new List<UnsupportedActionAttempt>(); unsupported = omitted;
            CombatContext? context = source.Spawn.Train.Context;
            if (context == null || source.PlayRules == null) return null;
            var choices = context.Cards.Hand.Select(token =>
            {
                CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == token.DataId);
                CardInstanceState? instance = context.FindCard(token.InstanceId);
                if (rule != null && instance != null) rule = CardModifierModel.Resolve(rule, instance);
                return (token, rule);
            }).Where(item => item.rule != null && !TowerDebuff(item.rule) &&
                !(item.rule.CostType == "ConsumeRemainingEnergy" && source.Energy == 0))
                .OrderByDescending(item => Priority(item.rule!));
            foreach (var choice in choices)
            {
                CardPlayRule rule = choice.rule!;
                if (rule.Effects.Concat(rule.OnCastEffects).Any(effect => effect.Recursion != null || effect.SpikeRecipe != null ||
                    effect.Type is "ChooseDiscard" or "FreezeCard" or "DuplicateCard" or "MergeEquipment" or "Mix"))
                {
                    BattleActionEnumeration actions = BattleActionModel.EnumerateActions(source, choice.token.InstanceId, includeAbilities: false);
                    omitted.AddRange(actions.UnsupportedAttempts);
                    SupportedActionCandidate? selected = actions.SupportedCandidates.OrderByDescending(candidate => Rank(candidate.Action, rule))
                        .ThenBy(candidate => candidate.Action.TargetUnitId).FirstOrDefault();
                    if (selected != null) return selected;
                    continue;
                }
                foreach (PlayCardAction action in Inputs(choice.token, rule).OrderByDescending(action => Rank(action, rule)).ThenBy(action => action.TargetUnitId))
                {
                    BattleActionResult result = BattleActionModel.PlayCard(source, action);
                    if (result.Supported) return new SupportedActionCandidate(action, result);
                    if (result.Rejection == ActionRejection.Unsupported)
                        omitted.Add(new UnsupportedActionAttempt(action, result.Reason ?? "Unsupported tactical-seed action."));
                }
            }
            BattleActionEnumeration abilities = UnitAbilityModel.EnumerateActivations(source);
            omitted.AddRange(abilities.UnsupportedAttempts);
            return abilities.SupportedCandidates.FirstOrDefault();

            IEnumerable<PlayCardAction> Inputs(CardToken token, CardPlayRule rule)
            {
                bool targeted = CardSpellModel.RequiresUnitTarget(rule.Effects.Concat(rule.OnCastEffects).ToArray());
                foreach (RoomPlayRule room in source.PlayRules.Rooms)
                {
                    RoomCombatState? state = source.Spawn.Train.Rooms.FirstOrDefault(item => item.RoomIndex == room.RoomIndex);
                    if (state == null) continue;
                    int first = rule.Effect == "SpawnMonster" ? 0 : -1;
                    int last = rule.Effect == "SpawnMonster" ? state.Units.Count(unit => unit.Team == CombatTeam.Player) : -1;
                    foreach (int target in targeted ? state.Units.Select(unit => unit.Id) : new[] { 0 })
                    for (int position = first; position <= last; position++)
                        yield return new PlayCardAction(token.InstanceId, room.RoomIndex, position, target);
                }
            }

            double Rank(PlayCardAction action, CardPlayRule rule)
            {
                RoomCombatState room = source.Spawn.Train.Rooms.Single(item => item.RoomIndex == action.RoomIndex);
                if (rule.CardType == "Monster" && rule.SpawnUnit != null)
                {
                    bool tank = rule.SpawnUnit.Health > room.Units.Where(unit => unit.Team == CombatTeam.Player)
                        .Select(unit => unit.Health).DefaultIfEmpty(0).Max();
                    return -100 * Math.Abs(action.RoomIndex - preferredDeploymentRoom) + (tank ? -action.PlayerPosition : action.PlayerPosition);
                }
                CombatUnit? target = room.Units.FirstOrDefault(unit => unit.Id == action.TargetUnitId);
                if (target == null) return -100 * Math.Abs(action.RoomIndex - preferredDeploymentRoom);
                bool harmful = rule.Effects.Any(effect => effect.Type is "Damage" or "Kill");
                if (harmful) return target.Team == CombatTeam.Enemy ? 1000 - target.Health : -100000;
                return target.Team == CombatTeam.Player && !target.IsPyre
                    ? target.Attack + (target.Modifiers?.IsChampion == true ? 10000 : 0) : -100000;
            }
        }
        private static bool TowerDebuff(CardPlayRule rule) => rule.Effects.Any(effect => effect.Target == "Tower" &&
            effect.Statuses.Any(status => status.DisplayCategory == "Negative"));
        private static double Priority(CardPlayRule rule)
        {
            if (rule.CardType == "Monster" && rule.SpawnUnit != null)
                return 1000 + rule.SpawnUnit.Attack * 2 + rule.SpawnUnit.Health + (rule.SpawnUnit.Modifiers?.IsChampion == true ? 500 : 0);
            if (rule.Cost == 0 && rule.CostType != "ConsumeRemainingEnergy" && rule.Effects.Any(effect => effect.Type is "Draw" or "DrawType")) return 1700;
            if (rule.Equipment != null) return 900;
            if (rule.Effects.Any(effect => effect.Statuses.Any(status => status.Id == "armor"))) return 700;
            return rule.CostType == "ConsumeRemainingEnergy" ? 500 : 100;
        }
    }
}
