using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MirroredExistingHordeChecks
{
    internal const string Reason = "Original troop birth aura attack differs after first target binding.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, historicalBoundaryMismatch: (old, actual) => {
            var predicted = Funguy(old); var native = Funguy(actual);
            Require(predicted.Status("horde")?.Stacks == 3 && native.Status("horde")?.Stacks == 4 &&
                predicted.Attack == 9 && native.Attack == 12 && predicted.Health == 13 && native.Health == 18 &&
                predicted.MaxHealth == 16 && native.MaxHealth == 21 && AuraCount(predicted) == 4 && AuraCount(native) == 5,
                "Preserve the original missed room copy on an existing Horde target.");
        });
        Require(rows.Length == 14 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong existing Horde boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(action.CardInstanceId == 30 && before.Spawn.Turn == 2 && Funguy(before).Status("horde")?.Stacks == 2 &&
            Funguy(actual).Status("horde")?.Stacks == 4 && actual.Spawn.Train.Context!.NextUnitId == before.Spawn.Train.Context!.NextUnitId,
            "Eager Conscript must add its stack and a mirror stack to the same actor without a new identity.");
        string parent = NativeBranchObservationChecks.Key(before);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Existing mirrored Horde differs from complete native state/forecast/RNG.");
        });
        Require(parent == NativeBranchObservationChecks.Key(before), "Existing mirrored Horde mutated its parent.");
        foreach (string control in new[] { "Asset", "AttackDefinition", "HallOwner", "HallAsset" })
        {
            var node = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            var context = node["Spawn"]!["Train"]!["Context"]!;
            foreach (var rooms in new[] { node["Spawn"]!["Train"]!["Rooms"]!, context["Enchantments"]!["Rooms"]! })
            {
                var room = rooms.AsArray().Single(room => room!["RoomIndex"]!.GetValue<int>() == 0)!;
                var target = room["Units"]!.AsArray().Single(unit => unit!["Id"]!.GetValue<int>() == 10)!;
                if (control == "Asset") target["AssetKey"] = "uncalibrated-funguy";
                if (control == "AttackDefinition") target["HordeDefinition"]!["Attack"] = 4;
            }
            var hall = context["RoomCapacities"]!.AsArray().Single(room => room!["RoomIndex"]!.GetValue<int>() == 0)!["Attachments"]![0]!;
            if (control == "HallOwner") hall["DataId"] = "uncalibrated-mirrors";
            if (control == "HallAsset") hall["AssetKey"] = "uncalibrated-mirrors";
            Require(!BattleActionModel.PlayCard(node.Deserialize<BattleTurnState>(ModelJson.Options)!, action).Supported,
                "Unknown existing Horde copy signature lost its guard: " + control);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("MIRRORED-EXISTING-HORDE PASS:14 native inputs/full state/forecast/RNG; original two-to-four Horde and aura increment on one identity; four immutable branches/four scope controls; independentPair=" + replay.HasValue);
    }
    private static CombatUnit Funguy(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
        .Single(unit => unit.Id == 10 && unit.AssetKey == "MonsterFunguy");
    private static int AuraCount(CombatUnit unit) => unit.Modifiers!.Upgrades.Count(upgrade => upgrade.AssetKey == "Underlegion1RegenOnHarvest1_Upgrade");
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
