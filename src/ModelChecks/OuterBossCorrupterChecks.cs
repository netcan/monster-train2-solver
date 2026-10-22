using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OuterBossCorrupterChecks
{
    internal static void Native(FixtureValue battle, FixtureValue birth, FixtureValue phases, FixtureValue controller)
    {
        const string boss = "Boss_Level8_Corrupter";
        FixtureValue control = battle.GetProperty("OriginalOuterBossRoute");
        Require(control.GetProperty("Kind").GetString() == "SelectOriginalOuterBossScenario" &&
            control.GetProperty("Distance").GetInt32() == 7 && control.GetProperty("Boss").GetString() == boss &&
            control.GetProperty("ScenarioAssetsUnchanged").GetBoolean() &&
            control.GetProperty("RngBefore").ContentEquals(control.GetProperty("RngAfter")),
            "Original Corrupter scenario control or unchanged RNG provenance is missing.");
        OuterBossBirthChecks.Native(birth);
        OuterBossPhaseChecks.Native(phases, boss, earlyDefeat: true);
        BossActionControllerChecks.Native(controller, boss, minimumOperations: 6);
        OuterBossTurnChecks.Deployment(battle, boss, minimumTurns: 3);
        Require(phases.GetProperty("QueueControls").GetArrayLength() == 0 &&
            battle.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing settled control-free boss phases.");
        CombatUnit original = birth.GetProperty("Births")[0].GetProperty("Before").Deserialize<OuterBossCreationState>()!.CreatedUnit;
        Require(original.AssetKey == boss && original.BaseAttack == 20 && original.MaxHealth == 2800 && original.Triggers.Count == 2 &&
            original.Triggers[0].Kind == "OnAttacking" && original.Triggers[0].Effects.Single().Type == "CardEffectMultiplyStatusEffect" &&
            original.Triggers[1].Kind == "OnTurnBegin" && original.Triggers[1].Effects.Single().Action!.Statuses.Single().Stacks == 2,
            "The original Corrupter attack, health or trigger payload changed.");
        int multiplied = 0, positive = 0, dying = 0;
        foreach (FixtureValue record in battle.GetProperty("TriggeredStatuses").EnumerateArray())
        {
            CombatEffect effect = record.GetProperty("Effect").Deserialize<CombatEffect>()!;
            if (effect.Type != "CardEffectMultiplyStatusEffect") continue;
            Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("Kind").GetString() == "OnAttacking" &&
                record.GetProperty("Interactions").GetArrayLength() == 0, "Incomplete native corruption multiplication.");
            RoomCombatState before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = record.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            CombatUnit[] retained = record.GetProperty("BeforeUnits").Deserialize<CombatUnit[]>()!;
            CombatUnit[] after = record.GetProperty("ActualUnits").Deserialize<CombatUnit[]>()!;
            int actor = record.GetProperty("ActorId").GetInt32();
            int[] targets = record.GetProperty("Targets").Deserialize<int[]>()!;
            var scope = new RoomCombatState(before.RoomIndex, before.Deployment,
                before.Units.Concat(retained.Where(unit => before.Units.All(live => live.Id != unit.Id))).ToArray(),
                before.ExternalInteractions, before.Context, before.Preview, before.ActiveCompanionBossIds);
            string immutable = JsonSerializer.Serialize(scope, ModelJson.Options);
            RoomCombatResult result = TriggeredStatusModel.Apply(scope, actor, effect, targets, record.GetProperty("SourceCardId").GetInt32());
            Require(result.Supported, "Native corruption multiplication unsupported: " + result.UnsupportedReason);
            // The effect helper receives retained victims in its local scope.
            // Project back to the captured pre-effect room membership without
            // turning a dead override target into a physical room occupant.
            var physicalIds = before.Units.Select(unit => unit.Id).ToHashSet();
            RoomCombatState projected = EnchantmentWorldModel.Sync(new RoomCombatState(before.RoomIndex, before.Deployment,
                result.State!.Units.Where(unit => physicalIds.Contains(unit.Id)).ToArray(), before.ExternalInteractions,
                result.State.Context, before.Preview, before.ActiveCompanionBossIds),
                result.State.Units.Where(unit => !physicalIds.Contains(unit.Id)).ToArray());
            string? contextDiff = ModelJson.Difference(JsonSerializer.Serialize(projected.Context, ModelJson.Options),
                JsonSerializer.Serialize(actual.Context, ModelJson.Options));
            Require(contextDiff == null, "Multiplication context/RNG differs at " + record.GetProperty("Sequence").GetInt32() + ": " + contextDiff);
            foreach (CombatUnit unit in after)
            {
                string? diff = ModelJson.Difference(JsonSerializer.Serialize(result.State.Units.Single(item => item.Id == unit.Id), ModelJson.Options),
                    JsonSerializer.Serialize(unit, ModelJson.Options));
                Require(diff == null, "Multiplication retained actor/target differs: " + diff);
            }
            positive += after.Count(unit => targets.Contains(unit.Id) && (unit.Status("corruption")?.Stacks ?? 0) >
                (retained.Single(item => item.Id == unit.Id).Status("corruption")?.Stacks ?? 0));
            dying += retained.Count(unit => targets.Contains(unit.Id) && unit.Health <= 0);
            Require(JsonSerializer.Serialize(scope, ModelJson.Options) == immutable, "Multiplication mutated its captured root.");
            multiplied++;
        }
        Require(multiplied > 0 && positive > 0, "Missing an actual native attack that increases existing corruption.");
        Console.WriteLine($"OUTER-BOSS-CORRUPTER PASS original scenario control, full birth/controller/phases and paid chain; {multiplied} native multiplication effects/{positive} increases/{dying} retained dying targets; no natural-route or player acceptance claimed.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        const string boss = "Boss_Level8_Corrupter";
        OuterBossTurnChecks.Deployment(replay, boss, minimumTurns: 3);
        RollingAdviceChecks.Native(replay);
        FixtureValue[] actions = source.GetProperty("Actions").EnumerateArray().ToArray();
        FixtureValue[] turns = source.GetProperty("Turns").EnumerateArray().ToArray();
        FixtureValue[] records = replay.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(records.Length == actions.Length + turns.Length && !source.GetProperty("NativeWon").GetBoolean() &&
            !replay.GetProperty("NativeWon").GetBoolean(), "Frozen original Corrupter input count or native loss differs.");
        int play = 0, end = 0;
        foreach (FixtureValue record in records)
        {
            bool isEnd = record.GetProperty("IsEndTurn").GetBoolean();
            FixtureValue native = isEnd ? turns[end++] : actions[play++];
            Require(record.GetProperty("Mode").GetString() == "replay" && record.GetProperty("Nodes").GetInt32() == 0 &&
                (isEnd || record.GetProperty("Action").ContentEquals(native.GetProperty("Action"))), "Frozen native input changed or performed new search.");
            BattleTurnState actual = native.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            BattleTurnState fresh = record.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            RoomOutcome outcome = record.GetProperty("Outcome").Deserialize<RoomOutcome>();
            bool terminal = outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
            Require(BattleTurnChecks.Comparable(actual, terminal) == BattleTurnChecks.Comparable(fresh, terminal),
                "Source/fresh complete native successor or RNG differs at input " + (play + end - 1));
        }
        Require(play == actions.Length && end == turns.Length, "Frozen native replay missed inputs.");
        Console.WriteLine($"OUTER-BOSS-CORRUPTER-PAIR PASS: {records.Length} exact original/fresh inputs and complete native successors/RNG, zero replay search nodes; native loss.");
    }

    internal static void Relentless(FixtureValue battle, FixtureValue phases)
    {
        const string boss = "Boss_Level8_Corrupter";
        OuterBossPhaseChecks.Native(phases, boss);
        Require(phases.GetProperty("ForcedSpawnQueueEmpty").GetBoolean() &&
            phases.GetProperty("QueueControls").GetArrayLength() == 2 &&
            battle.GetProperty("OriginalOuterBossRoute").GetProperty("Boss").GetString() == boss &&
            battle.GetProperty("CaptureFailures").GetInt32() == 0 && battle.GetProperty("Mismatches").GetInt32() == 1 &&
            battle.GetProperty("Unsupported").GetInt32() == 0 && battle.GetProperty("Pending").GetInt32() == 0 &&
            battle.GetProperty("NativeWon").GetBoolean() && battle.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Requires only the explicitly recorded mid-turn queue/Pyre control mismatch and a settled controlled win.");
        FixtureValue[] turns = battle.GetProperty("Turns").EnumerateArray().ToArray();
        FixtureValue[] actions = battle.GetProperty("Actions").EnumerateArray().ToArray();
        Require(turns.Length == 5 && turns[1].GetProperty("Difference").ValueKind == FixtureKind.String &&
            turns.Where((_, index) => index != 1).All(turn => turn.GetProperty("Difference").ValueKind == FixtureKind.Null),
            "The expected control mismatch moved or hid other native differences.");
        BattleTurnState controlBefore = turns[1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState controlAfter = turns[1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(controlBefore.Spawn.Turn == 1 && controlBefore.Spawn.Phase < controlBefore.Spawn.Waves.Count &&
            controlAfter.Spawn.Turn == 2 && controlAfter.Spawn.Phase == controlAfter.Spawn.Waves.Count &&
            controlAfter.Spawn.OuterBoss?.Phase == "Relentless" &&
            controlBefore.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).MaxHealth == 80 &&
            controlAfter.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).MaxHealth == 10000,
            "Missing the explicit native control boundary; it must not be labeled a natural victory.");
        FixtureValue[] remaining = actions.Where(row => row.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Turn >= 2).ToArray();
        BattleTurnState root = (remaining.Length > 0 ? remaining[0] : turns[2]).GetProperty("Before").Deserialize<BattleTurnState>()!;
        string immutable = JsonSerializer.Serialize(root, ModelJson.Options);
        void Replay()
        {
            BattleTurnState current = root;
            int action = 0;
            foreach (FixtureValue turn in turns.Skip(2))
            {
                int number = turn.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Turn;
                while (action < remaining.Length && remaining[action].GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Turn == number)
                {
                    FixtureValue row = remaining[action++];
                    BattleActionResult played = BattleActionModel.PlayCard(current, row.GetProperty("Action").Deserialize<PlayCardAction>()!);
                    Require(played.Supported && BattleTurnChecks.Comparable(played.State!, false) ==
                        BattleTurnChecks.Comparable(row.GetProperty("Actual").Deserialize<BattleTurnState>()!, false),
                        "Post-control paid action differs: " + played.Reason);
                    current = played.State!;
                }
                BattleTurnResult result = BattleTurnModel.EndTurn(current);
                Require(result.Supported && result.Outcome == turn.GetProperty("ActualOutcome").Deserialize<RoomOutcome>() &&
                    BattleTurnChecks.Comparable(result.State!, result.Outcome == RoomOutcome.BattleWon) ==
                    BattleTurnChecks.Comparable(turn.GetProperty("Actual").Deserialize<BattleTurnState>()!, result.Outcome == RoomOutcome.BattleWon),
                    "Post-control frozen EndTurn differs: " + result.UnsupportedReason);
                current = result.State!;
            }
            Require(action == remaining.Length && current.Spawn.OuterBoss == null &&
                current.PlayRules!.Rooms.Count(room => !room.IsPyre && !room.Enabled) == 3 &&
                current.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 8745 &&
                JsonSerializer.Serialize(root, ModelJson.Options) == immutable,
                "Controlled route lost its inputs, immutable root, original Pyre outcome or three destroyed rooms.");
        }
        Replay();
        Parallel.For(0, 4, _ => Replay());
        Console.WriteLine($"OUTER-BOSS-CORRUPTER-RELENTLESS PASS: eight native phases, three destroyed rooms, {remaining.Length} paid actions/three EndTurns from one post-control root and four concurrent branches; Pyre8745; explicit queue/Pyre controls, no natural victory claimed.");
    }
}
