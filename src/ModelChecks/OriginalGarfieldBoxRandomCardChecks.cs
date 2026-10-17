using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalGarfieldBoxRandomCardChecks
{
    private const string OwnerId = "d537c198-899f-4385-9552-f35f58826be4";
    private const string OwnerKey = "GarfieldBoxRandomCardPerTurn";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 189 &&
            root.GetProperty("ModifierScenario").GetString() == "original-garfield-box-random-card-per-turn" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("OriginalGarfieldBoxRandomCardPrepared").GetBoolean() &&
            new[] { "CaptureFailures", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "GarfieldBoxRandomCardPerTurn native battle failed acceptance.");

        var definitions = new Dictionary<string, string>();
        int callbacks = 0, positive = 0, fullControls = 0, skippedForeign = 0;
        var turns = new HashSet<int>();
        foreach (FixtureValue row in root.GetProperty("RelicPrecachedPlayerClanGeneration").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("TestResult").GetBoolean() &&
                row.GetProperty("Turn").GetInt32() >= 1 && row.GetProperty("Selected").ValueKind == FixtureKind.String,
                "Incomplete or deployment GarfieldBox callback.");
            var before = row.GetProperty("Before").Deserialize<CombatContext>()!;
            var actual = row.GetProperty("Actual").Deserialize<CombatContext>()!;
            string ownerId = row.GetProperty("OwnerId").GetString()!;
            int effectIndex = row.GetProperty("EffectIndex").GetInt32();
            CombatRelicState owner = before.Relics!.Single(item => item.DataId == OwnerId);
            RelicResourceEffect effect = owner.ResourceEffects!.Single(item => item.EffectIndex == effectIndex);
            CardGenerationRule rule = effect.CardGeneration!;
            bool[] eligibility = rule.PoolEligibility!.ToArray();
            Require(ownerId == OwnerId && effectIndex == 0 && owner.AssetKey == OwnerKey && owner.IsCovenant == false &&
                owner.DisallowedInPlacementPhase == true && owner.EffectTypes.SequenceEqual([RelicCardGenerationModel.PrecachedPlayerClan]) &&
                effect.Type == RelicCardGenerationModel.PrecachedPlayerClan && effect.Conditions.Count == 0 &&
                effect.RequiredMutatorId == null && !effect.CanApplyInPreviewMode && rule.Destination == "HandPile" &&
                rule.Count == 1 && rule.Permanent == false && rule.Pool.Count == 348 && eligibility.Length == 348 &&
                eligibility.Count(value => value) == 58 && rule.Upgrade == null && !rule.UpgradeIsPermanent &&
                !rule.SkipDuplicateInHand && !rule.RequireHandSpace && !rule.CopyModifiers && !rule.IgnoreTemporaryModifiers &&
                rule.DiscardCopyUpgrades.Count == 0 && !rule.DeterministicPoolSelection && !rule.SelectedPoolIndex.HasValue &&
                !rule.RequireCopyableSource && rule.ExternalInteractions.Count == 0,
                "Original GarfieldBox owner, timing or ordered MegaPool payload changed.");

            string[] clans = rule.Pool.Select((creation, index) => (creation, index))
                .Where(item => eligibility[item.index])
                .Select(item => item.creation.MaskDescriptor?.Definition.LinkedClan)
                .Where(clan => clan != null).Distinct(StringComparer.Ordinal).ToArray()!;
            Require(clans.Length == 2 && rule.Pool.Select((creation, index) => (creation, index))
                .Where(item => eligibility[item.index]).All(item => item.creation.MaskDescriptor?.Definition.LinkedClan != null) &&
                rule.Pool.Select((creation, index) => (creation, index))
                    .Where(item => !eligibility[item.index]).All(item => item.creation.MaskDescriptor == null),
                "GarfieldBox eligible and foreign-clan pool entries were not captured exactly.");

            string definition = JsonSerializer.Serialize(rule, ModelJson.Options);
            if (definitions.TryGetValue(ownerId, out string? previous)) Require(previous == definition, "GarfieldBox pool changed during battle.");
            else definitions.Add(ownerId, definition);

            var oracle = Select(before.BattleRng, eligibility);
            skippedForeign += oracle.Skipped;
            string selected = row.GetProperty("Selected").GetString()!;
            Require(rule.Pool[oracle.PoolIndex].DataId == selected &&
                oracle.Rng.Equals(row.GetProperty("SelectionRng").Deserialize<UnityRng>()) &&
                before.BattleRng.Equals(row.GetProperty("TestRng").Deserialize<UnityRng>()),
                "Native pool selection or partial-shuffle Battle RNG differs from the independent oracle.");

            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            CardGenerationResult result = RelicCardGenerationModel.ApplyPrecachedPlayerClanOne(before, ownerId, effectIndex);
            Require(result.Supported, "GarfieldBox model rejected the captured native payload: " + result.UnsupportedReason);
            Equal(result.Context, actual, "native callback");
            if (before.Cards.Hand.Count >= before.MaxHandSize)
            {
                Require(result.AddedCards.Count == 0 && actual.NextCardId == before.NextCardId &&
                    actual.Cards.Hand.Count == before.Cards.Hand.Count && actual.BattleRng.Equals(oracle.Rng),
                    "Native full hand skipped GarfieldBox pool RNG or allocated a card.");
            }
            else
            {
                Require(result.AddedCards.Count == 1 && result.AddedCards[0].DataId == selected &&
                    actual.Cards.Hand.Any(card => card.InstanceId == result.AddedCards[0].InstanceId),
                    "GarfieldBox failed to add its selected nonpermanent card to hand.");
                positive++;
            }

            CombatContext capped = FillHand(before);
            CardGenerationResult blocked = RelicCardGenerationModel.ApplyPrecachedPlayerClanOne(capped, ownerId, effectIndex);
            Require(blocked.Supported && blocked.AddedCards.Count == 0 && blocked.Context!.NextCardId == capped.NextCardId &&
                blocked.Context.Cards.Hand.Count == capped.MaxHandSize && blocked.Context.BattleRng.Equals(oracle.Rng),
                "Synthetic full-hand control skipped GarfieldBox pool RNG or allocated a card.");
            fullControls++;

            foreach (bool preview in new[] { false, true })
            {
                CardGenerationResult skipped = RelicCardGenerationModel.ApplyPrecachedPlayerClanOne(before, ownerId, effectIndex,
                    placement: !preview, preview: preview);
                Require(skipped.Supported && ReferenceEquals(skipped.Context, before) && skipped.AddedCards.Count == 0,
                    "GarfieldBox deployment or preview gate consumed RNG or changed the context.");
            }
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "GarfieldBox mutated its parent battle branch.");
            turns.Add(row.GetProperty("Turn").GetInt32());
            callbacks++;
        }
        Require(callbacks >= 2 && turns.Count == callbacks && positive > 0 && fullControls == callbacks &&
            skippedForeign > 0 && definitions.Count == 1,
            "Missing natural positive, full-hand or foreign-clan shuffle witnesses.");
        Require(root.GetProperty("Mismatches").GetInt32() == 0, "Full-battle turn replay has mismatches.");
        BattleActionChecks.Native(root);
        Console.WriteLine($"ORIGINAL-GARFIELD-BOX-RANDOM-CARD-NATIVE PASS: {callbacks} exact post-draw callbacks, {positive} generated hand cards, {fullControls} synthetic full-hand RNG-only controls, {skippedForeign} skipped foreign-clan candidates; two-clan 58/348 pool, deployment/preview gates and independent partial-shuffle replay.");
    }

    private static (int PoolIndex, UnityRng Rng, int Skipped) Select(UnityRng start, IReadOnlyList<bool> eligibility)
    {
        int[] order = Enumerable.Range(0, eligibility.Count).ToArray();
        UnityRng rng = start;
        int skipped = 0;
        for (int index = 0; index < order.Length; index++)
        {
            RngDraw draw = rng.Range(index, order.Length);
            rng = draw.State;
            int candidate = order[draw.Value];
            if (eligibility[candidate]) return (candidate, rng, skipped);
            int saved = order[index];
            order[index] = order[draw.Value];
            order[draw.Value] = saved;
            skipped++;
        }
        throw new InvalidOperationException("Captured GarfieldBox pool has no player-clan choice.");
    }

    private static CombatContext FillHand(CombatContext source)
    {
        CardCycleState cards = source.Cards;
        CardToken[] hand = cards.Hand.Concat(cards.Draw).Concat(cards.Discard).Take(source.MaxHandSize).ToArray();
        if (hand.Length != source.MaxHandSize) throw new InvalidOperationException("Native context cannot fill a full hand for control.");
        var ids = hand.Select(card => card.InstanceId).ToHashSet();
        return source.WithCards(new CardCycleState(hand,
            cards.Draw.Where(card => !ids.Contains(card.InstanceId)).ToArray(),
            cards.Discard.Where(card => !ids.Contains(card.InstanceId)).ToArray(), cards.Rng, cards.DrawModifier,
            cards.ExternalInteractions, cards.BonusDraw, cards.NextDrawUpgrades, cards.NextUpgradeReferenceId));
    }

    private static void Equal<T>(T predicted, T actual, string name) => Require(
        JsonSerializer.Serialize(predicted, ModelJson.Options) == JsonSerializer.Serialize(actual, ModelJson.Options),
        "GarfieldBox " + name + " differs: " + ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options),
            JsonSerializer.Serialize(actual, ModelJson.Options)));

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
