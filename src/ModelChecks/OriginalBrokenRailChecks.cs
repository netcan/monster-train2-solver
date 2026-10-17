using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalBrokenRailChecks
{
    private const string OwnerId = "1699b5c7-8c34-471c-8e5a-c9830e2bc482";
    private const string UpgradeId = "5941c92e-cdb4-4732-841c-3ab1ddbdb688";
    private const string CardId = "fc119000-0000-4000-8000-000000000855";

    internal static void Run()
    {
        CheckForgeEligibilityComparison();
        const int points = 3;
        CardUpgradeModifier baseline = Upgrade("default-forge", damage: 1, health: 2);
        CardUpgradeModifier brokenRail = Upgrade(UpgradeId, health: 2, statuses: [new CombatStatus("armor", 2)],
            maskMetadata: new CardUpgradeMaskMetadata([new UpgradeMaskStatus("armor", 2)], false, false, false, false));
        CombatRelicState owner = Owner();
        CardInstanceState card = CardInstanceState.Empty(42, CardId);
        CombatContext source = Context(card, points, true, true, owner);
        BattlePlayRules rules = Rules(baseline, brokenRail);
        CardPlayRule eligible = CardRule(true);

        ForgeUpgradeModelResult result = ForgeUpgradeModel.BeforeDirectCardPlay(source, rules, eligible, card, points);
        CardInstanceState forged = result.Context?.FindCard(card.InstanceId) ?? throw new InvalidOperationException("Forge did not return the card state.");
        CardUpgradeModifier scaledDefault = forged.Temporary.Upgrades.Single(upgrade => upgrade.DataId == baseline.DataId);
        CardUpgradeModifier scaledRelic = forged.Temporary.Upgrades.Single(upgrade => upgrade.DataId == UpgradeId);
        Require(result.Supported && result.Context!.QueryFrame!.ForgePoints == 0 && forged.LastForgedAmount == points &&
            scaledDefault.Stats.Damage == 3 && scaledDefault.Stats.Health == 6 &&
            scaledRelic.Stats.Health == 6 && scaledRelic.Statuses.Single().Stacks == 6 &&
            scaledRelic.MaskMetadata!.Statuses.Single().Count == 6,
            "Forge points did not scale and apply the default and BrokenRail upgrades.");
        Require(source.QueryFrame!.ForgePoints == points && source.FindCard(card.InstanceId)!.Temporary.Upgrades.Count == 0,
            "Forge application mutated its immutable source context.");

        bool DidNotSpend(ForgeUpgradeModelResult result) => result.Supported &&
            result.Context!.QueryFrame!.ForgePoints == points &&
            result.Context.FindCard(card.InstanceId)!.Temporary.Upgrades.Count == 0 &&
            result.Context.FindCard(card.InstanceId)!.LastForgedAmount == 0;
        Require(DidNotSpend(Apply(source, rules, eligible, card, tracking: false)) &&
            DidNotSpend(Apply(source, rules, eligible, card, toggle: false)) &&
            DidNotSpend(Apply(source, rules, CardRule(false), card)) &&
            DidNotSpend(ForgeUpgradeModel.BeforeDirectCardPlay(source, rules, eligible, card, 0)),
            "A tracking, toggle, eligibility, or zero-points control incorrectly spent forge points.");
        Require(!ForgeUpgradeModel.BeforeDirectCardPlay(Context(card, points, true, true, Owner("wrong-owner")), rules,
            eligible, card, points).Supported,
            "A mismatched native forge relic owner passed validation.");
        Require(!ForgeUpgradeModel.BeforeDirectCardPlay(source, Rules(baseline, brokenRail, roomOverride: true),
            eligible, card, points).Supported,
            "A room-specific forge replacement passed the unsupported boundary.");
        Require(!ForgeUpgradeModel.BeforeDirectCardPlay(source, rules, CardRule(true, "Spell"), card, points).Supported,
            "A non-monster forge play passed the unsupported boundary.");

        var legacyRule = CardRule(null);
        var legacyRules = new BattlePlayRules([], [legacyRule]);
        CombatContext legacy = Context(card, points, null, null, null);
        Require(ForgeUpgradeModel.BeforeDirectCardPlay(legacy, legacyRules, legacyRule, card, points).Context == legacy,
            "Legacy captures without forge metadata should retain their pre-forge behavior.");
        Console.WriteLine("ORIGINAL-BROKEN-RAIL-PURE PASS: scaled default/relic upgrades, LastForgedAmount, point reset, immutable input, inactive controls and explicit unsupported boundaries.");
    }

    private static void CheckForgeEligibilityComparison()
    {
        CardPlayRule Rule(bool? eligible) => new(CardId, "PojuBrokenRailCarrier", 0, "SpawnMonster", "Standby", null, [], forgeEligible: eligible);
        BattlePlayRules missing = new([], [Rule(null)]);
        BattlePlayRules ineligible = new([], [Rule(false)]);
        string inactiveMissing = JsonSerializer.Serialize(BattleTurnChecks.ComparablePlayRules(missing, true, false));
        string inactiveIneligible = JsonSerializer.Serialize(BattleTurnChecks.ComparablePlayRules(ineligible, true, false));
        string activeMissing = JsonSerializer.Serialize(BattleTurnChecks.ComparablePlayRules(missing, true, true));
        string activeIneligible = JsonSerializer.Serialize(BattleTurnChecks.ComparablePlayRules(ineligible, true, true));
        Require(inactiveMissing == inactiveIneligible && activeMissing != activeIneligible,
            "Forge eligibility comparison must be normalized only when no forge points remain.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 176 &&
            root.GetProperty("ModifierScenario").GetString() == "original-broken-rail" &&
            root.GetProperty("OriginalBrokenRailPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0),
            "Original BrokenRail battle is incomplete or has native/model failures.");

        FixtureValue[] entries = root.GetProperty("Actions").EnumerateArray()
            .Where(row => row.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(entries.Length == 1, "The native BrokenRail scenario must play exactly one authored carrier.");
        FixtureValue entry = entries[0];
        Require(entry.GetProperty("Actual").ValueKind != FixtureKind.Null &&
            entry.GetProperty("Difference").ValueKind == FixtureKind.Null,
            "The authored BrokenRail card action did not match its native transition.");
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext beforeContext = before.Spawn.Train.Context!;
        CombatContext afterContext = after.Spawn.Train.Context!;
        string cardDataId = beforeContext.Cards.Hand.Single(card => card.InstanceId == action.CardInstanceId).DataId;
        CardPlayRule cardRule = before.PlayRules!.Cards.Single(rule => rule.DataId == cardDataId);
        ForgeUpgradeDefinition forge = before.PlayRules.ForgeUpgrade!;
        ForgeUpgradeModifier relicUpgrade = forge.RelicUpgrades.Single(upgrade => upgrade.RelicDataId == OwnerId);
        CombatRelicState owner = beforeContext.Relics!.Single(relic => relic.DataId == OwnerId);
        CardInstanceState forgedCard = afterContext.FindCard(action.CardInstanceId)
            ?? throw new InvalidOperationException("The forged carrier card is absent from native card state.");
        CardUpgradeModifier actualRelicUpgrade = forgedCard.Temporary.Upgrades.Single(upgrade => upgrade.DataId == UpgradeId);
        CombatUnit spawned = after.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.SpawnerCardId == action.CardInstanceId);
        int baseHealth = cardRule.SpawnUnit?.MaxHealth ?? throw new InvalidOperationException("Missing native BrokenRail monster definition.");
        int defaultHealth = forge.DefaultUpgrade?.Stats.Health ?? throw new InvalidOperationException("Missing native default forge upgrade.");

        Require(before.ForgePoints == 3 && beforeContext.QueryFrame?.ForgePoints == 3 &&
            beforeContext.QueryFrame.ForgeTrackingActive == true && beforeContext.QueryFrame.ForgeToggleActive == true &&
            cardRule.ForgeEligible == true && cardRule.Effect == "SpawnMonster" &&
            owner.AssetKey == "BrokenRail" && owner.EffectTypes.SequenceEqual([RelicModel.ModifyForgeUpgrade]) &&
            relicUpgrade.Upgrade.DataId == UpgradeId && relicUpgrade.Upgrade.Stats.Health == 2 &&
            relicUpgrade.Filters.Count == 0,
            "The fixture does not capture BrokenRail's unchanged eligible native +2 HP upgrade inputs.");
        Require(afterContext.QueryFrame?.ForgePoints == 0 && forgedCard.LastForgedAmount == 3 &&
            actualRelicUpgrade.Stats.Health == 6 && spawned.MaxHealth == baseHealth + 3 * defaultHealth + 6,
            "Native summon did not spend three points and gain the scaled default plus six BrokenRail HP.");

        BattleActionChecks.Native(root);
        Console.WriteLine($"ORIGINAL-BROKEN-RAIL NATIVE PASS: one unchanged BrokenRail owner; 3 forge points spent; +6 relic HP plus {3 * defaultHealth} default HP; native action matched.");
    }

    private static ForgeUpgradeModelResult Apply(CombatContext source, BattlePlayRules rules,
        CardPlayRule rule, CardInstanceState card, bool? tracking = true, bool? toggle = true)
    {
        var frame = source.QueryFrame!.With(forgeTrackingActive: tracking, forgeToggleActive: toggle);
        return ForgeUpgradeModel.BeforeDirectCardPlay(source.WithQueryFrame(frame), rules, rule, card,
            source.QueryFrame.ForgePoints!.Value);
    }

    private static CombatContext Context(CardInstanceState card, int points, bool? tracking, bool? toggle,
        CombatRelicState? owner)
    {
        var rng = UnityRng.Seed(37);
        var context = new CombatContext(new CardCycleState([new CardToken(card.InstanceId, card.DataId)], [], [], rng, 0, []),
            rng, 0, card.InstanceId + 1, 10, cardInstances: [card], cardRegistry: [card],
            queryFrame: new StatisticQueryFrame(forgePoints: points, forgeTrackingActive: tracking, forgeToggleActive: toggle),
            relics: owner == null ? [] : [owner]);
        return context;
    }

    private static CombatRelicState Owner(string id = OwnerId) =>
        new(id, "BrokenRail", [RelicModel.ModifyForgeUpgrade]);

    private static CardUpgradeModifier Upgrade(string id, int damage = 0, int health = 0,
        IReadOnlyList<CombatStatus>? statuses = null, CardUpgradeMaskMetadata? maskMetadata = null) =>
        new(id, id, new CardStatModifier(damage: damage, health: health), statuses ?? [],
            false, false, false, 0, 0, [], maskMetadata: maskMetadata);

    private static CardPlayRule CardRule(bool? eligible, string effect = "SpawnMonster") =>
        new(CardId, "PojuBrokenRailCarrier", 0, effect, "Standby", null, [], forgeEligible: eligible);

    private static BattlePlayRules Rules(CardUpgradeModifier baseline, CardUpgradeModifier relic, bool roomOverride = false) =>
        new([], [CardRule(true)], forgeUpgrade: new ForgeUpgradeDefinition(baseline, roomOverride,
            [new ForgeUpgradeModifier(OwnerId, "BrokenRail", 0, relic, [])]));

    private static void Require(bool pass, string message)
    { if (!pass) throw new InvalidOperationException(message); }
}
