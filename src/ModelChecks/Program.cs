using System.Collections.Concurrent;
using MonsterTrain2Poju.Model;

const string steward = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
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
CombatEffectChecks.Run();
BattleStatisticsChecks.Run();
StatisticOverflowChecks.Run();
StatisticQueryChecks.Run();
DamageScalingChecks.Run();
StatusScalingChecks.Run();
DynamicStatisticChecks.Run();
CardModifierChecks.Run();
TerminalSpellChecks.Run();
UnitModifierChecks.Run();
UnitUpgradeScalingChecks.Run();
UnitTriggerUpgradeChecks.Run();
SpawnTriggerChecks.Run();
HandUpgradeChecks.Run();
HealingChecks.Run();
GoldRewardChecks.Run();
HealingTriggerChecks.Run();
EnemySpawningChecks.Run();
BattleActionChecks.Run();
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

static void Check(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
