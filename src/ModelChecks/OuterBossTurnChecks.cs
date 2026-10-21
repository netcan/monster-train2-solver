using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OuterBossTurnChecks
{
    internal static void Deployment(FixtureValue fixture)
    {
        Require(fixture.GetProperty("OuterBossTurnsEnabled").GetBoolean() &&
            fixture.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => fixture.GetProperty(name).GetInt32() == 0),
            "Complete outer-boss deployment requires an original native capture with four zero failure counters.");
        FixtureValue[] turns = fixture.GetProperty("Turns").EnumerateArray().ToArray();
        FixtureValue[] actions = fixture.GetProperty("Actions").EnumerateArray().ToArray();
        BattleTurnState deployment = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        Require(deployment.Spawn.Turn == 0 && deployment.Spawn.QueuedOuterBoss?.Boss.AssetKey == "Boss_Level8_AllyBuffer" &&
            deployment.Spawn.OuterBoss == null, "Missing authored queued boss at the natural deployment decision.");
        BattleTurnState firstTurn = turns[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(firstTurn.Spawn.Turn == 1 && firstTurn.Spawn.OuterBoss != null && firstTurn.Spawn.QueuedOuterBoss == null,
            "Deployment must consume the queued definition and produce a live original boss.");
        foreach (FixtureValue row in turns)
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Compare(result.Supported, result.State, result.Outcome, row, "native EndTurn " + before.Spawn.Turn, result.UnsupportedReason);
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == immutable, "Deployment EndTurn mutated its source.");
        }
        BattleTurnState root = actions.Length > 0 && actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Turn == 0
            ? actions[0].GetProperty("Before").Deserialize<BattleTurnState>()! : deployment;
        string parent = JsonSerializer.Serialize(root, ModelJson.Options);
        BattleTurnState current = root;
        int actionIndex = 0;
        foreach (FixtureValue row in turns)
        {
            int turn = row.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Turn;
            while (actionIndex < actions.Length && actions[actionIndex].GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Turn == turn)
            {
                FixtureValue actionRow = actions[actionIndex];
                BattleActionResult played = BattleActionModel.PlayCard(current, actionRow.GetProperty("Action").Deserialize<PlayCardAction>()!);
                Compare(played.Supported, played.State, played.Outcome, actionRow, "frozen deployment action " + actionIndex, played.Reason);
                current = played.State!; actionIndex++;
            }
            BattleTurnResult ended = BattleTurnModel.EndTurn(current);
            Compare(ended.Supported, ended.State, ended.Outcome, row, "frozen deployment EndTurn " + turn, ended.UnsupportedReason);
            current = ended.State!;
        }
        Require(actionIndex == actions.Length && turns.Length >= 5 && JsonSerializer.Serialize(root, ModelJson.Options) == parent &&
            turns[^1].GetProperty("ActualOutcome").GetInt32() == (int)RoomOutcome.PlayerDefeated,
            "Complete deployment replay missed actions, mutated its root or failed to reach the native terminal loss.");
        Console.WriteLine($"OUTER-BOSS-DEPLOYMENT PASS: {turns.Length} complete EndTurns/{actionIndex} legal actions from one deployment root, authored creation and initial placement through native terminal loss; complete controller/aura/physical/context/RNG equality and immutable inputs.");
    }

    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("OuterBossTurnsEnabled").GetBoolean() &&
            fixture.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            fixture.GetProperty("CaptureFailures").GetInt32() == 0 && fixture.GetProperty("Mismatches").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0 && fixture.GetProperty("Unsupported").GetInt32() == 2,
            "Outer-boss turn calibration requires a complete native diagnostic with only the two initial-spawn guards.");
        FixtureValue[] turns = fixture.GetProperty("Turns").EnumerateArray().ToArray();
        FixtureValue[] actions = fixture.GetProperty("Actions").EnumerateArray().ToArray();
        BattleTurnState deployment = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnResult guarded = BattleTurnModel.EndTurn(deployment);
        Require(deployment.Spawn.OuterBoss == null && !guarded.Supported &&
            guarded.UnsupportedReason == "Outer or queued final boss spawning",
            "Initial outer-boss spawning must remain explicitly guarded outside this calibration.");
        BattleTurnState root = turns[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(root.Spawn.OuterBoss?.AssetKey == "Boss_Level8_AllyBuffer" && root.Spawn.Turn == 1,
            "Missing the original settled first live-boss decision.");
        int matched = 0, moves = 0;
        foreach (FixtureValue row in turns.Skip(1))
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            Require(before.Spawn.OuterBoss != null, "A calibrated turn lost its boss progression.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Compare(result.Supported, result.State, result.Outcome, row, "EndTurn " + row.GetProperty("Index").GetInt32(), result.UnsupportedReason);
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Outer-boss EndTurn mutated its source.");
            if (before.Spawn.OuterBoss!.Room != result.State!.Spawn.OuterBoss?.Room) moves++;
            matched++;
        }
        Require(matched >= 4 && moves >= 2 && actions.Length >= 20, "Missing occupied native boss turns, moves or legal card actions.");

        // A frozen replay starts at one native decision. Later native Before
        // snapshots are checked but never injected as successor model states.
        BattleTurnState current = root;
        string original = JsonSerializer.Serialize(root, ModelJson.Options);
        int actionIndex = 0;
        foreach (FixtureValue row in turns.Skip(1))
        {
            int turn = row.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Turn;
            while (actionIndex < actions.Length && actions[actionIndex].GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Turn == turn)
            {
                FixtureValue actionRow = actions[actionIndex];
                PlayCardAction action = actionRow.GetProperty("Action").Deserialize<PlayCardAction>()!;
                BattleActionResult played = BattleActionModel.PlayCard(current, action);
                Compare(played.Supported, played.State, played.Outcome, actionRow, "frozen action " + actionIndex, played.Reason);
                current = played.State!;
                actionIndex++;
            }
            BattleTurnResult ended = BattleTurnModel.EndTurn(current);
            Compare(ended.Supported, ended.State, ended.Outcome, row, "frozen EndTurn " + turn, ended.UnsupportedReason);
            current = ended.State!;
        }
        Require(actionIndex == actions.Length && JsonSerializer.Serialize(root, ModelJson.Options) == original &&
            turns[^1].GetProperty("ActualOutcome").GetInt32() == (int)RoomOutcome.PlayerDefeated,
            "Frozen native replay missed actions, mutated the root or failed to reach the original terminal loss.");
        Console.WriteLine($"OUTER-BOSS-TURN PASS: {matched} complete native EndTurns, {moves} moves and {actionIndex} legal actions from one frozen root; exact controller/aura/physical/context/RNG, terminal loss and immutable inputs. Initial spawning remains guarded.");
    }

    private static void Compare(bool supported, BattleTurnState? predicted, RoomOutcome outcome,
        FixtureValue row, string label, string? reason)
    {
        Require(supported, label + " unsupported: " + reason);
        BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(outcome == (RoomOutcome)row.GetProperty("ActualOutcome").GetInt32(), label + " outcome differs.");
        bool terminal = outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
        string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(predicted!, terminal),
            BattleTurnChecks.Comparable(actual, terminal));
        Require(difference == null, label + " differs: " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
