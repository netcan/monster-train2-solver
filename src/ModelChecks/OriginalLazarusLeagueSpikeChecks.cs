using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalLazarusLeagueSpikeChecks
{
    private const string CardId = "7168ed36-de03-41db-a36f-79b54b121341";
    private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
    private const string Undying = "undying";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 251 &&
            root.GetProperty("ModifierScenario").GetString() == "original-lazarus-league-spike" &&
            root.GetProperty("OriginalLazarusLeagueSpikePrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Missing completed original Lazarus League Spike witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        int[] ids = root.GetProperty("OriginalLazarusLeagueSpikeIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        FixtureValue action = actions.Single(item => item.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[2]);
        BattleTurnState before = action.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = action.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction play = action.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext beforeContext = before.Spawn.Train.Context!;
        CombatContext afterContext = after.Spawn.Train.Context!;
        CardInstanceState card = beforeContext.FindCard(ids[2])!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == CardId);
        CombatUnit source = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == play.TargetUnitId);
        CombatUnit target = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == source.Id);
        CardSourceTrait[] traits = card.MaskDescriptor!.SourceTraits!.ToArray();
        CardSourceTrait exhaust = traits.Single(trait => trait.Type == "CardTraitExhaustState");
        CardSourceTrait scaling = traits.Single(trait => trait.Type == "CardTraitScalingAddStatusEffect");
        CombatStatus? undying = target.Status(Undying);

        Require(ids.Length == 3 && actions.Length >= 3 && card.DataId == CardId && rule.AssetKey == "LazarusLeagueSpike" &&
            rule.Cost == 0 && rule.CostType == "ConsumeRemainingEnergy" && rule.Effect == "Spell" &&
            rule.Effects.Count == 1 && rule.Effects[0].Type == "AddStatus" &&
            rule.Effects[0].Target == "DropTargetCharacter" && rule.Effects[0].AllowPlayer && rule.Effects[0].AllowEnemy &&
            rule.Effects[0].Statuses.Count == 1 && rule.Effects[0].Statuses[0].Id == Undying &&
            rule.Effects[0].Statuses[0].Stacks == 0 && rule.Effects[0].Statuses[0].ParamInt == 1 &&
            rule.ExternalInteractions.Count == 0 && rule.UpgradeInteractions!.Count == 0,
            "Original Lazarus League Spike identity, X-cost status effect or interaction guards differ.");
        Require(traits.Select(trait => trait.Type).SequenceEqual(["CardTraitExhaustState", "CardTraitScalingAddStatusEffect"]) &&
            exhaust.Int == 1 && exhaust.TrackedValue == "PlayedCost" && exhaust.EntryDuration == "ThisTurn" &&
            scaling.Int == 1 && scaling.Int2 == 0 && scaling.Int3 == 0 && !scaling.Scaling &&
            scaling.TrackedValue == "PlayedCost" && scaling.EntryDuration == "ThisTurn" &&
            scaling.StatusEffectIds?.SequenceEqual([Undying]) == true,
            "Original Consume or PlayedCost status-scaling traits differ.");
        Require(source.SpawnerCardId == ids[0] && beforeContext.FindCard(source.SpawnerCardId)?.DataId == StewardId &&
            source.Status(Undying) == null && undying?.Stacks == 4 && undying.ParamInt == 1 &&
            before.Energy == 4 && after.Energy == 0 &&
            afterContext.FindCard(ids[2])!.LastPlayedCost == 4 &&
            afterContext.Statistics.Value(ids[2], "TimesPlayed", "ThisBattle") == 1 &&
            !afterContext.Cards.Hand.Any(item => item.InstanceId == ids[2]) &&
            after.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(item => item.InstanceId == ids[2]),
            "Native four-Ember Undying scaling, target identity, play accounting or Consume routing differs.");

        RecordedBattleChecks.Native(root);
        Console.WriteLine($"ORIGINAL-LAZARUS-LEAGUE-SPIKE PASS: unchanged X-cost Spell spends four Ember, adds four Undying to its selected unit, exhausts and replays across {actions.Length} native actions.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
