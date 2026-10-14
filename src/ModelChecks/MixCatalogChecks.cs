using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MixCatalogChecks
{
    internal static void Native(FixtureValue root)
    {
        FixtureValue play = root.GetProperty("Actions")[0];
        FixtureValue before = play.GetProperty("Before").GetProperty("PlayRules").GetProperty("Elixirs").GetProperty("Recipes");
        FixtureValue after = play.GetProperty("Actual").GetProperty("PlayRules").GetProperty("Elixirs").GetProperty("Recipes");
        string[] ReadIds(FixtureValue recipes) => recipes.EnumerateArray()
            .Select(recipe => recipe.GetProperty("Creation").GetProperty("DataId").GetString()!).ToArray();
        string[] ids = ReadIds(before);
        if (ids.Length < 2 || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length ||
            !ids.SequenceEqual(ReadIds(after)))
            throw new InvalidDataException("Native elixir recipe order or identity changed across Mix.");
        foreach (FixtureValue recipe in before.EnumerateArray())
        {
            if (recipe.GetProperty("Upgrades").GetArrayLength() == 0)
                throw new InvalidDataException("A captured original elixir has no upgrade choices.");
        }
        var hand = play.GetProperty("Actual").GetProperty("Spawn").GetProperty("Train")
            .GetProperty("Context").GetProperty("Cards").GetProperty("Hand");
        string[] generated = hand.EnumerateArray().Select(card => card.GetProperty("DataId").GetString()!)
            .Where(ids.Contains).ToArray();
        if (generated.Length != 1)
            throw new InvalidDataException("Original Mix did not leave exactly one catalog elixir in hand.");
        FixtureValue initialMix = play.GetProperty("Before").GetProperty("Spawn").GetProperty("Train")
            .GetProperty("Context").GetProperty("MixState");
        FixtureValue finalMix = play.GetProperty("Actual").GetProperty("Spawn").GetProperty("Train")
            .GetProperty("Context").GetProperty("MixState");
        if (initialMix.ValueKind == FixtureKind.Null ||
            initialMix.GetProperty("LastCardDataId").ValueKind != FixtureKind.Null ||
            initialMix.GetProperty("LastUpgradeDataId").ValueKind != FixtureKind.Null ||
            finalMix.GetProperty("LastCardDataId").GetString() != generated[0])
            throw new InvalidDataException("Native Mix selection state was not captured before and after the play.");
        FixtureValue selectedRecipe = before.EnumerateArray().Single(recipe =>
            recipe.GetProperty("Creation").GetProperty("DataId").GetString() == generated[0]);
        string? selectedUpgrade = finalMix.GetProperty("LastUpgradeDataId").GetString();
        if (selectedUpgrade == null || !selectedRecipe.GetProperty("Upgrades").EnumerateArray()
            .Any(upgrade => upgrade.GetProperty("DataId").GetString() == selectedUpgrade))
            throw new InvalidDataException("Native Mix selected an upgrade absent from its captured recipe.");
        var catalog = play.GetProperty("Before").GetProperty("PlayRules").GetProperty("Elixirs")
            .Deserialize<ElixirCatalog>()!;
        UnityRng initialRng = play.GetProperty("Before").GetProperty("Spawn").GetProperty("Train")
            .GetProperty("Context").GetProperty("BattleRng").Deserialize<UnityRng>();
        ElixirDraftOptions newDraft = ElixirMixModel.NewDraft(catalog, initialRng, 2, null);
        string[] expectedCards = ["3cf25a9b-daa1-42cd-8174-eb0f59cd590f", "af13aa32-5442-408a-b491-229362c634f8"];
        if (!newDraft.Offers.Select(offer => offer.Recipe.Creation.DataId).SequenceEqual(expectedCards) ||
            !newDraft.Offers.Select(offer => offer.Upgrade?.DataId).SequenceEqual(new[] {
                "af3c9ab2-882e-469c-8977-9d2c4653e5ee", "f51da38d-b88d-453e-bd0e-2aa5d130c4c0" }))
            throw new InvalidDataException("Modeled new-elixir offers differ from the native Mix UI.");
        ElixirDraftOptions upgradeDraft = ElixirMixModel.UpgradeDraft(newDraft.Offers[0].Recipe, newDraft.Rng, 2);
        if (!upgradeDraft.Offers.Select(offer => offer.Upgrade?.DataId).SequenceEqual(new[] {
                "af3c9ab2-882e-469c-8977-9d2c4653e5ee", "92642b86-8c21-44c5-adbe-953bf8af9d71" }) ||
            !upgradeDraft.Rng.Equals(play.GetProperty("Actual").GetProperty("Spawn").GetProperty("Train")
                .GetProperty("Context").GetProperty("BattleRng").Deserialize<UnityRng>()))
            throw new InvalidDataException("Modeled upgrade offers or Battle RNG differ from native Mix.");
        Console.WriteLine("NATIVE-MIX-CATALOG PASS: " + ids.Length + " recipes, " +
            before.EnumerateArray().Sum(recipe => recipe.GetProperty("Upgrades").GetArrayLength()) +
            " upgrade choices, generated=" + generated[0] + " lastUpgrade=" + selectedUpgrade);
    }
}
