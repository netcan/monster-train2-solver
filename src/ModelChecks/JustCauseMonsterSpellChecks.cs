using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class JustCauseMonsterSpellChecks
{
    private const string Reason = "Only a single fixed, Monster-targeted DropTarget damage effect without OnCast is modeled.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Require(rows.Length == 13 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong JustCause native boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(action.CardInstanceId == 19 && action.RoomIndex == 0 && action.TargetUnitId == 6,
            "Wrong native JustCause input.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string expected = NativeBranchObservationChecks.Key(rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && result.Outcome == RoomOutcome.Exchanged &&
                NativeBranchObservationChecks.Key(result.State!) == expected &&
                JsonSerializer.Serialize(before, ModelJson.Options) == parent,
                "JustCause relic copy changed native state/RNG or its parent.");
        });
        var native = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var nativePlayers = native.Spawn.Train.Rooms.Single(r => r.RoomIndex == 0).Units.Where(u => u.Team == CombatTeam.Player).ToArray();
        Require(nativePlayers.Select(u => u.Id).SequenceEqual(new[] { 6, 11, 13 }) &&
            nativePlayers.All(u => u.Status("valor")?.Stacks == 2),
            "Native copied JustCause must grant Valor to all teammates without further rearrangement.");
        void Reject(Action<JsonNode> mutate)
        {
            var node = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            mutate(node);
            var control = node.Deserialize<BattleTurnState>(ModelJson.Options)!;
            string unchanged = JsonSerializer.Serialize(control, ModelJson.Options);
            Require(!BattleActionModel.PlayCard(control, action).Supported &&
                JsonSerializer.Serialize(control, ModelJson.Options) == unchanged,
                "Uncalibrated JustCause copy shape/upgrade was accepted or mutated its parent.");
        }
        JsonNode Rule(JsonNode node) => node["PlayRules"]!["Cards"]!.AsArray().Single(c => c!["AssetKey"]!.GetValue<string>() == "JustCause")!;
        JsonNode Card(JsonNode node) => node["Spawn"]!["Train"]!["Context"]!["CardInstances"]!.AsArray().Single(c => c!["InstanceId"]!.GetValue<int>() == 19)!;
        Reject(node => Rule(node)["Effects"]![1]!["Statuses"]![0]!["Stacks"] = 3);
        Reject(node => Card(node)["Permanent"]!["Upgrades"]![0]!["AssetKey"] = "ChangedCorruptUpgrade");
        Reject(node => Card(node)["Permanent"]!["Upgrades"]![0]!["Stats"]!["Damage"] = 1);
        Console.WriteLine("JUSTCAUSE-MONSTER-SPELL PASS:13 native inputs/full state/RNG, native position-effect replacement and four immutable parallel branches; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
