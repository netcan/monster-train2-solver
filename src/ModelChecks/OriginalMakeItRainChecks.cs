using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalMakeItRainChecks
{
    private const string CardDataId = "8a6ebb24-d5c1-430f-9332-1d3f43f47121";
    private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";

    internal static int[] CardIds(FixtureValue root) => root.GetProperty("Actions").EnumerateArray()
        .Select(action => action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).ToArray();

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        for (int index = 0; index < ids.Length; index++)
        {
            if (context.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") != 0 ||
                !context.Cards.Hand.Any(card => card.InstanceId == ids[index])) continue;
            int room = index == 1 ? 1 : 0;
            if (index == 2 && (source.Spawn.Turn < 1 || !source.Spawn.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Enemy))) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == room &&
                (index < 2 ? play.PlayerPosition == 0 : true));
        }
        return null;
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalMakeItRainPrepared").GetBoolean() &&
            root.GetProperty("ModifierScenario").GetString() == "original-make-it-rain" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Missing completed original Make It Rain witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        int[] ids = CardIds(root);
        Require(actions.Length == 3 && ids.Length == 3, "Expected two floor units and one Make It Rain play.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(Before(0).Spawn.Train.Context!.FindCard(ids[0])!.DataId == StewardId &&
            Before(1).Spawn.Train.Context!.FindCard(ids[1])!.DataId == StewardId &&
            Before(2).Spawn.Train.Context!.FindCard(ids[2])!.DataId == CardDataId,
            "The controlled action sequence did not deploy two Stewards before Make It Rain.");

        BattleTurnState before = Before(2), after = After(2);
        PlayCardAction action = actions[2].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit[] targetUnitsBefore = before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
        CombatUnit[] targetUnitsAfter = after.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
        CombatUnit controlBefore = before.Spawn.Train.Rooms[1].Units.Single(unit => unit.Team == CombatTeam.Player && !unit.IsPyre);
        CombatUnit controlAfter = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == controlBefore.Id);
        CombatUnit[] enemiesBefore = before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
        CombatUnit[] enemiesAfter = after.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
        CombatUnit? frontAfter = enemiesAfter.FirstOrDefault(enemy => enemy.Id == enemiesBefore.FirstOrDefault()?.Id);
        CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[2])!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(candidate => candidate.DataId == CardDataId);
        CardActionEffect[] damages = rule.Effects.Where(effect => effect.Type == "Damage").ToArray();
        CardActionEffect nullEffect = rule.Effects.Single(effect => effect.Type == "Null");
        CardActionEffect[] goldCosts = rule.Effects.Where(effect => effect.Type == "RewardGold").ToArray();
        Require(card.DataId == CardDataId && card.MaskDescriptor!.SourceTraits is { Count: 0 } &&
            rule.AssetKey == "MakeItRain" && rule.CardType == "Spell" && rule.Cost == 1 && rule.ExternalInteractions.Count == 0 &&
            rule.Effects.Select(effect => effect.Type).SequenceEqual(new[] { "Null", "Damage", "RewardGold", "Damage", "RewardGold", "Damage", "RewardGold", "Damage" }) &&
            damages.Length == 4 && damages.All(effect => effect.Target == "Room" && effect.Value == 5 && effect.AllowEnemy && !effect.AllowPlayer) &&
            goldCosts.Length == 3 && goldCosts.All(effect => effect.Target == "Room" && effect.Value == -5 && effect.GoldCostRequired == true) &&
            nullEffect.Target == "Room" && nullEffect.Value == 3 && !nullEffect.NullForwardSelf && !nullEffect.NullTargetless &&
            before.Spawn.Turn >= 1 && enemiesBefore.Length > 0 && action.CardInstanceId == ids[2] && action.RoomIndex == 0 && action.TargetUnitId == 0 &&
            (frontAfter == null || frontAfter.Health < enemiesBefore[0].Health ||
                (frontAfter.Status("armor")?.Stacks ?? 0) < (enemiesBefore[0].Status("armor")?.Stacks ?? 0)) &&
            after.Spawn.Train.Context!.Gold == before.Spawn.Train.Context.Gold - 15 &&
            before.Energy - after.Energy == rule.Cost &&
            targetUnitsBefore.Length == targetUnitsAfter.Length && targetUnitsBefore.All(unit => targetUnitsAfter.Any(next => next.Id == unit.Id && next.Attack == unit.Attack)) &&
            controlAfter.Attack == controlBefore.Attack && controlAfter.Health == controlBefore.Health &&
            after.Spawn.Train.Context.Statistics!.Value(ids[2], "TimesPlayed", "ThisBattle") == 1 &&
            after.Spawn.Train.Context.Cards.Discard.Any(item => item.InstanceId == ids[2]),
            "Make It Rain did not resolve native damage, NULL forwarding, exact gold reward, floor isolation or discard routing.");

        BattleActionResult replay = BattleActionModel.PlayCard(before, action);
        Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(after),
            "The recorded Make It Rain action does not replay to the native state: " + replay.Reason);
        Require(BattleActionModel.EnumerateActions(before).SupportedCandidates.Any(candidate => candidate.Action.CardInstanceId == ids[2] &&
            candidate.Action.RoomIndex == 0), "Action search did not enumerate the native Make It Rain play.");
        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-MAKE-IT-RAIN PASS: four 5-damage room hits, three required 5-Gold payments, null effect and complete replay.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new System.InvalidOperationException(message); }
}
