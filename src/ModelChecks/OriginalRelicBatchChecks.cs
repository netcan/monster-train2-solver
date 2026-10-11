using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalRelicBatchChecks
{
    private static readonly (string Key, string Id, string? Status, int Stacks)[] Artifacts =
    [
        ("MoltenManifold", "f141d334-b503-48ff-ac21-5f2ad27105dd", null, 0),
        ("SpawnWithRage", "55ca34e9-047b-4b93-b390-d8d228a43261", "buff", 3),
        ("UnitsGainLifesteal", "a6b0b657-8893-4cc8-bc62-ec09544c4306", "lifesteal", 2),
        ("ContingencyPlans", "0278309e-d15c-4041-a8dc-b76f360f306b", "spikes", 7)
    ];

    internal static void Native(FixtureValue root)
    {
        if (!root.TryGetProperty("ModifierScenario", out var scenario) || scenario.GetString() != "original-relic-batch") return;
        Require(root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("RelicCardModifiersEnabled").GetBoolean(), "Original relic batch lacks native modifier capture.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Original relic batch has nonzero " + field + ".");

        var rows = root.GetProperty("RelicCardModifiers").EnumerateArray().Select(row => (
            Before: row.GetProperty("Before").Deserialize<CardInstanceState>()!,
            Context: row.GetProperty("ContextBefore").Deserialize<CombatContext>()!,
            AfterContext: row.GetProperty("ContextAfter").Deserialize<CombatContext>()!,
            Relics: row.GetProperty("Relics").Deserialize<CombatRelicState[]>()!,
            Reset: row.GetProperty("ResetTemporary").GetBoolean(),
            Actual: row.GetProperty("Actual").Deserialize<CardInstanceState>()!,
            Dispatches: row.GetProperty("Dispatches").Deserialize<RelicCardModifierDispatch[]>()!,
            Notifications: row.GetProperty("Notifications").EnumerateArray().Select(item =>
                (item.GetProperty("RelicIndex").GetInt32(), item.GetProperty("EffectIndex").GetInt32())).ToArray())).ToArray();
        Require(rows.Length > 0 && rows.Any(row => row.Reset && row.Dispatches.Any(effect => effect.UpgradeAdded)) &&
            rows.Any(row => !row.Reset && row.Before.Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == "TinkerersToolbelt_Upgrade") &&
                row.Dispatches.Any(effect => effect.Returned && effect.UpgradeAdded)) &&
            rows.Any(row => row.Dispatches.Any(effect => !effect.Returned)),
            "Original batch lacks native reset, non-unique repeat stacking or failed X-cost filtering.");
        string ParentSnapshot() => string.Join("\n", rows.Select(row => Serialize(row.Before) + Serialize(row.Context) + Serialize(row.Relics)));
        string parent = ParentSnapshot();
        void CheckModifiers()
        {
            foreach (var row in rows)
            {
                CheckIdentities(row.Relics);
                var predicted = RelicCardModifierModel.Apply(row.Before, row.Relics, row.Reset, row.Context);
                Require(predicted.Supported, "Original relic modifier unsupported: " + predicted.UnsupportedReason);
                Equal(predicted.Card, row.Actual, "original relic complete card state");
                Equal(predicted.Dispatches, row.Dispatches, "original relic ordered effect/filter dispatch");
                Equal(predicted.Context!.Relics, row.AfterContext.Relics, "original relic condition state");
                Equal(predicted.Context.Statistics, row.AfterContext.Statistics, "original relic statistics");
                Require(predicted.Dispatches.Where(effect => effect.Returned).Select(effect => (effect.RelicIndex, effect.EffectIndex))
                    .SequenceEqual(row.Notifications), "Original relic notification order differs.");
            }
        }
        CheckModifiers();
        Parallel.For(0, 32, _ => CheckModifiers());
        Require(ParentSnapshot() == parent, "Original relic modifier branches mutated their parent.");

        int birthCount = 0, subtypeRejected = 0;
        var applied = Artifacts.Where(item => item.Status != null).ToDictionary(item => item.Key, _ => 0);
        foreach (var record in root.GetProperty("RelicSpawnStatuses").EnumerateArray())
        {
            Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "Original relic birth capture is incomplete or differs.");
            var before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = record.GetProperty("After").Deserialize<RoomCombatState>()!;
            int id = record.GetProperty("UnitId").GetInt32(), cardId = record.GetProperty("FromCardId").GetInt32();
            bool covenant = record.GetProperty("OnlyCovenants").GetBoolean();
            CheckIdentities(before.Context!.Relics!);
            string birthParent = Serialize(before);
            void CheckBirth()
            {
                var predicted = RelicSpawnStatusModel.CharacterAdded(before, id, cardId, covenant);
                Require(predicted.Supported, "Original relic birth unsupported: " + predicted.UnsupportedReason);
                Equal(predicted.State, after, "original relic complete birth state");
            }
            CheckBirth();
            Parallel.For(0, 32, _ => CheckBirth());
            Require(Serialize(before) == birthParent, "Original relic birth branches mutated their parent.");
            CombatUnit prior = before.Units.Single(unit => unit.Id == id), actor = after.Units.Single(unit => unit.Id == id);
            if (actor.Team == CombatTeam.Player && !actor.Subtypes.Contains("SubtypesData_Monstrosity"))
            {
                Require((actor.Status("spikes")?.Stacks ?? 0) == (prior.Status("spikes")?.Stacks ?? 0),
                    "Original ContingencyPlans applied to a non-Monstrosity birth.");
                subtypeRejected++;
            }
            foreach (var item in Artifacts.Where(item => item.Status != null))
                if (actor.Status(item.Status!)?.Stacks == (prior.Status(item.Status!)?.Stacks ?? 0) + item.Stacks)
                    applied[item.Key]++;
            birthCount++;
        }
        Require(birthCount > 0 && subtypeRejected > 0 && applied.Values.All(count => count > 0),
            "Some acquired original spawn-status artifacts never applied: " + string.Join(", ", applied.Select(pair => pair.Key + "=" + pair.Value)));

        int xPlays = 0, positivePayments = 0;
        foreach (var entry in root.GetProperty("Actions").EnumerateArray())
        {
            var before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            var card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
            var rule = before.PlayRules!.Cards.Single(item => item.DataId == card.DataId);
            if (rule.CostType != "ConsumeRemainingEnergy") continue;
            Require(card.Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == "TinkerersToolbelt_Upgrade" && upgrade.Stats.XCost == 2),
                "An actual X-cost play lacked the original MoltenManifold upgrade.");
            var actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(actual.Spawn.Train.Context!.FindCard(card.InstanceId)!.LastPlayedCost == before.Energy,
                "Original X-cost relic changed the actual ember payment.");
            xPlays++; if (before.Energy > 0) positivePayments++;
        }
        Require(xPlays > 0 && positivePayments > 0, "Original MoltenManifold was never used in a paid X-cost play.");
        int effectiveScaling = 0;
        foreach (var sample in root.GetProperty("DamageScaling").EnumerateArray())
        {
            var trait = sample.GetProperty("Trait").Deserialize<ScalingDamageTrait>()!;
            if (trait.Query.Type != "PlayedCost") continue;
            var context = sample.GetProperty("Before").Deserialize<CombatContext>()!;
            int owner = sample.GetProperty("OwnerCardId").GetInt32(), source = sample.GetProperty("DamageSourceCardId").GetInt32();
            var card = context.FindCard(owner)!;
            if (!card.Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == "TinkerersToolbelt_Upgrade")) continue;
            var withoutRelic = new CardModifiers(card.Temporary.Offsets,
                card.Temporary.Upgrades.Where(upgrade => upgrade.AssetKey != "TinkerersToolbelt_Upgrade").ToArray(),
                card.Temporary.PersistentHealth, card.Temporary.ExternalInteractions);
            var alternative = context.WithCard(card.WithModifiers(card.Permanent, withoutRelic));
            var predicted = DamageScalingModel.ApplyTrait(context, trait, owner, source, sample.GetProperty("IncomingDamage").GetInt32());
            var noRelic = DamageScalingModel.ApplyTrait(alternative, trait, owner, source, sample.GetProperty("IncomingDamage").GetInt32());
            Require(predicted.Supported && noRelic.Supported && predicted.Damage == sample.GetProperty("ActualDamage").GetInt32(),
                "Original X-cost relic damage scaling does not match native evidence.");
            if (noRelic.Damage != predicted.Damage) effectiveScaling++;
        }
        Require(effectiveScaling > 0, "Original MoltenManifold was acquired but never changed native X-cost damage.");
        Console.WriteLine($"NATIVE-ORIGINAL-RELIC-BATCH-CHECKS PASS: four unchanged artifacts, {rows.Length} exact manager calls, " +
            $"{birthCount} exact births ({string.Join(", ", applied.Select(pair => pair.Key + "=" + pair.Value))}), " +
            $"{xPlays} X-cost plays/{positivePayments} paid/{effectiveScaling} effective damage samples and 32 immutable branches.");
    }

    private static void CheckIdentities(IReadOnlyList<CombatRelicState> relics)
    {
        foreach (var item in Artifacts)
        {
            var relic = relics.SingleOrDefault(candidate => candidate.AssetKey == item.Key && candidate.DataId == item.Id);
            Require(relic != null, "Original artifact is missing or renamed: " + item.Key);
            if (item.Status == null)
                Require(relic!.EffectTypes.SequenceEqual(["RelicEffectAddTempUpgrade"]) && relic.CardModifiers?.Count == 1 &&
                    relic.CardModifiers[0].Upgrade?.AssetKey == "TinkerersToolbelt_Upgrade" &&
                    relic.CardModifiers[0].Upgrade?.Stats.XCost == 2, "Original MoltenManifold payload differs.");
            else
                Require(relic!.EffectTypes.SequenceEqual(["RelicEffectAddStatusEffectOnSpawn"]) && relic.SpawnStatuses?.Count == 1 &&
                    relic.SpawnStatuses[0].Statuses.Count == 1 && relic.SpawnStatuses[0].Statuses[0].Id == item.Status &&
                    relic.SpawnStatuses[0].Statuses[0].Stacks == item.Stacks &&
                    (item.Key == "ContingencyPlans" ? !relic.SpawnStatuses[0].SubtypeIsNone &&
                        relic.SpawnStatuses[0].RequiredSubtype == "SubtypesData_Monstrosity" : relic.SpawnStatuses[0].SubtypeIsNone),
                    "Original spawn-status payload differs: " + item.Key);
        }
    }
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal<T>(T expected, T actual, string label)
    {
        string? difference = ModelJson.Difference(Serialize(expected), Serialize(actual));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
