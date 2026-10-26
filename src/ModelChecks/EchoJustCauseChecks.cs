using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EchoJustCauseChecks
{
    internal const string EndReason = "Echo Just Cause reuses the remembered physical slot before rearranging and adding valor.";
    internal static void Native(FixtureValue source)
    {
        string reason = source.GetProperty("Request").GetProperty("Reason").GetString()!;
        Require(reason.StartsWith("Echo replay needs native calibration: onCast=0, unplayed=0, interactions=, traits=, relics=", StringComparison.Ordinal),
            "Wrong original traitless Echo boundary.");
        var rows = NativeBranchObservationChecks.Replay(source, reason);
        Require(rows.Length == 40, "Expected39 native prefix inputs and the JustCause copy.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var context = actual.Spawn.Train.Context!;
        var echo = context.RoomCapacities!.Single(room => room.RoomIndex == 1).Attachments!.Single(attachment => attachment.Echo != null).Echo!;
        var copy = context.FindCard(echo.CopyCardId)!;
        Require(before.Spawn.Turn == 8 && before.Energy == 3 && actual.Energy == 1 &&
            action.CardInstanceId == 17 && action.RoomIndex == 1 && action.TargetUnitId == 2 &&
            echo is { OriginalCardId: 17, CopyCardId: 44, PaidCost: 2, DropLocation: { RoomIndex: 1, Team: CombatTeam.Player, Index: 0 } } &&
            context.NextCardId == 45 && copy.LastPlayedCost == 2 && copy.PlayCount == 0 &&
            EchoJustCauseModel.Known(copy, echo.Effects!) && EchoJuiceSpikeModel.KnownRelics(context),
            "JustCause Echo must remember the original physical slot, paid2 and Titan curse.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var played = BattleActionModel.PlayCard(before, action);
            Require(played.Supported && NativeBranchObservationChecks.Key(played.State!) == NativeBranchObservationChecks.Key(actual),
                "Detached JustCause Echo copy differs.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "JustCause Echo mutated its parent.");
        foreach (string control in new[] { "Id", "Curse", "Offset", "Callback", "Amount" })
        {
            var changed = JsonSerializer.SerializeToNode(copy, ModelJson.Options)!;
            var effects = JsonSerializer.SerializeToNode(echo.Effects, ModelJson.Options)!;
            if (control == "Id") changed["DataId"] = "unknown-echo";
            if (control == "Curse") changed["Temporary"]!["Upgrades"]![0]!["Stats"]!["Cost"] = 2;
            if (control == "Offset") changed["Permanent"]!["Offsets"]!["Damage"] = 1;
            if (control == "Callback") changed["ExternalInteractions"]!.AsArray().Add("unknown-callback");
            if (control == "Amount") effects[1]!["Statuses"]![0]!["Stacks"] = 4;
            Require(!EchoJustCauseModel.Known(changed.Deserialize<CardInstanceState>(ModelJson.Options)!, effects.Deserialize<CardActionEffect[]>(ModelJson.Options)!),
                "Unknown JustCause Echo payload admitted: " + control);
        }
        Console.WriteLine("ECHO-JUST-CAUSE COPY PASS:40 native inputs/full state/RNG, remembered physical slot, paid2 and Titan curse; four immutable branches/five guards.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
