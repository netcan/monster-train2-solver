using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicCardTypeCostOrderChecks
{
    private const string CostRelicId = "a19cac7c-b2e0-42bd-bc1e-d180e17a1c15";
    private const string MarkerRelicId = "fc119000-0000-4000-8000-000000000190";
    private const string MarkerUpgradeId = "fc119000-0000-4000-8000-000000000191";
    private const string BlightId = "21038774-c0a4-413c-ab7e-27e458228e5f";
    private const string JunkId = "62cc5532-5fa0-4e66-9fec-85be573af359";
    private const string MarkerMask = "PojuZeroCostBlightMask";

    internal static void Native(FixtureValue root)
    {
        string? scenario = root.GetProperty("ModifierScenario").GetString();
        Require(scenario is "card-type-cost-discount-first" or "card-type-cost-upgrade-first",
            "Unexpected card-type cost order scenario.");
        bool upgradeFirst = scenario == "card-type-cost-upgrade-first";
        Require(root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("RelicCardModifiersEnabled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0),
            "Card-type cost order battle is not strictly accepted.");
        FixtureValue[] records = root.GetProperty("RelicCardModifiers").EnumerateArray().ToArray();
        Require(records.Length >= 10 && records.All(row => row.GetProperty("Difference").ValueKind == FixtureKind.Null),
            "Card-type cost order manager evidence is missing or differs.");
        var rows = records.Select(row => new
        {
            Before = row.GetProperty("Before").Deserialize<CardInstanceState>()!,
            Context = row.GetProperty("ContextBefore").Deserialize<CombatContext>()!,
            AfterContext = row.GetProperty("ContextAfter").Deserialize<CombatContext>()!,
            Relics = row.GetProperty("Relics").Deserialize<CombatRelicState[]>()!,
            Reset = row.GetProperty("ResetTemporary").GetBoolean(),
            Actual = row.GetProperty("Actual").Deserialize<CardInstanceState>()!,
            Dispatches = row.GetProperty("Dispatches").Deserialize<RelicCardModifierDispatch[]>()!,
            Notifications = row.GetProperty("Notifications").EnumerateArray().Select(item => new
            {
                RelicIndex = item.GetProperty("RelicIndex").GetInt32(),
                EffectIndex = item.GetProperty("EffectIndex").GetInt32()
            }).ToArray()
        }).ToArray();

        foreach (var row in rows)
        {
            int costIndex = Array.FindIndex(row.Relics, relic => relic.DataId == CostRelicId);
            int markerIndex = Array.FindIndex(row.Relics, relic => relic.DataId == MarkerRelicId);
            CheckIdentities(row.Relics);
            Require(upgradeFirst ? markerIndex < costIndex : costIndex < markerIndex,
                "The acquired relic order does not match the selected scenario.");
            var expectedOrder = upgradeFirst
                ? new[] { (markerIndex, 0), (costIndex, 0), (costIndex, 1) }
                : new[] { (costIndex, 0), (costIndex, 1), (markerIndex, 0) };
            Require(row.Dispatches.Select(item => (item.RelicIndex, item.EffectIndex)).SequenceEqual(expectedOrder),
                "The native effect dispatch order does not follow the acquired relic order.");
            RelicCardModifierDispatch marker = row.Dispatches.Single(item => item.RelicIndex == markerIndex);
            bool expectedMarker = row.Before.DataId == BlightId && (!upgradeFirst ||
                !row.Reset && CardBranchMaskModel.Resolve(row.Before).CostWithoutTraits == 0);
            Require(marker.Returned == expectedMarker && marker.UpgradeAdded == expectedMarker && marker.Filters.Count == 1 &&
                marker.Filters[0].AssetKey == MarkerMask && marker.Filters[0].Accepted == expectedMarker &&
                MarkerCount(row.Actual) == (row.Reset ? 0 : MarkerCount(row.Before)) + (expectedMarker ? 1 : 0),
                "Native zero-cost filter, upgrade insertion or repeat/reset marker count differs from the required witness.");
            foreach (CardUpgradeModifier upgrade in row.Actual.Temporary.Upgrades.Where(item => item.DataId == MarkerUpgradeId))
                CheckMarkerUpgrade(upgrade);
            if (row.Before.DataId != BlightId)
                Require(MarkerCount(row.Actual) == 0, "The Blight-only marker reached Junk or a nonmatching control card.");
        }

        string parent = Serialize(rows);
        void CheckModifiers()
        {
            foreach (var row in rows)
            {
                RelicCardModifierResult predicted = RelicCardModifierModel.Apply(row.Before, row.Relics, row.Reset, row.Context);
                Require(predicted.Supported, "Native card-type cost ordering is unsupported: " + predicted.UnsupportedReason);
                Equal(predicted.Card, row.Actual, "complete manager card state");
                Equal(predicted.Dispatches, row.Dispatches, "ordered dispatches, filters and insertion flags");
                Equal(predicted.Context!.WithCard(predicted.Card!), row.AfterContext, "complete manager context");
                Require(predicted.Dispatches.Where(item => item.Returned).Select(item => (item.RelicIndex, item.EffectIndex))
                    .SequenceEqual(row.Notifications.Select(item => (item.RelicIndex, item.EffectIndex))),
                    "Native manager trigger notifications differ in membership or order.");
            }
        }
        CheckModifiers();
        Parallel.For(0, 16, _ => CheckModifiers());
        Require(Serialize(rows) == parent, "Parallel manager replay mutated captured cards, contexts or relic definitions.");

        int resetMarkerCount = upgradeFirst ? 0 : 1;
        Require(rows.Any(row => row.Before.DataId == BlightId && !row.Reset && row.Before.Temporary.Offsets.Cost == -99 &&
                MarkerCount(row.Before) == resetMarkerCount && MarkerCount(row.Actual) == resetMarkerCount + 1 &&
                row.Actual.Temporary.Offsets.Cost == -198 && row.Actual.Temporary.Offsets.XCost == 198) &&
            rows.Any(row => row.Before.DataId == BlightId && row.Reset && row.Before.Temporary.Offsets.Cost == -198 &&
                MarkerCount(row.Before) == resetMarkerCount + 1 && MarkerCount(row.Actual) == resetMarkerCount &&
                row.Actual.Temporary.Offsets.Cost == -99 && row.Actual.Temporary.Offsets.XCost == 99) &&
            rows.Any(row => row.Before.DataId == JunkId && !row.Reset && row.Before.Temporary.Offsets.Cost == -99 &&
                row.Actual.Temporary.Offsets.Cost == -198 && row.Actual.Temporary.Offsets.XCost == 198) &&
            rows.Any(row => row.Before.DataId == JunkId && row.Reset && row.Actual.Temporary.Offsets.Cost == -99) &&
            rows.Any(row => row.Before.DataId != BlightId && row.Before.DataId != JunkId &&
                row.Dispatches.All(item => !item.Returned) && row.Actual.Temporary.Offsets.Cost == 0),
            "Native order controls lack the required repeat, reset, Junk or nonmatching-card witness.");

        var actions = root.GetProperty("Actions").EnumerateArray().Take(2).Select(entry => new
        {
            Scenario = entry.GetProperty("ScenarioAction").GetBoolean(),
            Before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!,
            Actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!,
            Action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!
        }).ToArray();
        Require(actions.Length == 2, "The original Blight/Junk play witnesses are missing.");
        for (int index = 0; index < actions.Length; index++)
        {
            var entry = actions[index];
            CardInstanceState card = entry.Before.Spawn.Train.Context!.FindCard(entry.Action.CardInstanceId)!;
            Require(entry.Scenario && card.DataId == (index == 0 ? BlightId : JunkId) &&
                MarkerCount(card) == (index == 0 ? resetMarkerCount : 0) &&
                card.Temporary.Offsets.Cost == -99 && card.Temporary.Offsets.XCost == 99 &&
                entry.Before.Energy == entry.Actual.Energy && entry.Actual.Spawn.Train.Context!.OtherPiles!
                    .Single(pile => pile.Name == "Purged").Cards.Any(item => item.InstanceId == entry.Action.CardInstanceId),
                "An original card did not retain the expected marker, play free and self-purge.");
        }
        string actionParent = Serialize(actions);
        void CheckActions()
        {
            foreach (var entry in actions)
            {
                BattleActionResult replay = BattleActionModel.PlayCard(entry.Before, entry.Action);
                Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(entry.Actual),
                    "Original card action differs from native state under ordered relic modifiers: " + replay.Reason);
            }
        }
        CheckActions();
        Parallel.For(0, 16, _ => CheckActions());
        Require(Serialize(actions) == actionParent, "Parallel original-card plays mutated their parent states.");
        int initialJunk = actions[1].Action.CardInstanceId;
        Require(rows.Any(row => row.Before.DataId == JunkId && row.Before.InstanceId != initialJunk && row.Reset &&
            row.Before.Temporary.Offsets.Cost == 0 && row.Actual.Temporary.Offsets.Cost == -99 &&
            !actions[0].Before.Spawn.Train.Context!.CardRegistry!.Any(card => card.InstanceId == row.Before.InstanceId)),
            "A distinct later generated Junk instance did not receive the original discount.");
        Console.WriteLine("CARD-TYPE-COST-ORDER-NATIVE PASS: " + scenario + ", " + rows.Length +
            " exact manager refreshes, 16 immutable parallel branches, free original plays and generated Junk.");
    }

    private static void CheckIdentities(IReadOnlyList<CombatRelicState> relics)
    {
        CombatRelicState original = relics.Single(item => item.DataId == CostRelicId);
        Require(original.AssetKey == "CheaperBlightScourge" &&
            original.EffectTypes.SequenceEqual(["RelicEffectModifyCardTypeCost", "RelicEffectModifyCardTypeCost"]) &&
            original.CardTypeCosts?.Select(item => (item.EffectIndex, item.CardType, item.Amount,
                item.IgnoreRestrictions, item.RequiredSubtype, item.ConditionCount)).SequenceEqual(
                [(0, "Blight", -99, false, null, 0), (1, "Junk", -99, false, null, 0)]) == true,
            "The original discount relic identity or payload changed.");
        CombatRelicState marker = relics.Single(item => item.DataId == MarkerRelicId);
        Require(marker.AssetKey == "PojuZeroCostBlightUpgrade" && marker.EffectTypes.SequenceEqual(["RelicEffectAddTempUpgrade"]) &&
            marker.CardModifiers?.Count == 1, "The authored marker relic identity or effect changed.");
        RelicCardModifier effect = marker.CardModifiers!.Single();
        Require(effect.EffectIndex == 0 && !effect.ApplyToCardlessSpawns && effect.ConditionCount == 0 &&
            effect.Conditions.Count == 0 && effect.Rule.SourceMonsters && !effect.Rule.SourceHeroes && effect.Rule.Filters.Count == 1 &&
            effect.Upgrade != null, "The authored marker dispatch parameters changed.");
        CheckMarkerUpgrade(effect.Upgrade!);
        CardUpgradeMaskRule mask = effect.Rule.Filters.Single();
        Require(mask.AssetKey == MarkerMask && mask.CardType == "Blight" && mask.MinCost == 0 && mask.MaxCost == 0 &&
            mask.AdditionalCardTypes.Count == 0 && !mask.RequireXCost && !mask.ExcludeXCost &&
            !mask.ExcludeIfHasAnyUpgrades && !mask.ExcludeIfHasNoUpgrades &&
            mask.Rarities.Required.Count == 0 && mask.Rarities.Excluded.Count == 0 &&
            mask.Traits.Required.Count == 0 && mask.Traits.Excluded.Count == 0 &&
            mask.Upgrades.Required.Count == 0 && mask.Upgrades.Excluded.Count == 0 &&
            mask.AllowedPools.Count == 0 && mask.DisallowedPools.Count == 0,
            "The authored zero-cost Blight mask changed.");
    }

    private static void CheckMarkerUpgrade(CardUpgradeModifier upgrade)
    {
        Require(upgrade.DataId == MarkerUpgradeId && upgrade.AssetKey == "PojuZeroCostBlightDamage" && !upgrade.Unique &&
            !upgrade.RemoveOnDiscard && upgrade.Statuses.Count == 0 && upgrade.ExternalInteractions.Count == 0 &&
            upgrade.Lifecycle != null && upgrade.Lifecycle.AddedTraits.Count == 0 && upgrade.Lifecycle.Triggers.Count == 0,
            "The authored marker upgrade identity or lifecycle changed.");
        Equal(upgrade.Stats, new CardStatModifier(damage: 7), "marker +7 Damage-only payload");
    }

    private static int MarkerCount(CardInstanceState card) => card.Temporary.Upgrades.Count(item => item.DataId == MarkerUpgradeId);
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(Serialize(predicted), Serialize(actual));
        if (difference != null) throw new InvalidOperationException("Card-type cost ordering " + label + " differs: " + difference);
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
