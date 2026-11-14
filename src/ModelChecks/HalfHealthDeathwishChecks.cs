using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HalfHealthDeathwishChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        FixtureValue[] samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        foreach (FixtureValue sample in samples) Verify(sample);
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == "NativeHalfHealthActivationAndQueueAdmission" &&
            root.GetProperty("RetainedActorsIncluded").GetBoolean() &&
            root.GetProperty("RngUnchanged").GetBoolean() && root.GetProperty("FrameUnchanged").GetBoolean() &&
            root.GetProperty("LiveContextUnchanged").GetBoolean() && !root.GetProperty("EffectsDrained").GetBoolean() &&
            root.GetProperty("Mismatches").GetInt32() == 0 && samples.Length == 27 &&
            samples.All(sample => sample.GetProperty("Completed").GetBoolean() && sample.GetProperty("Difference").ValueKind == FixtureKind.Null),
            "Incomplete or mismatched half-health native activation matrix.");
        Require(ModelJson.Difference(JsonSerializer.Serialize(root.GetProperty("RngBefore").ToObjectGraph()),
            JsonSerializer.Serialize(root.GetProperty("RngAfter").ToObjectGraph())) == null, "Half-health component changed RNG state.");
        var byLabel = samples.ToDictionary(sample => sample.GetProperty("Label").GetString()!);
        Require(Callbacks("exact-half").Length == 2 && Callbacks("exact-half").All(item => item.Kind == "OnDeathwish") &&
            Callbacks("repeat-half").Length == 0 && Callbacks("below-half").Length == 0 &&
            Callbacks("leave-half").All(item => item.Kind == "OnDeathwishLost") &&
            Callbacks("zero-maximum").All(item => item.Kind == "OnDeathwishLost") &&
            !HalfHealthDeathwishModel.Eligible(Actor("negative-maximum")) &&
            HalfHealthDeathwishModel.Eligible(Actor("maximum-hp-below-half")) &&
            !HalfHealthDeathwishModel.Eligible(Actor("maximum-hp-above-half")) &&
            HalfHealthDeathwishModel.Eligible(Actor("negative-hp")), "Missing exact threshold, zero/negative or maximum-health boundary controls.");
        EnchantmentCallback[] ordered = Callbacks("manager-vanguard-before-deathwish");
        Require(ordered.Length == 4 && ordered.Take(2).All(item => item.Kind == "OnVanguard") &&
            ordered.Skip(2).All(item => item.Kind == "OnDeathwish") && ordered[0].UnitId != ordered[2].UnitId,
            "Native global Vanguard/half-health pass order was not observed.");
        Require(Callbacks("manager-disabled").Length == 0 && Callbacks("manager-while-updating").Length == 0 &&
            Callbacks("direct-while-disabled").Length == 2 && Callbacks("direct-while-updating").Length == 2 &&
            Callbacks("muted-half").All(item => item.Kind == "OnDeathwishLost") &&
            Callbacks("silenced-half").All(item => item.Kind == "OnDeathwishLost") &&
            Callbacks("untouchable-half").Length == 0, "Missing mute/silence, front-independence or direct/manager guard controls.");
        Parallel.For(0, 32, _ => { foreach (FixtureValue sample in samples) Verify(sample); });
        Console.WriteLine("HALF-HEALTH-DEATHWISH PASS: 27 native source-membership/queue transitions, exclusive identities, Single thresholds, silence/mute, local/global guards, Vanguard pass order and 32 immutable branches. Effect lifecycle remains separately guarded.");

        EnchantmentCallback[] Callbacks(string label) => byLabel[label].GetProperty("Callbacks").Deserialize<EnchantmentCallback[]>()!;
        CombatUnit Actor(string label)
        {
            FixtureValue sample = byLabel[label];
            return sample.GetProperty("Actual").Deserialize<TrainCombatState>()!.Rooms.SelectMany(room => room.Units)
                .Single(unit => unit.Id == sample.GetProperty("ActorId").GetInt32());
        }
    }

    private static void Verify(FixtureValue sample)
    {
        TrainCombatState before = sample.GetProperty("Before").Deserialize<TrainCombatState>()!;
        TrainCombatState actual = sample.GetProperty("Actual").Deserialize<TrainCombatState>()!;
        int actor = sample.GetProperty("ActorId").GetInt32(), room = sample.GetProperty("RoomIndex").GetInt32();
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        TrainCombatState? predicted;
        EnchantmentCallback[] callbacks;
        if (sample.GetProperty("Manager").GetBoolean())
        {
            var frame = new EnchantmentCombatState(before, [], [], sample.GetProperty("AllowUpdates").GetBoolean(),
                sample.GetProperty("Updating").GetBoolean(), false, default);
            EnchantmentCombatResult result = EnchantmentCombatModel.UpdateAll(frame);
            Require(result.Supported, result.UnsupportedReason ?? "Manager half-health unsupported.");
            predicted = result.State!.Train; callbacks = result.Callbacks.ToArray();
        }
        else
        {
            RoomCombatResult result = HalfHealthDeathwishModel.Update(before.Rooms.Single(item => item.RoomIndex == room), actor, before.Context);
            Require(result.Supported, result.UnsupportedReason ?? "Local half-health unsupported.");
            predicted = new TrainCombatState(before.Rooms.Select(item => item.RoomIndex == room ? result.State! : item).ToArray(),
                before.Movement, before.EnemySlotsPerRoom, before.Context);
            callbacks = result.PendingCallbacks.Select(EnchantmentCallback.From).ToArray();
            RoomCombatResult repeated = HalfHealthDeathwishModel.Update(result.State!, actor);
            Require(repeated.Supported && repeated.PendingCallbacks.Count == 0, "Repeated local activation was not idempotent.");
        }
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, sample.GetProperty("Label").GetString() + ": " + difference);
        Require(ModelJson.Difference(JsonSerializer.Serialize(callbacks, ModelJson.Options),
            JsonSerializer.Serialize(sample.GetProperty("Callbacks").Deserialize<EnchantmentCallback[]>(), ModelJson.Options)) == null,
            "Half-health complete native callback payload/order differs: " + sample.GetProperty("Label").GetString());
        Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "A half-health branch mutated its parent.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
