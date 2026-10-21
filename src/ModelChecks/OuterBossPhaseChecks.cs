using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OuterBossPhaseChecks
{
    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 1 &&
            fixture.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            fixture.GetProperty("Boundary").GetString() == "AfterBossActionRemoveDeadCharacters" &&
            fixture.GetProperty("Errors").GetArrayLength() == 0, "Native outer-boss phase build or settled boundary differs.");
        int count = 0, pre = 0, post = 0, moves = 0, initial = 0, auraTargets = 0;
        bool occupied = false;
        foreach (FixtureValue row in fixture.GetProperty("Actions").EnumerateArray())
        {
            OuterBossPhaseState before = row.GetProperty("Before").Deserialize<OuterBossPhaseState>()!;
            OuterBossPhaseState actual = row.GetProperty("Actual").Deserialize<OuterBossPhaseState>()!;
            string phase = row.GetProperty("Phase").GetString()!;
            if (count == 0) occupied = before.Battle.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Team == CombatTeam.Player && !unit.IsPyre);
            Require(row.GetProperty("NativeReturned").GetBoolean() && phase is "BossActionPreCombat" or "BossActionPostCombat",
                "Missing completed native outer-boss action.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            OuterBossPhaseResult predicted = OuterBossPhaseModel.Apply(before, phase == "BossActionPreCombat");
            Require(predicted.Supported, "Outer-boss phase unsupported at " + count + ": " + predicted.UnsupportedReason);
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted.State, ModelJson.Options),
                JsonSerializer.Serialize(actual, ModelJson.Options));
            Require(difference == null, "Outer-boss phase mismatch at " + count + ": " + difference);
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Outer-boss phase changed its model parent.");
            if (phase == "BossActionPreCombat") pre++; else post++;
            if (before.Boss.Room != actual.Boss.Room) moves++;
            if (before.Boss.Controller.InitialAction != null) initial++;
            CombatUnit beforeBoss = before.Battle.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == before.Boss.UnitId);
            CombatUnit afterBoss = actual.Battle.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == actual.Boss.UnitId);
            var previousTargets = beforeBoss.Triggers.SelectMany(trigger => trigger.Effects).Where(effect => effect.Enchantment != null)
                .SelectMany(effect => effect.Enchantment!.State.PrimaryTargets).Select(target => target.UnitId).ToHashSet();
            var appliedTargets = afterBoss.Triggers.SelectMany(trigger => trigger.Effects).Where(effect => effect.Enchantment != null)
                .SelectMany(effect => effect.Enchantment!.State.PrimaryTargets).Where(target => target.IsEnchanted && !previousTargets.Contains(target.UnitId))
                .Select(target => target.UnitId).ToHashSet();
            auraTargets += actual.Battle.Spawn.Train.Rooms.SelectMany(room => room.Units).Count(unit =>
                appliedTargets.Contains(unit.Id) && unit.Team == CombatTeam.Player && unit.Status("debuff")?.Stacks == 5);
            count++;
        }
        Require(count >= (occupied ? 9 : 11) && pre >= (occupied ? 5 : 6) && post >= (occupied ? 4 : 5) &&
            moves >= (occupied ? 4 : 5) && initial == 1 && (!occupied || auraTargets > 0),
            "Missing native initial action, moving casting actions or post-combat no-op controls.");
        Console.WriteLine($"OUTER-BOSS-PHASE PASS: {count} settled native actions ({pre} pre, {post} post), {moves} moves, {initial} initial latch, {auraTargets} new live aura targets; complete battle/controller/RNG equality and immutable parents.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
