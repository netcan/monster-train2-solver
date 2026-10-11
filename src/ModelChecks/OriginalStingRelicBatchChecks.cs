using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalStingRelicBatchChecks
{
    private const string StingCardId = "356a00a4-010b-462b-b683-2ef296d076a8";
    private static readonly (string Key, string Id, string UpgradeId, int Damage, bool Piercing)[] Artifacts =
    [
        ("StingBuffPower", "0f6d3edc-d212-4e3a-877f-e9eb2ac78339", "9037e99a-d4cb-43cf-a3dd-e8bf88349be1", 20, false),
        ("StingBuffPiercing", "2d367bde-7616-4804-a6c9-910060ae5fd7", "04128a02-8289-422c-aad8-7a6a80a632ab", 10, true)
    ];

    internal static PlayCardAction? Choose(BattleTurnState source)
    {
        PlayCardAction? ordinary = BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        if (ordinary != null)
        {
            CardInstanceState? selected = source.Spawn.Train.Context!.FindCard(ordinary.CardInstanceId);
            CardPlayRule? selectedRule = selected == null ? null : source.PlayRules!.Cards.FirstOrDefault(rule => rule.DataId == selected.DataId);
            if (selectedRule?.Effect is "SpawnMonster" or "Equipment") return ordinary;
        }
        return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play =>
            source.Spawn.Train.Context!.FindCard(play.CardInstanceId)?.DataId == StingCardId) ?? ordinary;
    }

    internal static void Native(FixtureValue root, bool required = false)
    {
        if (!root.TryGetProperty("ModifierScenario", out var scenario) || scenario.GetString() != "original-sting-relics")
        { Require(!required, "Fixture lacks original Sting relic combat coverage."); return; }
        Require(root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("RelicCardModifiersEnabled").GetBoolean(), "Original Sting relic capture is incomplete or from another game build.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Original Sting relic battle has nonzero " + field + ".");

        var rows = root.GetProperty("RelicCardModifiers").EnumerateArray().Select(row => (
            Before: row.GetProperty("Before").Deserialize<CardInstanceState>()!,
            Context: row.GetProperty("ContextBefore").Deserialize<CombatContext>()!,
            AfterContext: row.GetProperty("ContextAfter").Deserialize<CombatContext>()!,
            Relics: row.GetProperty("Relics").Deserialize<CombatRelicState[]>()!,
            Reset: row.GetProperty("ResetTemporary").GetBoolean(),
            Actual: row.GetProperty("Actual").Deserialize<CardInstanceState>()!,
            Dispatches: row.GetProperty("Dispatches").Deserialize<RelicCardModifierDispatch[]>()!,
            Notifications: row.GetProperty("Notifications").EnumerateArray().Select(item =>
                (item.GetProperty("RelicIndex").GetInt32(), item.GetProperty("EffectIndex").GetInt32())).ToArray())).ToArray();
        Require(rows.Length > 0 && rows.Any(row => row.Reset && row.Dispatches.Any(effect => effect.UpgradeAdded)) &&
            rows.Any(row => row.Dispatches.Any(effect => effect.Returned && !effect.UpgradeAdded)) &&
            rows.Any(row => row.Dispatches.Any(effect => !effect.Returned && effect.Filters.Any(filter => !filter.Accepted))),
            "Original Sting relic manager capture lacks reset, unique-repeat or failed-filter coverage.");

        string ParentSnapshot() => string.Join("\n", rows.Select(row => Serialize(row.Before) + Serialize(row.Context) + Serialize(row.Relics)));
        string parent = ParentSnapshot();
        void CheckModifiers()
        {
            foreach (var row in rows)
            {
                CheckIdentities(row.Relics);
                var predicted = RelicCardModifierModel.Apply(row.Before, row.Relics, row.Reset, row.Context);
                Require(predicted.Supported, "Original Sting relic manager call is unsupported: " + predicted.UnsupportedReason);
                Equal(predicted.Card, row.Actual, "original Sting relic complete card state");
                Equal(predicted.Dispatches, row.Dispatches, "original Sting relic ordered filter dispatch");
                Equal(predicted.Context!.Relics, row.AfterContext.Relics, "original Sting relic manager context");
                Equal(predicted.Context.Statistics, row.AfterContext.Statistics, "original Sting relic statistic cache");
                Require(predicted.Dispatches.Where(effect => effect.Returned).Select(effect => (effect.RelicIndex, effect.EffectIndex))
                    .SequenceEqual(row.Notifications), "Original Sting relic notification order differs.");
            }
        }
        CheckModifiers();
        Parallel.For(0, 32, _ => CheckModifiers());
        Require(ParentSnapshot() == parent, "Original Sting relic branches mutated their native inputs.");

        int playedStings = 0;
        foreach (FixtureValue entry in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardInstanceState? card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId);
            if (card?.DataId != StingCardId) continue;
            var upgrades = card.Temporary.Upgrades;
            CardUpgradeModifier power = upgrades.Single(upgrade => upgrade.AssetKey == "StingBuffPower");
            CardUpgradeModifier piercing = upgrades.Single(upgrade => upgrade.AssetKey == "StingBuffPiercing");
            Require(power.Stats.Damage == 20 && piercing.Stats.Damage == 10 &&
                CardBranchMaskModel.Resolve(card).Traits.Contains("CardTraitIgnoreArmor", StringComparer.Ordinal),
                "Played Sting did not retain both exact original relic upgrades and its piercing trait.");
            CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == StingCardId);
            CardPlayRule resolved = CardModifierModel.Resolve(rule, card);
            int rawDamage = rule.Effects.Single(effect => effect.Type == "Damage").Value;
            int resolvedDamage = resolved.Effects.Single(effect => effect.Type == "Damage").Value;
            Require(resolvedDamage == rawDamage + 30,
                "The played Sting's original relic upgrades did not add 30 damage to its native effect.");
            BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CardInstanceState? played = actual.Spawn.Train.Context!.FindCard(action.CardInstanceId);
            Require(played?.Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == "StingBuffPower") == true &&
                played.Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == "StingBuffPiercing"),
                "The native played Sting lost an original relic upgrade during action resolution.");
            playedStings++;
        }
        Require(playedStings > 0, "The full battle never played the relic-upgraded native Sting.");
        Console.WriteLine($"NATIVE-ORIGINAL-STING-RELIC-CHECKS PASS: both unchanged original relic owners, {rows.Length} exact manager calls, {playedStings} Sting plays with +30 damage and IgnoreArmor, 32 immutable branches.");
    }

    private static void CheckIdentities(IReadOnlyList<CombatRelicState> relics)
    {
        foreach (var item in Artifacts)
        {
            CombatRelicState relic = relics.SingleOrDefault(candidate => candidate.AssetKey == item.Key && candidate.DataId == item.Id)!;
            Require(relic != null && relic.EffectTypes.SequenceEqual(["RelicEffectAddTempUpgrade", "RelicEffectAddCardsNull"]) && relic.CardModifiers?.Count == 1,
                "Original Sting artifact is missing or has changed: " + item.Key);
            RelicCardModifier modifier = relic.CardModifiers![0];
            CardLifecycleUpgrade upgrade = modifier.Rule.Upgrade ?? throw new InvalidDataException("Original Sting artifact lacks its upgrade: " + item.Key);
            Require(upgrade.Values.DataId == item.UpgradeId && upgrade.AssetKey == item.Key &&
                upgrade.Values.Stats.Damage == item.Damage && upgrade.Unique &&
                modifier.Rule.Filters.Count == 1 && modifier.Rule.Filters[0].AssetKey == "OnlyStings" &&
                (item.Piercing ? upgrade.AddedTraits.Any(trait => trait.RuntimeType == "CardTraitIgnoreArmor") :
                    upgrade.AddedTraits.Count == 0),
                "Original Sting artifact payload or OnlyStings filter differs: " + item.Key);
        }
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal<T>(T expected, T actual, string label)
    {
        string? difference = ModelJson.Difference(Serialize(expected), Serialize(actual));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}
