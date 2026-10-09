using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class CardModifierChecks
{
    private static CardUpgradeModifier Upgrade(CardStatModifier stats, bool discard = false, string id = "") =>
        new(id, "", stats, [], discard, false, false, 0, 0, []);
    internal static void Run()
    {
        var permanent = new CardModifiers(new(damage: 2, health: 3), [Upgrade(new(damage: 5, health: 12, cost: -1, size: 1))], 0, []);
        var temporary = new CardModifiers(new(), [Upgrade(new(damage: 3, health: 7, cost: -1, size: -1), true)], 0, []);
        var cheap = new CardModifiers(new(), [Upgrade(new(cost: -100)), Upgrade(new(cost: 10))], 0, []);
        Require(CardModifierModel.UpgradedStat(1, "Cost", true, cheap) == 10, "A large cost change did not clamp immediately.");
        var small = new CardModifiers(new(), [Upgrade(new(cost: -5)), Upgrade(new(cost: 8))], 0, []);
        Require(CardModifierModel.UpgradedStat(1, "Cost", true, small) == 4, "Small changes clamped between upgrades.");
        var low = new CardModifiers(new(), [Upgrade(new(damage: -8))], 0, []);
        var high = new CardModifiers(new(), [Upgrade(new(damage: 5))], 0, []);
        Require(CardModifierModel.UpgradedStat(3, "Damage", true, low, high) == 0 &&
            CardModifierModel.UpgradedStat(CardModifierModel.UpgradedStat(3, "Damage", true, low), "Damage", true, high) == 5,
            "Unit combined and spell separate modifier group clamps differ.");
        var wrapped = new CardModifiers(new(), [Upgrade(new(damage: int.MaxValue, xCost: int.MaxValue)), Upgrade(new(damage: 1, xCost: 1))], 0, []);
        Require(CardModifierModel.UpgradedStat(0, "Damage", false, wrapped) == int.MinValue &&
            CardModifierModel.UpgradedStat(0, "Damage", true, wrapped) == 0 &&
            CardModifierModel.UpgradedStat(0, "XCost", true, wrapped) == int.MinValue,
            "Native signed integer wrap did not precede the requested final floor.");
        Require(CardModifierModel.UpgradedStat(99, "Cost", true, new CardModifiers(new(cost: int.MaxValue), [], 0, [])) == 0,
            "A large overflowing cost offset must wrap before its immediate clamp.");
        bool minimumFailed = false;
        try { CardModifierModel.UpgradedStat(0, "Size", false, new CardModifiers(new(size: int.MinValue), [], 0, [])); }
        catch (OverflowException) { minimumFailed = true; }
        Require(minimumFailed, "Native Mathf.Abs(int.MinValue) exception was silently converted into a numeric result.");
        var instance = new CardInstanceState(1, "unit", permanent, temporary, 0, 0, 0, []);
        var unit = new CombatUnit(0, "steward", CombatTeam.Player, 8, 25, 25, true, false, false, [], size: 3);
        var rule = new CardPlayRule("unit", "unit", 1, "SpawnMonster", "Standby", unit, []);
        var resolved = CardModifierModel.Resolve(rule, instance);
        Require(resolved.Cost == 0 && resolved.SpawnUnit!.BaseAttack == 18 && resolved.SpawnUnit.MaxHealth == 47 &&
            resolved.SpawnUnit.Size == 3, "Permanent offsets and independent upgrade groups were not applied.");
        var rng = UnityRng.Seed(1);
        var context = new CombatContext(new CardCycleState([new(1, "unit"), new(2, "unit")], [], [], rng, 0, []), rng,
            0, 3, 10, cardInstances: [instance, CardInstanceState.Empty(2, "unit")]);
        var rooms = new[] { new RoomCombatState(0, false, [], [], context), new RoomCombatState(1, false, [], [], context) };
        var train = new TrainCombatState(rooms, [], 5, context);
        var spawn = new EnemySpawnState(train, [], [], 0, false, rng, 1, [], 0, false, 0, 0, 2, []);
        var root = new BattleTurnState(spawn, 1, 3, 5, 0, 0, "Full", [],
            [new("Standby", []), new("Exhausted", []), new("Purged", []), new("Eaten", [])], [],
            new BattlePlayRules([new(0, 6, 5, true, false, false)], [rule]));
        string parent = JsonSerializer.Serialize(root);
        var first = BattleActionModel.PlayCard(root, new(1, 0));
        Require(first.Supported && first.State!.Energy == 1 && first.State.Spawn.Train.Rooms[0].Units.Single().Health == 47 &&
            first.State.Spawn.Train.Context!.CardInstances!.Single(card => card.InstanceId == 1).Temporary.Upgrades.Count == 0,
            "Summon did not use the pre-discard upgrade or remove its temporary source afterward: " + first.Reason);
        var next = first.State!.Spawn.Train.Context!.CardInstances!.Single(card => card.InstanceId == 1);
        Require(next.PlayCount == 1 && next.LastPlayedCost == 0 && next.Permanent.Upgrades.Count == 1 &&
            first.State.Spawn.Train.Context.CardInstances!.Single(card => card.InstanceId == 2).PlayCount == 0,
            "Instance play history or identical unmodified copy changed.");
        var second = BattleActionModel.PlayCard(first.State, new(2, 0));
        Require(second.Supported && second.State!.Energy == 0 && second.State.Spawn.Train.Rooms[0].Units.Last().BaseAttack == 8,
            "Another identical card inherited the first copy's upgrades.");
        Parallel.For(0, 32, _ => Require(BattleTurnChecks.Comparable(BattleActionModel.PlayCard(root, new(1, 0)).State!) ==
            BattleTurnChecks.Comparable(first.State), "Parallel modifier branches differ."));
        Require(JsonSerializer.Serialize(root) == parent, "Playing an upgraded card changed its parent.");
        var duplicates = new CardModifiers(new(), [Upgrade(new(damage: 2), false, "same"), Upgrade(new(damage: 3), true, "same")], 0, []);
        var discarded = new CardInstanceState(5, "spell", CardModifiers.Empty(), duplicates, 1, 0, 1, []).OnDiscard(false);
        Require(discarded.Temporary.Upgrades.Count == 0,
            "Native reverse discard loop must revisit the shifted upgrade after removing the first matching data ID.");
        StartingStatusRegistry();
        Console.WriteLine("MODIFIER-CHECKS PASS: ordered clamps, permanent/temporary groups, discard lifecycle, per-instance actions and isolation.");
    }
    private static void StartingStatusRegistry()
    {
        var historical = new CombatStatus("buff", 0);
        var unit = new CombatUnit(0, "unit", CombatTeam.Player, 1, 1, 1, true, false, false, [], statusRegistry: [historical]);
        var rule = new CardPlayRule("unit", "unit", 0, "SpawnMonster", "Standby", unit, []);
        foreach (int[] stacks in new[] { new[] { 0 }, new[] { -3 }, new[] { 3, -3 }, new[] { 0, 3 } })
        {
            var upgrades = stacks.Select(count => new CardUpgradeModifier("", "", new(), [new("armor", count)],
                false, false, false, 0, 0, [])).ToArray();
            var instance = new CardInstanceState(1, "unit", CardModifiers.Empty(), new(new(), upgrades, 0, []), 0, 0, 0, []);
            CombatUnit resolved = CardModifierModel.Resolve(rule, instance).SpawnUnit!;
            int expected = stacks.SequenceEqual([0, 3]) ? 3 : 0;
            Require(resolved.Statuses.Sum(status => status.Stacks) == expected &&
                resolved.StatusRegistry!.Count == (expected > 0 ? 2 : 1) && resolved.StatusRegistry[0].Id == "buff",
                "Spawn upgrades created an inactive status definition or discarded the captured zero-stack registry.");
        }
    }
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        int count = 0;
        foreach (var sample in document.RootElement.GetProperty("Samples").EnumerateArray())
        {
            var modifiers = sample.GetProperty("Modifiers").Deserialize<CardModifiers[]>()!;
            Require(CardModifierModel.UpgradedStat(sample.GetProperty("BaseValue").GetInt32(), sample.GetProperty("Stat").GetString()!,
                sample.GetProperty("EnforceFloor").GetBoolean(), modifiers) == sample.GetProperty("Actual").GetInt32(),
                "Native ordered modifier calculation differs at " + count);
            count++;
        }
        Require(count > 0, "Missing native modifier samples.");
        Console.WriteLine("NATIVE-MODIFIER-CHECKS PASS: " + count + " native calculations across all eight statistics.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
