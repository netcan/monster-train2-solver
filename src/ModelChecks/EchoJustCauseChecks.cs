using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EchoJustCauseChecks
{
    internal const string EndReason = "Echo Just Cause reuses the remembered physical slot before rearranging and adding valor.";
    internal static void Native(FixtureValue source, FixtureValue? endTurn = null, FixtureValue? replay = null)
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
        foreach (string control in new[] { "Id", "Curse", "Offset", "Callback", "Amount", "Descriptor" })
        {
            var changed = JsonSerializer.SerializeToNode(copy, ModelJson.Options)!;
            var effects = JsonSerializer.SerializeToNode(echo.Effects, ModelJson.Options)!;
            if (control == "Id") changed["DataId"] = "unknown-echo";
            if (control == "Curse") changed["Temporary"]!["Upgrades"]![0]!["Stats"]!["Cost"] = 2;
            if (control == "Offset") changed["Permanent"]!["Offsets"]!["Damage"] = 1;
            if (control == "Callback") changed["ExternalInteractions"]!.AsArray().Add("unknown-callback");
            if (control == "Amount") effects[1]!["Statuses"]![0]!["Stacks"] = 4;
            if (control == "Descriptor") changed["MaskDescriptor"] = null;
            Require(!EchoJustCauseModel.Known(changed.Deserialize<CardInstanceState>(ModelJson.Options)!, effects.Deserialize<CardActionEffect[]>(ModelJson.Options)!),
                "Unknown JustCause Echo payload admitted: " + control);
        }
        if (endTurn.HasValue)
        {
            var ends = NativeBranchObservationChecks.Replay(endTurn.Value, "Train combat floor 1: " + EndReason);
            Require(ends.Length == 41 && ends[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native JustCause pre-combat replay.");
            for (int i = 0; i < rows.Length; i++)
                Require(rows[i].GetProperty("Action").ContentEquals(ends[i].GetProperty("Action")) &&
                    rows[i].GetProperty("IsEndTurn").ContentEquals(ends[i].GetProperty("IsEndTurn")) &&
                    NativeBranchObservationChecks.Key(rows[i].GetProperty("Actual").Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(ends[i].GetProperty("Actual").Deserialize<BattleTurnState>()!),
                    "JustCause EndTurn changed the original prefix: " + i);
            var final = ends[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(final.Spawn.Turn == 9 && final.Spawn.Train.Rooms.Single(room => room.RoomIndex == 3).Units.Single(unit => unit.IsPyre).Health == 77 &&
                final.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 1).Attachments!
                .Single(attachment => attachment.Echo != null).Echo!.CopyCardId == 0, "Recast JustCause cache was not cleared.");
            CheckPreCombat(endTurn.Value);
            string parent = JsonSerializer.Serialize(actual, ModelJson.Options);
            Parallel.For(0, 4, _ =>
            {
                var result = BattleTurnModel.EndTurn(actual);
                Require(result.Supported && result.Outcome == RoomOutcome.Exchanged &&
                    NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(final),
                    "Detached JustCause EndTurn differs.");
            });
            Require(parent == JsonSerializer.Serialize(actual, ModelJson.Options), "JustCause EndTurn mutated its parent.");
            if (replay.HasValue)
            {
                NativeBranchObservationChecks.Replay(replay.Value, "Train combat floor 1: " + EndReason);
                NativeBranchObservationChecks.Pair(endTurn.Value, replay.Value);
                CheckPreCombat(replay.Value);
            }
        }
        Console.WriteLine("ECHO-JUST-CAUSE PASS:40 native inputs/full state/RNG, remembered physical slot, paid2 and Titan curse; four immutable branches/six guards; nativeEndTurn=" + endTurn.HasValue + "; independentPair=" + replay.HasValue);
    }
    private static void CheckPreCombat(FixtureValue fixture)
    {
        var phase = fixture.GetProperty("RoomPreCombatRecords").EnumerateArray().Single(row =>
            row.GetProperty("Turn").GetInt32() == 8 && row.GetProperty("RoomIndex").GetInt32() == 1);
        Require(phase.GetProperty("Completed").GetBoolean(), "Incomplete JustCause pre-combat record.");
        var before = phase.GetProperty("Before").Deserialize<TrainCombatState>()!;
        var after = phase.GetProperty("After").Deserialize<TrainCombatState>()!;
        var old = before.Rooms.Single(room => room.RoomIndex == 1).Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
        var next = after.Rooms.Single(room => room.RoomIndex == 1).Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
        Require(old.Select(unit => unit.Id).SequenceEqual(new[] { 2, 5, 26 }) &&
            next.Select(unit => unit.Id).SequenceEqual(new[] { 2, 5, 26 }) &&
            old[0].Status("valor")!.Stacks == 88 && next[0].Status("valor")!.Stacks == 90 &&
            old.Zip(next).All(pair => pair.First.Health == pair.Second.Health) &&
            old.Skip(1).Zip(next.Skip(1)).All(pair => JsonSerializer.Serialize(pair.First, ModelJson.Options) == JsonSerializer.Serialize(pair.Second, ModelJson.Options)) &&
            before.Context!.Statistics!.PlayedCosts.Single(value => value.CardId == 44).Cost == 2 &&
            after.Context!.Statistics!.CardsPlayedThisTurn!.SequenceEqual(new[] { 13, 26, 17, 44 }) &&
            !after.Context.Statistics.PlayedCosts.Any(value => value.CardId == 44),
            "JustCause Echo must recast once on its physical front slot, add2 valor and clear its paid-cost entry.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
