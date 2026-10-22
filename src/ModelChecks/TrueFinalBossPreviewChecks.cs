using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossPreviewChecks
{
    internal static void Native(FixtureValue root, bool roomsOnly = false)
    {
        void Require(bool value, string message) => TrueFinalBossChecks.Require(value, message);
        FixtureValue[] rows = root.GetProperty("TrueFinalBossPreviews").EnumerateArray().ToArray();
        Require(rows.Length == 5 && rows.All(row => row.GetProperty("Actual").ValueKind != FixtureKind.Null), "Missing original UI preview completion.");
        var inputs = new List<TrainCombatState>(); var rules = new List<BattlePlayRules>(); var parents = new List<string>(); var expected = new List<string>();
        foreach (FixtureValue row in rows)
        {
            if (roomsOnly) break;
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            BattleTurnState decision = root.GetProperty("Turns").EnumerateArray().Select(turn => turn.GetProperty("Actual").Deserialize<BattleTurnState>()!)
                .First(turn => turn.Spawn.Turn == row.GetProperty("Turn").GetInt32());
            TrainCombatResult result = TrueFinalBossPreviewModel.Refresh(before, decision.PlayRules!);
            string? difference = result.Supported ? ModelJson.Difference(TrueFinalBossTrainChecks.Comparable(result.State!, "Preview"),
                TrueFinalBossTrainChecks.Comparable(actual, "Preview")) : result.UnsupportedReason;
            Require(result.Supported && difference == null, "Native original UI preview differs on turn " + row.GetProperty("Turn").GetInt32() + ": " + difference);
            inputs.Add(before); rules.Add(decision.PlayRules!); parents.Add(JsonSerializer.Serialize(before, ModelJson.Options));
            expected.Add(TrueFinalBossTrainChecks.Comparable(actual, "Preview"));
        }
        FixtureValue[] rooms = root.GetProperty("TrueFinalBossPreviewRooms").EnumerateArray().ToArray();
        Require(rooms.Length == 17, "Missing native preview rooms, including the final Pyre-death stop.");
        foreach (FixtureValue row in rooms)
        {
            RoomCombatState before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            var world = before.Context!.Enchantments!;
            CombatUnit? pyre = world.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.IsPyre);
            if (pyre == null)
            {
                Require(row.GetProperty("Turn").GetInt32() == 5 && before.RoomIndex == 3 &&
                    JsonSerializer.Serialize(before, ModelJson.Options) == JsonSerializer.Serialize(actual, ModelJson.Options),
                    "The preview coroutine stopped by unplayed Pyre death must leave its pending room unchanged.");
                continue;
            }
            RoomCombatResult result = RoomCombatModel.Resolve(before, externalPyre: before.RoomIndex != 3 ? pyre : null,
                externalPyreRoomIndex: before.RoomIndex != 3 && pyre != null ? 3 : -1,
                externalTargets: before.RoomIndex != 3 ? null : world.Rooms.Where(room => room.RoomIndex != before.RoomIndex).SelectMany(room => room.Units.Select(unit => (room.RoomIndex, unit))).ToArray());
            string? difference = result.Supported ? ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options),
                JsonSerializer.Serialize(actual, ModelJson.Options)) : result.UnsupportedReason;
            Require(result.Supported && difference == null, "Native preview room differs on turn " + row.GetProperty("Turn").GetInt32() + " room " + before.RoomIndex + ": " + difference);
        }
        if (!roomsOnly)
        {
            Parallel.For(0, 64, index =>
            {
                int selected = index % rows.Length;
                TrainCombatResult branch = TrueFinalBossPreviewModel.Refresh(inputs[selected], rules[selected]);
                Require(branch.Supported && TrueFinalBossTrainChecks.Comparable(branch.State!, "Preview") == expected[selected] &&
                    JsonSerializer.Serialize(inputs[selected], ModelJson.Options) == parents[selected], "Concurrent UI preview changed its parent or result.");
            });
            FixtureValue turns = root.GetProperty("Turns");
            BattleTurnState battle = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
            string parent = JsonSerializer.Serialize(battle, ModelJson.Options);
            BattleTurnState original = battle;
            for (int index = 0; index < 5; index++)
            {
                BattleTurnResult step = BattleTurnModel.EndTurn(battle);
                BattleTurnState actual = turns[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
                string? difference = step.Supported ? ModelJson.Difference(BattleTurnChecks.Comparable(step.State!), BattleTurnChecks.Comparable(actual)) : step.UnsupportedReason;
                Require(step.Supported && step.Outcome == RoomOutcome.Exchanged && difference == null, "Independent true-final-boss prefix differs at " + index + ": " + difference);
                battle = step.State!;
            }
            Require(JsonSerializer.Serialize(original, ModelJson.Options) == parent, "The five-turn preview prefix must preserve its root.");
        }
        Console.WriteLine("TRUE-FINAL-BOSS-PREVIEW PASS: five original UI previews, sixteen executed rooms and one cancelled room, full state/context/cards/RNG equality; 64 immutable previews and independent five-EndTurn prefix.");
    }
}
