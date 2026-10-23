using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ChosenIncantAttackChecks
{
    internal static void Native(FixtureValue root)
    {
        int casts = 0, remoteBuffs = 0, excludedBosses = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction play = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            string? card = before.Spawn.Train.Context!.FindCard(play.CardInstanceId)?.DataId;
            CardPlayRule? rule = before.PlayRules?.Cards.FirstOrDefault(value => value.DataId == card);
            if (rule?.CardType != "Spell" || rule.IsAnyAbility != false) continue;
            CombatUnit[] clerics = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == play.RoomIndex).Units
                .Where(unit => unit.AssetKey == "ClericT3_IncantAttack_Chosen" && unit.Health > 0 &&
                    !unit.Statuses.Any(status => status.Id == "silenced" && status.Stacks > 0)).ToArray();
            if (clerics.Length == 0) continue;
            int amount = 0;
            foreach (CombatUnit cleric in clerics)
            {
                CombatTrigger trigger = cleric.Triggers.Single(value => value.Kind == "CardSpellPlayed");
                var effect = trigger.Effects.Single();
                TrueFinalBossChecks.Require(effect.Type == "CardEffectBuffDamage" && effect.Value == 5 &&
                    effect.Action is { Type: "BuffAttack", Target: "Tower", AllowEnemy: true, AllowPlayer: false, UseCardTraits: false } &&
                    effect.Action.Filters?.IgnoreBosses == true && !trigger.Once,
                    "Chosen cleric must retain its original fixed +5 tower Incant and boss exclusion.");
                amount += effect.Value * trigger.FireCount;
            }
            var actual = after.Spawn.Train.Rooms.SelectMany(room => room.Units).ToDictionary(unit => unit.Id);
            foreach (CombatUnit prior in before.Spawn.Train.Rooms.Where(room => room.RoomIndex != play.RoomIndex).SelectMany(room => room.Units))
            {
                if (prior.Team != CombatTeam.Enemy || prior.Health <= 0 || prior.Modifiers == null ||
                    !actual.TryGetValue(prior.Id, out CombatUnit? current) || current.Modifiers == null) continue;
                if (prior.IsBoss == true)
                {
                    TrueFinalBossChecks.Require(current.Modifiers.DamageBuff == prior.Modifiers.DamageBuff,
                        "Tower Incant incorrectly buffed an excluded remote boss.");
                    excludedBosses++;
                }
                else if (prior.CanAttack && current.Modifiers.DamageBuff == prior.Modifiers.DamageBuff + amount) remoteBuffs++;
            }
            casts++;
        }
        TrueFinalBossChecks.Require(casts > 0 && remoteBuffs > 0 && excludedBosses > 0,
            "Chosen Incant acceptance requires actual spells, positive remote attack buffs and excluded remote bosses.");
        Console.WriteLine($"CHOSEN-INCANT-ATTACK PASS: {casts} actual spell casts, {remoteBuffs} native remote attack buffs and {excludedBosses} excluded remote bosses.");
    }
}
