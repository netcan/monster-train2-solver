using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalPyreshotChecks
{
    private const string CardDataId = "c30c479c-e7aa-4628-9091-dd5d2fb8e5a6";

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = System.Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0) return null;
        if (index == 0)
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == 0);
        if (source.Spawn.Turn < 1 || !source.Spawn.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Enemy)) return null;
        return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == 0);
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalPyreshotPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Pyreshot witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        int[] ids = root.GetProperty("OriginalPyreshotIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 2 && actions.Length == 2, "Missing one protector deployment and one original Pyreshot play.");
        BattleTurnState before = actions[1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = actions[1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = actions[1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[1])!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(candidate => candidate.DataId == CardDataId);
        CardActionEffect effect = rule.Effects.Single();
        CombatUnit pyreBefore = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre);
        CombatUnit pyreAfter = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == pyreBefore.Id);
        int enemiesBefore = before.Spawn.Train.Rooms[0].Units.Count(unit => unit.Team == CombatTeam.Enemy);
        int enemiesAfter = after.Spawn.Train.Rooms[0].Units.Count(unit => unit.Team == CombatTeam.Enemy);

        Require(card.DataId == CardDataId && card.MaskDescriptor!.SourceTraits!.Count == 0 && card.MaskDescriptor.BaseTraits.Count == 0 &&
            rule.AssetKey == "Pyreshot" && rule.CardType == "Spell" && rule.Cost == 3 && rule.CostType == "Default" &&
            rule.ExternalInteractions.Count == 0 && rule.UpgradeInteractions.Count == 0 &&
            effect.Type == "AttackWithUnit" && effect.Target == "Pyre" && effect.UseSelectedRoom && effect.SuppressPyreRoomFocus == true && !effect.RequireEquipment &&
            action.CardInstanceId == ids[1] && action.RoomIndex == 0 && before.Spawn.Turn >= 1 && enemiesBefore == 2 && enemiesAfter == 0 &&
            before.Energy - after.Energy == 3 && before.Spawn.Train.Context.Statistics!.Value(ids[1], "TimesPlayed", "ThisBattle") == 0 &&
            after.Spawn.Train.Context!.Statistics!.Value(ids[1], "TimesPlayed", "ThisBattle") == 1 &&
            after.Spawn.Train.Context.Cards.Discard.Any(token => token.InstanceId == ids[1]) &&
            after.Spawn.Train.Context.FindCard(ids[1])!.LastPlayedCost == 3 && pyreBefore.Health == 80 && pyreAfter.Health == 78,
            "Original Pyreshot identity, selected-floor attack or play lifecycle differs.");

        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-PYRESHOT PASS: unchanged three-cost card attacks all targets on the selected floor with the Pyre; full native replay.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}
