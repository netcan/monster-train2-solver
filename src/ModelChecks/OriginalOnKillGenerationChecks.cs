using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalOnKillGenerationChecks
{
    private const string OriginalId = "fe73da55-8250-4f96-b3e0-91c19b3e60de";
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 6 && source.Spawn.Turn < 1) return null;
        int victim = index switch { 5 => 1, 7 or 8 => 2, 9 or 13 => 3, 11 or 12 => 4, _ => -1 };
        int target = victim < 0 ? 0 : source.Spawn.Train.Rooms[1].Units.Single(unit => unit.SpawnerCardId == ids[victim]).Id;
        var action = new PlayCardAction(ids[index], index is 0 or 6 or 10 ? 0 : 1, playerPosition: index == 0 ? 0 : index < 5 ? index - 1 : -1, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalOnKillGenerationPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original kill-generation witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalOnKillGenerationIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 14 && actions.Length == 14, "Missing original plays or pool/full-hand controls.");
        BattleTurnState Before(int i) => actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int i) => actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Victim(BattleTurnState state, int index) => state.Spawn.Train.Rooms[1].Units.Single(unit => unit.SpawnerCardId == ids[index]);
        foreach (int i in new[] { 5, 7, 8, 9, 11, 12, 13 })
        {
            var before = Before(i); var after = After(i); var context = before.Spawn.Train.Context!; var actual = after.Spawn.Train.Context!;
            var card = context.FindCard(ids[i])!; var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == OriginalId);
            Require(card.DataId == OriginalId && rule.Cost == 1 && rule.HasKillTriggers && rule.ExternalInteractions.Count == 0 &&
                card.MaskDescriptor!.KillTriggers is { Count: 1 } triggers && CardKillTriggerModel.Supported(triggers[0]), "Original spell cost or generation trigger differs.");
            var generation = card.MaskDescriptor!.KillTriggers![0].Actions.Single().Generation!;
            var weights = generation.Pool.GroupBy(member => member.DataId).ToDictionary(group => group.Key, group => group.Count());
            Require(generation.Count == 2 && generation.Pool.Count == 19 && weights.Count == 5 &&
                weights["0347419a-2e3a-492d-a054-f6e340be3db2"] == 5 && weights["e16040b1-eac0-4769-bc89-90bc2eb31944"] == 5 &&
                weights["9c8d3e46-1758-4ade-a4b4-cd2bf649a4d6"] == 4 && weights["b24a8e4f-294f-4d24-9bc2-9c14474d83d1"] == 4 &&
                weights["536e6de0-7ccf-412a-9a4e-ec55ab3988e5"] == 1, "Original weighted Morsel pool differs.");
            Require(weights.Keys.All(id => Before(5).PlayRules!.Cards.Any(definition => definition.DataId == id)), "A pool member was missing before its first random selection.");
            Require(before.Energy - after.Energy == 1 && actual.Cards.Discard.Any(token => token.InstanceId == ids[i]), "Original cost or discard differs.");
            Require(Json(before.Spawn.Train.Rooms[0].Units) == Json(after.Spawn.Train.Rooms[0].Units), "Original spell changed its neighboring anchor floor.");
            UnityRng rng = context.BattleRng;
            var expected = new List<string>();
            if (i is not (7 or 11))
                for (int roll = 0; roll < 2; roll++) { var draw = rng.Range(0, generation.Pool.Count); rng = draw.State; expected.Add(generation.Pool[draw.Value].DataId); }
            int allocated = Math.Min(expected.Count, context.MaxHandSize - context.Cards.Hand.Count + 1);
            var added = actual.Cards.Hand.Where(token => token.InstanceId >= context.NextCardId).OrderBy(token => token.InstanceId).ToArray();
            Require(Json(actual.BattleRng) == Json(rng) && actual.NextCardId - context.NextCardId == allocated &&
                added.Select(token => token.DataId).SequenceEqual(expected.Take(allocated)), "Weighted roll, card allocation order or full-hand RNG differs.");
            if (i == 5) Require(before.Spawn.Turn == 0 && allocated == 2, "Positive deployment kill must create two Morsels.");
            if (i == 7) Require(Victim(before, 2).Health == 10 && Victim(after, 2).Health == 5, "Nonlethal control must deal five without generating cards.");
            if (i == 9) Require(Victim(before, 3).Status("undying")?.Stacks == 1 && Victim(after, 3).Health == 1 &&
                Victim(after, 3).Status("undying") == null && allocated == 2, "Undying hit must revive and still create two cards.");
            if (i == 11) Require(Victim(before, 4).Status("damage shield")?.Stacks == 1 && Victim(after, 4).Health == 2 &&
                Victim(after, 4).Status("damage shield") == null, "Shield control must consume its shield without rolling cards.");
            if (i == 13) Require(context.Cards.Hand.Count == context.MaxHandSize && allocated == 1 && actual.Cards.Hand.Count == context.MaxHandSize,
                "Full hand must free the played card's one slot, roll twice and allocate one result.");
        }
        Require(After(10).Spawn.Train.Context!.Cards.Hand.Count == 30, "Authored filler must reach the native hand limit.");
        foreach ((int action, int victim) in new[] { (5, 1), (8, 2), (12, 4), (13, 3) })
            Require(!After(action).Spawn.Train.Rooms[1].Units.Any(unit => unit.SpawnerCardId == ids[victim]), "A positive kill did not remove its victim.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-ON-KILL-GENERATION PASS: seven original Antumbra Assault plays, exact 5/5/4/4/1 weighted pool, deployment/exact/Undying kills, nonlethal/shield zero-roll controls, one-slot two-roll overflow, reachable pool definitions and 16 immutable full chains.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
