using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HalfHealthMaximumChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        FixtureValue[] samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == "NativeHalfHealthMaximumHealthLifecycle" &&
            root.GetProperty("LiveContextUnchanged").GetBoolean() && root.GetProperty("EffectsDrained").GetBoolean() &&
            root.GetProperty("RestorationDifference").ValueKind == FixtureKind.Null && root.GetProperty("Mismatches").GetInt32() == 0 &&
            samples.Length == 23 && samples.All(sample => sample.GetProperty("Completed").GetBoolean() &&
                sample.GetProperty("Difference").ValueKind == FixtureKind.Null && sample.GetProperty("GameplayRngUnchanged").GetBoolean() &&
                sample.GetProperty("PendingAfterDrain").GetInt32() == 0), "Incomplete native maximum-health lifecycle capture.");
        Require(Difference(root.GetProperty("RngBefore"), root.GetProperty("RngAfter")) == null, "Maximum-health component did not restore every RNG stream.");
        foreach (FixtureValue sample in samples) Verify(sample);
        var byLabel = samples.ToDictionary(sample => sample.GetProperty("Label").GetString()!);
        Expect("buff-heal-leaves-half", 12, 22, 0, 5, 5);
        Expect("buff-heal-remains-half", 7, 22, 0, 0, 0);
        Expect("buff-noheal-enters-half", 11, 22, 2, 0, 5);
        Expect("buff-noheal-leaves-stale-half", 12, 22, 2, 0, 5);
        Expect("buff-zero", 10, 20, 0, 0, 0);
        Expect("buff-negative", 10, 20, 0, 0, 0);
        Expect("debuff-enters-half", 9, 18, 2, 0, 5);
        Expect("debuff-remains-half", 7, 18, 0, 0, 0);
        Expect("debuff-nohp-leaves-half", 10, 18, 2, 0, 5);
        Expect("debuff-clamps-hp", 16, 16, 0, 0, 0);
        Expect("debuff-zero", 10, 20, 0, 0, 0);
        Expect("debuff-negative", 10, 20, 0, 0, 0);
        Expect("buff-capped", 50001, 99999, 0, 5, 5);
        Expect("buff-capped-noheal", 50000, 99999, 0, 0, 0);
        Expect("buff-ignores-heal-immunity", 12, 22, 0, 5, 5);
        Expect("buff-cannotheal-enters-half", 11, 22, 2, 0, 5);
        Expect("buff-onheal-damages-first", 11, 22, 0, 10, 10);
        Expect("buff-gain-heals-loses", 11, 22, 2, 0, 15, 12);
        Expect("debuff-lost-damages-gains", 10, 18, 2, 0, 15, 9);
        Expect("debuff-floor-nohp", 6, 6, 2, 0, 5);
        Expect("buff-onheal-before-loss", 12, 22, 0, 10, 10);
        Expect("buff-heal-multiplier", 14, 23, 0, 5, 5);
        Expect("buff-gain-adds-maxhealth", 11, 22, 2, 0, 10, 12, 23);
        Require(Kinds("buff-onheal-damages-first").SequenceEqual(new[] { "OnHeal", "OnHit" }) &&
            Kinds("buff-onheal-before-loss").SequenceEqual(new[] { "OnHeal", "OnDeathwishLost", "OnDeathwishLost" }) &&
            Kinds("buff-gain-heals-loses").SequenceEqual(new[] { "OnDeathwish", "OnDeathwish", "OnHeal", "OnDeathwishLost", "OnDeathwishLost" }) &&
            Kinds("debuff-lost-damages-gains").SequenceEqual(new[] { "OnDeathwishLost", "OnDeathwishLost", "OnHit", "OnDeathwish", "OnDeathwish" }),
            "Native maximum-health nested queue order was not observed.");
        RoomCombatState guarded = samples[0].GetProperty("Before").Deserialize<RoomCombatState>()!;
        int actor = samples[0].GetProperty("ActorId").GetInt32();
        Require(RoomCombatModel.Validate(guarded) == null,
            "Calibrated half-health battle scheduling was rejected.");
        RoomCombatResult lethal = UnitHealthModel.Apply(guarded, actor, 99999, debuff: true);
        Require(lethal.Supported && lethal.State!.Units.All(unit => unit.Id != actor) &&
            lethal.Dispatches.All(item => item.Kind is not ("OnDeathwish" or "OnDeathwishLost")),
            "Lethal maximum-health sacrifice must skip the ordinary half-health transition.");
        Parallel.For(0, 32, _ => { foreach (FixtureValue sample in samples) Verify(sample); });
        Require(Kinds("buff-gain-adds-maxhealth").SequenceEqual(new[] { "OnDeathwish", "OnDeathwish", "OnDeathwishLost", "OnDeathwishLost" }),
            "Nested maximum-health effect ran its callbacks before previously queued work.");
        Console.WriteLine("HALF-HEALTH-MAXIMUM PASS: 23 native API-return states/queues and 23 drained successors, complete dispatches, healing/no-heal/decrease/floor/cap controls, nested damage/heal/maximum-health callbacks, gameplay RNG and 32 immutable branches. Lethal maximum-health sacrifice/revival has separate native battle acceptance.");

        void Expect(string label, int hp, int max, int queued, int apiGold, int finalGold, int? finalHp = null, int? finalMax = null)
        {
            FixtureValue sample = byLabel[label]; int id = sample.GetProperty("ActorId").GetInt32();
            RoomCombatState before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState api = sample.GetProperty("ApiActual").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = sample.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            CombatUnit unit = api.Units.Single(item => item.Id == id), final = actual.Units.Single(item => item.Id == id);
            Require(unit.Health == hp && unit.MaxHealth == max && final.Health == (finalHp ?? hp) && final.MaxHealth == (finalMax ?? max) &&
                sample.GetProperty("ApiCallbacks").GetArrayLength() == queued && api.Context!.Gold - before.Context!.Gold == apiGold &&
                actual.Context!.Gold - before.Context.Gold == finalGold, "Missing native maximum-health boundary/effect control: " + label);
        }
        string[] Kinds(string label) => byLabel[label].GetProperty("Dispatches").EnumerateArray().Select(item => item.GetProperty("Kind").GetString()!).ToArray();
    }

    private static void Verify(FixtureValue sample)
    {
        string label = sample.GetProperty("Label").GetString()!;
        FixtureValue input = sample.GetProperty("Input");
        RoomCombatState before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        int id = sample.GetProperty("ActorId").GetInt32(), amount = input.GetProperty("Amount").GetInt32();
        bool debuff = input.GetProperty("Debuff").GetBoolean(), heal = input.GetProperty("Heal").GetBoolean();
        bool triggerOnHeal = input.GetProperty("TriggerOnHeal").GetBoolean(), decreaseHp = input.GetProperty("DecreaseHp").GetBoolean();
        int floor = input.GetProperty("Floor").GetInt32();
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        RoomCombatResult api = UnitHealthModel.ApplyNative(before, id, amount, debuff, heal, triggerOnHeal, floor, decreaseHp, true);
        Require(api.Supported, label + ": " + api.UnsupportedReason);
        Require(Difference(api.State, sample.GetProperty("ApiActual")) == null, label + " API state: " + Difference(api.State, sample.GetProperty("ApiActual")));
        Require(Difference(api.Dispatches, sample.GetProperty("ApiDispatches")) == null, label + ": native API dispatch payload/order differs.");
        Require(Difference(api.PendingCallbacks.Select(EnchantmentCallback.From).ToArray(), sample.GetProperty("ApiCallbacks")) == null,
            label + ": native API queued callback payload/order differs.");
        string intermediate = JsonSerializer.Serialize(api.State, ModelJson.Options);
        RoomCombatResult drained = RoomCombatModel.DrainCharacterCallbacks(api.State!, api.PendingCallbacks, true);
        Require(drained.Supported, label + ": " + drained.UnsupportedReason);
        Require(Difference(drained.State, sample.GetProperty("Actual")) == null, label + " drained state: " + Difference(drained.State, sample.GetProperty("Actual")));
        Require(Difference(api.Dispatches.Concat(drained.Dispatches).ToArray(), sample.GetProperty("Dispatches")) == null,
            label + ": native drained dispatch payload/order differs.");
        if (heal && !triggerOnHeal && decreaseHp && floor == 0)
        {
            RoomCombatResult card = UnitHealthModel.Apply(before, id, amount, debuff);
            Require(card.Supported && ModelJson.Difference(JsonSerializer.Serialize(card.State, ModelJson.Options), intermediate) == null &&
                Difference(card.PendingCallbacks.Select(EnchantmentCallback.From).ToArray(), sample.GetProperty("ApiCallbacks")) == null,
                label + ": card-effect maximum-health path differs from its native API oracle.");
        }
        foreach (string phase in new[] { "RngAfterApi", "RngAfter" })
            Require(ModelJson.Difference(HalfHealthLifecycleChecks.GameplayRng(sample.GetProperty("RngBefore")),
                HalfHealthLifecycleChecks.GameplayRng(sample.GetProperty(phase))) == null, label + ": gameplay or Unity RNG changed at " + phase);
        Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent && JsonSerializer.Serialize(api.State, ModelJson.Options) == intermediate,
            label + ": maximum-health or queue drain mutated a parent state.");
    }
    private static string? Difference(object? predicted, FixtureValue actual) => ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options),
        JsonSerializer.Serialize(actual.ToObjectGraph()));
    private static string? Difference(RoomCombatState? predicted, FixtureValue actual) => ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options),
        JsonSerializer.Serialize(actual.Deserialize<RoomCombatState>(), ModelJson.Options));
    private static string? Difference(FixtureValue left, FixtureValue right) => ModelJson.Difference(JsonSerializer.Serialize(left.ToObjectGraph()),
        JsonSerializer.Serialize(right.ToObjectGraph()));
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
