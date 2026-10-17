using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalJudgesScalesChecks
{
    private const int Schema = 211;

    public sealed class TestRecord(int turn, bool placement, bool result, int[] eligibleCardIds)
    {
        public int Turn { get; } = turn;
        public bool Placement { get; } = placement;
        public bool Result { get; } = result;
        public int[] EligibleCardIds { get; } = eligibleCardIds;
    }

    public sealed class ChoiceRecord(int cardId, string dataId)
    {
        public int CardId { get; } = cardId;
        public string DataId { get; } = dataId;
    }

    public sealed class ApplicationRecord(int turn, bool placement, CombatContext before, CombatContext? after,
        ChoiceRecord[] choices, bool cancelled, int suppressedFollowupSelections, int[] modelEligibleCardIds,
        int[] modelDiscardedCardIds, int[] modelDrawnCardIds, bool modelSupported,
        string? modelUnsupportedReason, string? difference, bool completed, string? error)
    {
        public int Turn { get; } = turn;
        public bool Placement { get; } = placement;
        public CombatContext Before { get; } = before;
        public CombatContext? After { get; } = after;
        public ChoiceRecord[] Choices { get; } = choices;
        public bool Cancelled { get; } = cancelled;
        public int SuppressedFollowupSelections { get; } = suppressedFollowupSelections;
        public int[] ModelEligibleCardIds { get; } = modelEligibleCardIds;
        public int[] ModelDiscardedCardIds { get; } = modelDiscardedCardIds;
        public int[] ModelDrawnCardIds { get; } = modelDrawnCardIds;
        public bool ModelSupported { get; } = modelSupported;
        public string? ModelUnsupportedReason { get; } = modelUnsupportedReason;
        public string? Difference { get; } = difference;
        public bool Completed { get; } = completed;
        public string? Error { get; } = error;
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == Schema &&
            root.GetProperty("OriginalJudgesScalesSchema").GetInt32() == Schema &&
            root.GetProperty("ModifierScenario").GetString() == "original-judges-scales" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Judges Scales battle is incomplete or from an unexpected capture schema.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Judges Scales battle has nonzero " + field + ".");

        FixtureValue evidence = root.GetProperty("OriginalJudgesScales");
        string ownerId = evidence.GetProperty("OwnerId").GetString()!;
        string carrierId = evidence.GetProperty("EligibleCarrierId").GetString()!;
        string persistentId = evidence.GetProperty("PersistentGuardId").GetString()!;
        string infusionId = evidence.GetProperty("InfusionGuardId").GetString()!;
        Require(ownerId == RelicJudgesScalesModel.DataId &&
            evidence.GetProperty("AssetKey").GetString() == RelicJudgesScalesModel.AssetKey &&
            evidence.GetProperty("EffectType").GetString() == RelicJudgesScalesModel.Effect &&
            carrierId == "fc119000-0000-4000-8000-000000000497" &&
            persistentId == "fc119000-0000-4000-8000-000000000498" &&
            infusionId == "fc119000-0000-4000-8000-000000000499" &&
            !evidence.GetProperty("DisallowedInPlacementPhase").GetBoolean() &&
            evidence.GetProperty("ParamInt").GetInt32() == 0 && evidence.GetProperty("ParamInt2").GetInt32() == 2 &&
            evidence.GetProperty("ParamMinInt").GetInt32() == 0 && evidence.GetProperty("ParamFloat").GetSingle() == 0 &&
            evidence.GetProperty("ConditionCount").GetInt32() == 0,
            "Original Judges Scales identity or native parameters changed.");

        TestRecord[] tests = root.GetProperty("RelicJudgesScalesTests").EnumerateArray()
            .Select(item => item.Deserialize<TestRecord>()!).ToArray();
        ApplicationRecord[] applications = root.GetProperty("RelicJudgesScalesApplications").EnumerateArray()
            .Select(item => item.Deserialize<ApplicationRecord>()!).ToArray();
        Require(tests.Length > 0 && tests.All(test => test.Result == (test.EligibleCardIds.Length > 0)) &&
            tests.Any(test => test.Result && test.Turn == 0 && test.Placement && test.EligibleCardIds.Contains(1)),
            "Native post-opening TestEffects lacks a placement-phase eligible-card positive control.");
        Require(applications.Length == 1 && applications.All(item => item.Completed && item.Error == null &&
            item.After != null && item.ModelSupported && item.ModelUnsupportedReason == null && item.Difference == null),
            "Native Judges Scales application is incomplete, unsupported or divergent.");

        ApplicationRecord application = applications.Single();
        CombatContext before = application.Before;
        CombatRelicState owner = before.Relics!.Single(relic => relic.DataId == ownerId);
        RelicResourceEffect payload = owner.ResourceEffects!.Single();
        Require(RelicJudgesScalesModel.Validate(owner) == null &&
            payload.ParamInt2 == evidence.GetProperty("ParamInt2").GetInt32() &&
            application.Turn == 0 && application.Placement && application.Cancelled &&
            application.SuppressedFollowupSelections == 1 && application.Choices.Length == 1 &&
            application.Choices[0].DataId == carrierId && application.Choices[0].CardId == 1,
            "Native Judges Scales owner payload or one-discard-then-cancel path changed.");

        int[] eligible = RelicJudgesScalesModel.EligibleCardIds(before, out string? eligibilityError).ToArray();
        Require(eligibilityError == null && eligible.SequenceEqual(application.ModelEligibleCardIds) &&
            eligible.SequenceEqual(tests.First(test => test.Result).EligibleCardIds) &&
            eligible.Contains(application.Choices[0].CardId) &&
            application.ModelDiscardedCardIds.SequenceEqual(new[] { application.Choices[0].CardId }) &&
            application.ModelDrawnCardIds.Length == 1,
            "Native eligible cards, discard identity or replacement draw differ from the independent model.");
        Require(before.Cards.Hand.Any(card => card.DataId == persistentId &&
                HasTrait(before, card.InstanceId, "CardTraitPersistent")) &&
            before.Cards.Hand.Any(card => card.DataId == infusionId &&
                HasTrait(before, card.InstanceId, "CardTraitInfusion")) &&
            !eligible.Contains(before.Cards.Hand.Single(card => card.DataId == persistentId).InstanceId) &&
            !eligible.Contains(before.Cards.Hand.Single(card => card.DataId == infusionId).InstanceId),
            "Persistent or Infusion cards leaked into the native discard candidate set.");

        BattlePlayRules rules = root.GetProperty("Turns")[0].GetProperty("Before")
            .Deserialize<BattleTurnState>()!.PlayRules!;
        RelicJudgesScalesResult replay = RelicJudgesScalesModel.Apply(before, rules,
            application.Choices.Select(choice => choice.CardId).ToArray(), placement: application.Placement);
        Require(replay.Supported && Same(replay.Context!, application.After!),
            "Independent Judges Scales replay differs from the native discard and replacement draw.");
        VerifyControls(application, rules, eligible, persistentId, infusionId);

        BattleTurnChecks.Native(root);
        BattleActionChecks.Native(root);
        Console.WriteLine("ORIGINAL-JUDGES-SCALES-NATIVE PASS: native filters, deployment gate, one discard/cancel, and replacement draw.");
    }

    private static void VerifyControls(ApplicationRecord application, BattlePlayRules rules, int[] eligible,
        string persistentId, string infusionId)
    {
        CombatContext source = application.Before;
        RelicJudgesScalesResult preview = RelicJudgesScalesModel.Apply(source, rules,
            application.Choices.Select(choice => choice.CardId).ToArray(), placement: true, preview: true);
        RelicJudgesScalesResult regularTurn = RelicJudgesScalesModel.Apply(source, rules,
            application.Choices.Select(choice => choice.CardId).ToArray(), placement: false);
        RelicJudgesScalesResult cancelled = RelicJudgesScalesModel.Apply(source, rules,
            Array.Empty<int>(), placement: true);
        CombatContext withoutOwner = source.WithRelics(source.Relics!.Where(relic =>
            relic.DataId != RelicJudgesScalesModel.DataId).ToArray());
        RelicJudgesScalesResult ablated = RelicJudgesScalesModel.Apply(withoutOwner, rules, placement: true);
        Require(preview.Supported && regularTurn.Supported && cancelled.Supported && ablated.Supported &&
            Same(source, preview.Context!) && Same(source, regularTurn.Context!) && Same(source, cancelled.Context!) &&
            Same(withoutOwner, ablated.Context!),
            "Preview, normal-turn, native cancel or owner-removal control changed combat state.");

        CardToken[] protectedCards = source.Cards.Hand.Where(card => card.DataId == persistentId || card.DataId == infusionId).ToArray();
        CardToken[] otherwiseEligible = source.Cards.Hand.Where(card => !protectedCards.Contains(card)).ToArray();
        var protectedOnlyCycle = new CardCycleState(protectedCards,
            source.Cards.Draw.Concat(otherwiseEligible).ToArray(), source.Cards.Discard, source.Cards.Rng,
            source.Cards.DrawModifier, source.Cards.ExternalInteractions, source.Cards.BonusDraw,
            source.Cards.NextDrawUpgrades, source.Cards.NextUpgradeReferenceId);
        CombatContext protectedOnly = source.WithCards(protectedOnlyCycle);
        RelicJudgesScalesResult noCandidates = RelicJudgesScalesModel.Apply(protectedOnly, rules, placement: true);
        Require(noCandidates.Supported && noCandidates.EligibleCardIds.Count == 0 &&
            Same(protectedOnly, noCandidates.Context!), "No eligible hand cards should not discard or draw.");
        RelicJudgesScalesResult invalidChoice = RelicJudgesScalesModel.Apply(source, rules,
            new[] { protectedCards[0].InstanceId }, placement: true);
        Require(!invalidChoice.Supported && invalidChoice.UnsupportedReason != null && eligible.Length > 0,
            "Judges Scales accepted a Persistent or Infusion card as a discard choice.");
    }

    private static bool HasTrait(CombatContext context, int cardId, string trait) =>
        CardBranchMaskModel.Resolve(context.FindCard(cardId)!).Traits.Contains(trait, StringComparer.Ordinal);

    private static bool Same(CombatContext expected, CombatContext actual) => ModelJson.Difference(
        JsonSerializer.Serialize(BattleTurnChecks.ComparableContext(expected), ModelJson.Options),
        JsonSerializer.Serialize(BattleTurnChecks.ComparableContext(actual), ModelJson.Options)) == null;

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidDataException(message); }
}
