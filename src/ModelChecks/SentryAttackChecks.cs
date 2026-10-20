using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SentryAttackChecks
{
    internal static void Native(FixtureValue root, IReadOnlyCollection<string> originals)
    {
        if (!originals.Contains("a67b8f2a-f3b4-4a8c-8ced-4dc3d4394475")) return;
        int attacks = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatUnit? sentry = before.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.AssetKey == "MonsterSentryTurret");
            if (sentry == null) continue;
            CombatUnit? target = before.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.Id == action.TargetUnitId);
            if (target?.Team != CombatTeam.Enemy) continue;
            CombatUnit moved = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == target.Id);
            CombatUnit next = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == sentry.Id);
            Require(sentry.BaseAttack == 10 && sentry.Health == 5 && next.Health == 3 && next.LastAttackerId == target.Id &&
                sentry.Modifiers!.Sentry is { Targets.Count: 0, IsAttacking: false } &&
                next.Modifiers!.Sentry is { Targets.Count: 0, IsAttacking: false } &&
                (target.Status("armor")?.Stacks ?? 0) - (moved.Status("armor")?.Stacks ?? 0) == 10 && moved.LastAttackerId == sentry.Id,
                "Original Sentry Turret must attack once, take two native retaliation Spikes and clear its private target cache.");
            Require(after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Any(unit => unit.Id == moved.Id),
                "Sentry target must enter the turret's floor.");
            attacks++;
        }
        Require(attacks > 0, "Missing positive original Sentry Turret attack.");
        Console.WriteLine($"ORIGINAL-SENTRY-ATTACK PASS: {attacks} native entry attacks, ten damage/two retaliation Spikes and cleared target/attack caches.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
