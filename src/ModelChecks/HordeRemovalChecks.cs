using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HordeRemovalChecks
{
    internal static void Run()
    {
        CombatStatus horde = new("horde", 2, stackable: true);
        var rng = UnityRng.Seed(149);
        var context = new CombatContext(new([new(3, "spell")], [], [], rng, 0, []), rng, 0, 5, 10,
            [horde], statistics: BattleStatistics.Empty(), otherPiles: [new("Standby", [new(4, "unit")]), new("Exhausted", [])]);
        var actor = new CombatUnit(1, "horde", CombatTeam.Player, 16, 50, 50, true, false, false, [horde],
            spawnerCardId: 4, modifiers: new(16, 0, 0, 1, 0, true, false, []), hordeDefinition: new(8, 25));
        var root = new RoomCombatState(0, false, [actor], [], context);
        string parent = Serialize(root);
        var lethalDamage = RoomCombatModel.ApplyCardDamage(root, 1, 9999, 3);
        Require(lethalDamage.Supported && lethalDamage.State!.Units.Count == 0 &&
            lethalDamage.State.Context!.Statistics!.MonstersDeadThisBattle == 2 &&
            lethalDamage.State.Context.Statistics.Value(4, "TimesExhausted") == 1,
            "Lethal damage must retain its OnHit Horde actor until casualty and physical-death processing finish.");
        CardActionEffect Effect(int n, string id = "horde") => new("RemoveStatus", "Room", 0, true, true, [new(id, n)]);
        var raw = StatusRemovalModel.Remove(root, 1, "HORDE", -1, 3);
        Require(raw.Supported && raw.State!.Units.Count == 0 && raw.State.Context!.Statistics!.MonstersDeadThisBattle == 2 &&
            raw.PendingCallbacks.All(item => item.Kind != "OnDeath"), "Raw final removal invented a physical death callback.");
        var rawDrained = StatusRemovalModel.Drain(raw);
        Require(rawDrained.State!.Context!.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Single().InstanceId == 4 &&
            rawDrained.State.Context.Statistics!.Value(4, "TimesExhausted") == 0 &&
            rawDrained.State.Context.Statistics.MonstersDeadThisBattle == 2, "Raw removal must retain the orphan spawner without a physical death signal.");
        var effect = StatusRemovalModel.Effect(root, 1, Effect(9999), 3);
        Require(effect.Supported && StatusRemovalModel.RequiresSacrifice(effect) &&
            effect.State!.Context!.Statistics!.MonstersDeadThisBattle == 3 &&
            effect.State.Context.Statistics.Value(3, "LastSacrificedMonsterStats") == 1 &&
            effect.State.Context.Statistics.Value(4, "TimesExhausted") == 0 &&
            effect.State.Context.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Single().InstanceId == 4 &&
            effect.PendingCallbacks.Single(item => item.Kind == "OnDeath").Unit.SacrificeCardId == 3,
            "Effect final removal must signal a sacrifice and retain its responsible card before standby settlement.");
        var grown = StatusRemovalModel.Effect(root, 1, Effect(-2), 3);
        Require(grown.State!.Units[0].Status("horde")!.Stacks == 4 && grown.State.Units[0].Health == 50 &&
            grown.PendingCallbacks.Count == 0 && grown.State.Context!.Statistics!.MonstersDeadThisBattle == 0,
            "Negative removal changes count only, without numerical growth or notifications.");
        Compare(StatusRemovalModel.Effect(root, 1, Effect(0), 3).State, root, "zero removal");
        Compare(StatusRemovalModel.Effect(root, 1, Effect(1, "regen"), 3).State, root, "missing status");
        var restricted = new CardActionEffect("RemoveStatus", "Room", 0, true, true, [new("horde", -1)],
            filters: new("Both", [], [], false, "missing-subtype", []));
        Require(!StatusRemovalModel.Test(root, restricted, [1]) && StatusRemovalModel.Test(root, Effect(-1), []),
            "Removal casting must apply only its subtype test and allow empty unfiltered selections.");
        Compare(StatusRemovalModel.Effect(root, 1, restricted, 3).State, root, "subtype refusal");
        Require(StatusRemovalModel.Remove(root, 1, "horde", int.MinValue).State!.Units.Count == 0,
            "Raw removal must use unchecked subtraction followed by native count clamping.");
        var marker = new CombatUnit(2, "marker", CombatTeam.Player, 0, 1, 1, false, false, false,
            [new("unit_ability", 1, stackable: false)]);
        Require(StatusRemovalModel.Remove(new(0, false, [marker], [], context), 2, "unit_ability", -100)
            .State!.Units.Single().Status("unit_ability")!.Stacks == 1, "Nonstackable removal failed its one-stack cap.");
        var spell = CardSpellModel.Apply(root, [Effect(9999), new("Heal", "Room", 1, true, false, [])], 3);
        Require(spell.Supported && spell.State!.Units.Count == 0 && spell.State.Context!.Statistics!.MonstersDeadThisBattle == 3 &&
            spell.State.Context.Statistics.Value(4, "TimesExhausted") == 1,
            "Spell-chain removal failed to settle its sacrificed unit: " + spell.UnsupportedReason);
        string expected = Serialize(spell.State);
        Parallel.For(0, 32, _ => Require(Serialize(CardSpellModel.Apply(root, [Effect(9999), new("Heal", "Room", 1, true, false, [])], 3).State)
            == expected, "Parallel removal spells differed."));
        Require(Serialize(root) == parent, "Removal changed its parent state.");
        Console.WriteLine("HORDE-REMOVAL-CHECKS PASS: raw orphan spawners, physical sacrifice/statistics, responsible cards, signed/wrapped counts, caps, spell continuation and 32 branches.");
    }
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("HordeRemovalOperations", out var operations) || operations.GetArrayLength() == 0) return;
        var samples = operations.EnumerateArray().ToArray();
        Require(samples.Select(sample => sample.GetProperty("Label").GetString()).SequenceEqual([
            "effect-zero", "effect-negative", "effect-partial", "effect-missing", "api-final-player", "effect-final-player",
            "api-final-enemy", "effect-final-enemy", "trigger-final-enemy"]), "Missing native removal boundaries.");
        var phases = fixture.GetProperty("HarvestTriggers").EnumerateArray().ToArray();
        var casts = fixture.GetProperty("HordeRemovalCasts").EnumerateArray().ToArray();
        Require(casts.Length == 1, "Missing real paid removal spell.");
        foreach (var sample in samples) VerifyOperation(sample);
        foreach (var phase in phases) HarvestChecks.VerifyPhase(phase);
        foreach (var cast in casts) VerifyCast(cast);
        Parallel.For(0, 32, _ =>
        {
            foreach (var sample in samples) VerifyOperation(sample);
            foreach (var phase in phases) HarvestChecks.VerifyPhase(phase);
            foreach (var cast in casts) VerifyCast(cast);
        });
        Console.WriteLine("NATIVE-HORDE-REMOVAL-CHECKS PASS: nine complete API/effect/trigger operations, " + phases.Length +
            " exact death/Harvest phases, paid spell, retained corpses, dispatch order and 32 branches.");
    }
    private static void VerifyOperation(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        string parent = Serialize(before), label = sample.GetProperty("Label").GetString()!;
        int actorId = sample.GetProperty("ActorId").GetInt32(), sourceId = sample.GetProperty("SourceCardId").GetInt32();
        int amount = sample.GetProperty("Amount").GetInt32(); string id = sample.GetProperty("StatusId").GetString()!;
        bool trigger = sample.GetProperty("Trigger").GetBoolean(), effect = sample.GetProperty("Effect").GetBoolean();
        var queue = new List<RoomCombatModel.QueuedCharacterTrigger>();
        var dispatched = new List<Dispatch>();
        bool drained = trigger;
        RoomCombatResult result;
        if (trigger)
        {
            result = new(before, RoomOutcome.Exchanged, 0, []);
            queue.Add(new(before.RoomIndex, before.Units.Single(unit => unit.Id == actorId), "PreCombat"));
            result = Drain(result, queue, dispatched);
        }
        else
        {
            result = effect ? StatusRemovalModel.Effect(before, actorId, new("RemoveStatus", "DropTargetCharacter", 0, true, true, [new(id, amount)]), sourceId)
                : StatusRemovalModel.Remove(before, actorId, id, amount, sourceId);
            Require(result.Supported, label + " unsupported: " + result.UnsupportedReason);
            queue.AddRange(result.PendingCallbacks);
            if (StatusRemovalModel.RequiresSacrifice(result)) { result = Drain(result, queue, dispatched); drained = true; }
        }
        Require(result.Supported, label + " unsupported: " + result.UnsupportedReason);
        Compare(result.State, sample.GetProperty("After").Deserialize<RoomCombatState>(), label + " operation room");
        Compare(Actor(), sample.GetProperty("AfterActor").Deserialize<CombatUnit>(), label + " operation actor");
        Require(drained || queue.Count == sample.GetProperty("QueueBeforeDrain").GetInt32(), label + " raw accepted queue count differs.");
        if (!drained) result = Drain(result, queue, dispatched);
        // The scenario explicitly runs another queue after the effect has finished
        // removal. At this boundary native standby checks can return the spawner.
        if (Actor().SacrificeCardId.HasValue) result = RoomCombatModel.SettleQueuedSpawner(result.State!, Actor());
        Compare(result.State, sample.GetProperty("AfterDrain").Deserialize<RoomCombatState>(), label + " drained room");
        Compare(Actor(), sample.GetProperty("AfterDrainActor").Deserialize<CombatUnit>(), label + " drained actor");
        Compare(dispatched.ToArray(), sample.GetProperty("Dispatched").Deserialize<Dispatch[]>(), label + " dispatch order");
        Require(sample.GetProperty("QueueAfterDrain").GetInt32() == 0 && Serialize(before) == parent,
            label + " did not settle or changed its parent.");
        CombatUnit Actor() => result.State!.Units.FirstOrDefault(unit => unit.Id == actorId) ??
            queue.Last(item => item.Unit.Id == actorId).Unit;
    }
    private static RoomCombatResult Drain(RoomCombatResult result, List<RoomCombatModel.QueuedCharacterTrigger> queue, List<Dispatch> dispatched)
    {
        Require(RoomCombatModel.DrainCharacterQueue(queue, queued =>
        {
            if (queued.Unit.Triggers.Any(trigger => trigger.Kind == queued.Kind))
                dispatched.Add(new(queued.Unit.Id, queued.Kind, queued.DyingCharacter?.Id ?? 0, queued.TriggerCount));
            result = RoomCombatModel.ApplyQueuedCharacterTrigger(result.State!, queued, queue.Add); return result.Supported;
        }, queued => { result = RoomCombatModel.SettleQueuedSpawner(result.State!, queued.Unit); return result.Supported; },
            () => { result = EnchantmentWorldModel.CompleteQueuedRemovals(result.State!, queue.Add); return result.Supported; }),
            "Removal queue unsupported: " + result.UnsupportedReason);
        return result;
    }
    private static void VerifyCast(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<BattleTurnState>()!; string parent = Serialize(before);
        Require(new[] { CombatTeam.Player, CombatTeam.Enemy }.All(team => before.Spawn.Train.Rooms
            .SelectMany(room => room.Units).Any(unit => unit.Team == team && unit.Status("horde")?.Stacks > 0)),
            "Paid removal did not exercise both teams.");
        var result = BattleActionModel.PlayCard(before, sample.GetProperty("Action").Deserialize<PlayCardAction>()!);
        Require(result.Supported, "Native paid removal spell unsupported: " + result.Reason);
        Compare(result.State, sample.GetProperty("After").Deserialize<BattleTurnState>(), "paid removal spell");
        Require(Serialize(before) == parent, "Paid removal changed its parent.");
    }
    private sealed record Dispatch(int ActorId, string Kind, int DyingId, int TriggerCount);
    private static void Compare<T>(T predicted, T actual, string label)
    {
        string? diff = ModelJson.Difference(Serialize(predicted), Serialize(actual));
        Require(diff == null, label + ": " + diff);
    }
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool pass, string message) { if (!pass) throw new InvalidOperationException(message); }
}
