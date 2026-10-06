using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class DynamicStatisticChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(241);
        var frame = new StatisticQueryFrame(3, true, 2, 7, 9, 1, 0);
        CardPileState[] piles = [new("Standby", [new(3, "unit")]), new("Exhausted", []),
            new("Purged", []), new("Eaten", []), new("DiscardBuffer", [])];
        var instances = new[] { CardInstanceState.Empty(1, "spell"), CardInstanceState.Empty(2, "spell"),
            new CardInstanceState(3, "unit", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
                damageScalingTraits: [new(new("TurnCount"), 1, 1, true), new(new("MoonPhase"), 1, 1, true),
                    new(new("EnergyRemainingEndOfTurn"), 1, 1, true), new(new("UnmodifiedPlayedCost", variableCost: true), 1, 1, true)]) };
        var statistics = new BattleStatistics([], [], [], [], [], [], [], 0, 0, 0, 19, 0, [1, 2, 3], [1, 2, 3]);
        CombatContext Context(StatisticQueryFrame? current) => new(new([new(1, "spell"), new(2, "spell")], [], [], rng, 0, []),
            rng, 36, 4, 10, statistics: statistics, cardInstances: instances, otherPiles: piles, queryFrame: current);
        var context = Context(frame);
        var attacker = new CombatUnit(11, "unit", CombatTeam.Player, 1, 50, 50, true, false, false, [], spawnerCardId: 3);
        var enemy = new CombatUnit(12, "enemy", CombatTeam.Enemy, 0, 200, 200, true, false, false, []);
        var pyre = new CombatUnit(10, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, []);
        var train = new TrainCombatState(Enumerable.Range(0, 4).Select(index => new RoomCombatState(index, false,
            index == 0 ? [enemy, attacker] : index == 3 ? [pyre] : [], [], context)).ToArray(), [new(12, 1, false, false)], 7, context);
        var spawn = new EnemySpawnState(train, [new([new([])]), new([new([])]), new([new([])])], [-1, -1, -1], 0,
            false, rng, 13, [], 0, false, 2, 1, 2, []);
        var rules = new BattlePlayRules([new(0, 5, 7, true, false, false)], [new("spell", "spell", 1, "Null", "Discard", null, [])]);
        var root = new BattleTurnState(spawn, 3, 3, 5, 7, 9, "New", [new("Spawning", 241, rng)], piles, [], rules);
        string parent = JsonSerializer.Serialize(root);
        Require(StatisticQueryModel.Evaluate(context, new("Gold")).Value == 19 &&
            StatisticQueryModel.Evaluate(Context(frame.With(runningCombat: false)), new("Gold")).Value == 36,
            "Combat gold failed to select the turn-start snapshot or stopped-loop balance.");
        Require(StatisticQueryModel.Evaluate(context, new("UnmodifiedPlayedCost", variableCost: true), 1).Value == 3 &&
            StatisticQueryModel.Evaluate(context, new("UnmodifiedPlayedCost", variableCost: true), 1, frame.With(energy: 8)).Value == 8,
            "Current energy fallback or explicit frame precedence differs.");
        BattleActionResult played = BattleActionModel.PlayCard(root, new(1, 0));
        Require(played.Supported && played.State!.Spawn.Train.Context!.QueryFrame!.Energy == 2,
            "Card payment failed to advance shared energy: " + played.Reason);
        BattleTurnResult next = BattleTurnModel.EndTurn(played.State!);
        Require(next.Supported, "Dynamic EndTurn rejected: " + next.UnsupportedReason);
        CombatContext nextContext = next.State!.Spawn.Train.Context!;
        Require(next.State.Spawn.Turn == 3 && next.State.MoonPhase == "Full" && nextContext.QueryFrame!.Turn == 3 &&
            nextContext.QueryFrame.MoonPhase == 2 && nextContext.QueryFrame.Energy == 3 &&
            nextContext.Statistics!.EnergyRemainingEndOfTurn == 0 && nextContext.QueryFrame.ForgePoints == 7 &&
            nextContext.QueryFrame.DragonsHoard == 9 && next.State.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 12).Health == 194,
            "Current-turn damage, pre-removal energy, turn rollover, moon flags or replenishment differ: " +
            JsonSerializer.Serialize(new { nextContext.QueryFrame, nextContext.Statistics!.EnergyRemainingEndOfTurn,
                next.State.MoonPhase, next.State.Spawn.Turn, Units = next.State.Spawn.Train.Rooms[0].Units.Select(unit => new { unit.Id, unit.Health }) }));
        BattleTurnResult following = BattleTurnModel.EndTurn(next.State);
        Require(following.Supported && following.State!.Spawn.Train.Context!.QueryFrame!.Turn == 4 &&
            following.State.Spawn.Train.Context!.QueryFrame!.MoonPhase == 1 &&
            following.State.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 12).Health == 185,
            "A second independent turn reused past-root statistic inputs.");
        foreach (StatisticQueryFrame bad in new[] { frame.With(energy: 9), frame.With(turn: 9), frame.With(moonPhase: 2),
            frame.With(forgePoints: 9), frame.With(dragonsHoard: 11), frame.With(runningCombat: false), frame.With(activeBattle: false) })
        {
            CombatContext badContext = Context(bad);
            var badTrain = new TrainCombatState(train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units, room.ExternalInteractions, badContext)).ToArray(), train.Movement, train.EnemySlotsPerRoom, badContext);
            var badSpawn = new EnemySpawnState(badTrain, spawn.Waves, spawn.SelectedGroups, spawn.Phase, spawn.Looping, spawn.Rng,
                spawn.NextUnitId, spawn.Treasures, spawn.TreasuresRemaining, spawn.TreasureEnabled, spawn.FirstTreasureTurn, spawn.FirstTreasureRoom, spawn.Turn, []);
            var badState = new BattleTurnState(badSpawn, 3, 3, 5, 7, 9, "New", root.RngStreams, piles, [], rules);
            Require(!BattleActionModel.PlayCard(badState, new(1, 0)).Supported && !BattleTurnModel.EndTurn(badState).Supported,
                "Contradictory dynamic inputs produced a usable search child.");
        }
        string expected = JsonSerializer.Serialize(following.State);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(BattleTurnModel.EndTurn(
            BattleTurnModel.EndTurn(BattleActionModel.PlayCard(root, new(1, 0)).State!).State!).State) == expected,
            "Parallel branches shared mutable dynamic statistic inputs."));
        Require(JsonSerializer.Serialize(root) == parent && context.QueryFrame!.Energy == 3 && context.QueryFrame.Turn == 2,
            "A child changed its parent's dynamic inputs.");
        Terminal(rng);
        Console.WriteLine("DYNAMIC-STATISTIC-CHECKS PASS: payment, current-turn damage, energy removal/replenishment, multi-turn moon flags, gold-loop selection, inconsistent-input rejection and 32 parallel branches.");
    }

    private static void Terminal(UnityRng rng)
    {
        CardPileState[] piles = [new("Standby", []), new("DiscardBuffer", []), new("Exhausted", []), new("Eaten", []), new("Purged", [])];
        var card = new CardInstanceState(1, "spell", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            damageScalingTraits: [new(new("Gold"), 1, 1, true)]);
        var context = new CombatContext(new([new(1, "spell")], [], [], rng, 0, []), rng, 36, 2, 10,
            statistics: new([], [], [], [], [], [], [], 0, 0, 0, 19, 0, [1], [1]), cardInstances: [card],
            otherPiles: piles, queryFrame: new(3, true, 2, 7, 9, 2, 0));
        var boss = new CombatUnit(10, "boss", CombatTeam.Enemy, 1, 1, 1, true, false, true, [], isBoss: true);
        var pyre = new CombatUnit(11, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, []);
        var train = new TrainCombatState([new(0, false, [boss], [], context), new(1, false, [pyre], [], context)], [new(10, 1, false, false)], 7, context);
        var spawn = new EnemySpawnState(train, [], [], 0, false, rng, 12, [], 0, false, 0, 0, 2, []);
        var root = new BattleTurnState(spawn, 3, 3, 5, 7, 9, "Full", [], piles, [], new(
            [new(0, 5, 7, true, false, false)], [new("spell", "spell", 1, "Spell", "Discard", null, [],
                [new("Damage", "DropTargetCharacter", 1, true, false, [])])]));
        BattleActionResult result = BattleActionModel.PlayCard(root, new(1, 0, targetUnitId: 10));
        Require(result.Supported && result.Outcome == RoomOutcome.BattleWon, "Dynamic terminal spell failed: " + result.Reason);
        CombatContext settled = result.State!.Spawn.Train.Context!;
        Require(DamageScalingModel.Apply(context, 1, 1, 1).Damage == 20 && settled.QueryFrame!.RunningCombat == false &&
            settled.QueryFrame.Energy == 2 && settled.QueryFrame.Turn == 2 && settled.QueryFrame.MoonPhase == 2 &&
            settled.QueryFrame.ActiveBattle && StatisticQueryModel.Evaluate(settled, new("Gold")).Value == 36,
            "Terminal effects lost live-loop gold, stopped at a wrong boundary, or advanced turn/moon before settlement: " +
                JsonSerializer.Serialize(new { settled.QueryFrame,
                    Gold = StatisticQueryModel.Evaluate(settled, new("Gold")).Value }));
        Require(context.QueryFrame!.RunningCombat == true && context.QueryFrame.Energy == 3,
            "Terminal settlement changed its parent's combat-loop state.");

        var helplessBoss = new CombatUnit(10, "boss", CombatTeam.Enemy, 0, 1, 1, false, false, true, [], isBoss: true);
        var helplessPyre = new CombatUnit(11, "pyre", CombatTeam.Player, 0, 80, 80, false, true, false, [new("relentless", 1)]);
        var stuckTrain = new TrainCombatState([new(0, false, [], [], context), new(1, false, [helplessBoss, helplessPyre], [], context)],
            [new(10, 1, false, false)], 7, context);
        var stuckSpawn = new EnemySpawnState(stuckTrain, [], [], 0, false, rng, 12, [], 0, false, 0, 0, 2, []);
        var stuckRoot = new BattleTurnState(stuckSpawn, 3, 3, 5, 7, 9, "Full", [new("Spawning", 241, rng)], piles, [], root.PlayRules);
        BattleTurnResult stuck = BattleTurnModel.EndTurn(stuckRoot);
        Require(stuck.Supported && stuck.Outcome == RoomOutcome.Stalemate && stuck.State!.Spawn.Train.Context!.QueryFrame!.RunningCombat == true,
            "An exact room cycle invented a stopped native combat loop: " + stuck.UnsupportedReason);
    }

    internal static void Native(JsonElement fixture)
    {
        if (fixture.GetProperty("Schema").GetInt32() < 23) return;
        int decisions = 0;
        foreach (string collection in new[] { "Actions", "Turns" })
        foreach (JsonElement record in fixture.GetProperty(collection).EnumerateArray())
        foreach (string side in new[] { "Before", "Actual" })
        {
            BattleTurnState state = record.GetProperty(side).Deserialize<BattleTurnState>(ModelJson.Options)!;
            StatisticQueryFrame frame = state.Spawn.Train.Context!.QueryFrame!;
            bool terminal = side == "Actual" && record.GetProperty("ActualOutcome").GetInt32() is
                (int)RoomOutcome.BattleWon or (int)RoomOutcome.PlayerDefeated;
            Require(frame != null && frame.Energy == state.Energy && frame.Turn == state.Spawn.Turn &&
                frame.ForgePoints == state.ForgePoints && frame.DragonsHoard == state.DragonsHoard &&
                frame.MoonPhase == (state.MoonPhase == "New" ? 1 : 2) && frame.RunningCombat == !terminal && frame.ActiveBattle,
                "Native decision dynamic inputs or terminal loop state disagree.");
            Require(state.Spawn.Train.Rooms.All(room => JsonSerializer.Serialize(room.Context!.QueryFrame) == JsonSerializer.Serialize(frame)),
                "A native decision room omitted its current statistic inputs.");
            decisions++;
        }
        if (fixture.GetProperty("ModifierScenario").GetString() == "dynamic-statistics")
        {
            var moons = new HashSet<int>(); var turns = new HashSet<int>();
            bool spell = false, endTurn = false, differingGold = false, unusedEnergy = false;
            foreach (JsonElement sample in fixture.GetProperty("DamageScaling").EnumerateArray())
            {
                CombatContext before = sample.GetProperty("Before").Deserialize<CombatContext>(ModelJson.Options)!;
                StatisticQueryFrame frame = before.QueryFrame!;
                Require(frame != null && frame.ForgePoints == 7 && frame.DragonsHoard == 9 && frame.PyreResurrectionCount == 0 &&
                    frame.RunningCombat == true && frame.ActiveBattle, "Native resource fixture inputs are incomplete.");
                moons.Add(frame!.MoonPhase!.Value); turns.Add(frame.Turn!.Value);
                int actionIndex = sample.GetProperty("ActionIndex").GetInt32();
                if (actionIndex >= 0)
                {
                    int energy = fixture.GetProperty("Actions")[actionIndex].GetProperty("Actual").GetProperty("Energy").GetInt32();
                    Require(frame.Energy == energy, "A live card effect queried pre-payment energy.");
                    spell = true;
                }
                else { Require(frame.Energy == 0, "Native combat queried energy before turn-end removal."); endTurn = true; }
                differingGold |= before.Gold != before.Statistics!.GoldStartOfThisTurn;
                unusedEnergy |= before.Statistics!.EnergyRemainingEndOfTurn > 0;
            }
            Require(spell && endTurn && differingGold && unusedEnergy && moons.SetEquals([1, 2]) && turns.Count >= 3,
                "Dynamic oracle lacks multiple turns, moon flags, unused energy, differing gold or spell/combat boundaries.");
        }
        Console.WriteLine($"NATIVE-DYNAMIC-STATISTIC-CHECKS PASS: {decisions} decision boundaries, shared current resources and stopped terminal loop.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
