using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DevoteBattleChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement, evidence = root.GetProperty("DevoteBattle");
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("ModifierScenario").GetString() == "devote-battle" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0),
            "Incomplete native Devote victory.");
        Require(evidence.GetProperty("Prepared").GetBoolean() && evidence.GetProperty("Error").ValueKind == FixtureKind.Null &&
            evidence.GetProperty("Devotes").GetInt32() == 14, "Missing native Devote controls.");
        int persistent = evidence.GetProperty("Persistent").GetInt32(), infusion = evidence.GetProperty("Infusion").GetInt32(),
            frozen = evidence.GetProperty("Frozen").GetInt32(), ordinary = evidence.GetProperty("Ordinary").GetInt32();
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray(),
            observations = evidence.GetProperty("Observations").EnumerateArray().ToArray();
        Require(actions.Length == 15 && observations.Length == 28, "Missing deployment or Devote observations.");
        var gates = evidence.GetProperty("Gates").EnumerateArray().Select(row =>
            (row.GetProperty("Label").GetString(), row.GetProperty("Error").GetString())).ToArray();
        Require(gates.Contains(("deployment", "DeploymentPhase")) && gates.Contains(("zero-energy", "NoEmber")) &&
            gates.Contains(("turn-1", "NoCards")) && gates.Contains(("turn-2", "NoEmber")), "Missing native legality witnesses.");
        foreach (FixtureValue gate in evidence.GetProperty("Gates").EnumerateArray())
        {
            Require(!gate.GetProperty("Yielded").GetBoolean() && gate.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "Illegal native Devote did not finish immediately.");
            Equal(gate.GetProperty("Before").ToObjectGraph(), gate.GetProperty("After").ToObjectGraph(), "Illegal Devote changes full native train");
            Equal(gate.GetProperty("RngBefore").ToObjectGraph(), gate.GetProperty("RngAfter").ToObjectGraph(), "Illegal Devote changes any RNG");
        }
        var chosen = new List<int>();
        for (int index = 1; index < actions.Length; index++)
        {
            FixtureValue row = actions[index], discard = observations[(index - 1) * 2], completed = observations[(index - 1) * 2 + 1];
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatContext from = before.Spawn.Train.Context!, to = after.Spawn.Train.Context!;
            int[] eligible = DevoteModel.EligibleCards(from, out string? error)!;
            Require(error == null && eligible.Length > 0 && !eligible.Contains(persistent) && !eligible.Contains(infusion) &&
                from.Cards.Hand.Any(card => card.InstanceId == persistent) && from.Cards.Hand.Any(card => card.InstanceId == infusion),
                "Devote lost protected cards or trait eligibility.");
            RngDraw random = from.BattleRng.Range(0, eligible.Length); int selected = eligible[random.Value]; chosen.Add(selected);
            Require(action.Operation == DevoteModel.Operation && action.CardInstanceId == 0 && action.RoomIndex == before.Spawn.Turn % 3 &&
                discard.GetProperty("Phase").GetString() == "Discard" && completed.GetProperty("Phase").GetString() == "Completed" &&
                discard.GetProperty("Number").GetInt32() == index && completed.GetProperty("Number").GetInt32() == index &&
                discard.GetProperty("Card").GetInt32() == selected && discard.GetProperty("Room").GetInt32() == action.RoomIndex &&
                new[] { "wasPlayed", "triggeredByCard", "handDiscarded" }.All(key => !discard.GetProperty(key).GetBoolean()),
                "Native random choice or discard arguments differ.");
            Require(to.Cards.Hand.Select(card => card.InstanceId).SequenceEqual(from.Cards.Hand.Where(card => card.InstanceId != selected).Select(card => card.InstanceId)) &&
                to.Cards.Discard.Last().InstanceId == selected && after.Energy == before.Energy - 1 &&
                discard.GetProperty("Energy").GetInt32() == after.Energy, "Devote pile ordering or energy differs.");
            Equal(random.State, to.BattleRng, "Battle RNG must draw exactly once, including singleton candidates");
            Equal(from.Cards.Rng, to.Cards.Rng, "Devote must preserve card-draw RNG");
            int level = index < 3 ? 0 : index < 9 ? 1 : 2, points = index - (level == 0 ? 0 : level == 1 ? 3 : 9);
            Require(to.QueryFrame!.Devotion!.Level == level && to.QueryFrame.Devotion.Points == points &&
                discard.GetProperty("Devotion").GetProperty("Level").GetInt32() == level &&
                discard.GetProperty("Devotion").GetProperty("Points").GetInt32() == points,
                "Devotion must update before discard, with native thresholds and remainders.");
            Require(completed.GetProperty("Notifications").EnumerateArray().Select(value => value.GetString())
                .SequenceEqual(index is 3 or 9 ? new[] { "Cards", "Monsters" } : Array.Empty<string>()), "Level notification order differs.");
            string unchanged = JsonSerializer.Serialize(before, ModelJson.Options);
            BattleActionResult result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported, result.Reason ?? "Devote unsupported.");
            RecordedBattleChecks.Compare(result.State!, after, false, "independent native Devote");
            Require(unchanged == JsonSerializer.Serialize(before, ModelJson.Options), "Devote changed its input.");
        }
        Require(chosen.Contains(frozen) && chosen.Contains(ordinary) && chosen.Any(id => id != frozen && id != ordinary) &&
            actions.Skip(1).Any(row => DevoteModel.EligibleCards(row.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!, out _)!.Length == 1),
            "Missing frozen/ordinary/generated-card/singleton random controls.");
        BattleTurnState initial = actions[1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleActionEnumeration frontier = BattleActionModel.EnumerateActions(initial);
        Require(frontier.SupportedActions.Any(action => action.Operation == DevoteModel.Operation), "Search omits Devote.");
        Require(!BattleActionModel.EnumerateActions(initial, onlyCardId: frozen).SupportedActions.Any(action => action.Operation != null),
            "Card-filtered action enumeration contains Devote.");
        Require(BattleActionModel.PlayCard(initial, new PlayCardAction(frozen, 1, operation: DevoteModel.Operation)).Rejection == ActionRejection.Illegal &&
            BattleActionModel.PlayCard(initial, new PlayCardAction(0, 1, operation: "Unknown")).Rejection == ActionRejection.Unsupported,
            "Invalid operation payloads are accepted.");
        BattleTurnState deployed = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        Require(DevoteModel.Apply(deployed, new PlayCardAction(0, 2, operation: DevoteModel.Operation)).Rejection == ActionRejection.Illegal,
            "Deployment allows Devote.");
        foreach (FixtureValue row in actions.Skip(1))
        {
            BattleTurnState after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            int[] eligible = DevoteModel.EligibleCards(after.Spawn.Train.Context!, out _)!;
            if (after.Energy == 0 || eligible.Length == 0)
                Require(DevoteModel.Apply(after, new PlayCardAction(0, after.SelectedRoom, operation: DevoteModel.Operation))
                    .Rejection == ActionRejection.Illegal, "No-energy/no-candidate model control accepts Devote.");
        }
        double Score(BattleTurnState state, RoomOutcome _) => -state.Spawn.Train.Context!.QueryFrame!.Devotion!.Points;
        BattleSearchResult serial = BattleSearchModel.Minimize(initial, Score, new BattleSearchOptions(128, 0, 1)),
            parallel = BattleSearchModel.Minimize(initial, Score, new BattleSearchOptions(128, 0, 4));
        Require(frontier.UnsupportedAttempts.Count == 0 && serial.UnsupportedAttempts.Count == 0 && parallel.UnsupportedAttempts.Count == 0 &&
            serial.Score == parallel.Score && serial.Score == -2 &&
            serial.Steps.Any(step => step.Action?.Operation == DevoteModel.Operation) &&
            parallel.Steps.Any(step => step.Action?.Operation == DevoteModel.Operation), "Serial/parallel search does not select the native Devote continuation.");
        foreach (BattleSearchResult plan in new[] { serial, parallel })
        {
            BattleTurnState state = initial;
            foreach (BattleSearchStep step in plan.Steps)
            {
                Require(!step.IsEndTurn && step.Action != null, "Unexpected turn in Devote search control.");
                BattleActionResult played = BattleActionModel.PlayCard(state, step.Action!);
                Require(played.Supported, played.Reason ?? "Devote search step cannot replay."); state = played.State!;
            }
            Equal(state, plan.BestState!, "Search plan does not reproduce its complete terminal candidate");
        }
        using (var keys = new BattleStateKeys())
        {
            BattlePlanResult plan = BattlePlanner.Plan(initial, keys.Key,
                new BattlePlannerOptions(maxNodes: 128, maxTurns: 1, timeLimit: TimeSpan.FromSeconds(20)), Score);
            Require(!plan.TimedOut && plan.UnsupportedAttempts.Count == 0 && plan.Score == -2 &&
                plan.Steps.Any(step => step.Action?.Operation == DevoteModel.Operation),
                "Cached advisor planner omits Devote or uses its card cost/target cache.");
        }
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        Console.WriteLine($"DEVOTE-BATTLE PASS: 14 native Devotes across five turns, explicit search operation, energy/points-before-discard, persistent/infusion exclusions, frozen/generated/singleton RNG choices, notification order, full successor/RNG and 16 immutable branches; serial/parallel search score {serial.Score}, frontier unsupported {frontier.UnsupportedAttempts.Count}.");
    }
    private static void Equal(object a, object b, string message) => Require(JsonSerializer.Serialize(a, ModelJson.Options) == JsonSerializer.Serialize(b, ModelJson.Options), message);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
