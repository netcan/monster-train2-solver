using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class CardTraitCompositionChecks
{
    private const string Exhaust = "CardTraitExhaustState", Purge = "CardTraitSelfPurge",
        Piercing = "CardTraitIgnoreArmor", Deployment = "CardTraitDrawInDeploymentPhase";
    private static CardTraitValue Trait(string name, int value = 2, int runtime = 9) =>
        new(name, name, value, runtime, true, 0);
    private static CardTraitCompositionState State(CardTraitValue[] bases, CardTraitValue[]? temporary = null,
        CardTraitValue[][]? upgrades = null, string[]? removed = null, CardTraitReplacement[]? replacements = null,
        CardTraitValue[]? combined = null) => new(bases, temporary ?? [], upgrades ?? [], removed ?? [], replacements ?? [], combined ?? bases);
    private static CardTraitReplacement Replace(string source, string target) => new(source, target, target);
    internal static void Run()
    {
        var exhaust = Trait(Exhaust); var piercing = Trait(Piercing); var purge = Trait(Purge);
        var roots = new[] { exhaust, exhaust }; var upgrades = new[] { new[] { piercing } };
        var source = State(roots, upgrades: upgrades);
        roots[0] = purge; upgrades[0][0] = purge;
        var refreshed = CardTraitCompositionModel.Refresh(source);
        Require(refreshed.BaseTraits[0].RuntimeType == Exhaust && refreshed.CombinedTraits![0].RuntimeType == "CardTraitDummy" &&
            ReferenceEquals(refreshed.CombinedTraits[1], source.BaseTraits[1]) && refreshed.CombinedTraits[2].RuntimeType == Piercing &&
            refreshed.CombinedTraits[2].ParamInt == 2, "Trait lists were shared, earlier Exhaust retained, or upgrade parameter not reset.");
        var purged = CardTraitCompositionModel.Refresh(State([exhaust, purge], [exhaust]));
        Require(purged.BaseTraits[0].RuntimeType == Exhaust && purged.CombinedTraits!.Count(trait => trait.RuntimeType == Exhaust) == 0 &&
            purged.CombinedTraits.Count(trait => !trait.HasData) == 2, "SelfPurge did not suppress all combined Exhaust traits independently of the base list.");
        var chained = State([exhaust, exhaust], replacements: [Replace(Exhaust, Piercing), Replace(Piercing, Deployment)]);
        var first = CardTraitCompositionModel.Refresh(chained); var second = CardTraitCompositionModel.Refresh(first);
        Require(first.BaseTraits.Select(trait => trait.RuntimeType).SequenceEqual([Deployment, Exhaust]) &&
            second.BaseTraits.All(trait => trait.RuntimeType == Deployment) && first.BaseTraits[0].ParamInt == 2 &&
            chained.BaseTraits.All(trait => trait.ParamInt == 9), "Persistent replacements lost ordering, first-match semantics, parameter reset or parent isolation.");
        var removedTarget = CardTraitCompositionModel.Refresh(State([], upgrades: [[exhaust]], removed: [Piercing], replacements: [Replace(Exhaust, Piercing)]));
        Require(removedTarget.CombinedTraits!.Single().RuntimeType == Piercing && removedTarget.CombinedTraits[0].TemporaryReplacement,
            "Temporary retyping incorrectly rechecked target removal or lost its replacement marker.");
        var removedBase = CardTraitCompositionModel.Refresh(State([exhaust], removed: [Piercing], replacements: [Replace(Exhaust, Piercing)]));
        Require(!removedBase.BaseTraits[0].HasData && ReferenceEquals(removedBase.BaseTraits[0], removedBase.CombinedTraits![0]),
            "Permanent replacement did not preserve the native removed-target dummy.");
        var duplicate = CardTraitCompositionModel.Refresh(State([], [exhaust], [[exhaust, exhaust]], replacements: [Replace(Exhaust, Piercing)]));
        Require(duplicate.CombinedTraits!.Count == 1 && ReferenceEquals(duplicate.CombinedTraits[0], duplicate.TemporaryTraits[0]),
            "Existing original runtime type did not block temporary upgrade replacement.");
        var appended = CardTraitCompositionModel.Refresh(State([purge, piercing, exhaust], replacements: [Replace(Piercing, Deployment)], combined: []));
        Require(appended.BaseTraits.Select(trait => trait.RuntimeType).SequenceEqual([Purge, Exhaust, Deployment]),
            "Replacement ignored the previous combined list's insertion bound.");
        Console.WriteLine("TRAIT-COMPOSITION-CHECKS PASS: ordered persistent/temporary replacements, removed types, parameter reset, Exhaust/SelfPurge, insertion bounds and immutable parents.");
    }

    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("ContextUnchanged").GetBoolean() &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("FrameBefore").ContentEquals(root.GetProperty("FrameAfter")) && root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")),
            "Native trait calibration changed context/RNG/frame or game identity.");
        var rows = root.GetProperty("Rows").EnumerateArray().Select(row => (
            Name: row.GetProperty("Name").GetString()!, Kind: row.GetProperty("Kind").GetString()!, Pass: row.GetProperty("Pass").GetInt32(),
            Before: row.GetProperty("Before").Deserialize<CardTraitCompositionState>()!,
            Actual: row.GetProperty("Actual").Deserialize<CardTraitCompositionState>()!, Origins: row.GetProperty("Origins").Deserialize<string[]>()!)).ToArray();
        Require(rows.Length == 2134 && rows.Count(row => row.Kind == "DefinitionTraits") == 1336 &&
            rows.Count(row => row.Kind == "OwnedTraits") == 30 && rows.Count(row => row.Kind == "Controlled") == 768 &&
            rows.Where(row => row.Kind == "DefinitionTraits").Select(row => row.Name).Distinct().Count() == 668,
            "Native trait refresh scope differs.");
        string parent = JsonSerializer.Serialize(rows.Select(row => new { row.Before, row.Actual, row.Origins }));
        void Check()
        {
            for (int i = 0; i < rows.Length; i += 2)
            {
                var first = rows[i]; var next = rows[i + 1];
                Require(first.Pass == 0 && next.Pass == 1 && first.Name == next.Name && first.Kind == next.Kind,
                    "Native trait refresh passes are incomplete or out of order.");
                Require(Equal(first.Actual, next.Before), $"Native carried trait input differs: {first.Kind}/{first.Name}.");
                var predicted = CardTraitCompositionModel.Refresh(first.Before);
                Compare(predicted, first.Actual, first.Origins, $"{first.Kind}/{first.Name}/0");
                Compare(CardTraitCompositionModel.Refresh(predicted), next.Actual, next.Origins, $"{next.Kind}/{next.Name}/1");
            }
        }
        Check(); Parallel.For(0, 32, _ => Check());
        Require(JsonSerializer.Serialize(rows.Select(row => new { row.Before, row.Actual, row.Origins })) == parent, "Trait composition replay mutated its parents.");
        Console.WriteLine("NATIVE-TRAIT-COMPOSITION-CHECKS PASS: 2134 exact refresh states/origins, 668 raw trait seeds, 15 owned cards, 384 controlled combinations, two carried refreshes and 32 immutable branches.");
    }
    private static bool Equal(CardTraitCompositionState first, CardTraitCompositionState second) => JsonSerializer.Serialize(first) == JsonSerializer.Serialize(second);
    private static void Compare(CardTraitCompositionState predicted, CardTraitCompositionState actual, string[] origins, string label)
    {
        Require(Equal(predicted, actual), $"Native trait composition differs: {label}. predicted={JsonSerializer.Serialize(predicted)} actual={JsonSerializer.Serialize(actual)}");
        var predictedOrigins = predicted.CombinedTraits!.Select(trait => {
            for (int index = 0; index < predicted.BaseTraits.Count; index++) if (ReferenceEquals(trait, predicted.BaseTraits[index])) return "base:" + index;
            for (int index = 0; index < predicted.TemporaryTraits.Count; index++) if (ReferenceEquals(trait, predicted.TemporaryTraits[index])) return "temporary:" + index;
            return "created";
        });
        Require(predictedOrigins.SequenceEqual(origins), $"Native trait composition ownership differs: {label}.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
