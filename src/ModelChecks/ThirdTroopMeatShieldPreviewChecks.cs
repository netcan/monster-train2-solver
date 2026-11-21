using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ThirdTroopMeatShieldPreviewChecks
{
    private const string Reason = "Battle preview floor 2: Combined troop upgrades and enchantment source ordering require native calibration.";
    private const string Aura = "e4217d7c-8301-4fcf-ab8b-a96b8c921a0c";
    private const string FightClubUpgrade = "f93ac9f7-45d2-47a4-a159-a1b326bd047f";
    private static readonly Guid Module = new("bf93f060-beda-46b1-b808-95ebf1e9a99d");

    internal static void Native(FixtureValue wrackingPain, FixtureValue fightClub)
    {
        BattleTurnState wrackingRoot = Check(wrackingPain, fightClub: false);
        BattleTurnState clubRoot = Check(fightClub, fightClub: true);
        Require(NativeBranchObservationChecks.Key(wrackingRoot) == NativeBranchObservationChecks.Key(clubRoot),
            "The two observed plays require the same original decision.");
        Console.WriteLine("THIRD-TROOP-MEAT-SHIELD-PREVIEW PASS: two 20-input native branches, ordered troop writes, complete successors and RNG, four immutable replays per branch.");
    }

    private static BattleTurnState Check(FixtureValue fixture, bool fightClub)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason, expectedGameModule: Module);
        Require(rows.Length == 20 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing the original native play prefix.");
        FixtureValue boundary = rows[^1];
        var before = boundary.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = boundary.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = boundary.GetProperty("Action").Deserialize<PlayCardAction>()!;
        string data = fightClub ? "6bc6afc8-a113-4f03-b090-d4436f41d822" : "67e742a7-d9b6-46e4-b9dc-37edd4dfb872";
        Require(before.Spawn.Turn == 4 && action.RoomIndex == 2 && action.TargetUnitId == (fightClub ? 0 : 13) &&
            before.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == data, "Unexpected original card or target.");

        int birth = before.Spawn.NextUnitId;
        var samples = boundary.GetProperty("TroopUpdates").EnumerateArray().ToArray();
        int[][] transitions = fightClub ? [[3, 12, 21, 1, 4], [3, 21, 30, 4, 7], [3, 30, 39, 7, 10]] :
            [[3, 12, 21, 0, 3], [3, 21, 30, 3, 6], [6, 51, 69, 9, 15], [6, 69, 87, 15, 21]];
        Require(samples.Length == transitions.Length + 1, "Unexpected number of original troop updates.");
        for (int index = 0; index < samples.Length; index++)
        {
            FixtureValue start = samples[index].GetProperty("Before"), end = samples[index].GetProperty("After");
            bool preview = index < transitions.Length;
            CheckFrame(start, preview);
            CheckFrame(end, preview);
            if (!preview) continue;
            int[] change = transitions[index];
            CheckBirth(start, change[0], change[1], change[3]);
            CheckBirth(end, change[0], change[2], change[4]);
        }
        Require(actual.Spawn.NextUnitId == birth && !actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == birth) &&
            !actual.Spawn.Train.Context!.Enchantments!.RetainedUnits.Any(actor => actor.Unit.Id == birth) &&
            !actual.Spawn.Train.Context.SpawnPoints!.Units.Any(point => point.UnitId == birth), "A temporary Funguy survived native preview teardown.");
        CombatUnit ActualUnit(int id) => actual.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == id);
        Require(ActualUnit(2).BaseAttack == (fightClub ? 95 : 80) && ActualUnit(5).BaseAttack == (fightClub ? 39 : 24) &&
            ActualUnit(25).BaseAttack == (fightClub ? 20 : 5) && ActualUnit(13).Health == (fightClub ? 193 : 126),
            "Native primary damage or FightClub attack upgrade differs.");

        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        string expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected,
                "Complete native preview successor or RNG differs: " + result.Reason);
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "A replay mutated the original decision.");
        return before;

        void CheckFrame(FixtureValue frame, bool preview) => Require(frame.GetProperty("Room").GetInt32() == 2 &&
            frame.GetProperty("Preview").GetBoolean() == preview && frame.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 25]),
            "Native preview may write health without updating the primary target cache.");

        void CheckBirth(FixtureValue frame, int horde, int health, int upgradeCount)
        {
            FixtureValue unit = frame.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == birth);
            string[] upgrades = unit.GetProperty("Upgrades").Deserialize<string[]>()!;
            Require(unit.GetProperty("Asset").GetString() == "MonsterFunguy" && unit.GetProperty("Horde").GetInt32() == horde &&
                unit.GetProperty("Health").GetInt32() == health && unit.GetProperty("MaxHealth").GetInt32() == health && upgrades.Length == upgradeCount &&
                (!fightClub || upgrades[0] == FightClubUpgrade) && upgrades.Skip(fightClub ? 1 : 0).All(upgrade => upgrade == Aura),
                "Original temporary birth health or upgrade order differs.");
        }
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
