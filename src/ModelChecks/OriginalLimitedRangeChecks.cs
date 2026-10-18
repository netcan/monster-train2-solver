using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalLimitedRangeChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        for (int index = 0; index < ids.Length; index++)
            if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
            {
                int room = index == 2 ? 3 : 2;
                if (index == 1 && !source.Spawn.Train.Rooms.Single(value => value.RoomIndex == room).Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0)) return null;
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == room);
            }
        return null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalLimitedRangePrepared").GetBoolean() && root.GetProperty("OriginalLimitedRangeQueriesUnchanged").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed Limited Range witness or query restoration.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalLimitedRangeIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray(), queries = root.GetProperty("OriginalLimitedRangeQueries").EnumerateArray().ToArray();
        Require(ids.Length == 3 && actions.Length == 3 && queries.Length == 12 && queries.Select(row => row.GetProperty("DataId").GetString()).Distinct().Count() == 3,
            "Missing original Limited Range plays or all three source filters.");
        foreach (FixtureValue query in queries)
        {
            CardSourceTrait trait = query.GetProperty("SourceTrait").Deserialize<CardSourceTrait>()!;
            Require(trait.Type == CardPlayRangeModel.Trait && trait.Int == 0 && trait.Int2 == 0 && trait.Int3 == 0 && !trait.Bool && trait.Str == "" &&
                trait.Float == 0 && !trait.Scaling && trait.TrackedValue == "SubtypeInDeck" && trait.EntryDuration == "ThisTurn", "Original Limited Range source changed.");
            int room = query.GetProperty("RoomIndex").GetInt32(), pyre = query.GetProperty("PyreRoomIndex").GetInt32();
            bool allowed = CardPlayRangeModel.Allows(room, pyre);
            Require(pyre == 3 && allowed == (room is 2 or 3) && query.GetProperty("FromHand").GetBoolean() == allowed && query.GetProperty("FromPlay").GetBoolean() == allowed &&
                query.GetProperty("HandError").GetString() == (allowed ? "None" : "Unplayable") && query.GetProperty("PlayError").GetString() == (allowed ? "None" : "Unplayable"),
                "Native hand/play range selection or errors differ.");
        }
        foreach (int index in new[] { 1, 2 })
        {
            BattleTurnState before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[index])!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(value => value.DataId == card.DataId);
            int room = index == 1 ? 2 : 3;
            Require(card.DataId == "27f9d3dc-b5aa-4a26-abae-aa197bedb6f2" && card.MaskDescriptor!.SourceTraits!.Select(trait => trait.Type).SequenceEqual([CardPlayRangeModel.Trait, "CardTraitStrongerMagicPower"]) &&
                rule.Cost == 1 && rule.Effects.Single().Value == 90 && rule.ExternalInteractions.Count == 0 && rule.UpgradeInteractions.Count == 0 &&
                before.Energy - after.Energy == 1 && actions[index].GetProperty("Action").GetProperty("RoomIndex").GetInt32() == room, "Original Ice and Pyre definition, payment or floor differs.");
            if (index == 1)
            {
                CombatUnit target = before.Spawn.Train.Rooms.Single(value => value.RoomIndex == room).Units.First(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0);
                CombatUnit changed = after.Spawn.Train.Rooms.SelectMany(value => value.Units).Single(unit => unit.Id == target.Id);
                Require(target.Health + (target.Status("armor")?.Stacks ?? 0) - changed.Health - (changed.Status("armor")?.Stacks ?? 0) == 90, "Original Ice and Pyre must deal ninety front-enemy damage.");
            }
            else Require(!before.Spawn.Train.Rooms.Single(value => value.RoomIndex == 3).Units.Any(unit => unit.Team == CombatTeam.Enemy) &&
                JsonSerializer.Serialize(before.Spawn.Train.Rooms.SelectMany(value => value.Units), ModelJson.Options) ==
                JsonSerializer.Serialize(after.Spawn.Train.Rooms.SelectMany(value => value.Units), ModelJson.Options), "Empty Pyre-floor cast must pay but leave every unit unchanged.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            foreach (int lower in new[] { 0, 1 })
            {
                BattleActionResult rejected = BattleActionModel.PlayCard(before, new PlayCardAction(ids[index], lower));
                Require(rejected.Rejection == ActionRejection.Illegal && rejected.Reason!.Contains("Limited Range"), "Lower-floor play must be illegal specifically because of Limited Range.");
            }
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Range rejection mutated its parent.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-LIMITED-RANGE PASS: original Ice and Pyre top-floor ninety damage and empty-Pyre paid cast, lower-floor illegal controls, 24 native hand/play trait checks from three original definitions, restored state/RNG/frame and 16 immutable chains. Other room payload guards remain separate.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
