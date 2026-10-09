using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HordeMergeChecks
{
    internal static void Run()
    {
        CombatStatus horde = new("horde", 2, stackable: true, hidden: false, displayCategory: "Persistent");
        CombatStatus immune = new("immune", 1, stackable: false, hidden: false, displayCategory: "Persistent");
        CombatUnit Unit(int id, CombatTeam team, int count, int attack, int hp) => new(id, "different-" + id, team,
            attack * count, hp * count, hp * count, true, false, false, [horde.WithStacks(count)],
            modifiers: new(attack * count, 0, 0, 1, 0, true, false, []), hordeDefinition: new(attack, hp),
            deathState: new(false, false, true, isSacrifice: false, statisticsListenerOnce: true, isDespawned: false, isDestroyed: false));
        var units = new[] { Unit(3, CombatTeam.Enemy, 3, 3, 7), Unit(1, CombatTeam.Player, 2, 8, 25), Unit(2, CombatTeam.Player, 3, 5, 19) };
        var points = new BattleSpawnPoints([
            new(0, CombatTeam.Enemy, 5, 5, [3, 0, 0, 0, 0], [false, false, false, false, false]),
            new(0, CombatTeam.Player, 5, 5, [1, 2, 0, 0, 0], [false, false, false, false, false])],
            units.Select(unit => { var point = new SpawnPointReference(0, unit.Team, unit.Id == 2 ? 1 : 0);
                return new UnitSpawnPointState(unit.Id, point, point); }).ToArray());
        var rng = UnityRng.Seed(271);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10, [horde, immune],
            BattleStatistics.Empty(), nextUnitId: 4, spawnPoints: points);
        var root = new RoomCombatState(0, false, units, [], context);
        string parent = Serialize(root);
        var clone = HordeMergeModel.Clone(root, 2);
        Require(clone.Supported && clone.SourceAfter!.Health == 76 && clone.SourceAfter.Status("horde")!.Stacks == 4 &&
            clone.Result.State!.Units.Count == 3 && clone.Result.State.Context!.NextUnitId == 4 &&
            Serialize(clone.Result.State.Context.SpawnPoints) == Serialize(points) &&
            Serialize(clone.Result.State.Context.BattleRng) == Serialize(rng), "Horde clone allocated a birth, consumed RNG or used another definition's stats.");
        var merged = HordeMergeModel.Merge(root, 1, 2);
        Require(merged.Supported && merged.Result.State!.Units.Single(unit => unit.Id == 2).Health == 95 &&
            merged.SourceAfter!.Health == 50 && merged.SourceAfter.DeathState!.IsDespawned == true &&
            merged.SourceAfter.DeathState.IsDestroyed == true && !merged.SourceAfter.DeathState.HasFinishedDying &&
            !merged.SourceAfter.DeathState.IsBeingRemoved && merged.SourceAfter.DeathState.HasStatisticsListener &&
            merged.Result.State.Context!.Statistics!.MonstersDeadThisBattle == 0 &&
            merged.Result.PendingCallbacks.All(callback => callback.Kind != "OnDeath" && !HarvestModel.Kinds.Contains(callback.Kind)),
            "Merge substituted death, changed source HP, lost its listener or used incoming troop stats.");
        var bump = HordeMergeModel.Merge(root, 1, 2, true);
        Require(bump.Supported && bump.Result.PendingCallbacks.Select(item => item.Kind).SequenceEqual(["OnStatusEffectChanged", "OnTroopAdded"]),
            "Bump merge did not suppress re-spawn and Rally callbacks.");
        Require(HordeMergeModel.Select(root, units[1]) == 2 && HordeMergeModel.Select(root, units[0]) == 0 &&
            HordeMergeModel.Select(null, units[1]) == 0, "Selection crossed teams, selected self or required matching definitions.");
        var self = HordeMergeModel.Merge(root, 3, 3);
        Require(self.Supported && self.SourceAfter!.Health == 42 && self.SourceAfter.Status("horde")!.Stacks == 6 &&
            self.Result.State!.Units.All(unit => unit.Id != 3), "Direct API invented a self-merge guard.");
        var preview = HordeMergeModel.Merge(new(0, false, units, [], context, preview: true), 1, 2, true);
        Require(preview.Supported && preview.SourceAfter!.DeathState!.IsDespawned == true &&
            preview.SourceAfter.DeathState.IsDestroyed == false && preview.SourceAfter.Health == 50 &&
            Serialize(preview.Result.State!.Context!.SpawnPoints) == Serialize(points),
            "Preview merge destroyed its source or removed primary physical positions.");
        var legacy = new CombatUnit(1, "legacy", CombatTeam.Player, 16, 50, 50, true, false, false, [horde],
            modifiers: units[1].Modifiers, hordeDefinition: units[1].HordeDefinition, deathState: new(false, false, true));
        var missingLifecycle = new RoomCombatState(0, false, units.Select(unit => unit.Id == 1 ? legacy : unit).ToArray(), [], context);
        Require(!HordeMergeModel.Merge(missingLifecycle, 1, 2).Supported,
            "Merge silently inferred missing destruction/despawn metadata from positive HP.");
        var immuneRoom = new RoomCombatState(0, false, units.Select(unit => unit.Id == 2
            ? CardSpellModel.Copy(unit, unit.Health, unit.Statuses.Concat([immune]).ToArray()) : unit).ToArray(), [], context);
        var blocked = HordeMergeModel.Merge(immuneRoom, 1, 2);
        Require(blocked.Supported && blocked.Result.PendingCallbacks.Count == 0 && blocked.SourceAfter!.DeathState!.IsDestroyed == true &&
            blocked.Result.State!.Units.Single(unit => unit.Id == 2).Health == 57, "Immune recipient prevented source removal or gained stats.");
        Require(HordeMergeModel.Merge(root, 0, 2).Result.State == root && HordeMergeModel.Merge(root, 1, 0).Result.State == root &&
            HordeMergeModel.Clone(root, 0).Result.State == root, "Null native gates changed state.");
        string expected = Serialize(merged.Result.State);
        Parallel.For(0, 32, _ => Require(Serialize(HordeMergeModel.Merge(root, 1, 2).Result.State) == expected,
            "Parallel Horde merge changed its result."));
        Require(Serialize(root) == parent, "Horde clone/merge mutated its parent.");
        Console.WriteLine("HORDE-MERGE-CHECKS PASS: clone without birth/RNG, recipient definition, non-death removal, Bump suppression, self/team/null/immunity gates and 32 branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("HordeMergeOperations", out var records) || records.GetArrayLength() == 0) return;
        var samples = records.EnumerateArray().ToArray();
        string[] labels = ["preview-player-clone", "preview-player-bump-merge-with-equipment",
            "preview-enemy-ordinary-merge", "preview-enemy-self-merge",
            "player-clone-adds-one-without-birth", "player-bump-merge-mixed-equipment",
            "enemy-clone-adds-one-without-birth", "enemy-ordinary-merge-different-definition", "null-source-merge",
            "null-target-merge", "non-horde-source-merge", "non-horde-target-merge",
            "enemy-self-merge-removes-positive-hp-recipient", "cross-team-direct-merge",
            "immune-target-still-removes-source", "immune-clone-no-growth", "null-source-clone"];
        Require(samples.Select(sample => sample.GetProperty("Label").GetString()).SequenceEqual(labels), "Missing native Horde merge boundary cases.");
        var selections = fixture.GetProperty("HordeMergeSelections").EnumerateArray().ToArray();
        Require(selections.Length == 5, "Missing native same-team manager selection cases.");
        void VerifyAll()
        {
            foreach (var sample in samples) Verify(sample);
            foreach (var sample in selections)
            {
                var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
                var source = before.Units.Single(unit => unit.Id == sample.GetProperty("SourceId").GetInt32());
                Require(before.Units.Where(unit => unit.Team == source.Team).Select(unit => unit.Id).SequenceEqual(
                    sample.GetProperty("Candidates").Deserialize<int[]>()!), "Captured room order differs from native manager selection order.");
                int predicted = HordeMergeModel.Select(before, source);
                Require(predicted == sample.GetProperty("TargetId").GetInt32() &&
                    (predicted != 0) == sample.GetProperty("Accepted").GetBoolean(), "Native merge selection differs.");
            }
        }
        VerifyAll(); Parallel.For(0, 32, _ => VerifyAll());
        var equipped = samples.Single(sample => sample.GetProperty("Label").GetString() == "player-bump-merge-mixed-equipment");
        var from = equipped.GetProperty("SourceAfter").Deserialize<CombatUnit>()!;
        var afterGear = equipped.GetProperty("AfterApi").Deserialize<RoomCombatState>()!;
        Require(from.EquipmentCards!.Count == 2 && afterGear.Context!.OtherPiles!.Single(pile => pile.Name == "Standby").Cards
            .Any(card => card.InstanceId == from.SpawnerCardId) && afterGear.Context.Cards.Hand.Any(card => from.EquipmentCards.Contains(card.InstanceId)) &&
            afterGear.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => from.EquipmentCards.Contains(card.InstanceId)),
            "Native merge did not retain the unit card or exercise both equipment destinations.");
        var beforeGear = equipped.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var otherBefore = beforeGear.Context!.SpawnPoints!.Group(0, CombatTeam.Enemy)!;
        var otherAfter = afterGear.Context!.SpawnPoints!.Group(0, CombatTeam.Enemy)!;
        Require(otherBefore.Occupants[4] == 0 && otherBefore.Occupants[6] > 0 && otherAfter.Occupants[4] > 0 &&
            otherAfter.Occupants[6] == 0, "Native player merge did not exercise opposing-team physical centering.");
        var ordinary = samples.Single(sample => sample.GetProperty("Label").GetString() == "enemy-ordinary-merge-different-definition");
        Require(ordinary.GetProperty("Queued").EnumerateArray().Any(item => item.GetProperty("ActorId").GetInt32() ==
            ordinary.GetProperty("SourceId").GetInt32() && item.GetProperty("Kind").GetString() == "CardMonsterPlayed"),
            "Native ordinary merge did not retain the removed source's Rally queue entry.");
        foreach (var sample in samples.Take(4))
        {
            var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = sample.GetProperty("After").Deserialize<RoomCombatState>()!;
            Require(before.Preview && after.Preview && Serialize(before.Context!.SpawnPoints) == Serialize(after.Context!.SpawnPoints),
                "Native preview changed physical positions.");
        }
        Console.WriteLine("NATIVE-HORDE-MERGE-CHECKS PASS: 17 exact API/drained/source states, four preview/restoration cases, accepted/dispatch queues, five manager selections, mixed equipment, both-team centering, self/cross-team/immunity/null boundaries and 32 branches.");
    }

    private static void Verify(FixtureValue sample)
    {
        string label = sample.GetProperty("Label").GetString()!, operation = sample.GetProperty("Operation").GetString()!;
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        string parent = Serialize(before);
        int source = sample.GetProperty("SourceId").GetInt32(), target = sample.GetProperty("TargetId").GetInt32();
        var applied = operation == "Clone" ? HordeMergeModel.Clone(before, source) :
            HordeMergeModel.Merge(before, source, target, sample.GetProperty("FromBump").GetBoolean());
        Require(applied.Supported, label + " unsupported: " + applied.Result.UnsupportedReason);
        Compare(applied.Result.State, sample.GetProperty("AfterApi"), label + " API return");
        Compare(applied.SourceAfter, sample.GetProperty("SourceAfter"), label + " retained source");
        Require((applied.SourceAfter?.DeathState?.IsDestroyed == true) == sample.GetProperty("SourceDestroyed").GetBoolean() &&
            (applied.SourceAfter?.DeathState?.IsDespawned == true) == sample.GetProperty("SourceDespawned").GetBoolean(), label + " source lifecycle differs");
        var callbacks = applied.Result.PendingCallbacks.ToList();
        Compare(Payloads(callbacks), sample.GetProperty("Queued"), label + " accepted queue");
        Require(callbacks.Count == sample.GetProperty("QueueAfterApi").GetInt32() && sample.GetProperty("QueueAfter").GetInt32() == 0,
            label + " queue was incomplete");
        RoomCombatResult result = applied.Result;
        CombatUnit? retained = applied.SourceAfter;
        Require(RoomCombatModel.DrainCharacterQueue(callbacks, queued =>
        {
            result = RoomCombatModel.ApplyQueuedCharacterTrigger(result.State!, queued, callbacks.Add);
            retained = result.State?.Units.FirstOrDefault(unit => unit.Id == source) ??
                result.RetainedUnits.FirstOrDefault(unit => unit.Id == source) ?? retained;
            return result.Supported;
        }, queued => { result = RoomCombatModel.SettleQueuedSpawner(result.State!, queued.Unit); return result.Supported; },
            () => { result = EnchantmentWorldModel.CompleteQueuedRemovals(result.State!, callbacks.Add); return result.Supported; }, () => result.State?.Context, message => result = new RoomCombatResult(null, RoomOutcome.Unsupported, 0, [], message)),
            label + " queue unsupported: " + result.UnsupportedReason);
        Compare(result.State, sample.GetProperty("After"), label + " drained state");
        Compare(retained, sample.GetProperty("SourceAfterDrain"), label + " drained source");
        Compare(Payloads(callbacks), sample.GetProperty("Dispatched"), label + " dispatch order");
        if (before.Preview)
        {
            var primary = sample.GetProperty("PrimaryBeforePreview").Deserialize<RoomCombatState>()!;
            var restored = HordeMergeModel.RestorePreview(primary, result.State!);
            Require(restored.Supported, label + " restore unsupported: " + restored.UnsupportedReason);
            Compare(restored.State, sample.GetProperty("PrimaryAfterPreview"), label + " complete primary restoration");
        }
        Require(Serialize(before) == parent, label + " mutated parent");
    }
    private static object Payloads(IEnumerable<RoomCombatModel.QueuedCharacterTrigger> callbacks) => callbacks.Select(item => new {
        ActorId = item.Unit.Id, item.Kind, item.ParamInt, item.ParamInt2, item.ParamString, item.TriggerCount,
        DyingId = item.DyingCharacter?.Id ?? 0, item.LastSpawnedOverrideUnitId }).ToArray();
    private static void Compare<T>(T predicted, FixtureValue actual, string label)
    {
        object? expected = typeof(T) == typeof(RoomCombatState) ? actual.Deserialize<RoomCombatState>() :
            typeof(T) == typeof(CombatUnit) ? actual.ValueKind == FixtureKind.Null ? null : actual.Deserialize<CombatUnit>() : actual.ToObjectGraph();
        string? difference = ModelJson.Difference(Serialize(predicted), Serialize(expected));
        Require(difference == null, label + ": " + difference);
    }
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
