using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalResourceSpellsChecks
{
    private static readonly string[] Originals = { "f884b713-6268-4ac2-8360-6a6c6b3c155e", "10f64bfb-81cd-4b7c-bf46-6fc1fa832008",
        "1e8ebf30-439a-49ce-9f02-ebfd8e6fc757", "1e8ebf30-439a-49ce-9f02-ebfd8e6fc757", "2ca56db0-bef2-48c6-b575-85bb611a3b0a",
        "2ca56db0-bef2-48c6-b575-85bb611a3b0a", "febac44c-ce77-484d-921f-bf34ae3de377", "febac44c-ce77-484d-921f-bf34ae3de377" };
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
        int target = index is 7 or 8 ? source.Spawn.Train.Rooms[0].Units.First(unit => unit.Team == CombatTeam.Enemy).Id : 0;
        var action = new PlayCardAction(ids[index], index < 3 ? index : 0, playerPosition: index < 3 ? 0 : -1, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalResourceSpellsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original resource spell witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalResourceSpellsIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 13 && actions.Length == 13, "Missing five original identities, repeated/resource-zero controls or anchors.");
        int[] costs = [3, 1, 1, 1, 2, 2];
        int consumed = 0;
        for (int i = 5; i < 13; i++)
        {
            BattleTurnState before = actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var context = before.Spawn.Train.Context!; var changed = after.Spawn.Train.Context!;
            var card = context.FindCard(ids[i])!; var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == Originals[i - 5]);
            int paid = i >= 11 ? before.Energy : costs[i - 5];
            Require(card.DataId == Originals[i - 5] && rule.ExternalInteractions.Count == 0 && before.Energy - after.Energy == paid,
                "Original resource spell identity, cost or projection differs.");
            if (i >= 11) Require(rule.CostType == "ConsumeRemainingEnergy" && after.Energy == 0 && (i == 11 ? paid > 0 : paid == 0), "Missing positive/zero original X cost control.");
            bool consume = CardExhaustModel.HasConsume(card); if (consume) consumed++;
            Require(consume ? changed.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[i]) :
                changed.Cards.Discard.Any(token => token.InstanceId == ids[i]), "Original Consume or discard route differs.");
            int hoard = context.QueryFrame!.DragonsHoard!.Value;
            int expectedHoard = i == 5 ? 4 : i is 7 or 8 ? hoard - 1 : hoard;
            Require(changed.QueryFrame!.DragonsHoard == expectedHoard && context.Resources!.DragonsHoardCap == 4, "Original capped Hoard gain or spending differs.");
            Require(changed.QueryFrame.ForgePoints == (i is 9 or 10 ? 0 : i >= 11 ? context.QueryFrame.ForgePoints + 4 * paid : context.QueryFrame.ForgePoints), "Original Forge spending or X scaling differs.");
            if (i is 7 or 8 or 9 or 10)
            {
                CombatUnit victim = before.Spawn.Train.Rooms[0].Units.First(unit => unit.Team == CombatTeam.Enemy);
                CombatUnit? remaining = after.Spawn.Train.Rooms[0].Units.FirstOrDefault(unit => unit.Id == victim.Id);
                int damage = i is 7 or 8 ? 25 * hoard : 35 * context.QueryFrame.ForgePoints!.Value;
                int Effective(CombatUnit? unit) => unit == null ? 0 : unit.Health + (unit.Status("armor")?.Stacks ?? 0);
                Require(Effective(victim) - Effective(remaining) == Math.Min(Effective(victim), damage), "Resource damage must use the pre-spend value and replace base damage.");
                Require(i != 10 || context.QueryFrame.ForgePoints == 0 && damage == 0 && remaining?.Health == victim.Health, "Missing zero Forge damage control.");
            }
            CombatUnit pyre = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre),
                actualPyre = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre);
            Require(pyre.Health - actualPyre.Health == (i == 6 ? 8 : 0), "Devilish Details must damage Pyre before its capped Hoard gain.");
            Require(Json(before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player)) ==
                Json(after.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player)) &&
                Json(before.Spawn.Train.Rooms.Skip(1).Take(2).Select(room => room.Units)) == Json(after.Spawn.Train.Rooms.Skip(1).Take(2).Select(room => room.Units)),
                "Original resource spell changed a friendly or neighboring floor.");
            Require(context.BattleRng.Equals(changed.BattleRng), "Fixed original resource spell changed Battle RNG.");
        }
        Require(consumed == 6, "Missing six original Consume routes.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-RESOURCE-SPELLS PASS: five unchanged originals/eight plays, capped Hoard/Pyre cost, pre-spend damage replacement, positive/zero Forge and X controls, six Consume routes and 16 immutable full chains.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
