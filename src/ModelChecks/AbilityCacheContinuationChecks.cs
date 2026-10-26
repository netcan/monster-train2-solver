using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AbilityCacheContinuationChecks
{
    internal static void Native(FixtureValue mismatch, FixtureValue display, FixtureValue? native = null)
    {
        var before = mismatch.GetProperty("PublishedRoot").Deserialize<BattleTurnState>()!;
        var actual = mismatch.GetProperty("ActualRoot").Deserialize<BattleTurnState>()!;
        Require(AbilityCacheAdviceReuse.TryWarm(before, actual, out BattleTurnState warmed), "Known native cache allocation not modeled.");
        using var keys = new BattleStateKeys();
        Require(keys.Key(warmed) == keys.Key(actual), "Warm cache differs from full native root: " + ModelJson.Difference(
            JsonSerializer.Serialize(warmed, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options)));
        var steps = display.GetProperty("SkillPresentation").GetProperty("Steps").EnumerateArray().Select(step => new BattleSearchStep(
            step.GetProperty("Action").Deserialize<PlayCardAction>(), step.GetProperty("IsEndTurn").GetBoolean())).ToArray();
        var final = BattlePlannerChecks.Replay(before, steps, out RoomOutcome outcome);
        Require(steps.Length == 5 && outcome == RoomOutcome.BattleWon, "Original complete five-input winning suffix missing.");
        var plan = new BattlePlanResult(final, outcome, steps, BattlePlanner.Evaluate(final, outcome),
            steps.Count(step => step.IsEndTurn), 1, 0, 0, TimeSpan.Zero, false, false, false, false, true, [], null, searchedToBattleEnd: true);
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        using var session = new BattleAdviceSession((_, _) => plan);
        session.Observe(before, keys.Key(before)); Pump(session);
        Require(session.MatchesCurrentRoot(actual), "Known native cache creation makes publication stale.");
        session.Observe(actual, keys.Key(actual)); Pump(session);
        Require(session.SearchCount == 1 && session.Current!.ReusedSelection && session.Current.Plan.Steps.Count == 5 &&
            session.Current.Plan.Outcome == RoomOutcome.BattleWon, "Cache warm discarded the complete winning suffix.");
        BattleAdviceSession.Verify(actual, session.Current!.Plan);
        foreach (string control in new[] { "Energy", "Rng", "Unknown", "Card", "Allocator" })
        {
            JsonNode changed = JsonSerializer.SerializeToNode(actual, ModelJson.Options)!;
            var context = changed["Spawn"]!["Train"]!["Context"]!;
            if (control == "Energy") changed["Energy"] = actual.Energy + 1;
            if (control == "Allocator") context["NextCardId"] = 99;
            if (control == "Unknown") context["AbilityCardCache"]![0]!["DataId"] = "unknown";
            if (control == "Card") context["CardRegistry"]!.AsArray().Last()!["DataId"] = "changed-definition";
            if (control == "Rng")
            {
                var words = changed["RngStreams"]!.AsArray()[0]!["State"]!.AsObject();
                var word = words.First(); words[word.Key] = unchecked(word.Value!.GetValue<uint>() + 1);
            }
            Require(!SelectionAdviceReuse.SameBattle(before, changed.Deserialize<BattleTurnState>(ModelJson.Options)!),
                "Cache warming hid real divergence: " + control);
        }
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Cache warming changed native parent.");
        if (native.HasValue)
        {
            var capture = native.Value;
            Require(capture.GetProperty("NativeCompleted").GetBoolean(), "Original native cache replay incomplete.");
            var warm = capture.GetProperty("AbilityCacheWarm");
            Require(warm.GetProperty("BeforeInput").GetInt32() == 35 &&
                keys.Key(warm.GetProperty("Before").Deserialize<BattleTurnState>()!) == keys.Key(before) &&
                keys.Key(warm.GetProperty("After").Deserialize<BattleTurnState>()!) == keys.Key(actual),
                "Native cold-cache API did not reproduce the exact old mismatch.");
            var rows = capture.GetProperty("Rows").EnumerateArray().ToArray();
            Require(rows.Length == 40, "Original35-input prefix plus five native winning inputs missing.");
            BattleTurnState current = capture.GetProperty("Request").GetProperty("Root").Deserialize<BattleTurnState>()!;
            for (int index = 0; index < rows.Length; index++)
            {
                var row = rows[index];
                if (index == 35)
                {
                    Require(keys.Key(current) == keys.Key(before), "Native cache event lost original prefix root.");
                    current = actual;
                }
                Require(BattleTurnChecks.Comparable(current, false) == BattleTurnChecks.Comparable(row.GetProperty("Before").Deserialize<BattleTurnState>()!, false),
                    "Native cache replay broke its chain at " + index);
                var step = new BattleSearchStep(row.GetProperty("Action").Deserialize<PlayCardAction>(), row.GetProperty("IsEndTurn").GetBoolean());
                var next = BattlePlannerChecks.Replay(current, [step], out RoomOutcome nativeOutcome);
                Require(BattleTurnChecks.Comparable(next, index == 39) ==
                    BattleTurnChecks.Comparable(row.GetProperty("Actual").Deserialize<BattleTurnState>()!, index == 39) &&
                    nativeOutcome == (index == 39 ? RoomOutcome.BattleWon : RoomOutcome.Exchanged),
                    "Complete native cache successor/state/RNG differs at " + index);
                current = next;
            }
            var overlay = capture.GetProperty("PlayerAdvisor").GetProperty("Overlay");
            Require(overlay.GetProperty("PurityFailures").GetInt32() == 0 && overlay.GetProperty("Failures").GetInt32() == 0,
                "Native cache acceptance changed projection purity.");
        }
        Console.WriteLine("ABILITY-CACHE-CONTINUATION PASS: exact native known-card allocation; five winning inputs replayed with one search; five strict guards; original40InputNativeWin=" + native.HasValue);
    }
    private static void Pump(BattleAdviceSession session)
    {
        Require(SpinWait.SpinUntil(() => { session.Poll(); return session.Status != BattleAdviceStatus.Planning; }, 30000), "Cache worker did not finish.");
        Require(session.Status == BattleAdviceStatus.Ready, session.Error ?? "Cache continuation unavailable.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
