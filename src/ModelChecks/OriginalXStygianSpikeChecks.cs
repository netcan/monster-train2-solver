using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalXStygianSpikeChecks
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
        Require(root.GetProperty("OriginalXStygianSpikePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Spike of the Stygian witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalXStygianSpikeIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 4 && actions.Length == 4, "Missing selected-ally and X=4/0 original-card plays.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Target(BattleTurnState state, int targetId) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == targetId);

        foreach (int index in new[] { 2, 3 })
        {
            BattleTurnState before = Before(index), after = After(index);
            CombatContext context = before.Spawn.Train.Context!;
            CardInstanceState card = context.FindCard(ids[index])!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(candidate => candidate.DataId == "bc5cea85-5b63-49fb-bd2b-fe057c015cdd");
            CardSourceTrait[] raw = card.MaskDescriptor!.SourceTraits!.ToArray();
            ScalingStatusTrait[] scaling = card.StatusScalingTraits!.ToArray();
            int expected = index == 2 ? 4 : 0;
            PlayCardAction action = actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatUnit target = Target(before, action.TargetUnitId), targetAfter = Target(after, action.TargetUnitId);
            int debuffBefore = target.Status("debuff")?.Stacks ?? 0;
            int debuffAfter = targetAfter.Status("debuff")?.Stacks ?? 0;
            int poisonBefore = target.Status("poison")?.Stacks ?? 0;
            int poisonAfter = targetAfter.Status("poison")?.Stacks ?? 0;
            int lastPlayedCost = after.Spawn.Train.Context!.FindCard(ids[index])!.LastPlayedCost;
            Require(card.DataId == "bc5cea85-5b63-49fb-bd2b-fe057c015cdd" && rule.AssetKey == "SpikeOfTheStygian" &&
                rule.Cost == 0 && rule.CostType == "ConsumeRemainingEnergy" && rule.Effects!.Count == 2 &&
                rule.Effects[0].Type == "AddStatus" && rule.Effects[0].Target == "DropTargetCharacter" &&
                rule.Effects[0].AllowEnemy && rule.Effects[0].AllowPlayer && rule.Effects[0].Statuses.Single().Id == "debuff" &&
                rule.Effects[0].Statuses[0].Stacks == 0 && rule.Effects[0].Statuses[0].ParamInt == 2 &&
                rule.Effects[0].Statuses[0].RemoveStackAtEnd &&
                rule.Effects[1].Type == "AddStatus" && rule.Effects[1].Target == "LastTargetedCharacters" &&
                rule.Effects[1].AllowEnemy && rule.Effects[1].AllowPlayer && rule.Effects[1].Statuses.Single().Id == "poison" &&
                rule.Effects[1].Statuses[0].Stacks == 0 && rule.Effects[1].Statuses[0].ParamInt == 1 &&
                rule.Effects[1].Statuses[0].RemoveWhenTriggered && raw.Length == 3 &&
                Source(raw[0], 1, "debuff") && Source(raw[1], 5, "poison") && raw[2].Type == "CardTraitExhaustState" &&
                raw[2].Int == 1 && raw[2].Int2 == 0 && raw[2].Int3 == 0 && !raw[2].Bool && raw[2].Str == "" &&
                raw[2].Float == 1 && raw[2].Scaling && raw[2].TrackedValue == "PlayedCost" && raw[2].EntryDuration == "ThisBattle" &&
                scaling.Length == 2 && Scaling(scaling.Single(trait => trait.StatusIds.SequenceEqual(new[] { "debuff" })), 1) &&
                Scaling(scaling.Single(trait => trait.StatusIds.SequenceEqual(new[] { "poison" })), 5),
                "Original Spike of the Stygian ordered effects or PlayedCost source-trait metadata differs.");
            Require(before.Energy == expected && after.Energy == 0 && lastPlayedCost == expected &&
                debuffAfter - debuffBefore == expected && poisonAfter - poisonBefore == expected * 5 &&
                targetAfter.Attack == target.Attack && targetAfter.Health == target.Health &&
                after.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[index]) &&
                context.BattleRng.Equals(after.Spawn.Train.Context.BattleRng) &&
                JsonSerializer.Serialize(before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != target.Id), ModelJson.Options) ==
                    JsonSerializer.Serialize(after.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != target.Id), ModelJson.Options) &&
                JsonSerializer.Serialize(before.Spawn.Train.Rooms.Skip(1).Select(room => room.Units), ModelJson.Options) ==
                    JsonSerializer.Serialize(after.Spawn.Train.Rooms.Skip(1).Select(room => room.Units), ModelJson.Options),
                "Spike of the Stygian X payment/status scaling, ordered selected target or neighboring-floor state differs.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-X-STYGIAN-SPIKE PASS: unchanged Spike of the Stygian at X=4/0, ordered Debuff/Poison scaling, Exhausted routing and outside-floor controls.");
    }

    private static bool Source(CardSourceTrait source, int amount, string status) => source.Type == "CardTraitScalingAddStatusEffect" &&
        source.Int == amount && source.Int2 == 0 && source.Int3 == 0 && !source.Bool && source.Str == "" && source.Float == 1 &&
        source.Scaling && source.TrackedValue == "PlayedCost" && source.EntryDuration == "ThisBattle" &&
        source.StatusEffectIds!.SequenceEqual(new[] { status });

    private static bool Scaling(ScalingStatusTrait trait, int amount) => trait.Query.Type == "PlayedCost" &&
        trait.StacksPerStat == amount && !trait.OnlyWhenSourceZero && trait.Filter == 0;

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
