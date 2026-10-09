using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HarvestChecks
{
    private static readonly string[] Kinds = ["OnAnyHeroDeathOnFloor", "OnAnyMonsterDeathOnFloor", "OnAnyUnitDeathOnFloor"];
    internal static void Run()
    {
        var rng = UnityRng.Seed(131);
        CombatStatus armor = new("armor", 0, 1, removeWhenTriggered: true, stackable: true);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10, [armor]);
        CombatEffect Gold(int n) => new("CardEffectRewardGold", n, 0, "", 0, [], false);
        CombatTrigger Reward(string kind, int n, bool once = false) => new(kind, once, false, true, 1, [Gold(n)], false,
            conditions: new([], ["ARMOR"]));
        CombatUnit Observer(int id, CombatTeam team) => new(id, "observer", team, 0, 20, 20, false, false, false, [],
            Kinds.SelectMany(kind => new[] { Reward(kind, 10), Reward(kind, 15, true) }).ToArray(),
            modifiers: new(0, 0, 0, 1, 0, true, false, []));
        CombatUnit Victim(CombatTeam team, bool horde) => new(1, "victim", team, 0, 20, 20, false, false, false,
            horde ? [new("horde", 2, stackable: true)] : [],
            [new("OnDeath", false, false, true, 1, [new("CardEffectAddStatusEffect", 0, 0, "", 0, [], false,
                action: new("AddStatus", "Self", 0, true, true, [armor.WithStacks(1)], tests: new(true, false, false, false))), Gold(40)], false)],
            modifiers: new(0, 0, 0, 1, 0, true, false, []), hordeDefinition: horde ? new(0, 10) : null);
        var player = Observer(2, CombatTeam.Player); var enemy = Observer(3, CombatTeam.Enemy);
        RoomCombatState Root(CombatTeam team, bool horde) => new(0, false, [enemy, Victim(team, horde), player], [], context);
        var root = Root(CombatTeam.Player, true); string parent = Serialize(root);
        var result = UnitHealthModel.Apply(root, 1, 20, debuff: true);
        Require(result.Supported && result.State!.Context!.Gold == 240 && result.State.Units.Count == 2,
            "Physical player Horde death lost death-child status, repeated harvest or dead actor exclusion: " + result.UnsupportedReason);
        Require(UnitHealthModel.Apply(Root(CombatTeam.Enemy, true), 1, 20, debuff: true).State!.Context!.Gold == 190 &&
            UnitHealthModel.Apply(Root(CombatTeam.Enemy, false), 1, 20, debuff: true).State!.Context!.Gold == 140,
            "Enemy Horde must repeat Unit harvest, but Hero harvest remains one; ordinary death must fire each once.");
        var corpse = CardSpellModel.Copy(root.Units.Single(unit => unit.Id == 1), 0, root.Units.Single(unit => unit.Id == 1).Statuses);
        var external = new RoomCombatResult(new(0, false, [enemy, player], [], context), RoomOutcome.Exchanged, 0, []);
        var queue = new List<RoomCombatModel.QueuedCharacterTrigger> { new(0, corpse, harvestAfterDeath: true) };
        var order = new List<string>();
        Require(RoomCombatModel.DrainCharacterQueue(queue, queued =>
        {
            if (queued.Unit.Triggers.Any(trigger => trigger.Kind == queued.Kind)) order.Add(queued.Kind + ":" + queued.Unit.Id);
            external = RoomCombatModel.ApplyQueuedCharacterTrigger(external.State!, queued, queue.Add); return external.Supported;
        }, _ => true, () => { external = EnchantmentWorldModel.CompleteQueuedRemovals(external.State!, queue.Add); return external.Supported; }, () => external.State?.Context, message => external = new RoomCombatResult(null, RoomOutcome.Unsupported, 0, [], message)),
            "External physical harvest unsupported: " + external.UnsupportedReason);
        Require(external.State!.Context!.Gold == 240 && order.SequenceEqual([
            "OnDeath:1", "OnAnyMonsterDeathOnFloor:2", "OnAnyMonsterDeathOnFloor:3", "OnAnyUnitDeathOnFloor:2", "OnAnyUnitDeathOnFloor:3"]),
            "External death routing lost retained children or player-first drained groups.");
        Compare(external.State, result.State, "local/external harvest parity");
        external = new RoomCombatResult(new(0, false, [enemy, player], [], context), RoomOutcome.Exchanged, 0, []);
        queue = [new(0, corpse, deferUntilRemoval: true, harvestAfterDeath: true)];
        Require(RoomCombatModel.DrainCharacterQueue(queue, queued =>
        {
            external = RoomCombatModel.ApplyQueuedCharacterTrigger(external.State!, queued, queue.Add); return external.Supported;
        }, _ => true, () => { external = EnchantmentWorldModel.CompleteQueuedRemovals(external.State!, queue.Add); return external.Supported; }, () => external.State?.Context, message => external = new RoomCombatResult(null, RoomOutcome.Unsupported, 0, [], message)),
            "Deferred physical harvest unsupported: " + external.UnsupportedReason);
        Compare(external.State, result.State, "local/deferred removal harvest parity");
        string expected = Serialize(result.State);
        Parallel.For(0, 32, _ => Require(Serialize(UnitHealthModel.Apply(root, 1, 20, debuff: true).State) == expected,
            "Parallel physical harvest differed."));
        Require(Serialize(root) == parent, "Physical harvest changed its parent.");
        Console.WriteLine("HARVEST-CHECKS PASS: physical team order, death-child conditions, Horde counts, dead exclusion, local/external/deferred parity and 32 branches.");
    }
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("HarvestOperations", out var operations) || operations.GetArrayLength() == 0) return;
        var samples = operations.EnumerateArray().ToArray();
        Require(samples.Select(sample => sample.GetProperty("Label").GetString()).SequenceEqual([
            "enemy-horde-health-lethal", "player-horde-health-lethal", "enemy-horde-damage-lethal", "player-horde-damage-lethal"]),
            "Native Harvest physical-death boundaries missing.");
        foreach (var sample in samples) VerifyOperation(sample);
        int[] gold = [990, 850, 350, 260], troops = [2, 2, 1, 1];
        for (int i = 0; i < samples.Length; i++)
        {
            var before = samples[i].GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = samples[i].GetProperty("After").Deserialize<RoomCombatState>()!;
            var dead = samples[i].GetProperty("AfterActor").Deserialize<CombatUnit>()!;
            Require(after.Context!.Gold - before.Context!.Gold == gold[i] && dead.Status("horde")!.Stacks == troops[i] &&
                dead.Status("armor")!.Stacks == 1, "Native Harvest did not exercise reward/remaining-troop/death-child boundaries.");
        }
        var phases = fixture.GetProperty("HarvestTriggers").EnumerateArray().ToArray();
        Require(phases.Any(sample => sample.GetProperty("Kind").GetString() == Kinds[0]) &&
            phases.Any(sample => sample.GetProperty("Kind").GetString() == Kinds[1]) &&
            phases.Any(sample => sample.GetProperty("Kind").GetString() == Kinds[2] && sample.GetProperty("TriggerCount").GetInt32() == 2),
            "Native Harvest coverage missing team-specific or repeated deaths.");
        foreach (var phase in phases) VerifyPhase(phase);
        Require(phases.Any(sample => sample.GetProperty("Actor").Deserialize<CombatUnit>()!.Status("silenced")?.Stacks > 0) &&
            phases.Any(sample => sample.GetProperty("Dying").Deserialize<CombatUnit>()?.Status("armor")?.Stacks > 0 &&
                sample.GetProperty("RequiredStackCounts").EnumerateArray().Any(value => value.GetInt32() == 999)),
            "Native Harvest did not exercise silence and child-before-required-dying conditions.");
        Parallel.For(0, 32, _ => { foreach (var sample in samples) VerifyOperation(sample); foreach (var phase in phases) VerifyPhase(phase); });
        Console.WriteLine("NATIVE-HARVEST-CHECKS PASS: four complete physical deaths, " + phases.Length +
            " exact room/actor/dying dispatches, team order, Horde repetition, silence/once/conditions and 32 branches.");
    }
    private static void VerifyOperation(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        string parent = Serialize(before), label = sample.GetProperty("Label").GetString()!;
        int id = sample.GetProperty("ActorId").GetInt32(), amount = sample.GetProperty("Amount").GetInt32();
        var result = sample.GetProperty("Kind").GetString() == "Damage"
            ? RoomCombatModel.ApplyCardDamage(before, id, amount, sample.GetProperty("SourceCardId").GetInt32())
            : UnitHealthModel.Apply(before, id, amount, debuff: true);
        Require(result.Supported, label + " unsupported: " + result.UnsupportedReason);
        var after = sample.GetProperty("After").Deserialize<RoomCombatState>()!;
        Compare(result.State, after, label);
        Require(sample.GetProperty("QueueAfter").GetInt32() == 0 && !after.Units.Any(unit => unit.Id == id) &&
            sample.GetProperty("AfterActor").Deserialize<CombatUnit>()!.Health == 0, "Native physical death did not settle.");
        if (sample.GetProperty("Kind").GetString() == "DebuffHealth")
        {
            var dead = before.Units.Single(unit => unit.Id == id);
            var expected = new List<(int Id, string Kind, int Dying, int Count)> { (id, "OnDeath", 0, 1), (id, "OnArmorAdded", 0, 1) };
            foreach (string kind in new[] { dead.Team == CombatTeam.Player ? Kinds[1] : Kinds[0], Kinds[2] })
                foreach (CombatTeam team in new[] { CombatTeam.Player, CombatTeam.Enemy })
                    foreach (var actor in before.Units.Where(unit => unit.Id != id && unit.Team == team))
                        expected.Add((actor.Id, kind, id, kind == Kinds[0] ? 1 : 2));
            var dispatched = sample.GetProperty("Dispatched").EnumerateArray().Select(item =>
                (item.GetProperty("ActorId").GetInt32(), item.GetProperty("Kind").GetString()!,
                    item.GetProperty("DyingId").GetInt32(), item.GetProperty("TriggerCount").GetInt32()));
            Require(dispatched.SequenceEqual(expected), label + " native own-death/child/player/enemy/kind drain order differs.");
        }
        Require(Serialize(before) == parent, label + " changed its parent.");
    }
    internal static void VerifyPhase(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var actor = sample.GetProperty("Actor").Deserialize<CombatUnit>()!;
        var dying = sample.GetProperty("Dying").Deserialize<CombatUnit>();
        string parent = Serialize(new { before, actor, dying });
        var queued = new RoomCombatModel.QueuedCharacterTrigger(before.RoomIndex, actor, sample.GetProperty("Kind").GetString()!,
            paramInt: sample.GetProperty("ParamInt").GetInt32(), dyingCharacter: dying, canFireTriggers: sample.GetProperty("CanFire").GetBoolean(),
            triggerCount: sample.GetProperty("TriggerCount").GetInt32());
        var result = RoomCombatModel.ApplyQueuedCharacterTrigger(before, queued, _ => { });
        Require(result.Supported, "Native Harvest dispatch unsupported: " + result.UnsupportedReason);
        Compare(result.State, sample.GetProperty("After").Deserialize<RoomCombatState>(), "Harvest phase room");
        Compare(queued.Unit, sample.GetProperty("AfterActor").Deserialize<CombatUnit>(), "Harvest phase actor");
        Compare(queued.DyingCharacter, sample.GetProperty("AfterDying").Deserialize<CombatUnit>(), "Harvest phase dying");
        Require(Serialize(new { before, actor, dying }) == parent, "Native Harvest dispatch changed its parent.");
    }
    private static void Compare<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(Serialize(predicted), Serialize(actual));
        Require(difference == null, label + ": " + difference);
    }
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool pass, string message) { if (!pass) throw new InvalidOperationException(message); }
}
