using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AngelDuoDeathChecks
{
    internal static void Battle(FixtureValue fixture)
    {
        foreach (string name in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(name).GetInt32() == 0, "Controlled Angel Duo battle failed: " + name);
        Require(!fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Controlled death baselines must preserve the settled native no-card loss.");
        FixtureValue input = fixture.GetProperty("NativeRunInput");
        Require(input.GetProperty("AscensionLevel").GetInt32() == 2 && input.GetProperty("Distance").GetInt32() == 5 &&
            input.GetProperty("BattlesWon").GetInt32() == 5 && !input.GetProperty("WasDevCheatUsed").GetBoolean() &&
            input.GetProperty("Scenario").GetString() == "Level6AngelDuo_RallyAttackIncantHealth",
            "Controlled death battle must retain its original progressed rank-two input.");
        FixtureValue transition = fixture.GetProperty("AngelDuoDeathTransition");
        Native(transition);
        FixtureValue turns = fixture.GetProperty("Turns");
        var root = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var control = transition.GetProperty("Original").Deserialize<BattleTurnState>()!;
        int id = transition.GetProperty("TargetId").GetInt32(), room = transition.GetProperty("Room").GetInt32();
        string immutable = Json(root);
        void Chain()
        {
            BattleTurnState state = root;
            bool applied = false;
            for (int index = 0; index < turns.GetArrayLength(); index++)
            {
                if (!applied && state.Spawn.Turn == control.Spawn.Turn)
                {
                    Require(BattleTurnChecks.Comparable(state) == BattleTurnChecks.Comparable(control),
                        "Independent chain differs before the explicit native health control.");
                    TrainCombatState oldTrain = state.Spawn.Train;
                    var controlled = new TrainCombatState(oldTrain.Rooms.Select(floor => floor.WithUnits(floor.Units
                        .Select(unit => unit.Id == id ? unit.WithHealth(1) : unit).ToArray())).ToArray(),
                        oldTrain.Movement, oldTrain.EnemySlotsPerRoom, oldTrain.Context);
                    controlled = EnchantmentWorldModel.Rebase(controlled);
                    state = ReplaceTrain(state, controlled);
                    Require(BattleTurnChecks.Comparable(state) == BattleTurnChecks.Comparable(
                        transition.GetProperty("Before").Deserialize<BattleTurnState>()!),
                        "Independent chain differs after the explicit native health control.");
                    state = Apply(state, room, id); applied = true;
                    Require(BattleTurnChecks.Comparable(state) == BattleTurnChecks.Comparable(
                        transition.GetProperty("Actual").Deserialize<BattleTurnState>()!),
                        "Independent chain differs after the complete native death transition.");
                }
                BattleTurnResult result = BattleTurnModel.EndTurn(state);
                Require(result.Supported, "Independent controlled death chain: " + result.UnsupportedReason);
                var actual = turns[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
                bool terminal = result.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
                string predicted = BattleTurnChecks.Comparable(result.State!, terminal);
                string expected = BattleTurnChecks.Comparable(actual, terminal);
                Require(result.Outcome == (RoomOutcome)turns[index].GetProperty("ActualOutcome").GetInt32() &&
                    predicted == expected, "Independent controlled death chain differs at index " + index + ": " +
                    ModelJson.Difference(predicted, expected));
                Require(terminal == (index == turns.GetArrayLength() - 1), "Controlled chain terminated at the wrong boundary.");
                state = result.State!;
            }
            Require(applied && Json(root) == immutable, "Controlled chain omitted the explicit death or changed its shared root.");
        }
        Chain(); Parallel.For(0, 16, _ => Chain());
        Console.WriteLine("ANGEL-DUO-DEATH-BATTLE PASS: " + turns.GetArrayLength() +
            " native turns, explicitly simulated health/death control and sixteen independent complete battle branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 1 &&
            fixture.GetProperty("Control").GetProperty("Kind").GetString() == "OriginalAngelDuoDebuffHealthToOneAndNativeDamage" &&
            !fixture.GetProperty("Control").GetProperty("NaturalVictory").GetBoolean(),
            "Angel Duo death fixture must disclose its native health/damage controls.");
        var before = fixture.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = fixture.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        int target = fixture.GetProperty("TargetId").GetInt32(), room = fixture.GetProperty("Room").GetInt32();
        CombatUnit killed = before.Spawn.Train.Rooms.SelectMany(floor => floor.Units).Single(unit => unit.Id == target);
        Require(killed.Health == 1 && killed.BumpRules?.CompanionBoss == true &&
            before.Spawn.Train.Context!.QueryFrame!.ForcedRelentless != null &&
            before.Spawn.Turn == 1 && before.Spawn.Phase < before.Spawn.Waves.Count - 1,
            "Controlled death must start before natural Relentless with one original paired boss at one HP.");
        string immutable = Json(before), expected = Json(actual);
        BattleTurnState predicted = Apply(before, room, target);
        string? difference = ModelJson.Difference(Json(predicted), expected);
        Require(difference == null, "Angel Duo native death full state differs: " + difference);
        CombatUnit survivor = actual.Spawn.Train.Rooms.SelectMany(floor => floor.Units)
            .Single(unit => unit.Id == killed.BumpRules!.PairedBossUnitId);
        Require(survivor.BumpRules?.PairedBossUnitId == 0 && survivor.EndsBattleOnDeath &&
            actual.Spawn.Turn == actual.Spawn.Waves.Count - 1 && actual.Spawn.Phase == actual.Spawn.Turn &&
            actual.Spawn.Train.Context!.QueryFrame!.ForcedRelentless!.RemainingTurns == actual.Spawn.Turn - before.Spawn.Turn &&
            survivor.Status(killed.BumpRules!.OuterBoss ? "titanskin" : "multistrike")?.Stacks ==
                (killed.BumpRules.OuterBoss ? 10 : 1),
            "Native companion death must apply the original filtered status, clear pairing and force native counters.");
        Require(fixture.GetProperty("CardsReadyBefore").GetBoolean() && fixture.GetProperty("CardsReadyAfter").GetBoolean() &&
                !fixture.GetProperty("CardsDrawnAfter").GetBoolean(),
                "Native readiness must retain the drawn hand across the forced turn jump.");
        Parallel.For(0, 16, _ => Require(Json(Apply(before, room, target)) == expected && Json(before) == immutable,
            "Companion death branch changed complete native state/RNG or its input."));
        Console.WriteLine("ANGEL-DUO-DEATH PASS: original filtered death buffs, pairing removal, forced turn/spawn/scoring, complete battle state/RNG and sixteen immutable branches.");
    }

    private static BattleTurnState Apply(BattleTurnState before, int room, int target)
    {
        var hit = CardSpellModel.ApplyNativeDamageStep(before.Spawn.Train, room, target, 100, 0,
            Array.Empty<RoomCombatModel.QueuedCharacterTrigger>());
        Require(hit.Supported, "Angel Duo native damage: " + hit.UnsupportedReason);
        var removed = TrainCombatModel.ApplyCharacterQueue(hit.State!, hit.PendingCallbacks.ToList());
        Require(removed.Supported, "Angel Duo native removal: " + removed.UnsupportedReason);
        TrainCombatState train = TrainCombatModel.ProcessRemovals(removed.State!);
        return ReplaceTrain(before, train);
    }

    private static BattleTurnState ReplaceTrain(BattleTurnState before, TrainCombatState train)
    {
        EnemySpawnState old = before.Spawn;
        var spawn = ForcedRelentlessModel.Synchronize(new EnemySpawnState(train, old.Waves, old.SelectedGroups,
            old.Phase, old.Looping, old.Rng, old.NextUnitId, old.Treasures, old.TreasuresRemaining, old.TreasureEnabled,
            old.FirstTreasureTurn, old.FirstTreasureRoom, old.Turn, old.ExternalInteractions, old.CanonicalDecisionReferences,
            old.PendingDestroyedUnitIds, old.OuterBoss, old.QueuedOuterBoss, old.QueuedTrueFinalBoss, old.TrueFinalBoss));
        return new BattleTurnState(spawn, before.Energy, before.EnergyPerTurn, before.DrawPerTurn, before.ForgePoints,
            before.DragonsHoard, before.MoonPhase, before.RngStreams.Select(stream => new BattleRngStream(stream.Name,
                stream.Seed, stream.Name == "Battle" ? train.Context!.BattleRng : stream.State)).ToArray(),
            before.OtherPiles, before.ExternalInteractions, before.PlayRules, before.BattlePreviewEnabled, before.UiRngIsolated,
            before.CanonicalDecisionReferences, before.CanonicalPhysicalReferences,
            train.Context!.QueryFrame?.SelectedRoomIndex ?? before.SelectedRoom, before.MoonPhaseTrackingActive);
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
