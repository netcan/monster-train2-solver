using System.Security.Cryptography;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static partial class OfflineBattleSearch
{
    internal static void ExtractSimplePolicyUnsupported(string input, string output)
    {
        if (File.Exists(output)) throw new IOException("Output already exists: " + output);
        using var source = FixtureDocument.Read(input);
        RequireNativeInput(source.RootElement);
        BattleTurnState root = DeploymentRoot(source.RootElement), current = root;
        using var keys = new BattleStateKeys();
        string original = keys.Key(root);
        var prefix = new List<Step>();
        RoomOutcome outcome = RoomOutcome.Exchanged;
        while (!Terminal(outcome) && prefix.Count < 256)
        {
            PlayCardAction? action = BattleActionModel.ChooseUnitSpellAndJunkPlay(current);
            bool end = action == null;
            BattleTurnResult? turn = end ? BattleTurnModel.EndTurn(current) : null;
            BattleActionResult? play = end ? null : BattleActionModel.PlayCard(current, action!);
            bool supported = end ? turn!.Supported : play!.Supported;
            if (!supported)
            {
                string? reason = end ? turn!.UnsupportedReason : play!.Reason;
                Require(!string.IsNullOrWhiteSpace(reason) && (end || play!.Rejection == ActionRejection.Unsupported),
                    "Simple policy reached a non-unsupported failure.");
                BattleTurnState replay = root;
                RoomOutcome replayOutcome = RoomOutcome.Exchanged;
                foreach (Step step in prefix)
                {
                    Require(!Terminal(replayOutcome), "Failure prefix follows a terminal result.");
                    replay = Apply(replay, step.Action, step.IsEndTurn, out replayOutcome);
                }
                using var independent = new BattleStateKeys();
                Require(independent.Key(replay) == independent.Key(current) && independent.Key(root) == original,
                    "Failure prefix differs or mutated its deployment root.");
                Require(end ? BattleTurnModel.EndTurn(replay).UnsupportedReason == reason :
                    BattleActionModel.PlayCard(replay, action!).Reason == reason,
                    "Failure boundary did not reproduce independently.");
                using var document = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "ModelUnsupportedBranch",
                    SourceArchive = Path.GetFileName(input), SourceSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))).ToLowerInvariant(),
                    Root = root, Prefix = prefix, FailedRoot = current, Action = action, IsEndTurn = end, Reason = reason,
                    NativeValidationRequired = true });
                using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write);
                document.Write(stream);
                Console.WriteLine($"SIMPLE-POLICY-UNSUPPORTED prefix={prefix.Count} turn={current.Spawn.Turn} end={end} reason={reason}");
                return;
            }
            prefix.Add(new Step { Action = action, IsEndTurn = end });
            current = end ? turn!.State! : play!.State!;
            outcome = end ? turn!.Outcome : play!.Outcome;
        }
        throw new InvalidDataException($"Simple policy reached {outcome} after {prefix.Count} steps without an unsupported boundary.");
    }
}
