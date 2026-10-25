using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class NaturalAngelPairCacheChecks
{
    internal static void Pair(string originalPath, string replayPath)
    {
        using var original = ModelJson.ReadFixture(originalPath);
        using var replay = ModelJson.ReadFixture(replayPath);
        var source = original.RootElement;
        var fresh = replay.RootElement;
        Require(source.GetProperty("Mismatches").GetInt32() == 3 && source.GetProperty("Unsupported").GetInt32() == 1,
            "Preserve the original three combat mismatches and the rejected empty retention capture.");
        foreach (var fixture in new[] { source, fresh })
        {
            var input = fixture.GetProperty("NativeRunInput");
            Require(input.GetProperty("Seed").GetInt32() == 972508374 && input.GetProperty("Distance").GetInt32() == 5 &&
                input.GetProperty("AscensionLevel").GetInt32() == 0 && !input.GetProperty("WasDevCheatUsed").GetBoolean() &&
                !fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("TerminalEffectsSettled").GetBoolean(),
                "This is the unchanged natural Ring6 defeat, not a controlled or winning battle.");
            CardCycleChecks.Native(fixture);
            TrainCombatChecks.Native(fixture);
        }
        var stage = source.GetProperty("Stages").EnumerateArray().Single(row => row.GetProperty("Index").GetInt32() == 74);
        var before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var old = stage.GetProperty("Predicted").GetProperty("State").Deserialize<RoomCombatState>()!;
        var actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        CombatUnit Boss(RoomCombatState room) => room.Context!.Enchantments!.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 11);
        Require(before.Units.Any(unit => unit.Id == 9 && unit.Health == 102) && Boss(old).BumpRules!.PairedBossUnitId == 9 &&
            Boss(actual).BumpRules!.PairedBossUnitId == 0 && Boss(actual).EndsBattleOnDeath,
            "Missing the native companion death and stale shared-world pair reference.");
        var cycle = source.GetProperty("CardCycles").EnumerateArray().Single(row => row.GetProperty("Index").GetInt32() == 5);
        var cards = cycle.GetProperty("Before").Deserialize<CardCycleState>()!;
        int[] ephemeral = cycle.GetProperty("EphemeralCardIds").Deserialize<int[]>()!;
        Require(cards.Hand.Select(card => card.InstanceId).SequenceEqual(new[] { 30 }) &&
            cards.ExternalInteractions.Contains("CardTraitFreeze discard routing") && ephemeral.SequenceEqual(new[] { 30 }) &&
            cycle.GetProperty("PersistentCardIds").GetArrayLength() == 0 &&
            cycle.GetProperty("Predicted").GetProperty("UnsupportedReason").GetString() == "Persistent discard requires captured hand identities." &&
            CardCycleModel.DiscardHand(cards, ephemeral, Array.Empty<int>()).Supported &&
            !CardCycleModel.DiscardHand(cards, ephemeral).Supported,
            "An explicit empty retention capture must differ from missing captured identities.");
        RoomCombatChecks.NativeRoomOnly(originalPath);
        RoomCombatChecks.NativeRoomOnly(replayPath);
        RollingAdviceChecks.ObservedSuccessors(source);
        RollingAdviceChecks.Native(fresh);
        var first = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        var second = fresh.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(first.Length == 56 && second.Length == first.Length, "Changed frozen natural failure inputs.");
        for (int index = 0; index < first.Length; index++)
        {
            foreach (string field in new[] { "Action", "IsEndTurn" })
                Require(first[index].GetProperty(field).ContentEquals(second[index].GetProperty(field)), "Changed frozen input " + index);
            foreach (string field in new[] { "Root", "Expected", "Actual" })
                Require(BattleTurnChecks.Comparable(first[index].GetProperty(field).Deserialize<BattleTurnState>()!, index == first.Length - 1) ==
                    BattleTurnChecks.Comparable(second[index].GetProperty(field).Deserialize<BattleTurnState>()!, index == first.Length - 1),
                    "Changed native state/RNG at " + index + " " + field);
        }
        Require(fresh.GetProperty("PlayerAdvisor").GetProperty("Overlay").GetProperty("Published").GetInt32() == 0,
            "Frozen repair validation unexpectedly published new advice.");
        Console.WriteLine("NATURAL-ANGEL-PAIR-CACHE PASS: 56 unchanged native inputs; immediate pairing cleanup, explicit empty retention, full room/train/successor/RNG comparisons; repaired four zero counters. No victory claimed.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
