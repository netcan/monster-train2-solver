using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MoonSilenceShiftChecks
{
    private const string Reason = "Moon silence callbacks with character or card traits require native calibration.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        Check(source);
        if (replay.HasValue)
        {
            Check(replay.Value);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: false);
        }
        Console.WriteLine("MOON-SILENCE-SHIFT PASS:34 native inputs/full state/RNG, Aurora global-shift callback retained without firing, four immutable branches/five strict guards; independentPair=" + replay.HasValue);
    }

    private static void Check(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 34, "Expected33 native prefix inputs and StoicPlatemail on Aurora.");
        var row = rows[^1];
        var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit Unit(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 19);
        Require(before.Spawn.Turn == 4 && action.CardInstanceId == 9 && action.RoomIndex == 1 && action.TargetUnitId == 19 &&
            Unit(before).AssetKey == "MonsterAuroraWeaver" && Unit(before).MoonPhaseMask == 5 && Unit(after).MoonPhaseMask == 5 &&
            Unit(before).Status("silenced") == null && Unit(after).Status("silenced")?.Stacks == 1 &&
            Unit(after).Status("armor")?.Stacks == 60 && Unit(before).Status("conduit")?.Stacks == 3 && Unit(after).Status("conduit")?.Stacks == 3 &&
            Unit(before).Triggers.Single().Kind == "OnMoonPhaseShift" &&
            JsonSerializer.Serialize(Unit(before).Triggers, ModelJson.Options) == JsonSerializer.Serialize(Unit(after).Triggers, ModelJson.Options) &&
            Unit(before).Modifiers!.Upgrades.Count(upgrade => upgrade.AssetKey == "AuroraWeaverMagicPowerRoomUpgrade") == 3 &&
            Unit(after).Modifiers!.Upgrades.Count(upgrade => upgrade.AssetKey == "AuroraWeaverMagicPowerRoomUpgrade") == 3,
            "Silence must retain three prior Conduit upgrades without firing the global MoonPhaseShift callback.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(after),
                "Moon silence/shift differs from complete native successor/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Moon silence/shift mutated parent.");
        foreach (string control in new[] { "OnMoonLit", "OnMoonlitLost", "OnMoonShade", "OnMoonshadeLost", "CardTrait" })
        {
            JsonNode changed = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            var unit = changed["Spawn"]!["Train"]!["Rooms"]!.AsArray().SelectMany(room => room!["Units"]!.AsArray())
                .Single(unit => unit!["Id"]!.GetValue<int>() == 19)!;
            if (control == "CardTrait")
            {
                var card = changed["Spawn"]!["Train"]!["Context"]!["CardInstances"]!.AsArray()
                    .Single(card => card!["InstanceId"]!.GetValue<int>() == Unit(before).SpawnerCardId)!;
                card["MaskDescriptor"]!["BaseTraits"]!.AsArray().Add(new JsonObject { ["RuntimeType"] = "UnknownMoonPhaseTrait" });
            }
            else unit["Triggers"]![0]!["Kind"] = control;
            var rejected = BattleActionModel.PlayCard(changed.Deserialize<BattleTurnState>(ModelJson.Options)!, action);
            Require(!rejected.Supported && rejected.State == null, "Uncalibrated moon silence admitted: " + control);
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
