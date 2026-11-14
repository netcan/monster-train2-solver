using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelentlessChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        FixtureValue[] samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == "NativeRelentlessNotification" &&
            root.GetProperty("NativeInterval").GetInt32() == 100 && root.GetProperty("LiveContextUnchanged").GetBoolean() &&
            root.GetProperty("RestorationDifference").ValueKind == FixtureKind.Null && root.GetProperty("Mismatches").GetInt32() == 0 &&
            samples.Length == 8 && samples.Select(sample => sample.GetProperty("Label").GetString()).Distinct().Count() == 8,
            "Incomplete native Relentless calibration.");
        Equal(root.GetProperty("RngBefore").ToObjectGraph(), root.GetProperty("RngAfter").ToObjectGraph(), "Native component RNG restoration");
        foreach (FixtureValue sample in samples) Verify(sample);
        Parallel.For(0, 32, _ => { foreach (FixtureValue sample in samples) Verify(sample); });
        RoomCombatState source = samples.Single(sample => sample.GetProperty("Label").GetString() == "abandon-three")
            .GetProperty("Before").Deserialize<RoomCombatState>()!;
        using var keys = new BattleStateKeys();
        Require(keys.SnapshotKey(source) != keys.SnapshotKey(source.WithContext(source.Context!.WithQueryFrame(
            source.Context.QueryFrame!.With(relentlessNotificationInterval: 4)))), "Pruning merged different notification intervals.");
        Require(!RoomCombatModel.Resolve(source.WithContext(source.Context!.WithQueryFrame(
            source.Context.QueryFrame!.With(relentlessNotificationInterval: 0)))).Supported, "Zero native interval was accepted.");
        Require(!RoomCombatModel.Resolve(source, relentlessDecisions: [new(0, 1, false)]).Supported &&
            !RoomCombatModel.Resolve(source, relentlessDecisions: [new(source.RoomIndex, 3, false), new(source.RoomIndex, 3, true)]).Supported,
            "Off-boundary or contradictory decisions were accepted.");
        Console.WriteLine("RELENTLESS PASS: eight native room successors, captured interval 100 and controlled interval 3, preview early exit, Abandon cleanup, one/two Continue callbacks, zero-damage cycles, room/Train/EndTurn pending requests and explicit API plans, complete room state/deterministic streams, distinct pruning keys and 32 immutable parallel branches. Search-plan/UI choice routing and whole-battle abandonment remain separate.");
    }
    private static void Verify(FixtureValue sample)
    {
        string label = sample.GetProperty("Label").GetString()!;
        Require(sample.GetProperty("Completed").GetBoolean() && sample.GetProperty("Difference").ValueKind == FixtureKind.Null,
            "Incomplete native sample: " + label);
        RoomCombatState before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        RoomCombatState actual = sample.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        RelentlessDecision[] choices = sample.GetProperty("Decisions").Deserialize<RelentlessDecision[]>()!;
        bool preview = sample.GetProperty("Preview").GetBoolean();
        int interval = sample.GetProperty("Interval").GetInt32(); int rounds = sample.GetProperty("Rounds").GetInt32();
        string parent = Serialize(before);
        Require(before.Preview == preview && before.Context?.QueryFrame?.RelentlessNotificationInterval == interval &&
            before.Units.Any(unit => unit.Status("relentless") != null), "Missing native Relentless inputs: " + label);
        RoomCombatResult result = RoomCombatModel.Resolve(before, relentlessDecisions: choices);
        Require(result.Supported && result.PreviewTruncated == preview && result.Rounds == rounds &&
            rounds == interval * (preview ? 1 : choices.Length), "Wrong native room boundary: " + label + "; " + result.UnsupportedReason);
        Equal(Comparable(result.State!), Comparable(actual), label + " complete room successor");
        Require(sample.GetProperty("ActualChoices").EnumerateArray().Select(value => value.GetBoolean())
            .SequenceEqual(choices.Select(choice => choice.Continue)), "Native callbacks differ: " + label);
        string[] phases = sample.GetProperty("Phases").EnumerateArray().Select(value => value.GetString()!).ToArray();
        Require(phases.Count(phase => phase == "Exchange") == rounds && phases.Count(phase => phase == "PostCombat") == (preview ? 0 : 1),
            "Missing independent native phase witness: " + label);
        Require(before.Units.Any(unit => unit.Team == CombatTeam.Player && unit.Status("valor")?.Stacks == 3) &&
            actual.Units.Any(unit => unit.Team == CombatTeam.Player && unit.Status("armor")?.Stacks == (preview ? null : 3)),
            "Missing preview/Abandon room-completion witness: " + label);
        if (!preview)
        {
            BattleTurnState decision = sample.GetProperty("DecisionBefore").Deserialize<BattleTurnState>()!;
            string decisionParent = Serialize(decision);
            TrainCombatResult trainPending = TrainCombatModel.ResolveCombat(decision.Spawn.Train);
            Require(!trainPending.Supported && trainPending.RelentlessChoice?.RoomIndex == before.RoomIndex &&
                trainPending.RelentlessChoice.CompletedRounds == interval, "Train lost pending choice metadata: " + label);
            BattleTurnResult turnPending = BattleTurnModel.EndTurn(decision);
            Require(!turnPending.Supported && turnPending.RelentlessChoice?.RoomIndex == before.RoomIndex &&
                turnPending.RelentlessChoice.CompletedRounds == interval,
                "EndTurn lost pending choice metadata: " + label + "; " + turnPending.UnsupportedReason);
            // Room successors above have direct native evidence. These checks
            // additionally verify the public whole-turn API routes that plan;
            // they do not claim native post-Abandon ascension acceptance.
            BattleTurnResult routed = BattleTurnModel.EndTurn(decision, choices);
            Require(routed.Supported, "EndTurn did not route explicit choices: " + label + "; " + routed.UnsupportedReason);
            Require(Serialize(decision) == decisionParent, "Choice routing mutated the decision: " + label);
            for (int count = 0; count < choices.Length; count++)
            {
                RoomCombatResult pending = RoomCombatModel.Resolve(before, relentlessDecisions: choices.Take(count).ToArray());
                Require(!pending.Supported && pending.RelentlessChoice?.RoomIndex == before.RoomIndex &&
                    pending.RelentlessChoice.CompletedRounds == interval * (count + 1) && pending.Rounds == interval * (count + 1),
                    "Missing or inaccurate pending dialog: " + label);
            }
        }
        // These room coroutines span UI frames. Unity's global rendering RNG,
        // Chatter and NonDeterministic streams remain recorded, but only the
        // seeded named gameplay streams must stay fixed for these controls.
        Equal(DeterministicStreams(sample.GetProperty("RngBefore")), DeterministicStreams(sample.GetProperty("RngAfter")),
            label + " deterministic gameplay streams");
        Require(Serialize(before) == parent, "Relentless branch mutated its parent: " + label);
    }
    private static object Comparable(RoomCombatState room) => new { room.RoomIndex, room.Deployment, room.Units, room.ExternalInteractions,
        room.Preview, room.ActiveCompanionBossIds, Context = BattleTurnChecks.ComparableContext(room.Context) };
    private static object DeterministicStreams(FixtureValue rng) => rng.GetProperty("Streams").EnumerateArray()
        .Where(stream => stream.GetProperty("Id").GetString() is not ("NonDeterministic" or "Chatter"))
        .Select(stream => stream.ToObjectGraph()).ToArray();
    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal(object? predicted, object? actual, string label)
    { string? difference = ModelJson.Difference(Serialize(predicted), Serialize(actual)); Require(difference == null, label + ": " + difference); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
