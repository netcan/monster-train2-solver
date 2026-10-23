using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;

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
            foreach (RoomCombatState room in before.Spawn.Train.Rooms)
            foreach (CombatUnit prior in room.Units)
            {
                if (prior.Team != CombatTeam.Enemy || prior.Health <= 0 || prior.Modifiers == null ||
                    !actual.TryGetValue(prior.Id, out CombatUnit? current) || current.Modifiers == null) continue;
                if (prior.IsBoss == true)
                {
                    TrueFinalBossChecks.Require(current.Modifiers.DamageBuff == prior.Modifiers.DamageBuff,
                        "Tower Incant incorrectly buffed an excluded boss.");
                    excludedBosses++;
                }
                else if (room.RoomIndex != play.RoomIndex && prior.CanAttack &&
                    current.Modifiers.DamageBuff == prior.Modifiers.DamageBuff + amount) remoteBuffs++;
            }
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            string expected = BattleTurnChecks.Comparable(after, compareRoomEnabled: BattleTurnChecks.ShouldCompareRoomEnabled(root));
            string input = JsonSerializer.Serialize(play, ModelJson.Options);
            TrueFinalBossChecks.Require(BattleActionModel.EnumerateActions(before).SupportedCandidates.Any(candidate =>
                JsonSerializer.Serialize(candidate.Action, ModelJson.Options) == input &&
                BattleTurnChecks.Comparable(candidate.Result.State!, compareRoomEnabled: BattleTurnChecks.ShouldCompareRoomEnabled(root)) == expected),
                "The actual chosen-floor spell must remain in the searchable frontier with its complete native successor.");
            Parallel.For(0, 16, _ =>
            {
                BattleActionResult branch = BattleActionModel.PlayCard(before, play);
                TrueFinalBossChecks.Require(branch.Supported &&
                    BattleTurnChecks.Comparable(branch.State!, compareRoomEnabled: BattleTurnChecks.ShouldCompareRoomEnabled(root)) == expected &&
                    JsonSerializer.Serialize(before, ModelJson.Options) == parent,
                    "Chosen-floor spell branches changed their native result or shared input.");
            });
            casts++;
        }
        TrueFinalBossChecks.Require(casts > 0 && remoteBuffs > 0 && excludedBosses > 0,
            $"Chosen Incant acceptance requires actual spells, positive remote attack buffs and excluded bosses; observed casts={casts}, remoteBuffs={remoteBuffs}, excludedBosses={excludedBosses}.");
        Console.WriteLine($"CHOSEN-INCANT-ATTACK PASS: {casts} searchable native casts, {remoteBuffs} remote attack buffs, {excludedBosses} excluded bosses and sixteen immutable branches per cast.");
    }
}
