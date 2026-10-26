using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RetiredActorFrameChecks
{
    internal static void Native(FixtureValue source, FixtureValue ownerDeath, FixtureValue? replay = null)
    {
        Require(source.GetProperty("Mismatches").GetInt32() == 1 && source.GetProperty("CaptureFailures").GetInt32() == 0 &&
            !source.GetProperty("NativeWon").GetBoolean(), "Preserve original one-phase mismatch and actual defeat.");
        var phase = source.GetProperty("TrainPhases")[17];
        var before = phase.GetProperty("Before").Deserialize<TrainCombatState>()!;
        var historical = phase.GetProperty("Predicted").GetProperty("State").Deserialize<TrainCombatState>()!;
        var actual = phase.GetProperty("Actual").Deserialize<TrainCombatState>()!;
        Require(phase.GetProperty("Kind").GetString() == "Ascend" && phase.GetProperty("Turn").GetInt32() == 8 &&
            new[] { 48, 50 }.All(id => before.Context!.Enchantments!.RetainedUnits.Any(actor => actor.Unit.Id == id &&
                actor.Preview && actor.Unit.DeathState?.IsDestroyed == true && actor.Unit.Modifiers!.Upgrades.Count == 1) &&
                historical.Context!.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == id).Unit.Modifiers!.Upgrades.Count == 1 &&
                actual.Context!.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == id).Unit.Modifiers!.Upgrades.Count == 0) &&
            historical.Context!.SpawnPoints!.Units.Single(point => point.UnitId == 24).LastKnown != null &&
            actual.Context!.SpawnPoints!.Units.Single(point => point.UnitId == 24).LastKnown == null,
            "Lost the actual pre-destroyed players or the retired physical pointer witness.");
        TrainCombatChecks.Native(source);
        RollingAdviceChecks.ObservedSuccessors(source);
        OriginalSupportSpellsChecks.Native(ownerDeath);
        if (replay.HasValue)
        {
            Require(replay.Value.GetProperty("Mismatches").GetInt32() == 0 &&
                replay.Value.GetProperty("CaptureFailures").GetInt32() == 0, "Independent native frame replay still mismatches.");
            var independentPhase = replay.Value.GetProperty("TrainPhases")[17];
            var independentBefore = independentPhase.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var independentActual = independentPhase.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            Require(independentPhase.GetProperty("Kind").GetString() == "Ascend" &&
                independentPhase.GetProperty("Turn").GetInt32() == 8 && new[] { 48, 50 }.All(id =>
                    independentBefore.Context!.Enchantments!.RetainedUnits.Any(actor => actor.Unit.Id == id &&
                        actor.Preview && actor.Unit.DeathState?.IsDestroyed == true && actor.Unit.Modifiers!.Upgrades.Count == 1) &&
                    independentActual.Context!.Enchantments!.RetainedUnits.Any(actor => actor.Unit.Id == id &&
                        actor.Unit.DeathState is { IsDestroyed: true, HasStatisticsListener: false } && actor.Unit.Modifiers!.Upgrades.Count == 0)),
                "Independent replay omitted the pending destruction frame witness.");
            TrainCombatChecks.Native(replay.Value);
        }
        Console.WriteLine("RETIRED-ACTOR-FRAME PASS:22 original train phases and43 actual native inputs recomputed, original phase17 mismatch retained; " +
            "old treasure retirement and Squad Leader death chain restored; independentFrame=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
