using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AngelDuoRelentlessChecks
{
    internal static void Entry(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 1 &&
            fixture.GetProperty("Control").GetString() == "NativeForceRelentlessKeepingBothOriginalBosses" &&
            fixture.GetProperty("HealthAndAssetsUnchanged").GetBoolean() &&
            !fixture.GetProperty("CardsDrawnAfter").GetBoolean() && fixture.GetProperty("CardsReadyAfter").GetBoolean(),
            "Paired Relentless entry must disclose its original native control and drawn-hand boundary.");
        var before = fixture.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = fixture.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit[] bosses = before.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Where(unit => unit.BumpRules?.CompanionBoss == true).ToArray();
        Require(before.Spawn.Turn == 1 && bosses.Length == 2 && bosses.All(unit => unit.Health == unit.MaxHealth &&
            unit.BumpRules!.PairedBossUnitId > 0 && !unit.EndsBattleOnDeath) &&
            bosses.Any(unit => OuterBossDefinitions.IsAngelDuo(unit.AssetKey) && unit.Health == 1500) &&
            bosses.Any(unit => unit.AssetKey == "Boss_Level6_AngelDuo_Savage_Variant2_SlayAttack" && unit.Health == 1350),
            "Paired Relentless entry must retain both original full-health rank-two bosses.");
        string root = Json(before), expected = Json(actual);
        string? difference = ModelJson.Difference(Json(ApplyEntry(before)), expected);
        Require(difference == null, "Paired Relentless global entry differs: " + difference);
        Parallel.For(0, 16, _ => Require(Json(ApplyEntry(before)) == expected && Json(before) == root,
            "Paired Relentless entry branch changed native state/RNG or its input."));
        Console.WriteLine("ANGEL-DUO-RELENTLESS-ENTRY PASS: both original full-health bosses, native turn/spawn/scoring and readiness, complete state/RNG and sixteen immutable branches.");
    }

    internal static void Battle(FixtureValue fixture)
    {
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(counter).GetInt32() == 0, "Paired native battle failed: " + counter);
        FixtureValue input = fixture.GetProperty("NativeRunInput");
        Require(input.GetProperty("AscensionLevel").GetInt32() == 2 && input.GetProperty("Distance").GetInt32() == 5 &&
            input.GetProperty("BattlesWon").GetInt32() == 5 && !input.GetProperty("WasDevCheatUsed").GetBoolean() &&
            !fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Paired native battle must retain its original progressed input and settled no-card loss.");
        FixtureValue entry = fixture.GetProperty("AngelDuoRelentlessEntry"); Entry(entry);
        var before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        FixtureValue turns = fixture.GetProperty("Turns");
        var root = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        string immutable = Json(root);
        void Chain()
        {
            BattleTurnState state = root; bool entered = false;
            for (int index = 0; index < turns.GetArrayLength(); index++)
            {
                if (!entered && state.Spawn.Turn == before.Spawn.Turn)
                {
                    Require(BattleTurnChecks.Comparable(state) == BattleTurnChecks.Comparable(before),
                        "Independent paired chain differs before its native control.");
                    state = ApplyEntry(state); entered = true;
                }
                BattleTurnResult result = BattleTurnModel.EndTurn(state);
                Require(result.Supported, "Independent paired chain: " + result.UnsupportedReason);
                var actual = turns[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
                bool terminal = result.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
                string predicted = BattleTurnChecks.Comparable(result.State!, terminal);
                string expected = BattleTurnChecks.Comparable(actual, terminal);
                Require(result.Outcome == (RoomOutcome)turns[index].GetProperty("ActualOutcome").GetInt32() && predicted == expected,
                    "Independent paired chain differs at index " + index + ": " + ModelJson.Difference(predicted, expected));
                Require(terminal == (index == turns.GetArrayLength() - 1), "Paired chain terminated at the wrong turn.");
                state = result.State!;
            }
            Require(entered && Json(root) == immutable, "Paired chain omitted its native control or changed its root.");
            Require(state.PlayRules!.Rooms.Count(room => !room.IsPyre && !room.Enabled) == 3 &&
                state.Spawn.Train.Rooms[^1].Units.Count(unit => unit.BumpRules?.CompanionBoss == true && unit.Health > 0) == 2,
                "Paired terminal battle must retain both live original bosses in the Pyre after three destroyed floors.");
        }
        Chain(); Parallel.For(0, 16, _ => Chain());
        Console.WriteLine("ANGEL-DUO-RELENTLESS-BATTLE PASS: " + turns.GetArrayLength() +
            " native turns, both-live Relentless/Pyre continuation and sixteen independent complete controlled branches.");
    }

    internal static void Phases(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Errors").GetArrayLength() == 0, "Native paired outer phase capture failed.");
        int rows = 0, pre = 0, post = 0, transition = 0, destroyed = 0;
        foreach (FixtureValue row in fixture.GetProperty("Actions").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<OuterBossPhaseState>()!;
            var actual = row.GetProperty("Actual").Deserialize<OuterBossPhaseState>()!;
            bool isPre = row.GetProperty("Phase").GetString() == "BossActionPreCombat";
            Require(row.GetProperty("NativeReturned").GetBoolean() && OuterBossDefinitions.IsAngelDuo(before.Boss.AssetKey),
                "Missing original settled paired outer phase.");
            string root = Json(before), expected = Json(actual);
            var result = OuterBossPhaseModel.Apply(before, isPre);
            Require(result.Supported, result.UnsupportedReason ?? "Unsupported paired outer phase.");
            string? difference = ModelJson.Difference(Json(result.State), expected);
            Require(difference == null, "Paired outer phase differs: " + difference);
            Parallel.For(0, 16, _ => Require(Json(OuterBossPhaseModel.Apply(before, isPre).State) == expected && Json(before) == root,
                "Paired outer phase branch changed native state/RNG or its input."));
            if (isPre) pre++; else post++;
            if (before.Boss.Phase != actual.Boss.Phase) transition++;
            destroyed += before.Battle.PlayRules!.Rooms.Count(room => room.Enabled && !room.IsPyre &&
                !actual.Battle.PlayRules!.Rooms.Single(after => after.RoomIndex == room.RoomIndex).Enabled);
            rows++;
        }
        Require(pre > 0 && post > 0 && transition == 1 && destroyed == 0,
            "Missing paired transition or original floor-destruction ordering before the outer post phase.");
        Console.WriteLine("ANGEL-DUO-PAIRED-PHASE PASS: " + rows + " complete native pre/post phases, one transition, native floor-destruction ordering and sixteen immutable branches per phase.");
    }

    internal static void Pyre(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Errors").GetArrayLength() == 0 &&
            fixture.GetProperty("Actions").GetArrayLength() == 1, "Expected one settled native paired Pyre attack.");
        FixtureValue row = fixture.GetProperty("Actions")[0];
        var before = row.GetProperty("Before").Deserialize<OuterBossPhaseState>()!;
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(OuterBossDefinitions.IsAngelDuo(before.Boss.AssetKey) &&
            row.GetProperty("TerminalStopped").GetBoolean() &&
            (RoomOutcome)row.GetProperty("Outcome").GetInt32() == RoomOutcome.PlayerDefeated,
            "Paired Pyre control must retain its original terminal no-card loss.");
        string root = Json(before), expected = PyreComparable(actual);
        var result = OuterBossPyreModel.Apply(before);
        Require(result.Supported, result.UnsupportedReason ?? "Unsupported paired Pyre attack.");
        string? difference = ModelJson.Difference(PyreComparable(result.State!), expected);
        Require(difference == null && result.Outcome == RoomOutcome.PlayerDefeated,
            "Paired Pyre complete native state differs: " + difference);
        Parallel.For(0, 16, _ => Require(PyreComparable(OuterBossPyreModel.Apply(before).State!) == expected && Json(before) == root,
            "Paired Pyre branch changed native state/RNG or its input."));
        Console.WriteLine("ANGEL-DUO-PAIRED-PYRE PASS: original deferred paired entry, terminal full state/RNG and sixteen immutable branches.");
    }

    private static BattleTurnState ApplyEntry(BattleTurnState before)
    {
        ForcedRelentlessCounters counters = before.Spawn.Train.Context!.QueryFrame!.ForcedRelentless!;
        return ForcedRelentlessModel.ApplyBattle(before, new ForcedRelentlessProgress(before.Spawn.Turn,
            counters.WaveCount, before.Spawn.Phase, counters.RemainingTurns));
    }
    private static string PyreComparable(BattleTurnState state)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(Json(state))!;
        // Native terminal stop disposes the card manager. Its cached card
        // definitions are excluded using the existing Pyre-capture contract.
        node["PlayRules"]?.AsObject().Remove("Cards");
        return node.ToJsonString();
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
