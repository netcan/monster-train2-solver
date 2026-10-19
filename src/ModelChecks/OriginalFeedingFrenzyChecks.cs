using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalFeedingFrenzyChecks
{
    private const string CardId = "9ed18c87-630e-4f91-a4b6-e6126e51c3b4";
    private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids) => OriginalFeedingFrenzyScenarioChooser.Choose(source, ids);

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 263 &&
            root.GetProperty("ModifierScenario").GetString() == "original-feeding-frenzy" &&
            root.GetProperty("OriginalFeedingFrenzyPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Feeding Frenzy witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        int[] ids = root.GetProperty("OriginalFeedingFrenzyCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 3 && actions.Length == 2 &&
            actions[0].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[0] &&
            actions[1].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1],
            "The native policy must summon its controlled Train Steward before playing Feeding Frenzy.");

        FixtureValue entry = actions[1];
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction played = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext beforeContext = before.Spawn.Train.Context!;
        CombatContext afterContext = after.Spawn.Train.Context!;
        var beforeRoom = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == played.RoomIndex);
        var afterRoom = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == played.RoomIndex);
        CombatUnit target = beforeRoom.Units.Single(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0);
        CombatUnit afterTarget = afterRoom.Units.Single(unit => unit.Id == target.Id);
        CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == CardId);
        Require(rule.Cost == 1 && rule.Effects.Count == 2 && rule.Effects[0].Type == "AddStatus" &&
            rule.Effects[0].Target == "Room" && rule.Effects[0].AllowEnemy && !rule.Effects[0].AllowPlayer &&
            rule.Effects[0].Value == 100 && rule.Effects[0].Statuses.Single().Id == "dazed" && rule.Effects[0].Statuses.Single().Stacks == 1 &&
            rule.Effects[1].Type == "DiscardHand" && rule.Effects[1].Target == "Room" && target.Health > 0 &&
            beforeContext.Cards.Hand.Any(item => item.InstanceId == ids[2] && item.DataId == StewardId),
            "Feeding Frenzy must apply one Dazed to its single enemy room target, then discard the one controlled card in hand.");
        Require(after.Energy == before.Energy - rule.Cost && afterTarget.Status("dazed")?.Stacks == 1 &&
            afterContext.Cards.Discard.Any(item => item.InstanceId == ids[1]) &&
            afterContext.Cards.Discard.Any(item => item.InstanceId == ids[2]) &&
            !afterContext.Cards.Hand.Any(item => item.InstanceId == ids[2]),
            "Feeding Frenzy did not spend one Ember, add one Dazed and discard the controlled hand card.");

        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-FEEDING-FRENZY PASS: unchanged one-cost play adds Dazed to the single enemy in its room, then discards its controlled hand card.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}

internal static class OriginalFeedingFrenzyScenarioChooser
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
        {
            var summon = new PlayCardAction(ids[0], 0, playerPosition: 0);
            if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
        }
        if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0 &&
            context.Cards.Hand.Any(item => item.InstanceId == ids[1]) && context.Cards.Hand.Any(item => item.InstanceId == ids[2]))
        {
            PlayCardAction? play = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
                action.CardInstanceId == ids[1] && source.Spawn.Train.Rooms.Any(room => room.RoomIndex == action.RoomIndex &&
                    room.Units.Count(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0) == 1));
            return play != null && BattleActionModel.PlayCard(source, play).Supported ? play : null;
        }
        return null;
    }
}
