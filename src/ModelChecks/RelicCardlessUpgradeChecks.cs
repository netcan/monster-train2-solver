using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class RelicCardlessUpgradeChecks
{
    internal static void Run()
    {
        var status = new CombatStatus("multistrike", 1);
        var metadata = new CardUpgradeMaskMetadata([new UpgradeMaskStatus("multistrike", 1)], true, false, false, false);
        var upgrade = new CardUpgradeModifier("cardless-upgrade", "CardlessUpgrade", new(damage: 4, health: 5),
            [status], false, true, false, 0, 0, [], maskMetadata: metadata);
        var lifecycle = new CardLifecycleUpgrade(new CardMaskUpgrade(upgrade.DataId, upgrade.Stats, metadata.Statuses,
            metadata.HasIcon, metadata.HideIcon, metadata.RegionRun, metadata.UnitAbility), upgrade.AssetKey, upgrade.Unique,
            upgrade.RemoveOnDiscard, [], [], false, [], [], []);
        var filter = new CardUpgradeMaskRule(subtypes: new UpgradeMaskContent<string>(required: ["eligible"]));
        var filtered = Modifier(upgrade, lifecycle, [filter]);
        var root = State(Unit(), filtered);
        string parent = JsonSerializer.Serialize(root);
        var applied = Apply(root);
        CombatUnit actual = applied.State!.Units.Single();
        Require(applied.Supported && actual.BaseAttack == 10 && actual.Health == 15 && actual.MaxHealth == 15 &&
            actual.Status("multistrike")?.Stacks == 1 && actual.Modifiers!.Upgrades.Any(item => item.DataId == upgrade.DataId),
            "An eligible cardless non-clone unit did not receive its relic upgrade.");
        Require(JsonSerializer.Serialize(root) == parent, "Cardless relic application mutated its parent.");

        foreach (CombatUnit excluded in new[]
        {
            Unit(cardless: false), Unit(clone: true), Unit(subtypes: ["other"]), Unit(team: CombatTeam.Enemy)
        })
        {
            RoomCombatState candidate = State(excluded, filtered);
            Require(JsonSerializer.Serialize(Apply(candidate).State) == JsonSerializer.Serialize(candidate),
                "Cardless relic applied to a non-cardless, clone, filtered or wrong-team unit.");
        }

        RoomCombatState covenantPhase = State(Unit(), filtered);
        Require(JsonSerializer.Serialize(RelicSpawnStatusModel.CharacterAdded(covenantPhase, 1, 0, true).State) ==
            JsonSerializer.Serialize(covenantPhase), "A non-covenant cardless relic ran in the covenant pass.");

        var source = new CardInstanceState(7, "source", new CardModifiers(new(), [upgrade], 0, []),
            CardModifiers.Empty(), 0, 0, 0, []);
        var noFilter = Modifier(upgrade, lifecycle, []);
        RoomCombatState sourced = State(Unit(), noFilter, [source]);
        Require(JsonSerializer.Serialize(Apply(sourced, 7).State) == JsonSerializer.Serialize(sourced),
            "A unique relic upgrade duplicated an upgrade already on its source card.");

        var disabled = Modifier(upgrade, lifecycle, [], applyToCardless: false);
        RoomCombatState disabledState = State(Unit(), disabled);
        Require(JsonSerializer.Serialize(Apply(disabledState).State) == JsonSerializer.Serialize(disabledState),
            "A relic upgrade with cardless dispatch disabled was applied to a summon.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(Apply(root).State) == JsonSerializer.Serialize(applied.State),
            "Parallel cardless relic branches diverged."));
        Console.WriteLine("RELIC-CARDLESS-UPGRADE-CHECKS PASS: team/cardless/clone/filter/source-unique gates, status and stat application, and 32 immutable branches.");
    }

    private static RelicCardModifier Modifier(CardUpgradeModifier upgrade, CardLifecycleUpgrade lifecycle,
        IReadOnlyList<CardUpgradeMaskRule> filters, bool applyToCardless = true)
    {
        var rule = new RelicCardUpgradeRule("CardlessUpgrade", true, lifecycle, filters);
        return new RelicCardModifier(0, rule, upgrade, applyToCardless, Array.Empty<RelicConditionState>());
    }

    private static CombatRelicState Relic(RelicCardModifier modifier) => new("relic-id", "CardlessUpgradeRelic",
        ["RelicEffectAddTempUpgrade"], isCovenant: false, disallowedInPlacementPhase: false, cardModifiers: [modifier]);

    private static RoomCombatState State(CombatUnit unit, RelicCardModifier modifier, IReadOnlyList<CardInstanceState>? cards = null)
    {
        UnityRng rng = UnityRng.Seed(17);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 1, 10,
            statusRules: [new CombatStatus("multistrike", 1)], cardInstances: cards ?? [], relics: [Relic(modifier)]);
        return new RoomCombatState(0, false, [unit], [], context);
    }

    private static CombatUnit Unit(bool cardless = true, bool clone = false, CombatTeam team = CombatTeam.Player,
        IReadOnlyList<string>? subtypes = null, IReadOnlyList<CardUpgradeModifier>? upgrades = null)
    {
        var statuses = cardless ? new[] { new CombatStatus("cardless", 1) } : Array.Empty<CombatStatus>();
        return new CombatUnit(1, "test-unit", team, 6, 10, 10, true, false, false, statuses,
            size: 2, subtypes: subtypes ?? ["eligible"],
            modifiers: new UnitModifiers(6, 0, 0, 2, 0, true, clone, upgrades ?? []), statusRegistry: statuses);
    }

    private static RoomCombatResult Apply(RoomCombatState state, int fromCardId = 0) =>
        RelicSpawnStatusModel.CharacterAdded(state, 1, fromCardId, onlyCovenants: false);

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
