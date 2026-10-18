using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalStrongMagicCardsChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        for (int index = 0; index < ids.Length; index++)
            if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
            {
                int room = index is 1 or 2 or 3 ? 1 : 0;
                int target = index is 2 or 3 or 5 or 6 or 7 ? source.Spawn.Train.Rooms.SelectMany(item => item.Units)
                    .Single(unit => unit.SpawnerCardId == ids[room]).Id : 0;
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] &&
                    play.RoomIndex == room && (target == 0 || play.TargetUnitId == target));
            }
        return null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalStrongMagicCardsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original strong-magic witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalStrongMagicCardsIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 8 && actions.Length == 8, "Missing original spells, Planetarium or zero-power floor controls.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Unit(BattleTurnState state, int owner) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[owner]);
        foreach (var sample in new[] { (2, 1, 35, 2, "bae01a8c-25dd-4a08-b08a-5f17a5f3f52a"),
            (3, 1, -10, 1, "5b2ba535-54b5-4f42-86e4-9b2ff2e07563"), (5, 0, 60, 2, "bae01a8c-25dd-4a08-b08a-5f17a5f3f52a"),
            (6, 0, -35, 1, "5b2ba535-54b5-4f42-86e4-9b2ff2e07563"), (7, 0, 50, 3, "9113371f-f1dc-4971-b8d4-ed2930e456bc") })
        {
            BattleTurnState before = Before(sample.Item1), after = After(sample.Item1);
            CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == sample.Item5);
            CardTraitValue trait = before.Spawn.Train.Context!.FindCard(ids[sample.Item1])!.MaskDescriptor!.BaseTraits.Single();
            Require(rule.Cost == sample.Item4 && before.Energy - after.Energy == sample.Item4 && rule.ExternalInteractions.Count == 0 &&
                rule.UpgradeInteractions.Count == 0 && trait.RuntimeType == "CardTraitStrongerMagicPower" && trait.DeclaredName == trait.RuntimeType &&
                trait.DefinitionParamInt == (sample.Item1 == 7 ? 5 : 0) && trait.ParamInt == trait.DefinitionParamInt,
                "Original strong-magic definition, raw parameter variant or cost differs.");
            Require(Unit(before, sample.Item2).Health - Unit(after, sample.Item2).Health == sample.Item3,
                "Native strong-magic damage/healing or zero-power control differs.");
            Require(Unit(before, 1 - sample.Item2).Health == Unit(after, 1 - sample.Item2).Health, "Strong-magic effect leaked to the outside floor.");
        }
        Require(Unit(After(4), 0).Status("conduit")?.Stacks == 5 && Unit(After(4), 1).Status("conduit") == null &&
            Unit(After(7), 0).Status("dazed")?.Stacks == 3 && Unit(Before(7), 0).Health < 500,
            "Missing native Planetarium contributor, damaged-only RIP target or LastTargetedCharacters daze.");
        Require(Before(5).Spawn.Train.Context!.MagicPower!.Single(room => room.RoomIndex == 0 && room.Team == CombatTeam.Player).Value == 5,
            "Missing positive room magic power.");
        // Removing the friendly branch must still reject the same friendly
        // target. Combining teams must not bypass a single-team restriction.
        BattleTurnState mixed = Before(3);
        CardPlayRule duality = mixed.PlayRules!.Cards.Single(card => card.DataId == "5b2ba535-54b5-4f42-86e4-9b2ff2e07563");
        var enemyOnly = new CardPlayRule(duality.DataId, duality.AssetKey, duality.Cost, duality.Effect, duality.Destination,
            duality.SpawnUnit, duality.ExternalInteractions, effects: new[] { duality.Effects[0] }, cardType: duality.CardType);
        var restricted = new BattleTurnState(mixed.Spawn, mixed.Energy, mixed.EnergyPerTurn, mixed.DrawPerTurn,
            mixed.ForgePoints, mixed.DragonsHoard, mixed.MoonPhase, mixed.RngStreams, mixed.OtherPiles, mixed.ExternalInteractions,
            new BattlePlayRules(mixed.PlayRules.Rooms, mixed.PlayRules.Cards.Select(card => card.DataId == duality.DataId ? enemyOnly : card).ToArray()),
            selectedRoom: mixed.SelectedRoom);
        Require(BattleActionModel.PlayCard(restricted, actions[3].GetProperty("Action").Deserialize<PlayCardAction>()!).Rejection == ActionRejection.Illegal,
            "Combining target teams removed the single-team restriction.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-STRONG-MAGIC-CARDS PASS: original Cosmic Bolt/Duality/RIP, zero/five room power, damage/heal multipliers, raw zero/five trait parameters, costs/floor controls and 16 immutable chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
