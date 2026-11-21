using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TurnEndBumpChecks
{
    internal static void Native(string path)
    {
        using var archive = ModelJson.ReadFixture(path);
        var root = archive.RootElement;
        var definition = root.GetProperty("OriginalTurnEndBumpDefinition");
        string id = definition.GetProperty("Id").GetString()!;
        int direction = Int(definition, "Amount");
        Require(root.GetProperty("OriginalTurnEndBumpPrepared").GetBoolean() &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            definition.ContentEquals(root.GetProperty("OriginalTurnEndBumpDefinitionAfter")) &&
            RelicTurnEndBumpModel.IsOwner(id) && direction == (id == RelicTurnEndBumpModel.GravityId ? -1 : 1),
            "Missing unchanged original combat-end movement mutator.");

        var callbacks = root.GetProperty("OriginalTurnEndBumpCallbacks").EnumerateArray().ToArray();
        Require(callbacks.All(row => row.GetProperty("Completed").GetBoolean() && Int(row.GetProperty("Before"), "Turn") > 0),
            "Original combat-end movement ran during placement or failed to settle.");
        var movingTurns = new HashSet<int>();
        foreach (var callback in callbacks.Where(row => row.GetProperty("Kind").GetString() == "Apply"))
        {
            var bumps = callback.GetProperty("Bumps").EnumerateArray().ToArray();
            var origins = bumps.Select(row => Int(row, "Room")).ToArray();
            Require(origins.SequenceEqual(direction < 0 ? origins.OrderBy(room => room) : origins.OrderByDescending(room => room)) &&
                origins.Distinct().Count() == origins.Length, "Original combat-end floor order differs.");
            var targets = new HashSet<int>();
            foreach (var bump in bumps)
            {
                Require(bump.GetProperty("Completed").GetBoolean() && Int(bump, "Amount") == direction, "Original Bump call did not settle.");
                var before = bump.GetProperty("Before").GetProperty("Actors").EnumerateArray().ToDictionary(actor => Int(actor, "Id"));
                var after = bump.GetProperty("After").GetProperty("Actors").EnumerateArray().ToDictionary(actor => Int(actor, "Id"));
                foreach (int actorId in bump.GetProperty("Targets").Deserialize<int[]>()!)
                {
                    Require(targets.Add(actorId) && before.TryGetValue(actorId, out var actor) && Int(actor, "Room") == Int(bump, "Room"),
                        "Original copied target list moved the same actor twice in one callback.");
                    if (after.TryGetValue(actorId, out var moved) && Int(moved, "Room") != Int(bump, "Room"))
                    {
                        Require(Int(moved, "Room") == Int(bump, "Room") + direction, "Original control moved in the wrong direction.");
                        movingTurns.Add(Int(callback.GetProperty("Before"), "Turn"));
                    }
                }
            }
        }
        Require(movingTurns.Count >= 2, "Missing original combat-end movements across multiple turns.");
        CardCycleChecks.Native(root);
        SolverStageChecks.Native(path);
        Console.WriteLine($"TURN-END-BUMP PASS owner={definition.GetProperty("Key").GetString()} direction={direction} movingTurns={movingTurns.Count}.");
    }

    private static int Int(FixtureValue value, string key) => value.GetProperty(key).GetInt32();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}
