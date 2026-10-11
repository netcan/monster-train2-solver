using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TypedDrawNativeChecks
{
    internal static void Native(FixtureValue fixture, bool required = false)
    {
        if (!fixture.TryGetProperty("TypedDrawEffects", out var records) || records.GetArrayLength() == 0)
        {
            Require(!required, "The requested native typed-draw fixture has no recorded effects.");
            return;
        }
        FixtureValue[] attempts = fixture.GetProperty("TypedDrawCards").EnumerateArray().ToArray();
        int index = 0, typed = 0, schedules = 0, fallback = 0, noDraw = 0, ranged = 0, unique = 0;
        var originals = new HashSet<string>();
        (CombatContext Before, TypedDrawRule Rule, int Amount, int Source, BattlePlayRules Definitions, string Expected)? branch = null;
        foreach (FixtureValue record in records.EnumerateArray())
        {
            Require(record.GetProperty("Completed").GetBoolean(), "Incomplete native typed-draw effect.");
            CombatContext before = record.GetProperty("Before").Deserialize<CombatContext>()!;
            CombatContext after = record.GetProperty("After").Deserialize<CombatContext>()!;
            BattlePlayRules definitions = record.GetProperty("Definitions").Deserialize<BattlePlayRules>()!;
            int source = record.GetProperty("PlayedCardId").GetInt32();
            string asset = record.GetProperty("CardAsset").GetString()!;
            originals.Add(asset);
            CombatContext predicted;
            if (record.GetProperty("Kind").GetString() == "DrawType")
            {
                Require(record.GetProperty("Sampled").GetBoolean(), "Native typed draw did not sample its amount.");
                TypedDrawRule rule = record.GetProperty("Rule").Deserialize<TypedDrawRule>()!;
                CardEffectRange? range = record.GetProperty("Range").Deserialize<CardEffectRange>();
                int amount = record.GetProperty("Amount").GetInt32();
                CombatContext application = before;
                if (range != null)
                {
                    RngDraw sample = range.Sample(before.BattleRng);
                    Require(sample.Value == amount, "Native typed-draw range value differs.");
                    application = before.WithBattleRng(sample.State); ranged++;
                }
                TypedDrawResult result = TypedDrawModel.Draw(application, rule, amount, source, definitions);
                Require(result.Supported, "Native typed draw unsupported: " + result.UnsupportedReason);
                predicted = result.Context!;
                int[] actualDraws = attempts.Where(attempt => attempt.GetProperty("EffectRecordIndex").GetInt32() == index &&
                    attempt.GetProperty("Success").GetBoolean()).Select(attempt => attempt.GetProperty("CardId").GetInt32()).ToArray();
                Require(result.DrawnCardIds.SequenceEqual(actualDraws), "Typed draw order differs from DrawSpecificCard callbacks.");
                if (result.DrawnCardIds.Count == 0) noDraw++;
                if (rule.PrimaryFilter != null && rule.SecondaryFilter != null && amount > 0 &&
                    before.Cards.Discard.Concat(before.Cards.Draw).Where(card => card.InstanceId != source).All(card =>
                        !CardUpgradeMaskModel.FilterCard(rule.PrimaryFilter, CardBranchMaskModel.Resolve(before.FindCard(card.InstanceId)!)))) fallback++;
                if (branch == null && result.DrawnCardIds.Count > 0 && application.Cards.NextDrawUpgrades?.Count > 0)
                    branch = (application, rule, amount, source, definitions, JsonSerializer.Serialize(predicted));
                typed++;
            }
            else
            {
                NextDrawUpgradeRule upgrade = record.GetProperty("Upgrade").Deserialize<NextDrawUpgradeRule>()!;
                predicted = NextDrawUpgradeModel.Schedule(before, upgrade, record.GetProperty("RequiredGold").GetInt32());
                if (upgrade.Upgrade.Unique && before.Cards.NextDrawUpgrades!.Count == after.Cards.NextDrawUpgrades!.Count) unique++;
                schedules++;
            }
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted), JsonSerializer.Serialize(after));
            Require(difference == null, $"Native typed-draw effect {index} ({asset}, {record.GetProperty("Kind").GetString()}) differs: {difference}");
            index++;
        }
        int clears = 0;
        foreach (FixtureValue clear in fixture.GetProperty("TypedDrawClears").EnumerateArray())
        {
            Require(clear.GetProperty("Boundary").GetString() is "OnCardPlayed" or "OnCardEffectsWereApplied",
                "Unknown native next-draw clearing boundary.");
            Require(clear.GetProperty("Before").GetArrayLength() > 0 && clear.GetProperty("After").GetArrayLength() == 0,
                "Next-draw upgrades did not clear at the native effect-end boundary.");
            clears++;
        }
        int drawStates = 0;
        foreach (FixtureValue attempt in attempts.Where(attempt => attempt.GetProperty("PendingUpgrades").GetInt32() > 0))
        {
            CombatContext before = attempt.GetProperty("Before").Deserialize<CombatContext>()!;
            CombatContext after = attempt.GetProperty("After").Deserialize<CombatContext>()!;
            BattlePlayRules definitions = attempt.GetProperty("Definitions").Deserialize<BattlePlayRules>()!;
            TypedDrawResult result = NextDrawUpgradeModel.DrawSpecific(before, attempt.GetProperty("CardId").GetInt32(),
                attempt.GetProperty("SourceCardId").GetInt32(), definitions);
            Require(result.Supported, "Native individual upgraded draw unsupported: " + result.UnsupportedReason);
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(result.Context), JsonSerializer.Serialize(after));
            Require(difference == null, "Native individual upgraded draw differs: " + difference);
            Require((result.DrawnCardIds.Count > 0) == attempt.GetProperty("Success").GetBoolean(), "Individual draw success differs.");
            drawStates++;
        }
        int full = attempts.Count(attempt => !attempt.GetProperty("Success").GetBoolean() &&
            attempt.GetProperty("HandCount").GetInt32() == attempt.GetProperty("MaxHandSize").GetInt32());
        int multiplied = attempts.Count(attempt => attempt.GetProperty("Success").GetBoolean() &&
            attempt.GetProperty("PendingUpgrades").GetInt32() > 0 && attempt.GetProperty("MagicPowerMultiplier").GetInt32() > 1);
        int discard = attempts.Count(attempt => attempt.GetProperty("Success").GetBoolean() && attempt.GetProperty("FromDiscard").GetBoolean());
        if (fixture.TryGetProperty("ModifierScenario", out var scenario) && scenario.GetString() == "typed-draw")
        {
            Require(typed > 0 && schedules > 0 && fallback > 0 && noDraw > 0 && ranged > 0 && unique > 0 && clears > 0 && full > 0 && multiplied > 0 &&
                originals.Contains("Channelsong") && originals.Contains("GiftsForAGuard"),
                $"Native typed-draw coverage incomplete: typed={typed}, schedules={schedules}, fallback={fallback}, noDraw={noDraw}, ranged={ranged}, " +
                $"unique={unique}, clears={clears}, full={full}, multiplied={multiplied}, originals={string.Join(',', originals)}.");
            VerifyPlayedConsume(fixture, records.EnumerateArray().ToArray(), attempts);
            VerifySharedUpgradeReferences(fixture, records.EnumerateArray().ToArray());
        }
        if (branch is { } root)
        {
            string parent = JsonSerializer.Serialize(root.Before);
            Parallel.For(0, 16, _ =>
            {
                TypedDrawResult result = TypedDrawModel.Draw(root.Before, root.Rule, root.Amount, root.Source, root.Definitions);
                Require(result.Supported && JsonSerializer.Serialize(result.Context) == root.Expected, "Parallel native typed-draw branches differ.");
            });
            Require(JsonSerializer.Serialize(root.Before) == parent, "Typed-draw branches mutated their parent.");
        }
        Console.WriteLine($"NATIVE-TYPED-DRAW-CHECKS PASS: {typed} exact draws, {schedules} upgrade schedules, {fallback} fallbacks, {noDraw} empty results, " +
            $"{ranged} ranges, {unique} unique suppressions, {full} full-hand refusals, {multiplied} magic-power draws, {discard} discard draws, {drawStates} individual draw states, {clears} effect-end clears and 16 parallel branches.");
    }

    private static void VerifyPlayedConsume(FixtureValue fixture, FixtureValue[] effects, FixtureValue[] attempts)
    {
        const string consume = "CardTraitExhaustState";
        // Follow the recorded source effect, actual drawn instance and its upgrade
        // identity. The recipient may be any spell; its asset name is irrelevant.
        var gifted = new Dictionary<int, HashSet<string>>();
        foreach (FixtureValue attempt in attempts.Where(item => item.GetProperty("Success").GetBoolean() &&
            item.GetProperty("PendingUpgrades").GetInt32() > 0))
        {
            int effectIndex = attempt.GetProperty("EffectRecordIndex").GetInt32();
            if (effectIndex < 0 || effects[effectIndex].GetProperty("CardAsset").GetString() != "GiftsForAGuard") continue;
            int id = attempt.GetProperty("CardId").GetInt32();
            var before = attempt.GetProperty("Before").Deserialize<CombatContext>()!;
            var after = attempt.GetProperty("After").Deserialize<CombatContext>()!;
            CardInstanceState oldCard = before.FindCard(id)!, newCard = after.FindCard(id)!;
            if (CardExhaustModel.HasConsume(oldCard) || !CardExhaustModel.HasConsume(newCard)) continue;
            var scheduled = before.Cards.NextDrawUpgrades!.Where(rule =>
                rule.Upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == consume) == true)
                .Select(rule => rule.Upgrade.DataId).ToHashSet();
            var installed = newCard.Temporary.Upgrades.Where(upgrade => scheduled.Contains(upgrade.DataId) &&
                upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == consume) == true)
                .Select(upgrade => upgrade.DataId).ToHashSet();
            if (installed.Count > 0) gifted[id] = installed;
        }
        var consumedOriginals = new HashSet<string>();
        int consumedRecipients = 0;
        foreach (FixtureValue entry in fixture.GetProperty("Actions").EnumerateArray())
        {
            var before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            if (action.ActivatorUnitId != 0) continue;
            CombatContext old = before.Spawn.Train.Context!, actual = after.Spawn.Train.Context!;
            CardInstanceState card = old.FindCard(action.CardInstanceId)!;
            string asset = before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId).AssetKey;
            bool original = asset is "Channelsong" or "GiftsForAGuard";
            bool recipient = gifted.TryGetValue(card.InstanceId, out var upgrades) &&
                card.Temporary.Upgrades.Any(upgrade => upgrades.Contains(upgrade.DataId));
            if (!original && !recipient) continue;
            Require(CardExhaustModel.HasConsume(card), "A recorded Consume witness lost its active trait before play.");
            Require(actual.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(item => item.InstanceId == card.InstanceId) &&
                actual.Statistics!.Value(card.InstanceId, "TimesExhausted") > old.Statistics!.Value(card.InstanceId, "TimesExhausted"),
                "A played Consume witness did not enter Exhausted and increment TimesExhausted: " + asset + "/" + card.InstanceId);
            Require(!actual.Cards.Hand.Concat(actual.Cards.Draw).Concat(actual.Cards.Discard).Any(item => item.InstanceId == card.InstanceId),
                "A played Consume witness remained in an ordinary pile.");
            if (original) consumedOriginals.Add(asset);
            if (recipient) consumedRecipients++;
        }
        Require(consumedOriginals.SetEquals(new[] { "Channelsong", "GiftsForAGuard" }) && consumedRecipients > 0,
            "Native Consume play coverage is incomplete: originals=" + string.Join(',', consumedOriginals) +
            ", temporary recipients=" + consumedRecipients + ", gifted candidates=" + gifted.Count + ".");
        Console.WriteLine($"NATIVE-TYPED-DRAW-CONSUME-CHECKS PASS: both original spells and {consumedRecipients} originally non-Consume recipients actually played, exhausted and incremented native statistics.");
    }

    private static void VerifySharedUpgradeReferences(FixtureValue fixture, FixtureValue[] effects)
    {
        int queuedRefreshes = 0, clearedRefreshes = 0;
        CombatContext? previous = null;
        foreach (FixtureValue effect in effects)
        {
            CombatContext before = effect.GetProperty("Before").Deserialize<CombatContext>()!;
            Require(before.Cards.NextUpgradeReferenceId.HasValue, "Native typed draw omitted its shared-reference allocator.");
            foreach (NextDrawUpgradeRule rule in before.Cards.NextDrawUpgrades!)
            {
                Require(rule.Upgrade.SharedReferenceId.HasValue, "A native queued upgrade omitted its shared-reference identity.");
                int id = rule.Upgrade.SharedReferenceId!.Value;
                CardUpgradeModifier? old = previous?.Cards.NextDrawUpgrades?.Select(item => item.Upgrade)
                    .SingleOrDefault(upgrade => upgrade.SharedReferenceId == id);
                if (old != null && (old.Stats.Damage != rule.Upgrade.Stats.Damage || old.Stats.Heal != rule.Upgrade.Stats.Heal) &&
                    previous!.CardInstances!.Any(card => card.Permanent.Upgrades.Concat(card.Temporary.Upgrades)
                        .Any(upgrade => upgrade.SharedReferenceId == id))) queuedRefreshes++;
            }
            previous = effect.GetProperty("After").Deserialize<CombatContext>()!;
        }
        foreach (FixtureValue entry in fixture.GetProperty("Actions").EnumerateArray())
        {
            CombatContext before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
            CombatContext actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
            if (before.Cards.NextDrawUpgrades?.Count != 0) continue;
            var aliases = before.CardInstances!.SelectMany(card => card.Permanent.Upgrades.Concat(card.Temporary.Upgrades)
                .Where(upgrade => upgrade.SharedReferenceId.HasValue).Select(upgrade => (card.InstanceId, Upgrade: upgrade)))
                .GroupBy(item => item.Upgrade.SharedReferenceId!.Value)
                .Where(group => group.Select(item => item.InstanceId).Distinct().Count() > 1);
            foreach (var alias in aliases)
            {
                CardUpgradeModifier old = alias.First().Upgrade;
                CardUpgradeModifier[] values = actual.CardInstances!.SelectMany(card => card.Permanent.Upgrades.Concat(card.Temporary.Upgrades))
                    .Where(upgrade => upgrade.SharedReferenceId == alias.Key).ToArray();
                if (values.Length < 2 || !values.Any(upgrade => upgrade.Stats.Damage != old.Stats.Damage || upgrade.Stats.Heal != old.Stats.Heal)) continue;
                Require(values.All(upgrade => upgrade.Stats.Damage == values[0].Stats.Damage && upgrade.Stats.Heal == values[0].Stats.Heal),
                    "Native shared-reference cards disagree after an in-place magic-power refresh.");
                clearedRefreshes++;
            }
        }
        Require(queuedRefreshes > 0 && clearedRefreshes > 0,
            $"Native upgrade alias witnesses are incomplete: queued refreshes={queuedRefreshes}, post-clear refreshes={clearedRefreshes}.");
        Console.WriteLine($"NATIVE-UPGRADE-ALIAS-CHECKS PASS: {queuedRefreshes} installed-to-queue refreshes and {clearedRefreshes} post-clear shared-card refreshes.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
