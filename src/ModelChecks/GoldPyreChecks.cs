using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class GoldPyreChecks
{
    internal static void Run()
    {
        CombatContext context = Context();
        CombatUnit pyre = Pyre(raw: 17, added: 0);
        TrainCombatState root = Train(context, pyre);
        string parent = Serialize(root);
        RelicPyreAttackRefreshResult refreshed = RelicResourceModel.RefreshPyreAttack(context, pyre);
        Require(refreshed.Supported && refreshed.Pyre!.Modifiers!.AttackDamage == 30 &&
            refreshed.Pyre.Modifiers.AttackDamageAdded == 0 && refreshed.Pyre.Modifiers.DamageBuff == 11 &&
            refreshed.Pyre.Modifiers.DamageBuffFromStatusEffects == 4 && refreshed.Pyre.BaseAttack == 41 &&
            RelicResourceModel.AttackDamage(refreshed.Pyre, refreshed.Context) == 45 &&
            Serialize(refreshed.Pyre.Modifiers.Upgrades) == Serialize(pyre.Modifiers!.Upgrades) &&
            Serialize(refreshed.Pyre.StatusRegistry) == Serialize(pyre.StatusRegistry),
            "The native gold refresh failed to overwrite a negative-upgrade raw cache or erased independent upgrade/buff/status ledgers.");
        RelicPyreAttackRefreshResult positive = RelicResourceModel.RefreshPyreAttack(context, Pyre(raw: 27, added: 7));
        Require(positive.Supported && positive.Pyre!.Modifiers!.AttackDamage == 37 && positive.Pyre.Modifiers.AttackDamageAdded == 7,
            "A Pyre refresh omitted or double-counted its existing upgrade accumulator.");

        TrainCombatResult zero = GoldModel.Adjust(root, 0);
        Require(zero.Supported && ReferenceEquals(zero.State, root), "AdjustGold(0) dispatched a gold signal or recomputed the native cache.");
        TrainCombatState unchangedSet = State(GoldModel.Set(root, 0));
        Require(unchangedSet.Context!.Gold == 0 && Heart(unchangedSet).Modifiers!.AttackDamage == 30,
            "SetGold with an unchanged balance failed to synchronously refresh the Pyre.");
        TrainCombatState clipped = State(GoldModel.Adjust(root, -5));
        Require(clipped.Context!.Gold == 0 && Heart(clipped).Modifiers!.AttackDamage == 30,
            "A nonzero spend clipped at zero was mistaken for AdjustGold(0).");
        TrainCombatState preview = State(GoldModel.Adjust(root, 5, preview: true));
        Require(preview.Context!.Gold == 5 && Heart(preview).Modifiers!.AttackDamage == 17,
            "Preview Save.AdjustGold lost the saved gold update or dispatched the suppressed signal.");
        Require(Serialize(root) == parent, "Gold/Pyre APIs mutated their immutable parent.");

        var once = new RelicConditionState(new("AnyCardPlayed"), true, 6, 1, false);
        TrainCombatState conditional = Train(Context(conditions: [once]), pyre);
        TrainCombatState recorded = State(GoldModel.Adjust(conditional, 5));
        Require(Heart(recorded).Modifiers!.AttackDamage == 30 && recorded.Context!.Relics![0].ResourceEffects![0].Conditions[0].Triggered,
            "The synchronous gold signal failed to notify and record the Pyre relic's outer condition.");
        TrainCombatState spent = State(GoldModel.Adjust(recorded, 5));
        Require(Heart(spent).Modifiers!.AttackDamage == 20 && Heart(spent).Modifiers!.DamageBuffFromStatusEffects == 4,
            "A later gold signal reused a consumed relic condition or erased the status attack history.");
        CombatContext removed = context.WithRelics([]);
        TrainCombatState ownerRemoved = Train(removed, refreshed.Pyre!);
        Require(Heart(ownerRemoved).Modifiers!.AttackDamage == 30 &&
            Heart(State(GoldModel.Adjust(ownerRemoved, 0))).Modifiers!.AttackDamage == 30 &&
            Heart(State(GoldModel.Set(ownerRemoved, 0))).Modifiers!.AttackDamage == 20,
            "Owner removal retroactively recomputed the born cache, or a subsequent true gold signal failed to recompute it.");
        Require(RelicResourceModel.AttackDamage(new(7, "enemy", CombatTeam.Enemy, 2, 5, 5, true, false, false, []), context) == 2,
            "Save's permanent Pyre modifier leaked into an ordinary unit's attack.");
        CombatContext saved = Context(modifiedAttack: 3);
        Require(RelicResourceModel.AttackDamage(pyre, saved) == 35 &&
            !RoomCombatModel.Exchange(new(1, false, [pyre], [], saved)).Supported,
            "The saved Pyre query adjustment or the consumer guard pending native battle evidence differs.");

        TrainCombatState multiplied = State(GoldModel.Adjust(Train(Context(multiplier: 3f), pyre), 7, isReward: true));
        Require(multiplied.Context!.Gold == 20 && GoldRewardModel.Adjust(7, multiplier: 0.5f) == 5 &&
            State(GoldModel.Adjust(Train(Context(multiplier: 3f), pyre), 7)).Context!.Gold == 7,
            "The actual reward multiplier, native floor/round-five order or non-reward bypass differs.");
        Require(State(GoldModel.Adjust(Train(context.WithGold(int.MaxValue), pyre), 1)).Context!.Gold == 0,
            "Save's unchecked signed gold addition was replaced by a checked addition.");
        Require(!GoldModel.Set(Train(Context(multiplier: float.NaN), pyre), 5).Supported &&
            !RelicResourceModel.RefreshPyreAttack(context, new(1, "pyre", CombatTeam.Player, 20, 80, 80, true, true, false, [])).Supported,
            "Missing native Pyre upgrade metadata or a nonfinite raw reward rule produced a guessed supported result.");
        CombatContext legacy = new(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(13), 0, 1, 10);
        TrainCombatState legacyGold = State(GoldModel.Adjust(Train(legacy, pyre), 5));
        Require(legacyGold.Context!.Gold == 5 && Heart(legacyGold).Modifiers!.AttackDamage == 17,
            "Adding the resource mechanism changed a legacy gold fixture without raw resource metadata.");
        Require(RelicResourceModel.RefreshPyreAttack(context, (CombatUnit?)null).Supported &&
            !RelicResourceModel.RefreshPyreAttack(context, (CombatUnit?)null).Context!.Relics![0].ResourceEffects![0].Conditions.Any(),
            "The absent native Pyre heart unexpectedly ran the gold listener.");

        NextDrawUpgradeRule next = new(new("next", "next", new(damage: 1), [], false, false, false, 0, 0, []), []);
        TrainCombatState scheduled = State(NextDrawUpgradeModel.Schedule(Train(context.WithGold(10), pyre), next, 5));
        Require(scheduled.Context!.Gold == 5 && Heart(scheduled).Modifiers!.AttackDamage == 30 &&
            scheduled.Context.Cards.NextDrawUpgrades!.Single().Upgrade.SharedReferenceId == 1 &&
            scheduled.Context.Cards.NextUpgradeReferenceId == 2,
            "A paid next-draw upgrade failed to settle gold/Pyre state before allocating its native queue identity.");
        Require(!NextDrawUpgradeModel.Schedule(root, next, int.MinValue).Supported,
            "Native Math.Abs(Int32.MinValue) became a silent negative gold cost.");

        CrossRoomCallbacks();
        CrossRoomHealthDeath();
        LocalCombatAndPreview();
        Parallel.For(0, 32, index =>
        {
            TrainCombatResult child = index % 2 == 0 ? GoldModel.Adjust(root, 5) : GoldModel.Set(root, 0);
            Require(child.Supported && Heart(child.State!).Modifiers!.AttackDamage == 30 &&
                Heart(child.State!).Modifiers!.DamageBuffFromStatusEffects == 4 && Serialize(root) == parent,
                "Parallel gold/Pyre branches shared a mutable native cache or changed their parent.");
        });
        Require(Serialize(root) == parent, "Parallel branches changed parent resource, RNG or unit history.");
        Console.WriteLine("GOLD-PYRE-CHECKS PASS: synchronous actual-unit refresh, zero/unchanged/clipped/preview gold, signed upgrades and retained status cache, notify conditions, reward multiplier, paid next draw, local and cross-room callbacks, legacy and 32 immutable branches.");
    }

    private static void CrossRoomCallbacks()
    {
        CombatContext context = Context();
        CombatTrigger callback = new("PreCombat", false, false, false, 1,
            [new("CardEffectRewardGold", 7, 0, "", 0, [], false)], skipDuringDeployment: false);
        CombatUnit actor = new(2, "callback", CombatTeam.Player, 2, 10, 10, true, false, false, [], [callback]);
        TrainCombatState root = new([new(0, false, [actor], [], context), new(1, false, [Pyre()], [], context)], [], 7, context);
        string parent = Serialize(root);
        TrainCombatResult result = TrainCombatModel.PreCombat(root, CombatTeam.Player);
        Require(result.Supported && result.State!.Context!.Gold == 5 && Heart(result.State).Modifiers!.AttackDamage == 30 &&
            result.RoomResults.SelectMany(room => room.Events).Single(e => e.Kind == "Gold").Amount == 5 && Serialize(root) == parent,
            "A queued gold effect on another floor did not update the actual Pyre room synchronously.");
    }

    private static void LocalCombatAndPreview()
    {
        CombatContext context = Context();
        CombatTrigger gold = new("OnAttacking", false, false, false, 1,
            [new("CardEffectRewardGold", 5, 0, "", 0, [], false)], skipDuringDeployment: false);
        CombatUnit pyre = Pyre(triggers: [gold]);
        CombatUnit enemy = new(2, "enemy", CombatTeam.Enemy, 0, 100, 100, true, false, false, []);
        RoomCombatState root = new(1, false, [pyre, enemy], [], context);
        RoomCombatResult exchange = RoomCombatModel.Exchange(root);
        Require(exchange.Supported && exchange.State!.Context!.Gold == 5 &&
            exchange.State.Units.Single(unit => unit.Id == 1).Modifiers!.AttackDamage == 30 &&
            exchange.Events.Single(e => e.Kind == "Attack" && e.Actor == 1).Amount == 32,
            "The local Pyre did not refresh after OnAttacking or retroactively recalculated damage already fixed before that callback.");
        RoomCombatResult next = RoomCombatModel.Exchange(exchange.State!);
        Require(next.Supported && next.Events.Single(e => e.Kind == "Attack" && e.Actor == 1).Amount == 45,
            "The next attack did not consume the refreshed raw cache plus its independent saved/status/buff additions.");
        RoomCombatResult preview = RoomCombatModel.Exchange(new(1, false, [pyre, enemy], [], context, preview: true));
        Require(preview.Supported && preview.State!.Context!.Gold == 0 &&
            preview.State.Units.Single(unit => unit.Id == 1).Modifiers!.AttackDamage == 17,
            "CardEffectRewardGold ran in native preview despite its explicit ApplyEffect exclusion.");
    }

    private static void CrossRoomHealthDeath()
    {
        CombatContext context = Context();
        CombatTrigger reward = new("OnDeath", false, false, false, 1,
            [new("CardEffectRewardGold", 5, 0, "", 0, [], false)], skipDuringDeployment: false);
        CombatUnit dying = new(2, "health-death", CombatTeam.Enemy, 0, 1, 1, true, false, false, [], [reward],
            modifiers: new(0, 0, 0, 1, 1, true, false, [], damageBuffFromStatusEffects: 0));
        TrainCombatState root = new([new(0, false, [dying], [], context), new(1, false, [Pyre()], [], context)], [], 7, context);
        string parent = Serialize(root);
        TrainCombatState state = root;
        RelicPyreAttackRefreshResult Refresh(CombatContext changed, bool placement)
        {
            TrainCombatResult refreshed = RelicResourceModel.RefreshPyreAttack(CardSpellModel.WithContext(state, changed), placement);
            if (!refreshed.Supported) return new(null, null, refreshed.UnsupportedReason);
            state = refreshed.State!;
            return new(state.Context, Heart(state));
        }
        RoomCombatResult death = UnitHealthModel.Apply(root.Rooms[0], 2, 1, debuff: true, refreshPyre: Refresh);
        Require(death.Supported && death.State!.Context!.Gold == 5 &&
            death.State.Units.All(unit => unit.Id != 2) && Heart(state).Modifiers!.AttackDamage == 30 &&
            death.Events.Single(e => e.Kind == "Gold").Amount == 5 && Serialize(root) == parent,
            "A maximum-health debuff's synchronous death reward failed to reach the Pyre on another floor through its wrapper.");
    }

    private static BattleResourceState Raw(float multiplier = 1f, int modifiedAttack = 0) => new(3, 1, 5, 10, 10, "pyre", 20, 80, 0, modifiedAttack,
        [10], [10], [], goldRewardMultiplier: multiplier);
    private static CombatContext Context(IReadOnlyList<RelicConditionState>? conditions = null, float multiplier = 1f,
        int modifiedAttack = 0) => new(
        new([], [], [], UnityRng.Seed(13), 0, [], nextDrawUpgrades: [], nextUpgradeReferenceId: 1), UnityRng.Seed(13), 0, 1, 10,
        statistics: BattleStatistics.Empty(deckCards: []), otherPiles: [], queryFrame: new(energy: 0, runningCombat: true, turn: 1),
        energyState: new(10, 0, 0, "MonsterTurn", true), relics:
        [new("owner", "owner", [RelicResourceModel.PyreAttack], disallowedInPlacementPhase: false,
            resourceEffects: [new(0, RelicResourceModel.PyreAttack, 999, 0, 10, false, conditions: conditions)])], resources: Raw(multiplier, modifiedAttack));
    private static CombatUnit Pyre(int raw = 17, int added = 0, IReadOnlyList<CombatTrigger>? triggers = null) => new(1,
        "pyre", CombatTeam.Player, Math.Max(0, raw + 11), 80, 80, true, true, false, [new("buff", 2, 2)], triggers,
        modifiers: new(raw, added, 11, 1, 1, true, false,
            [new("up", "up", new(damage: 7), [], false, false, false, 0, 0, []),
             new("down", "down", new(damage: -10), [], false, false, false, 0, 0, [])], damageBuffFromStatusEffects: 4),
        statusRegistry: [new("buff", 2, 2)]);
    private static TrainCombatState Train(CombatContext context, CombatUnit pyre) => new(
        [new(0, false, [], [], context), new(1, false, [pyre], [], context)], [], 7, context);
    private static CombatUnit Heart(TrainCombatState source) => source.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre);
    private static TrainCombatState State(TrainCombatResult result)
    { Require(result.Supported, result.UnsupportedReason ?? "Gold/Pyre operation failed."); return result.State!; }
    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
