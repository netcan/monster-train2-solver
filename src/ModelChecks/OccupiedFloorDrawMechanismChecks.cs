using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OccupiedFloorDrawMechanismChecks
{
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
    internal static void Batch(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Scope").GetString() == "DefinitionDrivenOccupiedFloorDraw" && root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d", "Wrong occupied draw native scope/version.");
        foreach (var pair in new[] { ("Before", "After"), ("Original", "OriginalAfter"), ("RngBefore", "RngAfter"), ("FrameBefore", "FrameAfter"), ("QueueBefore", "QueueAfter") })
            Require(root.GetProperty(pair.Item1).ContentEquals(root.GetProperty(pair.Item2)), "Occupied draw batch changed " + pair.Item1);
        var definitions = root.GetProperty("Definitions").EnumerateArray().ToDictionary(row => row.GetProperty("Id").GetString()!, row => row.GetProperty("Rule").Deserialize<RelicResourceEffect>()!);
        var samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(root.GetProperty("Original").GetArrayLength() == 3 && definitions.Count == 3 && samples.Length == 2304 && root.GetProperty("QueueBefore").GetInt32() == 0,
            "Missing original occupied draw sources or callback matrix.");
        int Expected(FixtureValue row) => row.GetProperty("Callback").GetString() is "EndTurn" or "AfterDraw" ||
            row.GetProperty("Phase").GetString() == "MonsterTurn" && row.GetProperty("Team").GetString() == "Heroes"
            ? Enumerable.Range(0, 3).Count(index => (row.GetProperty("Mask").GetInt32() & (1 << index)) != 0) : row.GetProperty("Stale").GetInt32();
        void Verify()
        {
            foreach (var row in samples)
            {
                bool refresh = row.GetProperty("Callback").GetString() is "EndTurn" or "AfterDraw" ||
                    row.GetProperty("Phase").GetString() == "MonsterTurn" && row.GetProperty("Team").GetString() == "Heroes";
                Require(row.GetProperty("Value").GetSingle() == Expected(row) && row.GetProperty("Signals").GetInt32() == (refresh ? 1 : 0) &&
                    row.GetProperty("Queries").GetInt32() == (refresh ? 3 : 0), "Native occupied draw callback gate/value/signal differs.");
                Require(row.GetProperty("Returned").ValueKind == FixtureKind.Null || !row.GetProperty("Returned").GetBoolean(), "Native occupied draw Test should always return false.");
            }
        }
        Verify(); Parallel.For(0, 16, _ => Verify());
        var initial = root.GetProperty("Before").Deserialize<BattleTurnState>()!; var source = initial.Spawn.Train.Context!;
        string parent = JsonSerializer.Serialize(initial, ModelJson.Options); int transitions = 0;
        foreach (var row in samples)
        {
            var raw = definitions[row.GetProperty("Id").GetString()!];
            var rule = new RelicResourceEffect(0, RelicOccupiedFloorDrawModel.Effect, raw.ParamInt, raw.ParamInt2, 0, false,
                drawValue: row.GetProperty("Stale").GetInt32(), drawMutationOrder: 0, paramMinInt: raw.ParamMinInt);
            var owner = new CombatRelicState("arbitrary-id", "arbitrary-key", new[] { rule.Type }, isCovenant: false, disallowedInPlacementPhase: false, resourceEffects: new[] { rule });
            var context = source.WithRelics(new[] { owner }).WithEnergyState(source.EnergyState!.With(phase: row.GetProperty("Phase").GetString()));
            var world = World(initial.Spawn.Train, context, row.GetProperty("Mask").GetInt32());
            var actor = new CombatUnit(999999, "callback-control", row.GetProperty("Team").GetString() == "Heroes" ? CombatTeam.Enemy : CombatTeam.Player,
                0, 10, 10, false, false, false, Array.Empty<CombatStatus>());
            var result = RelicOccupiedFloorDrawModel.RefreshOne(world, 0, 0, row.GetProperty("Callback").GetString()!, actor);
            Require(result.Supported, result.UnsupportedReason ?? "Occupied draw immutable callback failed.");
            Require(result.Context!.Relics!.Single().ResourceEffects!.Single().DrawValue == Expected(row) && result.Events.Count == 0,
                "Shared occupied draw cached value or trigger notifications differ.");
            var before = RelicResourceModel.GetDrawCount(context); var after = RelicResourceModel.GetDrawCount(result.Context!);
            int Draw(int bonus) => Math.Min(source.Resources!.MaxHandSize, Math.Max(1, source.Resources.BaseDraw + bonus));
            Require(before.Supported && after.Supported && before.Value == Draw(row.GetProperty("Stale").GetInt32()) && after.Value == Draw(Expected(row)),
                "Shared installed draw rule lost stale query or mutable registration.");
            transitions++;
        }
        var controlRule = new RelicResourceEffect(0, RelicOccupiedFloorDrawModel.Effect, 99, -7, 0, false, drawValue: 3, drawMutationOrder: 0);
        var controlOwner = new CombatRelicState("gates", "gates", new[] { controlRule.Type }, isCovenant: false, disallowedInPlacementPhase: true, resourceEffects: new[] { controlRule });
        var controlContext = source.WithRelics(new[] { controlOwner }).WithEnergyState(source.EnergyState!.With(phase: "MonsterTurn"));
        var controlWorld = World(initial.Spawn.Train, controlContext, 0);
        var enemy = new CombatUnit(999999, "callback-control", CombatTeam.Enemy, 0, 10, 10, false, false, false, Array.Empty<CombatStatus>());
        foreach (var gate in new[] { ("Birth", true, false, 0), ("Birth", false, true, 3), ("Killed", false, true, 0), ("Killed", true, false, 3), ("EndTurn", true, false, 3), ("AfterDraw", false, true, 3) })
        {
            var result = RelicOccupiedFloorDrawModel.RefreshOne(controlWorld, 0, 0, gate.Item1, enemy, preview: gate.Item2, placement: gate.Item3);
            Require(result.Supported && result.Context!.Relics!.Single().ResourceEffects!.Single().DrawValue == gate.Item4, "Occupied draw preview/placement gates were conflated.");
        }
        var duplicateRule = new RelicResourceEffect(0, RelicOccupiedFloorDrawModel.Effect, 99, -7, 0, false, drawValue: 3, drawMutationOrder: 1);
        var duplicateOwner = new CombatRelicState("gates", "gates", new[] { duplicateRule.Type }, isCovenant: false, disallowedInPlacementPhase: true, resourceEffects: new[] { duplicateRule });
        var duplicateWorld = World(initial.Spawn.Train, controlContext.WithRelics(new[] { controlOwner, duplicateOwner }), 1);
        var duplicates = RelicDeckTurnDrawModel.ApplyAfterDraw(duplicateWorld.Context!, train: duplicateWorld);
        Require(duplicates.Supported && duplicates.Context!.Relics!.All(owner => owner.ResourceEffects!.Single().DrawValue == 1),
            "Duplicate source IDs redirected occupied draw updates to the first owner.");
        foreach (string excluded in new[] { "dead", "despawned", "post-combat-despawn", "silenced-post-combat-despawn" })
        {
            var world = World(initial.Spawn.Train, controlContext, 7, excluded);
            var count = RelicOccupiedFloorDrawModel.Count(world);
            Require(count.Supported && count.Value == 0, "Dead, despawned or post-combat-despawn actors affected occupied draw.");
        }
        Require(RelicOccupiedFloorDrawModel.Count(World(initial.Spawn.Train, controlContext, 7, "later-post-combat-despawn")).Value == 3,
            "Native TryGetCharacterTriggerEffectStates only reads the first matching PostCombat trigger.");
        Require(RelicOccupiedFloorDrawModel.Count(World(initial.Spawn.Train, controlContext, 7)).Value == 3 &&
            parent == JsonSerializer.Serialize(initial, ModelJson.Options), "Occupied draw ignored live floors or mutated parent state.");
        Console.WriteLine($"OCCUPIED-DRAW-BATCH PASS originals=3 callbacks={samples.Length} immutableTransitions={transitions} parallel=16 nativeSeconds={root.GetProperty("Seconds").GetDouble():F3}; cached values/phase/team/signal requests, lifecycle gates and source purity.");
    }
    private static TrainCombatState World(TrainCombatState initial, CombatContext context, int mask, string variant = "live")
    {
        var rooms = new List<RoomCombatState>(); var groups = new List<SpawnPointGroupState>(); var references = new List<UnitSpawnPointState>(); int ordinary = 0;
        foreach (var metadata in context.RoomCapacities!.OrderBy(room => room.RoomIndex))
        {
            bool present = metadata.IsPyre || (mask & (1 << ordinary++)) != 0;
            int id = 900000 + metadata.RoomIndex;
            var statuses = variant.StartsWith("silenced", StringComparison.Ordinal) ? new[] { new CombatStatus("silenced", 1) } : Array.Empty<CombatStatus>();
            var unit = new CombatUnit(id, "authored-physical-enemy", CombatTeam.Enemy, 0, variant == "dead" ? 0 : 10, 10, false, false, false, statuses);
            if (variant == "despawned") unit = unit.WithDeathState(new UnitDeathState(false, false, false, isDespawned: true));
            if (variant.EndsWith("post-combat-despawn", StringComparison.Ordinal)) unit = unit.WithTriggers(new[] { new CombatTrigger("PostCombat", false, false, false, 0,
                new[] { new CombatEffect("CardEffectDespawnCharacter", 0, 1, "", 0, Array.Empty<string>(), false) }) });
            if (variant == "later-post-combat-despawn") unit = unit.WithTriggers(new[] {
                new CombatTrigger("PostCombat", false, false, false, 0, Array.Empty<CombatEffect>()), unit.Triggers.Single() });
            rooms.Add(new RoomCombatState(metadata.RoomIndex, false, present ? new[] { unit } : Array.Empty<CombatUnit>(), Array.Empty<string>(), context));
            groups.Add(new SpawnPointGroupState(metadata.RoomIndex, CombatTeam.Enemy, 1, 1, new[] { present ? id : 0 }, new[] { false }));
            if (present) { var point = new SpawnPointReference(metadata.RoomIndex, CombatTeam.Enemy, 0); references.Add(new UnitSpawnPointState(id, point, point)); }
        }
        context = context.WithSpawnPoints(new BattleSpawnPoints(groups, references));
        return new TrainCombatState(rooms, initial.Movement, initial.EnemySlotsPerRoom, context);
    }
    internal static void Battle(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("OccupiedFloorDrawPrepared").GetBoolean() && root.GetProperty("OccupiedFloorDrawDefinition").ContentEquals(root.GetProperty("OccupiedFloorDrawDefinitionAfter")),
            "Original occupied draw soul changed.");
        var callbacks = root.GetProperty("OccupiedFloorDrawCallbacks").EnumerateArray().ToArray();
        Require(callbacks.Any(row => row.GetProperty("Kind").GetString() == "OnCharacterKilled" && row.GetProperty("Phase").GetString() == "MonsterTurn" &&
            row.GetProperty("Before").GetSingle() > row.GetProperty("After").GetSingle()), "No actual player-phase kill refreshed the native draw cache.");
        Require(root.GetProperty("Turns").EnumerateArray().Any(turn => turn.GetProperty("Actual").Deserialize<BattleTurnState>()!.DrawPerTurn >
            turn.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.Resources!.BaseDraw), "No actual positive occupied-floor draw value.");
        CardCycleChecks.Native(root); SolverStageChecks.Native(path);
        Console.WriteLine("OCCUPIED-DRAW-BATTLE PASS original Warborne soul; player-phase kill cache, actual draws, complete successors/16 branches/frontiers/serial-parallel root search.");
    }
}
