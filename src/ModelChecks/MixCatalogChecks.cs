using MonsterTrain2Poju.Fixtures;

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
        Console.WriteLine("NATIVE-MIX-CATALOG PASS: " + ids.Length + " recipes, " +
            before.EnumerateArray().Sum(recipe => recipe.GetProperty("Upgrades").GetArrayLength()) +
            " upgrade choices, generated=" + generated[0]);
    }
}
