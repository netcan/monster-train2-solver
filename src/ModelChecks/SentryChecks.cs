using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SentryChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(53);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10);
        CombatEffect Gold(int amount) => new("CardEffectRewardGold", amount, 0, "", 0, [], false);
        CombatTrigger Trigger(string kind, int gold, int damage = 0, bool once = false, bool ignored = true) => new(kind, once,
            false, ignored, 1, damage == 0 ? [Gold(gold)] : [Gold(gold), new("CardEffectDamage", damage, 0, "", 0, [], false,
                action: new("Damage", "LastAttackedCharacter", damage, false, true, [],
                    filters: new("Damaged", ["missing"], [], true, "missing", [])))], false);
        CombatUnit Enemy(int id, int hp) => new(id, "enemy", CombatTeam.Enemy, 1, hp, hp, true, false, false, [],
            [Trigger("PostAscension", 25), Trigger("OnShift", 30), Trigger("OnHit", 15), Trigger("OnDeath", 20)], isBoss: false, lastAttackerId: 0);
        CombatUnit Guard(int id, bool once, bool ignored = true, bool silenced = false) => new(id, "guard", CombatTeam.Player, 1,
            20, 20, true, false, false, silenced ? [new("silenced", 1)] : [], [Trigger("OnSentry", once ? 5 : 10, 10, once, ignored),
                new("OnSentry", false, false, true, 1, [Gold(99)], false, 1)],
            isBoss: false, lastAttackerId: 0);
        TrainCombatState Root(bool silence = false, bool stationary = false) => new([
            new(0, false, [Enemy(1, 10), Enemy(2, 30)], [], context), new(1, false, [Guard(3, true, false, silence), Guard(4, false)], [], context),
            new(2, false, [], [], context), new(3, false, [], [], context)], [new(1, stationary ? 0 : 1, true, false), new(2, stationary ? 0 : 1, true, false)], 7, context);
        var root = Root(); string parent = JsonSerializer.Serialize(root, ModelJson.Options);
        var result = TrainCombatModel.Ascend(root);
        Require(result.Supported && result.State!.Rooms[1].Units.Single(unit => unit.Id == 2).Health == 20 &&
            result.State.Rooms[1].Units.All(unit => unit.Id != 1) && result.State.Context!.Gold == 185,
            "Moved-target override, once counters, hit/death FIFO or retained dead targets differ: " + JsonSerializer.Serialize(result, ModelJson.Options));
        Require(result.RoomResults.SelectMany(room => room.Events).Where(item => item.Kind == "TriggeredDamage")
            .Select(item => item.Target).SequenceEqual([1, 2]), "A later Sentry damaged a stale living copy of a killed target.");
        var silent = TrainCombatModel.Ascend(Root(silence: true));
        Require(silent.Supported && !silent.State!.Rooms[1].Units.Single(unit => unit.Id == 3).Triggers[0].HasTriggered &&
            silent.State.Rooms[1].Units.Single(unit => unit.Id == 2).Health == 20, "Sentry lost silence/ignored-silence gates.");
        Require(TrainCombatModel.Ascend(Root(stationary: true)).State!.Context!.Gold == 0,
            "Stationary enemies ran arrival callbacks.");
        var boss = new CombatUnit(1, "boss", CombatTeam.Enemy, 1, 10, 10, true, false, true, [], Enemy(1, 10).Triggers,
            isBoss: true, lastAttackerId: 0);
        var terminalRoot = new TrainCombatState([new(0, false, [boss], [], context),
            new(1, false, [Enemy(2, 30), Guard(3, true)], [], context), new(2, false, [Guard(4, false)], [], context),
            new(3, false, [], [], context)], [new(1, 1, true, false), new(2, 1, true, false)], 7, context);
        var terminal = TrainCombatModel.Ascend(terminalRoot);
        Require(terminal.Supported && terminal.Outcome == RoomOutcome.BattleWon && terminal.State!.Context!.Gold == 160 &&
            terminal.RoomResults.SelectMany(room => room.Events).Where(item => item.Kind == "TriggeredDamage")
                .Select(item => item.Target).SequenceEqual([2, 1]), "Terminal Sentry movement lost earlier floor events or dead-Boss gates.");
        var reverse = new TrainCombatState([new(0, false, [Enemy(1, 30), Guard(4, false)], [], context), new(1, false, [], [], context)],
            [new(1, 1, true, false)], 7, context);
        Require(TrainCombatModel.Sentry(reverse, 4).State!.Context!.Gold == 0 &&
            TrainCombatModel.Sentry(reverse, 1).State!.Rooms[0].Units.Single(unit => unit.Id == 1).Health == 20,
            "Sentry selected the moved unit's own team.");
        string expected = JsonSerializer.Serialize(result.State, ModelJson.Options);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(TrainCombatModel.Ascend(root).State, ModelJson.Options) == expected,
            "Parallel Sentry movement differs."));
        Require(JsonSerializer.Serialize(root, ModelJson.Options) == parent, "Sentry changed the parent state.");
        Console.WriteLine("SENTRY-CHECKS PASS: moved overrides, queue order, once/silence, dead-target retention, stationary/opposing-team gates and 32 branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("Sentries", out var records) || records.GetArrayLength() == 0)
        {
            Require(!fixture.TryGetProperty("ModifierScenario", out var modifier) || modifier.GetString()?.StartsWith("sentry") != true,
                "Requested native Sentry fixture did not execute.");
            return;
        }
        var samples = records.EnumerateArray().ToArray();
        foreach (var sample in samples) Verify(sample);
        Require(samples.Any(item => item.GetProperty("Target").Deserialize<CombatUnit>()!.IsBoss == true),
            "Native Sentry did not target the original Boss.");
        Require(samples.Any(item => item.GetProperty("Actor").Deserialize<CombatUnit>()!.Statuses.Any(status => status.Id == "silenced")),
            "Native Sentry did not demonstrate silenced actors.");
        Require(samples.Any(item => item.GetProperty("Target").Deserialize<CombatUnit>()!.Health >
            item.GetProperty("ActualTarget").Deserialize<CombatUnit>()!.Health && item.GetProperty("Actor").Deserialize<CombatUnit>()!
                .Triggers.SelectMany(trigger => trigger.Effects).Any(effect => effect.Action is { AllowEnemy: false, AllowPlayer: true })),
            "Native moved-target override did not demonstrate opposing-team filter bypass.");
        Require(samples.Any(item => {
            var before = item.GetProperty("Actor").Deserialize<CombatUnit>()!;
            var after = item.GetProperty("ActualActor").Deserialize<CombatUnit>()!;
            return before.Triggers.Select((trigger, index) => trigger.Once && !trigger.HasTriggered && after.Triggers[index].HasTriggered).Any(value => value);
        }) && samples.All(item => item.GetProperty("ActualActor").Deserialize<CombatUnit>()!.Triggers
            .Where(trigger => trigger.TriggerAtThreshold > 0).All(trigger => !trigger.HasTriggered)),
            "Native once counters or zero-argument threshold gates were not demonstrated.");
        if (fixture.GetProperty("ModifierScenario").GetString() == "sentry-lethal")
            Require(samples.Any(item => item.GetProperty("Target").Deserialize<CombatUnit>()!.Health > 0 &&
                item.GetProperty("ActualTarget").Deserialize<CombatUnit>()!.Health == 0) &&
                samples.Any(item => item.GetProperty("Target").Deserialize<CombatUnit>()!.Health == 0 &&
                    item.GetProperty("ActualTarget").Deserialize<CombatUnit>()!.Health == 0),
                "Native lethal Sentry did not retain the dead target for a later queued actor.");
        Parallel.For(0, 32, _ => { foreach (var sample in samples) Verify(sample); });
        Console.WriteLine($"NATIVE-SENTRY-CHECKS PASS: {samples.Length} complete room/actor/retained-target transitions and 32 isolated branches.");
    }
    private static void Verify(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var actor = sample.GetProperty("Actor").Deserialize<CombatUnit>()!;
        var target = sample.GetProperty("Target").Deserialize<CombatUnit>()!;
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        var queued = new RoomCombatModel.QueuedCharacterTrigger(before.RoomIndex, actor, "OnSentry", overrideTarget: target,
            canFireTriggers: sample.GetProperty("CanFire").GetBoolean());
        var result = RoomCombatModel.ApplyQueuedCharacterTrigger(before, queued, _ => { });
        Require(result.Supported, "Native Sentry unsupported: " + result.UnsupportedReason);
        Compare(result.State, sample.GetProperty("Actual").Deserialize<RoomCombatState>());
        Compare(queued.Unit, sample.GetProperty("ActualActor").Deserialize<CombatUnit>());
        Compare(queued.OverrideTarget, sample.GetProperty("ActualTarget").Deserialize<CombatUnit>());
        Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Sentry callback changed its parent.");
    }
    private static void Compare<T>(T predicted, T actual)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, "Sentry native state differs: " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
