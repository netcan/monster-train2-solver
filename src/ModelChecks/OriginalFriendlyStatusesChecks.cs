using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalFriendlyStatusesChecks
{
    private static readonly string[] Originals = { "3020f56f-71a5-4155-8bed-48ada7e7e0de", "a558ca3d-bf9b-429b-96ea-3fa4c0a5538b",
        "37aa21f4-1d01-4fe5-83ce-69f52c8c2750", "f9f20326-dccb-44af-894b-467534afab4a", "98276338-b18f-4ffa-83a2-9501c2741f03",
        "84ac6a73-b05b-4758-b044-39f0d30e4272", "141494fe-0ac3-41ad-b1e6-05aeafe060f6", "806fd915-b85c-4d0b-8490-fca08c2e0c0d",
        "0da73673-b84b-4525-a6f0-9604d8087d0e", "dc4166e0-fa31-47a0-baad-90744c8e7d21", "4ef06a46-806b-4638-8d49-4adbef10bfa2",
        "b8241653-fd40-4ca0-8dd3-2c1ef352dbc6" };
    private static readonly (string Status, int Stacks)[][] Added = {
        [("lifesteal", 4), ("scorch", 2)], [("multistrike", 1), ("scorch", 4)], [("multistrike", 1), ("fragile", 1)],
        [("buff", 12)], [("valor", 1)], [("lifesteal", 2)], [("armor", 30)], [("undying", 1)], [("fragile", 1), ("spikes", 40)],
        [("spikes", 6)], [("titanskin", 2)], [("damage shield", 2)] };
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
        int target = index >= 4 && index is not (8 or 9 or 15) ? source.Spawn.Train.Rooms[1].Units.Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
        var action = new PlayCardAction(ids[index], index is 2 or 3 ? 0 : 1,
            playerPosition: index < 2 ? index : index == 2 ? 0 : -1, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalFriendlyStatusesPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original friendly status witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalFriendlyStatusesIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 16 && actions.Length == 16, "Missing twelve original spells, three anchors or battery.");
        int consumed = 0;
        for (int i = 4; i < 16; i++)
        {
            BattleTurnState before = actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var card = before.Spawn.Train.Context!.FindCard(ids[i])!; var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == Originals[i - 4]);
            Require(card.DataId == Originals[i - 4] && rule.ExternalInteractions.Count == 0 && before.Energy - after.Energy == rule.Cost,
                "Original identity, native cost or effect projection differs.");
            bool consume = CardExhaustModel.HasConsume(card); if (consume) consumed++;
            Require(consume ? after.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[i]) :
                after.Spawn.Train.Context!.Cards.Discard.Any(token => token.InstanceId == ids[i]), "Native Consume or discard routing differs.");
            bool room = i is 8 or 9 or 15;
            if (i == 13)
            {
                Require(rule.Effects![0].Statuses.Select(status => status.Id + ":" + status.Stacks)
                    .SequenceEqual(new[] { "lifesteal:2", "undying:1", "spikes:6" }), "Pseudoscience native random pool changed.");
                RngDraw roll = before.Spawn.Train.Context!.BattleRng.Range(0, 3);
                Require(roll.Value == 2 && roll.State.Equals(after.Spawn.Train.Context!.BattleRng), "Pseudoscience must choose one status with exactly one Battle RNG draw.");
                var a = before.Spawn.Train.Rooms[1].Units.Single(unit => unit.SpawnerCardId == ids[0]);
                var b = after.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == a.Id);
                Require(a.Status("lifesteal")?.Stacks == b.Status("lifesteal")?.Stacks && a.Status("undying")?.Stacks == b.Status("undying")?.Stacks,
                    "Unselected Pseudoscience statuses must remain unchanged.");
            }
            foreach (int anchor in room ? new[] { 0, 1 } : new[] { 0 })
            {
                CombatUnit a = before.Spawn.Train.Rooms[1].Units.Single(unit => unit.SpawnerCardId == ids[anchor]),
                    b = after.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == a.Id);
                foreach (var added in Added[i - 4]) Require((b.Status(added.Status)?.Stacks ?? 0) - (a.Status(added.Status)?.Stacks ?? 0) == added.Stacks,
                    "Positive original status change differs: " + rule.AssetKey + "/" + added.Status);
            }
            if (!room) Require(Json(before.Spawn.Train.Rooms[1].Units.Where(unit => unit.SpawnerCardId != ids[0])) ==
                Json(after.Spawn.Train.Rooms[1].Units.Where(unit => unit.SpawnerCardId != ids[0])), "Selected status spell changed its same-room control.");
            Require(Json(before.Spawn.Train.Rooms[0].Units) == Json(after.Spawn.Train.Rooms[0].Units), "Friendly status spell changed its neighboring anchor floor.");
        }
        Require(consumed == 7, "Missing seven unchanged original Consume routes.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-FRIENDLY-STATUSES PASS: twelve unchanged original spells, exact positive stacks, selected/room controls, seven Consume routes and 16 immutable full chains.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
