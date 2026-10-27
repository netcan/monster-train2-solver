using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MirroredMeddlerMoonChecks
{
    internal const string Reason = "Triggered moon-phase advance requires a complete train callback.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 11 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong mirrored Meddler boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var meddlers = actual.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2).Units
            .Where(unit => unit.AssetKey == "MonsterFaeMeddler").ToArray();
        Require(before.Spawn.Turn == 2 && action.CardInstanceId == 20 && action.RoomIndex == 2 &&
            meddlers.Select(unit => unit.Id).SequenceEqual(new[] { 13, 14 }) &&
            meddlers[0].Modifiers?.IsClone == false && meddlers[1].Modifiers?.IsClone == true &&
            meddlers[1].SpawnerCardId == 32 && actual.Spawn.Train.Context!.AbilityCardCache!.Single().InstanceId == 33 &&
            meddlers.All(unit => unit.Triggers.Where(trigger => trigger.Kind == "OnSpawn").All(trigger => trigger.HasTriggered)) &&
            before.Spawn.Train.Context!.QueryFrame!.MoonPhase == 1 && actual.Spawn.Train.Context!.QueryFrame!.MoonPhase == 1 &&
            actual.Spawn.NextUnitId == before.Spawn.NextUnitId + 2,
            "Paid and mirrored Meddler must each advance New/Full/New with separate native identities.");
        string parent = NativeBranchObservationChecks.Key(before);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Mirrored Meddler differs from full native state/forecast/RNG.");
        });
        Require(parent == NativeBranchObservationChecks.Key(before), "Mirrored Meddler mutated its parent.");
        foreach (string control in new[] { "CopyAsset", "AdvanceAmount", "AdvanceTarget", "HallAsset" })
        {
            var node = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            var birth = node["PlayRules"]!["UnitCopyCatalog"]!["Births"]!.AsArray()
                .Single(item => item!["Unit"]!["AssetKey"]!.GetValue<string>() == "MonsterFaeMeddler")!["Unit"]!;
            var advance = birth["Triggers"]!.AsArray().SelectMany(trigger => trigger!["Effects"]!.AsArray())
                .Single(effect => effect!["Type"]!.GetValue<string>() == "CardEffectAdvanceMoonPhase")!;
            if (control == "CopyAsset") birth["AssetKey"] = "uncalibrated-meddler";
            if (control == "AdvanceAmount") advance["Action"]!["Value"] = 2;
            if (control == "AdvanceTarget") advance["Action"]!["Target"] = "Room";
            if (control == "HallAsset") node["Spawn"]!["Train"]!["Context"]!["RoomCapacities"]!.AsArray()
                .Single(room => room!["RoomIndex"]!.GetValue<int>() == 2)!["Attachments"]![0]!["AssetKey"] = "uncalibrated-mirrors";
            Require(!BattleActionModel.PlayCard(node.Deserialize<BattleTurnState>(ModelJson.Options)!, action).Supported,
                "Unknown mirrored moon signature lost its guard: " + control);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("MIRRORED-MEDDLER-MOON PASS:11 native inputs/full state/forecast/RNG; paid and copied original moon advances/all-floor phase refresh; four immutable branches/four scope controls; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
