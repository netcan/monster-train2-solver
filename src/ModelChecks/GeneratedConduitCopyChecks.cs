using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class GeneratedConduitCopyChecks
{
    internal const string Reason = "Native copied Moonbeam damage differs at input27.";
    internal static void Native(FixtureValue source, FixtureValue counterexample, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 27 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing complete copied-spell prefix.");
        var failed = source.GetProperty("Request").GetProperty("FailedRoot").Deserialize<BattleTurnState>()!;
        var nativeBefore = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var copy = nativeBefore.Spawn.Train.Context!.FindCard(34)!;
        Require(failed.Spawn.Turn == 4 && failed.Energy == 1 && copy.DataId == CardCopyOnPlayModel.MoonbeamSpellId &&
            !failed.Spawn.Train.Context!.FindCard(34)!.Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == "ConduitUpgrade") &&
            copy.Temporary.Upgrades.Where(upgrade => upgrade.AssetKey == "ConduitUpgrade").Select(upgrade => upgrade.Stats.Damage).SequenceEqual([2, 0]) &&
            Boss(nativeBefore).Health == 1029 && Boss(actual).Health == 987, "Lost same-floor generated copy, missing aura or42 damage witness.");
        var damage = rows[^1].GetProperty("DamageEffects");
        Require(damage.GetArrayLength() == 1 && damage[0].GetProperty("CardId").GetInt32() == 34 &&
            damage[0].GetProperty("ParentCardId").GetInt32() == 34 && damage[0].GetProperty("Unmodified").GetInt32() == 20 &&
            damage[0].GetProperty("Resolved").GetInt32() == 42, "Native effect parameters or parent alias differ.");
        Require(counterexample.GetProperty("Mismatches").GetInt32() == 0 &&
            counterexample.GetProperty("CaptureFailures").GetInt32() == 0,
            "Original per-action capture counters were changed; this is a rolling continuation failure.");
        var originalRows = counterexample.GetProperty("RollingDecisions");
        Require(originalRows.GetArrayLength() == 27 &&
            Boss(originalRows[26].GetProperty("Expected").Deserialize<BattleTurnState>()!).Health == 989 &&
            Boss(originalRows[26].GetProperty("Actual").Deserialize<BattleTurnState>()!).Health == 987,
            "Original full-battle counterexample lost the40/42 damage mismatch.");
        // Keep our own prefix throughout. Replacing it with each native Before
        // would silently supply the missing generated-card aura and hide the bug.
        BattleTurnState current = source.GetProperty("Request").GetProperty("Root").Deserialize<BattleTurnState>()!;
        BattleTurnState ownedBefore = null!;
        foreach (var row in rows)
        {
            ownedBefore = current;
            bool end = row.GetProperty("IsEndTurn").GetBoolean();
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>();
            current = BattlePlannerChecks.Replay(current, [new BattleSearchStep(action, end)], out var outcome);
            Require(outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(current) ==
                NativeBranchObservationChecks.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!),
                "Continuous model-owned prefix differs from native.");
        }
        Require(ownedBefore.Spawn.Train.Context!.FindCard(34)!.Temporary.Upgrades.Where(upgrade => upgrade.AssetKey == "ConduitUpgrade")
            .Select(upgrade => upgrade.Stats.Damage).SequenceEqual([2, 0]), "Owned copy did not receive both room contributors.");
        string parent = JsonSerializer.Serialize(ownedBefore, ModelJson.Options);
        var lastAction = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(ownedBefore, lastAction);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual) &&
                JsonSerializer.Serialize(ownedBefore, ModelJson.Options) == parent, "Copy changed its parent, successor or RNG.");
        });
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
            Require(replay.Value.GetProperty("Rows")[26].GetProperty("DamageEffects")[0].GetProperty("Resolved").GetInt32() == 42,
                "Independent native effect did not resolve42 damage.");
        }
        Console.WriteLine("GENERATED-CONDUIT-COPY PASS:27 native inputs/full state/RNG and continuously owned deployment replay, " +
            "two generated-card room upgrades, native20+20+2 damage, original40/42 failure and four immutable branches; independentPair=" + replay.HasValue);
    }
    private static CombatUnit Boss(BattleTurnState state) => state.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 7);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
