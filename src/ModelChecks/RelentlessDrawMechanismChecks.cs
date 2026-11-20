using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelentlessDrawMechanismChecks
{
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
    internal static void Batch(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Scope").GetString() == "DefinitionDrivenRelentlessDraw" && root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d", "Wrong native Relentless draw scope/version.");
        foreach (var pair in new[] { ("Before", "After"), ("Original", "OriginalAfter"), ("RngBefore", "RngAfter"), ("FrameBefore", "FrameAfter"), ("QueueBefore", "QueueAfter") })
            Require(root.GetProperty(pair.Item1).ContentEquals(root.GetProperty(pair.Item2)), "Relentless draw batch changed " + pair.Item1);
        var definitions = root.GetProperty("Definitions").EnumerateArray().ToDictionary(row => row.GetProperty("Id").GetString()!, row => row.GetProperty("Rule").Deserialize<RelicResourceEffect>()!);
        var samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(root.GetProperty("Original").GetArrayLength() == 2 && definitions.Count == 10 && samples.Length == 640 && root.GetProperty("QueueBefore").GetInt32() == 0,
            "Missing original Relentless draw sources or parameter matrix.");
        void Verify()
        {
            foreach (var row in samples)
            {
                int mask = row.GetProperty("Mask").GetInt32() & 7;
                int queries = mask == 0 ? 3 : Enumerable.Range(0, 3).First(index => (mask & (1 << index)) != 0) + 1;
                float amount = mask == 0 ? 0 : definitions[row.GetProperty("Id").GetString()!].Amount;
                Require(row.GetProperty("Value").GetSingle() == amount && row.GetProperty("Queries").GetInt32() == queries &&
                    !row.GetProperty("Returned").GetBoolean() && row.GetProperty("Signals").GetInt32() == 0,
                    "Native Relentless draw cache, short circuit, false Test or no-signal semantics differ.");
            }
        }
        Verify(); Parallel.For(0, 16, _ => Verify());
        var initial = root.GetProperty("Before").Deserialize<BattleTurnState>()!; var source = initial.Spawn.Train.Context!;
        Require(source.Resources!.BaseDraw == 5 && source.Resources.MaxHandSize == 10, "Native numeric query basis changed.");
        string parent = JsonSerializer.Serialize(initial, ModelJson.Options); int transitions = 0;
        foreach (var row in samples)
        {
            var raw = definitions[row.GetProperty("Id").GetString()!];
            var rule = new RelicResourceEffect(0, raw.Type, raw.ParamInt, raw.ParamInt2, raw.Amount, false,
                drawValue: row.GetProperty("Stale").GetSingle(), drawMutationOrder: 0);
            var owner = new CombatRelicState("arbitrary-id", "arbitrary-key", new[] { rule.Type }, isCovenant: false, disallowedInPlacementPhase: false, resourceEffects: new[] { rule });
            var context = source.WithRelics(new[] { owner });
            var world = World(initial.Spawn.Train, context, row.GetProperty("Mask").GetInt32());
            var before = RelicResourceModel.GetDrawCount(context);
            CompareQuery(before, row, "Before");
            var result = RelicRelentlessDrawModel.RefreshOne(world, 0, 0);
            Require(result.Supported, result.UnsupportedReason ?? "Relentless draw immutable callback failed.");
            Require(result.Events.Count == 0 && result.Context!.Relics!.Single().ResourceEffects!.Single().DrawValue == row.GetProperty("Value").GetSingle(),
                "Relentless draw update lost native float cache or incorrectly notified a trigger.");
            CompareQuery(RelicResourceModel.GetDrawCount(result.Context!), row, "After"); transitions++;
        }
        var control = new RelicResourceEffect(0, RelicRelentlessDrawModel.Effect, 2, 0, 2, false, drawValue: 2, drawMutationOrder: 0);
        var controller = new CombatRelicState("gates", "gates", new[] { control.Type }, isCovenant: false, disallowedInPlacementPhase: false, resourceEffects: new[] { control });
        var controlContext = source.WithRelics(new[] { controller });
        foreach (CombatTeam team in new[] { CombatTeam.Enemy, CombatTeam.Player })
        foreach (string state in new[] { "live", "dead", "despawned", "spawning" })
            Require(RelicRelentlessDrawModel.Query(World(initial.Spawn.Train, controlContext, 1, team, state)).Value == 1,
                "Relentless draw incorrectly filtered team/death/spawn lifecycle.");
        Require(RelicRelentlessDrawModel.Query(World(initial.Spawn.Train, controlContext, 8)).Value == 0 &&
            RelicRelentlessDrawModel.Query(World(initial.Spawn.Train, controlContext, 7, state: "removed")).Value == 0,
            "Pyre or physically removed Relentless actor affected draw.");
        foreach (var gate in new[] { (false, false, false, 0), (false, true, false, 2), (false, false, true, 2), (true, false, true, 0) })
        {
            var result = RelicRelentlessDrawModel.RefreshOne(World(initial.Spawn.Train, controlContext, 0), 0, 0, placement: gate.Item1, preview: gate.Item2, startOfCombat: gate.Item3);
            Require(result.Supported && result.Context!.Relics!.Single().ResourceEffects!.Single().DrawValue == gate.Item4,
                "Relentless draw manager start/placement/preview gates differ.");
        }
        Require(RelicRelentlessDrawModel.Query(World(initial.Spawn.Train, controlContext, 1, state: "upper")).Value == 1 &&
            RelicRelentlessDrawModel.Query(World(initial.Spawn.Train, controlContext, 1, state: "case-first-zero")).Value == 0,
            "Native HasStatusEffect uses the first case-insensitive registry match.");
        var health = new RelicResourceEffect(1, RelicTurnDrawModel.FullHealth, 1, 0, 1, false, drawValue: 0, drawMutationOrder: 1);
        var mixedOwner = new CombatRelicState("mixed", "mixed", new[] { control.Type, health.Type }, isCovenant: false,
            disallowedInPlacementPhase: false, resourceEffects: new[] { control.WithDrawValue(0), health });
        var mixedWorld = World(initial.Spawn.Train, source.WithRelics(new[] { mixedOwner }), 1);
        var maximum = RelicResourceModel.GetPyreMaxHealth(mixedWorld.Context!);
        var mixed = RelicTurnDrawModel.BeforeDraw(mixedWorld.Context!, maximum.Value, train: mixedWorld);
        Require(mixed.Supported && RelicResourceModel.GetDrawCount(mixed.Context!).Value == 8,
            "Ordered before-draw consumers lost Relentless or full-health value.");
        var resetWorld = World(initial.Spawn.Train, mixed.Context!, 0);
        var reset = RelicTurnDrawModel.BeforeDraw(resetWorld.Context!, maximum.Value, train: resetWorld);
        Require(reset.Supported && RelicResourceModel.GetDrawCount(reset.Context!).Value == 6,
            "Relentless reset incorrectly cleared the full-health draw mutation.");
        var laterRoom = new CombatRelicState("later-room", "later-room", new[] { RelicResourceModel.StartTurnRoomEffect });
        Require(RelicRelentlessDrawModel.ValidateBeforeDrawOrder(controlContext.WithRelics(new[] { controller, laterRoom })) != null &&
            RelicRelentlessDrawModel.ValidateBeforeDrawOrder(controlContext.WithRelics(new[] { laterRoom, controller })) == null,
            "Known later before-draw mutations must not be silently moved before the cached query.");
        Require(parent == JsonSerializer.Serialize(initial, ModelJson.Options), "Relentless draw modified parent state.");
        Console.WriteLine($"RELENTLESS-DRAW-BATCH PASS originals=2 definitions=10 callbacks={samples.Length} immutableTransitions={transitions} parallel=16 nativeSeconds={root.GetProperty("Seconds").GetDouble():F3}; timing/cache, signed float rounding, native mutable-rule queries and physical lifecycle controls.");
    }
    private static void CompareQuery(RelicResourceResult result, FixtureValue row, string suffix)
    {
        bool error = row.GetProperty("Error" + suffix).ValueKind != FixtureKind.Null;
        Require(error ? !result.Supported : result.Supported && result.Value == row.GetProperty("Query" + suffix).GetInt32(),
            "Relentless draw native signed float/round/clamp query differs: " + suffix);
    }
    private static TrainCombatState World(TrainCombatState initial, CombatContext context, int mask, CombatTeam team = CombatTeam.Enemy, string state = "live")
    {
        var rooms = new List<RoomCombatState>(); var groups = new List<SpawnPointGroupState>(); var references = new List<UnitSpawnPointState>();
        foreach (var metadata in context.RoomCapacities!.OrderBy(room => room.RoomIndex))
        {
            int id = 910000 + metadata.RoomIndex;
            var statuses = new[] { new CombatStatus("relentless", (mask & (1 << metadata.RoomIndex)) != 0 ? 1 : 0) };
            if (state == "upper") statuses = new[] { new CombatStatus("RELENTLESS", statuses[0].Stacks) };
            if (state == "case-first-zero") statuses = new[] { new CombatStatus("RELENTLESS", 0), statuses[0] };
            var unit = new CombatUnit(id, "physical-relentless-control", team, 0, state == "dead" ? 0 : 10, 10,
                false, false, false, statuses, isSpawning: state == "spawning", statusRegistry: statuses);
            if (state == "despawned") unit = unit.WithDeathState(new UnitDeathState(false, false, false, isDespawned: true));
            rooms.Add(new RoomCombatState(metadata.RoomIndex, false, new[] { unit }, Array.Empty<string>(), context));
            foreach (var groupTeam in new[] { CombatTeam.Enemy, CombatTeam.Player })
                groups.Add(new SpawnPointGroupState(metadata.RoomIndex, groupTeam, 1, 1, new[] { team == groupTeam && state != "removed" ? id : 0 }, new[] { false }));
            var point = new SpawnPointReference(metadata.RoomIndex, team, 0); references.Add(new UnitSpawnPointState(id, point, point));
        }
        context = context.WithSpawnPoints(new BattleSpawnPoints(groups, references));
        return new TrainCombatState(rooms, initial.Movement, initial.EnemySlotsPerRoom, context);
    }
    internal static void Battle(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("RelentlessDrawVariant").GetBoolean() && new[] { "WarborneT2", "WarborneT3" }.Contains(root.GetProperty("OccupiedFloorDrawDefinition").GetProperty("Key").GetString()),
            "Missing unchanged original Warborne T2/T3 integration.");
        var tests = root.GetProperty("RelentlessDrawTests").EnumerateArray().ToArray();
        Require(tests.Length > 1 && tests.All(row => !row.GetProperty("Returned").GetBoolean()) &&
            tests.Any(row => row.GetProperty("After").GetSingle() > 0) &&
            tests.Any(row => row.GetProperty("Before").GetSingle() > 0 && row.GetProperty("After").GetSingle() == 0),
            "Missing actual native positive and reset Relentless draw Test.");
        if (root.GetProperty("OccupiedFloorDrawDefinition").GetProperty("Key").GetString() == "WarborneT3")
        {
            var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
            Require(actions.Length == 10 && actions.All(action => action.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32() == 1),
                "WarborneT3 must execute the original ten-card condition in one turn.");
            var before = actions[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = actions[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var old = before.Spawn.Train.Context!.Cards.Hand.Select(card => card.InstanceId).ToHashSet();
            Require(after.Spawn.Train.Context!.Cards.Hand.Count(card => !old.Contains(card.InstanceId)) == 2,
                "Original WarborneT3 did not actually draw two cards on its tenth play.");
        }
        OccupiedFloorDrawMechanismChecks.Battle(path);
        Console.WriteLine("RELENTLESS-DRAW-BATTLE PASS unchanged " + root.GetProperty("OccupiedFloorDrawDefinition").GetProperty("Key").GetString() + "; combined occupied/Relentless caches, positive/reset draw timing and complete native successors/16 branches/frontiers/serial-parallel root search.");
    }
}
