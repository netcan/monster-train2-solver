using System;
using System.Linq;
using MonsterTrain2Poju.Model;

internal static class RelicUpgradeCountChecks
{
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
        new(index, "", "", 3, 0, 0, type, true, false, false, false);

    internal static void Run()
    {
        CardUpgradeModifier Upgrade(string id, bool icon = true, bool hide = false, bool region = false) =>
            new(id, id, new(), [], false, false, false, 0, 0, [],
                maskMetadata: new([], icon, hide, region, false));
        CardInstanceState Card(string type, CardUpgradeModifier[] permanent, CardUpgradeModifier[] temporary) =>
            new(1, "carrier", new(new(), permanent, 0, []), new(new(), temporary, 0, []), 0, 0, 0, [],
                maskDescriptor: new(new("carrier", type, "Common", true, false, false, [], [], [], [],
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
}
