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
        PlayCardAction nativeAction = play.GetProperty("Action").Deserialize<PlayCardAction>()!;
        ElixirDraftOptions newDraft = ElixirMixModel.NewDraft(catalog, initialRng, 2, null);
        string[] expectedCards = ["3cf25a9b-daa1-42cd-8174-eb0f59cd590f", "af13aa32-5442-408a-b491-229362c634f8"];
        if (!newDraft.Offers.Select(offer => offer.Recipe.Creation.DataId).SequenceEqual(expectedCards) ||
            !newDraft.Offers.Select(offer => offer.Upgrade?.DataId).SequenceEqual(new[] {
                "af3c9ab2-882e-469c-8977-9d2c4653e5ee", "f51da38d-b88d-453e-bd0e-2aa5d130c4c0" }))
            throw new InvalidDataException("Modeled new-elixir offers differ from the native Mix UI.");
        if (nativeAction.MixSelections.Count != 2 || nativeAction.MixSelections[0].ChoiceIndex is < 0 or > 1 ||
            nativeAction.MixSelections[1].ChoiceIndex is < 0 or > 1 ||
            newDraft.Offers[nativeAction.MixSelections[0].ChoiceIndex].Recipe.Creation.DataId != generated[0])
            throw new InvalidDataException("Native Mix action does not select its generated recipe.");
        ElixirDraftOptions upgradeDraft = ElixirMixModel.UpgradeDraft(
            newDraft.Offers[nativeAction.MixSelections[0].ChoiceIndex].Recipe, newDraft.Rng, 2);
        string[] expectedUpgrades = generated[0] == expectedCards[0]
            ? ["af3c9ab2-882e-469c-8977-9d2c4653e5ee", "92642b86-8c21-44c5-adbe-953bf8af9d71"]
            : ["da35ff94-4c9e-4dfa-a629-e74885c2d48e", "b6d7bf37-722c-4016-9416-d83769720d87"];
        if (!upgradeDraft.Offers.Select(offer => offer.Upgrade?.DataId).SequenceEqual(expectedUpgrades) ||
            upgradeDraft.Offers[nativeAction.MixSelections[1].ChoiceIndex].Upgrade?.DataId != selectedUpgrade ||
            !upgradeDraft.Rng.Equals(play.GetProperty("Actual").GetProperty("Spawn").GetProperty("Train")
                .GetProperty("Context").GetProperty("BattleRng").Deserialize<UnityRng>()))
            throw new InvalidDataException("Modeled upgrade offers or Battle RNG differ from native Mix.");
        BattleTurnState firstDecision = play.GetProperty("Before").Deserialize<BattleTurnState>()!;
        if (BattleActionModel.PlayCard(firstDecision,
                new PlayCardAction(nativeAction.CardInstanceId, nativeAction.RoomIndex)).Rejection != ActionRejection.Unsupported)
            throw new InvalidDataException("A Mix draft without a choice was accepted or treated as an illegal card.");
        var branches = BattleActionModel.EnumerateActions(firstDecision).SupportedCandidates
            .Where(candidate => candidate.Action.CardInstanceId == nativeAction.CardInstanceId &&
                candidate.Action.RoomIndex == nativeAction.RoomIndex && candidate.Action.MixSelections.Count == 2).ToArray();
        string[] choices = branches.Select(branch => string.Join(",", branch.Action.MixSelections
            .Select(selection => selection.ChoiceIndex))).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        if (!choices.SequenceEqual(new[] { "0,0", "0,1", "1,0", "1,1" }) ||
            branches.Select(branch => branch.Result.State!.Spawn.Train.Context!.MixState!.LastCardDataId + ":" +
                branch.Result.State.Spawn.Train.Context.MixState.LastUpgradeDataId).Distinct().Count() != 4)
            throw new InvalidDataException("Mix search omitted or merged one of the four two-draft choices.");
        if (generated[0] == expectedCards[1])
        {
            bool infusionPlayed = root.GetProperty("Actions").EnumerateArray().Skip(1).Any(entry =>
            {
                BattleTurnState beforePlay = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
                int cardId = entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
                CardInstanceState? card = beforePlay.Spawn.Train.Context!.FindCard(cardId);
                if (card?.DataId != generated[0]) return false;
                BattleTurnState afterPlay = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                return afterPlay.OtherPiles!.Any(pile => pile.Name == "DiscardBuffer" &&
                           pile.Cards.Any(item => item.InstanceId == cardId)) &&
                       afterPlay.Spawn.Train.Context!.FindCard(cardId)!.PlayCount == card.PlayCount &&
                       afterPlay.Spawn.Train.Context.Statistics!.Value(cardId, "TimesDiscarded", "ThisBattle") ==
                           beforePlay.Spawn.Train.Context.Statistics!.Value(cardId, "TimesDiscarded", "ThisBattle") &&
                       afterPlay.Spawn.Train.Context.Statistics.Value(cardId, "AnyStatusEffectStacksAdded", "ThisBattle") >
                           beforePlay.Spawn.Train.Context.Statistics.Value(cardId, "AnyStatusEffectStacksAdded", "ThisBattle");
            });
            if (!infusionPlayed)
                throw new InvalidDataException("Alternate Mix did not witness native Infusion status and discard routing.");
        }
        Console.WriteLine("NATIVE-MIX-CATALOG PASS: " + ids.Length + " recipes, " +
            before.EnumerateArray().Sum(recipe => recipe.GetProperty("Upgrades").GetArrayLength()) +
            " upgrade choices, four search branches, generated=" + generated[0] + " lastUpgrade=" + selectedUpgrade);
    }
}
