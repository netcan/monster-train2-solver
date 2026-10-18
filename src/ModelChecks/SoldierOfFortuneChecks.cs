using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SoldierOfFortuneChecks
{
    private const string DataId = "fbf7e58a-9369-49d8-ba1a-782fbcaf6498";
    internal static PlayCardAction? Choose(BattleTurnState source)
    {
        foreach (CardToken card in source.Spawn.Train.Context!.Cards.Hand.Where(card => card.DataId == DataId))
        {
            var action = new PlayCardAction(card.InstanceId, 2);
            if (BattleActionModel.PlayCard(source, action).Supported) return action;
        }
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("SoldierOfFortunePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() && root.GetProperty("SoldierOfFortuneEmptyTargetTest").GetBoolean(),
            "Missing completed original Soldier of Fortune witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int noops = 0, plays = 0, paid = 0, unpaid = 0;
        foreach (FixtureValue row in root.GetProperty("SoldierOfFortuneNullRecords").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("Targets").GetInt32() == 0, "Missing empty-target no-op.");
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState after = row.GetProperty("After").Deserialize<TrainCombatState>()!;
            var effect = new CardActionEffect("StatusNull", "Room", 0, false, false, []);
            RoomCombatState room = before.Rooms.Single(room => room.RoomIndex == 2);
            Require(CardSpellModel.TestPlay(room, [effect], 0).CanPlay, "Status no-op rejected empty targets.");
            var result = CardSpellModel.Apply(before, 2, [effect], 0);
            Require(result.Supported, result.UnsupportedReason ?? "Status no-op unsupported.");
            // Apply is the whole-effect-sequence adapter, so its documented
            // effect-end boundary clears the pending draw-upgrade queue.
            Equal(result.State, CardSpellModel.WithContext(after, after.Context!.AfterCardEffects()), "settled no-op state and RNG");
            Equal(before, after, "native no-op must not mutate state");
            noops++;
        }
        BattleTurnState? initial = null;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            initial ??= before;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            if (before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.DataId != DataId) continue;
            BattleTurnState after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatContext start = before.Spawn.Train.Context!, end = after.Spawn.Train.Context!;
            CardPlayRule definition = before.PlayRules!.Cards.Single(rule => rule.DataId == DataId);
            Require(definition.Effects!.Select(effect => effect.Type).SequenceEqual(new[] { "ScheduleNextDrawUpgrade", "DrawType", "StatusNull" }),
                "Original ordered effect pipeline changed.");
            bool affordable = start.Gold >= 30;
            Require(start.Gold - end.Gold == (affordable ? 30 : 0) && end.Cards.NextDrawUpgrades!.Count == 0,
                "Fee or effect-end queue clearing differs.");
            CardToken drawn = end.Cards.Hand.Single(card => start.Cards.Hand.All(prior => prior.InstanceId != card.InstanceId));
            CardInstanceState recipient = end.FindCard(drawn.InstanceId)!;
            CardInstanceState previous = start.FindCard(drawn.InstanceId)!;
            Require(recipient.MaskDescriptor!.Definition.CardType == "Monster", "Typed draw did not select a unit.");
            if (affordable)
            {
                Require(recipient.Temporary!.Upgrades.Count == (previous.Temporary?.Upgrades.Count ?? 0) + 1,
                    "Paid draw did not upgrade its recipient.");
                paid++;
            }
            else
            {
                Equal(previous.Temporary, recipient.Temporary, "Unaffordable draw must not add an upgrade");
                unpaid++;
            }
            Require(end.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == action.CardInstanceId),
                "Original card did not Consume.");
            plays++;
        }
        Require(plays == 3 && paid == 2 && unpaid == 1 && noops == 3,
            "Expected two paid draws, one unaffordable draw and three empty-target no-ops.");
        CardPlayRule original = initial!.PlayRules!.Cards.Single(rule => rule.DataId == DataId);
        int tests = 0;
        foreach (FixtureValue row in root.GetProperty("SoldierOfFortuneGoldTests").EnumerateArray())
        {
            int gold = row.GetProperty("Gold").GetInt32();
            bool native = row.GetProperty("Returned").GetBoolean();
            var room = CardSpellModel.WithContext(new TrainCombatState(initial.Spawn.Train.Rooms, initial.Spawn.Train.Movement,
                initial.Spawn.Train.EnemySlotsPerRoom, initial.Spawn.Train.Context), initial.Spawn.Train.Context!.WithGold(gold)).Rooms.Single(room => room.RoomIndex == 2);
            Require(native == (gold >= 30) && CardSpellModel.TestPlay(room, original.Effects!, 0).CanPlay,
                "Native affordability boundary or nonmandatory card test differs at " + gold);
            CardActionEffect next = original.Effects![0];
            var mandatory = new CardActionEffect("ScheduleNextDrawUpgrade", next.Target, next.Value, false, false, [],
                tests: new CardEffectTests(true, true, false, false, false), nextDrawUpgrade: next.NextDrawUpgrade, requiredGold: next.RequiredGold);
            Require(CardSpellModel.TestPlay(room, [mandatory], 0).CanPlay == native,
                "Native and modeled individual gold tests differ at " + gold);
            tests++;
        }
        Require(tests == 4, "Missing 0/29/30/60 gold boundaries.");
        TypedDrawNativeChecks.Native(root, required: true);
        RecordedBattleChecks.Native(root);
        Console.WriteLine("SOLDIER-OF-FORTUNE PASS: two original paid and upgraded draws, one unaffordable unupgraded draw, Consume, three empty-target no-ops, four gold boundaries; full state/RNG and 16 independent chains.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
