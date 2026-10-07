using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class TriggeredHealingChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(96);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 2, 10,
            statistics: BattleStatistics.Empty().TrackCards([1]), cardInstances: [new(1, "owner", CardModifiers.Empty(),
                new(new(heal: 99), [], 0, []), 0, 0, 0, [])]);
        var gold = new CombatEffect("CardEffectRewardGold", 1, 0, "", 0, [], false);
        CombatEffect Heal(int amount, string target = "Self", bool enemies = false, CardEffectRange? range = null,
            CardTargetFilters? filters = null, CardEffectTests? tests = null) => new("CardEffectHeal", amount, 0, "", 0, [], false,
                action: new("Heal", target, amount, enemies, !enemies, [], tests: tests, range: range, filters: filters));
        CombatTrigger Trigger(CombatEffect[] effects, string kind = "PreCombat", bool once = false, bool ignored = false) =>
            new(kind, once, false, ignored, 1, effects, false);
        CombatUnit Unit(int id, CombatTrigger[] triggers, CombatStatus[]? statuses = null, bool healable = true,
            CombatTeam team = CombatTeam.Player) => new(id, "unit", team, 0, 2, 10, false, false, false, statuses ?? [],
                triggers, id == 1 ? 1 : 0, modifiers: new(0, 0, 0, 1, 1, healable, false, []), isBoss: false);
        RoomCombatState Room(params CombatUnit[] units) => new(0, false, units, [], context);
        RoomCombatResult Run(RoomCombatState room) => RoomCombatModel.ApplyPreCombat(room, 1);
        var onHeal = Trigger([gold], "OnHeal");
        var root = Room(Unit(1, [Trigger([Heal(2, enemies: true)]), onHeal]));
        string parent = JsonSerializer.Serialize(root); var result = Run(root);
        Require(result.Supported && result.State!.Units.Single().Health == 4 && result.State.Context!.Gold == 5,
            "Self team bypass, source-card heal modifiers or nested OnHeal differ.");
        var room = Room(Unit(1, [Trigger([Heal(3, "Room")]), onHeal], [new("heal multiplier", 1, 2)]),
            Unit(2, [onHeal], [new("heal immunity", 1)]), Unit(3, [onHeal], healable: false),
            Unit(4, [onHeal], [new("untouchable", 1)]), Unit(5, [], team: CombatTeam.Enemy));
        var area = Run(room);
        Require(area.Supported && area.State!.Units.Single(unit => unit.Id == 1).Health == 8 &&
            area.State.Units.Where(unit => unit.Id != 1).All(unit => unit.Health == 2) && area.State.Context!.Gold == 10,
            "Area heal multiplier, immunity zero-trigger, healability, untouchable or team filter differs.");
        var zero = Run(Room(Unit(1, [Trigger([Heal(0)]), onHeal])));
        var negative = Run(Room(Unit(1, [Trigger([Heal(0, range: new(2, 2, -.5f))]), onHeal])));
        var immuneNegative = Run(Room(Unit(1, [Trigger([Heal(0, range: new(2, 2, -.5f))]), onHeal], [new("heal immunity", 1)])));
        Require(zero.State!.Context!.Gold == 5 && negative.State!.Context!.Gold == 0 && immuneNegative.State!.Context!.Gold == 5,
            "Zero/negative healing or immunity-before-negative-check differs.");
        var clipped = Run(Room(Unit(1, [Trigger([Heal(999), Heal(1)]), onHeal])));
        Require(clipped.State!.Units.Single().Health == 10 && clipped.State.Context!.Gold == 10,
            "Clipped/full-health healing skipped OnHeal.");
        var recursive = Run(Room(Unit(1, [Trigger([Heal(1)]), Trigger([Heal(2), gold], "OnHeal", once: true)])));
        Require(recursive.Supported && recursive.State!.Units.Single().Health == 5 && recursive.State.Context!.Gold == 5,
            "Once-only healing was marked after recursive OnHeal.");
        var filtered = Run(Room(Unit(1, [Trigger([Heal(2, "Room", filters: new("Damaged", ["dazed"], [], false, "", []))])], [new("dazed", 1)]), Unit(2, [])));
        Require(filtered.State!.Units.Single(unit => unit.Id == 1).Health == 4 && filtered.State.Units.Single(unit => unit.Id == 2).Health == 2,
            "Healing target status/health filters or global daze timing differ.");
        var fail = Run(Room(Unit(1, [Trigger([Heal(1, "FrontInRoom", enemies: true, tests: new(true, true, false, false)), gold], once: true)])));
        var cancel = Run(Room(Unit(1, [Trigger([Heal(1, "FrontInRoom", enemies: true, tests: new(true, false, true, false)), gold])])));
        var skip = Run(Room(Unit(1, [Trigger([Heal(1, tests: new(false, false, false, false))], once: true)])));
        Require(fail.State!.Context!.Gold == 0 && !fail.State.Units.Single().Triggers.Single().HasTriggered &&
            cancel.State!.Context!.Gold == 0 && cancel.State.Units.Single().Triggers.Single().HasTriggered &&
            !skip.State!.Units.Single().Triggers.Single().HasTriggered, "Trigger preflight/fail-to-cast/runtime-cancel/test-disabled gates differ.");
        var emptyRange = new CardEffectRange(0, 7, .5f);
        var empty = Run(Room(Unit(1, [Trigger([Heal(0, "Room", enemies: true, range: emptyRange)], once: true)])));
        Require(empty.Supported && empty.State!.Context!.BattleRng.Equals(emptyRange.Sample(rng).State) &&
            empty.State.Units.Single().Triggers.Single().HasTriggered, "Empty Room healing failed its test or skipped range sampling.");
        var range = new CardEffectRange(-2, 5, .5f);
        var randomRoot = Room(Unit(1, [Trigger([Heal(0, "RandomInRoom", range: range)])]), Unit(2, []));
        var random = Run(randomRoot); RngDraw selected = rng.Range(0, 2); RngDraw sampled = range.Sample(selected.State);
        Require(random.Supported && random.State!.Context!.BattleRng.Equals(sampled.State) && random.State.Units.All(unit =>
            unit.Health == (unit.Id == selected.Value + 1 ? HealingModel.HealedHealth(2, 10, sampled.Value, true, []) : 2)),
            "Random target tests consumed RNG or amount was sampled before collection.");
        var group = Run(Room(Unit(1, [Trigger([Heal(0, "Room", range: range)])]), Unit(2, [])));
        RngDraw groupDraw = range.Sample(rng);
        Require(group.State!.Context!.BattleRng.Equals(groupDraw.State) && group.State.Units.All(unit =>
            unit.Health == HealingModel.HealedHealth(2, 10, groupDraw.Value, true, [])), "Group targets sampled separate healing amounts.");
        var silent = Run(Room(Unit(1, [Trigger([Heal(2)]), Trigger([Heal(1)], ignored: true)], [new("silenced", 1)])));
        Require(silent.State!.Units.Single().Health == 3, "Ignored-silence healing did not bypass silence.");
        var upgrade = new CardUpgradeModifier("nested", "nested", new(damage: 2, health: 1), [], false, false, false, 0, 0, []);
        var nestedUpgrade = new CombatEffect("CardEffectAddTempCardUpgradeToUnits", 0, 0, "", 0, [], false,
            unitUpgrade: new("UnitUpgrade", "Self", 0, false, true, [], upgrade, "TemporaryUntilUnitDeath"));
        var nested = Run(Room(Unit(1, [Trigger([Heal(1), Heal(1)]), Trigger([nestedUpgrade], "OnHeal")])));
        Require(nested.Supported && nested.State!.Units.Single().BaseAttack == 4 && nested.State.Units.Single().Health == 6 &&
            nested.State.Units.Single().MaxHealth == 12, "Nested OnHeal detached the working unit used by later healing.");
        var unhealed = new CardUpgradeModifier("deferred", "deferred", new(), [], false, false, false, 5, 0, []);
        var delayedEffect = new CombatEffect("CardEffectAddTempCardUpgradeToUnits", 0, 0, "", 0, [], false,
            unitUpgrade: new("UnitUpgrade", "Self", 0, false, true, [], unhealed, "TemporaryUntilUnitDeath"));
        var queuedRoot = Room(Unit(1, [Trigger([Heal(999), Heal(4)]), Trigger([delayedEffect], "OnHeal", once: true)]));
        var queued = Run(queuedRoot);
        Require(queued.Supported && queued.State!.Units.Single().Health == 10 && queued.State.Units.Single().MaxHealth == 15 &&
            queued.State.Units.Single().Modifiers!.Upgrades.Count == 1, "OnHeal settled before the rest of its triggering effect sequence.");
        var teamRoot = new TrainCombatState([queuedRoot, new(1, false, [], [], context)], [], 5, context);
        var teamQueued = TrainCombatModel.PreCombat(teamRoot, CombatTeam.Player);
        Require(teamQueued.Supported && teamQueued.State!.Rooms[0].Units.Single().Health == 10 &&
            teamQueued.State.Rooms[0].Units.Single().MaxHealth == 15, "Shared team queue did not defer OnHeal after its actor.");
        var fifoRoot = Room(Unit(1, [Trigger([Heal(0)]), Trigger([delayedEffect], "OnHeal", once: true)]),
            Unit(2, [Trigger([Heal(999, "Room")])], healable: false));
        var fifo = TrainCombatModel.PreCombat(new([fifoRoot, new(1, false, [], [], context)], [], 5, context), CombatTeam.Player);
        Require(fifo.State!.Rooms[0].Units.Single(unit => unit.Id == 1).Health == 10 &&
            fifo.State.Rooms[0].Units.Single(unit => unit.Id == 1).MaxHealth == 15, "OnHeal ran before an already queued later team actor.");
        var previewQueued = RoomCombatModel.ApplyPreCombat(new(0, false, queuedRoot.Units, [], context, preview: true), 1);
        Require(previewQueued.Supported && previewQueued.State!.Units.Single().Modifiers!.Upgrades.Count == 1 &&
            previewQueued.State.Units.Single().Health == 10, "Queued preview callbacks reset once-only flags.");
        var unsupported = Run(Room(Unit(1, [Trigger([Heal(1, "Tower")])])));
        Require(!unsupported.Supported && unsupported.State == null, "Unmodeled cross-room trigger produced a usable search child.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(Run(root)) == JsonSerializer.Serialize(result) &&
            JsonSerializer.Serialize(Run(randomRoot)) == JsonSerializer.Serialize(random), "Parallel triggered healing diverged."));
        Require(JsonSerializer.Serialize(root) == parent, "Triggered healing changed its parent.");
        Console.WriteLine("TRIGGERED-HEALING-CHECKS PASS: self/team/filter targets, healability/multiplier/immunity/clipping, zero/negative, nested once/upgrades, tests/cancel, group/random/empty range RNG and 32 isolated parallel branches.");
    }

    internal static void Native(JsonElement fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out JsonElement scenario) || scenario.GetString() != "triggered-healing") return;
        int phases = 0, samples = 0, empty = 0, group = 0, random = 0, zero = 0, negative = 0, immune = 0, restored = 0, sourceModifiers = 0, deferred = 0, deferredOrder = 0;
        string Comparable(TrainCombatState state) => JsonSerializer.Serialize(new
        { state.Rooms, Movement = state.Movement.OrderBy(rule => rule.UnitId), state.EnemySlotsPerRoom, state.Context });
        foreach (JsonElement phase in fixture.GetProperty("PreCombats").EnumerateArray())
        {
            var before = phase.GetProperty("Before").Deserialize<TrainCombatState>(ModelJson.Options)!;
            var after = phase.GetProperty("Actual").Deserialize<TrainCombatState>(ModelJson.Options)!;
            var result = TrainCombatModel.PreCombat(before, (CombatTeam)phase.GetProperty("Team").GetInt32());
            Require(result.Supported && phase.GetProperty("Difference").ValueKind == JsonValueKind.Null &&
                ModelJson.Difference(Comparable(result.State!), Comparable(after)) == null, "Independent native triggered-healing phase differs.");
            sourceModifiers += before.Context!.CardInstances!.Count(card => card.Permanent.Offsets.Heal == 9 ||
                card.Permanent.Upgrades.Any(upgrade => upgrade.Stats.Heal == 9));
            deferred += after.Rooms.SelectMany(room => room.Units).Count(unit => unit.Modifiers!.Upgrades.Any(upgrade =>
                upgrade.AssetKey == "PojuOnHealDeferredMaxHealth") && unit.Triggers.Any(trigger => trigger.Kind == "OnHeal" &&
                trigger.Once && trigger.HasTriggered && trigger.Effects.Any(effect => effect.UnitUpgrade?.Upgrade?.AssetKey == "PojuOnHealDeferredMaxHealth")));
            foreach (CombatUnit unit in before.Rooms.SelectMany(room => room.Units).Where(unit => unit.Health == unit.MaxHealth &&
                unit.Triggers.Any(trigger => trigger.Kind == "OnHeal" && !trigger.HasTriggered && trigger.Effects.Any(effect =>
                    effect.UnitUpgrade?.Upgrade?.AssetKey == "PojuOnHealDeferredMaxHealth"))))
            {
                CombatUnit? next = after.Rooms.SelectMany(room => room.Units).FirstOrDefault(candidate => candidate.Id == unit.Id);
                if (next?.Modifiers?.Upgrades.Any(upgrade => upgrade.AssetKey == "PojuOnHealDeferredMaxHealth") == true && next.MaxHealth - next.Health == 5)
                    deferredOrder++;
            }
            phases++;
        }
        foreach (JsonElement sample in fixture.GetProperty("TriggeredHeals").EnumerateArray())
        {
            Require(sample.GetProperty("Completed").GetBoolean() && sample.GetProperty("Sampled").GetBoolean() &&
                sample.GetProperty("Difference").ValueKind == JsonValueKind.Null && sample.GetProperty("TriggerKind").GetString() == "PreCombat",
                "Native triggered-healing observation is incomplete.");
            CardActionEffect effect = sample.GetProperty("Effect").Deserialize<CardActionEffect>(ModelJson.Options)!;
            UnityRng before = sample.GetProperty("BeforeRng").Deserialize<UnityRng>(ModelJson.Options);
            RngDraw? draw = effect.Range?.Sample(before);
            int value = draw?.Value ?? effect.Value;
            Require(value == sample.GetProperty("Amount").GetInt32() && (draw?.State ?? before).Equals(sample.GetProperty("SampledRng").Deserialize<UnityRng>(ModelJson.Options)),
                "Independent native triggered-healing amount/RNG differs.");
            int[] targets = sample.GetProperty("Targets").Deserialize<int[]>()!;
            var requests = sample.GetProperty("Requests").EnumerateArray().ToArray();
            Require(targets.SequenceEqual(requests.Select(request => request.GetProperty("TargetId").GetInt32())) &&
                requests.All(request => request.GetProperty("Amount").GetInt32() == value), "Native group healing changed target order or sampled per target.");
            foreach (JsonElement request in requests)
            {
                int amount = request.GetProperty("Amount").GetInt32(), health = request.GetProperty("Health").GetInt32();
                CombatStatus[] statuses = request.GetProperty("Statuses").Deserialize<CombatStatus[]>()!;
                int predicted = HealingModel.HealedHealth(health, request.GetProperty("MaxHealth").GetInt32(), amount,
                    request.GetProperty("CanBeHealed").GetBoolean(), statuses);
                Require(predicted == request.GetProperty("AfterHealth").GetInt32(), "Native triggered-healing health differs.");
                zero += amount == 0 ? 1 : 0; negative += amount < 0 ? 1 : 0;
                immune += statuses.Any(status => status.Id == "heal immunity") ? 1 : 0; restored += predicted > health ? 1 : 0;
            }
            samples++; empty += targets.Length == 0 && effect.Range != null ? 1 : 0;
            group += targets.Length > 1 ? 1 : 0; random += effect.Target == "RandomInRoom" ? 1 : 0;
        }
        Require(phases >= 2 && samples > 0 && empty > 0 && group > 0 && random > 0 && zero > 0 && negative > 0 && immune > 0 && restored > 0 && sourceModifiers > 0 && deferred > 0 && deferredOrder > 0 &&
            fixture.GetProperty("UnitUpgradeScaling").EnumerateArray().Any(sample => sample.GetProperty("TriggerKind").GetString() == "OnHeal"),
            "Native triggered-healing coverage is incomplete.");
        Console.WriteLine($"TRIGGERED-HEALING-NATIVE PASS: phases={phases}, effects={samples}, empty-ranges={empty}, groups={group}, random={random}, zero={zero}, negative={negative}, immune={immune}, restored={restored}, source-heal-modifiers={sourceModifiers}, deferred-OnHeal={deferred}, deferred-order={deferredOrder}.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
