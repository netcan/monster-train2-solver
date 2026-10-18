using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalConsumeCardsChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        for (int index = 0; index < ids.Length; index++)
            if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
            {
                if (index >= 7 && source.Spawn.Turn < 1) return null;
                int target = index is 2 or 3 or 4 or 5 or 7 ? source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] &&
                    play.RoomIndex == (index == 1 ? 1 : 0) && (target == 0 || play.TargetUnitId == target));
            }
        return null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalConsumeCardsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Consume witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalConsumeCardsIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 10 && actions.Length == 10, "Missing seven original spells or played/unplayed/outside controls.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Unit(BattleTurnState state, int owner) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[owner]);
        string[] definitions = { "2ed78ce2-13c3-4e82-963f-99c9ea12d0f0", "a6dfc0d2-9bfe-499a-b521-0a2c4cefc1a2",
            "0ce88ae8-27f6-4894-8b1c-9c1aeed39f4d", "ec0980cf-9f88-4d38-9704-b89042f2f625", "45ace521-7551-4473-a4d7-2cf7ecc9fa44",
            "2ed78ce2-13c3-4e82-963f-99c9ea12d0f0", "9ea70171-8afd-4fab-ad54-7ff5c8b478b8", "768a53f0-2743-4298-82c3-8541cbecf935" };
        int[] costs = { 0, 1, 1, 1, 0, 0, 0, 0 };
        for (int index = 2; index < ids.Length; index++)
        {
            BattleTurnState before = Before(index), after = After(index);
            CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == definitions[index - 2]);
            CardTraitValue trait = before.Spawn.Train.Context!.FindCard(ids[index])!.MaskDescriptor!.BaseTraits.Single();
            Require(rule.Cost == costs[index - 2] && before.Energy - after.Energy == rule.Cost && rule.ExternalInteractions.Count == 0 &&
                rule.UpgradeInteractions.Count == 0 && trait.RuntimeType == "CardTraitExhaustState" && trait.DeclaredName == trait.RuntimeType &&
                trait.DefinitionParamInt == 0 && trait.ParamInt == 0, "Original Consume definition, trait identity or cost differs.");
            Require(after.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[index]) &&
                after.Spawn.Train.Context.Statistics!.Value(ids[index], "TimesExhausted", "ThisBattle") == 2 &&
                after.Spawn.Train.Context.Statistics.Value(ids[index], "TimesPlayed", "ThisBattle") == 1,
                "One original Consume play must enter Exhausted with both native exhaustion notifications.");
            Require(Unit(before, 1).Health == Unit(after, 1).Health && Unit(before, 1).Statuses.Count == Unit(after, 1).Statuses.Count,
                "Original status/capacity spell changed the outside-floor unit.");
        }
        Require(Unit(After(2), 0).Status("buff")?.Stacks == 5 && Unit(After(3), 0).Status("damage shield")?.Stacks == 1 &&
            Unit(After(3), 0).Status("valor")?.Stacks == 4 && Unit(After(4), 0).Status("spell weakness")?.Stacks == 1 &&
            Unit(After(4), 0).Status("melee weakness")?.Stacks == 1 && Unit(After(5), 0).Status("spikes")?.Stacks == 10,
            "Original targeted potion payloads or LastTargetedCharacters composition differs.");
        Require(After(6).Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).PlayerMaximum ==
            Before(6).Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).PlayerMaximum + 1, "Original Space Prism capacity differs.");
        BattleTurnState redraw = root.GetProperty("Turns")[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        foreach (int id in ids.Skip(7))
            Require(redraw.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == id) &&
                redraw.Spawn.Train.Context.Statistics!.Value(id, "TimesExhausted", "ThisBattle") == 0 &&
                redraw.Spawn.Train.Context.Statistics.Value(id, "TimesPlayed", "ThisBattle") == 0,
                "Unplayed Consume must discard and redraw without exhausting or incrementing play count.");
        foreach (var sample in new[] { (8, "spell weakness", 2), (9, "poison", 8) })
        {
            CombatUnit[] enemies = After(sample.Item1).Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
            Require(enemies.Length >= 2 && enemies.All(unit => unit.Status(sample.Item2)?.Stacks == sample.Item3), "Missing enemy-present room potion payload.");
            Require(Unit(Before(sample.Item1), 0).Status(sample.Item2)?.Stacks == Unit(After(sample.Item1), 0).Status(sample.Item2)?.Stacks,
                "Enemy-only potion changed a friendly unit.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-CONSUME-CARDS PASS: seven unchanged originals, played exhaustion, unplayed discard/redraw, targeted/room payloads, cost and floor/team controls, 16 immutable chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
