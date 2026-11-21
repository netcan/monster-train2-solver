using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ElleChecks
{
    private const string TorchId = "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b";
    private const string UpgradeId = "55649b59-9112-4ef0-96bd-0c4a902579d2";
    private static readonly string[] SoulIds =
    [
        "52a5b3a3-f00e-480b-ac86-2349dd127b4f",
        "8e6a9fa8-87ff-4825-9715-96c6ce56d365",
        "a9024031-7d28-4fe7-8956-8ff02194a42a"
    ];

    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        var root = document.RootElement;
        var definition = root.GetProperty("OriginalElleDefinition");
        int tier = Int(definition, "Tier");
        Require(root.GetProperty("OriginalEllePrepared").GetBoolean() &&
            Text(root, "GameVersion") == "2.3.1" &&
            Text(root, "GameModuleMvid") == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            tier is >= 1 and <= 3 && Text(definition, "Id") == SoulIds[tier - 1] &&
            Text(definition, "Key") == $"ElleT{tier}" && Int(definition, "Count") == tier + 1 &&
            Text(definition, "Mask") == "OnlyStarterCards" && Text(definition, "Pool") == "Elle_UpgradePool" &&
            definition.ContentEquals(root.GetProperty("OriginalElleDefinitionAfter")), "Missing unchanged original Elle source.");

        var upgrade = definition.GetProperty("Upgrades").EnumerateArray().Single();
        Require(Text(upgrade, "Id") == UpgradeId && Text(upgrade, "Key") == "Elle_UpgradeDrawAndEmber",
            "The original Elle pool did not select its installed upgrade.");
        var cast = upgrade.GetProperty("Cast").EnumerateArray().ToArray();
        Require(cast.Select(effect => Text(effect, "Type")).SequenceEqual(
            new[] { "CardEffectGainEnergy", "CardEffectDrawAdditionalNextTurn" }) &&
            cast.All(effect => Int(effect, "Amount") > 0), "The Elle upgrade did not supply energy and next-turn draw.");

        int[] upgradedIds = CheckAcquisition(root.GetProperty("OriginalElleAcquisition"), tier + 1);
        var witness = root.GetProperty("OriginalElleWitness");
        int carrierId = Int(witness, "CarrierId");
        Require(Text(witness, "CarrierDataId") == TorchId && upgradedIds.Contains(carrierId),
            "The played Torch is missing from the native upgraded deck instances.");

        var action = root.GetProperty("Actions").EnumerateArray().Single(row =>
            row.GetProperty("ScenarioAction").GetBoolean() && Int(row.GetProperty("Action"), "CardInstanceId") == carrierId);
        var before = action.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = action.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var beforeContext = before.Spawn.Train.Context!;
        var afterContext = after.Spawn.Train.Context!;
        var torch = beforeContext.FindCard(carrierId);
        Require(beforeContext.Cards.Hand.Any(card => card.InstanceId == carrierId && card.DataId == TorchId) &&
            torch?.Permanent.Upgrades.Count(item => item.DataId == UpgradeId) == 1 &&
            afterContext.Cards.Discard.Any(card => card.InstanceId == carrierId),
            "Native play did not use and discard the actual permanently upgraded Torch.");
        int energyGain = Int(cast[0], "Amount");
        int nextDrawGain = Int(cast[1], "Amount");
        int energyMaximum = beforeContext.EnergyState?.Maximum ?? int.MaxValue;
        Require(before.Energy >= 1 && after.Energy == Math.Min(energyMaximum, before.Energy - 1 + energyGain) &&
            afterContext.Cards.DrawModifier == beforeContext.Cards.DrawModifier + nextDrawGain,
            "Native upgraded Torch did not pay its original cost and apply both OnCast resource changes.");

        BonusDrawChecks.Native(root);
        CardCycleChecks.Native(root);
        SolverStageChecks.Native(path);
        Console.WriteLine($"ELLE PASS tier={tier} upgraded={upgradedIds.Length} energy={energyGain} nextDraw={nextDrawGain} reattach=unchanged");
    }

    private static int[] CheckAcquisition(FixtureValue acquisition, int expectedCount)
    {
        var before = acquisition.GetProperty("Before");
        var after = acquisition.GetProperty("After");
        var cardsBefore = before.GetProperty("Cards").EnumerateArray().ToArray();
        var cardsAfter = after.GetProperty("Cards").EnumerateArray().ToArray();
        Require(cardsBefore.Length == cardsAfter.Length && cardsBefore.Select(card => Int(card, "Id")).Distinct().Count() == cardsBefore.Length &&
            cardsBefore.Any(card => card.GetProperty("IsChampion").GetBoolean() && !card.GetProperty("Eligible").GetBoolean()) &&
            !before.GetProperty("RewardsRng").ContentEquals(after.GetProperty("RewardsRng")) &&
            after.ContentEquals(acquisition.GetProperty("AfterReattach")), "Elle deck or Rewards RNG did not settle as expected when attached and reattached.");

        var gained = new List<int>();
        foreach (var (oldCard, newCard) in cardsBefore.Zip(cardsAfter))
        {
            Require(Int(oldCard, "Id") == Int(newCard, "Id") && Int(oldCard, "Slot") == Int(newCard, "Slot") &&
                Text(oldCard, "DataId") == Text(newCard, "DataId"), "Elle acquisition changed the owned deck roster.");
            var oldUpgrades = oldCard.GetProperty("UpgradeIds").EnumerateArray().Select(item => item.GetString()).ToArray();
            var newUpgrades = newCard.GetProperty("UpgradeIds").EnumerateArray().Select(item => item.GetString()).ToArray();
            if (oldUpgrades.SequenceEqual(newUpgrades)) continue;
            Require(oldCard.GetProperty("Eligible").GetBoolean() && !oldCard.GetProperty("IsChampion").GetBoolean() &&
                Text(oldCard, "DataId") == TorchId && !oldUpgrades.Contains(UpgradeId) &&
                newUpgrades.SequenceEqual(oldUpgrades.Append(UpgradeId)), "Elle changed an unrelated upgrade or an ineligible card.");
            gained.Add(Int(newCard, "Id"));
        }

        var recorded = acquisition.GetProperty("UpgradedIds").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        Require(gained.Count == expectedCount && gained.SequenceEqual(recorded), "Elle gained a different set or count of permanent upgrades.");
        return recorded;
    }

    private static int Int(FixtureValue value, string key) => value.GetProperty(key).GetInt32();
    private static string? Text(FixtureValue value, string key) => value.GetProperty(key).GetString();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}
