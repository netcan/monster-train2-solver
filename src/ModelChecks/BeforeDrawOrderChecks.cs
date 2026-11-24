using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BeforeDrawOrderChecks
{
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
    private const string EnergyOwner = "970f699b-052e-4eec-ad98-ce32b9c5dc21";
    private const string HealOwner = "dbd5aec6-5046-4df2-ae61-4b646b8d1702";
    internal static void Battle(string path, bool componentOnly = false)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("BeforeDrawOrderPrepared").GetBoolean() && root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("BeforeDrawOrderDefinition").ContentEquals(root.GetProperty("BeforeDrawOrderDefinitionAfter")),
            "Before-draw ordering lacks unchanged original definitions or pinned native version.");
        bool healFirst = root.GetProperty("BeforeDrawOrderHealFirst").GetBoolean();
        var definitions = root.GetProperty("BeforeDrawOrderDefinition").EnumerateArray().ToArray();
        Require(definitions.Length == 2 && definitions.Select(row => row.GetProperty("Id").GetString()).SequenceEqual(
            healFirst ? new[] { HealOwner, EnergyOwner } : new[] { EnergyOwner, HealOwner }), "Original owner order was not preserved.");
        bool playerAdvice = root.TryGetProperty("PlayerAdvisor", out var playerEvidence) && playerEvidence.ValueKind == FixtureKind.Object;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length >= 6 && (playerAdvice || actions.Length == 6) && actions.Take(6)
            .All(action => action.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32() == 0),
            "Ordering controls must wound all three champions during deployment.");
        var first = root.GetProperty("Turns").EnumerateArray().First();
        var before = first.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = first.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var defenders = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre).ToArray();
        Require(defenders.Length == 3 && defenders.All(unit => unit.Health == unit.MaxHealth - 1 && unit.Subtypes.Contains(RelicStartTurnHealModel.ChampionSubtype)),
            "Missing actual native three-floor injured champion controls.");
        Require(actual.Energy == (healFirst ? 3 : 4) && actual.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre).All(unit => unit.Health == unit.MaxHealth),
            "Native owner order failed to distinguish reward-before-heal from heal-before-reward.");
        var callbacks = root.GetProperty("BeforeDrawOrderCallbacks").EnumerateArray().ToArray();
        Require(callbacks.Length > 0 && callbacks.All(row => row.GetProperty("Completed").GetBoolean()), "Native ordering callback observation incomplete.");
        var healing = callbacks.First(row => row.GetProperty("Turn").GetInt32() == 1 && row.GetProperty("Type").GetString() == RelicStartTurnHealModel.Effect);
        Require(healing.GetProperty("Passed").GetBoolean() && healing.GetProperty("Before").GetProperty("Energy").GetInt32() == (healFirst ? 3 : 4) &&
            healing.GetProperty("After").GetProperty("Actors").EnumerateArray().All(actor => actor.GetProperty("Health").GetInt32() == actor.GetProperty("Maximum").GetInt32()),
            "Native heal callback did not see its correct incoming energy or complete healing.");
        if (playerAdvice)
        {
            var runs = root.GetProperty("PlannerRuns").EnumerateArray().ToArray();
            Require(runs.Length == 1, "Real player advisor did not capture one frozen recommendation.");
            var run = runs[0];
            var planRoot = run.GetProperty("Root").Deserialize<BattleTurnState>()!;
            var steps = run.GetProperty("Steps").EnumerateArray().ToArray();
            var expected = run.GetProperty("Expected").EnumerateArray().ToArray();
            var observed = run.GetProperty("Actual").EnumerateArray().ToArray();
            var champions = planRoot.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre).ToArray();
            Require(run.GetProperty("Mode").GetString() == "search" && run.GetProperty("Completed").GetBoolean() &&
                run.GetProperty("Difference").ValueKind == FixtureKind.Null && steps.Length > 0 &&
                expected.Length == steps.Length && observed.Length == steps.Length &&
                planRoot.Spawn.Turn == 1 && planRoot.Energy == actual.Energy && champions.Length == 3 &&
                champions.All(unit => unit.Health == unit.MaxHealth && unit.Subtypes.Contains(RelicStartTurnHealModel.ChampionSubtype)),
                "Real player advisor lacked the ordered native first-turn root or a completed plan.");
            var overlay = playerEvidence.GetProperty("Overlay");
            Require(playerEvidence.GetProperty("Assembly").GetString() == "MonsterTrain2Poju.Advisor" &&
                playerEvidence.GetProperty("InteractionsPassed").GetBoolean() &&
                playerEvidence.GetProperty("IdlePassed").GetBoolean() &&
                playerEvidence.GetProperty("PlanPreviewPassed").GetBoolean() &&
                overlay.GetProperty("PublicationRootChecks").GetInt32() > 0 &&
                overlay.GetProperty("Publications").EnumerateArray().Any(row => row.GetProperty("Turn").GetInt32() == 1) &&
                overlay.GetProperty("Published").GetInt32() >= 4 && overlay.GetProperty("Invalidations").GetInt32() >= 3 &&
                overlay.GetProperty("PanelVertices").GetInt32() > 0 && overlay.GetProperty("PanelPixels").GetInt32() >= 1000 &&
                overlay.GetProperty("PurityFailures").GetInt32() == 0 && overlay.GetProperty("Failures").GetInt32() == 0,
                "Real player advice was not published at the current root.");
            for (int index = 0; index < steps.Length; index++)
                Require(BattleTurnChecks.Comparable(expected[index].Deserialize<BattleTurnState>()!) ==
                    BattleTurnChecks.Comparable(observed[index].Deserialize<BattleTurnState>()!),
                    $"Real player advice differs from native after step {index + 1}.");
        }
        VerifyPair(before);
        if (componentOnly)
        {
            Console.WriteLine($"BEFORE-DRAW-PAIR PASS healFirst={healFirst} nativeFirstEnergy={actual.Energy}; original native pair, both immutable orders/16 parallel pairs and per-effect selection.");
            return;
        }
        CardCycleChecks.Native(root); SolverStageChecks.Native(path);
        Console.WriteLine($"BEFORE-DRAW-ORDER PASS healFirst={healFirst} nativeFirstEnergy={actual.Energy}; full roster retained, both immutable orders/16 parallel pairs, complete successors/16 branches/frontiers/serial-parallel root search.");
    }

    private static void VerifyPair(BattleTurnState source)
    {
        var initial = source.Spawn.Train; var context = initial.Context!;
        string parent = JsonSerializer.Serialize(source, ModelJson.Options);
        var energy = context.Relics!.Single(owner => owner.DataId == EnergyOwner);
        var healing = context.Relics!.Single(owner => owner.DataId == HealOwner);
        int[] positions = context.Relics.Select((owner, index) => (owner, index)).Where(pair => pair.owner.DataId == EnergyOwner || pair.owner.DataId == HealOwner).Select(pair => pair.index).ToArray();
        void Verify()
        {
            foreach (bool healFirst in new[] { false, true })
            {
                var owners = context.Relics.ToArray(); owners[positions[0]] = healFirst ? healing : energy; owners[positions[1]] = healFirst ? energy : healing;
                var frame = context.WithRelics(owners).WithQueryFrame(context.QueryFrame!.With(energy: 3, turn: 1));
                frame = EnergyModel.SetPhase(frame, "MonsterTurn");
                var train = new TrainCombatState(initial.Rooms.Select(room => new RoomCombatState(room.RoomIndex, false, room.Units,
                    room.ExternalInteractions, frame, false, room.ActiveCompanionBossIds)).ToArray(), initial.Movement, initial.EnemySlotsPerRoom, frame);
                var steps = new List<RelicTurnPhaseStep>();
                var result = RelicTurnPhaseModel.BeforeDraw(train, source.PlayRules, steps);
                Require(result.Supported, result.UnsupportedReason ?? "Ordered before-draw callback dispatch failed.");
                Require(result.State!.Context!.QueryFrame!.Energy == (healFirst ? 3 : 4) && result.State.Context.Relics!.Count == owners.Length &&
                    steps.Where(step => step.Type == RelicDamagedFloorEnergyModel.Effect || step.Type == RelicStartTurnHealModel.Effect).Select(step => step.Type).SequenceEqual(
                        healFirst ? new[] { RelicStartTurnHealModel.Effect, RelicDamagedFloorEnergyModel.Effect } : new[] { RelicDamagedFloorEnergyModel.Effect, RelicStartTurnHealModel.Effect }),
                    "Dispatch grouped callbacks by family, dropped passive owners, or repeated a selected callback.");
                Require(result.State.Rooms.SelectMany(room => room.Units).Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre).All(unit => unit.Health == unit.MaxHealth),
                    "Ordered healing failed to carry actor state forward.");

                var previewTrain = new TrainCombatState(train.Rooms.Select(room => new RoomCombatState(room.RoomIndex,
                    false, room.Units, room.ExternalInteractions, frame, true, room.ActiveCompanionBossIds)).ToArray(),
                    train.Movement, train.EnemySlotsPerRoom, frame);
                var preview = RelicTurnPhaseModel.BeforeDraw(previewTrain, source.PlayRules);
                Require(preview.Supported && preview.State!.Context!.QueryFrame!.Energy == 3 &&
                    preview.State.Rooms.SelectMany(room => room.Units).Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre)
                        .All(unit => unit.Health == unit.MaxHealth - 1),
                    "Preview ran the original non-preview energy or healing callback.");
            }
        }
        var simpleRules = new[] {
            new RelicResourceEffect(0, RelicResourceModel.Energy, 1, 0, 1, false),
            new RelicResourceEffect(1, RelicResourceModel.Energy, 2, 0, 2, false) };
        var composite = new CombatRelicState("arbitrary-composite", "arbitrary-composite", simpleRules.Select(rule => rule.Type).ToArray(),
            isCovenant: false, disallowedInPlacementPhase: false, resourceEffects: simpleRules);
        var compositeContext = context.WithRelics(new[] { composite }).WithQueryFrame(context.QueryFrame!.With(energy: 3, turn: 1));
        compositeContext = EnergyModel.SetPhase(compositeContext, "MonsterTurn");
        var compositeTrain = new TrainCombatState(initial.Rooms.Select(room => new RoomCombatState(room.RoomIndex, false, room.Units,
            room.ExternalInteractions, compositeContext, false, room.ActiveCompanionBossIds)).ToArray(), initial.Movement, initial.EnemySlotsPerRoom, compositeContext);
        var compositeSteps = new List<RelicTurnPhaseStep>();
        var compositeResult = RelicTurnPhaseModel.BeforeDraw(compositeTrain, source.PlayRules, compositeSteps);
        Require(compositeResult.Supported && compositeResult.State!.Context!.QueryFrame!.Energy == 6 &&
            compositeSteps.Select(step => step.After.Context!.QueryFrame!.Energy).SequenceEqual(new int?[] { 4, 6 }),
            "Same-owner callbacks must execute once each in native effect-index order.");
        Verify(); Parallel.For(0, 16, _ => Verify());
        Require(parent == JsonSerializer.Serialize(source, ModelJson.Options), "Independent ordering branches modified their native parent.");
    }
}
