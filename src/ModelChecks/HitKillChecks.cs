using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class HitKillChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(110);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 4, 10,
            statistics: BattleStatistics.Empty().TrackCards([1, 2, 3]),
            cardInstances: [CardInstanceState.Empty(1, "owner"), CardInstanceState.Empty(2, "second"), CardInstanceState.Empty(3, "third")],
            otherPiles: [new("Standby", [new(1, "owner"), new(2, "second"), new(3, "third")]), new("Exhausted", [])]);
        CombatEffect Gold(int amount) => new("CardEffectRewardGold", amount * 5, 0, "", 0, [], false);
        CombatEffect Heal(int amount, string target = "Self", bool enemies = false) => new("CardEffectHeal", amount, 0, "", 0, [], false,
            action: new("Heal", target, amount, enemies, !enemies, []));
        CombatEffect Damage(int amount, string target = "Room", bool enemies = true) => new("CardEffectDamage", amount, 0, "", 0, [], false,
            action: new("Damage", target, amount, enemies, !enemies, []));
        CombatTrigger Trigger(string kind, CombatEffect[] effects, bool once = false, bool ignored = false, int threshold = 0) =>
            new(kind, once, false, ignored, 1, effects, false, threshold);
        CombatUnit Unit(int id, CombatTeam team, int attack, int hp, CombatTrigger[] triggers, CombatStatus[]? statuses = null, bool boss = false, int max = 10) =>
            new(id, "unit", team, attack, hp, max, true, false, boss, statuses ?? [], triggers,
                team == CombatTeam.Player ? id : 0, modifiers: new(attack, 0, 0, 1, 1, true, false, []), isBoss: boss);
        RoomCombatState Room(params CombatUnit[] units) => new(0, false, units, [], context);
        var revenge = new[] { Trigger("OnHit", [Gold(1)]), Trigger("OnHit", [Gold(2)], threshold: 2) };
        var blocked = RoomCombatModel.ApplyCardDamage(Room(Unit(1, CombatTeam.Player, 0, 10, revenge,
            [new("armor", 5, 1, removeWhenTriggered: true)])), 1, 3);
        Require(blocked.Supported && blocked.State!.Units.Single().Health == 10 && blocked.State.Context!.Gold == 5 &&
            !blocked.State.Units.Single().Triggers[1].HasTriggered, "Armor-blocked hit lost Revenge or passed its HP-damage threshold.");
        var shielded = RoomCombatModel.ApplyCardDamage(Room(Unit(1, CombatTeam.Player, 0, 10, revenge,
            [new("damage shield", 1, removeWhenTriggered: true)])), 1, 3);
        Require(shielded.Supported && shielded.State!.Context!.Gold == 5, "Shield-blocked damage did not trigger Revenge.");
        var wounded = RoomCombatModel.ApplyCardDamage(Room(Unit(1, CombatTeam.Player, 0, 10, revenge)), 1, 3);
        Require(wounded.Supported && wounded.State!.Context!.Gold == 15, "Positive HP damage did not satisfy the trigger threshold.");
        var zero = RoomCombatModel.ApplyCardDamage(Room(Unit(1, CombatTeam.Player, 0, 10, revenge)), 1, 0);
        Require(zero.Supported && zero.State!.Context!.Gold == 0 && zero.State.Units.Single().Triggers.All(t => !t.HasTriggered),
            "An empty zero-damage hit triggered Revenge.");
        var lethal = RoomCombatModel.ApplyCardDamage(Room(Unit(1, CombatTeam.Player, 0, 2,
            [Trigger("OnHit", [Heal(99), Gold(3)])])), 1, 3);
        Require(lethal.Supported && lethal.State!.Units.Count == 0 && lethal.State.Context!.Gold == 15 &&
            !lethal.Events.Any(e => e.Kind == "TriggeredHeal"), "Lethal Revenge was skipped or healing revived a dying actor.");
        var boss = RoomCombatModel.ApplyCardDamage(Room(Unit(4, CombatTeam.Enemy, 0, 2,
            [Trigger("OnHit", [Gold(3)]), Trigger("OnDeath", [Gold(1)])], boss: true)), 4, 3);
        Require(boss.Supported && boss.State!.Context!.Gold == 5, "A dead boss fired Revenge or lost its death trigger.");

        var ordering = RoomCombatModel.ApplyUnitTurn(Room(
            Unit(1, CombatTeam.Player, 4, 2, [Trigger("OnKill", [Gold(2)]), Trigger("OnHit", [Gold(3)]), Trigger("OnDeath", [Gold(5)])],
                [new("lifesteal", 1, removeWhenTriggered: true)]),
            Unit(4, CombatTeam.Enemy, 0, 2, [Trigger("OnKill", [Gold(4)]), Trigger("OnHit", [Gold(1)]), Trigger("OnDeath", [Gold(6)])],
                [new("spikes", 9, 1)])), 1);
        Require(ordering.Supported && ordering.State!.Units.Count == 0 &&
            ordering.Events.Where(e => e.Kind == "Gold").Select(e => e.Amount).SequenceEqual([10, 20, 15, 25, 5, 30]) &&
            ordering.Events.Count(e => e.Kind == "Death") == 2, "Slay/lifesteal/spikes/Revenge/death ordering differs.");

        var sweepRoot = Room(Unit(1, CombatTeam.Player, 4, 10, [Trigger("OnKill", [Gold(2)])], [new("sweep", 1)]),
            Unit(4, CombatTeam.Enemy, 0, 3, [Trigger("OnHit", [Heal(2, "Room", true)])]), Unit(5, CombatTeam.Enemy, 0, 6, [], max: 6));
        string parent = JsonSerializer.Serialize(sweepRoot);
        var sweep = RoomCombatModel.ApplyUnitTurn(sweepRoot, 1);
        Require(sweep.Supported && sweep.State!.Units.Single(u => u.Id == 5).Health == 4 &&
            sweep.Events.Where(e => e.Kind == "Attack" || e.Kind == "Gold" || e.Kind == "TriggeredHeal").Select(e => e.Kind)
                .SequenceEqual(["Attack", "Attack", "Gold", "TriggeredHeal"]), "Sweep callbacks ran between targets instead of after the whole strike.");
        var deadSlayer = RoomCombatModel.ApplyUnitTurn(Room(Unit(1, CombatTeam.Player, 4, 2,
            [Trigger("OnKill", [Heal(99), Gold(2)]), Trigger("OnHit", [Gold(1)])], [new("sweep", 1)]),
            Unit(4, CombatTeam.Enemy, 0, 3, [], [new("spikes", 9, 1)]), Unit(5, CombatTeam.Enemy, 0, 6, [])), 1);
        Require(deadSlayer.Supported && deadSlayer.State!.Context!.Gold == 15 && deadSlayer.State.Units.Single().Health == 2,
            "A queued Slay on a dying sweep attacker was skipped, revived it, or lost a pending target.");

        var phaseRoot = Room(Unit(1, CombatTeam.Player, 0, 2,
            [Trigger("PreCombat", [Damage(4), Damage(4, "Self")]), Trigger("OnKill", [Gold(2)], once: true),
             Trigger("OnHit", [Gold(4)]), Trigger("OnDeath", [Gold(5)])]),
            Unit(4, CombatTeam.Enemy, 0, 3, [Trigger("OnHit", [Gold(3)])]), Unit(5, CombatTeam.Enemy, 0, 3, [Trigger("OnHit", [Gold(3)])]));
        var phase = RoomCombatModel.ApplyPreCombat(phaseRoot, 1);
        Require(phase.Supported && phase.State!.Units.Count == 0 && phase.State.Context!.Gold == 85 &&
            phase.Events.Count(e => e.Kind == "Gold" && e.Amount == 10) == 1, "Global callback dispatch lost the dying actor or repeated once-only Slay.");
        var silent = RoomCombatModel.ApplyCardDamage(Room(Unit(1, CombatTeam.Player, 0, 10,
            [Trigger("OnHit", [Gold(2)]), Trigger("OnHit", [Gold(1)], ignored: true, threshold: -1)], [new("silenced", 1)])), 1, 1);
        Require(silent.Supported && silent.State!.Context!.Gold == 5, "Silence/ignored-silence or nonpositive threshold differs.");
        var previewRoot = Room(Unit(1, CombatTeam.Player, 0, 10, revenge));
        var preview = RoomCombatModel.ApplyCardDamage(new(0, false, previewRoot.Units, [], context, preview: true), 1, 3);
        Require(preview.Supported && preview.State!.Context!.Gold == 0 && previewRoot.Units.Single().Triggers.All(t => !t.HasTriggered),
            "Preview Revenge granted live gold or changed its parent's once flags.");
        var deployment = RoomCombatModel.ApplyCardDamage(new(0, true,
            [Unit(1, CombatTeam.Player, 0, 10, [new("OnHit", false, false, true, 1, [Gold(1)], true, 0)])], [], context), 1, 1);
        Require(deployment.Supported && deployment.State!.Context!.Gold == 0, "A disallowed deployment trigger fired.");
        Require(!RoomCombatModel.ApplyCardHeal(Room(Unit(1, CombatTeam.Player, 0, 5,
            [Trigger("OnHeal", [Gold(1)], threshold: 2)])), 1, 3).Supported,
            "An unmodeled threshold argument on another trigger kind was silently accepted.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(RoomCombatModel.ApplyUnitTurn(sweepRoot, 1)) == JsonSerializer.Serialize(sweep) &&
            JsonSerializer.Serialize(RoomCombatModel.ApplyPreCombat(phaseRoot, 1)) == JsonSerializer.Serialize(phase), "Parallel hit/kill branches differ."));
        Require(JsonSerializer.Serialize(sweepRoot) == parent, "Hit/kill simulation mutated its parent.");
        Console.WriteLine("HIT-KILL PASS: blocked/zero/lethal damage, HP thresholds, dead-boss suppression, Slay/lifesteal/spikes order, sweep FIFO, dying actors, once/silence and 32 parallel branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out var scenario) || scenario.GetString() != "hit-kill") return;
        int turns = 0, sweeps = 0, fires = 0, blocked = 0, lethal = 0, slays = 0, positiveThresholds = 0, skippedBosses = 0;
        foreach (FixtureValue turn in fixture.GetProperty("UnitTurns").EnumerateArray())
        {
            var before = turn.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = turn.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            RoomCombatResult result = RoomCombatModel.ApplyUnitTurn(before, turn.GetProperty("ActorId").GetInt32());
            Require(result.Supported && turn.GetProperty("Difference").ValueKind == FixtureKind.Null &&
                ModelJson.Difference(JsonSerializer.Serialize(result.State), JsonSerializer.Serialize(after)) == null &&
                result.Events.Where(e => e.Kind == "Attack" && e.Actor == turn.GetProperty("ActorId").GetInt32()).Select(e => e.Target)
                    .SequenceEqual(turn.GetProperty("AttackedTargetIds").Deserialize<int[]>()!),
                "Independent native hit/kill unit turn differs: " + result.UnsupportedReason);
            turns++; if (turn.GetProperty("AttackedTargetIds").GetArrayLength() > 1) sweeps++;
        }
        foreach (FixtureValue record in fixture.GetProperty("HitKills").EnumerateArray())
        {
            Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("GoldAfter").ValueKind == FixtureKind.Number &&
                record.GetProperty("AfterTriggered").ValueKind == FixtureKind.Array, "Native hit/kill observation is incomplete.");
            if (record.GetProperty("Stage").GetString() != "Fire") continue;
            int amount = record.GetProperty("ParamInt").GetInt32(), reward = 0, index = 0;
            bool deadBoss = record.GetProperty("DeadBoss").GetBoolean(), silent = record.GetProperty("Silenced").GetBoolean();
            foreach (FixtureValue trigger in record.GetProperty("Triggers").EnumerateArray())
            {
                bool wasFired = trigger.GetProperty("HasTriggered").GetBoolean();
                int threshold = trigger.GetProperty("Threshold").GetInt32();
                bool passed = !deadBoss && (!trigger.GetProperty("Once").GetBoolean() || !wasFired) &&
                    (trigger.GetProperty("IgnoreSilence").GetBoolean() || record.GetProperty("CanFire").GetBoolean() && !silent) &&
                    (threshold <= 0 || amount >= threshold);
                if (passed) reward += GoldRewardModel.Adjust(trigger.GetProperty("Gold").GetInt32()) * trigger.GetProperty("FireCount").GetInt32();
                Require(record.GetProperty("AfterTriggered")[index++].GetBoolean() == (wasFired || passed),
                    "Native hit/kill threshold/once/silence/boss trigger flag differs.");
                if (threshold > 0 && passed) positiveThresholds++;
            }
            Require(record.GetProperty("GoldAfter").GetInt32() - record.GetProperty("GoldBefore").GetInt32() == reward,
                "Native hit/kill reward differs at sequence " + record.GetProperty("Sequence"));
            fires++;
            bool hit = record.GetProperty("Kind").GetString() == "OnHit";
            if (hit && amount == 0 && reward > 0) blocked++;
            if (hit && record.GetProperty("Health").GetInt32() == 0 && reward > 0) lethal++;
            if (deadBoss) skippedBosses++;
            if (!hit && reward > 0) { Require(record.GetProperty("DyingId").GetInt32() > 0, "Slay lost its dying target."); slays++; }
        }
        Require(turns >= 12 && sweeps > 0 && fires >= 10 && blocked > 0 && lethal > 0 && slays > 0 && positiveThresholds > 0 && skippedBosses > 0,
            "Native hit/kill/sweep coverage is incomplete.");
        Console.WriteLine($"NATIVE-HIT-KILL PASS: {turns} independent unit turns, {sweeps} sweeps, {fires} fires, {blocked} blocked and {lethal} lethal Revenge, {slays} Slay, {positiveThresholds} threshold passes, {skippedBosses} dead-boss skips.");
    }

    private static void Require(bool condition, string error)
    { if (!condition) throw new InvalidOperationException(error); }
}
