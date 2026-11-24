using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalEndlessFirstWaveChecks
{
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

    internal static void Forecast(FixtureValue archive)
    {
        Require(Text(archive, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(archive, "Boundary") == "OriginalEndlessEnemyScaling" &&
            !Flag(archive.GetProperty("Scenario"), "ControlledFirstLoop"), "Expected the original Endless first battle.");
        BattleTurnState deployment = archive.GetProperty("Deployment").Deserialize<BattleTurnState>()!;
        BattleTurnResult advanced = BattleTurnModel.EndTurn(deployment);
        Require(advanced.Supported, advanced.UnsupportedReason ?? "The original first wave must be supported.");
        RecordedBattleChecks.RecordedBoundary(advanced.State!, archive.GetProperty("FirstDecision"),
            terminal: false, "original Endless first wave", trackPreviewPosition: true);
        Console.WriteLine("NATIVE-ENDLESS-FORECAST PASS: complete deployment successor and original first-wave RNG.");
    }

    private static int Number(FixtureValue value, string field) => value.GetProperty(field).GetInt32();
    private static string? Text(FixtureValue value, string field) => value.GetProperty(field).GetString();
    private static bool Flag(FixtureValue value, string field) => value.GetProperty(field).GetBoolean();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}
