using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using MonsterTrain2Poju.Probe;

internal static class RollingAdviceChecks
{
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
    private static BattleTurnState State(FixtureValue value) => value.Deserialize<BattleTurnState>()!;
    private static bool Terminal(RoomOutcome outcome) => outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
    private static string Key(BattleTurnState state, bool terminal = false) => BattleTurnChecks.Comparable(state, terminal);
    private static BattleSearchStep ReadStep(FixtureValue row)
    {
        bool end = row.GetProperty("IsEndTurn").GetBoolean();
        return new BattleSearchStep(end ? null : row.GetProperty("Action").Deserialize<PlayCardAction>(),
            end, RecordedBattleChecks.Decisions(row));
    }
    private static bool SameRelentlessChoices(FixtureValue left, FixtureValue right) =>
        (RecordedBattleChecks.Decisions(left) ?? []).Select(Choice).SequenceEqual(
            (RecordedBattleChecks.Decisions(right) ?? []).Select(Choice));
    private static (int Room, int Rounds, bool Continue) Choice(RelentlessDecision choice) =>
        (choice.RoomIndex, choice.CompletedRounds, choice.Continue);
    private static bool SameStep(FixtureValue left, FixtureValue right) =>
        left.GetProperty("IsEndTurn").GetBoolean() == right.GetProperty("IsEndTurn").GetBoolean() &&
        left.GetProperty("Action").ContentEquals(right.GetProperty("Action")) && SameRelentlessChoices(left, right);

    internal static void RapidContinuation(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 1 &&
            fixture.GetProperty("EvidenceKind").GetString() == "NativePlayerAdvisorRapidContinuation" &&
            fixture.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            fixture.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module,
            "Expected the pinned native rapid continuation receipt.");
        FixtureValue[] inputs = fixture.GetProperty("Inputs").EnumerateArray().ToArray();
        Require(inputs.Length == 2 && inputs[0].GetProperty("Mode").GetString() == "rolling" &&
            inputs[1].GetProperty("Mode").GetString() == "rapid-continuation" &&
            fixture.GetProperty("SecondAcceptedWhileBlocked").GetBoolean(), "Missing two accepted inputs during one continuation verification.");
        int verificationThread = fixture.GetProperty("VerificationThread").GetInt32();
        int mainThread = fixture.GetProperty("MainThread").GetInt32();
        long originalGeneration = fixture.GetProperty("OriginalGeneration").GetInt64();
        long finalGeneration = fixture.GetProperty("FinalGeneration").GetInt64();
        long searches = fixture.GetProperty("OriginalSearches").GetInt64();
        long publications = fixture.GetProperty("OriginalPublications").GetInt64();
        Require(verificationThread > 0 && mainThread > 0 && verificationThread != mainThread &&
            originalGeneration > 0 && finalGeneration > originalGeneration &&
            inputs.All(row => row.GetProperty("Generation").GetInt64() == originalGeneration) &&
            searches > 0 && fixture.GetProperty("FinalSearches").GetInt64() == searches && publications > 0 &&
            fixture.GetProperty("FinalPublications").GetInt64() == publications + 1,
            "Expected worker verification and exactly one new publication without another search.");

        FixtureValue initial = inputs[0];
        FixtureValue[] initialPlan = initial.GetProperty("PlanSteps").EnumerateArray().ToArray();
        Require(initialPlan.Length > inputs.Length && new[] { "PlanSteps", "Planned", "PlannedOutcome", "Score",
            "RequestedHorizon", "TurnsPlanned", "UsedShorterHorizon", "PolicyBaselineScore" }
            .All(field => initial.GetProperty(field).ContentEquals(inputs[1].GetProperty(field))),
            "The second input did not carry the original forecast unchanged.");
        using var keys = new BattleStateKeys();
        using var boundaryKeys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
        BattleTurnState? previous = null;
        for (int index = 0; index < inputs.Length; index++)
        {
            FixtureValue input = inputs[index];
            BattleTurnState root = State(input.GetProperty("Root"));
            BattleSearchStep step = ReadStep(input);
            Require(root.Spawn.Turn == 0 && !step.IsEndTurn && step.Action is { ActivatorUnitId: 0 } action &&
                action.RecursionSelections.Count == 0 && action.MixSelections.Count == 0 && action.SummonSelections.Count == 0 &&
                SameStep(initialPlan[index], input) && input.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "The driver did not play the corresponding pair of original deployment steps.");
            if (previous != null)
                Require(boundaryKeys.Key(previous) == boundaryKeys.Key(root), "The two native input roots are not consecutive.");
            string parent = JsonSerializer.Serialize(root);
            BattleTurnState modeled = BattlePlannerChecks.Replay(root, [step], out RoomOutcome outcome);
            Require(outcome == RoomOutcome.Exchanged && outcome == input.GetProperty("Outcome").Deserialize<RoomOutcome>(),
                "The native deployment input outcome differs.");
            RecordedBattleChecks.Compare(modeled, State(input.GetProperty("Expected")), terminal: false, $"Rapid forecast input {index + 1}");
            RecordedBattleChecks.RecordedBoundary(modeled, input.GetProperty("Actual"), terminal: false, $"Rapid native input {index + 1}");
            Require(JsonSerializer.Serialize(root) == parent, "Rapid input replay mutated its parent.");
            previous = State(input.GetProperty("Actual"));
        }

        FixtureValue publication = fixture.GetProperty("FinalRoot");
        BattleTurnState finalRoot = State(publication);
        FixtureValue nativeRoot = fixture.GetProperty("FinalNativeRoot");
        string nativeKey = fixture.GetProperty("FinalRootKey").GetString()!;
        Require(nativeKey.Length == 64 && nativeKey.All(Uri.IsHexDigit) &&
            nativeKey == fixture.GetProperty("FinalNativeRootKey").GetString() && publication.ContentEquals(nativeRoot) &&
            keys.Key(finalRoot) == keys.Key(State(nativeRoot)) && boundaryKeys.Key(finalRoot) == boundaryKeys.Key(previous!),
            "The final published root or native key differs from the independently captured native successor.");

        FixtureValue finalPlan = fixture.GetProperty("FinalPlan");
        FixtureValue[] suffix = finalPlan.GetProperty("Steps").EnumerateArray().ToArray();
        Require(suffix.Length == initialPlan.Length - inputs.Length && suffix.Length <= finalPlan.GetProperty("MaxSteps").GetInt32() &&
            suffix.Select((step, index) => SameStep(step, initialPlan[index + inputs.Length])).All(matches => matches) &&
            finalPlan.GetProperty("Outcome").ContentEquals(initial.GetProperty("PlannedOutcome")) &&
            finalPlan.GetProperty("Score").GetDouble() == initial.GetProperty("Score").GetDouble() &&
            !finalPlan.GetProperty("Cancelled").GetBoolean() && finalPlan.GetProperty("ExpandedNodes").GetInt32() == 0 &&
            suffix.Count(step => step.GetProperty("IsEndTurn").GetBoolean()) == finalPlan.GetProperty("TurnsCompleted").GetInt32(),
            "The final publication did not retain the exact original two-step suffix.");
        VerifyPlan(State(initial.GetProperty("Root")), initialPlan, initial.GetProperty("Planned"),
            initial.GetProperty("PlannedOutcome"), initial.GetProperty("Score"));
        VerifyPlan(finalRoot, suffix, finalPlan.GetProperty("State"), finalPlan.GetProperty("Outcome"), finalPlan.GetProperty("Score"));
        Console.WriteLine($"PLAYER-ADVISOR-RAPID-CONTINUATION PASS. Two native plays during one verification, no new search, one publication and {suffix.Length} replayed suffix steps.");

        void VerifyPlan(BattleTurnState root, FixtureValue[] steps, FixtureValue expected, FixtureValue expectedOutcome, FixtureValue score)
        {
            string parent = JsonSerializer.Serialize(root);
            BattleTurnState settled = BattlePlannerChecks.Replay(root, steps.Select(ReadStep).ToArray(), out RoomOutcome outcome);
            Require(keys.Key(settled) == keys.Key(State(expected)) && outcome == expectedOutcome.Deserialize<RoomOutcome>() &&
                BattlePlanner.Evaluate(settled, outcome) == score.GetDouble() && JsonSerializer.Serialize(root) == parent,
                "The published forecast does not independently reproduce its result, score and immutable root.");
        }
    }

    internal static void MixPrompt(FixtureValue fixture, bool unitAbility = false)
    {
        string evidenceKind = unitAbility ? "NativePlayerAdvisorUnitMixPrompt" : "NativePlayerAdvisorMixPrompt";
        Require(fixture.GetProperty("Schema").GetInt32() == 1 &&
            fixture.GetProperty("EvidenceKind").GetString() == evidenceKind &&
            fixture.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            fixture.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            fixture.GetProperty("PlayerAdvisor").GetProperty("Assembly").GetString() == "MonsterTrain2Poju.Advisor",
            "Expected a Mix prompt receipt from the pinned native build and player advisor.");
        FixtureValue input = fixture.GetProperty("Input");
        BattleSearchStep step = ReadStep(input);
        bool rightSource = unitAbility
            ? step.Action is { ActivatorUnitId: > 0, ActivatorRoomIndex: null }
            : step.Action is { ActivatorUnitId: 0, ActivatorRoomIndex: null };
        Require(input.GetProperty("Mode").GetString() == "rolling" && !step.IsEndTurn &&
            rightSource &&
            SameStep(input.GetProperty("PlanSteps")[0], input) &&
            input.GetProperty("Difference").ValueKind == FixtureKind.Null,
            "Expected an accepted direct card matching the first published action.");
        PlayCardAction action = step.Action!;
        ElixirMixSelection[] choices = action.MixSelections.OrderBy(choice => choice.EffectIndex).ThenBy(choice => choice.MixNumber).ToArray();
        FixtureValue[] prompts = fixture.GetProperty("Prompts").EnumerateArray().ToArray();
        Require(choices.Length > 0 && choices.Length == prompts.Length && fixture.GetProperty("NormalPromptRestored").GetBoolean(),
            "The native draft did not show and clear each published selection.");

        int previousConfirmation = -1;
        for (int index = 0; index < prompts.Length; index++)
        {
            FixtureValue prompt = prompts[index];
            ElixirMixSelection choice = choices[index];
            string[] offers = prompt.GetProperty("Offers").GetString()!.Split(',');
            string selected = prompt.GetProperty("SelectedCard").GetString() + ":" +
                (prompt.GetProperty("SelectedUpgrade").GetString() ?? "none");
            int opened = prompt.GetProperty("PromptFrame").GetInt32();
            int confirmed = prompt.GetProperty("ConfirmationFrame").GetInt32();
            Require(prompt.GetProperty("Kind").GetString() is "new" or "upgrade" &&
                (!unitAbility || (prompt.GetProperty("Kind").GetString() == "new" &&
                    prompt.GetProperty("DraftType").GetString() == "CraftedSpike" && offers.Length == 2)) &&
                prompt.GetProperty("EffectIndex").GetInt32() == choice.EffectIndex &&
                prompt.GetProperty("MixNumber").GetInt32() == choice.MixNumber &&
                prompt.GetProperty("ChoiceIndex").GetInt32() == choice.ChoiceIndex &&
                prompt.GetProperty("Text").GetString() == AdvisorForecastPresentation.MixChoice(index, choice.ChoiceIndex) &&
                choice.ChoiceIndex >= 0 && choice.ChoiceIndex < offers.Length && offers.All(OfferIdentity) &&
                offers[choice.ChoiceIndex] == selected && opened >= 0 && opened >= previousConfirmation &&
                confirmed >= opened && prompt.GetProperty("HiddenAfterConfirmation").GetBoolean(),
                $"Native Mix selection {index + 1} differs from the published option, native offer, or prompt lifecycle.");
            previousConfirmation = confirmed;
        }

        BattleTurnState root = State(input.GetProperty("Root"));
        BattleTurnState actual = State(input.GetProperty("Actual"));
        if (unitAbility)
        {
            var actor = root.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.ActivatorUnitId);
            Require(choices.Length == 1 && actor.AssetKey == "MonsterSpikedriverFactory" &&
                actor.Ability?.DataId == "798142fe-fdf3-4f4f-8af7-dbc5dbc333c6",
                "Expected the original Spikedriver Factory's Craft Spike activation.");
        }
        long finalGeneration = CheckElixirPromptSettlement(fixture, input, action, root, actual);
        Console.WriteLine($"PLAYER-ADVISOR-MIX-PROMPT PASS. {prompts.Length} native selections, exact paid successor and next generation {finalGeneration}.");

        static bool OfferIdentity(string offer)
        {
            string[] parts = offer.Split(':');
            return parts.Length == 2 && Guid.TryParseExact(parts[0], "D", out Guid card) && card != Guid.Empty &&
                (parts[1] == "none" || Guid.TryParseExact(parts[1], "D", out Guid upgrade) && upgrade != Guid.Empty);
        }
    }

    internal static void SummonPrompt(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 1 &&
            fixture.GetProperty("EvidenceKind").GetString() == "NativePlayerAdvisorSummonPrompt" &&
            fixture.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            fixture.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            fixture.GetProperty("PlayerAdvisor").GetProperty("Assembly").GetString() == "MonsterTrain2Poju.Advisor",
            "Expected a summon receipt from the pinned original build and player advisor.");
        FixtureValue entitlement = fixture.GetProperty("Entitlement");
        string champion = entitlement.GetProperty("ChampionId").GetString()!;
        string ability = entitlement.GetProperty("AbilityId").GetString()!;
        string rank = entitlement.GetProperty("RankId").GetString()!;
        string checkpoint = entitlement.GetProperty("CheckpointSha256").GetString()!;
        Require(entitlement.GetProperty("Installed").GetBoolean() && entitlement.GetProperty("EnabledInRun").GetBoolean() &&
            champion == "c58f8d77-8578-4d97-bcf1-beb4cdbe2f27" &&
            OriginalSummonChoiceChecks.MatchesGrimFlightRank(entitlement.GetProperty("Rank").GetInt32(), rank, ability) &&
            checkpoint.Length == 64 && checkpoint.All(Uri.IsHexDigit),
            "The original DLC, champion and checkpoint provenance is missing.");

        FixtureValue input = fixture.GetProperty("Input");
        BattleSearchStep step = ReadStep(input);
        Require(input.GetProperty("Mode").GetString() == "rolling" && !step.IsEndTurn &&
            step.Action is { ActivatorUnitId: > 0, ActivatorRoomIndex: null, TargetUnitId: 0, TargetSpawnPointIndex: >= 0,
                RecursionSelections.Count: 0, MixSelections.Count: 0, SummonSelections.Count: 1 } &&
            SameStep(input.GetProperty("PlanSteps")[0], input) && input.GetProperty("Difference").ValueKind == FixtureKind.Null,
            "Expected the first published physical summon as an exact settled native input.");
        PlayCardAction action = step.Action!;
        var root = State(input.GetProperty("Root"));
        var actual = State(input.GetProperty("Actual"));
        CombatUnit actor = root.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.ActivatorUnitId);
        var context = root.Spawn.Train.Context!;
        var ownedChampion = context.FindCard(actor.SpawnerCardId);
        Require(actor.Ability?.DataId == ability && ownedChampion?.DataId == champion &&
            ownedChampion.Permanent.Upgrades.Any(upgrade => upgrade.DataId == rank) &&
            context.FindCard(action.CardInstanceId)?.DataId == ability, "GrimFlight actor or ability identity changed.");

        FixtureValue prompt = fixture.GetProperty("Prompt");
        SummonChoiceSelection choice = action.SummonSelections.Single();
        string?[] offers = prompt.GetProperty("Offers").EnumerateArray().Select(offer => offer.GetString()).ToArray();
        int position = prompt.GetProperty("ChoiceIndex").GetInt32();
        int opened = prompt.GetProperty("PromptFrame").GetInt32();
        Require(prompt.GetProperty("DraftType").GetString() == "SummonChoice" &&
            offers.Length == 3 && offers.All(offer => Guid.TryParseExact(offer, "D", out Guid id) && id != Guid.Empty) &&
            position >= 0 && position == AdvisorForecastPresentation.SummonOfferIndex(offers, choice.CardDataId) &&
            prompt.GetProperty("EffectIndex").GetInt32() == choice.EffectIndex &&
            prompt.GetProperty("CardDataId").GetString() == choice.CardDataId &&
            prompt.GetProperty("Text").GetString() is string text && text.EndsWith(" · 第 " + (position + 1) + " 项", StringComparison.Ordinal) &&
            opened >= 0 && prompt.GetProperty("ConfirmationFrame").GetInt32() >= opened &&
            prompt.GetProperty("HiddenAfterConfirmation").GetBoolean() && fixture.GetProperty("NormalPromptRestored").GetBoolean(),
            "The original option, prompt position or confirmation evidence differs from the published summon.");

        long finalGeneration = CheckElixirPromptSettlement(fixture, input, action, root, actual);
        Console.WriteLine($"PLAYER-ADVISOR-SUMMON-PROMPT PASS. Exact selected offer, paid successor and next generation {finalGeneration}.");
    }

    private static long CheckElixirPromptSettlement(FixtureValue fixture, FixtureValue input, PlayCardAction action,
        BattleTurnState root, BattleTurnState actual)
    {
        string parent = JsonSerializer.Serialize(root);
        BattleActionResult played = BattleActionModel.PlayCard(root, action);
        Require(played.Supported && played.Outcome == RoomOutcome.Exchanged &&
            played.Outcome == input.GetProperty("Outcome").Deserialize<RoomOutcome>() &&
            Key(played.State!) == Key(State(input.GetProperty("Expected"))) &&
            Key(played.State!) == Key(actual) && JsonSerializer.Serialize(root) == parent,
            "The paid native draft successor differs from independent replay or replay mutated its root.");

        FixtureValue publication = fixture.GetProperty("FinalRoot");
        FixtureValue native = fixture.GetProperty("FinalNativeRoot");
        BattleTurnState finalRoot = State(publication);
        string nativeKey = fixture.GetProperty("FinalRootKey").GetString()!;
        long inputGeneration = input.GetProperty("Generation").GetInt64();
        long finalGeneration = fixture.GetProperty("FinalGeneration").GetInt64();
        using var boundaryKeys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
        Require(inputGeneration > 0 && finalGeneration > inputGeneration &&
            nativeKey.Length == 64 && nativeKey.All(Uri.IsHexDigit) &&
            nativeKey == fixture.GetProperty("FinalNativeRootKey").GetString() && publication.ContentEquals(native) &&
            boundaryKeys.Key(finalRoot) == boundaryKeys.Key(actual),
            "The next advisor publication does not match the settled native root.");

        FixtureValue plan = fixture.GetProperty("FinalPlan");
        BattleSearchStep[] steps = plan.GetProperty("Steps").EnumerateArray().Select(ReadStep).ToArray();
        Require(steps.Length > 0 && steps.Length <= plan.GetProperty("MaxSteps").GetInt32() &&
            !plan.GetProperty("Cancelled").GetBoolean() &&
            (plan.GetProperty("CompletedHorizon").GetBoolean() || plan.GetProperty("UsedShorterHorizon").GetBoolean()),
            "The next publication has no usable plan.");
        using var keys = new BattleStateKeys();
        string finalParent = JsonSerializer.Serialize(finalRoot);
        BattleTurnState forecast = BattlePlannerChecks.Replay(finalRoot, steps, out RoomOutcome forecastOutcome);
        Require(keys.Key(forecast) == keys.Key(State(plan.GetProperty("State"))) &&
            forecastOutcome == plan.GetProperty("Outcome").Deserialize<RoomOutcome>() &&
            BattlePlanner.Evaluate(forecast, forecastOutcome) == plan.GetProperty("Score").GetDouble() &&
            steps.Count(item => item.IsEndTurn) == plan.GetProperty("TurnsCompleted").GetInt32() &&
            JsonSerializer.Serialize(finalRoot) == finalParent,
            "The next forecast differs on independent replay or replay mutated its root.");
        return finalGeneration;
    }

    internal static void OffRoute(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 1 &&
            fixture.GetProperty("EvidenceKind").GetString() == "NativePlayerAdvisorOffRoute" &&
            fixture.GetProperty("GameVersion").GetString() == "2.3.1" &&
            fixture.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            fixture.GetProperty("PlayerAdvisor").GetProperty("Assembly").GetString() == "MonsterTrain2Poju.Advisor",
            "Expected an off-route witness from the pinned player advisor and native build.");
        FixtureValue input = fixture.GetProperty("Input");
        BattleTurnState root = State(input.GetProperty("Root"));
        BattleTurnState actual = State(input.GetProperty("Actual"));
        BattleSearchStep executed = ReadStep(input);
        BattleSearchStep published = ReadStep(input.GetProperty("PlanSteps")[0]);
        Require(input.GetProperty("Mode").GetString() == "off-route-observation" && root.Spawn.Turn == 0 &&
            root.Spawn.Turn == actual.Spawn.Turn && executed.Action != null && published.Action != null &&
            executed.Action.CardInstanceId != published.Action.CardInstanceId && executed.Action.RoomIndex == root.SelectedRoom &&
            input.GetProperty("Difference").ValueKind == FixtureKind.Null && fixture.GetProperty("StaleInputInvalidated").GetBoolean(),
            "Missing a distinct accepted original deployment or immediate invalidation.");
        using var keys = new BattleStateKeys();
        using var boundaryKeys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
        string parent = keys.Key(root);
        BattleActionResult played = BattleActionModel.PlayCard(root, executed.Action!);
        Require(played.Supported && played.Outcome == RoomOutcome.Exchanged &&
            played.Outcome == input.GetProperty("Outcome").Deserialize<RoomOutcome>() &&
            boundaryKeys.Key(played.State!) == boundaryKeys.Key(actual) &&
            boundaryKeys.Key(State(input.GetProperty("Expected"))) == boundaryKeys.Key(actual) &&
            keys.Key(root) == parent && keys.Key(actual) != parent,
            "Independent alternate input replay differs in state/RNG or mutated its parent.");
        FixtureValue publishedRoot = fixture.GetProperty("FreshRoot");
        BattleTurnState fresh = State(publishedRoot);
        string freshKey = keys.Key(fresh);
        string nativeKey = fixture.GetProperty("FreshRootKey").GetString()!;
        // The live driver verifies the publication key in the engine process.
        // Independently compare the complete archived values before hydration.
        Require(nativeKey.Length == 64 && nativeKey.All(Uri.IsHexDigit) && boundaryKeys.Key(fresh) == boundaryKeys.Key(actual) &&
            publishedRoot.ContentEquals(fixture.GetProperty("FreshNativeRoot")) &&
            fixture.GetProperty("FreshGeneration").GetInt64() > input.GetProperty("Generation").GetInt64() &&
            fixture.GetProperty("FreshSearches").GetInt64() == fixture.GetProperty("PreviousSearches").GetInt64() + 1,
            "The post-input publication was stale, reused search, or captured a different native state/RNG.");
        FixtureValue forecast = fixture.GetProperty("FreshPlan");
        BattleSearchStep[] steps = forecast.GetProperty("Steps").EnumerateArray().Select(ReadStep).ToArray();
        Require(steps.Length > 0 && steps.Length <= forecast.GetProperty("MaxSteps").GetInt32() &&
            !forecast.GetProperty("Cancelled").GetBoolean() &&
            (forecast.GetProperty("CompletedHorizon").GetBoolean() || forecast.GetProperty("UsedShorterHorizon").GetBoolean()),
            "The fresh publication has no usable forecast.");
        BattleTurnState forecastEnd = BattlePlannerChecks.Replay(fresh, steps, out RoomOutcome outcome);
        Require(keys.Key(forecastEnd) == keys.Key(State(forecast.GetProperty("State"))) && keys.Key(fresh) == freshKey &&
            outcome == forecast.GetProperty("Outcome").Deserialize<RoomOutcome>() &&
            BattlePlanner.Evaluate(forecastEnd, outcome) == forecast.GetProperty("Score").GetDouble() &&
            steps.Count(step => step.IsEndTurn) == forecast.GetProperty("TurnsCompleted").GetInt32(),
            "The fresh forecast does not independently reproduce its state, RNG, horizon and score.");
        Console.WriteLine($"PLAYER-ADVISOR-OFF-ROUTE PASS planned={published.Action!.CardInstanceId} actual={executed.Action.CardInstanceId} next-generation={fixture.GetProperty("FreshGeneration").GetInt64()}");
    }

    internal static void FutureTargets(FixtureValue fixture)
    {
        FixtureValue decision = fixture.GetProperty("RollingDecisions")[0];
        BattleTurnState root = State(decision.GetProperty("Root"));
        BattleSearchStep[] steps = decision.GetProperty("PlanSteps").EnumerateArray().Select(ReadStep).ToArray();
        Require(root.Spawn.Turn == 0 && root.Spawn.NextUnitId == 2 &&
            root.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Id != 3) &&
            steps[3].IsEndTurn && steps[4].Action is { CardInstanceId: 7, TargetUnitId: 3 } &&
            steps[6].Action is { CardInstanceId: 6, TargetUnitId: 5 },
            "The Ring 9 forecast must target future original units.");
        BattleTurnState end = BattlePlannerChecks.Replay(root, steps, out RoomOutcome outcome);
        var plan = new BattlePlanResult(end, outcome, steps, BattlePlanner.Evaluate(end, outcome),
            steps.Count(step => step.IsEndTurn), 1, 0, 0, TimeSpan.Zero, true, false, false, false, false,
            [], null, requestedTurns: decision.GetProperty("RequestedHorizon").GetInt32(), usedShorterHorizon: true,
            searchedToBattleEnd: true);
        using var keys = new BattleStateKeys();
        using var session = new BattleAdviceSession((_, _) => plan);
        Observe(root);
        Require(session.Current!.Continuation!.UnitKeys(4).Target == "TrainStewardBig" &&
            session.Current.Continuation.UnitKeys(6).Target == "Boss_Level9_Dominion_V2",
            "The initial forecast omitted the future unit identities.");

        session.InvalidateForInput();
        Observe(State(decision.GetProperty("Actual")));
        Require(session.SearchCount == 1 && session.Current!.ReusedContinuation &&
            session.Current.Plan.Steps.Count == steps.Length - 1 &&
            session.Current.Continuation!.UnitKeys(3).Target == "TrainStewardBig" &&
            session.Current.Continuation.UnitKeys(5).Target == "Boss_Level9_Dominion_V2",
            "The retained forecast lost the future identities or applied them to the wrong steps.");
        Console.WriteLine("ADVISOR-FUTURE-TARGET PASS: verified future ally and enemy names retained at the correct suffix indices.");

        void Observe(BattleTurnState state)
        {
            session.Observe(state, keys.Key(state));
            Require(SpinWait.SpinUntil(() => { session.Poll(); return session.Status != BattleAdviceStatus.Planning; }, 30000),
                "The advice publication timed out.");
            Require(session.Status == BattleAdviceStatus.Ready, session.Error ?? "Advice was unavailable.");
        }
    }

    // Partial failure archives remain useful native observations. This deliberately
    // checks actual successors only; it does not accept their old plans or counters.
    internal static void ObservedSuccessors(FixtureValue fixture)
    {
        FixtureValue[] records = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(records.Length > 0 && fixture.GetProperty("CaptureFailures").GetInt32() == 0,
            "Missing captured native successors.");
        BattleTurnState? previous = null;
        for (int index = 0; index < records.Length; index++)
        {
            FixtureValue record = records[index];
            BattleTurnState root = State(record.GetProperty("Root"));
            Require(previous == null || Key(previous) == Key(root), "Observed native chain differs at " + index);
            BattleTurnState next = BattlePlannerChecks.Replay(root, [ReadStep(record)], out RoomOutcome outcome);
            FixtureValue actual = record.GetProperty("Actual");
            RecordedBattleChecks.RecordedBoundary(next, actual, Terminal(outcome), "Observed native successor " + index);
            previous = State(actual);
        }
        Console.WriteLine($"OBSERVED-SUCCESSORS PASS: {records.Length} actual native inputs; original plan acceptance remains separate.");
    }
    private static string[] ActualBosses(FixtureValue fixture) => fixture.GetProperty("RollingDecisions").EnumerateArray()
        .SelectMany(record => new[] { State(record.GetProperty("Root")), State(record.GetProperty("Actual")) })
        .SelectMany(state => state.Spawn.Train.Rooms.Where(room => !room.Preview).SelectMany(room => room.Units))
        .Where(unit => unit.IsBoss == true).Select(unit => unit.AssetKey).Distinct().ToArray();
    private static void Accepted(FixtureValue fixture, bool conduitCalibration = false, string? calibration = null)
    {
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(counter).GetInt32() == 0, "Rolling native capture has nonzero " + counter);
        Require(fixture.GetProperty("TerminalEffectsSettled").GetBoolean(), "Rolling battle did not settle.");
        string? expected = conduitCalibration ? "original-conduit-relics" : calibration;
        Require(expected != null ? fixture.GetProperty("ModifierScenario").GetString() == expected :
            string.IsNullOrEmpty(fixture.GetProperty("ModifierScenario").GetString()), "Unexpected rolling encounter calibration.");
    }
    private static int ResumedPrefixLength(FixtureValue fixture, FixtureValue[] records)
    {
        if (!fixture.TryGetProperty("RollingPrefixResume", out FixtureValue resume) || resume.ValueKind == FixtureKind.Null)
            return 0;
        int count = resume.GetProperty("PrefixInputs").GetInt32();
        int stale = resume.GetProperty("StalePredictions").GetInt32();
        long firstFresh = resume.GetProperty("FirstFreshGeneration").GetInt64();
        string? hash = resume.GetProperty("SourceSHA256").GetString();
        Require(resume.GetProperty("Resumed").GetBoolean() && count > 0 && count < records.Length &&
            stale >= 0 && stale <= count && hash?.Length == 64 && hash.All(Uri.IsHexDigit) &&
            firstFresh > resume.GetProperty("ResumeGeneration").GetInt64() &&
            records[count].GetProperty("Generation").GetInt64() == firstFresh,
            "Incomplete native prefix handoff evidence.");
        for (int index = 0; index < records.Length; index++)
            Require(records[index].GetProperty("Mode").GetString() == (index < count ? "prefix-replay" : "rolling"),
                "Native prefix handoff changed mode at decision " + index);
        return count;
    }
    internal static void Presentation(FixtureValue fixture)
    {
        Require(fixture.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            fixture.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Expected a settled original 2.3.1 Advisor presentation victory.");
        FixtureValue advisor = fixture.GetProperty("PlayerAdvisor");
        Require(advisor.GetProperty("Assembly").GetString() == "MonsterTrain2Poju.Advisor" &&
            advisor.GetProperty("InteractionsPassed").GetBoolean() && advisor.GetProperty("IdlePassed").GetBoolean() &&
            advisor.GetProperty("PlanPreviewPassed").GetBoolean(), "Archived release Advisor interactions did not pass.");
        Require(advisor.TryGetProperty("Presentation", out FixtureValue presentation) &&
            presentation.ValueKind == FixtureKind.Object, "Missing native Advisor presentation measurements.");
        Require(presentation.GetProperty("RetainedInputs").GetInt32() > 0 &&
            presentation.GetProperty("PendingFrames").GetInt32() > 0 &&
            presentation.GetProperty("ReusedPublications").GetInt32() > 0 &&
            presentation.GetProperty("ScrolledPublications").GetInt32() > 0 &&
            presentation.GetProperty("Resets").GetInt32() > 0 &&
            presentation.GetProperty("BudgetChanges").GetInt32() >= 4 &&
            presentation.GetProperty("CollapsedTurnResets").GetInt32() > 0 &&
            presentation.GetProperty("CardIdentityChecks").GetInt32() >= 4 &&
            presentation.GetProperty("CardHoverChecks").GetInt32() >= 5 &&
            presentation.GetProperty("ForecastScopeChecks").GetInt32() >= 7,
            "Archived Advisor lacks retained text, scroll, reset, identity, hover or forecast-scope evidence.");
        FixtureValue browse = advisor.GetProperty("FloorBrowse");
        const int switches = 7;
        Require(browse.GetProperty("Passed").GetBoolean() && browse.GetProperty("DuringSearch").GetBoolean() &&
            browse.GetProperty("WheelInput").GetBoolean() && browse.GetProperty("Switches").GetInt32() == switches &&
            browse.GetProperty("CaptureFreeSwitches").GetInt32() == switches,
            "The archived Advisor did not preserve capture-free native wheel browsing.");
        double[] switchMs = browse.GetProperty("SwitchMilliseconds").Deserialize<double[]>()!;
        Require(switchMs.Length == switches && switchMs.All(duration => double.IsFinite(duration) && duration >= 0),
            "Invalid archived floor-switch measurements.");
        FixtureValue overlay = advisor.GetProperty("Overlay");
        int published = overlay.GetProperty("Published").GetInt32();
        Require(published >= 4 && overlay.GetProperty("Publications").GetArrayLength() == published &&
            overlay.GetProperty("PublicationRootChecks").GetInt32() == published &&
            overlay.GetProperty("BrowseSelections").GetInt32() >= switches &&
            overlay.GetProperty("Failures").GetInt32() == 0 && overlay.GetProperty("PurityFailures").GetInt32() == 0,
            "Archived Advisor publications lack exact native root checks or purity.");
        Native(fixture);
        Console.WriteLine($"PLAYER-ADVISOR-PRESENTATION PASS publications={published} retainedInputs={presentation.GetProperty("RetainedInputs").GetInt32()} " +
            $"pendingFrames={presentation.GetProperty("PendingFrames").GetInt32()} scrolledPublications={presentation.GetProperty("ScrolledPublications").GetInt32()} " +
            $"captureFreeWheelSwitches={switches} inFlightSwitchMs={switchMs[0]:F1} laterSwitchMs={switchMs.Skip(1).Min():F1}–{switchMs.Skip(1).Max():F1}");
    }

    internal static void Native(FixtureValue fixture, bool conduitCalibration = false, string? calibration = null)
    {
        Accepted(fixture, conduitCalibration, calibration);
        FixtureValue[] records = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(records.Length > 1, "Missing complete rolling decision sequence.");
        int prefixLength = ResumedPrefixLength(fixture, records);
        BattleTurnState? previous = null;
        long generation = -1;
        int cards = 0, turns = 0;
        RoomOutcome outcome = RoomOutcome.Exchanged;
        for (int index = 0; index < records.Length; index++)
        {
            FixtureValue record = records[index];
            bool prefix = index < prefixLength;
            BattleTurnState root = State(record.GetProperty("Root"));
            if (previous != null) Require(Key(previous) == Key(root), "Rolling decisions do not form one native chain.");
            else Require(root.Spawn.Turn == 0, "Rolling advice did not start at the deployment turn.");
            Require(record.GetProperty("Difference").ValueKind == FixtureKind.Null, "Rolling native mismatch.");
            bool end = record.GetProperty("IsEndTurn").GetBoolean();
            Require(end == (record.GetProperty("Action").ValueKind == FixtureKind.Null), "Invalid rolling input.");
            BattleTurnState next = BattlePlannerChecks.Replay(root, [ReadStep(record)], out outcome);
            Require(outcome == record.GetProperty("Outcome").Deserialize<RoomOutcome>() &&
                Key(next, Terminal(outcome)) == Key(State(record.GetProperty("Expected")), Terminal(outcome)),
                "Rolling expected successor differs at " + index);
            FixtureValue actual = record.GetProperty("Actual");
            RecordedBattleChecks.RecordedBoundary(next, actual, Terminal(outcome), "Rolling native successor " + index);
            if (end) turns++; else cards++;

            FixtureValue[] plan = record.GetProperty("PlanSteps").EnumerateArray().ToArray();
            Require(plan.Length > 0 && plan[0].GetProperty("IsEndTurn").GetBoolean() == end &&
                plan[0].GetProperty("Action").ContentEquals(record.GetProperty("Action")) &&
                SameRelentlessChoices(plan[0], record), "Driver did not execute the latest plan's first step.");
            BattleSearchStep[] steps = plan.Select(ReadStep).ToArray();
            BattleTurnState planned = BattlePlannerChecks.Replay(root, steps, out RoomOutcome plannedOutcome);
            if (prefix)
                Require(plan.Length == 1 && outcome == RoomOutcome.Exchanged &&
                    record.GetProperty("Generation").GetInt64() == 0 && record.GetProperty("Timing").ValueKind == FixtureKind.Null &&
                    record.GetProperty("PolicyBaselineScore").ValueKind == FixtureKind.Null,
                    "Verified native prefix retained an original search forecast.");
            else if (record.TryGetProperty("UsedShorterHorizon", out FixtureValue shorter) && shorter.GetBoolean())
                Require(!Terminal(plannedOutcome) && steps.Last().IsEndTurn &&
                    steps.Count(step => step.IsEndTurn) == record.GetProperty("TurnsPlanned").GetInt32() &&
                    record.GetProperty("TurnsPlanned").GetInt32() > 0 &&
                    record.GetProperty("TurnsPlanned").GetInt32() < record.GetProperty("RequestedHorizon").GetInt32() &&
                    record.GetProperty("PolicyBaselineScore").ValueKind == FixtureKind.Null,
                    "Shorter advice mislabeled a partial turn or winning forecast.");
            double score = record.GetProperty("Score").GetDouble();
            Require(plannedOutcome == record.GetProperty("PlannedOutcome").Deserialize<RoomOutcome>() &&
                Key(planned, Terminal(plannedOutcome)) == Key(State(record.GetProperty("Planned")), Terminal(plannedOutcome)) &&
                BattlePlanner.Evaluate(planned, plannedOutcome) == score, "Published continuation does not reproduce its forecast.");
            if (record.GetProperty("PolicyBaselineScore").ValueKind == FixtureKind.Number)
                Require(score <= record.GetProperty("PolicyBaselineScore").GetDouble(), "Rolling advice lost its completed policy seed.");
            if (record.GetProperty("Mode").GetString() == "rolling")
            {
                long currentGeneration = record.GetProperty("Generation").GetInt64();
                Require(currentGeneration > generation, "Advice was reused after a native action."); generation = currentGeneration;
                FixtureValue timing = record.GetProperty("Timing");
                Require(timing.GetProperty("Generation").GetInt64() == generation, "Advice timing belongs to another publication.");
                double sum = 0;
                foreach (string field in new[] { "CaptureMs", "QueueMs", "SearchMs", "VerificationMs" })
                { double duration = timing.GetProperty(field).GetDouble(); Require(double.IsFinite(duration) && duration >= 0, "Invalid advice latency."); sum += duration; }
                Require(timing.GetProperty("EndToEndMs").GetDouble() + 1 >= sum, "End-to-end latency omitted a measured stage.");
            }
            else Require((prefix || record.GetProperty("Mode").GetString() == "replay") && record.GetProperty("Nodes").GetInt32() == 0,
                "Frozen rolling replay searched again.");
            previous = State(actual);
        }
        Require(Terminal(outcome) && fixture.GetProperty("NativeWon").GetBoolean() == (outcome == RoomOutcome.BattleWon),
            "Rolling sequence did not cover the whole native battle.");
        Require(cards == fixture.GetProperty("Actions").GetArrayLength() && turns == fixture.GetProperty("Turns").GetArrayLength(),
            "Some native inputs bypassed rolling advice.");
        Console.WriteLine($"ROLLING-ADVICE PASS mode={records[0].GetProperty("Mode").GetString()} decisions={records.Length} prefix={prefixLength} cards={cards} turns={turns} outcome={outcome}");
    }
    internal static void VictoryPair(FixtureValue source, FixtureValue replay, FixtureValue baseline)
    {
        Require(source.GetProperty("NativeWon").GetBoolean() && replay.GetProperty("NativeWon").GetBoolean(),
            "Progressed battle acceptance requires an actual native victory and winning fresh replay.");
        Require(ActualBosses(source).Length > 0, "The winning battle must exercise an actual native boss.");
        Pair(source, replay, baseline);
        Console.WriteLine("ROLLING-ADVICE-VICTORY PASS: actual boss, native victory and winning fresh frozen replay.");
    }
    internal static void PrefixResumePair(FixtureValue source, FixtureValue replay)
    {
        Require(source.TryGetProperty("RollingPrefixResume", out FixtureValue resume) && resume.ValueKind == FixtureKind.Object,
            "Source capture has no native prefix handoff.");
        FrozenPair(source, replay);
    }
    internal static void FrozenPair(FixtureValue source, FixtureValue replay, bool conduitCalibration = false, string? calibration = null,
        bool sourceFrozen = false)
    {
        Native(source, conduitCalibration, calibration); Native(replay, conduitCalibration, calibration);
        FixtureValue[] first = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        FixtureValue[] second = replay.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(first.Length == second.Length, "Fresh rolling replay has a different decision count.");
        int prefixLength = ResumedPrefixLength(source, first);
        for (int index = 0; index < first.Length; index++)
        {
            bool prefix = index < prefixLength;
            string sourceMode = prefix ? "prefix-replay" : sourceFrozen ? "replay" : "rolling";
            Require(first[index].GetProperty("Mode").GetString() == sourceMode && second[index].GetProperty("Mode").GetString() == "replay" &&
                first[index].GetProperty("Action").ContentEquals(second[index].GetProperty("Action")) &&
                first[index].GetProperty("IsEndTurn").ContentEquals(second[index].GetProperty("IsEndTurn")) &&
                SameRelentlessChoices(first[index], second[index]), "Fresh rolling replay changed an input.");
            bool terminal = index == first.Length - 1;
            foreach (string field in new[] { "Root", "Expected", "Actual" })
                Require(Key(State(first[index].GetProperty(field)), terminal) == Key(State(second[index].GetProperty(field)), terminal),
                    "Fresh rolling replay changed full state or RNG at " + field);
            foreach (string field in new[] { "PlanSteps", "PlannedOutcome", "Score", "PolicyBaselineScore", "Generation" })
                Require(first[index].GetProperty(field).ContentEquals(second[index].GetProperty(field)),
                    "Fresh rolling replay changed the frozen forecast field " + field);
            if (prefix)
                Require(second[index].GetProperty("RequestedHorizon").GetInt32() == 0 &&
                    second[index].GetProperty("TurnsPlanned").GetInt32() == 0 &&
                    !second[index].GetProperty("UsedShorterHorizon").GetBoolean(),
                    "Fresh rolling replay retained an original prefix planning window.");
            else
                foreach (string field in new[] { "RequestedHorizon", "TurnsPlanned", "UsedShorterHorizon" })
                    if (first[index].TryGetProperty(field, out FixtureValue value))
                        Require(second[index].TryGetProperty(field, out FixtureValue frozenValue) && value.ContentEquals(frozenValue),
                            "Fresh rolling replay changed the planning window field " + field);
            bool plannedTerminal = Terminal(first[index].GetProperty("PlannedOutcome").Deserialize<RoomOutcome>());
            Require(Key(State(first[index].GetProperty("Planned")), plannedTerminal) ==
                Key(State(second[index].GetProperty("Planned")), plannedTerminal), "Fresh replay changed the full forecast state or RNG.");
        }
        Console.WriteLine("ROLLING-FROZEN-PAIR PASS: every input, root, successor, forecast and RNG matches in independent native processes.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay, FixtureValue baseline)
    {
        FrozenPair(source, replay); Accepted(baseline);
        FixtureValue[] first = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        // A terminal pre-boss loss is still a useful full-battle mechanic control.
        // Suite counts distinct bosses only from actual decision-room occupants.
        BattleAdviceChecks.Native(source, requireBoss: false);
        Require(baseline.GetProperty("Policy").GetString() == "units-spells-and-junk", "Wrong fixed-policy baseline.");
        Require(source.GetProperty("BattleDistance").GetInt32() == baseline.GetProperty("BattleDistance").GetInt32(), "Baseline encounter distance differs.");
        Require(source.GetProperty("BattleDistance").GetInt32() == replay.GetProperty("BattleDistance").GetInt32(), "Replay encounter distance differs.");
        FixtureValue[] baselineActions = baseline.GetProperty("Actions").EnumerateArray().ToArray();
        FixtureValue[] baselineTurns = baseline.GetProperty("Turns").EnumerateArray().ToArray();
        BattleTurnState baselineRoot = State((baselineActions.Length > 0 ? baselineActions[0] : baselineTurns[0]).GetProperty("Before"));
        Require(Key(State(first[0].GetProperty("Root"))) == Key(baselineRoot), "Fixed-policy baseline starts from a different battle state.");
        BattleTurnState policyState = baselineRoot;
        int actionIndex = 0;
        RoomOutcome policyOutcome = RoomOutcome.Exchanged;
        void Follow(FixtureValue record, BattleSearchStep step)
        {
            Require(Key(policyState) == Key(State(record.GetProperty("Before"))), "Fixed policy has a discontinuous native chain.");
            policyState = BattlePlannerChecks.Replay(policyState, new[] { step }, out policyOutcome);
            Require(policyOutcome == record.GetProperty("ActualOutcome").Deserialize<RoomOutcome>() &&
                Key(policyState, Terminal(policyOutcome)) == Key(State(record.GetProperty("Actual")), Terminal(policyOutcome)),
                "Completed fixed policy does not reproduce its native successor.");
        }
        foreach (FixtureValue turn in baselineTurns)
        {
            while (actionIndex < baselineActions.Length &&
                State(baselineActions[actionIndex].GetProperty("Before")).Spawn.Turn == policyState.Spawn.Turn)
            {
                FixtureValue action = baselineActions[actionIndex++];
                Follow(action, new BattleSearchStep(action.GetProperty("Action").Deserialize<PlayCardAction>(), false));
            }
            Follow(turn, new BattleSearchStep(null, true, RecordedBattleChecks.Decisions(turn)));
        }
        Require(actionIndex == baselineActions.Length && Terminal(policyOutcome), "Fixed policy did not finish all recorded inputs.");
        FixtureValue baselineTerminal = baselineTurns.Last().GetProperty("Actual");
        RoomOutcome baselineOutcome = baseline.GetProperty("NativeWon").GetBoolean() ? RoomOutcome.BattleWon : RoomOutcome.PlayerDefeated;
        double baselineScore = BattlePlanner.Evaluate(State(baselineTerminal), baselineOutcome);
        RoomOutcome actualOutcome = first.Last().GetProperty("Outcome").Deserialize<RoomOutcome>();
        double actualScore = BattlePlanner.Evaluate(State(first.Last().GetProperty("Actual")), actualOutcome);
        Require(actualScore <= baselineScore, "Rolling advisor lost to the completed fixed policy.");
        Console.WriteLine($"ROLLING-ADVICE-PAIR PASS: all {first.Length} decisions reproduced independently; terminal score={actualScore}, completed native policy={baselineScore}.");
    }
    internal static void Suite(FixtureValue[] fixtures)
    {
        Require(fixtures.Length >= 9 && fixtures.Length % 3 == 0, "Need at least three source/replay/policy triples.");
        var bosses = new HashSet<string>();
        var buffTargets = new HashSet<string>();
        bool laterRing = false;
        bool retiredAura = false;
        for (int index = 0; index < fixtures.Length; index += 3)
        {
            Pair(fixtures[index], fixtures[index + 1], fixtures[index + 2]);
            string[] actualBosses = ActualBosses(fixtures[index]);
            bosses.UnionWith(actualBosses);
            laterRing |= actualBosses.Length > 0 && fixtures[index].GetProperty("BattleDistance").GetInt32() >= 4;
            retiredAura |= fixtures[index].GetProperty("RollingDecisions").EnumerateArray()
                .Select(record => State(record.GetProperty("Actual")).Spawn.Train.Context?.Enchantments)
                .Any(world => world?.AutomaticLifecycle == true && world.RetainedUnits.Any(actor =>
                    actor.Unit.DeathState?.IsDestroyed == true && actor.Unit.Triggers.SelectMany(trigger => trigger.Effects)
                        .Any(effect => effect.Enchantment is { Bound: false })));
            foreach (FixtureValue fixture in new[] { fixtures[index], fixtures[index + 2] })
            {
                foreach (FixtureValue stage in fixture.GetProperty("Stages").EnumerateArray())
                    ObserveBuffs(stage.GetProperty("Before").Deserialize<RoomCombatState>()!,
                        stage.GetProperty("Actual").Deserialize<RoomCombatState>()!, buffTargets);
                foreach (FixtureValue action in fixture.GetProperty("Actions").EnumerateArray())
                {
                    BattleTurnState before = State(action.GetProperty("Before")), after = State(action.GetProperty("Actual"));
                    foreach (RoomCombatState room in before.Spawn.Train.Rooms)
                        ObserveBuffs(room, after.Spawn.Train.Rooms.Single(next => next.RoomIndex == room.RoomIndex), buffTargets);
                }
            }
        }
        Require(bosses.Count >= 3 && laterRing, "Need three distinct actual bosses including a middle/later ring.");
        Require(buffTargets.SetEquals(new[] { "Self", "Room" }), "Missing positive native Harvest/Incant attack-buff witnesses.");
        Require(retiredAura, "Missing native destruction and unbinding of a retained natural aura.");
        Console.WriteLine("ROLLING-ADVICE-SUITE PASS actual bosses=" + string.Join(",", bosses.Order()));
    }
    private static void ObserveBuffs(RoomCombatState before, RoomCombatState after, HashSet<string> targets)
    {
        foreach (CombatUnit caster in before.Units.Where(unit => unit.Team == CombatTeam.Enemy))
            foreach (CombatTrigger trigger in caster.Triggers)
                foreach (var effect in trigger.Effects.Where(effect => effect.Type == "CardEffectBuffDamage" && effect.Value > 0))
                    foreach (CombatUnit target in before.Units.Where(unit => unit.Team == CombatTeam.Enemy &&
                        (effect.Action?.Target == "Room" || effect.Action?.Target == "Self" && unit.Id == caster.Id)))
                    {
                        CombatUnit? actual = after.Units.FirstOrDefault(unit => unit.Id == target.Id);
                        if (actual?.Modifiers != null && target.Modifiers != null &&
                            actual.Modifiers.DamageBuff > target.Modifiers.DamageBuff)
                            targets.Add(effect.Action!.Target);
                    }
    }
}
