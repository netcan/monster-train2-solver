using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ConduitViewContinuationChecks
{
    internal static void Native(FixtureValue capture)
    {
        Require(capture.GetProperty("EvidenceKind").GetString() == "NativeAdviceResearch" &&
            capture.GetProperty("PreviousSearches").GetInt64() + 1 == capture.GetProperty("CurrentSearches").GetInt64(),
            "Missing independently captured automatic new search.");
        var previous = capture.GetProperty("PreviousRoot").Deserialize<BattleTurnState>()!;
        var actual = capture.GetProperty("ActualRoot").Deserialize<BattleTurnState>()!;
        var recorded = capture.GetProperty("PreviousPlan");
        var steps = recorded.GetProperty("Steps").EnumerateArray().Select(row => new BattleSearchStep(
            row.GetProperty("Action").Deserialize<PlayCardAction>(), row.GetProperty("IsEndTurn").GetBoolean())).ToArray();
        Require(previous.Spawn.Turn == 3 && actual.Spawn.Turn == 4 && previous.SelectedRoom == 1 && actual.SelectedRoom == 2 &&
            steps.Length == 8 && steps[0].IsEndTurn, "Wrong natural EndTurn/automatic Conduit focus boundary.");
        var raw = capture.GetProperty("ActualRoot").GetProperty("Spawn").GetProperty("Train").GetProperty("Context");
        var hand = actual.Spawn.Train.Context!.Cards.Hand.Select(card => card.InstanceId).ToArray();
        var units = raw.GetProperty("MagicPower")[5].GetProperty("Conduit").GetProperty("Units").EnumerateArray().ToArray();
        Require(units.Length == 3 && units.All(unit => unit.GetProperty("Cards").EnumerateArray().Select(id => id.GetInt32())
                .SequenceEqual(hand.Reverse())), "Lost the native reverse dictionary enumeration counterexample.");
        BattleTurnResult ended = BattleTurnModel.EndTurn(previous);
        Require(ended.Supported && ended.State!.SelectedRoom == 0 && SelectionAdviceReuse.SameBattle(ended.State, actual),
            "Automatic focus must reproduce all native Conduit upgrades, trackers and full state/RNG.");
        var final = BattlePlannerChecks.Replay(previous, steps, out RoomOutcome outcome);
        var recordedFinal = recorded.GetProperty("State").Deserialize<BattleTurnState>()!;
        Require(outcome == RoomOutcome.BattleWon && SelectionAdviceReuse.SameBattle(final, recordedFinal),
            "The original complete suffix must still reach exactly its winning final state/RNG.");
        var plan = new BattlePlanResult(final, outcome, steps, BattlePlanner.Evaluate(final, outcome), steps.Count(step => step.IsEndTurn),
            1, 0, 0, TimeSpan.Zero, false, false, false, false, true, [], null, searchedToBattleEnd: true);
        string immutable = JsonSerializer.Serialize(actual, ModelJson.Options);
        using var keys = new BattleStateKeys();
        using var session = new BattleAdviceSession((_, _) => plan);
        session.Observe(previous, keys.Key(previous)); Pump(session);
        var original = session.Current!;
        session.InvalidateForInput(); session.Observe(actual, keys.Key(actual)); Pump(session);
        Require(session.SearchCount == 1 && session.Current!.ReusedWinningContinuation &&
            session.Current.Plan.Steps.Count == 7 && session.Current.Plan.Outcome == RoomOutcome.BattleWon,
            "Automatic Conduit selection restarted search or discarded the seven-step winning suffix.");
        foreach (string control in new[] { "Energy", "Rng", "Membership", "Power", "Upgrade" })
        {
            JsonNode node = JsonSerializer.SerializeToNode(actual, ModelJson.Options)!;
            var context = node["Spawn"]!["Train"]!["Context"]!;
            if (control == "Energy") node["Energy"] = actual.Energy + 1;
            if (control == "Rng")
            {
                var words = node["RngStreams"]!.AsArray()[0]!["State"]!.AsObject();
                var word = words.First(); words[word.Key] = unchecked(word.Value!.GetValue<uint>() + 1);
            }
            if (control == "Membership") context["MagicPower"]![5]!["Conduit"]!["Units"]![0]!["Cards"]![0] = 18;
            if (control == "Power") context["MagicPower"]![5]!["Conduit"]!["Units"]![0]!["Power"] = 4;
            if (control == "Upgrade")
            {
                var card = context["CardInstances"]!.AsArray().First(card => card!["Temporary"]!["Upgrades"]!.AsArray().Count > 0)!;
                card["Temporary"]!["Upgrades"]![0]!["Stats"]!["Damage"] = 999;
            }
            Require(SelectionAdviceReuse.MatchAfterObservations(original, [], node.Deserialize<BattleTurnState>(ModelJson.Options)!, default) == null,
                "Conduit view normalization hid actual divergence: " + control);
        }
        Require(immutable == JsonSerializer.Serialize(actual, ModelJson.Options), "Conduit continuation mutated the native parent.");
        Console.WriteLine("CONDUIT-VIEW-CONTINUATION PASS: native reverse dictionary order, three contributors/six cards; " +
            "full-state/RNG focus and seven-step winning suffix; one search; energy/RNG/membership/power/upgrade guards.");
    }

    private static void Pump(BattleAdviceSession session)
    {
        Require(SpinWait.SpinUntil(() => { session.Poll(); return session.Status != BattleAdviceStatus.Planning; }, 30000),
            "Conduit view worker did not finish.");
        Require(session.Status == BattleAdviceStatus.Ready, session.Error ?? "Continuation unavailable.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
