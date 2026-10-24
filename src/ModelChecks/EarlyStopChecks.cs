using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EarlyStopChecks
{
    internal static void Run(FixtureValue fixture, FixtureValue healingFixture, FixtureValue lossFixture)
    {
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(counter).GetInt32() == 0, "Native source failed: " + counter);
        FixtureValue[] records = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        // A late native winning decision keeps the regression small while
        // retaining actual cards, turn callbacks, private histories and RNG.
        BattleTurnState root = records[^3].GetProperty("Root").Deserialize<BattleTurnState>()!;
        using var keys = new BattleStateKeys();
        string original = keys.Key(root);
        BattleEarlyStop? bound = BattleEarlyStop.Create(root, default);
        Require(bound != null && bound.PyreHealthUpperBound == Health(root), "Missing native no-healing bound.");

        BattlePlannerOptions Options(bool enabled, int nodes = 128) => new(nodes, 3, 8,
            timeLimit: TimeSpan.FromSeconds(30), allowShorterHorizon: true, searchToBattleEnd: true,
            stopAtBestPyreHealth: enabled);
        BattlePlanResult baseline = BattlePlanner.Plan(root, keys.Key, Options(false));
        Require(baseline.CompletedHorizon && baseline.Outcome == RoomOutcome.BattleWon && !baseline.StoppedEarly,
            "The fixed native control must reach a complete winning forecast.");
        foreach (int workers in new[] { 1, 2 })
        {
            BattlePlanResult early = ParallelBattlePlanner.Plan(root, Options(true), workers);
            Console.WriteLine($"EARLY-STOP-CONTROL workers={workers} outcome={early.Outcome} completed={early.CompletedHorizon} " +
                $"early={early.StoppedEarly} cancelled={early.Cancelled} timedOut={early.TimedOut} " +
                $"hp={Health(early.State)}/{bound!.PyreHealthUpperBound} nodes={early.ExpandedNodes}/{baseline.ExpandedNodes} score={early.Score}/{baseline.Score}");
            Require(early.CompletedHorizon && early.Outcome == RoomOutcome.BattleWon && early.StoppedEarly &&
                !early.Cancelled && !early.TimedOut && Health(early.State) == bound!.PyreHealthUpperBound &&
                early.Score <= baseline.Score && early.ExpandedNodes < baseline.ExpandedNodes,
                "Early stop lost score, failed to save work, or was confused with cancellation.");
            Require(early.SearchWorkers == Math.Min(workers, Math.Max(1, Environment.ProcessorCount - 1)),
                "Early stop dropped a background worker's accounting.");
            BattleAdviceSession.Verify(root, early);
            BattleTurnState replayed = BattlePlannerChecks.Replay(root, early.Steps, out RoomOutcome outcome);
            Require(keys.Key(replayed) == keys.Key(early.State) && outcome == early.Outcome,
                "Early winning forecast failed full-state/RNG replay.");
            BattlePlanResult suffix = VerifiedAdviceContinuation.Remaining(early, 1);
            Require(suffix.StoppedEarly && suffix.PyreHealthUpperBound == early.PyreHealthUpperBound,
                "Continuation lost early-stop provenance.");
            Console.WriteLine($"EARLY-STOP workers={early.SearchWorkers} nodes={early.ExpandedNodes}/{baseline.ExpandedNodes} " +
                $"elapsedMs={early.Elapsed.TotalMilliseconds:F0}/{baseline.Elapsed.TotalMilliseconds:F0} " +
                $"pyre={Health(early.State)} bound={early.PyreHealthUpperBound} fullReplay=true cancelled=false");
        }

        // Winning a short window, losing a battle, or improving a custom score
        // must never trip the default terminal-victory bound.
        bound!.Consider(root, RoomOutcome.Exchanged);
        bound.Consider(root, RoomOutcome.PlayerDefeated);
        Require(!bound.Reached, "A nonwinning outcome stopped search.");
        var custom = BattlePlanner.Plan(root, keys.Key, Options(true, 32), (_, _) => 0);
        Require(!custom.StoppedEarly && custom.PyreHealthUpperBound == null, "A custom evaluator used the Pyre objective bound.");
        var cancelled = ParallelBattlePlanner.Plan(root, Options(true), cancellationToken: new CancellationToken(true));
        Require(cancelled.Cancelled && !cancelled.StoppedEarly && cancelled.ExpandedNodes == 0,
            "User cancellation became an early-stop success.");
        var expired = ParallelBattlePlanner.Plan(root, new BattlePlannerOptions(timeLimit: TimeSpan.FromTicks(1),
            searchToBattleEnd: true, stopAtBestPyreHealth: true));
        Require(expired.TimedOut && !expired.StoppedEarly && expired.ExpandedNodes == 0,
            "An expired budget became an early-stop success.");

        BattleTurnState lossRoot = lossFixture.GetProperty("RollingDecisions").EnumerateArray().ToArray()[^3]
            .GetProperty("Root").Deserialize<BattleTurnState>()!;
        var loss = BattlePlanner.Plan(lossRoot, keys.Key, Options(true, 32));
        Require(loss.Outcome == RoomOutcome.BattleWon && Health(loss.State) < Health(lossRoot) && !loss.StoppedEarly,
            "A winning forecast below the objective bound stopped the search.");
        BattleAdviceSession.Verify(lossRoot, loss);

        // Reachability is conservative: even an unowned healing definition
        // raises the bound. Future/generative payloads must also be inspected.
        JsonNode changed = JsonSerializer.SerializeToNode(root)!;
        JsonNode pyre = changed["Spawn"]!["Train"]!["Rooms"]!.AsArray().SelectMany(room => room!["Units"]!.AsArray())
            .Single(unit => unit!["IsPyre"]!.GetValue<bool>())!;
        int damagedHp = Math.Max(1, Health(root) - 1);
        pyre["Health"] = damagedHp;
        var cards = changed["PlayRules"]!["Cards"]!.AsArray();
        cards.Add(JsonSerializer.SerializeToNode(new CardPlayRule("early-stop-healing-control", "control", 0, "Spell", "Discard",
            null, [], effects: [new CardActionEffect("HealTrain", "Pyre", 1, false, true, [])])));
        BattleTurnState healing = changed.Deserialize<BattleTurnState>()!;
        Require(BattleEarlyStop.Create(healing, default) == null,
            "Healing without captured native resource limits justified an unproven bound.");
        TrainHealingChecks.Native(healingFixture);
        BattleTurnState nativeHealing = healingFixture.GetProperty("Actions").EnumerateArray()
            .Single(record => record.GetProperty("ScenarioAction").GetBoolean())
            .GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleEarlyStop? healBound = BattleEarlyStop.Create(nativeHealing, default);
        Require(healBound != null && healBound.PyreHealthUpperBound == 80 && Health(nativeHealing) == 70,
            "Native train healing was ignored by the upper bound.");
        healBound!.Consider(nativeHealing, RoomOutcome.BattleWon);
        Require(!healBound.Reached, "A no-loss win stopped before potentially useful Pyre healing.");

        JsonNode growth = JsonSerializer.SerializeToNode(root)!;
        var trigger = new CombatTrigger("OnEncounterComplete", false, false, true, 0,
            [new CombatEffect("CardEffectPermanentlyAdjustMaxPyreHealth", 5, 0, "", 0, [], false,
                action: new CardActionEffect("AdjustPyreMaxHealth", "Self", 5, false, true, []))]);
        JsonNode futureUnit = growth["Spawn"]!["Waves"]![0]!["Candidates"]![0]!["Units"]![0]!["Unit"]!;
        futureUnit["Triggers"]!.AsArray().Add(JsonSerializer.SerializeToNode(trigger));
        Require(BattleEarlyStop.Create(growth.Deserialize<BattleTurnState>()!, default) == null,
            "A future spawn's encounter-complete maximum increase was ignored.");
        JsonNode resurrection = JsonSerializer.SerializeToNode(root)!;
        resurrection["Spawn"]!["Train"]!["Context"]!["Relics"]![0]!["EffectTypes"]!.AsArray()
            .Add("RelicEffectPyreHeartResurrection");
        Require(BattleEarlyStop.Create(resurrection.Deserialize<BattleTurnState>()!, default) == null,
            "Pyre resurrection bypassed the health bound guard.");

        // A sibling that sees the shared success signal stops without returning
        // Cancelled; an actual user cancellation still takes precedence.
        BattleEarlyStop shared = BattleEarlyStop.Create(root, default)!;
        shared.Consider(baseline.State, baseline.Outcome);
        Require(shared.Reached, "Winning worker did not signal its sibling.");
        var sibling = BattlePlanner.PlanCore(root, keys.Key, Options(true), null, default, true, shared);
        Require(sibling.StoppedEarly && !sibling.Cancelled && sibling.ExpandedNodes == 0, "Sibling kept searching after a proven win.");
        var userCancelled = BattlePlanner.PlanCore(root, keys.Key, Options(true), null, new CancellationToken(true), true, shared);
        Require(userCancelled.Cancelled, "Success signal suppressed a user cancellation.");
        Require(keys.Key(root) == original, "Early-stop inspection mutated the native root.");
        Console.WriteLine("EARLY-STOP PASS: serial/parallel work saved, unchanged best score, complete native state/RNG replay, healing/growth guards, custom evaluator, timeout, user cancellation and shared success.");
    }

    private static int Health(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health;
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
