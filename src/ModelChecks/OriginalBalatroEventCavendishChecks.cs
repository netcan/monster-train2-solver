using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalBalatroEventCavendishChecks
{
    private const string OwnerId = "24c4ad26-f2b3-4a95-bddc-caab94bd4767";
    private const string OwnerKey = "BalatroEventCavendish";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 204 &&
            root.GetProperty("ModifierScenario").GetString() == "original-balatro-event-cavendish" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("OriginalBalatroEventCavendishPrepared").GetBoolean() &&
            root.GetProperty("OriginalBalatroEventCavendishEndCombatRestored").GetBoolean() &&
            root.GetProperty("OriginalBalatroEventCavendishCardlessCallbacksRestored").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "BalatroEventCavendish native battle failed acceptance.");

        int monsterId = root.GetProperty("OriginalBalatroEventCavendishMonsterCardId").GetInt32();
        int championId = root.GetProperty("OriginalBalatroEventCavendishChampionCardId").GetInt32();
        int spellId = root.GetProperty("OriginalBalatroEventCavendishSpellCardId").GetInt32();
        FixtureValue[] records = root.GetProperty("RelicCardModifiers").EnumerateArray().ToArray();
        Require(monsterId != championId && monsterId != spellId && championId != spellId && records.Length >= 6 &&
            records.Skip(6).All(row => row.GetProperty("Difference").ValueKind == FixtureKind.Null &&
                row.GetProperty("Actual").ValueKind != FixtureKind.Null),
            "The isolated Cavendish fixture must capture both manager reset modes for a Monster, champion and Spell.");
        FixtureValue[] setupRecords = records.Take(6).ToArray();
        CheckEndCombat(root.GetProperty("OriginalBalatroEventCavendishEndCombat"));
        CheckCardlessCallbacks(root.GetProperty("OriginalBalatroEventCavendishCardlessCallbacks"));
        FixtureValue[] trainPhases = root.GetProperty("TrainPhases").EnumerateArray().ToArray();
        FixtureValue finalCombat = trainPhases.Last(row => row.GetProperty("Kind").GetString() == "Combat");
        FixtureValue beforeContext = finalCombat.GetProperty("Before").GetProperty("Context");
        FixtureValue actualContext = finalCombat.GetProperty("Actual").GetProperty("Context");
        FixtureValue actualOwner = actualContext.GetProperty("Relics").EnumerateArray()
            .Single(relic => relic.GetProperty("DataId").GetString() == OwnerId);
        Require(actualContext.GetProperty("BattleRng").ContentEquals(beforeContext.GetProperty("BattleRng")) &&
            actualOwner.GetProperty("CardAttackModifiers")[0].GetProperty("AppliedCardAttacks").ContentEquals(
                beforeContext.GetProperty("Relics").EnumerateArray()
                    .Single(relic => relic.GetProperty("DataId").GetString() == OwnerId)
                    .GetProperty("CardAttackModifiers")[0].GetProperty("AppliedCardAttacks")),
            "Native terminal stop must preserve Cavendish and skip the end-of-combat callback that follows the room loop.");

        foreach ((int cardId, bool expectedApplied) in new[] { (monsterId, true), (championId, false), (spellId, false) })
        {
            FixtureValue[] rows = setupRecords.Where(row => row.GetProperty("Before").GetProperty("InstanceId").GetInt32() == cardId).ToArray();
            Require(rows.Length == 2 && rows.Select(row => row.GetProperty("ResetTemporary").GetBoolean()).OrderBy(value => value).SequenceEqual([false, true]),
                "Cavendish is missing the accumulated and reset observations for card " + cardId + ".");
            foreach (FixtureValue row in rows)
            {
                Require(row.GetProperty("Difference").ValueKind == FixtureKind.Null &&
                    row.GetProperty("Actual").ValueKind != FixtureKind.Null,
                    "A Cavendish native card modifier diverged from the model for card " + cardId + ".");
                CardInstanceState before = row.GetProperty("Before").Deserialize<CardInstanceState>()!;
                CardInstanceState actual = row.GetProperty("Actual").Deserialize<CardInstanceState>()!;
                CombatContext context = row.GetProperty("ContextBefore").Deserialize<CombatContext>()!;
                CombatRelicState owner = context.Relics!.Single(relic => relic.DataId == OwnerId);
                int relicIndex = context.Relics.Select((relic, index) => (relic, index))
                    .Single(item => item.relic.DataId == OwnerId).index;
                RelicCardAttackModifier rule = owner.CardAttackModifiers!.Single();
                RelicCardModifierResult predicted = RelicCardModifierModel.Apply(before, context.Relics,
                    row.GetProperty("ResetTemporary").GetBoolean(), context);
                Require(owner.AssetKey == OwnerKey && owner.EffectTypes.SequenceEqual(new[] {
                        "RelicEffectModifyCardAttack", "RelicEffectChanceToDestroySelf", "RelicEffectBalatroBananaShake" }) &&
                    rule.EffectIndex == 0 && rule.AttackModifier == 0 && rule.UnmodifiedAttackMultiplier == 3 &&
                    rule.CardType == "Monster" && rule.ExcludeChampions && rule.CharacterSubtypeKey.Length == 0 &&
                    rule.AppliedCardAttacks != null &&
                    predicted.Supported && ModelJson.Difference(JsonSerializer.Serialize(predicted.Card, ModelJson.Options),
                        JsonSerializer.Serialize(actual, ModelJson.Options)) == null,
                    "Cavendish native owner payload or complete card-state prediction changed for card " + cardId + ".");

                FixtureValue[] dispatches = row.GetProperty("Dispatches").EnumerateArray().ToArray();
                FixtureValue attack = dispatches.Single(item => item.GetProperty("RelicIndex").GetInt32() == relicIndex &&
                    item.GetProperty("EffectIndex").GetInt32() == 0);
                Require(attack.GetProperty("Returned").GetBoolean() == expectedApplied,
                    "Cavendish Monster/champion/Spell match result changed for card " + cardId + ".");
            }
            if (expectedApplied)
            {
                FixtureValue accumulated = rows.Single(row => !row.GetProperty("ResetTemporary").GetBoolean());
                FixtureValue reset = rows.Single(row => row.GetProperty("ResetTemporary").GetBoolean());
                CardInstanceState before = accumulated.GetProperty("Before").Deserialize<CardInstanceState>()!;
                CardInstanceState applied = accumulated.GetProperty("Actual").Deserialize<CardInstanceState>()!;
                CardInstanceState resetActual = reset.GetProperty("Actual").Deserialize<CardInstanceState>()!;
                int baseAttack = before.MaskDescriptor!.Definition.UnmodifiedSpawnAttack ?? 0;
                RelicCardAttackContribution tracked = accumulated.GetProperty("ContextBefore").Deserialize<CombatContext>()!
                    .Relics!.Single(relic => relic.DataId == OwnerId).CardAttackModifiers!.Single().AppliedCardAttacks!
                    .Single(item => item.CardInstanceId == monsterId);
                Require(baseAttack > 0 && applied.Temporary.Offsets.Damage == unchecked(before.Temporary.Offsets.Damage + 2 * baseAttack) &&
                    resetActual.Temporary.Offsets.Damage == before.Temporary.Offsets.Damage && tracked.InDeckState &&
                    tracked.Amount == 2 * baseAttack,
                    "Cavendish did not add twice the native unmodified Monster attack or reset without accumulation.");
            }
            else
            {
                Require(rows.All(row => row.GetProperty("ContextBefore").Deserialize<CombatContext>()!
                        .Relics!.Single(relic => relic.DataId == OwnerId).CardAttackModifiers!.Single().AppliedCardAttacks!
                        .All(item => item.CardInstanceId != cardId)),
                    "Cavendish tracked an excluded champion or Spell card for undo.");
            }
        }

        Console.WriteLine("ORIGINAL-BALATRO-EVENT-CAVENDISH NATIVE PASS: unchanged owner; manager resets; cardless ordinary/Champion callbacks; deterministic end-combat miss/hit and exact deck undo.");
    }

    private static void CheckCardlessCallbacks(FixtureValue value)
    {
        FixtureValue[] records = value.EnumerateArray().ToArray();
        Require(records.Length == 2, "Cavendish must capture ordinary and Champion cardless callbacks.");
        foreach ((string branch, bool champion) in new[] { ("ordinary", false), ("champion-control", true) })
        {
            FixtureValue row = records.Single(item => item.GetProperty("Branch").GetString() == branch);
            int unmodifiedBefore = row.GetProperty("UnmodifiedAttackBefore").GetInt32();
            int expected = champion ? 0 : unchecked(2 * unmodifiedBefore);
            Require(row.GetProperty("Callback").GetString() == "RelicEffectModifyCardAttack.OnCharacterAdded" &&
                row.GetProperty("IsCardless").GetBoolean() && row.GetProperty("IsChampion").GetBoolean() == champion &&
                !row.GetProperty("IsClone").GetBoolean() && unmodifiedBefore > 0 &&
                row.GetProperty("ExpectedModifier").GetInt32() == expected &&
                row.GetProperty("ObservedModifier").GetInt32() == expected &&
                row.GetProperty("UnmodifiedAttackAfter").GetInt32() - unmodifiedBefore == expected,
                "Cavendish " + branch + " cardless OnCharacterAdded modifier changed.");
        }
    }

    private static void CheckEndCombat(FixtureValue value)
    {
        FixtureValue[] records = value.EnumerateArray().ToArray();
        Require(records.Length == 2, "Cavendish end-combat fixture must contain deterministic miss and hit branches.");
        foreach ((string branch, int expectedRoll) in new[] { ("miss", 1), ("hit", 0) })
        {
            FixtureValue row = records.Single(item => item.GetProperty("Branch").GetString() == branch);
            uint[] before = row.GetProperty("BattleRngBefore").Deserialize<uint[]>()!;
            uint[] after = row.GetProperty("BattleRngAfter").Deserialize<uint[]>()!;
            var rng = new UnityRng(before[0], before[1], before[2], before[3]);
            RngDraw draw = rng.Range(0, 100);
            int[] observedRolls = row.GetProperty("ObservedRolls").Deserialize<int[]>()!;
            bool hit = expectedRoll == 0;
            FixtureValue[] events = row.GetProperty("Events").EnumerateArray().ToArray();
            Require(draw.Value == expectedRoll && observedRolls.SequenceEqual([expectedRoll]) &&
                draw.State.Equals(new UnityRng(after[0], after[1], after[2], after[3])) &&
                row.GetProperty("OwnerPresentBefore").GetBoolean() && row.GetProperty("OwnerPresentAfter").GetBoolean() == !hit,
                "Cavendish native " + branch + " branch did not match the Battle RNG and owner transition.");
            string[] expectedEvents = hit
                ? ["Triggered:RelicEffectChanceToDestroySelf", "Attempted:RelicEffectBalatroBananaShake"]
                : ["Attempted:RelicEffectBalatroBananaShake"];
            Require(events.Select(item => item.GetProperty("Kind").GetString() + ":" + item.GetProperty("Effect").GetString())
                    .SequenceEqual(expectedEvents),
                "Cavendish native " + branch + " branch did not emit the expected trigger/attempt order.");

            Dictionary<int, int> expectedDeck = row.GetProperty("DeckBefore").EnumerateArray().ToDictionary(
                item => item.GetProperty("CardInstanceId").GetInt32(), item => item.GetProperty("Attack").GetInt32());
            if (hit)
            {
                foreach (FixtureValue contribution in row.GetProperty("Contributions").EnumerateArray())
                {
                    if (!contribution.GetProperty("InDeckState").GetBoolean()) continue;
                    int cardId = contribution.GetProperty("CardInstanceId").GetInt32();
                    expectedDeck[cardId] -= contribution.GetProperty("Amount").GetInt32();
                }
            }
            Dictionary<int, int> actualDeck = row.GetProperty("DeckAfter").EnumerateArray().ToDictionary(
                item => item.GetProperty("CardInstanceId").GetInt32(), item => item.GetProperty("Attack").GetInt32());
            Require(actualDeck.Count == expectedDeck.Count && expectedDeck.All(item => actualDeck.TryGetValue(item.Key, out int attack) && attack == item.Value),
                "Cavendish native " + branch + " branch did not preserve or undo precisely the tracked deck attack contributions.");
        }
        Console.WriteLine("ORIGINAL-BALATRO-EVENT-CAVENDISH END-COMBAT PASS: native roll 1 miss, roll 0 removal/undo, notification order, and reacquisition reset.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}
