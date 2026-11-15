using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
internal static class TurnEnergyMechanismChecks
{
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
    internal static void Batch(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Scope").GetString() == "DefinitionDrivenTurnEnergy" && root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d", "Wrong native dynamic energy version/scope.");
        foreach (var pair in new[] { ("Before", "After"), ("Original", "OriginalAfter"), ("RngBefore", "RngAfter"), ("FrameBefore", "FrameAfter"), ("QueueBefore", "QueueAfter") })
            Require(root.GetProperty(pair.Item1).ContentEquals(root.GetProperty(pair.Item2)), "Dynamic energy batch changed " + pair.Item1);
        var definitions = root.GetProperty("Definitions").EnumerateArray().ToDictionary(row => row.GetProperty("Id").GetString()!, row => row.GetProperty("Rule").Deserialize<RelicResourceEffect>()!);
        var samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(root.GetProperty("Original").GetArrayLength() == 6 && definitions.Count == 90 && samples.Length == 2643 && root.GetProperty("QueueBefore").GetInt32() == 0,
            "Missing six original sources or dynamic energy parameter/timing matrix.");
        void Verify()
        {
            foreach (var row in samples)
            {
                var effect = definitions[row.GetProperty("Definition").GetString()!]; var rule = effect.TurnEnergy!;
                int tick = row.GetProperty("Turn").GetInt32(), mask = row.GetProperty("Mask").GetInt32();
                int[] sizes = row.GetProperty("Sizes").Deserialize<int[]>()!;
                bool condition = effect.Type == RelicTurnEnergyModel.Reducing || mask == 7;
                int queryBefore = effect.Type == RelicTurnEnergyModel.Reducing ? rule.ReducingAmount(row.GetProperty("CachedBefore").GetInt32()) : rule.OccupancyBonus;
                int queryAfter = effect.Type == RelicTurnEnergyModel.Reducing ? rule.ReducingAmount(tick) : rule.OccupancyBonus;
                bool test = condition && (effect.Type == RelicTurnEnergyModel.Reducing ? queryAfter != 0 : rule.OccupancyBonus != 0);
                Require(row.GetProperty("Condition").GetBoolean() == condition && row.GetProperty("Test").GetBoolean() == test &&
                    row.GetProperty("QueryBefore").GetInt32() == queryBefore && row.GetProperty("QueryAfter").GetInt32() == queryAfter &&
                    row.GetProperty("CachedAfter").GetInt32() == (effect.Type == RelicTurnEnergyModel.Reducing ? tick : 0), "Native query cache/test/conditions differ.");
                int[] requested = RelicTurnEnergyModel.Requests(effect, tick, condition, sizes.Sum());
                bool overflow = requested.Contains(int.MinValue);
                Require((row.GetProperty("Error").ValueKind != FixtureKind.Null) == overflow, "Native Math.Abs overflow control differs.");
                var expected = requested.TakeWhile(amount => amount != int.MinValue).Select(amount => (Kind: amount > 0 ? "Add" : "Remove", Amount: Math.Abs(amount), Suppress: amount > 0));
                var actual = row.GetProperty("Requests").EnumerateArray().Select(request => (Kind: request.GetProperty("Kind").GetString()!, Amount: request.GetProperty("Amount").GetInt32(), Suppress: request.GetProperty("Suppress").GetBoolean()));
                Require(expected.SequenceEqual(actual) && row.GetProperty("Notifications").GetInt32() == (test && !overflow ? 1 : 0), "Native ordered energy/notification requests differ.");
            }
        }
        Verify(); Parallel.For(0, 16, _ => Verify());
        var source = root.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
        string parent = JsonSerializer.Serialize(source, ModelJson.Options); int transitions = 0;
        foreach (var row in samples)
        {
            var original = definitions[row.GetProperty("Definition").GetString()!];
            var rule = original.Type == RelicTurnEnergyModel.Reducing ? original.WithTurnEnergy(original.TurnEnergy!.WithCheckedTurn(row.GetProperty("CachedBefore").GetInt32())) : original;
            var owner = new CombatRelicState("arbitrary-id", "arbitrary-key", new[] { rule.Type }, isCovenant: false, disallowedInPlacementPhase: false, resourceEffects: new[] { rule });
            foreach (int energy in new[] { 0, source.Resources!.MaxEnergy - 1 })
            {
                var context = source.WithRelics(new[] { owner }).WithQueryFrame(source.QueryFrame!.With(energy: energy, turn: row.GetProperty("Turn").GetInt32()));
                var train = OccupancyWorld(context, row.GetProperty("Mask").GetInt32(), row.GetProperty("Sizes").Deserialize<int[]>()!);
                context = train.Context!;
                var beforeQuery = RelicResourceModel.GetEnergyModification(context, train: train);
                Require(beforeQuery.Supported && beforeQuery.Value == Math.Max(0, row.GetProperty("Condition").GetBoolean() ? row.GetProperty("QueryBefore").GetInt32() : 0), "Shared current query used the current turn or threshold bonus.");
                var result = RelicResourceModel.ApplyEnergyTiming(context, train: train);
                if (row.GetProperty("Error").ValueKind != FixtureKind.Null) { Require(!result.Supported, "Native exceptional energy input was silently accepted."); continue; }
                Require(result.Supported, result.UnsupportedReason ?? "Shared turn energy timing failed.");
                int expected = energy;
                foreach (var request in row.GetProperty("Requests").EnumerateArray())
                { int amount = request.GetProperty("Amount").GetInt32(); expected = request.GetProperty("Kind").GetString() == "Add" ? Math.Min(source.Resources.MaxEnergy, unchecked(expected + amount)) : Math.Max(0, unchecked(expected - amount)); }
                Require(result.Value == expected && result.Events.Count == row.GetProperty("Notifications").GetInt32(), "Shared ordered energy mutations/notifications differ.");
                var afterQuery = RelicResourceModel.GetEnergyModification(result.Context!, train: train);
                Require(afterQuery.Supported && afterQuery.Value == Math.Max(0, row.GetProperty("Condition").GetBoolean() ? row.GetProperty("QueryAfter").GetInt32() : 0) &&
                    result.Context!.Relics!.Single().ResourceEffects!.Single().TurnEnergy!.LastCheckedTurn == row.GetProperty("CachedAfter").GetInt32(), "Shared timing lost zero-test cached turn or stale display semantics.");
                transitions++;
            }
        }
        // Legacy zero bonuses skip the damaged-floor world query entirely.
        var zero = new RelicResourceEffect(0, RelicDamagedFloorEnergyModel.Effect, 0, 0, 0, false);
        var zeroOwner = new CombatRelicState("legacy-zero", "legacy-zero", new[] { zero.Type },
            isCovenant: false, disallowedInPlacementPhase: false, resourceEffects: new[] { zero });
        var zeroContext = source.WithRelics(new[] { zeroOwner });
        var zeroResult = RelicResourceModel.ApplyEnergyTiming(zeroContext);
        Require(zeroResult.Supported && zeroResult.Events.Count == 0 &&
            zeroResult.Context!.QueryFrame!.Energy == zeroContext.QueryFrame!.Energy, "Zero legacy energy unexpectedly required a train world.");
        var missing = new CombatRelicState("missing", "missing", new[] { RelicTurnEnergyModel.Reducing });
        Require(RelicModel.Validate(new[] { missing }) != null && parent == JsonSerializer.Serialize(source, ModelJson.Options), "Missing energy rule accepted or shared source changed.");
        Console.WriteLine($"TURN-ENERGY-BATCH PASS originals=6 definitions=90 samples=2643 exceptional=63 immutableTransitions={transitions} parallel=16 nativeSeconds={root.GetProperty("Seconds").GetDouble():F3}; full source/definitions/RNG/frame/queue unchanged.");
    }
    private static TrainCombatState OccupancyWorld(CombatContext context, int mask, int[] sizes)
    {
        var rooms = new List<RoomCombatState>(); var groups = new List<SpawnPointGroupState>(); var locations = new List<UnitSpawnPointState>(); int ordinary = 0;
        foreach (var metadata in context.RoomCapacities!.OrderBy(room => room.RoomIndex))
        {
            int index = ordinary; bool present = !metadata.IsPyre && ((mask >> ordinary++) & 1) != 0;
            var units = new List<CombatUnit>(); var occupants = new List<int>();
            int remaining = present ? sizes[index] : 0;
            if (present)
                do
                {
                    int id = 900000 + metadata.RoomIndex * 100 + units.Count, size = Math.Min(6, remaining);
                    units.Add(new CombatUnit(id, "authored-physical-actor", CombatTeam.Player, 1, size == 0 ? 0 : 10, 10,
                        true, false, false, Array.Empty<CombatStatus>(), size: Math.Max(1, size)));
                    occupants.Add(id); var point = new SpawnPointReference(metadata.RoomIndex, CombatTeam.Player, occupants.Count - 1);
                    locations.Add(new UnitSpawnPointState(id, point, point)); remaining -= size;
                } while (remaining > 0);
            if (occupants.Count == 0) occupants.Add(0);
            rooms.Add(new RoomCombatState(metadata.RoomIndex, false, units, Array.Empty<string>(), context));
            groups.Add(new SpawnPointGroupState(metadata.RoomIndex, CombatTeam.Player, occupants.Count, occupants.Count, occupants, Enumerable.Repeat(false, occupants.Count).ToArray()));
        }
        context = context.WithSpawnPoints(new BattleSpawnPoints(groups, locations));
        return new TrainCombatState(rooms, Array.Empty<EnemyMovement>(), 6, context);
    }
    internal static void Battle(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("TurnEnergyPrepared").GetBoolean() && root.GetProperty("TurnEnergyDefinition").ContentEquals(root.GetProperty("TurnEnergyDefinitionAfter")), "Original energy souls changed.");
        var turns = root.GetProperty("Turns").EnumerateArray().Select(turn => turn.GetProperty("Actual").Deserialize<BattleTurnState>()!).ToArray();
        var played = root.GetProperty("Actions").EnumerateArray().Last().GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(RelicTurnEnergyModel.Occupancy(played.Spawn.Train, out bool occupied, out int size) == null && occupied && size == 20, "Native authored placement did not reach the original size threshold.");
        Require(turns.Any(state => state.Spawn.Train.Context!.Relics!.SelectMany(owner => owner.ResourceEffects ?? Array.Empty<RelicResourceEffect>()).Any(effect =>
            effect.Type == RelicTurnEnergyModel.Reducing && effect.TurnEnergy!.LastCheckedTurn > 0)), "No actual native cached turn transition.");
        Require(turns.Any(state => state.Spawn.Train.Context!.Relics!.SelectMany(owner => owner.ResourceEffects ?? Array.Empty<RelicResourceEffect>()).Any(effect =>
            effect.Type == RelicTurnEnergyModel.Reducing && effect.TurnEnergy!.LastCheckedTurn > 0 && effect.TurnEnergy.ReducingAmount(effect.TurnEnergy.LastCheckedTurn) == 0)),
            "No actual zero-test cached turn update in native battle.");
        CardCycleChecks.Native(root); SolverStageChecks.Native(path);
        Console.WriteLine("TURN-ENERGY-BATTLE PASS original Solaris/Wormgut souls and capacity artifact, size20 threshold, ordered energy/cache, complete successors/16 branches/frontiers/serial-parallel root search.");
    }
}
