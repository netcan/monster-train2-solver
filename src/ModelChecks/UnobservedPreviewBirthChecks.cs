using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnobservedPreviewBirthChecks
{
    private const string Reason = "Observed egg hatch identity allocation differs after native corrupt spell.";

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, (historical, actual) =>
        {
            Require(historical.Spawn.Train.Context!.NextCardId == 39 && historical.Spawn.NextUnitId == 32 &&
                actual.Spawn.Train.Context!.NextCardId == 38 && actual.Spawn.NextUnitId == 31 &&
                historical.Spawn.Train.Context.CardRegistry!.Any(card => card.InstanceId == 38 &&
                    card.DataId == "057f7f77-df4b-4981-9e6d-533a6e667f39"), "Lost the original phantom preview birth.");
        });
        Require(rows.Length == 45, "Incomplete original input chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var eggBefore = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 19);
        var eggAfter = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 19);
        Require(before.Spawn.Turn == 8 && action.CardInstanceId == 25 && action.TargetUnitId == 2 && action.RoomIndex == 2 &&
            eggBefore.AssetKey == "MonsterKinhostVessel" && eggBefore.Status("hatch")?.Stacks == 1 &&
            eggBefore.Status("valor")?.Stacks == 16 && eggAfter.Status("hatch")?.Stacks == 1 &&
            eggAfter.Status("valor")?.Stacks == 18 && actual.Spawn.Train.Context!.NextCardId == before.Spawn.Train.Context!.NextCardId &&
            actual.Spawn.NextUnitId == before.Spawn.NextUnitId, "Missing original unhatched egg and unobserved preview identities.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string native = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && result.Outcome == RoomOutcome.Exchanged &&
                NativeBranchObservationChecks.Key(result.State!) == native && JsonSerializer.Serialize(before, ModelJson.Options) == parent,
                "Preview birth changed full state/RNG or immutable parent.");
        });
        Console.WriteLine("UNOBSERVED-PREVIEW-BIRTH PASS:45 native inputs/full state/RNG, historical phantom spawner copy/actor retained, native live egg unchanged and four immutable branches; independentPair=" + replay.HasValue);
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
