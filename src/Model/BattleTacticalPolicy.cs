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
            BattleActionEnumeration? cachedAbilities = null;
            bool protectedChampion = source.Spawn.Train.Rooms.Any(room =>
                room.Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.IsBoss == true) &&
                room.Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0)
                    ?.Modifiers?.IsChampion != true &&
                room.Units.Any(unit => unit.Modifiers?.IsChampion == true && unit.Health > 0 && unit.Ability?.HasAbility == true));
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
                if (protectedChampion && rule.Effects.Any(effect => effect.Type == "FloorRearrange" &&
                    effect.Target == "DropTargetCharacter" && effect.Value == 0))
                {
                    // A shift ability can temporarily expose its champion. Use
                    // it before a legal tank-restoring card instead of wasting
                    // that card on the tank already standing in front.
                    BattleActionEnumeration shifts = Abilities();
                    foreach (SupportedActionCandidate ability in shifts.SupportedCandidates.Where(ExposesChampion))
                    {
                        RoomCombatState room = source.Spawn.Train.Rooms.Single(item =>
                            item.Units.Any(unit => unit.Id == ability.Action.ActivatorUnitId));
                        CombatUnit tank = room.Units.First(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0);
                        BattleActionResult restored = BattleActionModel.PlayCard(ability.Result.State!,
                            new PlayCardAction(choice.token.InstanceId, room.RoomIndex, targetUnitId: tank.Id));
                        if (restored.Supported && restored.State!.Spawn.Train.Rooms.Single(item => item.RoomIndex == room.RoomIndex)
                            .Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0)?.Id == tank.Id)
                            return ability;
                    }
                }
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
            BattleActionEnumeration abilities = Abilities();
            return abilities.SupportedCandidates.FirstOrDefault(candidate => !ExposesChampion(candidate) && !EmptyGoldReward(candidate));

            BattleActionEnumeration Abilities()
            {
                if (cachedAbilities == null)
                {
                    cachedAbilities = UnitAbilityModel.EnumerateActivations(source);
                    omitted.AddRange(cachedAbilities.UnsupportedAttempts);
                }
                return cachedAbilities;
            }

            bool EmptyGoldReward(SupportedActionCandidate candidate)
            {
                CombatContext? after = candidate.Result.State?.Spawn.Train.Context;
                CardInstanceState? card = after?.FindCard(candidate.Action.CardInstanceId);
                CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card?.DataId);
                return after?.Gold == context.Gold && rule != null && rule.Effects.Count > 0 &&
                    rule.Effects.Concat(rule.OnCastEffects).All(effect => effect.Type == "RewardGoldPerDragonHoard");
            }

            bool ExposesChampion(SupportedActionCandidate candidate)
            {
                RoomCombatState? room = source.Spawn.Train.Rooms.FirstOrDefault(item =>
                    item.Units.Any(unit => unit.Id == candidate.Action.ActivatorUnitId));
                CombatUnit? caster = room?.Units.FirstOrDefault(unit => unit.Id == candidate.Action.ActivatorUnitId);
                if (caster?.Modifiers?.IsChampion != true || !room!.Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.IsBoss == true))
                    return false;
                CombatUnit? front = room.Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0);
                return front != null && front.Id != caster.Id && candidate.Result.State!.Spawn.Train.Rooms
                    .Single(item => item.RoomIndex == room.RoomIndex).Units
                    .FirstOrDefault(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0)?.Id == caster.Id;
            }

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
                if (target == null) return BattleHeuristicPolicy.RoomDamageValue(source, action, rule) ??
                    -100 * Math.Abs(action.RoomIndex - preferredDeploymentRoom);
                CardActionEffect[] effects = rule.Effects.Concat(rule.OnCastEffects).ToArray();
                bool damaging = effects.Any(effect => effect.Type is "Damage" or "Kill");
                bool harmful = damaging || effects.Any(effect =>
                    (effect.Target is "DropTargetCharacter" or "LastTargetedCharacters") &&
                    effect.Statuses.Any(status => status.DisplayCategory == "Negative"));
                if (harmful) return target.Team != CombatTeam.Enemy ? -100000 : damaging ? 1000 - target.Health :
                    (target.IsBoss == true ? 10000 : 0) + target.Health + target.Attack * 3;
                if (target.Team != CombatTeam.Player || target.IsPyre) return -100000;
                if (rule.Equipment != null)
                {
                    double value = EquipmentValue(room, target, action.CardInstanceId);
                    IReadOnlyList<int> equipped = target.EquipmentCards ?? Array.Empty<int>();
                    int replaced = Math.Max(0, equipped.Count - (target.Modifiers?.EquipmentLimit ?? 1) + 1);
                    foreach (int cardId in equipped.Take(replaced)) value -= EquipmentValue(room, target, cardId);
                    return value - Math.Abs(action.RoomIndex - preferredDeploymentRoom) * 2 +
                        (target.Modifiers?.IsChampion == true ? 3 : 0);
                }
                if (effects.Any(effect => effect.Type == "FloorRearrange" && effect.Target == "DropTargetCharacter" && effect.Value == 0))
                    return target.Health + Math.Max(0, target.Status("armor")?.Stacks ?? 0) +
                        target.Triggers.Count(trigger => trigger.Kind == "OnHit") * 50;
                return target.Team == CombatTeam.Player && !target.IsPyre
                    ? target.Attack + (target.Modifiers?.IsChampion == true ? 10000 : 0) : -100000;
            }

            double EquipmentValue(RoomCombatState room, CombatUnit unit, int cardId)
            {
                CardInstanceState? card = context.FindCard(cardId);
                EquipmentDefinition? definition = EquipmentMergeModel.Definition(context, source.PlayRules, cardId);
                if (card == null || definition == null) return 0;
                bool front = room.Units.FirstOrDefault(item => item.Team == CombatTeam.Player && !item.IsPyre && item.Health > 0)?.Id == unit.Id;
                double value = 0;
                foreach (CardUpgradeModifier upgrade in definition.Upgrades.Concat(card.Permanent.Upgrades)
                    .Concat(new[] { EquipmentModel.TemporaryAggregate(card) }))
                {
                    value += upgrade.Stats.Health * .5 + upgrade.Stats.Damage * unit.Attacks;
                    value += Math.Min(unit.Size, Math.Max(0, -upgrade.Stats.Size)) * 8;
                    if (upgrade.Statuses.Any(status => status.Id == "silenced" && status.Stacks > 0))
                        value -= unit.Triggers.Count(trigger => !trigger.IgnoreSilence) * 50 +
                            (unit.Ability?.HasAbility == true ? 100 : 0);
                    foreach (CombatTrigger trigger in upgrade.TriggerUpgrades ?? Array.Empty<CombatTrigger>())
                    foreach (CombatEffect effect in trigger.Effects)
                    {
                        CardActionEffect? action = effect.Action;
                        if (action?.Type == "DamagePerHp" && action.DamagePerHpLastAttacker)
                            value += unit.Health * (front ? 1.4 : .4);
                        if (action?.Type == "RemoveStatus" && action.Statuses.Any(status => status.Id == "cooldown") && unit.Ability?.HasAbility == true)
                            value += unit.Modifiers?.IsChampion == true ? 220 : 80;
                        if (action?.Type == "Heal") value += front ? 25 : 6;
                        if (action?.Type == "PlayUnitTrigger") value += unit.Triggers.Count(item => item.Kind == "OnShift") * 70;
                    }
                }
                return value;
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
