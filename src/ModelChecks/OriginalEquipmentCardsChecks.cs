using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalEquipmentCardsChecks
{
    private static readonly string[] Originals = ["217d467c-77c2-481d-b78f-57aaf3dcd4a6", "567f37b4-9f93-4e55-9fcb-315b870627c3", "7725a001-cc6d-4aef-89cf-19480b16c015",
        "99fed8d9-e235-4109-860f-04a0293ba821", "e625aaa1-911c-43f3-8a0d-963e6a9a07bc", "f1c2a098-e20d-4587-b8de-5bef315de42d",
        "1dd23b59-fa5e-44bc-83ca-de119ede4f21", "66cbbd14-f5f3-44a1-bd1c-ce98f3c13584", "87c155c4-55e4-4434-a99d-1dda85d5784c",
        "e4ba411f-09b2-4a87-91f6-5f00cd742ba6", "7369cb8e-f8cf-44be-8667-c699c630c063", "1f352534-0895-488e-9345-481bc447a089"];
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 4 && source.Spawn.Turn < 1) return null;
        int host = index is >= 7 and <= 10 ? 1 : index is 11 or 12 or 17 or 18 ? 2 : 0;
        int target = index >= 7 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[host]).Id : 0;
        var action = new PlayCardAction(ids[index], index == 3 ? 1 : 0, playerPosition: index < 4 ? (index == 3 ? 0 : index) : -1, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalEquipmentCardsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original equipment card witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalEquipmentCardsIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 19 && actions.Length == 19, "Missing twelve original equipment cards, three four-slot hosts or wound/energy/enemy health controls.");
        for (int i = 7; i < 19; i++)
        {
            BattleTurnState before = actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var context = before.Spawn.Train.Context!; var changed = after.Spawn.Train.Context!;
            var card = context.FindCard(ids[i])!; var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == Originals[i - 7]);
            var action = actions[i].GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatUnit a = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == action.TargetUnitId), b = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == action.TargetUnitId);
            Require(card.DataId == rule.DataId && rule.ExternalInteractions.Count == 0 && before.Energy - after.Energy == rule.Cost && rule.Equipment != null,
                "Original equipment identity, cost or projection differs.");
            Require(a.Modifiers!.EquipmentLimit == 4 && b.EquipmentCards!.SequenceEqual(a.EquipmentCards!.Concat(new[] { ids[i] })), "Equipment must attach once within four slots without displacing an existing card.");
            var standby = changed.OtherPiles!.Single(pile => pile.Name == "Standby");
            Require(standby.Cards.Any(token => token.InstanceId == ids[i]) && standby.EquipmentConditions!.Any(condition => condition.CardId == ids[i] && condition.HostUnitId == b.Id && !condition.ReturnToHand), "Original equipment must bind its native host in Standby.");
            int attack = rule.Equipment!.Upgrades.Sum(upgrade => upgrade.Stats.Damage), health = rule.Equipment.Upgrades.Sum(upgrade => upgrade.Stats.Health);
            Require(b.Modifiers!.AttackDamage - a.Modifiers.AttackDamage == attack && b.MaxHealth - a.MaxHealth == health && b.Health - a.Health == health,
                "Original equipment attack or current/max HP adjustment differs.");
            foreach (CombatStatus status in rule.Equipment.Upgrades.SelectMany(upgrade => upgrade.Statuses))
                Require((b.Status(status.Id)?.Stacks ?? 0) - (a.Status(status.Id)?.Stacks ?? 0) == status.Stacks, "Original equipment status installation differs: " + status.Id);
            if (i == 7) Require(a.Size == 3 && b.Size == 1, "Exile's Diadem must shrink the size-three host by two.");
            if (i == 10) Require(a.Modifiers.MagebladeDamagePerMagicPower == null && b.Modifiers.MagebladeDamagePerMagicPower == 3,
                "Moonlit Glaive must create its Mageblade attack cache through the status-added callback.");
            Require(Json(before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != b.Id)) == Json(after.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != b.Id)) &&
                Json(before.Spawn.Train.Rooms.Skip(1).Select(room => room.Units)) == Json(after.Spawn.Train.Rooms.Skip(1).Select(room => room.Units)), "Equipment attachment changed another host or floor.");
            Require(context.BattleRng.Equals(changed.BattleRng), "These equipment attachments must not consume Battle RNG.");
        }
        var firstCombat = root.GetProperty("TrainPhases").EnumerateArray().First(phase => phase.GetProperty("Turn").GetInt32() == 1 && phase.GetProperty("Kind").GetString() == "Combat");
        TrainCombatState combatBefore = firstCombat.GetProperty("Before").Deserialize<TrainCombatState>()!, combatAfter = firstCombat.GetProperty("Actual").Deserialize<TrainCombatState>()!;
        CombatUnit healerBefore = combatBefore.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[2]), healerAfter = combatAfter.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[2]);
        CombatUnit primaryAfter = combatAfter.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]);
        Require(combatAfter.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player).All(unit => unit.Status("valor")?.Stacks > 0) &&
            combatAfter.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).PlayerMaximum > combatBefore.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).PlayerMaximum,
            "Everlasting Light attack Valor and Heph's Hammer kill capacity need positive native callbacks.");
        Require(combatAfter.Context.FindCard(ids[14])!.Permanent.Upgrades.Any(upgrade => upgrade.DataId == "147c2bab-6487-4b1f-9e6f-f8388dc77199") &&
            healerAfter.Health > healerBefore.Health && primaryAfter.Status("armor")?.Stacks >= 6,
            "Bloodthirsty Blade equipment growth, wounded Meat Shield healing and Fortify postcombat armor need positive witnesses.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-EQUIPMENT-CARDS PASS: twelve unchanged originals, exact paid attachment/Standby bindings, attack/HP/size/statuses, separate host/floor controls, positive equipment growth/capacity/Valor/healing/armor callbacks and 16 immutable full chains.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
