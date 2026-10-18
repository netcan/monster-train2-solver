using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MushyRoomChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("MushyRoomPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Mushy Room witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("MushyRoomCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 5 && actions.Length == 5, "Missing original room or replacement controls.");
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        foreach (int index in new[] { 2, 4 })
        {
            RoomCapacityAttachmentState attachment = After(index).Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single();
            CardActionEffect effect = attachment.PostCombat!.Effects.Single();
            Require(attachment.DataId == "83e40135-9e3a-4290-ab9f-d4c8c7e1ac0b" && !attachment.PostCombat.DisallowedInDeployment &&
                effect.Type == "AddStatus" && effect.AllowEnemy && !effect.AllowPlayer && effect.Statuses.Single().Id == "debuff" && effect.Statuses.Single().Stacks == 3,
                "Original enemy status three, deployment restriction or room identity differs.");
            Require(actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!.Energy - After(index).Energy == 1, "Original cost changed.");
        }
        Require(After(3).Spawn.Turn == 2 && After(4).Spawn.Turn == 3 &&
            After(3).Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single().PostCombat == null,
            "Replacement/restoration timing differs.");
        FixtureValue[] pulses = root.GetProperty("RoomPostCombatRecords").EnumerateArray().ToArray();
        Require(pulses.Length == 3 && pulses.Select(pulse => pulse.GetProperty("Turn").GetInt32()).SequenceEqual(new[] { 0, 1, 3 }),
            "Deployment/replacement/terminal room callback controls differ.");
        int positive = 0, friendly = 0, outside = 0;
        foreach (FixtureValue pulse in pulses)
        {
            Require(pulse.GetProperty("Completed").GetBoolean() && pulse.GetProperty("RoomIndex").GetInt32() == 0, "Incomplete or wrong-floor callback.");
            TrainCombatState before = pulse.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState after = pulse.GetProperty("After").Deserialize<TrainCombatState>()!;
            RoomPostCombatRule rule = before.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single().PostCombat!;
            TrainSpellResult predicted = CardSpellModel.Apply(before, 0, rule.Effects, 0);
            Require(predicted.Supported, predicted.UnsupportedReason ?? "Unsupported isolated status pulse.");
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted.State, ModelJson.Options), JsonSerializer.Serialize(after, ModelJson.Options));
            Require(difference == null, "Native enemy status pulse differs: " + difference);
            foreach (RoomCombatState room in before.Rooms)
            foreach (CombatUnit unit in room.Units)
            {
                CombatUnit changed = after.Rooms.Single(item => item.RoomIndex == room.RoomIndex).Units.Single(item => item.Id == unit.Id);
                int delta = room.RoomIndex == 0 && unit.Team == CombatTeam.Enemy ? 3 : 0;
                Require((changed.Status("debuff")?.Stacks ?? 0) == (unit.Status("debuff")?.Stacks ?? 0) + delta, "Enemy status or floor/team control differs.");
                if (delta > 0) positive++; else if (room.RoomIndex == 0) friendly++; else if (unit.Team == CombatTeam.Enemy) outside++;
            }
        }
        Require(positive >= 2 && friendly >= 2 && outside > 0, "Missing surviving enemy, friendly or outside enemy controls.");
        // Replacing the room stops future applications; it does not undo the
        // ordinary status already applied to an enemy which has ascended.
        CombatUnit retained = After(3).Spawn.Train.Rooms.SelectMany(room => room.Units).First(unit => unit.Team == CombatTeam.Enemy && unit.Status("debuff")?.Stacks > 0);
        Require(retained.Status("debuff")!.Stacks == 3, "Applied status was removed with the attachment.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("MUSHY ROOM PASS: original enemy status three, isolated full train callbacks, retained status after replacement, team/floor controls and 16 immutable chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
