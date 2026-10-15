using System;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicUpgradeCountChecks
{
    internal const string ExcludedSpellId = "10f64bfb-81cd-4b7c-bf46-6fc1fa832008";
    private static CardUpgradeMaskRule SpellFilter() => new("GiveSpellStatsForUpgrades_DH_CardFilter",
        rarities: new(requiredOperator: 1), disallowedPools: [[ExcludedSpellId]]);
    internal static CombatRelicState[] Relics() => [
        new("4200307a-616d-4cbb-a54e-527c5dc0f492", "GiveSpellStatsForUpgrades_DH", [RelicModel.ModifyCardAttack],
            isCovenant: false, disallowedInPlacementPhase: false,
            cardAttackModifiers: [Attack(0, "Spell")]),
        new("fc1f6104-b3c1-4714-ac0f-9c76a6fb1a77", "GiveUnitStatsForUpgrades_DH",
            [RelicModel.ModifyCardHealth, RelicModel.ModifyCardAttack],
            isCovenant: false, disallowedInPlacementPhase: false,
            cardHealthModifiers: [new(0, "", "", 3, 0, "Monster", true)],
            cardAttackModifiers: [Attack(1, "Monster")])];

    private static RelicCardAttackModifier Attack(int index, string type) =>
        new(index, "", "", 3, 0, 0, type, true, false, false, false, type == "Spell" ? SpellFilter() : null);

    internal static void Run()
    {
        CardUpgradeModifier Upgrade(string id, bool icon = true, bool hide = false, bool region = false) =>
            new(id, id, new(), [], false, false, false, 0, 0, [],
                maskMetadata: new([], icon, hide, region, false));
        CardInstanceState Card(string type, CardUpgradeModifier[] permanent, CardUpgradeModifier[] temporary, string id = "carrier") =>
            new(1, id, new(new(), permanent, 0, []), new(new(), temporary, 0, []), 0, 0, 0, [],
                maskDescriptor: new(new(id, type, "Common", true, false, false, [], [], [], [],
                    null, false, 1, 0, false, false, false), 0, [], []));
        CombatRelicState[] relics = Relics();
        foreach (string type in new[] { "Monster", "Spell", "Blight" })
        foreach (int count in new[] { 0, 1, 2 })
        {
            // Region-run upgrades with visible icons count here, even though
            // CardUpgradeMask excludes them from its separate upgrade count.
            CardUpgradeModifier[] upgrades = Enumerable.Range(0, count)
                .Select(index => Upgrade("visible-" + index, region: index == 1))
                .Concat([Upgrade("hidden", hide: true), Upgrade("no-icon", icon: false)]).ToArray();
            CardInstanceState before = Card(type, upgrades, [Upgrade("temporary-visible")]);
            RelicCardModifierResult result = RelicCardModifierModel.Apply(before, relics, resetTemporary: false);
            int attack = type is "Monster" or "Spell" ? count * 3 : 0;
            int health = type == "Monster" ? count * 3 : 0;
            Require(result.Supported && result.Card!.Temporary.Offsets.Damage == attack &&
                result.Card.Temporary.Offsets.Health == health &&
                result.Dispatches.Count(item => item.Returned) == (count == 0 ? 0 : type == "Monster" ? 2 : type == "Spell" ? 1 : 0),
                "Permanent visible upgrade scaling differs for " + type + "/" + count + ": " + result.UnsupportedReason);
            RelicCardModifierResult reset = RelicCardModifierModel.Apply(result.Card!, relics);
            RelicCardModifierResult repeat = RelicCardModifierModel.Apply(reset.Card!, relics);
            Require(reset.Supported && repeat.Supported && repeat.Card!.Temporary.Offsets.Damage == attack &&
                repeat.Card.Temporary.Offsets.Health == health && before.Temporary.Offsets.Damage == 0 &&
                before.Temporary.Upgrades.Count == 1, "Reset accumulated scaling or mutated the sibling input.");
            RelicCardModifierResult ablated = RelicCardModifierModel.Apply(before, []);
            Require(ablated.Supported && ablated.Card!.Temporary.Offsets.Damage == 0 &&
                ablated.Card.Temporary.Offsets.Health == 0, "Relic ablation retained the scaling bonus.");
        }

        var excluded = RelicCardModifierModel.Apply(Card("Spell", [Upgrade("visible")], [], ExcludedSpellId), relics);
        Require(excluded.Supported && excluded.Card!.Temporary.Offsets.Damage == 0 &&
            excluded.Dispatches[0].Filters.Single().Accepted == false && !excluded.Dispatches[0].Returned,
            "Original excluded spell pool was ignored.");

        CardInstanceState sourceCard = Card("Spell", [Upgrade("source-visible")], []);
        var rng = UnityRng.Seed(321);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 2, 10,
            relics: relics, cardRegistry: [sourceCard]);
        CombatUnit born = new(1, "token", CombatTeam.Player, 4, 20, 20, true, false, false,
            [new("cardless", 1)], size: 1, modifiers: new(4, 0, 0, 1, 0, true, false, []));
        foreach (int fromCardId in new[] { 0, 1 })
        {
            RoomCombatResult birth = RelicSpawnStatusModel.CharacterAdded(new(0, false, [born], [], context),
                1, fromCardId, onlyCovenants: false);
            // Native cardless birth ignores the relic's card-type filter. Both
            // attack effects use the source card's count; health has no birth hook.
            Require(birth.Supported && birth.State!.Units.Single().Attack == (fromCardId == 0 ? 4 : 10) &&
                birth.State.Units.Single().MaxHealth == 20, "Cardless source upgrade scaling differs: " + birth.UnsupportedReason);
        }
        Console.WriteLine("RELIC-UPGRADE-COUNT-CHECKS PASS: permanent visible icons, region/hidden/no-icon/temporary boundaries, zero-return, reset, ablation and cardless source counts.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-upgrade-count-relics" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("RelicCardModifiersEnabled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0), "Upgrade-count relic battle is not strictly accepted.");
        var rows = root.GetProperty("RelicCardModifiers").EnumerateArray().Select(row => (
            Before: row.GetProperty("Before").Deserialize<CardInstanceState>()!,
            Context: row.GetProperty("ContextBefore").Deserialize<CombatContext>()!,
            AfterContext: row.GetProperty("ContextAfter").Deserialize<CombatContext>()!,
            Relics: row.GetProperty("Relics").Deserialize<CombatRelicState[]>()!,
            Reset: row.GetProperty("ResetTemporary").GetBoolean(),
            Actual: row.GetProperty("Actual").Deserialize<CardInstanceState>()!,
            Dispatches: row.GetProperty("Dispatches").Deserialize<RelicCardModifierDispatch[]>()!)).ToArray();
        Require(rows.Length >= 38, "Missing native upgrade-count manager observations.");
        foreach (var row in rows)
        {
            Equal(row.Relics.Where(item => Relics().Any(expected => item.DataId == expected.DataId)).ToArray(),
                Relics(), "original owner payload/order");
            string frozen = JsonSerializer.Serialize(row.Before);
            var predicted = RelicCardModifierModel.Apply(row.Before, row.Relics, row.Reset, row.Context);
            Require(predicted.Supported, "Native upgrade-count refresh is unsupported: " + predicted.UnsupportedReason);
            Equal(predicted.Card, row.Actual, "manager card state");
            Equal(predicted.Dispatches, row.Dispatches, "manager dispatch order and return values");
            Equal(predicted.Context!.Relics, row.AfterContext.Relics, "manager relic state");
            Equal(predicted.Context.Statistics, row.AfterContext.Statistics, "manager statistics");
            Require(frozen == JsonSerializer.Serialize(row.Before), "Manager replay mutated its source.");
        }
        foreach (string type in new[] { "Monster", "Spell", "Blight" })
        foreach (int count in new[] { 0, 1, 2 })
        {
            var group = rows.Where(row => row.Before.DataId != ExcludedSpellId && row.Before.MaskDescriptor!.Definition.CardType == type &&
                row.Before.Permanent.Upgrades.Any(upgrade => upgrade.AssetKey.StartsWith("PojuUpgradeCount", StringComparison.Ordinal)) &&
                row.Before.Permanent.Upgrades.Count(upgrade => upgrade.MaskMetadata!.HasIcon && !upgrade.MaskMetadata.HideIcon) == count).ToArray();
            Require(group.Any(row => !row.Reset && row.Before.Temporary.Upgrades.Any(upgrade =>
                    upgrade.MaskMetadata!.HasIcon && !upgrade.MaskMetadata.HideIcon)) && group.Count(row => row.Reset) >= 2,
                "Missing count/reset/temporary upgrade witnesses for " + type + "/" + count);
            foreach (var row in group.Where(row => row.Reset))
            {
                Require(row.Actual.Temporary.Offsets.Damage == (type is "Monster" or "Spell" ? count * 3 : 0) &&
                    row.Actual.Temporary.Offsets.Health == (type == "Monster" ? count * 3 : 0),
                    "Native upgrade-count offsets differ for " + type + "/" + count);
                foreach (CombatRelicState owner in Relics())
                {
                    var ablated = RelicCardModifierModel.Apply(row.Before,
                        row.Relics.Where(relic => relic.DataId != owner.DataId).ToArray(), context: row.Context);
                    Require(ablated.Supported, "Owner ablation is unsupported.");
                    bool relevant = count > 0 && (type == "Spell" ? owner.AssetKey == "GiveSpellStatsForUpgrades_DH" :
                        type == "Monster" && owner.AssetKey == "GiveUnitStatsForUpgrades_DH");
                    Require((JsonSerializer.Serialize(ablated.Card) != JsonSerializer.Serialize(row.Actual)) == relevant,
                        "Owner ablation did not isolate the native effect for " + type + "/" + count);
                }
            }
        }
        Require(rows.Any(row => row.Before.Permanent.Upgrades.Any(upgrade =>
            upgrade.MaskMetadata is { HasIcon: true, HideIcon: false, RegionRun: true })),
            "Native visible region-upgrade inclusion was not exercised.");
        Require(rows.Any(row => row.Before.DataId == ExcludedSpellId && row.Reset &&
            row.Before.Permanent.Upgrades.Count(upgrade => upgrade.MaskMetadata!.HasIcon && !upgrade.MaskMetadata.HideIcon) == 2 &&
            row.Actual.Temporary.Offsets.Damage == 0 && !row.Dispatches[0].Returned &&
            row.Dispatches[0].Filters.Single().Accepted == false), "Original excluded spell pool lacks a native rejection witness.");
        var played = new System.Collections.Generic.HashSet<string>();
        int plays = 0;
        int preludes = 0;
        foreach (FixtureValue entry in root.GetProperty("Actions").EnumerateArray())
        {
            var before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
            if (!card.Permanent.Upgrades.Any(upgrade => upgrade.AssetKey == "PojuUpgradeCount0")) continue;
            if (entry.GetProperty("ScenarioAction").GetBoolean()) preludes++;
            Require(card.Temporary.Offsets.Damage == 6 && card.Temporary.Offsets.Health ==
                (card.MaskDescriptor!.Definition.CardType == "Monster" ? 6 : 0), "Played original card lost its relic scaling.");
            var replay = BattleActionModel.PlayCard(before, action);
            Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(actual),
                "Upgraded original card play differs from native: " + replay.Reason);
            played.Add(card.MaskDescriptor!.Definition.CardType);
            plays++;
        }
        Require(preludes == 2 && played.SetEquals(["Monster", "Spell"]),
            "Both upgraded original card types must be played; only their initial plays are scenario preludes.");
        Console.WriteLine($"ORIGINAL-UPGRADE-COUNT-RELICS-NATIVE PASS: {rows.Length} exact manager refreshes; 0/1/2 visible permanent upgrades, controls, reset/repeat, owner ablations and {plays} plays of both original card types.");
    }

    private static void Equal<T>(T left, T right, string label)
    { Require(JsonSerializer.Serialize(left) == JsonSerializer.Serialize(right), "Native upgrade-count " + label + " differs."); }
}
