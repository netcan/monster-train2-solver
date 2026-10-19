using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalDeadlyVariantChecks
{
    private const string CardId = "5efb679b-6035-4d64-ab80-f29daad1fdf4";
    private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids) => OriginalDeadlyVariantScenarioChooser.Choose(source, ids);

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 260 &&
            root.GetProperty("ModifierScenario").GetString() == "original-deadly-variant" &&
            root.GetProperty("OriginalDeadlyVariantPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Deadly Variant witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        int[] ids = root.GetProperty("OriginalDeadlyVariantCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 2 && actions.Length == 2 &&
            actions[0].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[0] &&
            actions[1].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1],
            "The native policy must summon its controlled Train Steward before playing Deadly Variant.");

        FixtureValue entry = actions[1];
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction played = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext beforeContext = before.Spawn.Train.Context!;
        CombatContext afterContext = after.Spawn.Train.Context!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == CardId);
        CombatUnit target = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == played.TargetUnitId);
        CombatUnit afterTarget = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == target.Id);
        Require(rule.Cost == 2 && rule.Effects.Count == 1 && rule.Effects[0].Type == "AddStatusPerOtherEffect" &&
            rule.Effects[0].Target == "DropTargetCharacter" && rule.Effects[0].AllowEnemy && rule.Effects[0].AllowPlayer &&
            rule.Effects[0].CountStatusOnlyOnTargets && rule.Effects[0].Value == 5 &&
            rule.Effects[0].Statuses[0].Id == "decay" && rule.Effects[0].Statuses[0].Stacks == 1 &&
            rule.Effects[0].Statuses[1].Id == "debuff" && rule.Effects[0].Statuses[1].Stacks == 5 &&
            target.Team == CombatTeam.Player && target.SpawnerCardId == ids[0] && target.Status("debuff")?.Stacks == 1,
            "Deadly Variant must count one Debuff on the selected Train Steward and scale Decay by five.");
        Require(after.Energy == before.Energy - rule.Cost && afterTarget.Status("debuff")?.Stacks == 1 &&
            afterTarget.Status("decay")?.Stacks == 5 &&
            afterContext.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(item => item.InstanceId == ids[1]) &&
            !afterContext.Cards.Discard.Any(item => item.InstanceId == ids[1]),
            "Deadly Variant did not spend two energy, apply five Decay without consuming the counted Debuff, and exhaust itself.");

        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-DEADLY-VARIANT PASS: one selected-target Debuff scales the original spell to five Decay, then Consume exhausts it.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}

internal static class OriginalDeadlyVariantScenarioChooser
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
            PlayCardAction? play = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
                action.CardInstanceId == ids[1] && action.TargetUnitId == target.Id);
            return play != null && BattleActionModel.PlayCard(source, play).Supported ? play : null;
        }
        return null;
    }
}
