using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PreventMonsterDamageChecks
{
    internal static void Native(FixtureValue witness)
    {
        Require(witness.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            witness.GetProperty("Effect").GetString() == RelicModel.PreventMonsterDamage &&
            witness.GetProperty("FrameBefore").ContentEquals(witness.GetProperty("FrameAfter")) &&
            witness.GetProperty("RngBefore").ContentEquals(witness.GetProperty("RngAfter")) &&
            witness.GetProperty("HealthBefore").ContentEquals(witness.GetProperty("HealthAfter")),
            "Native damage query changed frame, health or RNG.");
        var owner = new CombatRelicState(witness.GetProperty("OwnerId").GetString()!,
            witness.GetProperty("OwnerKey").GetString()!, [RelicModel.PreventMonsterDamage]);
        Require(RelicModel.Validate([owner]) == null && RelicModel.Validate([
            new("different-owner", owner.AssetKey, owner.EffectTypes)]) != null, "Native owner validation changed.");
        var rows = witness.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == 10 && rows.Select(row => (row.GetProperty("Kind").GetString(), row.GetProperty("Requested").GetInt32()))
            .SequenceEqual(from kind in new[] { "FriendlyToEnemy", "EnemyToFriendly", "PyreToEnemy", "NoAttackerToEnemy", "FriendlyToFriendly" }
                from damage in new[] { 7, -3 } select ((string?)kind, damage)), "Native damage control rows are incomplete.");
        foreach (FixtureValue row in rows)
        {
            int requested = row.GetProperty("Requested").GetInt32();
            int adjusted = row.GetProperty("Kind").GetString() == "FriendlyToEnemy" ? 0 : requested;
            Require(row.GetProperty("BeforeOwner").GetInt32() == requested && row.GetProperty("WithOwner").GetInt32() == adjusted &&
                row.GetProperty("AfterStatusHook").GetInt32() == requested, "Native signed damage adjustment changed.");
        }

        FixtureValue metadata = witness.GetProperty("LiveDamage");
        int health = metadata.GetProperty("Health").GetInt32();
        UnityRng rng = UnityRng.Seed(734);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10,
            statistics: BattleStatistics.Empty(), relics: [owner]);
        FixtureValue[] actors = witness.GetProperty("Actors").EnumerateArray().ToArray();
        CombatUnit[] units = actors.Select((actor, index) => new CombatUnit(index + 1, actor.GetProperty("AssetKey").GetString()!,
            actor.GetProperty("Team").GetString() == "Monsters" ? CombatTeam.Player : CombatTeam.Enemy,
            0, health, health, canAttack: false, isPyre: actor.GetProperty("Pyre").GetBoolean(), endsBattleOnDeath: false, [])).ToArray();
        var room = new RoomCombatState(0, false, units, [], context);
        CheckDamage(room, 2, 1, metadata.GetProperty("AfterFriendly").GetInt32());
        CheckDamage(room, 2, 0, metadata.GetProperty("AfterAttackerless").GetInt32());
        CheckDamage(room, 2, 3, health - 1);
        CheckDamage(room, 1, 2, health - 1);
        CheckDamage(room, 1, 1, health - 1);

        FixtureValue marked = witness.GetProperty("NaturalPyregel");
        Require(marked.ValueKind != FixtureKind.Null, "Expected a live original enemy with Pyregel.");
        int stacks = marked.GetProperty("Stacks").GetInt32();
        int markedHealth = marked.GetProperty("HealthBefore").GetInt32();
        CombatUnit pyregelTarget = new(2, units[1].AssetKey, CombatTeam.Enemy, 0, markedHealth, markedHealth,
            false, false, false, [new CombatStatus("pyregel", stacks, 1)]);
        CheckDamage(new(0, false, [units[0], pyregelTarget], [], context), 2, 1, marked.GetProperty("HealthAfter").GetInt32());
        Console.WriteLine("NATIVE-PREVENT-MONSTER-DAMAGE PASS: signed original queries, live damage and Pyregel, model attacker controls and immutable RNG.");

        void CheckDamage(RoomCombatState source, int target, int attacker, int expectedHealth)
        {
            using var keys = new BattleStateKeys();
            string parent = keys.SnapshotKey(source);
            RoomCombatResult result = RoomCombatModel.ApplyCardDamage(source, target, 1, attackerUnitId: attacker);
            Require(result.Supported && result.State!.Units.Single(unit => unit.Id == target).Health == expectedHealth &&
                result.State.Context!.BattleRng.Equals(rng) && keys.SnapshotKey(source) == parent,
                result.UnsupportedReason ?? "Model damage, RNG or parent differs from the native scope.");
        }
    }

    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidDataException(message);
    }
}
