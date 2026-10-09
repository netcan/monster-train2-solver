using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HordeDeathChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(157);
        var horde = new CombatStatus("horde", 2, stackable: true);
        var remove = new CardActionEffect("RemoveStatus", "Self", 0, true, true, [new("horde", 9999)]);
        var trigger = new CombatTrigger("OnDeath", false, false, true, 1,
            [new("CardEffectRemoveStatusEffect", 0, 0, "", 0, [], false, action: remove)], false);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 5, 10, [horde],
            statistics: BattleStatistics.Empty(), otherPiles: [new("Standby", [new(4, "unit")]), new("Exhausted", [])]);
        var corpse = new CombatUnit(1, "horde", CombatTeam.Player, 16, 0, 0, true, false, false, [horde], [trigger],
            spawnerCardId: 4, modifiers: new(16, 0, 0, 1, 0, true, false, []), hordeDefinition: new(8, 25),
            sacrificeCardId: 0, deathState: new(true, true, true, 0));
        var room = new RoomCombatState(0, false, [], [], context);
        string parent = Serialize(new { room, corpse });
        void CheckPending()
        {
            var queue = new List<RoomCombatModel.QueuedCharacterTrigger>();
            var queued = new RoomCombatModel.QueuedCharacterTrigger(0, corpse);
            var result = RoomCombatModel.ApplyQueuedCharacterTrigger(room, queued, queue.Add);
            Require(result.Supported && result.State!.Context!.Statistics!.MonstersDeadThisBattle == 4 &&
                result.State.Context.Statistics.Value(4, "LastSacrificedMonsterStats") == 1 &&
                queued.Unit.SacrificeCardId == 4 && queued.Unit.DeathState is { HasFinishedDying: true, IsBeingRemoved: true,
                    HasStatisticsListener: false, PendingStatisticsCardId: null } && queue.All(item => item.Kind != "OnDeath"),
                "Pending and nested death signals must each count once without another physical removal.");
        }
        CheckPending(); Parallel.For(0, 32, _ => CheckPending());
        var direct = StatusRemovalModel.Effect(new(0, false, [corpse], [], context), corpse.Id, remove, 4, whileRunningQueue: true);
        Require(direct.Supported && direct.State!.Context!.Statistics!.MonstersDeadThisBattle == 4 &&
            direct.PendingCallbacks.All(item => item.Kind != "OnDeath"), "Nested removal must also settle its original pending death signal at the effect boundary.");
        var cleared = corpse.WithDeathState(new(true, true, false));
        var clearedQueue = new List<RoomCombatModel.QueuedCharacterTrigger>();
        var afterClear = RoomCombatModel.ApplyQueuedCharacterTrigger(room, new(0, cleared), clearedQueue.Add);
        Require(afterClear.Supported && afterClear.State!.Context!.Statistics!.MonstersDeadThisBattle == 2 &&
            afterClear.State.Context.Statistics.Value(4, "LastSacrificedMonsterStats") == 0 &&
            clearedQueue.All(item => item.Kind != "OnDeath"), "Cleared death listeners must not count another signal.");
        var alive = new CombatUnit(1, "horde", CombatTeam.Player, 16, 50, 50, true, false, false, [horde],
            [new("PreCombat", false, false, true, 1, trigger.Effects, false)], spawnerCardId: 4,
            modifiers: new(16, 0, 0, 1, 0, true, false, []), hordeDefinition: new(8, 25), deathState: new(false, false, true));
        var pending = new List<RoomCombatModel.QueuedCharacterTrigger>();
        var sacrifice = RoomCombatModel.ApplyQueuedCharacterTrigger(new(0, false, [alive], [], context), new(0, alive, "PreCombat"), pending.Add);
        Require(sacrifice.Supported && sacrifice.State!.Context!.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Count == 0 &&
            sacrifice.State.Context.Statistics!.Value(4, "TimesExhausted") == 1 &&
            pending.Single(item => item.Kind == "OnDeath").Unit.DeathState is { IsBeingRemoved: true, HasStatisticsListener: false },
            "Player sacrifice inside a queue must return its card before the queued death callback.");
        Require(Serialize(new { room, corpse }) == parent, "Death signals changed their parent.");
        Console.WriteLine("HORDE-DEATH-CHECKS PASS: pending/cleared death listeners, nested statistics without repeated removal, queued player card return and 32 branches.");
    }
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("HordeDeathOperations", out var operations) || operations.GetArrayLength() == 0) return;
        var samples = operations.EnumerateArray().ToArray();
        Require(samples.Select(sample => sample.GetProperty("Label").GetString()).SequenceEqual([
            "queued-final-player", "dying-final-enemy", "dying-final-player"]), "Missing queued or repeated Horde death cases.");
        var phases = fixture.GetProperty("HarvestTriggers").EnumerateArray().ToArray();
        void CheckAll()
        {
            foreach (var sample in samples) VerifyOperation(sample);
            foreach (var phase in phases) HarvestChecks.VerifyPhase(phase);
        }
        CheckAll(); Parallel.For(0, 32, _ => CheckAll());
        Console.WriteLine("NATIVE-HORDE-DEATH-CHECKS PASS: three complete death operations, " + phases.Length +
            " exact retained death/Harvest phases, pending signals, card returns and 32 branches.");
    }
    internal static void VerifyOperation(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!; string parent = Serialize(before);
        int actorId = sample.GetProperty("ActorId").GetInt32();
        RoomCombatResult result;
        CombatUnit corpse;
        var predictedDispatches = new List<CharacterTriggerDispatch>();
        if (sample.GetProperty("Kind").GetString() == "PreCombat")
        {
            var queue = new List<RoomCombatModel.QueuedCharacterTrigger> { new(before.RoomIndex, before.Units.Single(unit => unit.Id == actorId), "PreCombat") };
            result = new(before, RoomOutcome.Exchanged, 0, []);
            Require(RoomCombatModel.DrainCharacterQueue(queue, queued =>
            {
                result = RoomCombatModel.ApplyQueuedCharacterTrigger(result.State!, queued, queue.Add); predictedDispatches.AddRange(result.Dispatches); return result.Supported;
            }, queued => { result = RoomCombatModel.SettleQueuedSpawner(result.State!, queued.Unit); return result.Supported; },
                () => { result = EnchantmentWorldModel.CompleteQueuedRemovals(result.State!, queue.Add); return result.Supported; }, () => result.State?.Context, message => result = new RoomCombatResult(null, RoomOutcome.Unsupported, 0, [], message)),
                "Queued player sacrifice unsupported: " + result.UnsupportedReason);
            corpse = queue.Last(item => item.Unit.Id == actorId).Unit;
        }
        else
        {
            result = UnitHealthModel.Apply(before, actorId, sample.GetProperty("Amount").GetInt32(), debuff: true);
            Require(result.Supported, "Repeated death unsupported: " + result.UnsupportedReason);
            corpse = result.RetainedUnits.Single(unit => unit.Id == actorId);
            predictedDispatches.AddRange(result.Dispatches);
        }
        Compare(result.State, sample.GetProperty("After").Deserialize<RoomCombatState>(), sample.GetProperty("Label").GetString()!);
        Compare(corpse, sample.GetProperty("AfterActor").Deserialize<CombatUnit>(), "settled death actor");
        Require(sample.GetProperty("QueueAfter").GetInt32() == 0 && Serialize(before) == parent, "Death queue pending or parent changed.");
        var dispatched = sample.GetProperty("Dispatched").EnumerateArray().ToArray();
        var actualDispatches = dispatched.Select(item => new Dispatch(item.GetProperty("ActorId").GetInt32(),
            item.GetProperty("Kind").GetString()!, item.GetProperty("DyingId").GetInt32(), item.GetProperty("TriggerCount").GetInt32())).ToArray();
        Compare(predictedDispatches.Select(item => new Dispatch(item.ActorId, item.Kind, item.DyingId, item.TriggerCount)).ToArray(),
            actualDispatches, "complete death dispatch order");
        Require(dispatched.Count(item => item.GetProperty("ActorId").GetInt32() == actorId &&
            item.GetProperty("Kind").GetString() == "OnDeath") == 1, "Native nested sacrifice duplicated its physical removal.");
    }
    private sealed record Dispatch(int ActorId, string Kind, int DyingId, int TriggerCount);
    private static void Compare<T>(T expected, T actual, string label)
    {
        string? diff = ModelJson.Difference(Serialize(expected), Serialize(actual));
        Require(diff == null, label + ": " + diff);
    }
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool pass, string message) { if (!pass) throw new InvalidOperationException(message); }
}
