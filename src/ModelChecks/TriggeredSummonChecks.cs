using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TriggeredSummonChecks
{
    internal static void Native(FixtureValue fixture)
    {
        string scenario = fixture.TryGetProperty("ModifierScenario", out var setting) ? setting.GetString() ?? "" : "";
        if (!scenario.StartsWith("triggered-summon", StringComparison.Ordinal)) return;
        var records = fixture.GetProperty("TriggeredSummons").EnumerateArray().ToArray();
        int births = 0, zero = 0, deaths = 0, copied = 0, fresh = 0;
        Require(records.Length >= 2, "Native triggered summon effects were not observed.");
        foreach (var record in records)
        {
            Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("Error").ValueKind == FixtureKind.Null &&
                record.GetProperty("QueueRunning").GetBoolean(), "Native summon observation or its queue boundary is incomplete.");
            var before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = record.GetProperty("After").Deserialize<RoomCombatState>()!;
            var actor = record.GetProperty("ActorBefore").Deserialize<CombatUnit>()!;
            var rule = record.GetProperty("RuleBefore").Deserialize<TriggeredSummonRule>()!;
            var resultingRule = record.GetProperty("RuleAfter").Deserialize<TriggeredSummonRule>()!;
            int count = after.Context!.NextUnitId!.Value - before.Context!.NextUnitId!.Value;
            Require(count >= 0 && after.Context.SummonCatalog != null && after.Context.SpawnPoints != null,
                "A triggered summon lost its definitions, positions or identity allocation.");
            if (actor.Health <= 0) deaths++;
            if (count == 0)
            {
                zero++;
                Require(rule.FirstSpawnedUnitId == resultingRule.FirstSpawnedUnitId,
                    "A zero-birth application overwrote its native first-birth cache.");
                continue;
            }
            Require(resultingRule.FirstSpawnedUnitId == before.Context.NextUnitId,
                "The native effect did not retain the first actual birth.");
            var born = after.Units.Where(unit => unit.Id >= before.Context.NextUnitId).ToArray();
            Require(born.Length == count, "The fixture lost a new triggered unit before its application boundary.");
            foreach (CombatUnit unit in born)
            {
                Require(unit.Status("cardless")?.Stacks == 1 && unit.SpawnerCardId != actor.SpawnerCardId &&
                    unit.IsSpawning == false, "Triggered birth lost its cardless marker, detached source or completed spawn state.");
                CardInstanceState? source = after.Context.FindCard(unit.SpawnerCardId);
                Require(source != null && !after.Context.CardInstances!.Any(card => card.InstanceId == source.InstanceId),
                    "Triggered source creation incorrectly added card ownership.");
                if (rule.IgnoreCardUpgrades) fresh++; else copied++;
                if (rule.Upgrade != null)
                    Require(unit.Modifiers!.Upgrades.Any(upgrade => upgrade.DataId == rule.Upgrade.DataId) &&
                        source!.Temporary.Upgrades.Any(upgrade => upgrade.DataId == rule.Upgrade.DataId),
                        "The extra spawn upgrade did not reach both the unit and its detached source.");
                // OnSpawn was queued behind the current native effect. Its once marker
                // remains untouched until the outer queue resumes.
                Require(unit.Triggers.Where(trigger => trigger.Kind == "OnSpawn" || trigger.Kind == "OnHeal").All(trigger => !trigger.HasTriggered),
                    "A queued birth ran its spawn or extra-upgrade healing callback inside the creating effect.");
            }
            births += count;
        }
        Require(births >= 2 && (scenario.Contains("death") ? deaths >= 2 : zero >= 2),
            "The fixture did not reach its required live/zero-birth or dying-source paths.");
        Require(scenario.EndsWith("fresh", StringComparison.Ordinal) ? fresh == births : copied == births,
            "The fixture did not exercise the requested fresh/copied source path.");
        var root = fixture.GetProperty("Stages").EnumerateArray().Select(sample => sample.GetProperty("Before").Deserialize<RoomCombatState>()!)
            .First(room => room.Units.Any(unit => unit.Triggers.Any(trigger => trigger.Effects.Any(effect => effect.Summon != null))));
        string parent = JsonSerializer.Serialize(root);
        var catalog = root.Context!.SummonCatalog!;
        var badContext = root.Context.WithSummonCatalog(new TriggeredSummonCatalog([], catalog.Cards, catalog.Rooms));
        var bad = new RoomCombatState(root.RoomIndex, root.Deployment, root.Units, root.ExternalInteractions, badContext, root.Preview);
        var rejected = RoomCombatModel.Exchange(bad);
        Require(!rejected.Supported && rejected.State == null && JsonSerializer.Serialize(root) == parent,
            "Uncaptured triggered definitions produced a partial child or mutated the parent.");
        DamagePhases(fixture, scenario);
        Console.WriteLine($"NATIVE-TRIGGERED-SUMMON-CHECKS PASS: {records.Length} native queued applications, {births} births, " +
            $"{zero} retained zero-birth caches, {deaths} dying sources, {copied} copies/{fresh} fresh sources; complete battle transitions checked independently.");
    }

    private static void DamagePhases(FixtureValue fixture, string scenario)
    {
        if (!fixture.TryGetProperty("TriggeredSummonDamages", out var captured)) return;
        var records = captured.EnumerateArray().Where(record => !record.GetProperty("QueueRunning").GetBoolean() &&
            record.TryGetProperty("Damage", out _)).ToArray();
        if (scenario.Contains("death")) Require(records.Length >= 3 && records.Any(record =>
            record.GetProperty("PendingDeathsBefore").EnumerateArray().Any()) && records.Any(record =>
            record.GetProperty("PendingDeathRooms").EnumerateArray().Any(room => room.GetInt32() !=
                record.GetProperty("Before").GetProperty("RoomIndex").GetInt32())),
            "Sequential and cross-room native damage/death phases were not captured.");
        foreach (var record in records)
        {
            Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("Error").ValueKind == FixtureKind.Null,
                "A native spell damage phase did not complete.");
            var before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var target = record.GetProperty("TargetAfter").Deserialize<CombatUnit>()!;
            var pending = record.GetProperty("PendingDeathsBefore").Deserialize<CombatUnit[]>()!;
            var beforeTrain = record.GetProperty("BeforeTrain").Deserialize<TrainCombatState>()!;
            var afterTrain = record.GetProperty("AfterTrain").Deserialize<TrainCombatState>()!;
            var deathRooms = record.GetProperty("PendingDeathRooms").Deserialize<int[]>()!;
            Require(deathRooms.Length == pending.Length, "A pending death lost its native room.");
            int source = record.GetProperty("SourceCardId").GetInt32();
            int attacker = record.GetProperty("AttackerUnitId").GetInt32();
            int damage = record.GetProperty("Damage").GetInt32();
            string original = JsonSerializer.Serialize(beforeTrain);
            System.Threading.Tasks.Parallel.For(0, 16, _ =>
            {
                var scaled = DamageScalingModel.Apply(before.Context, source, source, Math.Max(0, damage));
                Require(scaled.Supported, scaled.UnsupportedReason ?? "Native spell damage scaling failed.");
                var train = CardSpellModel.WithContext(beforeTrain, scaled.Context!);
                var deaths = pending.Select((unit, index) => new RoomCombatModel.QueuedCharacterTrigger(deathRooms[index], unit,
                    returnSpawnerAfterQueue: unit.Team == CombatTeam.Player && unit.SpawnerCardId > 0,
                    deferUntilRemoval: true, harvestAfterDeath: true, completePhysicalRemovalAfterQueue: true)).ToArray();
                var result = CardSpellModel.ApplyNativeDamageStep(train, before.RoomIndex, record.GetProperty("TargetId").GetInt32(),
                    Math.Max(0, scaled.Damage), source, deaths, attacker);
                string? difference = result.Supported ? ModelJson.Difference(JsonSerializer.Serialize(result.State),
                    JsonSerializer.Serialize(afterTrain)) : result.UnsupportedReason;
                Require(result.Supported && difference == null, "Native queued spell damage differs: " + difference);
                CombatUnit? resultingTarget = result.State!.Rooms.SelectMany(room => room.Units)
                    .Concat(result.PendingCallbacks.Select(queued => queued.Unit)).SingleOrDefault(unit => unit.Id == target.Id);
                string? targetDifference = resultingTarget == null ? "Missing retained victim." :
                    ModelJson.Difference(JsonSerializer.Serialize(resultingTarget), JsonSerializer.Serialize(target));
                Require(targetDifference == null, "Native damage target " + target.Id + " differs: " + targetDifference);
            });
            Require(JsonSerializer.Serialize(beforeTrain) == original, "Native damage branches mutated the parent.");
            if (pending.Length > 0)
            {
                var duplicate = new RoomCombatModel.QueuedCharacterTrigger(deathRooms[0], pending[0], deferUntilRemoval: true);
                var rejected = CardSpellModel.ApplyNativeDamageStep(beforeTrain, before.RoomIndex, target.Id, damage, source,
                    [duplicate, duplicate], attacker);
                Require(!rejected.Supported && rejected.State == null && JsonSerializer.Serialize(beforeTrain) == original,
                    "A duplicate pending death produced a partial child or mutated its parent.");
            }
        }
        if (records.Length > 0) Console.WriteLine($"NATIVE-TRIGGERED-SUMMON-DAMAGE-CHECKS PASS: {records.Length} complete native damage phases, " +
            "pending deaths, zero-HP victims and 16 independent branches.");
    }
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
