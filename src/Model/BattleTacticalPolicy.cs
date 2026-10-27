using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Optional long-horizon seed. Preserve tower attack, build one combat floor,
    // and spend on equipment/armor before X buffs and free unit abilities.
    public static class BattleTacticalPolicy
    {
        private const double UnwantedTargetRank = double.NegativeInfinity;
        public static SupportedActionCandidate? Choose(BattleTurnState source,
            out IReadOnlyList<UnsupportedActionAttempt> unsupported, int preferredDeploymentRoom = 2)
            => Choose(source, out unsupported, preferredDeploymentRoom, null);

        internal static SupportedActionCandidate? Choose(BattleTurnState source,
            out IReadOnlyList<UnsupportedActionAttempt> unsupported, int preferredDeploymentRoom, BattleSearchWorkspace? workspace)
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
                bool positiveXBonus = rule?.CostType == "ConsumeRemainingEnergy" && source.Energy == 0 && instance != null &&
                    CardModifierModel.UpgradedStat(0, "XCost", true, instance.Permanent, instance.Temporary) > 0;
                return (token, rule, positiveXBonus);
            }).Where(item => item.rule != null && !TowerDebuff(item.rule) &&
                !(item.rule.CostType == "ConsumeRemainingEnergy" && source.Energy == 0 && !item.positiveXBonus))
                .OrderByDescending(item => Priority(item.rule!));
            foreach (var choice in choices)
            {
                CardPlayRule rule = choice.rule!;
                if (protectedChampion && rule.Effects.Any(effect => effect.Type == "FloorRearrange" &&
                    effect.Target == "DropTargetCharacter" && effect.Value == 0))
                {
                    // A shift ability can temporarily expose its champion. Use
                    // it before a legal tank-restoring card only when the
                    // champion also survives both sets of shift callbacks.
                    BattleActionEnumeration shifts = Abilities();
                    foreach (SupportedActionCandidate ability in shifts.SupportedCandidates.Where(ExposesChampion))
                    {
                        RoomCombatState room = source.Spawn.Train.Rooms.Single(item =>
                            item.Units.Any(unit => unit.Id == ability.Action.ActivatorUnitId));
                        CombatUnit tank = room.Units.First(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0);
                        BattleActionResult restored = Play(ability.Result.State!,
                            new PlayCardAction(choice.token.InstanceId, room.RoomIndex, targetUnitId: tank.Id));
                        if (restored.Supported)
                        {
                            RoomCombatState after = restored.State!.Spawn.Train.Rooms.Single(item => item.RoomIndex == room.RoomIndex);
                            if (after.Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0)?.Id == tank.Id &&
                                after.Units.Any(unit => unit.Id == ability.Action.ActivatorUnitId && unit.Health > 0))
                                return ability;
                        }
                    }
                }
                if (rule.Effects.Concat(rule.OnCastEffects).Any(effect => effect.Recursion != null || effect.SpikeRecipe != null ||
                    effect.Type is "ChooseDiscard" or "FreezeCard" or "DuplicateCard" or "MergeEquipment" or "Mix"))
                {
                    // This seed's rank depends on the input and captured state,
                    // never the simulated successor. Try inputs in that same
                    // stable order and stop at the first legal result instead
                    // of simulating every selection on every floor. The full
                    // action frontier and unoptimized reference stay exhaustive.
                    if (workspace?.OptimizeSelections == true)
                    {
                        foreach (PlayCardAction action in Inputs(choice.token, rule)
                            .SelectMany(action => BattleActionModel.EnumerateRecursionChoices(source, action, rule))
                            .Where(action => Rank(action, rule) > UnwantedTargetRank)
                            .OrderByDescending(action => Rank(action, rule)).ThenBy(action => action.TargetUnitId))
                        {
                            BattleActionResult result = Play(source, action);
                            if (result.Supported) return new SupportedActionCandidate(action, result);
                            if (result.Rejection == ActionRejection.Unsupported)
                                omitted.Add(new UnsupportedActionAttempt(action, result.Reason ?? "Unsupported tactical-seed selection."));
                        }
                        continue;
                    }
                    BattleActionEnumeration actions = workspace?.Enumerate(source, choice.token.InstanceId, false) ?? BattleActionModel.EnumerateActions(source, choice.token.InstanceId, includeAbilities: false);
                    omitted.AddRange(actions.UnsupportedAttempts);
                    SupportedActionCandidate? selected = actions.SupportedCandidates.Where(candidate => Rank(candidate.Action, rule) > UnwantedTargetRank)
                        .OrderByDescending(candidate => Rank(candidate.Action, rule))
                        .ThenBy(candidate => candidate.Action.TargetUnitId).FirstOrDefault();
                    if (selected != null) return selected;
                    continue;
                }
                // An unfavorable team is not a fallback target. Keep those
                // legal sacrifice/Incant branches in the full enumerator, but
                // let this seed defer rather than buff an enemy or hurt an ally.
                SupportedActionCandidate? shieldBreakingFallback = null;
                foreach (PlayCardAction action in Inputs(choice.token, rule).Where(action => Rank(action, rule) > UnwantedTargetRank)
                    .OrderByDescending(action => Rank(action, rule)).ThenBy(action => action.TargetUnitId))
                {
                    BattleActionResult result = Play(source, action);
                    if (result.Supported)
                    {
                        var candidate = new SupportedActionCandidate(action, result);
                        // Prefer actual injury over spending a damage-only card
                        // entirely on shields. If every target blocks it, keep
                        // the original legal shield-breaking cast as a fallback.
                        if (OnlyBreaksShield(source, action, rule, result.State!))
                        { shieldBreakingFallback ??= candidate; continue; }
                        return candidate;
                    }
                    if (result.Rejection == ActionRejection.Unsupported)
                        omitted.Add(new UnsupportedActionAttempt(action, result.Reason ?? "Unsupported tactical-seed action."));
                }
                if (shieldBreakingFallback != null) return shieldBreakingFallback;
            }
            BattleActionEnumeration abilities = Abilities();
            return abilities.SupportedCandidates.Where(candidate => !ExposesChampion(candidate) && !EmptyGoldReward(candidate) &&
                AbilityRank(candidate) > UnwantedTargetRank).OrderByDescending(AbilityRank).FirstOrDefault();

            double AbilityRank(SupportedActionCandidate candidate)
            {
                CardInstanceState? card = candidate.Result.State?.Spawn.Train.Context?.FindCard(candidate.Action.CardInstanceId);
                CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card?.DataId);
                // Apply the same preferred-team rule to a targeted ability as to
                // a hand card. The full enumerator retains legal sacrifice casts.
                return rule == null || candidate.Action.TargetUnitId == 0 ? 0 :
                    Rank(candidate.Action, CardModifierModel.Resolve(rule, card!));
            }

            BattleActionEnumeration Abilities()
            {
                if (cachedAbilities == null)
                {
                    cachedAbilities = workspace?.Abilities(source) ?? UnitAbilityModel.EnumerateActivations(source);
                    omitted.AddRange(cachedAbilities.UnsupportedAttempts);
                }
                return cachedAbilities;
            }

            BattleActionResult Play(BattleTurnState state, PlayCardAction action) => workspace?.Play(state, action) ?? BattleActionModel.PlayCard(state, action);

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
                    BattleHeuristicPolicy.RoomNegativeStatusValue(source, action, rule, preferredDeploymentRoom) ??
                    -100 * Math.Abs(action.RoomIndex - preferredDeploymentRoom);
                if (BattleHeuristicPolicy.SacrificeTargetValue(rule, target) is double sacrifice) return sacrifice;
                CardActionEffect[] effects = rule.Effects.Concat(rule.OnCastEffects).ToArray();
                // Only effects on the selected actor determine its team.
                // Room-wide enemy damage may accompany an allied-target buff.
                CardActionEffect[] targeted = effects.Where(effect =>
                    effect.Target is "DropTargetCharacter" or "LastTargetedCharacters").ToArray();
                bool damaging = targeted.Any(effect => effect.Type is "Damage" or "DamagePerSourceAttack" or
                    "DamagePerHp" or "DamageByUnitsKilled" or "Kill");
                bool harmful = damaging || targeted.Any(effect =>
                    effect.Statuses.Any(status => status.DisplayCategory == "Negative"));
                if (harmful) return target.Team != CombatTeam.Enemy ? UnwantedTargetRank : damaging ? 1000 - target.Health :
                    (target.IsBoss == true ? 10000 : 0) + target.Health + target.Attack * 3;
                if (target.Team != CombatTeam.Player || target.IsPyre) return UnwantedTargetRank;
                if (BattleHeuristicPolicy.RedundantEndlessUpgrade(source, action, rule, target)) return UnwantedTargetRank;
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
                if (BattleHeuristicPolicy.CorruptionUpgradeTargetValue(source, action, rule, target) is double upgrade) return upgrade;
                return target.Team == CombatTeam.Player && !target.IsPyre
                    ? target.Attack + (target.Modifiers?.IsChampion == true ? 10000 : 0) : UnwantedTargetRank;
            }

            double EquipmentValue(RoomCombatState room, CombatUnit unit, int cardId)
            {
                CardInstanceState? card = context.FindCard(cardId);
                EquipmentDefinition? definition = EquipmentMergeModel.Definition(context, source.PlayRules, cardId);
                if (card == null || definition == null) return 0;
                bool front = room.Units.FirstOrDefault(item => item.Team == CombatTeam.Player && !item.IsPyre && item.Health > 0)?.Id == unit.Id;
                double value = 0;
                var upgrades = definition.Upgrades.Concat(card.Permanent.Upgrades)
                    .Concat(new[] { EquipmentModel.TemporaryAggregate(card) }).ToArray();
                foreach (CardUpgradeModifier upgrade in upgrades)
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
                // A fixed attack replaces ordinary damage modifiers. Count
                // its actual gain (or loss) across this host's strikes instead
                // of favoring a champion whose attack the equipment lowers.
                // Existing fixed-attack stacks require a separate stacking /
                // replacement estimate; preserve their prior ranking here.
                var fixedAttacks = upgrades.SelectMany(upgrade => upgrade.Statuses)
                    .Where(status => status.Id == "fixedattack" && status.Stacks > 0).ToArray();
                if (fixedAttacks.Length == 1 && unit.Status("fixedattack") == null)
                    value += (fixedAttacks[0].Stacks - (double)unit.Attack -
                        upgrades.Sum(upgrade => (double)upgrade.Stats.Damage)) * unit.Attacks;
                return value;
            }
        }
        private static bool OnlyBreaksShield(BattleTurnState before, PlayCardAction action, CardPlayRule rule, BattleTurnState after)
        {
            if (rule.OnCastEffects.Count != 0 || rule.Effects.Count == 0 || rule.Effects.Any(effect =>
                effect.Type != "Damage" || effect.Value <= 0 ||
                effect.Target is not ("DropTargetCharacter" or "LastTargetedCharacters"))) return false;
            CombatUnit? target = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex)
                .Units.FirstOrDefault(unit => unit.Id == action.TargetUnitId);
            if (target?.Team != CombatTeam.Enemy ||
                !target.Statuses.Any(status => status.Stacks > 0 && status.Id is "spell shield" or "damage shield")) return false;
            CombatUnit? remaining = after.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .FirstOrDefault(unit => unit.Id == target.Id);
            return remaining != null && remaining.Health >= target.Health &&
                (remaining.Status("armor")?.Stacks ?? 0) >= (target.Status("armor")?.Stacks ?? 0);
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
