using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossPostCombatChecks
{
    internal static void Native(FixtureValue root)
    {
        void Require(bool value, string message) => TrueFinalBossChecks.Require(value, message);
        FixtureValue[] rows = root.GetProperty("UnitPostCombats").EnumerateArray().ToArray();
        Require(rows.Length == 18 && root.GetProperty("OriginalTrueFinalBossRoute").GetProperty("Scenario").GetString() == "Level9TrueFinalBoss_V2",
            "Missing original true-final-boss native post-combat phases.");
        int born = 0, generated = 0, savageryBuffs = 0, pyreDamage = 0;
        var parents = new List<string>(); var inputs = new List<RoomCombatState>(); var expected = new List<string>();
        int PyreHealth(RoomCombatState state) => state.Context!.Enchantments!.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.IsPyre)?.Health ?? 0;
        foreach (FixtureValue row in rows)
        {
            RoomCombatState before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            string parent = JsonSerializer.Serialize(before, ModelJson.Options), target = JsonSerializer.Serialize(actual, ModelJson.Options);
            Require(!before.Preview && before.Context!.Enchantments!.Rooms.SelectMany(room => room.Units)
                .All(unit => unit.Team != CombatTeam.Player || unit.IsPyre), "Post-combat proof must retain the disclosed no-allied-unit native route.");
            RoomCombatResult result = RoomCombatModel.ApplyUnitPostCombat(before,
                row.GetProperty("CannotAttackOrHeal").Deserialize<int[]>()!, row.GetProperty("CannotFireTriggers").Deserialize<int[]>()!);
            string? difference = result.Supported ? ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options), target) : result.UnsupportedReason;
            Require(result.Supported && difference == null && JsonSerializer.Serialize(before, ModelJson.Options) == parent,
                "Native true-final-boss post-combat differs in room " + before.RoomIndex + ": " + difference);
            if (before.Context.LifeLinks != null)
            {
                Require(before.Context.LifeLinks.Actors.Count == 5, "Post-combat omitted canonical linked actors.");
                born++;
            }
            generated += actual.Context!.NextCardId - before.Context.NextCardId;
            CombatUnit? savage = before.Units.FirstOrDefault(unit => unit.AssetKey == TrueFinalBossChecks.Assets[1]);
            if (savage != null) savageryBuffs += actual.Units.Single(unit => unit.Id == savage.Id).Status("buff")?.Stacks - (savage.Status("buff")?.Stacks ?? 0) ?? 0;
            pyreDamage += Math.Max(0, PyreHealth(before) - PyreHealth(actual));
            parents.Add(parent); inputs.Add(before); expected.Add(target);
        }
        Require(born == 17 && generated == 12 && savageryBuffs == 8 && pyreDamage == 20,
            "Missing original scourge generation, Savagery strengthening or remote Pyre damage.");
        Parallel.For(0, 64, index =>
        {
            int selected = index % rows.Length;
            RoomCombatResult branch = RoomCombatModel.ApplyUnitPostCombat(inputs[selected], rows[selected].GetProperty("CannotAttackOrHeal").Deserialize<int[]>()!,
                rows[selected].GetProperty("CannotFireTriggers").Deserialize<int[]>()!);
            Require(branch.Supported && JsonSerializer.Serialize(branch.State, ModelJson.Options) == expected[selected] &&
                JsonSerializer.Serialize(inputs[selected], ModelJson.Options) == parents[selected], "Concurrent post-combat phase changed its parent or result.");
        });
        Require(!TrainCombatModel.ResolveCombat(root.GetProperty("PreCombats")[2].GetProperty("Before").Deserialize<TrainCombatState>()!).Supported,
            "Post-combat calibration must not open uncalibrated full room combat and preview.");
        Console.WriteLine($"TRUE-FINAL-BOSS-POST-COMBAT PASS: {rows.Length} actual phases ({born} born), generated scourges={generated}, Savagery buff stacks={savageryBuffs}, remote Pyre damage={pyreDamage}; full room/context/RNG and 64 immutable concurrent branches; allied-unit callbacks/full battle remain guarded.");
    }
}
