using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PreDiscardSpellRngChecks
{
    private const string Reason = "Targeted spell consumes a different Battle RNG sequence in the native campaign.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, Historical);
        Require(rows.Length == 15, "Wrong native pre-discard RNG prefix.");
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var roll = before.Spawn.Train.Context!.BattleRng.Range(0, 100);
        Require(action.CardInstanceId == 23 && action.TargetUnitId == 2 &&
            before.Spawn.Train.Context.FindCard(23)!.DataId == "ab3c2bfa-0ad4-4be0-a050-293f4866bd65" &&
            before.Energy == 4 && actual.Energy == 0 && roll.Value >= 50 &&
            actual.Spawn.Train.Context!.BattleRng.Equals(roll.State) &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 2).Status("valor")?.Stacks == 29,
            "Missing the paid X-cost Valor spell and failed50% consume-suppression roll.");
        OriginalSuppressExhaustChecks.Run();
        if (replay is FixtureValue second)
        {
            NativeBranchObservationChecks.Replay(second, Reason);
            NativeBranchObservationChecks.Pair(source, second, originalPredictionPresent: true);
        }
        Console.WriteLine("PRE-DISCARD-SPELL-RNG PASS:15 native inputs/full state/RNG; spell tests retain the pre-discard suppression roll;4 energy/20 Valor/Consume unchanged; historical RNG rewind retained; suppression controls pass; independentPair=" + replay.HasValue);
    }

    private static void Historical(BattleTurnState predicted, BattleTurnState actual)
    {
        Require(predicted.Spawn.Train.Context!.BattleRng.Range(0, 100).State.Equals(actual.Spawn.Train.Context!.BattleRng),
            "Original RNG rewind is no longer exactly one suppression roll.");
        var old = JsonNode.Parse(NativeBranchObservationChecks.Key(predicted))!;
        var native = JsonNode.Parse(NativeBranchObservationChecks.Key(actual))!;
        old["Context"]!["BattleRng"] = native["Context"]!["BattleRng"]!.DeepClone();
        var streams = old["RngStreams"]!.AsArray();
        var nativeStreams = native["RngStreams"]!.AsArray();
        Require(streams.Count == nativeStreams.Count, "Unrelated RNG streams changed.");
        int corrected = 0;
        for (int i = 0; i < streams.Count; i++)
        {
            if (streams[i]!["Name"]!.GetValue<string>() != "Battle") continue;
            Require(nativeStreams[i]!["Name"]!.GetValue<string>() == "Battle", "Battle stream moved.");
            streams[i]!["State"] = nativeStreams[i]!["State"]!.DeepClone(); corrected++;
        }
        Require(corrected == 1 && JsonNode.DeepEquals(old, native), "Historical mismatch extends beyond the rewound Battle RNG.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
