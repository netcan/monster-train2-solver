using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalXLunaSpikeChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        for (int index = 0; index < ids.Length; index++)
            if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play =>
                    play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0) &&
                    (index < 2 || play.TargetUnitId > 0));
        return null;
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalXLunaSpikePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Spike of the Luna Coven witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalXLunaSpikeIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 4 && actions.Length == 4, "Missing selected-ally, outside-floor and X=4/0 original-card plays.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Target(BattleTurnState state, int targetId) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == targetId);

        foreach (int index in new[] { 2, 3 })
        {
            BattleTurnState before = Before(index), after = After(index);
            CombatContext context = before.Spawn.Train.Context!;
            CardInstanceState card = context.FindCard(ids[index])!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(candidate => candidate.DataId == "5656f5dd-e400-4e19-96d3-c9415820a4bc");
            CardSourceTrait[] raw = card.MaskDescriptor!.SourceTraits!.ToArray();
            ScalingStatusTrait scaling = card.StatusScalingTraits!.Single();
            int expected = index == 2 ? 4 : 0;
            int targetId = actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!.TargetUnitId;
            CombatUnit target = Target(before, targetId), targetAfter = Target(after, targetId);
            int conduitBefore = target.Status("conduit")?.Stacks ?? 0;
            int conduitAfter = targetAfter.Status("conduit")?.Stacks ?? 0;
            int lastPlayedCost = after.Spawn.Train.Context!.FindCard(ids[index])!.LastPlayedCost;
            Require(card.DataId == "5656f5dd-e400-4e19-96d3-c9415820a4bc" && rule.AssetKey == "SpikeOfTheLunaCoven" &&
                rule.Cost == 0 && rule.CostType == "ConsumeRemainingEnergy" && rule.Effects!.Count == 1 &&
                rule.Effects[0].Type == "AddStatus" && rule.Effects[0].Target == "DropTargetCharacter" &&
                !rule.Effects[0].AllowEnemy && rule.Effects[0].AllowPlayer && rule.Effects[0].Statuses.Single().Id == "conduit" &&
                rule.Effects[0].Statuses[0].Stacks == 0 && rule.Effects[0].Statuses[0].ParamInt == 1 &&
                raw.Length == 2 && raw[0].Type == "CardTraitExhaustState" && raw[0].Int == 3 && raw[0].Int2 == 0 &&
                raw[0].Int3 == 0 && !raw[0].Bool && raw[0].Str == "" && raw[0].Float == 0 && !raw[0].Scaling &&
                raw[0].TrackedValue == "PlayedCost" && raw[0].EntryDuration == "ThisBattle" &&
                raw[1].Type == "CardTraitScalingAddStatusEffect" && raw[1].Int == 3 && raw[1].Int2 == 0 && raw[1].Int3 == 0 &&
                !raw[1].Bool && raw[1].Str == "" && raw[1].Float == 0 && !raw[1].Scaling &&
                raw[1].TrackedValue == "PlayedCost" && raw[1].EntryDuration == "ThisBattle" &&
                raw[1].StatusEffectIds!.SequenceEqual(new[] { "conduit" }) && scaling.Query.Type == "PlayedCost" &&
                scaling.StacksPerStat == 3 && !scaling.OnlyWhenSourceZero && scaling.Filter == 0 &&
                scaling.StatusIds.SequenceEqual(new[] { "conduit" }),
                "Original Spike of the Luna Coven effect or PlayedCost source-trait metadata differs.");
            Require(before.Energy == expected && after.Energy == 0 && lastPlayedCost == expected &&
                conduitAfter - conduitBefore == expected * 3 && targetAfter.Attack == target.Attack && targetAfter.Health == target.Health &&
                after.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[index]) &&
                context.BattleRng.Equals(after.Spawn.Train.Context.BattleRng) &&
                JsonSerializer.Serialize(before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != target.Id), ModelJson.Options) ==
                    JsonSerializer.Serialize(after.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != target.Id), ModelJson.Options) &&
                JsonSerializer.Serialize(before.Spawn.Train.Rooms.Skip(1).Select(room => room.Units), ModelJson.Options) ==
                    JsonSerializer.Serialize(after.Spawn.Train.Rooms.Skip(1).Select(room => room.Units), ModelJson.Options),
                "Spike of the Luna Coven X payment/scaling, discard route, target isolation or neighboring-floor state differs.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-X-LUNA-SPIKE PASS: unchanged Spike of the Luna Coven at X=4/0, exact Conduit scaling, Exhausted routing, selected target and outside-floor controls.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
