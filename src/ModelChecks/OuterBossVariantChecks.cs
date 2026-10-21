using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OuterBossVariantChecks
{
    internal static void Fragile(FixtureValue battle, FixtureValue birth, FixtureValue phases, FixtureValue controller)
    {
        const string boss = "Boss_Level8_Fragile";
        OuterBossBirthChecks.Native(birth);
        OuterBossPhaseChecks.Native(phases, boss, earlyDefeat: true);
        BossActionControllerChecks.Native(controller, boss, minimumOperations: 6);
        Require(phases.GetProperty("QueueControls").GetArrayLength() == 0,
            "Fragile baseline must not force the boss phase or health.");
        Battle(battle);
    }

    internal static void Battle(FixtureValue battle)
    {
        const string boss = "Boss_Level8_Fragile";
        OuterBossTurnChecks.Deployment(battle, boss, minimumTurns: 4);
        Require(!battle.GetProperty("NativeWon").GetBoolean() && battle.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            battle.GetProperty("Actions").GetArrayLength() == 0,
            "Fragile baseline requires an unmodified no-card native loss and settled terminal effects.");
        bool damage = false, enemyAura = false;
        foreach (FixtureValue row in battle.GetProperty("Turns").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatUnit? pyre = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.IsPyre);
            damage |= pyre != null && pyre.Health < pyre.MaxHealth;
            enemyAura |= actual.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Team == CombatTeam.Enemy && unit.Status("buff")?.Stacks == 3);
            // Every repeated branch must start from the same immutable native
            // root. This specifically exercises remote Pyre damage with an aura
            // world present, including independent concurrent branches.
            string parent = System.Text.Json.JsonSerializer.Serialize(before, ModelJson.Options);
            Parallel.For(0, 4, _ =>
            {
                BattleTurnResult branch = BattleTurnModel.EndTurn(before);
                Require(branch.Supported && BattleTurnChecks.Comparable(branch.State!, branch.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated) ==
                    BattleTurnChecks.Comparable(actual, branch.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated),
                    "Fragile parallel branch changed complete native state or RNG.");
            });
            Require(System.Text.Json.JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Fragile parallel branches mutated their root.");
        }
        Require(damage && enemyAura, "Missing native remote Pyre damage or the original enemy buff aura.");
        Console.WriteLine("OUTER-BOSS-FRAGILE PASS: original birth, controller, moving casting aura, remote Pyre damage and complete native loss; serial/concurrent full-state and RNG equality.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
