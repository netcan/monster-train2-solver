using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalPositiveStatusChecks
{
    private const string CardId = "f213cc62-90f4-4bc8-a96f-0b191a7ec7ef";
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
        int target = index is 4 or 9 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[index == 4 ? 0 : 1]).Id :
            index == 7 ? source.Spawn.Train.Rooms[0].Units.FirstOrDefault(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0)?.Id ?? 0 : 0;
        if (index == 7 && target == 0) return null;
        return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 2 ? 1 : 0) && play.TargetUnitId == target);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalPositiveStatusPrepared").GetBoolean() && root.GetProperty("OriginalPositiveStatusQueriesUnchanged").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed Symphony witness or unchanged queries.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalPositiveStatusIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray(), queries = root.GetProperty("OriginalPositiveStatusQueries").EnumerateArray().ToArray(),
            effects = root.GetProperty("OriginalPositiveStatusEffects").EnumerateArray().ToArray();
        Require(ids.Length == 10 && actions.Length == 10 && queries.Length == 4 && effects.Length == 5, "Missing friendly/enemy/empty-positive controls.");
        foreach (FixtureValue row in queries)
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!; int id = row.GetProperty("CardId").GetInt32();
            string parent = Json(before); CardInstanceState card = before.Spawn.Train.Context!.FindCard(id)!;
            Require(CardCorruptRestrictedModel.Requirement(card, out int amount) == null && amount == 5 &&
                CardCorruptRestrictedModel.Test(before.Spawn.Train.Context, card, 0, out bool allowed) == null &&
                allowed == (row.GetProperty("Corruption").GetInt32() >= 5) && allowed == row.GetProperty("FromHand").GetBoolean() && allowed == row.GetProperty("FromPlay").GetBoolean(),
                "Original threshold-five hand/play legality differs.");
            Require(parent == Json(before), "Legality query mutated its parent.");
        }
        foreach (FixtureValue row in effects)
        {
            int id = row.GetProperty("CardId").GetInt32();
            FixtureValue action = actions.Single(item => item.GetProperty("Action").Deserialize<PlayCardAction>()!.CardInstanceId == id);
            BattleTurnState actionBefore = action.GetProperty("Before").Deserialize<BattleTurnState>()!;
            PlayCardAction play = action.GetProperty("Action").Deserialize<PlayCardAction>()!;
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, after = row.GetProperty("After").Deserialize<TrainCombatState>()!;
            string type = row.GetProperty("Effect").GetString()!;
            Require(before.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 1, "Corruption must be paid before either native effect.");
            CardActionEffect effect = actionBefore.PlayRules!.Cards.Single(rule => rule.DataId == CardId).Effects.Single(effect => effect.Type == type);
            string parent = Json(before);
            // This is one already-resolved native effect, rather than the full two-effect card.
            TrainSpellResult predicted = CardSpellModel.Apply(before, play.RoomIndex, [effect], play.TargetUnitId, id);
            string? difference = predicted.Supported ? ModelJson.Difference(Json(predicted.State), Json(after)) : predicted.UnsupportedReason;
            Require(row.GetProperty("Completed").GetBoolean() && difference == null, "Native complete Symphony effect boundary differs: " + difference);
            Require(parent == Json(before), "Effect application mutated its parent.");
        }
        foreach (int index in new[] { 4, 7, 9 })
        {
            BattleTurnState before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction play = actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatUnit a = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == play.TargetUnitId), b = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == play.TargetUnitId);
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[index])!;
            Require(card.DataId == CardId && CardCorruptRestrictedModel.Requirement(card, out int amount) == null && amount == 5 &&
                card.MaskDescriptor!.SourceTraits!.Count == 1 && card.MaskDescriptor.SourceTraits[0].Int == 5 &&
                before.Energy - after.Energy == 2 && before.Spawn.Train.Context.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 6 &&
                after.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 1 &&
                after.Spawn.Train.Context.Cards.Discard.Any(item => item.InstanceId == ids[index]), "Original cost, corruption payment or discard differs.");
            foreach (CombatStatus status in a.Statuses)
            {
                int multiplier = status.Id == "armor" && a.Team == CombatTeam.Player ? 2 : status.Id != "horde" && status.DisplayCategory == "Positive" ? 2 : 1;
                Require(b.Status(status.Id)?.Stacks == status.Stacks * multiplier, "Native status multiplication differs: " + status.Id);
            }
            Require(Json(before.Spawn.Train.Rooms[1].Units) == Json(after.Spawn.Train.Rooms[1].Units), "Outside-floor unit changed.");
            foreach (CombatUnit control in before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != a.Id))
                Require(Json(control) == Json(after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == control.Id)), "Non-target unit changed.");
            var expected = a.Team == CombatTeam.Player ? before.Spawn.Train.Context.BattleRng.Range(0, 100).State : before.Spawn.Train.Context.BattleRng;
            Require(Json(expected) == Json(after.Spawn.Train.Context.BattleRng), "Armor chance RNG differs, including the zero-armor control.");
            Require(a.Health == b.Health && a.MaxHealth == b.MaxHealth && (index == 9 || a.Status("debuff") != null),
                "Missing negative-status or health control.");
            if (index == 9) Require(!a.Statuses.Any(status => status.DisplayCategory == "Positive") && a.Status("armor") == null && Json(a) == Json(b), "Missing native empty-positive/zero-armor no-op.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-POSITIVE-STATUS PASS: original Symphony, threshold-five restored legality, full effect boundaries, doubled Rage/Regeneration, persistent armor excluded from the first stage and doubled only for friendlies, unchanged enemy armor/negative/non-target/floor controls, empty-positive no-op, exact chance RNG, corruption/ember/discard and 16 immutable chains. Horde-positive and relic-modified combinations need separate witnesses.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
