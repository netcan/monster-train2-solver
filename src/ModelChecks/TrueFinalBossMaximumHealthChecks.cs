using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossMaximumHealthChecks
{
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static TrainCombatState Train(RoomCombatState room)
    {
        EnchantmentWorld world = room.Context!.Enchantments!;
        return new TrainCombatState(world.Rooms.Select(floor => new RoomCombatState(floor.RoomIndex, floor.Deployment,
            floor.Units, floor.ExternalInteractions, room.Context, floor.Preview, floor.ActiveCompanionBossIds)).ToArray(),
            world.Movement, world.EnemySlotsPerRoom, room.Context);
    }
    private static RoomCombatResult Apply(RoomCombatState before, FixtureValue row) => UnitHealthModel.ApplyNative(before,
        row.GetProperty("TargetId").GetInt32(), row.GetProperty("Amount").GetInt32(), row.GetProperty("Debuff").GetBoolean(),
        heal: row.GetProperty("Heal").GetBoolean(), floor: row.GetProperty("Floor").GetInt32(),
        decreaseHp: row.GetProperty("DecreaseHp").GetBoolean());
    private static TrainCombatState Step(TrainCombatState before, FixtureValue row)
    {
        int target = row.GetProperty("TargetId").GetInt32();
        RoomCombatState room = before.Rooms.Single(floor => floor.Units.Any(unit => unit.Id == target));
        RoomCombatResult applied = Apply(room, row);
        Require(applied.Supported && applied.PendingCallbacks.Count == 0 && applied.RetainedUnits.Count == 0,
            "Linked maximum-health API failed or produced an unexpected callback: " + applied.UnsupportedReason);
        return Train(applied.State!);
    }
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == "OriginalLiveLifeLinkMaximumHealthCoroutines" &&
            root.GetProperty("OriginalActorsAndStartingStats").GetBoolean() && root.GetProperty("ControllerExcluded").GetBoolean() &&
            root.GetProperty("NativeCompleted").GetBoolean(), "Missing current-version original live maximum-health API evidence.");
        FixtureValue[] rows = root.GetProperty("Rows").EnumerateArray().ToArray();
        string[] groupLabels = ["grow-unhealed", "zero", "negative", "subtract", "clip-only", "protected-lethal", "protected-one",
            "grow-one-unhealed", "hp-only-protection", "grow-and-heal", "clip-wounded", "raising-floor", "recover-group"];
        string[] tailLabels = ["reduce-dominion", "reduce-savagery", "one-dominion", "one-savagery", "unprotected-subtract", "unprotected-one",
            "unprotected-clip-floor", "unprotected-grow-one", "unprotected-clip-only", "restore-protector", "protected-again", "upper-floor", "zero-buff", "negative-buff"];
        Require(rows.Length == 79 && rows.Take(65).Select(row => row.GetProperty("Label").GetString()).SequenceEqual(
            Enumerable.Range(0, 5).SelectMany(_ => groupLabels)) && rows.Skip(65).Select(row => row.GetProperty("Label").GetString()).SequenceEqual(tailLabels),
            "Incomplete maximum-health matrix or dependent protection chain.");
        RoomCombatState initialRoom = rows[0].GetProperty("Before").Deserialize<RoomCombatState>()!;
        TrainCombatState initial = Train(initialRoom), chained = initial;
        string parent = Json(initial);
        CombatUnit[] bosses = initial.Context!.LifeLinks!.Actors.Select(actor => initial.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == actor.Id)).ToArray();
        Require(bosses.Select(unit => unit.AssetKey).SequenceEqual(TrueFinalBossChecks.Assets) &&
            bosses.Select(unit => unit.Health).SequenceEqual(new[] { 1555, 2750, 3500, 3500, 3500 }) && bosses.All(unit => unit.Health == unit.MaxHealth),
            "Native maximum-health chain did not start from original born boss statistics.");
        int protectedRows = 0, unprotectedRows = 0, sharedChanges = 0, noSignals = 0;
        foreach (FixtureValue row in rows)
        {
            int index = row.GetProperty("Index").GetInt32(), target = row.GetProperty("TargetId").GetInt32();
            string label = row.GetProperty("Label").GetString()!;
            RoomCombatState before = row.GetProperty("Before").Deserialize<RoomCombatState>()!, actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            string immutable = Json(before);
            Require(index == Array.IndexOf(rows, row) && row.GetProperty("AllBossesAlive").GetBoolean() && row.GetProperty("NativeQueueEmpty").GetBoolean() &&
                before.Context!.LifeLinks!.Actors.All(actor => actor.Initialized && actor.TrueFinalBoss && actor.HasLifeLink) &&
                (index >= 65 || target == bosses[index / 13].Id), "Native maximum-health actor order, liveness or queue boundary differs.");
            bool protection = row.GetProperty("Protected").GetBoolean();
            Require(LifeLinkModel.Protected(before.Context!.LifeLinks!, target) == protection,
                "Native maximum-health protection condition differs: " + index + "/" + label);
            if (protection) protectedRows++; else unprotectedRows++;
            RoomCombatResult predicted = Apply(before, row);
            string? difference = predicted.Supported ? ModelJson.Difference(Json(predicted.State!), Json(actual)) : predicted.UnsupportedReason;
            Require(predicted.Supported && difference == null && predicted.PendingCallbacks.Count == 0 && Json(before) == immutable,
                $"Native linked maximum-health successor differs at {index}/{label}: {difference}");
            chained = Step(chained, row);
            difference = ModelJson.Difference(Json(chained), Json(Train(actual)));
            Require(difference == null, $"Frozen-root mixed-floor maximum-health chain differs at {index}/{label}: {difference}");
            CombatUnit oldTarget = before.Units.Single(unit => unit.Id == target), next = actual.Units.Single(unit => unit.Id == target);
            HealthChangedEvent[] signals = row.GetProperty("Events").Deserialize<HealthChangedEvent[]>()!;
            Require(signals.Length == (oldTarget.Health == next.Health ? 0 : 1) && signals.All(signal => signal.Id == target && signal.Health == next.Health),
                "Native maximum-health HP signal did not remain local to the selected body.");
            if (signals.Length == 0) noSignals++;
            LifeLinkContext links = actual.Context!.LifeLinks!;
            CombatUnit[] physical = actual.Context.Enchantments!.Rooms.SelectMany(floor => floor.Units).Where(unit => links.Actors.Any(actor => actor.Id == unit.Id)).ToArray();
            Require(physical.Length == 5 && links.Actors.All(actor => physical.Single(unit => unit.Id == actor.Id).Health == actor.Health) &&
                physical.Where(unit => unit.AssetKey.StartsWith("Boss_Level9_Entropy_", StringComparison.Ordinal)).Select(unit => (unit.Health, unit.MaxHealth)).Distinct().Count() == 1,
                "Maximum health or current health failed to synchronize across the three physical Entropy rooms.");
            if (target == bosses[3].Id || target == bosses[4].Id) if (oldTarget.MaxHealth != next.MaxHealth) sharedChanges++;
            if (label == "protected-lethal") Require(next.Health == 1 && next.MaxHealth == 1 && protection, "Missing independent protected lethal maximum-health clamp.");
            if (label == "hp-only-protection") Require(next.Health == 1 && next.MaxHealth == 8 && protection, "Current-health clamp incorrectly reused the maximum-health clamp.");
            if (label == "raising-floor") Require(next.Health == 4 && next.MaxHealth == 20, "Native floor increases maximum health without changing its separate HP subtraction.");
            if (label == "unprotected-one") Require(!protection && next.Health == 1 && next.MaxHealth == 17, "Linked peers counted as independent LifeLink protectors.");
            if (label == "upper-floor") Require(protection && next.Health == 1 && next.MaxHealth == 99999, "Native upper floor witness differs.");
            if (!row.GetProperty("Debuff").GetBoolean() && row.GetProperty("Heal").GetBoolean() ||
                row.GetProperty("Debuff").GetBoolean() && row.GetProperty("Floor").GetInt32() == 0 && row.GetProperty("DecreaseHp").GetBoolean())
            {
                RoomCombatResult effect = UnitHealthModel.Apply(before, target, row.GetProperty("Amount").GetInt32(), row.GetProperty("Debuff").GetBoolean());
                Require(effect.Supported && ModelJson.Difference(Json(effect.State!), Json(actual)) == null, "Maximum-health card-effect entry differs from native API: " + label);
            }
            FixtureValue rngBefore = row.GetProperty("RngBefore"), rngAfter = row.GetProperty("RngAfter");
            Require(rngBefore.GetProperty("Streams").GetArrayLength() == 16 && rngAfter.GetProperty("Streams").GetArrayLength() == 16 &&
                rngBefore.GetProperty("Unity").GetArrayLength() == 4 && rngAfter.GetProperty("Unity").GetArrayLength() == 4 &&
                rngBefore.GetProperty("Streams").EnumerateArray().Zip(rngAfter.GetProperty("Streams").EnumerateArray()).All(pair =>
                    pair.First.GetProperty("Id").GetString() is "NonDeterministic" or "Chatter" || pair.First.ContentEquals(pair.Second)),
                "Maximum-health coroutine changed an unmodeled gameplay RNG stream.");
        }
        Require(protectedRows == 73 && unprotectedRows == 6 && sharedChanges > 0 && noSignals > 0 && Json(initial) == parent,
            "Missing protected/unprotected/shared/no-op evidence or mutated parent.");
        string final = Json(chained);
        Parallel.For(0, 16, branch =>
        {
            TrainCombatState state = initial;
            foreach (FixtureValue row in rows) state = Step(state, row);
            Require(Json(state) == final && Json(initial) == parent, "Concurrent maximum-health chain diverged or mutated its parent: " + branch);
        });
        FixtureValue unprotected = rows.Single(row => row.GetProperty("Label").GetString() == "unprotected-grow-one");
        RoomCombatState terminalParent = unprotected.GetProperty("Before").Deserialize<RoomCombatState>()!;
        string terminalImmutable = Json(terminalParent);
        Require(!UnitHealthModel.ApplyNative(terminalParent, unprotected.GetProperty("TargetId").GetInt32(), 99, debuff: true).Supported &&
            Json(terminalParent) == terminalImmutable, "Uncalibrated linked sacrifice must remain explicitly guarded.");
        Require(!UnitHealthModel.ApplyNative(new RoomCombatState(initialRoom.RoomIndex, initialRoom.Deployment, initialRoom.Units,
            initialRoom.ExternalInteractions, initialRoom.Context, true, initialRoom.ActiveCompanionBossIds), bosses[0].Id, 1, debuff: true).Supported,
            "Live maximum-health evidence incorrectly enabled linked temporary previews.");
        Console.WriteLine("TRUE-FINAL-BOSS-MAXIMUM-HEALTH PASS: 79 actual API coroutines, five original bodies, independent HP/max protection, three-room shared changes, protection release/recovery, native local HP signals, complete train/world and gameplay RNG, frozen-root replay and 16 immutable concurrent chains. Paid inputs, terminal sacrifice and temporary previews remain separate.");
    }
}
