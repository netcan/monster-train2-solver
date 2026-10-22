using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossDamageChecks
{
    internal static void Native(FixtureValue root)
    {
        void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
        string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Boundary").GetString() == "OriginalLiveEntropyDamageAndHealCoroutines" &&
            root.GetProperty("OriginalActorsAndStartingStats").GetBoolean() && root.GetProperty("NativeCompleted").GetBoolean() &&
            root.GetProperty("ControllerExcluded").GetBoolean() && root.GetProperty("SourceCard").GetInt32() == 0 &&
            root.GetProperty("Attacker").GetInt32() == 0 && root.GetProperty("DamageType").GetString() == "Default",
            "Missing original paid non-preview damage/healing controls.");
        FixtureValue[] rows = root.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == 12 && rows.Select(row => row.GetProperty("Value").GetInt32()).SequenceEqual(
            new[] { 19, 4000, 9, 100, int.MaxValue, 0, 3500, 0, 100, 1, 4000, int.MaxValue }) &&
            rows[0].GetProperty("Before").Deserialize<RoomCombatState>()!.Context!.LifeLinks!.Actors.Select(actor => actor.Health)
                .SequenceEqual(new[] { 1555, 2750, 3500, 3500, 3500 }), "Incomplete native damage/heal chain or original starting health.");
        var sharedRoots = new List<RoomCombatState>();
        var immutableRoots = new List<string>();
        TrainCombatState? frozenChain = null;
        string? frozenParent = null;
        TrainCombatState? originalParent = null;
        int damageCount = 0, healCount = 0, recovered = 0, protectedLethal = 0;
        foreach (FixtureValue row in rows)
        {
            RoomCombatState before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            int target = row.GetProperty("TargetId").GetInt32(), value = row.GetProperty("Value").GetInt32();
            string operation = row.GetProperty("Operation").GetString()!;
            CombatUnit original = before.Units.Single(unit => unit.Id == target);
            Require(!before.Preview && before.RoomIndex is 1 or 2 && before.ExternalInteractions.All(item => item == "Outer train boss") &&
                (before.RoomIndex != 2 || before.ExternalInteractions.Contains("Outer train boss")) &&
                original.AssetKey == TrueFinalBossChecks.Assets[before.RoomIndex + 2] && row.GetProperty("AllBossesAlive").GetBoolean(),
                "Native target was not an original middle/top Entropy or unmodeled controller interactions were present.");
            string immutable = Json(before);
            RoomCombatResult result = operation == "Damage" ? RoomCombatModel.ApplyCardDamage(before, target, value) :
                RoomCombatModel.ApplyCardHeal(before, target, value);
            string? difference = result.Supported ? ModelJson.Difference(Json(result.State!), Json(actual)) : result.UnsupportedReason;
            Require(result.Supported && difference == null && immutable == Json(before),
                $"Native Entropy {operation}/{value} differs at {row.GetProperty("Index").GetInt32()}: {difference}");
            TrainCombatState Train(RoomCombatState room)
            {
                EnchantmentWorld captured = room.Context!.Enchantments!;
                return new TrainCombatState(captured.Rooms.Select(floor => new RoomCombatState(floor.RoomIndex,
                    floor.Deployment, floor.Units, floor.ExternalInteractions, room.Context, floor.Preview, floor.ActiveCompanionBossIds)).ToArray(),
                    captured.Movement, captured.EnemySlotsPerRoom, room.Context);
            }
            TrainCombatState beforeTrain = Train(before);
            string immutableTrain = Json(beforeTrain);
            LinkedBossHealthResult wholeTrain = operation == "Damage" ? LinkedBossHealthModel.ApplyDamage(beforeTrain, target, value) :
                LinkedBossHealthModel.ApplyHeal(beforeTrain, target, value);
            Require(wholeTrain.Supported && ModelJson.Difference(Json(wholeTrain.State!), Json(Train(actual))) == null && Json(beforeTrain) == immutableTrain,
                "Full native train/world damage or heal propagation differs: " + operation + "/" + value + ": " + wholeTrain.UnsupportedReason);
            originalParent ??= beforeTrain;
            frozenParent ??= Json(originalParent);
            frozenChain ??= beforeTrain;
            LinkedBossHealthResult chained = operation == "Damage" ? LinkedBossHealthModel.ApplyDamage(frozenChain, target, value) :
                LinkedBossHealthModel.ApplyHeal(frozenChain, target, value);
            Require(chained.Supported && ModelJson.Difference(Json(chained.State!), Json(Train(actual))) == null,
                "Paid native mixed-floor damage/heal chain differs from one frozen train root: " + operation + "/" + value + ": " + chained.UnsupportedReason);
            frozenChain = chained.State;
            LifeLinkContext world = actual.Context!.LifeLinks!;
            LinkedHealthValue[] health = row.GetProperty("Health").Deserialize<LinkedHealthValue[]>()!;
            Require(world.Actors.All(actor => actor.Health == health.Single(item => item.Id == actor.Id).Health) &&
                health.Skip(2).Select(actor => actor.Health).Distinct().Count() == 1,
                "Paid native damage/healing lost global physical shared health.");
            FixtureValue[] rngBefore = row.GetProperty("RngBefore").EnumerateArray().ToArray(), rngAfter = row.GetProperty("RngAfter").EnumerateArray().ToArray();
            Require(rngBefore.Length == 16 && rngAfter.Length == 16 && rngBefore.Zip(rngAfter).All(pair =>
                pair.First.GetProperty("Id").GetString() is "NonDeterministic" or "Chatter" || pair.First.ContentEquals(pair.Second)),
                "Native paid damage/healing changed an unmodeled gameplay RNG stream.");
            if (operation == "Damage")
            {
                damageCount++;
                if (value >= original.Health && actual.Units.Single(unit => unit.Id == target).Health == 1) protectedLethal++;
            }
            else
            {
                healCount++;
                if (actual.Units.Single(unit => unit.Id == target).Health > original.Health) recovered++;
            }
            sharedRoots.Add(before); immutableRoots.Add(immutable);
        }
        Require(damageCount == 7 && healCount == 5 && protectedLethal >= 2 && recovered >= 2,
            "Native chain lacks protected lethal hits, actual healing or alternating physical floors.");
        RoomCombatState seed = rows[0].GetProperty("Before").Deserialize<RoomCombatState>()!;
        Parallel.For(0, 64, index =>
        {
            int selected = index % rows.Length; FixtureValue row = rows[selected];
            RoomCombatState before = sharedRoots[selected];
            int target = row.GetProperty("TargetId").GetInt32(), value = row.GetProperty("Value").GetInt32();
            RoomCombatResult result = row.GetProperty("Operation").GetString() == "Damage" ? RoomCombatModel.ApplyCardDamage(before, target, value) :
                RoomCombatModel.ApplyCardHeal(before, target, value);
            Require(result.Supported && ModelJson.Difference(Json(result.State!), Json(row.GetProperty("Actual").Deserialize<RoomCombatState>()!)) == null,
                "Concurrent paid native damage/heal branch differs.");
        });
        Require(!RoomCombatModel.Exchange(seed).Supported && !RoomCombatModel.Resolve(seed).Supported &&
            sharedRoots.Select(state => Json(state)).SequenceEqual(immutableRoots) &&
            Json(originalParent!) == frozenParent &&
            TrueFinalBossChecks.Assets.All(asset => !OuterBossDefinitions.Supports(asset)),
            "Paid damage/healing must not admit uncalibrated TFB attack scheduling or the Level8 controller.");
        Console.WriteLine("TRUE-FINAL-BOSS-DAMAGE PASS: 12 paid original live coroutine calls; full room/train/global context/status/RNG match, linked floor health, lethal protection, actual healing and 64 immutable concurrent branches. Attack scheduling, terminal death and full battle remain guarded.");
    }
}
