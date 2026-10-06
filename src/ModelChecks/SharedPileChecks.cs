using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Model;

internal static class SharedPileChecks
{
    internal static void Run()
    {
        UnityRng rng = UnityRng.Seed(611);
        CardPileState[] piles = [new("Standby", [new(4, "unit"), new(5, "unit")], [4, 0, 5], [1]),
            new("Exhausted", [new(6, "unit")]), new("Eaten", [new(7, "unit")]),
            new("Purged", [new(8, "unit")]), new("DiscardBuffer", [new(3, "spell")])];
        var context = new CombatContext(new([new(1, "spell")], [new(2, "spell")], [new(3, "spell")], rng, 0, []),
            rng, 0, 9, 10, statistics: BattleStatistics.Empty(), otherPiles: piles);
        string parent = JsonSerializer.Serialize(context);
        CombatUnit Unit(int id, int card) => new(id, "unit", CombatTeam.Player, 0, 1, 1, true, false, false, [], spawnerCardId: card);
        var room = new RoomCombatState(0, false, [Unit(1, 4)], [], context);
        RoomCombatResult death = RoomCombatModel.ApplyCardDamage(room, 1, 1);
        Require(death.Supported, "Room death rejected shared pile state: " + death.UnsupportedReason);
        CombatContext deadContext = death.State!.Context!;
        Require(deadContext.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Select(card => card.InstanceId).SequenceEqual([6, 4]) &&
            deadContext.OtherPiles!.Single(pile => pile.Name == "Standby").EntrySlots!.SequenceEqual([0, 0, 5]) &&
            deadContext.OtherPiles!.Single(pile => pile.Name == "Standby").FreeSlots!.SequenceEqual([0, 1]) &&
            deadContext.Statistics!.Value(4, "TimesExhausted") == 1,
            "Room death failed to route its spawner or preserve the native free-list chain.");
        RoomCombatResult deferred = RoomCombatModel.ApplyCardDamage(room, 1, 1, deferSpawnerExhaustion: true);
        Require(deferred.Supported && JsonSerializer.Serialize(deferred.State!.Context!.OtherPiles) == JsonSerializer.Serialize(piles) &&
            deferred.State.Context.Statistics!.Value(4, "TimesExhausted") == 0,
            "Deferred spell damage routed its spawner before the death queue drained.");
        var train = new TrainCombatState([room, new(1, false, [Unit(2, 5)], [], context)], [], 5, context);
        TrainSpellResult spell = CardSpellModel.Apply(train, 0, [new("Damage", "Tower", 1, false, true, [])], 0);
        Require(spell.Supported, "Tower damage rejected context-owned piles: " + spell.UnsupportedReason);
        CombatContext afterSpell = spell.State!.Context!;
        Require(afterSpell.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Count == 0 &&
            afterSpell.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Count == 3 &&
            new[] { 4, 5 }.All(id => afterSpell.Statistics!.Value(id, "TimesExhausted") == 1) &&
            spell.State.Rooms.All(result => ReferenceEquals(result.Context, afterSpell)) &&
            JsonSerializer.Serialize(spell.OtherPiles) == JsonSerializer.Serialize(afterSpell.OtherPiles),
            "Cross-room death returns failed to update the shared context and exposed piles together.");
        var despawn = new CombatTrigger("PostCombat", false, false, false, 1,
            [new("CardEffectDespawnCharacter", 0, 1, "", 0, [], false)]);
        CombatUnit fading = new(1, "fading", CombatTeam.Player, 0, 1, 1, true, false, false, [], [despawn], 4);
        RoomCombatResult faded = RoomCombatModel.Resolve(new(0, false, [fading], [], context));
        Require(faded.Supported && faded.State!.Units.Count == 0 &&
            faded.State.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Last().InstanceId == 4,
            "Post-combat despawn left its card in standby.");
        CombatUnit boss = new(3, "boss", CombatTeam.Enemy, 0, 1, 1, true, false, true, []);
        RoomCombatResult terminal = RoomCombatModel.ApplyCardDamage(new(0, false, [boss], [], context), 3, 1);
        Require(terminal.Supported && terminal.Outcome == RoomOutcome.BattleWon &&
            terminal.State!.Context!.OtherPiles!.All(pile => pile.Cards.Count == 0) &&
            terminal.State.Context.OtherPiles!.Single(pile => pile.Name == "Standby").EntrySlots!.Count == 0 &&
            terminal.State.Context.OtherPiles!.Single(pile => pile.Name == "Standby").FreeSlots!.Count == 0,
            "Terminal card clearing retained secondary piles or dictionary entries.");
        RoomCombatResult preview = RoomCombatModel.Resolve(new(0, false, [fading], [], context, preview: true));
        Require(preview.Supported && JsonSerializer.Serialize(preview.State!.Context!.OtherPiles) == JsonSerializer.Serialize(piles),
            "Preview deaths mutated gameplay pile membership.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(CardSpellModel.Apply(train, 0,
            [new("Damage", "Tower", 1, false, true, [])], 0).State) == JsonSerializer.Serialize(spell.State),
            "Parallel secondary pile branches diverged."));
        Require(JsonSerializer.Serialize(context) == parent, "Secondary pile resolution mutated its parent.");
        piles[0] = new("Standby", []);
        Require(context.OtherPiles![0].Cards.Count == 2, "Shared pile capture retained the caller's mutable collection.");
        Console.WriteLine("SHARED-PILE-CHECKS PASS: immediate/deferred returns, physical slots, tower context propagation, despawn, terminal clearing, preview isolation and 32 parallel branches.");
    }

    internal static void Native(JsonElement fixture)
    {
        if (!fixture.TryGetProperty("Schema", out JsonElement schema) || schema.GetInt32() < 21) return;
        int deaths = 0, freeSlotReuses = 0, transientReuses = 0, contexts = 0;
        foreach (JsonElement stage in fixture.GetProperty("Stages").EnumerateArray())
        {
            RoomCombatState before = stage.GetProperty("Before").Deserialize<RoomCombatState>(ModelJson.Options)!;
            RoomCombatState after = stage.GetProperty("Actual").Deserialize<RoomCombatState>(ModelJson.Options)!;
            Require(before.Context!.OtherPiles != null && after.Context!.OtherPiles != null,
                "Modern native room capture omitted secondary piles.");
            contexts += 2;
            foreach (CombatUnit unit in before.Units.Where(unit => unit.Team == CombatTeam.Player && unit.SpawnerCardId > 0 &&
                after.Units.All(alive => alive.Id != unit.Id)))
                if (after.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == unit.SpawnerCardId))
                    deaths++;
        }
        foreach (string collection in new[] { "Turns", "Actions" })
        foreach (JsonElement record in fixture.GetProperty(collection).EnumerateArray())
        foreach (string side in new[] { "Before", "Actual" })
        {
            BattleTurnState state = record.GetProperty(side).Deserialize<BattleTurnState>(ModelJson.Options)!;
            CombatContext context = state.Spawn.Train.Context!;
            Require(context.OtherPiles != null && JsonSerializer.Serialize(context.OtherPiles) == JsonSerializer.Serialize(state.OtherPiles),
                "Modern native decision exposes inconsistent shared and outer pile state.");
            Require(state.Spawn.Train.Rooms.All(room => JsonSerializer.Serialize(room.Context!.OtherPiles) == JsonSerializer.Serialize(context.OtherPiles)),
                "A native decision room omitted the shared secondary piles.");
            contexts++;
        }
        foreach (JsonElement record in fixture.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = record.GetProperty("Before").Deserialize<BattleTurnState>(ModelJson.Options)!;
            BattleTurnState after = record.GetProperty("Actual").Deserialize<BattleTurnState>(ModelJson.Options)!;
            int cardId = record.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            CardPileState standby = before.OtherPiles!.Single(pile => pile.Name == "Standby");
            CardPileState moved = after.OtherPiles!.Single(pile => pile.Name == "Standby");
            if (standby.FreeSlots?.Count > 0 && moved.Cards.Any(card => card.InstanceId == cardId) &&
                moved.EntrySlots![standby.FreeSlots[0]] == cardId) freeSlotReuses++;
            var alive = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id).ToHashSet();
            var exhausted = after.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Select(card => card.InstanceId).ToHashSet();
            deaths += before.Spawn.Train.Rooms.SelectMany(room => room.Units).Count(unit => unit.Team == CombatTeam.Player &&
                unit.SpawnerCardId > 0 && !alive.Contains(unit.Id) && exhausted.Contains(unit.SpawnerCardId));
        }
        if (fixture.TryGetProperty("HandRemovals", out JsonElement removals))
        foreach (JsonElement record in removals.EnumerateArray())
        {
            if (record.GetProperty("Mode").GetInt32() != 1) continue;
            CombatContext before = record.GetProperty("Before").GetProperty("Context").Deserialize<CombatContext>(ModelJson.Options)!;
            CardPileState standby = before.OtherPiles!.Single(pile => pile.Name == "Standby");
            CardPileState moved = record.GetProperty("Actual").GetProperty("Context").Deserialize<CombatContext>(ModelJson.Options)!
                .OtherPiles!.Single(pile => pile.Name == "Standby");
            int sourceCardId = record.GetProperty("CardId").GetInt32();
            int targets = before.Cards.Hand.Count(card => card.InstanceId != sourceCardId);
            // Native consumption temporarily inserts each card, then removes it. With an existing
            // free entry, unchanged allocated length proves these insertions reused the slots.
            if (targets > 0 && standby.FreeSlots?.Count > 0 && standby.EntrySlots!.Count == moved.EntrySlots!.Count)
                transientReuses += targets;
        }
        bool lethalHandRemoval = fixture.TryGetProperty("ModifierScenario", out JsonElement modifier) &&
            modifier.GetString() == "hand-removal-lethal";
        Require(contexts > 0 && (!lethalHandRemoval || deaths > 0 && freeSlotReuses + transientReuses > 0),
            "Lethal shared-pile oracle lacks death returns or standby free-slot reuse.");
        Console.WriteLine($"NATIVE-SHARED-PILE-COVERAGE PASS: {contexts} room/decision contexts, {deaths} observed death returns, {freeSlotReuses} summon and {transientReuses} transient consumption slot reuses.");
    }

    internal static void MigratedRoot(JsonElement fixture)
    {
        JsonElement turns = fixture.GetProperty("Turns");
        fixture.TryGetProperty("Actions", out JsonElement actions);
        string? policy = fixture.TryGetProperty("Policy", out JsonElement policyElement) ? policyElement.GetString() : null;
        bool cardPolicy = policy is "units-and-junk" or "units-spells-and-junk";
        BattleTurnState root = (cardPolicy ? actions[0] : turns[0]).GetProperty("Before")
            .Deserialize<BattleTurnState>(ModelJson.Options)!;
        CombatContext old = root.Spawn.Train.Context!;
        if (old.OtherPiles != null) return;
        string parent = JsonSerializer.Serialize(root);
        var context = new CombatContext(old.Cards, old.BattleRng, old.Gold, old.NextCardId, old.MaxHandSize,
            old.StatusRules, old.Statistics, old.CardInstances, old.CardRegistry, old.AllScenarioBossesDead,
            old.NextAddedTemporaryUpgrades, root.OtherPiles);
        TrainCombatState originalTrain = root.Spawn.Train;
        var train = new TrainCombatState(originalTrain.Rooms.Select(room => new RoomCombatState(room.RoomIndex,
            room.Deployment, room.Units, room.ExternalInteractions, context, room.Preview)).ToArray(),
            originalTrain.Movement, originalTrain.EnemySlotsPerRoom, context);
        EnemySpawnState originalSpawn = root.Spawn;
        var spawn = new EnemySpawnState(train, originalSpawn.Waves, originalSpawn.SelectedGroups, originalSpawn.Phase,
            originalSpawn.Looping, originalSpawn.Rng, originalSpawn.NextUnitId, originalSpawn.Treasures,
            originalSpawn.TreasuresRemaining, originalSpawn.TreasureEnabled, originalSpawn.FirstTreasureTurn,
            originalSpawn.FirstTreasureRoom, originalSpawn.Turn, originalSpawn.ExternalInteractions);
        var migrated = new BattleTurnState(spawn, root.Energy, root.EnergyPerTurn, root.DrawPerTurn, root.ForgePoints,
            root.DragonsHoard, root.MoonPhase, root.RngStreams, root.OtherPiles, root.ExternalInteractions,
            root.PlayRules, root.BattlePreviewEnabled, root.UiRngIsolated);
        Func<BattleTurnState, PlayCardAction?> chooser = policy == "units-spells-and-junk"
            ? BattleActionModel.ChooseUnitSpellAndJunkPlay : policy == "units-and-junk"
            ? BattleActionModel.ChooseUnitAndJunkPlay : _ => null;
        BattleSimulationResult result = BattleSimulator.Resolve(migrated, chooser);
        Require(result.Supported && result.Turns.Count == turns.GetArrayLength() &&
            (!cardPolicy || result.Actions.Count == actions.GetArrayLength()),
            "A legacy root with shared piles lost full-battle support: " + result.UnsupportedReason);
        for (int index = 0; index < result.Turns.Count; index++)
            Compare(result.Turns[index].State!, turns[index].GetProperty("Actual"));
        if (cardPolicy)
            for (int index = 0; index < result.Actions.Count; index++)
                Compare(result.Actions[index].Result.State!, actions[index].GetProperty("Actual"));
        Require(JsonSerializer.Serialize(root) == parent, "Migrating a legacy root mutated its captured native state.");
        Console.WriteLine("SHARED-PILE-MIGRATION-CHECKS PASS: complete legacy policy/decision states reproduced with context-owned piles.");

        static void Compare(BattleTurnState modeled, JsonElement native)
        {
            Require(JsonSerializer.Serialize(modeled.Spawn.Train.Context!.OtherPiles) == JsonSerializer.Serialize(modeled.OtherPiles),
                "Migrated model exposes inconsistent shared and outer piles.");
            BattleTurnState actual = native.Deserialize<BattleTurnState>(ModelJson.Options)!;
            // Older native records lack only the new shared copy. Keep every captured field,
            // including the authoritative outer piles, in the comparison.
            JsonNode view = JsonNode.Parse(BattleTurnChecks.Comparable(modeled))!;
            view["Context"]!.AsObject().Remove("OtherPiles");
            JsonNode oracle = JsonNode.Parse(BattleTurnChecks.Comparable(actual))!;
            oracle["Context"]!.AsObject().Remove("OtherPiles");
            Require(JsonNode.DeepEquals(view, oracle), "Migrated shared-pile policy differs from a native legacy decision.");
        }
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
