using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HordeMaximumChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        FixtureValue[] samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == "NativeHordeMaximumHealthLifecycle" &&
            root.GetProperty("LiveContextUnchanged").GetBoolean() && root.GetProperty("EffectsDrained").GetBoolean() &&
            root.GetProperty("RestorationDifference").ValueKind == FixtureKind.Null && root.GetProperty("Mismatches").GetInt32() == 0 &&
            samples.Length == 22 && samples.Select(sample => sample.GetProperty("Label").GetString()).Distinct().Count() == 22,
            "Incomplete current-version Horde maximum-health component.");
        Equal(root.GetProperty("RngBefore"), root.GetProperty("RngAfter"), "Component did not restore all RNG streams.");
        foreach (FixtureValue sample in samples) ZealStatusChecks.Verify(sample, maximumHealth: true);
        var byLabel = samples.ToDictionary(sample => sample.GetProperty("Label").GetString()!);
        Expect("horde-buff-heal-leaves-half", 9, 17, 3);
        Expect("horde-buff-heal-remains-half", 5, 17, 3);
        Expect("horde-buff-noheal-enters-half", 8, 17, 3);
        Expect("horde-buff-cannotheal-enters-half", 8, 17, 3);
        Expect("horde-buff-onheal-before-loss", 9, 17, 3);
        Expect("horde-buff-heal-multiplier", 9, 18, 3);
        Expect("horde-buff-ignores-heal-immunity", 9, 17, 3);
        Expect("horde-debuff-one", 8, 8, 2);
        Expect("horde-enemy-debuff-one", 8, 8, 2);
        Expect("horde-debuff-two-leaves-half", 5, 7, 1);
        Expect("horde-debuff-two-remains-half", 3, 7, 1);
        Expect("horde-debuff-clips-hp", 10, 10, 2);
        Expect("horde-debuff-floor-retains-one", 1, 1, 1);
        Expect("horde-debuff-zero-keeps-wounded", 5, 15, 3);
        Expect("horde-debuff-negative-keeps-wounded", 5, 15, 3);
        Expect("horde-debuff-healthy", 14, 17, 3);
        Expect("horde-buff-zero-stale-half", 5, 15, 3);
        Expect("horde-buff-negative-stale-half", 5, 15, 3);
        Expect("horde-buff-capped", 50002, 99999, 3);
        Expect("horde-buff-onheal-damages-troops", 7, 12, 2);
        Expect("horde-buff-gain-damages-troops", 7, 12, 2);
        Expect("horde-buff-loss-damages-troops", 7, 12, 2);
        foreach (string label in new[] { "horde-debuff-two-leaves-half", "horde-debuff-floor-retains-one" })
        {
            FixtureValue sample = byLabel[label];
            string[] kinds = sample.GetProperty("ApiCallbacks").EnumerateArray().Select(item => item.GetProperty("Kind").GetString()!).ToArray();
            Require(kinds.SequenceEqual(["OnAnyMonsterDeathOnFloor", "OnAnyUnitDeathOnFloor", "OnAnyMonsterDeathOnFloor",
                    "OnAnyUnitDeathOnFloor", "OnStatusEffectChanged", "OnTroopRemoved", "OnDeathwishLost", "OnDeathwishLost"]) &&
                sample.GetProperty("ApiTriggerCounts").EnumerateArray().Select(count => count.GetInt32()).SequenceEqual([2, 2, 2, 2, 1, 1, 1, 1]),
                "Horde casualty payloads/multiplicities must precede Deathwish: " + label);
        }
        Require(Kinds("horde-buff-onheal-before-loss").SequenceEqual(["OnHeal", "OnDeathwishLost", "OnDeathwishLost"]),
            "OnHeal must finish before the final maximum-health Deathwish update.");
        Require(Kinds("horde-buff-onheal-damages-troops").SequenceEqual(["OnHeal", "OnBloodshed", "OnHit",
            "OnAnyMonsterDeathOnFloor", "OnAnyUnitDeathOnFloor", "OnAnyMonsterDeathOnFloor", "OnAnyUnitDeathOnFloor",
            "OnStatusEffectChanged", "OnTroopRemoved", "OnPunish", "OnDeathwishLost", "OnDeathwishLost"]),
            "Nested OnHeal damage must settle troop casualties before Punish/Deathwish callbacks.");
        Parallel.For(0, 32, _ => { foreach (FixtureValue sample in samples) ZealStatusChecks.Verify(sample, maximumHealth: true); });
        Console.WriteLine("HORDE-MAXIMUM PASS: 22 native full API/queue/drained successors, both teams, healability/multiplier/immunity/cap/floor, casualties before Deathwish, nested OnHeal/Deathwish damage, observer multiplicities/statistics, all-RNG restoration and 32 immutable parallel branches. Lethal sacrifice/revival remains separate.");

        void Expect(string label, int hp, int maximum, int troops)
        {
            FixtureValue sample = byLabel[label]; int id = sample.GetProperty("ActorId").GetInt32();
            RoomCombatState actual = sample.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            CombatUnit actor = actual.Units.Single(unit => unit.Id == id);
            Require(actor.Health == hp && actor.MaxHealth == maximum && actor.Status("horde")?.Stacks == troops &&
                actor.Modifiers!.AttackDamage == 3 * troops, "Missing independent Horde health/casualty control: " + label);
        }
        string[] Kinds(string label) => byLabel[label].GetProperty("Dispatches").EnumerateArray().Select(item => item.GetProperty("Kind").GetString()!).ToArray();
    }
    private static void Equal(FixtureValue left, FixtureValue right, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(left.ToObjectGraph()), JsonSerializer.Serialize(right.ToObjectGraph()));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
