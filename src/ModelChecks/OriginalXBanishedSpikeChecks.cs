using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalXBanishedSpikeChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        for (int index = 0; index < ids.Length; index++)
            if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
            {
                int target = index >= 2
                    ? source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[0]).Id
                    : 0;
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play =>
                    play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0) &&
                    (target == 0 || play.TargetUnitId == target));
            }
        return null;
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalXBanishedSpikePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Spike of the Banished witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalXBanishedSpikeIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 4 && actions.Length == 4, "Missing selected ally, outside-floor and X=4/0 original-card plays.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Target(BattleTurnState state) => state.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]);

        foreach (int index in new[] { 2, 3 })
        {
            BattleTurnState before = Before(index), after = After(index);
            CombatContext context = before.Spawn.Train.Context!;
            CardInstanceState card = context.FindCard(ids[index])!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(candidate => candidate.DataId == "ab3c2bfa-0ad4-4be0-a050-293f4866bd65");
            CardSourceTrait[] raw = card.MaskDescriptor!.SourceTraits!.ToArray();
            ScalingStatusTrait scaling = card.StatusScalingTraits!.Single();
            int expected = index == 2 ? 4 : 0;
            CombatUnit target = Target(before), targetAfter = Target(after);
            int valorBefore = target.Status("valor")?.Stacks ?? 0;
            int valorAfter = targetAfter.Status("valor")?.Stacks ?? 0;
            int lastPlayedCost = after.Spawn.Train.Context!.FindCard(ids[index])!.LastPlayedCost;
            Require(card.DataId == "ab3c2bfa-0ad4-4be0-a050-293f4866bd65" && rule.AssetKey == "SpikeOfTheBanished" &&
                rule.Cost == 0 && rule.CostType == "ConsumeRemainingEnergy" && rule.Effects!.Count == 1 &&
                rule.Effects[0].Type == "AddStatus" && rule.Effects[0].Target == "DropTargetCharacter" &&
                rule.Effects[0].AllowEnemy && rule.Effects[0].AllowPlayer && rule.Effects[0].Statuses.Single().Id == "valor" &&
                rule.Effects[0].Statuses[0].Stacks == 0 && rule.Effects[0].Statuses[0].ParamInt == 1 &&
                raw.Length == 2 && raw[0].Type == "CardTraitScalingAddStatusEffect" && raw[0].Int == 4 && raw[0].Int2 == 0 &&
                raw[0].Int3 == 0 && !raw[0].Bool && raw[0].Str == "" && raw[0].Float == 1 && raw[0].Scaling &&
                raw[0].TrackedValue == "PlayedCost" && raw[0].EntryDuration == "ThisBattle" &&
                raw[0].StatusEffectIds!.SequenceEqual(new[] { "valor" }) && raw[1].Type == "CardTraitExhaustState" &&
                scaling.Query.Type == "PlayedCost" && scaling.StacksPerStat == 4 && !scaling.OnlyWhenSourceZero &&
                scaling.Filter == 0 && scaling.StatusIds.SequenceEqual(new[] { "valor" }),
                "Original Spike of the Banished effect or PlayedCost source-trait metadata differs.");
            Require(before.Energy == expected && after.Energy == 0 && lastPlayedCost == expected &&
                valorAfter - valorBefore == expected * 4 && targetAfter.Attack - target.Attack == expected * 4 &&
                targetAfter.Health == target.Health &&
                after.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[index]) &&
                context.BattleRng.Equals(after.Spawn.Train.Context.BattleRng) &&
                JsonSerializer.Serialize(before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != target.Id), ModelJson.Options) ==
                    JsonSerializer.Serialize(after.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != target.Id), ModelJson.Options) &&
                JsonSerializer.Serialize(before.Spawn.Train.Rooms.Skip(1).Select(room => room.Units), ModelJson.Options) ==
                    JsonSerializer.Serialize(after.Spawn.Train.Rooms.Skip(1).Select(room => room.Units), ModelJson.Options),
                "Spike of the Banished X payment/scaling, Consume route, target isolation or neighboring-floor state differs.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-X-BANISHED-SPIKE PASS: unchanged Spike of the Banished at X=4/0, exact Valor and Attack deltas, Consume routing, selected target and outside-floor controls.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
