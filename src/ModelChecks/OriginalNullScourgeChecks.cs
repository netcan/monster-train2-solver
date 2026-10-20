using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalNullScourgeChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-null-scourges" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Missing completed original Scourge witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        string[] originals = ["2785cc8e-d0e8-46c0-84ab-9c1d602525d4", "fbf74ec4-2781-4b99-b6f7-b7d75236da60", "4d1f27af-3122-4a08-87e3-5ef5a14ced94"];
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().Where(row => row.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(actions.Length == 3, "Expected three original targetless Scourge plays.");
        for (int i = 0; i < actions.Length; i++)
        {
            BattleTurnState before = actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!,
                after = actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = actions[i].GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == card.DataId);
            CardActionEffect effect = rule.Effects.Single();
            Require(card.DataId == originals[i] && rule.CardType == "Junk" && rule.Cost == 1 &&
                before.Energy - after.Energy == 1 && rule.ExternalInteractions.Count == 0 && action.RoomIndex == 0 &&
                action.TargetUnitId == 0 && action.PlayerPosition == -1 && before.Spawn.Train.Rooms[0].Units.Count == 0 &&
                effect.Type == "Null" && effect.Target == "Room" && effect.NullTargetless && !effect.NullForwardSelf &&
                effect.Statuses.Count == 0 && !effect.AllowEnemy && !effect.AllowPlayer &&
                after.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Purged").Cards.Any(token => token.InstanceId == card.InstanceId) &&
                !after.Spawn.Train.Context.Cards.Hand.Any(token => token.InstanceId == card.InstanceId),
                "Original Scourge must spend its native cost, resolve with zero targets and self-purge.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-NULL-SCOURGES PASS: three unchanged one-cost originals, zero-target plays, self-purge and immutable full chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
