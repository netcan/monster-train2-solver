using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MoonConduitBirthOrderChecks
{
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        // Preserve the request's original diagnostic label, before the newborn
        // registration boundary was isolated from the champion's slay effect.
        const string reason = "Observed room slay positive-status registry order differs.";
        var rows = NativeBranchObservationChecks.Replay(source, reason, (historical, actual) =>
        {
            var old = historical.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 41);
            var native = actual.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 41);
            Require(old.AssetKey == "MonsterFunguy" && native.AssetKey == old.AssetKey &&
                old.StatusRegistry!.Select(status => status.Id).SequenceEqual(["horde", "cardless", "conduit", "fullmoon"]) &&
                native.StatusRegistry!.Select(status => status.Id).SequenceEqual(["horde", "cardless", "fullmoon", "conduit"]),
                "Lost original newborn moon/conduit registration mismatch.");
        });
        Require(rows.Length == 25 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Incomplete natural EagerConscript birth prefix.");
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var born = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 41);
        Require(before.Spawn.Turn == 5 && action is { CardInstanceId: 21, RoomIndex: 0 } &&
            before.Spawn.Train.Context!.FindCard(21)!.DataId == "0c8ff108-478c-4c1c-9c49-f6a297b4e0d8" &&
            born.AssetKey == "MonsterFunguy" && born.Status("conduit")?.Stacks == 5 && born.Status("fullmoon")?.Stacks == 1 &&
            born.StatusDictionary!.Slots.SequenceEqual(["horde", "cardless", "fullmoon", "conduit"]),
            "Missing original full-moon Funguy birth inside the conduit room.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, reason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("MOON-CONDUIT-BIRTH-ORDER PASS: twenty-five native inputs/full state/RNG, original reversed order retained, " +
            "Funguy41 registers moon before conduit5; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
