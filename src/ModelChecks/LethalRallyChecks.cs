using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LethalRallyChecks
{
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out var scenario) || scenario.GetString() != "rally-lethal") return;
        var phases = fixture.GetProperty("RallyPhases").EnumerateArray().ToArray();
        var triggers = fixture.GetProperty("RallyTriggers").EnumerateArray().ToArray();
        if (!phases.Any(sample => sample.GetProperty("Before").Deserialize<TrainCombatState>()!.Context!.AllScenarioBossesDead == false &&
                sample.GetProperty("After").Deserialize<TrainCombatState>()!.Context!.AllScenarioBossesDead == true) ||
            !triggers.Any(sample => sample.GetProperty("Actor").Deserialize<CombatUnit>()!.EndsBattleOnDeath &&
                sample.GetProperty("AfterActor").Deserialize<CombatUnit>()!.DeathState is
                    { HasFinishedDying: true, IsBeingRemoved: false, HasStatisticsListener: false }))
            throw new InvalidOperationException("Lethal Rally lacks a live-to-terminal phase or a finished Boss death before physical removal.");
        foreach (var phase in phases) RallyChecks.VerifyPhase(phase);
        foreach (var trigger in triggers) RallyChecks.VerifyTrigger(trigger);
        var terminal = fixture.GetProperty("Actions").EnumerateArray().Single(sample =>
            sample.GetProperty("ActualOutcome").Deserialize<RoomOutcome>() == RoomOutcome.BattleWon);
        var action = terminal.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var after = terminal.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var context = after.Spawn.Train.Context!;
        if (context.LastSpawnedUnitId <= 0 || context.Cards.Hand.Count != 0 || context.Cards.Draw.Count != 0 || context.Cards.Discard.Count != 0 ||
            context.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Single().InstanceId != action.CardInstanceId ||
            context.CardInstances!.Single().InstanceId != action.CardInstanceId)
            throw new InvalidOperationException("Lethal Rally did not retain the live summon reference and restore only its Standby card.");
        Parallel.For(0, 32, _ =>
        {
            foreach (var phase in phases) RallyChecks.VerifyPhase(phase);
            foreach (var trigger in triggers) RallyChecks.VerifyTrigger(trigger);
        });
        Console.WriteLine($"NATIVE-LETHAL-RALLY-CHECKS PASS: {phases.Length} complete team phases, {triggers.Length} actor/room dispatches, Boss self-death, post-kill upgrades/rewards, live last-spawned reference, terminal Standby and 32 branches.");
    }
    internal static void NativeTerminal(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = sample.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = sample.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var old = before.Spawn.Train.Context!;
        var context = after.Spawn.Train.Context!;
        var instance = old.FindCard(action.CardInstanceId)!;
        var unit = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == context.LastSpawnedUnitId);
        var restored = context.CardInstances!.Single();
        if (restored.InstanceId != action.CardInstanceId || restored.PlayCount != instance.PlayCount + 1 ||
            context.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Single().InstanceId != action.CardInstanceId ||
            context.Statistics!.Value(instance.InstanceId, "TimesPlayed") != old.Statistics!.Value(instance.InstanceId, "TimesPlayed") + 1 ||
            context.Statistics.Value(instance.InstanceId, "TimesDiscarded") != old.Statistics.Value(instance.InstanceId, "TimesDiscarded") + 1 ||
            context.Statistics.PlayedCosts.Count != 0 || unit.SpawnerCardId != instance.InstanceId || unit.Health <= 0)
            throw new InvalidOperationException("Terminal summon lost its completed card history, cleared cost, Standby routing or living unit.");
        if (!restored.Permanent.Upgrades.Any(upgrade => upgrade.DataId == "c2f6ed7f-18ce-4070-b65f-7dd9f5190011") ||
            !unit.Modifiers!.Upgrades.Any(upgrade => upgrade.DataId == "c2f6ed7f-18ce-4070-b65f-7dd9f5190012") || unit.Status("armor")?.Stacks != 1 ||
            context.Gold - old.Gold < 24)
            throw new InvalidOperationException("Boss-kill Rally skipped later permanent/unit-death upgrades or death/reward effects.");
        Console.WriteLine("NATIVE-TERMINAL-SUMMON-COVERAGE PASS: paid Rally Boss kill, surviving summon/reference, completed history, cleared cost and restored Standby card.");
    }
}
