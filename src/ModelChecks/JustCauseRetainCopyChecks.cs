using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class JustCauseRetainCopyChecks
{
    private const string Reason = "Monster spell replication requires an owned, unupgraded card with captured mask metadata.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Require(rows.Length == 19, "Wrong retained JustCause prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(action.CardInstanceId == 26 && action.RoomIndex == 0 && action.TargetUnitId == 6,
            "Wrong retained JustCause boundary input.");
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var players = actual.Spawn.Train.Rooms.Single(r => r.RoomIndex == 0).Units.Where(u => u.Team == CombatTeam.Player).ToArray();
        Require(players.Select(u => u.Id).SequenceEqual(new[] { 6, 7 }) && players.All(u => u.Status("valor")?.Stacks == 4),
            "Copied retained JustCause changed the native team order or Valor.");
        var nativeCard = actual.Spawn.Train.Context!.FindCard(26)!;
        Require(nativeCard.Permanent.Upgrades.Count == 2 &&
            (nativeCard.MaskDescriptor!.BaseTraits.Any(t => t.RuntimeType == "CardTraitRetain" && t.HoldoverPending == true) ||
                nativeCard.MaskDescriptor.HoldoverUpgradePending == true), "The original card lost its native Holdover marker.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string native = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && result.Outcome == RoomOutcome.Exchanged &&
                NativeBranchObservationChecks.Key(result.State!) == native &&
                JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Retained copy differs from native state/RNG or mutates its parent.");
        });
        foreach (string field in new[] { "DataId", "Damage" })
        {
            var node = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            var card = node["Spawn"]!["Train"]!["Context"]!["CardInstances"]!.AsArray().Single(c => c!["InstanceId"]!.GetValue<int>() == 26)!;
            var upgrade = card["Permanent"]!["Upgrades"]![1]!;
            if (field == "DataId") upgrade[field] = "changed-retain-upgrade";
            else upgrade["Stats"]![field] = 1;
            var control = node.Deserialize<BattleTurnState>(ModelJson.Options)!;
            string unchanged = JsonSerializer.Serialize(control, ModelJson.Options);
            Require(!BattleActionModel.PlayCard(control, action).Supported &&
                JsonSerializer.Serialize(control, ModelJson.Options) == unchanged, "Uncalibrated retained upgrade was accepted.");
        }
        Console.WriteLine("JUSTCAUSE-RETAIN-COPY PASS:19 native inputs/full state/RNG, original Holdover marker, recast trait suppression and four immutable branches; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
