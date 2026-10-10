using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class CardOwnedMaskChecks
{
    internal static void Run()
    {
        var exhaust = new CardTraitValue("CardTraitExhaustState", "CardTraitExhaustState", 2, 9, true, 0);
        var composition = new CardTraitCompositionState([exhaust, exhaust], [], [], [],
            [new CardTraitReplacement("CardTraitExhaustState", "CardTraitIgnoreArmor", "CardTraitIgnoreArmor")], [exhaust, exhaust]);
        var clean = new CardTraitRefreshState(composition, false, 2, 3, 2, 3);
        Require(ReferenceEquals(CardTraitRefreshModel.Ensure(clean), clean), "Clean trait queries repeated pending replacements.");
        foreach (var dirty in new[] { new CardTraitRefreshState(composition, true, 2, 3, 2, 3),
            new CardTraitRefreshState(composition, false, 2, 3, 1, 3), new CardTraitRefreshState(composition, false, 2, 3, 2, 1) })
        {
            var refreshed = CardTraitRefreshModel.Ensure(dirty);
            Require(refreshed.Composition.BaseTraits.Select(trait => trait.RuntimeType).SequenceEqual(["CardTraitIgnoreArmor", "CardTraitExhaustState"]) &&
                !refreshed.Dirty && refreshed.CachedPermanentCount == 2 && refreshed.CachedTemporaryCount == 3 &&
                ReferenceEquals(CardTraitRefreshModel.Ensure(refreshed), refreshed), "Dirty counters or cached successive queries changed persistent replacement semantics.");
        }
        var definition = new CardMaskDefinition("card", "Monster", "Common", true, true, true, ["A"], [new("armor", 2)],
            [[new("poison", 1)]], ["main"], "clan", false, 3, 0, true, false, true);
        var visible = new CardMaskUpgrade("same", new(cost: -1, size: 2), [new("armor", 3, true)], true, false, false, false);
        var hidden = new CardMaskUpgrade("same", new(), [], true, true, false, false);
        var region = new CardMaskUpgrade("region", new(cost: -2, size: -8), [], true, false, true, true);
        var permanent = new CardMaskModifiers(new(cost: 1), [visible, hidden]); var temporary = new CardMaskModifiers(new(), [region]);
        var source = new CardOwnedMaskState(definition, 3, permanent, temporary, clean, ["cast"], true, false);
        var result = CardOwnedMaskModel.Resolve(source);
        Require(result.Card.CostWithoutTraits == 1 && result.Card.Size == 1 && CardOwnedMaskModel.Resolve(source, true).Card.CostWithoutTraits == 3 &&
            result.Card.VisibleUpgradeCount == 1 && result.Card.UpgradeIds.SequenceEqual(["same", "same", "region"]) &&
            result.Card.SpawnStatuses.Single().FromPermanentUpgrade && result.Card.SpawnStatuses[0].Count == 3 &&
            result.Card.HasUnitAbility && !result.Card.HasGraftedEquipment && result.Card.Effects.SequenceEqual(["main", "cast"]),
            "Owned view lost native cost/size, source flags, duplicate upgrades, icon/region filtering, ability, Purify/graft or effect order.");
        var effects = new[] { "trait-cast" };
        var trait = new CardTraitValue("CardTraitIgnoreArmor", "CardTraitIgnoreArmor", 0, 0, true, 0, parameterUpgradeCastEffects: effects);
        var traitSource = new CardTraitCompositionState([trait], [], [], [], [], [trait]);
        var other = new CardOwnedMaskState(definition, 3, permanent, temporary, new(traitSource, false, 1, 1, 1, 1), ["cast"], true, true);
        effects[0] = "mutated";
        var otherResult = CardOwnedMaskModel.Resolve(other);
        Require(otherResult.Card.HasGraftedEquipment && otherResult.Card.Effects.SequenceEqual(["main", "cast", "trait-cast"]), "Permanent graft or trait parameter effect definitions were lost/shared.");
        var dummy = new CardTraitValue("CardTraitDummy", null, 0, 0, false, 0);
        var dummySource = new CardTraitCompositionState([dummy], [], [], [], [new("CardTraitDummy", "CardTraitIgnoreArmor", "CardTraitIgnoreArmor")], [dummy]);
        var dummyResult = CardTraitCompositionModel.Refresh(dummySource);
        Require(dummyResult.BaseTraits.Single().Removable && dummyResult.BaseTraits[0].DeclaredName == "CardTraitIgnoreArmor",
            "Native non-null None definition could not be replaced or inherited the runtime dummy's removable flag.");
        string parent = JsonSerializer.Serialize(other);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(CardOwnedMaskModel.Resolve(other).Card) == JsonSerializer.Serialize(otherResult.Card), "Parallel owned card views differ."));
        Require(JsonSerializer.Serialize(other) == parent, "Owned card queries mutated their parent.");
        Console.WriteLine("OWNED-CARD-MASK-CHECKS PASS: lazy trait cache/counters, current-branch cost/size/status/effects/upgrades/ability/graft, icon/region filtering and immutable parents.");
    }
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("ContextUnchanged").GetBoolean() &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("FrameBefore").ContentEquals(root.GetProperty("FrameAfter")) && root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")) &&
            root.GetProperty("ParameterTraitPresent").GetBoolean(), "Native owned card mask calibration changed context/RNG/frame or omitted trait parameter effects.");
        var masks = root.GetProperty("Masks").Deserialize<CardUpgradeMaskRule[]>()!;
        var rows = root.GetProperty("Rows").EnumerateArray().Select(row => (
            Name: row.GetProperty("Name").GetString()!, Kind: row.GetProperty("Kind").GetString()!, Pass: row.GetProperty("Pass").GetInt32(),
            Before: row.GetProperty("Before").Deserialize<CardOwnedMaskState>()!, Actual: row.GetProperty("Actual").Deserialize<CardOwnedMaskState>()!,
            Card: row.GetProperty("Card").Deserialize<CardUpgradeMaskCard>()!, Cost: row.GetProperty("CostWithoutTemporary").GetInt32(),
            Results: row.GetProperty("Results").Deserialize<bool[]>()!)).ToArray();
        Require(rows.Length == 416 && masks.Length == 62 && rows.Count(row => row.Kind == "Owned") == 30 && rows.Count(row => row.Kind == "Controlled") == 384 &&
            rows.Count(row => row.Kind == "Edge") == 2 && rows.Any(row => row.Card.Traits.Contains(null!)) &&
            rows.Where(row => row.Kind == "Controlled").Select(row => row.Name).Distinct().Count() == 192 &&
            rows.Any(row => row.Card.VisibleUpgradeCount > 0) && rows.Any(row => row.Card.HasUnitAbility) && rows.Any(row => row.Card.XCost) &&
            rows.Any(row => row.Actual.Traits.Composition.CombinedTraits!.Any(trait => trait.ParameterUpgradeCastEffects.Count > 0)), "Native owned card view scope differs.");
        string parent = JsonSerializer.Serialize(rows.Select(row => new { row.Before, row.Actual, row.Card, row.Results }));
        void Compare(CardOwnedMaskResult predicted, int index)
        {
            var row = rows[index];
            Require(Equal(predicted.Card, row.Card), $"Native owned view differs: {row.Kind}/{row.Name}/{row.Pass}, predicted={JsonSerializer.Serialize(predicted.Card)}, actual={JsonSerializer.Serialize(row.Card)}.");
            Require(Equal(predicted.State, row.Actual), $"Native owned card state differs: {row.Kind}/{row.Name}/{row.Pass}, predicted={JsonSerializer.Serialize(predicted.State)}, actual={JsonSerializer.Serialize(row.Actual)}.");
            Require(row.Results.Length == masks.Length && CardOwnedMaskModel.Resolve(row.Before, true).Card.CostWithoutTraits == row.Cost, "Native permanent-only cost or filter outcome scope differs.");
            for (int mask = 0; mask < masks.Length; mask++)
                Require(CardUpgradeMaskModel.FilterCard(masks[mask], predicted.Card) == row.Results[mask], $"Native derived owned mask differs: {row.Name}/{masks[mask].AssetKey}.");
        }
        void Check()
        {
            for (int i = 0; i < rows.Length; i += 2)
            {
                Require(rows[i].Pass == 0 && rows[i + 1].Pass == 1 && rows[i].Name == rows[i + 1].Name && rows[i].Kind == rows[i + 1].Kind &&
                    Equal(rows[i].Actual, rows[i + 1].Before), "Native carried owned-card passes differ.");
                var predicted = CardOwnedMaskModel.Resolve(rows[i].Before); Compare(predicted, i);
                Compare(CardOwnedMaskModel.Resolve(predicted.State), i + 1);
            }
        }
        Check(); Parallel.For(0, 32, _ => Check());
        Require(JsonSerializer.Serialize(rows.Select(row => new { row.Before, row.Actual, row.Card, row.Results })) == parent, "Derived owned card view replay mutated its parents.");
        Console.WriteLine("NATIVE-OWNED-CARD-MASK-CHECKS PASS: 416 exact carried states/views, 15 owned cards, 192 controlled combinations and None replacement edge, 25792 native outcomes across 62 original filters, lazy trait caches, permanent-only costs and 32 immutable branches.");
    }
    private static bool Equal<T>(T first, T second) => JsonSerializer.Serialize(first) == JsonSerializer.Serialize(second);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
