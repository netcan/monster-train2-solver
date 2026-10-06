using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class UnitTriggerUpgradeChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(95);
        var owner = new CardInstanceState(8, "owner", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            unitUpgradeScalingTraits: [new(new("AnyCharacter"), "Damage", 2), new(new("AnyCharacter"), "Health", 10, 1)]);
        var other = new CardInstanceState(9, "other", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            unitUpgradeScalingTraits: [new(new("AnyCharacter"), "Damage", 99)]);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 10, 10,
            statistics: BattleStatistics.Empty().TrackCards([8, 9]), cardInstances: [owner, other],
            otherPiles: [new("Standby", [new(8, "owner"), new(9, "other")]), new("Exhausted", [])]);
        CardUpgradeModifier Upgrade(int damage = 1, int health = 1, string id = "upgrade") =>
            new(id, id, new(damage: damage, health: health), [], false, false, false, 0, 0, []);
        CombatEffect Effect(CardUpgradeModifier upgrade, string lifetime = "TemporaryUntilEndOfBattle", string target = "Self", bool remove = false,
            CardTargetFilters? filters = null, CardEffectTests? tests = null, bool enemies = false) =>
            new(remove ? "CardEffectRemoveTempUpgradeFromUnit" : "CardEffectAddCardUpgradeToUnits", 0, 0, "", 0, [], false,
                unitUpgrade: new(remove ? "RemoveUnitUpgrade" : "UnitUpgrade", target, 0, enemies, !enemies, [], upgrade, lifetime, tests, filters: filters));
        CombatTrigger Trigger(CombatEffect[] effects, string kind = "OnHeal", bool once = false, bool ignoreSilence = false, int count = 1) =>
            new(kind, once, false, ignoreSilence, count, effects, false);
        CombatUnit Unit(int id, int spawner, CombatTrigger[] triggers, CombatStatus[]? statuses = null) =>
            new(id, "unit", CombatTeam.Player, 2, 5, 10, true, false, false, statuses ?? [], triggers, spawner, 1,
                modifiers: new(2, 0, 0, 1, 1, true, false, []), isBoss: false);
        RoomCombatState Room(CombatUnit[] units, bool preview = false) => new(0, false, units, [], context, preview);
        CardUpgradeModifier upgrade = Upgrade();
        var repeated = Trigger([Effect(upgrade), Effect(upgrade), Effect(upgrade, remove: true)]);
        var once = Trigger([Effect(Upgrade(id: "permanent"), "Permanent")], once: true);
        var root = Room([Unit(1, 8, [repeated, once])]);
        string parent = JsonSerializer.Serialize(root);
        var first = RoomCombatModel.ApplyCardHeal(root, 1, 0);
        Require(first.Supported && first.State!.Units.Single().BaseAttack == 9 && first.State.Units.Single().MaxHealth == 11 &&
            first.State.Units.Single().Modifiers!.Upgrades.Single().DataId == "permanent" &&
            first.State.Context!.CardInstances!.Single(card => card.InstanceId == 8).Temporary.Upgrades.Count == 0 &&
            first.State.Context.CardInstances!.Single(card => card.InstanceId == 8).Permanent.Upgrades.Single().Stats.Damage == 3,
            "Trigger upgrade order, source scaling, spawn restriction, base-valued removal or lifetimes differ: " +
            first.UnsupportedReason + " " + JsonSerializer.Serialize(first.State));
        var second = RoomCombatModel.ApplyCardHeal(first.State!, 1, 0);
        Require(second.Supported && second.State!.Units.Single().BaseAttack == 13 && second.State.Units.Single().MaxHealth == 11 &&
            second.State.Context!.CardInstances!.Single(card => card.InstanceId == 8).Permanent.Upgrades.Count == 1,
            "Repeated triggers lost once-only state or max-health increases recursively fired OnHeal.");
        var roomTargets = Room([Unit(1, 8, [Trigger([Effect(upgrade, target: "Room")])]), Unit(2, 9, [])]);
        var area = RoomCombatModel.ApplyCardHeal(roomTargets, 1, 0);
        Require(area.Supported && area.State!.Units.All(unit => unit.BaseAttack == 5 && unit.MaxHealth == 11) &&
            area.State.Context!.CardInstances!.All(card => card.Temporary.Upgrades.Single().Stats.Damage == 3),
            "Room upgrades used the target's traits instead of the triggering unit's spawner.");
        var bypass = new CardTargetFilters("Damaged", ["silenced"], ["lifesteal"], false, "unknown", [""]);
        var self = Room([Unit(1, 8, [Trigger([Effect(upgrade, filters: bypass, enemies: true)])], [new("untouchable", 1)])]);
        Require(RoomCombatModel.ApplyCardHeal(self, 1, 0).State!.Units.Single().BaseAttack == 5,
            "Self targeting applied room/team/status/subtype filters.");
        var blocked = Room([Unit(1, 8, [Trigger([Effect(upgrade, target: "Room", enemies: true)], once: true)])]);
        var noTargets = RoomCombatModel.ApplyCardHeal(blocked, 1, 0);
        Require(noTargets.Supported && !noTargets.State!.Units.Single().Triggers.Single().HasTriggered,
            "Failed trigger preflight consumed its once-only state.");
        var generation = new CombatEffect("CardEffectAddBattleCard", 0, 0, "HandPile", 1, ["junk"], false);
        var mixedPreview = Room([Unit(1, 8, [Trigger([Effect(upgrade, target: "Room", enemies: true), generation], once: true)])], preview: true);
        Require(!RoomCombatModel.ApplyCardHeal(mixedPreview, 1, 0).State!.Units.Single().Triggers.Single().HasTriggered,
            "Preview-ineligible generation permitted a failed mixed upgrade trigger.");
        var canceled = Room([Unit(1, 8, [Trigger([Effect(upgrade, target: "Room", enemies: true,
            tests: new(true, false, true, false)), Effect(upgrade)])])]);
        Require(RoomCombatModel.ApplyCardHeal(canceled, 1, 0).State!.Units.Single().BaseAttack == 2,
            "Failed effect did not cancel subsequent upgrades.");
        var mandatory = Room([Unit(1, 8, [Trigger([Effect(upgrade), Effect(upgrade, target: "Room", enemies: true,
            tests: new(true, true, false, false))])])]);
        Require(!RoomCombatModel.ApplyCardHeal(mandatory, 1, 0).State!.Units.Single().Triggers.Single().HasTriggered,
            "Mandatory target failure did not block the whole trigger.");
        var silent = Room([Unit(1, 8, [repeated, Trigger([Effect(upgrade, "TemporaryUntilUnitDeath")], ignoreSilence: true)], [new("silenced", 1)])]);
        Require(RoomCombatModel.ApplyCardHeal(silent, 1, 0).State!.Units.Single().BaseAttack == 5,
            "Silence blocked an ignored-silence trigger or fired an ordinary trigger.");
        var simpleOwner = CardInstanceState.Empty(8, "owner");
        var simpleContext = new CombatContext(context.Cards, rng, 0, 10, 10, statistics: context.Statistics,
            cardInstances: [simpleOwner, other], otherPiles: context.OtherPiles);
        CombatUnit attacker = Unit(1, 8, [Trigger([Effect(Upgrade(3, 1), "TemporaryUntilUnitDeath")])],
            [new("lifesteal", 2, removeWhenTriggered: true), new("multistrike", 1, 2)]);
        var enemy = new CombatUnit(3, "enemy", CombatTeam.Enemy, 0, 100, 100, true, false, false, []);
        var combat = new RoomCombatState(0, false, [enemy, attacker], [], simpleContext);
        var exchange = RoomCombatModel.Exchange(combat);
        Require(exchange.Supported && exchange.State!.Units.Single(unit => unit.Id == 3).Health == 93 &&
            exchange.State.Units.Single(unit => unit.Id == 1).BaseAttack == 8 &&
            exchange.State.Units.Single(unit => unit.Id == 1).Health == 12,
            "Lifesteal upgrades detached the active attacker or subsequent strike used stale stats.");
        var onceAttacker = Unit(1, 8, [Trigger([Effect(Upgrade(3, 1), "TemporaryUntilEndOfBattle")], once: true)],
            [new("lifesteal", 2, removeWhenTriggered: true), new("multistrike", 1, 2)]);
        var preview = RoomCombatModel.Exchange(new(0, false, [enemy, onceAttacker], [], simpleContext, preview: true));
        Require(preview.Supported && preview.State!.Units.Single(unit => unit.Id == 1).BaseAttack == 5 &&
            preview.State.Context!.CardInstances!.Single(card => card.InstanceId == 8).Temporary.Upgrades.Count == 0,
            "Nested preview upgrade reset the once-only flag or wrote the source card.");
        var postCombat = Room([Unit(1, 8, [Trigger([Effect(upgrade, "TemporaryUntilUnitDeath")], kind: "PostCombat")])]);
        Require(RoomCombatModel.Resolve(postCombat).State!.Units.Single().BaseAttack == 5,
            "PostCombat upgrades did not settle.");
        var rngRoot = Room([Unit(1, 8, [Trigger([Effect(upgrade, target: "RandomInRoom")])]), Unit(2, 9, [])]);
        var random = RoomCombatModel.ApplyCardHeal(rngRoot, 1, 0);
        RngDraw expected = rng.Range(0, 2);
        Require(random.Supported && random.State!.Context!.BattleRng.Equals(expected.State) &&
            random.State.Units.Single(unit => unit.BaseAttack == 5).Id == expected.Value + 1,
            "Random upgrade tests consumed RNG or target application lost the draw.");
        var lethal = Upgrade(0, -100, "lethal");
        var deathReward = new CombatTrigger("OnDeath", false, false, false, 1,
            [new("CardEffectRewardGold", 1, 0, "", 0, [], false)], false);
        var dying = Room([Unit(1, 8, [Trigger([Effect(lethal, "TemporaryUntilUnitDeath")]), deathReward])]);
        var killed = RoomCombatModel.ApplyCardHeal(dying, 1, 0);
        Require(killed.Supported && killed.State!.Units.Count == 0 && killed.State.Context!.Gold == 5 &&
            killed.State.Context.Statistics!.MonstersDeadThisBattle == 1,
            "Lethal trigger upgrade failed to settle nested death effects/statistics once.");
        var invalid = Room([Unit(1, 8, [Trigger([Effect(upgrade)], kind: "OnDeath")])]);
        Require(!RoomCombatModel.Resolve(invalid).Supported, "Dead self upgrade routing was silently accepted.");
        string result = JsonSerializer.Serialize(first.State);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(RoomCombatModel.ApplyCardHeal(root, 1, 0).State) == result,
            "Parallel triggered upgrades diverged."));
        Require(JsonSerializer.Serialize(root) == parent, "Triggered upgrades mutated parent state.");
        Console.WriteLine("UNIT-TRIGGER-UPGRADE-CHECKS PASS: source ownership/scaling, continuous removal, lifetimes, target tests/filters/RNG, once/silence, active attacks, nested death, preview and 32 parallel branches.");
    }
    internal static void Native(JsonElement fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out JsonElement scenario) || scenario.GetString() != "unit-trigger-upgrades") return;
        int live = 0, heal = 0, postCombat = 0, skipped = 0, magic = 0;
        foreach (JsonElement sample in fixture.GetProperty("UnitUpgradeScaling").EnumerateArray())
        {
            Require(sample.GetProperty("Origin").GetString() == "Live" && sample.GetProperty("Difference").ValueKind == JsonValueKind.Null &&
                sample.GetProperty("CaptureError").ValueKind == JsonValueKind.Null, "Native trigger callback capture is incomplete.");
            CombatContext before = sample.GetProperty("Before").Deserialize<CombatContext>(ModelJson.Options)!;
            CombatContext after = sample.GetProperty("After").Deserialize<CombatContext>(ModelJson.Options)!;
            ScalingUnitUpgradeTrait trait = sample.GetProperty("Trait").Deserialize<ScalingUnitUpgradeTrait>()!;
            CardUpgradeModifier original = sample.GetProperty("BeforeUpgrade").Deserialize<CardUpgradeModifier>()!;
            CardUpgradeModifier actual = sample.GetProperty("AfterUpgrade").Deserialize<CardUpgradeModifier>()!;
            string? kind = sample.GetProperty("TriggerKind").GetString();
            var result = UnitUpgradeScalingModel.ApplyTrait(before, trait, sample.GetProperty("OwnerCardId").GetInt32(), original, kind);
            Require(result.Supported && JsonSerializer.Serialize(result.Upgrade) == JsonSerializer.Serialize(actual) &&
                JsonSerializer.Serialize(result.Context) == JsonSerializer.Serialize(after), "Independent trigger callback differs from native.");
            live++; heal += kind == "OnHeal" ? 1 : 0; postCombat += kind == "PostCombat" ? 1 : 0;
            skipped += trait.Restriction == 1 && JsonSerializer.Serialize(original) == JsonSerializer.Serialize(actual) ? 1 : 0;
            magic += original.MagicPowerTraitScalingOnly ? 1 : 0;
        }
        Require(live >= 30 && heal > 0 && postCombat > 0 && skipped > 0 && magic > 0,
            "Native trigger upgrades lack healing/post-combat/scaling gate coverage.");
        CombatUnit[] units = fixture.GetProperty("Stages").EnumerateArray().SelectMany(stage =>
            stage.GetProperty("Actual").GetProperty("Units").Deserialize<CombatUnit[]>()!).ToArray();
        int silentOnce = units.Count(unit => unit.Statuses.Any(status => status.Id == "silenced") && unit.Triggers.Any(trigger =>
            trigger.Once && trigger.IgnoreSilence && trigger.HasTriggered && trigger.Effects.Any(effect => effect.UnitUpgrade != null)));
        Require(silentOnce > 0 && units.Any(unit => unit.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "PojuTriggerPermanent")) &&
            units.Any(unit => unit.Modifiers!.Upgrades.Count(upgrade => upgrade.AssetKey == "PojuTriggerUntilDeath") > 1) &&
            units.All(unit => unit.Modifiers!.Upgrades.All(upgrade => upgrade.AssetKey != "PojuTriggerTemporary")),
            "Native trigger upgrades lack ignored-silence, permanent, repeated until-death or completed removal coverage.");
        Console.WriteLine($"NATIVE-UNIT-TRIGGER-UPGRADE-CHECKS PASS: {live} callbacks, {heal} OnHeal, {postCombat} PostCombat, {skipped} restricted, {magic} magic-only skips and {silentOnce} silent once-upgraded units; complete states checked.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
