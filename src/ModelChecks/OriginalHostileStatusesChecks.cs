using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalHostileStatusesChecks
{
    private static readonly string[] Originals = { "28875721-10df-4427-b9ef-4d237e89608e", "479ceb1b-fc8c-420f-8fa0-17683cee4511",
        "4f930128-efce-47c7-ad02-67781d528971", "47df0371-7bd9-4bc2-b8e1-de61cc0060c7", "75ce1442-757b-4e9f-826d-8325f3dc7d9e",
        "9e2edcaf-b882-4b8e-ae02-95470ab63047", "b197fdec-a77d-49f5-bc05-6233b7ce01c7", "e5decf78-0be8-4b05-820a-3e7a94ac1600",
        "61fec066-a70a-45b9-8869-263e4a12bf74", "3799918c-a831-4599-a65f-acfc68f582b9", "1cc57bf5-b4c0-4102-8fb3-0c7e68813472" };
    private static readonly (string Id, int Count)[][] Statuses = { [("pyregel", 10)], [("pyregel", 4)], [("pyregel", 2)], [("debuff", 2)],
        [("corrupt_poison", 5)], [("corrupt_poison", 15), ("rooted", 1)], [("corrupt_poison", 2)], [("decay", 2)], [("dazed", 1)],
        [("buff", 2)], [("melee weakness", 2)] };
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
        int target = index is 7 or 9 or 12 or 13 or 14 or 17 ? source.Spawn.Train.Rooms[0].Units.First(unit => unit.Team == CombatTeam.Enemy).Id : 0;
        var action = new PlayCardAction(ids[index], index == 2 ? 1 : 0, playerPosition: index < 2 ? index : index == 2 ? 0 : -1, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalHostileStatusesPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original hostile status witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalHostileStatusesIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 18 && actions.Length == 18, "Missing eleven original spells or authored controls.");
        int[] costs = [1, 0, 3, 1, 1, 2, 1, 1, 3, 3, 1];
        int consumed = 0;
        for (int i = 7; i < 18; i++)
        {
            BattleTurnState before = actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var context = before.Spawn.Train.Context!; var changed = after.Spawn.Train.Context!;
            var card = context.FindCard(ids[i])!; var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == Originals[i - 7]);
            Require(card.DataId == Originals[i - 7] && rule.ExternalInteractions.Count == 0 && rule.Cost == costs[i - 7] && before.Energy - after.Energy == costs[i - 7],
                "Original status identity, cost or effect projection differs.");
            bool consume = CardExhaustModel.HasConsume(card); if (consume) consumed++;
            Require(consume ? changed.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[i]) :
                changed.Cards.Discard.Any(token => token.InstanceId == ids[i]), "Native original Consume or discard routing differs.");
            CombatUnit first = before.Spawn.Train.Rooms[0].Units.First(unit => unit.Team == CombatTeam.Enemy);
            int[] targetIds = i == 16 ? before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player).Select(unit => unit.Id).ToArray() :
                i == 15 ? before.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.Team == CombatTeam.Enemy).Select(unit => unit.Id).ToArray() :
                i is 8 or 10 or 11 ? before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).Select(unit => unit.Id).ToArray() : [first.Id];
            Require(targetIds.Length >= (i is 8 or 10 or 11 or 15 or 16 ? 2 : 1), "Missing positive room/tower/multiplier targets.");
            foreach (CombatUnit a in before.Spawn.Train.Rooms.SelectMany(room => room.Units))
            {
                CombatUnit b = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == a.Id);
                if (!targetIds.Contains(a.Id)) { Require(Json(a) == Json(b), "Original status spell changed an excluded control unit."); continue; }
                foreach (var status in Statuses[i - 7])
                {
                    int old = a.Status(status.Id)?.Stacks ?? 0, actual = b.Status(status.Id)?.Stacks ?? 0;
                    Require(actual == (i is 9 or 16 ? old * status.Count : old + status.Count) && actual > old,
                        "Positive native status addition/multiplication differs: " + rule.AssetKey + "/" + status.Id);
                }
                Require(a.Health == b.Health, "Original status application changed immediate target HP.");
            }
            UnityRng rng = context.BattleRng;
            for (int draw = 0; draw < (i is 8 or 16 ? targetIds.Length : 0); draw++) rng = rng.Next();
            Require(rng.Equals(changed.BattleRng), "Chance-100 must consume one Battle RNG draw per target; chance-zero must not roll.");
            Require(context.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 3 &&
                changed.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == (i == 17 ? 0 : 3), "Forced Contamination must spend three corruption before its status effect.");
        }
        Require(consumed == 7, "Missing seven native Consume routes.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-HOSTILE-STATUSES PASS: eleven unchanged originals, exact selected/room/tower additions, positive Pyregel/Rage doubling, chance-100 RNG, three-Corruption spending, seven Consume routes and 16 immutable full chains.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
