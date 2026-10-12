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
        VerifyDeathwish(samples);
        VerifyGenericDeathwish();
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameModuleMvid").GetString() ==
            "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" && root.GetProperty("Boundary").GetString() == "RealStatusAndDrainedQueue" &&
            !root.GetProperty("StatusMutationsSuppressed").GetBoolean() && root.GetProperty("LiveContextUnchanged").GetBoolean() &&
            root.GetProperty("ExternalPreviewPreparationsRecorded").GetBoolean() &&
            root.GetProperty("Mismatches").GetInt32() == 0 && samples.Length == 37,
            "Real enchantment combat version, mutation, restoration or matrix gate failed.");
        foreach (var sample in samples)
            Require(sample.GetProperty("Completed").GetBoolean() && sample.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "Incomplete/mismatched real enchantment combat sample.");
        Require(samples.Count(sample => sample.GetProperty("Label").GetString()!.StartsWith("random-carried-", StringComparison.Ordinal)) == 16 &&
            samples.Count(sample => sample.GetProperty("Label").GetString()!.StartsWith("deathwish-", StringComparison.Ordinal)) == 5 &&
            samples.Any(sample => sample.GetProperty("Direct").GetBoolean()) &&
            samples.Any(sample => !sample.GetProperty("Before").GetProperty("AllowUpdates").GetBoolean()),
            "Native random, direct reentry or disabled-manager cases are missing.");
        Parallel.For(0, 32, _ => { foreach (var sample in samples) Verify(sample); });
        int requests = samples.Sum(sample => sample.GetProperty("Requests").GetArrayLength());
        int callbacks = samples.Sum(sample => sample.GetProperty("Callbacks").GetArrayLength());
        Console.WriteLine($"NATIVE-ENCHANTMENT-COMBAT-CHECKS PASS: {samples.Length} real status/train/effect transitions, {requests} status calls, " +
            $"{callbacks} complete callback payloads, standalone Deathwish gold/armor drains, shared-world gold drains and guarded actor mutations, " +
            "guarded/direct reentry, retained per-effect maps/RNG and 32 isolated branches.");
    }
    private static void VerifyDeathwish(FixtureValue[] samples)
    {
        EnchantmentCombatState State(string label, string field)
        {
            FixtureValue sample = samples.Single(item => item.GetProperty("Label").GetString() == label);
            return sample.GetProperty(field).Deserialize<EnchantmentCombatState>()!;
        }
        int[] ActiveSources(EnchantmentCombatState state) => state.Train.Rooms.SelectMany(room => room.Units)
            .Where(unit => unit.Triggers.Any(trigger => trigger.DeathwishActive == true)).Select(unit => unit.Id).Distinct().ToArray();

        FixtureValue initial = samples.Single(item => item.GetProperty("Label").GetString() == "deathwish-initial");
        EnchantmentCombatState before = initial.GetProperty("Before").Deserialize<EnchantmentCombatState>()!;
        EnchantmentCombatResult update = EnchantmentCombatModel.UpdateAll(before);
        Require(update.Supported, "Deathwish initial update: " + update.UnsupportedReason);
        int[] active = ActiveSources(update.State!);
        EnchantmentCallback[] entries = update.Callbacks.Where(item => item.Kind == "OnDeathwish").ToArray();
        Require(active.Length == 1 && entries.Length == 2 && entries.Select(item => item.UnitId).Distinct().SequenceEqual(active) &&
            entries.Select(item => item.ExclusiveTriggerStateId).Distinct().Count() == 2,
            "Initial front activation did not retain separate exclusive Deathwish sources.");
        int[] muted = ActiveSources(State("deathwish-muted", "AfterQueue"));
        int[] unmuted = ActiveSources(State("deathwish-unmuted", "AfterQueue"));
        int[] shifted = ActiveSources(State("deathwish-front-shift", "AfterQueue"));
        int[] returned = ActiveSources(State("deathwish-front-return", "AfterQueue"));
        Require(muted.Length == 0 && unmuted.Length == 1 && shifted.Length == 1 && returned.SequenceEqual(unmuted) &&
            !shifted.SequenceEqual(unmuted), "Mute or front-line transitions did not move Deathwish source membership.");
        int GoldDelta(string label)
        {
            FixtureValue sample = samples.Single(item => item.GetProperty("Label").GetString() == label);
            EnchantmentCombatState before = sample.GetProperty("Before").Deserialize<EnchantmentCombatState>()!;
            EnchantmentCombatState after = sample.GetProperty("AfterQueue").Deserialize<EnchantmentCombatState>()!;
            return after.Train.Context!.Gold - before.Train.Context!.Gold;
        }
        Require(new[] { GoldDelta("deathwish-initial"), GoldDelta("deathwish-muted"), GoldDelta("deathwish-unmuted"),
            GoldDelta("deathwish-front-shift"), GoldDelta("deathwish-front-return") }.SequenceEqual(new[] { 5, 5, 5, 10, 10 }),
            "Nonempty Deathwish gold effects did not execute once per matching native callback.");
        int ArmorDelta(string label)
        {
            FixtureValue sample = samples.Single(item => item.GetProperty("Label").GetString() == label);
            EnchantmentCombatState before = sample.GetProperty("Before").Deserialize<EnchantmentCombatState>()!;
            EnchantmentCombatState after = sample.GetProperty("AfterQueue").Deserialize<EnchantmentCombatState>()!;
            int Armor(EnchantmentCombatState state) => state.Train.Rooms.SelectMany(room => room.Units)
                .Sum(unit => unit.Status("armor")?.Stacks ?? 0);
            return Armor(after) - Armor(before);
        }
        Require(new[] { ArmorDelta("deathwish-initial"), ArmorDelta("deathwish-muted"), ArmorDelta("deathwish-unmuted"),
            ArmorDelta("deathwish-front-shift"), ArmorDelta("deathwish-front-return") }.SequenceEqual(new[] { 1, 1, 1, 2, 2 }),
            "Self-targeted Deathwish Armor effects did not execute once per matching native callback.");
    }
    private static void VerifyGenericDeathwish()
    {
        CombatUnit Actor(int id, bool active, int stateId, IReadOnlyList<CombatStatus>? statuses = null)
        {
            CombatTrigger Source(string kind, int triggerId) => new(kind, false, false, false, 1,
                Array.Empty<CombatEffect>(), stateId: triggerId, deathwishActive: active);
            return new CombatUnit(id, "deathwish-unit", CombatTeam.Player, 0, 10, 10, true, false, false,
                statuses ?? Array.Empty<CombatStatus>(), new[] { Source("OnDeathwish", stateId), Source("OnDeathwishLost", stateId + 1) },
                nextTriggerId: stateId + 2);
        }
        CombatUnit WithStatuses(CombatUnit unit, params CombatStatus[] statuses) => new(unit.Id, unit.AssetKey, unit.Team,
            unit.BaseAttack, unit.Health, unit.MaxHealth, unit.CanAttack, unit.IsPyre, unit.EndsBattleOnDeath,
            statuses, unit.Triggers, unit.SpawnerCardId, unit.Size, unit.StatusImmunities, unit.Subtypes, unit.Modifiers,
            unit.IsBoss, unit.LastAttackerId, unit.StatusRegistry, unit.EquipmentCards, unit.NextTriggerId, unit.Ability,
            unit.StatusDictionary, unit.AbilityRules, unit.HordeDefinition, unit.IsSpawning, unit.SacrificeCardId,
            unit.DeathState, unit.BumpRules);
        int[] Active(RoomCombatState state) => state.Units.Where(unit => unit.Triggers.Any(trigger => trigger.DeathwishActive == true))
            .Select(unit => unit.Id).ToArray();
        RoomCombatState Room(CombatUnit front, CombatUnit back) => new(2, false, new[] { front, back }, Array.Empty<string>());

        CombatUnit front = Actor(1, false, 10), back = Actor(2, false, 20);
        RoomCombatResult initial = EnchantmentWorldModel.Update(Room(front, back));
        Require(initial.Supported && Active(initial.State!).SequenceEqual(new[] { 1 }) &&
            initial.PendingCallbacks.Count == 2 && initial.PendingCallbacks.All(item => item.Kind == "OnDeathwish" && item.Unit.Id == 1) &&
            initial.PendingCallbacks.Select(item => item.ExclusiveTriggerStateId).SequenceEqual(new int?[] { 10, 11 }),
            "A room without an aura world did not activate both front Deathwish sources with exclusive identities.");

        var muted = new RoomCombatResult(Room(WithStatuses(initial.State!.Units[0], new CombatStatus("muted", 1)), initial.State.Units[1]),
            RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        RoomCombatResult silenced = EnchantmentWorldModel.StatusChanged(muted, "muted", false, true);
        Require(silenced.Supported && Active(silenced.State!).Length == 0 && silenced.PendingCallbacks.Count == 2 &&
            silenced.PendingCallbacks.All(item => item.Kind == "OnDeathwishLost" && item.Unit.Id == 1),
            "A muted Deathwish source did not lose membership in a room without an aura world.");

        RoomCombatResult unmuted = EnchantmentWorldModel.StatusChanged(new RoomCombatResult(
            Room(WithStatuses(silenced.State!.Units[0]), silenced.State.Units[1]), RoomOutcome.Exchanged, 0,
            new List<CombatEvent>()), "muted", false, true);
        CombatUnit shiftedFront = WithStatuses(unmuted.State!.Units[0], new CombatStatus("untouchable", 1));
        RoomCombatResult shifted = DeathwishModel.Update(Room(shiftedFront, unmuted.State.Units[1]));
        Require(unmuted.Supported && shifted.Supported && Active(shifted.State!).SequenceEqual(new[] { 2 }) &&
            shifted.PendingCallbacks.Select(item => item.Kind).SequenceEqual(new[] { "OnDeathwishLost", "OnDeathwishLost", "OnDeathwish", "OnDeathwish" }) &&
            shifted.PendingCallbacks.Select(item => item.ExclusiveTriggerStateId).SequenceEqual(new int?[] { 10, 11, 20, 21 }),
            "A front change did not transfer Deathwish source membership in native actor/trigger order.");

        var trainCallbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
        TrainCombatResult global = EnchantmentWorldModel.UpdateAll(new TrainCombatState(new[] { Room(front, back) },
            Array.Empty<EnemyMovement>(), 0), trainCallbacks.Add);
        Require(global.Supported && Active(global.State!.Rooms.Single()).SequenceEqual(new[] { 1 }) &&
            trainCallbacks.Count == 2 && trainCallbacks.All(item => item.Kind == "OnDeathwish"),
            "The global combat update skipped Deathwish when no persistent aura world was captured.");

        UnityRng rng = default;
        var context = new CombatContext(new CardCycleState(Array.Empty<CardToken>(), Array.Empty<CardToken>(),
            Array.Empty<CardToken>(), rng, 0, Array.Empty<string>()), rng, 40, 1, 10);
        CombatUnit GoldActor(int id, int stateId) => new CombatUnit(id, "deathwish-unit", CombatTeam.Player, 0, 10, 10,
            true, false, false, Array.Empty<CombatStatus>(), new[]
            {
                new CombatTrigger("OnDeathwish", false, false, true, 1,
                    new[] { new CombatEffect("CardEffectRewardGold", 1, 0, "", 0, Array.Empty<string>(), false) },
                    skipDuringDeployment: false, stateId: stateId, deathwishActive: false),
                new CombatTrigger("OnDeathwishLost", false, false, true, 1,
                    new[] { new CombatEffect("CardEffectRewardGold", 1, 0, "", 0, Array.Empty<string>(), false) },
                    skipDuringDeployment: false, stateId: stateId + 1, deathwishActive: false)
            }, nextTriggerId: stateId + 2);
        RoomCombatState goldRoom = new RoomCombatState(0, false, new[] { GoldActor(3, 30), GoldActor(4, 40) },
            Array.Empty<string>(), context);
        TrainCombatResult drained = EnchantmentWorldModel.UpdateAll(new TrainCombatState(new[] { goldRoom },
            Array.Empty<EnemyMovement>(), 0, context));
        Require(drained.Supported && drained.State!.Context!.Gold == 45 &&
            drained.State.Rooms.Single().Units.Single(unit => unit.Id == 3).Triggers[0].DeathwishActive == true,
            "A nonempty ordinary-battle Deathwish callback did not execute through the combat queue.");
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
        VerifySharedWorld(sample, before);
        Require(JsonSerializer.Serialize(before) == parent, label + ": parent state changed.");
        foreach (var effect in before.Train.Rooms.SelectMany(room => room.Units).SelectMany(unit => unit.Triggers).SelectMany(trigger => trigger.Effects)
            .Where(effect => effect.Enchantment != null))
        {
            Require(ReferenceEquals(effect.Enchantment, effect.WithCounter(9).Enchantment) &&
                ReferenceEquals(effect.Enchantment, effect.WithActionValue(11).Enchantment), "Effect copying lost its persistent enchantment state.");
        }
    }
    private static void VerifySharedWorld(FixtureValue sample, EnchantmentCombatState before)
    {
        string label = sample.GetProperty("Label").GetString()!;
        TrainCombatState train = EnchantmentWorldModel.Attach(before);
        RoomCombatState input = train.Rooms.First();
        string parent = JsonSerializer.Serialize(train);
        RoomCombatResult result = sample.GetProperty("Direct").GetBoolean()
            ? EnchantmentWorldModel.UpdateEffect(input, sample.GetProperty("SourceId").GetInt32(),
                sample.GetProperty("TriggerIndex").GetInt32(), sample.GetProperty("EffectIndex").GetInt32())
            : EnchantmentWorldModel.Update(input);
        Require(result.Supported, label + " shared-world update: " + result.UnsupportedReason);
        EnchantmentCombatState actual = result.State!.Context!.Enchantments!.Frame(result.State.Context);
        Compare(actual, sample.GetProperty("Actual").Deserialize<EnchantmentCombatState>()!, label + " shared-world state");
        Compare(result.PendingCallbacks.Select(EnchantmentCallback.From).ToArray(),
            sample.GetProperty("Callbacks").Deserialize<EnchantmentCallback[]>()!, label + " shared-world callbacks");
        var drained = EnchantmentWorldModel.Drain(new EnchantmentCombatResult(actual, Array.Empty<EnchantmentRequest>(), result.PendingCallbacks));
        bool hasActorMutatingChildren = result.PendingCallbacks.Any(callback =>
        {
            CombatUnit unit = actual.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(actor => actor.Id == callback.Unit.Id) ?? callback.Unit;
            return unit.Triggers.Where(trigger => trigger.Kind == callback.Kind).SelectMany(trigger => trigger.Effects)
                .Any(effect => effect.Type != "CardEffectRewardGold");
        });
        if (hasActorMutatingChildren)
        {
            Require(!drained.Supported && drained.UnsupportedReason ==
                "Aura callbacks that change actors/cards require automatic combat lifecycle integration.",
                label + " shared-world actor mutation was not held at the documented boundary.");
            Require(JsonSerializer.Serialize(train) == parent, label + ": rejected shared-world drain changed its parent state.");
            return;
        }
        Require(drained.Supported, label + " shared-world drain: " + drained.UnsupportedReason);
        EnchantmentCombatState after = drained.State!;
        foreach (var id in sample.GetProperty("PreviewPreparedUnitIds").EnumerateArray())
            after = EnchantmentCombatModel.PrepareForPreview(after, id.GetInt32());
        Compare(after, sample.GetProperty("AfterQueue").Deserialize<EnchantmentCombatState>()!, label + " shared-world after queue");
        Require(JsonSerializer.Serialize(train) == parent, label + ": shared-world parent state changed.");
        Require(!EnchantmentWorldModel.UpdateEffect(input, int.MaxValue, 0, 0).Supported &&
            !EnchantmentWorldModel.UpdateEffect(input, sample.GetProperty("SourceId").GetInt32(), -1, 0).Supported,
            "Invalid shared-world enchantment effect identity was accepted.");
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
