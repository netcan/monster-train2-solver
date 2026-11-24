using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SkullCollectorsChecks
{
    internal static void Native(FixtureValue witness, bool observeOnly)
    {
        FixtureValue owner = witness.GetProperty("Owner");
        Require(witness.GetProperty("Schema").GetInt32() == 1 &&
            witness.GetProperty("Boundary").GetString() == "OriginalSkullCollectorsTreasureBirth" &&
            witness.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            witness.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            owner.GetProperty("Id").GetString() == RelicSkullCollectorsModel.OwnerId &&
            owner.GetProperty("AssetKey").GetString() == RelicSkullCollectorsModel.OwnerKey &&
            !owner.GetProperty("DisallowInDeploymentPhase").GetBoolean(), "Expected the pinned original SkullCollectors owner.");

        var tiers = owner.GetProperty("OrderedMappings").EnumerateArray().Select(row =>
            (Before: Character(row.GetProperty("Before")), After: Character(row.GetProperty("After")))).ToArray();
        Require(tiers.Length == 3 && tiers.Select(tier => tier.Before.Id).SequenceEqual(new[]
            {
                RelicSkullCollectorsModel.StockTierOneId, "0af268b6-ccf5-4707-b695-8ef2db08f9f4", "828ee3b1-ce27-43f2-9864-bd4126a17974"
            }) && tiers.Select(tier => tier.After.Id).SequenceEqual(new[]
            {
                RelicSkullCollectorsModel.ReplacementTierOneId, "6ebc26b8-061b-4ffa-b7ba-b2c5c584f824", "f1cee43a-b773-4d8c-b5de-0df9a2ea4eb7"
            }) && tiers.Select(tier => (tier.Before.Attack, tier.Before.Health)).SequenceEqual(new[] { (0, 1), (0, 3), (0, 5) }) &&
            tiers.Select(tier => (tier.After.Attack, tier.After.Health)).SequenceEqual(new[] { (5, 11), (10, 23), (15, 35) }) &&
            tiers.All(tier => tier.Before.Size == 2 && tier.After.Size == 2 && tier.After.Key == tier.Before.Key + "_SkullCollectors"),
            "The ordered original treasure mappings differ.");
        var effects = owner.GetProperty("Effects").EnumerateArray().Select(effect => (
            effect.GetProperty("Type").GetString()!, effect.GetProperty("Amount").GetInt32(),
            effect.GetProperty("Team").GetString()!, effect.GetProperty("Target").GetString()!,
            effect.GetProperty("Subtype").GetString()!)).ToArray();
        string[] subtypes = [RelicSkullCollectorsModel.TierOne, "SubtypesData_TreasureCollectorT2", "SubtypesData_TreasureCollectorT3"];
        int[] healthBonuses = [30, 60, 90];
        int[] attackBonuses = [5, 15, 30];
        var expectedEffects = new[] { (RelicSkullCollectorsModel.Effect, 0, "None", "Room", "SubtypeData_None") }
            .Concat(healthBonuses.Select((amount, index) => (RelicModel.ModifyCharacterMaxHealth, amount, "Heroes", "Room", subtypes[index])))
            .Concat(attackBonuses.Select((amount, index) => (RelicModel.ModifyCharacterAttackDamage, amount, "Heroes", "Room", subtypes[index])));
        Require(effects.SequenceEqual(expectedEffects), "The ordered original treasure callbacks differ.");

        FixtureValue birth = witness.GetProperty("Birth");
        int actorId = birth.GetProperty("ActorId").GetInt32();
        var original = Character(birth.GetProperty("Original"));
        int tier = Array.FindIndex(tiers, mapping => mapping.Before == original);
        Require(tier >= 0, "The observed collector's original definition is outside the pinned substitution table.");
        var replacement = tiers[tier].After;
        Require(Character(birth.GetProperty("ActorOriginal")) == original &&
            Character(birth.GetProperty("Requested")) == replacement && Character(birth.GetProperty("Used")) == replacement &&
            Character(birth.GetProperty("ActorSource")) == replacement, "Native collector definition provenance differs.");
        // Original Ring 2, 4 and 7 receipts, respectively. Floors and points are zero-based.
        (int Turn, int First, int Floor, int Point) placement = tier switch
        {
            0 => (1, 1, 2, 0),
            1 => (2, 0, 1, 2),
            _ => (2, 0, 0, 3)
        };

        FixtureValue spawn = witness.GetProperty("Spawn");
        EnemySpawnState before = spawn.GetProperty("Before").Deserialize<EnemySpawnState>()!;
        EnemySpawnState actual = spawn.GetProperty("Actual").Deserialize<EnemySpawnState>()!;
        EnemySpawnState decision = witness.GetProperty("FirstDecision").Deserialize<BattleTurnState>()!.Spawn;
        EnemyDefinition definition = before.Treasures.Single();
        Require(spawn.GetProperty("IncludeTreasure").GetBoolean() && before.TreasureEnabled && before.Turn == placement.Turn &&
            actual.Turn == before.Turn && decision.Turn == before.Turn + 1 && before.Phase == placement.Turn &&
            actual.Phase == before.Phase + 1 && actual.Phase == decision.Phase &&
            before.TreasuresRemaining == 1 && actual.TreasuresRemaining == 0 && decision.TreasuresRemaining == 0 &&
            before.Train.Context!.Relics!.Count(RelicSkullCollectorsModel.IsOriginal) == 1 &&
            before.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Id != actorId) &&
            actorId == before.NextUnitId + 3 && actual.NextUnitId == actorId + 1 && decision.NextUnitId == actual.NextUnitId,
            "Expected one new collector after three ordinary wave births at the pinned spawn boundary.");
        string poolKey = observeOnly ? original.Key : replacement.Key;
        Require(definition.Unit.AssetKey == poolKey && actual.Treasures.Single().Unit.AssetKey == poolKey &&
            definition.TreasureOriginalId == (observeOnly ? null : original.Id), "The captured treasure pool or source identifier differs.");
        foreach (EnemySpawnState state in new[] { actual, decision })
        {
            CombatUnit actor = state.Train.Rooms.Single(room => room.RoomIndex == placement.Floor).Units.Single(unit => unit.Id == actorId);
            Require(actor.AssetKey == replacement.Key && actor.Team == CombatTeam.Enemy && actor.Subtypes.Contains(subtypes[tier]) &&
                actor.Size == replacement.Size && actor.Modifiers is { IsClone: false } modifiers &&
                modifiers.AttackDamage == replacement.Attack && modifiers.DamageBuff == attackBonuses[tier] &&
                actor.Attack == replacement.Attack + attackBonuses[tier] &&
                actor.Health == replacement.Health + healthBonuses[tier] && actor.MaxHealth == actor.Health &&
                actor.Status("rooted") == null &&
                state.Train.Movement.Single(move => move.UnitId == actorId) is { Speed: 1, Ascends: false, Loops: false, CompanionBoss: false } &&
                state.Train.Context!.SpawnPoints!.SelectedGroup(placement.Floor, CombatTeam.Enemy)!.Occupants[placement.Point] == actorId,
                "The settled collector's stats, movement or physical slot differs.");
        }

        RngDraw wave = before.Rng.Range(0, before.Waves[before.Phase].Candidates.Count);
        int[] eligibleFloors = actual.Train.Rooms
            .Where(room => !room.Units.Any(unit => unit.IsPyre) && room.RoomIndex >= placement.First &&
                room.Units.Count(unit => unit.Team == CombatTeam.Enemy && unit.Id != actorId) < before.Train.EnemySlotsPerRoom)
            .Select(room => room.RoomIndex).ToArray();
        Require(eligibleFloors.SequenceEqual(Enumerable.Range(placement.First, 3 - placement.First)),
            "The original stock spawn did not leave every eligible floor open.");
        RngDraw floor = wave.State.Range(0, eligibleFloors.Length);
        RngDraw collector = floor.State.Range(0, before.Treasures.Count);
        Require(before.FirstTreasureRoom == placement.First && before.SelectedGroups[before.Phase] == -1 &&
            wave.Value == 0 && actual.SelectedGroups[before.Phase] == wave.Value && eligibleFloors[floor.Value] == placement.Floor &&
            collector.Value == 0 &&
            collector.State.Equals(actual.Rng), "The native wave, eligible floor and collector RNG sequence differs.");

        if (observeOnly)
        {
            string[] historicalGuards = tier == 0
                ? [$"Unmodeled relic effect {RelicSkullCollectorsModel.Effect} on SkullCollectors.", "Spawn mutators"]
                : ["Spawn mutators"];
            Require(before.ExternalInteractions.SequenceEqual(historicalGuards) && actual.ExternalInteractions.SequenceEqual(historicalGuards),
                "Expected the historical unsupported projection in observation-only mode.");
        }
        else
        {
            string parent = JsonSerializer.Serialize(before);
            EnemySpawnResult replay = EnemySpawningModel.Spawn(before, includeTreasure: true,
                roomRules: spawn.GetProperty("Rooms").Deserialize<RoomPlayRule[]>());
            Require(replay.Supported, replay.UnsupportedReason ?? "The projected original collector spawn was rejected.");
            string predicted = EnemySpawningChecks.Comparable(replay.State!);
            string observed = EnemySpawningChecks.Comparable(actual);
            if (predicted != observed)
                throw new InvalidOperationException($"The projected original collector successor differs: {ModelJson.Difference(predicted, observed)}");
            Require(JsonSerializer.Serialize(before) == parent, "The collector spawn mutated its recorded parent.");
        }
        Console.WriteLine($"NATIVE-SKULL-COLLECTORS PASS. Tier {tier + 1} provenance, {replacement.Attack + attackBonuses[tier]} attack, " +
            $"{replacement.Health + healthBonuses[tier]} health, exact Spawning RNG and {(observeOnly ? "historical observation" : "complete spawned successor")}.");
    }

    private static (string Id, string Key, int Attack, int Health, int Size) Character(FixtureValue row) =>
        (row.GetProperty("Id").GetString()!, row.GetProperty("AssetKey").GetString()!, row.GetProperty("Attack").GetInt32(),
            row.GetProperty("Health").GetInt32(), row.GetProperty("Size").GetInt32());

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
