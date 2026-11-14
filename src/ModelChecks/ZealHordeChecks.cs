using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ZealHordeChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); FixtureValue root = document.RootElement;
        FixtureValue[] samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == "NativeZealHordeLifecycle" &&
            root.GetProperty("LiveContextUnchanged").GetBoolean() && root.GetProperty("EffectsDrained").GetBoolean() &&
            root.GetProperty("RestorationDifference").ValueKind == FixtureKind.Null && root.GetProperty("Mismatches").GetInt32() == 0 &&
            samples.Length == 13 && samples.Select(sample => sample.GetProperty("Label").GetString()).Distinct().Count() == 13,
            "Incomplete native Zeal/Horde component.");
        foreach (FixtureValue sample in samples) ZealStatusChecks.Verify(sample);
        var labels = samples.ToDictionary(sample => sample.GetProperty("Label").GetString()!);
        Expect("horde-add", 11, 17, 3);
        Expect("horde-enemy-add", 11, 17, 3);
        Expect("horde-add-wounded-retains-troops", 5, 17, 3);
        Expect("horde-remove-healthy", 14, 16, 3);
        Expect("horde-remove-one-at-threshold", 10, 10, 2);
        Expect("horde-enemy-remove-one", 10, 10, 2);
        Expect("horde-remove-two-leaves-half", 5, 7, 1);
        Expect("horde-remove-two-remains-half", 1, 7, 1);
        Expect("horde-remove-floor-retains-one", 1, 1, 1);
        Expect("horde-remove-zero-keeps-wounded", 5, 18, 3);
        Expect("horde-remove-negative-keeps-wounded", 5, 18, 3);
        Expect("horde-single-remove", 5, 6, 1);
        Expect("horde-remove-all-healthy", 15, 15, 3);
        var changed = labels["horde-remove-two-leaves-half"].GetProperty("ApiCallbacks").EnumerateArray().ToArray();
        int death = Array.FindIndex(changed, row => row.GetProperty("Kind").GetString() == "OnAnyUnitDeathOnFloor");
        int half = Array.FindIndex(changed, row => row.GetProperty("Kind").GetString() == "OnDeathwishLost");
        Require(death >= 0 && half > death && labels["horde-remove-two-leaves-half"].GetProperty("ApiTriggerCounts")
            .EnumerateArray().Take(half).Any(count => count.GetInt32() == 2),
            "Horde casualty callbacks must precede half-health transitions.");
        Parallel.For(0, 32, _ => { foreach (FixtureValue sample in samples) ZealStatusChecks.Verify(sample); });
        Console.WriteLine("ZEAL-HORDE PASS: 13 native API/drained full successors, both teams, clipped/wounded casualties before Deathwish, observer queues/statistics, zero/negative/no-casualty controls, minimum survivor, original attack relic and 32 immutable parallel branches.");
        void Expect(string label, int hp, int maximum, int horde)
        {
            FixtureValue sample = labels[label]; int id = sample.GetProperty("ActorId").GetInt32();
            CombatUnit actor = sample.GetProperty("Actual").Deserialize<RoomCombatState>()!.Units.Single(unit => unit.Id == id);
            Require(actor.Health == hp && actor.MaxHealth == maximum && actor.Status("horde")?.Stacks == horde &&
                actor.HordeDefinition is { Attack: 3, Health: 5 } && actor.Modifiers!.AttackDamage == horde * 3,
                "Missing independent native casualty/control witness: " + label);
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
