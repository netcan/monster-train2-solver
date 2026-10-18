using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalMovementSpellsChecks
{
    private static readonly string[] Originals = ["77770369-492e-4e9e-8ce3-ba044862bb1b", "7cb76384-d8e7-4b4e-a3eb-f9c5b844c941",
        "b67ca7bf-bd65-4d98-ae53-26397de1e355", "9170969d-ab4b-4de2-8d24-674a65aa2894", "01f1fe24-f72b-4749-833b-46796463cdd7",
        "d67c52be-d7bb-4291-a716-31408e4ced60", "f21a04ea-2d1d-4f40-a36b-da4e9239b808", "8b1c7ea7-a7f4-4e21-9a2a-01a255e15d67", "07fd0035-5dde-40f1-bfed-7d9f1f89f5c8"];
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
        var room = source.Spawn.Train.Rooms.FirstOrDefault(item => item.Units.Any(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack"));
        int target = index is >= 5 and <= 10 ? room!.Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack").Id : 0;
        var action = new PlayCardAction(ids[index], index < 3 ? index : index is >= 5 and <= 11 ? room!.RoomIndex : 0,
            playerPosition: index < 3 ? 0 : -1, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalMovementSpellsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original movement spell witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalMovementSpellsIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 13 && actions.Length == 13, "Missing nine original movement spells or three floor controls.");
        int[] targetFloors = [0, 0, 1, 2, 1, 2, 1, 0, 0];
        for (int i = 4; i < 13; i++)
        {
            BattleTurnState before = actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var context = before.Spawn.Train.Context!; var changed = after.Spawn.Train.Context!;
            var card = context.FindCard(ids[i])!; var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == Originals[i - 4]);
            Require(card.DataId == rule.DataId && rule.ExternalInteractions.Count == 0 && before.Energy - after.Energy == rule.Cost,
                "Original movement spell identity, cost or projection differs.");
            CombatUnit[] a = before.Spawn.Train.Rooms.SelectMany(room => room.Units).OrderBy(unit => unit.Id).ToArray(),
                b = after.Spawn.Train.Rooms.SelectMany(room => room.Units).OrderBy(unit => unit.Id).ToArray();
            Require(a.Select(unit => unit.Id).SequenceEqual(b.Select(unit => unit.Id)) &&
                a.All(unit => b.Single(other => other.Id == unit.Id).Health == unit.Health && b.Single(other => other.Id == unit.Id).Attack == unit.Attack),
                "Movement must preserve all actors and their health/attack.");
            CombatUnit guard = b.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack");
            var point = changed.SpawnPoints!.Units.Single(unit => unit.UnitId == guard.Id).Current!;
            Require(point.RoomIndex == targetFloors[i - 4] && after.Spawn.Train.Rooms[point.RoomIndex].Units.Any(unit => unit.Id == guard.Id),
                "Tracked enemy logical/physical floor differs at movement " + i);
            int[] EnemyOrder(BattleTurnState state) => state.Spawn.Train.Context!.SpawnPoints!.Group(0, CombatTeam.Enemy)!.Occupants.Where(id => id != 0).ToArray();
            if (i == 4) Require(EnemyOrder(before).First() == guard.Id && EnemyOrder(after).Last() == guard.Id &&
                b.Where(unit => unit.Team == CombatTeam.Enemy).All(unit => unit.Status("melee weakness")?.Stacks == 1), "Begone must push the front enemy and weaken both enemies.");
            if (i == 5) Require(EnemyOrder(after).First() == guard.Id && guard.Status("melee weakness")?.Stacks == 2, "Challenge must pull the selected rear enemy and weaken that last target.");
            if (i == 7) Require(guard.Status("dazed")?.Stacks == 2, "Tiresome Climb must daze the moved enemy by two.");
            if (i == 8) Require(guard.Status("dazed")?.Stacks == 3, "Dripfall must retain the moved target and add one Dazed.");
            if (i == 9) Require(guard.Status("spell weakness")?.Stacks == 2, "Hexcalation must weaken before moving the selected enemy.");
            if (i == 10) Require(guard.Status("melee weakness")?.Stacks == 4, "Demonize must weaken the last moved target by two.");
            if (i == 11) Require(after.Spawn.Train.Rooms[1].Units.Count == 0 && after.Spawn.Train.Rooms[0].Units.Any(unit => unit.SpawnerCardId == ids[1]),
                "Cave In must move the selected room's enemy and friendly anchor together.");
            if (i == 12)
            {
                var friendlies = after.Spawn.Train.Rooms[1].Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
                Require(friendlies.Length == 2 && friendlies.Select(unit => unit.Health).Order().SequenceEqual(new[] { 500, 600 }) &&
                    after.Spawn.Train.Rooms[0].Units.All(unit => unit.Team != CombatTeam.Player) &&
                    changed.SpawnPoints!.Group(1, CombatTeam.Player)!.Occupants.First(id => id != 0) == friendlies.Single(unit => unit.Health == 600).Id,
                    "Heavensward must move both friendlies up, then pull the unique strongest ally to the front.");
                Require(Json(a.Where(unit => unit.Team == CombatTeam.Enemy)) == Json(b.Where(unit => unit.Team == CombatTeam.Enemy)), "Heavensward changed excluded enemies.");
            }
            Require(Json(before.Spawn.Train.Rooms[2].Units.Where(unit => unit.Team == CombatTeam.Player)) ==
                Json(after.Spawn.Train.Rooms[2].Units.Where(unit => unit.Team == CombatTeam.Player)), "Movement spell changed the top-floor friendly control.");
            bool consume = CardExhaustModel.HasConsume(card);
            Require(consume == (i == 12) && (consume ? changed.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[i]) :
                changed.Cards.Discard.Any(token => token.InstanceId == ids[i])), "Original movement Consume/discard route differs.");
            Require(context.BattleRng.Equals(changed.BattleRng), "These movement spells must not consume Battle RNG.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-MOVEMENT-SPELLS PASS: nine unchanged originals, front/rear ordering, five tracked enemy ascents/descents with chained statuses, both-team room descent, friendly ascent then strongest reorder, 16 full immutable chains.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
