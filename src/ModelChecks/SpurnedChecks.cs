using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class SpurnedChecks
{
    private const string Scenario = "SoulSavior_R4_BossBattle_EstrangedChild_2";
    private const string BossKey = TriggeredStatusModel.OriginalSpurned;

    internal static void FullBattle(string path)
    {
        using var fixture = ModelJson.ReadFixture(path);
        FixtureValue root = fixture.RootElement;
        FixtureValue identity = root.GetProperty("OriginalSpurnedBattle");
        string bossKey = identity.GetProperty("Scenario").GetString() switch
        {
            Scenario => BossKey,
            "SoulSavior_R4_BossBattle_EstrangedChild_3" => TriggeredStatusModel.OriginalSpurnedWitherbloom,
            _ => throw new InvalidOperationException("Expected an original supported Spurned battle.")
        };
        bool witherbloom = bossKey == TriggeredStatusModel.OriginalSpurnedWitherbloom;
        Require(root.GetProperty("ModifierScenario").GetString() == "original-spurned-battle" &&
            identity.GetProperty("BossKey").GetString() == bossKey &&
            identity.GetProperty("TestScenario").GetBoolean(), "Expected the controlled original Spurned battle.");
        if (witherbloom)
            Require(identity.GetProperty("CompanionKeys").GetArrayLength() == 0,
                "Expected the original Witherbloom scenario with no companion boss.");

        bool MatchesBoss(CombatUnit unit) => unit.AssetKey == bossKey &&
            unit.Triggers.Any(trigger => TriggeredStatusModel.MatchesSpurnedAttack(unit, trigger)) &&
            (!witherbloom || unit.Triggers.Any(TriggeredStatusModel.MatchesSpurnedWitherbloomTurnBegin));
        FixtureValue[] turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        Require(turns.Length >= 2, "The original Spurned battle must capture its opening and later turns.");
        BattleTurnState opening = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        OuterBossSpawnDefinition? queued = opening.Spawn.QueuedOuterBoss;
        Require(opening.Spawn.OuterBoss == null && queued is { Boss: { Phase: "Casting", Room: -1, UnitId: 0 } } &&
            queued.Boss.AssetKey == bossKey && MatchesBoss(queued.Actor.Unit),
            "The opening must contain the original queued boss and its complete trigger definitions.");

        BattleTurnState[] actual = turns.Select(turn => turn.GetProperty("Actual").Deserialize<BattleTurnState>()!).ToArray();
        OuterBossProgress? first = actual[0].Spawn.OuterBoss;
        Require(first is { Phase: "Casting", Room: >= 0, UnitId: > 0 } && first.AssetKey == bossKey &&
            actual[0].Spawn.Train.Rooms.Single(room => room.RoomIndex == first.Room)
                .Units.Any(unit => unit.Id == first.UnitId && MatchesBoss(unit)),
            "The original boss must enter a physical floor in Casting with its complete native projection.");
        OuterBossProgress?[] phases = actual.Select(turn => turn.Spawn.OuterBoss).ToArray();
        Require(phases.Zip(phases.Skip(1)).Any(pair => pair.First?.Phase == "Casting" && pair.Second?.Phase == "Relentless" &&
                pair.First.UnitId == pair.Second.UnitId && pair.Second.AssetKey == bossKey),
            "The original installed boss must naturally advance from Casting to Relentless.");
        int pyreRoom = opening.PlayRules!.Rooms.Single(room => room.IsPyre).RoomIndex;
        Require(phases.Any(phase => phase?.Phase == "Relentless" && phase.AssetKey == bossKey &&
            phase.UnitId == first!.UnitId && phase.Room == pyreRoom), "The captured Relentless boss never reached the Pyre.");
        RoomCombatState[] rooms = actual.SelectMany(turn => turn.Spawn.Train.Rooms).ToArray();
        Require(rooms.SelectMany(room => room.Units)
            .Any(unit => unit.Team == CombatTeam.Player && unit.Status(StatusAttackDamageModel.Malaise)?.Stacks > 0 &&
                unit.Status(RoomCombatModel.Witherbloom)?.Stacks > 0),
            "No original complete-turn successor contains an affected player unit with both statuses.");
        if (witherbloom)
        {
            Require(rooms.Any(room => room.Units.Any(unit => unit.Id == first!.UnitId && unit.Status(RoomCombatModel.Witherbloom)?.Stacks > 0) &&
                room.Units.Any(unit => unit.Team == CombatTeam.Player && unit.Status(RoomCombatModel.Witherbloom)?.Stacks > 0)),
                "No original complete-turn successor contains Witherbloom on both Spurned and a player unit on its floor.");
            Require(rooms.SelectMany(room => room.Units).Any(unit => unit.Id == first!.UnitId &&
                unit.BumpRules is { OuterBoss: true, OuterBossRelentlessPhase: true } && unit.Triggers.Count == 0),
                "The original Relentless snapshot must show both Casting triggers removed.");
        }

        SolverStageChecks.Native(path, requireVictory: false);
        Console.WriteLine($"NATIVE-SPURNED-BATTLE PASS: {bossKey}, queued and placed Casting, ordinary status combat, Relentless and Pyre arrival.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == "NativeOriginalSpurnedAttack" &&
            root.GetProperty("Scenario").GetString() == Scenario &&
            root.GetProperty("BossKey").GetString() == BossKey &&
            root.GetProperty("TestScenario").GetBoolean(), "Expected a controlled native Spurned witness.");

        CombatUnit before = Boss(root, "BeforeMalaise");
        CombatUnit after = Boss(root, "AfterMalaise");
        CombatUnit removed = Boss(root, "AfterRemoval");
        CombatStatus malaise = after.RegisteredStatus(StatusAttackDamageModel.Malaise)!;
        Require(malaise.Stacks == 2 && removed.RegisteredStatus(malaise.Id)?.Stacks == 0 &&
            before.MaxHealth - after.MaxHealth == 2 && after.Health == after.MaxHealth &&
            removed.MaxHealth == before.MaxHealth && removed.Health == after.Health,
            "The native Malaise witness must clip the boss and restore its upper limit without healing.");

        var room = new RoomCombatState(0, false, [before], []);
        RoomCombatResult added = StatusApplicationModel.ApplyRetained(room, before.Id, malaise, 0);
        Require(added.Supported, added.UnsupportedReason ?? "Malaise application failed.");
        CompareMalaise(added.State!.Units.Single(), after);
        RoomCombatResult clear = StatusRemovalModel.Remove(added.State, before.Id, malaise.Id, -1);
        Require(clear.Supported, clear.UnsupportedReason ?? "Malaise removal failed.");
        CompareMalaise(clear.State!.Units.Single(), removed);
        Console.WriteLine("NATIVE-SPURNED-MALAISE PASS: exact native stack, HP, maximum HP, and attack accumulator changes.");

        Witherbloom(root);
        Attack(root);
    }

    private static void Attack(FixtureValue root)
    {
        Require(root.GetProperty("Attacks").GetInt32() == 1 && root.GetProperty("PhaseBefore").GetString() == "Casting" &&
            root.GetProperty("PhaseAtAttack").GetString() == "Casting", "Expected the native, already active Spurned attack.");
        CombatUnit actor = Boss(root, "BeforeAttack");
        CombatTrigger trigger = actor.Triggers.Single(item => item.Kind == "OnAttacking");
        Require(TriggeredStatusModel.MatchesSpurnedAttack(actor, trigger), "The original Spurned attack payload changed.");
        TrainCombatState train = root.GetProperty("BeforeAttack").GetProperty("Train").Deserialize<TrainCombatState>()!;
        RoomCombatState captured = train.Rooms.Single(item => item.Units.Any(unit => unit.Id == actor.Id));
        Require(captured.ExternalInteractions.SequenceEqual(["Outer train boss"]), "Expected an already present exterior boss.");
        // Native RunUnitTurn is a narrower operation than the room's outer-boss scheduler.
        var room = new RoomCombatState(captured.RoomIndex, captured.Deployment, captured.Units, [], train.Context,
            captured.Preview, captured.ActiveCompanionBossIds);
        RoomCombatResult attack = RoomCombatModel.ApplyUnitTurn(room, actor.Id);
        Require(attack.Supported, attack.UnsupportedReason ?? "Modeled Spurned attack failed.");
        foreach (string role in new[] { "Boss", "Target" })
        {
            CombatUnit native = root.GetProperty("AfterAttack").GetProperty(role).Deserialize<CombatUnit>()!;
            CombatUnit modeled = attack.State!.Units.Single(unit => unit.Id == native.Id);
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(modeled), JsonSerializer.Serialize(native));
            Require(difference == null, $"Modeled Spurned attack changed native {role} state at {difference}.");
        }
        TrainCombatState nativeTrain = root.GetProperty("AfterAttack").GetProperty("Train").Deserialize<TrainCombatState>()!;
        Require(Equals(attack.State!.Context!.BattleRng, nativeTrain.Context!.BattleRng) &&
            Equals(nativeTrain.Context.BattleRng, train.Context!.BattleRng),
            "The deterministic native status count must not advance Battle RNG.");
        Console.WriteLine("NATIVE-SPURNED-ATTACK PASS: ordinary attack and both ordered count-based applications.");
    }

    private static void Witherbloom(FixtureValue root)
    {
        Require(root.GetProperty("TickFired").GetBoolean() && root.GetProperty("TickStage").GetString() == "OnPostCombatPoison",
            "Expected a native Witherbloom tick at the post-combat poison stage.");
        FixtureValue rule = root.GetProperty("WitherbloomRule");
        Require(rule.GetProperty("ParamInt").GetInt32() == 1 && rule.GetProperty("Stage").GetString() == "OnPostCombatPoison" &&
            rule.GetProperty("AdditionalStages").GetArrayLength() == 0 && !rule.GetProperty("RemoveOnTrigger").GetBoolean() &&
            !rule.GetProperty("RemoveStackAtEnd").GetBoolean() && !rule.GetProperty("RemoveAllAtEnd").GetBoolean(),
            "The recorded Witherbloom rule has a different magnitude, dispatch stage or decay.");
        FixtureValue before = root.GetProperty("AfterAttack");
        CombatUnit owner = before.GetProperty("Target").Deserialize<CombatUnit>()!;
        TrainCombatState train = before.GetProperty("Train").Deserialize<TrainCombatState>()!;
        RoomCombatState room = train.Rooms.Single(item => item.Units.Any(unit => unit.Id == owner.Id)).WithContext(train.Context);
        FixtureValue[] hits = root.GetProperty("TickDamage").EnumerateArray().ToArray();
        CombatUnit[] native = hits.Select(hit => hit.GetProperty("After").Deserialize<CombatUnit>()!).ToArray();
        Require(native.Length == 2 && native[0].Id == owner.Id && native[1].Id == Boss(root, "AfterAttack").Id &&
            hits.All(hit => hit.GetProperty("Amount").GetInt32() == 2), "Expected the native owner and adjacent boss in order.");
        RoomCombatResult tick = RoomCombatModel.ApplyWitherbloomTick(room, owner.Id);
        Require(tick.Supported, tick.UnsupportedReason ?? "Witherbloom tick failed.");
        Require(tick.Events.Where(item => item.Kind == "Poison").Select(item => item.Target).SequenceEqual(native.Select(unit => unit.Id)),
            "Modeled Witherbloom recipient order differs from native damage.");
        foreach (CombatUnit target in native)
        {
            CombatUnit modeled = tick.State!.Units.Single(unit => unit.Id == target.Id);
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(modeled), JsonSerializer.Serialize(target));
            Require(difference == null, $"Modeled Witherbloom victim {target.Id} differs from native at {difference}.");
        }
        Console.WriteLine("NATIVE-SPURNED-WITHERBLOOM PASS: exact owner and adjacent boss poison results, physical recipient order and persistent status.");
    }

    private static CombatUnit Boss(FixtureValue root, string stage) =>
        root.GetProperty(stage).GetProperty("Boss").Deserialize<CombatUnit>()!;

    private static void CompareMalaise(CombatUnit modeled, CombatUnit native)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(modeled), JsonSerializer.Serialize(native));
        Require(difference == null, "Modeled Malaise actor differs from the native boss at " + difference);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
