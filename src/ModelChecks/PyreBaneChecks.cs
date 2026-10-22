using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PyreBaneChecks
{
    internal static void Native(FixtureValue root)
    {
        void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Boundary").GetString() == "OriginalSavageryPyreBaneTargetQueries" &&
            root.GetProperty("DetachedHealthAndStatusControls").GetBoolean() && root.GetProperty("OriginalUnitsAndRooms").GetBoolean() &&
            root.GetProperty("LiveWorldAndFrameAndRngUnchanged").GetBoolean() && root.GetProperty("NativeCompleted").GetBoolean(),
            "Missing isolated original Savagery target queries or live actor/frame/all-RNG restoration.");
        Require(root.GetProperty("Route").GetProperty("Scenario").GetString() == "Level9TrueFinalBoss_V2" &&
            root.GetProperty("Route").GetProperty("CharacterAssetsUnchanged").GetBoolean(), "Missing original true-final-boss route.");
        int savageryId = root.GetProperty("SavageryId").GetInt32(), bottomId = root.GetProperty("BottomId").GetInt32();
        FixtureValue[] rows = root.GetProperty("Rows").EnumerateArray().ToArray();
        Require(savageryId == 3 && bottomId == 4 && rows.Length == 480 &&
            rows.Select(row => (row.GetProperty("Health").GetInt32(), row.GetProperty("LifeLink").GetBoolean(),
                row.GetProperty("PyreBane").GetBoolean(), row.GetProperty("InCombat").GetBoolean(),
                row.GetProperty("Seed").GetInt32(), row.GetProperty("Mode").GetString())).Distinct().Count() == 480,
            "Incomplete native health/status/combat/seed/target-mode matrix.");
        var rooms = new List<RoomCombatState>(); var effects = new List<CardActionEffect>(); var parents = new List<string>();
        foreach (FixtureValue row in rows)
        {
            int health = row.GetProperty("Health").GetInt32();
            bool lifeLink = row.GetProperty("LifeLink").GetBoolean(), pyreBane = row.GetProperty("PyreBane").GetBoolean();
            bool inCombat = row.GetProperty("InCombat").GetBoolean(); string mode = row.GetProperty("Mode").GetString()!;
            uint[] before = row.GetProperty("RngBefore").EnumerateArray().Select(word => word.GetUInt32()).ToArray();
            uint[] after = row.GetProperty("RngAfter").EnumerateArray().Select(word => word.GetUInt32()).ToArray();
            var rng = new UnityRng(before[0], before[1], before[2], before[3]);
            var statuses = new List<CombatStatus> { new CombatStatus("trample", 1) };
            if (lifeLink) statuses.Add(new CombatStatus("lifelink", 1));
            if (pyreBane) statuses.Add(new CombatStatus("pyrebane", 1));
            var savagery = new CombatUnit(savageryId, TrueFinalBossChecks.Assets[1], CombatTeam.Enemy,
                10, health, 2750, true, false, true, statuses, Array.Empty<CombatTrigger>());
            var bottom = new CombatUnit(bottomId, TrueFinalBossChecks.Assets[2], CombatTeam.Enemy,
                15, 3500, 3500, true, false, true, new[] { new CombatStatus("lifelink", 1), new CombatStatus("immobile", 1) }, Array.Empty<CombatTrigger>());
            var cards = new CardCycleState(Array.Empty<CardToken>(), Array.Empty<CardToken>(), Array.Empty<CardToken>(), rng, 0, Array.Empty<string>());
            var context = new CombatContext(cards, rng, 0, 1, 10);
            var room = new RoomCombatState(0, false, new[] { savagery, bottom }, Array.Empty<string>(), context);
            var effect = new CardActionEffect("Damage", mode, 1, true, false, Array.Empty<CombatStatus>());
            string parent = JsonSerializer.Serialize(room, ModelJson.Options);
            CardTargets result = CardTargetModel.Collect(room, effect, Array.Empty<int>(), inCombat: inCombat);
            int[] expected = row.GetProperty("Targets").EnumerateArray().Select(target => target.GetInt32()).ToArray();
            UnityRng actualRng = result.BattleRng ?? context.BattleRng;
            Require(result.Supported && result.UnitIds.SequenceEqual(expected) &&
                new[] { actualRng.S0, actualRng.S1, actualRng.S2, actualRng.S3 }.SequenceEqual(after) &&
                PyreBaneModel.Targetable(savagery, inCombat) == row.GetProperty("Targetable").GetBoolean() &&
                JsonSerializer.Serialize(room, ModelJson.Options) == parent,
                $"Native pyrebane {health}/{lifeLink}/{pyreBane}/{inCombat}/{mode} targets, targetability or RNG differ.");
            if (!inCombat && mode == "RandomInRoom")
            {
                var train = new TrainCombatState(new[] { room }, Array.Empty<EnemyMovement>(), 7, context);
                CardTargets pool = CardTargetModel.RandomCandidates(train, 0, effect);
                int[] nativePool = rows.Where(other => other.GetProperty("Health").GetInt32() == health &&
                    other.GetProperty("LifeLink").GetBoolean() == lifeLink && other.GetProperty("PyreBane").GetBoolean() == pyreBane &&
                    !other.GetProperty("InCombat").GetBoolean() && other.GetProperty("Mode").GetString() == mode)
                    .SelectMany(other => other.GetProperty("Targets").EnumerateArray()).Select(target => target.GetInt32()).Distinct().OrderBy(id => id).ToArray();
                Require(pool.Supported && pool.UnitIds.OrderBy(id => id).SequenceEqual(nativePool),
                    $"Random spell branch candidates differ for {health}/{lifeLink}/{pyreBane}: model={string.Join(',', pool.UnitIds)}, native={string.Join(',', nativePool)}.");
            }
            rooms.Add(room); effects.Add(effect); parents.Add(parent);
        }
        Parallel.For(0, 128, index =>
        {
            int selected = index * 37 % rows.Length; FixtureValue row = rows[selected];
            CardTargets result = CardTargetModel.Collect(rooms[selected], effects[selected], Array.Empty<int>(),
                inCombat: row.GetProperty("InCombat").GetBoolean());
            Require(result.Supported && result.UnitIds.SequenceEqual(row.GetProperty("Targets").EnumerateArray().Select(target => target.GetInt32())) &&
                JsonSerializer.Serialize(rooms[selected], ModelJson.Options) == parents[selected], "Concurrent native target query changed its parent or result.");
        });
        Require(!RoomCombatModel.Resolve(rooms[0]).Supported, "Target calibration must not open uncalibrated true-final-boss combat.");
        Console.WriteLine("PYREBANE-TARGETS PASS: 480 native targetability/selection/RNG queries, five modes, combat vs placement, status/HP controls; random branch pools and 128 immutable concurrent queries; live world/frame/all RNG restored.");
    }
}
