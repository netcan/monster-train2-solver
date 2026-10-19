using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalSmeltedTreasureChecks
{
    private const string CardId = "a1049085-eba0-4118-9546-ca20d680c2b8";
    private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids) => OriginalSmeltedTreasureScenarioChooser.Choose(source, ids);

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 258 &&
            root.GetProperty("ModifierScenario").GetString() == "original-smelted-treasure" &&
            root.GetProperty("OriginalSmeltedTreasurePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Smelted Treasure witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        int[] ids = root.GetProperty("OriginalSmeltedTreasureCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 2 && actions.Length == 2 &&
            actions[0].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[0] &&
            actions[1].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1],
            "The native policy must summon its controlled Train Steward before playing Smelted Treasure.");

        FixtureValue entry = actions[1];
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction played = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext beforeContext = before.Spawn.Train.Context!;
        CombatContext afterContext = after.Spawn.Train.Context!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == CardId);
        CombatUnit target = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == played.TargetUnitId);
        CombatUnit afterTarget = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == target.Id);
        Require(rule.Cost == 1 && rule.Effects.Count == 2 && rule.Effects[0].Type == "RewardGold" &&
            rule.Effects[0].Target == "Room" && rule.Effects[0].Value == -50 && rule.Effects[0].GoldCostRequired == true &&
            rule.Effects[1].Type == "AddStatus" && rule.Effects[1].Target == "DropTargetCharacter" &&
            !rule.Effects[1].AllowEnemy && rule.Effects[1].AllowPlayer &&
            rule.Effects[1].Statuses.Single().Id == "armor" && rule.Effects[1].Statuses.Single().Stacks == 50 &&
            target.Team == CombatTeam.Player && target.SpawnerCardId == ids[0] && beforeContext.Gold == 100,
            "Smelted Treasure must spend 50 gold before applying 50 Armor to the selected friendly Train Steward.");
        Require(afterContext.Gold == beforeContext.Gold - 50 && after.Energy == before.Energy - rule.Cost &&
            afterTarget.Status("armor")?.Stacks == 50 && afterContext.Cards.Discard.Any(item => item.InstanceId == ids[1]),
            "Smelted Treasure did not debit 50 gold, add 50 Armor, spend one energy and discard itself.");

        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-SMELTED-TREASURE PASS: unchanged one-cost play spends 50 gold and gives the selected friendly unit 50 Armor.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}

internal static class OriginalSmeltedTreasureScenarioChooser
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
        {
            var summon = new PlayCardAction(ids[0], 0, playerPosition: 0);
            if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
        }
        if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0)
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
