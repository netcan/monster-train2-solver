using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InfernoRoomChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("InfernoRoomPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Inferno Room witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("InfernoRoomCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 5 && actions.Length == 5, "Missing original rooms or floor/replacement controls.");
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        foreach (int index in new[] { 2, 4 })
        {
            RoomCapacityAttachmentState attachment = After(index).Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single();
            CardActionEffect effect = attachment.PreCombat!.Effects.Single();
            Require(attachment.DataId == "378cea0c-14e9-45fb-9918-fb853bd04a7d" && attachment.PreCombat.DisallowedInDeployment &&
                effect.Type == "Damage" && effect.Value == 50 && effect.AllowEnemy && effect.AllowPlayer && effect.Target == "Room",
                "Original both-team damage fifty or deployment restriction differs.");
            Require(actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!.Energy - After(index).Energy == 2, "Original room cost differs.");
        }
        Require(After(3).Spawn.Turn == 2 && After(4).Spawn.Turn == 3 &&
            After(3).Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single().PreCombat == null,
            "Replacement/restoration timing differs.");
        FixtureValue[] pulses = root.GetProperty("RoomPreCombatRecords").EnumerateArray().ToArray();
        Require(pulses.Length == 3 && pulses.Select(pulse => pulse.GetProperty("Turn").GetInt32()).SequenceEqual(new[] { 1, 3, 4 }),
            "Deployment/replacement/once-per-room callback controls differ.");
        BattleTurnState firstTurn = root.GetProperty("Turns")[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(firstTurn.Spawn.Train.Context!.Statistics!.LastAttackDamageDealt == 0 &&
            firstTurn.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.SpawnerCardId == ids[0]).Health == 500,
            "Native preview damage/statistics or restoration of live health differs.");
        int killed = 0, boss = 0;
        foreach (FixtureValue pulse in pulses)
        {
            Require(pulse.GetProperty("Completed").GetBoolean() && pulse.GetProperty("RoomIndex").GetInt32() == 0, "Incomplete or wrong-floor pre-combat callback.");
            TrainCombatState before = pulse.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState after = pulse.GetProperty("After").Deserialize<TrainCombatState>()!;
            RoomPreCombatRule rule = before.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single().PreCombat!;
            Require(rule.FocusRoom, "Missing original attachment VFX room focus.");
            CombatContext focused = before.Context.WithQueryFrame(before.Context.QueryFrame!.With(selectedRoomIndex: 0));
            var effectFrame = new TrainCombatState(before.Rooms.Select(room => room.WithContext(focused)).ToArray(), before.Movement, before.EnemySlotsPerRoom, focused);
            TrainSpellResult predicted = CardSpellModel.Apply(effectFrame, 0, rule.Effects, 0);
            Require(predicted.Supported, predicted.UnsupportedReason ?? "Unsupported isolated pre-combat damage.");
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted.State, ModelJson.Options), JsonSerializer.Serialize(after, ModelJson.Options));
            Require(difference == null, "Native room pre-combat pulse differs: " + difference);
            CombatUnit owner = before.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.SpawnerCardId == ids[0]);
            CombatUnit changed = after.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == owner.Id);
            Require(owner.Health - changed.Health == 50, "Same-floor friendly damage or positive restoration differs.");
            foreach (RoomCombatState outside in before.Rooms.Where(room => room.RoomIndex != 0))
            foreach (CombatUnit control in outside.Units)
                Require(after.Rooms.Single(room => room.RoomIndex == outside.RoomIndex).Units.Single(unit => unit.Id == control.Id).Health == control.Health,
                    "Pre-combat damage leaked to another floor.");
            foreach (CombatUnit enemy in before.Rooms.Single(room => room.RoomIndex == 0).Units.Where(unit => unit.Team == CombatTeam.Enemy))
            {
                CombatUnit? survivor = after.Rooms.Single(room => room.RoomIndex == 0).Units.FirstOrDefault(unit => unit.Id == enemy.Id);
                if (survivor == null) killed++;
                else if (enemy.Status("relentless")?.Stacks > 0 && enemy.Health - survivor.Health == 50) boss++;
            }
        }
        Require(killed >= 2 && boss == 1, "Missing lethal enemy cleanup or once-only damage before relentless combat.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("INFERNO ROOM PASS: original pre-combat fifty, lethal enemy cleanup, friendly/outside controls, deployment/replacement/restoration, one relentless pulse and 16 immutable chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
