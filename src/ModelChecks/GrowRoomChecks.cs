using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class GrowRoomChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        for (int index = 0; index < ids.Length; index++)
            if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
            {
                if (index >= 3 && source.Spawn.Turn < index - 1) return null;
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
            }
        return null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("GrowRoomPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Grow Room witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("GrowRoomCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 5 && actions.Length == 5, "Missing room/floor/replacement controls.");
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        foreach (int index in new[] { 2, 4 })
        {
            RoomCapacityAttachmentState attachment = After(index).Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single();
            Require(attachment.DataId == "7768326f-bc62-4143-93ba-63a5a17b9306" && attachment.PostCombat?.DisallowedInDeployment == true &&
                attachment.PostCombat.Effects.Single().StatusAdjustmentFilter == 1 && root.GetProperty("GrowRoomStatusFilter").GetInt32() == 1,
                "Original post-combat payload, deployment restriction or native filter differs.");
            Require(actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!.Energy - After(index).Energy == 1, "Original cost changed.");
        }
        Require(After(3).Spawn.Turn == 2 && After(4).Spawn.Turn == 3 &&
            After(3).Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single().PostCombat == null,
            "Replacement and restored room timing differ.");
        FixtureValue[] pulses = root.GetProperty("RoomPostCombatRecords").EnumerateArray().ToArray();
        Require(pulses.Length == 2 && pulses.Select(pulse => pulse.GetProperty("Turn").GetInt32()).SequenceEqual(new[] { 1, 3 }),
            "Missing growth/removal/restoration pulse controls.");
        int positive = 0;
        foreach (FixtureValue pulse in pulses)
        {
            Require(pulse.GetProperty("Completed").GetBoolean() && pulse.GetProperty("RoomIndex").GetInt32() == 0, "Incomplete pulse or wrong floor.");
            Require(pulse.GetProperty("Turn").GetInt32() != 0, "Room pulse ran during deployment.");
            TrainCombatState before = pulse.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState after = pulse.GetProperty("After").Deserialize<TrainCombatState>()!;
            RoomPostCombatRule rule = before.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single().PostCombat!;
            TrainSpellResult predicted = CardSpellModel.Apply(before, 0, rule.Effects, 0);
            Require(predicted.Supported, predicted.UnsupportedReason ?? "Unsupported pulse.");
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted.State, ModelJson.Options), JsonSerializer.Serialize(after, ModelJson.Options));
            Require(difference == null, "Native room pulse differs from isolated model: " + difference);
            CombatUnit? owner = before.Rooms.Single(room => room.RoomIndex == 0).Units.FirstOrDefault(unit => unit.SpawnerCardId == ids[0]);
            if (owner != null)
            {
                CombatUnit changed = after.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == owner.Id);
                foreach (CombatStatus status in owner.Statuses.Where(status => status.Id is "buff" or "regen" or "armor"))
                {
                    int delta = rule.Effects.Single().StatusAdjustmentFilter == 0 || status.Propagatable == true ? 1 : 0;
                    Require(changed.Status(status.Id)?.Stacks == status.Stacks + delta, "Original growth/filter mismatch: " + status.Id);
                    if (delta > 0) positive++;
                }
            }
        }
        Require(positive >= 3, "Missing positive status growth evidence.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("GROW ROOM PASS: original post-combat status growth, isolated full-state pulses, floor controls, removal/restoration and 16 immutable chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
