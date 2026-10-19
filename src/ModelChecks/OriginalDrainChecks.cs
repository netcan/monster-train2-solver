using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalDrainChecks
{
    private const string CardId = "6fe6bf99-7e77-4191-9136-fbbecc223bdf";
    private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids) => OriginalDrainScenarioChooser.Choose(source, ids);

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 261 &&
            root.GetProperty("ModifierScenario").GetString() == "original-drain" &&
            root.GetProperty("OriginalDrainPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Drain witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        int[] ids = root.GetProperty("OriginalDrainCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 3 && actions.Length == 2 &&
            actions[0].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[0] &&
            actions[1].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1],
            "The native policy must summon its controlled Train Steward before playing Drain.");

        FixtureValue entry = actions[1];
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction played = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext beforeContext = before.Spawn.Train.Context!;
        CombatContext afterContext = after.Spawn.Train.Context!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == CardId);
        CombatUnit target = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == played.TargetUnitId);
        CombatUnit afterTarget = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == target.Id);
        Require(rule.Cost == 1 && rule.Effects.Select(effect => effect.Type).SequenceEqual(new[] { "AddStatus", "RandomDiscard" }) &&
            rule.Effects[0].Target == "DropTargetCharacter" && rule.Effects[0].Statuses.Single().Id == "debuff" &&
            rule.Effects[0].Statuses.Single().Stacks == 3 && rule.Effects[1].Target == "Hand" && rule.Effects[1].Value == 1 &&
            target.Team == CombatTeam.Player && target.SpawnerCardId == ids[0] &&
            beforeContext.Cards.Hand.Any(item => item.InstanceId == ids[2] && item.DataId == StewardId),
            "Drain must apply three Debuff to the selected Train Steward and random-discard its one controlled candidate.");
        Require(after.Energy == before.Energy - rule.Cost && afterTarget.Status("debuff")?.Stacks == 3 &&
            afterContext.Cards.Discard.Any(item => item.InstanceId == ids[1]) &&
            afterContext.Cards.Discard.Any(item => item.InstanceId == ids[2]) &&
            !afterContext.Cards.Hand.Any(item => item.InstanceId == ids[2]),
            "Drain did not spend one energy, add three Debuff, and discard both the played spell and selected hand card.");

        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-DRAIN PASS: unchanged one-cost play applies three Debuff, then randomly discards its single eligible card.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}

internal static class OriginalDrainScenarioChooser
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
            CombatUnit? target = source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .FirstOrDefault(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0 && unit.SpawnerCardId == ids[0]);
            if (target == null) return null;
            PlayCardAction? play = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
                action.CardInstanceId == ids[1] && action.TargetUnitId == target.Id);
            return play != null && BattleActionModel.PlayCard(source, play).Supported ? play : null;
        }
        return null;
    }
}
