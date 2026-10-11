using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class RelicStatusDecayChecks
{
    internal static void Run()
    {
        foreach (int sourceTeam in new[] { 0, 1, 2, 3, 255 })
        foreach (int targetTeam in new[] { 0, 1, 2, 3, 255 })
        foreach (bool preview in new[] { false, true })
        {
            CombatContext context = Context([Relic("team", new(0, sourceTeam, ["buff", "poison"]))]);
            RelicStatusDecayResult query = Get(context, true, "buff", targetTeam, preview);
            bool prevented = sourceTeam == 0 || sourceTeam == targetTeam;
            Require(query.ShouldRemove == !prevented && query.SuppressingRelicId == (prevented ? "team" : null) &&
                query.SuppressingEffectIndex == (prevented ? 0 : null) && ReferenceEquals(query.Context, context),
                "Native decay exact team equality/None wildcard, preview or suppressing owner differs.");
        }
        CombatContext ordered = Context([Relic("unmatched", new(0, 1, ["buff"])),
            Relic("first", new(0, 2, ["unrelated", "buff", "poison"])), Relic("later", new(0, 0, ["buff"]))]);
        Require(Get(ordered, true, "buff", 2).SuppressingRelicId == "first" &&
            Get(ordered, true, "poison", 2).SuppressingRelicId == "first" &&
            Get(ordered, true, "Buff", 2).ShouldRemove && Get(ordered, true, "regen", 2).ShouldRemove,
            "Native decay first-owner order, complete status list or exact status ID matching differs.");
        var effects = new CombatRelicState("ordered-effects", "ordered-effects",
            [RelicModel.PreventStatusDecay, RelicModel.PreventStatusDecay],
            statusDecays: [new(0, 1, ["buff"]), new(1, 2, ["buff"])]);
        Require(Get(Context([effects]), true, "buff", 2).SuppressingEffectIndex == 1 &&
            !Get(null, false, "anything", 2).ShouldRemove && Get(null, true, "anything", 2).ShouldRemove,
            "Native effect order or context-free removeAtContext boundary differs.");

        var ignored = new RelicConditionState(new("NotCaptured"), false, -1, -1, true);
        CombatContext rootContext = Context([Relic("rage", new(0, 2, ["buff"], [ignored]))]);
        string contextParent = Serialize(rootContext);
        Require(!Get(rootContext, true, "buff", 2).ShouldRemove &&
            !Get(rootContext, false, "buff", 2).ShouldRemove &&
            Get(rootContext, false, "buff", 2).SuppressingRelicId == null && Serialize(rootContext) == contextParent,
            "Decay value queries evaluated, notified or recorded ignored outer conditions.");

        var rage = new CombatStatus("buff", 3, 2, removeStackAtEnd: true);
        RoomCombatState root = Room(Unit(statuses: [rage], cache: 6), rootContext);
        string parent = Serialize(root);
        RoomCombatResult kept = Resolve(root);
        Require(Actor(kept).Status("buff")!.Stacks == 3 && Actor(kept).Modifiers!.DamageBuffFromStatusEffects == 6 &&
            Actor(kept).Attack == 7 && Serialize(kept.State!.Context) == contextParent,
            "Preventing natural Rage decay changed native stacks, attack history or full context.");
        RoomCombatResult ordinary = Resolve(Room(Unit(statuses: [rage], cache: 6), Context([])));
        Require(Actor(ordinary).Status("buff")!.Stacks == 2 && Actor(ordinary).Modifiers!.DamageBuffFromStatusEffects == 4,
            "A natural unprotected Rage removal did not update its raw status-attack accumulator.");
        for (int index = 0; index < 3; index++)
        {
            kept = Resolve(kept.State!);
            Require(Actor(kept).Status("buff")!.Stacks == 3 && Actor(kept).Modifiers!.DamageBuffFromStatusEffects == 6,
                "Repeated natural decay changed protected Rage or recomputed its attack history.");
        }
        RoomCombatResult explicitOne = StatusRemovalModel.Remove(root, 1, "buff", 1);
        RoomCombatResult explicitAll = StatusRemovalModel.Remove(root, 1, "buff", -1);
        Require(explicitOne.Supported && Actor(explicitOne).Status("buff")!.Stacks == 2 &&
            Actor(explicitOne).Modifiers!.DamageBuffFromStatusEffects == 4 && explicitAll.Supported &&
            Actor(explicitAll).Status("buff") == null && Actor(explicitAll).Modifiers!.DamageBuffFromStatusEffects == 0 &&
            Actor(explicitAll).RegisteredStatus("buff")!.Stacks == 0,
            "A decay relic suppressed an explicit single/all removal or its native attack callback.");

        CombatStatus clearAll = new("buff", 3, 2, removeAllAtEnd: true);
        RoomCombatResult naturalAll = Resolve(Room(Unit(statuses: [clearAll], cache: 6), rootContext));
        Require(Actor(naturalAll).Status("buff") == null && Actor(naturalAll).RegisteredStatus("buff") == null &&
            Actor(naturalAll).Modifiers!.DamageBuffFromStatusEffects == 0,
            "Decay prevention suppressed the separate natural RemoveAllAtEnd branch.");
        CombatStatus afterCombat = new("buff", 3, 2, removeStackAtEnd: true, removeAfterPostCombat: true);
        Require(Actor(Resolve(Room(Unit(statuses: [afterCombat], cache: 6), rootContext))).Status("buff")!.Stacks == 3 &&
            Actor(Resolve(Room(Unit(statuses: [afterCombat], cache: 6), Context([])))).Status("buff")!.Stacks == 2,
            "The post-combat natural-decay boundary ignored the same native prevention rule.");
        Require(Actor(Resolve(new(0, true, root.Units, [], Context([])))).Status("buff")!.Stacks == 3 &&
            Actor(Resolve(new(0, false, root.Units, [], rootContext, true))).Status("buff")!.Stacks == 3,
            "Placement natural decay or preview-capable prevention differs.");

        RoomCombatResult enemyRage = Resolve(Room(Unit(team: CombatTeam.Enemy, statuses: [rage], cache: 6), rootContext));
        Require(Actor(enemyRage).Status("buff")!.Stacks == 2 && Actor(enemyRage).Modifiers!.DamageBuffFromStatusEffects == 4,
            "A Monsters-team decay relic protected an enemy's Rage.");
        var frost = new CombatStatus("poison", 3, 1, removeStackAtEnd: true);
        CombatContext frostContext = Context([Relic("frost", new(0, 1, ["poison"]))]);
        RoomCombatResult frostKept = Resolve(Room(Unit(team: CombatTeam.Enemy, statuses: [frost]), frostContext));
        Require(Actor(frostKept).Health == 97 && Actor(frostKept).Status("poison")!.Stacks == 3 &&
            frostKept.Events.Any(effect => effect.Kind == "Poison" && effect.Amount == 3) &&
            Actor(Resolve(Room(Unit(statuses: [frost]), frostContext))).Status("poison")!.Stacks == 2,
            "Native Frostbite still damages its owner, while exact Heroes-team prevention controls only decay.");

        // Trigger-consumed statuses use the same native getter before the effect
        // itself runs. This also exercises the branch separately from Clear.
        var consumedFrost = new CombatStatus("poison", 3, 1, removeWhenTriggered: true);
        RoomCombatResult triggerKept = Resolve(Room(Unit(team: CombatTeam.Enemy, statuses: [consumedFrost]), frostContext));
        RoomCombatResult triggerRemoved = Resolve(Room(Unit(team: CombatTeam.Enemy, statuses: [consumedFrost]), Context([])));
        Require(Actor(triggerKept).Health == 97 && Actor(triggerKept).Status("poison")!.Stacks == 3 &&
            Actor(triggerRemoved).Health == 97 && Actor(triggerRemoved).Status("poison")!.Stacks == 2,
            "Trigger-stack prevention suppressed the status effect itself or missed the separate native consumption getter.");

        var relentless = new CombatStatus("relentless", 1);
        var heldRage = new CombatStatus("buff", 3, 2, removeStackAtEnd: true, preventRemovalDuringRelentless: true);
        Require(Actor(Resolve(Room(Unit(team: CombatTeam.Enemy, statuses: [relentless, heldRage], cache: 6), Context([]))))
                .Status("buff")!.Stacks == 3 &&
            Actor(Resolve(Room(Unit(statuses: [relentless, heldRage], cache: 6), Context([])))).Status("buff")!.Stacks == 2 &&
            Actor(Resolve(Room(Unit(team: CombatTeam.Enemy, statuses: [relentless, rage], cache: 6),
                Context([Relic("enemy-rage", new(0, 1, ["buff"]))])))).Status("buff")!.Stacks == 3,
            "Relentless status-level prevention, player-only relentless or relic prevention changed its native boundary.");

        // A real application passes through the adder's immutable With* copy.
        // Its subsequent prevention metadata must survive that independent effect.
        CombatRelicState mixed = new("mixed", "mixed", [RelicModel.AddStatusOnApplied, RelicModel.PreventStatusDecay],
            statusStackAdditions: [new(0, "Heroes", "poison", 2, 1)], statusDecays: [new(1, 1, ["poison"])]);
        RoomCombatState mixedRoot = Room(Unit(team: CombatTeam.Enemy), Context([mixed]));
        RoomCombatResult applied = StatusApplicationModel.Apply(mixedRoot, 1, new("poison", 1, 1, removeStackAtEnd: true));
        Require(applied.Supported && Actor(applied).Status("poison")!.Stacks == 3 &&
            applied.State!.Context!.Relics![0].StatusDecays?.Single().EffectIndex == 1 &&
            Actor(Resolve(applied.State!)).Status("poison")!.Stacks == 3,
            "An independently notifying status adder discarded the same relic's decay metadata.");

        var missing = new CombatRelicState("missing", "missing", [RelicModel.PreventStatusDecay]);
        var unordered = new CombatRelicState("unordered", "unordered", [RelicModel.PreventStatusDecay, RelicModel.PreventStatusDecay],
            statusDecays: [new(1, 2, ["buff"]), new(0, 2, ["poison"])]);
        var unexpected = new CombatRelicState("unexpected", "unexpected", [], statusDecays: [new(0, 2, ["buff"])]);
        foreach (CombatRelicState bad in new[] { missing, unordered, unexpected })
            Require(RelicStatusDecayModel.Validate(bad) != null &&
                !RelicStatusDecayModel.GetShouldRemove(Context([bad]), true, "buff", 2).Supported &&
                !RoomCombatModel.Resolve(Room(Unit(), Context([bad]))).Supported,
                "Missing, unordered or unexpected decay metadata produced a guessed query or combat child.");
        Require(!Get(Context([missing]), false, "buff", 2).ShouldRemove,
            "Native removeAtContext=false inspected malformed later metadata instead of returning immediately.");
        foreach (string id in new[] { "regen", "armor", "stealth", "dazed", "cooldown", "not-a-status" })
        {
            CombatRelicState guarded = Relic("guarded", new(0, 0, [id]));
            Require(!Get(Context([guarded]), true, id, 2).ShouldRemove &&
                RelicStatusDecayModel.Validate(guarded) != null &&
                !RoomCombatModel.Resolve(Room(Unit(), Context([guarded]))).Supported,
                "A pure getter match silently enabled an unmodeled status consumer: " + id);
        }
        Require(!RoomCombatModel.Resolve(Room(Unit(), Context([new("unknown", "unknown", ["UnknownRelicEffect"])]))).Supported,
            "Adding decay support weakened the unknown-owner effect guard.");
        RoomCombatResult missingHistory = RoomCombatModel.Resolve(Room(Unit(statuses: [rage], cache: null), rootContext));
        Require(!missingHistory.Supported && missingHistory.UnsupportedReason!.Contains("status attack accumulator", StringComparison.Ordinal),
            "A Rage decay relic accepted uncaptured starting status-attack history.");
        Require(Resolve(Room(Unit(team: CombatTeam.Enemy, statuses: [frost], cache: null), frostContext)).Supported,
            "A poison-only decay rule unnecessarily required an attack-history accumulator.");
        MultiStatusCardWithProtectedRage();

        string keptState = Serialize(Resolve(root).State);
        string removedState = Serialize(explicitOne.State);
        Parallel.For(0, 32, _ =>
        {
            Require(Serialize(Resolve(root).State) == keptState &&
                Serialize(StatusRemovalModel.Remove(root, 1, "buff", 1).State) == removedState &&
                Get(ordered, true, "buff", 2).SuppressingRelicId == "first",
                "Parallel natural decay, explicit removal or ordered owner queries diverged.");
        });
        Require(Serialize(root) == parent && Serialize(rootContext) == contextParent &&
            kept.State!.Context!.BattleRng.Equals(root.Context!.BattleRng),
            "Decay branches mutated their full parent context or consumed RNG.");
        Console.WriteLine("RELIC-STATUS-DECAY-CHECKS PASS: exact team/None/status/first-owner/preview getters, ignored outer conditions, natural and trigger consumption, explicit/all removal, both post-combat boundaries, attack history, multi-effect status-card plays, deployment/relentless, mixed effect copies, metadata/consumer guards and 32 immutable branches.");
    }

    private static void MultiStatusCardWithProtectedRage()
    {
        var rng = UnityRng.Seed(13);
        CombatStatus[] statuses = [new("buff", 2, 2, removeStackAtEnd: true),
            new("poison", 1, 1, removeStackAtEnd: true), new("armor", 30, 1, removeWhenTriggered: true)];
        CardActionEffect[] effects = statuses.Select(status =>
            new CardActionEffect("AddStatus", "DropTargetCharacter", 0, false, true, [status])).ToArray();
        var rules = new BattlePlayRules(Enumerable.Range(0, 4).Select(index =>
            new RoomPlayRule(index, index == 3 ? 0 : 5, 7, true, index == 3, index == 3)).ToArray(),
            [new("multi-status", "multi-status", 1, "Spell", "Discard", null, [], effects,
                cardType: "Spell", isAnyAbility: false)], statuses);
        CardPileState[] piles = [new("Standby", []), new("Exhausted", []), new("Purged", []), new("DiscardBuffer", [])];
        var card = CardInstanceState.Empty(1, "multi-status");
        var context = new CombatContext(new([new(1, card.DataId)], [], [], rng, 0, []), rng, 0, 2, 10,
            statuses, statistics: BattleStatistics.Empty(deckCards: [1]), cardInstances: [card], cardRegistry: [card],
            allScenarioBossesDead: false, nextAddedTemporaryUpgrades: [], otherPiles: piles,
            queryFrame: new(energy: 3, runningCombat: true, turn: 1, forgePoints: 0, dragonsHoard: 0,
                moonPhase: 1, pyreResurrectionCount: 0), nextUnitId: 3,
            relics: [Relic("rage", new(0, 2, ["buff"]))]);
        var pyre = new CombatUnit(2, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, [],
            modifiers: new(45, 0, 0, 1, 1, true, false, [], damageBuffFromStatusEffects: 0));
        BattleTurnState Create(int? cache)
        {
            var train = new TrainCombatState(Enumerable.Range(0, 4).Select(index => new RoomCombatState(index, false,
                index == 0 ? [Unit(cache: cache)] : index == 3 ? [pyre] : [], [], context)).ToArray(), [], 7, context);
            var spawn = new EnemySpawnState(train, [new EnemyWave([new EnemyGroup([])])], [-1], 0, false, rng,
                3, [], 0, false, 2, 1, 1, []);
            return new(spawn, 3, 3, 5, 0, 0, "New", [new("Spawning", 13, rng)], piles, [], rules, selectedRoom: 0);
        }
        BattleTurnState source = Create(0);
        string parent = Serialize(source);
        var action = new PlayCardAction(1, 0, targetUnitId: 1);
        BattleActionResult played = BattleActionModel.PlayCard(source, action);
        Require(played.Supported, played.Reason ?? "A complete Rage-decay state rejected a multi-effect status card.");
        CombatUnit unit = played.State!.Spawn.Train.Rooms[0].Units.Single();
        CombatContext after = played.State.Spawn.Train.Context!;
        Require(unit.Status("buff")?.Stacks == 2 && unit.Status("poison")?.Stacks == 1 &&
            unit.Status("armor")?.Stacks == 30 && unit.Modifiers!.DamageBuffFromStatusEffects == 4 && unit.Attack == 5 &&
            unit.Health == 100 && played.State.Energy == 2 && after.QueryFrame!.Energy == 2 &&
            after.Cards.Hand.Count == 0 && after.Cards.Discard.Single().InstanceId == 1 &&
            after.FindCard(1)!.PlayCount == 1 && after.FindCard(1)!.LastPlayedCost == 1 &&
            after.Relics![0].StatusDecays!.Single().StatusIds.SequenceEqual(["buff"]) &&
            after.BattleRng.Equals(context.BattleRng) && Serialize(source) == parent,
            "A status-card play lost effects, raw Rage attack history, payment/routing, decay metadata or parent isolation.");
        BattleTurnState missing = Create(null);
        string missingParent = Serialize(missing);
        BattleActionResult rejected = BattleActionModel.PlayCard(missing, action);
        Require(!rejected.Supported && rejected.Rejection == ActionRejection.Unsupported &&
            rejected.Reason?.Contains("status attack accumulator", StringComparison.Ordinal) == true &&
            Serialize(missing) == missingParent,
            "Synthetic definition validation bypassed a real unit's missing attack-history guard or mutated its parent.");
    }

    private static CombatRelicState Relic(string id, RelicStatusDecay rule) =>
        new(id, id, [RelicModel.PreventStatusDecay], statusDecays: [rule]);
    private static CombatContext Context(IReadOnlyList<CombatRelicState> relics) =>
        new(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(13), 0, 1, 10,
            statistics: BattleStatistics.Empty(deckCards: []), otherPiles: [], queryFrame: new(turn: 1), relics: relics);
    private static CombatUnit Unit(int id = 1, CombatTeam team = CombatTeam.Player,
        IReadOnlyList<CombatStatus>? statuses = null, int? cache = 0) =>
        new(id, "status-decay", team, 1, 100, 100, false, false, false, statuses ?? [], [],
            modifiers: new(1, 0, 0, 1, 1, true, false, [], damageBuffFromStatusEffects: cache),
            isBoss: false, lastAttackerId: 0, statusRegistry: statuses ?? []);
    private static RoomCombatState Room(CombatUnit unit, CombatContext context) => new(0, false, [unit], [], context);
    private static RelicStatusDecayResult Get(CombatContext? context, bool removeAtContext, string id, int team, bool preview = false)
    {
        RelicStatusDecayResult result = RelicStatusDecayModel.GetShouldRemove(context, removeAtContext, id, team, preview);
        Require(result.Supported, result.UnsupportedReason ?? "Native decay query failed."); return result;
    }
    private static RoomCombatResult Resolve(RoomCombatState source)
    {
        RoomCombatResult result = RoomCombatModel.Resolve(source);
        Require(result.Supported, result.UnsupportedReason ?? "Native decay branch failed."); return result;
    }
    private static CombatUnit Actor(RoomCombatResult result)
    { Require(result.Supported, result.UnsupportedReason ?? "Decay operation failed."); return result.State!.Units.Single(unit => unit.Id == 1); }
    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
