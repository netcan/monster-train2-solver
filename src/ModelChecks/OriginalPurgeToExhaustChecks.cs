using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalPurgeToExhaustChecks
{
    private const string Id = "10648aee-8577-49b6-8659-318a7f922d5a";
    private const string Key = "PurgeToExhaust";
    private const string Purge = "CardTraitSelfPurge";
    private const string Exhaust = "CardTraitExhaustState";
    private const string PositiveSpell = "fc119000-0000-4000-8000-0000000002a1";
    private const string SpellControl = "fc119000-0000-4000-8000-0000000002a2";
    private const string PositiveEquipment = "fc119000-0000-4000-8000-0000000002a3";
    private const string EquipmentControl = "fc119000-0000-4000-8000-0000000002a4";
    private const string BlightControl = "21038774-c0a4-413c-ab7e-27e458228e5f";
    private const string BattleSpell = PositiveSpell;

    internal static void Run()
    {
        CombatRelicState relic = Owner();
        CardInstanceState spell = Card(PositiveSpell, "Spell", true);
        CardInstanceState convertedSpell = Apply(spell, relic, returned: true, ("OnlySpells", true));
        Require(Traits(convertedSpell).SequenceEqual([Exhaust]) &&
            convertedSpell.MaskDescriptor!.PermanentReplacements.Count == 1 &&
            convertedSpell.ExhaustState?.BaseTraits.Single().TraitIndex == 0 &&
            convertedSpell.ExhaustState.PlayCountModifier == 0,
            "SelfPurge spell did not become one persistent Exhaust trait with native private state.");
        var purgeRule = new CardPlayRule(PositiveSpell, "PurgeToExhaustSpell", 0, "Spell", "Purged", null, [],
            cardType: "Spell");
        Require(CardModifierModel.Resolve(purgeRule, convertedSpell).Destination == "Discard",
            "Converted SelfPurge did not route through the native Exhaust-state discard callback.");

        CardInstanceState repeatedSpell = Apply(convertedSpell, relic, returned: false,
            ("OnlySpells", true), ("OnlyEquipment", false));
        Require(repeatedSpell.MaskDescriptor!.PermanentReplacements.Count == 1 &&
            JsonSerializer.Serialize(spell.MaskDescriptor!.PermanentReplacements) == "[]",
            "Repeat conversion duplicated the replacement or mutated its parent.");

        CardInstanceState spellWithoutTrait = Apply(Card(SpellControl, "Spell", false), relic,
            returned: false, ("OnlySpells", true), ("OnlyEquipment", false));
        Require(spellWithoutTrait.MaskDescriptor!.PermanentReplacements.Count == 1 &&
            Traits(spellWithoutTrait).Length == 0,
            "A filter-matched spell without SelfPurge did not preserve native replacement metadata.");

        CardInstanceState equipment = Apply(Card(PositiveEquipment, "Equipment", true), relic,
            returned: true, ("OnlySpells", false), ("OnlyEquipment", true));
        Require(Traits(equipment).SequenceEqual([Exhaust]), "The secondary Equipment filter did not convert SelfPurge.");
        CardInstanceState equipmentWithoutTrait = Apply(Card(EquipmentControl, "Equipment", false), relic,
            returned: false, ("OnlySpells", false), ("OnlyEquipment", true));
        Require(equipmentWithoutTrait.MaskDescriptor!.PermanentReplacements.Count == 1 &&
            Traits(equipmentWithoutTrait).Length == 0,
            "A filter-matched Equipment without SelfPurge did not preserve replacement metadata.");
        CardInstanceState blight = Apply(Card(BlightControl, "Blight", true), relic,
            returned: false, ("OnlySpells", false), ("OnlyEquipment", false));
        Require(blight.MaskDescriptor!.PermanentReplacements.Count == 0 && Traits(blight).SequenceEqual([Purge]),
            "The original SelfPurge Blight passed a spell/equipment filter or was converted.");

        var unknown = new CombatRelicState("unclassified", Key, [RelicModel.ModifyTrait],
            cardModifiers: relic.CardModifiers);
        Require(!RelicCardModifierModel.Apply(spell, [unknown], resetTemporary: false).Supported,
            "An unclassified trait-replacement owner escaped the exact native owner guard.");
        Require(!RelicCardModifierModel.Apply(Card(PositiveSpell, "Spell", true, descriptor: false), [relic],
            resetTemporary: false).Supported,
            "Trait replacement accepted a card without branch-owned mask metadata.");

        string parent = JsonSerializer.Serialize(spell);
        Parallel.For(0, 32, _ =>
        {
            CardInstanceState branch = Apply(spell, relic, returned: true, ("OnlySpells", true));
            Require(Traits(branch).SequenceEqual([Exhaust]), "Parallel trait-replacement branch diverged.");
        });
        Require(JsonSerializer.Serialize(spell) == parent, "Parallel trait-replacement branches mutated their parent.");
        Console.WriteLine("ORIGINAL-PURGE-TO-EXHAUST-PURE PASS: ordered Spell/Equipment filters, source-missing metadata, repeated/reset idempotence, Blight control, owner guard and immutable branches.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 184 &&
            root.GetProperty("ModifierScenario").GetString() == "original-purge-to-exhaust-relic" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("RelicCardModifiersEnabled").GetBoolean() &&
            new[] { "CaptureFailures", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0),
            "PurgeToExhaust native battle is incomplete, mismatched or from another build.");

        RelicCardModifierChecks.Native(root, required: true, requireScenarioCoverage: false);
        var rows = root.GetProperty("RelicCardModifiers").EnumerateArray().Select(row => (
            Before: row.GetProperty("Before").Deserialize<CardInstanceState>()!,
            Relics: row.GetProperty("Relics").Deserialize<CombatRelicState[]>()!,
            Reset: row.GetProperty("ResetTemporary").GetBoolean(),
            Actual: row.GetProperty("Actual").Deserialize<CardInstanceState>()!,
            Dispatches: row.GetProperty("Dispatches").Deserialize<RelicCardModifierDispatch[]>()!)).ToArray();
        Require(rows.Length >= 18, "Missing repeated native manager controls for PurgeToExhaust.");
        var ownerRows = rows.Where(row => row.Relics.Any(relic => relic.DataId == Id)).ToArray();
        Require(ownerRows.Length >= 18, "Native manager rows do not carry the acquired original owner.");
        foreach (var row in ownerRows)
        {
            CombatRelicState owner = row.Relics.Single(relic => relic.DataId == Id);
            Require(owner.AssetKey == Key && owner.EffectTypes.SequenceEqual([RelicModel.ModifyTrait]) &&
                owner.CardModifiers?.Count == 1, "Original owner identity or effect order changed.");
            RelicTraitReplacement replacement = owner.CardModifiers![0].TraitReplacement!;
            Require(replacement.SourceTrait == Purge && replacement.TargetTrait == Exhaust &&
                replacement.CardFilter?.AssetKey == "OnlySpells" && replacement.CardFilter.CardType == "Spell" &&
                replacement.CardFilterSecondary?.AssetKey == "OnlyEquipment" && replacement.CardFilterSecondary.CardType == "Equipment",
                "Native trait replacement or ordered card-type filters changed.");
            var result = RelicCardModifierModel.Apply(row.Before, row.Relics, row.Reset);
            Require(result.Supported, "Native trait replacement is unsupported: " + result.UnsupportedReason);
            Equal(result.Card, row.Actual, "native card state");
            Equal(result.Dispatches, row.Dispatches, "native return/filter dispatches");
        }

        CheckWitness(PositiveSpell, returned: true, [("OnlySpells", true)]);
        CheckWitness(PositiveSpell, returned: false, [("OnlySpells", true), ("OnlyEquipment", false)]);
        CheckWitness(SpellControl, returned: false, [("OnlySpells", true), ("OnlyEquipment", false)]);
        CheckWitness(PositiveEquipment, returned: true, [("OnlySpells", false), ("OnlyEquipment", true)]);
        CheckWitness(EquipmentControl, returned: false, [("OnlySpells", false), ("OnlyEquipment", true)]);
        CheckWitness(BlightControl, returned: false, [("OnlySpells", false), ("OnlyEquipment", false)]);

        bool replayedBattleSpell = false;
        foreach (FixtureValue entry in root.GetProperty("Actions").EnumerateArray())
        {
            if (!entry.GetProperty("ScenarioAction").GetBoolean()) continue;
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardInstanceState? card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId);
            if (card?.DataId != BattleSpell) continue;
            Require(card.MaskDescriptor!.PermanentReplacements.Any(item => item.OldName == Purge && item.NewName == Exhaust) &&
                Traits(card).SequenceEqual([Exhaust]), "The native battle witness did not carry the converted Exhaust trait.");
            BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatContext after = actual.Spawn.Train.Context!;
            CardInstanceState played = after.FindCard(action.CardInstanceId)!;
            Require(after.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(item => item.InstanceId == action.CardInstanceId) &&
                !after.OtherPiles.Single(pile => pile.Name == "Purged").Cards.Any(item => item.InstanceId == action.CardInstanceId) &&
                played.ExhaustState?.BaseTraits.Any(trait => trait.IsExhausted && trait.ExhaustedCardId == action.CardInstanceId) == true,
                "Converted SelfPurge did not play into Exhausted with native private Exhaust state.");
            BattleActionResult replay = BattleActionModel.PlayCard(before, action);
            Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(actual),
                "Converted SelfPurge action differs from native replay: " + replay.Reason);
            replayedBattleSpell = true;
        }
        Require(replayedBattleSpell, "The synthetic PurgeToExhaust spell was not actually played in the native battle.");
        Require(root.GetProperty("Mismatches").GetInt32() == 0,
            "PurgeToExhaust native battle captured " + root.GetProperty("Mismatches").GetInt32() + " model mismatches.");
        Console.WriteLine($"ORIGINAL-PURGE-TO-EXHAUST-NATIVE PASS: {ownerRows.Length} exact native manager calls, six source/filter controls, played Exhaust card with private state, full action replay.");

        void CheckWitness(string dataId, bool returned, (string Asset, bool Accepted)[] filters)
        {
            bool found = ownerRows.Any(row => row.Before.DataId == dataId && row.Dispatches.Any(dispatch =>
                dispatch.Returned == returned && dispatch.Filters.Select(filter => (filter.AssetKey, filter.Accepted)).SequenceEqual(filters)) &&
                row.Actual.MaskDescriptor!.PermanentReplacements.Any(item => item.OldName == Purge && item.NewName == Exhaust) ==
                    (dataId != BlightControl));
            Require(found, $"Native {dataId} witness with returned={returned} and ordered filters is missing.");
        }
    }

    private static CombatRelicState Owner() => new(Id, Key, [RelicModel.ModifyTrait], cardModifiers: [
        new RelicCardModifier(0, null, null, false, traitReplacement: new RelicTraitReplacement(Purge, Exhaust,
            new CardUpgradeMaskRule("OnlySpells", "Spell"), new CardUpgradeMaskRule("OnlyEquipment", "Equipment")))]);

    private static CardInstanceState Apply(CardInstanceState source, CombatRelicState relic, bool returned,
        params (string Asset, bool Accepted)[] expectedFilters)
    {
        RelicCardModifierResult result = RelicCardModifierModel.Apply(source, [relic], resetTemporary: false);
        Require(result.Supported, "PurgeToExhaust model rejected a calibrated card: " + result.UnsupportedReason);
        RelicCardModifierDispatch dispatch = result.Dispatches.Single();
        Require(dispatch.Returned == returned && dispatch.Filters.Select(filter => (filter.AssetKey, filter.Accepted)).SequenceEqual(expectedFilters),
            "PurgeToExhaust model dispatch order or return value changed.");
        return result.Card!;
    }

    private static CardInstanceState Card(string id, string type, bool selfPurge, bool descriptor = true)
    {
        var definition = new CardMaskDefinition(id, type, "Common", false, false, false,
            [], [], [], [], null, false, 0, 0, false, false, false);
        CardTraitValue[] traits = selfPurge ? [new CardTraitValue(Purge, Purge, 0, 0, true, 0)] : [];
        return new CardInstanceState(1, id, CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            maskDescriptor: descriptor ? new CardMaskDescriptor(definition, 0, traits, []) : null);
    }

    private static string[] Traits(CardInstanceState card) => CardBranchMaskModel.Resolve(card).Traits.ToArray();
    private static void Equal<T>(T expected, T actual, string label) =>
        Require(JsonSerializer.Serialize(expected) == JsonSerializer.Serialize(actual), "PurgeToExhaust " + label + " differs.");
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
