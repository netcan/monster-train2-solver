using System;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalCardTypeCostChecks
{
    private const string RelicId = "a19cac7c-b2e0-42bd-bc1e-d180e17a1c15";
    private const string BlightId = "21038774-c0a4-413c-ab7e-27e458228e5f";
    private const string JunkId = "62cc5532-5fa0-4e66-9fec-85be573af359";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-card-type-cost-relic" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("RelicCardModifiersEnabled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0),
            "Original card-type cost relic battle is not strictly accepted.");
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
        Require(rows.Length >= 10, "Missing native cost refreshes for the two played cards, control and generated Junk.");
        foreach (var row in rows)
        {
            CombatRelicState relic = row.Relics.Single(item => item.DataId == RelicId);
            Require(relic.AssetKey == "CheaperBlightScourge" &&
                relic.EffectTypes.SequenceEqual(["RelicEffectModifyCardTypeCost", "RelicEffectModifyCardTypeCost"]) &&
                relic.CardTypeCosts?.Select(item => (item.EffectIndex, item.CardType, item.Amount,
                    item.IgnoreRestrictions, item.RequiredSubtype, item.ConditionCount)).SequenceEqual(
                    [(0, "Blight", -99, false, null, 0), (1, "Junk", -99, false, null, 0)]) == true,
                "Original card-type cost relic identity, effect order or parameters changed.");
            RelicCardModifierResult predicted = RelicCardModifierModel.Apply(row.Before, row.Relics, row.Reset, row.Context);
            Require(predicted.Supported, "Relic card-type cost refresh is unsupported: " + predicted.UnsupportedReason);
            Equal(predicted.Card, row.Actual, "native card modifier state");
            Equal(predicted.Dispatches, row.Dispatches, "ordered native relic dispatches");
            Equal(predicted.Context!.Relics, row.AfterContext.Relics, "native relic state");
            Equal(predicted.Context.Statistics, row.AfterContext.Statistics, "native statistics");
            Require(predicted.Dispatches.Where(item => item.Returned).Select(item => (item.RelicIndex, item.EffectIndex))
                .SequenceEqual(row.Notifications), "Relic notification order differs.");
        }
        Require(rows.Any(row => row.Before.DataId == BlightId && !row.Reset &&
                row.Before.Temporary.Offsets.Cost == -99 && row.Actual.Temporary.Offsets.Cost == -198 &&
                row.Actual.Temporary.Offsets.XCost == 198 && row.Dispatches.Select(item => item.Returned).SequenceEqual([true, false])) &&
            rows.Any(row => row.Before.DataId == JunkId && !row.Reset &&
                row.Before.Temporary.Offsets.Cost == -99 && row.Actual.Temporary.Offsets.Cost == -198 &&
                row.Actual.Temporary.Offsets.XCost == 198 && row.Dispatches.Select(item => item.Returned).SequenceEqual([false, true])) &&
            rows.Any(row => row.Before.DataId == BlightId && row.Reset && row.Actual.Temporary.Offsets.Cost == -99) &&
            rows.Any(row => row.Before.DataId == JunkId && row.Reset && row.Actual.Temporary.Offsets.Cost == -99) &&
            rows.Any(row => row.Before.DataId != BlightId && row.Before.DataId != JunkId &&
                row.Dispatches.All(item => !item.Returned) && row.Actual.Temporary.Offsets.Cost == 0) &&
            rows.Count(row => row.Before.DataId == JunkId && row.Reset &&
                row.Before.Temporary.Offsets.Cost == 0 && row.Actual.Temporary.Offsets.Cost == -99) >= 1,
            "Native repeat/reset, nonmatching card or enemy-generated Junk cost witness is missing.");

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().Take(2).ToArray();
        Require(actions.Length == 2, "The two original cards were not played.");
        for (int index = 0; index < actions.Length; index++)
        {
            FixtureValue entry = actions[index];
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            string id = index == 0 ? BlightId : JunkId;
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
            Require(entry.GetProperty("ScenarioAction").GetBoolean() && card.DataId == id &&
                card.Temporary.Offsets.Cost == -99 && card.Temporary.Offsets.XCost == 99 &&
                before.Energy == actual.Energy && actual.Spawn.Train.Context!.OtherPiles!
                    .Single(pile => pile.Name == "Purged").Cards.Any(item => item.InstanceId == action.CardInstanceId),
                "An original Blight/Junk card did not play free and purge as captured.");
            BattleActionResult replay = BattleActionModel.PlayCard(before, action);
            Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(actual),
                "Original free Blight/Junk action differs from native state: " + replay.Reason);
        }
        Console.WriteLine("ORIGINAL-CARD-TYPE-COST-NATIVE PASS: " + rows.Length +
            " exact manager refreshes, both free original plays and generated Junk cost.");
    }

    private static void Equal<T>(T left, T right, string label)
    {
        if (JsonSerializer.Serialize(left) != JsonSerializer.Serialize(right))
            throw new InvalidOperationException("Original card-type cost " + label + " differs.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
