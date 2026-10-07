using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class UnitHealthChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(92);
        var cards = new[] { CardInstanceState.Empty(8, "unit"), CardInstanceState.Empty(9, "unit") };
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 10, 10,
            statistics: BattleStatistics.Empty(deckCards: [8, 9]).TrackCards([8, 9]), cardInstances: cards, cardRegistry: cards,
            allScenarioBossesDead: false);
        var onHeal = new CombatTrigger("OnHeal", true, false, false, 1, [new("CardEffectRewardGold", 3, 0, "", 0, [], false)], false);
        var onDeath = new CombatTrigger("OnDeath", true, false, false, 1, [new("CardEffectRewardGold", 10, 0, "", 0, [], false)]);
        var unit = new CombatUnit(1, "unit", CombatTeam.Player, 8, 5, 10, true, false, false,
            [new("heal immunity", 1), new("heal multiplier", 1, 2), new("armor", 99)], [onHeal, onDeath], 8,
            modifiers: new(8, 0, 0, 1, 1, true, false, []));
        var root = new RoomCombatState(0, false, [unit], [], context);
        string parent = JsonSerializer.Serialize(root);
        var buff = UnitHealthModel.Apply(root, 1, 4);
        Require(buff.Supported && buff.State!.Units[0].MaxHealth == 14 && buff.State.Units[0].Health == 13 &&
            buff.State.Context!.CardInstances!.Single(card => card.InstanceId == 8).Temporary.Offsets.Health == 4 &&
            buff.State.Context.Gold == 0 && !buff.State.Units[0].Triggers[0].HasTriggered,
            "Maximum-health healing lost multiplier/immunity behavior, spawner offsets or OnHeal suppression.");
        var negative = UnitHealthModel.Apply(buff.State!, 1, -7);
        Require(negative.Supported && negative.State!.Units[0].MaxHealth == 14 && negative.State.Units[0].Health == 13 &&
            negative.State.Context!.CardInstances!.Single(card => card.InstanceId == 8).Temporary.Offsets.Health == -3,
            "A negative maximum-health buff changed the unit or lost its signed source-card offset.");
        var spawnRule = new CardPlayRule("unit", "unit", 1, "SpawnMonster", "Standby", unit, []);
        var laterSpawn = CardModifierModel.Resolve(spawnRule, negative.State!.Context!.CardInstances!.Single(card => card.InstanceId == 8));
        Require(laterSpawn.SpawnUnit!.MaxHealth == 7 && laterSpawn.SpawnUnit.Health == 7,
            "Signed maximum-health source offsets did not affect a later summon definition.");
        var untilDeath = UnitHealthModel.Apply(negative.State!, 1, 3, lifetime: "TemporaryUntilUnitDeath");
        Require(untilDeath.Supported && untilDeath.State!.Units[0].MaxHealth == 17 && untilDeath.State.Units[0].Health == 17 &&
            untilDeath.State.Context!.CardInstances!.Single(card => card.InstanceId == 8).Temporary.Offsets.Health == -3,
            "An until-death maximum-health buff changed the source card or lost the health clamp.");
        var reduced = UnitHealthModel.Apply(root, 1, 4, true);
        Require(reduced.Supported && reduced.State!.Units[0].MaxHealth == 6 && reduced.State.Units[0].Health == 1 &&
            reduced.State.Units[0].Statuses.Single(status => status.Id == "armor").Stacks == 99, "Maximum-health loss used armor or ordinary damage rules.");
        var killed = UnitHealthModel.Apply(root, 1, 5, true);
        Require(killed.Supported && killed.State!.Units.Count == 0 && killed.State.Context!.Gold == 10 &&
            killed.State.Context.Statistics!.MonstersDeadThisBattle == 1 && killed.State.Context.Statistics.Value(8, "TimesExhausted") == 1,
            "Lethal maximum-health loss did not apply death triggers and statistics.");
        Require(JsonSerializer.Serialize(UnitHealthModel.Apply(root, 1, -8, true).State) == parent &&
            JsonSerializer.Serialize(UnitHealthModel.Apply(root, 1, 0, true).State) == parent &&
            !UnitHealthModel.Apply(root, 1, 1, lifetime: "Permanent").Supported,
            "Debuff no-op values or invalid lifetimes differ.");
        var preview = new RoomCombatState(0, false, [unit], [], context, preview: true);
        Require(UnitHealthModel.Apply(preview, 1, 4).State!.Context!.CardInstances!.Single(card => card.InstanceId == 8).Temporary.Offsets.Health == 0,
            "Preview maximum-health buffs wrote source-card state.");
        var unhealable = new CombatUnit(2, "unhealable", CombatTeam.Player, 8, 3, 10, true, false, false, [], spawnerCardId: 9,
            modifiers: new(8, 0, 0, 1, 1, false, false, [], spawnerMatchesDefinition: false));
        var noHeal = UnitHealthModel.Apply(new(0, false, [unhealable], [], context), 2, 4);
        Require(noHeal.Supported && noHeal.State!.Units[0].Health == 3 && noHeal.State.Units[0].MaxHealth == 14 &&
            noHeal.State.Context!.CardInstances!.Single(card => card.InstanceId == 9).Temporary.Offsets.Health == 4,
            "Unhealable units or mismatched spawners did not receive maximum-health/card changes.");
        var capped = new CombatUnit(3, "cap", CombatTeam.Player, 0, 99998, 99998, true, false, false, [],
            modifiers: new(0, 0, 0, 1, 1, true, false, []));
        Require(UnitHealthModel.Apply(new(0, false, [capped], [], context), 3, 8).State!.Units[0].MaxHealth == 99999 &&
            UnitHealthModel.Apply(new(0, false, [capped], [], context), 3, 8).State!.Units[0].Health == 99999,
            "Native maximum-health ceiling differs.");
        var boss = new CombatUnit(4, "boss", CombatTeam.Enemy, 1, 3, 5, true, false, true, []);
        CardActionEffect Effect(string type, int value, string target, bool player) => new(type, target, value, !player, player, []);
        var pyre = new CombatUnit(5, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, []);
        var pyreRoom = new RoomCombatState(1, false, [pyre], [], context);
        var train = new TrainCombatState([new(0, false, [boss, unit], [], context), pyreRoom], [], 5, context);
        var terminal = CardSpellModel.Apply(train, 0, [Effect("DebuffHealth", 5, "DropTargetCharacter", false),
            Effect("BuffHealth", 4, "Tower", true)], 4);
        Require(terminal.Supported && terminal.Outcome == RoomOutcome.BattleWon && terminal.State!.Rooms[0].Units.Single().MaxHealth == 14 &&
            terminal.State.Context!.CardInstances!.Count == 0 && terminal.State.Context.CardRegistry!.Single(card => card.InstanceId == 8).Temporary.Offsets.Health == 4,
            "Post-sacrifice maximum-health buffs lost detached spawners or the terminal outcome: " + terminal.UnsupportedReason + "; " + terminal.Outcome);
        Require(CardSpellModel.TestPlay(new TrainCombatState([new(0, false, [], [], context), pyreRoom], [], 5, context), 0,
            [Effect("BuffHealth", 4, "Tower", true)], 0).CanPlay, "Empty maximum-health effects incorrectly failed native base casting tests.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(UnitHealthModel.Apply(root, 1, 4).State) == JsonSerializer.Serialize(buff.State),
            "Parallel maximum-health changes differ."));
        Require(JsonSerializer.Serialize(root) == parent, "Maximum-health changes mutated their parent.");
        Console.WriteLine("UNIT-HEALTH-CHECKS PASS: raw signed spawner offsets, lifetimes, healability, multiplier/immunity, OnHeal suppression, ceilings, lethal/terminal state and parallel isolation.");
    }

    internal static void Native(FixtureValue fixture)
    {
        bool lethal = fixture.GetProperty("ModifierScenario").GetString() == "max-health-lethal";
        int plays = 0, remote = 0, deaths = 0, immuneHeals = 0, multipliers = 0, detached = 0;
        foreach (FixtureValue entry in fixture.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatContext context = before.Spawn.Train.Context!;
            string dataId = context.Cards.Hand.Single(card => card.InstanceId == action.CardInstanceId).DataId;
            CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == dataId);
            if (!rule.Effects.Any(effect => effect.Type == "BuffHealth")) continue;
            plays++;
            BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatContext nextContext = actual.Spawn.Train.Context!;
            CombatUnit[] after = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).ToArray();
            foreach (RoomCombatState room in before.Spawn.Train.Rooms)
                foreach (CombatUnit old in room.Units)
                {
                    CombatUnit? next = after.SingleOrDefault(unit => unit.Id == old.Id);
                    if (old.IsPyre) { Require(next != null && JsonSerializer.Serialize(old) == JsonSerializer.Serialize(next), "Global HP changes affected the Pyre."); continue; }
                    if (old.Team == CombatTeam.Enemy)
                    {
                        if (lethal || old.Health == 1) { Require(next == null, "A lethal native maximum-health target survived."); deaths++; }
                        else Require(next != null && next.MaxHealth == old.MaxHealth - 1 && next.Health == old.Health - 1,
                            "Native nonlethal maximum-health loss differs.");
                        continue;
                    }
                    Require(next != null && next.MaxHealth == old.MaxHealth + (lethal ? 13 : 11), "Native friendly maximum health differs.");
                    CardInstanceState oldCard = context.CardRegistry!.Single(card => card.InstanceId == old.SpawnerCardId);
                    CardInstanceState nextCard = nextContext.CardRegistry!.Single(card => card.InstanceId == old.SpawnerCardId);
                    Require(nextCard.Temporary.Offsets.Health == oldCard.Temporary.Offsets.Health + (lethal ? 4 : 2) &&
                        JsonSerializer.Serialize(nextCard.Permanent) == JsonSerializer.Serialize(oldCard.Permanent) &&
                        next!.Triggers.Where(trigger => trigger.Kind == "OnHeal").All(trigger => !trigger.HasTriggered),
                        "Native signed temporary offsets, lifetime or suppressed OnHeal differs.");
                    if (room.RoomIndex != action.RoomIndex) remote++;
                    if (old.Statuses.Any(status => status.Id == "heal immunity") && next!.Health > old.Health) immuneHeals++;
                    if (old.Statuses.Any(status => status.Id == "heal multiplier") && next!.Health > old.Health) multipliers++;
                    if (!nextContext.CardInstances!.Any(card => card.InstanceId == old.SpawnerCardId)) detached++;
                }
        }
        Require(plays > 0 && remote > 0 && deaths > 0 && immuneHeals > 0 && multipliers > 0,
            "Maximum-health oracle lacks spells, remote state, sacrifice, immunity or multipliers.");
        if (lethal) Require(detached > 0 && fixture.GetProperty("Actions").EnumerateArray().Any(entry => entry.GetProperty("ActualOutcome").GetInt32() == (int)RoomOutcome.BattleWon),
            "Lethal maximum-health oracle lacks post-boss detached spawner changes.");
        Console.WriteLine($"NATIVE-UNIT-HEALTH-COVERAGE PASS: {plays} plays, {remote} remote friendlies, {deaths} sacrifices, {immuneHeals} immune heals, {multipliers} multiplier heals and {detached} detached spawners.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
