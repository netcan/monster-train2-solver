using System.Collections.Concurrent;
using MonsterTrain2Poju.Model;

if (args.Length == 2 && args[0] == "--physical-decision-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    var physicalRoot = document.RootElement;
    if (physicalRoot.GetProperty("CaptureFailures").GetInt32() != 0 || physicalRoot.GetProperty("Pending").GetInt32() != 0 ||
        physicalRoot.GetProperty("DecisionSpawnPoints").GetArrayLength() == 0)
        throw new InvalidDataException("Physical decision mappings are incomplete.");
    BattleSpawnPointChecks.DecisionReferences(physicalRoot);
    return;
}
if (args.Length == 2 && args[0] == "--physical-plane-only")
{
    PhysicalPlaneChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--preview-birth-only")
{
    PreviewBirthChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--preview-reference-only")
{
    PreviewReferenceChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--enchantment-summon-only")
{
    EnchantmentSummonChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--character-removal-only")
{
    CharacterRemovalChecks.Native(args[1]);
    return;
}

if (args.Length == 2 && args[0] == "--enchantment-source-order-only")
{
    EnchantmentSourceOrderChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--enchantment-world-only")
{
    EnchantmentWorldChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--enchantment-combat-only")
{
    EnchantmentCombatChecks.Native(args[1]);
    return;
}

if (args.Length == 2 && args[0] == "--enchantment-lifecycle-only")
{
    EnchantmentLifecycleChecks.Native(args[1]);
    return;
}

if (args.Length == 2 && args[0] == "--hero-copy-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (fixture.RootElement.GetProperty("ModifierScenario").GetString() != "hero-copy" ||
        fixture.RootElement.GetProperty("HeroCopyOperations").GetArrayLength() == 0)
        throw new InvalidDataException("The requested fixture has no hero-copy observations.");
    HeroCopyChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--unit-copy-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (fixture.RootElement.GetProperty("ModifierScenario").GetString() != "unit-copy" ||
        fixture.RootElement.GetProperty("UnitCopyOperations").GetArrayLength() == 0)
        throw new InvalidDataException("The requested fixture has no paid unit-copy observations.");
    UnitCopyChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--unit-clone-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (fixture.RootElement.GetProperty("ModifierScenario").GetString() != "unit-clone" ||
        fixture.RootElement.GetProperty("UnitCloneOperations").GetArrayLength() == 0)
        throw new InvalidDataException("The requested fixture has no ordinary clone observations.");
    UnitCloneChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--bump-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (fixture.RootElement.GetProperty("ModifierScenario").GetString() != "bump" ||
        fixture.RootElement.GetProperty("BumpOperations").GetArrayLength() == 0)
        throw new InvalidDataException("The requested fixture has no native Bump observations.");
    BumpChecks.Native(fixture.RootElement);
    return;
}

const string steward = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
FixtureArchiveChecks.Run();
var input = new CombatProjectionData
{
    Scenario = "Level1BattleJunker",
    Turn = 0,
    Energy = 4,
    PyreHealth = 80,
    PyreMaxHealth = 80,
    GameplayRng = "fixed-seed",
    Hand = [new CardToken(1, steward), new CardToken(2, steward)],
    Draw = [new CardToken(3, "other")],
    DiscardBuffer = [new CardToken(4, "buffered")],
    Monsters = [new UnitToken(10, "PyreHeartStarter", 3, 0, 45, 80, 80, 0, "")],
    Rooms =
    [
        new RoomToken(0, 0, 5, 0, 0, 5, 0, 0),
        new RoomToken(3, 0, 5, 0, 0, 5, 0, 0)
    ]
};
var root = new CombatProjection(input);
input.Hand.Clear();
Check(root.Hand.Count == 2, "Capture did not isolate the caller's list.");

var errors = new ConcurrentQueue<string>();
Parallel.For(0, 32, index =>
{
    int handIndex = index % 2;
    int playedId = handIndex + 1;
    ModelStep step = SimpleUnitPlayModel.Apply(root,
        new SimpleUnitPlay(handIndex, playedId, 0, 1, 3, "TrainStewardBig", 8, 25));
    CombatProjection? branch = step.State;
    if (branch == null || branch.Energy != 3 || branch.Hand.Count != 1 ||
        branch.Hand[0].InstanceId == playedId || branch.Draw[0].InstanceId != 3 ||
        branch.DiscardBuffer.Count != 1 || branch.DiscardBuffer[0].InstanceId != 4 ||
        branch.Monsters.Count != 2 || branch.Monsters[1].InstanceId != 11 ||
        branch.Monsters[1].Health != 25 || branch.Rooms[0].MonsterCapacity != 3 ||
        branch.Rooms[0].MonsterNextSpawn != 0 || branch.GameplayRng != "fixed-seed")
    {
        errors.Enqueue("Branch " + index + " differed from the expected isolated transition.");
    }
});
Check(errors.IsEmpty, string.Join(" ", errors));
Check(root.Hand.Count == 2 && root.Energy == 4 && root.Monsters.Count == 1 &&
    root.Rooms[0].MonsterCapacity == 0, "A branch mutated its parent.");
Check(!SimpleUnitPlayModel.Apply(root,
    new SimpleUnitPlay(0, 2, 0, 1, 3, "TrainStewardBig", 8, 25)).Supported,
    "A different identical card instance was accepted.");
var lowEnergy = new CombatProjection(new CombatProjectionData
{
    Scenario = "Level1BattleJunker",
    Turn = 0,
    Energy = 0,
    Hand = [new CardToken(1, steward)],
    Rooms = [new RoomToken(0, 0, 5, 0, 0, 5, 0, 0),
        new RoomToken(3, 0, 5, 0, 0, 5, 0, 0)]
});
Check(!SimpleUnitPlayModel.Apply(lowEnergy,
    new SimpleUnitPlay(0, 1, 0, 1, 3, "TrainStewardBig", 8, 25)).Supported,
    "An unaffordable action was accepted.");
var unknownCard = new CombatProjection(new CombatProjectionData
{
    Scenario = "Level1BattleJunker",
    Turn = 0,
    Energy = 4,
    Hand = [new CardToken(5, "unknown-card")],
    Rooms = [new RoomToken(0, 0, 5, 0, 0, 5, 0, 0),
        new RoomToken(3, 0, 5, 0, 0, 5, 0, 0)]
});
Check(!SimpleUnitPlayModel.Apply(unknownCard,
    new SimpleUnitPlay(0, 5, 0, 1, 3, "TrainStewardBig", 8, 25)).Supported,
    "An unknown card was accepted as a simple unit play.");
Console.WriteLine("MODEL-CHECKS PASS: independent branches, card identity, and unsupported actions.");
CardCycleChecks.Run();
BonusDrawChecks.Run();
RoomCapacityChecks.Run();
CombatEffectChecks.Run();
StatusRegistryChecks.Run();
StatusCallbackChecks.Run();
PersistentEnchantmentChecks.Run();
PreviewCopyChecks.Run();
ContextReferenceChecks.Run();
UnitUpgradeCallbackChecks.Run();
RetainedCallbackChecks.Run();
ConditionalTriggerChecks.Run();
TriggerRepeatChecks.Run();
CompanionBossChecks.Run();
SentryChecks.Run();
AbilityCooldownChecks.Run();
AbilityLifecycleChecks.Run();
AbilityEffectChecks.Run();
AbilityUpgradeChecks.Run();
HordeStatChecks.Run();
HordeStatusChecks.Run();
HordeMergeChecks.Run();
HarvestChecks.Run();
RevivalChecks.Run();
HordeRemovalChecks.Run();
HordeDeathChecks.Run();
AbilityCardChecks.Run();
UnitAbilityChecks.Run();
BattleStatisticsChecks.Run();
StatisticOverflowChecks.Run();
StatisticZeroIncrementChecks.Run();
StatisticCacheChecks.Run();
StatisticQueryChecks.Run();
DamageScalingChecks.Run();
StatusScalingChecks.Run();
DynamicStatisticChecks.Run();
EnergyChecks.Run();
CardCostChecks.Run();
CardModifierChecks.Run();
TerminalSpellChecks.Run();
UnitModifierChecks.Run();
UnitUpgradeScalingChecks.Run();
UnitTriggerUpgradeChecks.Run();
SpawnTriggerChecks.Run();
UnitTurnBeginChecks.Run();
TeamTurnBeginChecks.Run();
PreHandDiscardChecks.Run();
PreCombatChecks.Run();
TriggeredHealingChecks.Run();
PostCombatHealingChecks.Run();
TriggeredDamageChecks.Run();
DamageDeathQueueChecks.Run();
TerminalDeathChecks.Run();
HitKillChecks.Run();
DyingUpgradeChecks.Run();
AttackTriggerChecks.Run();
TriggeredStatusChecks.Run();
HandUpgradeChecks.Run();
HealingChecks.Run();
GoldRewardChecks.Run();
HealingTriggerChecks.Run();
TriggerUpgradeChecks.Run();
EnemySpawningChecks.Run();
BattleActionChecks.Run();
DecisionReferenceChecks.Run();
UnitIdentityChecks.Run();
CardSpellChecks.Run();
RoomSpellChecks.Run();
RandomSpellChecks.Run();
RandomStatusChecks.Run();
TrainSpellChecks.Run();
CrossRoomSpellChecks.Run();
UnitAttackChecks.Run();
UnitHealthChecks.Run();
NumericRangeChecks.Run();
TargetFilterChecks.Run();
DrawSpellChecks.Run();
HandRemovalChecks.Run();
SharedPileChecks.Run();
CardGenerationChecks.Run();
CloneUpgradeRefreshChecks.Run();
TrainCombatChecks.Run();
RoomCombatChecks.Run(args.Where(path => !path.Contains("calibration", StringComparison.OrdinalIgnoreCase)).ToArray());
foreach (string path in args.Where(path => path.Contains("card-modifier-calibration", StringComparison.OrdinalIgnoreCase)))
    CardModifierChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("rng-calibration", StringComparison.OrdinalIgnoreCase)))
    RngChecks.Run(path);
foreach (string path in args.Where(path => path.Contains("gold-reward-calibration", StringComparison.OrdinalIgnoreCase)))
    GoldRewardChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("standby-routing-calibration", StringComparison.OrdinalIgnoreCase)))
    StandbyPileChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("ui-rng-isolation-calibration", StringComparison.OrdinalIgnoreCase)))
    UiRngIsolationChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("statistic-query-calibration", StringComparison.OrdinalIgnoreCase)))
    StatisticQueryChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("statistic-overflow-calibration", StringComparison.OrdinalIgnoreCase)))
    StatisticOverflowChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("statistic-zero-increment-calibration", StringComparison.OrdinalIgnoreCase)))
    StatisticZeroIncrementChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("horde-stat-calibration", StringComparison.OrdinalIgnoreCase)))
    HordeStatChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("enchantment-lifecycle-calibration", StringComparison.OrdinalIgnoreCase)))
    EnchantmentLifecycleChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("enchantment-combat-calibration", StringComparison.OrdinalIgnoreCase)))
    EnchantmentCombatChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("enchantment-world-calibration", StringComparison.OrdinalIgnoreCase)))
    EnchantmentWorldChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("enchantment-source-order-calibration", StringComparison.OrdinalIgnoreCase)))
    EnchantmentSourceOrderChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("character-removal-calibration", StringComparison.OrdinalIgnoreCase)))
    CharacterRemovalChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("enchantment-summon-", StringComparison.OrdinalIgnoreCase) &&
    path.Contains("calibration", StringComparison.OrdinalIgnoreCase)))
    EnchantmentSummonChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("preview-reference-calibration", StringComparison.OrdinalIgnoreCase)))
    PreviewReferenceChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("preview-birth-", StringComparison.OrdinalIgnoreCase) &&
    path.Contains("calibration", StringComparison.OrdinalIgnoreCase)))
    PreviewBirthChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("physical-plane-calibration", StringComparison.OrdinalIgnoreCase)))
    PhysicalPlaneChecks.Native(path);

static void Check(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
