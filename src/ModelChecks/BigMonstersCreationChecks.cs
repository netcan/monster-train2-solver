using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class BigMonstersCreationChecks
{
    internal static void Native(FixtureValue root)
    {
        const string shadow = "4dd2be98-c73c-4f96-865f-6c24cc42e91d";
        const string steward = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        const string tiny = "7127098b-96cb-445b-a15a-1293bcd8125c";
        int schema = root.GetProperty("Schema").GetInt32();
        if (schema is not (1 or 2) || root.GetProperty("Turn").GetInt32() != 1 ||
            root.GetProperty("Boundary").GetString() != "NativeBigMonstersCardCreation" ||
            root.GetProperty("GameVersion").GetString() != Game231FixtureCatalog.Version ||
            root.GetProperty("GameModuleMvid").GetString() != Game231FixtureCatalog.Module)
            throw new InvalidDataException("Expected the original 2.3.1 BigMonsters card creation component.");

        FixtureValue owner = root.GetProperty("Owner"), enhancer = root.GetProperty("Enhancer");
        if (owner.GetProperty("Id").GetString() != "af623737-a208-4b85-bce3-e1066b4f4bcd" ||
            owner.GetProperty("Key").GetString() != "BigMonsters" ||
            owner.GetProperty("Type").GetString() != "RelicEffectModifyMonsterCapacity" ||
            owner.GetProperty("Int").GetInt32() != 1 || !owner.GetProperty("All").GetBoolean() ||
            enhancer.GetProperty("Key").GetString() != "UnitUpgradeMakeTiny" ||
            enhancer.GetProperty("UpgradeId").GetString() != tiny ||
            enhancer.GetProperty("UpgradeKey").GetString() != "MakeOneCapacity" || enhancer.GetProperty("Size").GetInt32() != -99)
            throw new InvalidDataException("Expected the original mutator and Tiny Stone upgrade definitions.");

        FixtureValue[] control = root.GetProperty("Control").EnumerateArray().ToArray();
        FixtureValue[] active = root.GetProperty("Active").EnumerateArray().ToArray();
        if (control.Length != 3 || active.Length != 3 ||
            control.Concat(active).Select(row => row.GetProperty("RegistryId").GetInt32()).Where(id => id > 0).Distinct().Count() != 6 ||
            !Card(control[0], shadow, 6, 6, [], [], []) || !Card(active[0], shadow, 6, 6, [], [], []) ||
            !Card(control[1], shadow, 1, 1, [-99], [], [tiny]) || !Card(active[1], shadow, 1, 1, [-99], [], [tiny]) ||
            !Card(control[2], steward, 3, 3, [], [], []) || !Card(active[2], steward, 4, 3, [], [1], []) ||
            !CardHoverFixtureChecks.GameplayRng(root.GetProperty("RngBefore"), root.GetProperty("RngAfter")))
            throw new InvalidDataException("Native creation did not preserve the expected size, original upgrades, or gameplay RNG.");

        Console.WriteLine("NATIVE-BIG-MONSTERS-CREATION PASS original in-battle size and upgrade order.");
        if (schema == 2)
        {
            for (int index = 0; index < control.Length; index++)
            {
                Replay(control[index], false, index == 2 ? 3 : 6);
                Replay(active[index], true, index == 2 ? 3 : 6);
            }
            Console.WriteLine("NATIVE-BIG-MONSTERS-GENERATION PASS six exact native context successors.");
        }

        static void Replay(FixtureValue row, bool withOwner, int originalSize)
        {
            CombatContext before = row.GetProperty("Before").Deserialize<CombatContext>()!;
            CombatContext actual = row.GetProperty("After").Deserialize<CombatContext>()!;
            CardGenerationRule generation = row.GetProperty("Rule").Deserialize<CardGenerationRule>()!;
            int? capturedSize = generation.Pool.Single().SizeBeforeAddCardUpgrades;
            if (capturedSize != (withOwner ? originalSize : (int?)null) ||
                generation.Pool[0].DataId != row.GetProperty("DataId").GetString() ||
                generation.SelectedPoolIndex is not (null or 0) ||
                (before.Relics?.Any(RelicModel.IsOriginalBigMonsters) == true) != withOwner)
                throw new InvalidDataException("Captured native source size or mutator scope differs.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            // The controlled native API takes the exact observed CardData as its input.
            CardGenerationResult predicted = CardGenerationModel.Apply(before, generation.WithSelectedPoolIndex(0));
            if (!predicted.Supported || predicted.AddedCards.Count != 1 ||
                predicted.AddedCards[0].InstanceId != row.GetProperty("RegistryId").GetInt32())
                throw new InvalidDataException("Original native generation failed: " + predicted.UnsupportedReason);
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted.Context, ModelJson.Options),
                JsonSerializer.Serialize(actual, ModelJson.Options));
            if (difference != null || parent != JsonSerializer.Serialize(before, ModelJson.Options))
                throw new InvalidDataException("Original native generation context differs: " + difference);

            if (withOwner && originalSize < 6)
            {
                var missing = JsonSerializer.SerializeToNode(generation, ModelJson.Options)!;
                missing["Pool"]![0]!.AsObject().Remove(nameof(CardCreationRule.SizeBeforeAddCardUpgrades));
                CardGenerationResult unavailable = CardGenerationModel.Apply(before, missing.Deserialize<CardGenerationRule>(ModelJson.Options)!);
                if (unavailable.Supported || unavailable.UnsupportedReason?.StartsWith("BigMonsters requires", StringComparison.Ordinal) != true)
                    throw new InvalidDataException("An unobserved generated unit size must not be inferred.");
            }
        }

        static bool Card(FixtureValue row, string id, int size, int permanentSize, int[] permanent, int[] temporary, string[] upgrades) =>
            row.GetProperty("DataId").GetString() == id && row.GetProperty("Size").GetInt32() == size &&
            row.GetProperty("SizeWithoutTemporaryModifiers").GetInt32() == permanentSize &&
            row.GetProperty("PermanentSizeModifiers").EnumerateArray().Select(value => value.GetInt32()).SequenceEqual(permanent) &&
            row.GetProperty("TemporarySizeModifiers").EnumerateArray().Select(value => value.GetInt32()).SequenceEqual(temporary) &&
            row.GetProperty("PermanentUpgrades").EnumerateArray().Select(value => value.GetString()).SequenceEqual(upgrades);
    }
}
