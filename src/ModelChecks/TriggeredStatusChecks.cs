using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class TriggeredStatusChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(551);
        var owner = new CardInstanceState(1, "owner", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            statusScalingTraits: [new(new("AnyStatusEffectStacksAdded"), 1, false, 0, ["armor"])]);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 2, 10,
            statistics: BattleStatistics.Empty().TrackCards([1]).Increment(1, "AnyStatusEffectStacksAdded", 3),
            cardInstances: [owner], cardRegistry: [owner], otherPiles: [new("Standby", [new(1, "owner")]), new("Exhausted", [])], magicPower: [new(0, CombatTeam.Enemy, 2), new(0, CombatTeam.Player, 0)]);
        CombatUnit Unit(int id, CombatTeam team, int hp = 10, int max = 10, CombatStatus[]? statuses = null,
            CombatTrigger[]? triggers = null, int card = 0) =>
            new(id, "unit", team, 2, hp, max, true, false, false, statuses ?? [], triggers, card,
                modifiers: new(2, 0, 0, 1, 1, true, false, []), isBoss: false, lastAttackerId: 0);
        CombatEffect Effect(int chance, CombatStatus[] statuses, string target = "Room", bool strict = false,
            CardEffectRange? range = null, TriggeredStatusScaling? scaling = null, CardTargetFilters? filters = null) =>
            new("CardEffectAddStatusEffect", chance, 0, "", 0, [], false,
                action: new("AddStatus", target, chance, true, true, statuses,
                    tests: new(true, false, false, strict), range: range, filters: filters), statusScaling: scaling);
        CombatTrigger Trigger(string kind, params CombatEffect[] effects) => new(kind, false, false, true, 1, effects, false);
        RoomCombatState Room(params CombatUnit[] units) => new(0, false, units, [], context);
        var root = Room(Unit(1, CombatTeam.Player, card: 1), Unit(4, CombatTeam.Enemy), Unit(5, CombatTeam.Enemy));
        string parent = JsonSerializer.Serialize(root);
        var feedback = TriggeredStatusModel.Apply(root, 1, Effect(0, [new("armor", 0, stackable: true)]), [4, 5]);
        Require(feedback.Supported && feedback.State!.Units.Single(u => u.Id == 5).Status("armor")!.Stacks == 3 &&
            feedback.State.Units.Single(u => u.Id == 4).Status("armor")!.Stacks == 6 &&
            feedback.State.Context!.Statistics!.Value(1, "AnyStatusEffectStacksAdded") == 12 &&
            feedback.Events.Select(e => e.Target).SequenceEqual([5, 4]) && feedback.State.Context.BattleRng.Equals(rng),
            "Status targets were reordered, source feedback lost, or zero probability consumed randomness: " + JsonSerializer.Serialize(feedback));
        var pooled = TriggeredStatusModel.Apply(root, 1, Effect(0, [new("regen", 2), new("pyregel", 3)]), [4, 5], 0);
        RngDraw choice = rng.Range(0, 2);
        string selected = choice.Value == 0 ? "regen" : "pyregel";
        Require(pooled.Supported && pooled.State!.Context!.BattleRng.Equals(choice.State) &&
            pooled.State.Units.Where(u => u.Team == CombatTeam.Enemy).All(u => u.Status(selected) != null), "Status pool was sampled per target.");
        var empty = TriggeredStatusModel.Apply(root, 1, Effect(0, [new("regen", 2), new("armor", 1)], range: new(50, 50)), [], 0);
        Require(empty.Supported && empty.State!.Context!.BattleRng.Equals(choice.State.Range(50, 50).State) &&
            empty.State.Units.All(u => u.Statuses.Count == 0), "Empty collection skipped pool/range or drew a per-target chance.");
        var immuneRoot = Room(Unit(1, CombatTeam.Player), Unit(4, CombatTeam.Enemy, statuses: [new("immune", 1)]), Unit(5, CombatTeam.Enemy));
        foreach (int chance in new[] { -10, 50, 100, 200 })
        {
            var applied = TriggeredStatusModel.Apply(immuneRoot, 1, Effect(chance, [new("regen", 2)]), [4, 5], 0);
            RngDraw back = rng.Range(0, 100); RngDraw front = back.State.Range(0, 100);
            Require(applied.Supported && applied.State!.Context!.BattleRng.Equals(front.State) &&
                applied.State.Units.Single(u => u.Id == 4).Status("regen") == null &&
                (applied.State.Units.Single(u => u.Id == 5).Status("regen") != null) == (back.Value < chance),
                "Chance/immune/reverse order differs for chance " + chance);
        }
        var scalingRoot = Room(Unit(1, CombatTeam.Player, statuses: [new("regen", 3)]),
            Unit(4, CombatTeam.Enemy, 5, 10), Unit(5, CombatTeam.Enemy, 9, 10));
        var scaled = TriggeredStatusModel.Apply(scalingRoot, 1, Effect(0, [new("regen", 2)], scaling: new("regen", true, true)), [4, 5], 0);
        Require(scaled.Supported && scaled.State!.Units.Where(u => u.Team == CombatTeam.Enemy).All(u => u.Status("regen")!.Stacks == 20),
            "Status/first-target missing HP/room magic multipliers multiplied each other or read a later target.");
        var zero = TriggeredStatusModel.Apply(root, 1, Effect(0, [new("regen", 2)], scaling: new("missing")), [4, 5], 0);
        Require(zero.Supported && zero.State!.Units.All(u => u.Statuses.Count == 0), "Zero stacks leaked into captured targeting state.");
        var removed = TriggeredStatusModel.Apply(feedback.State!, 1, Effect(0, [new("armor", -40000)]), [4, 5], 0);
        Require(removed.Supported && removed.State!.Units.All(u => u.Statuses.Count == 0) &&
            removed.State.Context!.Statistics!.Value(1, "AnyStatusEffectStacksAdded") == 12, "Negative addition counted removals or retained zero counts.");
        var deadRoot = Room(Unit(1, CombatTeam.Player, 0, 10, card: 1), Unit(4, CombatTeam.Enemy, 0, 10));
        var dead = TriggeredStatusModel.Apply(deadRoot, 1, Effect(0, [new("armor", 1)]), [1, 4]);
        Require(dead.Supported && dead.State!.Units.All(u => u.Health == 0 && u.Status("armor")!.Stacks > 0), "Retained status resurrected or discarded dying actors/targets.");
        var noMagic = new RoomCombatState(0, false, scalingRoot.Units, [], new(context.Cards, rng, 0, 2, 10));
        Require(!TriggeredStatusModel.Apply(noMagic, 1, Effect(0, [new("regen", 1)], scaling: new(magicPower: true)), [4], 0).Supported &&
            TriggeredStatusModel.Apply(noMagic, 1, Effect(0, [new("regen", 1)], scaling: new(magicPower: true)), [], 0).Supported,
            "Uncaptured magic power was guessed or queried with an empty collection.");
        var duplicateMagic = new RoomCombatState(0, false, scalingRoot.Units, [], new(context.Cards, rng, 0, 2, 10,
            magicPower: [new(0, CombatTeam.Enemy, 1), new(0, CombatTeam.Enemy, 2)]));
        Require(!TriggeredStatusModel.Apply(duplicateMagic, 1, Effect(0, [new("regen", 1)], scaling: new(magicPower: true)), [4], 0).Supported,
            "Ambiguous magic power threw an exception or selected an arbitrary entry.");
        var strict = Effect(0, [new("piercing", 1, stackable: false)], "Self", strict: true);
        Require(TriggeredStatusModel.Test(root, strict, [1], out _) && !TriggeredStatusModel.Test(root, strict, [], out _) &&
            !TriggeredStatusModel.Test(Room(Unit(1, CombatTeam.Player, statuses: [new("piercing", 1)])), strict, [1], out _),
            "Strict nonstackable status legality differs.");
        Require(TriggeredStatusModel.Validate(Effect(0, [new("piercing", 1, stackable: false), new("armor", 1, stackable: true)], strict: true)) != null,
            "Uncaptured randomized nonstackable legality was silently guessed.");
        var turn = RoomCombatModel.ApplyUnitTurn(Room(Unit(1, CombatTeam.Player, card: 1, triggers:
            [Trigger("OnTurnBegin", Effect(0, [new("regen", 2)], "Self", scaling: new("regen")),
                Effect(0, [new("piercing", 1, stackable: false)], "Self", strict: true), strict)])), 1);
        Require(turn.Supported && turn.State!.Units.Single().Status("regen") == null && turn.State.Units.Single().Status("piercing")!.Stacks == 1,
            "Engine did not apply status effects or repeated an already present strict nonstackable effect.");
        var attacked = RoomCombatModel.ApplyUnitTurn(Room(Unit(1, CombatTeam.Player, triggers:
            [Trigger("OnAttacking", Effect(0, [new("regen", 3)], "LastAttackedCharacter"))]), Unit(4, CombatTeam.Enemy, 1, 1)), 1);
        Require(attacked.Supported && attacked.State!.Units.Count == 1 &&
            attacked.Events.Any(e => e.Kind == "TriggeredStatus:regen" && e.Target == 4 && e.Amount == 3),
            "Attack status effect lost its retained dying victim override.");
        var preview = TriggeredStatusModel.Apply(new(0, false, root.Units, [], context, true), 1, Effect(0, [new("armor", 1)]), [4, 5]);
        Require(preview.Supported && preview.State!.Context!.Statistics!.Value(1, "AnyStatusEffectStacksAdded") == 3,
            "Preview modified live added-status statistics.");
        Require(!TriggeredStatusModel.Apply(new(0, false, root.Units, [], context, true), 1, Effect(50, [new("armor", 1)]), [4, 5]).Supported,
            "Vanilla history-dependent preview randomness was silently approximated.");
        string expected = JsonSerializer.Serialize(feedback);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(TriggeredStatusModel.Apply(root, 1,
            Effect(0, [new("armor", 0, stackable: true)]), [4, 5])) == expected, "Parallel status branch differs."));
        Require(JsonSerializer.Serialize(root) == parent && context.MagicPower!.Count == 2 &&
            feedback.State!.Context!.MagicPower!.Count == 2, "Status branch mutated its parent or dropped captured magic power.");
        UnityRng previewRng = Enumerable.Range(1, 100).Select(UnityRng.Seed).First(state =>
            (state.Range(0, 100).Value < 50) != (state.Range(0, 100).State.Range(0, 100).Value < 50));
        var previewContext = new CombatContext(new([], [], [], previewRng, 0, []), previewRng, 0, 1, 10,
            statistics: BattleStatistics.Empty(), isolatedBattlePreview: true);
        CombatUnit PreviewEnemy(int id, int damage) => new(id, "enemy", CombatTeam.Enemy, damage, 10, 10, true, false, false, [],
            [Trigger("OnTurnBegin", Effect(50, [new("buff", 1, 1)], "Self"))], isBoss: false);
        var previewRoot = new TrainCombatState([new(0, false, [PreviewEnemy(4, 5), Unit(8, CombatTeam.Player, 1, 1)], [], previewContext),
            new(1, false, [PreviewEnemy(5, 7), Unit(9, CombatTeam.Player, 1, 1)], [], previewContext)], [], 7, previewContext);
        string previewParent = JsonSerializer.Serialize(previewRoot);
        var refreshed = BattlePreviewModel.Refresh(previewRoot);
        int expectedLast = 5 + (previewRng.Range(0, 100).State.Range(0, 100).Value < 50 ? 1 : 0);
        Require(refreshed.Supported && refreshed.State!.Context!.Statistics!.LastAttackDamageDealt == expectedLast &&
            refreshed.State.Context.BattleRng.Equals(previewRng) && refreshed.State.Rooms.SelectMany(room => room.Units).All(unit => unit.Statuses.Count == 0) &&
            JsonSerializer.Serialize(previewRoot) == previewParent, "Isolated preview reset randomness per floor, changed live RNG/units, or lost its native statistic.");
        Console.WriteLine("TRIGGERED-STATUS-CHECKS PASS: pools, reverse chances/immune order, strict legality, additive first-target multipliers, retained dying/source state, preview and 32 parallel branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out var scenario) || scenario.GetString() != "triggered-status") return;
        int count = 0, pooled = 0, empty = 0, ranged = 0, area = 0, dying = 0, scaled = 0, sourced = 0, immune = 0, strict = 0, turns = 0;
        int battleScopes = 0, bossScopes = 0, changedScopes = 0;
        foreach (FixtureValue scope in fixture.GetProperty("PreviewRngIsolation").EnumerateArray())
        {
            UnityRng battleBefore = scope.GetProperty("BattleBefore").Deserialize<UnityRng>();
            UnityRng testBefore = scope.GetProperty("TestBefore").Deserialize<UnityRng>();
            UnityRng testObserved = scope.GetProperty("TestObserved").Deserialize<UnityRng>();
            Require(scope.GetProperty("Completed").GetBoolean() && battleBefore.Equals(scope.GetProperty("BattleAfter").Deserialize<UnityRng>()) &&
                testBefore.Equals(scope.GetProperty("TestAfter").Deserialize<UnityRng>()), "Native preview failed to restore its RNG streams.");
            battleScopes += scope.GetProperty("Kind").GetString() == "Battle" ? 1 : 0;
            bossScopes += scope.GetProperty("Kind").GetString() == "BossKill" ? 1 : 0;
            changedScopes += !testObserved.Equals(battleBefore) ? 1 : 0;
        }
        var chances = new HashSet<int>(); var kinds = new HashSet<string>();
        foreach (FixtureValue record in fixture.GetProperty("TriggeredStatuses").EnumerateArray())
        {
            Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("Interactions").GetArrayLength() == 0 &&
                record.GetProperty("Actual").ValueKind == FixtureKind.Object, "Incomplete native status effect.");
            var before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = record.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            var retained = record.GetProperty("BeforeUnits").Deserialize<CombatUnit[]>()!;
            var actualUnits = record.GetProperty("ActualUnits").Deserialize<CombatUnit[]>()!;
            var effect = record.GetProperty("Effect").Deserialize<CombatEffect>()!;
            int actor = record.GetProperty("ActorId").GetInt32(), sourceCard = record.GetProperty("SourceCardId").GetInt32();
            int[] targets = record.GetProperty("Targets").Deserialize<int[]>()!;
            var scope = new RoomCombatState(before.RoomIndex, before.Deployment,
                before.Units.Concat(retained.Where(unit => before.Units.All(live => live.Id != unit.Id))).ToArray(), before.ExternalInteractions, before.Context);
            var result = TriggeredStatusModel.Apply(scope, actor, effect, targets, sourceCard);
            string? difference = result.Supported ? ModelJson.Difference(JsonSerializer.Serialize(result.State!.Context), JsonSerializer.Serialize(actual.Context)) : result.UnsupportedReason;
            if (difference == null && result.Supported)
            {
                foreach (CombatUnit unit in actualUnits)
                {
                    difference = ModelJson.Difference(JsonSerializer.Serialize(result.State!.Units.Single(u => u.Id == unit.Id)), JsonSerializer.Serialize(unit));
                    if (difference != null) break;
                }
            }
            Require(result.Supported && difference == null, "Independent native status differs at sequence " + record.GetProperty("Sequence") + ": " + difference);
            count++; pooled += effect.Action!.Statuses.Count > 1 ? 1 : 0; empty += targets.Length == 0 ? 1 : 0;
            area += targets.Length > 1 ? 1 : 0; ranged += effect.Action.Range != null ? 1 : 0;
            dying += retained.Any(unit => unit.Health == 0) ? 1 : 0; scaled += effect.StatusScaling?.MissingHealth == true ? 1 : 0;
            sourced += sourceCard > 0 ? 1 : 0; immune += retained.Any(unit => targets.Contains(unit.Id) && unit.Status("immune") != null) ? 1 : 0;
            strict += effect.Action.Tests?.StrictTargets == true ? 1 : 0; chances.Add(effect.Action.Value); kinds.Add(record.GetProperty("Kind").GetString()!);
        }
        foreach (FixtureValue turn in fixture.GetProperty("UnitTurns").EnumerateArray())
        {
            var before = turn.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = turn.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            Require(before.Context?.IsolatedBattlePreview == true, "Native status fixture lacks its explicit preview isolation protocol.");
            var result = RoomCombatModel.ApplyUnitTurn(before, turn.GetProperty("ActorId").GetInt32());
            Require(result.Supported && turn.GetProperty("Difference").ValueKind == FixtureKind.Null &&
                ModelJson.Difference(JsonSerializer.Serialize(result.State), JsonSerializer.Serialize(actual)) == null, "Independent status unit turn differs.");
            turns++;
        }
        Require(count > 20 && pooled > 0 && empty > 0 && ranged > 0 && area > 0 && dying > 0 && scaled > 0 && sourced > 0 && immune > 0 && strict > 0 &&
            chances.IsSupersetOf([-10, 0, 50, 100]) && kinds.IsSupersetOf(["OnSpawn", "PreCombat", "OnAttacking", "OnHit", "OnDeath", "OnTurnBegin"]) && turns > 0 &&
            battleScopes > 0 && bossScopes > 0 && changedScopes > 0,
            $"Native status coverage incomplete: {count} effects, pool={pooled}, empty={empty}, range={ranged}, area={area}, dying={dying}, scaled={scaled}, source={sourced}, immune={immune}, strict={strict}, turns={turns}.");
        Console.WriteLine($"NATIVE-TRIGGERED-STATUS PASS: {count} exact effects, {pooled} pools, {empty} empty, {ranged} ranges, {area} areas, {dying} retained dying, {scaled} multipliers, {sourced} source cards, {immune} immune, {strict} strict, {turns} unit turns and {battleScopes}/{bossScopes} restored preview scopes ({changedScopes} consumed).");
    }
    private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
}
