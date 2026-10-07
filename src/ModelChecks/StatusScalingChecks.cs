using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class StatusScalingChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(431);
        var traits = new ScalingStatusTrait[] { new(new("AnyCharacter"), 3, true, 0, ["armor"]),
            new(new("Gold"), 100, true, 0, ["armor"]), new(new("AnyCharacter"), -1, false, 0, ["armor"]) };
        var owner = new CardInstanceState(1, "spell", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [], statusScalingTraits: traits);
        var statistics = new BattleStatistics([], [], [], [], [], [], [], 0, 0, 0, 0, 0, [1, 99], [1]);
        var context = new CombatContext(new([new(1, "spell")], [], [], rng, 0, []), rng, 0, 2, 10,
            statistics: statistics, cardInstances: [owner], otherPiles: [new("Exhausted", [])]);
        var unit = new CombatUnit(10, "target", CombatTeam.Enemy, 1, 10, 10, true, false, false, [new("armor", 5, stackable: true)]);
        var room = new RoomCombatState(0, false, [unit], [], context);
        string parent = JsonSerializer.Serialize(room);
        RoomCombatResult result = StatusApplicationModel.Apply(room, 10, new("armor", 0, stackable: true), 1);
        Require(result.Supported && Stacks(result.State!, "armor") == 7 &&
            result.State!.Context!.Statistics!.Value(1, "AnyStatusEffectStacksAdded") == 2 &&
            result.State.Context.Statistics.TrackedCards.SequenceEqual([1]),
            "Zero-only traits inspected existing stacks, ignored earlier bonuses, queried a skipped trait, or failed to refresh membership.");
        RoomCombatResult capped = StatusApplicationModel.Apply(room, 10, new("armor", 20000, stackable: true), 0);
        Require(capped.Supported && Stacks(capped.State!, "armor") == 9999 &&
            capped.State!.Context!.Statistics!.Values.Count == 0, "Stack cap or source-free attribution differs.");
        RoomCombatResult sourcedCap = StatusApplicationModel.Apply(room, 10, new("armor", 20000, stackable: true), 1, allowModification: false);
        Require(Stacks(sourcedCap.State!, "armor") == 9999 &&
            sourcedCap.State!.Context!.Statistics!.Value(1, "AnyStatusEffectStacksAdded") == 9994,
            "Statistics counted requested stacks instead of the applied positive delta.");
        RoomCombatResult removed = StatusApplicationModel.Apply(result.State!, 10, new("armor", -20), 1, allowModification: false);
        Require(removed.Supported && Stacks(removed.State!, "armor") == 0 &&
            removed.State!.Context!.Statistics!.Value(1, "AnyStatusEffectStacksAdded") == 2 &&
            removed.State.Context.Statistics.Value(1, "AnyStatusEffectStacksRemoved") == 0,
            "Negative additions underflowed stacks or impersonated a removal callback.");
        var nonstackable = StatusApplicationModel.Apply(new(0, false, [new(10, "target", CombatTeam.Player, 1, 10, 10,
            true, false, false, [])], [], context), 10, new("piercing", 12, stackable: false), 1, allowModification: false);
        Require(nonstackable.Supported && Stacks(nonstackable.State!, "piercing") == 1 &&
            nonstackable.State!.Context!.Statistics!.Value(1, "AnyStatusEffectStacksAdded") == 1, "Nonstackable cap differs.");
        var bad = new CardInstanceState(1, "spell", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            statusScalingTraits: [new(new("Gold"), 1, false, 0, ["armor"])]);
        var incomplete = new CombatContext(context.Cards, rng, 0, 2, 10, statistics: statistics, cardInstances: [bad]);
        var immune = new RoomCombatState(0, false, [new(10, "immune", CombatTeam.Player, 1, 10, 10, true, false, false,
            [new("immune", 1)])], [], incomplete);
        Require(StatusApplicationModel.Apply(immune, 10, new("armor", 1), 1).State == immune &&
            !StatusApplicationModel.Apply(immune, 10, new("armor", 1), 1, overrideImmunity: true).Supported,
            "Immunity ran after an unsupported query, or Pyre-style override failed to execute it.");
        RoomCombatResult preview = StatusApplicationModel.Apply(new(0, false, [unit], [], context, true), 10, new("armor", 0), 1);
        Require(preview.Supported && Stacks(preview.State!, "armor") == 7 && preview.State!.Context!.Statistics!.Values.Count == 0,
            "Preview failed to apply scaling or incremented live added-stack statistics.");
        var growing = new CardInstanceState(1, "spell", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            statusScalingTraits: [new(new("AnyStatusEffectStacksAdded"), 1, false, 0, ["armor"])]);
        var feedbackContext = new CombatContext(context.Cards, rng, 0, 2, 10,
            statistics: statistics.Increment(1, "AnyStatusEffectStacksAdded", 3), cardInstances: [growing], otherPiles: context.OtherPiles);
        var feedback = CardSpellModel.Apply(new(0, false, [new(10, "front", CombatTeam.Enemy, 1, 10, 10, true, false, false, []),
            new(11, "back", CombatTeam.Enemy, 1, 10, 10, true, false, false, [])], [], feedbackContext),
            [new("AddStatus", "Room", 0, true, false, [new("armor", 0, stackable: true)])], 0, 1);
        Require(feedback.Supported && Stacks(feedback.State!, "armor", 11) == 3 && Stacks(feedback.State!, "armor", 10) == 6 &&
            feedback.State!.Context!.Statistics!.Value(1, "AnyStatusEffectStacksAdded") == 12,
            "Successive reverse-order targets did not observe the preceding application's actual positive delta.");
        var propagate = new ScalingStatusTrait(new("AnyCharacter"), 4, false, 1, [], ["poison"], ["armor"]);
        Require(StatusScalingModel.ApplyTrait(context, propagate, 1, CombatTeam.Enemy, "armor", 0).Stacks == 0 &&
            StatusScalingModel.ApplyTrait(context, propagate, 1, CombatTeam.Player, "armor", 0).Stacks == 4 &&
            !StatusScalingModel.ApplyTrait(context, new(new("AnyCharacter"), 4, false, 1, []), 1, CombatTeam.Player, "armor", 0).Supported,
            "Propagation ignored team exclusion masks or guessed missing definitions.");
        Require(StatusScalingModel.ApplyTrait(context, new(new("AnyCharacter"), 6, false, 2, []), 1, CombatTeam.Player, "horde", 0).Stacks == 6 &&
            StatusScalingModel.ApplyTrait(null, new(new("Gold"), 6, false, 2, []), 1, CombatTeam.Player, "armor", 0).Stacks == 0 &&
            StatusScalingModel.ApplyTrait(null, new(new("Gold"), 6, false, 99, []), 1, CombatTeam.Player, "armor", 0).Stacks == 0,
            "Horde/unknown filter queried unmatched statuses.");
        var overflowContext = new CombatContext(context.Cards, rng, 0, 2, 10, statistics: statistics, cardInstances: [owner],
            otherPiles: context.OtherPiles, queryFrame: new(forgePoints: 2));
        Require(StatusScalingModel.ApplyTrait(overflowContext, new(new("ForgePoints"), int.MaxValue, false, 0, ["armor"]),
            1, CombatTeam.Enemy, "armor", 0).Stacks == -2, "Native integer product did not wrap.");
        Require(!StatusScalingModel.ApplyTrait(context, new(new("AnyStatusEffectStacksRemoved"), 1, false, 0, ["armor"]),
            1, CombatTeam.Enemy, "armor", 0).Supported && !StatusApplicationModel.Apply(room, 10, new("horde", 0), 1).Supported &&
            !StatusApplicationModel.Apply(room, 10, new("armor", 0), 999).Supported,
            "Unknown effects, incomplete removal attribution or missing source metadata produced usable states.");
        Require(owner.OnDiscard(true).StatusScalingTraits!.Count == 3, "Discard cleared scaling metadata.");
        var upgrade = new CardUpgradeModifier("upgrade", "upgrade", new(damage: 3), [], false, false, false, 0, 0, []);
        var upgradedUnit = new CombatUnit(10, "upgraded", CombatTeam.Player, 4, 20, 20, true, false, false, [],
            spawnerCardId: 1, size: 1, modifiers: new(4, 0, 0, 1, 1, true, false, []));
        var upgradeRoom = new RoomCombatState(0, false, [upgradedUnit], [], context);
        RoomCombatResult unitUpgrade = UnitModifierModel.Apply(upgradeRoom, 10, upgrade, "TemporaryUntilEndOfBattle");
        RoomCombatResult healthUpgrade = UnitHealthModel.Apply(upgradeRoom, 10, 4);
        RoomCombatResult handUpgrade = HandUpgradeModel.Apply(upgradeRoom, upgrade, "TemporaryUntilEndOfBattle",
            new([], [new("spell", "spell", 0, "Null", "Discard", null, [])]));
        foreach (RoomCombatResult changed in new[] { unitUpgrade, healthUpgrade, handUpgrade })
            Require(changed.Supported && Stacks(StatusApplicationModel.Apply(changed.State!, 10, new("armor", 0), 1).State!, "armor") == 2,
                "Changing card/unit upgrades or health erased later status scaling.");
        var generation = CardGenerationModel.Apply(context, new("HandPile", 1,
            [new("generated", CardModifiers.Empty(), null, [], statusScalingTraits: traits)]));
        Require(generation.Supported && generation.Context!.CardInstances!.Single(card => card.InstanceId == 2).StatusScalingTraits!.Count == 3 &&
            !CardGenerationModel.Apply(new(context.Cards, rng, 0, 2, 10), new("HandPile", 1,
                [new("generated", CardModifiers.Empty(), null, [], statusScalingTraits: traits)])).Supported,
            "Creation lost scaling metadata or accepted an incomplete identity context.");
        Parallel.For(0, 64, _ => Require(JsonSerializer.Serialize(StatusApplicationModel.Apply(room, 10, new("armor", 0), 1)) ==
            JsonSerializer.Serialize(result), "Parallel status applications diverged."));
        Require(JsonSerializer.Serialize(room) == parent, "Status application mutated its parent.");
        traits[0] = new(new("Gold"), 999, false, 0, ["armor"]);
        Require(owner.StatusScalingTraits![0].StacksPerStat == 3, "Caller mutation changed captured traits.");
        Console.WriteLine("STATUS-SCALING-CHECKS PASS: ordered zero gates, signed application/caps, attribution/preview/immunity, propagation masks, integer wrap, creation/discard and 64 parallel branches.");
    }
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out FixtureValue scenario) || scenario.GetString() != "status-scaling") return;
        int count = 0, applications = 0, capped = 0, decreased = 0, immune = 0;
        bool zeroApplied = false, zeroSkipped = false, negative = false, feedback = false, propagation = false;
        var filters = new HashSet<int>(); var moons = new HashSet<int>(); var turns = new HashSet<int>();
        foreach (FixtureValue sample in fixture.GetProperty("StatusScaling").EnumerateArray())
        {
            Require(sample.GetProperty("CaptureError").ValueKind == FixtureKind.Null && sample.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "Native status callback capture is incomplete or differs.");
            CombatContext before = sample.GetProperty("Before").Deserialize<CombatContext>()!;
            CombatContext after = sample.GetProperty("After").Deserialize<CombatContext>()!;
            ScalingStatusTrait trait = sample.GetProperty("Trait").Deserialize<ScalingStatusTrait>()!;
            int owner = sample.GetProperty("OwnerCardId").GetInt32(), source = sample.GetProperty("SourceStacks").GetInt32();
            StatusScalingResult result = StatusScalingModel.ApplyTrait(before, trait, owner,
                (CombatTeam)sample.GetProperty("TargetTeam").GetInt32(), sample.GetProperty("StatusId").GetString()!, source);
            int actual = sample.GetProperty("ActualBonus").GetInt32();
            Require(result.Supported && result.Stacks == actual && JsonSerializer.Serialize(result.Context) == JsonSerializer.Serialize(after),
                "Independent native status bonus or complete refreshed context differs.");
            filters.Add(trait.Filter); moons.Add(before.QueryFrame!.MoonPhase!.Value); turns.Add(before.QueryFrame.Turn!.Value);
            zeroApplied |= trait.OnlyWhenSourceZero && source == 0 && actual > 0;
            zeroSkipped |= trait.OnlyWhenSourceZero && source != 0 && actual == 0;
            negative |= actual < 0; feedback |= trait.Query.Type == "AnyStatusEffectStacksAdded" && actual < 0; count++;
            propagation |= trait.Filter == 1 && actual > 0;
        }
        foreach (FixtureValue sample in fixture.GetProperty("StatusApplications").EnumerateArray())
        {
            Require(sample.GetProperty("CaptureError").ValueKind == FixtureKind.Null && sample.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "Native status application capture is incomplete or differs.");
            RoomCombatState before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState after = sample.GetProperty("After").Deserialize<RoomCombatState>()!;
            int target = sample.GetProperty("TargetId").GetInt32();
            CombatStatus added = sample.GetProperty("Added").Deserialize<CombatStatus>()!;
            RoomCombatResult result = StatusApplicationModel.Apply(before, target, added, sample.GetProperty("SourceCardId").GetInt32(),
                sample.GetProperty("OverrideImmunity").GetBoolean(), sample.GetProperty("AllowModification").GetBoolean());
            Require(result.Supported && JsonSerializer.Serialize(result.State) == JsonSerializer.Serialize(after),
                "Independent native status application differs.");
            int old = Stacks(before, added.Id, target), current = Stacks(after, added.Id, target);
            capped += current == 9999 ? 1 : 0; decreased += current < old ? 1 : 0;
            immune += before.Units.Single(unit => unit.Id == target).Statuses.Any(status => status.Id == "immune") ? 1 : 0;
            applications++;
        }
        Require(count >= 60 && filters.Count == 3 && moons.Count == 2 && turns.Count >= 2 && zeroApplied && zeroSkipped && negative && feedback && propagation &&
            capped > 0 && decreased > 0 && immune > 0, "Native trace lacks meaningful scaling, cap, negative, immune or cross-turn coverage.");
        Console.WriteLine($"NATIVE-STATUS-SCALING-CHECKS PASS: {count} callbacks, {applications} applications, {capped} caps, {decreased} decreases, {immune} immune; complete contexts and independent full battle.");
    }
    private static int Stacks(RoomCombatState state, string id, int target = 10) =>
        state.Units.Single(unit => unit.Id == target).Statuses.FirstOrDefault(status => status.Id == id)?.Stacks ?? 0;
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
