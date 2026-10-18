using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalCorruptionDamageChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        for (int index = 0; index < ids.Length; index++)
            if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
            {
                bool damage = index is 3 or 6 or 7 or 9;
                int target = damage ? source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Single(unit => unit.SpawnerCardId == ids[index == 7 ? 2 : 0]).Id : 0;
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] &&
                    play.RoomIndex == (index is 2 or 7 ? 1 : 0) && (target == 0 || play.TargetUnitId == target));
            }
        return null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalCorruptionDamagePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original corruption-damage witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalCorruptionDamageIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 10 && actions.Length == 10, "Missing original corruption damage, fill-to-max or floor controls.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Unit(BattleTurnState state, int owner) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[owner]);
        RoomCapacityState Room(BattleTurnState state, int room) => state.Spawn.Train.Context!.RoomCapacities!.Single(value => value.RoomIndex == room);
        foreach (int index in new[] { 3, 6, 7, 9 })
        {
            BattleTurnState before = Before(index), after = After(index);
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[index])!;
            CardSourceTrait raw = card.MaskDescriptor!.SourceTraits!.Single();
            ScalingDamageTrait scaling = card.DamageScalingTraits!.Single();
            Require(raw.Type == "CardTraitScalingAddDamage" && raw.Int == 1 && raw.Int2 == 0 && raw.Int3 == 0 &&
                !raw.Bool && raw.Str == "" && raw.Float == 1 && raw.Scaling && raw.TrackedValue == "CorruptionInTargetRoom" &&
                raw.EntryDuration == "ThisTurn" && scaling.Query.Type == "CorruptionInTargetRoom" && scaling.Query.Duration == "ThisTurn" &&
                scaling.DamagePerStat == 1 && scaling.Multiplier == 1 && !scaling.AddToDamage,
                "Original Echo Break raw/runtime source parameters differ.");
            CardPlayRule rule = before.PlayRules!.Cards.Single(value => value.DataId == "6730f232-eba5-4b86-b8fc-fdb673d7824d");
            Require(rule.Cost == 1 && rule.CostType == "Default" && rule.ExternalInteractions.Count == 0 && rule.Effects.Single().Value == 0 &&
                before.Energy - after.Energy == 1 && after.Spawn.Train.Context!.FindCard(ids[index])!.LastPlayedCost == 1,
                "Original damage base, cost or paid-cost capture differs.");
            int target = index == 7 ? 2 : 0, damage = index == 6 ? 2 : index == 9 ? 6 : 0;
            Require(Room(before, index == 7 ? 1 : 0).Corruption == damage && Unit(before, target).Health - Unit(after, target).Health == damage,
                "Damage must use the selected floor's current corruption, including zero and fill-to-max.");
            foreach (int owner in new[] { 0, 1, 2 }.Where(value => value != target))
                Require(JsonSerializer.Serialize(Unit(before, owner), ModelJson.Options) == JsonSerializer.Serialize(Unit(after, owner), ModelJson.Options),
                    "Echo Break changed a non-target neighbor or outside-floor unit.");
            Require(Room(before, 0).Corruption == Room(after, 0).Corruption && Room(before, 1).Corruption == Room(after, 1).Corruption,
                "Reading corruption changed room state.");
        }
        Require(Room(After(4), 0).Corruption == 1 && Room(After(5), 0).Corruption == 2 &&
            Room(After(5), 0).MaxCorruption == Room(Before(4), 0).MaxCorruption + 2 &&
            Room(After(8), 0).Corruption == 6 && Room(After(8), 1).Corruption == 0 &&
            Unit(After(8), 0).Status("scorch")?.Stacks == 1 && Unit(After(8), 1).Status("scorch")?.Stacks == 1 &&
            Unit(After(8), 2).Status("scorch") == null, "Original accumulation/fill-to-max or room-only Soul Siphon differs.");
        foreach (int index in new[] { 4, 5, 8 })
            Require(After(index).Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[index]),
                "Original corruption support spell did not Consume.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-CORRUPTION-DAMAGE PASS: unchanged Echo Break at corruption 0/2/0/6, original accumulation/fill-to-max/Consume, selected-floor and neighbor controls, exact source parameters and 16 immutable chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
