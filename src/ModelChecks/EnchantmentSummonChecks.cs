using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EnchantmentSummonChecks
{
    internal static void Native(string path)
    {
        using var document = FixtureDocument.Read(path);
        var root = document.RootElement;
        var samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        foreach (var sample in samples) TriggeredSummonChecks.VerifyEffect(sample.GetProperty("Native"));
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Boundary").GetString() == "OriginalQueuedSpawnMonster" &&
            !root.GetProperty("GameplaySuppressed").GetBoolean() && !root.GetProperty("WholeBattleVerified").GetBoolean() &&
            root.GetProperty("Mismatches").GetInt32() == 0 && samples.Length == 2,
            "Queued aura summon calibration version, native boundary or completeness gate failed.");
        string scenario = root.GetProperty("ModifierScenario").GetString()!;
        Require(scenario is "persistent-enchantment-summons" or "persistent-enchantment-summons-fresh" or
            "persistent-enchantment-random-summons" or "persistent-enchantment-random-summons-fresh", "Unexpected aura summon scenario.");
        int births = 0, callbacks = 0;
        foreach (var sample in samples)
        {
            var record = sample.GetProperty("Native");
            Require(sample.GetProperty("Difference").ValueKind == FixtureKind.Null && record.GetProperty("Completed").GetBoolean() &&
                record.GetProperty("Error").ValueKind == FixtureKind.Null && record.GetProperty("QueueRunning").GetBoolean() &&
                record.GetProperty("Kind").GetString() == "OnStatusEffectChanged", "Incomplete original aura child application.");
            var before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = record.GetProperty("After").Deserialize<RoomCombatState>()!;
            var actor = record.GetProperty("ActorBefore").Deserialize<CombatUnit>()!;
            var rule = record.GetProperty("RuleBefore").Deserialize<TriggeredSummonRule>()!;
            var resulting = record.GetProperty("RuleAfter").Deserialize<TriggeredSummonRule>()!;
            var children = after.Units.Where(unit => unit.Id >= before.Context!.NextUnitId).ToArray();
            Require(rule.Count == 2 && rule.IgnoreCardUpgrades == scenario.EndsWith("-fresh", StringComparison.Ordinal) &&
                actor.Triggers.Single(trigger => trigger.StateId == record.GetProperty("TriggerStateId").GetInt32()).Once &&
                children.Length == 2 && after.Context!.NextUnitId - before.Context!.NextUnitId == children.Length &&
                resulting.FirstSpawnedUnitId == children[0].Id, "Missing finite once-only births or first-birth identity.");
            foreach (var child in children)
            {
                Require(child.BaseAttack == 9 && child.MaxHealth == 32 && child.Status("cardless")?.Stacks == 1 && child.IsSpawning == false &&
                    child.SpawnerCardId != actor.SpawnerCardId && after.Context!.FindCard(child.SpawnerCardId) is CardInstanceState source &&
                    !after.Context.CardInstances!.Any(card => card.InstanceId == source.InstanceId) &&
                    source.Temporary.Upgrades.Any(upgrade => upgrade.DataId == "ed1091ee-b111-4db1-b5e1-b69f15a50703") &&
                    child.Triggers.SelectMany(trigger => trigger.Effects).Any(effect => effect.Enchantment?.Bound == true &&
                        effect.Enchantment.State.PrimaryTargets.Count > 0) &&
                    child.Triggers.Where(trigger => trigger.Kind == "OnSpawn").All(trigger => !trigger.HasTriggered),
                    "Queued aura birth lost its actor/source upgrade, binding or deferred spawn callback.");
                bool fresh = rule.IgnoreCardUpgrades;
                Require(child.Modifiers!.SpawnerMatchesDefinition == fresh &&
                    (after.Context!.FindCard(child.SpawnerCardId)!.DataId == before.Context!.FindCard(actor.SpawnerCardId)!.DataId) != fresh,
                    "Fresh/copied aura births lost their fallback/source-character distinction.");
            }
            var auraRules = after.Context!.Enchantments!.Rooms.SelectMany(room => room.Units).SelectMany(unit => unit.Triggers)
                .SelectMany(trigger => trigger.Effects).Where(effect => effect.Enchantment != null).Select(effect => effect.Enchantment!).ToArray();
            bool random = scenario.StartsWith("persistent-enchantment-random", StringComparison.Ordinal);
            Require(auraRules.All(aura => aura.StatusPool.Count == (random ? 3 : 1)) &&
                (!random || !before.Context!.BattleRng.Equals(after.Context.BattleRng)),
                "Aura birth batches lost their authored pool or native Battle RNG advancement.");
            var queue = record.GetProperty("FullQueued").Deserialize<UnitCloneCallback[]>()!;
            Require(queue.Any(item => item.Kind == "OnSpawn") && queue.Any(item => item.Kind == "OnStatusEffectChanged") &&
                queue.Any(item => item.Kind == "CardMonsterPlayed" && item.LastSpawnedOverrideUnitId > 0),
                "Native aura births omitted ordered spawn/status/Rally callback payloads.");
            births += children.Length; callbacks += queue.Length;
        }
        foreach (var clone in root.GetProperty("SourceClones").EnumerateArray()) UnitSummonChecks.VerifyClone(clone, false);
        Parallel.For(0, 32, _ => { foreach (var sample in samples) TriggeredSummonChecks.VerifyEffect(sample.GetProperty("Native")); });
        Console.WriteLine($"NATIVE-ENCHANTMENT-SUMMON-CHECKS PASS: {samples.Length} original queued batches, {births} nested aura births, " +
            $"{callbacks} complete callback payloads, detached sources, final batch refresh and 32 immutable branches; whole-battle preview birth retention remains open.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
