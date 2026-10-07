using System.Text.Json;
using MonsterTrain2Poju.Model;
using MonsterTrain2Poju.Fixtures;

internal static class ConditionalTriggerChecks
{
    internal static void Run()
    {
        var self = new[] { "sWeEp" }; var dying = new[] { "PIERCING" };
        var conditions = new CombatTriggerConditions(self, dying); self[0] = "poison"; dying[0] = "armor";
        Require(conditions.RequiredStatuses[0] == "sWeEp" && conditions.RequiredDyingStatuses[0] == "PIERCING", "Condition arrays retained mutable caller storage.");
        var rng = UnityRng.Seed(97);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10);
        var trigger = new CombatTrigger("OnKill", false, false, false, 1,
            [new("CardEffectRewardGold", 7, 0, "", 0, [], false)], false, conditions: conditions);
        var actor = new CombatUnit(1, "actor", CombatTeam.Player, 4, 20, 20, true, false, false, [new("sweep", 1)], [trigger]);
        var victim = new CombatUnit(2, "victim", CombatTeam.Enemy, 0, 2, 2, false, false, false, [new("piercing", 1)]);
        var other = new CombatUnit(3, "other", CombatTeam.Enemy, 0, 2, 2, false, false, false, []);
        var room = new RoomCombatState(0, false, [victim, other, actor], [], context);
        string parent = JsonSerializer.Serialize(room, ModelJson.Options);
        var result = RoomCombatModel.Resolve(room);
        Require(result.Supported && result.State!.Context!.Gold == 5 && result.Outcome == RoomOutcome.Cleared,
            "Sweep callbacks lost their distinct dying characters or ignored case-insensitive status presence.");
        var retained = new CombatUnit(4, "removed", CombatTeam.Enemy, 0, 0, 2, false, false, false, [new("piercing", 1)]);
        var living = new RoomCombatState(0, false, [actor], [], context);
        var queued = new RoomCombatModel.QueuedCharacterTrigger(0, actor, "OnKill", dyingCharacter: retained);
        var fired = RoomCombatModel.ApplyQueuedCharacterTrigger(living, queued, _ => { });
        Require(fired.Supported && fired.State!.Context!.Gold == 5 && fired.State.Units.Count == 1 && queued.DyingCharacter!.Id == 4,
            "An external callback lost its removed dying target or inserted it into room membership.");
        var zero = new CombatUnit(4, "removed", CombatTeam.Enemy, 0, 0, 2, false, false, false, [], statusRegistry: [new("piercing", 0)]);
        var skipped = RoomCombatModel.ApplyQueuedCharacterTrigger(living,
            new(0, actor, "OnKill", dyingCharacter: zero), _ => { });
        var absent = RoomCombatModel.ApplyQueuedCharacterTrigger(living, new(0, actor, "OnKill"), _ => { });
        var blocked = RoomCombatModel.ApplyQueuedCharacterTrigger(living,
            new(0, actor, "OnKill", dyingCharacter: retained, canFireTriggers: false), _ => { });
        Require(skipped.Supported && skipped.State!.Context!.Gold == 0 && absent.Supported && absent.State!.Context!.Gold == 5 &&
            blocked.Supported && blocked.State!.Context!.Gold == 0, "Zero presence, null-dying bypass or explicit queue fire permission differs.");
        string expected = JsonSerializer.Serialize(result.State, ModelJson.Options);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(RoomCombatModel.Resolve(room).State, ModelJson.Options) == expected,
            "Parallel conditional sweep differed."));
        Require(JsonSerializer.Serialize(room, ModelJson.Options) == parent, "Conditional callbacks mutated their parent.");
        Console.WriteLine("CONDITIONAL-TRIGGER-CHECKS PASS: immutable requirements, case/zero/null conditions, local sweep and retained/external dying payloads, fire permission and 32 branches.");
    }
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ConditionalTriggers", out var records) || records.GetArrayLength() == 0) return;
        var samples = records.EnumerateArray().ToArray();
        foreach (var sample in samples) Verify(sample);
        var controlled = samples.Where(sample => !sample.GetProperty("Label").GetString()!.StartsWith("natural:") &&
            !sample.GetProperty("Label").GetString()!.StartsWith("repeat:")).ToDictionary(sample => sample.GetProperty("Label").GetString()!);
        var expectedGold = new Dictionary<string, int> { ["self-missing-all"] = 25, ["self-missing-one"] = 25,
            ["self-present-case-insensitive"] = 35, ["dying-missing-status"] = 0, ["dying-present"] = 25,
            ["dying-zero-status"] = 0, ["dying-null-bypasses"] = 25, ["same-phase-status-change"] = 60 };
        Require(controlled.Keys.ToHashSet().SetEquals(expectedGold.Keys), "Missing native conditional cases.");
        foreach (var pair in expectedGold)
        {
            var before = controlled[pair.Key].GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = controlled[pair.Key].GetProperty("After").Deserialize<RoomCombatState>()!;
            Require(after.Context!.Gold - before.Context!.Gold == pair.Value, pair.Key + " did not demonstrate native presence/count/case/null/current-phase semantics.");
        }
        var first = controlled["self-missing-all"].GetProperty("AfterActor").Deserialize<CombatUnit>()!;
        var present = controlled["self-present-case-insensitive"].GetProperty("AfterActor").Deserialize<CombatUnit>()!;
        Require(controlled["self-present-case-insensitive"].GetProperty("RequiredStackCounts").EnumerateArray().All(value => value.GetInt32() == 999) &&
            present.Status("armor")!.Stacks == 1 && present.Status("valor")!.Stacks == 1,
            "Native metadata did not prove that configured required stack counts are ignored.");
        Require(!first.Triggers[0].HasTriggered && present.Triggers[0].HasTriggered && !present.Triggers[1].HasTriggered,
            "A failed status condition consumed a once flag, or an unmet condition fired.");
        var natural = samples.Where(sample => sample.GetProperty("Label").GetString() == "natural:OnKill").ToArray();
        Require(natural.Length > 0 && natural.All(sample => sample.GetProperty("Dying").Deserialize<CombatUnit>()!.Health == 0) &&
            natural.Any(sample => sample.GetProperty("After").Deserialize<RoomCombatState>()!.Context!.Gold >
                sample.GetProperty("Before").Deserialize<RoomCombatState>()!.Context!.Gold), "Natural Slay conditions did not fire with actual dead targets.");
        Parallel.For(0, 32, _ => { foreach (var sample in samples) Verify(sample); });
        Console.WriteLine($"NATIVE-CONDITIONAL-TRIGGER-CHECKS PASS: {samples.Length} complete room/actor/dying states, eight controlled cases, {natural.Length} natural Slay dispatches and 32 isolated branches.");
    }
    private static void Verify(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var actor = sample.GetProperty("Actor").Deserialize<CombatUnit>()!;
        var dying = sample.GetProperty("Dying").Deserialize<CombatUnit>();
        string parent = JsonSerializer.Serialize(new { before, actor, dying }, ModelJson.Options);
        var queued = new RoomCombatModel.QueuedCharacterTrigger(before.RoomIndex, actor, sample.GetProperty("Kind").GetString()!,
            paramInt: sample.GetProperty("ParamInt").GetInt32(), dyingCharacter: dying, canFireTriggers: sample.GetProperty("CanFire").GetBoolean(),
            triggerCount: sample.TryGetProperty("TriggerCount", out var count) ? count.GetInt32() : 1);
        var result = RoomCombatModel.ApplyQueuedCharacterTrigger(before, queued, _ => { });
        Require(result.Supported, "Conditional native phase unsupported: " + result.UnsupportedReason);
        Compare(result.State, sample.GetProperty("After").Deserialize<RoomCombatState>());
        Compare(queued.Unit, sample.GetProperty("AfterActor").Deserialize<CombatUnit>());
        Compare(queued.DyingCharacter, sample.GetProperty("AfterDying").Deserialize<CombatUnit>());
        Require(JsonSerializer.Serialize(new { before, actor, dying }, ModelJson.Options) == parent, "Conditional phase changed its parent.");
        void Compare<T>(T predicted, T actual)
        {
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
            Require(difference == null, sample.GetProperty("Label").GetString() + ": " + difference);
        }
    }
    private static void Require(bool passed, string message) { if (!passed) throw new InvalidOperationException(message); }
}
