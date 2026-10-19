using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalImpOlateChecks
{
    private const string CardDataId = "e6e77821-f0c2-4d76-bc7f-2efece0e065f";
    private const string ImpCardDataId = "d76be2d0-f851-4d43-bdc0-7c7a7779724f";
    private const string ImpSubtype = "SubtypesData_Imp_0f9b989f-15b5-4b16-8378-5d8ed8691e7c";

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = System.Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0) return null;
        if (index == 0)
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == 0);
        if (source.Spawn.Turn < 1 || !source.Spawn.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Enemy)) return null;
        return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == 0);
    }

    internal static void Native(FixtureValue root, bool expectImp)
    {
        Require(root.GetProperty("OriginalImpOlatePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Imp Olate witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalImpOlateIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 2 && actions.Length == 2, "Missing one protector deployment and one original Imp Olate play.");
        BattleTurnState before = actions[1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = actions[1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = actions[1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[1])!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(candidate => candidate.DataId == CardDataId);
        CardSourceTrait source = card.MaskDescriptor!.SourceTraits!.Single();
        ScalingDamageTrait scaling = card.DamageScalingTraits!.Single();
        Require(card.DataId == CardDataId && rule.AssetKey == "ImpOlate" && rule.CardType == "Spell" &&
            rule.Effects.Count == 1 && rule.Effects[0].Type == "Damage" && rule.ExternalInteractions.Count == 0 &&
            card.MaskDescriptor.SourceTraits is { Count: 1 } && card.MaskDescriptor.BaseTraits.Count == 1 &&
            source.Type == "CardTraitScalingAddDamage" && source.Int == 25 && source.Int2 == 0 && source.Int3 == 0 &&
            !source.Bool && source.Str == "" && source.Float == 1 && !source.Scaling && source.TrackedValue == "SubtypeInDeck" &&
            source.EntryDuration == "ThisTurn" && scaling.DamagePerStat == 25 && scaling.Multiplier == 1 && !scaling.AddToDamage &&
            scaling.Query.Type == "SubtypeInDeck" && scaling.Query.Duration == "ThisTurn" && scaling.Query.Subtype == ImpSubtype &&
            scaling.Query.SubtypeMatchDataIds?.Contains(ImpCardDataId) == true && !scaling.Query.SubtypeIsNone &&
            scaling.Query.TypeMatchDataIds == null && scaling.Query.SpecificCardDataId == null && scaling.Query.SourceRawCost == 1 &&
            !scaling.Query.VariableCost && scaling.Query.TargetTeam == "Heroes" && scaling.Query.CurrentCostCardModifiersOnly &&
            scaling.Query.CurrentCostSourceIsSpell && scaling.Query.StatusIds == null && rule.Cost == 1 && rule.CostType == "Default" &&
            rule.Effects[0].Target == "FrontInRoom" && rule.Effects[0].Value == 0 &&
            before.Spawn.Turn >= 1 && before.Spawn.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Enemy) &&
            before.Spawn.Train.Context.Statistics!.Value(ids[1], "TimesPlayed", "ThisBattle") == 0 &&
            after.Spawn.Train.Context!.Statistics!.Value(ids[1], "TimesPlayed", "ThisBattle") == 1 &&
            after.Spawn.Train.Context.Cards.Discard.Any(token => token.InstanceId == ids[1]) &&
            after.Spawn.Train.Context.FindCard(ids[1])!.LastPlayedCost == rule.Cost,
            "Original Imp Olate identity, source traits, damage projection or play lifecycle differs.");
        int expectedCount = expectImp ? 1 : 0;
        StatisticQueryResult count = StatisticQueryModel.Evaluate(before.Spawn.Train.Context, scaling.Query, ids[1]);
        DamageScalingResult projected = DamageScalingModel.Apply(before.Spawn.Train.Context, ids[1], ids[1], rule.Effects[0].Value);
        CombatUnit target = before.Spawn.Train.Rooms[0].Units.First(unit => unit.Team == CombatTeam.Enemy);
        CombatUnit? hit = after.Spawn.Train.Rooms[0].Units.FirstOrDefault(unit => unit.Id == target.Id);
        int targetArmor = target.Status("armor")?.Stacks ?? 0;
        int actualArmor = hit?.Status("armor")?.Stacks ?? 0;
        Require(count.Supported && count.Value == expectedCount && projected.Supported && projected.Damage == expectedCount * 25 &&
            before.Spawn.Train.Context.CardInstances!.Count(instance => instance.DataId == ImpCardDataId) == expectedCount &&
            action.RoomIndex == 0 && target.Health > 0 && hit != null &&
            (expectImp ? hit.Health == target.Health && targetArmor == 25 && actualArmor == 0 :
                hit.Health == target.Health && actualArmor == targetArmor),
            "Imp Olate SubtypeInDeck damage did not match the native zero/one-Imp witness.");
        if (expectImp)
        {
            int witnessId = root.GetProperty("OriginalImpOlateImpWitnessId").GetInt32();
            Require(witnessId > 0 && before.Spawn.Train.Context.Cards.Hand.Any(token => token.InstanceId == witnessId && token.DataId == ImpCardDataId),
                "Native Imp witness is not present in the captured owned-card query boundary.");
        }
        RecordedBattleChecks.Native(root);
        System.Console.WriteLine($"ORIGINAL-IMP-OLATE {(expectImp ? "ONE-IMP" : "ZERO-IMP")} PASS: exact source trait, native subtype query count, projected damage and complete replay.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}
