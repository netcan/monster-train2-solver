using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalXWurmkinSpikeChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        for (int index = 0; index < ids.Length; index++)
            if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
            {
                if (index >= 2 && !source.Spawn.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Enemy)) return null;
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play =>
                    play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
            }
        return null;
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalXWurmkinSpikePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Wurmkin Spike witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalXWurmkinSpikeIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 4 && actions.Length == 4, "Missing two deployments and positive/zero X original-card plays.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;

        foreach (int index in new[] { 2, 3 })
        {
            BattleTurnState before = Before(index), after = After(index);
            CombatContext context = before.Spawn.Train.Context!;
            CardInstanceState card = context.FindCard(ids[index])!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(candidate => candidate.DataId == "83c87c23-39a9-4c92-a6d0-3f640c35534d");
            CardSourceTrait[] raw = card.MaskDescriptor!.SourceTraits!.ToArray();
            ScalingStatusTrait scaling = card.StatusScalingTraits!.Single();
            int expected = index == 2 ? 3 : 0;
            PlayCardAction action = actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            RoomCombatState room = before.Spawn.Train.Rooms.Single(candidate => candidate.RoomIndex == action.RoomIndex);
            RoomCombatState roomAfter = after.Spawn.Train.Rooms.Single(candidate => candidate.RoomIndex == action.RoomIndex);
            int[] enemyIds = room.Units.Where(unit => unit.Team == CombatTeam.Enemy).Select(unit => unit.Id).ToArray();
            Require(enemyIds.Length > 0, "Wurmkin Spike witness room lacks enemy units.");
            CombatUnit Unit(BattleTurnState state, int id) => state.Spawn.Train.Rooms.SelectMany(candidate => candidate.Units).Single(unit => unit.Id == id);
            int Status(BattleTurnState state, int id) => state.Spawn.Train.Rooms.SelectMany(candidate => candidate.Units)
                .Single(unit => unit.Id == id).Status("corrupt_poison")?.Stacks ?? 0;
            int lastPlayedCost = after.Spawn.Train.Context!.FindCard(ids[index])!.LastPlayedCost;
            Require(card.DataId == "83c87c23-39a9-4c92-a6d0-3f640c35534d" && rule.AssetKey == "WurmkinSpike" &&
                rule.Cost == 0 && rule.CostType == "ConsumeRemainingEnergy" && rule.Effects!.Count == 1 &&
                rule.Effects[0].Type == "AddStatus" && rule.Effects[0].Target == "Room" &&
                rule.Effects[0].AllowEnemy && !rule.Effects[0].AllowPlayer && rule.Effects[0].Statuses.Single().Id == "corrupt_poison" &&
                rule.Effects[0].Statuses[0].Stacks == 0 && rule.Effects[0].Statuses[0].ParamInt == 1 &&
                raw.Length == 2 && raw[0].Type == "CardTraitExhaustState" && raw[0].Int == 0 && raw[0].Int2 == 0 &&
                raw[0].Int3 == 0 && !raw[0].Bool && raw[0].Str == "" && raw[0].Float == 0 && !raw[0].Scaling &&
                raw[0].TrackedValue == "SubtypeInDeck" && raw[0].EntryDuration == "ThisTurn" &&
                raw[1].Type == "CardTraitScalingAddStatusEffect" && raw[1].Int == 6 && raw[1].Int2 == 0 && raw[1].Int3 == 0 &&
                !raw[1].Bool && raw[1].Str == "" && raw[1].Float == 1 && raw[1].Scaling &&
                raw[1].TrackedValue == "PlayedCost" && raw[1].EntryDuration == "ThisTurn" &&
                raw[1].StatusEffectIds!.SequenceEqual(new[] { "corrupt_poison" }) && scaling.Query.Type == "PlayedCost" &&
                scaling.StacksPerStat == 6 && !scaling.OnlyWhenSourceZero && scaling.Filter == 0 &&
                scaling.StatusIds.SequenceEqual(new[] { "corrupt_poison" }),
                "Original Wurmkin Spike effect or PlayedCost source-trait metadata differs.");
            Require(before.Energy == expected && after.Energy == 0 && lastPlayedCost == expected &&
                enemyIds.All(id => Status(after, id) - Status(before, id) == expected * 6 &&
                    Unit(after, id).Health == Unit(before, id).Health && Unit(after, id).Attack == Unit(before, id).Attack) &&
                JsonSerializer.Serialize(room.Units.Where(unit => unit.Team == CombatTeam.Player), ModelJson.Options) ==
                    JsonSerializer.Serialize(roomAfter.Units.Where(unit => unit.Team == CombatTeam.Player), ModelJson.Options),
                "Wurmkin Spike X payment/status scaling or immediate room isolation differs.");
            Require(context.BattleRng.Equals(after.Spawn.Train.Context.BattleRng) &&
                JsonSerializer.Serialize(before.Spawn.Train.Rooms.Where(candidate => candidate.RoomIndex != action.RoomIndex).Select(candidate => candidate.Units), ModelJson.Options) ==
                    JsonSerializer.Serialize(after.Spawn.Train.Rooms.Where(candidate => candidate.RoomIndex != action.RoomIndex).Select(candidate => candidate.Units), ModelJson.Options),
                "Wurmkin Spike changed deterministic RNG or another floor.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-X-WURMKIN-SPIKE PASS: unchanged Wurmkin Spike at X=3/0, exact enemy-room Corrupt Poison scaling and outside-floor controls.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
