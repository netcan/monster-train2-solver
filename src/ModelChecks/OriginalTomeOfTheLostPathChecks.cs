using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalTomeOfTheLostPathChecks
{
    private const string CardDataId = "e79f31b0-749a-4655-8af8-1fa226974edc";
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
            int target = index == 2 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.Team == CombatTeam.Player && !unit.IsPyre).Id : 0;
            var action = new PlayCardAction(ids[index], room, targetUnitId: target, playerPosition: index < 2 ? 0 : -1);
            if (BattleActionModel.PlayCard(source, action).Supported) return action;
        }
        return null;
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalTomeOfTheLostPathPrepared").GetBoolean() &&
            root.GetProperty("ModifierScenario").GetString() == "original-tome-of-the-lost-path" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Missing completed original Tome of the Lost Path witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        int[] ids = CardIds(root);
        Require(actions.Length == 3 && ids.Length == 3, "Expected two floor units and one Tome of the Lost Path play.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(Before(0).Spawn.Train.Context!.FindCard(ids[0])!.DataId == StewardId &&
            Before(1).Spawn.Train.Context!.FindCard(ids[1])!.DataId == StewardId &&
            Before(2).Spawn.Train.Context!.FindCard(ids[2])!.DataId == CardDataId,
            "The controlled action sequence did not deploy two Stewards before the native Tome.");

        BattleTurnState before = Before(2), after = After(2);
        PlayCardAction action = actions[2].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit targetBefore = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == action.TargetUnitId);
        CombatUnit targetAfter = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == targetBefore.Id);
        CombatUnit controlBefore = before.Spawn.Train.Rooms[1].Units.Single(unit => unit.Team == CombatTeam.Player && !unit.IsPyre);
        CombatUnit controlAfter = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == controlBefore.Id);
        CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[2])!;
        CardSourceTrait exhaust = card.MaskDescriptor!.SourceTraits!.Single();
        CardPlayRule rule = before.PlayRules!.Cards.Single(candidate => candidate.DataId == CardDataId);
        AbilityChangeRule change = rule.Effects.Single().AbilityChange!;
        UnitAbilityDefinition definition = change.Definition!;
        Require(card.DataId == CardDataId && exhaust.Type == "CardTraitExhaustState" && rule.AssetKey == "TomeOfTheLostPath" &&
            rule.CardType == "Spell" && rule.Effects.Count == 1 && rule.Effects[0].Type == "SetUnitAbility" &&
            rule.Effects[0].Target == "DropTargetCharacter" && rule.Effects[0].AllowPlayer && !rule.Effects[0].AllowEnemy &&
            rule.ExternalInteractions.Count == 0 && definition.IsUnitAbility && definition.DataId == definition.CardCreation!.DataId &&
            targetBefore.Team == CombatTeam.Player && targetBefore.Ability == null && targetAfter.Ability is { HasAbility: true, FromEquipment: false } &&
            targetAfter.Ability.DataId == definition.DataId && targetAfter.Ability.Cooldown == definition.Cooldown &&
            targetAfter.Ability.CooldownAtSpawn == definition.CooldownAtSpawn && controlBefore.Ability == null && controlAfter.Ability == null &&
            action.CardInstanceId == ids[2] && action.RoomIndex == 0 && before.Energy - after.Energy == rule.Cost &&
            after.Spawn.Train.Context!.Statistics!.Value(ids[2], "TimesPlayed", "ThisBattle") == 1 &&
            after.Spawn.Train.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(item => item.InstanceId == ids[2]),
            "Tome of the Lost Path changed its ability definition, target legality, floor scope, cost or Exhaust routing.");

        BattleActionResult replay = BattleActionModel.PlayCard(before, action);
        Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(after),
            "The recorded Tome of the Lost Path action does not replay to the native state: " + replay.Reason);
        Require(BattleActionModel.EnumerateActions(before).SupportedCandidates.Any(candidate => candidate.Action.CardInstanceId == ids[2] &&
            candidate.Action.RoomIndex == 0 && candidate.Action.TargetUnitId == targetBefore.Id),
            "Action search did not enumerate the native Tome of the Lost Path play.");
        RecordedBattleChecks.Native(root);
        System.Console.WriteLine($"ORIGINAL-TOME-OF-THE-LOST-PATH PASS: assigned native unit ability {definition.DataId}, left the control floor untouched, Exhausted the Tome and replayed exactly.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new System.InvalidOperationException(message); }
}
