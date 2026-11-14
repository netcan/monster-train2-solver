using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HalfHealthUpgradeChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        FixtureValue[] samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == "NativeHalfHealthUnitUpgradeLifecycle" &&
            root.GetProperty("LiveContextUnchanged").GetBoolean() && root.GetProperty("EffectsDrained").GetBoolean() &&
            root.GetProperty("RestorationDifference").ValueKind == FixtureKind.Null && root.GetProperty("Mismatches").GetInt32() == 0 &&
            samples.Length == 27 && samples.All(sample => sample.GetProperty("Completed").GetBoolean() &&
                sample.GetProperty("Difference").ValueKind == FixtureKind.Null && sample.GetProperty("GameplayRngUnchanged").GetBoolean() &&
                sample.GetProperty("PendingAfterDrain").GetInt32() == 0), "Incomplete native half-health unit-upgrade capture.");
        Require(Difference(root.GetProperty("RngBefore"), root.GetProperty("RngAfter")) == null, "Upgrade component did not restore every RNG stream.");
        foreach (FixtureValue sample in samples) Verify(sample);
        var byLabel = samples.ToDictionary(sample => sample.GetProperty("Label").GetString()!);
        Expect("add-heal-leaves-half", 12, 22, 0, 5, 5);
        Expect("add-unhealed-enters-half", 11, 22, 0, 5, 5);
        Expect("add-negative-enters-half", 9, 18, 0, 5, 5);
        Expect("add-negative-unhealed-enters-half", 9, 18, 0, 5, 5);
        Expect("add-two-phase-loss-gain", 12, 26, 0, 10, 10);
        Expect("add-gain-heals-after-second", 10, 20, 0, 10, 10);
        Expect("add-loss-damages-before-second", 11, 26, 0, 15, 15);
        Expect("add-zero-stale-half", 10, 20, 0, 0, 0);
        Expect("add-cannotheal-enters-half", 11, 22, 0, 5, 5);
        Expect("add-ignores-heal-immunity", 12, 22, 0, 5, 5);
        Expect("add-heal-multiplier", 14, 23, 0, 5, 5);
        Expect("add-capped", 50001, 99999, 0, 5, 5);
        Expect("add-two-phase-remains-half", 7, 26, 0, 0, 0);
        Expect("remove-healed-leaves-half", 10, 18, 2, 0, 5);
        Expect("remove-unhealed-leaves-half", 10, 18, 2, 0, 5);
        Expect("remove-negative-healed-leaves-half", 12, 22, 0, 5, 5);
        Expect("remove-negative-unhealed-enters-half", 11, 22, 2, 0, 5);
        Expect("remove-two-phase-enters-half", 13, 26, 2, 0, 5);
        Expect("remove-two-phase-loss-gain", 10, 22, 4, 0, 10);
        Expect("remove-loss-damages-before-second", 11, 26, 0, 15, 15);
        Expect("remove-associated-health", 10, 18, 2, 0, 5);
        Expect("remove-ledger-after-gain-loss", 11, 18, 4, 0, 10);
        Expect("remove-three-phase-loss-gain-loss", 12, 22, 4, 5, 15);
        Expect("remove-cannotheal-enters-half", 11, 22, 2, 0, 5);
        Expect("remove-zero-stale-half", 10, 20, 0, 0, 0);
        Expect("add-gain-buffs-max-after-second", 10, 21, 0, 5, 5);
        Expect("remove-gain-buffs-max-and-loses", 11, 22, 2, 0, 10, 12, 23);
        Require(Kinds("add-gain-heals-after-second").SequenceEqual(new[] { "OnDeathwish", "OnDeathwish", "OnHeal" }) &&
            Kinds("add-loss-damages-before-second").SequenceEqual(new[] { "OnDeathwishLost", "OnDeathwishLost", "OnHit", "OnDeathwish", "OnDeathwish" }) &&
            Kinds("remove-two-phase-loss-gain").SequenceEqual(new[] { "OnDeathwishLost", "OnDeathwishLost", "OnDeathwish", "OnDeathwish" }) &&
            Kinds("remove-three-phase-loss-gain-loss").SequenceEqual(new[] { "OnDeathwishLost", "OnDeathwishLost", "OnDeathwish", "OnDeathwish", "OnDeathwishLost", "OnDeathwishLost" }),
            "Native upgrade phase boundaries or nested callback ordering were not observed.");
        RoomCombatState guarded = samples[0].GetProperty("Before").Deserialize<RoomCombatState>()!;
        int actor = samples[0].GetProperty("ActorId").GetInt32();
        CardUpgradeModifier upgrade = samples[0].GetProperty("Upgrade").Deserialize<CardUpgradeModifier>()!;
        Require(RoomCombatModel.Validate(guarded)?.Contains("HalfHealth", StringComparison.Ordinal) == true &&
            !UnitModifierModel.Apply(guarded, actor, upgrade, "TemporaryUntilEndOfBattle", scaleUpgrade: true).Supported,
            "Whole-room or accumulated-scaling half-health guard was lost.");
        RoomCombatResult lethal = UnitModifierModel.ApplyDirect(guarded, actor, upgrade.WithScaledStats(0, -99999));
        Require(!lethal.Supported && lethal.UnsupportedReason?.Contains("sacrifice/revival", StringComparison.Ordinal) == true,
            "Uncalibrated lethal upgrade was accepted.");
        Require(!RoomCombatModel.ApplyDirectUnitUpgrade(guarded, actor, upgrade, false, "", null, deferCallbacks: true).Supported,
            "Uncalibrated external running queue was accepted.");
        var preview = new RoomCombatState(guarded.RoomIndex, guarded.Deployment, guarded.Units, guarded.ExternalInteractions,
            guarded.Context, true, guarded.ActiveCompanionBossIds);
        Require(!UnitModifierModel.ApplyDirect(preview, actor, upgrade).Supported, "Uncalibrated preview upgrade was accepted.");
        var triggerUpgrade = new CardUpgradeModifier("", "", upgrade.Stats, Array.Empty<CombatStatus>(), false, false, false, 0, 0,
            Array.Empty<string>(), triggerUpgrades: guarded.Units.Single(unit => unit.Id == actor).Triggers);
        Require(!UnitModifierModel.ApplyDirect(guarded, actor, triggerUpgrade).Supported, "Uncalibrated trigger installation was accepted.");
        Parallel.For(0, 32, _ => { foreach (FixtureValue sample in samples) Verify(sample); });
        Console.WriteLine("HALF-HEALTH-UPGRADE PASS: 27 native numeric upgrade API-return states/queues and drained successors, healed/unhealed/attributed phases, add/remove, nested callbacks, complete dispatches/RNG and 32 immutable branches. Trigger/ability/status changes, accumulated scaling, death and whole-room scheduling remain guarded; spawner writes have separate effect acceptance.");

        void Expect(string label, int hp, int max, int queued, int apiGold, int finalGold, int? finalHp = null, int? finalMax = null)
        {
            FixtureValue sample = byLabel[label]; int id = sample.GetProperty("ActorId").GetInt32();
            RoomCombatState before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState api = sample.GetProperty("ApiActual").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = sample.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            CombatUnit unit = api.Units.Single(item => item.Id == id), final = actual.Units.Single(item => item.Id == id);
            Require(unit.Health == hp && unit.MaxHealth == max && final.Health == (finalHp ?? hp) && final.MaxHealth == (finalMax ?? max) &&
                sample.GetProperty("ApiCallbacks").GetArrayLength() == queued && api.Context!.Gold - before.Context!.Gold == apiGold &&
                actual.Context!.Gold - before.Context.Gold == finalGold, "Missing native upgrade boundary/effect control: " + label);
        }
        string[] Kinds(string label) => byLabel[label].GetProperty("Dispatches").EnumerateArray().Select(item => item.GetProperty("Kind").GetString()!).ToArray();
    }

    private static void Verify(FixtureValue sample)
    {
        string label = sample.GetProperty("Label").GetString()!;
        RoomCombatState before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        CardUpgradeModifier upgrade = sample.GetProperty("Upgrade").Deserialize<CardUpgradeModifier>()!;
        FixtureValue index = sample.GetProperty("AnonymousRemovalIndex");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        RoomCombatResult api = RoomCombatModel.ApplyDirectUnitUpgrade(before, sample.GetProperty("ActorId").GetInt32(), upgrade,
            sample.GetProperty("Input").GetProperty("Remove").GetBoolean(), sample.GetProperty("UpgradeId").GetString()!,
            index.ValueKind == FixtureKind.Null ? null : index.GetInt32(), captureAllDispatches: true);
        Require(api.Supported, label + ": " + api.UnsupportedReason);
        Require(Difference(api.State, sample.GetProperty("ApiActual")) == null, label + " API state: " + Difference(api.State, sample.GetProperty("ApiActual")));
        Require(Difference(api.Dispatches, sample.GetProperty("ApiDispatches")) == null, label + ": API dispatch payload/order differs.");
        Require(Difference(api.PendingCallbacks.Select(EnchantmentCallback.From).ToArray(), sample.GetProperty("ApiCallbacks")) == null,
            label + ": API queued payload/order differs.");
        string intermediate = JsonSerializer.Serialize(api.State, ModelJson.Options);
        RoomCombatResult drained = RoomCombatModel.DrainCharacterCallbacks(api.State!, api.PendingCallbacks, true);
        Require(drained.Supported, label + ": " + drained.UnsupportedReason);
        Require(Difference(drained.State, sample.GetProperty("Actual")) == null, label + " drained state: " + Difference(drained.State, sample.GetProperty("Actual")));
        Require(Difference(api.Dispatches.Concat(drained.Dispatches).ToArray(), sample.GetProperty("Dispatches")) == null, label + ": drained dispatch payload/order differs.");
        foreach (string phase in new[] { "RngAfterApi", "RngAfter" })
            Require(ModelJson.Difference(HalfHealthLifecycleChecks.GameplayRng(sample.GetProperty("RngBefore")),
                HalfHealthLifecycleChecks.GameplayRng(sample.GetProperty(phase))) == null, label + ": gameplay or Unity RNG changed at " + phase);
        Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent && JsonSerializer.Serialize(api.State, ModelJson.Options) == intermediate,
            label + ": upgrade or queue drain mutated a parent state.");
    }
    private static string? Difference(object? predicted, FixtureValue actual) => ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual.ToObjectGraph()));
    private static string? Difference(RoomCombatState? predicted, FixtureValue actual) => ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual.Deserialize<RoomCombatState>(), ModelJson.Options));
    private static string? Difference(FixtureValue left, FixtureValue right) => ModelJson.Difference(JsonSerializer.Serialize(left.ToObjectGraph()), JsonSerializer.Serialize(right.ToObjectGraph()));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
