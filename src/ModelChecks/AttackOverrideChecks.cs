using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class AttackOverrideChecks
{
    internal static void Run()
    {
        (CombatStatus[] Statuses, int Expected)[] cases = [
            ([new("buff", 2, 2)], 121),
            ([new("equalizer", 1, 999), new("buff", 2, 2)], 100),
            ([new("armorattack", 1, 999), new("armor", 25, 7), new("equalizer", 1), new("buff", 2, 2)], 25),
            ([new("armorattack", 1), new("equalizer", 1), new("buff", 2, 2)], 0),
            ([new("fixedattack", 30, 999), new("armorattack", 1), new("armor", 25), new("equalizer", 1), new("buff", 2, 2)], 30),
            ([new("fixedattack", 30), new("debuff", 200, 2)], 30),
            ([new("armorattack", 1), new("armor", 25), new("debuff", 200, 2)], 25),
            ([new("equalizer", 1), new("debuff", 200, 2)], 100)
        ];
        foreach (var test in cases)
        {
            var effect = new CombatEffect("CardEffectAddStatusEffectPerSelfAttack", 0, 0, "", 0, [], false,
                action: new("AddStatus", "Self", 2, false, false, [new("regen", 1, 1)]));
            var actor = new CombatUnit(1, "override-actor", CombatTeam.Player, 117, 80, 100, true, false, false, test.Statuses,
                [new("OnHit", false, false, false, 1, [effect], false)],
                modifiers: new(17, 0, 100, 1, 1, true, false, []), isBoss: false, lastAttackerId: 0);
            var victim = new CombatUnit(2, "victim", CombatTeam.Enemy, 0, 1000, 1000, false, false, false, [], [], isBoss: false, lastAttackerId: 0);
            var rng = UnityRng.Seed(83);
            var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10);
            var root = new RoomCombatState(0, false, [actor, victim], [], context);
            string parent = JsonSerializer.Serialize(root);
            RoomCombatResult Attack() => RoomCombatModel.Exchange(root);
            RoomCombatResult result = Attack();
            Require(result.Supported && result.State!.Units.Single(unit => unit.Id == 2).Health == 1000 - test.Expected &&
                actor.Attack == test.Expected && RelicResourceModel.AttackDamage(actor, context) == test.Expected &&
                result.State.Context!.BattleRng.Equals(rng), result.UnsupportedReason ?? "Attack replacement priority, raw stacks or max HP differs.");
            RoomCombatResult scaled = RoomCombatModel.ApplyQueuedCharacterTrigger(root, new(0, actor, "OnHit"), _ => { });
            Require(scaled.Supported && (scaled.State!.Units.Single(unit => unit.Id == 1).Status("regen")?.Stacks ?? 0) == test.Expected / 2 &&
                scaled.State.Context!.BattleRng.Equals(rng), scaled.UnsupportedReason ?? "Self-attack-scaled status ignored replacement attack or consumed RNG.");
            string expected = JsonSerializer.Serialize(result.State);
            Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(Attack().State) == expected, "Attack override branch diverged."));
            Require(JsonSerializer.Serialize(root) == parent && actor.Modifiers!.AttackDamage == 17 && actor.Modifiers.DamageBuff == 100,
                "Attack query destroyed hidden raw attack fields or mutated its parent.");
        }
        Console.WriteLine("ATTACK-OVERRIDE PASS: eight damage/status-scaling controls, fixed > armor > max HP priority, raw stacks, wounded health, positive/negative hidden buffs, unchanged RNG and 32 immutable branches each.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
