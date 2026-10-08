using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EnchantmentWorldChecks
{
    internal static void Native(string path)
    {
        using var document = FixtureDocument.Read(path);
        var root = document.RootElement;
        var samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        foreach (var sample in samples) Verify(sample);
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameModuleMvid").GetString() ==
            "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" && root.GetProperty("Boundary").GetString() == "AutomaticControlStatusAndQueue" &&
            !root.GetProperty("StatusMutationsSuppressed").GetBoolean() && root.GetProperty("LiveContextUnchanged").GetBoolean() &&
            root.GetProperty("ExternalPreviewPreparationsRecorded").GetBoolean() && root.GetProperty("Mismatches").GetInt32() == 0 &&
            samples.Length == 32, "Automatic aura status version/mutation/restoration/coverage gate failed.");
        foreach (var sample in samples)
            Require(sample.GetProperty("Completed").GetBoolean() && sample.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "Incomplete or mismatched automatic aura status sample.");
        Require(samples.Count(sample => sample.GetProperty("Label").GetString()!.StartsWith("control-random-carried-", StringComparison.Ordinal)) == 16 &&
            samples.Any(sample => sample.GetProperty("Status").GetProperty("Id").GetString() == "spark") &&
            samples.Any(sample => !sample.GetProperty("Before").GetProperty("AllowUpdates").GetBoolean()),
            "Missing Spark, random or disabled manager cases.");
        var chain = samples.Where(sample => sample.GetProperty("Label").GetString()!.StartsWith("control-random-carried-", StringComparison.Ordinal)).ToArray();
        var state = chain[0].GetProperty("Before").Deserialize<EnchantmentCombatState>()!;
        foreach (var sample in chain)
        {
            Compare(state, sample.GetProperty("Before").Deserialize<EnchantmentCombatState>()!, "carried control input");
            state = Verify(sample, state);
        }
        Parallel.For(0, 32, _ => { foreach (var sample in samples) Verify(sample); });
        VerifyLocations(samples[0].GetProperty("Before").Deserialize<EnchantmentCombatState>()!);
        int calls = samples.Sum(sample => sample.GetProperty("ObservedAuraRequests").GetArrayLength());
        int callbacks = samples.Sum(sample => sample.GetProperty("Callbacks").GetArrayLength());
        Console.WriteLine($"NATIVE-ENCHANTMENT-WORLD-CHECKS PASS: {samples.Length} real control-status transitions, {calls} observed automatic aura calls, " +
            $"{callbacks} exact callback payloads, shared world/queue/context, 16 carried random steps and 32 isolated branches.");
    }

    private static EnchantmentCombatState Verify(FixtureValue sample, EnchantmentCombatState? input = null)
    {
        string label = sample.GetProperty("Label").GetString()!;
        var before = input ?? sample.GetProperty("Before").Deserialize<EnchantmentCombatState>()!;
        string parent = JsonSerializer.Serialize(before);
        var result = EnchantmentWorldModel.ChangeStatus(before, sample.GetProperty("UnitId").GetInt32(),
            sample.GetProperty("Status").Deserialize<CombatStatus>()!, sample.GetProperty("Remove").GetBoolean());
        Require(result.Supported, label + ": " + result.UnsupportedReason);
        Compare(result.State!, sample.GetProperty("Actual").Deserialize<EnchantmentCombatState>()!, label + " automatic state");
        Compare(result.Callbacks, sample.GetProperty("Callbacks").Deserialize<EnchantmentCallback[]>()!, label + " automatic queue");
        var drained = EnchantmentWorldModel.Drain(result);
        Require(drained.Supported, label + " drain: " + drained.UnsupportedReason);
        EnchantmentCombatState after = drained.State!;
        foreach (var id in sample.GetProperty("PreviewPreparedUnitIds").EnumerateArray())
            after = EnchantmentCombatModel.PrepareForPreview(after, id.GetInt32());
        Compare(after, sample.GetProperty("AfterQueue").Deserialize<EnchantmentCombatState>()!, label + " after queue/preparation");
        Require(JsonSerializer.Serialize(before) == parent, label + ": parent changed.");
        return after;
    }

    private static void VerifyLocations(EnchantmentCombatState before)
    {
        TrainCombatState train = EnchantmentWorldModel.Attach(before);
        RoomCombatState unaffiliated = train.Rooms.First(item => item.Units.All(unit => unit.Triggers.All(trigger =>
            trigger.Effects.All(effect => effect.Type != "CardEffectEnchant"))));
        var premature = RoomCombatModel.Resolve(unaffiliated);
        Require(!premature.Supported && premature.UnsupportedReason!.Contains("CardEffectEnchant", StringComparison.Ordinal),
            "A room without an aura source bypassed the unfinished shared train lifecycle gate.");
        RoomCombatState room = train.Rooms.First(item => item.Units.Any(unit => !unit.IsPyre));
        CombatUnit moved = room.Units.First(unit => !unit.IsPyre);
        RoomCombatState destination = train.Rooms.First(item => item.RoomIndex != room.RoomIndex);
        var incoming = new RoomCombatState(destination.RoomIndex, destination.Deployment, destination.Units.Concat(new[] { moved }).ToArray(),
            destination.ExternalInteractions, train.Context, destination.Preview);
        incoming = EnchantmentWorldModel.Sync(incoming);
        var world = incoming.Context!.Enchantments!;
        Require(world.Rooms.SelectMany(item => item.Units).Count(unit => unit.Id == moved.Id) == 1 &&
            world.RetainedUnits.All(actor => actor.Unit.Id != moved.Id), "Moved actor duplicated in live/retained world.");
        var outgoing = new RoomCombatState(incoming.RoomIndex, incoming.Deployment, incoming.Units.Where(unit => unit.Id != moved.Id).ToArray(),
            incoming.ExternalInteractions, incoming.Context, incoming.Preview);
        CombatUnit corpse = CardSpellModel.Copy(moved, 0, moved.Statuses);
        outgoing = EnchantmentWorldModel.Sync(outgoing, new[] { corpse });
        Require(outgoing.Context!.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == moved.Id).Unit.Health == 0,
            "Retained world kept stale pre-death actor health.");
        // Serialization must terminate; context-owned world room snapshots have no back-reference.
        var roundtrip = JsonSerializer.Deserialize<TrainCombatState>(JsonSerializer.Serialize(train), ModelJson.Options)!;
        Compare(EnchantmentWorldModel.Snapshot(roundtrip), before, "world serialization");
    }

    private static void Compare<T>(T actual, T expected, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(actual), JsonSerializer.Serialize(expected));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidDataException(message); }
}
