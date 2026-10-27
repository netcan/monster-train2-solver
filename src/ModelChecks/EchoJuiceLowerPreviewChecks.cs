using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EchoJuiceLowerPreviewChecks
{
    private const string Reason = "Battle preview floor 1: Triple Juice Echo preview requires its calibrated empty Pyre room above the top floor.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 43, "Expected42 original inputs plus the middle-floor Juice Echo.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var context = after.Spawn.Train.Context!;
        var echo = context.RoomCapacities!.Single(room => room.RoomIndex == 1).Attachments!.Single(attachment => attachment.Echo != null).Echo!;
        var copy = context.FindCard(echo.CopyCardId)!;
        CombatUnit Target(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 2);
        int Pyre(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health;
        var maximum = RelicResourceModel.GetPyreMaxHealth(before.Spawn.Train.Context!);
        Require(before.Spawn.Turn == 9 && before.Energy == 3 && after.Energy == 0 && action.CardInstanceId == 22 &&
            action.RoomIndex == 1 && action.TargetUnitId == 2 && echo.OriginalCardId == 22 && echo.PaidCost == 3 &&
            echo.CopyCardId == before.Spawn.Train.Context!.NextCardId && context.NextCardId == echo.CopyCardId + 1 &&
            copy.LastPlayedCost == 3 && copy.PlayCount == 0 && EchoJuiceSpikeModel.Known(copy, echo.Effects!) &&
            EchoJuiceSpikeModel.KnownRelics(context) && Target(after).Status("valor")!.Stacks == (Target(before).Status("valor")?.Stacks ?? 0) + 36 &&
            maximum.Supported && Pyre(after) == Math.Min(maximum.Value, Pyre(before) + 3),
            "Middle-floor Echo must retain paid3/raw scaling, add36 Valor and heal three real Pyre HP.");
        var frame = before.Spawn.Train.Context.QueryFrame!;
        var previewFrame = frame.WithPreviewTowerHealth(Pyre(before));
        Require(frame.PreviewTowerHealth == null && previewFrame.With(energy: 0, selectedRoomIndex: 2).PreviewTowerHealth == Pyre(before) &&
            !JsonSerializer.Serialize(previewFrame, ModelJson.Options).Contains("PreviewTowerHealth", StringComparison.Ordinal),
            "Preview HP must be immutable, survive frame copies and remain outside captured/public state.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && result.State!.Spawn.Train.Context!.QueryFrame!.PreviewTowerHealth == null &&
                NativeBranchObservationChecks.Key(result.State) == NativeBranchObservationChecks.Key(after),
                "Middle-floor Echo preview differs or leaked its temporary live-HP input.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Middle-floor Echo mutated its parent.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("ECHO-JUICE-LOWER-PREVIEW PASS:43 native inputs/full state/RNG, paid3/scaled12/triple36 and Pyre heal3; immutable private preview HP and four parallel branches; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
