using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class RoomCombatChecks
{
    internal static void Run(string[] fixtures)
    {
        RoomCombatResult flatFirst = RoomCombatModel.Exchange(Room(Unit(1, CombatTeam.Enemy, 1, 20),
            Unit(2, CombatTeam.Player, 0, 20, new("armor", 10, 1, removeWhenTriggered: true), new("pyregel", 1, 1),
                new("melee weakness", 1, removeWhenTriggered: true))));
        Require(flatFirst.Supported && flatFirst.State!.Units.Single(unit => unit.Id == 2).Status("armor")!.Stacks == 6,
            "Flat pyregel damage was multiplied after rather than before melee weakness.");
        var enemy = Unit(1, CombatTeam.Enemy, 6, 20);
        var player = Unit(2, CombatTeam.Player, 8, 25);
        RoomCombatState root = Room(enemy, player);
        RoomCombatResult basic = RoomCombatModel.Exchange(root);
        Require(basic.State!.Units[0].Health == 12 && basic.State.Units[1].Health == 19,
            "Enemy-first exchange differed.");
        Require(root.Units[0].Health == 20 && root.Units[1].Health == 25, "Room mutated its parent.");

        RoomCombatResult lethal = RoomCombatModel.Exchange(Room(enemy, Unit(2, CombatTeam.Player, 100, 5)));
        Require(lethal.State!.Units.Count == 1 && lethal.State.Units[0].Health == 20,
            "A dead defender attacked.");
        var multi = new CombatStatus("multistrike", 1, 2);
        RoomCombatResult retarget = RoomCombatModel.Exchange(Room(Unit(1, CombatTeam.Enemy, 0, 5),
            Unit(2, CombatTeam.Enemy, 0, 9), Unit(3, CombatTeam.Player, 6, 25, multi)));
        Require(retarget.State!.Units.Count == 2 && retarget.State.Units[0].Id == 2 &&
            retarget.State.Units[0].Health == 3, "Multistrike did not retarget after a kill.");
        RoomCombatResult quick = RoomCombatModel.Exchange(Room(Unit(1, CombatTeam.Enemy, 99, 5),
            Unit(2, CombatTeam.Player, 8, 25, new CombatStatus("ambush", 1))));
        Require(quick.State!.Units.Count == 1 && quick.State.Units[0].Health == 25,
            "Ambush failed to act once before the enemy.");
        RoomCombatResult stealth = RoomCombatModel.Exchange(Room(Unit(1, CombatTeam.Enemy, 6, 20),
            Unit(2, CombatTeam.Player, 8, 25, new CombatStatus("stealth", 1)),
            Unit(3, CombatTeam.Player, 1, 25)));
        Require(stealth.State!.Units[1].Health == 25 && stealth.State.Units[2].Health == 19,
            "Stealth did not remove the front unit from target selection.");
        RoomCombatResult shield = RoomCombatModel.Exchange(Room(Unit(1, CombatTeam.Enemy, 6, 20, multi),
            Unit(2, CombatTeam.Player, 0, 25, new CombatStatus("damage shield", 1, removeWhenTriggered: true),
                new CombatStatus("armor", 4, 1, removeWhenTriggered: true))));
        Require(shield.State!.Units[1].Health == 23 && shield.State.Units[1].Statuses.Count == 0,
            "Shield did not precede armor across successive attacks.");
        RoomCombatResult dazed = RoomCombatModel.Exchange(Room(
            Unit(1, CombatTeam.Enemy, 6, 20, new CombatStatus("dazed", 2, removeWhenTriggered: true)), player));
        Require(dazed.State!.Units[0].Statuses[0].Stacks == 1 && dazed.State.Units[1].Health == 25,
            "Dazed did not consume a stack and skip action.");
        RoomCombatResult sniper = RoomCombatModel.Exchange(Room(
            Unit(1, CombatTeam.Enemy, 6, 20, new CombatStatus("sniper", 1)), player,
            Unit(3, CombatTeam.Player, 1, 25)));
        Require(sniper.State!.Units[1].Health == 25 && sniper.State.Units[2].Health == 19,
            "Sniper did not select the back unit.");
        RoomCombatResult fragile = RoomCombatModel.Exchange(Room(enemy,
            Unit(2, CombatTeam.Player, 0, 25, new CombatStatus("fragile", 1))));
        Require(fragile.State!.Units.Count == 1, "Fragile survived positive damage.");
        RoomCombatResult protectedFragile = RoomCombatModel.Exchange(Room(enemy,
            Unit(2, CombatTeam.Player, 0, 25, new CombatStatus("fragile", 1),
                new CombatStatus("armor", 8, 1, removeWhenTriggered: true))));
        Require(protectedFragile.State!.Units[1].Health == 25,
            "Fragile incorrectly died when armor prevented all damage.");
        RoomCombatResult retaliation = RoomCombatModel.Exchange(Room(
            Unit(1, CombatTeam.Enemy, 8, 5, new CombatStatus("sweep", 1)),
            Unit(2, CombatTeam.Player, 0, 25, new CombatStatus("spikes", 5, 1)),
            Unit(3, CombatTeam.Player, 0, 25)));
        Require(retaliation.State!.Units.Count == 2 && retaliation.State.Units[0].Health == 17 &&
            retaliation.State.Units[1].Health == 17, "A dying sweep attacker lost a pending target before native removal.");

        var relentless = new CombatStatus("relentless", 1);
        RoomCombatResult boss = RoomCombatModel.Resolve(Room(Unit(1, CombatTeam.Enemy, 6, 20, relentless), player));
        Require(boss.Rounds == 3 && boss.State!.Units.Count == 1 && boss.State.Units[0].Health == 7,
            "Relentless did not repeat exchanges until a team died.");
        RoomCombatResult frozen = RoomCombatModel.Resolve(Room(Unit(1, CombatTeam.Enemy, 0, 20, relentless),
            Unit(2, CombatTeam.Player, 0, 25)));
        Require(frozen.Outcome == RoomOutcome.Stalemate && frozen.Rounds == 1,
            "A state cycle in relentless was not detected.");
        RoomCombatResult changingCycle = RoomCombatModel.Resolve(Room(Unit(1, CombatTeam.Enemy, 6, 20, relentless),
            Unit(2, CombatTeam.Player, 0, 5, new CombatStatus("stealth", 1,
                removeStackAtEnd: true, removeAfterPostCombat: true))));
        Require(changingCycle.Rounds == 2 && changingCycle.Outcome != RoomOutcome.Stalemate &&
            changingCycle.State!.Units.Count == 1, "Status decay was pruned as a false relentless cycle.");
        var poisonedPyre = new CombatUnit(2, "pyre", CombatTeam.Player, 0, 3, 3, true, true, false,
            [new CombatStatus("poison", 5, 1)]);
        Require(RoomCombatModel.Resolve(Room(Unit(1, CombatTeam.Enemy, 0, 20), poisonedPyre)).Outcome ==
            RoomOutcome.PlayerDefeated, "Post-combat Pyre death did not terminate the battle.");
        RoomCombatResult decay = RoomCombatModel.Resolve(Room(Unit(1, CombatTeam.Enemy, 0, 20),
            Unit(2, CombatTeam.Player, 1, 20, new CombatStatus("buff", 2, 2, removeStackAtEnd: true))));
        Require(decay.State!.Units[0].Health == 15 && decay.State.Units[1].Attack == 3,
            "Rage did not decay after combat.");
        RoomCombatResult regen = RoomCombatModel.Resolve(Room(Unit(1, CombatTeam.Enemy, 6, 20),
            Unit(2, CombatTeam.Player, 1, 20,
                new CombatStatus("regen", 3, 1, removeStackAtEnd: true))));
        Require(regen.State!.Units[1].Health == 17 && regen.State.Units[1].Statuses[0].Stacks == 2,
            "Regeneration and decay order differed.");
        Require(!RoomCombatModel.Resolve(Room(Unit(1, CombatTeam.Enemy, 1, 5,
            new CombatStatus("unknown", 1)), player)).Supported, "An unmodeled status was silently accepted.");
        Require(!RoomCombatModel.Resolve(new RoomCombatState(0, false, new[] { enemy, player },
            new[] { "death trigger" })).Supported, "An external trigger was silently accepted.");
        Parallel.For(0, 64, _ => Require(RoomCombatModel.Exchange(root).State!.Units[1].Health == 19,
            "Parallel expansion changed state."));
        var cooldown = new CombatStatus("cooldown", 1, removeStackAtEnd: true, preventRemovalDuringRelentless: true);
        var soloBoss = RoomCombatModel.Resolve(Room(Unit(1, CombatTeam.Enemy, 0, 20, relentless, cooldown),
            Unit(2, CombatTeam.Enemy, 0, 20, cooldown)));
        Require(soloBoss.Supported && soloBoss.State!.Units.All(unit => unit.Statuses.Any(status => status.Id == "cooldown")),
            "An enemy relentless status must preserve room cooldowns even without an opposing team.");
        Require(RoomCombatModel.Resolve(Room(Unit(1, CombatTeam.Enemy, 0, 20, cooldown))).State!.Units[0].Statuses.Count == 0 &&
            !RoomCombatModel.Resolve(Room(Unit(1, CombatTeam.Player, 0, 20, relentless, cooldown))).State!.Units[0].Statuses.Any(status => status.Id == "cooldown"),
            "Ordinary rooms and player-only relentless must still decay cooldown.");
        Console.WriteLine("ROOM-CHECKS PASS: initiative, targeting, defenses, retaliation, decay, relentless, isolation.");
        foreach (string fixture in fixtures) CheckNativeFixture(fixture);
    }

    private static CombatUnit Unit(int id, CombatTeam team, int attack, int health, params CombatStatus[] statuses)
        => new(id, "unit-" + id, team, attack, health, health, true, false, false, statuses);
    private static RoomCombatState Room(params CombatUnit[] units) => new(0, false, units, Array.Empty<string>());
    private static void Require(bool condition, string error)
    { if (!condition) throw new InvalidOperationException(error); }

    private static void CheckNativeFixture(string path)
    {
        using FixtureDocument document = ModelJson.ReadFixture(path);
        FixtureValue fixture = document.RootElement;
        Require(fixture.GetProperty("NativeWon").ValueKind is FixtureKind.True or FixtureKind.False,
            "Native fixture did not reach the end of the battle.");
        Require(fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0, "Fixture capture was incomplete.");
        if (fixture.GetProperty("Schema").GetInt32() >= 12)
            Require(fixture.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
                fixture.GetProperty("TerminalEffectsSettled").GetBoolean(), "Native terminal effects were not settled.");
        int matched = 0, unsupported = 0;
        foreach (FixtureValue stage in fixture.GetProperty("Stages").EnumerateArray())
        {
            RoomCombatState state = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatResult result = stage.GetProperty("Kind").GetString() == "Exchange"
                ? RoomCombatModel.Exchange(state) : RoomCombatModel.Resolve(state);
            if (!result.Supported) { unsupported++; continue; }
            // The oracle is the captured game state, not the recorded model prediction.
            CombatUnit[] actual = stage.GetProperty("Actual").GetProperty("Units").Deserialize<CombatUnit[]>()!;
            Require(JsonSerializer.Serialize(result.State!.Units) == JsonSerializer.Serialize(actual),
                "Native room difference at stage " + stage.GetProperty("Index") + " in " + path);
            if (state.Context != null)
            {
                CombatContext actualContext = stage.GetProperty("Actual").GetProperty("Context")
                    .Deserialize<CombatContext>()!;
                Require(JsonSerializer.Serialize(result.State.Context) == JsonSerializer.Serialize(actualContext),
                    "Native room context difference at stage " + stage.GetProperty("Index"));
            }
            matched++;
        }
        Require(matched > 0, "No native stages were verified.");
        Console.WriteLine($"NATIVE-ROOM-CHECKS PASS: {matched} matched, {unsupported} unsupported; {path}");
        CardCycleChecks.Native(fixture);
        TrainCombatChecks.Native(fixture);
        EnemySpawningChecks.Native(fixture);
        BattleTurnChecks.Native(fixture);
        BattleActionChecks.Native(fixture);
        UnitIdentityChecks.Native(fixture);
        SpawnPointChecks.Native(fixture);
        SharedPileChecks.Native(fixture);
        DamageScalingChecks.Native(fixture);
        StatusScalingChecks.Native(fixture);
        UnitUpgradeScalingChecks.Native(fixture);
        UnitTriggerUpgradeChecks.Native(fixture);
        SpawnTriggerChecks.Native(fixture);
        UnitTurnBeginChecks.Native(fixture);
        TeamTurnBeginChecks.Native(fixture);
        PreHandDiscardChecks.Native(fixture);
        PreCombatChecks.Native(fixture);
        TriggeredHealingChecks.Native(fixture);
        PostCombatHealingChecks.Native(fixture);
        TriggeredDamageChecks.Native(fixture);
        DamageDeathQueueChecks.Native(fixture);
        TerminalDeathChecks.Native(fixture);
        HitKillChecks.Native(fixture);
        DyingUpgradeChecks.Native(fixture);
        AttackTriggerChecks.Native(fixture);
        StatusCallbackChecks.Native(fixture);
        TriggeredStatusChecks.Native(fixture);
        StatusRegistryChecks.Native(fixture);
        StatisticCacheChecks.Native(fixture);
        CloneUpgradeRefreshChecks.Native(fixture);
        DynamicStatisticChecks.Native(fixture);
        EnergyChecks.Native(fixture);
        CardCostChecks.Native(fixture);
        BonusDrawChecks.Native(fixture);
        RoomCapacityChecks.Native(fixture);
        DirectUnitUpgradeChecks.Native(fixture);
        EquipmentChecks.Native(fixture);
        ConditionalTriggerChecks.Native(fixture);
        TriggerRepeatChecks.Native(fixture);
        CompanionBossChecks.Native(fixture);
        SentryChecks.Native(fixture);
        AbilityLifecycleChecks.Native(fixture);
        AbilityEffectChecks.Native(fixture);
        AbilityUpgradeChecks.Native(fixture);
        HordeUpgradeChecks.Native(fixture);
        DyingHordeUpgradeChecks.Native(fixture);
        HordeStatusChecks.Native(fixture);
        RallyChecks.Native(fixture);
        LethalRallyChecks.Native(fixture);
        UnitSummonChecks.Native(fixture);
        HarvestChecks.Native(fixture);
        HordeRemovalChecks.Native(fixture);
        HordeDeathChecks.Native(fixture);
        AbilityCooldownChecks.Native(fixture);
        AbilityCardChecks.Native(fixture);
        UnitAbilityChecks.Native(fixture);
        TriggerUpgradeChecks.Native(fixture);
        SharedPileChecks.MigratedRoot(fixture);
    }
}
