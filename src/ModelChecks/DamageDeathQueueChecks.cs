using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class DamageDeathQueueChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(108);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 4, 10,
            statistics: BattleStatistics.Empty().TrackCards([1, 2, 3]),
            cardInstances: [CardInstanceState.Empty(1, "owner"), CardInstanceState.Empty(2, "second"), CardInstanceState.Empty(3, "third")],
            otherPiles: [new("Standby", [new(1, "owner"), new(2, "second"), new(3, "third")]), new("Exhausted", [])]);
        CombatEffect Damage(int amount, string target = "Room", bool enemies = true) => new("CardEffectDamage", amount, 0, "", 0, [], false,
            action: new("Damage", target, amount, enemies, !enemies, []));
        var heal = new CombatEffect("CardEffectHeal", 0, 0, "", 0, [], false, action: new("Heal", "Self", 0, false, true, []));
        var armor = new CardUpgradeModifier("armor-after-heal", "armor-after-heal", new(),
            [new("armor", 7, 1, removeWhenTriggered: true)], false, false, false, 0, 0, []);
        var upgrade = new CombatEffect("CardEffectAddTempCardUpgradeToUnits", 0, 0, "", 0, [], false,
            unitUpgrade: new("UnitUpgrade", "Self", 0, false, true, [], armor, "TemporaryUntilUnitDeath"));
        CombatEffect Gold(int amount = 1) => new("CardEffectRewardGold", amount, 0, "", 0, [], false);
        CombatTrigger Trigger(string kind, params CombatEffect[] effects) => new(kind, false, false, false, 1, effects, false);
        CombatUnit Player(int id, int hp, params CombatTrigger[] triggers) => new(id, "player", CombatTeam.Player,
            0, hp, hp, true, false, false, [], triggers, id, modifiers: new(0, 0, 0, 1, 1, true, false, []), isBoss: false);
        CombatUnit Enemy(int id, params CombatTrigger[] triggers) => new(id, "enemy", CombatTeam.Enemy,
            0, 1, 1, true, false, false, [], triggers, modifiers: new(0, 0, 0, 1, 1, true, false, []), isBoss: false);
        RoomCombatState Room(params CombatUnit[] units) => new(0, false, units, [], context);
        RoomCombatResult Run(RoomCombatState room) => RoomCombatModel.ApplyPreCombat(room, 1);
        var root = Room(Player(1, 10, Trigger("PreCombat", Damage(1), heal), Trigger("OnHeal", upgrade)),
            Enemy(4, Trigger("OnDeath", Damage(3, enemies: false))));
        string parent = JsonSerializer.Serialize(root);
        var result = Run(root);
        Require(result.Supported && result.State!.Units.Single().Health == 10 && result.State.Units.Single().Statuses.Single(s => s.Id == "armor").Stacks == 4,
            "Damage death ran before a pending ordinary OnHeal callback: " + result.UnsupportedReason);
        Require(result.Events.Where(e => e.Kind == "TriggeredHeal" || e.Kind == "TriggeredDamage").Select(e => e.Amount).SequenceEqual([1, 0, 0]),
            "Damage/heal/death event order or armor consumption differs.");
        var trainRoot = new TrainCombatState([root, new(1, false,
            [Player(2, 10, Trigger("PreCombat", Gold()))], [], context)], [], 5, context);
        var train = TrainCombatModel.PreCombat(trainRoot, CombatTeam.Player);
        Require(train.Supported && train.State!.Rooms[0].Units.Single().Health == 10 &&
            train.State.Rooms[0].Units.Single().Statuses.Single(s => s.Id == "armor").Stacks == 4 && train.State.Context!.Gold == 5,
            "Train-wide ordinary callbacks did not drain before damage removal.");

        // Target order is 5,4,3,1. Native removal snapshots enemy then player, creation order within each team.
        var batchRoot = Room(Enemy(5, Trigger("OnDeath", Gold())), Enemy(4, Trigger("OnDeath", Gold())),
            Player(3, 1, Trigger("OnDeath", Gold())), Player(1, 1,
                Trigger("PreCombat", Damage(9), Damage(9, enemies: false)), Trigger("OnDeath", Gold())));
        var batch = Run(batchRoot);
        Require(batch.Supported && batch.Events.Where(e => e.Kind == "Gold").Select(e => e.Actor).SequenceEqual([4, 5, 1, 3]) &&
            batch.State!.Context!.OtherPiles!.Single(p => p.Name == "Exhausted").Cards.Select(c => c.InstanceId).SequenceEqual([1, 3]),
            "Damage removal followed physical target order instead of native team/creation order.");

        // Both 1 and 2 enter the initial removal batch. Unit 1's death kills 3. Nested removal
        // returns 3 first, then its parent 1; already-marked unit 2 remains in the original batch.
        var nestedRoot = Room(Player(1, 1, Trigger("PreCombat", Damage(1, enemies: false)),
                Trigger("OnDeath", Gold(), Damage(9, enemies: false))),
            Player(2, 1, Trigger("OnDeath", Gold())), Player(3, 2, Trigger("OnDeath", Gold())));
        var nested = Run(nestedRoot);
        Require(nested.Supported && nested.State!.Units.Count == 0 &&
            nested.Events.Where(e => e.Kind == "Gold").Select(e => e.Actor).SequenceEqual([1, 3, 2]) &&
            nested.State.Context!.OtherPiles!.Single(p => p.Name == "Exhausted").Cards.Select(c => c.InstanceId).SequenceEqual([3, 1, 2]),
            "Nested death removal included an already-marked unit or returned its parent too early.");
        var spawned = new CombatEffect("CardEffectAddBattleCard", 0, 0, "HandPile", 1, ["junk"], false,
            generation: new("HandPile", 1, [new("junk", CardModifiers.Empty(), null, [])]));
        var deathCard = Run(Room(Player(1, 1, Trigger("PreCombat", Damage(9, "Self")), Trigger("OnDeath", spawned))));
        Require(deathCard.Supported && deathCard.State!.Context!.Cards.Hand.Single().InstanceId == 4 &&
            deathCard.State.Context.Statistics!.Values.Any(v => v.CardId == 4 && v.Duration == "ThisBattle" && v.Type == "AnyExhausted" && v.Value == 1),
            "A death-generated card missed the subsequent spawner exhaustion.");

        // Exercise the local engine, which does not use the train-wide callback dispatcher.
        var localRoot = Room(Player(1, 10, Trigger("OnSpawn", Damage(1), heal), Trigger("OnHeal", upgrade)),
            Enemy(4, Trigger("OnDeath", Damage(3, enemies: false))));
        var local = RoomCombatModel.ApplySpawnTriggers(localRoot, 1, false);
        Require(local.Supported && local.State!.Units.Single().Health == 10 && local.State.Units.Single().Statuses.Single(s => s.Id == "armor").Stacks == 4,
            "Local spawn queue ran death effects before OnHeal.");
        var localNested = RoomCombatModel.ApplySpawnTriggers(Room(
            Player(1, 1, Trigger("OnSpawn", Damage(1, enemies: false)), Trigger("OnDeath", Gold(), Damage(9, enemies: false))),
            Player(2, 1, Trigger("OnDeath", Gold())), Player(3, 2, Trigger("OnDeath", Gold()))), 1, false);
        Require(localNested.Supported && localNested.Events.Where(e => e.Kind == "Gold").Select(e => e.Actor).SequenceEqual([1, 3, 2]) &&
            localNested.State!.Context!.OtherPiles!.Single(p => p.Name == "Exhausted").Cards.Select(c => c.InstanceId).SequenceEqual([3, 1, 2]),
            "Local nested removal order or parent spawner return differs.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(Run(root)) == JsonSerializer.Serialize(result) &&
            JsonSerializer.Serialize(Run(nestedRoot)) == JsonSerializer.Serialize(nested), "Parallel removal branches differ."));
        Require(JsonSerializer.Serialize(root) == parent, "Removal simulation mutated its parent.");
        Console.WriteLine("DAMAGE-DEATH-QUEUE PASS: ordinary callbacks before removal, enemy/player creation batches, nested removal/returns, death-generated exhaustion, local/global queues and 32 parallel branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out var scenario) || scenario.GetString() is not ("damage-death-queue" or "terminal-death-damage")) return;
        int phases = 0, deathArmor = 0, zeroHeals = 0, upgrades = 0;
        string Comparable(TrainCombatState state) => JsonSerializer.Serialize(new
        { state.Rooms, Movement = state.Movement.OrderBy(rule => rule.UnitId), state.EnemySlotsPerRoom, state.Context });
        foreach (FixtureValue phase in fixture.GetProperty("PreCombats").EnumerateArray())
        {
            var before = phase.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var after = phase.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            var result = TrainCombatModel.PreCombat(before, (CombatTeam)phase.GetProperty("Team").GetInt32());
            Require(result.Supported && phase.GetProperty("Difference").ValueKind == FixtureKind.Null &&
                ModelJson.Difference(Comparable(result.State!), Comparable(after)) == null, "Independent native damage-death queue phase differs.");
            phases++;
        }
        foreach (FixtureValue sample in fixture.GetProperty("TriggeredDamage").EnumerateArray())
        {
            Require(sample.GetProperty("Completed").GetBoolean() && sample.GetProperty("Sampled").GetBoolean() &&
                sample.GetProperty("Difference").ValueKind == FixtureKind.Null, "Damage-death damage observation is incomplete.");
            if (sample.GetProperty("Stage").GetString() != "Application" || sample.GetProperty("TriggerKind").GetString() != "OnDeath") continue;
            foreach (FixtureValue request in sample.GetProperty("Requests").EnumerateArray())
            {
                var unit = request.GetProperty("Before").Deserialize<CombatUnit>()!;
                if (unit.Modifiers?.Upgrades.Any(u => u.AssetKey == "PojuOnHealBeforeDeathArmor") != true || !unit.Statuses.Any(s => s.Id == "armor")) continue;
                Require(request.GetProperty("Amount").GetInt32() == 3 && unit.Health == request.GetProperty("AfterHealth").GetInt32(),
                    "Native OnHeal armor did not block subsequent OnDeath damage.");
                deathArmor++;
            }
        }
        foreach (FixtureValue sample in fixture.GetProperty("TriggeredHeals").EnumerateArray())
        {
            Require(sample.GetProperty("Completed").GetBoolean() && sample.GetProperty("Sampled").GetBoolean() &&
                sample.GetProperty("Difference").ValueKind == FixtureKind.Null, "Damage-death healing observation is incomplete.");
            var action = sample.GetProperty("Effect").Deserialize<CardActionEffect>()!;
            UnityRng before = sample.GetProperty("BeforeRng").Deserialize<UnityRng>();
            RngDraw? draw = action.Range?.Sample(before);
            int amount = draw?.Value ?? action.Value;
            Require(amount == sample.GetProperty("Amount").GetInt32() &&
                (draw?.State ?? before).Equals(sample.GetProperty("SampledRng").Deserialize<UnityRng>()),
                "Independent damage-death heal quantity or RNG differs.");
            zeroHeals += sample.GetProperty("Amount").GetInt32() == 0 ? 1 : 0;
        }
        foreach (FixtureValue upgrade in fixture.GetProperty("UnitUpgradeScaling").EnumerateArray())
            upgrades += upgrade.GetProperty("TriggerKind").GetString() == "OnHeal" &&
                upgrade.GetProperty("BeforeUpgrade").GetProperty("AssetKey").GetString() == "PojuOnHealBeforeDeathArmor" ? 1 : 0;
        Require(phases >= 2 && deathArmor >= 2 && zeroHeals > 0 && upgrades > 0, "Native damage-death queue coverage is incomplete.");
        Console.WriteLine($"DAMAGE-DEATH-QUEUE-NATIVE PASS: phases={phases}, death-hits-after-heal-armor={deathArmor}, zero-heals={zeroHeals}, armor-callbacks={upgrades}.");
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
