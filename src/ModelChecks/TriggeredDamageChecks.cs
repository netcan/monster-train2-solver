using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class TriggeredDamageChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(107);
        var owner = new CardInstanceState(1, "owner", new(new(damage: 9), [], 0, []), CardModifiers.Empty(), 0, 0, 0, [],
            damageScalingTraits: [new(new("LastAttackDamageDealt"), 1, .5f, true)]);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 2, 10,
            statistics: BattleStatistics.Empty().TrackCards([1]), cardInstances: [owner],
            otherPiles: [new("Standby", [new(1, "owner")]), new("Exhausted", [])]);
        var gold = new CombatEffect("CardEffectRewardGold", 1, 0, "", 0, [], false);
        CombatEffect Damage(int value, string target = "Room", CardEffectRange? range = null, string? multiplier = null,
            CardEffectTests? tests = null, bool enemies = true) => new("CardEffectDamage", value, 0, "", 0, [], false,
                action: new("Damage", target, value, enemies, !enemies, [], tests: tests, range: range), damageStatusMultiplier: multiplier);
        CombatTrigger Trigger(CombatEffect[] effects, string kind = "PreCombat", bool once = false) => new(kind, once, false, false, 1, effects, false);
        CombatUnit Player(CombatTrigger[] triggers, CombatStatus[]? statuses = null, int hp = 30) => new(1, "actor", CombatTeam.Player,
            0, hp, 30, true, false, false, statuses ?? [], triggers, 1, modifiers: new(0, 0, 0, 1, 1, true, false, []), isBoss: false);
        CombatUnit Enemy(int id, int hp = 30, CombatStatus[]? statuses = null, CombatTrigger[]? triggers = null) => new(id, "enemy", CombatTeam.Enemy,
            0, hp, hp, true, false, false, statuses ?? [], triggers ?? [], isBoss: false);
        RoomCombatState Room(params CombatUnit[] units) => new(0, false, units, [], context);
        RoomCombatResult Run(RoomCombatState room) => RoomCombatModel.ApplyPreCombat(room, 1);
        var root = Room(Player([Trigger([Damage(3), Damage(3)])]), Enemy(2, 100)); string parent = JsonSerializer.Serialize(root);
        var scaled = Run(root);
        Require(scaled.Supported && scaled.State!.Units.Single(unit => unit.Id == 2).Health == 70 &&
            scaled.State.Context!.Statistics!.LastAttackDamageDealt == 18, "Trigger damage lost its attacker, explicit spawner source or live scaling statistics: " +
            (scaled.UnsupportedReason ?? JsonSerializer.Serialize(scaled.State)));
        // Use an owner without traits to isolate status and RNG behavior.
        context = new(context.Cards, rng, 0, 2, 10, statistics: BattleStatistics.Empty().TrackCards([1]),
            cardInstances: [CardInstanceState.Empty(1, "owner")]);
        var armor = new CombatStatus("armor", 10, removeWhenTriggered: true);
        var shield = new CombatStatus("damage shield", 1, removeWhenTriggered: true);
        var pierce = Run(Room(Player([Trigger([Damage(4)])], [new("piercing", 1), new("lifesteal", 1)], hp: 10),
            Enemy(2, statuses: [armor, new("melee weakness", 2), new("spikes", 10)]), Enemy(3, statuses: [shield, armor])));
        Require(pierce.State!.Units.Single(unit => unit.Id == 2).Health == 26 && pierce.State.Units.Single(unit => unit.Id == 2).Statuses.Single(s => s.Id == "armor").Stacks == 10 &&
            pierce.State.Units.Single(unit => unit.Id == 3).Health == 30 && !pierce.State.Units.Single(unit => unit.Id == 3).Statuses.Any(s => s.Id == "damage shield") &&
            pierce.State.Units.Single(unit => unit.Id == 1).Health == 10 && pierce.State.Units.Single(unit => unit.Id == 1).Statuses.Single(s => s.Id == "lifesteal").Stacks == 1,
            "Default damage piercing/shield, melee weakness, spikes or lifesteal semantics differ.");
        var range = new CardEffectRange(0, 7, .5f);
        var randomized = Run(Room(Player([Trigger([Damage(0, "RandomInRoom", range)])]), Enemy(2), Enemy(3)));
        RngDraw preflight = range.Sample(rng), test = range.Sample(preflight.State), target = test.State.Range(0, 2), value = range.Sample(target.State);
        Require(randomized.Supported && randomized.State!.Context!.BattleRng.Equals(value.State) && randomized.State.Units.Where(unit => unit.Team == CombatTeam.Enemy)
            .All(unit => unit.Health == 30 - (unit.Id == target.Value + 2 ? value.Value : 0)), "Damage preflight/runtime-test/target/application RNG order differs.");
        var empty = Run(Room(Player([Trigger([Damage(0, range: range)], once: true)])));
        RngDraw emptyTest = range.Sample(range.Sample(rng).State), emptyValue = range.Sample(emptyTest.State);
        Require(empty.State!.Context!.BattleRng.Equals(emptyValue.State) && empty.State.Units.Single().Triggers.Single().HasTriggered,
            "An empty damage target set skipped testing or application samples.");
        var negative = new CardEffectRange(2, 2, -.5f);
        var failed = Run(Room(Player([Trigger([Damage(0, range: negative)], once: true)])));
        Require(failed.State!.Context!.BattleRng.Equals(negative.Sample(rng).State) && !failed.State.Units.Single().Triggers.Single().HasTriggered,
            "Negative damage preflight consumed the wrong sample or marked the trigger.");
        var cancellation = Run(Room(Player([Trigger([gold, Damage(0, range: negative, tests: new(true, false, true, false)), gold])])));
        Require(cancellation.State!.Context!.Gold == 5 && cancellation.State.Context.BattleRng.Equals(negative.Sample(negative.Sample(rng).State).State),
            "Runtime damage test did not cancel subsequent effects.");
        var noTest = Run(Room(Player([Trigger([Damage(0, range: range, tests: new(false, false, false, false))], once: true)])));
        Require(!noTest.State!.Units.Single().Triggers.Single().HasTriggered && noTest.State.Context!.BattleRng.Equals(rng), "Disabled-only tests activated a trigger.");
        var mixed = Run(Room(Player([Trigger([gold, Damage(0, range: range, tests: new(false, false, false, false))])]), Enemy(2)));
        RngDraw mixedTest = range.Sample(rng), mixedValue = range.Sample(mixedTest.State);
        Require(mixed.State!.Context!.BattleRng.Equals(mixedValue.State) && mixed.State.Units.Single(unit => unit.Id == 2).Health == 30 - mixedValue.Value,
            "Test-disabled effects skipped their mandatory runtime test or sampled during preflight.");
        var maxZero = new CardEffectRange(0, 0, 1);
        var invalidRange = Run(Room(Player([Trigger([Damage(0, range: maxZero)], once: true)])));
        Require(!invalidRange.State!.Units.Single().Triggers.Single().HasTriggered && invalidRange.State.Context!.BattleRng.Equals(maxZero.Sample(rng).State),
            "Zero maximum damage range bypassed its native gate.");
        var multiplied = Run(Room(Player([Trigger([Damage(2, multiplier: "regen")])], [new("regen", 3)]), Enemy(2, statuses: [new("regen", 9)])));
        Require(multiplied.State!.Units.Single(unit => unit.Id == 2).Health == 24, "Damage multiplier read target rather than actor stacks.");
        var zeroScaled = Run(Room(Player([Trigger([Damage(999, multiplier: "regen")])]), Enemy(2)));
        var overflow = Run(Room(Player([Trigger([Damage(int.MaxValue, multiplier: "regen")])], [new("regen", 2)]), Enemy(2)));
        Require(zeroScaled.State!.Units.Single(unit => unit.Id == 2).Health == 30 && overflow.State!.Units.Single(unit => unit.Id == 2).Health == 30,
            "Missing stacks or unchecked status multiplication differ.");
        var deadSelf = Run(Room(Player([Trigger([Damage(99, "Self"), Damage(99, "Self"), gold]), Trigger([gold], "OnDeath")])));
        Require(deadSelf.State!.Units.Count == 0 && deadSelf.State.Context!.Gold == 10 && deadSelf.Events.Count(e => e.Kind == "Death") == 1,
            "A sequence stopped after self death, applied damage to a dead unit or duplicated its death callback.");
        var noContext = new RoomCombatState(0, false, [Player([Trigger([Damage(0, range: range)])])], []);
        Require(!Run(noContext).Supported, "Random damage accepted missing shared battle context.");
        context = new(context.Cards, rng, 0, 2, 10, statistics: BattleStatistics.Empty().TrackCards([1]),
            cardInstances: [CardInstanceState.Empty(1, "owner")], otherPiles: [new("Standby", [new(1, "owner")]), new("Exhausted", [])]);
        var generate = new CombatEffect("CardEffectAddBattleCard", 0, 0, "HandPile", 1, ["junk"], false,
            generation: new("HandPile", 1, [new("junk", CardModifiers.Empty(), null, [])]));
        var lateCard = Run(Room(Player([Trigger([Damage(99, "Self"), generate]), Trigger([gold], "OnDeath")])));
        Require(lateCard.Supported && lateCard.State!.Context!.Cards.Hand.Single().InstanceId == 2 &&
            lateCard.State.Context.Statistics!.Values.Any(v => v.CardId == 2 && v.Duration == "ThisBattle" && v.Type == "AnyExhausted" && v.Value == 1) &&
            lateCard.State.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Single().InstanceId == 1,
            "Triggered damage returned/exhausted its spawner before late queue generation.");
        context = new(context.Cards, rng, 0, 3, 10, statistics: BattleStatistics.Empty().TrackCards([1, 2]),
            cardInstances: [CardInstanceState.Empty(1, "owner"), CardInstanceState.Empty(2, "owner")],
            otherPiles: [new("Standby", [new(1, "owner"), new(2, "owner")]), new("Exhausted", [])]);
        var younger = new CombatUnit(2, "younger", CombatTeam.Player, 0, 30, 30, true, false, false, [], [], 2,
            modifiers: new(0, 0, 0, 1, 1, true, false, []), isBoss: false);
        var removalOrder = Run(Room(younger, Player([Trigger([Damage(99, enemies: false)])])));
        Require(removalOrder.Supported && removalOrder.State!.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Select(card => card.InstanceId).SequenceEqual([1, 2]),
            "Dead spawners returned in damage target order rather than active character creation order.");
        var death = Run(Room(Player([Trigger([Damage(99), Damage(1, "Self")]), Trigger([Damage(99, "Self"), gold], "OnDeath")]),
            Enemy(2, 1, triggers: [Trigger([Damage(2, "Room", enemies: false), gold], "OnDeath")])));
        Require(death.Supported && death.State!.Units.Count == 1 && death.State.Units.Single().Health == 27 && death.State.Context!.Gold == 5 &&
            death.Events.Where(e => e.Kind == "TriggeredDamage").Select(e => e.Amount).SequenceEqual([99, 1, 2]),
            "Death-trigger damage did not retain its attacker, skip dead targets or follow FIFO ordering.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(Run(root).State) == JsonSerializer.Serialize(scaled.State), "Parallel damage branches differ."));
        Require(JsonSerializer.Serialize(root) == parent, "Damage simulation mutated its parent.");
        Console.WriteLine("TRIGGERED-DAMAGE PASS: actor traits/statuses, shield/armor, three amount samples, random targets, empty/negative/test gates, death FIFO and 32 parallel branches.");
    }
    internal static void Native(JsonElement fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out var scenario) || scenario.GetString() is not ("triggered-damage" or "damage-death-queue" or "terminal-death-damage")) return;
        bool removalQueue = scenario.GetString() is "damage-death-queue" or "terminal-death-damage";
        int phases = 0, samples = 0, applications = 0, tests = 0, negativeTests = 0, empty = 0, groups = 0, random = 0,
            multipliers = 0, deaths = 0, shields = 0, armor = 0, sourceModifiers = 0, damageHits = 0, lateExhausted = 0;
        string Comparable(TrainCombatState state) => JsonSerializer.Serialize(new
        { state.Rooms, Movement = state.Movement.OrderBy(rule => rule.UnitId), state.EnemySlotsPerRoom, state.Context });
        foreach (JsonElement phase in fixture.GetProperty("PreCombats").EnumerateArray())
        {
            var before = phase.GetProperty("Before").Deserialize<TrainCombatState>(ModelJson.Options)!;
            var after = phase.GetProperty("Actual").Deserialize<TrainCombatState>(ModelJson.Options)!;
            var result = TrainCombatModel.PreCombat(before, (CombatTeam)phase.GetProperty("Team").GetInt32());
            Require(result.Supported && phase.GetProperty("Difference").ValueKind == JsonValueKind.Null &&
                ModelJson.Difference(Comparable(result.State!), Comparable(after)) == null, "Independent native triggered-damage phase differs.");
            sourceModifiers += before.Context!.CardInstances!.Count(card => card.Permanent.Offsets.Damage == 9 ||
                card.Permanent.Upgrades.Any(upgrade => upgrade.Stats.Damage == 9));
            lateExhausted += after.Context!.Statistics!.Values.Count(value => value.CardId >= before.Context.NextCardId &&
                value.Duration == "ThisBattle" && value.Type == "AnyExhausted" && value.Value > 0);
            phases++;
        }
        foreach (JsonElement sample in fixture.GetProperty("TriggeredDamage").EnumerateArray())
        {
            Require(sample.GetProperty("Completed").GetBoolean() && sample.GetProperty("Sampled").GetBoolean() &&
                sample.GetProperty("Difference").ValueKind == JsonValueKind.Null, "Native triggered-damage observation is incomplete.");
            CardActionEffect effect = sample.GetProperty("Effect").Deserialize<CardActionEffect>(ModelJson.Options)!;
            UnityRng before = sample.GetProperty("BeforeRng").Deserialize<UnityRng>(ModelJson.Options);
            RngDraw? draw = effect.Range?.Sample(before);
            int value = draw?.Value ?? effect.Value;
            Require(value == sample.GetProperty("Amount").GetInt32() && (draw?.State ?? before).Equals(sample.GetProperty("SampledRng").Deserialize<UnityRng>(ModelJson.Options)),
                "Independent native triggered-damage sample/RNG differs.");
            int[] targets = sample.GetProperty("Targets").Deserialize<int[]>()!;
            samples++;
            if (sample.GetProperty("Stage").GetString() == "Test")
            {
                bool passed = value >= 0 && (effect.Range == null || effect.Range.Max > 0) && (effect.Target != "DropTargetCharacter" || targets.Length > 0);
                Require(passed == sample.GetProperty("TestPassed").GetBoolean(), "Independent native triggered-damage test differs.");
                tests++; negativeTests += value < 0 && !passed ? 1 : 0; continue;
            }
            applications++; empty += targets.Length == 0 && effect.Range != null ? 1 : 0; groups += targets.Length > 1 ? 1 : 0;
            random += effect.Target == "RandomInRoom" ? 1 : 0; deaths += sample.GetProperty("TriggerKind").GetString() == "OnDeath" ? 1 : 0;
            bool multiplied = sample.GetProperty("StatusMultiplier").ValueKind == JsonValueKind.String;
            int amount = multiplied ? unchecked(value * sample.GetProperty("MultiplierStacks").GetInt32()) : value;
            multipliers += multiplied && sample.GetProperty("MultiplierStacks").GetInt32() > 0 ? 1 : 0;
            int lastTargetPosition = -1;
            foreach (JsonElement request in sample.GetProperty("Requests").EnumerateArray())
            {
                int targetId = request.GetProperty("TargetId").GetInt32(); int position = Array.IndexOf(targets, targetId);
                Require(position > lastTargetPosition && request.GetProperty("Amount").GetInt32() == amount &&
                    request.GetProperty("AttackerPreserved").GetBoolean() && request.GetProperty("DefaultDamage").GetBoolean() &&
                    request.GetProperty("SourceCardId").GetInt32() == sample.GetProperty("ActorCardId").GetInt32(),
                    "Native triggered damage changed group order, actor, damage type or quantity.");
                lastTargetPosition = position;
                var target = request.GetProperty("Before").Deserialize<CombatUnit>(ModelJson.Options)!;
                var context = request.GetProperty("Context").Deserialize<CombatContext>(ModelJson.Options)!;
                var scaled = DamageScalingModel.Apply(context, sample.GetProperty("ActorCardId").GetInt32(), request.GetProperty("SourceCardId").GetInt32(), amount);
                Require(scaled.Supported, "Native triggered damage source scaling is unsupported.");
                int damage = Math.Max(0, scaled.Damage);
                CombatStatus? Status(string id) => target.Statuses.FirstOrDefault(status => status.Id == id && status.Stacks > 0);
                if (Status("pyregel") is CombatStatus pyregel) damage += pyregel.ParamInt * pyregel.Stacks;
                if (damage > 0 && Status("damage shield") != null) { damage = 0; shields++; }
                if (damage > 0 && Status("armor") is CombatStatus block && !sample.GetProperty("ActorPiercing").GetBoolean())
                { damage = Math.Max(0, damage - block.ParamInt * block.Stacks); armor++; }
                if (damage > 0 && Status("fragile") != null) damage = target.Health + damage - 1;
                if (Status("untouchable") != null) damage = 0;
                Require(Math.Max(0, target.Health - damage) == request.GetProperty("AfterHealth").GetInt32(), "Independent native triggered-damage health differs.");
                damageHits++;
            }
        }
        Require(phases >= 2 && applications > 0 && tests > applications && deaths > 0 && armor > 0 && damageHits > 0 &&
            (removalQueue || negativeTests > 0 && empty > 0 && groups > 0 && random > 0 && multipliers > 0 && shields > 0 && sourceModifiers > 0 && lateExhausted > 0),
            "Native triggered-damage coverage is incomplete.");
        Console.WriteLine($"TRIGGERED-DAMAGE-NATIVE PASS: phases={phases}, samples={samples}, applications={applications}, tests={tests}, negative-tests={negativeTests}, empty-ranges={empty}, groups={groups}, random={random}, status-multipliers={multipliers}, death-effects={deaths}, shields={shields}, armor={armor}, source-offsets={sourceModifiers}, hits={damageHits}, late-card-exhaustion={lateExhausted}.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
