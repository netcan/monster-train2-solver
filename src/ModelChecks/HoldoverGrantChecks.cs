using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HoldoverGrantChecks
{
    private const string Torch = "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b";
    private const string Sting = "356a00a4-010b-462b-b683-2ef296d076a8";

    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        var root = document.RootElement;
        var definition = root.GetProperty("OriginalHoldoverGrantDefinition");
        Require(root.GetProperty("OriginalHoldoverGrantPrepared").GetBoolean() &&
            definition.ContentEquals(root.GetProperty("OriginalHoldoverGrantDefinitionAfter")) &&
            Text(definition, "Id") == RelicHoldoverGrantModel.OwnerId && Text(definition, "Key") == RelicHoldoverGrantModel.OwnerKey &&
            Text(definition, "Type") == RelicHoldoverGrantModel.Effect && Text(definition, "CardType") == "Spell" &&
            Text(definition, "SourceTeam") == "Monsters" && Int(definition, "Conditions") == 0 &&
            !definition.GetProperty("PlacementExcluded").GetBoolean() && definition.GetProperty("ExcludedTraits").GetArrayLength() == 0 &&
            definition.GetProperty("Traits").EnumerateArray().Select(trait => trait.GetString()).SequenceEqual([CardRetainModel.Trait]),
            "Original Holdover grant definition differs.");

        int carrierId = Int(root, "OriginalHoldoverGrantTorch");
        int torchAdded = 0, stingAdded = 0, ineligible = 0, afterDraw = 0;
        foreach (FixtureValue callback in root.GetProperty("OriginalHoldoverGrantCallbacks").EnumerateArray())
        {
            FixtureValue[] before = callback.GetProperty("Before").EnumerateArray().ToArray();
            FixtureValue[] after = callback.GetProperty("After").EnumerateArray().ToArray();
            Require(before.Length == after.Length, "The grant changed the hand roster.");
            bool cardAdded = Text(callback, "Kind") == "CardAdded";
            if (cardAdded) Require(before.Length == 1, "A hand-entry callback must inspect exactly one card.");
            else
            {
                Require(Text(callback, "Kind") == "AfterDraw", "Unknown Holdover callback.");
                afterDraw++;
            }
            foreach (var (oldCard, newCard) in before.Zip(after))
            {
                int id = Int(oldCard, "Id");
                string? data = Text(oldCard, "DataId");
                bool eligible = Text(oldCard, "Type") == "Spell" && !oldCard.GetProperty("Champion").GetBoolean();
                bool[] oldTrait = Pending(oldCard);
                bool[] newTrait = Pending(newCard);
                Require(id == Int(newCard, "Id") && data == Text(newCard, "DataId") &&
                    Text(oldCard, "Type") == Text(newCard, "Type") &&
                    oldCard.GetProperty("Champion").ContentEquals(newCard.GetProperty("Champion")) &&
                    newTrait.Length <= 1, "Native Holdover changed card identity or duplicated the trait.");
                bool installed = oldTrait.Length == 0 && newTrait.SequenceEqual([false]);
                Require(installed ? eligible : oldTrait.SequenceEqual(newTrait), "Native Holdover altered an ineligible or pre-existing trait.");
                if (cardAdded)
                {
                    Require(callback.GetProperty("Fired").GetBoolean() == installed, "The hand-entry return value did not match its grant.");
                    if (installed && id == carrierId && data == Torch) torchAdded++;
                    if (installed && data == Sting) stingAdded++;
                    if (!eligible && oldTrait.Length == 0 && newTrait.Length == 0) ineligible++;
                }
            }
        }
        Require(torchAdded == 1 && stingAdded > 0 && ineligible > 0 && afterDraw >= 2,
            "Expected a native Torch draw, generated Sting, non-spell and repeated post-draw callback.");

        FixtureValue torchPlay = root.GetProperty("Actions").EnumerateArray().Single(row =>
            Int(row.GetProperty("Action"), "CardInstanceId") == carrierId);
        var actionBefore = torchPlay.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
        var actionAfter = torchPlay.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
        Require(actionBefore.FindCard(carrierId)?.DataId == Torch && TraitPending(actionBefore, carrierId) == false &&
            actionAfter.Cards.Discard.Any(card => card.InstanceId == carrierId) &&
            TraitPending(actionAfter, carrierId) == true, "The actual cast and played discard did not arm temporary Holdover.");
        FixtureValue returnTurn = root.GetProperty("Turns").EnumerateArray().First(row =>
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
            return TraitPending(before, carrierId) == true;
        });
        var afterReturn = returnTurn.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
        Require(afterReturn.Cards.Hand.Any(card => card.InstanceId == carrierId) &&
            TraitPending(afterReturn, carrierId) == false, "The played Torch did not spend its pending Holdover to return.");

        CardCycleChecks.Native(root);
        SolverStageChecks.Native(path);
        Console.WriteLine($"HOLDOVER-GRANT PASS torchDraw={torchAdded} generatedSting={stingAdded} ineligible={ineligible} afterDraw={afterDraw}");
    }

    private static bool? TraitPending(CombatContext context, int id) => context.FindCard(id)?.MaskDescriptor?
        .TemporaryTraits.SingleOrDefault(trait => trait.RuntimeType == CardRetainModel.Trait)?.HoldoverPending;
    private static bool[] Pending(FixtureValue card) =>
        card.GetProperty("Holdover").EnumerateArray().Select(flag => flag.GetBoolean()).ToArray();
    private static int Int(FixtureValue row, string key) => row.GetProperty(key).GetInt32();
    private static string? Text(FixtureValue row, string key) => row.GetProperty(key).GetString();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}
