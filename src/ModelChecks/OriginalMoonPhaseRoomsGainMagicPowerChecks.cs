using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalMoonPhaseRoomsGainMagicPowerChecks
{
    private const string Effect = "RelicEffectApplyCardUpgradeToHandEndOfTurnOnMoon";
    private const string UpgradeId = "ba427938-c3ad-480b-aa27-44a6426a059c";
    private const string UpgradeKey = "MoonPhaseRooms_UpgradeData";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 214 &&
            root.GetProperty("OriginalMoonPhaseRoomsGainMagicPowerSchema").GetInt32() == 214 &&
            root.GetProperty("ModifierScenario").GetString() == "original-moon-phase-rooms-magic-power" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "MoonPhaseRoomsGainMagicPower battle is incomplete or from an unexpected capture schema.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(field).GetInt32() == 0, "MoonPhaseRoomsGainMagicPower battle has nonzero " + field + ".");

        FixtureValue evidence = root.GetProperty("OriginalMoonPhaseRoomsGainMagicPower");
        FixtureValue definition = evidence.GetProperty("Definition");
        Require(definition.GetProperty("OwnerId").GetString() == RelicMoonPhaseHandUpgradeModel.OwnerId &&
            definition.GetProperty("AssetKey").GetString() == RelicMoonPhaseHandUpgradeModel.OwnerKey &&
            definition.GetProperty("EffectType").GetString() == Effect &&
            !definition.GetProperty("IsCovenant").GetBoolean() &&
            !definition.GetProperty("DisallowedInPlacementPhase").GetBoolean() &&
            definition.GetProperty("ParamInt2").GetInt32() == 2 &&
            !definition.GetProperty("ParamBool").GetBoolean() &&
            definition.GetProperty("ConditionCount").GetInt32() == 0 &&
            definition.GetProperty("MoonPhaseMask").GetInt32() == 2 &&
            !definition.GetProperty("RemoveOnDiscard").GetBoolean() &&
            !definition.GetProperty("CanApplyInPreviewMode").GetBoolean(),
            "Unchanged MoonPhaseRoomsGainMagicPower owner or native phase/lifetime gates changed.");

        FixtureValue upgrade = definition.GetProperty("Upgrade");
        Require(upgrade.GetProperty("DataId").GetString() == UpgradeId &&
            upgrade.GetProperty("AssetKey").GetString() == UpgradeKey &&
            upgrade.GetProperty("Stats").GetProperty("Damage").GetInt32() == 10 &&
            upgrade.GetProperty("Stats").GetProperty("Heal").GetInt32() == 10 &&
            !upgrade.GetProperty("Unique").GetBoolean() &&
            !upgrade.GetProperty("RemoveOnDiscard").GetBoolean(),
            "Native MoonPhaseRoomsGainMagicPower upgrade payload changed.");
        FixtureValue[] filters = definition.GetProperty("Filters").EnumerateArray().ToArray();
        Require(filters.Length == 2 && filters[0].GetProperty("AssetKey").GetString() == "MagicPowerSpells" &&
            filters[0].GetProperty("CardType").GetString() == "Spell" &&
            filters[1].GetProperty("AssetKey").GetString() == "ExcludedSpellsFromMagicPower" &&
            filters[1].GetProperty("DisallowedPools").GetArrayLength() == 1,
            "Native positive and exclusion mask definitions changed.");

        FixtureValue[] witnesses = evidence.GetProperty("Witnesses").EnumerateArray().ToArray();
        FixtureValue[] callbacks = evidence.GetProperty("Callbacks").EnumerateArray().ToArray();
        Require(witnesses.Length == 10 && callbacks.Length == 5 &&
            callbacks.Select(item => item.GetProperty("Turn").GetInt32()).SequenceEqual(new[] { 1, 2, 3, 4, 5 }),
            "Expected positive and filter-control witnesses at five pre-discard callbacks.");
        Require(witnesses.All(item => !item.GetProperty("DefinitionTraits").EnumerateArray()
                .Select(trait => trait.GetString()).Any(trait =>
                    trait is "CardTraitScalingAddEnergy" or "CardTraitAddCardUpgradeOnMoonPhase")),
            "A synthetic witness introduced an unrelated unsupported card callback.");

        int fullMoonApplications = 0, newMoonPositiveNoOps = 0, filterControls = 0;
        foreach (FixtureValue callback in callbacks)
        {
            string phase = callback.GetProperty("Phase").GetString()!;
            Require(phase is "Full" or "New", "A pre-discard callback has an unmodeled moon phase.");
            foreach (FixtureValue observed in callback.GetProperty("Witnesses").EnumerateArray())
            {
                int instanceId = observed.GetProperty("InstanceId").GetInt32();
                FixtureValue witness = witnesses.Single(item => item.GetProperty("InstanceId").GetInt32() == instanceId);
                bool expectedMatch = witness.GetProperty("ExpectedMatch").GetBoolean();
                bool inHand = observed.GetProperty("InHand").GetBoolean();
                string role = observed.GetProperty("Role").GetString()!;
                Require(observed.GetProperty("DataId").GetString() == witness.GetProperty("DataId").GetString() &&
                    observed.GetProperty("ExpectedMatch").GetBoolean() == expectedMatch &&
                    role == witness.GetProperty("Role").GetString(),
                    "A native callback witness differs from its prepared card identity.");

                CardUpgradeModifier[] temporary = observed.GetProperty("TemporaryUpgrades")
                    .EnumerateArray().Select(item => item.Deserialize<CardUpgradeModifier>()!).ToArray();
                CardUpgradeModifier[] applied = temporary.Where(item => item.DataId == UpgradeId).ToArray();
                bool shouldApply = expectedMatch && phase == "Full" && inHand;
                Require(applied.Length == (shouldApply ? 1 : 0),
                    "The pre-discard native callback applied the moon upgrade on the wrong phase, card or hand state.");
                if (shouldApply)
                {
                    CardUpgradeModifier actual = applied[0];
                    Require(actual.AssetKey == UpgradeKey && actual.Stats.Damage == 10 && actual.Stats.Heal == 10 &&
                        !actual.Unique && !actual.RemoveOnDiscard,
                        "The native callback upgrade values or discard lifetime differ from its definition.");
                    fullMoonApplications++;
                }
                if (expectedMatch && phase == "New") newMoonPositiveNoOps++;
                if (!expectedMatch)
                {
                    Require(role == "filter-control" && applied.Length == 0,
                        "The native nonmatching filter control received the moon upgrade.");
                    filterControls++;
                }
            }
        }
        Require(fullMoonApplications >= 2 && newMoonPositiveNoOps >= 1 && filterControls >= 5,
            "Native positive, opposite-phase or filter-control evidence is incomplete.");
        Console.WriteLine("ORIGINAL-MOON-PHASE-HAND-UPGRADE-NATIVE PASS: unchanged owner, Full/New phase gates, positive/exclusion masks, exact +10 damage/heal upgrade and pre-discard lifetime.");
    }

    internal static void Run()
    {
        var stronger = new CardTraitValue("CardTraitStrongerMagicPower", "CardTraitStrongerMagicPower", 0, 0, true, 0);
        var mask = new CardUpgradeMaskRule("OnlySpells", "Spell");
        var upgrade = Upgrade(removeOnDiscard: true, unique: true, damage: 3, heal: 2, damageBase: 3, healBase: 2);
        var rule = new RelicMoonPhaseHandUpgrade(2, true, false, 0, upgrade, [mask]);
        var owner = new CombatRelicState(RelicMoonPhaseHandUpgradeModel.OwnerId,
            RelicMoonPhaseHandUpgradeModel.OwnerKey, [Effect], isCovenant: false, disallowedInPlacementPhase: true,
            cardModifiers: [new RelicCardModifier(0, null, null, false, moonPhaseHandUpgrade: rule)]);
        Require(RelicModel.Validate([owner]) == null, "A captured moon-phase end-turn hand-upgrade definition was rejected.");

        var rng = UnityRng.Seed(771);
        CardInstanceState spell = Card(1, "spell", "Spell", [stronger],
            [Upgrade(removeOnDiscard: false, unique: false, damage: 2, heal: 0, damageBase: 2, healBase: null, dataId: "existing")]);
        CardInstanceState monster = Card(2, "monster", "Monster", [], []);
        var source = new CombatContext(new CardCycleState([new(1, spell.DataId), new(2, monster.DataId)], [], [], rng, 0, []),
            rng, 0, 3, 10, cardInstances: [spell, monster], relics: [owner]);
        string parent = JsonSerializer.Serialize(source);

        var wrongPhase = RelicMoonPhaseHandUpgradeModel.Apply(source, 1, deployment: false, preview: false);
        var placement = RelicMoonPhaseHandUpgradeModel.Apply(source, 2, deployment: true, preview: false);
        var preview = RelicMoonPhaseHandUpgradeModel.Apply(source, 2, deployment: false, preview: true);
        Require(wrongPhase.Supported && placement.Supported && preview.Supported &&
            JsonSerializer.Serialize(wrongPhase.Context) == parent && JsonSerializer.Serialize(placement.Context) == parent &&
            JsonSerializer.Serialize(preview.Context) == parent,
            "Moon mask, deployment or preview gates applied the hand upgrade unexpectedly.");

        var applied = RelicMoonPhaseHandUpgradeModel.Apply(source, 2, deployment: false, preview: false);
        CardInstanceState changedSpell = applied.Context.FindCard(1)!;
        Require(applied.Supported && changedSpell.Temporary.Upgrades.Select(item => item.DataId)
                .SequenceEqual(["existing", UpgradeId]) && changedSpell.Temporary.Upgrades[0].Stats.Damage == 10 &&
            changedSpell.Temporary.Upgrades[1].Stats.Damage == 15 && changedSpell.Temporary.Upgrades[1].Stats.Heal == 10 &&
            applied.Context.FindCard(2)!.Temporary.Upgrades.Count == 0,
            "The active moon phase failed to filter, append or refresh card magic-power upgrades.");
        var repeated = RelicMoonPhaseHandUpgradeModel.Apply(applied.Context, 2, deployment: false, preview: false);
        Require(repeated.Supported && repeated.Context.FindCard(1)!.Temporary.Upgrades.Count == 2,
            "Repeated unique end-turn application duplicated the hand upgrade.");
        CardInstanceState discarded = changedSpell.OnDiscard(false);
        Require(discarded.Temporary.Upgrades.Select(item => item.DataId).SequenceEqual(["existing"]) &&
            changedSpell.Temporary.Upgrades.Count == 2 && JsonSerializer.Serialize(source) == parent,
            "Remove-on-discard did not clear only the moon-hand upgrade or mutated its parent branch.");

        var persistentRule = new RelicMoonPhaseHandUpgrade(2, false, false, 0,
            Upgrade(removeOnDiscard: false, unique: false, damage: 1, heal: 0, damageBase: null, healBase: null,
                dataId: "persistent"), [mask]);
        var persistentOwner = new CombatRelicState(RelicMoonPhaseHandUpgradeModel.OwnerId,
            RelicMoonPhaseHandUpgradeModel.OwnerKey, [Effect], isCovenant: false, disallowedInPlacementPhase: false,
            cardModifiers: [new RelicCardModifier(0, null, null, false, moonPhaseHandUpgrade: persistentRule)]);
        var persistent = RelicMoonPhaseHandUpgradeModel.Apply(source.WithRelics([persistentOwner]), 2, false, false);
        Require(persistent.Supported && persistent.Context.FindCard(1)!.OnDiscard(false).Temporary.Upgrades.Any(item => item.DataId == "persistent"),
            "A hand upgrade without remove-on-discard did not survive discard.");

        Require(RelicModel.Validate([new CombatRelicState(RelicMoonPhaseHandUpgradeModel.OwnerId,
            RelicMoonPhaseHandUpgradeModel.OwnerKey, [Effect], isCovenant: false, disallowedInPlacementPhase: false)]) != null,
            "An uncaptured moon-phase hand-upgrade payload passed relic validation.");
        Console.WriteLine("ORIGINAL-MOON-PHASE-HAND-UPGRADE-CHECKS PASS: moon/deployment/preview gates, ordered masks, unique repeats, magic refresh, discard lifetime and immutable branches.");
    }

    private static CardUpgradeModifier Upgrade(bool removeOnDiscard, bool unique, int damage, int heal,
        int? damageBase, int? healBase, string dataId = UpgradeId) =>
        new(dataId, dataId, new CardStatModifier(damage: damage, heal: heal), [], removeOnDiscard, unique, false,
            0, 0, [], cloneDamageBase: damageBase, cloneHealBase: healBase,
            maskMetadata: new CardUpgradeMaskMetadata([], false, false, false, false));

    private static CardInstanceState Card(int id, string dataId, string type, CardTraitValue[] traits,
        CardUpgradeModifier[] temporary) => new(id, dataId, CardModifiers.Empty(),
            new CardModifiers(new CardStatModifier(), temporary, 0, []), 0, 0, 0, [],
            maskDescriptor: new CardMaskDescriptor(new CardMaskDefinition(dataId, type, "Common", false, false, false,
                [], [], [], [], null, false, 0, 0, false, false, false, canScaleWithMagicPower: true), 3,
                traits, []));

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidDataException(message); }
}
