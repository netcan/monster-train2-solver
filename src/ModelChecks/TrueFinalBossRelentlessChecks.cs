using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossRelentlessChecks
{
    private static void Require(bool value, string reason) => TrueFinalBossChecks.Require(value, reason);
    private static string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static string PhaseJson(OuterBossPhaseState state, bool terminal) => terminal ? Json(new
    {
        Battle = BattleTurnChecks.Comparable(state.Battle, true, true), state.Boss, state.TrueFinalBoss
    }) : Json(state);
    internal static void Phases(FixtureValue rows)
    {
        int count = 0, terminal = 0;
        foreach (FixtureValue row in rows.EnumerateArray())
        {
            OuterBossPhaseState before = row.GetProperty("Before").Deserialize<OuterBossPhaseState>()!;
            OuterBossPhaseState actual = row.GetProperty("Actual").Deserialize<OuterBossPhaseState>()!;
            string immutable = Json(before);
            bool ended = row.TryGetProperty("TerminalWon", out FixtureValue won) && won.ValueKind != FixtureKind.Null;
            OuterBossPhaseResult result = OuterBossPhaseModel.Apply(before, row.GetProperty("Phase").GetString() == "BossActionPreCombat");
            Require(result.Supported && PhaseJson(result.State!, ended) == PhaseJson(actual, ended), "Hellforged phase " + count + " differs: " +
                (result.Supported ? ModelJson.Difference(PhaseJson(result.State!, ended), PhaseJson(actual, ended)) : result.UnsupportedReason));
            Require(ended ? won.GetBoolean() && !row.GetProperty("NativeReturned").GetBoolean() && result.Outcome == RoomOutcome.BattleWon :
                row.GetProperty("NativeReturned").GetBoolean() && result.Outcome == RoomOutcome.Exchanged,
                "Native action return and terminal interruption were conflated.");
            if (ended)
            {
                terminal++;
                Require(actual.Battle.PlayRules!.Rooms.All(room => room.IsPyre || !room.Enabled) &&
                    actual.Battle.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.IsPyre) &&
                    actual.Battle.Spawn.Train.Context!.LifeLinks == null && actual.Battle.Spawn.OuterBoss == null,
                    "Savagery sacrifice must clear all five bodies, life links and the live global controller.");
            }
            Require(Json(before) == immutable, "Hellforged phase mutated its parent.");
            count++;
        }
        Require(count == 4 && terminal == 1, "Expected four native phase boundaries with one explicitly interrupted terminal action.");
        FixtureValue[] captured = rows.EnumerateArray().ToArray();
        Parallel.For(0, 64, index =>
        {
            FixtureValue row = captured[index % captured.Length];
            OuterBossPhaseState input = row.GetProperty("Before").Deserialize<OuterBossPhaseState>()!;
            string parent = Json(input);
            OuterBossPhaseResult result = OuterBossPhaseModel.Apply(input, row.GetProperty("Phase").GetString() == "BossActionPreCombat");
            bool ended = row.TryGetProperty("TerminalWon", out FixtureValue won) && won.ValueKind != FixtureKind.Null;
            Require(result.Supported && PhaseJson(result.State!, ended) == PhaseJson(row.GetProperty("Actual").Deserialize<OuterBossPhaseState>()!, ended) &&
                Json(input) == parent, "Concurrent Hellforged phase differs or mutates its parent.");
        });
        Console.WriteLine("TRUE-FINAL-BOSS-RELENTLESS-PHASES PASS: four full state/RNG boundaries; wait latch, three disabled floors and five-body sacrifice cleanup; terminal coroutine interruption preserved; 64 immutable parallel phases.");
    }

    internal static void Native(FixtureValue root)
    {
        FixtureValue route = root.GetProperty("OriginalTrueFinalBossRoute");
        Require(route.GetProperty("Scenario").GetString() == "Level9TrueFinalBoss_V2" &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() && route.GetProperty("CharacterAssetsUnchanged").GetBoolean() &&
            route.GetProperty("StartingDeckUnchanged").GetBoolean() && route.GetProperty("PyreHealth").GetInt32() == 80 &&
            root.GetProperty("Policy").GetString() == "no-cards" && root.GetProperty("BattleDistance").GetInt32() == 8 &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() && root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            root.GetProperty("Actions").GetArrayLength() == 0, "Missing original isolated TFB route and terminal capture provenance.");
        FixtureValue control = root.GetProperty("TrueFinalBossRelentlessControl");
        OuterBossPhaseState before = control.GetProperty("Before").Deserialize<OuterBossPhaseState>()!;
        OuterBossPhaseState actual = control.GetProperty("Actual").Deserialize<OuterBossPhaseState>()!;
        OuterBossPhaseResult prepared = TrueFinalBossPhaseModel.EnterRelentlessWithExhaustedQueue(before);
        Require(prepared.Supported && Json(prepared.State!) == Json(actual), "Native phase/queue control differs: " +
            (prepared.Supported ? ModelJson.Difference(Json(prepared.State!), Json(actual)) : prepared.UnsupportedReason));
        Require(control.GetProperty("NativeWaveCount").GetInt32() == actual.Battle.Spawn.Waves.Count &&
            before.Battle.Spawn.Phase < actual.Battle.Spawn.Phase && actual.Battle.Spawn.Phase == actual.Battle.Spawn.Waves.Count &&
            before.Battle.Spawn.Looping && !actual.Battle.Spawn.Looping &&
            Json(before.Battle.RngStreams) == Json(actual.Battle.RngStreams), "Missing RNG-neutral, explicit queue and phase control.");
        FixtureValue[] turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        Require(turns.Length == 3, "Expected deployment, controlled waiting turn and terminal destruction.");
        foreach (FixtureValue row in turns)
        {
            BattleTurnState input = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState expected = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            BattleTurnResult result = BattleTurnModel.EndTurn(input);
            bool terminal = result.Outcome == RoomOutcome.BattleWon;
            Require(result.Supported && result.Outcome == (RoomOutcome)row.GetProperty("ActualOutcome").GetInt32() &&
                BattleTurnChecks.Comparable(result.State!, terminal, true) == BattleTurnChecks.Comparable(expected, terminal, true),
                "Controlled Hellforged EndTurn " + row.GetProperty("Index").GetInt32() + " differs: " +
                (result.Supported ? ModelJson.Difference(BattleTurnChecks.Comparable(result.State!, terminal, true),
                    BattleTurnChecks.Comparable(expected, terminal, true)) : result.UnsupportedReason));
        }
        BattleTurnState deployment = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        string parent = Json(deployment);
        BattleTurnState Replay()
        {
            BattleTurnState state = deployment;
            for (int index = 0; index < turns.Length; index++)
            {
                if (index == 1)
                {
                    var boundary = new OuterBossPhaseState(state, state.Spawn.OuterBoss!, state.Spawn.TrueFinalBoss);
                    OuterBossPhaseResult controlled = TrueFinalBossPhaseModel.EnterRelentlessWithExhaustedQueue(boundary);
                    Require(controlled.Supported, "Independent phase/queue control failed: " + controlled.UnsupportedReason);
                    state = controlled.State!.Battle;
                }
                BattleTurnResult result = BattleTurnModel.EndTurn(state);
                bool terminal = index == turns.Length - 1;
                Require(result.Supported && result.Outcome == (terminal ? RoomOutcome.BattleWon : RoomOutcome.Exchanged),
                    "Independent Hellforged continuation rejected: " + result.UnsupportedReason);
                state = result.State!;
                BattleTurnState expected = turns[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
                Require(BattleTurnChecks.Comparable(state, terminal, true) == BattleTurnChecks.Comparable(expected, terminal, true),
                    "Independent Hellforged chain differs at " + index);
            }
            return state;
        }
        string expectedTerminal = Json(Replay());
        Require(Replay().Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 61,
            "Controlled native victory must preserve Pyre61.");
        Parallel.For(0, 16, _ => Require(Json(Replay()) == expectedTerminal, "Concurrent Hellforged branch differs."));
        Require(Json(deployment) == parent, "Hellforged full chain mutated its deployment root.");
        Phases(root.GetProperty("TrueFinalBossRelentlessActions"));
        Require(root.GetProperty("NativeWon").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0),
            "Native controlled full battle must finish with all four failure counters zero.");
        Console.WriteLine("TRUE-FINAL-BOSS-RELENTLESS PASS: explicit original phase/queue control, three EndTurns through victory, 16 independent immutable full chains; controlled evidence only, no surviving-body Pyre combat claim.");
    }
}
