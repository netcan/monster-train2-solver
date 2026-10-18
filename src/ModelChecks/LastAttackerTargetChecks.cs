using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class LastAttackerTargetChecks
{
    internal static void Run()
    {
        RoomCombatState Root(int? attackerId = 2, bool? boss = false, bool ignoreBosses = false, CombatTeam team = CombatTeam.Enemy)
        {
            var effect = new CombatEffect("CardEffectAddStatusEffect", 0, 0, "", 0, [], false,
                action: new("AddStatus", "LastAttackerCharacter", 0, false, false, [new("regen", 3, 1)],
                    tests: new(true, false, false, false), filters: new("native-bypassed", ["missing"], ["regen"], ignoreBosses, "missing-subtype", ["other-subtype"])));
            var actor = new CombatUnit(1, "reactor", CombatTeam.Player, 0, 20, 20, false, false, false, [],
                [new("OnHit", false, false, false, 1, [effect], false)], modifiers: new(0, 0, 0, 1, 1, true, false, []), isBoss: false, lastAttackerId: attackerId);
            var attacker = new CombatUnit(2, "attacker", team, 1, 20, 20, true, false, false, [], [],
                modifiers: new(1, 0, 0, 1, 1, true, false, []), isBoss: boss, lastAttackerId: 0);
            var rng = UnityRng.Seed(71);
            return new(0, false, [actor, attacker], [], new(new([], [], [], rng, 0, []), rng, 0, 1, 10));
        }
        RoomCombatResult Fire(RoomCombatState state) => RoomCombatModel.ApplyQueuedCharacterTrigger(state,
            new(0, state.Units.Single(unit => unit.Id == 1), "OnHit"), _ => { });
        foreach (CombatTeam team in new[] { CombatTeam.Player, CombatTeam.Enemy })
        {
            RoomCombatState root = Root(team: team);
            string parent = JsonSerializer.Serialize(root);
            RoomCombatResult hit = Fire(root);
            Require(hit.Supported && hit.State!.Units.Single(unit => unit.Id == 2).Status("regen")?.Stacks == 3 &&
                hit.State.Units.Single(unit => unit.Id == 1).Statuses.Count == 0 && hit.State.Context!.BattleRng.Equals(root.Context!.BattleRng),
                hit.UnsupportedReason ?? "Last attacker must bypass team, full-health, status and subtype filters without RNG.");
            string expected = JsonSerializer.Serialize(hit.State);
            Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(Fire(root).State) == expected, "Last-attacker branches diverged."));
            Require(JsonSerializer.Serialize(root) == parent, "Last-attacker selection mutated its parent.");
        }
        Require(Fire(Root(ignoreBosses: true)).State!.Units.Single(unit => unit.Id == 2).Status("regen")?.Stacks == 3,
            "IgnoreBosses excluded a known nonboss attacker.");
        RoomCombatResult boss = Fire(Root(boss: true, ignoreBosses: true));
        Require(boss.Supported && boss.State!.Units.All(unit => unit.Statuses.Count == 0), "IgnoreBosses selected a boss attacker.");
        Require(Fire(Root(boss: null)).Supported && !Fire(Root(boss: null, ignoreBosses: true)).Supported,
            "Last-attacker boss classification must be required only when the native boss gate is active.");
        RoomCombatResult absent = Fire(Root(attackerId: 0));
        Require(absent.Supported && absent.State!.Units.All(unit => unit.Statuses.Count == 0), "Known absent attacker was inferred from team order.");
        Require(!Fire(Root(attackerId: null)).Supported && !Fire(Root(attackerId: 99)).Supported,
            "Missing attacker relationships or an incomplete actor world were guessed.");
        Console.WriteLine("LAST-ATTACKER PASS: exact captured relationship, team/health/status/subtype bypass, boss gate, known-none/missing world guards, fixed RNG and 32 immutable branches per team.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
