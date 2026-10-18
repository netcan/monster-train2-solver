using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalStatusUnitsChecks
{
    private static readonly string[] Originals = ["36aa1f24-a48e-41bf-b30a-7c98b250261a", "5de24977-6800-4015-a20e-9949e79e057c",
        "e66178fd-b8e7-401c-b570-7832840ea902", "b53dd72c-141c-49f2-abae-6505f803773a", "c3c595d2-898d-445c-8f3c-d06d6c120bbd",
        "3d32e031-9796-4eaf-84c3-a30b48db22d4", "3d81c731-8315-40ae-b731-4f2ed80df572", "63a48bad-07da-4687-bfef-2f7c6653af5e",
        "a5237619-8de3-46d2-8c52-e0d22abbc008", "c493d7e0-6731-4d8a-9ae1-56e44febb891", "4dd2be98-c73c-4f96-865f-6c24cc42e91d"];
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
        int room = index < 3 ? index : index is >= 4 and <= 6 ? index - 4 : index is >= 11 and <= 13 ? 1 : index is >= 14 and <= 17 ? 2 : index == 20 ? 1 : 0;
        int position = index < 3 ? 0 : index is >= 7 and <= 10 ? index - 6 : index is >= 11 and <= 13 ? index - 10 : index is >= 14 and <= 17 ? index - 13 : -1;
        int target = index >= 18 ? source.Spawn.Train.Rooms[room].Units.Single(unit => unit.SpawnerCardId == ids[index == 20 ? 13 : 10]).Id : 0;
        var action = new PlayCardAction(ids[index], room, playerPosition: position, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalStatusUnitsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original status unit witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalStatusUnitsIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 21 && actions.Length == 21, "Missing eleven original unit births or positive controls.");
        for (int i = 7; i < 18; i++)
        {
            BattleTurnState before = actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = actions[i].GetProperty("Action").Deserialize<PlayCardAction>()!;
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == Originals[i - 7]);
            CombatUnit expected = rule.SpawnUnit!, unit = after.Spawn.Train.Rooms[action.RoomIndex].Units.Single(unit => unit.SpawnerCardId == ids[i]);
            Require(before.Spawn.Train.Context!.FindCard(ids[i])!.DataId == rule.DataId && rule.ExternalInteractions.Count == 0 && before.Energy - after.Energy == rule.Cost,
                "Original unit identity, projection or paid cost differs.");
            Require(unit.AssetKey == expected.AssetKey && unit.BaseAttack == expected.BaseAttack && unit.Health == expected.Health && unit.MaxHealth == expected.MaxHealth &&
                unit.Size == expected.Size && Json(unit.Statuses) == Json(expected.Statuses) && unit.Modifiers!.Upgrades.Count == 0,
                "Original unit base stats or authored starting statuses changed.");
            Require(after.Spawn.Train.Rooms.Where(room => room.RoomIndex != action.RoomIndex).SelectMany(room => room.Units).All(unit =>
                before.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(previous => Json(previous) == Json(unit))), "A unit birth changed another floor.");
        }
        for (int i = 18; i < 21; i++)
        {
            BattleTurnState before = actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = actions[i].GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatUnit a = before.Spawn.Train.Rooms[action.RoomIndex].Units.Single(unit => unit.Id == action.TargetUnitId), b = after.Spawn.Train.Rooms[action.RoomIndex].Units.Single(unit => unit.Id == action.TargetUnitId);
            if (i < 20) Require(a.Health == b.Health && a.Status("armor")!.Stacks - b.Status("armor")!.Stacks == a.Status("steelguard")!.ParamInt,
                "Steel Steward must positively block a large hit while spending only its native Steelguard cap.");
            else Require(a.Health == 1 && b.Health == 1 && a.Status("undying")!.Stacks == 8 && b.Status("undying")!.Stacks == 7,
                "Nine-Lived Nightmare must spend one Undying charge and reanimate after lethal damage.");
        }
        Require(root.GetProperty("TrainPhases").EnumerateArray().Where(phase => phase.GetProperty("Kind").GetString() == "Combat").Any(phase =>
            phase.GetProperty("Before").Deserialize<TrainCombatState>()!.Rooms.SelectMany(room => room.Units).Any(unit => unit.SpawnerCardId == ids[14] && unit.Status("ephemeral")?.Stacks == 1) &&
            phase.GetProperty("Actual").Deserialize<TrainCombatState>()!.Rooms.SelectMany(room => room.Units).All(unit => unit.SpawnerCardId != ids[14])),
            "Starter Dreg needs a positive Burnout expiry/death witness.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-STATUS-UNITS PASS: eleven unchanged units, exact paid birth/stats/starting statuses, native Steelguard blocking, Undying reanimation, Burnout expiry and 16 immutable full chains.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
