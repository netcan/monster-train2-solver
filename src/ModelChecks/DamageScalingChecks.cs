using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class DamageScalingChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(173);
        var traits = new ScalingDamageTrait[] { new(new("AnyHeroKilled", "ThisBattle"), 3, .5f, false),
            new(new("AnyCharacter"), 2, .75f, true), new(new("MagicPowerInTargetRoom"), 100, 1, true) };
        var owner = new CardInstanceState(1, "owner", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [], damageScalingTraits: traits);
        var source = new CardInstanceState(2, "spell", new(new(damage: -3), [], 0, []), new(new(damage: 2), [], 0, []), 0, 0, 0, []);
        var statistics = new BattleStatistics([new(1, "ThisBattle", "HeroesKilled", 4), new(99, "ThisBattle", "HeroesKilled", 100)],
            [], [], [], [], [], [], 0, 0, 0, 0, 0, [1, 2, 99], [1, 2]);
        var context = new CombatContext(new([new(2, "spell")], [], [], rng, 0, []), rng, 0, 3, 10,
            statistics: statistics, cardInstances: [owner, source], otherPiles: [new("Standby", [new(1, "owner")]), new("Exhausted", [])]);
        string parent = JsonSerializer.Serialize(context);
        DamageScalingResult spell = DamageScalingModel.Apply(context, 1, 2, 999);
        Require(spell.Supported && spell.Damage == 13 && spell.Context!.Statistics!.TrackedCards.SequenceEqual([1, 2]),
            "Trait order, replacement/addition, fractional flooring, per-group upgrade floors or membership refresh differ.");
        Require(DamageScalingModel.Apply(context, 1, 0, 999).Damage == 7,
            "A spawner's numeric damage upgrades were added without an explicit damage-source card.");
        var fractional = DamageScalingModel.ApplyTrait(context, new(new("AnyCharacter"), 3, -.5f, false), 1, 0, 0);
        Require(fractional.Supported && fractional.Damage == -2, "Signed fractional scaling truncated instead of flooring.");
        var magic = DamageScalingModel.ApplyTrait(new(context.Cards, rng, 0, 3, 10), traits[2], 1, 0, 8);
        Require(magic.Supported && magic.Damage == 8, "Magic power's special zero path unexpectedly queried incomplete statistics.");
        Require(DamageScalingModel.ApplyTrait(context, new(new("AnyCharacter"), int.MaxValue, 1, false), 1, 0, 0).Supported == false &&
            !DamageScalingModel.ApplyTrait(context, new(new("CurrentCost"), 1, 1, false), 1, 0, 0).Supported &&
            !DamageScalingModel.ApplyTrait(context, new(new("Gold"), 1, 1, false), 1, 0, 0).Supported &&
            !DamageScalingModel.Apply(context, 1, 77, 0).Supported,
            "Uncaptured query inputs, numeric domains or source identities returned partial transitions.");
        var growing = new CardInstanceState(1, "owner", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            damageScalingTraits: [new(new("LastAttackDamageDealt"), 1, .75f, true)]);
        var attackContext = new CombatContext(context.Cards, rng, 0, 3, 10, statistics: statistics,
            cardInstances: [growing, source], otherPiles: context.OtherPiles);
        var enemy = new CombatUnit(10, "enemy", CombatTeam.Enemy, 0, 100, 100, true, false, false, []);
        var attacker = new CombatUnit(11, "attacker", CombatTeam.Player, 4, 20, 20, true, false, false,
            [new("multistrike", 1, 2)], spawnerCardId: 1);
        RoomCombatResult exchange = RoomCombatModel.Exchange(new(0, false, [enemy, attacker], [], attackContext));
        Require(exchange.Supported && exchange.State!.Units[0].Health == 89 &&
            exchange.State.Context!.Statistics!.LastAttackDamageDealt == 7,
            "Successive attacks failed to query the preceding hit's updated statistic.");
        var boss = new CombatUnit(10, "boss", CombatTeam.Enemy, 0, 100, 100, true, false, false, [], isBoss: true);
        RoomCombatResult bossExchange = RoomCombatModel.Exchange(new(0, false, [boss, attacker], [], attackContext));
        Require(bossExchange.Supported && bossExchange.State!.Units[0].Health == 83 &&
            bossExchange.State.Context!.Statistics!.LastAttackDamageDealt == 10,
            "The per-hit boss preview failed to carry its statistic back before the live scaling query.");
        RoomCombatResult shield = RoomCombatModel.ApplyCardDamage(new(0, false,
            [new(10, "shield", CombatTeam.Enemy, 0, 100, 100, false, false, false,
                [new("damage shield", 1, removeWhenTriggered: true)])], [], context), 10, 0, 1);
        Require(shield.Supported && shield.State!.Units.Single().Health == 100 && shield.State.Units.Single().Statuses.Count == 0,
            "Defenses ran before scaling created positive damage from a zero base.");
        RoomCombatResult unsupported = RoomCombatModel.ApplyCardDamage(new(0, false, [enemy], [],
            new(context.Cards, rng, 0, 3, 10, statistics: statistics, cardInstances:
                [new(1, "owner", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
                    damageScalingTraits: [new(new("Gold"), 1, 1, true)])], otherPiles: context.OtherPiles)), 10, 1, 1);
        Require(!unsupported.Supported && unsupported.State == null, "Unsupported damage became a usable room state.");
        Require(owner.OnDiscard(true).DamageScalingTraits!.Count == 3,
            "Discarding/playing a card lost its damage traits.");
        var upgrade = new CardUpgradeModifier("scaling-upgrade", "scaling-upgrade", new(damage: 3), [], false, false, false, 0, 0, []);
        var upgradeTarget = new CombatUnit(11, "upgraded", CombatTeam.Player, 4, 20, 20, true, false, false, [],
            spawnerCardId: 1, size: 1, modifiers: new(4, 0, 0, 1, 1, true, false, []));
        var upgradeRoom = new RoomCombatState(0, false, [upgradeTarget], [], context);
        RoomCombatResult unitUpgrade = UnitModifierModel.Apply(upgradeRoom, 11, upgrade, "TemporaryUntilEndOfBattle");
        Require(unitUpgrade.Supported && DamageScalingModel.Apply(unitUpgrade.State!.Context, 1, 1, 999).Damage == 16,
            "A runtime unit upgrade erased traits or failed to affect later explicit-source scaling.");
        RoomCombatResult healthUpgrade = UnitHealthModel.Apply(upgradeRoom, 11, 4);
        Require(healthUpgrade.Supported && DamageScalingModel.Apply(healthUpgrade.State!.Context, 1, 0, 999).Damage == 7,
            "Changing spawner health erased damage traits.");
        var handContext = new CombatContext(new([new(1, "owner")], [], [], rng, 0, []), rng, 0, 3, 10,
            statistics: statistics, cardInstances: [owner, source], otherPiles: context.OtherPiles);
        RoomCombatResult handUpgrade = HandUpgradeModel.Apply(new(0, false, [], [], handContext), upgrade, "TemporaryUntilEndOfBattle",
            new([], [new("owner", "owner", 0, "Null", "Discard", null, [])]));
        Require(handUpgrade.Supported && DamageScalingModel.Apply(handUpgrade.State!.Context, 1, 1, 999).Damage == 16,
            "A hand upgrade erased traits or failed to affect later scaling.");
        var generation = CardGenerationModel.Apply(context, new("HandPile", 1,
            [new("generated", CardModifiers.Empty(), null, [], traits)]));
        Require(generation.Supported && generation.Context!.CardInstances!.Single(card => card.InstanceId == 3).DamageScalingTraits!.Count == 3,
            "Generated card creation lost its immutable damage traits.");
        var incomplete = new CombatContext(context.Cards, rng, 0, 3, 10);
        Require(!CardGenerationModel.Apply(incomplete, new("HandPile", 1, [new("generated", CardModifiers.Empty(), null, [], traits)])).Supported,
            "Generating a scaling card into a context without card metadata silently discarded its traits.");
        Parallel.For(0, 64, _ => Require(JsonSerializer.Serialize(DamageScalingModel.Apply(context, 1, 2, 999)) ==
            JsonSerializer.Serialize(spell), "Parallel damage scaling diverged."));
        Require(JsonSerializer.Serialize(context) == parent, "Scaling damage mutated its parent.");
        traits[0] = new(new("AnyCharacter"), 999, 1, false);
        Require(owner.DamageScalingTraits![0].DamagePerStat == 3, "Captured trait order retained the caller's mutable array.");
        Console.WriteLine("DAMAGE-SCALING-CHECKS PASS: trait order, replacement/addition, per-source modifiers, signed floors, per-hit updates, defenses, unsupported isolation, creation/discard and 64 parallel branches.");
    }

    internal static void Native(JsonElement fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out JsonElement scenario) ||
            scenario.GetString() is not ("damage-scaling" or "dynamic-statistics")) return;
        int count = 0;
        var queries = new HashSet<string>(); var types = new HashSet<string>();
        bool replacement = false, additive = false;
        foreach (JsonElement sample in fixture.GetProperty("DamageScaling").EnumerateArray())
        {
            Require(sample.GetProperty("CaptureError").ValueKind == JsonValueKind.Null && sample.GetProperty("Difference").ValueKind == JsonValueKind.Null,
                "Native scaling callback capture is incomplete or differs.");
            CombatContext before = sample.GetProperty("Before").Deserialize<CombatContext>(ModelJson.Options)!;
            CombatContext after = sample.GetProperty("After").Deserialize<CombatContext>(ModelJson.Options)!;
            ScalingDamageTrait trait = sample.GetProperty("Trait").Deserialize<ScalingDamageTrait>()!;
            string parent = JsonSerializer.Serialize(before);
            DamageScalingResult result = DamageScalingModel.ApplyTrait(before, trait, sample.GetProperty("OwnerCardId").GetInt32(),
                sample.GetProperty("DamageSourceCardId").GetInt32(), sample.GetProperty("IncomingDamage").GetInt32());
            Require(result.Supported && result.Damage == sample.GetProperty("ActualDamage").GetInt32() &&
                JsonSerializer.Serialize(result.Context) == JsonSerializer.Serialize(after), "Native scaling callback damage or refreshed context differs.");
            Require(JsonSerializer.Serialize(before) == parent, "Callback check mutated its captured root.");
            queries.Add(trait.Query.Type); types.Add(sample.GetProperty("DamageType").GetString()!);
            replacement |= !trait.AddToDamage; additive |= trait.AddToDamage; count++;
        }
        Require(count >= 60 && replacement && additive && queries.Contains("MagicPowerInTargetRoom") && queries.Contains("LastAttackDamageDealt") &&
            types.Contains("DirectAttack") && types.Contains("Default") && types.Contains("Spikes"), "Native scaling trace lacks spell/unit/retaliation or trait-mode coverage.");
        if (scenario.GetString() == "dynamic-statistics")
            Require(new[] { "Gold", "TurnCount", "MoonPhase", "ForgePoints", "DragonsHoardAmount", "EnergyRemainingEndOfTurn", "PyreHeartResurrection" }
                .All(queries.Contains), "Native scaling lacks dynamic resource queries.");
        Console.WriteLine($"NATIVE-DAMAGE-SCALING-CHECKS PASS: {count} callbacks, {queries.Count} queries, spell/unit damage and complete refreshed contexts.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
