using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalCorruptRestrictedChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
        if (index == 6 && !source.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0)) return null;
        int target = index is 3 or 4 ? source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
        return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0) && play.TargetUnitId == target);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalCorruptRestrictedPrepared").GetBoolean() && root.GetProperty("OriginalCorruptRestrictedQueriesUnchanged").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed Corrupt Restricted witness or query restoration.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalCorruptRestrictedIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray(), queries = root.GetProperty("OriginalCorruptRestrictedQueries").EnumerateArray().ToArray(),
            prePlays = root.GetProperty("OriginalCorruptRestrictedPrePlays").EnumerateArray().ToArray();
        Require(ids.Length == 7 && actions.Length == 7 && queries.Length == 12 && prePlays.Length == 3, "Missing original threshold, pre-effect or refill controls.");
        foreach (FixtureValue query in queries)
        {
            BattleTurnState before = query.GetProperty("Before").Deserialize<BattleTurnState>()!;
            CombatContext context = before.Spawn.Train.Context!; int id = query.GetProperty("CardId").GetInt32();
            CardInstanceState card = context.FindCard(id)!;
            Require(CardCorruptRestrictedModel.Requirement(card, out int amount) == null && amount == query.GetProperty("Required").GetInt32() && amount is 1 or 2,
                "Original captured corruption requirement differs.");
            bool expected = query.GetProperty("Corruption").GetInt32() >= amount;
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            Require(CardCorruptRestrictedModel.Test(context, card, 0, out bool allowed) == null && allowed == expected &&
                query.GetProperty("FromHand").GetBoolean() == expected && query.GetProperty("FromPlay").GetBoolean() == expected &&
                new[] { "HandError", "PlayError" }.All(key => query.GetProperty(key).GetString() == (expected ? "None" : "InsufficientCorruption")) &&
                query.GetProperty("SourceRoomIndex").GetInt32() == 1 && context.RoomCapacities!.Single(room => room.RoomIndex == 1).Corruption == 0,
                "Native selected-room hand/play threshold or source-floor control differs.");
            if (!expected)
            {
                BattleActionResult rejected = BattleActionModel.PlayCard(before, new PlayCardAction(id, 0));
                Require(rejected.Rejection == ActionRejection.Illegal && rejected.Reason!.Contains("Insufficient corruption"), "Below-threshold direct play must be specifically illegal.");
            }
            Require(CardCorruptRestrictedModel.Test(context, card, 1, out bool neighbor) == null && !neighbor, "Selected-floor corruption must not satisfy the empty neighbor.");
            CardMaskDescriptor descriptor = card.MaskDescriptor!;
            CardInstanceState WithSource(IReadOnlyList<CardSourceTrait>? sources) => card.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost,
                descriptor.BaseTraits, descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft, descriptor.TemporaryTraits, descriptor.PermanentReplacements, sources));
            Require(CardCorruptRestrictedModel.Requirement(WithSource(null), out _) != null, "Missing positive requirement provenance must be rejected.");
            CardSourceTrait[] changed = descriptor.SourceTraits!.Select(source => source.Type != CardCorruptRestrictedModel.Trait ? source :
                new CardSourceTrait(source.Type, source.Int, source.Int2, source.Int3, true, source.Str, source.Float, source.Scaling, source.TrackedValue, source.EntryDuration)).ToArray();
            Require(CardCorruptRestrictedModel.Requirement(WithSource(changed), out _) != null, "Source-room requirement must remain guarded.");
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Threshold query or failed action mutated its parent.");
        }
        foreach (FixtureValue row in prePlays)
        {
            Require(row.GetProperty("Completed").GetBoolean(), "Incomplete native pre-play callback.");
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, after = row.GetProperty("After").Deserialize<TrainCombatState>()!;
            int id = row.GetProperty("CardId").GetInt32(), room = row.GetProperty("RoomIndex").GetInt32();
            RoomCorruptionResult predicted = CardCorruptRestrictedModel.BeforeEffects(before, before.Context!.FindCard(id)!, room);
            Require(predicted.Supported && JsonSerializer.Serialize(CardSpellModel.WithContext(before, predicted.Context!), ModelJson.Options) == JsonSerializer.Serialize(after, ModelJson.Options),
                "Native positive corruption subtraction or pre-effect ordering differs.");
        }
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Friendly(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[0]);
        int Corruption(BattleTurnState state) => state.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption!.Value;
        Require(Corruption(After(2)) == 4 && Corruption(After(3)) == 3 && Corruption(After(4)) == 1 && Corruption(After(5)) == 4 && Corruption(After(6)) == 2,
            "Native fill, one/two consumption and restoration sequence differs.");
        Require(Friendly(After(3)).MaxHealth - Friendly(Before(3)).MaxHealth == 15 && Friendly(After(4)).Attack - Friendly(Before(4)).Attack == 15,
            "Original Echo Infusion/Transfer must apply positive health/attack upgrades.");
        CombatUnit enemy = Before(6).Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.First(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0);
        CombatUnit hit = After(6).Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == enemy.Id);
        Require(enemy.Health + (enemy.Status("armor")?.Stacks ?? 0) - hit.Health - (hit.Status("armor")?.Stacks ?? 0) == 80,
            "Original Proclamation must deal eighty front-enemy damage after paying two corruption.");
        foreach (int index in new[] { 3, 4, 6 })
        {
            CardInstanceState card = Before(index).Spawn.Train.Context!.FindCard(ids[index])!;
            Require(card.DataId == (index == 3 ? "096def92-500b-4b1f-89f7-43e96aa1e54a" : index == 4 ? "afbbafc8-fd5a-42af-ac79-a677ab0185d8" : "4de04eeb-76d9-4ad0-b5cd-8c2b997395f5") &&
                Before(index).Energy == After(index).Energy && card.MaskDescriptor!.SourceTraits!.All(trait => trait.Int2 == 0 && trait.Int3 == 0 && !trait.Bool && trait.Str == "" &&
                    trait.Float == 0 && !trait.Scaling && trait.TrackedValue == "SubtypeInDeck" && trait.EntryDuration == "ThisTurn") &&
                Before(index).Spawn.Train.Context.RoomCapacities!.Single(room => room.RoomIndex == 1).Corruption == After(index).Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 1).Corruption,
                "Original source metadata, zero ember cost or outside-floor corruption differs.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-CORRUPT-RESTRICTED PASS: original Echo Infusion/Transfer/Proclamation, twelve restored below/at/above hand/play queries, source-floor controls, positive pre-effect subtraction/refill, health/attack/eighty damage and 16 immutable full chains. Source-room abilities, Corrupt State and upgrade-added traits remain guarded.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
