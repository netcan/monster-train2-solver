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

    internal static void DuplicateHand(FixtureValue fixture)
    {
        FixtureValue record = fixture.GetProperty("Actions")[11];
        var root = record.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var action = record.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext context = root.Spawn.Train.Context!;
        int[] hand = context.Cards.Hand.Select(card => card.InstanceId).ToArray();
        CardInstanceState copy = context.FindCard(29)!;
        CardInstanceState original = context.FindCard(28)!;
        CardUpgradeModifier upgrade = copy.Permanent.Upgrades.Single();
        Require(hand.SequenceEqual([29, 28, 15]) && action.RecursionSelections.Single().CardInstanceIds.SequenceEqual([29]) &&
            copy.DataId == original.DataId && original.Permanent.Upgrades.Count == 0 &&
            upgrade.AssetKey == "MassProduction_Upgrade" && upgrade.Stats.Cost == -1 && upgrade.Stats.XCost == 1,
            "Expected native Metallurgy to choose the upgraded copy of the two same-name Morsel Miners.");
        var actions = BattleActionModel.EnumerateActions(root, action.CardInstanceId, includeAbilities: false)
            .SupportedCandidates.Where(candidate => candidate.Action.RoomIndex == action.RoomIndex).ToArray();
        Require(actions.Length == 2, "The native hand must have two legal choices.");
        var labels = new Dictionary<int, string>();
        foreach (SupportedActionCandidate candidate in actions)
        {
            int chosen = candidate.Action.RecursionSelections.Single().CardInstanceIds.Single();
            int position = Array.IndexOf(hand, chosen);
            labels[chosen] = AdvisorForecastPresentation.Choices(candidate.Action, id =>
                AdvisorForecastPresentation.ChoiceCard("Morsel Miner", true,
                    AdvisorForecastPresentation.ChoiceHandPosition(position), id == 29 ? "费用-1，X费用-1" : "无升级"),
                UnexpectedSummon);
            Require(candidate.Result.State!.Spawn.Train.Context!.Cards.Hand.Single().InstanceId == (chosen == 29 ? 28 : 29),
                "The selected instance must determine which native card remains.");
        }
        Require(labels[29] == " · 选择 Morsel Miner（手牌左起第 1 张，费用-1，X费用-1）" &&
            labels[28] == " · 选择 Morsel Miner（手牌左起第 2 张，无升级）" &&
            AdvisorForecastPresentation.ChoiceCard("Pain Tithe", false, "ignored", "ignored") == "Pain Tithe" &&
            AdvisorForecastPresentation.ChoiceDeckPosition(4, 3) == "从上往下第 2 行，左起第 2 列",
            "The duplicate choice labels must distinguish both position and upgrades and preserve unique card titles.");
        BattleSearchStep[] steps = [new BattleSearchStep(action, false), new BattleSearchStep(null, true)];
        Require(ReferenceEquals(TentativeAdviceProgress.MatchedCard(steps, 0, 15, 1, -1, 0), action) &&
            TentativeAdviceProgress.MatchedCard(steps, 0, 15, 2, -1, 0) == null &&
            !TentativeAdviceProgress.Matches(steps, 0, 15, 1, -1, 0, true),
            "The callback may guide the accepted original choice while the next forecast step must wait.");
        Console.WriteLine("ADVISOR-DUPLICATE-CHOICE PASS: native Metallurgy alternatives resolve to different actionable hand labels.");
    }

    private static string UnexpectedSummon(string id) => throw new InvalidDataException("Unexpected summon choice " + id);
    private static string UnexpectedCard(int id) => throw new InvalidDataException("Unexpected card choice " + id);
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}
