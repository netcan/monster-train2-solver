using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalEndlessFirstWaveChecks
{
    internal static void Factors(FixtureValue archive)
    {
        Require(Text(archive, "GameVersion") == Game231FixtureCatalog.Version &&
            Text(archive, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(archive, "Boundary") == "OriginalEndlessEnemyScalingFactors" &&
            Number(archive, "RunLength") == 8 && archive.GetProperty("RngBefore").ContentEquals(archive.GetProperty("RngAfter")),
            "Expected the pinned native factor queries with unchanged gameplay RNG.");
        FixtureValue[] rows = archive.GetProperty("Factors").EnumerateArray().ToArray();
        string[] fields = { "Attack", "Health", "Status", "BuffDamage", "Heal", "Damage" };
        Require(rows.Length == 24 && Enumerable.Range(0, rows.Length).All(index =>
            Number(rows[index], "Loop") == index / 8 + 1 && Number(rows[index], "Distance") == index % 8 &&
            fields.All(field => rows[index].GetProperty(field).GetDouble() is >= 1 and < 1000)),
            "Expected six native balance queries across each of the 24 loop/distance pairs.");
        Require(rows[0].GetProperty("Health").GetDouble() == 1.5 &&
            rows[1].GetProperty("Health").GetDouble() == 2 &&
            rows[8].GetProperty("Damage").GetDouble() > rows[7].GetProperty("Damage").GetDouble(),
            "Expected the measured early-ring health and the next-loop damage increase.");
        var eligibility = archive.GetProperty("StatusEligibility").EnumerateArray()
            .ToDictionary(row => Text(row, "Id")!, row => Flag(row, "ScalesInEndless"));
        Require(eligibility.Count == 90 && eligibility.Count(entry => entry.Value) == 26 &&
            eligibility["armor"] && eligibility["titanskin"] && eligibility["buff"] && !eligibility["relentless"],
            "The calibrated status eligibility changed.");
        Console.WriteLine("NATIVE-ENDLESS-FACTORS PASS: 24 original balance points, 90 status records and unchanged gameplay RNG.");
    }

    internal static void Native(FixtureValue transition, FixtureValue natural, FixtureValue controlled)
    {
        foreach (FixtureValue item in new[] { transition, natural, controlled })
            Require(Text(item, "GameVersion") == Game231FixtureCatalog.Version &&
                Text(item, "GameModuleMvid") == Game231FixtureCatalog.Module, "Expected the pinned game for all Endless observations.");

        FixtureValue scenario = natural.GetProperty("Scenario");
        FixtureValue control = controlled.GetProperty("Scenario");
        Require(Text(transition, "Boundary") == "NativeEndlessTransition" && Text(transition, "RunType") == "Class" &&
            Flag(transition, "IsEndlessRun") && !Flag(transition, "InTestScenario") && !Flag(transition, "WasDevCheatUsed") &&
            Number(transition, "Loop") == 1 && Number(transition, "Distance") == 0 &&
            Text(natural, "Boundary") == "OriginalEndlessEnemyScaling" && Text(controlled, "Boundary") == "OriginalEndlessEnemyScaling" &&
            !Flag(scenario, "ControlledFirstLoop") && Flag(control, "ControlledFirstLoop") &&
            Text(scenario, "OwnerKey") == "Enemy_EndlessScaling", "Expected a genuine game-over transition and a separate controlled loop.");
        foreach (string field in new[] { "Scenario", "Loop", "Distance", "RunLength" })
            Require(transition.GetProperty(field).ContentEquals(scenario.GetProperty(field)), "The resumed battle changed the original transition " + field);
        foreach (string field in new[] { "Scenario", "Loop", "Distance", "RunLength", "OwnerId", "OwnerKey", "Attack",
            "Health", "Status", "BuffDamage", "Heal", "Damage", "ExcludedSubtypes" })
            Require(scenario.GetProperty(field).ContentEquals(control.GetProperty(field)), "Native first-loop configuration differs for " + field);

        FixtureValue[] observed = natural.GetProperty("Births").EnumerateArray().ToArray();
        FixtureValue[] controls = controlled.GetProperty("Births").EnumerateArray().ToArray();
        Require(observed.Length == 3 && controls.Length == observed.Length, "Expected all three original enemy births.");
        for (int index = 0; index < observed.Length; index++)
        {
            FixtureValue birth = observed[index];
            FixtureValue other = controls[index];
            bool heavy = index < 2;
            Require(Text(birth, "AssetKey") == (heavy ? "HeavyT1_LoseHealthOnMove" : "ClericT1_OminousAcolyte_Chosen") &&
                !Flag(birth, "FromCard") && Number(birth, "BaselineAttack") == (heavy ? 4 : 0) &&
                Number(birth, "BaselineHealth") == 80, "The original first wave changed at birth " + index);
            foreach (string field in new[] { "AssetKey", "BaselineAttack", "BaselineHealth", "RelicAttackBonus", "LateAttackMultiplier" })
                Require(birth.GetProperty(field).ContentEquals(other.GetProperty(field)), "Birth input differs for " + field);
            FixtureValue before = birth.GetProperty("Before"), after = birth.GetProperty("After");
            Require(Number(before, "Attack") == (heavy ? 3 : 0) && Number(before, "Health") == (heavy ? 90 : 30) &&
                Number(after, "Attack") == (heavy ? 5 : 0) && Number(after, "Health") == 120 && Number(after, "MaximumHealth") == 120 &&
                before.GetProperty("Rng").ContentEquals(after.GetProperty("Rng")), "Unexpected native scaling or RNG at birth " + index);
            foreach (string checkpoint in new[] { "Before", "After" })
                foreach (string field in new[] { "Attack", "Health", "MaximumHealth", "Statuses", "Triggers" })
                    Require(birth.GetProperty(checkpoint).GetProperty(field).ContentEquals(other.GetProperty(checkpoint).GetProperty(field)),
                        "Scaled actor differs for " + checkpoint + " " + field);
        }
        Console.WriteLine("NATIVE-ENDLESS-FIRST-WAVE PASS: original victory transition, six factor queries, three births and unchanged callback RNG.");
    }

    internal static void Forecast(FixtureValue archive, FixtureValue? outerBirth = null)
    {
        FixtureValue scenario = archive.GetProperty("Scenario");
        string? boundary = Text(archive, "Boundary");
        Require(Text(archive, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            !Flag(scenario, "ControlledFirstLoop") && Number(scenario, "Loop") == 1 &&
            (boundary == "OriginalEndlessEnemyScaling" && Number(scenario, "Distance") == 0 ||
                boundary == "OriginalEndlessEnemyScalingControlledNextRing" && Number(scenario, "Distance") is 1 or 2),
            "Expected one of the pinned original Endless opening waves.");
        BattleTurnState deployment = archive.GetProperty("Deployment").Deserialize<BattleTurnState>()!;
        BattleTurnResult advanced = BattleTurnModel.EndTurn(deployment);
        Require(advanced.Supported, advanced.UnsupportedReason ?? "The original first wave must be supported.");
        RecordedBattleChecks.RecordedBoundary(advanced.State!, archive.GetProperty("FirstDecision"),
            terminal: false, "original Endless first wave", trackPreviewPosition: true);
        if (Number(scenario, "Distance") == 1)
        {
            var births = archive.GetProperty("Births").EnumerateArray().ToArray();
            Require(births.Length == 4 && births.All(row => row.GetProperty("Before").GetProperty("Rng").ContentEquals(
                row.GetProperty("After").GetProperty("Rng"))), "Expected four original birth callbacks with unchanged Battle and BattleTest.");
            CombatUnit[] enemies = advanced.State!.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(actor => actor.Team == CombatTeam.Enemy).ToArray();
            CombatUnit enchanter = enemies.Single(actor => actor.AssetKey == "ShieldGuardT3_Enchanter_Chosen");
            Require(enchanter.Health == 600 && enchanter.Status("armor")?.Stacks == 240 &&
                enchanter.Status("titanskin")?.Stacks == 30 && enemies.Count(actor => actor.Status("titanskin")?.Stacks == 10) == 3,
                "Original starting statuses and aura recipients must match.");
        }
        if (Number(scenario, "Distance") == 2)
        {
            var births = archive.GetProperty("Births").EnumerateArray().ToArray();
            Require(births.Select(row => (Text(row, "AssetKey"), Number(row.GetProperty("After"), "Attack"),
                Number(row.GetProperty("After"), "Health"))).SequenceEqual(new[] {
                    ("HeavyT1_Basic", 10, 250), ("ClericT1_OminousAcolyte", 0, 20), ("ClericT3_IncantAttack_Chosen", 0, 600)
                }) && births.All(row => row.GetProperty("Before").GetProperty("Rng").ContentEquals(
                    row.GetProperty("After").GetProperty("Rng"))), "Expected the three original Ring 3 births and unchanged callback RNG.");
            OuterBossProgress? boss = advanced.State!.Spawn.OuterBoss;
            CombatUnit? actor = advanced.State.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.Id == boss?.UnitId);
            Require(boss is { AssetKey: "Boss_Level3_Arkion_PushAttack", Room: 2 } &&
                actor is { Attack: 40, Health: 6250, MaxHealth: 6250 }, "The original Endless Arkion birth must settle at 40 attack and 6250 HP.");
            if (outerBirth is { } nativeBoss)
            {
                OuterBossBirthChecks.Native(nativeBoss, Game231FixtureCatalog.Module);
                FixtureValue scale = nativeBoss.GetProperty("Births")[0].GetProperty("EndlessScaling");
                FixtureValue before = scale.GetProperty("Before"), after = scale.GetProperty("After");
                Require(Text(scale, "AssetKey") == boss!.AssetKey && Number(scale, "BaselineAttack") == 25 &&
                    Number(scale, "BaselineHealth") == 2500 && Number(before, "Attack") == 13 && Number(before, "Health") == 425 &&
                    Number(after, "Attack") == actor!.Attack && Number(after, "Health") == actor.Health &&
                    before.GetProperty("Rng").ContentEquals(after.GetProperty("Rng")),
                    "The live Arkion callback must record the ordered before/after stats and unchanged RNG.");
            }
        }
        Console.WriteLine("NATIVE-ENDLESS-FORECAST PASS: complete deployment successor and original first-wave RNG.");
    }

    internal static void Waves(FixtureValue archive)
    {
        Require(Text(archive, "GameVersion") == Game231FixtureCatalog.Version &&
            Text(archive, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(archive, "Boundary") == "OriginalEndlessEnemyScalingWaves" &&
            !Flag(archive.GetProperty("Scenario"), "ControlledFirstLoop"), "Expected the original post-victory Endless wave capture.");
        FixtureValue[] waves = archive.GetProperty("Waves").EnumerateArray().ToArray();
        FixtureValue[] turns = archive.GetProperty("Turns").EnumerateArray().ToArray();
        Require(waves.Length == 6 && turns.Length == waves.Length && Number(archive, "CompletedWaves") == waves.Length,
            "Expected a native EndTurn for every scheduled wave.");

        int births = 0;
        for (int index = 0; index < waves.Length; index++)
        {
            FixtureValue wave = waves[index], turn = turns[index];
            Require(Number(wave, "Index") == index && Number(turn, "Index") == index &&
                Flag(wave, "PhaseAdvanced"), "The scheduled wave order changed.");
            FixtureValue actual = turn.GetProperty("Actual");
            Require(actual.GetProperty("Spawn").GetProperty("Phase").GetInt32() == index + 1,
                "The native turn must settle exactly one wave.");
            var root = turn.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var decisions = turn.GetProperty("RelentlessDecisions").Deserialize<IReadOnlyList<RelentlessDecision>>();
            BattleTurnResult next = BattleTurnModel.EndTurn(root, decisions);
            Require(next.Supported, "Wave " + (index + 1) + ": " + next.UnsupportedReason);
            RecordedBattleChecks.RecordedBoundary(next.State!, actual,
                terminal: false, "original Endless wave " + (index + 1), trackPreviewPosition: true);
            foreach (FixtureValue birth in wave.GetProperty("Births").EnumerateArray())
            {
                births++;
                Require(birth.GetProperty("Before").GetProperty("Rng").ContentEquals(
                    birth.GetProperty("After").GetProperty("Rng")), "An observed birth changed callback RNG.");
            }
        }
        Require(births == 21, "The original six-wave roster changed.");
        Console.WriteLine("NATIVE-ENDLESS-WAVES PASS: six exact EndTurns and 21 ordered native birth callbacks.");
    }

    private static int Number(FixtureValue value, string field) => value.GetProperty(field).GetInt32();
    private static string? Text(FixtureValue value, string field) => value.GetProperty(field).GetString();
    private static bool Flag(FixtureValue value, string field) => value.GetProperty(field).GetBoolean();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}
