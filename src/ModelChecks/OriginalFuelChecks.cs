using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalFuelChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 3 && source.Spawn.Turn < 1 || index == 10 && source.Spawn.Turn < 2 || index >= 11 && source.Spawn.Turn < 3) return null;
        int target = index is 7 or 8 or 9 or 11 or 12 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[6]).Id : 0;
        var action = new PlayCardAction(ids[index], index < 3 ? index : 0, playerPosition: index < 3 || index == 6 ? 0 : index == 10 ? 2 : -1, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalFuelPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original fuel witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalFuelIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 13 && actions.Length == 13, "Missing original Construct, fuel exhaustion/refill or feeding controls.");
        BattleTurnState Before(int i) => actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int i) => actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Construct(TrainCombatState state) => state.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[6]);
        CombatUnit Guard(TrainCombatState state) => state.Rooms[0].Units
            .Where(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack" && unit.MaxHealth >= 1000)
            .Single();
        int Vitality(CombatUnit unit) => unit.Health + (unit.Status("armor")?.Stacks ?? 0);
        var birth = After(6); var rule = Before(6).PlayRules!.Cards.Single(card => card.DataId == "3db9cb2c-3495-47a7-983e-d717a206460c");
        Require(Before(6).Spawn.Train.Context!.FindCard(ids[6])!.DataId == rule.DataId && rule.ExternalInteractions.Count == 0 &&
            Before(6).Energy - birth.Energy == rule.Cost && Construct(birth.Spawn.Train).BaseAttack == 25 &&
            Construct(birth.Spawn.Train).MaxHealth == 25 && Construct(birth.Spawn.Train).Status("spark")?.Stacks == 2 &&
            Construct(birth.Spawn.Train).Status("inert")?.Stacks == 1 && Construct(birth.Spawn.Train).Attacks == 2,
            "Original Alloyed Construct identity, paid cost, stats or native starting statuses differ.");
        foreach (int i in new[] { 7, 8, 9 })
        {
            var before = Before(i).Spawn.Train; var after = After(i).Spawn.Train;
            Require((Construct(before).Status("spark")?.Stacks ?? 0) == Math.Max(0, 9 - i) &&
                (Construct(after).Status("spark")?.Stacks ?? 0) == Math.Max(0, 8 - i) && Vitality(Guard(before)) - Vitality(Guard(after)) == 50,
                "Native victim damage must prove two/one/zero-fuel forced Multistrike attacks and one-stack consumption.");
        }
        Require(Before(10).Spawn.Train.Context!.FindCard(ids[10])!.DataId == "0347419a-2e3a-492d-a054-f6e340be3db2" &&
            Before(10).Spawn.Turn == 2 && After(10).Spawn.Train.Rooms[0].Units.Any(unit => unit.SpawnerCardId == ids[10]),
            "Original Shadow Morsel must be held until the empty-fuel second turn.");
        Require((Construct(Before(12).Spawn.Train).Status("spark")?.Stacks ?? 0) == 1 &&
            Construct(After(12).Spawn.Train).Status("spark")?.Stacks == 2,
            "Selected refill must add one positive fuel stack before normal combat.");
        foreach (int turn in new[] { 1, 2, 3, 4 })
        {
            var phase = root.GetProperty("TrainPhases").EnumerateArray().Single(row => row.GetProperty("Turn").GetInt32() == turn && row.GetProperty("Kind").GetString() == "Combat");
            var before = phase.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var after = phase.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            CombatUnit attacker = Construct(before);
            CombatUnit? remaining = after.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.SpawnerCardId == ids[6]);
            int expectedDamage = turn is 3 or 4 ? 50 : 0;
            int expectedFuel = turn switch { 2 or 3 => 1, _ => 0 };
            Require(Vitality(Guard(before)) - Vitality(Guard(after)) == expectedDamage &&
                (remaining?.Status("spark")?.Stacks ?? 0) == expectedFuel &&
                (turn != 3 || attacker.Status("spark")?.Stacks == 2) &&
                (turn != 4 || attacker.Status("spark")?.Stacks == 1),
                "Native normal combat must block empty fuel, then consume the fed/refilled fuel once per turn.");
            if (turn == 2) Require(Construct(after).MaxHealth == 34 && !after.Rooms[0].Units.Any(unit => unit.SpawnerCardId == ids[10]),
                "Original Morsel must be eaten, positively raise maximum HP and refuel the existing Construct.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-FUEL PASS: unchanged Alloyed Construct, native victim damage proves 2/1/0 forced turns, empty-fuel blocks, original feeding/refill last-stack Multistrike and 16 immutable full chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
