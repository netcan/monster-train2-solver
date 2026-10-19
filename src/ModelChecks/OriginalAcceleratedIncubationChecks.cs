using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalAcceleratedIncubationChecks
{
    private const string CardId = "71df734f-cb47-4f8b-a298-d15052e6b396";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-accelerated-incubation" && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Accelerated Incubation witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length >= 3, "Missing two control unit plays and Accelerated Incubation.");
        FixtureValue spellAction = actions.Single(action => Plays(action, CardId));
        BattleTurnState before = spellAction.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = spellAction.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        int spellId = spellAction.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == CardId);
        CombatUnit[] targetsBefore = before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
        CombatUnit[] targetsAfter = after.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
        Require(targetsBefore.Length == 2 && targetsBefore.All(unit => unit.Status("hatch")?.Stacks == 3) &&
            targetsAfter.Length == 2 && targetsAfter.All(unit => unit.Status("hatch") == null),
            "The native room spell must remove Hatch 3 from both friendly controls.");
        Require(rule.AssetKey == "AcceleratedIncubation" && rule.Cost == 0 && rule.Effect == "Spell" &&
            rule.Effects.Count == 1 && rule.Effects[0].Type == "RemoveStatus" &&
            rule.Effects[0].Target == "Room" && rule.Effects[0].Statuses.Count == 1 &&
            rule.Effects[0].Statuses[0].Id == "hatch" && rule.Effects[0].Statuses[0].Stacks == 3 &&
            rule.ExternalInteractions.Count == 0 && before.Energy == after.Energy &&
            spellAction.GetProperty("Action").GetProperty("RoomIndex").GetInt32() == 0 &&
            after.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == spellId),
            "Unchanged 0-cost Accelerated Incubation must remove exactly three Hatch stacks and Exhaust.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-ACCELERATED-INCUBATION PASS: unchanged 0-cost spell removes Hatch 3 from two friendly room targets and Exhausts.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    private static bool Plays(FixtureValue action, string dataId)
    {
        int instanceId = action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
        return action.GetProperty("Before").GetProperty("Spawn").GetProperty("Train").GetProperty("Context")
            .GetProperty("CardRegistry").EnumerateArray().Any(card =>
                card.GetProperty("InstanceId").GetInt32() == instanceId && card.GetProperty("DataId").GetString() == dataId);
    }
}
