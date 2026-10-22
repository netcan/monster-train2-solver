using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PlayerTerminalCopyChecks
{
    internal static void TargetBoundary(FixtureValue fixture)
    {
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(counter).GetInt32() == 0, "Nonzero native target diagnostic " + counter);
        Require(fixture.GetProperty("TerminalEffectsSettled").GetBoolean(), "Native target diagnostic did not settle.");
        RecordedBattleChecks.Native(fixture);
        FixtureValue failed = fixture.GetProperty("PlannerRuns").EnumerateArray().Single();
        BattleTurnState root = failed.GetProperty("Root").Deserialize<BattleTurnState>()!;
        Require(!fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("CopyOnPlayTerminalPurge").GetBoolean(),
            "Missing failed native temporary-copy target diagnostic.");
        using var keys = new BattleStateKeys();
        string parent = keys.Key(root);
        int bossRoom = root.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.IsBoss == true)).RoomIndex;
        // Discard the three affordable junk cards selected before the spell.
        BattleTurnState spellRoot = root;
        foreach (FixtureValue step in failed.GetProperty("Steps").EnumerateArray())
        {
            if (step.GetProperty("IsEndTurn").GetBoolean()) break;
            PlayCardAction action = step.GetProperty("Action").Deserialize<PlayCardAction>()!;
            if (spellRoot.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.DataId == "3d1a2e1b-7751-4b2c-a549-77d03db715c9") break;
            BattleActionResult next = BattleActionModel.PlayCard(spellRoot, action);
            Require(next.Supported, next.Reason ?? "Diagnostic prefix failed.");
            spellRoot = next.State!;
        }
        foreach (bool tactical in new[] { false, true })
        {
            SupportedActionCandidate? chosen = tactical ? BattleTacticalPolicy.Choose(spellRoot, out _) :
                BattleHeuristicPolicy.Choose(spellRoot, out _);
            Require(chosen?.Action.RoomIndex == bossRoom, "Room damage seed still prefers low-health crowds over useful damage.");
        }
        BattlePlanResult plan = BattlePlanner.Plan(root, keys.Key,
            new BattlePlannerOptions(64, 3, 8, timeLimit: TimeSpan.FromSeconds(10)));
        BattleTurnState replayed = BattlePlannerChecks.Replay(root, plan.Steps, out RoomOutcome outcome);
        Require(plan.CompletedHorizon && outcome == RoomOutcome.BattleWon && plan.Outcome == outcome &&
            keys.Key(replayed) == keys.Key(plan.State) && keys.Key(root) == parent,
            "Improved default seed lost the complete independently replayable terminal-copy victory or mutated its root.");
        Console.WriteLine($"TERMINAL-COPY-TARGET PASS usefulRoomDamage=true defaultSeedWins=true nodes={plan.ExpandedNodes} completeReplayAndRng=true immutableParent=true; failed native source remains diagnostic.");
    }

    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        bool purge = source.GetProperty("CopyOnPlayTerminalPurge").GetBoolean();
        bool consume = source.GetProperty("CopyOnPlayTerminalConsume").GetBoolean();
        foreach (FixtureValue fixture in new[] { source, replay })
        {
            Require(fixture.GetProperty("CopyOnPlayTerminalThreat").GetBoolean() &&
                fixture.GetProperty("CopyOnPlayTerminalPurge").GetBoolean() == purge &&
                fixture.GetProperty("CopyOnPlayTerminalConsume").GetBoolean() == consume &&
                fixture.GetProperty("PlayerAdvisor").GetProperty("Assembly").GetString() == "MonsterTrain2Poju.Advisor",
                "Missing matching controlled threat and separate actual player assembly.");
            if (purge) TerminalPurgeCopyChecks.Native(fixture);
            else TerminalCopyOnPlayChecks.Native(fixture);
            FixtureValue plan = fixture.GetProperty("PlannerRuns").EnumerateArray().Single();
            BattleTurnState root = plan.GetProperty("Root").Deserialize<BattleTurnState>()!;
            Require(root.Spawn.Turn == 4 && plan.GetProperty("Horizon").GetInt32() == 3 &&
                plan.GetProperty("Outcomes").EnumerateArray().Last().Deserialize<RoomOutcome>() == RoomOutcome.BattleWon &&
                plan.GetProperty("Steps").EnumerateArray().All(step => !step.GetProperty("IsEndTurn").GetBoolean()),
                "The actual recommendation must win at boss birth through card plays, using the default horizon.");
            CombatUnit boss = root.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsBoss == true);
            Require(boss.Attack == 2000 && boss.Health == (purge ? 2000 : 125), "Authored native threat control differs.");
            BattleTurnResult passed = BattleTurnModel.EndTurn(root);
            for (int turn = 1; turn < 3 && passed.Supported && passed.Outcome == RoomOutcome.Exchanged; turn++)
                passed = BattleTurnModel.EndTurn(passed.State!);
            Require(passed.Supported && passed.Outcome == RoomOutcome.PlayerDefeated,
                "This control must require a card recommendation rather than a winning three-turn pass.");
        }
        Require(source.GetProperty("PlannerRuns")[0].GetProperty("Mode").GetString() == "search", "Source did not consume real advice.");
        BattlePlannerChecks.Pair(source, replay);
        BattleAdviceChecks.Native(source, requireBoss: false, calibration: "copy-on-play");
        Console.WriteLine($"PLAYER-TERMINAL-COPY PASS purge={purge} consume={consume} actualPlayerPublication=true defaultHorizon=3 lethalPassControl=true independentFrozenReplay=true");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
