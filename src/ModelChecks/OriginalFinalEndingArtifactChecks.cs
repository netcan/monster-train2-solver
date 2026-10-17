using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalFinalEndingArtifactChecks
{
    private const string OwnerId = "6bb39014-9b03-4620-b088-f618a7e680b7";
    private const string OwnerKey = "FinalEndingArtifact";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 193 &&
            root.GetProperty("ModifierScenario").GetString() == "original-final-ending-artifact" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("OriginalFinalEndingArtifactPrepared").GetBoolean() &&
            new[] { "CaptureFailures", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "FinalEndingArtifact native battle failed acceptance.");

        var definitions = new HashSet<string>(StringComparer.Ordinal);
        int callbacks = 0, generated = 0, gated = 0, excluded = 0, fullHandControls = 0, championHandControls = 0,
            filteredPoolControls = 0, emptyPoolControls = 0;
        CombatContext? positiveBefore = null;
        CombatRelicState? positiveOwner = null;
        RelicResourceEffect? positiveEffect = null;
        CardGenerationRule? positiveRule = null;
        foreach (FixtureValue row in root.GetProperty("RelicChampionCardAfterDraw").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("Turn").GetInt32() >= 1 &&
                row.GetProperty("OwnerId").GetString() == OwnerId && row.GetProperty("EffectIndex").GetInt32() == 0,
                "Incomplete or misplaced FinalEndingArtifact callback.");
            CombatContext before = row.GetProperty("Before").Deserialize<CombatContext>()!;
            CombatContext actual = row.GetProperty("Actual").Deserialize<CombatContext>()!;
            CombatRelicState owner = before.Relics!.Single(item => item.DataId == OwnerId);
            RelicResourceEffect effect = owner.ResourceEffects!.Single(item => item.EffectIndex == 0);
            CardGenerationRule rule = effect.CardGeneration!;
            bool[] eligible = rule.PoolEligibility!.ToArray();
            string definition = JsonSerializer.Serialize(rule, ModelJson.Options);
            definitions.Add(definition);
            Require(owner.AssetKey == OwnerKey && owner.IsCovenant == false &&
                owner.EffectTypes.SequenceEqual([RelicCardGenerationModel.ChampionCardAfterDraw]) &&
                effect.Type == RelicCardGenerationModel.ChampionCardAfterDraw && effect.Conditions.Count == 0 &&
                effect.RequiredMutatorId == null && !effect.CanApplyInPreviewMode && rule.Destination == "HandPile" &&
                rule.Count == 1 && rule.Permanent == false && rule.Pool.Count > 0 && eligible.Length == rule.Pool.Count &&
                rule.ChampionCardDataIds?.Count > 0 && rule.Pool.All(creation =>
                    rule.ChampionCardDataIds.Contains(creation.DataId, StringComparer.Ordinal)),
                "Original FinalEndingArtifact owner or champion pool definition changed.");

            string[] deckChampions = row.GetProperty("DeckChampionDataIds").EnumerateArray().Select(item => item.GetString()!).ToArray();
            bool[] expectedEligibility = rule.Pool.Select(creation => !deckChampions.Contains(creation.DataId, StringComparer.Ordinal)).ToArray();
            Require(eligible.SequenceEqual(expectedEligibility), "Native deck champion exclusion differs from the captured candidate mask.");
            excluded += eligible.Count(value => !value);

            bool handHasChampion = before.Cards.Hand.Any(card => rule.ChampionCardDataIds.Contains(card.DataId, StringComparer.Ordinal));
            bool shouldTrigger = before.Cards.Hand.Count < before.MaxHandSize && !handHasChampion && eligible.Any(value => value);
            bool testResult = row.GetProperty("TestResult").GetBoolean();
            Require(testResult == shouldTrigger, "Native hand-size/champion/candidate TestEffect gate differs.");
            int selectedIndex = row.GetProperty("SelectedPoolIndex").GetInt32();
            string? selectedDataId = row.GetProperty("SelectedDataId").ValueKind == FixtureKind.String
                ? row.GetProperty("SelectedDataId").GetString() : null;
            UnityRng testRng = row.GetProperty("TestRng").Deserialize<UnityRng>();

            if (!testResult)
            {
                Require(selectedIndex == -1 && selectedDataId == null && testRng.Equals(before.BattleRng),
                    "A gated champion callback selected a card or consumed Battle RNG.");
                Equal(before, actual, "gated callback");
                gated++;
            }
            else
            {
                positiveBefore ??= before;
                positiveOwner ??= owner;
                positiveEffect ??= effect;
                positiveRule ??= rule;
                (int expectedIndex, UnityRng expectedRng) = Shuffle(before.BattleRng, eligible);
                Require(selectedIndex == expectedIndex && selectedDataId == rule.Pool[selectedIndex].DataId &&
                    testRng.Equals(expectedRng) && row.GetProperty("SelectedUpgradeIds").EnumerateArray()
                        .Select(item => item.GetString()).SequenceEqual(rule.Pool[selectedIndex].AddCardUpgrades.Select(upgrade => upgrade.DataId)),
                    "Full native champion shuffle, selection identity, or attached upgrades differ.");
                CardGenerationResult predicted = RelicCardGenerationModel.ApplyChampionCardAfterDrawOne(before, OwnerId, 0);
                Require(predicted.Supported && predicted.AddedCards.Count == 1 &&
                    predicted.AddedCards[0].DataId == selectedDataId,
                    "Champion consumer did not create the native selected card: " + predicted.UnsupportedReason);
                Equal(predicted.Context!, actual, "native champion generation");
                generated++;

                bool[] filteredEligibility = rule.Pool.Select((creation, index) => eligible[index] &&
                    creation.ExternalInteractions.Count == 0 && creation.StartingModifiers.ExternalInteractions.Count == 0 &&
                    creation.AddCardUpgrades.All(upgrade => upgrade.ExternalInteractions.Count == 0)).ToArray();
                int filteredIndex = Array.FindIndex(filteredEligibility, value => value);
                Require(filteredIndex >= 0 && filteredEligibility.Count(value => value) > 1,
                    "Synthetic candidate-filter control requires at least two eligible champions.");
                filteredEligibility[filteredIndex] = false;
                RelicResourceEffect filteredEffect = effect.WithCardGeneration(rule.WithPoolEligibility(filteredEligibility));
                CombatRelicState filteredOwner = owner.WithResourceEffects(owner.ResourceEffects!
                    .Select(item => item.EffectIndex == filteredEffect.EffectIndex ? filteredEffect : item).ToArray());
                CombatContext filteredContext = before.WithRelics(before.Relics!.Select(item =>
                    item.DataId == OwnerId ? filteredOwner : item).ToArray());
                (int filteredExpectedIndex, UnityRng filteredExpectedRng) = Shuffle(before.BattleRng, filteredEligibility);
                CardGenerationResult filtered = RelicCardGenerationModel.ApplyChampionCardAfterDrawOne(filteredContext, OwnerId, 0);
                Require(filtered.Supported && filtered.AddedCards.Count == 1 &&
                    filtered.AddedCards[0].DataId == rule.Pool[filteredExpectedIndex].DataId &&
                    filtered.Context!.BattleRng.Equals(filteredExpectedRng) && filteredExpectedIndex != filteredIndex,
                    "Synthetic duplicate-deck candidate filtering selected an excluded champion or changed the native RNG path.");
                filteredPoolControls++;

                bool[] noCandidates = new bool[eligible.Length];
                RelicResourceEffect emptyEffect = effect.WithCardGeneration(rule.WithPoolEligibility(noCandidates));
                CombatRelicState emptyOwner = owner.WithResourceEffects(owner.ResourceEffects!
                    .Select(item => item.EffectIndex == emptyEffect.EffectIndex ? emptyEffect : item).ToArray());
                CombatContext emptyContext = before.WithRelics(before.Relics!.Select(item =>
                    item.DataId == OwnerId ? emptyOwner : item).ToArray());
                CardGenerationResult empty = RelicCardGenerationModel.ApplyChampionCardAfterDrawOne(emptyContext, OwnerId, 0);
                Require(empty.Supported && ReferenceEquals(empty.Context, emptyContext) && empty.AddedCards.Count == 0 &&
                    empty.Context!.BattleRng.Equals(emptyContext.BattleRng) && empty.Context.NextCardId == emptyContext.NextCardId,
                    "A synthetic empty eligible pool should skip generation without consuming RNG or allocating a card.");
                emptyPoolControls++;

                CombatContext full = FillHand(before);
                CardGenerationResult fullBlocked = RelicCardGenerationModel.ApplyChampionCardAfterDrawOne(full, OwnerId, 0);
                Require(fullBlocked.Supported && ReferenceEquals(fullBlocked.Context, full) && fullBlocked.AddedCards.Count == 0 &&
                    fullBlocked.Context!.BattleRng.Equals(full.BattleRng) && fullBlocked.Context.NextCardId == full.NextCardId,
                    "Synthetic full hand should block generation without consuming RNG or allocating a card.");
                fullHandControls++;

                string championId = rule.ChampionCardDataIds!.First();
                CombatContext championHand = PutChampionInHand(before, championId);
                CardGenerationResult championBlocked = RelicCardGenerationModel.ApplyChampionCardAfterDrawOne(championHand, OwnerId, 0);
                Require(championBlocked.Supported && ReferenceEquals(championBlocked.Context, championHand) &&
                    championBlocked.AddedCards.Count == 0 && championBlocked.Context!.BattleRng.Equals(championHand.BattleRng) &&
                    championBlocked.Context.NextCardId == championHand.NextCardId,
                    "Synthetic champion-in-hand control should block generation without RNG or allocation.");
                championHandControls++;
            }
            Require(row.GetProperty("CaptureError").ValueKind == FixtureKind.Null &&
                row.GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null &&
                row.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "FinalEndingArtifact callback reported a capture, unsupported, or differential error.");
            callbacks++;
        }

        int externalUpgradeCandidates = 0;
        if (positiveBefore != null && positiveOwner != null && positiveEffect != null && positiveRule != null)
        {
            foreach (var candidate in positiveRule.Pool.Select((creation, index) => (creation, index))
                .Where(item => positiveRule.PoolEligibility![item.index] &&
                    item.creation.AddCardUpgrades.Any(upgrade => upgrade.ExternalInteractions.Count > 0)))
            {
                bool[] onlyCandidate = new bool[positiveRule.Pool.Count];
                onlyCandidate[candidate.index] = true;
                RelicResourceEffect effect = positiveEffect.WithCardGeneration(positiveRule.WithPoolEligibility(onlyCandidate));
                CombatRelicState owner = positiveOwner.WithResourceEffects(positiveOwner.ResourceEffects!
                    .Select(item => item.EffectIndex == effect.EffectIndex ? effect : item).ToArray());
                CombatContext context = positiveBefore.WithRelics(positiveBefore.Relics!
                    .Select(item => item.DataId == OwnerId ? owner : item).ToArray());
                CardGenerationResult unsupported = RelicCardGenerationModel.ApplyChampionCardAfterDrawOne(context, OwnerId, 0);
                Require(!unsupported.Supported && unsupported.UnsupportedReason?.StartsWith(
                    "Champion grant upgrades with external interactions are not modeled:", StringComparison.Ordinal) == true,
                    "An externally interactive champion grant upgrade should be refused with an explicit boundary.");
                externalUpgradeCandidates++;
            }
        }

        Require(callbacks >= 2 && generated > 0 && gated > 0 &&
            fullHandControls == generated && championHandControls == generated && filteredPoolControls == generated &&
            emptyPoolControls == generated && definitions.Count == 1 &&
            externalUpgradeCandidates == 8 &&
            root.GetProperty("Mismatches").GetInt32() == 0,
            "Missing positive, gated, duplicate-deck exclusion, synthetic hand controls, or stable pool witnesses.");
        BattleActionChecks.Native(root);
        Console.WriteLine($"ORIGINAL-FINAL-ENDING-ARTIFACT-NATIVE PASS: {callbacks} post-draw tests, {generated} generated champions, {gated} gated callbacks, {excluded} native deck-excluded entries, {externalUpgradeCandidates} explicit external-upgrade refusals; exact N-1 Battle RNG shuffle, filtered/empty-pool controls, selected upgrades, full-hand/champion-hand controls and one-action policy replay.");
    }

    private static (int PoolIndex, UnityRng Rng) Shuffle(UnityRng start, IReadOnlyList<bool> eligibility)
    {
        int[] order = Enumerable.Range(0, eligibility.Count).Where(index => eligibility[index]).ToArray();
        if (order.Length == 0) throw new InvalidOperationException("No eligible FinalEndingArtifact candidate.");
        UnityRng rng = start;
        for (int index = order.Length - 1; index > 0; index--)
        {
            RngDraw draw = rng.Range(0, index + 1);
            rng = draw.State;
            (order[index], order[draw.Value]) = (order[draw.Value], order[index]);
        }
        return (order[0], rng);
    }

    private static CombatContext FillHand(CombatContext source)
    {
        CardCycleState cards = source.Cards;
        CardToken[] hand = cards.Hand.Concat(cards.Draw).Concat(cards.Discard).Take(source.MaxHandSize).ToArray();
        if (hand.Length != source.MaxHandSize) throw new InvalidOperationException("Native context cannot fill a full hand for control.");
        HashSet<int> ids = hand.Select(card => card.InstanceId).ToHashSet();
        return source.WithCards(new CardCycleState(hand,
            cards.Draw.Where(card => !ids.Contains(card.InstanceId)).ToArray(),
            cards.Discard.Where(card => !ids.Contains(card.InstanceId)).ToArray(), cards.Rng,
            cards.DrawModifier, cards.ExternalInteractions, cards.BonusDraw, cards.NextDrawUpgrades, cards.NextUpgradeReferenceId));
    }

    private static CombatContext PutChampionInHand(CombatContext source, string championId)
    {
        CardCycleState cards = source.Cards;
        CardToken[] hand = cards.Hand.Take(Math.Max(0, source.MaxHandSize - 1)).ToArray();
        CardToken[] draw = cards.Draw.Concat(cards.Hand.Skip(hand.Length)).ToArray();
        int id = source.NextCardId;
        var token = new CardToken(id, championId);
        CombatContext context = source.WithCards(new CardCycleState(new[] { token }.Concat(hand).ToArray(), draw,
            cards.Discard, cards.Rng, cards.DrawModifier, cards.ExternalInteractions, cards.BonusDraw,
            cards.NextDrawUpgrades, cards.NextUpgradeReferenceId));
        return context.WithCard(CardInstanceState.Empty(id, championId)).WithNextCardId(checked(id + 1));
    }

    private static void Equal(CombatContext predicted, CombatContext actual, string name)
    {
        string left = JsonSerializer.Serialize(predicted, ModelJson.Options);
        string right = JsonSerializer.Serialize(actual, ModelJson.Options);
        Require(ModelJson.Difference(left, right) == null,
            "FinalEndingArtifact " + name + " differs: " + ModelJson.Difference(left, right));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
