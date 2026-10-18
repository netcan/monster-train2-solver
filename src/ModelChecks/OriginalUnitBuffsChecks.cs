using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalUnitBuffsChecks
{
    private static readonly string[] Originals = { "175a7730-7a1c-4da1-97b8-d799e7570396", "84314844-3a47-4173-9d34-5d15de013571",
        "194685d1-6c39-42ce-9208-bcadcf5c34a9", "a1b4c056-c5cc-4389-ab6c-aca170119a05", "777eb807-d29c-45e5-b1f2-cafd768ce2a5",
        "e77e9f90-8a79-4af5-bbb1-31a99bdf2466", "116e651d-eebb-43dd-a7fb-f3cce68997de", "331b6cd2-cdf9-452e-a35d-5d310240190d" };
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
        int target = index >= 4 && index != 7 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
        var action = new PlayCardAction(ids[index], index == 2 ? 1 : 0, playerPosition: index < 2 ? index : index == 2 ? 0 : -1, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalUnitBuffsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original unit buffs witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalUnitBuffsIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 12 && actions.Length == 12, "Missing eight original spells or three anchors and battery.");
        int Stacks(CombatUnit unit, string status) => unit.Status(status)?.Stacks ?? 0;
        int consumed = 0;
        for (int i = 4; i < 12; i++)
        {
            BattleTurnState before = actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var card = before.Spawn.Train.Context!.FindCard(ids[i])!;
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == Originals[i - 4]);
            Require(card.DataId == Originals[i - 4] && rule.ExternalInteractions.Count == 0 && before.Energy - after.Energy == rule.Cost,
                "Original identity, cost or projection differs.");
            bool consume = CardExhaustModel.HasConsume(card); if (consume) consumed++;
            Require(consume ? after.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[i]) :
                after.Spawn.Train.Context!.Cards.Discard.Any(token => token.InstanceId == ids[i]), "Original played discard or Consume route differs.");
            CombatUnit a = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]), b = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == a.Id);
            Require(Json(before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != a.Id)) == Json(after.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != a.Id)) &&
                Json(before.Spawn.Train.Rooms[1].Units) == Json(after.Spawn.Train.Rooms[1].Units), "Same-floor control, enemy or neighbor floor changed.");
            if (i == 4) Require(b.MaxHealth - a.MaxHealth == 10 && Stacks(b, "spikes") - Stacks(a, "spikes") == 8, "Cycle of Life health/Spikes differs.");
            if (i == 5) Require(b.Attack - a.Attack == 4 && b.MaxHealth - a.MaxHealth == 4, "Steel Enhancer must add four attack/health.");
            if (i == 6) Require(b.Attack - a.Attack == 10 && Stacks(a, "ephemeral") == 0 && Stacks(b, "ephemeral") == 2, "Wicklash must add attack and two Burnout.");
            if (i == 7) Require(Stacks(a, "ephemeral") == 2 && Stacks(b, "ephemeral") == 7 && b.Attack - a.Attack == 5 &&
                rule.Effects.All(effect => effect.Filters!.RequiredStatuses.SequenceEqual(new[] { "ephemeral" })), "Wick Extension positive/filter control differs.");
            if (i == 8) Require(Stacks(b, "damage shield") - Stacks(a, "damage shield") == 2 && Stacks(b, "buff") - Stacks(a, "buff") == 6 &&
                Stacks(b, "scorch") - Stacks(a, "scorch") == 3, "Void Binding ordered shield/Rage/Emberdrain differs.");
            if (i == 9) Require(Stacks(b, "trample") - Stacks(a, "trample") == 1, "Umbra Stone Trample differs.");
            if (i == 10) Require(Stacks(b, "unstable") - Stacks(a, "unstable") == 50 && Stacks(b, "undying") - Stacks(a, "undying") == 1, "Tome of Horrors Unstable/Undying differs.");
            if (i == 11) Require(Stacks(b, "ambush") - Stacks(a, "ambush") == 1, "Wildwood Tome Quick differs.");
        }
        Require(consumed == 4, "Missing four native Consume routes.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-UNIT-BUFFS PASS: eight unchanged originals, positive upgrade/status changes, Burnout filter and same/neighbor-floor controls, four Consume routes and 16 immutable complete chains.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
