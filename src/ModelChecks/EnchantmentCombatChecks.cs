using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EnchantmentCombatChecks
{
    internal static void Native(string path)
    {
        using var document = FixtureDocument.Read(path);
        var root = document.RootElement;
        var samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        // Compare independently before acceptance gates so a rejected native experiment gives
        // an exact field difference, rather than merely repeating its live comparison label.
        foreach (var sample in samples) Verify(sample);
        VerifyBoundaries(samples[0].GetProperty("Before").Deserialize<EnchantmentCombatState>()!);
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameModuleMvid").GetString() ==
            "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" && root.GetProperty("Boundary").GetString() == "RealStatusAndDrainedQueue" &&
            !root.GetProperty("StatusMutationsSuppressed").GetBoolean() && root.GetProperty("LiveContextUnchanged").GetBoolean() &&
            root.GetProperty("ExternalPreviewPreparationsRecorded").GetBoolean() &&
            root.GetProperty("Mismatches").GetInt32() == 0 && samples.Length == 32,
            "Real enchantment combat version, mutation, restoration or matrix gate failed.");
        foreach (var sample in samples)
            Require(sample.GetProperty("Completed").GetBoolean() && sample.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "Incomplete/mismatched real enchantment combat sample.");
        Require(samples.Count(sample => sample.GetProperty("Label").GetString()!.StartsWith("random-carried-", StringComparison.Ordinal)) == 16 &&
            samples.Any(sample => sample.GetProperty("Direct").GetBoolean()) &&
            samples.Any(sample => !sample.GetProperty("Before").GetProperty("AllowUpdates").GetBoolean()),
            "Native random, direct reentry or disabled-manager cases are missing.");
        Parallel.For(0, 32, _ => { foreach (var sample in samples) Verify(sample); });
        int requests = samples.Sum(sample => sample.GetProperty("Requests").GetArrayLength());
        int callbacks = samples.Sum(sample => sample.GetProperty("Callbacks").GetArrayLength());
        Console.WriteLine($"NATIVE-ENCHANTMENT-COMBAT-CHECKS PASS: {samples.Length} real status/train/effect transitions, {requests} status calls, " +
            $"{callbacks} complete callback payloads, drained child gold effects, guarded/direct reentry, retained per-effect maps/RNG and 32 isolated branches.");
    }
    private static void Verify(FixtureValue sample)
    {
        string label = sample.GetProperty("Label").GetString()!;
        var before = sample.GetProperty("Before").Deserialize<EnchantmentCombatState>()!;
        string parent = JsonSerializer.Serialize(before);
        var result = sample.GetProperty("Direct").GetBoolean()
            ? EnchantmentCombatModel.UpdateEffect(before, sample.GetProperty("SourceId").GetInt32(),
                sample.GetProperty("TriggerIndex").GetInt32(), sample.GetProperty("EffectIndex").GetInt32())
            : EnchantmentCombatModel.UpdateAll(before);
        Require(result.Supported, label + ": " + result.UnsupportedReason);
        Compare(result.State!, sample.GetProperty("Actual").Deserialize<EnchantmentCombatState>()!, label + " state");
        Compare(result.Requests, sample.GetProperty("Requests").Deserialize<EnchantmentRequest[]>()!, label + " requests");
        Compare(result.Callbacks, sample.GetProperty("Callbacks").Deserialize<EnchantmentCallback[]>()!, label + " callbacks");
        var drained = EnchantmentCombatModel.Drain(result);
        Require(drained.Supported, label + " drain: " + drained.UnsupportedReason);
        EnchantmentCombatState afterQueue = drained.State!;
        foreach (var id in sample.GetProperty("PreviewPreparedUnitIds").EnumerateArray())
            afterQueue = EnchantmentCombatModel.PrepareForPreview(afterQueue, id.GetInt32());
        Compare(afterQueue, sample.GetProperty("AfterQueue").Deserialize<EnchantmentCombatState>()!, label + " after queue/preparation events");
        Require(JsonSerializer.Serialize(before) == parent, label + ": parent state changed.");
        foreach (var effect in before.Train.Rooms.SelectMany(room => room.Units).SelectMany(unit => unit.Triggers).SelectMany(trigger => trigger.Effects)
            .Where(effect => effect.Enchantment != null))
        {
            Require(ReferenceEquals(effect.Enchantment, effect.WithCounter(9).Enchantment) &&
                ReferenceEquals(effect.Enchantment, effect.WithActionValue(11).Enchantment), "Effect copying lost its persistent enchantment state.");
        }
    }
    private static void Compare<T>(T actual, T expected, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(actual), JsonSerializer.Serialize(expected));
        Require(difference == null, label + ": " + difference);
    }
    private static void VerifyBoundaries(EnchantmentCombatState source)
    {
        CombatUnit owner = source.Train.Rooms.SelectMany(room => room.Units).First(unit => unit.Triggers.Any(trigger =>
            trigger.Effects.Any(effect => effect.Enchantment != null)));
        EnchantmentCombatState Replace(CombatUnit unit)
        {
            var train = new TrainCombatState(source.Train.Rooms.Select(room => new RoomCombatState(room.RoomIndex,
                room.Deployment, room.Units.Select(old => old.Id == unit.Id ? unit : old).ToArray(), room.ExternalInteractions,
                room.Context, room.Preview)).ToArray(), source.Train.Movement, source.Train.EnemySlotsPerRoom, source.Train.Context);
            return new EnchantmentCombatState(train, source.RetainedUnits, source.EnchanterIds, source.AllowUpdates,
                source.Updating, source.Preview, source.TestRng);
        }
        var deathwish = owner.WithTriggers(owner.Triggers.Concat(new[] { new CombatTrigger("OnDeathwish", false, false, false, 1,
            new[] { new CombatEffect("CardEffectRewardGold", 1, 0, "", 0, [], false) }) }).ToArray());
        var result = EnchantmentCombatModel.UpdateAll(Replace(deathwish));
        Require(!result.Supported && result.UnsupportedReason!.Contains("Deathwish", StringComparison.Ordinal),
            "Deathwish trigger updates silently became supported no-ops.");
        var harmfulCallback = owner.WithTriggers(owner.Triggers.Select(trigger => trigger.Kind != "OnStatusEffectChanged" ? trigger :
            trigger.WithEffects(new[] { new CombatEffect("CardEffectDamage", 1, 0, "", 0, [], false) })).ToArray());
        result = EnchantmentCombatModel.Drain(EnchantmentCombatModel.UpdateAll(Replace(harmfulCallback)));
        Require(!result.Supported && result.UnsupportedReason!.Contains("automatic combat lifecycle", StringComparison.Ordinal),
            "A mutating child callback silently bypassed automatic aura updates.");
        var roomResult = RoomCombatModel.Resolve(source.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == owner.Id)));
        Require(!roomResult.Supported && roomResult.UnsupportedReason!.Contains("CardEffectEnchant", StringComparison.Ordinal),
            "The standalone aura kernel silently enabled incomplete generic combat support.");
        var hordeOwner = owner.WithTriggers(owner.Triggers.Select(trigger => trigger.WithEffects(trigger.Effects.Select(effect =>
            effect.Enchantment == null ? effect : effect.WithEnchantment(new EnchantmentRule(effect.Enchantment.Targeting,
                new[] { new CombatStatus("horde", 1) }, effect.Enchantment.State, true))).ToArray())).ToArray());
        result = EnchantmentCombatModel.UpdateAll(Replace(hordeOwner));
        Require(!result.Supported && result.UnsupportedReason!.Contains("Horde aura", StringComparison.Ordinal),
            "Horde aura casualties silently bypassed retained-actor/birth hooks.");
        int origin = source.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == owner.Id)).RoomIndex;
        int destination = source.Train.Rooms.First(room => room.RoomIndex != origin).RoomIndex;
        var moved = new TrainCombatState(source.Train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
            room.RoomIndex == origin ? room.Units.Where(unit => unit.Id != owner.Id).ToArray() :
            room.RoomIndex == destination ? room.Units.Append(owner).ToArray() : room.Units,
            room.ExternalInteractions, room.Context, room.Preview)).ToArray(), source.Train.Movement, source.Train.EnemySlotsPerRoom, source.Train.Context);
        result = EnchantmentCombatModel.UpdateAll(new EnchantmentCombatState(moved, source.RetainedUnits, source.EnchanterIds,
            source.AllowUpdates, source.Updating, source.Preview, source.TestRng));
        Require(!result.Supported && result.UnsupportedReason!.Contains("physical points", StringComparison.Ordinal),
            "Cross-room aura sources silently used room traversal instead of captured physical ordering.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidDataException(message); }
}
