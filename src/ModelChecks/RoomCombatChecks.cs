using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class RoomCombatChecks
{
    internal static void NativeRoomOnly(string path) => CheckNativeFixture(path, roomOnly: true);

    internal static void Run(string[] fixtures)
    {
        var decayStatus = new CombatStatus("decay", 2, 3, removeStackAtEnd: true);
        var decayRoot = Room(Unit(1, CombatTeam.Enemy, 0, 20, decayStatus,
            new CombatStatus("armor", 4, 1, removeWhenTriggered: true)));
        RoomCombatResult decayDamage = RoomCombatModel.Resolve(decayRoot);
        Require(decayDamage.Supported && decayDamage.State!.Units.Single().Health == 18 &&
            decayDamage.State.Units.Single().Status("armor") == null &&
            decayDamage.State.Units.Single().Status("decay")?.Stacks == 1 &&
            decayDamage.Events.Any(item => item.Kind == "Decay" && item.Amount == 2) &&
            decayRoot.Units.Single().Health == 20 && decayRoot.Units.Single().Status("decay")?.Stacks == 2,
            "Decay must deal three damage per stack through armor before its one-stack end-turn removal, without mutating its parent.");
        RoomCombatResult deploymentDecay = RoomCombatModel.Resolve(new RoomCombatState(0, true,
            [Unit(1, CombatTeam.Enemy, 0, 20, decayStatus)], []));
        Require(deploymentDecay.Supported && deploymentDecay.State!.Units.Single().Health == 14 &&
            deploymentDecay.State.Units.Single().Status("decay")?.Stacks == 2,
            "Decay should damage during deployment while retaining its stacks until an ordinary end turn.");
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
        var trample = new CombatStatus("trample", 1);
        RoomCombatResult trampleChain = RoomCombatModel.Exchange(Room(Unit(1, CombatTeam.Enemy, 0, 3),
            Unit(2, CombatTeam.Enemy, 0, 4), Unit(3, CombatTeam.Enemy, 0, 20),
            Unit(4, CombatTeam.Player, 10, 30, trample)));
        Require(trampleChain.Supported && trampleChain.State!.Units.Single(unit => unit.Id == 3).Health == 17 &&
            trampleChain.Events.Where(item => item.Kind is "Attack" or "Trample").Select(item => item.Target)
                .SequenceEqual([1, 2, 3]), "Trample did not recursively carry excess damage through physical order.");
        RoomCombatResult trampleBlocked = RoomCombatModel.Exchange(Room(Unit(1, CombatTeam.Enemy, 0, 3,
            new CombatStatus("armor", 10, 1, removeWhenTriggered: true)), Unit(2, CombatTeam.Enemy, 0, 20),
            Unit(3, CombatTeam.Player, 10, 30, trample)));
        Require(trampleBlocked.Supported && trampleBlocked.State!.Units.Single(unit => unit.Id == 2).Health == 20 &&
            !trampleBlocked.Events.Any(item => item.Kind == "Trample"),
            "Armor-blocked damage incorrectly trampled into the next unit.");
        RoomCombatResult trampleStealth = RoomCombatModel.Exchange(Room(Unit(1, CombatTeam.Enemy, 0, 3),
            Unit(2, CombatTeam.Enemy, 0, 4, new CombatStatus("stealth", 1)),
            Unit(3, CombatTeam.Enemy, 0, 20), Unit(4, CombatTeam.Player, 10, 30, trample)));
        Require(trampleStealth.Supported && trampleStealth.State!.Units.Single(unit => unit.Id == 2).Health == 4 &&
            trampleStealth.State.Units.Single(unit => unit.Id == 3).Health == 13,
            "Trample did not skip a stealthed unit behind the original target.");
        RoomCombatResult enemyTrample = RoomCombatModel.Exchange(Room(Unit(1, CombatTeam.Enemy, 9, 30, trample),
            Unit(2, CombatTeam.Player, 0, 3), Unit(3, CombatTeam.Player, 0, 4),
            Unit(4, CombatTeam.Player, 0, 20)));
        Require(enemyTrample.Supported && enemyTrample.State!.Units.Single(unit => unit.Id == 4).Health == 18 &&
            enemyTrample.Events.Count(item => item.Kind == "Trample") == 2,
            "Enemy Trample did not recurse through player defenders.");
        RoomCombatResult trampleSpikes = RoomCombatModel.Exchange(Room(Unit(1, CombatTeam.Enemy, 0, 3),
            Unit(2, CombatTeam.Enemy, 0, 20, new CombatStatus("spikes", 1, 2)),
            Unit(3, CombatTeam.Player, 10, 30, trample)));
        Require(trampleSpikes.Supported && trampleSpikes.State!.Units.Single(unit => unit.Id == 2).Health == 13 &&
            trampleSpikes.State.Units.Single(unit => unit.Id == 3).Health == 28,
            "Trample damage failed to trigger the second defender's Spikes.");
        var burstStatus = new CombatStatus("burst", 1, 2);
        CombatUnit burstActor = Unit(2, CombatTeam.Player, 6, 100, burstStatus);
        RoomCombatResult burstOnly = RoomCombatModel.Exchange(Room(Unit(1, CombatTeam.Enemy, 0, 100), burstActor));
        Require(burstActor.Attacks == 2 && burstOnly.Supported && burstOnly.Events.Count(item => item.Kind == "Attack" && item.Actor == 2) == 2 &&
            burstOnly.State!.Units.Single(unit => unit.Id == 1).Health == 88 && burstOnly.State.Units.Single(unit => unit.Id == 2).Status("burst") == null,
            "Burst did not add and consume one attack.");
        RoomCombatResult burstWithMultistrike = RoomCombatModel.Exchange(Room(Unit(1, CombatTeam.Enemy, 0, 100),
            Unit(2, CombatTeam.Player, 6, 100, multi, burstStatus)));
        Require(burstWithMultistrike.Supported && burstWithMultistrike.Events.Count(item => item.Kind == "Attack" && item.Actor == 2) == 3 &&
            burstWithMultistrike.State!.Units.Single(unit => unit.Id == 1).Health == 82 &&
            burstWithMultistrike.State.Units.Single(unit => unit.Id == 2).Status("burst") == null &&
            burstWithMultistrike.State.Units.Single(unit => unit.Id == 2).Status("multistrike")?.Stacks == 1,
            "Burst did not add an attack after ordinary Multistrike and consume at the correct threshold.");
        RoomCombatResult burstWithoutTarget = RoomCombatModel.Exchange(Room(Unit(2, CombatTeam.Player, 6, 100, burstStatus)));
        Require(burstWithoutTarget.Supported && burstWithoutTarget.State!.Units.Single().Status("burst")?.Stacks == 1,
            "Burst was consumed without a target for its attack.");
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
        RoomCombatResult spellShieldMelee = RoomCombatModel.Exchange(Room(
            Unit(1, CombatTeam.Enemy, 0, 20, new CombatStatus("spell shield", 2, removeWhenTriggered: true)),
            Unit(2, CombatTeam.Player, 6, 20)));
        Require(spellShieldMelee.Supported && spellShieldMelee.State!.Units[0].Health == 14 &&
            spellShieldMelee.State.Units[0].Status("spell shield")?.Stacks == 2,
            "A normal attack consumed Spell Shield.");
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
        var burnout = new CombatTrigger("OnBurnout", false, false, false, 1, Array.Empty<CombatEffect>(), skipDuringDeployment: false);
        var death = new CombatTrigger("OnDeath", false, false, false, 1, Array.Empty<CombatEffect>());
        CombatUnit ephemeralUnit = new(3, "ephemeral-unit", CombatTeam.Player, 0, 4, 4, true, false, false,
            [new CombatStatus("ephemeral", 1, removeWhenTriggered: true)], [burnout, death],
            deathState: new UnitDeathState(false, false, true, isSacrifice: false, statisticsListenerOnce: true,
                isDespawned: false, isDestroyed: false));
        RoomCombatResult burnedOut = RoomCombatModel.Resolve(Room(ephemeralUnit));
        Require(burnedOut.Supported && burnedOut.State!.Units.Count == 0 &&
            burnedOut.Dispatches.Select(item => item.Kind).SequenceEqual(["OnBurnout", "OnDeath"]) &&
            burnedOut.Events.Any(item => item.Kind == "Death" && item.Target == ephemeralUnit.Id),
            $"Ephemeral did not run OnBurnout before its sacrifice death: supported={burnedOut.Supported}, " +
            $"outcome={burnedOut.Outcome}, units={string.Join(',', burnedOut.State?.Units.Select(item => item.Id) ?? [])}, " +
            $"triggers={string.Join(',', burnedOut.Dispatches.Select(item => item.Kind))}, " +
            $"events={string.Join(',', burnedOut.Events.Select(item => item.Kind))}, reason={burnedOut.UnsupportedReason}");
        RoomCombatResult layeredEphemeral = RoomCombatModel.Resolve(Room(Unit(4, CombatTeam.Player, 0, 4,
            new CombatStatus("ephemeral", 2, removeWhenTriggered: true))));
        Require(layeredEphemeral.Supported && layeredEphemeral.State!.Units.Single().Status("ephemeral")?.Stacks == 1,
            "Ephemeral did not consume one stack per post-combat trigger.");
        CombatUnit poisonedEphemeral = new(5, "poisoned-ephemeral", CombatTeam.Player, 0, 1, 1, true, false, false,
            [new CombatStatus("poison", 1, 1), new CombatStatus("ephemeral", 1, removeWhenTriggered: true)], [burnout]);
        RoomCombatResult poisonDeath = RoomCombatModel.Resolve(Room(poisonedEphemeral));
        Require(poisonDeath.Supported && !poisonDeath.Dispatches.Any(item => item.Kind == "OnBurnout"),
            "A unit killed by poison still triggered Ephemeral.");
        CombatUnit undyingEphemeral = new(6, "undying-ephemeral", CombatTeam.Player, 0, 4, 4, true, false, false,
            [new CombatStatus("undying", 1), new CombatStatus("ephemeral", 1, removeWhenTriggered: true)],
            [new CombatTrigger("OnReanimated", false, false, false, 1, Array.Empty<CombatEffect>(), skipDuringDeployment: false), death]);
        RoomCombatResult revivedEphemeral = RoomCombatModel.Resolve(Room(undyingEphemeral));
        CombatUnit revivedUnit = revivedEphemeral.State!.Units.Single();
        Require(revivedEphemeral.Supported && revivedUnit.Health == 1 && revivedUnit.Status("undying") == null &&
            revivedUnit.Status("ephemeral")?.Stacks == 1 && revivedEphemeral.Dispatches.Any(item => item.Kind == "OnReanimated") &&
            revivedEphemeral.Dispatches.Any(item => item.Kind == "OnDeath" && item.ParamInt == 1),
            "Undying did not revive an ephemeral unit and restore one Ephemeral stack.");
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

    private static void CheckNativeFixture(string path, bool roomOnly = false)
    {
        using FixtureDocument document = ModelJson.ReadFixture(path);
        FixtureValue fixture = document.RootElement;
        ContextReferenceChecks.Native(fixture);
        Require(fixture.GetProperty("NativeWon").ValueKind is FixtureKind.True or FixtureKind.False,
            "Native fixture did not reach the end of the battle.");
        Require(fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0, "Fixture capture was incomplete.");
        if (fixture.GetProperty("Schema").GetInt32() >= 12)
            Require(fixture.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
                fixture.GetProperty("TerminalEffectsSettled").GetBoolean(), "Native terminal effects were not settled.");
        int matched = 0, unsupported = 0;
        string? firstUnsupported = null;
        foreach (FixtureValue stage in fixture.GetProperty("Stages").EnumerateArray())
        {
            RoomCombatState state = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
            CombatUnit? externalPyre = stage.TryGetProperty("ExternalPyre", out FixtureValue pyreValue) &&
                pyreValue.ValueKind != FixtureKind.Null ? pyreValue.Deserialize<CombatUnit>() : null;
            int externalPyreRoomIndex = externalPyre == null ? -1 : stage.GetProperty("ExternalPyreRoomIndex").GetInt32();
            RoomCombatResult result = stage.GetProperty("Kind").GetString() == "Exchange"
                ? RoomCombatModel.Exchange(state) : RoomCombatModel.Resolve(state,
                    externalPyre: externalPyre, externalPyreRoomIndex: externalPyreRoomIndex);
            if (!result.Supported) { unsupported++; firstUnsupported ??= result.UnsupportedReason; continue; }
            // The oracle is the captured game state, not the recorded model prediction.
            CombatUnit[] actual = stage.GetProperty("Actual").GetProperty("Units").Deserialize<CombatUnit[]>()!;
            string? unitDifference = ModelJson.Difference(JsonSerializer.Serialize(result.State!.Units),
                JsonSerializer.Serialize(actual));
            Require(unitDifference == null, "Native room difference at stage " +
                stage.GetProperty("Index").GetInt32() + " in " + path + ": " + unitDifference);
            if (externalPyre != null)
            {
                CombatUnit actualPyre = stage.GetProperty("ActualExternalPyre").Deserialize<CombatUnit>()!;
                string? pyreDifference = ModelJson.Difference(JsonSerializer.Serialize(result.RetainedUnits
                    .Single(unit => unit.Id == externalPyre.Id)), JsonSerializer.Serialize(actualPyre));
                Require(pyreDifference == null, "Native external Pyre difference at stage " +
                    stage.GetProperty("Index").GetInt32() + ": " + pyreDifference);
            }
            if (state.Context != null)
            {
                CombatContext actualContext = stage.GetProperty("Actual").GetProperty("Context")
                    .Deserialize<CombatContext>()!;
                string? contextDifference = ModelJson.Difference(
                    JsonSerializer.Serialize(BattleTurnChecks.ComparableContext(result.State.Context)),
                    JsonSerializer.Serialize(BattleTurnChecks.ComparableContext(actualContext)));
                Require(contextDifference == null, "Native room context difference at stage " +
                    stage.GetProperty("Index").GetInt32() + ": " + contextDifference);
            }
            matched++;
        }
        Require(matched > 0, "No native stages were verified; unsupported=" + unsupported +
            (firstUnsupported == null ? string.Empty : "; first reason: " + firstUnsupported));
        Require(unsupported == 0, "Native room verification skipped unsupported stages in " + path);
        Console.WriteLine($"NATIVE-ROOM-CHECKS PASS: {matched} matched, {unsupported} unsupported; {path}");
        if (fixture.TryGetProperty("ModifierScenario", out FixtureValue scenario) &&
            scenario.GetString() == "titan-skin-combat")
            TitanSkinChecks.Native(fixture);
        if (roomOnly) return;
        CardCycleChecks.Native(fixture);
        TrainCombatChecks.Native(fixture);
        EnemySpawningChecks.Native(fixture);
        BattleTurnChecks.Native(fixture);
        RevivalChecks.Native(fixture);
        BumpChecks.Native(fixture);
        UnitCloneChecks.Native(fixture);
        UnitCopyChecks.Native(fixture);
        HeroCopyChecks.Native(fixture);
        SpawnEnchantChecks.Native(fixture);
        PersistentEnchantmentChecks.Native(fixture);
        BattleActionChecks.Native(fixture);
        ForgePointsChecks.Native(fixture);
        TypedDrawNativeChecks.Native(fixture);
        CardBranchMaskChecks.Native(fixture);
        if (fixture.TryGetProperty("ModifierScenario", out var relicScenario) && relicScenario.GetString() == "original-relic-batch")
            OriginalRelicBatchChecks.Native(fixture);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-sting-relics")
            OriginalStingRelicBatchChecks.Native(fixture, required: true);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-deployable-relic")
            OriginalDeployableRelicChecks.Native(fixture, required: true);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-unit-relic-batch")
            OriginalUnitRelicBatchChecks.Native(fixture, required: true);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-trigger-relic-batch")
            OriginalTriggerRelicBatchChecks.Native(fixture, required: true);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-fused-pyreshard")
            OriginalFusedPyreshardChecks.Native(fixture, required: true);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() is "original-status-relic-batch" or "original-status-relic-batch-burnout")
            OriginalStatusRelicBatchChecks.Native(fixture, required: true);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-magnitude-relic-batch")
            OriginalMagnitudeRelicBatchChecks.Native(fixture, required: true);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-lifecycle-relic-batch")
            OriginalLifecycleRelicBatchChecks.Native(fixture, required: true);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-decay-relic-batch")
            OriginalDecayRelicBatchChecks.Native(fixture, required: true);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-resource-relic-batch")
            OriginalResourceRelicBatchChecks.Native(fixture, required: true);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "pyre-resource-refresh")
            PyreResourceRefreshChecks.Native(fixture, required: true);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-floor-capacity-relics")
            OriginalFloorCapacityRelicChecks.Native(fixture, required: true);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-endless-return")
            EndlessReturnChecks.Native(fixture);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-more-spikes")
            OriginalMoreSpikesChecks.Native(fixture);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-stealth-slay")
            OriginalStealthSlayChecks.Native(fixture);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-relic-count-attack")
            OriginalRelicCountAttackChecks.Native(fixture);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-card-attack-relic")
            RelicCardAttackChecks.Native(fixture);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-card-type-cost-relic")
            OriginalCardTypeCostChecks.Native(fixture);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-upgrade-count-relics")
            RelicUpgradeCountChecks.Native(fixture);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-healing-power-relic")
            OriginalHealingPowerChecks.Native(fixture);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-start-combat-energy-relic")
            OriginalStartCombatEnergyChecks.Native(fixture);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-forge-conservation-relic")
            RelicForgeConservationChecks.Native(fixture);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-opening-corruption-relics")
            RelicOpeningCorruptionChecks.Native(fixture);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-spell-play-threshold-relics")
            OriginalSpellPlayThresholdChecks.Native(fixture);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() == "original-combat-trigger-relics")
            OriginalCombatTriggerBatchChecks.Native(fixture);
        else if (fixture.TryGetProperty("ModifierScenario", out relicScenario) && relicScenario.GetString() is "card-type-cost-discount-first" or "card-type-cost-upgrade-first")
            RelicCardTypeCostOrderChecks.Native(fixture);
        else RelicCardModifierChecks.Native(fixture, requireScenarioCoverage:
            !fixture.TryGetProperty("ModifierScenario", out var integrationScenario) || integrationScenario.GetString() != "typed-draw");
        UnitIdentityChecks.Native(fixture);
        SpawnPointChecks.Native(fixture);
        BattleSpawnPointChecks.Native(fixture);
        TriggeredSummonChecks.Native(fixture);
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
        PurifyChecks.Native(fixture);
        StatusRegistryChecks.Native(fixture);
        StatisticCacheChecks.Native(fixture);
        CloneUpgradeRefreshChecks.Native(fixture);
        DynamicStatisticChecks.Native(fixture);
        EnergyChecks.Native(fixture);
        if (fixture.TryGetProperty("ModifierScenario", out var conserveScenario) &&
            conserveScenario.GetString() == "original-conserve-ember-relic")
            ConserveEmberChecks.Native(fixture);
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
        HordeMergeChecks.Native(fixture);
        RallyChecks.Native(fixture);
        IncantChecks.Native(fixture);
        RelicChecks.Native(fixture);
        TriggerCountChecks.Native(fixture);
        RelicSpawnStatusChecks.Native(fixture);
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
