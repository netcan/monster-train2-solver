using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalMindsOfTheirOwnChecks
{
    private const string CardId = "809b4e83-cbd6-4b59-a263-d0ad76252066";
    private const string FortifyId = "1dd23b59-fa5e-44bc-83ca-de119ede4f21";
    private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";

    internal static int[] CardIds(FixtureValue root) => root.GetProperty("Actions").EnumerateArray()
        .Select(action => action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).ToArray();

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids) => OriginalMindsOfTheirOwnScenarioChooser.Choose(source, ids);

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 103 && root.GetProperty("ModifierScenario").GetString() == "original-minds-of-their-own" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Missing completed original Minds of Their Own witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        int[] ids = CardIds(root);
        Require(actions.Length == 3 && ids.Length == 3, "Expected one Train Steward deployment, Fortify equip and Minds of Their Own play.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;

        CardInstanceState hostCard = Before(0).Spawn.Train.Context!.FindCard(ids[0])!;
        CardInstanceState fortifyCard = Before(1).Spawn.Train.Context!.FindCard(ids[1])!;
        CardInstanceState mindsCard = Before(2).Spawn.Train.Context!.FindCard(ids[2])!;
        CardPlayRule fortifyRule = Before(1).PlayRules!.Cards.Single(card => card.DataId == FortifyId);
        CardPlayRule mindsRule = Before(2).PlayRules!.Cards.Single(card => card.DataId == CardId);
        CardActionEffect mindsEffect = mindsRule.Effects.Single();
        CombatUnit equippedAfter = After(1).Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]);
        BattleTurnState before = Before(2);
        BattleTurnState after = After(2);
        CombatUnit hostBefore = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]);
        CombatUnit hostAfter = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == hostBefore.Id);
        int EnemyVitality(TrainCombatState state) => state.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy)
            .Sum(unit => unit.Health + (unit.Status("armor")?.Stacks ?? 0));

        PlayCardAction deploy = actions[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        PlayCardAction equip = actions[1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        PlayCardAction cast = actions[2].GetProperty("Action").Deserialize<PlayCardAction>()!;
        int enemiesBefore = before.Spawn.Train.Rooms[0].Units.Count(unit => unit.Team == CombatTeam.Enemy);
        int damage = EnemyVitality(before.Spawn.Train) - EnemyVitality(after.Spawn.Train);

        Require(hostCard.DataId == StewardId && deploy.CardInstanceId == ids[0] && deploy.PlayerPosition == 0 &&
            fortifyCard.DataId == FortifyId && fortifyRule.CardType == "Equipment" && fortifyRule.Cost == 0 &&
            fortifyRule.Effects.Single().Type == "AttachEquipment" && fortifyRule.Equipment != null &&
            equip.CardInstanceId == ids[1] && equip.TargetUnitId == equippedAfter.Id && equippedAfter.EquipmentCards?.Contains(ids[1]) == true &&
            mindsCard.DataId == CardId && mindsRule.AssetKey == "MindsOfTheirOwn" && mindsRule.CardType == "Spell" && mindsRule.Cost == 3 &&
            mindsCard.MaskDescriptor!.SourceTraits!.Count == 0 && mindsCard.MaskDescriptor.BaseTraits.Count == 0 &&
            mindsRule.ExternalInteractions.Count == 0 && mindsRule.UpgradeInteractions.Count == 0 && mindsRule.HandDiscardInteractions.Count == 0 &&
            mindsEffect.Type == "AttackWithUnit" && mindsEffect.Target == "Room" && mindsEffect.RequireEquipment && !mindsEffect.UseSelectedRoom &&
            cast.CardInstanceId == ids[2] && cast.RoomIndex == 0 && before.Spawn.Turn >= 1 && enemiesBefore > 0 &&
            hostBefore.EquipmentCards?.Contains(ids[1]) == true && hostBefore.BaseAttack == 20 && hostBefore.Attacks == 1 &&
            before.Energy - after.Energy == 3 && damage == hostBefore.Attack * hostBefore.Attacks && hostAfter.Id == hostBefore.Id &&
            after.Spawn.Train.Context!.Statistics!.Value(ids[2], "TimesPlayed", "ThisBattle") == 1 &&
            after.Spawn.Train.Context.Cards.Discard.Any(card => card.InstanceId == ids[2]),
            "Original Minds of Their Own identity, equipment gate, attack result, cost or play lifecycle differs.");

        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-MINDS-OF-THEIR-OWN PASS: unchanged 3-cost spell executes one equipped unit's attack on its current floor; full native replay.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new System.InvalidOperationException(message); }
}

internal static class OriginalMindsOfTheirOwnScenarioChooser
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
        {
            var summon = new PlayCardAction(ids[0], 0, playerPosition: 0);
            if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
        }

        CombatUnit? host = source.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units
            .SingleOrDefault(unit => unit.SpawnerCardId == ids[0]);
        if (host == null) return null;
        if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0 && context.Cards.Hand.Any(card => card.InstanceId == ids[1]))
        {
            var equip = new PlayCardAction(ids[1], 0, targetUnitId: host.Id);
            if (BattleActionModel.PlayCard(source, equip).Supported) return equip;
        }

        if (host.EquipmentCards?.Contains(ids[1]) == true && source.Spawn.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Enemy) &&
            source.Spawn.Turn >= 1 && context.Statistics.Value(ids[2], "TimesPlayed", "ThisBattle") == 0 && context.Cards.Hand.Any(card => card.InstanceId == ids[2]))
        {
            var cast = new PlayCardAction(ids[2], 0);
            if (BattleActionModel.PlayCard(source, cast).Supported) return cast;
        }
        return null;
    }
}
