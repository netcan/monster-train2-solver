using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MagneticRelayChecks
{
    private const string DataId = "ee166f41-20e0-4bc5-b238-f4082fe72a17";
    private const string ControlId = "f15d0008-0000-4000-8000-000000000001";
    private const string FullNoOverflowId = "f15d0008-0000-4000-8000-000000000003";
    private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
    internal static PlayCardAction? Choose(BattleTurnState source, int[] cards, int[] units)
    {
        CombatContext context = source.Spawn.Train.Context!; var plays = BattleActionModel.EnumerateSupportedPlays(source);
        var steward = plays.FirstOrDefault(play => play.RoomIndex == 0 && context.FindCard(play.CardInstanceId)!.DataId == StewardId);
        if (steward != null) return steward;
        int Target(int cardId) => source.Spawn.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.SpawnerCardId == cardId)?.Id ?? -1;
        if (context.Statistics!.Value(cards[0], "TimesPlayed", "ThisBattle") == 0)
            return plays.FirstOrDefault(play => play.CardInstanceId == cards[0] && play.TargetUnitId == Target(units[0]));
        CardInstanceState control = context.CardInstances!.First(card => card.DataId == ControlId);
        if (context.Statistics!.Value(control.InstanceId, "TimesPlayed", "ThisBattle") == 0)
            return plays.FirstOrDefault(play => play.CardInstanceId == control.InstanceId && play.TargetUnitId == Target(units[0]));
        CardInstanceState blocked = context.CardInstances!.First(card => card.DataId == FullNoOverflowId);
        if (context.Statistics!.Value(blocked.InstanceId, "TimesPlayed", "ThisBattle") == 0)
            return plays.FirstOrDefault(play => play.CardInstanceId == blocked.InstanceId && play.TargetUnitId == Target(units[1]));
        if (context.Statistics!.Value(cards[1], "TimesPlayed", "ThisBattle") == 0)
            return plays.FirstOrDefault(play => play.CardInstanceId == cards[1] && play.TargetUnitId == Target(units[1]));
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source, card => card.DataId != DataId && card.DataId != ControlId && card.DataId != FullNoOverflowId);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("MagneticRelayPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed recall witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int original = 0, overflow = 0, blockedCount = 0;
        foreach (FixtureValue row in root.GetProperty("MagneticRelayRecords").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("TestReturned").GetBoolean(), "Incomplete recall/Test.");
            var before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var after = row.GetProperty("After").Deserialize<TrainCombatState>()!;
            var rule = row.GetProperty("Rule").Deserialize<UnitReturnRule>()!;
            int targetId = row.GetProperty("TargetId").GetInt32(), sourceId = row.GetProperty("CardId").GetInt32();
            CombatUnit unit = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == targetId);
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                var result = UnitReturnModel.Apply(before, targetId, rule);
                Require(result.Supported, result.UnsupportedReason ?? "Recall unsupported.");
                Equal(result.State, after, "complete native recall state/RNG");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Recall mutated its parent.");
            if (!rule.OverflowToDraw)
            {
                Require(before.Context!.Cards.Hand.Count == before.Context.MaxHandSize, "Missing full-hand refusal.");
                Equal(before, after, "Full-hand recall without overflow must preserve every state/RNG field");
                blockedCount++; continue;
            }
            Require(after.Rooms.SelectMany(room => room.Units).All(other => other.Id != targetId) &&
                before.Context!.Gold == after.Context!.Gold && before.Context.NextCardId == after.Context.NextCardId,
                "Recall failed to remove the unit, allocated an identity or fired death-gold.");
            CardInstanceState returned = after.Context.FindCard(unit.SpawnerCardId)!;
            Require(returned.Permanent.Upgrades.Count == 1, "Recall lost the permanent source upgrade.");
            if (before.Context.FindCard(sourceId)!.DataId == DataId)
            {
                Require(rule.KeepTemporaryUpgrades && rule.OverflowToDraw && rule.ReturnEquipment &&
                    after.Context.Cards.Hand[0].InstanceId == unit.SpawnerCardId && returned.Temporary.Upgrades.Count == 1 &&
                    returned.Temporary.Offsets.Damage == 4 && returned.Temporary.Offsets.Cost == 1,
                    "Original recall did not retain the upgrade and return the same spawner to hand."); original++;
            }
            else
            {
                Require(before.Context.Cards.Hand.Count == before.Context.MaxHandSize && !rule.KeepTemporaryUpgrades &&
                    after.Context.Cards.Draw.Last().InstanceId == unit.SpawnerCardId && returned.Temporary.Upgrades.Count == 0 &&
                    returned.Temporary.Offsets.Damage == 0 && returned.Temporary.Offsets.Cost == 0 && returned.Temporary.PersistentHealth == 0,
                    "Full-hand recall failed to clear temporary upgrades or overflow to draw top."); overflow++;
            }
        }
        Require(original == 2 && overflow == 1 && blockedCount == 1, "Missing original recall/replay, overflow/clear or full-hand refusal controls.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("MAGNETIC-RELAY PASS: two original recalls before Kill, same-source replay, upgrade retention, full-hand draw overflow/temporary clear and immutable parallel native contexts.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
