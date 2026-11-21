using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using MonsterTrain2Poju.Probe;

internal static class AdvisorChoicePresentationChecks
{
    internal static void Native(string cardPath, string draftPath)
    {
        using var cardArchive = ModelJson.ReadFixture(cardPath);
        FixtureValue cardAction = cardArchive.RootElement.GetProperty("Actions").EnumerateArray().Single(row =>
            row.GetProperty("Action").TryGetProperty("RecursionSelections", out FixtureValue selections) &&
            selections.GetArrayLength() != 0);
        var state = cardAction.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var played = cardAction.GetProperty("Action").Deserialize<PlayCardAction>()!;
        int target = played.RecursionSelections.Single().CardInstanceIds.Single();
        CardInstanceState selected = state.Spawn.Train.Context!.FindCard(target)!;
        string label = state.PlayRules!.Cards.Single(rule => rule.DataId == selected.DataId).AssetKey;
        Require(label == "PainTithe" && state.Spawn.Train.Context.Cards.Discard.Any(card => card.InstanceId == target),
            "Expected the native Pain Tithe discard-pile selection.");
        string cardText = AdvisorForecastPresentation.Choices(played,
            id => id == target ? label : throw new InvalidDataException("Wrong selected card identity."), UnexpectedSummon);
        Require(cardText == " · 选择 PainTithe", "The player summary omitted the captured card selection.");

        using var draftArchive = ModelJson.ReadFixture(draftPath);
        var draft = draftArchive.RootElement.GetProperty("Rows").EnumerateArray().Last()
            .GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(draft.MixSelections.Count == 2 &&
            draft.MixSelections.Select(choice => choice.EffectIndex).SequenceEqual([0, 1]) &&
            draft.MixSelections.All(choice => choice.ChoiceIndex == 0), "Expected the two actual native draft choices.");
        string draftText = AdvisorForecastPresentation.Choices(draft, UnexpectedCard, UnexpectedSummon);
        Require(draftText == " · 第 1 次备选中选第 1 项 · 第 2 次备选中选第 1 项",
            "The player summary omitted or combined the two native draft selections.");

        var repeated = new PlayCardAction(1, 0,
            mixSelections: [new ElixirMixSelection(2, 0, 0), new ElixirMixSelection(1, 1, 0), new ElixirMixSelection(1, 0, 1)]);
        Require(AdvisorForecastPresentation.Choices(repeated, UnexpectedCard, UnexpectedSummon) ==
            " · 第 1 次备选中选第 2 项 · 第 2 次备选中选第 1 项 · 第 3 次备选中选第 1 项" &&
            AdvisorForecastPresentation.Choices(new PlayCardAction(1, 0), UnexpectedCard, UnexpectedSummon) == "",
            "Repeated or absent choices changed the player summary order.");
        Console.WriteLine("ADVISOR-CHOICE-PRESENTATION PASS " + cardText.Trim() + draftText);
    }

    private static string UnexpectedSummon(string id) => throw new InvalidDataException("Unexpected summon choice " + id);
    private static string UnexpectedCard(int id) => throw new InvalidDataException("Unexpected card choice " + id);
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}
