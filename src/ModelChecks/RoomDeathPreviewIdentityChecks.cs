using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RoomDeathPreviewIdentityChecks
{
    private const string Reason = "Observed room death fallback preview identity differs after native attachment.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, (historical, actual) =>
        {
            var old = historical.Spawn.Train.Context!;
            var native = actual.Spawn.Train.Context!;
            var phantom = old.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == 14).Unit;
            Require(old.NextUnitId == 15 && native.NextUnitId == 14 &&
                old.NextCardId == 19 && native.NextCardId == 19 &&
                old.CardRegistry!.Count == 18 && native.CardRegistry!.Count == 18 &&
                phantom.AssetKey == "MonsterFunguy" && phantom.SpawnerCardId == 0 &&
                native.Enchantments!.RetainedUnits.Count == 0,
                "Lost the original cardless room-death preview identity counterexample.");
        });
        Require(rows.Length == 12, "Incomplete natural second-battle prefix.");
        var before = rows.Last().GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows.Last().GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows.Last().GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(before.Spawn.Turn == 2 && before.Spawn.Train.Context!.NextUnitId == 14 &&
            action.CardInstanceId == 11 && action.RoomIndex == 0 &&
            actual.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!
                .Any(attachment => attachment.DataId == "e1a39d88-4a77-4399-98b2-0b81f44fe6a8" && attachment.DeathEffect != null) &&
            actual.Spawn.NextUnitId == before.Spawn.NextUnitId,
            "Missing the actual Infectory attachment and unchanged live allocation.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string nativeKey = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && result.Outcome == RoomOutcome.Exchanged &&
                NativeBranchObservationChecks.Key(result.State!) == nativeKey &&
                JsonSerializer.Serialize(before, ModelJson.Options) == parent,
                "Room-death preview changed native full state/RNG or its immutable parent.");
        });
        Console.WriteLine("ROOM-DEATH-PREVIEW-IDENTITY PASS:12 native inputs/full state/RNG, original unowned Funguy phantom retained as evidence, " +
            "live unit/card allocation unchanged, four immutable branches; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
