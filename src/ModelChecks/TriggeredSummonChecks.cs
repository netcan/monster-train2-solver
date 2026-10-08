using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TriggeredSummonChecks
{
    internal static void Native(FixtureValue fixture)
    {
        string scenario = fixture.TryGetProperty("ModifierScenario", out var setting) ? setting.GetString() ?? "" : "";
        bool auraChildren = scenario.StartsWith("persistent-enchantment", StringComparison.Ordinal) && scenario.Contains("-summons");
        if (!scenario.StartsWith("triggered-summon", StringComparison.Ordinal) && !auraChildren) return;
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
            if (auraChildren) Require(record.GetProperty("Kind").GetString() == "OnStatusEffectChanged" &&
                actor.Triggers.Single(trigger => trigger.StateId == record.GetProperty("TriggerStateId").GetInt32()).Once &&
                rule.Count == 2, "Aura child summons require the original once-only status callback and two requested births.");
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
                if (auraChildren) Require(unit.BaseAttack == 9 && unit.MaxHealth == 32 &&
                    unit.Triggers.SelectMany(trigger => trigger.Effects).Any(effect => effect.Enchantment?.Bound == true &&
                        effect.Enchantment.State.PrimaryTargets.Count > 0),
                    "A summoned aura child did not bind and update before the native effect returned.");
                // OnSpawn was queued behind the current native effect. Its once marker
                // remains untouched until the outer queue resumes.
                Require(unit.Triggers.Where(trigger => trigger.Kind == "OnSpawn" || trigger.Kind == "OnHeal").All(trigger => !trigger.HasTriggered),
                    "A queued birth ran its spawn or extra-upgrade healing callback inside the creating effect.");
            }
            births += count;
        }
        Require(births >= 2 && (auraChildren || (scenario.Contains("death") ? deaths >= 2 : zero >= 2)),
            "The fixture did not reach its required live/zero-birth or dying-source paths.");
        Require(scenario.EndsWith("fresh", StringComparison.Ordinal) ? fresh == births : copied == births,
            "The fixture did not exercise the requested fresh/copied source path.");
        foreach (var clone in fixture.GetProperty("DetachedCardClones").EnumerateArray()) UnitSummonChecks.VerifyClone(clone, false);
        EffectBoundaries(records, scenario);
        if (scenario.Contains("equipment")) EquipmentTransfers(records, scenario);
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

    private sealed record Callback(int ActorId, string Kind, int DyingId, int ParamInt, int TriggerCount);
    private static void EffectBoundaries(FixtureValue[] records, string scenario)
    {
        var captured = records.Where(record => record.TryGetProperty("TriggerStateId", out _)).ToArray();
        if (captured.Length == 0) return;
        int owned = 0, liveOwnedBirths = 0, dyingOwnedBirths = 0;
        foreach (var record in captured)
        {
            var before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = record.GetProperty("After").Deserialize<RoomCombatState>()!;
            var actor = record.GetProperty("ActorBefore").Deserialize<CombatUnit>()!;
            int equipmentId = record.GetProperty("EquipmentCardId").GetInt32();
            int sourceId = record.GetProperty("SourceCardId").GetInt32();
            Require(sourceId == actor.SpawnerCardId, "An equipment-bound trigger replaced the native host source card.");
            if (sourceId > 0)
                Require(before.Context!.FindCard(sourceId)!.PlayedRoomUnitIds!.SequenceEqual(before.Units
                    .Where(unit => unit.Health > 0 && unit.IsSpawning != true).Select(unit => unit.Id).OrderBy(id => id)),
                    "The native trigger did not cache its host card's complete live room membership.");
            var trigger = actor.Triggers.Single(item => item.StateId == record.GetProperty("TriggerStateId").GetInt32());
            Require(trigger.Origin!.EquipmentCardId == equipmentId && trigger.Origin.IsFromEquipment == (equipmentId > 0),
                "A native summon effect lost its separate equipment binding.");
            if (equipmentId > 0)
            {
                owned++;
                Require(sourceId != equipmentId && !record.GetProperty("RuleBefore").Deserialize<TriggeredSummonRule>()!.HasParentCard,
                    "Native equipment binding was confused with the parent card or effect source.");
                if (after.Context!.NextUnitId > before.Context!.NextUnitId)
                { if (actor.Health > 0) liveOwnedBirths++; else dyingOwnedBirths++; }
            }
            VerifyEffect(record);
        }
        if (scenario.Contains("equipment-owned"))
            Require(owned > 0 && (scenario.Contains("death") && !scenario.Contains("revival") ? dyingOwnedBirths > 0 : liveOwnedBirths > 0),
                "Equipment-owned summons did not reach the requested live/dead birth path.");
        Parallel.For(0, 16, _ => { foreach (var record in captured) VerifyEffect(record); });
        Console.WriteLine($"NATIVE-TRIGGERED-SUMMON-EFFECT-CHECKS PASS: {captured.Length} complete effect boundaries, " +
            $"{owned} equipment bindings, {liveOwnedBirths} live/{dyingOwnedBirths} dying equipment births, native source caches and accepted callbacks in 16 branches.");
    }
    internal static void VerifyEffect(FixtureValue record)
    {
        var before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var actor = record.GetProperty("ActorBefore").Deserialize<CombatUnit>()!;
        string parent = JsonSerializer.Serialize(before), originalActor = JsonSerializer.Serialize(actor);
        var result = RoomCombatModel.ApplyNativeTriggeredSummon(before, actor, record.GetProperty("TriggerStateId").GetInt32(),
            record.GetProperty("EffectIndex").GetInt32(), out var resultingRule);
        Require(result.Supported, "Native summon effect unsupported: " + result.UnsupportedReason);
        Compare(result.State, record.GetProperty("After").Deserialize<RoomCombatState>(), "Native summon effect room");
        Compare(result.State!.Units.Concat(result.RetainedUnits).Single(unit => unit.Id == actor.Id),
            record.GetProperty("ActorAfter").Deserialize<CombatUnit>(), "Native summon effect actor");
        Compare(resultingRule, record.GetProperty("RuleAfter").Deserialize<TriggeredSummonRule>(), "Native summon effect cache");
        var callbacks = result.PendingCallbacks.Where(item => item.Unit.Triggers.Any(trigger => trigger.Kind == item.Kind))
            .Select(item => new Callback(item.Unit.Id, item.Kind, item.DyingCharacter?.Id ?? 0, item.ParamInt, item.TriggerCount)).ToArray();
        Compare(callbacks, record.GetProperty("Queued").Deserialize<Callback[]>(), "Native summon accepted callbacks");
        if (record.TryGetProperty("FullQueued", out var full))
            Compare(result.PendingCallbacks.Where(item => item.Unit.Triggers.Any(trigger => trigger.Kind == item.Kind))
                .Select(UnitCloneCallback.From).ToArray(), full.Deserialize<UnitCloneCallback[]>(), "Native summon full callback payloads");
        Require(JsonSerializer.Serialize(before) == parent && JsonSerializer.Serialize(actor) == originalActor,
            "Native summon effect changed its parent or retained source actor.");
        var invalid = RoomCombatModel.ApplyNativeTriggeredSummon(before, actor, record.GetProperty("TriggerStateId").GetInt32(),
            -1, out var invalidRule);
        Require(!invalid.Supported && invalid.State == null && invalidRule == null && JsonSerializer.Serialize(before) == parent,
            "An invalid native summon cursor produced a partial child or changed the parent.");
    }
    private static void Compare<T>(T actual, T expected, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(actual), JsonSerializer.Serialize(expected));
        Require(difference == null, label + " differs: " + difference);
    }

    private static void EquipmentTransfers(FixtureValue[] records, string scenario)
    {
        int applications = 0, transferred = 0, excluded = 0, duplicates = 0, reused = 0;
        foreach (var record in records)
        {
            var before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = record.GetProperty("After").Deserialize<RoomCombatState>()!;
            var actor = record.GetProperty("ActorBefore").Deserialize<CombatUnit>()!;
            var rule = record.GetProperty("RuleBefore").Deserialize<TriggeredSummonRule>()!;
            if (actor.EquipmentCards!.Count == 0 || before.Context!.NextUnitId == after.Context!.NextUnitId) continue;
            applications++;
            var catalog = before.Context.SummonCatalog!;
            var candidates = actor.EquipmentCards.Select(id => before.Context.FindCard(id)!).ToArray();
            var returning = candidates.Where(card => catalog.Cards.Single(def => def.Creation.DataId == card.DataId).Equipment!.ReturnToHand).ToArray();
            var normal = candidates.Except(returning).ToArray();
            if (!scenario.Contains("equipment-owned"))
                Require(normal.Length == 2 && returning.Length == 1, "The equipped native actor did not retain both transfer and return-to-hand definitions.");
            if (!rule.IgnoreCardUpgrades && normal.Length > 0)
            {
                string parent = JsonSerializer.Serialize(before);
                var incomplete = new TriggeredSummonCatalog(catalog.Units, catalog.Cards.Select(definition =>
                    new SummonCardDefinition(definition.Creation, definition.SpawnCharacterId)).ToArray(), catalog.Rooms);
                var bad = new RoomCombatState(before.RoomIndex, before.Deployment, before.Units, before.ExternalInteractions,
                    before.Context.WithSummonCatalog(incomplete), before.Preview);
                Parallel.For(0, 16, branch =>
                {
                    var queued = new RoomCombatModel.QueuedCharacterTrigger(before.RoomIndex, actor, record.GetProperty("Kind").GetString()!);
                    var rejected = record.TryGetProperty("TriggerStateId", out var cursor)
                        ? RoomCombatModel.ApplyNativeTriggeredSummon(bad, actor, cursor.GetInt32(), record.GetProperty("EffectIndex").GetInt32(), out _)
                        : RoomCombatModel.ApplyQueuedCharacterTrigger(bad, queued, callback => { });
                    Require(!rejected.Supported && rejected.State == null &&
                        rejected.UnsupportedReason!.Contains("equipment attachment definition"),
                        "An incomplete equipment catalog produced a partial summon child.");
                });
                Require(JsonSerializer.Serialize(before) == parent, "Missing-equipment branches mutated the source state.");
            }
            var born = after.Units.Where(unit => unit.Id >= before.Context.NextUnitId).ToArray();
            foreach (CombatUnit unit in born)
            {
                excluded += returning.Length;
                Require(!unit.EquipmentCards!.Any(id => returning.Any(card => card.DataId == after.Context.FindCard(id)!.DataId)),
                    "Return-to-hand equipment was transferred to a summoned child.");
                if (rule.IgnoreCardUpgrades || normal.Length == 0)
                {
                    Require(unit.EquipmentCards.Count == 0 && !after.Context.OtherPiles!.Single(pile => pile.Name == "Standby")
                        .EquipmentConditions!.Any(condition => condition.HostUnitId == unit.Id),
                        "A fresh summoned child inherited equipment or its standby condition.");
                    continue;
                }
                var attached = unit.EquipmentCards.Single();
                var card = after.Context.FindCard(attached)!;
                Require(card.DataId == normal.Last().DataId && card.EquippedUnitId == unit.Id &&
                    card.Permanent.Upgrades.Any(upgrade => upgrade.Statuses.Any(status => status.Id == "spikes")),
                    "Equipment transfer lost original ordering, cloned modifiers or the last attached relationship.");
                var standby = after.Context.OtherPiles!.Single(pile => pile.Name == "Standby");
                Require(standby.EquipmentConditions!.Any(condition => condition.CardId == attached && condition.HostUnitId == unit.Id && !condition.ReturnToHand),
                    "Transferred equipment did not bind its native standby host.");
                Require(unit.Triggers.Where(trigger => trigger.Origin?.IsFromEquipment == true && trigger.Once)
                    .All(trigger => !trigger.HasTriggered), "Equipment callbacks ran inside the enclosing summon effect.");
                transferred++;
                if (candidates.Any(original => original.InstanceId == attached)) reused++;
                else
                {
                    duplicates++;
                    Require(after.Context.CardInstances!.Any(owned => owned.InstanceId == attached),
                        "Duplicated equipment did not join non-permanent deck ownership.");
                }
                if (actor.Health > 0) Require(!candidates.Any(original => original.InstanceId == attached),
                    "A living actor and its child shared the same attached equipment instance.");
            }
        }
        Require(applications > 0 && excluded > 0, "Native equipped-source summon coverage was not reached.");
        if (!scenario.EndsWith("fresh", StringComparison.Ordinal))
            Require(transferred >= 2 && duplicates > 0 && (!scenario.Contains("death") || reused > 0),
                "Native live duplication or dying-source reuse was not reached.");
        Console.WriteLine($"NATIVE-TRIGGERED-EQUIPMENT-CHECKS PASS: {applications} equipped applications, {transferred} final attachments, " +
            $"{duplicates} owned copies, {reused} reused originals, {excluded} return-to-hand exclusions; complete battle and queued API checks.");
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
