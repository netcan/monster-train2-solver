using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalStarterFrostbiteDiscardChecks
{
    private const string CardId = "6b91f902-deb4-4519-863b-0c8a7f5d871c";
    private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids) => OriginalStarterFrostbiteDiscardScenarioChooser.Choose(source, ids);

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 262 &&
            root.GetProperty("ModifierScenario").GetString() == "original-starter-frostbite-discard" &&
            root.GetProperty("OriginalStarterFrostbiteDiscardPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Starter Frostbite Discard witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        int[] ids = root.GetProperty("OriginalStarterFrostbiteDiscardCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 3 && actions.Length == 2 &&
            actions[0].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[0] &&
            actions[1].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1],
            "The native policy must summon its controlled Train Steward before playing Starter Frostbite Discard.");

        FixtureValue entry = actions[1];
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction played = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext beforeContext = before.Spawn.Train.Context!;
        CombatContext afterContext = after.Spawn.Train.Context!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == CardId);
        var targetRoom = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == played.RoomIndex);
        CombatUnit target = targetRoom.Units.Single(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0);
        CombatUnit afterTarget = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == target.Id);
        Require(rule.Cost == 0 && rule.Effects.Count == 2 && rule.Effects[0].Type == "AddStatus" &&
            rule.Effects[0].Target == "FrontInRoom" && rule.Effects[0].AllowEnemy && !rule.Effects[0].AllowPlayer &&
            rule.Effects[0].Statuses.Single().Id == "poison" && rule.Effects[0].Statuses.Single().Stacks == 8 &&
            rule.Effects[1].Type == "RandomDiscard" && rule.Effects[1].Target == "Hand" && rule.Effects[1].Value == 1 &&
            played.TargetUnitId == 0 && target.Team == CombatTeam.Enemy && target.Health > 0 &&
            beforeContext.Cards.Hand.Any(item => item.InstanceId == ids[2] && item.DataId == StewardId),
            "Starter Frostbite Discard must apply eight Poison to its selected enemy front target and have one controlled discard candidate.");
        Require(after.Energy == before.Energy && afterTarget.Status("poison")?.Stacks == 8 &&
            afterContext.Cards.Discard.Any(item => item.InstanceId == ids[1]) &&
            afterContext.Cards.Discard.Any(item => item.InstanceId == ids[2]) &&
            !afterContext.Cards.Hand.Any(item => item.InstanceId == ids[2]),
            "Starter Frostbite Discard did not add eight Poison without an Ember cost, then discard itself and its single eligible card.");

        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-STARTER-FROSTBITE-DISCARD PASS: unchanged zero-cost play applies eight Poison to an enemy front target, then randomly discards its single eligible card.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}

internal static class OriginalStarterFrostbiteDiscardScenarioChooser
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
