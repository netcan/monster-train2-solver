using System;
using System.IO;
using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicCardAttackChecks
{
    internal static void Run()
    {
        CardMaskDefinition whelp = Definition("whelp", "SubtypesData_Whelp");
        CardMaskDescriptor descriptor = new(whelp, 0, [], []);
        CardInstanceState card = new(1, whelp.DataId, CardModifiers.Empty(), CardModifiers.Empty(),
            0, 0, 0, [], maskDescriptor: descriptor);
        CombatRelicState relic = Relic();

        RelicCardModifierResult applied = RelicCardModifierModel.Apply(card, [relic]);
        Require(applied.Supported && applied.Card!.Temporary.Offsets.Damage == 8 &&
            applied.Dispatches.Select(item => item.Returned).SequenceEqual([true, false]) &&
            card.Temporary.Offsets.Damage == 0,
            "Whelp subtype did not receive only its ordered +8 temporary relic attack modifier: " + applied.UnsupportedReason);
        RelicCardModifierResult reset = RelicCardModifierModel.Apply(applied.Card!, [relic], resetTemporary: true);
        Require(reset.Supported && reset.Card!.Temporary.Offsets.Damage == 8,
            "Reset-and-apply accumulated the relic attack modifier: " + reset.UnsupportedReason);

        CardMaskDefinition other = Definition("other", "SubtypesData_Tomb");
        CardInstanceState otherCard = new(2, other.DataId, CardModifiers.Empty(), CardModifiers.Empty(),
            0, 0, 0, [], maskDescriptor: new CardMaskDescriptor(other, 0, [], []));
        RelicCardModifierResult mismatch = RelicCardModifierModel.Apply(otherCard, [relic]);
        Require(mismatch.Supported && mismatch.Card!.Temporary.Offsets.Damage == 0 &&
            mismatch.Dispatches.All(item => !item.Returned), "Nonmatching spawned subtype received a relic attack modifier.");

        CombatRelicState unsupported = new("scaled-attack", "ScaledAttack", [RelicModel.ModifyCardAttack],
            cardAttackModifiers: [new RelicCardAttackModifier(0, "", "", 0, 1.5f, 0, "Monster",
                false, false, false, false)]);
        Require(!RelicCardModifierModel.Apply(card, [unsupported]).Supported,
            "An unmodeled relic attack multiplier was silently accepted.");

        var rng = UnityRng.Seed(1107);
        var birthContext = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 2, 10,
            relics: [relic]);
        CombatUnit born = new(1, "whelp-token", CombatTeam.Player, 4, 20, 20, true, false, false,
            [new CombatStatus("cardless", 1)], size: 1, subtypes: ["SubtypesData_Whelp"],
            modifiers: new UnitModifiers(4, 0, 0, 1, 0, true, false, []));
        RoomCombatResult birth = RelicSpawnStatusModel.CharacterAdded(
            new RoomCombatState(0, false, [born], [], birthContext), 1, 0, onlyCovenants: false);
        Require(birth.Supported && birth.State!.Units.Single().Attack == 12 &&
            birth.State.Units.Single().Modifiers!.DamageBuff == 8,
            "Cardless Whelp birth did not receive the native subtype-matched attack bonus: " + birth.UnsupportedReason);

        var spikeRule = new RelicStatusAttackModifier(0, "spikes", "Monsters", 2);
        var spikeModifiers = new UnitModifiers(7, 0, 0, 1, 1, true, false, [], statusAttackModifiers: [spikeRule]);
        var spiked = new CombatUnit(21, "spiked", CombatTeam.Player, 7, 20, 20, true, false, false,
            [new CombatStatus("spikes", 3)], modifiers: spikeModifiers);
        var enemySpiked = new CombatUnit(22, "enemy-spiked", CombatTeam.Enemy, 7, 20, 20, true, false, false,
            [new CombatStatus("spikes", 3)], modifiers: spikeModifiers);
        var noSpikes = new CombatUnit(23, "plain", CombatTeam.Player, 7, 20, 20, true, false, false,
            [], modifiers: spikeModifiers);
        Require(spiked.Attack == 13 && enemySpiked.Attack == 7 && noSpikes.Attack == 7,
            "Dynamic native IAddAttack status scaling did not remain separate and team-filtered.");

        Console.WriteLine("RELIC-CARD-ATTACK-CHECKS PASS: ordered subtype dispatch, reset, nonmatch, multiplier rejection, cardless birth and separate team-filtered dynamic relic attack.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 147 &&
            root.GetProperty("ModifierScenario").GetString() == "original-card-attack-relic" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Original card-attack relic battle is incomplete or from an unexpected game build.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Card-attack relic capture has nonzero " + field + ".");

        var witnesses = root.GetProperty("RelicCardModifiers").EnumerateArray()
            .Where(row => row.GetProperty("Before").GetProperty("DataId").GetString() is
                "fc119000-0000-4000-8000-000000000201" or "fc119000-0000-4000-8000-000000000202")
            .ToArray();
        Require(witnesses.Length >= 4, "Native manager did not produce repeated Whelp and Dragon card-attack observations.");
        foreach (string cardId in new[] { "fc119000-0000-4000-8000-000000000201", "fc119000-0000-4000-8000-000000000202" })
        {
            FixtureValue[] rows = witnesses.Where(row => row.GetProperty("Before").GetProperty("DataId").GetString() == cardId).ToArray();
            Require(rows.Length >= 2, "Native manager did not observe both reset modes for " + cardId + ".");
            int expectedEffect = cardId.EndsWith("201", StringComparison.Ordinal) ? 0 : 1;
            foreach (FixtureValue row in rows)
            {
                CardInstanceState actual = row.GetProperty("Actual").Deserialize<CardInstanceState>()!;
                FixtureValue[] dispatches = row.GetProperty("Dispatches").EnumerateArray().ToArray();
                Require(actual.Temporary.Offsets.Damage == 8 && dispatches.Length == 2 &&
                    dispatches[0].GetProperty("EffectIndex").GetInt32() == 0 &&
                    dispatches[1].GetProperty("EffectIndex").GetInt32() == 1 &&
                    dispatches[expectedEffect].GetProperty("Returned").GetBoolean() &&
                    !dispatches[1 - expectedEffect].GetProperty("Returned").GetBoolean(),
                    "Native card-attack effect order or subtype result differs for " + cardId + ".");
            }
        }
        Console.WriteLine($"NATIVE-ORIGINAL-CARD-ATTACK-RELIC-CHECKS PASS: {witnesses.Length} Whelp/Dragon manager observations match native +8 subtype dispatch.");
    }

    private static CombatRelicState Relic() => new("original-whelp-dragon-attack", "WhelpDragonAttack_DH",
        [RelicModel.ModifyCardAttack, RelicModel.ModifyCardAttack], isCovenant: false,
        disallowedInPlacementPhase: false, cardAttackModifiers:
        [new RelicCardAttackModifier(0, "", "SubtypesData_Whelp", 8, 0, 0, "Spell", false, false, false, false),
         new RelicCardAttackModifier(1, "", "SubtypesData_Dragon", 8, 0, 0, "Spell", false, false, false, false)]);

    private static CardMaskDefinition Definition(string id, string subtype) => new(id, "Spell", "Common",
        true, true, true, [subtype], [], [], ["CardEffectSpawnMonster"], null, false, 1, 0, false, false, false);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}
