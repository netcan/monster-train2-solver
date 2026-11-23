using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RandomSpellTargetsChecks
{
    internal static void Native(FixtureValue root)
    {
        const string ownerId = "a83bc048-7924-4beb-87bd-00419bda6be0";
        const string ownerKey = "AllSpellsRandomTargets";
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("Boundary").GetString() == "OriginalRandomSpellTargets" &&
            root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            root.GetProperty("OwnerId").GetString() == ownerId && root.GetProperty("OwnerKey").GetString() == ownerKey &&
            root.GetProperty("CallbackRngBefore").ContentEquals(root.GetProperty("CallbackRngAfter")),
            "Expected the original mutator and an RNG-neutral hand callback.");

        var expected = new[]
        {
            ("StarterTorch", "DropTargetCharacter", "RandomInRoom"),
            ("StarterIceSpear", "FrontInRoom", "RandomInRoom"),
            ("StarterReform", "Exhaust", "Exhaust")
        };
        FixtureValue[] cases = root.GetProperty("Cases").EnumerateArray().ToArray();
        Require(cases.Length == expected.Length, "Native hand callback controls are incomplete.");
        for (int i = 0; i < cases.Length; i++)
        {
            FixtureValue row = cases[i];
            var (card, prior, next) = expected[i];
            Require(row.GetProperty("AssetKey").GetString() == card &&
                row.GetProperty("Before").EnumerateArray().Single().GetString() == prior &&
                row.GetProperty("After").EnumerateArray().Single().GetString() == next &&
                row.GetProperty("Targetless").GetBoolean() == (prior != next || row.GetProperty("WasTargetless").GetBoolean()),
                "Unexpected native target rewrite on " + card + ".");
        }

        FixtureValue cast = root.GetProperty("Cast");
        BattleTurnState before = cast.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = cast.GetProperty("After").Deserialize<BattleTurnState>()!;
        PlayCardAction action = cast.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext context = before.Spawn.Train.Context!;
        CombatRelicState owner = context.Relics!.Single(item => item.DataId == ownerId);
        Require(owner.AssetKey == ownerKey && owner.EffectTypes.SequenceEqual(["RelicEffectModifyTargetsRandom", "RelicEffectDisableUndo"]) &&
            action.TargetUnitId == 0 && before.Energy - after.Energy == 1,
            "Native cast should use the original targetless input and spend one Ember.");

        int[] targets = cast.GetProperty("Candidates").EnumerateArray().Select(value => value.GetInt32()).ToArray();
        int hit = cast.GetProperty("ActualTargets").EnumerateArray().Single().GetInt32();
        RngDraw draw = context.BattleRng.Range(0, targets.Length);
        Require(targets.Length == 2 && targets[draw.Value] == hit && draw.State.Equals(after.Spawn.Train.Context!.BattleRng),
            "The single native Battle RNG step should pick the recorded target from the ordered room candidates.");

        using var keys = new BattleStateKeys();
        string parent = keys.SnapshotKey(before);
        BattleActionResult result = BattleActionModel.PlayCard(before, action);
        Require(result.State != null && result.Outcome == RoomOutcome.Exchanged, "Targetless cast failed: " + result.Reason);
        string? mismatch = ModelJson.Difference(BattleTurnChecks.Comparable(result.State!), BattleTurnChecks.Comparable(after));
        Require(mismatch == null, "Native random spell successor differs: " + mismatch);
        PlayCardAction[] roomActions = BattleActionModel.EnumerateActions(before, action.CardInstanceId, false)
            .SupportedActions.Where(candidate => candidate.RoomIndex == action.RoomIndex).ToArray();
        Require(roomActions is [PlayCardAction candidate] && candidate.TargetUnitId == 0 && keys.SnapshotKey(before) == parent,
            "The legal-action frontier should offer one targetless cast in that room and preserve its parent.");
        Console.WriteLine("NATIVE-RANDOM-SPELL-TARGETS PASS: three hand callback controls, two native targets, one Battle RNG step, complete successor and targetless action frontier.");
    }

    internal static void JustCause(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("Boundary").GetString() == "OriginalRandomJustCause" &&
            root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module, "Expected pinned native Just Cause.");
        BattleTurnState before = root.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = root.GetProperty("After").Deserialize<BattleTurnState>()!;
        PlayCardAction action = root.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext context = before.Spawn.Train.Context!;
        CardInstanceState card = context.FindCard(action.CardInstanceId)!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(entry => entry.DataId == card.DataId);
        int target = root.GetProperty("MoveTarget").GetInt32();
        int[] candidates = root.GetProperty("Candidates").EnumerateArray().Select(entry => entry.GetInt32()).ToArray();
        Require(rule.AssetKey == "JustCause" && action.TargetUnitId == 0 && card.MaskDescriptor?.RandomTargetsFromRelic == true &&
            target == root.GetProperty("StatusTarget").GetInt32() && candidates.Length >= 2 && candidates.Contains(target),
            "Expected a targetless Just Cause with one shared movement and Valor target among several actors.");
        RoomCombatState oldRoom = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
        RoomCombatState newRoom = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
        CombatUnit original = oldRoom.Units.Single(unit => unit.Id == target);
        CombatUnit moved = newRoom.Units.Single(unit => unit.Id == target);
        Require(!oldRoom.Units.Select(unit => unit.Id).SequenceEqual(newRoom.Units.Select(unit => unit.Id)) &&
            (moved.Status("valor")?.Stacks ?? 0) > (original.Status("valor")?.Stacks ?? 0), "Native did not reorder the unit and add Valor to it.");

        using var keys = new BattleStateKeys();
        string parent = keys.SnapshotKey(before);
        BattleActionResult result = BattleActionModel.PlayCard(before, action);
        Require(result.State != null && result.Outcome == RoomOutcome.Exchanged, "Native Just Cause replay failed: " + result.Reason);
        string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(result.State!), BattleTurnChecks.Comparable(after));
        Require(difference == null, "Native Just Cause successor differs: " + difference);
        Require(BattleActionModel.EnumerateActions(before, action.CardInstanceId, false).SupportedActions.Any(candidate =>
            candidate.RoomIndex == action.RoomIndex && candidate.TargetUnitId == 0) && keys.SnapshotKey(before) == parent,
            "Just Cause frontier lost the targetless native action or modified the root.");
        Console.WriteLine("NATIVE-RANDOM-JUST-CAUSE PASS: same target moved and received Valor, physical order, complete state and RNG.");
    }

    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidDataException(message);
    }
}
