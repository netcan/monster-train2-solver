using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PlayerRoomMoonChecks
{
    internal static void MovementPair(FixtureValue source, FixtureValue replay)
    {
        Require(source.GetProperty("EclipseChamberMovement").GetBoolean() && replay.GetProperty("EclipseChamberMovement").GetBoolean(),
            "Missing player room-moon movement captures.");
        Pair(source, replay);
        int moves = 0;
        foreach (FixtureValue row in source.GetProperty("RollingDecisions").EnumerateArray())
            if (row.GetProperty("Action").ValueKind != FixtureKind.Null && EclipseChamberChecks.MovementWitness(
                row.GetProperty("Root").Deserialize<BattleTurnState>()!, row.GetProperty("Actual").Deserialize<BattleTurnState>()!,
                row.GetProperty("Action").Deserialize<PlayCardAction>()!)) moves++;
        Require(moves > 0, "Player advisor never executed a friendly move that changed original room-moon callbacks.");
        Console.WriteLine($"PLAYER-ROOM-MOON-MOVEMENT PASS livePublishedCallbackMoves={moves} exactFrozenReplay=true");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        foreach (FixtureValue fixture in new[] { source, replay })
        {
            FixtureValue player = fixture.GetProperty("PlayerAdvisor");
            FixtureValue overlay = player.GetProperty("Overlay");
            Require(player.GetProperty("Assembly").GetString() == "MonsterTrain2Poju.Advisor" &&
                fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("EclipseChamberPrepared").GetBoolean() &&
                fixture.GetProperty("EclipseChamberShadeCallbacks").GetBoolean(), "Missing winning player room-moon capture.");
            Require(overlay.GetProperty("Captures").GetInt32() > 0 && overlay.GetProperty("PurityFailures").GetInt32() == 0 &&
                overlay.GetProperty("Failures").GetInt32() == 0, "Player room-moon capture was impure or failed.");
            var (characters, support) = EclipseChamberChecks.Refreshes(fixture);
            Require(characters > 0 && support > 0, "Missing original full and support refreshes in the player battle.");
            EnemySpawningChecks.Native(fixture);
        }
        RollingAdviceChecks.FrozenPair(source, replay, calibration: "eclipse-chamber");
        BattleAdviceChecks.Native(source, requireBoss: false, calibration: "eclipse-chamber");
        var phases = new HashSet<string>();
        var actors = new HashSet<int>();
        var maskedEnemyBirths = new HashSet<int>();
        int mixed = 0, casts = 0, newUpgrades = 0, fullUpgrades = 0;
        foreach (FixtureValue row in source.GetProperty("RollingDecisions").EnumerateArray())
        {
            BattleTurnState root = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            foreach (BattleTurnState state in new[] { root, actual })
            {
                phases.Add(state.MoonPhase);
                Require(state.MoonPhaseTrackingActive == true, "Player omitted active moon tracking.");
                foreach (RoomCombatState room in state.Spawn.Train.Rooms)
                    foreach (CombatUnit unit in room.Units.Where(unit => unit.Health > 0 && !unit.IsPyre && unit.Team == CombatTeam.Player))
                    {
                        if (!unit.Triggers.Any(trigger => trigger.Kind == "OnMoonShade")) continue;
                        actors.Add(unit.Id);
                        Require(unit.Triggers.Any(trigger => trigger.Kind == "OnMoonshadeLost") &&
                            unit.Triggers.Any(trigger => trigger.Kind == "OnMoonLit") && unit.Triggers.Any(trigger => trigger.Kind == "OnMoonlitLost"),
                            "Player omitted original Full/New callback payloads.");
                        int mask = RoomMoonPhaseModel.Effective(state.Spawn.Train.Context, room.RoomIndex,
                            state.Spawn.Train.Context!.QueryFrame!.MoonPhase!.Value);
                        Require((unit.MoonPhaseMask & 3) == mask && (unit.MoonPhaseMask & 4) == 4,
                            "Player character understanding differs from global phase plus room mask.");
                        if (mask == 3) mixed++;
                        bool hasNew = unit.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "Nightingale_Upgrade");
                        bool hasFull = unit.Modifiers.Upgrades.Any(upgrade => upgrade.AssetKey == "SeleneAcolyte_MagicPower_Upgrade");
                        Require(hasNew == ((mask & 1) != 0) && hasFull == ((mask & 2) != 0), "Original callback upgrades differ from understanding.");
                        if (hasNew) newUpgrades++;
                        if (hasFull) fullUpgrades++;
                    }
            }
            if (row.GetProperty("Action").ValueKind != FixtureKind.Null)
            {
                PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
                if (root.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.DataId == "0fcb1ed0-49e2-465f-b8fa-444699348dfc") casts++;
            }
        }
        Require(phases.SetEquals(new[] { "New", "Full" }) && actors.Count >= 3 && mixed > 0 && casts > 0 && newUpgrades > 0 && fullUpgrades > 0,
            "Missing both global phases, callback actors, mixed room masks, original Eclipse casts or original upgrades.");
        foreach (FixtureValue row in source.GetProperty("Spawns").EnumerateArray())
        {
            EnemySpawnState before = row.GetProperty("Before").Deserialize<EnemySpawnState>()!;
            EnemySpawnState actual = row.GetProperty("Actual").Deserialize<EnemySpawnState>()!;
            var existing = before.Train.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id).ToHashSet();
            if (before.Train.Context!.QueryFrame!.MoonPhase != 2) continue;
            foreach (RoomCombatState room in actual.Train.Rooms.Where(room => RoomMoonPhaseModel.Effective(actual.Train.Context, room.RoomIndex, 2) == 3))
                foreach (CombatUnit unit in room.Units.Where(unit => unit.Team == CombatTeam.Enemy && !existing.Contains(unit.Id)))
                {
                    Require((unit.MoonPhaseMask & 3) == 2, "Native enemy birth applied the room override before its later refresh.");
                    maskedEnemyBirths.Add(unit.Id);
                }
        }
        Require(maskedEnemyBirths.Count >= 2, "Missing native off-train enemy initialization beneath an active Full/New room mask.");
        Console.WriteLine($"PLAYER-ROOM-MOON PASS authoredCarriers=true originalCallbacks=true phases=2 actors={actors.Count} casts={casts} mixedSnapshots={mixed} newUpgrades={newUpgrades} fullUpgrades={fullUpgrades} maskedEnemyBirths={maskedEnemyBirths.Count}");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
