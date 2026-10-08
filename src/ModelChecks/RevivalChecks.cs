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
        var retiredAttacker = Actor(4, CombatTeam.Enemy, 0, []).WithSacrifice(12)
            .WithDeathState(new(true, true, false, isSacrifice: true, statisticsListenerOnce: false));
        var attributedRoot = new RoomCombatState(root.RoomIndex, root.Deployment, root.Units, [],
            context.WithStatistics(BattleStatistics.Empty()));
        string attributedParent = Serialize(attributedRoot), retiredParent = Serialize(retiredAttacker);
        var attributed = RoomCombatModel.ApplyRevival(attributedRoot, 1, attackerUnitId: 4, retainedAttacker: retiredAttacker);
        Require(attributed.Supported && attributed.State!.Units.All(unit => unit.Id != 4) &&
            attributed.State.Context!.Statistics!.Value(12, "SpawnedMonsterDeaths") == 1,
            "A retained revival attacker lost sacrifice attribution or returned to the room.");
        Require(!RoomCombatModel.ApplyRevival(attributedRoot, 1, attackerUnitId: 4).Supported &&
            !RoomCombatModel.ApplyRevival(attributedRoot, 1, attackerUnitId: 5, retainedAttacker: retiredAttacker).Supported &&
            !RoomCombatModel.ApplyRevival(attributedRoot, 1, attackerUnitId: 2, retainedAttacker: root.Units[1]).Supported,
            "Missing, mismatched or duplicate retained attackers were accepted.");
        Parallel.For(0, 32, _ => Require(Serialize(RoomCombatModel.ApplyCardDamage(root, 1, 999).State) == Serialize(first.State),
            "Parallel revival branches differ."));
        Parallel.For(0, 32, _ => Require(Serialize(RoomCombatModel.ApplyRevival(attributedRoot, 1,
            attackerUnitId: 4, retainedAttacker: retiredAttacker).State) == Serialize(attributed.State),
            "Parallel retained-attacker revival branches differ."));
        Require(Serialize(attributedRoot) == attributedParent && Serialize(retiredAttacker) == retiredParent,
            "Retained-attacker revival mutated its room or actor input.");
        Require(Serialize(root) == parent, "Revival mutated its parent.");
        Console.WriteLine("REVIVAL-CHECKS PASS: repeated/last-stack revival, retained listeners/attackers, exact sacrifice attribution, all-group FIFO, once/threshold callbacks, direct pending API, final physical death and 32 branches.");
    }
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("Revivals", out var entries) || entries.GetArrayLength() == 0) return;
        var samples = entries.EnumerateArray().ToArray();
        var operations = fixture.GetProperty("RevivalOperations").EnumerateArray().ToArray();
        bool auraIntegration = fixture.GetProperty("ModifierScenario").GetString() == "persistent-enchantment-revivals";
        bool summonIntegration = !auraIntegration && fixture.GetProperty("ModifierScenario").GetString()?.Contains("-revival") == true;
        if (auraIntegration) VerifyAuraCoverage(samples, operations);
        else if (summonIntegration) VerifySummonCoverage(fixture, samples, operations);
        else Require(operations.Length == 7 && samples.Length >= 7 && samples.Count(sample => sample.GetProperty("QueueRunning").GetBoolean()) >= 2,
            "Native revival omitted direct, last-stack, zero-stack or nested queue paths.");
        foreach (var sample in samples) VerifyRevival(sample);
        foreach (var sample in operations) VerifyOperation(sample);
        var phases = fixture.GetProperty("HarvestTriggers").EnumerateArray().ToArray();
        foreach (var phase in phases) HarvestChecks.VerifyPhase(phase);
        if (!summonIntegration && !auraIntegration) Require(samples.Any(sample => sample.GetProperty("AfterActor").Deserialize<CombatUnit>()!.MaxHealth == 1) &&
            samples.Any(sample => sample.GetProperty("Before").Deserialize<RoomCombatState>()!.Units.Single(unit =>
                unit.Id == sample.GetProperty("ActorId").GetInt32()).Status("undying") == null),
            "Native revival missed zero max health and direct revival without a stack.");
        Require(samples.Where(sample => sample.GetProperty("Label").GetString() == "natural:revival")
            .All(sample => sample.GetProperty("AutomaticQueueDeferrals").GetInt32() == 0), "The setup queue guard changed natural battle timing.");
        Parallel.For(0, 16, _ => { foreach (var sample in samples) VerifyRevival(sample); foreach (var sample in operations) VerifyOperation(sample);
            foreach (var phase in phases) HarvestChecks.VerifyPhase(phase); });
        if (auraIntegration)
            Console.WriteLine($"NATIVE-REVIVAL-AURA-CHECKS PASS: {samples.Length} natural revival boundaries, {phases.Length} callback phases, " +
                "first/last stacks, retained source binding, complete world/RNG/queue states and 16 branches.");
        else if (summonIntegration)
            Console.WriteLine($"NATIVE-REVIVAL-SUMMON-CHECKS PASS: {samples.Length} complete revival boundaries, {phases.Length} callback phases, host/child equipment and live-source death births, exact states and 16 branches.");
        else Console.WriteLine($"NATIVE-REVIVAL-CHECKS PASS: {samples.Length} complete revival boundaries, seven direct/removal operations, {phases.Length} callback phases, both teams, queue retention and 16 branches.");
    }
    private static void VerifyAuraCoverage(FixtureValue[] samples, FixtureValue[] operations)
    {
        Require(operations.Length == 0 && samples.Length >= 4 && samples.All(sample =>
            sample.GetProperty("Completed").GetBoolean() && sample.GetProperty("QueueRunning").GetBoolean() &&
            sample.GetProperty("Label").GetString() == "natural:revival" &&
            sample.GetProperty("AutomaticQueueDeferrals").GetInt32() == 0),
            "Aura revival must run naturally inside the native trigger queue without setup or queue deferrals.");
        var actors = samples.Select(sample => (Before: sample.GetProperty("Before").Deserialize<RoomCombatState>()!.Units
                .Single(unit => unit.Id == sample.GetProperty("ActorId").GetInt32()),
            After: sample.GetProperty("AfterActor").Deserialize<CombatUnit>()!)).ToArray();
        Require(actors.GroupBy(pair => pair.Before.Id).Count(group =>
                group.Any(pair => pair.Before.Status("undying")?.Stacks == 2) &&
                group.Any(pair => pair.Before.Status("undying")?.Stacks == 1)) >= 2 &&
            actors.All(pair => pair.Before.Health == 0 && pair.After.Health == 1 &&
                pair.After.DeathState?.IsDestroyed == false && pair.After.DeathState.IsDespawned == false &&
                pair.After.Triggers.SelectMany(trigger => trigger.Effects).Any(effect => effect.Enchantment?.Bound == true) &&
                pair.After.Status("undying")?.Stacks == (pair.Before.Status("undying")!.Stacks > 1 ?
                    pair.Before.Status("undying")!.Stacks - 1 : (int?)null)),
            "Aura revival omitted first/last stacks on two bound sources or lost their lifecycle flags.");
        Require(actors.All(pair => pair.Before.Triggers.SelectMany(trigger => trigger.Effects)
                .Where(effect => effect.Enchantment != null).All(effect => effect.Enchantment!.Bound) &&
            Serialize(pair.Before.Triggers.SelectMany(trigger => trigger.Effects).Where(effect => effect.Enchantment != null)
                .Select(effect => effect.Enchantment!.State).ToArray()) ==
            Serialize(pair.After.Triggers.SelectMany(trigger => trigger.Effects).Where(effect => effect.Enchantment != null)
                .Select(effect => effect.Enchantment!.State).ToArray())),
            "Natural source revival must preserve its bound aura's primary/preview maps and cached status.");
        foreach (var sample in samples)
        {
            int id = sample.GetProperty("ActorId").GetInt32();
            Require(sample.GetProperty("Queued").Deserialize<Callback[]>()!.TakeLast(2)
                .Select(item => (item.ActorId, item.Kind, item.ParamInt)).SequenceEqual(
                    [(id, "OnReanimated", 0), (id, "OnDeath", 1)]),
                "Aura revival lost the reanimated/death callback order or revived payload.");
        }
    }
    private static void VerifySummonCoverage(FixtureValue fixture, FixtureValue[] samples, FixtureValue[] operations)
    {
        Require(operations.Length == 0, "The summon integration used standalone setup operations instead of real card/combat deaths.");
        var actors = samples.Select(sample => {
            var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
            int id = sample.GetProperty("ActorId").GetInt32();
            return (Before: before.Units.Single(unit => unit.Id == id),
                After: sample.GetProperty("AfterActor").Deserialize<CombatUnit>()!);
        }).ToArray();
        var hosts = actors.Where(pair => pair.Before.EquipmentCards?.Count > 0 &&
            pair.Before.Triggers.Any(trigger => trigger.Effects.Any(effect => effect.Summon != null))).ToArray();
        var children = actors.Where(pair => pair.Before.Status("cardless") != null &&
            !pair.Before.Triggers.Any(trigger => trigger.Effects.Any(effect => effect.Summon != null))).ToArray();
        Require(hosts.Length >= 2 && hosts.GroupBy(pair => pair.Before.Id).Any(group =>
                group.Any(pair => pair.Before.Status("undying")?.Stacks == 2) &&
                group.Any(pair => pair.Before.Status("undying")?.Stacks == 1)) && children.Length > 0,
            "Summon revival did not cover an equipped host's first/last stacks and cardless child revival.");
        Require(actors.All(pair => pair.Before.Health == 0 && pair.After.Health == 1 &&
            pair.After.SpawnerCardId == pair.Before.SpawnerCardId && pair.After.DeathState?.IsBeingRemoved == false &&
            pair.Before.EquipmentCards!.SequenceEqual(pair.After.EquipmentCards!)),
            "Revival removed equipment, changed its source or lost physical life.");
        var hostIds = hosts.Select(pair => pair.Before.Id).ToHashSet();
        var births = fixture.GetProperty("TriggeredSummons").EnumerateArray().Where(record =>
            record.GetProperty("Kind").GetString() == "OnDeath" &&
            record.GetProperty("ActorBefore").GetProperty("Health").GetInt32() > 0 &&
            hostIds.Contains(record.GetProperty("ActorBefore").GetProperty("Id").GetInt32()) &&
            record.GetProperty("After").GetProperty("Context").GetProperty("NextUnitId").GetInt32() >
            record.GetProperty("Before").GetProperty("Context").GetProperty("NextUnitId").GetInt32()).ToArray();
        Require(births.Any(record => record.GetProperty("EquipmentCardId").GetInt32() > 0),
            "No equipment-owned OnDeath summon used a revived live host.");
        int restoredCaches = 0;
        foreach (var sample in samples)
        {
            var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = sample.GetProperty("After").Deserialize<RoomCombatState>()!;
            int actorId = sample.GetProperty("ActorId").GetInt32();
            foreach (var card in before.Context!.CardRegistry!.Where(card => card.RawPlayedRoomUnitIds?.Contains(actorId) == true))
            {
                Require(!card.PlayedRoomUnitIds!.Contains(actorId) && after.Context!.FindCard(card.InstanceId)!.PlayedRoomUnitIds!.Contains(actorId),
                    "A native raw room-cache member was not hidden at zero HP and restored after revival.");
                restoredCaches++;
            }
        }
        Require(restoredCaches > 0, "The recording omitted cached weak references across revival.");
        if (fixture.GetProperty("Schema").GetInt32() >= 92)
        {
            Require(samples.All(sample => sample.GetProperty("AttackerId").GetInt32() == 0 ||
                sample.GetProperty("Attacker").Deserialize<CombatUnit>()?.Id == sample.GetProperty("AttackerId").GetInt32()),
                "A revival omitted its original attacker reference.");
            if (fixture.GetProperty("ModifierScenario").GetString()!.Contains("-fresh"))
                Require(samples.Any(sample => {
                    int id = sample.GetProperty("AttackerId").GetInt32();
                    return id != 0 && sample.GetProperty("Before").Deserialize<RoomCombatState>()!.Units.All(unit => unit.Id != id);
                }), "Fresh-source revival did not cover an attacker retained after room removal.");
        }
        foreach (var sample in fixture.GetProperty("DecisionRoomCaches").EnumerateArray())
        {
            int[] raw = sample.GetProperty("Raw").Deserialize<int[]>()!;
            var alive = sample.GetProperty("Living").Deserialize<int[]>()!.ToHashSet();
            Require(raw.Where(alive.Contains).SequenceEqual(sample.GetProperty("Captured").Deserialize<int[]>()!),
                "Decision cache normalization changed live membership or kept a removed reference.");
        }
        foreach (var sample in samples.Where(sample => hostIds.Contains(sample.GetProperty("ActorId").GetInt32())))
        {
            var queue = sample.GetProperty("Queued").Deserialize<Callback[]>()!;
            int actorId = sample.GetProperty("ActorId").GetInt32();
            Require(queue.TakeLast(2).Select(item => (item.ActorId, item.Kind, item.ParamInt)).SequenceEqual(
                [(actorId, "OnReanimated", 0), (actorId, "OnDeath", 1)]),
                "Revival changed reanimated/death callback order or lost its revived payload.");
        }
    }
    private static void VerifyRevival(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        string parent = Serialize(before);
        int attackerId = sample.GetProperty("AttackerId").GetInt32();
        CombatUnit? attacker = sample.TryGetProperty("Attacker", out var capturedAttacker) ? capturedAttacker.Deserialize<CombatUnit>() : null;
        if (attacker != null) Require(attacker.Id == attackerId, "Native revival attacker snapshot has a different identity.");
        CombatUnit? retained = before.Units.Any(unit => unit.Id == attackerId) ? null : attacker;
        string retainedParent = Serialize(retained);
        var result = RoomCombatModel.ApplyRevival(before, sample.GetProperty("ActorId").GetInt32(),
            sample.GetProperty("SourceCardId").GetInt32(), attackerId, retained);
        Require(result.Supported, "Native revival API unsupported: " + result.UnsupportedReason);
        Compare(result.State, sample.GetProperty("After").Deserialize<RoomCombatState>(), "Revival API room");
        Compare(result.State!.Units.Single(unit => unit.Id == sample.GetProperty("ActorId").GetInt32()),
            sample.GetProperty("AfterActor").Deserialize<CombatUnit>(), "Revival API actor");
        var callbacks = result.PendingCallbacks.Where(item => item.Unit.Triggers.Any(trigger => trigger.Kind == item.Kind))
            .Select(item => new Callback(item.Unit.Id, item.Kind, item.DyingCharacter?.Id ?? 0, item.ParamInt, item.TriggerCount)).ToArray();
        Compare(callbacks, sample.GetProperty("Queued").Deserialize<Callback[]>(), "Revival accepted callbacks");
        Require(Serialize(before) == parent, "Native revival changed its parent.");
        Require(Serialize(retained) == retainedParent && (retained == null || result.State.Units.All(unit => unit.Id != retained.Id)),
            "Revival mutated or returned a retained attacker to the room.");
        if (retained != null)
        {
            var missing = RoomCombatModel.ApplyRevival(before, sample.GetProperty("ActorId").GetInt32(), attackerUnitId: attackerId);
            Require(!missing.Supported && missing.State == null && missing.UnsupportedReason!.Contains("attacker reference"),
                "A missing retained attacker silently lost revival attribution.");
        }
        if (before.Context?.CardRegistry?.Any(card => card.RawPlayedRoomUnitIds != null) == true)
        {
            var context = before.Context;
            foreach (var card in context.CardRegistry!) context = context.WithCard(card.WithRoomCacheState(card.PlayedRoomUnitIds, null));
            var incomplete = new RoomCombatState(before.RoomIndex, before.Deployment, before.Units, before.ExternalInteractions, context, before.Preview);
            string incompleteParent = Serialize(incomplete);
            var rejected = RoomCombatModel.ApplyRevival(incomplete, sample.GetProperty("ActorId").GetInt32());
            Require(!rejected.Supported && rejected.State == null && rejected.UnsupportedReason!.Contains("raw card room-cache") &&
                Serialize(incomplete) == incompleteParent, "An unknown raw cache produced a guessed revival child.");
        }
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
