using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class AttackTriggerChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(41);
        var card = CardInstanceState.Empty(1, "owner");
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 2, 10,
            statistics: BattleStatistics.Empty().TrackCards([1]), cardInstances: [card], cardRegistry: [card],
            otherPiles: [new("Standby", [new(1, "owner")]), new("Exhausted", [])]);
        CombatTrigger Trigger(string kind, CombatEffect[] effects, bool once = false, bool ignored = true, int threshold = 0) =>
            new(kind, once, false, ignored, 1, effects, false, threshold);
        CombatEffect Gold(int amount) => new("CardEffectRewardGold", amount, 0, "", 0, [], false);
        CombatEffect Action(string type, string target, int amount, bool enemy = true, bool player = false) =>
            new(type == "Damage" ? "CardEffectDamage" : "CardEffectHeal", amount, 0, "", 0, [], false,
                action: new(type, target, amount, enemy, player, []));
        CardUpgradeModifier Upgrade(string id, int damage, int health, int armor) =>
            new(id, id, new(damage: damage, health: health), [new("armor", armor, 1, removeWhenTriggered: true)], false, false, false, 0, 0, []);
        CombatEffect Up(CardUpgradeModifier upgrade, string target = "Self", string lifetime = "TemporaryUntilUnitDeath", bool enemy = false,
            bool player = true, CardTargetFilters? filters = null) =>
            new("CardEffectAddCardUpgradeToUnits", 0, 0, "", 0, [], false,
                unitUpgrade: new("UnitUpgrade", target, 0, enemy, player, [], upgrade, lifetime, filters: filters));
        CombatUnit Unit(int id, CombatTeam team, int attack, int health, int max, CombatTrigger[]? triggers = null,
            CombatStatus[]? statuses = null, int? last = 0, bool boss = false) =>
            new(id, "unit", team, attack, health, max, true, false, false, statuses ?? [], triggers, team == CombatTeam.Player ? 1 : 0,
                1, modifiers: new(attack, 0, 0, 1, 1, true, false, []), isBoss: boss, lastAttackerId: last);
        RoomCombatState Room(params CombatUnit[] units) => new(0, false, units, [], context);
        var preUpgrade = Upgrade("before", 6, 0, 4);
        var afterUpgrade = Upgrade("after", 0, 1, 3);
        var root = Room(Unit(1, CombatTeam.Player, 4, 5, 10,
            [Trigger("OnAttackingBeforeDamage", [Up(preUpgrade), Action("Heal", "LastAttackedCharacter", 3)]),
             Trigger("OnAttacking", [Action("Damage", "LastAttackedCharacter", 1), Up(afterUpgrade, "LastAttackedCharacter", enemy: true, player: false)]),
             Trigger("OnHit", [Gold(5)])], [new("lifesteal", 1, removeWhenTriggered: true)]),
            Unit(4, CombatTeam.Enemy, 0, 9, 20, [Trigger("OnHit", [Gold(1)])],
                [new("armor", 2, 1, removeWhenTriggered: true), new("spikes", 2, 1)]));
        string parent = JsonSerializer.Serialize(root);
        var first = RoomCombatModel.ApplyUnitTurn(root, 1);
        var actor = first.State!.Units.Single(u => u.Id == 1); var victim = first.State.Units.Single(u => u.Id == 4);
        Require(first.Supported && actor.BaseAttack == 10 && actor.Health == 9 && actor.Status("armor")!.Stacks == 2 &&
            victim.Health == 7 && victim.MaxHealth == 21 && victim.Status("armor")!.Stacks == 3 &&
            first.State.Context!.Gold == 15 && first.State.Context.Statistics!.LastAttackDamageDealt == 2 &&
            actor.LastAttackerId == 4 && victim.LastAttackerId == 1 && first.Events.Single(e => e.Kind == "Attack").Amount == 2,
            "Attack before/after phases recomputed cached damage/HP, lost upgrade/heal order, or changed lifesteal/spikes/hit timing: " + JsonSerializer.Serialize(first));

        var armor = RoomCombatModel.ApplyUnitTurn(Room(Unit(1, CombatTeam.Player, 4, 10, 10,
            [Trigger("OnAttackingBeforeDamage", [Up(Upgrade("late-armor", 0, 0, 5), "LastAttackedCharacter", enemy: true, player: false)])]),
            Unit(4, CombatTeam.Enemy, 0, 10, 10, statuses: [new("armor", 2, 1, removeWhenTriggered: true)])), 1);
        Require(armor.Supported && armor.State!.Units.Single(u => u.Id == 4).Health == 8 &&
            armor.State.Units.Single(u => u.Id == 4).Status("armor")!.Stacks == 5, "Pre-damage armor retroactively blocked the computed attack.");

        var sweepRoot = Room(Unit(1, CombatTeam.Player, 4, 10, 10,
            [Trigger("OnAttackingBeforeDamage", [Action("Damage", "LastAttackedCharacter", 1), Gold(1)]),
             Trigger("OnAttacking", [Action("Damage", "LastAttackedCharacter", 1), Gold(2)])], [new("sweep", 1)]),
            Unit(4, CombatTeam.Enemy, 0, 10, 10), Unit(5, CombatTeam.Enemy, 0, 10, 10));
        var sweep = RoomCombatModel.ApplyUnitTurn(sweepRoot, 1);
        Require(sweep.Supported && sweep.State!.Units.Where(u => u.Team == CombatTeam.Enemy).All(u => u.Health == 4 && u.LastAttackerId == 1) &&
            sweep.State.Context!.Gold == 20, "Sweep callback lost its per-hit target override or changed queue order.");
        var shield = RoomCombatModel.ApplyUnitTurn(Room(Unit(1, CombatTeam.Player, 4, 10, 10,
            [Trigger("OnAttackingBeforeDamage", [Gold(1)]), Trigger("OnAttacking", [Gold(2)])]),
            Unit(4, CombatTeam.Enemy, 0, 10, 10, statuses: [new("damage shield", 1, removeWhenTriggered: true)])), 1);
        Require(shield.Supported && shield.State!.Context!.Gold == 10 && shield.State.Units.Single(u => u.Id == 4).Health == 10,
            "A fully blocked attack skipped its attacking callbacks.");
        var stopped = RoomCombatModel.ApplyUnitTurn(Room(Unit(1, CombatTeam.Player, 4, 10, 10,
            [Trigger("OnAttackingBeforeDamage", [Gold(1)]), Trigger("OnAttacking", [Gold(2)])], [new("dazed", 1, removeWhenTriggered: true)]),
            Unit(4, CombatTeam.Enemy, 0, 10, 10)), 1);
        Require(stopped.Supported && stopped.State!.Context!.Gold == 0, "A skipped attack ran attacking callbacks.");
        var silent = RoomCombatModel.ApplyUnitTurn(Room(Unit(1, CombatTeam.Player, 4, 10, 10,
            [Trigger("OnAttackingBeforeDamage", [Gold(1)], ignored: true), Trigger("OnAttacking", [Gold(2)], ignored: false),
             Trigger("OnAttacking", [Gold(99)], threshold: 1)], [new("silenced", 1)]), Unit(4, CombatTeam.Enemy, 0, 10, 10)), 1);
        Require(silent.Supported && silent.State!.Context!.Gold == 5 && !silent.State.Units[0].Triggers[2].HasTriggered,
            "Attack callbacks ignored silence or supplied an HP threshold argument instead of native zero.");

        var fallback = RoomCombatModel.ApplyUnitTurn(Room(Unit(1, CombatTeam.Player, 0, 10, 10,
            [Trigger("OnTurnBegin", [Up(Upgrade("fallback", 0, 0, 2), "LastAttackedCharacter", enemy: true, player: false,
                filters: new("FullHealth", ["missing"], [], true, "missing", []))])]),
            Unit(4, CombatTeam.Enemy, 0, 3, 10, last: 1, boss: true), Unit(5, CombatTeam.Enemy, 0, 10, 10, last: 1),
            Unit(6, CombatTeam.Enemy, 0, 10, 10, last: 0)), 1);
        Require(fallback.Supported && fallback.State!.Units.Count(u => u.Status("armor")?.Stacks == 2) == 2,
            "Last-attacked fallback lost multiple victims or applied filters that native bypasses.");
        var missing = RoomCombatModel.ApplyUnitTurn(Room(Unit(1, CombatTeam.Player, 0, 10, 10,
            [Trigger("OnTurnBegin", [Up(Upgrade("unknown", 0, 0, 2), "LastAttackedCharacter", enemy: true, player: false)])]),
            Unit(4, CombatTeam.Enemy, 0, 10, 10, last: null)), 1);
        Require(!missing.Supported, "Legacy states guessed unknown attacker relationships.");
        var bypass = RoomCombatModel.ApplyUnitTurn(Room(Unit(1, CombatTeam.Player, 4, 10, 10,
            [Trigger("OnAttacking", [Gold(0), Up(Upgrade("override", 0, 0, 2), "LastAttackedCharacter", enemy: false, player: true,
                filters: new("Damaged", ["missing"], [], true, "missing", []))])]), Unit(4, CombatTeam.Enemy, 0, 10, 10, boss: true)), 1);
        Require(bypass.Supported && bypass.State!.Units.Single(u => u.Id == 4).Status("armor")!.Stacks == 2,
            "Per-hit override did not bypass team/health/status/subtype/boss filters.");
        var retained = RoomCombatModel.ApplyUnitTurn(Room(Unit(4, CombatTeam.Enemy, 20, 10, 10,
            [Trigger("OnAttacking", [Up(Upgrade("dying-victim", 1, 2, 3), "LastAttackedCharacter", "Permanent")])]),
            Unit(1, CombatTeam.Player, 0, 1, 10)), 4);
        Require(retained.Supported && retained.State!.Units.Count == 1 && retained.State.Context!.CardInstances!.Single().Permanent.Upgrades.Single().DataId == "dying-victim" &&
            retained.State.Context.Statistics!.MonstersDeadThisBattle == 1, "OnAttacking lost its dying victim/source upgrade or revived it.");

        var dead = Unit(1, CombatTeam.Player, 4, 0, 10,
            [Trigger("OnAttacking", [Gold(1)]), Trigger("OnAttacking", [Gold(2)])]);
        var queued = new RoomCombatModel.QueuedCharacterTrigger(0, dead, "OnAttacking", overrideTarget: Unit(4, CombatTeam.Enemy, 0, 10, 10));
        var deadResult = RoomCombatModel.ApplyQueuedCharacterTrigger(Room(), queued, _ => { });
        Require(deadResult.Supported && deadResult.State!.Context!.Gold == 0 && queued.Unit.Triggers[0].HasTriggered && !queued.Unit.Triggers[1].HasTriggered,
            "Dying attacking callback fired effects or continued past the native early abort.");
        var nonAttack = RoomCombatModel.ApplyPreCombat(Room(Unit(1, CombatTeam.Player, 4, 10, 10,
            [Trigger("PreCombat", [Action("Damage", "Room", 1)]), Trigger("OnAttackingBeforeDamage", [Gold(1)]), Trigger("OnAttacking", [Gold(2)])]),
            Unit(4, CombatTeam.Enemy, 0, 10, 10)), 1);
        Require(nonAttack.Supported && nonAttack.State!.Context!.Gold == 0, "Default effect damage ran direct-attack callbacks.");
        var pyre = new CombatUnit(9, "pyre", CombatTeam.Player, 4, 10, 10, true, true, false, [], lastAttackerId: 0);
        var pyreAttack = RoomCombatModel.ApplyUnitTurn(Room(pyre, Unit(4, CombatTeam.Enemy, 0, 10, 10, last: 1)), 9);
        Require(pyreAttack.Supported && pyreAttack.State!.Units.Single(u => u.Id == 4).LastAttackerId == 0, "Pyre became a retained unit attacker.");
        var history = new TrainCombatState([new(0, false, [Unit(1, CombatTeam.Player, 0, 10, 10, last: 4),
            Unit(5, CombatTeam.Enemy, 0, 10, 10, last: 99), Unit(6, CombatTeam.Enemy, 0, 10, 10, last: null)], [], context),
            new(1, false, [Unit(4, CombatTeam.Enemy, 0, 10, 10)], [], context)], [], 4, context);
        string historyParent = JsonSerializer.Serialize(history);
        var processed = TrainCombatModel.ProcessRemovals(history);
        Require(processed.Rooms[0].Units[0].LastAttackerId == 4 && processed.Rooms[0].Units[1].LastAttackerId == 0 &&
            processed.Rooms[0].Units[2].LastAttackerId == null && JsonSerializer.Serialize(history) == historyParent,
            "Native object destruction cleared a live cross-floor attacker, retained a removed actor, guessed legacy history or mutated its parent.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(RoomCombatModel.ApplyUnitTurn(root, 1)) == JsonSerializer.Serialize(first) &&
            JsonSerializer.Serialize(RoomCombatModel.ApplyUnitTurn(sweepRoot, 1)) == JsonSerializer.Serialize(sweep), "Parallel attack callbacks diverged."));
        Require(JsonSerializer.Serialize(root) == parent, "Attack callback simulation mutated its parent.");
        Console.WriteLine("ATTACK-TRIGGER PASS: cached damage/HP, pre/post upgrades/heals, blocked and skipped attacks, sweep overrides, victim history, filter bypass, dying targets/actors, thresholds/silence, damage types, Pyre and 32 parallel branches.");
    }

    internal static void Native(JsonElement fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out var scenario) || scenario.GetString() != "attack-triggers") return;
        int turns = 0, sweeps = 0, fires = 0, beforeFires = 0, afterFires = 0, dying = 0, thresholds = 0, once = 0, silent = 0, queued = 0;
        foreach (JsonElement turn in fixture.GetProperty("UnitTurns").EnumerateArray())
        {
            var before = turn.GetProperty("Before").Deserialize<RoomCombatState>(ModelJson.Options)!;
            var after = turn.GetProperty("Actual").Deserialize<RoomCombatState>(ModelJson.Options)!;
            int actor = turn.GetProperty("ActorId").GetInt32();
            var result = RoomCombatModel.ApplyUnitTurn(before, actor);
            string? difference = result.Supported ? ModelJson.Difference(JsonSerializer.Serialize(result.State), JsonSerializer.Serialize(after)) : result.UnsupportedReason;
            Require(result.Supported && difference == null && turn.GetProperty("Difference").ValueKind == JsonValueKind.Null &&
                result.Events.Where(e => e.Kind == "Attack" && e.Actor == actor).Select(e => e.Target)
                    .SequenceEqual(turn.GetProperty("AttackedTargetIds").Deserialize<int[]>()!),
                "Independent attack-trigger turn differs at sequence " + turn.GetProperty("Sequence") + ": " + difference);
            turns++; if (turn.GetProperty("AttackedTargetIds").GetArrayLength() > 1) sweeps++;
        }
        foreach (JsonElement record in fixture.GetProperty("AttackTriggers").EnumerateArray())
        {
            Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("GoldAfter").ValueKind == JsonValueKind.Number &&
                record.GetProperty("AfterTriggered").ValueKind == JsonValueKind.Array && record.GetProperty("OverrideTargetId").GetInt32() > 0 &&
                record.GetProperty("ParamInt").GetInt32() == 0, "Native attack-trigger record lost completion, target override or zero argument.");
            if (record.GetProperty("Stage").GetString() == "Queue")
            { if (record.GetProperty("QueueRunning").GetBoolean()) queued++; continue; }
            bool silentActor = record.GetProperty("Silenced").GetBoolean();
            bool halted = record.GetProperty("DeadBoss").GetBoolean();
            bool dead = record.GetProperty("Health").GetInt32() == 0;
            int reward = 0, index = 0;
            foreach (JsonElement trigger in record.GetProperty("Triggers").EnumerateArray())
            {
                bool fired = trigger.GetProperty("HasTriggered").GetBoolean();
                int threshold = trigger.GetProperty("Threshold").GetInt32(), count = trigger.GetProperty("FireCount").GetInt32();
                bool one = trigger.GetProperty("Once").GetBoolean(), ignored = trigger.GetProperty("IgnoreSilence").GetBoolean();
                // This fixture's triggers all include an unconditional reward effect,
                // so preflight cannot fail; thresholds receive native paramInt=0.
                bool passed = !halted && !(one && fired) && threshold <= 0 &&
                    (ignored || record.GetProperty("CanFire").GetBoolean() && !silentActor);
                Require(record.GetProperty("AfterTriggered")[index++].GetBoolean() == (fired || passed),
                    "Native attacking once/silence/death-abort flag differs at sequence " + record.GetProperty("Sequence"));
                if (passed && dead && count > 0) { halted = true; dying++; }
                else if (passed) reward += GoldRewardModel.Adjust(trigger.GetProperty("Gold").GetInt32()) * count;
                if (threshold > 0) thresholds++;
                if (one && fired) once++;
                if (silentActor && !ignored) silent++;
            }
            Require(record.GetProperty("GoldAfter").GetInt32() - record.GetProperty("GoldBefore").GetInt32() == reward,
                "Native attacking reward differs at sequence " + record.GetProperty("Sequence"));
            fires++; if (record.GetProperty("Kind").GetString() == "OnAttackingBeforeDamage") beforeFires++; else afterFires++;
        }
        Require(turns > 10 && sweeps > 0 && fires > 10 && beforeFires > 0 && afterFires > 0 && dying > 0 && thresholds > 0 && once > 0 && silent > 0 && queued > 0,
            $"Native attack-trigger coverage is incomplete: turns={turns}, sweeps={sweeps}, fires={fires}, before={beforeFires}, after={afterFires}, dying={dying}, thresholds={thresholds}, once={once}, silence={silent}, queued={queued}.");
        Console.WriteLine($"NATIVE-ATTACK-TRIGGER PASS: {turns} exact unit turns, {sweeps} sweeps, {fires} dispatches ({beforeFires} before/{afterFires} after), {queued} queued targets, {dying} dying aborts, {thresholds} zero-threshold skips, {once} once skips, {silent} silence gates.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
