using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RevivalChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(88);
        CombatStatus undying = new("undying", 2, stackable: true);
        CombatTrigger Gold(string kind, int amount, bool once = false, int threshold = 0) =>
            new(kind, once, false, true, 1, [new("CardEffectRewardGold", amount, 0, "", 0, [], false)], false, threshold);
        CombatUnit Actor(int id, CombatTeam team, int hp, CombatStatus[] statuses) => new(id, "revival", team,
            0, hp, 10, false, false, false, statuses,
            [Gold("OnAnyMonsterDeathOnFloor", 10), Gold("OnAnyUnitDeathOnFloor", 15), Gold("OnReanimated", 30, true),
                Gold("OnDeath", 40), Gold("OnDeath", 41, threshold: 1)],
            statusRegistry: statuses, deathState: new(false, false, true, isSacrifice: false, statisticsListenerOnce: false));
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10, [undying]);
        var root = new RoomCombatState(0, false, [Actor(1, CombatTeam.Player, 10, [undying]),
            Actor(2, CombatTeam.Player, 10, []), Actor(3, CombatTeam.Enemy, 10, [])], [], context);
        string parent = Serialize(root);
        var first = RoomCombatModel.ApplyCardDamage(root, 1, 999);
        Require(first.Supported && first.State!.Units.Count == 3, "Undying death removed its living unit: " + first.UnsupportedReason);
        CombatUnit revived = first.State!.Units.Single(unit => unit.Id == 1);
        Require(revived.Health == 1 && revived.MaxHealth == 10 && revived.Status("undying")!.Stacks == 1 &&
            revived.DeathState!.HasStatisticsListener && !revived.DeathState.HasFinishedDying && !revived.DeathState.IsBeingRemoved,
            "Revival lost health, one-stack removal, listeners or physical life.");
        string[] order = first.Dispatches.Select(item => item.Kind + ":" + item.ActorId).ToArray();
        Require(order.SequenceEqual(["OnAnyMonsterDeathOnFloor:1", "OnAnyMonsterDeathOnFloor:2", "OnAnyMonsterDeathOnFloor:3",
            "OnAnyUnitDeathOnFloor:1", "OnAnyUnitDeathOnFloor:2", "OnAnyUnitDeathOnFloor:3", "OnReanimated:1", "OnDeath:1"]),
            "Revival changed the all-group harvest / self callbacks FIFO.");
        var second = RoomCombatModel.ApplyCardDamage(first.State, 1, 999);
        Require(second.Supported && second.State!.Units.Count == 3 && second.State.Units.Single(unit => unit.Id == 1).Status("undying") == null,
            "The last undying stack did not revive its host.");
        Require(second.State!.Context!.Gold - first.State.Context!.Gold == first.State.Context.Gold - 30,
            "A once-only reanimated callback fired twice.");
        var third = RoomCombatModel.ApplyCardDamage(second.State, 1, 999);
        Require(third.Supported && third.State!.Units.All(unit => unit.Id != 1), "A zero-stack victim did not physically die.");
        var direct = RoomCombatModel.ApplyRevival(second.State, 1);
        Require(direct.Supported && direct.State!.Units.Single(unit => unit.Id == 1).Health == 1 && direct.PendingCallbacks.Count == 8 &&
            direct.State.Context!.Gold == second.State.Context!.Gold && direct.PendingCallbacks.Last().ParamInt == 1,
            "Direct revival must work without a stack, keep its actor and leave all callbacks pending.");
        var drained = direct;
        var queue = direct.PendingCallbacks.ToList();
        Require(RoomCombatModel.DrainCharacterQueue(queue, item => {
            drained = RoomCombatModel.ApplyQueuedCharacterTrigger(drained.State!, item, queue.Add); return drained.Supported;
        }, _ => true), "Direct revival queue failed: " + drained.UnsupportedReason);
        Require(drained.State!.Units.Single(unit => unit.Id == 1).DeathState!.IsBeingRemoved == false,
            "The revival OnDeath callback marked a live actor removed.");
        var onceRoot = new RoomCombatState(root.RoomIndex, root.Deployment, root.Units.Select(unit => unit.Id == 1 ?
            unit.WithDeathState(new(false, false, true, isSacrifice: false, statisticsListenerOnce: true)) : unit).ToArray(), [], context);
        var onceResult = RoomCombatModel.ApplyCardDamage(onceRoot, 1, 999);
        Require(onceResult.Supported && onceResult.State!.Units.Single(unit => unit.Id == 1).DeathState!.HasStatisticsListener == false,
            "Revival did not consume a one-time death statistics listener.");
        var missing = new RoomCombatState(root.RoomIndex, root.Deployment, root.Units.Select(unit => unit.Id == 1 ?
            unit.WithDeathState(new(false, false, true)) : unit).ToArray(), [], context);
        Require(!RoomCombatModel.ApplyCardDamage(missing, 1, 999).Supported,
            "Unknown death listener lifetime yielded a guessed revival child.");
        Parallel.For(0, 32, _ => Require(Serialize(RoomCombatModel.ApplyCardDamage(root, 1, 999).State) == Serialize(first.State),
            "Parallel revival branches differ."));
        Require(Serialize(root) == parent, "Revival mutated its parent.");
        Console.WriteLine("REVIVAL-CHECKS PASS: repeated/last-stack revival, retained listeners, all-group FIFO, once/threshold callbacks, direct pending API, final physical death and 32 branches.");
    }
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("Revivals", out var entries) || entries.GetArrayLength() == 0) return;
        var samples = entries.EnumerateArray().ToArray();
        var operations = fixture.GetProperty("RevivalOperations").EnumerateArray().ToArray();
        Require(operations.Length == 7 && samples.Length >= 7 && samples.Count(sample => sample.GetProperty("QueueRunning").GetBoolean()) >= 2,
            "Native revival omitted direct, last-stack, zero-stack or nested queue paths.");
        foreach (var sample in samples) VerifyRevival(sample);
        foreach (var sample in operations) VerifyOperation(sample);
        var phases = fixture.GetProperty("HarvestTriggers").EnumerateArray().ToArray();
        foreach (var phase in phases) HarvestChecks.VerifyPhase(phase);
        Require(samples.Any(sample => sample.GetProperty("AfterActor").Deserialize<CombatUnit>()!.MaxHealth == 1) &&
            samples.Any(sample => sample.GetProperty("Before").Deserialize<RoomCombatState>()!.Units.Single(unit =>
                unit.Id == sample.GetProperty("ActorId").GetInt32()).Status("undying") == null),
            "Native revival missed zero max health and direct revival without a stack.");
        Require(samples.Where(sample => sample.GetProperty("Label").GetString() == "natural:revival")
            .All(sample => sample.GetProperty("AutomaticQueueDeferrals").GetInt32() == 0), "The setup queue guard changed natural battle timing.");
        Parallel.For(0, 16, _ => { foreach (var sample in samples) VerifyRevival(sample); foreach (var sample in operations) VerifyOperation(sample);
            foreach (var phase in phases) HarvestChecks.VerifyPhase(phase); });
        Console.WriteLine($"NATIVE-REVIVAL-CHECKS PASS: {samples.Length} complete revival boundaries, seven direct/removal operations, {phases.Length} callback phases, both teams, queue retention and 16 branches.");
    }
    private static void VerifyRevival(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        string parent = Serialize(before);
        var result = RoomCombatModel.ApplyRevival(before, sample.GetProperty("ActorId").GetInt32(),
            sample.GetProperty("SourceCardId").GetInt32(), sample.GetProperty("AttackerId").GetInt32());
        Require(result.Supported, "Native revival API unsupported: " + result.UnsupportedReason);
        Compare(result.State, sample.GetProperty("After").Deserialize<RoomCombatState>(), "Revival API room");
        Compare(result.State!.Units.Single(unit => unit.Id == sample.GetProperty("ActorId").GetInt32()),
            sample.GetProperty("AfterActor").Deserialize<CombatUnit>(), "Revival API actor");
        var callbacks = result.PendingCallbacks.Where(item => item.Unit.Triggers.Any(trigger => trigger.Kind == item.Kind))
            .Select(item => new Callback(item.Unit.Id, item.Kind, item.DyingCharacter?.Id ?? 0, item.ParamInt, item.TriggerCount)).ToArray();
        Compare(callbacks, sample.GetProperty("Queued").Deserialize<Callback[]>(), "Revival accepted callbacks");
        Require(Serialize(before) == parent, "Native revival changed its parent.");
    }
    private static void VerifyOperation(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        string parent = Serialize(before);
        int id = sample.GetProperty("ActorId").GetInt32(), source = sample.GetProperty("SourceCardId").GetInt32();
        var kind = sample.GetProperty("Kind").GetString();
        RoomCombatResult result = kind == "Damage" ? RoomCombatModel.ApplyNativeCardDamageAfterTraits(before, id, sample.GetProperty("Amount").GetInt32(), source) :
            kind == "DebuffHealth" ? UnitHealthModel.Apply(before, id, sample.GetProperty("Amount").GetInt32(), debuff: true) :
            RoomCombatModel.ApplyRevival(before, id, source);
        Require(result.Supported, "Native revival operation unsupported: " + result.UnsupportedReason);
        var dispatched = result.Dispatches.ToList();
        if (result.PendingCallbacks.Count > 0)
        {
            var queue = result.PendingCallbacks.ToList();
            Require(RoomCombatModel.DrainCharacterQueue(queue, item => {
                result = RoomCombatModel.ApplyQueuedCharacterTrigger(result.State!, item, queue.Add);
                dispatched.AddRange(result.Dispatches); return result.Supported;
            }, item => {
                result = item.CompletePhysicalRemovalAfterQueue ? RoomCombatModel.SettleQueuedSpawnerAndCenter(result.State!, item.Unit) :
                    RoomCombatModel.SettleQueuedSpawner(result.State!, item.Unit);
                return result.Supported;
            }), "Native revival/removal drain failed: " + result.UnsupportedReason);
        }
        Compare(result.State, sample.GetProperty("After").Deserialize<RoomCombatState>(), "Revival operation " + sample.GetProperty("Label").GetString());
        var nativeDispatches = sample.GetProperty("Dispatched").Deserialize<Callback[]>()!;
        Require(dispatched.Select(item => (item.ActorId, item.Kind, item.DyingId, item.TriggerCount)).SequenceEqual(
            nativeDispatches.Select(item => (item.ActorId, item.Kind, item.DyingId, item.TriggerCount))),
            "Native revival/removal callback FIFO differs.");
        if (result.State!.Units.FirstOrDefault(unit => unit.Id == id) is CombatUnit alive)
            Compare(alive, sample.GetProperty("AfterActor").Deserialize<CombatUnit>(), "Revival operation actor");
        Require(sample.GetProperty("QueueAfter").GetInt32() == 0 && Serialize(before) == parent, "Revival left callbacks or mutated its parent.");
    }
    private sealed record Callback(int ActorId, string Kind, int DyingId, int ParamInt, int TriggerCount);
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Compare<T>(T predicted, T actual, string label)
    { string? difference = ModelJson.Difference(Serialize(predicted), Serialize(actual)); Require(difference == null, label + ": " + difference); }
    private static void Require(bool pass, string message) { if (!pass) throw new InvalidOperationException(message); }
}
