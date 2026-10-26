using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnplacedMoreMassMoonChecks
{
    internal const string Reason = "Unplaced MoreMass summons keep receiving global moon changes.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length == 12 && rows[^1].GetProperty("IsEndTurn").GetBoolean() &&
            source.GetProperty("NativeWon").ValueKind == FixtureKind.Null && source.GetProperty("CaptureFailures").GetInt32() == 0 &&
            source.GetProperty("Mismatches").GetInt32() == 1 && source.GetProperty("Unsupported").GetInt32() == 0 &&
            source.GetProperty("Pending").GetInt32() == 0, "Missing incomplete12-input native retained MoreMass counterexample.");
        var before = rows[^1].GetProperty("Root").Deserialize<BattleTurnState>()!;
        var historical = rows[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var retained = before.Spawn.Train.Context!.Enchantments!.RetainedUnits.Where(actor => actor.RoomIndex == -1 &&
            actor.Unit.AssetKey == "Boss_Level3_MiniFleshweaver_MoreMass").ToArray();
        Require(before.Spawn.Turn == 1 && before.MoonPhase == "Full" && actual.MoonPhase == "New" && retained.Length == 7 &&
            retained.All(actor => !actor.Preview && actor.Unit.Health == 5 && actor.Unit.DeathState?.IsDestroyed == false && actor.Unit.MoonPhaseMask == 4),
            "Missing seven live unplaced MoreMass summons before the real full-to-new moon transition.");
        foreach (var actor in retained)
        {
            var old = historical.Spawn.Train.Context!.Enchantments!.RetainedUnits.Single(item => item.Unit.Id == actor.Unit.Id).Unit;
            var next = actual.Spawn.Train.Context!.Enchantments!.RetainedUnits.Single(item => item.Unit.Id == actor.Unit.Id).Unit;
            Require(old.MoonPhaseMask == 4 && old.Statuses.Count == 0 && old.StatusRegistry!.Count == 0 &&
                next.MoonPhaseMask == 5 && next.Statuses.Single().Id == "newmoon" && next.StatusRegistry!.Single().Id == "newmoon" &&
                next.Health == actor.Unit.Health && next.Attack == actor.Unit.Attack,
                "Preserve the missing original moon status/mask without changing summon stats or identity.");
        }
        var root = rows[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        string immutable = JsonSerializer.Serialize(root, ModelJson.Options);
        var current = root;
        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            Require(NativeBranchObservationChecks.Key(current) == NativeBranchObservationChecks.Key(row.GetProperty("Root").Deserialize<BattleTurnState>()!),
                "Retained MoreMass native root differs at" + i);
            current = BattlePlannerChecks.Replay(current, [new(row.GetProperty("Action").Deserialize<PlayCardAction>(), row.GetProperty("IsEndTurn").GetBoolean())], out var outcome);
            Require((int)outcome == row.GetProperty("Outcome").GetInt32() &&
                NativeBranchObservationChecks.Key(current) == NativeBranchObservationChecks.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!),
                "Retained MoreMass complete successor/RNG differs at" + i);
        }
        Require(immutable == JsonSerializer.Serialize(root, ModelJson.Options), "Retained MoreMass replay changed its root.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Detached retained MoreMass moon transition differs.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Retained MoreMass transition changed its parent.");
        if (replay.HasValue)
        {
            var fresh = NativeBranchObservationChecks.Replay(replay.Value, Reason);
            Require(fresh.Length == rows.Length, "Independent retained MoreMass replay changed its input count.");
            for (int i = 0; i < rows.Length; i++)
            {
                foreach (string field in new[] { "Action", "IsEndTurn" })
                    Require(rows[i].GetProperty(field).ContentEquals(fresh[i].GetProperty(field)), "Independent retained MoreMass input changed.");
                Require(NativeBranchObservationChecks.Key(rows[i].GetProperty("Root").Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(fresh[i].GetProperty("Before").Deserialize<BattleTurnState>()!) &&
                    NativeBranchObservationChecks.Key(rows[i].GetProperty("Actual").Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(fresh[i].GetProperty("Actual").Deserialize<BattleTurnState>()!),
                    "Independent retained MoreMass full native state/RNG differs at" + i);
            }
        }
        Console.WriteLine("UNPLACED-MOREMASS-MOON PASS:12 native inputs/full state/RNG, seven live unplaced summons gain newmoon/mask5, original missing statuses preserved, four immutable EndTurns; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
