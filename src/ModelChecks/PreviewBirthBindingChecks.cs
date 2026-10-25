using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class PreviewBirthBindingChecks
{
    internal static void Native(FixtureValue source, FixtureValue replay)
    {
        Require(source.GetProperty("ModifierScenario").GetString() == "persistent-enchantment-summons" &&
            replay.GetProperty("ModifierScenario").GetString() == "persistent-enchantment-summons" &&
            source.GetProperty("NativeWon").GetBoolean() && replay.GetProperty("NativeWon").GetBoolean() &&
            source.GetProperty("Mismatches").GetInt32() > 0 &&
            new[] { "CaptureFailures", "Unsupported", "Pending" }.All(key => source.GetProperty(key).GetInt32() == 0 &&
                replay.GetProperty(key).GetInt32() == 0),
            "Missing original mismatching observation or independent corrected native win.");
        // This component verifies every paid input and EndTurn. Preserve the
        // four independent intra-combat gaps rather than declaring a clean run.
        string[] StageGaps(FixtureValue fixture) => new[] { "Stages", "TrainPhases" }.SelectMany(name =>
            fixture.GetProperty(name).EnumerateArray().Where(row => row.GetProperty("Difference").ValueKind != FixtureKind.Null)
                .Select(row => name + ":" + row.GetProperty("Index").GetInt32() + ":" + row.GetProperty("Difference").GetString())).ToArray();
        string[] residual = StageGaps(replay);
        Require(residual.Length == 4 && StageGaps(source).SequenceEqual(residual) &&
            replay.GetProperty("Mismatches").GetInt32() == residual.Length &&
            new[] { "Actions", "Turns" }.SelectMany(name => replay.GetProperty(name).EnumerateArray())
                .All(row => row.GetProperty("Difference").ValueKind == FixtureKind.Null),
            "The four explicitly open intra-combat gaps changed or a corrected decision still mismatched.");
        int boundaries = 0;
        foreach (string collection in new[] { "Actions", "Turns" })
        {
            FixtureValue[] original = source.GetProperty(collection).EnumerateArray().ToArray();
            FixtureValue[] corrected = replay.GetProperty(collection).EnumerateArray().ToArray();
            Require(original.Length == corrected.Length && original.Length > 0, "Independent native input count changed.");
            for (int index = 0; index < original.Length; index++)
            {
                foreach (string field in new[] { "Before", "Actual" })
                {
                    BattleTurnState first = original[index].GetProperty(field).Deserialize<BattleTurnState>()!;
                    BattleTurnState second = corrected[index].GetProperty(field).Deserialize<BattleTurnState>()!;
                    Require(NativeBranchObservationChecks.Key(first) == NativeBranchObservationChecks.Key(second),
                        "Independent native " + collection + " " + field + " differs at " + index);
                }
                if (collection == "Actions") Require(original[index].GetProperty("Action").ContentEquals(corrected[index].GetProperty("Action")),
                    "Independent native card input changed.");
                BattleTurnState before = original[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
                string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
                BattleTurnState predicted;
                RoomOutcome outcome;
                if (collection == "Actions")
                {
                    BattleActionResult result = BattleActionModel.PlayCard(before, original[index].GetProperty("Action").Deserialize<PlayCardAction>()!);
                    Require(result.Supported, result.Reason ?? "Unsupported native paid input.");
                    predicted = result.State!; outcome = result.Outcome;
                }
                else
                {
                    BattleTurnResult result = BattleTurnModel.EndTurn(before);
                    Require(result.Supported, result.UnsupportedReason ?? "Unsupported native EndTurn.");
                    predicted = result.State!; outcome = result.Outcome;
                }
                bool terminal = outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
                string actualKey = NativeBranchObservationChecks.Key(original[index].GetProperty("Actual").Deserialize<BattleTurnState>()!, terminal);
                Require((int)outcome == original[index].GetProperty("ActualOutcome").GetInt32() &&
                    NativeBranchObservationChecks.Key(predicted, terminal) == actualKey &&
                    NativeBranchObservationChecks.Key(corrected[index].GetProperty("Predicted").GetProperty("State").Deserialize<BattleTurnState>()!, terminal) == actualKey &&
                    immutable == JsonSerializer.Serialize(before, ModelJson.Options),
                    "Full input/EndTurn forecast, state/RNG or parent differs at " + collection + index);
                if (collection != "Actions") continue;
                BattleTurnState historical = original[index].GetProperty("Predicted").GetProperty("State").Deserialize<BattleTurnState>()!;
                BattleTurnState actual = original[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
                string? difference = ModelJson.Difference(NativeBranchObservationChecks.Key(historical), NativeBranchObservationChecks.Key(actual));
                if (difference == null) continue;
                Require(difference.Contains(".Enchantment.Bound:", StringComparison.Ordinal), "Historical mismatch changed its boundary: " + difference);
                int[] missing = historical.Spawn.Train.Context!.Enchantments!.RetainedUnits.Where(actor =>
                    actor.Unit.Team == CombatTeam.Player && actor.Unit.DeathState?.IsDestroyed == true && Bound(actor.Unit) &&
                    actual.Spawn.Train.Context!.Enchantments!.RetainedUnits.Any(other => other.Unit.Id == actor.Unit.Id && !Bound(other.Unit)))
                    .Select(actor => actor.Unit.Id).ToArray();
                Require(missing.Length > 0 && missing.All(id => historical.Spawn.Train.Context.SpawnPoints!.Units
                    .Any(point => point.UnitId == id && point.SpawnedInPreview)), "Missing destroyed preview-birth binding witness.");
                if (boundaries++ == 0) Controls(historical.Spawn.Train, missing[0]);
            }
        }
        Require(boundaries > 0, "Original incorrect bound preview births were replaced.");
        Console.WriteLine("PREVIEW-BIRTH-BINDING PAIR PASS: unchanged native actions/turns/full state/RNG; " + boundaries +
            " preserved historical binding mismatches; corrected paid policy and independent native win; " + residual.Length +
            " unchanged intra-combat stage gaps remain OPEN.");
    }

    private static bool Bound(CombatUnit actor) => actor.Triggers.SelectMany(trigger => trigger.Effects).Any(effect => effect.Enchantment?.Bound == true);
    private static void Controls(TrainCombatState root, int id)
    {
        TrainCombatState Prepare(bool preview, bool destroyed)
        {
            CombatContext context = root.Context!;
            EnchantmentWorld world = context.Enchantments!;
            context = context.WithSpawnPoints(context.SpawnPoints!.WithState(context.SpawnPoints.Groups,
                context.SpawnPoints.Units.Select(point => point.UnitId == id ?
                    new UnitSpawnPointState(point.UnitId, point.Current, point.LastKnown, point.OuterBoss, preview) : point).ToArray()));
            context = context.WithEnchantments(new EnchantmentWorld(world.Rooms, world.Movement, world.EnemySlotsPerRoom,
                world.RetainedUnits.Select(actor => actor.Unit.Id == id ? new EnchantmentRetainedUnit(
                    actor.Unit.WithDeathState(actor.Unit.DeathState!.WithLifecycle(actor.Unit.DeathState.IsDespawned, destroyed)),
                    actor.RoomIndex, actor.Preview) : actor).ToArray(), world.EnchanterIds, world.AllowUpdates, world.Updating,
                world.Preview, world.TestRng, world.AutomaticLifecycle, world.TemporaryPreviewUnitIds));
            return CardSpellModel.WithContext(root, context);
        }
        bool IsBound(TrainCombatState state) => Bound(state.Context!.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == id).Unit);
        TrainCombatState pending = Prepare(true, true);
        string immutable = JsonSerializer.Serialize(pending, ModelJson.Options);
        TrainCombatState settled = TrainCombatModel.CompleteFrameRemovals(pending, []);
        Require(!IsBound(settled) && !IsBound(TrainCombatModel.CompleteFrameRemovals(settled, [])) &&
            immutable == JsonSerializer.Serialize(pending, ModelJson.Options), "Destroyed preview birth did not settle immutably and idempotently.");
        Require(IsBound(TrainCombatModel.CompleteFrameRemovals(Prepare(false, true), [])), "Real combat death was settled before manager removal.");
        Require(IsBound(TrainCombatModel.CompleteFrameRemovals(Prepare(true, false), [])), "Living retained preview birth was unbound.");
        Require(!IsBound(TrainCombatModel.CompleteFrameRemovals(Prepare(false, true), [id])), "Explicit frame destruction did not unbind the source.");
        Console.WriteLine("PREVIEW-BIRTH-BINDING CONTROLS PASS: immutable/idempotent preview removal, real death deferral, living retained actor and explicit destruction.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
