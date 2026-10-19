using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalCoverOfNightChecks
{
    private const string CoverId = "e022d66a-f794-4ff9-95a6-b3cc53f49a27";
    private const string MoonriseId = "fe6dfafa-9712-484c-9bb4-145c0989e667";
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
            var action = new PlayCardAction(ids[index], index switch { 0 => 0, 1 => 1, _ => 0 }, playerPosition: index < 2 ? 0 : -1);
            if (BattleActionModel.PlayCard(source, action).Supported) return action;
        }
        return null;
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 103 && root.GetProperty("ModifierScenario").GetString() == "original-cover-of-night" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Missing completed original Cover of Night witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        int[] ids = CardIds(root);
        Require(actions.Length == 4 && ids.Length == 4, "Expected two floor controls, Moonrise and one original Cover of Night play.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;

        Require(Before(0).Spawn.Train.Context!.FindCard(ids[0])!.DataId == StewardId &&
            Before(1).Spawn.Train.Context!.FindCard(ids[1])!.DataId == StewardId &&
            Before(2).Spawn.Train.Context!.FindCard(ids[2])!.DataId == MoonriseId &&
            Before(3).Spawn.Train.Context!.FindCard(ids[3])!.DataId == CoverId,
            "The controlled action sequence did not deploy both Stewards before the two original spells.");
        Require(After(2).MoonPhase == "Full" && Before(3).MoonPhase == "Full" && After(3).MoonPhase == "New",
            "Moonrise did not establish Full before Cover of Night set New.");

        BattleTurnState before = Before(3), after = After(3);
        CombatUnit targetBefore = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]);
        CombatUnit targetAfter = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == targetBefore.Id);
        CombatUnit controlBefore = before.Spawn.Train.Rooms[1].Units.Single(unit => unit.SpawnerCardId == ids[1]);
        CombatUnit controlAfter = after.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == controlBefore.Id);
        CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[3])!;
        CardSourceTrait exhaust = card.MaskDescriptor!.SourceTraits!.Single();
        CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == CoverId);
        PlayCardAction action = actions[3].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(card.DataId == CoverId && card.MaskDescriptor.SourceTraits.Count == 1 && exhaust.Type == "CardTraitExhaustState" &&
            exhaust.Int == 0 && exhaust.Int2 == 0 && exhaust.Int3 == 0 && !exhaust.Scaling && exhaust.TrackedValue == "SubtypeInDeck" &&
            exhaust.EntryDuration == "ThisTurn" && rule.AssetKey == "CoverOfNight" && rule.CardType == "Spell" && rule.Cost == 1 &&
            rule.ExternalInteractions.Count == 0 && rule.Effects.Select(effect => effect.Type).SequenceEqual(["SetMoonPhase", "UnitUpgrade", "AddStatus"]),
            "Original Cover of Night identity, source trait or ordered effects differ.");
        Require(action.CardInstanceId == ids[3] && action.RoomIndex == 0 && before.Energy - after.Energy == 1 &&
            targetAfter.Attack == targetBefore.Attack + 5 && targetAfter.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "CoverOfNightUnit_Upgrade") &&
            targetAfter.Status("stealth")?.Stacks == 1 && controlAfter.Attack == controlBefore.Attack && controlAfter.Status("stealth") == null &&
            after.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(item => item.InstanceId == ids[3]) &&
            after.Spawn.Train.Context.Cards.Discard.All(item => item.InstanceId != ids[3]),
            "Cover of Night failed to upgrade and grant Stealth on only the selected floor or paid/routed its card incorrectly.");

        BattleActionResult replay = BattleActionModel.PlayCard(before, action);
        Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(after),
            "The recorded Cover of Night action does not replay to the native state: " + replay.Reason);
        Require(BattleActionModel.EnumerateActions(before).SupportedCandidates.Any(candidate => candidate.Action.CardInstanceId == ids[3] &&
            candidate.Action.RoomIndex == 0 && candidate.Action.TargetUnitId == action.TargetUnitId),
            "Action search did not enumerate the native Cover of Night play.");
        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-COVER-OF-NIGHT PASS: Full-to-New phase ordering, +5 temporary room upgrade, one Stealth, untouched control floor and exact native replay.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new System.InvalidOperationException(message); }
}
