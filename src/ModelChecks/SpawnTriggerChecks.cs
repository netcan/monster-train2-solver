using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class SpawnTriggerChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(58);
        CombatEffect Effect(string kind, int damage, int health, string target = "Self", bool enemy = false,
            string lifetime = "TemporaryUntilEndOfBattle") => new("CardEffectAddTempCardUpgradeToUnits", 0, 0, "", 0, [], false,
                unitUpgrade: new("UnitUpgrade", target, 0, enemy, !enemy, [],
                    new(kind, kind, new(damage: damage, health: health), [], false, false, false, 0, 0, []), lifetime));
        CombatTrigger Trigger(string kind, params CombatEffect[] effects) => new(kind, false, false, false, 1, effects, false);
        CombatEffect Gold(int amount) => new("CardEffectRewardGold", amount, 0, "", 0, [], false);
        var generation = new CombatEffect("CardEffectAddBattleCard", 0, 0, "HandPile", 1, ["junk"], false);
        var traits = new ScalingUnitUpgradeTrait[] { new(new("PlayedCost", sourceRawCost: 1), "Damage", 1, 1),
            new(new("AnyMonsterSpawned", "ThisBattle"), "Health", 2, 1) };
        CardInstanceState Card(int id, string type) => new(id, type, CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            unitUpgradeScalingTraits: type == "unit" ? traits : null);
        var context = new CombatContext(new([new(1, "unit"), new(2, "junk"), new(3, "unit")], [], [], rng, 0, []), rng, 0, 4, 10,
            statistics: BattleStatistics.Empty(deckCards: [1, 2, 3]).TrackCards([1, 2, 3]), cardInstances: [Card(1, "unit"), Card(2, "junk"), Card(3, "unit")],
            cardRegistry: [], allScenarioBossesDead: false, otherPiles: [new("Standby", [], [], []), new("DiscardBuffer", []),
                new("Exhausted", []), new("Purged", []), new("Eaten", [])], queryFrame: new(3, true, 0, 0, 0, 1, 0));
        CombatUnit Template(CombatTrigger[] triggers) => new(0, "unit", CombatTeam.Player, 2, 10, 10, true, false, false, [], triggers,
            size: 1, subtypes: ["imp"], modifiers: new(2, 0, 0, 1, 1, true, false, []), isBoss: false);
        var pyre = new CombatUnit(10, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, []);
        BattleTurnState Root(CombatUnit template)
        {
            var train = new TrainCombatState(Enumerable.Range(0, 4).Select(index => new RoomCombatState(index, true,
                index == 3 ? [pyre] : [], [], context)).ToArray(), [], 7, context);
            var spawn = new EnemySpawnState(train, [new([new([])])], [-1], 0, false, rng, 11, [], 0, false, 0, 1, 0, []);
            var rules = new BattlePlayRules(Enumerable.Range(0, 4).Select(index => new RoomPlayRule(index, 5, 7, true, false, index == 3)).ToArray(),
                [new("unit", "unit", 1, "SpawnMonster", "Standby", template, []), new("junk", "junk", 0, "Null", "Purged", null, [])]);
            return new(spawn, 3, 3, 5, 0, 0, "New", [], context.OtherPiles!, [], rules);
        }
        var template = Template([Trigger("OnSpawn", Effect("scaled", 1, 1), generation),
            Trigger("OnUnscaledSpawn", Effect("unscaled", 1, 1, lifetime: "TemporaryUntilUnitDeath")),
            Trigger("OnSpawnNotFromCard", Gold(100))]);
        var root = Root(template);
        string parent = JsonSerializer.Serialize(root);
        var first = BattleActionModel.PlayCard(root, new(1, 0));
        Require(first.Supported, first.Reason ?? "Spawn rejected.");
        CombatUnit unit = first.State!.Spawn.Train.Rooms[0].Units.Single();
        CombatContext after = first.State.Spawn.Train.Context!;
        Require(unit.BaseAttack == 6 && unit.Health == 14 && unit.MaxHealth == 14 &&
            unit.Triggers.Take(2).All(trigger => trigger.HasTriggered) && !unit.Triggers.Last().HasTriggered && after.Gold == 0 &&
            unit.Modifiers!.Upgrades.Select(upgrade => upgrade.DataId).SequenceEqual(["scaled", "unscaled"]) &&
            unit.Modifiers.Upgrades[0].Stats.Health == 3 && unit.Modifiers.Upgrades[0].Stats.Damage == 3,
            "Paid cost/new spawn queries, ordered phases or card-origin gating differ: " + JsonSerializer.Serialize(unit));
        Require(after.Cards.Hand.First().InstanceId == 4 && after.CardInstances!.Single(card => card.InstanceId == 1).Temporary.Upgrades.Single().DataId == "scaled" &&
            after.Statistics!.Value(1, "TimesPlayed") == 1 && after.Statistics.Value(1, "TimesDiscarded") == 1 && after.Statistics.PlayedCosts.Count == 0 &&
            after.Statistics.SubtypesSpawnedThisBattle.Single().Value == 1 && first.State.Energy == 2 && after.QueryFrame!.Energy == 2,
            "Spawn generation, source upgrades, subtype/callback counters or paid energy differ.");
        var second = BattleActionModel.PlayCard(first.State, new(3, 1));
        Require(second.Supported && second.State!.Spawn.Train.Rooms[1].Units.Single().Health == 16 &&
            second.State.Spawn.Train.Context!.Statistics!.SpawnedThisBattlePerFloor.Sum(count => count.Value) == 2,
            "Later summons did not observe previous/current spawn statistics.");
        var plain = new CombatUnit(20, "plain", CombatTeam.Enemy, 1, 10, 10, true, false, false, [],
            [Trigger("OnSpawn", Gold(1)), Trigger("OnUnscaledSpawn", Gold(2)), Trigger("OnSpawnNotFromCard", Gold(3))],
            modifiers: new(1, 0, 0, 1, 1, true, false, []));
        Require(RoomCombatModel.ApplySpawnTriggers(new(0, false, [plain], [], context), 20, false).State!.Context!.Gold == 15,
            "Cardless creation omitted one of its ordered spawn phases.");
        var deployment = Template([new("OnSpawn", true, false, false, 1, [Gold(1)], true)]);
        var skipped = BattleActionModel.PlayCard(Root(deployment), new(1, 0));
        Require(skipped.Supported && skipped.State!.Spawn.Train.Context!.Gold == 0 &&
            !skipped.State.Spawn.Train.Rooms[0].Units.Single().Triggers.Single().HasTriggered,
            "Deployment timing consumed a skipped spawn trigger.");
        var death = Template([Trigger("OnSpawn", Effect("fatal", 0, -100, lifetime: "TemporaryUntilUnitDeath")), Trigger("OnDeath", Gold(1))]);
        var died = BattleActionModel.PlayCard(Root(death), new(1, 0));
        Require(died.Supported && died.State!.Spawn.Train.Rooms[0].Units.Count == 0 &&
            died.State.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Single().InstanceId == 1 &&
            died.State.OtherPiles.Single(pile => pile.Name == "Standby").EntrySlots!.SequenceEqual([0]) &&
            died.State.OtherPiles.Single(pile => pile.Name == "Standby").FreeSlots!.SequenceEqual([0]) &&
            died.State.Spawn.Train.Context!.Statistics!.MonstersDeadThisBattle == 1 &&
            died.State.Spawn.Train.Context.Statistics.Value(1, "TimesExhausted") == 1 &&
            died.State.Spawn.Train.Context.Statistics.Value(1, "TimesPlayed") == 1 && died.State.Spawn.Train.Context.Gold == 5,
            "Transient summon death lost callbacks, exhausted twice or skipped standby allocation history.");
        var vanish = Template([Trigger("OnSpawn", new CombatEffect("CardEffectDespawnCharacter", 1, 1, "", 0, [], false)), Trigger("OnDeath", Gold(1))]);
        var gone = BattleActionModel.PlayCard(Root(vanish), new(1, 0));
        Require(gone.Supported && gone.State!.Spawn.Train.Rooms[0].Units.Count == 0 && gone.State.Spawn.Train.Context!.Gold == 0 &&
            gone.State.Spawn.Train.Context.Statistics!.MonstersDeadThisBattle == 0 && gone.State.Spawn.Train.Context.Statistics.Value(1, "TimesExhausted") == 1,
            "Spawn despawn fired death effects or counted source exhaustion twice.");
        var enemyTriggers = new[] { Trigger("OnSpawn", Effect("batch", 1, 1, "Room", true, "TemporaryUntilUnitDeath")),
            Trigger("OnUnscaledSpawn", Effect("unscaled", 1, 1, enemy: true, lifetime: "TemporaryUntilUnitDeath")),
            Trigger("OnSpawnNotFromCard", Effect("cardless", 1, 1, enemy: true, lifetime: "TemporaryUntilUnitDeath")) };
        var enemy = new EnemyDefinition(new(0, "enemy", CombatTeam.Enemy, 1, 10, 10, true, false, false, [], enemyTriggers,
            modifiers: new(1, 0, 0, 1, 1, true, false, [])), true, false, []);
        var enemyRoot = new EnemySpawnState(root.Spawn.Train, [new([new([enemy, enemy, enemy])])], [-1], 0, false,
            rng, 11, [], 0, false, 0, 1, 0, []);
        var batch = EnemySpawningModel.Spawn(enemyRoot, false);
        Require(batch.Supported && batch.State!.Train.Rooms[0].Units.All(enemy => enemy.BaseAttack == 6 && enemy.MaxHealth == 15 &&
            enemy.Triggers.All(trigger => trigger.HasTriggered)) && batch.State.Train.Context!.Statistics!.SpawnedThisBattlePerFloor.Count == 0,
            "Enemy triggers ran before the whole group entered or counted enemy creation as a monster summon.");
        var deadEnemy = new EnemyDefinition(new(0, "deadEnemy", CombatTeam.Enemy, 1, 10, 10, true, false, false, [],
            [Trigger("OnSpawn", Effect("fatal", 0, -100, enemy: true, lifetime: "TemporaryUntilUnitDeath"))],
            modifiers: new(1, 0, 0, 1, 1, true, false, [])), true, false, []);
        var deadWave = new EnemySpawnState(root.Spawn.Train, [new([new([deadEnemy])])], [-1], 0, false,
            rng, 11, [], 0, false, 0, 1, 0, []);
        var wave = EnemySpawningModel.Spawn(deadWave, false);
        Require(wave.Supported && wave.State!.Train.Rooms.All(room => room.Units.All(unit => unit.Team == CombatTeam.Player)) &&
            wave.State.Train.Movement.Count == 0 && wave.State.NextUnitId == 12,
            "Spawn death retained enemy movement or recycled its allocated identity.");
        var treasureRoot = new EnemySpawnState(root.Spawn.Train, [new([new([])]), new([new([])]), new([new([])])], [0, 0, 0],
            1, false, rng, 11, [enemy], 1, true, 0, 1, 0, []);
        var treasure = EnemySpawningModel.Spawn(treasureRoot, true);
        Require(treasure.Supported && treasure.State!.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Team == CombatTeam.Enemy).BaseAttack == 4 &&
            treasure.State.TreasuresRemaining == 0, "Treasure entry omitted cardless spawn settlement.");
        string expected = JsonSerializer.Serialize(first.State);
        string expectedBatch = JsonSerializer.Serialize(batch.State);
        Parallel.For(0, 32, _ => {
            Require(JsonSerializer.Serialize(BattleActionModel.PlayCard(root, new(1, 0)).State) == expected, "Parallel summon branches differ.");
            Require(JsonSerializer.Serialize(EnemySpawningModel.Spawn(enemyRoot, false).State) == expectedBatch, "Parallel batch branches differ.");
        });
        Require(JsonSerializer.Serialize(root) == parent, "Spawn effects mutated the parent.");
        Console.WriteLine("SPAWN-TRIGGER-CHECKS PASS: paid costs/resources, pre-trigger spawn counters, ordered card/cardless phases, source upgrades/generation, death/despawn/slots, enemy batches/treasures and 32 parallel branches.");
    }
    internal static void Native(JsonElement fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out JsonElement scenario) || scenario.GetString() is not ("spawn-triggers" or "spawn-triggers-lethal")) return;
        bool lethal = scenario.GetString() == "spawn-triggers-lethal";
        int spawn = 0, unscaled = 0, bonus = 0, paid = 0;
        foreach (JsonElement sample in fixture.GetProperty("UnitUpgradeScaling").EnumerateArray())
        {
            Require(sample.GetProperty("Difference").ValueKind == JsonValueKind.Null && sample.GetProperty("CaptureError").ValueKind == JsonValueKind.Null,
                "Native spawn callback capture is incomplete.");
            CombatContext before = sample.GetProperty("Before").Deserialize<CombatContext>(ModelJson.Options)!;
            CombatContext after = sample.GetProperty("After").Deserialize<CombatContext>(ModelJson.Options)!;
            ScalingUnitUpgradeTrait trait = sample.GetProperty("Trait").Deserialize<ScalingUnitUpgradeTrait>()!;
            CardUpgradeModifier original = sample.GetProperty("BeforeUpgrade").Deserialize<CardUpgradeModifier>()!;
            CardUpgradeModifier actual = sample.GetProperty("AfterUpgrade").Deserialize<CardUpgradeModifier>()!;
            string? kind = sample.GetProperty("TriggerKind").GetString();
            var result = UnitUpgradeScalingModel.ApplyTrait(before, trait, sample.GetProperty("OwnerCardId").GetInt32(), original, kind);
            Require(result.Supported && JsonSerializer.Serialize(result.Upgrade) == JsonSerializer.Serialize(actual) &&
                JsonSerializer.Serialize(result.Context) == JsonSerializer.Serialize(after), "Independent spawn callback differs.");
            spawn += kind == "OnSpawn" ? 1 : 0; unscaled += kind == "OnUnscaledSpawn" ? 1 : 0;
            bonus += trait.Query.Type == "AnyMonsterSpawned" && actual.Stats.Health > original.Stats.Health ? 1 : 0;
            paid += trait.Query.Type == "PlayedCost" && actual.Stats.Health > original.Stats.Health ? 1 : 0;
        }
        Require(spawn >= 8 && (lethal || unscaled > 0) && bonus > 0 && paid > 0,
            "Native summon callbacks lack cost/spawn/order coverage.");
        int generatedFromSummons = 0;
        foreach (JsonElement record in fixture.GetProperty("CardGenerations").EnumerateArray())
        {
            CombatContext before = record.GetProperty("Before").Deserialize<CombatContext>(ModelJson.Options)!;
            CombatContext actual = record.GetProperty("Actual").Deserialize<CombatContext>(ModelJson.Options)!;
            CardGenerationRule rule = record.GetProperty("Rule").Deserialize<CardGenerationRule>(ModelJson.Options)!;
            int sourceId = record.GetProperty("SourceCardId").GetInt32();
            var predicted = CardGenerationModel.Apply(before, rule, sourceId);
            Require(predicted.Supported && record.GetProperty("Difference").ValueKind == JsonValueKind.Null &&
                ModelJson.Difference(JsonSerializer.Serialize(predicted.Context), JsonSerializer.Serialize(actual)) == null,
                "Independent native spawn generation differs.");
            if (record.GetProperty("Origin").GetString() == "Unit" && sourceId > 0 && rule.Count > 0)
                generatedFromSummons += predicted.AddedCards.Count;
        }
        Require(generatedFromSummons > 0, "Native summon captures lack source-owned card generation.");
        EnemySpawnState[] spawns = fixture.GetProperty("Spawns").EnumerateArray().Select(record =>
            record.GetProperty("Actual").Deserialize<EnemySpawnState>(ModelJson.Options)!).ToArray();
        Require(spawns.SelectMany(state => state.Train.Rooms).SelectMany(room => room.Units).Any(unit => unit.Team == CombatTeam.Enemy &&
            unit.Modifiers!.Upgrades.Count(upgrade => upgrade.AssetKey == "PojuSpawnWaveRoom") > 1 &&
            unit.Triggers.Any(trigger => trigger.Kind == "OnSpawnNotFromCard" && trigger.HasTriggered)),
            "Native wave captures lack batched room upgrades and cardless settlement.");
        if (lethal)
        {
            BattleTurnState[] actions = fixture.GetProperty("Actions").EnumerateArray().Select(record =>
                record.GetProperty("Actual").Deserialize<BattleTurnState>(ModelJson.Options)!).ToArray();
            Require(actions.Any(state => state.Spawn.Train.Context!.Statistics!.MonstersDeadThisBattle > 0 &&
                state.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.DataId == "d14a50f3-728d-43e1-87f0-ef1b013f6678") &&
                state.OtherPiles.Single(pile => pile.Name == "Standby").FreeSlots!.Count > 0), "Native transient summon death lacks exhausted/slot history coverage.");
        }
        Console.WriteLine($"NATIVE-SPAWN-TRIGGER-CHECKS PASS: {spawn} spawn and {unscaled} unscaled callbacks, {bonus} spawn-count and {paid} paid-cost bonuses, {generatedFromSummons} summon-generated cards; batched/cardless enemies and lethal={lethal}.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
