using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MoreMassMoonBirthChecks
{
    private const string Reason = "Outer-boss instantiation requires the calibrated original definition without creation modifiers or moon tracking.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 2 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing natural champion/first-EndTurn boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var definition = before.Spawn.QueuedOuterBoss!;
        var actor = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == actual.Spawn.OuterBoss!.UnitId);
        Require(before.MoonPhaseTrackingActive == true && before.MoonPhase == "New" && actual.MoonPhase == "Full" &&
            actual.Spawn.QueuedOuterBoss == null && actor is { Id: 5, AssetKey: "Boss_Level3_Fleshweaver_MoreMass", BaseAttack: 6, Attack: 22,
                Health: 600, MaxHealth: 600, MoonPhaseMask: 6 } && actor.Status("mass")?.Stacks == 8 && actor.Status("mass")?.ParamInt == 2 &&
            actor.Status("fullmoon")?.Stacks == 1 && actor.StatusRegistry!.Select(status => status.Id).SequenceEqual(new[] { "mass", "fullmoon" }),
            "Native MoreMass must retain authored eight mass,600 health/22 attack and ordered full-moon initialization before casting.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Detached MoreMass moon birth differs.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "MoreMass birth changed its parent.");
        // Check the instantiation boundary itself in the same full-moon frame.
        var frame = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        frame["MoonPhase"] = "Full";
        frame["Spawn"]!["Train"]!["Context"]!["QueryFrame"]!["MoonPhase"] = 2;
        var createdFrame = frame.Deserialize<BattleTurnState>(ModelJson.Options)!;
        Require(OuterBossInstantiationModel.Apply(new(createdFrame, definition)).Supported,
            "The known native definition must be admitted in its creation moon frame.");
        foreach (string control in new[] { "Asset", "Health", "Attack", "Mass", "Trigger" })
        {
            var changed = JsonSerializer.SerializeToNode(definition, ModelJson.Options)!;
            if (control == "Asset") changed["Actor"]!["Unit"]!["AssetKey"] = "Boss_Level3_UnknownMass";
            if (control == "Health") changed["Actor"]!["Unit"]!["Health"] = 601;
            if (control == "Attack") changed["Actor"]!["Unit"]!["BaseAttack"] = 7;
            if (control == "Mass") changed["Actor"]!["Unit"]!["Statuses"]![0]!["Stacks"] = 9;
            if (control == "Trigger") changed["Actor"]!["Unit"]!["Triggers"]![0]!["Kind"] = "OnMoonLit";
            var result = OuterBossInstantiationModel.Apply(new(createdFrame, changed.Deserialize<OuterBossSpawnDefinition>(ModelJson.Options)!));
            Require(!result.Supported, "Unknown MoreMass moon payload was admitted: " + control);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("MOREMASS-MOON-BIRTH PASS:two native inputs/full state/RNG,600HP/eight mass/22 attack, identity5 and mass/fullmoon registration, four immutable branches/five guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
