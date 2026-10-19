using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalMoonRitualChecks
{
    private const string CardId = "b1fe0ba1-fcce-4764-9f4c-54b77d20b665";

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids) => OriginalMoonRitualScenarioChooser.Choose(source, ids);

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 256 &&
            root.GetProperty("ModifierScenario").GetString() == "original-moon-ritual" &&
            root.GetProperty("OriginalMoonRitualPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Moon Ritual witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        int[] ids = root.GetProperty("OriginalMoonRitualCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 2 && actions.Length == 2, "Missing Moon Ritual or its Train Steward target.");
        Require(actions[0].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[0] &&
            actions[1].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1],
            "The native policy must summon its controlled target before playing Moon Ritual.");

        FixtureValue action = actions[1];
        BattleTurnState before = action.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = action.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction played = action.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == CardId);
        CombatUnit target = before.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.Id == played.TargetUnitId);
        Require(rule.Cost == 0 && rule.Effects.Count == 2 && rule.Effects[0].Type == "AddStatus" &&
            rule.Effects[0].Target == "DropTargetCharacter" && !rule.Effects[0].AllowEnemy && rule.Effects[0].AllowPlayer &&
            rule.Effects[0].Statuses.Single().Id == "conduit" && rule.Effects[0].Statuses.Single().Stacks == 1 &&
            rule.Effects[1].Type == "AdvanceMoonPhase" && rule.Effects[1].Value == 1 &&
            rule.Effects[1].Target == "Room" && !rule.Effects[1].AllowEnemy && rule.Effects[1].AllowPlayer &&
            target.Team == CombatTeam.Player && target.SpawnerCardId == ids[0],
            "Moon Ritual must apply one Conduit to its selected friendly Train Steward before advancing one Moon Phase.");
        Require(before.MoonPhaseTrackingActive == true && before.MoonPhase is "New" or "Full" &&
            after.MoonPhaseTrackingActive == true && after.MoonPhase != before.MoonPhase &&
            after.Spawn.Train.Context!.QueryFrame!.MoonPhase == (after.MoonPhase == "New" ? 1 : 2) &&
            after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == target.Id).Status("conduit")?.Stacks == 1 &&
            after.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => !unit.IsPyre && unit.Health > 0)
                .All(unit => unit.Status(after.MoonPhase == "New" ? "newmoon" : "fullmoon")?.Stacks == 1 &&
                    unit.Status(after.MoonPhase == "New" ? "fullmoon" : "newmoon") == null) &&
            before.Energy == after.Energy && after.Spawn.Train.Context.Cards.Discard.Any(card => card.InstanceId == ids[1]),
            "Moon Ritual did not grant Conduit, advance the tracked phase, refresh all live units or remain zero-cost.");

        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-MOON-RITUAL PASS: unchanged zero-cost play gives the selected Monster Conduit and advances the tracked phase.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}

internal static class OriginalMoonRitualScenarioChooser
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
                .FirstOrDefault(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0 && unit.SpawnerCardId == ids[0]);
            if (target == null) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
                action.CardInstanceId == ids[1] && action.TargetUnitId == target.Id);
        }
        return null;
    }
}
