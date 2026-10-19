using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalTitansGratitudeChecks
{
    private const string CardId = "9de23f57-7471-4c57-9b2e-b47f17e5f278";
    private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids) => OriginalTitansGratitudeScenarioChooser.Choose(source, ids);

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 264 &&
            root.GetProperty("ModifierScenario").GetString() == "original-titans-gratitude" &&
            root.GetProperty("OriginalTitansGratitudePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Titan's Gratitude witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        int[] ids = root.GetProperty("OriginalTitansGratitudeCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 3 && actions.Length == 2 &&
            actions[0].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[0] &&
            actions[1].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1],
            "The native policy must summon its controlled Train Steward before playing Titan's Gratitude.");

        FixtureValue entry = actions[1];
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction played = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext beforeContext = before.Spawn.Train.Context!;
        CombatContext afterContext = after.Spawn.Train.Context!;
        var beforeRoom = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == played.RoomIndex);
        var afterRoom = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == played.RoomIndex);
        CombatUnit target = beforeRoom.Units.Single(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0);
        CombatUnit? afterTarget = afterRoom.Units.SingleOrDefault(unit => unit.Id == target.Id);
        CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == CardId);
        CardTraitValue trait = beforeContext.FindCard(ids[1])!.MaskDescriptor!.BaseTraits.Single();
        Require(rule.Cost == 1 && rule.Effects.Count == 2 && rule.Effects[0].Type == "Damage" &&
            rule.Effects[0].Target == "FrontInRoom" && rule.Effects[0].AllowEnemy && !rule.Effects[0].AllowPlayer &&
            rule.Effects[0].Value == 60 && rule.Effects[1].Type == "RandomDiscard" &&
            rule.Effects[1].Target == "Hand" && rule.Effects[1].Value == 1 && played.TargetUnitId == 0 &&
            target.Health > 0 && beforeContext.Cards.Hand.Any(item => item.InstanceId == ids[2] && item.DataId == StewardId) &&
            trait.RuntimeType == "CardTraitStrongerMagicPower" && trait.DeclaredName == trait.RuntimeType &&
            trait.DefinitionParamInt == 5 && trait.ParamInt == 5,
            "Titan's Gratitude must retain its original five-parameter stronger-magic trait, hit one enemy front target and have one eligible discard card.");
        Require(before.Energy - after.Energy == 1 && target.Health - (afterTarget?.Health ?? 0) == System.Math.Min(rule.Effects[0].Value, target.Health) &&
            afterContext.Cards.Discard.Any(item => item.InstanceId == ids[1]) &&
            afterContext.Cards.Discard.Any(item => item.InstanceId == ids[2]) &&
            !afterContext.Cards.Hand.Any(item => item.InstanceId == ids[2]),
            "Titan's Gratitude did not spend one Ember, apply its base-60 damage up to target health, and discard its single controlled hand candidate.");

        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-TITANS-GRATITUDE PASS: unchanged one-cost base-60 damage effect hits one enemy front target with original stronger-magic metadata, then randomly discards its single eligible card; observed hit is health-capped.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}

internal static class OriginalTitansGratitudeScenarioChooser
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
