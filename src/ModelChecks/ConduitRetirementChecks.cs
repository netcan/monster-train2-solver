using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ConduitRetirementChecks
{
    internal const string Reason = "Native Conduit retirement differs at fifth EndTurn.";

    internal static void Native(FixtureValue source, FixtureValue counterexample, FixtureValue? replay = null)
    {
        void Historical(BattleTurnState old, BattleTurnState actual)
        {
            Require(old.Spawn.Train.Context!.ConduitRetained is [{ Contributor.UnitId: 2, Contributor.Active: false }] &&
                actual.Spawn.Train.Context!.ConduitRetained!.Count == 0 && old.ExternalInteractions.Count == 0 &&
                actual.ExternalInteractions.SequenceEqual(["CardTraitFreeze discard routing"]),
                "Lost the original destroyed Conduit and newly drawn Freeze guard omissions.");
        }
        var rows = NativeBranchObservationChecks.Replay(source, Reason, Historical);
        Require(rows.Length == 29 && rows.Count(row => row.GetProperty("IsEndTurn").GetBoolean()) == 5,
            "Missing the complete five-turn native prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 4 && before.Spawn.Train.Rooms[0].Units.Any(unit => unit.Id == 2 && unit.Health == 14) &&
            actual.Spawn.Turn == 5 && !actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == 2) &&
            actual.Spawn.Train.Context!.Cards.Hand.Select(card => card.InstanceId).SequenceEqual([26, 16, 5, 17, 14]) &&
            CardBranchMaskModel.Resolve(actual.Spawn.Train.Context.FindCard(14)!).Traits.Contains("CardTraitFreeze"),
            "Lost champion death, draw order or the Permafrost draw callback witness.");
        var phases = counterexample.GetProperty("TrainPhases");
        var phase = phases[8];
        var removed = phase.GetProperty("Actual").Deserialize<TrainCombatState>()!.Context!.Enchantments!.RetainedUnits
            .Single(actor => actor.Unit.Id == 2).Unit;
        Require(removed.DeathState?.IsDestroyed == true && removed.Health == 0 &&
            phase.GetProperty("Predicted").GetProperty("State").Deserialize<TrainCombatState>()!.Context!.ConduitRetained!
                .Any(row => row.Contributor.UnitId == 2), "Missing raw combat destruction and stale tracker evidence.");
        var oldLast = counterexample.GetProperty("RollingDecisions")[28];
        Historical(oldLast.GetProperty("Expected").Deserialize<BattleTurnState>()!, oldLast.GetProperty("Actual").Deserialize<BattleTurnState>()!);
        var ascendBefore = phases[9].GetProperty("Before").Deserialize<TrainCombatState>()!.Context!.Enchantments!;
        var ascendActual = phases[9].GetProperty("Actual").Deserialize<TrainCombatState>()!.Context!.Enchantments!;
        Require(ascendBefore.RetainedUnits.Any(actor => actor.Preview && actor.Unit.Id == 32 &&
            actor.Unit.DeathState is { IsDestroyed: true, HasStatisticsListener: true }) && new[] { 13, 24 }.All(id =>
                ascendBefore.RetainedUnits.Single(actor => actor.Unit.Id == id).Unit.Modifiers!.Upgrades.Count == 1 &&
                ascendActual.RetainedUnits.Single(actor => actor.Unit.Id == id).Unit.Modifiers!.Upgrades.Count == 0 &&
                ascendActual.RetainedUnits.Single(actor => actor.Unit.Id == id).Unit.LastAttackerId == 0) &&
            !ascendActual.RetainedUnits.Single(actor => actor.Unit.Id == 13).Unit.Triggers[0].Effects[0].Enchantment!.Bound,
            "Lost pending-preview frame completion of real dead actors and the retained enchantment owner.");
        TrainCombatChecks.Native(counterexample);

        BattleTurnState current = source.GetProperty("Request").GetProperty("Root").Deserialize<BattleTurnState>()!;
        BattleTurnState ownedBefore = null!;
        foreach (var row in rows)
        {
            ownedBefore = current;
            current = BattlePlannerChecks.Replay(current, [new BattleSearchStep(row.GetProperty("Action").Deserialize<PlayCardAction>(),
                row.GetProperty("IsEndTurn").GetBoolean())], out var outcome);
            Require(outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(current) ==
                NativeBranchObservationChecks.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!),
                "Continuous model-owned replay differs from native.");
        }
        var retainedContext = phase.GetProperty("Predicted").GetProperty("State").Deserialize<TrainCombatState>()!.Context!;
        var death = removed.DeathState!;
        var pending = removed.WithDeathState(new UnitDeathState(death.HasFinishedDying, death.IsBeingRemoved,
            death.HasStatisticsListener, death.PendingStatisticsCardId, death.IsSacrifice, death.StatisticsListenerOnce,
            death.IsDespawned, false));
        string retainedParent = JsonSerializer.Serialize(retainedContext, ModelJson.Options);
        Require(ReferenceEquals(retainedContext, ConduitModel.Destroyed(retainedContext, [pending])) &&
            ConduitModel.Destroyed(retainedContext, [removed]).ConduitRetained!.Count == 0 &&
            retainedParent == JsonSerializer.Serialize(retainedContext, ModelJson.Options),
            "Cleanup confused pending removal with destruction or mutated its parent.");
        string parent = JsonSerializer.Serialize(ownedBefore, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var end = BattleTurnModel.EndTurn(ownedBefore);
            Require(end.Supported && NativeBranchObservationChecks.Key(end.State!) == NativeBranchObservationChecks.Key(actual) &&
                parent == JsonSerializer.Serialize(ownedBefore, ModelJson.Options), "Immutable EndTurn branch changed state or RNG.");
        });
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("CONDUIT-RETIREMENT PASS:29 native inputs, continuously owned replay/full state/RNG, raw combat phases, " +
            "destroyed versus pending trackers, Permafrost draw guard and four immutable EndTurn branches; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
