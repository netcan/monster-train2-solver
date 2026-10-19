using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalPerfectInsanityChecks
{
    private const string CardId = "9d04efb5-8794-42a8-8282-e8ef895e6c3a";
    private const string UpgradeId = "10c215e1-0317-4e83-b551-d601b7858219";
    private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids) => OriginalPerfectInsanityScenarioChooser.Choose(source, ids);

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 265 &&
            root.GetProperty("ModifierScenario").GetString() == "original-perfect-insanity" &&
            root.GetProperty("OriginalPerfectInsanityPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Perfect Insanity witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        int[] ids = root.GetProperty("OriginalPerfectInsanityCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 2 && actions.Length == 2 &&
            actions[0].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[0] &&
            actions[1].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1],
            "The native policy must summon its controlled Train Steward before playing Perfect Insanity.");

        FixtureValue entry = actions[1];
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction played = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext beforeContext = before.Spawn.Train.Context!;
        CombatContext afterContext = after.Spawn.Train.Context!;
        CombatUnit target = before.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.Id == played.TargetUnitId);
        CombatUnit afterTarget = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == target.Id);
        CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == CardId);
        CardTraitValue trait = beforeContext.FindCard(ids[1])!.MaskDescriptor!.BaseTraits.Single();
        RoomCapacityState beforeRoom = beforeContext.RoomCapacities!.Single(room => room.RoomIndex == played.RoomIndex);
        RoomCapacityState afterRoom = afterContext.RoomCapacities!.Single(room => room.RoomIndex == played.RoomIndex);
        Require(rule.Cost == 1 && rule.Effects.Count == 1 && rule.Effects[0].Type == "UnitUpgrade" &&
            rule.Effects[0].Target == "DropTargetCharacter" && rule.Effects[0].AllowEnemy && rule.Effects[0].AllowPlayer &&
            rule.Effects[0].Value == 1 && rule.Effects[0].Lifetime == "TemporaryUntilEndOfBattle" &&
            rule.Effects[0].Upgrade!.DataId == UpgradeId && rule.Effects[0].Upgrade.Stats.Health == -666 &&
            target.Team == CombatTeam.Player && target.SpawnerCardId == ids[0] &&
            beforeRoom.Corruption == 6 && trait.RuntimeType == "CardTraitCorruptRestricted" &&
            trait.DeclaredName == trait.RuntimeType && trait.DefinitionParamInt == 6 && trait.ParamInt == 6 &&
            beforeContext.Cards.Hand.Any(item => item.InstanceId == ids[1]),
            "Perfect Insanity must retain Corrupt Restricted(6), target the controlled Train Steward and carry its temporary Minus666Health upgrade.");
        Require(before.Energy - after.Energy == 1 && afterRoom.Corruption == 0 &&
            target.MaxHealth - afterTarget.MaxHealth == 666 && target.Health - afterTarget.Health == 666 && afterTarget.Health > 0 &&
            afterContext.Cards.Discard.Any(item => item.InstanceId == ids[1]),
            "Perfect Insanity did not consume six corruption, spend one Ember, apply the 666-health reduction and discard itself.");

        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-PERFECT-INSANITY PASS: Corrupt Restricted(6) consumes selected-room corruption, then the original temporary Minus666Health upgrade reduces its durable friendly target by 666.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}

internal static class OriginalPerfectInsanityScenarioChooser
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
                .FirstOrDefault(unit => unit.Team == CombatTeam.Player && unit.Health > 0 && unit.SpawnerCardId == ids[0]);
            if (target == null) return null;
            PlayCardAction? play = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
                action.CardInstanceId == ids[1] && action.RoomIndex == 0 && action.TargetUnitId == target.Id);
            return play != null && BattleActionModel.PlayCard(source, play).Supported ? play : null;
        }
        return null;
    }
}
