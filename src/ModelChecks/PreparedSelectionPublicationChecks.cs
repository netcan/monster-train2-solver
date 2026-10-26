using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PreparedSelectionPublicationChecks
{
    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("EvidenceKind").GetString() == "NativeAdviceRootMismatch", "Wrong native mismatch evidence.");
        var published = fixture.GetProperty("PublishedRoot").Deserialize<BattleTurnState>()!;
        var actual = fixture.GetProperty("ActualRoot").Deserialize<BattleTurnState>()!;
        string parent = JsonSerializer.Serialize(published, ModelJson.Options);
        using var keys = new BattleStateKeys();
        Require(published.SelectedRoom == 1 && actual.SelectedRoom == 0 && published.Spawn.Turn == 1 &&
            fixture.GetProperty("Observations").GetArrayLength() == 0 &&
            fixture.GetProperty("PublishedRootKey").GetString() == fixture.GetProperty("StoredRootKey").GetString() &&
            fixture.GetProperty("ActualRootKey").GetString() != fixture.GetProperty("StoredRootKey").GetString() &&
            keys.Key(published) != keys.Key(actual),
            "Missing exact native stale middle-floor publication after deployment.");
        var selected = BattleTurnModel.SelectRoom(published, actual.SelectedRoom);
        using var selectionKeys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
        Require(selected.Supported && selectionKeys.Key(selected.State!) == selectionKeys.Key(actual),
            "Native publication differs beyond its modeled room-selection callback: " + selected.UnsupportedReason + ": " +
            (selected.Supported ? ModelJson.Difference(JsonSerializer.Serialize(selected.State, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options)) : ""));
        var end = BattleTurnModel.EndTurn(published);
        Require(end.Supported, end.UnsupportedReason!);
        var plan = new BattlePlanResult(end.State!, end.Outcome, [new BattleSearchStep(null, true)],
            BattlePlanner.Evaluate(end.State!, end.Outcome), 1, 1, 0, 0, TimeSpan.Zero,
            false, false, false, false, true, [], null, 1);
        Check(false);
        Check(true);
        Require(parent == JsonSerializer.Serialize(published, ModelJson.Options), "Publication mutated its native parent.");
        Console.WriteLine("PREPARED-SELECTION-PUBLICATION PASS: exact native middle-to-bottom view; old stale control; delayed prepared publication preserves latest view without new search/generation; energy divergence rejected.");

        void Check(bool recordLatest)
        {
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            int verifications = 0;
            using var session = new BattleAdviceSession((_, _) => plan, (root, result) =>
            {
                if (Interlocked.Increment(ref verifications) == 2)
                { entered.Set(); if (!release.Wait(10000)) throw new TimeoutException("Delayed publication did not resume."); }
                BattleAdviceSession.Verify(root, result);
            });
            try
            {
                session.Observe(published, keys.Key(published)); Pump(session);
                session.InvalidateForInput(); session.BrowseRoom(actual.SelectedRoom);
                session.Observe(session.PrepareObservationAsync(published).GetAwaiter().GetResult());
                Require(entered.Wait(10000), "Prepared publication did not enter verification.");
                long generation = session.Generation, searches = session.SearchCount;
                if (recordLatest) session.BrowseRoom(actual.SelectedRoom, force: true);
                release.Set(); Pump(session);
                Require(session.MatchesCurrentRoot(actual) == recordLatest && session.Generation == generation &&
                    session.SearchCount == searches && session.SearchCount == 1,
                    "Late view was lost, or it restarted search/generation.");
                if (recordLatest)
                {
                    JsonNode changed = JsonSerializer.SerializeToNode(actual, ModelJson.Options)!;
                    changed["Energy"] = actual.Energy + 1;
                    Require(!session.MatchesCurrentRoot(changed.Deserialize<BattleTurnState>(ModelJson.Options)!),
                        "Late view matching hid a real gameplay change.");
                }
            }
            finally { release.Set(); }
        }
    }

    private static void Pump(BattleAdviceSession session)
    {
        var watch = Stopwatch.StartNew();
        while (session.Status == BattleAdviceStatus.Planning)
        { session.Poll(); if (watch.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException(); Thread.Yield(); }
        Require(session.Status == BattleAdviceStatus.Ready, session.Error ?? "Publication unavailable.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
