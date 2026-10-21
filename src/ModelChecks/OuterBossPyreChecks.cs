using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OuterBossPyreChecks
{
    internal static void Turns(FixtureValue fixture)
    {
        bool victory = fixture.GetProperty("NativeWon").GetBoolean();
        RoomOutcome expectedOutcome = victory ? RoomOutcome.BattleWon : RoomOutcome.PlayerDefeated;
        Require(fixture.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            fixture.GetProperty("OuterBossTurnsEnabled").GetBoolean() && fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0 && fixture.GetProperty("Mismatches").GetInt32() == 1 &&
            fixture.GetProperty("Unsupported").GetInt32() == (victory ? 0 : 1) && fixture.GetProperty("Actions").GetArrayLength() == 0,
            "Requires an isolated diagnostic with its explicit mid-turn control mismatch and only the historical loss capture's Pyre guard.");
        FixtureValue[] turns = fixture.GetProperty("Turns").EnumerateArray().ToArray();
        Require(turns.Length == 5, "Unexpected controlled native turn count.");
        BattleTurnState controlBefore = turns[1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState controlAfter = turns[1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(controlBefore.Spawn.Turn == 1 && controlBefore.Spawn.Phase < controlBefore.Spawn.Waves.Count &&
            controlAfter.Spawn.Turn == 2 && controlAfter.Spawn.Phase == controlAfter.Spawn.Waves.Count &&
            controlAfter.Spawn.OuterBoss?.Phase == "Relentless", "Missing the explicit mid-turn native queue control.");
        BattleTurnState root = turns[2].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState current = root;
        string immutable = JsonSerializer.Serialize(root, ModelJson.Options);
        foreach (FixtureValue row in turns.Skip(2))
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(current);
            Require(result.Supported, "Controlled frozen EndTurn unsupported: " + result.UnsupportedReason);
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            bool terminal = result.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
            string? diff = ModelJson.Difference(BattleTurnChecks.Comparable(result.State!, terminal), BattleTurnChecks.Comparable(actual, terminal));
            Require(diff == null && result.Outcome == (RoomOutcome)row.GetProperty("ActualOutcome").GetInt32(),
                "Controlled frozen EndTurn differs: " + diff);
            current = result.State!;
        }
        Require(JsonSerializer.Serialize(root, ModelJson.Options) == immutable &&
            current.PlayRules!.Rooms.Count(room => !room.IsPyre && !room.Enabled) == 3 &&
            turns[^1].GetProperty("ActualOutcome").GetInt32() == (int)expectedOutcome,
            "Missing immutable root, three destroyed floors or expected native Pyre terminal outcome.");
        if (victory) Require(current.Spawn.OuterBoss == null && current.Spawn.Train.Context!.AllScenarioBossesDead == true &&
            current.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 13,
            "Controlled Pyre victory must remove the boss and retain native Pyre13.");
        Console.WriteLine($"OUTER-BOSS-PYRE-TURN PASS: three complete native EndTurns from one post-control root through floor destruction and Pyre combat; {expectedOutcome}, exact successors without native reinjection.");
    }

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = fixture.GetProperty("Actions").EnumerateArray().ToArray();
        bool victory = rows.Length > 0 && (RoomOutcome)rows[^1].GetProperty("Outcome").GetInt32() == RoomOutcome.BattleWon;
        Require(fixture.GetProperty("Schema").GetInt32() == 1 &&
            fixture.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            fixture.GetProperty("Boundary").GetString() == "AfterDoBossTowerAttackOrTerminalStop" &&
            fixture.GetProperty("Errors").GetArrayLength() == 0 && fixture.GetProperty("Actions").GetArrayLength() > 0 &&
            fixture.GetProperty("QueueControls").GetArrayLength() == (victory ? 2 : 1),
            "Missing original Pyre-entry capture and explicit native queue control.");
        if (victory)
        {
            FixtureValue health = fixture.GetProperty("QueueControls")[1];
            Require(health.GetProperty("Kind").GetString() == "SetOuterBossHealth" && health.GetProperty("Turn").GetInt32() == 2 &&
                health.GetProperty("HealthBefore").GetInt32() == 3800 && health.GetProperty("HealthAfter").GetInt32() == 1 &&
                health.GetProperty("MaxHealth").GetInt32() == 3800, "Missing explicit native health control; this is not a natural victory.");
        }
        int count = 0;
        foreach (FixtureValue row in fixture.GetProperty("Actions").EnumerateArray())
        {
            Require(row.GetProperty("NativeReturned").GetBoolean() || row.GetProperty("TerminalStopped").GetBoolean() &&
                (RoomOutcome)row.GetProperty("Outcome").GetInt32() is RoomOutcome.PlayerDefeated or RoomOutcome.BattleWon,
                "Native Pyre attack neither returned nor reached the terminal stop boundary.");
            OuterBossPhaseState before = row.GetProperty("Before").Deserialize<OuterBossPhaseState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            OuterBossPyreResult result = OuterBossPyreModel.Apply(before);
            Require(result.Supported, "Outer-boss Pyre entry unsupported: " + result.UnsupportedReason);
            string Compare(BattleTurnState state)
            {
                var node = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(state, ModelJson.Options))!;
                // Native deletes its card manager at terminal stop. Cached card
                // definitions are no longer gameplay state; retain all room rules.
                if (result.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated)
                    node["PlayRules"]?.AsObject().Remove("Cards");
                return node.ToJsonString();
            }
            string? diff = ModelJson.Difference(Compare(result.State!), Compare(actual));
            Require(diff == null, "Outer-boss Pyre entry differs: " + diff);
            Require(result.Outcome == (RoomOutcome)row.GetProperty("Outcome").GetInt32() && JsonSerializer.Serialize(before, ModelJson.Options) == parent,
                "Pyre-entry outcome differs or mutated its source.");
            count++;
        }
        Console.WriteLine($"OUTER-BOSS-PYRE PASS: {count} native moves/callbacks/immediate Pyre combats; full battle/physical/controller/aura/RNG equality, explicit isolated queue control and immutable inputs.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
