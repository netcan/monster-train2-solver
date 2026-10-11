using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class RelicFloorCapacityChecks
{
    internal static void Run()
    {
        CheckArithmeticAndRegistration();
        CheckInitializationAndLifetime();
        CheckSummons();
        CheckGuardsAndCopies();
        Console.WriteLine("RELIC-FLOOR-CAPACITY-CHECKS PASS: registered native float folding, signed wrap and final zero clamp, ignored owner gates/no notifications, explicit enemy/player/Pyre room initialization and frozen ledgers, reachable owner-removal summon witnesses, separate physical slots, metadata/legacy/copy guards and 32 immutable branches.");
    }

    private static void CheckArithmeticAndRegistration()
    {
        Require(Get(Context([])).Value == 5 && Get(Context([], baseCapacity: 0)).Value == 0 &&
            Get(Context([], baseCapacity: -7)).Value == 0 && Get(Context([], baseCapacity: 40)).Value == 40,
            "Initial native capacity acquired the spell's minimum-one/maximum-thirty bounds.");
        Require(Get(Context([Rule("zero", 0, 0)], baseCapacity: 0)).Value == 0 &&
            Get(Context([Rule("negative", -10, 0)])).Value == 0 &&
            Get(Context([Rule("wrap-low", 1, 0)], baseCapacity: int.MaxValue)).Value == 0 &&
            Get(Context([Rule("wrap-high", -1, 0)], baseCapacity: int.MinValue)).Value == int.MaxValue,
            "Capacity signed additions saturated or clamped before native Int32 wrap.");
        CombatRelicState[] cancellation = [Rule("later", 10, 1), Rule("earlier", -10, 0), Rule("unregistered", 100, null)];
        Require(Get(Context(cancellation)).Value == 5 && Get(Context(cancellation.Reverse().ToArray())).Value == 5 &&
            Get(Context([Rule("never-registered", -100, null)])).Value == 5,
            "Capacity folding applied an unregistered object or clamped intermediate sums.");
        Require(Get(Context([Rule("float-up", 16777217, 0)], baseCapacity: 0)).Value == 16777216 &&
            Get(Context([Rule("float-down", -16777217, 0)], baseCapacity: 16777218)).Value == 2 &&
            Get(Context([Rule("near-max", 2147483520, 0), Rule("overflow", 128, 1)], baseCapacity: 1)).Value == 0,
            "Initialized single precision capacity values were replaced by integer parameters or summed in wider arithmetic.");

        // This unavailable statistic would reject an ordinary condition consumer.
        var ignoredCondition = new RelicConditionState(new("NotCaptured"), false, 2, 0, true);
        CombatContext ignored = Context([Rule("ignored-gates", 2, 0, [ignoredCondition], disallowed: true)]);
        string parent = Serialize(ignored);
        RelicResourceResult result = Get(ignored);
        Require(result.Value == 7 && result.Events.Count == 0 && ReferenceEquals(result.Context, ignored) &&
            Get(ignored.WithQueryFrame(ignored.QueryFrame!.With(runningCombat: false))).Value == 7 && Serialize(ignored) == parent,
            "A mutable capacity rule evaluated owner conditions/placement/preview phase, notified, or changed state.");
    }

    private static void CheckInitializationAndLifetime()
    {
        CombatContext source = Context([Rule("installed", 2, 0)], rooms: [new(2, 19, 3, false), new(7, 0, 1, true)]);
        string parent = Serialize(source);
        RoomCapacityResult initialized = Initialize(source);
        CombatContext born = initialized.Context!;
        Require(initialized.Amount == 7 && born.RoomCapacities!.Select(room => (room.RoomIndex, room.IsPyre)).SequenceEqual([(2, false), (7, true)]) &&
            born.RoomCapacities!.All(room => room.PlayerMaximum == 7 && room.EnemyMaximum == 7) && Serialize(source) == parent,
            "Room setup lost room identity, omitted the enemy/Pyre group, or mutated a preexisting ledger.");
        CombatContext ownerRemoved = born.WithRelics([]);
        Require(Get(ownerRemoved).Value == 5 && ownerRemoved.RoomCapacities!.All(room => room.PlayerMaximum == 7 && room.EnemyMaximum == 7) &&
            Initialize(ownerRemoved).Context!.RoomCapacities!.All(room => room.PlayerMaximum == 5 && room.EnemyMaximum == 5),
            "Removing a rule retroactively recomputed a room's frozen native base, or failed to affect future room setup.");
        RoomCapacityResult enlarged = RoomCapacityModel.Apply(born, 2, new("AdjustCapacity", "Room", 1, false, true, []));
        Require(enlarged.Supported && Get(enlarged.Context!).Value == 7 && enlarged.Context!.RoomCapacities![0].PlayerMaximum == 8 &&
            enlarged.Context!.RoomCapacities![0].EnemyMaximum == 7 && Initialize(enlarged.Context).Context!.RoomCapacities![0].PlayerMaximum == 7,
            "A runtime capacity adjustment changed the raw save rule or became part of future room initialization.");
        Require(Initialize(Context([], baseCapacity: 0)).Context!.RoomCapacities!.All(room => room.PlayerMaximum == 0 && room.EnemyMaximum == 0) &&
            Initialize(Context([], baseCapacity: 40)).Context!.RoomCapacities!.All(room => room.PlayerMaximum == 40 && room.EnemyMaximum == 40),
            "Room birth applied action bounds to native base capacity.");
    }

    private static void CheckSummons()
    {
        CombatContext single = Initialize(Context([Rule("single", 2, 0)])).Context!;
        BattleTurnState seven = Battle(single, [6]);
        Require(Play(seven, 1).Supported && BattleActionModel.PlayCard(Battle(Initialize(single.WithRelics([])).Context!, [6]), new(1, 0)).Rejection == ActionRejection.Illegal,
            "The independently initialized seven-versus-five capacity did not control a real six-size card summon.");

        CombatRelicState[] owners = Enumerable.Range(0, 5).Select(index => Rule("owner-" + index, 2, index)).ToArray();
        CombatContext installed = Initialize(Context(owners)).Context!;
        BattleTurnState initial = Battle(installed, [6, 6, 2, 1]);
        string parent = Serialize(initial);
        BattleTurnState prefix = Play(Play(initial, 1).State!, 2).State!;
        BattleTurnState fourteen = Play(prefix, 3).State!;
        BattleTurnState full = Play(fourteen, 4).State!;
        Require(full.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Sum(unit => unit.Size) == 15 &&
            ReferenceEquals(full.Spawn.Train.Context!.Resources, installed.Resources),
            "Actual summons did not consume the independently initialized fifteen-capacity ledger or lost raw inputs.");
        foreach (CombatRelicState owner in owners)
        {
            CombatContext withoutOwner = installed.WithRelics(owners.Where(candidate => candidate.DataId != owner.DataId).ToArray());
            Require(Get(withoutOwner).Value == 13 && withoutOwner.RoomCapacities![0].PlayerMaximum == 15,
                "Owner removal changed an existing room instead of only its future rule query.");
            BattleTurnState historical = Battle(withoutOwner, [6, 6, 2, 1]);
            Require(Play(Play(Play(historical, 1).State!, 2).State!, 3).Supported,
                "An already born room was silently reinitialized at card play.");
            BattleTurnState fresh = Battle(Initialize(withoutOwner).Context!, [6, 6, 2, 1]);
            BattleTurnState reachable = Play(Play(fresh, 1).State!, 2).State!;
            BattleActionResult rejected = BattleActionModel.PlayCard(reachable, new(3, 0));
            Require(rejected.Rejection == ActionRejection.Illegal && reachable.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Sum(unit => unit.Size) == 12,
                "A per-owner future-room counterfactual accepted fourteen size in thirteen capacity or required an unreachable prefix.");
        }
        BattleTurnState slotRoot = Battle(Initialize(Context([], baseCapacity: 100)).Context!, [6, 6, 2], slots: 2);
        BattleTurnState occupied = Play(Play(slotRoot, 1).State!, 2).State!;
        Require(occupied.Spawn.Train.Context!.RoomCapacities![0].PlayerMaximum == 100 &&
            BattleActionModel.PlayCard(occupied, new(3, 0)).Rejection == ActionRejection.Illegal,
            "Additional floor capacity fabricated an extra physical spawn slot.");

        string expected = Serialize(fourteen);
        Parallel.For(0, 32, _ =>
        {
            BattleTurnState branch = Play(Play(Play(initial, 1).State!, 2).State!, 3).State!;
            Require(Serialize(branch) == expected && Get(installed).Value == 15 && Initialize(installed).Amount == 15,
                "Parallel capacity initialization/card-play branches diverged.");
        });
        Require(Serialize(initial) == parent && installed.BattleRng.Equals(initial.Spawn.Train.Context!.BattleRng),
            "A capacity branch mutated its parent or a pure capacity query consumed gameplay RNG.");
    }

    private static void CheckGuardsAndCopies()
    {
        CombatRelicState owner = Rule("valid", 2, 0);
        CombatRelicState[] invalid =
        [
            new("missing", "missing", [RelicResourceModel.FloorCapacity]),
            new("wrong-index", "wrong-index", [RelicResourceModel.FloorCapacity], resourceEffects: [new(1, RelicResourceModel.FloorCapacity, 2, 0, 2, false, capacityValue: 2f, capacityMutationOrder: 0)]),
            new("wrong-type", "wrong-type", [RelicResourceModel.FloorCapacity], resourceEffects: [new(0, RelicResourceModel.Energy, 2, 0, 2, false)]),
            Owner("missing-value", new(0, RelicResourceModel.FloorCapacity, 2, 0, 2, false, capacityMutationOrder: 0)),
            Owner("wrong-value", new(0, RelicResourceModel.FloorCapacity, 2, 0, 2, false, capacityValue: 3f, capacityMutationOrder: 0)),
            Owner("wrong-amount", new(0, RelicResourceModel.FloorCapacity, 16777217, 0, 16777217, false, capacityValue: (float)16777217, capacityMutationOrder: 0)),
            Owner("not-finite", new(0, RelicResourceModel.FloorCapacity, 2, 0, 2, false, capacityValue: float.NaN, capacityMutationOrder: 0)),
            Owner("outside-int32", new(0, RelicResourceModel.FloorCapacity, int.MaxValue, 0, int.MaxValue, false, capacityValue: (float)int.MaxValue, capacityMutationOrder: 0)),
            Rule("negative-order", 2, -1),
            Owner("wrong-native-gate", new(0, RelicResourceModel.FloorCapacity, 2, 0, 2, false, requiredMutatorId: "mutator", capacityValue: 2f, capacityMutationOrder: 0)),
            Owner("draw-fields", new(0, RelicResourceModel.FloorCapacity, 2, 0, 2, false, drawValue: 2f, drawMutationOrder: 0, capacityValue: 2f, capacityMutationOrder: 0)),
            Owner("capacity-fields-on-draw", new(0, RelicResourceModel.Draw, 2, 0, 2, false, drawValue: 2f, drawMutationOrder: 0, capacityValue: 2f, capacityMutationOrder: 0)),
            new("unknown", "unknown", ["UnknownCapacityEffect"])
        ];
        foreach (CombatRelicState malformed in invalid)
            Require(!RelicResourceModel.GetFloorCapacity(Context([malformed])).Supported,
                "Malformed native floor capacity metadata became a guessed result: " + malformed.AssetKey);
        Require(!RelicResourceModel.GetFloorCapacity(Context([owner, Rule("duplicate-order", 2, 0)])).Supported &&
            !RelicResourceModel.GetFloorCapacity(Context([owner], baseCapacity: null)).Supported &&
            !RelicResourceModel.GetFloorCapacity(Context([], baseCapacity: null)).Supported,
            "Duplicate mutation registration or missing raw capacity was accepted.");
        var noRaw = new CombatContext(new([], [], [], UnityRng.Seed(439), 0, []), UnityRng.Seed(439), 0, 1, 10, relics: [owner]);
        Require(!RelicResourceModel.GetFloorCapacity(noRaw).Supported &&
            !RelicResourceModel.InitializeRoomCapacities(noRaw).Supported,
            "An original capacity effect without raw resource/room inputs was accepted.");
        Require(!RelicResourceModel.InitializeRoomCapacities(Context([owner], captureRooms: false)).Supported &&
            !RelicResourceModel.InitializeRoomCapacities(Context([owner], rooms: [new(0, 5, 5, false), new(0, 5, 5, false)])).Supported &&
            !RelicResourceModel.InitializeRoomCapacities(Context([owner], rooms: [new(-1, 5, 5, false)])).Supported,
            "Explicit initialization accepted missing, duplicate or invalid native room identities.");

        var draw = Owner("draw", new(0, RelicResourceModel.Draw, 1, 0, 1, false, drawValue: 1f, drawMutationOrder: 0));
        CombatContext independent = Context([owner, draw]);
        Require(Get(independent).Value == 7 && RelicResourceModel.GetDrawCount(independent).Value == 6 &&
            RelicResourceModel.GetDrawCount(Context([draw], baseCapacity: null)).Supported &&
            RelicResourceModel.GetEnergyModification(Context([], baseCapacity: null)).Supported,
            "Capacity registrations shared draw ordinals or made old resource captures require new capacity input.");
        var legacy = new RoomCombatState(0, false, [], [], new(new([], [], [], UnityRng.Seed(439), 0, []), UnityRng.Seed(439), 0, 1, 10));
        Require(RoomCombatModel.Resolve(legacy).Supported, "A legacy capture without capacity effects/resources stopped replaying.");

        var once = new RelicConditionState(new("AnyCardPlayed", "ThisTurn"), true, 6, 1, false).Recorded(0);
        CombatContext copiedSource = Context([Rule("copied", 2, 0, [once])]);
        RelicResourceEffect copied = copiedSource.Relics![0].ResourceEffects![0].WithConditions([]);
        Require(copied.CapacityValue == 2f && copied.CapacityMutationOrder == 0 && copied.DrawValue == null && copied.DrawMutationOrder == null,
            "Resource condition replacement lost or cross-populated capacity mutation metadata.");
        CombatContext duration = RelicConditionModel.EndDuration(copiedSource, "ThisTurn");
        Require(Get(duration).Value == 7 && duration.Relics![0].ResourceEffects![0].CapacityValue == 2f &&
            duration.Relics![0].ResourceEffects![0].CapacityMutationOrder == 0 && ReferenceEquals(duration.Resources, copiedSource.Resources),
            "Condition duration rollover lost capacity inputs or registration history.");
        foreach (CombatRelicState copy in new[] { owner.WithSpawnStatuses([]), owner.WithCardModifiers([]), owner.WithTriggerAdditions([]),
            owner.WithStatusStackAdditions([]), owner.WithStatusMagnitudes([]), owner.WithResourceEffects(owner.ResourceEffects!) })
            Require(copy.ResourceEffects!.Single().CapacityValue == 2f && copy.ResourceEffects![0].CapacityMutationOrder == 0,
                "A relic-family copy discarded native capacity mutation metadata.");
        foreach (CombatContext copy in new[] { independent.WithGold(4), independent.WithCards(independent.Cards), independent.WithRelics(independent.Relics),
            independent.WithEnergyState(independent.EnergyState!), independent.WithBattleRng(independent.BattleRng), independent.WithStatistics(independent.Statistics),
            independent.AfterCardEffects(), independent.WithOtherPiles([]), independent.WithQueryFrame(independent.QueryFrame),
            independent.WithRoomCapacities(independent.RoomCapacities!) })
            Require(ReferenceEquals(copy.Resources, independent.Resources) && Get(copy).Value == 7,
                "A runtime context copy discarded raw capacity or its registered mutation definitions.");
    }

    private static CombatRelicState Owner(string id, RelicResourceEffect effect, bool? disallowed = null) =>
        new(id, id, [effect.Type], disallowedInPlacementPhase: disallowed, resourceEffects: [effect]);
    private static CombatRelicState Rule(string id, int amount, int? order, IReadOnlyList<RelicConditionState>? conditions = null, bool? disallowed = null) =>
        Owner(id, new(0, RelicResourceModel.FloorCapacity, amount, 0, (int)(float)amount, false,
            conditions: conditions, capacityValue: (float)amount, capacityMutationOrder: order), disallowed);
    private static CombatContext Context(IReadOnlyList<CombatRelicState> relics, int? baseCapacity = 5,
        IReadOnlyList<RoomCapacityState>? rooms = null, bool captureRooms = true)
    {
        var rng = UnityRng.Seed(439);
        return new(new([], [], [], rng, 0, []), rng, 0, 1, 10, statistics: BattleStatistics.Empty(deckCards: []), otherPiles: [],
            queryFrame: new(energy: 10, runningCombat: true, turn: 1), energyState: new(10, 0, 0, "MonsterTurn", true), relics: relics,
            roomCapacities: captureRooms ? rooms ?? [new(0, 5, 5, false), new(1, 5, 5, true)] : null,
            resources: new(3, 1, 5, 10, 10, "pyre", 20, 80, 0, 0, [5, 10], [10, 20], [], baseFloorCapacity: baseCapacity));
    }
    private static BattleTurnState Battle(CombatContext source, IReadOnlyList<int> sizes, int slots = 7)
    {
        var rng = UnityRng.Seed(439);
        CardToken[] hand = Enumerable.Range(1, sizes.Count).Select(id => new CardToken(id, "unit-" + id)).ToArray();
        CardInstanceState[] instances = hand.Select(card => CardInstanceState.Empty(card.InstanceId, card.DataId)).ToArray();
        CardPileState[] piles = [new("Standby", []), new("Exhausted", []), new("Purged", []), new("Eaten", [])];
        CombatContext context = source.WithCards(new(hand, [], [], rng, 0, [])).WithCardInstances(instances).WithOtherPiles(piles)
            .WithNextCardId(sizes.Count + 1).WithStatistics(BattleStatistics.Empty(deckCards: hand.Select(card => card.InstanceId).ToArray()))
            .WithQueryFrame(new(10, true, 2, 0, 0, 1, 0));
        var pyre = new CombatUnit(50, "pyre", CombatTeam.Player, 20, 80, 80, true, true, false, []);
        var train = new TrainCombatState([new(0, false, [], [], context), new(1, false, [pyre], [], context)], [], 7, context);
        var spawn = new EnemySpawnState(train, [new([new([])])], [-1], 0, false, rng, 100, [], 0, false, 0, 0, 2, []);
        CardPlayRule[] cards = sizes.Select((size, index) => new CardPlayRule("unit-" + (index + 1), "unit-" + (index + 1),
            0, "SpawnMonster", "Standby", new(60 + index, "unit", CombatTeam.Player, 1, 50, 50, true, false, false, [], size: size), [])).ToArray();
        var rules = new BattlePlayRules([new(0, 5, slots, true, false, false), new(1, 5, 7, true, true, true)], cards);
        return new(spawn, 10, 3, 5, 0, 0, "New", [], piles, [], rules, selectedRoom: 0);
    }
    private static BattleActionResult Play(BattleTurnState source, int card)
    {
        BattleActionResult result = BattleActionModel.PlayCard(source, new(card, 0));
        Require(result.Supported, "Capacity witness card play failed: " + result.Reason);
        return result;
    }
    private static RelicResourceResult Get(CombatContext source)
    {
        RelicResourceResult result = RelicResourceModel.GetFloorCapacity(source);
        Require(result.Supported, result.UnsupportedReason ?? "Floor capacity query failed.");
        return result;
    }
    private static RoomCapacityResult Initialize(CombatContext source)
    {
        RoomCapacityResult result = RelicResourceModel.InitializeRoomCapacities(source);
        Require(result.Supported, result.UnsupportedReason ?? "Room capacity initialization failed.");
        return result;
    }
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
