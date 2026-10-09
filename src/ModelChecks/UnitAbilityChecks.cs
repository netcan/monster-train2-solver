using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnitAbilityChecks
{
    internal static void Run()
    {
        var root = Root(); string parent = JsonSerializer.Serialize(root);
        var action = new PlayCardAction(2, 0, activatorUnitId: 10);
        BattleActionResult result = BattleActionModel.PlayCard(root, action);
        Require(result.Supported, "Skill activation rejected: " + result.Reason);
        var after = result.State!; var context = after.Spawn.Train.Context!;
        var actor = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 10);
        var enemy = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 11);
        Require(after.Energy == 3 && actor.Health == 19 && enemy.Health == 36 && enemy.LastAttackerId == 10,
            $"Payment/self/damage attribution differs: energy={after.Energy}, selfHP={actor.Health}, enemyHP={enemy.Health}, attacker={enemy.LastAttackerId}.");
        Require(actor.Ability!.Resolving == false && actor.Statuses.Single(status => status.Id == "cooldown").Stacks == 3 &&
            actor.Statuses.All(status => status.Id != "unit_ability_available"), "Own activation did not settle cooldown/availability callbacks.");
        Require(context.LastAbilityActivatorUnitId == 10 && context.CardInstances!.Select(card => card.InstanceId).SequenceEqual([1, 3]) &&
            context.Cards.Hand.Select(card => card.InstanceId).SequenceEqual([3]) &&
            context.CardRegistry!.Single(card => card.InstanceId == 2) is { LastPlayedCost: 1, PlayCount: 0 } &&
            context.Statistics!.CardsPlayedThisTurn.SequenceEqual([2]) && context.Statistics.TrackedCards.SequenceEqual([1, 3]) &&
            context.Statistics.Values.All(value => value.CardId != 2), "Detached skills changed ownership or acquired per-card deck statistics.");
        Require(UnitAbilityModel.Activate(after, action).Rejection == ActionRejection.Illegal &&
            BattleActionModel.EnumerateSupportedPlays(root).Any(play => play.ActivatorUnitId == 10) &&
            BattleActionModel.EnumerateSupportedPlays(after).All(play => play.ActivatorUnitId != 10),
            "Search enumeration accepted a cooling-down actor or omitted an available skill.");
        var ordinary = BattleActionModel.PlayCard(after, new(3, 0));
        Require(ordinary.Supported && ordinary.State!.Spawn.Train.Context!.LastAbilityActivatorUnitId == 0,
            "An ordinary card did not clear the last ability actor.");
        foreach (string status in new[] { "cooldown", "silenced", "muted" })
            Require(UnitAbilityModel.Activate(Root(status), action).Rejection == ActionRejection.Illegal,
                status + " did not prevent activation.");
        Require(UnitAbilityModel.Activate(Root(resolving: true), action).Rejection == ActionRejection.Illegal &&
            UnitAbilityModel.Activate(Root(deployment: true), action).Rejection == ActionRejection.Illegal &&
            UnitAbilityModel.Activate(Root(energy: 0), action).Rejection == ActionRejection.Illegal &&
            UnitAbilityModel.Activate(Root(handFull: true, allowFull: false), action).Rejection == ActionRejection.Illegal,
            "Resolving/deployment/cost/full-hand availability gates differ.");
        Require(UnitAbilityModel.Activate(Root(handFull: true), action).Supported &&
            UnitAbilityModel.Activate(root, new(3, 0, activatorUnitId: 10)).Rejection == ActionRejection.Illegal &&
            UnitAbilityModel.Activate(root, new(2, 1, activatorUnitId: 10)).Rejection == ActionRejection.Illegal &&
            UnitAbilityModel.Activate(Root(otherFloors: true), new(2, 9, activatorUnitId: 10)).Rejection == ActionRejection.Illegal,
            "Full-hand permission, cached card identity or room gates differ.");
        foreach (int energy in new[] { 0, 3 })
        {
            var x = UnitAbilityModel.Activate(Root(energy: energy, xCost: true), action);
            Require(x.Supported && x.State!.Energy == 1 && x.State.Spawn.Train.Context!.CardRegistry!
                .Single(card => card.InstanceId == 2).LastPlayedCost == energy, "X skill failed zero/current payment before pre-own effects.");
        }
        var fresh = Root(emptyCache: true);
        var allocated = UnitAbilityModel.Activate(fresh, new(4, 0, activatorUnitId: 10));
        Require(allocated.Supported && allocated.State!.Spawn.Train.Context!.NextCardId == 5 &&
            fresh.Spawn.Train.Context!.NextCardId == 4 && fresh.Spawn.Train.Context.AbilityCardCache!.Count == 0,
            "An uncached skill allocation mutated its parent or reused an identity.");
        var terminal = UnitAbilityModel.Activate(Root(terminalEnemy: true), action);
        Require(terminal.Supported && terminal.Outcome == RoomOutcome.BattleWon &&
            terminal.State!.Spawn.Train.Context!.QueryFrame!.RunningCombat == false &&
            terminal.State.Spawn.Train.Context.CardInstances!.Count == 0 &&
            terminal.State.Spawn.Train.Context.CardRegistry!.Any(card => card.InstanceId == 2) &&
            terminal.State.Spawn.Train.Context.LastAbilityActivatorUnitId == 10,
            "A winning skill did not preserve its detached identity and settle terminal state: " + terminal.Reason);
        var filters = new CardTargetFilters("Undamaged", ["absent"], ["unit_ability_available"], false, "absent", [""]);
        var self = new CardActionEffect("Heal", "Self", 1, true, false, [], filters: filters);
        Require(CardTargetModel.Collect(root.Spawn.Train, 0, self, [], selfUnitId: 10).UnitIds.SequenceEqual([10]),
            "Self must bypass team, health, status and subtype filters.");
        string expected = JsonSerializer.Serialize(result.State);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(UnitAbilityModel.Activate(root, action).State) == expected,
            "Parallel activations differ."));
        Require(JsonSerializer.Serialize(root) == parent, "Skill activation or enumeration mutated its parent.");
        Console.WriteLine("UNIT-ABILITY-CHECKS PASS: fixed/X payment, capped pre-own timing, self/damage attribution, cooldown callbacks, detached history, availability, search actions and 32 branches.");
    }

    internal static BattleTurnState Root(string? blocked = null, bool resolving = false, bool deployment = false, int energy = 3,
        bool handFull = false, bool allowFull = true, bool xCost = false, bool emptyCache = false, bool otherFloors = false,
        bool terminalEnemy = false, bool typed = true)
    {
        var rng = UnityRng.Seed(84);
        CombatStatus cooldown = new("cooldown", 1, removeStackAtEnd: true, preventRemovalDuringRelentless: true, stackable: true);
        CombatStatus marker = new("unit_ability_available", 1, stackable: false);
        CardPileState[] piles = [new("Standby", [new(1, "unit")]), new("Exhausted", []), new("DiscardBuffer", []), new("Eaten", []), new("Purged", [])];
        CardInstanceState[] owned = [CardInstanceState.Empty(1, "unit"), CardInstanceState.Empty(3, "ordinary")];
        var skill = CardInstanceState.Empty(2, "skill");
        var context = new CombatContext(new([new(3, "ordinary")], [], [], rng, 0, [], new([], [])), rng, 0, 4, handFull ? 1 : 10,
            [cooldown, marker], new([], [], [], [], [], [], [], 0, 0, 0, 0, 0, [1, 3], [1, 3], [1, 3]), owned,
            emptyCache ? owned : [owned[0], skill, owned[1]], false, [], piles,
            new(energy, true, deployment ? 0 : 1, 0, 0, 1, 0), false, [], false, new(3, 0, 0, "MonsterTurn", true),
            abilityCardCache: emptyCache ? [] : [new("skill", 2)], lastAbilityActivatorUnitId: 0);
        var creation = new CardCreationRule("skill", CardModifiers.Empty(), null, []);
        var triggers = new[] {
            new CombatTrigger("OnPreOwnAbilityActivated", false, false, false, 1,
                [new("CardEffectGainEnergy", 1, 0, "", 0, [], false, action: new("GainEnergy", "Self", 1, false, true, []))], false),
            new CombatTrigger("OnOwnAbilityActivated", false, false, true, 1,
                [new("CardEffectResetCooldown", 0, 0, "", 0, [], false, action: new("ResetCooldown", "Self", 0, false, true, [], cooldownParameter: false))], false),
            new CombatTrigger("OnUnitAbilityUnavailable", false, false, true, 1,
                [new("CardEffectRemoveStatusEffect", -1, 0, "", 0, [], false, action: new("RemoveStatus", "Self", -1, false, true, [marker]))], false) };
        var actor = new CombatUnit(10, "actor", CombatTeam.Player, 1, 18, 20, true, false, false,
            blocked == null ? [marker] : [marker, new(blocked, 1)], triggers, 1, 1, modifiers: new(1, 0, 0, 1, 1, true, false, []), isBoss: false,
            ability: new("skill", 3, 2, resolving: resolving, cardCreation: creation));
        var enemy = new CombatUnit(11, "enemy", CombatTeam.Enemy, 1, terminalEnemy ? 4 : 40, 40, true, false, terminalEnemy, [],
            isBoss: terminalEnemy, lastAttackerId: 0);
        var pyre = new CombatUnit(12, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, [], isBoss: false);
        var train = new TrainCombatState([new(0, deployment, [actor, enemy], [], context), new(1, deployment, [], [], context),
            new(2, deployment, [pyre], [], context)], [], 7, context);
        var spawn = new EnemySpawnState(train, [new([new([])]), new([new([])]), new([new([])])], [-1, -1, -1], 0, false, rng, 13,
            [], 0, false, 0, 0, deployment ? 0 : 1, []);
        var rules = new BattlePlayRules([new(0, 5, 7, true, false, false), new(1, 5, 7, true, false, false),
            new(2, 0, 7, true, true, true), new(9, 5, 7, true, false, false)],
            [new("skill", "skill", 1, "Spell", "Discard", null, [],
                [new("Damage", "FrontInRoom", 4, true, false, []), new("Heal", "Self", 1, true, false, [])],
                upgradeInteractions: [], costType: xCost ? "ConsumeRemainingEnergy" : "Default", ability: new("Unit", otherFloors, allowFull),
                cardType: typed ? "Spell" : null, isAnyAbility: typed ? true : null),
             new("ordinary", "ordinary", 0, "Null", "Discard", null, [], upgradeInteractions: [])]);
        return new(spawn, energy, 3, 3, 0, 0, "New", [new("Battle", 84, rng), new("CardDraw", 84, rng)], piles, [], rules);
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out var modifier) ||
            modifier.GetString() is not ("ability-activation" or "ability-activation-x" or "ability-activation-lethal")) return;
        var samples = fixture.GetProperty("Actions").EnumerateArray().Where(entry =>
            entry.GetProperty("Action").Deserialize<PlayCardAction>()!.ActivatorUnitId > 0).ToArray();
        Require(samples.Length >= 2 && samples.Select(entry => entry.GetProperty("Action").Deserialize<PlayCardAction>()!.ActivatorUnitId)
            .Distinct().Count() >= 2 && samples.Select(entry => entry.GetProperty("Action").Deserialize<PlayCardAction>()!.CardInstanceId)
            .Distinct().Count() == 1, "Native policy did not activate two units sharing one skill card.");
        int selfHeals = 0, zero = 0, positive = 0, terminal = 0, kills = 0;
        foreach (var sample in samples)
        {
            var before = sample.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = sample.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = sample.GetProperty("Action").Deserialize<PlayCardAction>()!;
            var context = after.Spawn.Train.Context!;
            var oldContext = before.Spawn.Train.Context!;
            var actor = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.ActivatorUnitId);
            var actualActor = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.ActivatorUnitId);
            var rule = before.PlayRules!.Cards.Single(card => card.DataId == actor.Ability!.DataId);
            int cost = rule.CostType == "ConsumeRemainingEnergy" ? before.Energy : rule.Cost;
            Require(context.LastAbilityActivatorUnitId == actor.Id && actualActor.Ability!.Resolving == false &&
                context.CardRegistry!.Single(card => card.InstanceId == action.CardInstanceId) is { PlayCount: 0 } cached &&
                cached.LastPlayedCost == cost && context.CardInstances!.All(card => card.InstanceId != cached.InstanceId) &&
                context.AbilityCardCache!.Single(entry => entry.DataId == actor.Ability!.DataId).InstanceId == cached.InstanceId &&
                context.Statistics!.CardsPlayedThisTurn.Last() == cached.InstanceId &&
                context.Statistics.TrackedCards.All(id => id != cached.InstanceId), "Native skill payment, history, actor or detached identity differs.");
            Require(actualActor.Triggers.Any(trigger => trigger.Kind == "OnPreOwnAbilityActivated" && trigger.HasTriggered) &&
                actualActor.Triggers.Any(trigger => trigger.Kind == "OnOwnAbilityActivated" && trigger.HasTriggered) &&
                actualActor.Statuses.Any(status => status.Id == "cooldown") &&
                actualActor.Statuses.All(status => status.Id != "unit_ability_available"), "Native own callbacks did not settle cooldown.");
            int armor = actor.Statuses.FirstOrDefault(status => status.Id == "armor")?.Stacks ?? 0;
            int damagedHealth = actor.Health - Math.Max(0, 3 - armor);
            if (actualActor.Health == Math.Min(actor.MaxHealth, damagedHealth + 1) && actualActor.Health > damagedHealth) selfHeals++;
            if (cost == 0) zero++; else positive++;
            if (sample.GetProperty("ActualOutcome").GetInt32() == (int)RoomOutcome.BattleWon) terminal++;
            if (after.Spawn.Train.Rooms.SelectMany(room => room.Units).Count(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0) <
                before.Spawn.Train.Rooms.SelectMany(room => room.Units).Count(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0)) kills++;
            if (sample.GetProperty("ActualOutcome").GetInt32() != (int)RoomOutcome.BattleWon)
                Require(after.Energy == before.Energy - cost && context.Cards.Hand.Select(card => card.InstanceId)
                    .SequenceEqual(oldContext.Cards.Hand.Select(card => card.InstanceId)), "Skill discarded a card or paid the wrong energy.");
            Verify(before, after, action);
            Parallel.For(0, 32, _ => Verify(before, after, action));
        }
        Require(selfHeals > 0 && positive > 0, "Native self healing after pre-own damage or paid skill coverage is missing.");
        if (modifier.GetString() == "ability-activation-x") Require(zero > 0, "Native X skill never paid zero energy.");
        if (modifier.GetString() == "ability-activation-lethal") Require(kills > 0 && terminal == 1, "Native lethal skill did not win by killing the Boss.");
        Console.WriteLine($"NATIVE-UNIT-ABILITY-CHECKS PASS: {samples.Length} complete activations, {selfHeals} self heals, {positive} paid/{zero} zero, {terminal} terminal; shared identity and 32 branches.");
    }
    internal static void NativeTerminal(FixtureValue entry)
    {
        var before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var old = before.Spawn.Train.Context!; var settled = after.Spawn.Train.Context!;
        var cached = old.CardRegistry!.Single(card => card.InstanceId == action.CardInstanceId);
        var final = settled.CardRegistry!.Single(card => card.InstanceId == action.CardInstanceId);
        var rule = CardModifierModel.Resolve(before.PlayRules!.Cards.Single(card => card.DataId == cached.DataId), cached);
        int paid = rule.CostType == "ConsumeRemainingEnergy" ? before.Energy : rule.Cost;
        Require(before.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.EndsBattleOnDeath && unit.Health > 0) &&
            after.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => !unit.EndsBattleOnDeath || unit.Health <= 0) &&
            settled.AllScenarioBossesDead == true && settled.QueryFrame!.RunningCombat == false && settled.KillCamActivated == true &&
            settled.CardInstances!.Count == 0 && settled.Cards.Hand.Count + settled.Cards.Draw.Count + settled.Cards.Discard.Count == 0 &&
            settled.OtherPiles!.All(pile => pile.Cards.Count == 0), "Native winning skill did not settle Boss death and clear owned piles.");
        Require(settled.LastAbilityActivatorUnitId == action.ActivatorUnitId && final.PlayCount == cached.PlayCount &&
            final.LastPlayedCost == paid && settled.Statistics!.PlayedCosts.Count == 0 &&
            settled.Statistics.CardsPlayedThisTurn.SequenceEqual(old.Statistics!.CardsPlayedThisTurn.Append(cached.InstanceId)) &&
            settled.Statistics.TrackedCards.Order().SequenceEqual(old.Statistics.DeckCards!.Order()) &&
            settled.Statistics.Values.All(value => value.CardId != cached.InstanceId) &&
            old.CardRegistry!.All(card => settled.CardRegistry!.Any(retained => retained.InstanceId == card.InstanceId)) &&
            after.Energy == before.Energy - paid, "Native terminal skill lost history, cache identity, paid cost or fallback deck ownership.");
        Console.WriteLine("NATIVE-TERMINAL-ABILITY-COVERAGE PASS: Boss kill, settled own callbacks, cleared owned piles, detached skill history and permanent-deck fallback.");
    }
    private static void Verify(BattleTurnState before, BattleTurnState actual, PlayCardAction action)
    {
        string parent = JsonSerializer.Serialize(before);
        var result = UnitAbilityModel.Activate(before, action);
        Require(result.Supported, "Native skill independently rejected: " + result.Reason);
        string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(result.State!), BattleTurnChecks.Comparable(actual));
        Require(difference == null, "Native skill state differs: " + difference);
        Require(JsonSerializer.Serialize(before) == parent, "Native skill comparison mutated its input.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
