using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalCopyMorselTwiceChecks
{
    private const string CardId = "e4c1814d-3c45-47aa-afdd-cb3fbe8e821d";
    private const string MorselCardId = "536e6de0-7ccf-412a-9a4e-ec55ab3988e5";
    private const string SnackSubtype = "SubtypesData_Snack";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 250 &&
            root.GetProperty("ModifierScenario").GetString() == "original-copy-morsel-twice" &&
            root.GetProperty("OriginalCopyMorselTwicePrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Missing completed original Copy Morsel Twice witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        FixtureValue[] copyActions = actions.Where(Plays).ToArray();
        Require(copyActions.Length > 0, "No native Copy Morsel Twice play was recorded.");
        BattleTurnState firstBefore = copyActions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        CardPlayRule copyRule = firstBefore.PlayRules!.Cards.Single(card => card.DataId == CardId);
        CardPlayRule morselRule = firstBefore.PlayRules.Cards.Single(card => card.DataId == MorselCardId);

        Require(copyRule.AssetKey == "CopyMorselTwice" && copyRule.Cost == 1 && copyRule.Effect == "Spell" &&
            copyRule.Effects.Count == 1 && copyRule.Effects[0].Type == "CopyUnits" &&
            copyRule.Effects[0].Target == "DropTargetCharacter" && copyRule.Effects[0].AllowPlayer &&
            !copyRule.Effects[0].AllowEnemy && copyRule.Effects[0].Filters?.Subtype == SnackSubtype &&
            copyRule.Effects[0].Value == 3 && copyRule.Effects[0].CopyHeroStats == false &&
            copyRule.ExternalInteractions.Count == 0 && copyRule.UpgradeInteractions!.Count == 0,
            "Original Copy Morsel Twice identity, target filter, copy count or interaction guards differ.");
        Require(morselRule.Cost == 0 && morselRule.SpawnUnit?.Subtypes.Contains(SnackSubtype) == true,
            "Morsel Miner is not a zero-cost Snack unit.");
        foreach (FixtureValue action in copyActions) VerifyPlay(action, copyRule, morselRule);

        RecordedBattleChecks.Native(root);
        Console.WriteLine($"ORIGINAL-COPY-MORSEL-TWICE PASS: {copyActions.Length} native plays copy the selected Snack up to three times at physical spawn points; {actions.Length} actions replay.");
    }

    private static void VerifyPlay(FixtureValue action, CardPlayRule copyRule, CardPlayRule morselRule)
    {
        BattleTurnState before = action.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = action.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction play = action.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext beforeContext = before.Spawn.Train.Context!;
        CombatContext afterContext = after.Spawn.Train.Context!;
        CardInstanceState spell = beforeContext.FindCard(play.CardInstanceId)!;
        CombatUnit[] beforeMorsels = before.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Where(unit => unit.Team == CombatTeam.Player && unit.AssetKey == morselRule.SpawnUnit!.AssetKey).ToArray();
        CombatUnit[] afterMorsels = after.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Where(unit => unit.Team == CombatTeam.Player && unit.AssetKey == morselRule.SpawnUnit!.AssetKey).ToArray();
        CombatUnit? source = beforeMorsels.SingleOrDefault(unit => unit.Id == play.TargetUnitId);
        Require(source != null && source.Subtypes.Contains(SnackSubtype) &&
            beforeContext.FindCard(source.SpawnerCardId)?.DataId == MorselCardId,
            "Copy target is not a living Morsel Miner Snack source.");
        SpawnPointGroupState group = beforeContext.SpawnPoints!.Group(play.RoomIndex, CombatTeam.Player)!;
        int freePoints = group.Occupants.Take(group.GroupCount).Count(id => id == 0);
        int expectedBirths = Math.Min(copyRule.Effects[0].Value, freePoints);
        HashSet<int> beforeIds = beforeMorsels.Select(unit => unit.Id).ToHashSet();
        CombatUnit[] copies = afterMorsels.Where(unit => !beforeIds.Contains(unit.Id)).ToArray();
        UnitSpawnPointState[] copyPoints = copies.Select(unit => afterContext.SpawnPoints!.Units
            .Single(point => point.UnitId == unit.Id)).ToArray();
        int playsBefore = beforeContext.Statistics!.Value(spell.InstanceId, "TimesPlayed", "ThisBattle");

        Require(before.Energy - after.Energy == 1 &&
            !afterContext.Cards.Hand.Any(card => card.InstanceId == spell.InstanceId) &&
            afterContext.Statistics!.Value(spell.InstanceId, "TimesPlayed", "ThisBattle") == playsBefore + 1,
            "Original Copy Morsel Twice target, one-Ember payment or play accounting differs.");
        Require(copies.Length == expectedBirths && afterMorsels.Length == beforeMorsels.Length + expectedBirths &&
            afterContext.NextCardId == beforeContext.NextCardId + copyRule.Effects[0].Value &&
            afterContext.NextUnitId == beforeContext.NextUnitId + expectedBirths &&
            copies.All(unit => unit.BaseAttack == source!.BaseAttack && unit.Health == source.Health &&
                unit.MaxHealth == source.MaxHealth && unit.Size == source.Size) &&
            copyPoints.All(point => point.Current != null && point.Current.RoomIndex == play.RoomIndex),
            $"Native copy count, detached card allocations or copied stats/physical placement differs: expected={expectedBirths}, copies={copies.Length}, free={freePoints}.");
    }

    private static bool Plays(FixtureValue action)
    {
        int instanceId = action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
        return action.GetProperty("Before").GetProperty("Spawn").GetProperty("Train").GetProperty("Context")
            .GetProperty("CardRegistry").EnumerateArray().Any(card =>
                card.GetProperty("InstanceId").GetInt32() == instanceId && card.GetProperty("DataId").GetString() == CardId);
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
