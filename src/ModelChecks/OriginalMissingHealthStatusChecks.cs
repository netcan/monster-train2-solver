using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class OriginalMissingHealthStatusChecks
{
    private const string Collector = "53374762-31cd-4947-a65a-426a01c519cc";
    private const string Ability = "02bcd324-4bef-4aa7-93f1-01277973f7c0";
    private const string Oil = "a62b7ef1-efde-4daf-825a-ddf64fbe81f2";
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static IEnumerable<CombatUnit> Units(TrainCombatState state) => state.Rooms.SelectMany(room => room.Units);
    private static int Status(CombatUnit unit, string id) => unit.Status(id)?.Stacks ?? 0;

    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("OriginalMissingHealthStatusPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete original missing-health status battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        var definitions = root.GetProperty("OriginalMissingHealthStatusDefinitions").EnumerateArray().ToArray();
        Require(definitions.Length == 3, "Missing three unchanged original definitions.");
        string[] originalIds = { Collector, Ability, Oil }; int[] costs = { 1, 0, 1 };
        for (int i = 0; i < definitions.Length; i++)
            Require(definitions[i].GetProperty("Id").GetString() == originalIds[i] &&
                definitions[i].GetProperty("Cost").GetInt32() == costs[i] &&
                definitions[i].GetProperty("Rule").Deserialize<CardPlayRule>()!.ExternalInteractions.Count == 0,
                "Original identity, cost or projection differs at " + i);
        var collector = definitions[0].GetProperty("Rule").Deserialize<CardPlayRule>()!.SpawnUnit!;
        Require(collector.BaseAttack == 10 && collector.Health == 5 && collector.Size == 1 &&
            Status(collector, "zeal") == 5 && collector.Ability?.DataId == Ability &&
            collector.Ability.Cooldown == 2 && collector.Ability.CooldownAtSpawn == 0,
            "Original Collector stats, starting zeal or cooldown changed.");
        var skill = definitions[1].GetProperty("Rule").Deserialize<CardPlayRule>()!;
        Require(definitions[1].GetProperty("IsAbility").GetBoolean() && skill.Effects.Count == 2 &&
            skill.Effects[0] is { Type: "AddStatusPerFriendlyMissingHp", Target: "Self", Value: 0,
                FriendlyMissingHealthAllFloors: false, IgnoreDualism: true } &&
            skill.Effects[0].Statuses.Single() is { Id: "pain", Stacks: 1 } &&
            skill.Effects[1] is { Type: "DamagePerCasterStatus", Target: "DropTargetCharacter", Value: 1,
                AllowEnemy: true, AllowPlayer: false, Range: null } && skill.Effects[1].Statuses.Single().Id == "pain",
            "Original skill must accumulate pain, then deal damage from its current counter.");
        var oil = definitions[2].GetProperty("Rule").Deserialize<CardPlayRule>()!.Effects.Single();
        Require(oil is { Type: "AddStatusPerMissingHp", Target: "Room", AllowEnemy: false, AllowPlayer: true } &&
            oil.Statuses.Single() is { Id: "armor", Stacks: 1 }, "Original Oil room/status definition changed.");
        var pain = root.GetProperty("OriginalMissingHealthPainDefinition");
        var painRule = pain.GetProperty("Rule").Deserialize<CombatStatus>()!;
        Require(pain.GetProperty("Type").GetString() == "StatusEffectPainState" && painRule is {
            Id: "pain", Stackable: true, RemoveWhenTriggered: false, RemoveStackAtEnd: false,
            RemoveAllAtEnd: false, RemoveAfterPostCombat: false, RemoveDuringDeployment: false },
            "Pain must be an inert, retained stack counter.");
        int[] ids = root.GetProperty("OriginalMissingHealthStatusIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        int[] skills = { 6, 11, 15, 19 };
        Require(ids.Length == 28 && ids.Count(id => id == 0) == 4 && ids.Where(id => id > 0).Distinct().Count() == 24 &&
            actions.Length == 28, "Missing 24 hand inputs and four original skill activations.");
        BattleTurnState Before(int i) => actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int i) => actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Unit(BattleTurnState state, int card) => Units(state.Spawn.Train).Single(unit => unit.SpawnerCardId == ids[card]);
        int cached = actions[skills[0]].GetProperty("Action").Deserialize<PlayCardAction>()!.CardInstanceId;
        // Starting zeal increases maximum HP without healing: the Collector is initially missing five.
        int[] counts = { 5, 18, 28, 28 };
        for (int i = 0; i < actions.Length; i++)
        {
            var action = actions[i].GetProperty("Action").Deserialize<PlayCardAction>()!;
            Require(action.CardInstanceId == (ids[i] == 0 ? cached : ids[i]), "Changed native input identity at " + i);
        }
        for (int ordinal = 0; ordinal < skills.Length; ordinal++)
        {
            int i = skills[ordinal]; var before = Before(i); var after = After(i); var actor = Unit(after, 2);
            var action = actions[i].GetProperty("Action").Deserialize<PlayCardAction>()!;
            Require(action.ActivatorUnitId == actor.Id && before.Energy == after.Energy && Status(actor, "pain") == counts[ordinal] &&
                Status(actor, "zeal") == 5 && Status(actor, "cooldown") == 2 && actor.MaxHealth == Unit(before, 2).MaxHealth,
                "Original skill counter, cooldown, zero cost or independence from zeal differs at " + i);
            var oldTarget = Units(before.Spawn.Train).Single(unit => unit.Id == action.TargetUnitId);
            var target = Units(after.Spawn.Train).Single(unit => unit.Id == action.TargetUnitId);
            Require(oldTarget.Health + Status(oldTarget, "armor") - target.Health - Status(target, "armor") == counts[ordinal],
                "Skill must damage from the newly accumulated pain at " + i);
        }
        foreach (int i in new[] { 12, 26 })
            Require(Before(i).Spawn.Train.Context!.FindCard(ids[i])!.DataId == Oil && Before(i).Energy - After(i).Energy == 1,
                "Original Oil identity/cost changed.");
        Require(Status(Unit(After(12), 2), "armor") == 5 && Status(Unit(After(12), 3), "armor") == 3 &&
            Status(Unit(After(12), 4), "armor") == 5 && Status(Unit(After(12), 5), "armor") == 0,
            "Oil must use each target's own live missing health and exclude other floors.");
        var pyre = Units(Before(20).Spawn.Train).Single(unit => unit.IsPyre);
        Require(pyre.MaxHealth - pyre.Health == 7 && Unit(Before(20), 5).MaxHealth - Unit(Before(20), 5).Health == 2,
            "Missing independent Pyre and remote-floor tower controls.");
        foreach (int card in new[] { 2, 3, 4 })
        {
            Require(Status(Unit(After(20), card), "armor") - Status(Unit(Before(20), card), "armor") == 6,
                "Tower total must include Pyre 7 plus remote 2, multiply by 2 and divide by 3.");
            Require(Status(Unit(After(21), card), "armor") == Status(Unit(Before(21), card), "armor"),
                "Room aggregate without an activating caster must do nothing.");
            Require(Status(Unit(After(24), card), "armor") - Status(Unit(Before(24), card), "armor") == (card == 3 ? 4 : 0),
                "Target missing-health effect incorrectly used authored count 7 or divisor 99.");
            Require(Status(Unit(After(26), card), "armor") == Status(Unit(Before(26), card), "armor"),
                "Full-health original Oil should add zero stacks.");
        }
        Require(Status(Unit(After(27), 5), "armor") - Status(Unit(Before(27), 5), "armor") == 9,
            "A zero divisor defaults to one and sums every friendly room including the Pyre.");
        foreach (int i in new[] { 12, 24, 26 })
            Require(After(i).Spawn.Train.Context!.Statistics!.Value(ids[i], "AnyStatusEffectStacksAdded", "ThisBattle") == 0,
                "Plain target-missing-health overload must omit played-card status attribution.");
        int friendly = 0, targetEffects = 0, damageEffects = 0;
        foreach (var row in root.GetProperty("MissingHealthStatusEffects").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean(), "Incomplete native missing-health iterator.");
            var before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            if (before.Rooms.Any(room => room.Preview)) continue;
            string type = row.GetProperty("NativeType").GetString()!;
            int actorId = row.GetProperty("ActorId").GetInt32();
            var statuses = row.GetProperty("Statuses").Deserialize<CombatStatus[]>()!;
            string status = statuses.Single().Id;
            int[] targets = row.GetProperty("Targets").Deserialize<int[]>()!;
            int total = 0, increment = 0;
            if (type == "CardEffectAddStatusEffectPerFriendlyMissingHp")
            {
                bool tower = row.GetProperty("AllFloors").GetBoolean();
                var room = before.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == actorId));
                // Independently reproduce the native getter, then compare its directly intercepted result.
                foreach (var scope in before.Rooms.Where(candidate => tower || candidate == room))
                    foreach (var unit in scope.Units.Where(unit => unit.Team == CombatTeam.Player && unit.Health > 0 && unit.DeathState?.IsDestroyed != true))
                        if (unit.MaxHealth > unit.Health) total = unchecked(total + unit.MaxHealth - unit.Health);
                Require(row.GetProperty("NativeTotalMissingHealth").GetInt32() == total,
                    "Direct native friendly missing-health total differs from captured living monster groups.");
                int divisor = row.GetProperty("Value").GetInt32(); if (divisor == 0) divisor = 1;
                increment = total > 0 ? Math.Max(0, unchecked(total * statuses[0].Stacks) / divisor) : 0;
                friendly++;
            }
            else if (type == "CardEffectDamagePerStatusEffect")
            {
                var actor = Units(before).Single(unit => unit.Id == actorId);
                int coefficient = row.GetProperty("SampledCoefficient").GetInt32();
                Require(coefficient == 1, "Changed original native damage coefficient.");
                increment = unchecked(coefficient * Status(actor, status)); damageEffects++;
            }
            else { Require(type == "CardEffectAddStatusEffectPerMissingHp", "Unexpected missing-health observer."); targetEffects++; }
            foreach (int id in targets)
            {
                var old = Units(before).Single(unit => unit.Id == id); var next = Units(actual).Single(unit => unit.Id == id);
                if (type == "CardEffectDamagePerStatusEffect")
                    Require(old.Health + Status(old, "armor") - next.Health - Status(next, "armor") == Math.Max(0, increment),
                        "Direct native status-scaled damage differs.");
                else
                {
                    int added = type == "CardEffectAddStatusEffectPerMissingHp" ? Math.Max(0, old.MaxHealth - old.Health) : increment;
                    Require(Status(next, status) - Status(old, status) == added && next.Health == old.Health && next.MaxHealth == old.MaxHealth,
                        "Direct native missing-health status delta or target order differs.");
                }
            }
        }
        Require(friendly == 7 && targetEffects == 3 && damageEffects == 4,
            "Missing room/tower/null-caster/target-derived/zero damage native observations.");
        foreach (int i in Enumerable.Range(6, 22))
            Require(Before(i).Spawn.Train.Context!.BattleRng.Equals(After(i).Spawn.Train.Context!.BattleRng),
                "Fixed missing-health/status effect consumed gameplay RNG at " + i);
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        BattleSearchChecks.Native(root, requireZeroUnsupported: true);
        Console.WriteLine($"ORIGINAL-MISSING-HEALTH-STATUS PASS: unchanged Collector/Oil, four cached skills (pain 5/18/28/28), {friendly} native friendly totals including Pyre, {targetEffects} per-target effects, {damageEffects} status-scaled damage applications, full chain/16 branches and zero-unsupported serial/parallel search.");
    }
}
