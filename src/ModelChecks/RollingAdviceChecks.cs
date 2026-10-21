using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RollingAdviceChecks
{
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
    private static BattleTurnState State(FixtureValue value) => value.Deserialize<BattleTurnState>()!;
    private static bool Terminal(RoomOutcome outcome) => outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
    private static string Key(BattleTurnState state, bool terminal = false) => BattleTurnChecks.Comparable(state, terminal);
    private static string[] ActualBosses(FixtureValue fixture) => fixture.GetProperty("RollingDecisions").EnumerateArray()
        .SelectMany(record => new[] { State(record.GetProperty("Root")), State(record.GetProperty("Actual")) })
        .SelectMany(state => state.Spawn.Train.Rooms.Where(room => !room.Preview).SelectMany(room => room.Units))
        .Where(unit => unit.IsBoss == true).Select(unit => unit.AssetKey).Distinct().ToArray();
    private static void Accepted(FixtureValue fixture)
    {
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(counter).GetInt32() == 0, "Rolling native capture has nonzero " + counter);
        Require(fixture.GetProperty("TerminalEffectsSettled").GetBoolean(), "Rolling battle did not settle.");
        Require(string.IsNullOrEmpty(fixture.GetProperty("ModifierScenario").GetString()), "Rolling encounter was modified.");
    }
    internal static void Native(FixtureValue fixture)
    {
        Accepted(fixture);
        FixtureValue[] records = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(records.Length > 1, "Missing complete rolling decision sequence.");
        BattleTurnState? previous = null;
        long generation = -1;
        int cards = 0, turns = 0;
        RoomOutcome outcome = RoomOutcome.Exchanged;
        foreach (FixtureValue record in records)
        {
            BattleTurnState root = State(record.GetProperty("Root"));
            if (previous != null) Require(Key(previous) == Key(root), "Rolling decisions do not form one native chain.");
            else Require(root.Spawn.Turn == 0, "Rolling advice did not start at the deployment turn.");
            Require(record.GetProperty("Difference").ValueKind == FixtureKind.Null, "Rolling native mismatch.");
            bool end = record.GetProperty("IsEndTurn").GetBoolean();
            Require(end == (record.GetProperty("Action").ValueKind == FixtureKind.Null), "Invalid rolling input.");
            PlayCardAction? action = end ? null : record.GetProperty("Action").Deserialize<PlayCardAction>()!;
            BattleTurnState next = BattlePlannerChecks.Replay(root, new[] { new BattleSearchStep(action, end) }, out outcome);
            Require(outcome == record.GetProperty("Outcome").Deserialize<RoomOutcome>() &&
                Key(next, Terminal(outcome)) == Key(State(record.GetProperty("Expected")), Terminal(outcome)) &&
                Key(next, Terminal(outcome)) == Key(State(record.GetProperty("Actual")), Terminal(outcome)), "Rolling native successor differs.");
            if (end) turns++; else cards++;

            FixtureValue[] plan = record.GetProperty("PlanSteps").EnumerateArray().ToArray();
            Require(plan.Length > 0 && plan[0].GetProperty("IsEndTurn").GetBoolean() == end &&
                plan[0].GetProperty("Action").ContentEquals(record.GetProperty("Action")), "Driver did not execute the latest plan's first step.");
            BattleSearchStep[] steps = plan.Select(step => new BattleSearchStep(
                step.GetProperty("Action").ValueKind == FixtureKind.Null ? null : step.GetProperty("Action").Deserialize<PlayCardAction>(),
                step.GetProperty("IsEndTurn").GetBoolean())).ToArray();
            BattleTurnState planned = BattlePlannerChecks.Replay(root, steps, out RoomOutcome plannedOutcome);
            double score = record.GetProperty("Score").GetDouble();
            Require(plannedOutcome == record.GetProperty("PlannedOutcome").Deserialize<RoomOutcome>() &&
                Key(planned, Terminal(plannedOutcome)) == Key(State(record.GetProperty("Planned")), Terminal(plannedOutcome)) &&
                BattlePlanner.Evaluate(planned, plannedOutcome) == score, "Published continuation does not reproduce its forecast.");
            if (record.GetProperty("PolicyBaselineScore").ValueKind == FixtureKind.Number)
                Require(score <= record.GetProperty("PolicyBaselineScore").GetDouble(), "Rolling advice lost its completed policy seed.");
            if (record.GetProperty("Mode").GetString() == "rolling")
            {
                long currentGeneration = record.GetProperty("Generation").GetInt64();
                Require(currentGeneration > generation, "Advice was reused after a native action."); generation = currentGeneration;
                FixtureValue timing = record.GetProperty("Timing");
                Require(timing.GetProperty("Generation").GetInt64() == generation, "Advice timing belongs to another publication.");
                double sum = 0;
                foreach (string field in new[] { "CaptureMs", "QueueMs", "SearchMs", "VerificationMs" })
                { double duration = timing.GetProperty(field).GetDouble(); Require(double.IsFinite(duration) && duration >= 0, "Invalid advice latency."); sum += duration; }
                Require(timing.GetProperty("EndToEndMs").GetDouble() + 1 >= sum, "End-to-end latency omitted a measured stage.");
            }
            else Require(record.GetProperty("Mode").GetString() == "replay" && record.GetProperty("Nodes").GetInt32() == 0,
                "Frozen rolling replay searched again.");
            previous = State(record.GetProperty("Actual"));
        }
        Require(Terminal(outcome) && fixture.GetProperty("NativeWon").GetBoolean() == (outcome == RoomOutcome.BattleWon),
            "Rolling sequence did not cover the whole native battle.");
        Require(cards == fixture.GetProperty("Actions").GetArrayLength() && turns == fixture.GetProperty("Turns").GetArrayLength(),
            "Some native inputs bypassed rolling advice.");
        Console.WriteLine($"ROLLING-ADVICE PASS mode={records[0].GetProperty("Mode").GetString()} decisions={records.Length} cards={cards} turns={turns} outcome={outcome}");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay, FixtureValue baseline)
    {
        Native(source); Native(replay); Accepted(baseline);
        FixtureValue[] first = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        FixtureValue[] second = replay.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(first.Length == second.Length, "Fresh rolling replay has a different decision count.");
        for (int index = 0; index < first.Length; index++)
        {
            Require(first[index].GetProperty("Mode").GetString() == "rolling" && second[index].GetProperty("Mode").GetString() == "replay" &&
                first[index].GetProperty("Action").ContentEquals(second[index].GetProperty("Action")) &&
                first[index].GetProperty("IsEndTurn").ContentEquals(second[index].GetProperty("IsEndTurn")), "Fresh rolling replay changed an input.");
            bool terminal = index == first.Length - 1;
            foreach (string field in new[] { "Root", "Expected", "Actual" })
                Require(Key(State(first[index].GetProperty(field)), terminal) == Key(State(second[index].GetProperty(field)), terminal),
                    "Fresh rolling replay changed full state or RNG at " + field);
        }
        // A terminal pre-boss loss is still a useful full-battle mechanic control.
        // Suite counts distinct bosses only from actual decision-room occupants.
        BattleAdviceChecks.Native(source, requireBoss: false);
        Require(baseline.GetProperty("Policy").GetString() == "units-spells-and-junk", "Wrong fixed-policy baseline.");
        Require(source.GetProperty("BattleDistance").GetInt32() == baseline.GetProperty("BattleDistance").GetInt32(), "Baseline encounter distance differs.");
        Require(source.GetProperty("BattleDistance").GetInt32() == replay.GetProperty("BattleDistance").GetInt32(), "Replay encounter distance differs.");
        FixtureValue[] baselineActions = baseline.GetProperty("Actions").EnumerateArray().ToArray();
        FixtureValue[] baselineTurns = baseline.GetProperty("Turns").EnumerateArray().ToArray();
        BattleTurnState baselineRoot = State((baselineActions.Length > 0 ? baselineActions[0] : baselineTurns[0]).GetProperty("Before"));
        Require(Key(State(first[0].GetProperty("Root"))) == Key(baselineRoot), "Fixed-policy baseline starts from a different battle state.");
        BattleTurnState policyState = baselineRoot;
        int actionIndex = 0;
        RoomOutcome policyOutcome = RoomOutcome.Exchanged;
        void Follow(FixtureValue record, BattleSearchStep step)
        {
            Require(Key(policyState) == Key(State(record.GetProperty("Before"))), "Fixed policy has a discontinuous native chain.");
            policyState = BattlePlannerChecks.Replay(policyState, new[] { step }, out policyOutcome);
            Require(policyOutcome == record.GetProperty("ActualOutcome").Deserialize<RoomOutcome>() &&
                Key(policyState, Terminal(policyOutcome)) == Key(State(record.GetProperty("Actual")), Terminal(policyOutcome)),
                "Completed fixed policy does not reproduce its native successor.");
        }
        foreach (FixtureValue turn in baselineTurns)
        {
            while (actionIndex < baselineActions.Length &&
                State(baselineActions[actionIndex].GetProperty("Before")).Spawn.Turn == policyState.Spawn.Turn)
            {
                FixtureValue action = baselineActions[actionIndex++];
                Follow(action, new BattleSearchStep(action.GetProperty("Action").Deserialize<PlayCardAction>(), false));
            }
            Follow(turn, new BattleSearchStep(null, true));
        }
        Require(actionIndex == baselineActions.Length && Terminal(policyOutcome), "Fixed policy did not finish all recorded inputs.");
        FixtureValue baselineTerminal = baselineTurns.Last().GetProperty("Actual");
        RoomOutcome baselineOutcome = baseline.GetProperty("NativeWon").GetBoolean() ? RoomOutcome.BattleWon : RoomOutcome.PlayerDefeated;
        double baselineScore = BattlePlanner.Evaluate(State(baselineTerminal), baselineOutcome);
        RoomOutcome actualOutcome = first.Last().GetProperty("Outcome").Deserialize<RoomOutcome>();
        double actualScore = BattlePlanner.Evaluate(State(first.Last().GetProperty("Actual")), actualOutcome);
        Require(actualScore <= baselineScore, "Rolling advisor lost to the completed fixed policy.");
        Console.WriteLine($"ROLLING-ADVICE-PAIR PASS: all {first.Length} decisions reproduced independently; terminal score={actualScore}, completed native policy={baselineScore}.");
    }
    internal static void Suite(FixtureValue[] fixtures)
    {
        Require(fixtures.Length >= 9 && fixtures.Length % 3 == 0, "Need at least three source/replay/policy triples.");
        var bosses = new HashSet<string>();
        var buffTargets = new HashSet<string>();
        bool laterRing = false;
        bool retiredAura = false;
        for (int index = 0; index < fixtures.Length; index += 3)
        {
            Pair(fixtures[index], fixtures[index + 1], fixtures[index + 2]);
            string[] actualBosses = ActualBosses(fixtures[index]);
            bosses.UnionWith(actualBosses);
            laterRing |= actualBosses.Length > 0 && fixtures[index].GetProperty("BattleDistance").GetInt32() >= 4;
            retiredAura |= fixtures[index].GetProperty("RollingDecisions").EnumerateArray()
                .Select(record => State(record.GetProperty("Actual")).Spawn.Train.Context?.Enchantments)
                .Any(world => world?.AutomaticLifecycle == true && world.RetainedUnits.Any(actor =>
                    actor.Unit.DeathState?.IsDestroyed == true && actor.Unit.Triggers.SelectMany(trigger => trigger.Effects)
                        .Any(effect => effect.Enchantment is { Bound: false })));
            foreach (FixtureValue fixture in new[] { fixtures[index], fixtures[index + 2] })
            {
                foreach (FixtureValue stage in fixture.GetProperty("Stages").EnumerateArray())
                    ObserveBuffs(stage.GetProperty("Before").Deserialize<RoomCombatState>()!,
                        stage.GetProperty("Actual").Deserialize<RoomCombatState>()!, buffTargets);
                foreach (FixtureValue action in fixture.GetProperty("Actions").EnumerateArray())
                {
                    BattleTurnState before = State(action.GetProperty("Before")), after = State(action.GetProperty("Actual"));
                    foreach (RoomCombatState room in before.Spawn.Train.Rooms)
                        ObserveBuffs(room, after.Spawn.Train.Rooms.Single(next => next.RoomIndex == room.RoomIndex), buffTargets);
                }
            }
        }
        Require(bosses.Count >= 3 && laterRing, "Need three distinct actual bosses including a middle/later ring.");
        Require(buffTargets.SetEquals(new[] { "Self", "Room" }), "Missing positive native Harvest/Incant attack-buff witnesses.");
        Require(retiredAura, "Missing native destruction and unbinding of a retained natural aura.");
        Console.WriteLine("ROLLING-ADVICE-SUITE PASS actual bosses=" + string.Join(",", bosses.Order()));
    }
    private static void ObserveBuffs(RoomCombatState before, RoomCombatState after, HashSet<string> targets)
    {
        foreach (CombatUnit caster in before.Units.Where(unit => unit.Team == CombatTeam.Enemy))
            foreach (CombatTrigger trigger in caster.Triggers)
                foreach (var effect in trigger.Effects.Where(effect => effect.Type == "CardEffectBuffDamage" && effect.Value > 0))
                    foreach (CombatUnit target in before.Units.Where(unit => unit.Team == CombatTeam.Enemy &&
                        (effect.Action?.Target == "Room" || effect.Action?.Target == "Self" && unit.Id == caster.Id)))
                    {
                        CombatUnit? actual = after.Units.FirstOrDefault(unit => unit.Id == target.Id);
                        if (actual?.Modifiers != null && target.Modifiers != null &&
                            actual.Modifiers.DamageBuff > target.Modifiers.DamageBuff)
                            targets.Add(effect.Action!.Target);
                    }
    }
}
