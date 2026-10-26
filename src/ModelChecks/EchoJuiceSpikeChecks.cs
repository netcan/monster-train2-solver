using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EchoJuiceSpikeChecks
{
    internal const string EndReason = "Triple Juice Echo repeats paid-cost scaling and its Pyre-heal OnCast upgrade.";
    internal static void Native(FixtureValue source)
    {
        string reason = source.GetProperty("Request").GetProperty("Reason").GetString()!;
        Require(reason.StartsWith("Echo replay needs native calibration: onCast=1, unplayed=0, interactions=, traits=CardTraitScalingAddStatusEffect;CardTraitExhaustState;CardTraitJuice3, relics=", StringComparison.Ordinal),
            "Missing original triple-Juice Echo guard.");
        var rows = NativeBranchObservationChecks.Replay(source, reason);
        Require(rows.Length == 46, "Expected45 original inputs and the triple-Juice Echo copy.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var context = actual.Spawn.Train.Context!;
        var echo = context.RoomCapacities!.Single(room => room.RoomIndex == 2).Attachments!.Single(attachment => attachment.Echo != null).Echo!;
        var copy = context.FindCard(echo.CopyCardId)!;
        Require(before.Spawn.Turn == 9 && before.Energy == 5 && actual.Energy == 0 && action.CardInstanceId == 22 &&
            action.RoomIndex == 2 && action.TargetUnitId == 2 && echo is { OriginalCardId: 22, CopyCardId: 45, PaidCost: 5 } &&
            context.NextCardId == 46 && copy.LastPlayedCost == 5 && copy.PlayCount == 0 &&
            EchoJuiceSpikeModel.Known(copy, echo.Effects!) && EchoJuiceSpikeModel.KnownRelics(context) &&
            echo.Effects![0].Statuses[0].Stacks == 0 && copy.Permanent.Upgrades.Count == 2 && copy.Temporary.Upgrades.Count == 1,
            "Native Echo must retain raw valor, paid5, original triple-Juice/Pyre-heal upgrades and the Titan curse.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var played = BattleActionModel.PlayCard(before, action);
            Require(played.Supported && NativeBranchObservationChecks.Key(played.State!) == NativeBranchObservationChecks.Key(actual),
                "Detached triple-Juice Echo copy differs.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Triple-Juice Echo mutated its parent.");
        foreach (string control in new[] { "Id", "Multiplier", "Heal", "Curse", "Scale" })
        {
            var changed = JsonSerializer.SerializeToNode(copy, ModelJson.Options)!;
            if (control == "Id") changed["DataId"] = "unknown-juice";
            if (control == "Multiplier") changed["Permanent"]!["Upgrades"]![1]!["Lifecycle"]!["AddedTraits"]![0]!["ParamInt"] = 4;
            if (control == "Heal") changed["Permanent"]!["Upgrades"]![0]!["Lifecycle"]!["Triggers"]![0]!["Actions"]![0]!["Value"] = 2;
            if (control == "Curse") changed["Temporary"]!["Upgrades"]![0]!["Stats"]!["Cost"] = 2;
            if (control == "Scale") changed["StatusScalingTraits"]![0]!["StacksPerStat"] = 8;
            Require(!EchoJuiceSpikeModel.Known(changed.Deserialize<CardInstanceState>(ModelJson.Options)!, echo.Effects!),
                "Unknown triple-Juice Echo card admitted: " + control);
        }
        var ended = BattleTurnModel.EndTurn(actual);
        Require(!ended.Supported && ended.UnsupportedReason?.Contains(EndReason, StringComparison.Ordinal) == true,
            "Actual Echo callback must retain its native-calibration boundary: " + ended.UnsupportedReason);
        Console.WriteLine("ECHO-JUICE-SPIKE COPY PASS:46 native inputs/full state/RNG, raw valor, paid5 and original upgrades, four immutable branches/five guards. EndTurn model only: supported=" + ended.Supported + "; outcome=" + ended.Outcome + "; reason=" + ended.UnsupportedReason);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
