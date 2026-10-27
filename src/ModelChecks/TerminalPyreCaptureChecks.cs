using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TerminalPyreCaptureChecks
{
    private const string Reason = "Native terminal Pyre retained world differs after input58.";
    internal static void Native(string sourcePath, string? replayPath = null)
    {
        using var source = ModelJson.ReadFixture(sourcePath);
        var rows = source.RootElement.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length == 58 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing original58-input defeat.");
        var before = State(rows[^1].GetProperty("Root"));
        var actual = State(rows[^1].GetProperty("Actual"));
        var historical = State(rows[^1].GetProperty("Expected"));
        Require(before.Spawn.Turn == 9 && !source.RootElement.GetProperty("NativeWon").GetBoolean() &&
            historical.Spawn.Train.Context!.Enchantments!.RetainedUnits.Count == 0,
            "Wrong terminal observation boundary.");
        var extra = actual.Spawn.Train.Context!.Enchantments!.RetainedUnits.Single();
        Require(extra is { Preview: false, RoomIndex: 3, Unit: { Id: 1, AssetKey: "PyreHeartStarter", IsPyre: true, Health: 0,
            DeathState: { HasFinishedDying: false, IsBeingRemoved: false, HasStatisticsListener: true } } } &&
            actual.Spawn.Train.Context.SpawnPoints!.Units.Single(point => point.UnitId == 1) is { Current: null, LastKnown: null },
            "Original extra actor is not the terminal Pyre stopped before removal signals.");
        Require(Key(historical, true) == WithoutExtraPyre(actual), "Original difference extends beyond capture-only terminal Pyre retention.");
        BattleTurnState? previous = null;
        for (int index = 0; index < rows.Length; index++)
        {
            var row = rows[index]; var root = State(row.GetProperty("Root"));
            Require(previous == null || Key(previous) == Key(root), "Original native prefix changed at " + index);
            var next = BattlePlannerChecks.Replay(root, [new BattleSearchStep(row.GetProperty("Action").Deserialize<PlayCardAction>(),
                row.GetProperty("IsEndTurn").GetBoolean())], out var outcome);
            bool terminal = outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
            previous = State(row.GetProperty("Actual"));
            Require(Key(next, terminal) == Key(State(row.GetProperty("Expected")), terminal) &&
                Key(next, terminal) == (index == rows.Length - 1 ? WithoutExtraPyre(previous) : Key(previous)),
                "Original full successor/forecast/RNG changed at " + index);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var ended = BattleTurnModel.EndTurn(before);
            Require(ended.Supported && ended.Outcome == RoomOutcome.PlayerDefeated &&
                Key(ended.State!, true) == Key(historical, true), "Terminal Pyre capture changed gameplay or RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Terminal capture mutated its parent.");
        if (replayPath != null)
        {
            using var replay = ModelJson.ReadFixture(replayPath);
            Require(replay.RootElement.GetProperty("Request").GetProperty("SourceSHA256").GetString() ==
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sourcePath))).ToLowerInvariant(), "Independent replay changed its source.");
            var independent = NativeBranchObservationChecks.Replay(replay.RootElement, Reason, boundaryOutcome: RoomOutcome.PlayerDefeated);
            Require(independent.Length == rows.Length, "Independent replay changed input count.");
            for (int index = 0; index < rows.Length; index++)
            {
                bool terminal = index == rows.Length - 1;
                Require(Key(State(rows[index].GetProperty("Root"))) == Key(State(independent[index].GetProperty("Before"))) &&
                    (terminal ? WithoutExtraPyre(State(rows[index].GetProperty("Actual"))) : Key(State(rows[index].GetProperty("Actual")))) ==
                    Key(State(independent[index].GetProperty("Actual")), terminal) &&
                    rows[index].GetProperty("Action").ContentEquals(independent[index].GetProperty("Action")) &&
                    rows[index].GetProperty("IsEndTurn").ContentEquals(independent[index].GetProperty("IsEndTurn")),
                    "Independent native input/state/RNG changed at " + index);
            }
        }
        Console.WriteLine("TERMINAL-PYRE-CAPTURE PASS:58 original inputs/full successors/forecast/RNG; exact capture-only unfinished Pyre witness; four immutable branches; raw room acceptance remains separate; independentPair=" + (replayPath != null));
    }
    private static BattleTurnState State(FixtureValue value) => value.Deserialize<BattleTurnState>()!;
    private static string Key(BattleTurnState state, bool terminal = false) => NativeBranchObservationChecks.Key(state, terminal);
    private static string WithoutExtraPyre(BattleTurnState state)
    {
        var node = JsonNode.Parse(Key(state, true))!;
        node["Context"]!["Enchantments"]!["RetainedUnits"]!.AsArray().Clear();
        return node.ToJsonString();
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
