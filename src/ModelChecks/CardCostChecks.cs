using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CardCostChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(719);
        CardActionEffect Damage() => new("Damage", "Room", 0, true, false, []);
        CardActionEffect Gain() => new("GainEnergy", "Room", 2, false, false, []);
        ScalingDamageTrait[] Traits(int raw) => [new(new("UnmodifiedPlayedCost", sourceRawCost: raw, variableCost: true), 2, 1, false),
            new(new("PlayedCost", sourceRawCost: raw, variableCost: true), 1, .5f, true)];
        BattleTurnState Root(int energy, string costType = "ConsumeRemainingEnergy", bool terminal = false)
        {
            var instances = new CardInstanceState[] {
                new(1, "x", new(new(cost: -99, xCost: 3), [], 0, []), new(new(xCost: -1), [], 0, []), 33, 0, 0, [], damageScalingTraits: Traits(2)),
                new(2, "other", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [], damageScalingTraits: Traits(0)) };
            CardPileState[] piles = [new("Standby", []), new("Exhausted", []), new("DiscardBuffer", []), new("Eaten", []), new("Purged", [])];
            var context = new CombatContext(new([new(1, "x"), new(2, "other")], [], [], rng, 0, []), rng, 0, 3, 10,
                statistics: new([], [], [], [], [], [], [], 0, 0, 0, 0, 0, [1, 2], [1, 2]), cardInstances: instances, cardRegistry: instances,
                otherPiles: piles, queryFrame: new(energy, true, 2, 0, 0, 1, 0), energyState: new(9, 0, 0, "MonsterTurn", true));
            var enemy = new CombatUnit(10, "enemy", CombatTeam.Enemy, 0, terminal ? 1 : 100, terminal ? 1 : 100,
                false, false, terminal, [], isBoss: terminal);
            var pyre = new CombatUnit(11, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, []);
            var train = new TrainCombatState([new(0, false, [enemy], [], context), new(1, false, [pyre], [], context)],
                [new(10, 0, false, false)], 7, context);
            var spawn = new EnemySpawnState(train, [new([new([])]), new([new([])]), new([new([])])], [-1, -1, -1],
                0, false, rng, 12, [], 0, false, 0, 0, 2, []);
            var rules = new BattlePlayRules([new(0, 5, 7, true, false, false), new(1, 0, 7, true, true, true)],
                [new("x", "x", 2, "Spell", "Discard", null, [], [Damage(), Gain()], costType: costType),
                    new("other", "other", 0, "Spell", "Discard", null, [], [Damage(), Gain()], costType: "ConsumeRemainingEnergy")]);
            return new(spawn, energy, 3, 5, 0, 0, "New", [new("Spawning", 719, rng)], piles, [], rules);
        }
        var root = Root(4); string parent = JsonSerializer.Serialize(root);
        BattleActionResult first = BattleActionModel.PlayCard(root, new(1, 0));
        Require(first.Supported && first.State!.Energy == 2 && first.State.Spawn.Train.Rooms[0].Units.Single().Health == 88,
            "X payment, fixed-cost reduction, X-cost modifiers, raw-cost scaling or later gains differ: " + first.Reason);
        var instance = first.State!.Spawn.Train.Context!.CardInstances!.Single(card => card.InstanceId == 1);
        Require(instance.LastPlayedCost == 4 && instance.PlayCount == 1 && first.State.Spawn.Train.Context.Statistics!.PlayedCosts.Count == 0 &&
            first.State.Spawn.Train.Context.CardRegistry!.Single(card => card.InstanceId == 1).LastPlayedCost == 4,
            "Completed X payment failed to update instance/registry or clear the transient payment statistic.");
        BattleActionResult second = BattleActionModel.PlayCard(first.State, new(2, 0));
        Require(second.Supported && second.State!.Energy == 2 && second.State.Spawn.Train.Rooms[0].Units.Single().Health == 83 &&
            second.State.Spawn.Train.Context!.CardInstances!.Single(card => card.InstanceId == 2).LastPlayedCost == 2,
            "A later X play reused the root energy instead of its current payment.");
        var nextTurn = BattleTurnModel.EndTurn(second.State!);
        Require(nextTurn.Supported && nextTurn.State!.Energy == 3 && nextTurn.State.Spawn.Turn == 3,
            "X plays did not preserve the next-turn transition: " + nextTurn.UnsupportedReason);
        var redrawn = BattleActionModel.PlayCard(nextTurn.State!, new(1, 0));
        Require(redrawn.Supported && redrawn.State!.Energy == 2 &&
            redrawn.State.Spawn.Train.Context!.CardInstances!.Single(card => card.InstanceId == 1).LastPlayedCost == 3,
            "A redrawn X card reused a prior-turn payment: " + redrawn.Reason);
        var zero = Root(0); var zeroPlay = BattleActionModel.PlayCard(zero, new(1, 0));
        Require(zeroPlay.Supported && zeroPlay.State!.Energy == 2 && zeroPlay.State.Spawn.Train.Rooms[0].Units.Single().Health == 98 &&
            zeroPlay.State.Spawn.Train.Context!.CardInstances!.Single(card => card.InstanceId == 1).LastPlayedCost == 0 &&
            BattleActionModel.EnumerateSupportedPlays(zero).Any(action => action.CardInstanceId == 1),
            "Zero-energy X casts were unaffordable or lost their modified effect amount.");
        var won = BattleActionModel.PlayCard(Root(4, terminal: true), new(1, 0));
        Require(won.Supported && won.Outcome == RoomOutcome.BattleWon && won.State!.Energy == 0 &&
            won.State.Spawn.Train.Context!.CardInstances!.Single().LastPlayedCost == 4 && won.State.Spawn.Train.Context.Statistics!.PlayedCosts.Count == 0,
            "Winning X cast did not consume energy, skip later gains or settle paid-card metadata.");
        Require(BattleActionModel.PlayCard(Root(4, "FutureCost"), new(1, 0)).Rejection == ActionRejection.Unsupported,
            "An unknown cost type became an ordinary payment.");
        Require(CardModifierModel.Resolve(root.PlayRules!.Cards[0], root.Spawn.Train.Context!.CardInstances![0]).CostType == "ConsumeRemainingEnergy",
            "Card modifiers lost the cost type.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(BattleActionModel.PlayCard(root, new(1, 0)).State) == JsonSerializer.Serialize(first.State),
            "Parallel X-cost branches differ."));
        Require(JsonSerializer.Serialize(root) == parent, "X payment mutated its root.");
        Console.WriteLine("CARD-COST-CHECKS PASS: current/zero X payments, fixed versus X modifiers, raw/modified scaling, later gains, redrawn next-turn payments, terminal settlement and 32 parallel branches.");
    }
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out var scenario) || scenario.GetString() is not ("x-cost" or "x-cost-lethal")) return;
        int count = 0, zero = 0, positive = 0, terminal = 0, fixedReductions = 0;
        var definitions = new HashSet<string>();
        foreach (var entry in fixture.GetProperty("Actions").EnumerateArray())
        {
            var before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            int id = entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            var old = before.Spawn.Train.Context!.CardInstances!.Single(card => card.InstanceId == id);
            var rule = CardModifierModel.Resolve(before.PlayRules!.Cards.Single(card => card.DataId == old.DataId), old);
            if (rule.CostType != "ConsumeRemainingEnergy") continue;
            var played = actual.Spawn.Train.Context!.CardInstances!.Single(card => card.InstanceId == id);
            Require(played.LastPlayedCost == before.Energy && played.PlayCount == old.PlayCount + 1 &&
                actual.Spawn.Train.Context.Statistics!.PlayedCosts.Count == 0, "Native X payment or settled metadata differs.");
            if (entry.GetProperty("ActualOutcome").GetInt32() == (int)RoomOutcome.BattleWon)
            { Require(actual.Energy == 0, "Post-boss X gain applied."); terminal++; }
            else Require(actual.Energy == rule.Effects.Last(effect => effect.Type == "GainEnergy").Value,
                "Native X payment did not remove all current energy before its later gain.");
            if (before.Energy == 0) zero++; else positive++;
            if (rule.Cost == 0 && before.PlayRules.Cards.Single(card => card.DataId == old.DataId).Cost > 0) fixedReductions++;
            definitions.Add(old.DataId); count++;
        }
        Require(count > 0 && zero > 0 && positive > 0 && definitions.Count == 2 && fixedReductions > 0,
            "Native X-cost trace lacks zero/positive payments, both definitions or ignored fixed reductions.");
        if (scenario.GetString() == "x-cost-lethal") Require(terminal == 1, "Native X-cost fixture lacks a winning cast.");
        foreach (var sample in fixture.GetProperty("DamageScaling").EnumerateArray())
        {
            var before = sample.GetProperty("Before").Deserialize<CombatContext>()!;
            int id = sample.GetProperty("OwnerCardId").GetInt32();
            var paid = before.Statistics!.PlayedCosts.Single(cost => cost.CardId == id);
            Require(before.QueryFrame!.Energy == 0 && before.FindCardForChecks(id).LastPlayedCost == paid.Cost,
                "Native scaling did not see paid-card metadata and depleted current energy.");
        }
        Console.WriteLine($"NATIVE-CARD-COST-CHECKS PASS: {count} X casts, {zero} zero/{positive} positive payments, {fixedReductions} ignored fixed reductions and {terminal} terminal casts.");
    }
    private static CardInstanceState FindCardForChecks(this CombatContext context, int id) =>
        context.CardInstances!.FirstOrDefault(card => card.InstanceId == id) ?? context.CardRegistry!.Single(card => card.InstanceId == id);
    private static void Require(bool pass, string message) { if (!pass) throw new InvalidOperationException(message); }
}
