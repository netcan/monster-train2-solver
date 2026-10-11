using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BumpChecks
{
    private sealed record CallbackSnapshot(int ActorId, string Kind, int ParamInt, int ParamInt2, string? ParamString,
        int TriggerCount, int DyingId, int OverrideTargetId, int LastSpawnedOverrideUnitId);
    internal static readonly string[] Labels = ["player-up", "player-down", "player-partial-up-ignores-range",
        "player-pyre-blocked", "player-zero", "player-clamped-down", "enemy-up", "enemy-down", "enemy-rooted",
        "enemy-immobile-before-rooted", "enemy-loop-up-to-bottom", "enemy-room-multiple-targets",
        "enemy-to-top-before-merge", "player-cross-room-horde-merge", "enemy-cross-room-horde-merge",
        "enemy-into-pyre", "enemy-full-room-blocked", "enemy-partial-full-room", "enemy-relentless-up-destroys-room",
        "enemy-companion-relentless-up-refuses-ascension"];

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("BumpOperations", out var values) || values.GetArrayLength() == 0) return;
        var records = values.EnumerateArray().ToArray();
        const string pairedRelentlessLabel = "enemy-companion-relentless-up-refuses-ascension";
        const string outerBossLabel = "enemy-outer-boss-move";
        const string pairedOuterBossLabel = "enemy-outer-boss-paired-relentless-move";
        bool hasPairedRelentless = records.Any(record => record.GetProperty("Label").GetString() == pairedRelentlessLabel);
        bool hasOuterBoss = records.Any(record => record.GetProperty("Label").GetString() == outerBossLabel);
        bool hasPairedOuterBoss = records.Any(record => record.GetProperty("Label").GetString() == pairedOuterBossLabel);
        string[] expectedLabels = Labels.Where(label => label != pairedRelentlessLabel || hasPairedRelentless)
            .Concat(hasOuterBoss ? new[] { outerBossLabel } : Array.Empty<string>())
            .Concat(hasPairedOuterBoss ? new[] { pairedOuterBossLabel } : Array.Empty<string>()).ToArray();
        Require(records.Select(record => record.GetProperty("Label").GetString()!).SequenceEqual(expectedLabels), "Missing native Bump boundary cases.");
        var setup = fixture.GetProperty("Actions").EnumerateArray().Where(action => action.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(setup.Length == records.Length, "Authored paid Bump action inventory differs from the effect observations.");
        for (int index = 0; index < setup.Length; index++)
        {
            var before = setup[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
            var action = setup[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            var card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
            Require(action.CardInstanceId == records[index].GetProperty("SourceCardId").GetInt32() &&
                rule.Effects.Count == 1 && rule.Effects[0].Type == "Bump" &&
                rule.Effects[0].Value == records[index].GetProperty("Amount").GetInt32(), "Authored action and native Bump effect differ.");
        }
        foreach (var record in records) Verify(record);
        Parallel.For(0, 32, _ => { foreach (var record in records) Verify(record); });
        var group = records.Single(record => record.GetProperty("Label").GetString() == "enemy-room-multiple-targets");
        Require(group.GetProperty("Targets").GetArrayLength() == 2, "Bump never exercised simultaneous targets.");
        var destinations = new Dictionary<string, int> { ["player-up"] = 1, ["player-down"] = 0,
            ["player-partial-up-ignores-range"] = 2, ["player-pyre-blocked"] = 2, ["player-zero"] = 2,
            ["player-clamped-down"] = 0, ["enemy-up"] = 1, ["enemy-down"] = 0, ["enemy-rooted"] = 0,
            ["enemy-immobile-before-rooted"] = 0, ["enemy-loop-up-to-bottom"] = 0, ["enemy-room-multiple-targets"] = 1,
            ["enemy-to-top-before-merge"] = 2, ["enemy-into-pyre"] = 3, ["enemy-full-room-blocked"] = 1,
            ["enemy-partial-full-room"] = 1 };
        foreach (var record in records.Where(record => destinations.ContainsKey(record.GetProperty("Label").GetString()!)))
        {
            string label = record.GetProperty("Label").GetString()!;
            var after = record.GetProperty("After").Deserialize<TrainCombatState>()!;
            foreach (int id in record.GetProperty("Targets").Deserialize<int[]>()!)
                Require(after.Context!.SpawnPoints!.Units.Single(unit => unit.UnitId == id).Current!.RoomIndex == destinations[label],
                    label + " did not reach the intended native boundary");
        }
        var ranged = records.Single(record => record.GetProperty("Label").GetString() == "player-partial-up-ignores-range");
        Compare(ranged.GetProperty("Before").Deserialize<TrainCombatState>()!.Context!.BattleRng,
            ranged.GetProperty("After").Deserialize<TrainCombatState>()!.Context!.BattleRng, "Native Bump unexpectedly sampled its integer range");
        VerifyOrdinaryCompanionMovement(records.Single(record => record.GetProperty("Label").GetString() == "enemy-up"));
        VerifyOuterBossMovement(records.Single(record => record.GetProperty("Label").GetString() == "enemy-up"));
        if (hasOuterBoss)
        {
            var outer = records.Single(record => record.GetProperty("Label").GetString() == outerBossLabel);
            var before = outer.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var after = outer.GetProperty("After").Deserialize<TrainCombatState>()!;
            int outerId = outer.GetProperty("Targets").Deserialize<int[]>()!.Single();
            CombatUnit boss = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == outerId);
            SpawnPointReference oldPoint = before.Context!.SpawnPoints!.Units.Single(unit => unit.UnitId == outerId).Current!;
            SpawnPointReference newPoint = after.Context!.SpawnPoints!.Units.Single(unit => unit.UnitId == outerId).Current!;
            Require(boss.BumpRules?.OuterBoss == true && oldPoint.OuterBossAnchor && newPoint.OuterBossAnchor &&
                Math.Abs(newPoint.RoomIndex - oldPoint.RoomIndex) == 1,
                "Native Outer Boss Bump did not move between separate train anchors.");
        }
        var rooted = records.Single(record => record.GetProperty("Label").GetString() == "enemy-rooted");
        var immobile = records.Single(record => record.GetProperty("Label").GetString() == "enemy-immobile-before-rooted");
        Require(rooted.GetProperty("TargetsAfter").Deserialize<CombatUnit[]>()!.Single().Status("rooted")!.Stacks == 1 &&
            immobile.GetProperty("TargetsAfter").Deserialize<CombatUnit[]>()!.Single().Status("rooted")!.Stacks == 1,
            "Native rooting or immobility priority was not exercised");
        foreach (var record in records.Where(record => record.GetProperty("Label").GetString()!.Contains("full-room")))
            Require(record.GetProperty("Before").Deserialize<TrainCombatState>()!.Context!.SpawnPoints!.Group(2, CombatTeam.Enemy)!
                .Occupants.Take(record.GetProperty("Before").Deserialize<TrainCombatState>()!.Context!.SpawnPoints!.Group(2, CombatTeam.Enemy)!.GroupCount)
                .All(id => id > 0), "Native full-room case had an empty physical destination");
        foreach (var record in records.Where(record => record.GetProperty("Label").GetString()!.EndsWith("horde-merge")))
        {
            var before = record.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var after = record.GetProperty("After").Deserialize<TrainCombatState>()!;
            int id = record.GetProperty("Targets")[0].GetInt32();
            var removed = record.GetProperty("TargetsAfter").Deserialize<CombatUnit[]>()!.Single();
            Require(before.Rooms.Single(room => room.Units.Any(unit => unit.Id == id)).RoomIndex !=
                after.Rooms.Single(room => room.Units.Any(unit => unit.Team == removed.Team && unit.Status("horde") != null)).RoomIndex &&
                removed.Health > 0 && removed.DeathState!.IsDespawned == true && removed.DeathState.IsDestroyed == true &&
                !removed.DeathState.HasFinishedDying, "Bump merge did not remove a positive-HP actor across different rooms.");
            Require(!record.GetProperty("Dispatched").EnumerateArray().Any(callback =>
                callback.GetProperty("Kind").GetString() is "OnSpawn" or "OnUnscaledSpawn" or "OnSpawnNotFromCard" or "CardMonsterPlayed" or "OnDeath"),
                "Paid Bump merge fired birth, Rally or death callbacks.");
        }
        var relentless = records.Single(record => record.GetProperty("Label").GetString() == "enemy-relentless-up-destroys-room");
        TrainCombatState relentlessBefore = relentless.GetProperty("Before").Deserialize<TrainCombatState>()!;
        TrainCombatState relentlessAfter = relentless.GetProperty("After").Deserialize<TrainCombatState>()!;
        int relentlessTarget = relentless.GetProperty("Targets").Deserialize<int[]>()!.Single();
        int sourceRoom = relentlessBefore.Context!.SpawnPoints!.Units.Single(unit => unit.UnitId == relentlessTarget).Current!.RoomIndex;
        int destinationRoom = relentlessAfter.Context!.SpawnPoints!.Units.Single(unit => unit.UnitId == relentlessTarget).Current!.RoomIndex;
        Require(destinationRoom > sourceRoom && relentless.GetProperty("RoomEnabledAfter")[sourceRoom].GetBoolean() == false,
            "Native Relentless Bump did not disable every crossed room.");
        if (records.Any(record => record.GetProperty("Label").GetString() == pairedRelentlessLabel))
        {
            var paired = records.Single(record => record.GetProperty("Label").GetString() == pairedRelentlessLabel);
            TrainCombatState pairedBefore = paired.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState pairedAfter = paired.GetProperty("After").Deserialize<TrainCombatState>()!;
            int pairedTarget = paired.GetProperty("Targets").Deserialize<int[]>()!.Single();
            int[] companionIds = pairedBefore.Movement.Where(rule => rule.CompanionBoss).Select(rule => rule.UnitId).ToArray();
            int pairedVictimId = companionIds.Single(id => id != pairedTarget);
            int pairedFloor = pairedBefore.Context!.SpawnPoints!.Units.Single(unit => unit.UnitId == pairedTarget).Current!.RoomIndex;
            Require(companionIds.Length == 2 && pairedBefore.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == pairedTarget && unit.Status("relentless") != null) &&
                pairedBefore.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == pairedVictimId) &&
                pairedAfter.Context!.SpawnPoints!.Units.Single(unit => unit.UnitId == pairedTarget).Current!.RoomIndex == pairedFloor &&
                pairedAfter.Context.SpawnPoints.Units.Single(unit => unit.UnitId == pairedVictimId).Current!.RoomIndex == pairedFloor &&
                paired.GetProperty("RoomEnabledAfter").EnumerateArray().All(enabled => enabled.GetBoolean()) &&
                paired.GetProperty("Dispatched").EnumerateArray().Any(callback => callback.GetProperty("ActorId").GetInt32() == pairedTarget &&
                    callback.GetProperty("Kind").GetString() == "PostAttemptedAscension"),
                "Native Relentless Bump did not preserve a paired companion and dispatch the attempted-movement callback.");
        }
        if (hasPairedOuterBoss)
            VerifyPairedOuterBossMovement(records.Single(record => record.GetProperty("Label").GetString() == pairedOuterBossLabel));
        Console.WriteLine($"NATIVE-BUMP-CHECKS PASS: {records.Length} real paid-card effect states including unpaired Relentless room destruction; paired and outer Boss movement when recorded; plus synthetic ordinary/outer-Boss branches, exact dispatch/retained targets, signed/clamped/fixed-range movement, rooting/immobility, looping, simultaneous targets, both-team cross-room Horde removal, Pyre/full/partially blocked rooms and 32 branches.");
    }

    private static void VerifyOrdinaryCompanionMovement(FixtureValue record)
    {
        TrainCombatState source = record.GetProperty("Before").Deserialize<TrainCombatState>()!;
        int id = record.GetProperty("Targets").Deserialize<int[]>()!.Single();
        CombatUnit original = source.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
        var companion = new CombatUnit(original.Id, original.AssetKey, original.Team, original.BaseAttack, original.Health,
            original.MaxHealth, original.CanAttack, original.IsPyre, original.EndsBattleOnDeath, original.Statuses,
            original.Triggers, original.SpawnerCardId, original.Size, original.StatusImmunities, original.Subtypes,
            original.Modifiers, isBoss: true, lastAttackerId: original.LastAttackerId, statusRegistry: original.StatusRegistry,
            equipmentCards: original.EquipmentCards, nextTriggerId: original.NextTriggerId, ability: original.Ability,
            statusDictionary: original.StatusDictionary, abilityRules: original.AbilityRules, hordeDefinition: original.HordeDefinition,
            isSpawning: original.IsSpawning, sacrificeCardId: original.SacrificeCardId, deathState: original.DeathState,
            bumpRules: new(false, true, false));
        RoomCombatState[] rooms = source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
            room.Units.Select(unit => unit.Id == id ? companion : unit).ToArray(), room.ExternalInteractions, source.Context, room.Preview)).ToArray();
        EnemyMovement[] movement = source.Movement.Select(rule => rule.UnitId == id
            ? new EnemyMovement(rule.UnitId, rule.Speed, rule.Ascends, rule.Loops, companionBoss: true) : rule).ToArray();
        var companionState = new TrainCombatState(rooms, movement, source.EnemySlotsPerRoom, source.Context);
        BumpResult result = BumpModel.Apply(companionState, [id], 1,
            record.GetProperty("Rooms").Deserialize<RoomPlayRule[]>()!);
        int oldRoom = source.Context!.SpawnPoints!.Units.Single(unit => unit.UnitId == id).Current!.RoomIndex;
        Require(result.Supported && result.State!.Context!.SpawnPoints!.Units.Single(unit => unit.UnitId == id).Current!.RoomIndex == oldRoom + 1,
            "A non-relentless companion Boss failed to use ordinary native Bump movement: " + result.UnsupportedReason);
    }

    private static void VerifyOuterBossMovement(FixtureValue record)
    {
        TrainCombatState source = record.GetProperty("Before").Deserialize<TrainCombatState>()!;
        int id = record.GetProperty("Targets").Deserialize<int[]>()!.Single();
        CombatUnit original = source.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
        CombatUnit boss = new CombatUnit(original.Id, original.AssetKey, original.Team, original.BaseAttack, original.Health,
            original.MaxHealth, original.CanAttack, original.IsPyre, original.EndsBattleOnDeath, original.Statuses,
            original.Triggers, original.SpawnerCardId, original.Size, original.StatusImmunities, original.Subtypes,
            original.Modifiers, isBoss: true, lastAttackerId: original.LastAttackerId, statusRegistry: original.StatusRegistry,
            equipmentCards: original.EquipmentCards, nextTriggerId: original.NextTriggerId, ability: original.Ability,
            statusDictionary: original.StatusDictionary, abilityRules: original.AbilityRules, hordeDefinition: original.HordeDefinition,
            isSpawning: original.IsSpawning, sacrificeCardId: original.SacrificeCardId, deathState: original.DeathState,
            bumpRules: new UnitBumpRules(false, false, true));
        BattleSpawnPoints beforePoints = source.Context!.SpawnPoints!;
        SpawnPointReference oldPoint = beforePoints.Units.Single(unit => unit.UnitId == id).Current!;
        int oldRoom = oldPoint.RoomIndex;
        SpawnPointGroupState[] groups = beforePoints.Groups.Select(group => group.RoomIndex == oldPoint.RoomIndex && group.Team == CombatTeam.Enemy
            ? new SpawnPointGroupState(group.RoomIndex, group.Team, group.InnerCount, group.GroupCount,
                group.Occupants.Select(occupant => occupant == id ? 0 : occupant).ToArray(), group.Outside, group.PreviewCopyId)
            : group).ToArray();
        UnitSpawnPointState[] points = beforePoints.Units.Select(unit => unit.UnitId == id
            ? new UnitSpawnPointState(id, new SpawnPointReference(oldRoom, CombatTeam.Enemy, 0, outerBossAnchor: true),
                unit.LastKnown, outerBoss: true, unit.SpawnedInPreview)
            : unit).ToArray();
        BattleSpawnPoints outerPoints = new BattleSpawnPoints(groups, points, beforePoints.NextPreviewCopyId);
        CombatContext context = source.Context.WithSpawnPoints(outerPoints);
        RoomCombatState[] rooms = source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
            room.Units.Select(unit => unit.Id == id ? boss : unit).ToArray(), room.ExternalInteractions, context, room.Preview)).ToArray();
        TrainCombatState outerState = CardSpellModel.WithContext(new TrainCombatState(rooms,
            source.Movement.Where(rule => rule.UnitId != id).ToArray(), source.EnemySlotsPerRoom, context), context);
        BumpResult result = BumpModel.Apply(outerState, [id], 1, record.GetProperty("Rooms").Deserialize<RoomPlayRule[]>()!);
        Require(result.Supported, "Outer Boss anchor movement was rejected: " + result.UnsupportedReason);
        BattleSpawnPoints afterPoints = result.State!.Context!.SpawnPoints!;
        Require(afterPoints.Units.Single(unit => unit.UnitId == id).Current is SpawnPointReference current &&
            current.OuterBossAnchor && current.RoomIndex == oldRoom + 1 &&
            afterPoints.Groups.Where(group => group.Team == CombatTeam.Enemy).All(group =>
                group.Occupants.SequenceEqual(outerPoints.Groups.Single(before => before.RoomIndex == group.RoomIndex &&
                    before.Team == group.Team && before.PreviewCopyId == group.PreviewCopyId).Occupants)),
            "Outer Boss movement occupied or shifted ordinary enemy points.");
        var callbacks = result.Dispatched.Where(callback => !callback.Kind.StartsWith("#PhysicalHarvest:", StringComparison.Ordinal)).ToArray();
        Require(callbacks.Length >= 2 && callbacks[0].Unit.Id == id && callbacks[0].Kind == "PostAscension" &&
            callbacks[1].Unit.Id == id && callbacks[1].Kind == "OnShift",
            "Outer Boss ForceMoveBoss callbacks did not begin with PostAscension then Shift.");
    }

    private static void VerifyPairedOuterBossMovement(FixtureValue record)
    {
        TrainCombatState before = record.GetProperty("Before").Deserialize<TrainCombatState>()!;
        TrainCombatState after = record.GetProperty("After").Deserialize<TrainCombatState>()!;
        int outerId = record.GetProperty("Targets").Deserialize<int[]>()!.Single();
        CombatUnit outerBoss = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == outerId);
        int pairedId = outerBoss.BumpRules?.PairedBossUnitId ?? 0;
        CombatUnit? pairedBoss = before.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.Id == pairedId);
        SpawnPointReference? outerBefore = before.Context!.SpawnPoints!.Units.Single(unit => unit.UnitId == outerId).Current;
        SpawnPointReference? pairedBefore = before.Context.SpawnPoints.Units.Single(unit => unit.UnitId == pairedId).Current;
        SpawnPointReference? outerAfter = after.Context!.SpawnPoints!.Units.Single(unit => unit.UnitId == outerId).Current;
        SpawnPointReference? pairedAfter = after.Context.SpawnPoints.Units.Single(unit => unit.UnitId == pairedId).Current;
        Require(pairedId != 0 && pairedBoss?.BumpRules?.CompanionBoss == true && pairedBoss.Status("relentless") == null &&
            outerBoss.Status("relentless") != null && outerBefore?.RoomIndex == pairedBefore?.RoomIndex &&
            outerAfter?.RoomIndex != outerBefore?.RoomIndex && pairedAfter?.RoomIndex != pairedBefore?.RoomIndex &&
            outerAfter?.RoomIndex == pairedAfter?.RoomIndex,
            "A paired Relentless Outer Boss Bump did not move both boss anchors to the same destination.");

        var callbacks = record.GetProperty("Dispatched").EnumerateArray().ToArray();
        int Index(int actorId, params string[] kinds) => Array.FindIndex(callbacks, callback =>
            callback.GetProperty("ActorId").GetInt32() == actorId && kinds.Contains(callback.GetProperty("Kind").GetString()));
        int pairedPost = Index(pairedId, "PostAscension", "PostDescension");
        int pairedShift = Index(pairedId, "OnShift");
        int outerPost = Index(outerId, "PostAscension", "PostDescension");
        int outerShift = Index(outerId, "OnShift");
        Require(pairedPost >= 0 && pairedShift > pairedPost && outerPost > pairedShift && outerShift > outerPost,
            "Paired Boss force-move callbacks did not run before the Outer Boss callbacks.");
    }

    private static void Verify(FixtureValue record)
    {
        string label = record.GetProperty("Label").GetString()!;
        var before = record.GetProperty("Before").Deserialize<TrainCombatState>()!;
        string parent = JsonSerializer.Serialize(before);
        var result = BumpModel.Apply(before, record.GetProperty("Targets").Deserialize<int[]>()!, record.GetProperty("Amount").GetInt32(),
            record.GetProperty("Rooms").Deserialize<RoomPlayRule[]>()!, record.GetProperty("SourceCardId").GetInt32());
        Require(result.Supported, label + " unsupported: " + result.UnsupportedReason);
        Compare(result.State, record.GetProperty("After").Deserialize<TrainCombatState>(), label + " effect return");
        Compare(result.RoomRules.Select(room => room.Enabled).ToArray(), record.GetProperty("RoomEnabledAfter").Deserialize<bool[]>()!,
            label + " room enabled state");
        var targets = record.GetProperty("Targets").Deserialize<int[]>()!;
        var retained = targets.Select(id => result.State!.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == id)
            ?? result.RetainedUnits.FirstOrDefault(unit => unit.Id == id)).ToArray();
        Compare(retained, record.GetProperty("TargetsAfter").Deserialize<CombatUnit[]>(), label + " retained targets");
        // Physical harvest phases are global combat callbacks, not CharacterState.FireTriggers observations.
        var callbacks = result.Dispatched.Where(callback => !callback.Kind.StartsWith("#PhysicalHarvest:", StringComparison.Ordinal))
            .Select(callback => new CallbackSnapshot(callback.Unit.Id, callback.Kind,
            callback.ParamInt, callback.ParamInt2, callback.ParamString, callback.TriggerCount,
            callback.DyingCharacter?.Id ?? 0, callback.OverrideTarget?.Id ?? 0, callback.LastSpawnedOverrideUnitId)).ToArray();
        Compare(callbacks, record.GetProperty("Dispatched").Deserialize<CallbackSnapshot[]>()!, label + " dispatch order");
        Require(result.PendingCallbacks.Count == 0 && record.GetProperty("QueueAfter").GetInt32() == 0, label + " left pending callbacks");
        Require(JsonSerializer.Serialize(before) == parent, label + " mutated its parent");
    }
    private static void Compare<T>(T predicted, T actual, string label)
    {
        string? error = ModelJson.Difference(JsonSerializer.Serialize(predicted), JsonSerializer.Serialize(actual));
        if (error != null && predicted is CallbackSnapshot[] predictedCallbacks && actual is CallbackSnapshot[] actualCallbacks)
        {
            string Format(IEnumerable<CallbackSnapshot> callbacks) => string.Join(",", callbacks.Select(callback =>
                callback.ActorId + ":" + callback.Kind + "(" + callback.ParamInt + "," + callback.ParamInt2 + "," +
                callback.ParamString + ",dying=" + callback.DyingId + ",target=" + callback.OverrideTargetId + ")"));
            error += "; predicted=[" + Format(predictedCallbacks) + "]; native=[" + Format(actualCallbacks) + "]";
        }
        Require(error == null, label + ": " + error);
    }
    private static void Require(bool okay, string message) { if (!okay) throw new Exception(message); }
}
