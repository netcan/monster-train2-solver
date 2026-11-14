using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ZealStatusChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        FixtureValue[] samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == "NativeZealStatusLifecycle" &&
            root.GetProperty("LiveContextUnchanged").GetBoolean() && root.GetProperty("EffectsDrained").GetBoolean() &&
            root.GetProperty("RestorationDifference").ValueKind == FixtureKind.Null && root.GetProperty("Mismatches").GetInt32() == 0 &&
            samples.Length == 19 && samples.Select(sample => sample.GetProperty("Label").GetString()).Distinct().Count() == 19,
            "Incomplete current-version native Zeal component.");
        FixtureValue owner = root.GetProperty("OwnerDefinition");
        Require(owner.GetProperty("Id").GetString() == "ee429978-fa41-4c66-93c6-3d3c7d5a4733" &&
            owner.GetProperty("Key").GetString() == "ZealDamageIncrease" && !owner.GetProperty("Placement").GetBoolean() &&
            owner.GetProperty("Type").GetString() == RelicModel.ModifyStatusMagnitude && owner.GetProperty("Int").GetInt32() == 1 &&
            owner.GetProperty("Team").GetString() == "Monsters" && owner.GetProperty("Conditions").GetInt32() == 0 &&
            owner.GetProperty("Statuses").GetArrayLength() == 1 &&
            owner.GetProperty("Statuses")[0].GetProperty("statusId").GetString() == "zeal" &&
            owner.GetProperty("Statuses")[0].GetProperty("count").GetInt32() == 0,
            "Changed original Zeal attack-magnitude relic.");
        foreach (FixtureValue sample in samples) Verify(sample);
        var byLabel = samples.ToDictionary(sample => sample.GetProperty("Label").GetString()!);
        Expect("add-enters-half", 11, 22, 4, 5, "OnDeathwish", "OnDeathwish", "OnStatusEffectChanged", "OnNewStatusEffectAdded");
        Expect("enemy-add-enters-half", 11, 22, 2, 5, "OnDeathwish", "OnDeathwish", "OnStatusEffectChanged", "OnNewStatusEffectAdded");
        Expect("add-remains-half", 9, 22, 4, 0, "OnStatusEffectChanged", "OnNewStatusEffectAdded");
        Expect("add-above-half", 20, 22, 4, 0, "OnStatusEffectChanged", "OnNewStatusEffectAdded");
        Expect("add-zero", 11, 20, 0, 0, "OnStatusEffectChanged", "OnNewStatusEffectAdded");
        Expect("add-negative-retains-stats", 11, 23, 0, 0, "OnStatusEffectChanged");
        Expect("add-saturates-stacks", 10018, 10019, 2, 0, "OnStatusEffectChanged");
        Expect("add-saturates-health", 99998, 99999, 6, 0, "OnStatusEffectChanged", "OnNewStatusEffectAdded");
        Expect("immune-add", 11, 20, 0, 0);
        Expect("purify-blocks-add", 11, 20, 0, 0);
        Expect("remove-leaves-half", 11, 21, -4, 5, "OnDeathwishLost", "OnDeathwishLost", "OnStatusEffectChanged");
        Expect("enemy-remove-leaves-half", 11, 21, -2, 5, "OnDeathwishLost", "OnDeathwishLost", "OnStatusEffectChanged");
        Expect("remove-remains-half", 9, 21, -4, 0, "OnStatusEffectChanged");
        Expect("remove-clips-current", 21, 21, -4, 0, "OnStatusEffectChanged");
        Expect("remove-all", 11, 20, -6, 5, "OnDeathwishLost", "OnDeathwishLost", "OnStatusEffectChanged");
        Expect("remove-health-floor", 1, 1, -6, 0, "OnStatusEffectChanged");
        Expect("remove-zero", 11, 23, 0, 0);
        Expect("remove-missing", 11, 20, 0, 0);
        Expect("purify-clears-zeal", 11, 20, -6, 5, "OnDeathwishLost", "OnDeathwishLost");
        Parallel.For(0, 32, _ => { foreach (FixtureValue sample in samples) Verify(sample); });
        Console.WriteLine("ZEAL-STATUS PASS: 19 native API and drained successors, original team-specific attack relic, unhealed/clipped maximum HP, half-health callback order, negative/zero/saturation controls, immunity/Purify and 32 immutable parallel branches. Devotion, birth and whole-battle relic acceptance remain separate.");

        void Expect(string label, int hp, int maximum, int attackDelta, int gold, params string[] callbacks)
        {
            FixtureValue sample = byLabel[label]; int id = sample.GetProperty("ActorId").GetInt32();
            RoomCombatState before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = sample.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            CombatUnit previous = before.Units.Single(unit => unit.Id == id), actor = actual.Units.Single(unit => unit.Id == id);
            Require(actor.Health == hp && actor.MaxHealth == maximum &&
                actor.Modifiers!.DamageBuffFromStatusEffects - previous.Modifiers!.DamageBuffFromStatusEffects == attackDelta &&
                actual.Context!.Gold - before.Context!.Gold == gold && sample.GetProperty("Dispatches").EnumerateArray()
                    .Select(item => item.GetProperty("Kind").GetString()).SequenceEqual(callbacks),
                "Missing independent Zeal effect/order control: " + label);
        }
    }
    internal static void Verify(FixtureValue sample, bool maximumHealth = false)
    {
        string label = sample.GetProperty("Label").GetString()!;
        Require(sample.GetProperty("Completed").GetBoolean() && sample.GetProperty("Difference").ValueKind == FixtureKind.Null &&
            sample.GetProperty("GameplayRngUnchanged").GetBoolean() && sample.GetProperty("PendingAfterDrain").GetInt32() == 0,
            "Incomplete native successor: " + label);
        RoomCombatState before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        int id = sample.GetProperty("ActorId").GetInt32(); FixtureValue input = sample.GetProperty("Input");
        CombatStatus status = sample.GetProperty("Status").Deserialize<CombatStatus>()!;
        CombatRelicState owner = before.Context!.Relics!.Single(relic => relic.DataId == "ee429978-fa41-4c66-93c6-3d3c7d5a4733");
        Require(owner.AssetKey == "ZealDamageIncrease" && owner.EffectTypes.SequenceEqual([RelicModel.ModifyStatusMagnitude]) &&
            owner.StatusMagnitudes is { Count: 1 } && owner.StatusMagnitudes[0].StatusId == "zeal" &&
            owner.StatusMagnitudes[0].SourceTeam == 2 && owner.StatusMagnitudes[0].Modifier == 1 && owner.StatusMagnitudes[0].Conditions.Count == 0 &&
            status.ParamInt == 1, "Missing original Zeal status/relic payload: " + label);
        string parent = Serialize(before), operation = input.GetProperty("Operation").GetString()!;
        int amount = input.GetProperty("Amount").GetInt32();
        RoomCombatResult predicted = maximumHealth ? UnitHealthModel.ApplyNative(before, id, amount,
            debuff: operation == "DebuffMaximum", heal: input.GetProperty("Heal").GetBoolean(),
            triggerOnHeal: input.GetProperty("TriggerOnHeal").GetBoolean(), floor: input.GetProperty("Floor").GetInt32(),
            decreaseHp: input.GetProperty("DecreaseHp").GetBoolean(), captureAllDispatches: true) :
            operation == "Remove" ? StatusRemovalModel.Remove(before, id, "zeal", amount) :
            StatusApplicationModel.Apply(before, id, operation == "Purify" ?
                before.Context.StatusRules.Single(rule => rule.Id == "purify").WithStacks(1) : status.WithStacks(amount));
        Require(predicted.Supported, label + ": " + predicted.UnsupportedReason);
        Equal(predicted.State, sample.GetProperty("ApiActual").Deserialize<RoomCombatState>(), label + " native API state");
        Equal(predicted.Dispatches, sample.GetProperty("ApiDispatches"), label + " immediate dispatches");
        Equal(predicted.PendingCallbacks.Select(EnchantmentCallback.From).ToArray(), sample.GetProperty("ApiCallbacks"), label + " pending payload/order");
        if (sample.TryGetProperty("ApiTriggerCounts", out var counts))
            Equal(predicted.PendingCallbacks.Select(item => item.TriggerCount).ToArray(), counts, label + " pending trigger multiplicities");
        RoomCombatResult drained = RoomCombatModel.DrainCharacterCallbacks(predicted.State!, predicted.PendingCallbacks, true);
        Require(drained.Supported, label + ": " + drained.UnsupportedReason);
        Equal(drained.State, sample.GetProperty("Actual").Deserialize<RoomCombatState>(), label + " drained state");
        Equal(predicted.Dispatches.Concat(drained.Dispatches).ToArray(), sample.GetProperty("Dispatches"), label + " complete dispatches");
        Require(HalfHealthLifecycleChecks.GameplayRng(sample.GetProperty("RngBefore")) ==
                HalfHealthLifecycleChecks.GameplayRng(sample.GetProperty("RngAfterApi")) &&
            HalfHealthLifecycleChecks.GameplayRng(sample.GetProperty("RngBefore")) ==
                HalfHealthLifecycleChecks.GameplayRng(sample.GetProperty("RngAfter")), "Native gameplay RNG changed: " + label);
        Require(Serialize(before) == parent, "Lifecycle branch mutated its parent: " + label);
    }
    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal(object? predicted, FixtureValue actual, string label) => Equal(predicted, actual.ToObjectGraph(), label);
    private static void Equal(object? predicted, object? actual, string label)
    {
        string? difference = ModelJson.Difference(Serialize(predicted), Serialize(actual));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
