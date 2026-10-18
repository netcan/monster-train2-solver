using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalAfterDrawGenerationChecks
{
    private static readonly (string Key, string Id)[] Owners = [
        ("AddImpToHand", "91211832-747e-4995-b4cb-7ca23c000f10"), ("ChanceToVineGrasp", "00063533-9277-485c-b100-13637a228f9a"),
        ("AddSnackToHand", "81958958-6481-4847-afe1-cb651a041f6c"), ("AddStingToHand", "5e5d863b-514c-4026-a9b0-ffe56af2a3eb") ];
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-after-draw-generation-relics" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "Original after-draw generation native battle failed acceptance.");
        var definitions = new Dictionary<string, string>(); var positive = Owners.ToDictionary(owner => owner.Id, _ => 0);
        int count = 0, full = 0, upgraded = 0;
        foreach (var row in root.GetProperty("RelicAfterDrawGeneration").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("TestResult").GetBoolean() && row.GetProperty("Turn").GetInt32() >= 1,
                "Incomplete or deployment native generation callback.");
            var before = row.GetProperty("Before").Deserialize<CombatContext>()!;
            var actual = row.GetProperty("Actual").Deserialize<CombatContext>()!;
            foreach (var expected in Owners)
            {
                var owner = before.Relics!.Single(item => item.DataId == expected.Id);
                var effect = owner.ResourceEffects!.Single(); var rule = effect.CardGeneration!;
                Require(owner.AssetKey == expected.Key && owner.DisallowedInPlacementPhase == true && owner.IsCovenant == false &&
                    owner.EffectTypes.SequenceEqual([RelicCardGenerationModel.AfterDraw]) && effect.EffectIndex == 0 &&
                    effect.Conditions.Count == 0 && !effect.CanApplyInPreviewMode && effect.ParamInt == 1 && effect.ParamInt2 == 0 &&
                    effect.RequiredMutatorId == null && rule.Destination == "HandPile" && rule.Count == 1 && rule.Pool.Count > 0 &&
                    !rule.UpgradeIsPermanent && !rule.DeterministicPoolSelection && !rule.RequireHandSpace && !rule.CopyModifiers &&
                    !rule.SkipDuplicateInHand && rule.ExternalInteractions.Count == 0,
                    "Original after-draw owner, lifecycle gate or generated pool changed: " + expected.Key);
                if (expected.Key == "ChanceToVineGrasp")
                    Require(rule.Upgrade?.DataId == "80a5edf4-6dc8-41a7-ab4e-9d0e914e0848" &&
                        rule.Upgrade.Lifecycle!.AddedTraits.Single().RuntimeType == "CardTraitEphemeral", "Original temporary Ephemeral upgrade changed.");
                else Require(rule.Upgrade == null, "Unexpected original after-draw upgrade.");
                string serialized = JsonSerializer.Serialize(effect, ModelJson.Options);
                if (definitions.TryGetValue(expected.Id, out string? previous)) Require(previous == serialized, "Original generation pool changed during battle.");
                else definitions.Add(expected.Id, serialized);
            }
            string ownerId = row.GetProperty("OwnerId").GetString()!;
            int effectIndex = row.GetProperty("EffectIndex").GetInt32();
            var generation = before.Relics!.Single(owner => owner.DataId == ownerId).ResourceEffects!.Single().CardGeneration!;
            var choice = before.BattleRng.Range(0, generation.Pool.Count);
            Require(generation.Pool[choice.Value].DataId == row.GetProperty("Selected").GetString() &&
                choice.State.Equals(row.GetProperty("TestRng").Deserialize<UnityRng>()), "Native TestEffect selection or RNG differs.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            var result = RelicCardGenerationModel.ApplyOne(before, ownerId, effectIndex);
            Require(result.Supported, "After-draw consumer unsupported: " + result.UnsupportedReason);
            Equal(result.Context, actual, "native consumer");
            int added = result.AddedCards.Count;
            if (before.Cards.Hand.Count >= before.MaxHandSize)
            {
                Require(added == 0 && actual.NextCardId == before.NextCardId, "Full native hand allocated a generated card."); full++;
            }
            else
            {
                Require(added == 1 && result.AddedCards[0].DataId == row.GetProperty("Selected").GetString() &&
                    actual.Cards.Hand.Any(card => card.InstanceId == result.AddedCards[0].InstanceId), "Original owner lacks positive generated hand card.");
                positive[ownerId]++;
                if (generation.Upgrade != null)
                {
                    var card = actual.FindCard(result.AddedCards[0].InstanceId)!;
                    Require(card.Temporary.Upgrades.Any(upgrade => upgrade.DataId == generation.Upgrade.DataId) &&
                        card.Permanent.Upgrades.All(upgrade => upgrade.DataId != generation.Upgrade.DataId), "Ephemeral upgrade became permanent or was lost."); upgraded++;
                }
            }
            foreach (bool preview in new[] { false, true })
            {
                var skipped = RelicCardGenerationModel.ApplyOne(before, ownerId, effectIndex, placement: !preview, preview: preview);
                Require(skipped.Supported && ReferenceEquals(skipped.Context, before) && skipped.AddedCards.Count == 0, "Deployment/preview gate consumed generation state.");
            }
            var cards = before.Cards;
            var tokens = cards.Hand.Concat(cards.Draw).Concat(cards.Discard).Take(before.MaxHandSize).ToArray();
            if (tokens.Length == before.MaxHandSize)
            {
                var ids = tokens.Select(card => card.InstanceId).ToHashSet();
                var capped = before.WithCards(new(tokens, cards.Draw.Where(card => !ids.Contains(card.InstanceId)).ToArray(),
                    cards.Discard.Where(card => !ids.Contains(card.InstanceId)).ToArray(), cards.Rng, cards.DrawModifier, cards.ExternalInteractions,
                    cards.BonusDraw, cards.NextDrawUpgrades, cards.NextUpgradeReferenceId));
                var blocked = RelicCardGenerationModel.ApplyOne(capped, ownerId, effectIndex);
                Require(blocked.Supported && blocked.AddedCards.Count == 0 && blocked.Context!.NextCardId == capped.NextCardId &&
                    blocked.Context.BattleRng.Equals(choice.State), "Full-hand control skipped pool RNG or allocated an identity.");
            }
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Generation mutated its parent branch."); count++;
        }
        Require(positive.Values.All(value => value > 0) && count >= 8 && upgraded > 0 && definitions.Count == 4,
            "Missing positive original generation owners or temporary upgrade witness.");
        int rejectedFoodPlays = 0;
        foreach (var action in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState decision = action.GetProperty("Before").Deserialize<BattleTurnState>()!;
            CombatContext context = decision.Spawn.Train.Context!;
            CardToken? food = context.Cards.Hand.FirstOrDefault(card => decision.PlayRules!.Cards.Any(rule => rule.DataId == card.DataId &&
                rule.SpawnUnit?.Triggers.Any(trigger => trigger.Kind == "OnEaten") == true));
            if (food == null) continue;
            BattleActionResult unsupported = BattleActionModel.PlayCard(decision, new(food.InstanceId, decision.SelectedRoom, 0));
            Require(unsupported.Rejection == ActionRejection.Unsupported && unsupported.Reason ==
                "Playing food cards requires separate native card-backed feeding/removal evidence.",
                "Generated food became playable despite the explicit card-backed feeding boundary.");
            rejectedFoodPlays++;
            break;
        }
        Require(rejectedFoodPlays > 0, "Missing native generated-food hand for unsupported-play guard.");
        BattleActionChecks.Native(root);
        Console.WriteLine($"ORIGINAL-AFTER-DRAW-GENERATION-NATIVE PASS: four unchanged owners, {count} exact Test/Apply consumers, positive hand cards {string.Join('/', positive.Values)}, {full} native full hands and {upgraded} temporary Ephemeral upgrades; exact Battle RNG, deployment/preview/full-hand controls and independent policy replay.");
    }
    private static void Equal<T>(T predicted, T actual, string name) => Require(JsonSerializer.Serialize(predicted, ModelJson.Options) == JsonSerializer.Serialize(actual, ModelJson.Options),
        "After-draw " + name + " differs: " + ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options)));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
