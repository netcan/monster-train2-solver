using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class RandomCampaignChecks
{
    internal static void Curated(string path)
    {
        using var source = ModelJson.ReadFixture(path);
        if (path.EndsWith("full-battle-random-campaign-11-ring7-pyre-refined-search.mt2f", StringComparison.Ordinal))
        {
            using var replay = ModelJson.ReadFixture(path.Replace("-search.mt2f", "-replay.mt2f", StringComparison.Ordinal));
            RefinedPair(source.RootElement, replay.RootElement, 73751504, 0, 6, "Stygian", "Awoken", 38);
            foreach (var fixture in new[] { source.RootElement, replay.RootElement })
            {
                var final = fixture.GetProperty("RollingDecisions").EnumerateArray().Last().GetProperty("Actual").Deserialize<BattleTurnState>()!;
                Require(final.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 21,
                    "Changed winning-route improvement: expected Pyre21.");
            }
            return;
        }
        if (path.EndsWith("full-battle-random-campaign-11-ring7-refined-search.mt2f", StringComparison.Ordinal))
        {
            using var replay = ModelJson.ReadFixture(path.Replace("-search.mt2f", "-replay.mt2f", StringComparison.Ordinal));
            RefinedPair(source.RootElement, replay.RootElement, 73751504, 0, 6, "Stygian", "Awoken", 36);
            return;
        }
        if (path.EndsWith("full-battle-random-campaign-10-ring8-release099-budget10000-loss.mt2f", StringComparison.Ordinal))
            TacticalShiftChecks.LethalRestoration(source.RootElement);
        if (path.Contains("full-battle-random-campaign-10-ring7-", StringComparison.Ordinal))
            CardCorruptionAttackUpgradeChecks.NaturalQueries(source.RootElement);
        if (path.EndsWith("full-battle-random-campaign-10-ring7-refined-order-search.mt2f", StringComparison.Ordinal))
        {
            using var replay = ModelJson.ReadFixture(path.Replace("-search.mt2f", "-replay.mt2f", StringComparison.Ordinal));
            CardCorruptionAttackUpgradeChecks.NaturalQueries(replay.RootElement);
            RefinedPair(source.RootElement, replay.RootElement, 1895012796, 0, 6, "Banished", "Wurmkin", 54);
            return;
        }
        if (path.EndsWith("full-battle-random-campaign-10-ring6-unbox-refined-search.mt2f", StringComparison.Ordinal))
        {
            using var replay = ModelJson.ReadFixture(path.Replace("-search.mt2f", "-replay.mt2f", StringComparison.Ordinal));
            RefinedPair(source.RootElement, replay.RootElement, 1895012796, 0, 5, "Banished", "Wurmkin", 66);
            return;
        }
        if (path.EndsWith("full-battle-random-campaign-09-ring8-refined-suffix-search.mt2f", StringComparison.Ordinal))
        {
            using var replay = ModelJson.ReadFixture(path.Replace("-search.mt2f", "-replay.mt2f", StringComparison.Ordinal));
            RefinedPair(source.RootElement, replay.RootElement, 771950956, 0, 7, "Wurmkin", "Hellhorned", 48);
            foreach (var fixture in new[] { source.RootElement, replay.RootElement })
            {
                var final = fixture.GetProperty("RollingDecisions").EnumerateArray().Last().GetProperty("Actual").Deserialize<BattleTurnState>()!;
                Require(final.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 1,
                    "Changed curated final-battle win: expected Pyre1.");
            }
            return;
        }
        if (path.EndsWith("full-battle-random-campaign-09-ring7-refined-order-search.mt2f", StringComparison.Ordinal))
        {
            using var replay = ModelJson.ReadFixture(path.Replace("-search.mt2f", "-replay.mt2f", StringComparison.Ordinal));
            RefinedPair(source.RootElement, replay.RootElement, 771950956, 0, 6, "Wurmkin", "Hellhorned", 37);
            return;
        }
        if (path.EndsWith("full-battle-random-campaign-09-ring6-refined-search.mt2f", StringComparison.Ordinal) ||
            path.EndsWith("full-battle-random-campaign-09-ring6-release082-refined-search.mt2f", StringComparison.Ordinal))
        {
            using var replay = ModelJson.ReadFixture(path.Replace("-search.mt2f", "-replay.mt2f", StringComparison.Ordinal));
            RefinedPair(source.RootElement, replay.RootElement, 771950956, 0, 5, "Wurmkin", "Hellhorned", 50);
            return;
        }
        if (path.EndsWith("full-battle-random-campaign-09-ring5-refined-search.mt2f", StringComparison.Ordinal))
        {
            using var replay = ModelJson.ReadFixture(path.Replace("-search.mt2f", "-replay.mt2f", StringComparison.Ordinal));
            RefinedPair(source.RootElement, replay.RootElement, 771950956, 0, 4, "Wurmkin", "Hellhorned", 43);
            return;
        }
        if (path.EndsWith("full-battle-random-campaign-09-ring3-refined-search.mt2f", StringComparison.Ordinal))
        {
            using var replay = ModelJson.ReadFixture(path.Replace("-search.mt2f", "-replay.mt2f", StringComparison.Ordinal));
            RefinedPair(source.RootElement, replay.RootElement, 771950956, 0, 2, "Wurmkin", "Hellhorned", 37);
            return;
        }
        if (path.EndsWith("full-battle-random-campaign-09-ring2-refined-search.mt2f", StringComparison.Ordinal))
        {
            using var replay = ModelJson.ReadFixture(path.Replace("-search.mt2f", "-replay.mt2f", StringComparison.Ordinal));
            RefinedPair(source.RootElement, replay.RootElement, 771950956, 0, 1, "Wurmkin", "Hellhorned", 24);
            StrongerCorruptionCopyChecks.Effects(source.RootElement);
            StrongerCorruptionCopyChecks.Effects(replay.RootElement);
            return;
        }
        if (path.EndsWith("full-battle-random-campaign-06-ring5-budget10000-loss.mt2f", StringComparison.Ordinal))
            SacrificePolicyChecks.Native(source.RootElement);
        if (path.EndsWith("full-battle-random-campaign-09-ring5-budget10000-loss.mt2f", StringComparison.Ordinal))
            FriendlyRoomDamagePolicyChecks.Native(source.RootElement);
        if (path.EndsWith("full-battle-random-campaign-05-ring6-root30000-keep-champion-search.mt2f", StringComparison.Ordinal))
        {
            using var replay = ModelJson.ReadFixture(path.Replace("-search.mt2f", "-replay.mt2f", StringComparison.Ordinal));
            RefinedPair(source.RootElement, replay.RootElement, 972508374, 0, 5, "Stygian", "LazarusLeague", 50);
            foreach (var fixture in new[] { source.RootElement, replay.RootElement })
            {
                var final = fixture.GetProperty("RollingDecisions").EnumerateArray().Last().GetProperty("Actual").Deserialize<BattleTurnState>()!;
                Require(final.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 59,
                    "Changed curated offline-root Ring6 win: expected Pyre59.");
            }
            return;
        }
        if (path.EndsWith("full-battle-metagame-campaign-ring3-refined-search.mt2f", StringComparison.Ordinal))
        {
            using var replay = ModelJson.ReadFixture(path.Replace("-search.mt2f", "-replay.mt2f", StringComparison.Ordinal));
            RefinedPair(source.RootElement, replay.RootElement, 424245, 0, 2, "Banished", "Pyreborne", 41);
            return;
        }
        if (path.EndsWith("full-battle-metagame-campaign-ring4-refined-search.mt2f", StringComparison.Ordinal) ||
            path.EndsWith("full-battle-metagame-campaign-ring5-refined-search.mt2f", StringComparison.Ordinal))
        {
            bool ring4 = path.EndsWith("full-battle-metagame-campaign-ring4-refined-search.mt2f", StringComparison.Ordinal);
            using var replay = ModelJson.ReadFixture(path.Replace("-search.mt2f", "-replay.mt2f", StringComparison.Ordinal));
            RefinedPair(source.RootElement, replay.RootElement, 424245, 0, ring4 ? 3 : 4, "Banished", "Pyreborne", ring4 ? 53 : 48);
            return;
        }
        if (path.EndsWith("full-battle-random-campaign-08-ring3-refined-search.mt2f", StringComparison.Ordinal))
        {
            using var replay = ModelJson.ReadFixture(path.Replace("-search.mt2f", "-replay.mt2f", StringComparison.Ordinal));
            RefinedPair(source.RootElement, replay.RootElement, 1347120220, 0, 2, "Umbra", "Stygian", 61);
            return;
        }
        if (path.EndsWith("full-battle-random-campaign-04-ring5-refined-search.mt2f", StringComparison.Ordinal))
        {
            using var replay = ModelJson.ReadFixture(path.Replace("-search.mt2f", "-replay.mt2f", StringComparison.Ordinal));
            RefinedPair(source.RootElement, replay.RootElement);
            return;
        }
        if (path.EndsWith("player-random-campaign-02-ring3-fixed-policy-native-win.mt2f", StringComparison.Ordinal))
        {
            FixedPolicyWin(source.RootElement);
            return;
        }
        if (path.EndsWith("player-random-campaign-02-ring3-relentless-preview-observed.mt2f", StringComparison.Ordinal))
        {
            RelentlessPreviewCooldownChecks.Native(source.RootElement);
            return;
        }
        if (path.EndsWith("player-random-campaign-02-ring3-permafrost-copy-observed-prefix.mt2f", StringComparison.Ordinal))
        {
            PermafrostCopyObserved(source.RootElement);
            return;
        }
        if (path.EndsWith("player-random-campaign-02-ring3-upgrade-observed-prefix.mt2f", StringComparison.Ordinal))
        {
            GrowthObserved(source.RootElement);
            return;
        }
        FixtureValue input = source.RootElement.GetProperty("NativeRunInput");
        int seed = input.GetProperty("Seed").GetInt32(), rank = input.GetProperty("AscensionLevel").GetInt32();
        int distance = input.GetProperty("Distance").GetInt32();
        int timeLimitMs = source.RootElement.GetProperty("PlayerAdvisor").GetProperty("Overlay").GetProperty("TimeLimitMs").GetInt32();
        string primary = input.GetProperty("PrimaryClan").GetString()!, secondary = input.GetProperty("SecondaryClan").GetString()!;
        if (path.EndsWith("-loss.mt2f", StringComparison.Ordinal))
        {
            Require(!source.RootElement.GetProperty("NativeWon").GetBoolean(), "A curated loss must be an actual terminal defeat.");
            Source(source.RootElement, seed, rank, distance, primary, secondary, timeLimitMs);
        }
        else
        {
            Require(path.EndsWith("-search.mt2f", StringComparison.Ordinal), "A curated pair must start with its source archive.");
            string replayPath = path.Substring(0, path.Length - "-search.mt2f".Length) + "-replay.mt2f";
            using var replay = ModelJson.ReadFixture(replayPath);
            Pair(source.RootElement, replay.RootElement, seed, rank, distance, primary, secondary, timeLimitMs);
            if (Path.GetFileName(path) == "full-battle-random-campaign-02-ring2-search.mt2f")
            {
                using var before = ModelJson.ReadFixture(Path.Combine(Path.GetDirectoryName(path)!,
                    "player-random-campaign-02-ring2-permafrost-unavailable.mt2f"));
                MerchantPermafrost(before.RootElement, source.RootElement);
            }
        }
    }

    internal static void GrowthObserved(FixtureValue fixture)
    {
        Input(fixture, 1862997127, 0, 2, "Pyreborne", "Banished");
        RollingAdviceChecks.ObservedSuccessors(fixture);
        FixtureValue last = fixture.GetProperty("RollingDecisions").EnumerateArray().Last();
        BattleTurnState before = last.GetProperty("Root").Deserialize<BattleTurnState>()!;
        BattleTurnState after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        const string growthId = "995b9c12-c816-4960-9905-cd6e1a29d362";
        CardInstanceState owner = before.Spawn.Train.Context!.CardInstances!.Single(card => card.InstanceId == 4);
        CardInstanceState changed = after.Spawn.Train.Context!.FindCard(4)!;
        Require(owner.Permanent.Upgrades.Where(upgrade => upgrade.DataId == growthId).Select(upgrade => upgrade.Stats.Damage).SequenceEqual([1, 1, 1]) &&
            changed.Permanent.Upgrades.Where(upgrade => upgrade.DataId == growthId).Select(upgrade => upgrade.Stats.Damage).SequenceEqual([1, 1, 2]),
            "Native champion growth must update its current spawn record and retain both preceding battles' records.");
        Require(fixture.GetProperty("RollingDecisions").GetArrayLength() == 12 &&
            fixture.GetProperty("NativeWon").ValueKind == FixtureKind.Null,
            "This archive is a twelve-input observation, not a completed native victory.");
        Console.WriteLine("RANDOM-CAMPAIGN-GROWTH PASS: twelve native successors, cached box IDs and current-spawn permanent growth; no full battle acceptance claimed.");
    }

    private static void FixedPolicyWin(FixtureValue fixture)
    {
        Input(fixture, 1862997127, 0, 2, "Pyreborne", "Banished", 10000);
        RollingAdviceChecks.Native(fixture);
        TrainCombatChecks.Native(fixture);
        FixtureValue[] rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        BattleTurnState final = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(fixture.GetProperty("NativeWon").GetBoolean() && rows.Length == 48 &&
            rows.All(row => row.GetProperty("Mode").GetString() == "replay" && row.GetProperty("Nodes").GetInt32() == 0) &&
            fixture.GetProperty("PlayerAdvisor").GetProperty("Overlay").GetProperty("Published").GetInt32() == 0 &&
            final.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 5,
            "Fixed policy evidence must remain a native 48-input win with zero search/publications and Pyre5.");
        BattleTurnState after = rows[43].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(after.Spawn.Train.Context!.FindCard(4)!.PlayedRoomUnitIds!.SequenceEqual([2, 8, 24]),
            "The previously failing preview/cooldown callback was not exercised.");
        Console.WriteLine("RANDOM-CAMPAIGN-FIXED-POLICY PASS: 48 native inputs, Pyre5; frozen external policy, not actual-default or campaign-clear acceptance.");
    }

    internal static void PermafrostCopyObserved(FixtureValue fixture)
    {
        Input(fixture, 1862997127, 0, 2, "Pyreborne", "Banished");
        RollingAdviceChecks.ObservedSuccessors(fixture);
        FixtureValue last = fixture.GetProperty("RollingDecisions").EnumerateArray().Last();
        BattleTurnState before = last.GetProperty("Root").Deserialize<BattleTurnState>()!;
        BattleTurnState after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext original = before.Spawn.Train.Context!, actual = after.Spawn.Train.Context!;
        const string permafrost = "e0af4fe5-75c0-4017-ae5e-5a87099aa6c5";
        CardInstanceState copied = actual.CardInstances!.Single(card => original.FindCard(card.InstanceId) == null &&
            card.Permanent.Upgrades.Any(upgrade => upgrade.DataId == permafrost));
        Require(copied.MaskDescriptor!.BaseTraits.Count(trait => trait.RuntimeType == "CardTraitPermafrost") == 1 &&
            copied.MaskDescriptor.TemporaryTraits.Count(trait => trait.RuntimeType == "CardTraitFreeze") == 1 &&
            actual.Cards.Hand.Any(card => card.InstanceId == copied.InstanceId),
            "A boxed merchant Permafrost copy must install its permanent trait and one immediate temporary Freeze.");
        Require(fixture.GetProperty("RollingDecisions").GetArrayLength() == 18 &&
            fixture.GetProperty("NativeWon").ValueKind == FixtureKind.Null,
            "The Permafrost copy observation is an incomplete native battle.");
        Console.WriteLine("RANDOM-CAMPAIGN-PERMAFROST-COPY PASS: eighteen native successors and RNG, copied permanent trait and immediate Freeze; no full battle acceptance claimed.");
    }

    internal static void MerchantPermafrost(FixtureValue before, FixtureValue source)
    {
        const string upgradeId = "e0af4fe5-75c0-4017-ae5e-5a87099aa6c5";
        const string cardId = "479ceb1b-fc8c-420f-8fa0-17683cee4511";
        BattleTurnState observed = before.GetProperty("Root").Deserialize<BattleTurnState>()!;
        BattleTurnState current = source.GetProperty("RollingDecisions")[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        JsonNode projected = JsonNode.Parse(JsonSerializer.Serialize(observed, ModelJson.Options))!;
        int upgrades = 0;
        void Project(JsonNode? node)
        {
            if (node is JsonObject row)
            {
                string? id = row["DataId"]?.GetValue<string>();
                if (id == upgradeId && row["AssetKey"]?.GetValue<string>() == "TraitPermafrost" && row["ExternalInteractions"] is JsonArray errors)
                {
                    Require(errors.Count == 1 && errors[0]!.GetValue<string>() == "Upgrade traits",
                        "Before-fix shop upgrade must retain the exact original capture rejection.");
                    row["ExternalInteractions"] = new JsonArray(); upgrades++;
                }
                foreach (var child in row.ToArray()) Project(child.Value);
            }
            else if (node is JsonArray list) foreach (JsonNode? child in list) Project(child);
        }
        Project(projected);
        Require(upgrades > 0 && observed.Spawn.Train.Context!.CardInstances!
            .SelectMany(card => card.Permanent.Upgrades).Count(upgrade => upgrade.DataId == upgradeId) == 1,
            "Before-fix observation must retain one real shop upgrade and its captured snapshot references.");
        BattleTurnState expected = projected.Deserialize<BattleTurnState>(ModelJson.Options)!;
        string expectedKey = BattleTurnChecks.Comparable(expected), currentKey = BattleTurnChecks.Comparable(current);
        Require(expectedKey == currentKey, "Shop fix changed more than the upgrade capture guard: " + ModelJson.Difference(expectedKey, currentKey));
        int frozen = 0, retained = 0, thawed = 0;
        foreach (FixtureValue decision in source.GetProperty("RollingDecisions").EnumerateArray())
        {
            BattleTurnState root = decision.GetProperty("Root").Deserialize<BattleTurnState>()!;
            BattleTurnState after = decision.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatContext context = root.Spawn.Train.Context!;
            foreach (CardToken token in context.Cards.Hand)
            {
                CardInstanceState? card = context.FindCard(token.InstanceId);
                if (card?.DataId != cardId || !card.Permanent.Upgrades.Any(upgrade => upgrade.DataId == upgradeId)) continue;
                Require(CardBranchMaskModel.Resolve(card).Traits.Count(trait => trait == "CardTraitFreeze") == 1 &&
                    card.Permanent.Upgrades.Where(upgrade => upgrade.DataId == upgradeId).All(upgrade => upgrade.ExternalInteractions.Count == 0),
                    "Shop Permafrost draw must install exactly one Freeze and clear its old capture rejection.");
                frozen++;
                CombatContext next = after.Spawn.Train.Context!;
                if (decision.GetProperty("IsEndTurn").GetBoolean() && decision.GetProperty("Outcome").Deserialize<RoomOutcome>() is not
                    (RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated))
                {
                    Require(next.Cards.Hand.Any(value => value.InstanceId == card.InstanceId), "Unplayed shop Permafrost card was discarded.");
                    retained++;
                }
                else if (!decision.GetProperty("IsEndTurn").GetBoolean() &&
                    decision.GetProperty("Action").Deserialize<PlayCardAction>()!.CardInstanceId == card.InstanceId)
                {
                    CardInstanceState played = next.FindCard(card.InstanceId)!;
                    Require(!CardBranchMaskModel.Resolve(played).Traits.Contains("CardTraitFreeze"), "Playing shop Permafrost must thaw its temporary Freeze.");
                    thawed++;
                }
            }
        }
        Require(frozen > 0, "The accepted player route must actually draw its shop Permafrost card.");
        Console.WriteLine($"MERCHANT-PERMAFROST PASS: identical native checkpoint/RNG, only the upgrade capture guard removed; frozen hand observations={frozen}, retained turns={retained}, played/thawed={thawed}.");
    }

    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }

    internal static void Input(FixtureValue fixture, int seed, int rank, int distance, string primary, string secondary,
        int timeLimitMs = 3000, bool validateSearchSettings = true)
    {
        Require(seed > 0 && rank is >= 0 and <= 10 && distance is >= 0 and <= 7 && primary != secondary && timeLimitMs is >= 100 and <= 30000,
            "Invalid declared random campaign configuration.");
        FixtureValue input = fixture.GetProperty("NativeRunInput");
        Require(input.GetProperty("AscensionLevel").GetInt32() == rank && input.GetProperty("StartingLevel").GetInt32() == rank &&
            input.GetProperty("PrimaryClan").GetString() == primary && input.GetProperty("SecondaryClan").GetString() == secondary &&
            !string.IsNullOrEmpty(input.GetProperty("PrimaryClanId").GetString()) &&
            input.GetProperty("PrimaryClanId").GetString() != input.GetProperty("SecondaryClanId").GetString(),
            "Random campaign changed its selected native clans or difficulty.");
        Require(fixture.GetProperty("StartingCovenantControl").ValueKind == FixtureKind.Null &&
            fixture.GetProperty("CovenantEnemyStatsControl").ValueKind == FixtureKind.Null &&
            fixture.GetProperty("OriginalOuterBossRoute").ValueKind == FixtureKind.Null,
            "Random campaign must preserve natural encounters and startup without authored battle controls.");
        BattleTurnState initial = fixture.GetProperty("RollingDecisions")[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        int pyre = initial.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health;
        NativeCampaignInputChecks.Native(fixture, distance, seed, pyre, input.GetProperty("Scenario").GetString()!);
        FixtureValue settings = fixture.GetProperty("PlayerAdvisor").GetProperty("Overlay");
        Require(!validateSearchSettings || settings.GetProperty("Horizon").GetInt32() == 3 && settings.GetProperty("WholeBattle").GetBoolean() &&
            settings.GetProperty("MaxSteps").GetInt32() == 256 && settings.GetProperty("SearchWorkers").GetInt32() == 2 &&
            settings.GetProperty("TimeLimitMs").GetInt32() == timeLimitMs &&
            settings.GetProperty("PreferredDeploymentRoom").GetInt32() == 2 && !settings.GetProperty("TacticalSeed").GetBoolean(),
            "Random campaign changed whole-battle/two-worker settings or its declared search budget.");
    }

    internal static void Source(FixtureValue fixture, int seed, int rank, int distance, string primary, string secondary, int timeLimitMs = 3000)
    {
        Input(fixture, seed, rank, distance, primary, secondary, timeLimitMs);
        RollingAdviceChecks.Native(fixture);
        BattleAdviceChecks.Native(fixture, requireBoss: false);
        TrainCombatChecks.Native(fixture);
        JuiceTraitChecks.Native(fixture);
        Console.WriteLine($"RANDOM-CAMPAIGN-SOURCE PASS {primary}/{secondary} seed={seed} rank={rank} ring={distance + 1} budgetMs={timeLimitMs} won={fixture.GetProperty("NativeWon").GetBoolean()}.");
    }

    internal static void RefinedPair(FixtureValue source, FixtureValue replay, int seed = 770862752, int rank = 0,
        int distance = 4, string primary = "Hellhorned", string secondary = "Wurmkin", int inputCount = 50)
    {
        Require(source.GetProperty("NativeRunInput").ContentEquals(replay.GetProperty("NativeRunInput")),
            "Refined route changed the native campaign input.");
        foreach (FixtureValue fixture in new[] { source, replay })
        {
            // The overlay budget is captured metadata only: these processes execute
            // an offline-selected policy and must not count as default-search wins.
            int metadataBudget = fixture.GetProperty("PlayerAdvisor").GetProperty("Overlay").GetProperty("TimeLimitMs").GetInt32();
            Input(fixture, seed, rank, distance, primary, secondary, metadataBudget, validateSearchSettings: false);
            if (rank > 0)
                foreach (FixtureValue row in fixture.GetProperty("RollingDecisions").EnumerateArray())
                foreach (string field in new[] { "Root", "Actual" })
                {
                    CombatRelicState[] owners = row.GetProperty(field).Deserialize<BattleTurnState>()!.Spawn.Train.Context!
                        .Relics!.Where(owner => owner.IsCovenant == true).ToArray();
                    Require(owners.Length == rank && TrueFinalBossCovenantModel.CalibratedOrdinaryWaveOwners(owners) &&
                        owners.Select((owner, index) => owner.AssetKey!.StartsWith($"Covenant{index + 1:D2}_", StringComparison.Ordinal)).All(match => match),
                        "Refined natural campaign changed its original cumulative Covenant owners.");
                }
            Require(inputCount > 0 && fixture.GetProperty("NativeWon").GetBoolean() &&
                fixture.GetProperty("RollingDecisions").GetArrayLength() == inputCount &&
                fixture.GetProperty("PlayerAdvisor").GetProperty("Overlay").GetProperty("Published").GetInt32() == 0,
                "Refined native wins must preserve frozen inputs and publish no new advice.");
            TrainCombatChecks.Native(fixture);
        }
        RollingAdviceChecks.FrozenPair(source, replay, sourceFrozen: true);
        Console.WriteLine($"RANDOM-CAMPAIGN-REFINED-PAIR PASS:{inputCount} unchanged frozen inputs, {primary}/{secondary} seed={seed} rank={rank} ring={distance + 1}, two native wins/full forecasts/state/RNG; offline refined route, not a default search-budget acceptance.");
    }

    internal static void Pair(FixtureValue source, FixtureValue replay, int seed, int rank, int distance, string primary, string secondary, int timeLimitMs = 3000)
    {
        Input(source, seed, rank, distance, primary, secondary, timeLimitMs);
        Input(replay, seed, rank, distance, primary, secondary, timeLimitMs);
        Require(source.GetProperty("NativeWon").GetBoolean() && replay.GetProperty("NativeWon").GetBoolean(),
            "Accepted random campaign pairs require two native victories.");
        Require(source.GetProperty("NativeRunInput").ContentEquals(replay.GetProperty("NativeRunInput")),
            "Independent random campaign replay changed the native campaign input.");
        RollingAdviceChecks.FrozenPair(source, replay);
        BattleAdviceChecks.Native(source, requireBoss: false);
        TrainCombatChecks.Native(source);
        TrainCombatChecks.Native(replay);
        JuiceTraitChecks.Native(source);
        JuiceTraitChecks.Native(replay);
        Require(replay.GetProperty("PlayerAdvisor").GetProperty("Overlay").GetProperty("Published").GetInt32() == 0,
            "Frozen campaign replay must execute the recorded advice without a new search.");
        Console.WriteLine($"RANDOM-CAMPAIGN-PAIR PASS {primary}/{secondary} seed={seed} rank={rank} ring={distance + 1} budgetMs={timeLimitMs}: two native wins, full state/forecast/RNG identity.");
    }
}
