using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HalfHealthTriggerInstallationChecks
{
    internal static void Native(string path)
    {
        HalfHealthDeathwishChecks.Native(path);
        using var document = ModelJson.ReadFixture(path);
        FixtureValue[] samples = document.RootElement.GetProperty("Installations").EnumerateArray().ToArray();
        Require(samples.Length == 16 && samples.All(sample => sample.GetProperty("Completed").GetBoolean() &&
            sample.GetProperty("Difference").ValueKind == FixtureKind.Null), "Incomplete native trigger installation matrix.");
        var cases = samples.ToDictionary(sample => sample.GetProperty("Label").GetString()!);
        int[] counts = { 1, 2, 2, 1, 2, 3, 3, 3, 1, 1, 3, 3, 3, 1, 1, 5 };
        for (int index = 0; index < samples.Length; index++)
        {
            FixtureValue sample = samples[index];
            RelicCharacterTriggers rule = sample.GetProperty("Rule").Deserialize<RelicCharacterTriggers>()!;
            EnchantmentCallback[] callbacks = sample.GetProperty("Callbacks").Deserialize<EnchantmentCallback[]>()!;
            Require(callbacks.Length == counts[index], "Missing native admission control: " + sample.GetProperty("Label").GetString());
            if (rule.Triggers.All(HalfHealthDeathwishModel.IsTrigger))
                Require(callbacks.All(callback => callback.Kind == "OnDeathwish"), "Half-health installation refreshed Vanguard.");
            if (rule.Triggers.All(trigger => trigger.Kind.StartsWith("OnVanguard", StringComparison.Ordinal)))
                Require(callbacks.All(callback => callback.Kind == "OnVanguard"), "Vanguard installation refreshed half-health.");
            Verify(sample);
        }
        foreach (string label in new[] { "muted", "silenced" })
        {
            int actor = cases[label].GetProperty("ActorId").GetInt32();
            Require(cases[label].GetProperty("Callbacks").Deserialize<EnchantmentCallback[]>()!.All(callback => callback.UnitId != actor),
                "Muted/silenced actor admitted a half-health gain during installation.");
        }
        Parallel.For(0, 32, _ => { foreach (FixtureValue sample in samples) Verify(sample); });
        Console.WriteLine("HALF-HEALTH-TRIGGER-INSTALLATION PASS: 16 native installations, 35 exclusive queue payloads; separate Vanguard/half-health updates, stale bystander sources, prepend/duplicate order, odd thresholds, silence/mute, untouchable and disabled aura gate; complete state/RNG and 32 immutable branches. Effects remain queued at this boundary.");
    }

    private static void Verify(FixtureValue sample)
    {
        string label = sample.GetProperty("Label").GetString()!;
        TrainCombatState before = sample.GetProperty("Before").Deserialize<TrainCombatState>()!;
        TrainCombatState actual = sample.GetProperty("Actual").Deserialize<TrainCombatState>()!;
        int actor = sample.GetProperty("ActorId").GetInt32(), room = sample.GetProperty("RoomIndex").GetInt32();
        RelicCharacterTriggers rule = sample.GetProperty("Rule").Deserialize<RelicCharacterTriggers>()!;
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        RoomCombatResult result = RelicCharacterTriggerModel.Apply(before.Rooms.Single(item => item.RoomIndex == room), actor, rule);
        Require(result.Supported, label + ": " + result.UnsupportedReason);
        var predicted = new TrainCombatState(before.Rooms.Select(item => item.RoomIndex == room ? result.State! : item).ToArray(),
            before.Movement, before.EnemySlotsPerRoom, result.State!.Context);
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
        Require(JsonSerializer.Serialize(result.PendingCallbacks.Select(EnchantmentCallback.From), ModelJson.Options) ==
            JsonSerializer.Serialize(sample.GetProperty("Callbacks").Deserialize<EnchantmentCallback[]>(), ModelJson.Options), label + ": queue differs.");
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), label + ": parent changed.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
