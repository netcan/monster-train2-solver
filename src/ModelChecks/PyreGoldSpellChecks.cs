using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class PyreGoldSpellChecks
{
    internal static void Run()
    {
        PyreTargets();
        EnginePermission();
        GoldEffects();
        LocalGoldDispatch();
        MissingInputs();
        Console.WriteLine("PYRE-GOLD-SPELL-CHECKS PASS: dedicated Pyre targets, captured engine permission, signed gold affordability and runtime gates, preview/zero exclusions, remote refresh and initialized-range guards.");
    }

    private static void PyreTargets()
    {
        CombatContext context = Context();
        var filters = new CardTargetFilters("FutureHealthMode", ["missing"], ["untouchable"], true, "missing-subtype", ["missing-subtype"]);
        var effect = new CardActionEffect("BuffAttack", "Pyre", 3, false, false, [], filters: filters);
        CombatUnit heart = Pyre(statuses: [new("untouchable", 1, 0)]);
        TrainCombatState root = Train(context, heart);
        CardTargets tower = CardTargetModel.Collect(root, 0, effect, []);
        CardTargets room = CardTargetModel.Collect(root.Rooms[1], effect, []);
        Require(tower.Supported && tower.UnitIds.SequenceEqual([1]) && room.Supported && room.UnitIds.SequenceEqual([1]),
            "The dedicated Pyre collector consumed team, subtype, health, status or immunity filters.");
        Require(CardTargetModel.Collect(Train(context, Pyre(health: 0)), 0, effect, []).UnitIds.SequenceEqual([1]) &&
            CardTargetModel.Collect(Train(context, null), 0, effect, []).UnitIds.Count == 0,
            "The dedicated native Pyre lookup became a living-target query or invented an absent heart.");
        SpellCastCheck cast = CardSpellModel.TestPlay(root, 0, [effect], 0);
        TrainSpellResult applied = CardSpellModel.Apply(root, 0, [effect], 0);
        Require(cast.Supported && cast.CanPlay && applied.Supported && Heart(applied.State!).Modifiers!.DamageBuff == 3 &&
            applied.TargetCollections.Single().UnitIds.SequenceEqual([1]) && Heart(root).Modifiers!.DamageBuff == 0,
            "Full spell validation reapplied filters bypassed by native Pyre selection, or lost the remote target.");
    }

    private static void EnginePermission()
    {
        foreach (bool? permission in new bool?[] { null, false, true })
        {
            BattleTurnState root = Decision(permission);
            string parent = Serialize(root);
            BattleActionResult engine = BattleActionModel.PlayCard(root, new(1, 1));
            Require(permission == true ? engine.Supported : engine.Rejection == ActionRejection.Illegal,
                "Engine-room play did not require an affirmative captured native card permission.");
            Require(BattleActionModel.EnumerateSupportedPlays(root).Any(action => action.CardInstanceId == 1 && action.RoomIndex == 1) == (permission == true),
                "The legal action enumerator disagreed with the captured engine-room permission.");
            Require(BattleActionModel.PlayCard(root, new(1, 0)).Supported && Serialize(root) == parent,
                "Engine metadata restricted an ordinary floor or card resolution changed its immutable parent.");
            if (engine.Supported)
                Require(engine.State!.Spawn.Train.Context!.Cards.Hand.Count == 0 &&
                    engine.State.Spawn.Train.Context.Cards.Discard.Single().InstanceId == 1 &&
                    Heart(engine.State.Spawn.Train).Modifiers!.DamageBuff == 3,
                    "A permitted native room spell lost its Pyre effect or normal card routing.");
        }
        Require(BattleActionModel.PlayCard(Decision(true, summon: true), new(1, 1)).Rejection == ActionRejection.Illegal,
            "An engine permission flag allowed a unit deployment into the Pyre room.");
        Require(BattleActionModel.PlayCard(Decision(true), new(1, 1, targetUnitId: 1)).Rejection == ActionRejection.Illegal,
            "A dedicated Pyre room spell accepted an invented drop-target unit action.");
    }

    private static void GoldEffects()
    {
        TrainCombatState root = Train(Context(gold: 4), Pyre());
        string parent = Serialize(root);
        CheckGold(root, 0, true, canPlay: true, gold: 4, raw: 17);
        CheckGold(root, 5, true, canPlay: true, gold: 9, raw: 20);
        CheckGold(root, -5, true, canPlay: false, gold: 4, raw: 17);
        CheckGold(root, -5, false, canPlay: true, gold: 0, raw: 20);
        CheckGold(root, -4, true, canPlay: true, gold: 0, raw: 20);
        CheckGold(root, int.MinValue, true, canPlay: true, gold: 0, raw: 20);

        CardActionEffect buff = new("BuffAttack", "Pyre", 3, false, false, []);
        CardActionEffect required = Gold(-5, true);
        SpellCastCheck partialCast = CardSpellModel.TestPlay(root, 0, [buff, required], 0);
        TrainSpellResult partial = CardSpellModel.Apply(root, 0, [buff, required], 0);
        Require(partialCast.Supported && partialCast.CanPlay && partial.Supported && partial.State!.Context!.Gold == 4 &&
            Heart(partial.State).Modifiers!.DamageBuff == 3 && Heart(partial.State).Modifiers!.AttackDamage == 17,
            "A successful companion effect bypassed an unaffordable gold effect's runtime test.");
        var mandatory = Gold(-5, true, tests: new(true, true, false, false));
        SpellCastCheck blocked = CardSpellModel.TestPlay(root, 0, [buff, mandatory], 0);
        Require(blocked.Supported && !blocked.CanPlay, "An unaffordable mandatory gold effect allowed a compound cast.");
        var cancelling = Gold(-5, true, tests: new(true, false, true, false));
        TrainSpellResult cancelled = CardSpellModel.Apply(root, 0, [cancelling, buff], 0);
        Require(cancelled.Supported && cancelled.State!.Context!.Gold == 4 && Heart(cancelled.State).Modifiers!.DamageBuff == 0,
            "A failed gold runtime test ignored its native cancel-subsequent flag.");

        TrainCombatState preview = Train(Context(gold: 4), Pyre(), preview: true);
        SpellCastCheck previewCast = CardSpellModel.TestPlay(preview, 0, [Gold(5, true)], 0);
        TrainSpellResult skipped = CardSpellModel.Apply(preview, 0, [Gold(5, true)], 0);
        Require(previewCast.Supported && previewCast.CanPlay && skipped.Supported && skipped.State!.Context!.Gold == 4 &&
            Heart(skipped.State).Modifiers!.AttackDamage == 17,
            "Preview RewardGold either failed its valid test or ran its excluded native body.");
        TrainCombatState absent = Train(Context(pyreAlive: false), null);
        SpellCastCheck noHeart = CardSpellModel.TestPlay(absent, 0, [Gold(5, false)], 0);
        TrainSpellResult noBody = CardSpellModel.Apply(absent, 0, [Gold(5, false)], 0);
        Require(noHeart.Supported && !noHeart.CanPlay && noBody.Supported && noBody.State!.Context!.Gold == 4,
            "A gold spell with no living tower ran despite its native tower-HP test.");
        Require(Serialize(root) == parent, "Gold spell tests or child executions mutated their parent.");
    }

    private static void LocalGoldDispatch()
    {
        foreach ((int amount, float multiplier, bool preview, int calls, int gold) in new[]
            { (0, 1f, false, 0, 4), (5, 1f, false, 1, 9), (-5, 1f, false, 1, 0),
              (1, 0.1f, false, 1, 9), (5, 1f, true, 0, 4) })
        {
            CombatContext context = Context(multiplier: multiplier);
            RoomCombatState floor = new(0, false, [], [], context, preview);
            CombatUnit remote = Pyre();
            int dispatches = 0;
            RelicPyreAttackRefreshResult Refresh(CombatContext changed, bool placement)
            {
                dispatches++;
                RelicPyreAttackRefreshResult refreshed = RelicResourceModel.RefreshPyreAttack(changed, remote, placement);
                if (refreshed.Supported) remote = refreshed.Pyre!;
                return refreshed;
            }
            RoomCombatResult result = CardSpellModel.Apply(floor, [Gold(amount, false, target: "Room")], 0, refreshPyre: Refresh);
            Require(result.Supported && result.State!.Context!.Gold == gold && dispatches == calls &&
                remote.Modifiers!.AttackDamage == (calls == 0 ? 17 : 20) && context.Gold == 4,
                "A local gold card lost its real remote Pyre refresh, minimum-five reward, preview exclusion or zero-body distinction.");
        }
    }

    private static void MissingInputs()
    {
        TrainCombatState root = Train(Context(), Pyre());
        foreach (CardActionEffect effect in new[] { Gold(5, null), Gold(5, false, range: new(1, 9)) })
        {
            Require(!CardSpellModel.TestPlay(root, 0, [effect], 0).Supported &&
                !CardSpellModel.Apply(root, 0, [effect], 0).Supported,
                "Missing native gold requirement or a setup-time ranged amount produced a guessed supported spell.");
        }
        TrainCombatState legacy = Train(Context(resources: false), Pyre());
        Require(!CardSpellModel.TestPlay(legacy, 0, [Gold(5, false)], 0).Supported &&
            !CardSpellModel.Apply(legacy, 0, [Gold(5, false)], 0).Supported,
            "A new direct gold spell silently guessed missing native resource rules.");
    }

    private static void CheckGold(TrainCombatState root, int amount, bool required, bool canPlay, int gold, int raw)
    {
        CardActionEffect effect = Gold(amount, required);
        SpellCastCheck cast = CardSpellModel.TestPlay(root, 0, [effect], 0);
        TrainSpellResult applied = CardSpellModel.Apply(root, 0, [effect], 0);
        Require(cast.Supported && cast.CanPlay == canPlay && applied.Supported && applied.State!.Context!.Gold == gold &&
            Heart(applied.State).Modifiers!.AttackDamage == raw && applied.State.Context.BattleRng.Equals(root.Context!.BattleRng),
            "RewardGold signed affordability, actual dispatch or deterministic test/body order differs.");
    }

    private static CardActionEffect Gold(int amount, bool? required, CardEffectTests? tests = null,
        CardEffectRange? range = null, string target = "Pyre") =>
        new("RewardGold", target, amount, false, false, [], tests: tests, range: range, goldCostRequired: required);

    private static CombatContext Context(int gold = 4, bool hand = false, float multiplier = 1f, bool pyreAlive = true,
        bool resources = true) => new(
        new(hand ? [new(1, "spell")] : [], [], [], UnityRng.Seed(17), 0, []), UnityRng.Seed(17), gold, hand ? 2 : 1, 10,
        cardInstances: hand ? [new(1, "spell", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [])] : null,
        otherPiles: [], queryFrame: new(energy: 3, runningCombat: true, turn: 1, forgePoints: 0, dragonsHoard: 0,
            moonPhase: 1, pyreResurrectionCount: 0), energyState: new(10, 0, 0, "MonsterTurn", pyreAlive), relics: [],
        resources: resources ? new(3, 1, 5, 10, 10, "pyre", 20, 80, 0, 0, [], [], [], goldRewardMultiplier: multiplier) : null);

    private static CombatUnit Pyre(int health = 80, IReadOnlyList<CombatStatus>? statuses = null) =>
        new(1, "pyre", CombatTeam.Player, 17, health, 80, true, true, false, statuses ?? [],
            modifiers: new(17, 0, 0, 0, 0, true, false, [], damageBuffFromStatusEffects: 0));
    private static TrainCombatState Train(CombatContext context, CombatUnit? heart, bool preview = false) =>
        new([new(0, false, [], [], context, preview), new(1, false, heart == null ? [] : [heart], [], context, preview)], [], 7, context);
    private static CombatUnit Heart(TrainCombatState train) => train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre);

    private static BattleTurnState Decision(bool? permission, bool summon = false)
    {
        CombatContext context = Context(hand: true);
        TrainCombatState train = Train(context, Pyre());
        var spawn = new EnemySpawnState(train, [new([new([])])], [-1], 0, false, context.BattleRng, 2, [], 0, false, 2, 0, 1, []);
        var unit = new CombatUnit(0, "unit", CombatTeam.Player, 1, 1, 1, true, false, false, [], size: 1);
        var rule = new CardPlayRule("spell", "spell", 0, summon ? "SpawnMonster" : "Spell", summon ? "Standby" : "Discard",
            summon ? unit : null, [], [new("BuffAttack", "Pyre", 3, false, false, [])],
            canPlayInEngineRoom: permission);
        var rules = new BattlePlayRules([new(0, 5, 7, true, false, false), new(1, 0, 7, true, true, true)], [rule]);
        return new(spawn, 3, 3, 5, 0, 0, "New", [], [new("Standby", []), new("Exhausted", []), new("Purged", [])], [], rules);
    }

    private static string Serialize(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
