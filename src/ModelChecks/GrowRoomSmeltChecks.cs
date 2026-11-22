using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class GrowRoomSmeltChecks
{
    private const string GrowRoom = "7768326f-bc62-4143-93ba-63a5a17b9306";
    private const string Metallurgy = "bdfec381-a336-4b7d-b08a-c10c7ce231c0";
    private const string Morsel = "89605a1e-342e-401d-8f54-396ec12a7ede";
    private const string Unsupported = "Room smelt modifiers require native room-state calibration.";

    private sealed record Query(int Room, int Input, int Forge, int Ember);

    internal static void Native(FixtureValue query, FixtureValue battle)
    {
        foreach (FixtureValue root in new[] { query, battle })
            Require(root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
                root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module, "Unexpected native module.");
        Require(query.GetProperty("Schema").GetInt32() == 1 &&
            query.GetProperty("Boundary").GetString() == "NativeOriginalGrowRoomSmeltCosts" &&
            query.GetProperty("AttachmentId").GetString() == GrowRoom && query.GetProperty("CardId").GetString() == Morsel &&
            query.GetProperty("Modifier").GetString() == "RoomStateAddEffectPostCombatModifier" &&
            query.GetProperty("DecisionUnchanged").GetBoolean() && query.GetProperty("Turn").GetInt32() == 1 &&
            query.GetProperty("FrameBefore").GetInt32() == query.GetProperty("FrameAfter").GetInt32() &&
            query.GetProperty("RngBefore").ContentEquals(query.GetProperty("RngAfter")),
            "Missing native live GrowRoom/control-room smelt query or purity evidence.");
        var observed = query.GetProperty("Decision").Deserialize<BattleTurnState>()!;
        CombatContext observedContext = observed.Spawn.Train.Context!;
        Require(observed.Spawn.Turn == 1 && observedContext.Cards.Hand.Any(card =>
                card.DataId == Morsel && card.InstanceId == query.GetProperty("CardInstanceId").GetInt32()) &&
            observedContext.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single().DataId == GrowRoom &&
            observedContext.RoomCapacities!.Single(room => room.RoomIndex == 1).Attachments is { Count: 0 },
            "Native query did not use a live original card and the recorded attachment/control rooms.");
        Query[] rows = query.GetProperty("Rows").Deserialize<Query[]>()!;
        Query[] unchanged = Enumerable.Range(0, 2).SelectMany(room => Enumerable.Range(0, 3)
            .Select(cost => new Query(room, cost, cost, cost))).ToArray();
        Require(rows.SequenceEqual(unchanged), "The original RoomState queries changed smelt resources.");

        Require(battle.GetProperty("NativeWon").GetBoolean() && battle.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(field => battle.GetProperty(field).GetInt32() == 0),
            "The original seven-card battle did not settle with exact comparisons.");
        FixtureValue[] actions = battle.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length == 11 && battle.GetProperty("Turns").GetArrayLength() == 5, "Unexpected seven-card battle shape.");
        BattleTurnState before = actions[9].GetProperty("Before").Deserialize<BattleTurnState>()!;
        CombatContext context = before.Spawn.Train.Context!;
        CardToken metall = context.Cards.Hand.Single(card => card.DataId == Metallurgy);
        CardToken morsel = context.Cards.Hand.Single(card => card.DataId == Morsel);
        RoomCapacityState capacity = context.RoomCapacities!.Single(room => room.RoomIndex == 0);
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == Metallurgy);
        int smeltIndex = rule.Effects.Count - 1;
        CardActionEffect smelt = rule.Effects[smeltIndex];
        Require(capacity.Attachments!.Single().DataId == GrowRoom && smelt is { Type: "ChooseDiscard", Value: 3, ChooseDiscard.GainEmberForCost: true } &&
            actions[9].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == morsel.InstanceId,
            "The witness did not expose the intended original optional smelt input.");

        using var keys = new BattleStateKeys();
        string parent = keys.Key(before);
        var play = new PlayCardAction(metall.InstanceId, 0, recursionSelections: [new CardRecursionSelection(smeltIndex, [morsel.InstanceId])]);
        BattleActionResult alternative = BattleActionModel.PlayCard(before, play);
        Query expected = rows.Single(row => row.Room == 0 && row.Input == context.FindCard(morsel.InstanceId)!.MaskDescriptor!.BaseCost);
        Require(alternative.Supported && alternative.State is { } successor &&
            successor.ForgePoints == before.ForgePoints + expected.Forge &&
            successor.Energy == before.Energy - rule.Cost + expected.Ember &&
            successor.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == morsel.InstanceId) &&
            parent == keys.Key(before), "The optional original smelt branch disagrees with the observed native room queries: " + alternative.Reason);

        var unknownRoom = new RoomCapacityState(0, capacity.PlayerMaximum, capacity.EnemyMaximum, false,
            attachments: [new RoomCapacityAttachmentState("unknown", "Unknown", 0, 0)]);
        var mixedRoom = new RoomCapacityState(0, capacity.PlayerMaximum, capacity.EnemyMaximum, false,
            attachments: [capacity.Attachments!.Single(), unknownRoom.Attachments!.Single()]);
        var enchantments = new EnchantmentWorld([], [], 0, [], [], false, false, false, context.BattleRng);
        foreach (CombatContext guarded in new[]
        {
            context.WithRoomCapacities([unknownRoom]), context.WithRoomCapacities([mixedRoom]), context.WithEnchantments(enchantments)
        })
            Require(CardChooseDiscardModel.Apply(guarded, smelt, metall.InstanceId, smeltIndex, before.PlayRules, 0, morsel.InstanceId)
                .UnsupportedReason == Unsupported, "The original unknown room-smelt guard was bypassed.");

        RecordedBattleChecks.Native(battle);
        var (_, _, matched, unsupported) = BattleSearchChecks.RecordedFrontiers(battle, requireZeroUnsupported: true, report: false);
        Console.WriteLine($"GROW-ROOM-SMELT PASS: six native identity queries and preserved room guards; {matched} original card frontiers, {unsupported} unsupported.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
