using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalImpPressiveChecks
{
    private const string CardDataId = "adfdfdab-18cf-4f2e-8664-f33dd9251ba4";
    private const string ImpCardDataId = "d76be2d0-f851-4d43-bdc0-7c7a7779724f";
    private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
    private const string ImpSubtype = "SubtypesData_Imp_0f9b989f-15b5-4b16-8378-5d8ed8691e7c";

    internal static int[] CardIds(FixtureValue root) => root.GetProperty("Actions").EnumerateArray()
        .Select(action => action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).ToArray();

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        var context = source.Spawn.Train.Context!;
        if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[0] && play.RoomIndex == 0 && play.PlayerPosition == 0);
        if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0)
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[1] && play.RoomIndex == 0 && play.PlayerPosition == 1);
        if (context.Statistics.Value(ids[2], "TimesPlayed", "ThisBattle") != 0 || source.Spawn.Turn < 1 ||
            !source.Spawn.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Enemy)) return null;
        CombatUnit? imp = source.Spawn.Train.Rooms[0].Units.SingleOrDefault(unit => unit.Team == CombatTeam.Player && unit.Subtypes.Contains(ImpSubtype));
        if (imp == null) return null;
        var action = new PlayCardAction(ids[2], 0, targetUnitId: imp.Id);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalImpPressivePrepared").GetBoolean() && root.GetProperty("ModifierScenario").GetString() == "original-imp-pressive" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original ImpPressive witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        int[] ids = CardIds(root);
        Require(actions.Length == 3 && ids.Length == 3, "Expected one protector, one native Imp and one ImpPressive play.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(Before(0).Spawn.Train.Context!.FindCard(ids[0])!.DataId == StewardId &&
            Before(1).Spawn.Train.Context!.FindCard(ids[1])!.DataId == ImpCardDataId &&
            Before(2).Spawn.Train.Context!.FindCard(ids[2])!.DataId == CardDataId,
            "The native ImpPressive action sequence changed.");

        BattleTurnState before = Before(2), after = After(2);
        PlayCardAction action = actions[2].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit imp = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == action.TargetUnitId);
        CombatUnit? survivingImp = after.Spawn.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == imp.Id && unit.Health > 0);
        CombatUnit[] enemiesBefore = before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
        CombatUnit[] enemiesAfter = after.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
        CombatUnit? frontAfter = enemiesAfter.FirstOrDefault(enemy => enemy.Id == enemiesBefore.FirstOrDefault()?.Id);
        CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[2])!;
        CardSourceTrait trait = card.MaskDescriptor!.SourceTraits!.Single();
        CardPlayRule rule = before.PlayRules!.Cards.Single(candidate => candidate.DataId == CardDataId);
        Require(card.DataId == CardDataId && trait.Type == "CardTraitStrongerMagicPower" && trait.Int == 5 &&
            rule.AssetKey == "ImpPressive" && rule.CardType == "Spell" && rule.Cost == 1 && rule.CostType == "Default" && rule.Effects.Count == 2 &&
            rule.Effects.Select(effect => effect.Type).SequenceEqual(new[] { "Sacrifice", "Damage" }) &&
            rule.Effects[0].Target == "DropTargetCharacter" && rule.Effects[0].TargetSubtype == "SubtypesData_Imp_0f9b989f-15b5-4b16-8378-5d8ed8691e7c" &&
            rule.Effects[1].Target == "FrontInRoom" && rule.Effects[1].Value == 50 &&
            rule.ExternalInteractions.Count == 0 && before.Spawn.Turn >= 1 && enemiesBefore.Length > 0 &&
            action.CardInstanceId == ids[2] && action.RoomIndex == 0 && action.TargetUnitId == imp.Id && imp.Subtypes.Contains("SubtypesData_Imp_0f9b989f-15b5-4b16-8378-5d8ed8691e7c") &&
            before.Energy - after.Energy == 1 && survivingImp == null && (frontAfter == null || frontAfter.Health < enemiesBefore[0].Health ||
                (frontAfter.Status("armor")?.Stacks ?? 0) < (enemiesBefore[0].Status("armor")?.Stacks ?? 0)) &&
            After(1).Spawn.Train.Rooms[0].Units.Any(unit => unit.Subtypes.Contains("SubtypesData_Imp_0f9b989f-15b5-4b16-8378-5d8ed8691e7c")) &&
            after.Spawn.Train.Context!.Statistics!.Value(ids[2], "TimesPlayed", "ThisBattle") == 1 &&
            after.Spawn.Train.Context.Cards.Discard.Any(token => token.InstanceId == ids[2]),
            "ImpPressive did not apply its native strong-magic damage, Imp-only sacrifice, front damage or play lifecycle.");

        BattleActionResult replay = BattleActionModel.PlayCard(before, action);
        Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(after),
            "The recorded ImpPressive action does not replay to the native state: " + replay.Reason);
        Require(BattleActionModel.EnumerateActions(before).SupportedCandidates.Any(candidate => candidate.Action.CardInstanceId == ids[2] &&
            candidate.Action.RoomIndex == 0 && candidate.Action.TargetUnitId == imp.Id), "Action search did not enumerate the native ImpPressive play.");
        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-IMP-PRESSIVE PASS: native Imp-only sacrifice, front-room damage, +5 magic power and complete replay.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new System.InvalidOperationException(message); }
}
