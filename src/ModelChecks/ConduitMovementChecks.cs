using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ConduitMovementChecks
{
    internal static void Native(FixtureValue root)
    {
        bool enemy = root.TryGetProperty("EnemyCarrier", out FixtureValue flag) && flag.GetBoolean();
        CombatTeam team = enemy ? CombatTeam.Enemy : CombatTeam.Player;
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
            Require((from == null || from.Team == team) && (to == null || to.Team == team), "Conduit point belongs to the wrong team.");
            bool isDead = row.GetProperty("Dead").GetBoolean();
            string label = row.GetProperty("Label").GetString()!;
            string parent = JsonSerializer.Serialize(before);
            Require(contributor.UnitId == row.GetProperty("UnitId").GetInt32() && contributor.Power == 4 &&
                before.Context!.QueryFrame!.SelectedRoomIndex == row.GetProperty("SelectedRoom").GetInt32(),
                "Conduit contributor/selection controls changed.");
            var scope = new RoomCombatState(from?.RoomIndex ?? to?.RoomIndex ?? 0, false,
                before.Rooms.SelectMany(room => room.Units).ToArray(), [], before.Context);
            BattleSpawnPointResult points = BattleSpawnPointModel.Apply(before.Context.SpawnPoints!, scope, "Set", team,
                contributor.UnitId, target: to);
            Require(points.Supported, points.Error ?? "Physical move failed.");
            Require(before.Context.ConduitRetained != null && actual.Context!.ConduitRetained != null,
                "Missing detached/dead tracker capture.");
            CombatContext predicted = before.Context.WithSpawnPoints(points.State!);
            Equal(predicted, actual.Context, label + " complete context/physical points/retained trackers");
            Parallel.For(0, 8, _ =>
            {
                BattleSpawnPointResult branch = BattleSpawnPointModel.Apply(before.Context.SpawnPoints!, scope, "Set", team,
                    contributor.UnitId, target: to);
                Require(branch.Supported, branch.Error ?? "Independent physical move failed.");
                Equal(before.Context.WithSpawnPoints(branch.State!), actual.Context, label + " independent complete branch");
            });
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
            Require(ConduitModel.Validate(before.Context) == null && ConduitModel.Validate(before.Context.WithConduitRetained(null)) != null,
                "Complete capture must be accepted while legacy physical captures remain guarded.");
            rows++;
        }
        Require(rows == 38 && entered > 0 && cleared > 0 && unchanged > 0 && detached >= 4 && dead == 4 && retained >= 5,
            "Missing selected/unselected, detached, dead or unchanged native controls.");
        int previews = 0;
        foreach (FixtureValue row in root.GetProperty("Previews").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").GetProperty("Train").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").GetProperty("Train").Deserialize<TrainCombatState>()!;
            string parent = JsonSerializer.Serialize(before), label = row.GetProperty("Label").GetString()!;
            BattleSpawnPoints original = before.Context!.SpawnPoints!, selected = original.BeginPreview();
            SpawnPointReference? nativeTarget = row.GetProperty("Target").Deserialize<SpawnPointReference>();
            SpawnPointReference? target = nativeTarget == null ? null : new SpawnPointReference(nativeTarget.RoomIndex,
                team, nativeTarget.Index, selected.SelectedGroup(nativeTarget.RoomIndex, team)!.PreviewCopyId);
            Equal(target, nativeTarget, label + " independent preview point identity");
            var scope = new RoomCombatState(0, false, before.Rooms.SelectMany(room => room.Units).ToArray(), [],
                before.Context.WithSpawnPoints(selected), preview: true);
            BattleSpawnPointResult moved = BattleSpawnPointModel.Apply(selected, scope, "Set", team,
                row.GetProperty("UnitId").GetInt32(), target: target);
            Require(moved.Supported, moved.Error ?? "Preview move failed.");
            CombatContext predicted = before.Context.WithSpawnPoints(BattleSpawnPoints.EndPreview(original, moved.State!, clearBirths: true));
            Equal(predicted, actual.Context, label + " restored full context/points/trackers");
            Equal(before.Rooms.SelectMany(room => room.Units).ToArray(), actual.Rooms.SelectMany(room => room.Units).ToArray(),
                label + " restored combat actors");
            foreach (string name in new[] { "Entered", "Inside" })
            {
                FixtureValue value = row.GetProperty(name);
                ConduitUnitState copied = (name == "Inside" ? value.GetProperty("Contributor") : value).Deserialize<ConduitUnitState>()!;
                Require(copied.Power == 4 && copied.Active && copied.Cards.Count == 0 && copied.MultipliedCards.Count == 0,
                    label + " preview modifier must have an empty private tracker.");
            }
            Require(row.GetProperty("BeforeUpgrades").ContentEquals(row.GetProperty("Inside").GetProperty("Upgrades")) &&
                row.GetProperty("Before").GetProperty("Rng").ContentEquals(row.GetProperty("Inside").GetProperty("Rng")) &&
                row.GetProperty("Before").GetProperty("Rng").ContentEquals(row.GetProperty("Actual").GetProperty("Rng")),
                label + " preview changed primary hand upgrades or gameplay RNG.");
            Equal(row.GetProperty("Before").GetProperty("Contributor").Deserialize<ConduitUnitState>(),
                row.GetProperty("Actual").GetProperty("Contributor").Deserialize<ConduitUnitState>(), label + " primary tracker restored");
            Require(JsonSerializer.Serialize(before) == parent, "Preview movement mutated its parent.");
            previews++;
        }
        Require(previews == 3, "Missing native preview movement controls.");
        Console.WriteLine($"CONDUIT-MOVEMENT PASS team={team} rows={rows} entered={entered} cleared={cleared} unchanged={unchanged} detached={detached} dead={dead} retained={retained} previews={previews}: integrated physical callbacks, exact context, private trackers, points, RNG and immutable parents; incomplete captures guarded.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string first = JsonSerializer.Serialize(predicted), second = JsonSerializer.Serialize(actual);
        Require(first == second, label + " differs: " + ModelJson.Difference(first, second));
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
