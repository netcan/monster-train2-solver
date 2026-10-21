using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ConduitMovementChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("NativeCompleted").GetBoolean() && root.GetProperty("RngUnchanged").GetBoolean() &&
            root.GetProperty("Boundary").GetString() == "AfterNativeSetSpawnPointReturn" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4",
            "Missing original native Conduit movement boundary.");
        int rows = 0, entered = 0, cleared = 0, unchanged = 0, detached = 0, dead = 0, retained = 0;
        foreach (FixtureValue row in root.GetProperty("Rows").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").GetProperty("Train").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").GetProperty("Train").Deserialize<TrainCombatState>()!;
            ConduitUnitState contributor = row.GetProperty("Before").GetProperty("Contributor").Deserialize<ConduitUnitState>()!;
            ConduitUnitState afterContributor = row.GetProperty("Actual").GetProperty("Contributor").Deserialize<ConduitUnitState>()!;
            SpawnPointReference? from = row.GetProperty("From").Deserialize<SpawnPointReference>();
            SpawnPointReference? to = row.GetProperty("To").Deserialize<SpawnPointReference>();
            bool isDead = row.GetProperty("Dead").GetBoolean();
            string label = row.GetProperty("Label").GetString()!;
            string parent = JsonSerializer.Serialize(before);
            Require(contributor.UnitId == row.GetProperty("UnitId").GetInt32() && contributor.Power == 4 &&
                before.Context!.QueryFrame!.SelectedRoomIndex == row.GetProperty("SelectedRoom").GetInt32(),
                "Conduit contributor/selection controls changed.");
            var scope = new RoomCombatState(from?.RoomIndex ?? to?.RoomIndex ?? 0, false,
                before.Rooms.SelectMany(room => room.Units).ToArray(), [], before.Context);
            BattleSpawnPointResult points = BattleSpawnPointModel.Apply(before.Context.SpawnPoints!, scope, "Set", CombatTeam.Player,
                contributor.UnitId, target: to);
            Require(points.Supported, points.Error ?? "Physical move failed.");
            Require(before.Context.ConduitRetained != null && actual.Context!.ConduitRetained != null,
                "Missing detached/dead tracker capture.");
            CombatContext predicted = ConduitModel.MovePhysicalContributor(before.Context.WithSpawnPoints(points.State!),
                contributor.UnitId, CombatTeam.Player, from, to);
            Equal(predicted, actual.Context, label + " complete context/physical points/retained trackers");
            ConduitUnitState captured = predicted.MagicPower!.SelectMany(room => room.Conduit!.Units)
                .Concat(predicted.ConduitRetained!.Select(value => value.Contributor))
                .Single(value => value.UnitId == contributor.UnitId);
            Equal(captured, afterContributor, label + " private tracker");
            retained += actual.Context!.ConduitRetained!.Count;
            Require(row.GetProperty("Before").GetProperty("Rng").ContentEquals(row.GetProperty("Actual").GetProperty("Rng")),
                "Conduit movement consumed gameplay RNG.");
            CardInstanceState plain = actual.Context!.FindCard(root.GetProperty("PlainCardId").GetInt32())!;
            CardInstanceState stronger = actual.Context.FindCard(root.GetProperty("StrongerCardId").GetInt32())!;
            bool tracking = afterContributor.Cards.Contains(plain.InstanceId);
            Require(afterContributor.Cards.Contains(stronger.InstanceId) == tracking &&
                (plain.Temporary.Upgrades.SingleOrDefault(upgrade => upgrade.DataId == "conduit:" + contributor.UnitId)?.Stats.Damage ?? 0) == (tracking ? 4 : 0) &&
                (stronger.Temporary.Upgrades.SingleOrDefault(upgrade => upgrade.DataId == "conduit:" + contributor.UnitId)?.Stats.Damage ?? 0) == (tracking ? 20 : 0),
                "Native Conduit eligibility/multiplier control differs.");
            if (contributor.Cards.Count == 0 && tracking) entered++;
            else if (contributor.Cards.Count > 0 && !tracking) cleared++;
            else unchanged++;
            if (from == null || to == null) detached++;
            if (isDead) dead++;
            Require(JsonSerializer.Serialize(before) == parent, "Conduit movement mutated its source snapshot.");
            Require(ConduitModel.Validate(before.Context) != null, "Uncalibrated whole-battle physical guard was removed.");
            rows++;
        }
        Require(rows == 34 && entered > 0 && cleared > 0 && unchanged > 0 && detached >= 4 && dead == 3 && retained >= 5,
            "Missing selected/unselected, detached, dead or unchanged native controls.");
        Console.WriteLine($"CONDUIT-MOVEMENT PASS rows={rows} entered={entered} cleared={cleared} unchanged={unchanged} detached={detached} dead={dead} retained={retained}: snapshot-only prediction, exact context, private trackers, physical points, RNG and immutable parents; whole-battle guard retained.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string first = JsonSerializer.Serialize(predicted), second = JsonSerializer.Serialize(actual);
        Require(first == second, label + " differs: " + ModelJson.Difference(first, second));
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
