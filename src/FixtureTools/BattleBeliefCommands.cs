using System.Security.Cryptography;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BattleBeliefCommands
{
    internal static void Run(string input, string output, int worlds, int decisions, int nodes, int milliseconds, int? nativeTurn)
    {
        if (File.Exists(output)) throw new IOException("Output already exists: " + output);
        using var fixture = FixtureDocument.Read(input); var source = fixture.RootElement;
        BattleTurnState root;
        if (source.TryGetProperty("Root", out var explicitRoot))
        {
            Require(!nativeTurn.HasValue, "A request Root cannot be combined with a native turn index.");
            root = explicitRoot.Deserialize<BattleTurnState>()!;
        }
        else
        {
            foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
                Require(source.GetProperty(counter).GetInt32() == 0, "Unaccepted native source: " + counter);
            var actions = source.GetProperty("Actions").EnumerateArray().ToArray();
            root = (nativeTurn.HasValue ? source.GetProperty("Turns")[nativeTurn.Value] :
                actions.Length > 0 ? actions[0] : source.GetProperty("Turns")[0]).GetProperty("Before").Deserialize<BattleTurnState>()!;
        }
        var belief = DrawOrderBelief.Create(root, worlds);
        var result = BattleBeliefPlanner.Plan(belief, new BattleBeliefOptions(nodes, decisions, TimeSpan.FromMilliseconds(milliseconds)));
        var replay = BattleBeliefReplay.Verify(belief, result.Policy);
        using var document = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "ModelPrediction", Verified = true,
            ProofScope = "FiniteCurrentDrawOrderBelief", SourceArchive = Path.GetFileName(input),
            SourceSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))).ToLowerInvariant(),
            NativeTurnIndex = nativeTurn, Belief = belief, Policy = result.Policy, FinalWorlds = replay.FinalWorlds,
            Options = new { MaxWorlds = worlds, MaxDecisions = decisions, MaxNodes = nodes, Milliseconds = milliseconds },
            result.ExpandedWorlds, result.ReachedNodeLimit, result.TimedOut, result.Cancelled, result.UnsupportedReasons,
            result.CertifiedFiniteHorizonOptimal, ExpectedScore = replay.ExpectedScore });
        using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write); document.Write(stream);
        Console.WriteLine($"BELIEF-BATTLE verified=true worlds={belief.Worlds.Count} exhaustivePrior={belief.ExhaustiveDistribution} " +
            $"complete={result.Policy.CompletedHorizon} certifiedFiniteHorizon={result.CertifiedFiniteHorizonOptimal} " +
            $"nodes={result.ExpandedWorlds} expectedScore={replay.ExpectedScore}");
    }
    internal static void Verify(string input)
    {
        using var fixture = FixtureDocument.Read(input); var source = fixture.RootElement;
        Require(source.GetProperty("Schema").GetInt32() == 1 && source.GetProperty("ProofScope").GetString() == "FiniteCurrentDrawOrderBelief",
            "Unknown belief policy archive.");
        var belief = source.GetProperty("Belief").Deserialize<BattleBelief>()!;
        var policy = source.GetProperty("Policy").Deserialize<BattleBeliefPolicy>()!;
        var replay = BattleBeliefReplay.Verify(belief, policy);
        Require(replay.ExpectedScore == source.GetProperty("ExpectedScore").GetDouble(), "Archived policy score differs.");
        var actual = source.GetProperty("FinalWorlds").Deserialize<BattleBeliefWorld[]>()!;
        using var keys = new BattleStateKeys();
        Require(actual.Length == replay.FinalWorlds.Count && actual.Zip(replay.FinalWorlds).All(pair =>
            pair.First.Weight == pair.Second.Weight && pair.First.Outcome == pair.Second.Outcome &&
            keys.Key(pair.First.State) == keys.Key(pair.Second.State)), "Archived policy final states/RNG differ.");
        Console.WriteLine($"BELIEF-VERIFY PASS: {actual.Length} immutable hypotheses, observation-based actions, " +
            "all posterior probabilities/values and final complete states/RNG reproduced independently.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
