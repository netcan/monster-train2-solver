using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalAddSnackOnKillChecks
{
    private const string OwnerId = "b6548209-3ccf-4deb-9896-f07d5df3dab9";
    private const string OwnerKey = "AddSnackOnKill";
    private const string Effect = "RelicEffectAddBattleCardToHandOnUnitTrigger";
    private static readonly string[] PoolIds =
    {
        "0347419a-2e3a-492d-a054-f6e340be3db2", "0347419a-2e3a-492d-a054-f6e340be3db2",
        "0347419a-2e3a-492d-a054-f6e340be3db2", "0347419a-2e3a-492d-a054-f6e340be3db2",
        "0347419a-2e3a-492d-a054-f6e340be3db2", "e16040b1-eac0-4769-bc89-90bc2eb31944",
        "e16040b1-eac0-4769-bc89-90bc2eb31944", "e16040b1-eac0-4769-bc89-90bc2eb31944",
        "e16040b1-eac0-4769-bc89-90bc2eb31944", "e16040b1-eac0-4769-bc89-90bc2eb31944",
        "9c8d3e46-1758-4ade-a4b4-cd2bf649a4d6", "9c8d3e46-1758-4ade-a4b4-cd2bf649a4d6",
        "9c8d3e46-1758-4ade-a4b4-cd2bf649a4d6", "9c8d3e46-1758-4ade-a4b4-cd2bf649a4d6",
        "b24a8e4f-294f-4d24-9bc2-9c14474d83d1", "b24a8e4f-294f-4d24-9bc2-9c14474d83d1",
        "b24a8e4f-294f-4d24-9bc2-9c14474d83d1", "b24a8e4f-294f-4d24-9bc2-9c14474d83d1",
        "536e6de0-7ccf-412a-9a4e-ec55ab3988e5"
    };

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 192 &&
            root.GetProperty("ModifierScenario").GetString() == "original-add-snack-on-kill" &&
            root.GetProperty("OriginalAddSnackOnKillPrepared").GetBoolean() &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "Original AddSnackOnKill native battle failed acceptance.");

        var definitions = new Dictionary<string, string>();
        int callbacks = 0, generated = 0, fullHand = 0, skipped = 0;
        bool fullHandControl = false;
        foreach (FixtureValue row in root.GetProperty("RelicUnitTriggerCardGeneration").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("CaptureError").ValueKind == FixtureKind.Null &&
                row.GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null &&
                row.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "AddSnackOnKill callback did not complete with a supported matching model.");
            CombatContext before = row.GetProperty("Before").Deserialize<CombatContext>()!;
            CombatContext predicted = row.GetProperty("Predicted").Deserialize<CombatContext>()!;
            CombatContext actual = row.GetProperty("Actual").Deserialize<CombatContext>()!;
            CombatUnit actor = row.GetProperty("Actor").Deserialize<CombatUnit>()!;
            CombatRelicState owner = before.Relics!.Single(item => item.DataId == OwnerId);
            RelicResourceEffect effect = owner.ResourceEffects!.Single(item => item.EffectIndex == 0);
            CardGenerationRule cards = effect.CardGeneration!;
            Require(owner.AssetKey == OwnerKey && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
                owner.EffectTypes.SequenceEqual(new[] { Effect }) && effect.Type == Effect && effect.ParamInt == 2 &&
                effect.ParamInt2 == 0 && effect.ParamMinInt == 0 && effect.Amount == 2 && !effect.CanApplyInPreviewMode &&
                effect.RequiredMutatorId == null && effect.UnitTrigger?.Trigger == "OnDeath" &&
                effect.UnitTrigger.Team == "Heroes" && effect.UnitTrigger.Subtype == null && effect.UnitTrigger.RequiredStatuses.Count == 0 &&
                effect.Conditions.Count == 1 && ValidCondition(effect.Conditions[0]) && cards.Destination == "HandPile" &&
                cards.Count == 2 && cards.Pool.Select(card => card.DataId).SequenceEqual(PoolIds) &&
                cards.Permanent == false && cards.Upgrade == null && !cards.SkipDuplicateInHand && !cards.RequireHandSpace &&
                !cards.CopyModifiers && !cards.IgnoreTemporaryModifiers && !cards.DeterministicPoolSelection &&
                !cards.RequireCopyableSource && !cards.UpgradeIsPermanent && cards.DiscardCopyUpgrades.Count == 0 &&
                cards.ExternalInteractions.Count == 0 && !cards.SelectedPoolIndex.HasValue && cards.PoolEligibility == null,
                "Original AddSnackOnKill owner, weighted filtered pool or callback condition changed.");

            string definition = JsonSerializer.Serialize(new
            {
                effect.EffectIndex, effect.Type, effect.ParamInt, effect.ParamInt2, effect.ParamMinInt,
                effect.Amount, effect.CanApplyInPreviewMode, effect.RequiredMutatorId, effect.UnitTrigger,
                effect.CardGeneration,
                Conditions = effect.Conditions.Select(condition => new
                {
                    condition.Query, condition.TrackTriggerCount, condition.Comparator, condition.Value, condition.AllowMultiple
                }).ToArray()
            }, ModelJson.Options);
            if (definitions.TryGetValue(OwnerId, out string? previous)) Require(previous == definition, "AddSnackOnKill definition changed during battle.");
            else definitions.Add(OwnerId, definition);
            Require(JsonSerializer.Serialize(predicted, ModelJson.Options) == JsonSerializer.Serialize(actual, ModelJson.Options),
                "AddSnackOnKill full callback context differs from native execution.");

            int beforeCount = effect.Conditions[0].DurationTriggerCount;
            RelicConditionState afterCondition = predicted.Relics!.Single(item => item.DataId == OwnerId)
                .ResourceEffects!.Single(item => item.EffectIndex == 0).Conditions.Single();
            int expectedNotifications = afterCondition.DurationTriggerCount - beforeCount;
            Require(expectedNotifications is 0 or 1 or 2 && row.GetProperty("Notifications").GetInt32() == expectedNotifications,
                "Native inner/manager notification count differs from same-turn trigger accounting.");
            string[] predictedCards = row.GetProperty("PredictedAddedCardDataIds").EnumerateArray().Select(item => item.GetString()!).ToArray();
            int[] predictedCardIds = row.GetProperty("PredictedAddedCardInstanceIds").EnumerateArray().Select(item => item.GetInt32()).ToArray();
            Require(predictedCardIds.Length == predictedCards.Length && new HashSet<int>(predictedCardIds).SetEquals(
                predicted.Cards.Hand.Take(predictedCards.Length).Select(card => card.InstanceId)),
                "AddSnackOnKill generated-card instance identities differ from native-predicted hand insertion.");
            Require(expectedNotifications == 2 ? predictedCards.Length is 1 or 2 : predictedCards.Length == 0,
                "Native callback generated an unexpected number of hand cards.");
            if (expectedNotifications > 0)
            {
                UnityRng rng = before.BattleRng;
                for (int index = 0; index < cards.Count; index++)
                {
                    RngDraw choice = rng.Range(0, cards.Pool.Count);
                    if (index < predictedCards.Length)
                        Require(cards.Pool[choice.Value].DataId == predictedCards[index], "AddSnackOnKill ordered-pool selection diverged from Battle RNG.");
                    rng = choice.State;
                }
                Require(predicted.BattleRng.Equals(rng), "AddSnackOnKill failed to consume one Battle RNG choice per generation attempt.");
                if (before.Cards.Hand.Count >= before.MaxHandSize)
                {
                    Require(predictedCards.Length == 0 && expectedNotifications == 1, "Full-hand native callback should select twice and notify once.");
                    fullHand++;
                }
                else generated += predictedCards.Length;
                callbacks++;

                if (!fullHandControl)
                {
                    CardCycleState sourceCards = before.Cards;
                    CardToken[] tokens = sourceCards.Hand.Concat(sourceCards.Draw).Concat(sourceCards.Discard)
                        .Take(before.MaxHandSize).ToArray();
                    Require(tokens.Length == before.MaxHandSize && tokens.Select(card => card.InstanceId).Distinct().Count() == tokens.Length,
                        "Captured cards are insufficient for an isolated AddSnackOnKill full-hand control.");
                    var tokenIds = tokens.Select(card => card.InstanceId).ToHashSet();
                    CombatContext capped = before.WithCards(new CardCycleState(tokens,
                        sourceCards.Draw.Where(card => !tokenIds.Contains(card.InstanceId)).ToArray(),
                        sourceCards.Discard.Where(card => !tokenIds.Contains(card.InstanceId)).ToArray(), sourceCards.Rng,
                        sourceCards.DrawModifier, sourceCards.ExternalInteractions, sourceCards.BonusDraw,
                        sourceCards.NextDrawUpgrades, sourceCards.NextUpgradeReferenceId));
                    CardGenerationResult blocked = RelicUnitTriggerCardGenerationModel.ApplyOne(capped, OwnerId, 0,
                        actor, "OnDeath", row.GetProperty("Alive").GetBoolean(), row.GetProperty("ParamInt").GetInt32());
                    Require(blocked.Supported && blocked.AddedCards.Count == 0 && blocked.Context!.Cards.Hand.Count == capped.MaxHandSize &&
                        blocked.Context.NextCardId == capped.NextCardId && blocked.Context.Cards.Hand.SequenceEqual(capped.Cards.Hand),
                        "Full-hand AddSnackOnKill control added or allocated a card.");
                    UnityRng fullHandRng = capped.BattleRng;
                    for (int index = 0; index < cards.Count; index++) fullHandRng = fullHandRng.Range(0, cards.Pool.Count).State;
                    Require(blocked.Context.BattleRng.Equals(fullHandRng),
                        "Full-hand AddSnackOnKill did not consume Battle RNG for both selection attempts.");
                    int fullHandNotifications = blocked.Context.Relics!.Single(item => item.DataId == OwnerId)
                        .ResourceEffects!.Single(item => item.EffectIndex == 0).Conditions.Single().DurationTriggerCount -
                        capped.Relics!.Single(item => item.DataId == OwnerId).ResourceEffects!.Single(item => item.EffectIndex == 0)
                            .Conditions.Single().DurationTriggerCount;
                    Require(fullHandNotifications == 1, "Full-hand AddSnackOnKill should issue only the manager notification.");
                    fullHandControl = true;
                }
            }
            else skipped++;

            Require(actor.Team == CombatTeam.Enemy || expectedNotifications == 0,
                "AddSnackOnKill generated a card for a non-Hero native team.");
            if (row.GetProperty("Trigger").GetString() != "OnDeath")
                Require(expectedNotifications == 0, "AddSnackOnKill generated outside OnDeath.");
        }

        FixtureValue tests = root.GetProperty("RelicUnitTriggerCardGenerationTests");
        bool positiveTest = tests.EnumerateArray().Any(item => item.GetProperty("OwnerId").GetString() == OwnerId &&
            item.GetProperty("Trigger").GetString() == "OnDeath" && item.GetProperty("Team").GetString() == "Heroes" &&
            item.GetProperty("Returned").GetBoolean());
        bool triggerControl = tests.EnumerateArray().Any(item => item.GetProperty("OwnerId").GetString() == OwnerId &&
            item.GetProperty("Trigger").GetString() != "OnDeath" && !item.GetProperty("Returned").GetBoolean());
        Require(definitions.Count == 1 && callbacks > 0 && generated > 0 && positiveTest && triggerControl && fullHandControl,
            "Missing positive original OnDeath callback, generated cards or non-death trigger control.");
        Console.WriteLine($"ORIGINAL-ADD-SNACK-ON-KILL-NATIVE PASS: {callbacks} qualifying native callbacks, {generated} native generated cards, {fullHand} native full-hand cases, {skipped} gated callbacks; synthetic full-hand control consumed two Battle RNG choices and recorded one notification; exact 19-choice weighted pool and notification replay.");
    }

    private static bool ValidCondition(RelicConditionState condition) => condition.TrackTriggerCount &&
        condition.Comparator == 6 && condition.Value == 1 && !condition.AllowMultiple &&
        condition.Query.Type == "SubtypeInDeck" && condition.Query.Duration == "ThisTurn" &&
        condition.Query.TypeMatchDataIds == null && condition.Query.SubtypeMatchDataIds == null && condition.Query.SubtypeIsNone &&
        condition.Query.Subtype == null && condition.Query.SpecificCardDataId == null && condition.Query.SourceRawCost == null &&
        !condition.Query.VariableCost && condition.Query.TargetTeam == "None" &&
        !condition.Query.CurrentCostCardModifiersOnly && !condition.Query.CurrentCostSourceIsSpell;

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
