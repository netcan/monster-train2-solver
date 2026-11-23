using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DoubleUnitsTriggeredChecks
{
    private const string Vessel = "057f7f77-df4b-4981-9e6d-533a6e667f39";
    private const string Pupa = "e2f7a12a-b33b-4dbe-b95b-73f39cdf3a70";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("Boundary").GetString() == "NativeOriginalDoubleUnitsTriggeredSummon" &&
            root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            root.GetProperty("CaptureFailures").GetInt32() == 0 && root.GetProperty("Pending").GetInt32() == 0,
            "Expected a settled 2.3.1 DoubleUnits triggered summon component.");

        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        var turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        Require(actions.Length == 2 && actions.All(row => Decision(row, "Before").Spawn.Turn == 0) &&
            turns.Length == 2 && Decision(turns[0], "Before").Spawn.Turn == 0 && Decision(turns[1], "Before").Spawn.Turn == 1,
            "Expected two deployment inputs and no paid input on the hatch turn.");
        var queries = root.GetProperty("ExtraSpawnCountQueries").EnumerateArray().Select(row => (
            row.GetProperty("Unit").GetString(), row.GetProperty("Team").GetString(), row.GetProperty("Turn").GetInt32(),
            row.GetProperty("CountBefore").GetInt32(), row.GetProperty("CountAfter").GetInt32())).ToArray();
        Require(queries.SequenceEqual(new[] { ("TrainStewardBig", "Monsters", 0, 1, 2),
            ("MonsterKinhostVessel", "Monsters", 0, 1, 1), ("MonsterKinhostPupa", "Monsters", 1, 1, 2) }),
            "Native deployment guard, reset, or first triggered count query differs.");

        FixtureValue hatch = root.GetProperty("TriggeredSummons").EnumerateArray().Single();
        var egg = hatch.GetProperty("ActorBefore").Deserialize<CombatUnit>()!;
        var retired = hatch.GetProperty("ActorAfter").Deserialize<CombatUnit>()!;
        var before = hatch.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var after = hatch.GetProperty("After").Deserialize<RoomCombatState>()!;
        Require(hatch.GetProperty("Kind").GetString() == "OnHatched" && hatch.GetProperty("Completed").GetBoolean() &&
            hatch.GetProperty("Error").ValueKind == FixtureKind.Null && hatch.GetProperty("FullQueued").GetArrayLength() == 0 &&
            hatch.GetProperty("RuleBefore").GetProperty("CharacterId").GetString() == Pupa &&
            before.RoomIndex == 2 && after.RoomIndex == 2 && egg.AssetKey == "MonsterKinhostVessel" &&
            egg.Team == CombatTeam.Player && retired.SacrificeCardId == egg.SpawnerCardId &&
            before.Context!.FindCard(egg.SpawnerCardId)?.DataId == Vessel,
            "Expected the unchanged original Vessel hatch with its source ownership.");

        CombatContext prior = before.Context!, actual = after.Context!;
        var born = after.Units.Where(unit => before.Units.All(existing => existing.Id != unit.Id)).OrderBy(unit => unit.Id).ToArray();
        var points = actual.SpawnPoints!.Group(after.RoomIndex, CombatTeam.Player)!;
        Require(born.Length == 2 && born.All(unit => unit is { AssetKey: "MonsterKinhostPupa", Team: CombatTeam.Player } &&
                unit.Status("cardless")?.Stacks == 1 && actual.FindCard(unit.SpawnerCardId)?.DataId == Vessel) &&
            born.Select(unit => unit.SpawnerCardId).Distinct().Count() == 2 &&
            points.Occupants.Take(2).SequenceEqual(born.Select(unit => unit.Id)) &&
            prior.BattleRng.Equals(actual.BattleRng) && prior.Cards.Rng.Equals(actual.Cards.Rng),
            "Native hatch child identities, positions, source cards, or RNG differ.");
        Require(Condition(prior) is { Triggered: false, DurationTriggerCount: 0, ValueAtLastTrigger: 0 } &&
            Condition(actual) is { Triggered: true, DurationTriggerCount: 1, ValueAtLastTrigger: 0 } &&
            Condition(Decision(turns[1], "Actual").Spawn.Train.Context!) is { Triggered: false, DurationTriggerCount: 0 },
            "Native hatch did not consume exactly one turn-scoped trigger before the next draw.");

        var result = BattleTurnModel.EndTurn(Decision(turns[1], "Before"));
        Require(result.Supported && result.Outcome == turns[1].GetProperty("ActualOutcome").Deserialize<RoomOutcome>(),
            result.UnsupportedReason ?? "Triggered summon outcome differs from native.");
        RecordedBattleChecks.Compare(result.State!, Decision(turns[1], "Actual"), false, "Original DoubleUnits hatch EndTurn");
        Console.WriteLine("NATIVE-DOUBLE-UNITS-TRIGGERED PASS: two original hatchlings, turn-scoped count and exact EndTurn state/RNG.");
    }

    private static RelicConditionState Condition(CombatContext context) => context.Relics!
        .Single(owner => owner.DataId == RelicUnitSpawnCountModel.DataId).ResourceEffects!.Single().Conditions.Single();

    private static BattleTurnState Decision(FixtureValue row, string field) => row.GetProperty(field).Deserialize<BattleTurnState>()!;

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
