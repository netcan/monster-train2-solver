using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HalfHealthLifecycleChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        FixtureValue[] samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == "NativeHalfHealthDamageHealLifecycle" &&
            root.GetProperty("LiveContextUnchanged").GetBoolean() && root.GetProperty("EffectsDrained").GetBoolean() &&
            root.GetProperty("RestorationDifference").ValueKind == FixtureKind.Null &&
            root.GetProperty("Mismatches").GetInt32() == 0 && samples.Length == 13 &&
            samples.All(sample => sample.GetProperty("Completed").GetBoolean() && sample.GetProperty("GameplayRngUnchanged").GetBoolean() &&
                sample.GetProperty("Difference").ValueKind == FixtureKind.Null),
            "Incomplete or mismatched half-health damage/heal lifecycle capture.");
        Require(ModelJson.Difference(JsonSerializer.Serialize(root.GetProperty("RngBefore").ToObjectGraph()),
            JsonSerializer.Serialize(root.GetProperty("RngAfter").ToObjectGraph())) == null, "Native lifecycle did not restore all RNG streams.");
        foreach (FixtureValue sample in samples) Verify(sample);
        var byLabel = samples.ToDictionary(sample => sample.GetProperty("Label").GetString()!);
        // Native reward adjustment maps this scenario's authored 2/3/5/7 rewards
        // to five gold each. Dispatch payloads independently identify each source.
        Expect("damage-enters-half", 10, 10, "OnHit", "OnDeathwish", "OnDeathwish");
        Expect("damage-remains-half", 9, 5, "OnHit");
        Expect("damage-zero-enters-half", 10, 5, "OnDeathwish", "OnDeathwish");
        Expect("damage-armor-blocked", 11, 5, "OnStatusEffectChanged", "OnHit");
        Expect("heal-leaves-half", 11, 10, "OnHeal", "OnDeathwishLost", "OnDeathwishLost");
        Expect("heal-remains-half", 9, 5, "OnHeal");
        Expect("heal-zero-enters-half", 10, 10, "OnHeal", "OnDeathwish", "OnDeathwish");
        Expect("heal-clipped", 20, 5, "OnHeal");
        Expect("heal-zero-leaves-half", 11, 10, "OnHeal", "OnDeathwishLost", "OnDeathwishLost");
        Expect("damage-onhit-heals-first", 11, 10, "OnHit", "OnHeal");
        Expect("heal-onheal-damages-first", 10, 10, "OnHeal", "OnHit");
        Expect("gain-callback-heals-and-loses", 11, 20, "OnHit", "OnDeathwish", "OnDeathwish", "OnHeal", "OnDeathwishLost", "OnDeathwishLost");
        Expect("lost-callback-damages-and-gains", 10, 20, "OnHeal", "OnDeathwishLost", "OnDeathwishLost", "OnHit", "OnDeathwish", "OnDeathwish");
        foreach (FixtureValue sample in samples)
        {
            RoomCombatState before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
            int id = sample.GetProperty("ActorId").GetInt32();
            Require(RoomCombatModel.Validate(before)?.Contains("HalfHealth", StringComparison.Ordinal) == true,
                "Uncalibrated whole-room lifecycle was opened by the damage/heal component.");
            Require(!UnitHealthModel.Apply(before, id, 99999, debuff: true).Supported,
                "Lethal maximum-health sacrifice/revival lost its native calibration guard.");
            RoomCombatResult lethal = RoomCombatModel.ApplyCardDamage(before, id, 99999);
            Require(!lethal.Supported && lethal.UnsupportedReason?.Contains("death/revival", StringComparison.Ordinal) == true,
                "Uncalibrated half-health lethal damage/revival was accepted.");
        }
        Parallel.For(0, 32, _ => { foreach (FixtureValue sample in samples) Verify(sample); });
        Console.WriteLine("HALF-HEALTH-LIFECYCLE PASS: 13 complete native damage/heal successors and dispatch payloads; zero/blocked/clipped controls, OnHit/OnHeal ordering, nested gain/loss effects, parent immutability and 32 parallel branches. Lethal and whole-room lifecycles remain guarded; maximum health has separate acceptance.");

        void Expect(string label, int hp, int gold, params string[] kinds)
        {
            FixtureValue sample = byLabel[label];
            RoomCombatState before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = sample.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            int id = sample.GetProperty("ActorId").GetInt32();
            FixtureValue[] dispatches = sample.GetProperty("Dispatches").EnumerateArray().ToArray();
            Require(actual.Units.Single(unit => unit.Id == id).Health == hp && actual.Context!.Gold - before.Context!.Gold == gold &&
                dispatches.Select(item => item.GetProperty("Kind").GetString()).SequenceEqual(kinds), "Missing native ordering/effect control: " + label);
            Require(dispatches.Where(item => item.GetProperty("Kind").GetString() is "OnDeathwish" or "OnDeathwishLost")
                .All(item => item.GetProperty("ExclusiveTriggerStateId").ValueKind != FixtureKind.Null),
                "Half-health callback lost its exclusive source identity: " + label);
        }
    }

    private static void Verify(FixtureValue sample)
    {
        string label = sample.GetProperty("Label").GetString()!;
        RoomCombatState before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        RoomCombatState actual = sample.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        int id = sample.GetProperty("ActorId").GetInt32(), amount = sample.GetProperty("Amount").GetInt32();
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        RoomCombatResult result = sample.GetProperty("Operation").GetString() == "Heal" ?
            RoomCombatModel.ApplyCardHeal(before, id, amount, captureAllDispatches: true) :
            RoomCombatModel.ApplyCardDamage(before, id, amount, captureAllDispatches: true);
        Require(result.Supported, label + ": " + result.UnsupportedReason);
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
        Require(ModelJson.Difference(JsonSerializer.Serialize(result.Dispatches, ModelJson.Options),
            JsonSerializer.Serialize(sample.GetProperty("Dispatches").ToObjectGraph())) == null,
            "Complete native dispatch payload/order differs: " + label);
        Require(ModelJson.Difference(GameplayRng(sample.GetProperty("RngBefore")), GameplayRng(sample.GetProperty("RngAfter"))) == null,
            "Native lifecycle changed gameplay or Unity RNG: " + label);
        Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "A half-health lifecycle branch mutated its parent: " + label);
    }
    internal static string GameplayRng(FixtureValue value)
    {
        var node = JsonNode.Parse(JsonSerializer.Serialize(value.ToObjectGraph()))!.AsObject();
        JsonArray streams = node["Streams"]!.AsArray();
        foreach (JsonNode stream in streams.ToArray()!)
            if (stream["Id"]!.GetValue<string>() is "NonDeterministic" or "Chatter") streams.Remove(stream);
        return node.ToJsonString();
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
