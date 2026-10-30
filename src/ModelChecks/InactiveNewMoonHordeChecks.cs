using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveNewMoonHordeChecks
{
    private const string Reason = "Inactive movement moon understanding requires native calibration.";

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit unit = before.Spawn.Train.Rooms[2].Units.Single(item => item.Id == 5);
        CombatUnit moved = actual.Spawn.Train.Rooms[1].Units.Single(item => item.Id == 5);
        Require(rows.Length == 9 && before.Spawn.Train.Context!.QueryFrame is { MoonPhaseTrackingActive: false, MoonPhase: 1 } &&
            unit is { AssetKey: "HordeT3_PureSouls", MoonPhaseMask: 6, Health: 1, MaxHealth: 1, Attack: 12 } && moved.MoonPhaseMask == 5,
            "Missing native inactive new-moon Horde movement.");
        JsonNode mutant = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        JsonNode original = mutant["Spawn"]!["Train"]!["Rooms"]![2]!["Units"]!.AsArray().Single(item => item!["Id"]!.GetValue<int>() == 5)!;
        original["AssetKey"] = "UnknownUnit";
        BattleActionResult rejected = BattleActionModel.PlayCard(mutant.Deserialize<BattleTurnState>(ModelJson.Options)!, action);
        Require(!rejected.Supported, "Unknown unit movement must remain guarded.");
        mutant = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        mutant["Spawn"]!["Train"]!["Context"]!["Relics"]![0]!["EffectTypes"]!.AsArray().Add("RelicEffectMoonPhaseAlwaysActive");
        TrainCombatState overridden = mutant.Deserialize<BattleTurnState>(ModelJson.Options)!.Spawn.Train;
        MoonPhaseTransitionResult overrideResult = RoomMoonPhaseModel.RefreshMovedCharacter(overridden, 5, new List<RoomCombatModel.QueuedCharacterTrigger>());
        Require(overrideResult.State == null && overrideResult.UnsupportedReason == "Movement with relic moon-phase override requires native calibration.",
            "Relic phase overrides must remain guarded.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options), expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            BattleActionResult result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected, "Concurrent Horde movement differs.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Horde movement mutated its parent.");
        Console.WriteLine("INACTIVE-NEW-MOON-HORDE PASS: 9 native inputs, complete successor/RNG, unit and relic guards and four immutable branches.");
    }

    internal static void Pair(FixtureValue original, FixtureValue repaired, FixtureValue repaired2, FixtureValue original2)
    {
        Native(original); Native(original2); Native(repaired); Native(repaired2);
        NativeBranchObservationChecks.Pair(original, repaired);
        NativeBranchObservationChecks.Pair(original2, repaired2);
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
