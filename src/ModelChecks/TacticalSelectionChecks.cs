using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TacticalSelectionChecks
{
    internal static void Native(FixtureValue fixture)
    {
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(counter).GetInt32() == 0, "Unaccepted selection witness: " + counter);
        using var keys = new BattleStateKeys();
        int roots = 0, before = 0, after = 0;
        foreach (var row in fixture.GetProperty("RollingDecisions").EnumerateArray())
        {
            BattleTurnState state = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
            string parent = keys.Key(state);
            int floor = state.Spawn.Train.Rooms.FirstOrDefault(room => room.Units.Any(unit =>
                unit.Team == CombatTeam.Player && unit.Health > 0 && unit.Modifiers?.IsChampion == true))?.RoomIndex ?? 2;
            var reference = new BattleSearchWorkspace(false);
            var optimized = new BattleSearchWorkspace();
            var expected = reference.Choose(state, out var exhaustive, floor);
            var actual = optimized.Choose(state, out var attempted, floor);
            Require(JsonSerializer.Serialize(expected?.Action) == JsonSerializer.Serialize(actual?.Action),
                "Lazy selection changed the reference policy at input " + roots);
            if (expected != null)
                Require(actual != null && expected.Result.Outcome == actual.Result.Outcome &&
                    keys.Key(expected.Result.State!) == keys.Key(actual.Result.State!),
                    "Lazy selection changed a complete successor/RNG at input " + roots);
            Require(attempted.All(attempt => exhaustive.Any(other => other.Reason == attempt.Reason &&
                JsonSerializer.Serialize(other.Action) == JsonSerializer.Serialize(attempt.Action))),
                "Lazy selection lost an attempted unsupported diagnostic.");
            Require(keys.Key(state) == parent, "Selection candidates mutated the native parent.");
            before += reference.Metrics.SimulatedCandidates;
            after += optimized.Metrics.SimulatedCandidates;
            roots++;
        }
        Require(roots > 0 && after < before, "Selection witness did not eliminate candidate simulations.");
        Console.WriteLine($"TACTICAL-SELECTION PASS roots={roots} candidates={before}->{after}: " +
            "identical inputs, complete successors/RNG, immutable native parents and attempted diagnostics; full frontier unchanged.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    internal static void CostRejections(FixtureValue fixture)
    {
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray();
        foreach (var row in rows)
        {
            var root = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
            if (root.Energy != 0) continue;
            foreach (var card in root.Spawn.Train.Context!.Cards.Hand)
            foreach (var room in root.PlayRules!.Rooms)
            {
                var input = new PlayCardAction(card.InstanceId, room.RoomIndex);
                var rejected = BattleActionModel.PlayCard(root, input);
                if (rejected.Rejection != ActionRejection.Illegal || rejected.Reason != "Insufficient energy.") continue;
                var workspace = new BattleSearchWorkspace();
                workspace.Play(root, input);
                foreach (var other in new[] {
                    new PlayCardAction(card.InstanceId, room.RoomIndex, 99, int.MaxValue),
                    new PlayCardAction(card.InstanceId, room.RoomIndex, recursionSelections:
                        new[] { new CardRecursionSelection(99, new[] { 1 }) }),
                    new PlayCardAction(card.InstanceId, room.RoomIndex, mixSelections:
                        new[] { new ElixirMixSelection(99, 0, 99) }) })
                {
                    var plain = BattleActionModel.PlayCard(root, other);
                    int before = workspace.Metrics.SimulatedCandidates;
                    var cached = workspace.Play(root, other);
                    Require(plain.Rejection == cached.Rejection && plain.Reason == cached.Reason &&
                        cached.State == null && workspace.Metrics.SimulatedCandidates == before,
                        "Cost rejection differed or simulated target/choice effects before payment.");
                }
                foreach (var other in new[] {
                    new PlayCardAction(card.InstanceId, room.RoomIndex, targetSpawnPointIndex: 0),
                    new PlayCardAction(card.InstanceId, room.RoomIndex, activatorRoomIndex: room.RoomIndex) })
                {
                    int before = workspace.Metrics.SimulatedCandidates;
                    var plain = BattleActionModel.PlayCard(root, other);
                    var actual = workspace.Play(root, other);
                    Require(plain.Rejection == actual.Rejection && plain.Reason == actual.Reason &&
                        workspace.Metrics.SimulatedCandidates == before + 1,
                        "Ability/spawn-point inputs reused a direct-card cost rejection.");
                }
                var changed = JsonNode.Parse(JsonSerializer.Serialize(root, ModelJson.Options))!;
                changed["Energy"] = 99;
                changed["Spawn"]!["Train"]!["Context"]!["QueryFrame"]!["Energy"] = 99;
                var affordable = changed.Deserialize<BattleTurnState>(ModelJson.Options)!;
                int simulations = workspace.Metrics.SimulatedCandidates;
                var fresh = BattleActionModel.PlayCard(affordable, input);
                var retried = workspace.Play(affordable, input);
                Require(fresh.Reason != "Insufficient energy." && fresh.Rejection == retried.Rejection &&
                    fresh.Reason == retried.Reason && workspace.Metrics.SimulatedCandidates == simulations + 1,
                    "Changed decision energy reused an earlier rejection.");
                var full = BattleActionModel.EnumerateActions(root);
                var optimized = new BattleSearchWorkspace().Enumerate(root);
                Require(JsonSerializer.Serialize(full.UnsupportedAttempts) == JsonSerializer.Serialize(optimized.UnsupportedAttempts) &&
                    JsonSerializer.Serialize(full.SupportedActions) == JsonSerializer.Serialize(optimized.SupportedActions),
                    "Cost caching changed the complete legal frontier or unsupported diagnostics.");
                using var keys = new BattleStateKeys();
                for (int i = 0; i < full.SupportedCandidates.Count; i++)
                    Require(full.SupportedCandidates[i].Result.Outcome == optimized.SupportedCandidates[i].Result.Outcome &&
                        keys.Key(full.SupportedCandidates[i].Result.State!) == keys.Key(optimized.SupportedCandidates[i].Result.State!),
                        "Cost caching changed a legal successor/RNG.");
                Console.WriteLine("COST-REJECTION PASS: exact observed rejection, choice/target independence, changed energy, ability/spawn-point bypass and complete frontier/state/RNG identity.");
                return;
            }
        }
        throw new InvalidOperationException("Missing native zero-energy cost witness.");
    }
}
