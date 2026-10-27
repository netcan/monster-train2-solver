using System.Text.Json;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PyregelJuiceChecks
{
    internal const string Reason = "Juice with this card or ordered scaling traits requires native calibration.";
    internal const string CastReason = "Original Juice pyregel spell doubles its status after direct damage.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null, FixtureValue? cast = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 1 && !rows[0].GetProperty("IsEndTurn").GetBoolean(), "Missing original deployment rejection.");
        var before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var card = before.Spawn.Train.Context!.FindCard(12)!;
        var rule = before.PlayRules!.Cards.Single(item => item.DataId == card.DataId);
        Require(card.DataId == "fb0683b1-20ac-4dd2-b580-14b502e182f4" &&
            rule.Effects is [{ Type: "Damage", Target: "DropTargetCharacter", Value: 1 },
                { Type: "AddStatus", Target: "LastTargetedCharacters", Statuses: [{ Id: "pyregel", Stacks: 2 }] }] &&
            CardJuiceModel.Validate(card) == null, "Wrong original damage/pyregel Juice signature.");
        var action = rows[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        string parent = NativeBranchObservationChecks.Key(before);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Juice owner incorrectly blocks an unrelated legal deployment.");
        });
        Require(parent == NativeBranchObservationChecks.Key(before), "Juice deployment mutated its parent.");
        foreach (string control in new[] { "Owner", "Upgrade", "Temporary", "Parameter" })
        {
            var node = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            if (control == "Owner") node["DataId"] = "uncalibrated-pyregel";
            if (control == "Upgrade") node["Permanent"]!["Upgrades"]!.AsArray().Clear();
            if (control == "Temporary") node["Temporary"]!["Upgrades"]!.AsArray().Add(node["Permanent"]!["Upgrades"]![0]!.DeepClone());
            if (control == "Parameter")
            {
                node["MaskDescriptor"]!["BaseTraits"]![0]!["ParamInt"] = 3;
                node["Permanent"]!["Upgrades"]![0]!["Lifecycle"]!["AddedTraits"]![0]!["ParamInt"] = 3;
            }
            Require(CardJuiceModel.Validate(node.Deserialize<CardInstanceState>(ModelJson.Options)!) != null,
                "Unknown Juice signature lost its guard: " + control);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        if (cast.HasValue)
        {
            var casts = NativeBranchObservationChecks.Replay(cast.Value, CastReason);
            var last = casts[^1];
            var root = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var native = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var input = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
            var target = root.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == input.TargetUnitId);
            var result = native.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == input.TargetUnitId);
            Require(input.CardInstanceId == 12 && target.Health > 1 && target.Status("armor") == null &&
                target.Status("damage shield") == null && result.Health == target.Health - 1 &&
                (result.Status("pyregel")?.Stacks ?? 0) == (target.Status("pyregel")?.Stacks ?? 0) + 4,
                "Actual native Juice cast did not preserve one damage followed by four pyregel stacks.");
        }
        Console.WriteLine("PYREGEL-JUICE PASS: original deployment/full state/forecast/RNG, four immutable branches/four scope controls; independentPair=" + replay.HasValue + "; nativeCast=" + cast.HasValue);
    }

    internal static void ExportCastRequest(string sourcePath, string output)
    {
        using var source = ModelJson.ReadFixture(sourcePath);
        var root = source.RootElement.GetProperty("Rows")[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var current = root;
        var prefix = new List<BattleSearchStep>();
        string immutable = NativeBranchObservationChecks.Key(root);
        for (int index = 0; index < 128; index++)
        {
            var candidates = BattleActionModel.EnumerateActions(current).SupportedCandidates;
            var cast = candidates.FirstOrDefault(candidate => candidate.Action.CardInstanceId == 12 &&
                current.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == candidate.Action.TargetUnitId &&
                    unit.Health > 1 && unit.Status("armor") == null && unit.Status("damage shield") == null));
            if (cast != null)
            {
                Require(immutable == NativeBranchObservationChecks.Key(root), "Cast request mutated its deployment root.");
                using var document = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "ModelUnsupportedBranch",
                    SourceArchive = Path.GetFileName(sourcePath), SourceSHA256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(sourcePath))).ToLowerInvariant(),
                    Root = root, Prefix = prefix, FailedRoot = current, Action = cast.Action, IsEndTurn = false,
                    Reason = CastReason, NativeValidationRequired = true });
                using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write);
                document.Write(stream);
                Console.WriteLine("PYREGEL-CAST-REQUEST prefix=" + prefix.Count + " turn=" + current.Spawn.Turn + "; model request, native validation required.");
                return;
            }
            var chosen = BattleHeuristicPolicy.Choose(current, out _, 0);
            var step = new BattleSearchStep(chosen?.Action, chosen == null);
            BattleTurnState next;
            RoomOutcome outcome;
            try { next = BattlePlannerChecks.Replay(current, [step], out outcome); }
            catch (InvalidOperationException error)
            {
                throw new InvalidOperationException("Cast request prefix=" + prefix.Count + " turn=" + current.Spawn.Turn +
                    " hand=" + string.Join(",", current.Spawn.Train.Context!.Cards.Hand.Select(card => card.InstanceId)) + ": " + error.Message, error);
            }
            current = next;
            Require(outcome == RoomOutcome.Exchanged, "Cast policy reached terminal combat before Juice.");
            prefix.Add(step);
        }
        throw new InvalidOperationException("No bounded native Juice cast request found.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
