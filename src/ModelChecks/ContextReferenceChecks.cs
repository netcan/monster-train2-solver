using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ContextReferenceChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(71);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 2, 10,
            cardRegistry: [new(1, "source", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [])], nextUnitId: 2);
        CombatUnit Unit(int card) => new(1, "actor", CombatTeam.Player, 1, 10, 10, true, false, false, [], spawnerCardId: card);
        Verify(context, [Unit(1)]);
        Parallel.For(0, 32, _ => Verify(context, [Unit(1)]));
        bool rejected = false;
        try { Verify(context, [Unit(2)]); } catch (InvalidDataException) { rejected = true; }
        Require(rejected, "A retained actor's source card was allowed outside the frozen registry/counter.");
        Console.WriteLine("CONTEXT-REFERENCE-CHECKS PASS: detached source closure, missing registry entries and 32 immutable checks.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (fixture.GetProperty("Schema").GetInt32() < 105) return;
        int contexts = 0;
        foreach (string collection in new[] { "Stages", "TrainPhases", "Spawns", "Actions", "Turns" })
        foreach (var sample in fixture.GetProperty(collection).EnumerateArray())
        foreach (string boundary in new[] { "Before", "Actual" })
        {
            CombatContext context;
            IEnumerable<CombatUnit> actors;
            var value = sample.GetProperty(boundary);
            if (collection == "Stages")
            {
                var state = value.Deserialize<RoomCombatState>()!;
                context = state.Context!; actors = state.Units;
            }
            else
            {
                var train = collection == "TrainPhases" ? value.Deserialize<TrainCombatState>()! :
                    collection == "Spawns" ? value.Deserialize<EnemySpawnState>()!.Train :
                    value.Deserialize<BattleTurnState>()!.Spawn.Train;
                context = train.Context!; actors = train.Rooms.SelectMany(room => room.Units);
            }
            Verify(context, actors); contexts++;
        }
        Console.WriteLine($"NATIVE-CONTEXT-REFERENCE-CHECKS PASS: {contexts} complete registries/counters, retained source cards, weak targets, equipment, piles and copied-point inventories.");
    }

    private static void Verify(CombatContext context, IEnumerable<CombatUnit> current)
    {
        Require(context.CardRegistry != null && context.CardRegistry.Select(card => card.InstanceId)
            .SequenceEqual(Enumerable.Range(1, context.NextCardId - 1)), "Snapshot card registry is incomplete or its counter is stale.");
        var cards = context.CardRegistry!.Select(card => card.InstanceId).ToHashSet();
        var actors = current.Concat(context.Enchantments?.Rooms.SelectMany(room => room.Units) ?? [])
            .Concat(context.Enchantments?.RetainedUnits.Select(actor => actor.Unit) ?? []).ToArray();
        foreach (CombatUnit actor in actors)
        {
            Require(context.NextUnitId.HasValue && actor.Id > 0 && actor.Id < context.NextUnitId,
                "Snapshot actor is outside its unit identity counter.");
            Require(actor.SpawnerCardId == 0 || cards.Contains(actor.SpawnerCardId),
                $"Snapshot actor {actor.Id} references unregistered source card {actor.SpawnerCardId}.");
            Require((actor.EquipmentCards ?? []).All(cards.Contains), "Snapshot equipment references an unregistered card.");
        }
        Require(context.Cards.Hand.Concat(context.Cards.Draw).Concat(context.Cards.Discard)
            .Concat((context.OtherPiles ?? []).SelectMany(pile => pile.Cards)).All(card => cards.Contains(card.InstanceId)),
            "Snapshot pile references an unregistered card.");
        foreach (CardInstanceState card in context.CardRegistry!)
            foreach (int id in (card.RawPlayedRoomUnitIds ?? []).Concat(card.PlayedRoomUnitIds ?? []))
                Require(id > 0 && id < context.NextUnitId && (context.SpawnPoints == null ||
                    context.SpawnPoints.Units.Any(unit => unit.UnitId == id)), "Snapshot weak target is outside its unit inventory.");
        if (context.SpawnPoints != null)
        {
            Require(BattleSpawnPointModel.Validate(context.SpawnPoints, context.NextUnitId) == null,
                "Snapshot copied-point references or allocation counters are incomplete.");
            Require(actors.All(actor => context.SpawnPoints.Units.Any(unit => unit.UnitId == actor.Id)),
                "Snapshot actor is missing from its physical inventory.");
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
