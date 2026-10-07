using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CompanionBossChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(51);
        var relentless = new CombatStatus("relentless", 1, stackable: false, hidden: false, displayCategory: "Positive");
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10, [relentless]);
        CombatTrigger Trigger(string kind, int gold, int id, bool remove, bool conditional = false) => new(kind, false, false, true, 1,
            [new("CardEffectRewardGold", gold, 0, "", 0, [], false)], false, stateId: id,
            conditions: conditional ? new(["relentless"], []) : null, removeOnRelentlessChange: remove);
        var boss = new CombatUnit(1, "boss", CombatTeam.Enemy, 7, 125, 125, true, false, true, [],
            [Trigger("OnTrainRoomLoop", 7, 0, true), Trigger("PostAscension", 11, 1, true), Trigger("OnShift", 13, 2, false),
                Trigger("OnStatusEffectChanged", 17, 3, true, true), Trigger("OnStatusEffectChanged", 23, 4, false, true)],
            isBoss: true, statusRegistry: [], nextTriggerId: 5);
        var other = new CombatUnit(2, "other", CombatTeam.Enemy, 1, 10, 10, true, false, false, []);
        EnemySpawnState Root(int phase = 1, int floor = 2, bool loops = true, CombatUnit? actor = null,
            CombatUnit? otherUnit = null, IReadOnlyList<int>? pending = null) => new(
            new TrainCombatState(Enumerable.Range(0, 4).Select(index => new RoomCombatState(index, false,
                index == floor ? [actor ?? boss] : index == 1 ? [otherUnit ?? other] : [], [], context)).ToArray(),
                [new(1, 1, true, loops, true), new(2, 1, true, false)], 7, context),
            [new EnemyWave([new EnemyGroup([])])], [0], phase, false, rng, pending?.Count > 0 ? pending.Max() + 1 : 3,
            [], 0, false, 1, 0, 4, [], pendingDestroyedUnitIds: pending);
        var root = Root(); string parent = JsonSerializer.Serialize(root, ModelJson.Options);
        var result = CompanionBossModel.Resolve(root, true);
        Require(result.Supported && result.State!.Rooms[0].Units.Single().Id == 1 && result.State.Rooms[1].Units.Single().Id == 2 &&
            result.State.Context!.Gold == 55, "Forced Boss loop, movement callbacks or status/removal/queue order differs.");
        var changed = result.State!.Rooms[0].Units.Single();
        Require(result.RoomResults.SelectMany(room => room.Events).Count(item => item.Kind == "Gold") == 4,
            "Movement or settled status-callback events were lost from the complete Boss action.");
        Require(changed.Status("relentless")!.Stacks == 1 && changed.StatusImmunities.Contains("rooted") &&
            changed.Triggers.Select(trigger => trigger.StateId).SequenceEqual([2, 4]) && changed.NextTriggerId == 5,
            "Relentless membership, immunity, trigger order or allocation cursor differs.");
        Require(CompanionBossModel.Resolve(Root(0), true).State!.Context!.Gold == 0 &&
            CompanionBossModel.Resolve(root, false).State!.Context!.Gold == 0 &&
            CompanionBossModel.Resolve(Root(actor: changed), true).State!.Rooms[2].Units.Single().Triggers.Count == 2,
            "Wave/phase/already-relentless gates differ.");
        var stationary = CompanionBossModel.Resolve(Root(floor: 0), true);
        Require(stationary.Supported && stationary.State!.Context!.Gold == 25,
            "A Boss already at floor zero received movement callbacks.");
        var nonloop = CompanionBossModel.Resolve(Root(floor: 0, loops: false), true);
        Require(nonloop.Supported && nonloop.State!.Rooms[1].Units.Last().Id == 1 && nonloop.State.Context!.Gold == 50,
            "Force-loop incorrectly changed an intrinsically non-looping movement rule.");
        CombatUnit removed = CompanionBossModel.RemoveTriggers(boss);
        Require(removed.Triggers.Select(trigger => trigger.StateId).SequenceEqual([2, 4]) && removed.NextTriggerId == 5 &&
            removed.Statuses.Count == 0 && boss.Triggers[0].ForPreview().RemoveOnRelentlessChange &&
            boss.Triggers[0].WithOrigin("upgrade", 8, 7).RemoveOnRelentlessChange,
            "Removal changed unrelated state or trigger copies lost their native flag.");
        string expected = JsonSerializer.Serialize(result.State, ModelJson.Options);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(CompanionBossModel.Resolve(root, true).State, ModelJson.Options) == expected,
            "Parallel companion transition differs."));
        Require(JsonSerializer.Serialize(root, ModelJson.Options) == parent, "Companion transition changed its parent.");
        var detached = new CombatUnit(2, "other", CombatTeam.Enemy, 1, 10, 10, true, false, false, [], lastAttackerId: 99);
        int[] pending = [99]; var pendingRoot = Root(otherUnit: detached, pending: pending); pending[0] = 0;
        var settled = TrainCombatModel.ProcessRemovals(pendingRoot.Train);
        Require(settled.Rooms[1].Units.Single().LastAttackerId == 0 && pendingRoot.PendingDestroyedUnitIds!.Single() == 99 &&
            pendingRoot.Train.Rooms[1].Units.Single().LastAttackerId == 99 &&
            !CompanionBossModel.Resolve(pendingRoot, true).Supported && !CompanionBossModel.Resolve(Root(otherUnit: detached), true).Supported,
            "Canonical phase projection lost input ownership or accepted unsettled destroyed references.");
        Console.WriteLine("COMPANION-BOSS-CHECKS PASS: wave/phase/already-relentless gates, forced/stationary/non-loop movement, ordered callbacks/removal, retained IDs and 32 branches.");
    }
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("CompanionBossActions", out var records) || records.GetArrayLength() == 0)
        {
            Require(!fixture.TryGetProperty("ModifierScenario", out var modifier) || modifier.GetString() != "companion-boss",
                "The requested native companion scenario never executed.");
            return;
        }
        var samples = records.EnumerateArray().ToArray();
        foreach (var sample in samples) Verify(sample);
        var removals = fixture.GetProperty("RelentlessTriggerRemovals").EnumerateArray().ToArray();
        Require(removals.Length == 1, "Expected one actual native relentless removal.");
        foreach (var removal in removals)
        {
            var before = removal.GetProperty("Before").Deserialize<CombatUnit>()!;
            var actual = removal.GetProperty("Actual").Deserialize<CombatUnit>()!;
            Compare(CompanionBossModel.RemoveTriggers(before), actual);
            Require(before.Triggers.Count(trigger => trigger.RemoveOnRelentlessChange) == 4 &&
                actual.Triggers.All(trigger => !trigger.RemoveOnRelentlessChange) && actual.NextTriggerId == before.NextTriggerId &&
                removal.GetProperty("QueuedBefore").GetInt32() > 0 && removal.GetProperty("QueuedBefore").GetInt32() == removal.GetProperty("QueuedAfter").GetInt32(),
                "Native removal did not prove selective deletion with retained queued callbacks/allocation.");
        }
        Require(samples.Any(sample => {
            var before = sample.GetProperty("Before").Deserialize<EnemySpawnState>()!;
            var actual = sample.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            return before.Train.Rooms[2].Units.Any(unit => unit.IsBoss == true && unit.Status("relentless") == null) &&
                actual.Rooms[0].Units.Any(unit => unit.IsBoss == true && unit.Status("relentless") != null);
        }), "No native forced return from floor two followed by the relentless transition.");
        var transition = samples.Single(sample => sample.GetProperty("Actual").Deserialize<TrainCombatState>()!.Context!.Gold >
            sample.GetProperty("Before").Deserialize<EnemySpawnState>()!.Train.Context!.Gold);
        var prior = transition.GetProperty("Before").Deserialize<EnemySpawnState>()!;
        var raw = transition.GetProperty("RawAfter").Deserialize<TrainCombatState>()!;
        var final = transition.GetProperty("Actual").Deserialize<TrainCombatState>()!;
        Require(raw.Context!.Gold - prior.Train.Context!.Gold == 75 && final.Context!.Gold - raw.Context.Gold == 55,
            "Native movement rewards or removal-before-status-callback order were not demonstrated.");
        if (fixture.GetProperty("Policy").GetString() == "units-spells-and-junk")
        {
            var original = transition.GetProperty("RawBefore").Deserialize<EnemySpawnState>()!;
            Require(original.PendingDestroyedUnitIds != null && original.Train.Rooms.SelectMany(room => room.Units).Any(unit =>
                unit.LastAttackerId > 0 && original.PendingDestroyedUnitIds.Contains(unit.LastAttackerId.Value) &&
                prior.Train.Rooms.SelectMany(room => room.Units).Single(after => after.Id == unit.Id).LastAttackerId == 0),
                "The native card policy did not demonstrate explicit raw versus canonical destroyed references.");
        }
        Parallel.For(0, 32, _ => { foreach (var sample in samples) Verify(sample); });
        Console.WriteLine($"NATIVE-COMPANION-BOSS-CHECKS PASS: {samples.Length} complete phases, one native selective removal, forced floor-two loop and 32 branches.");
    }
    private static void Verify(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<EnemySpawnState>()!;
        var original = sample.GetProperty("RawBefore").Deserialize<EnemySpawnState>()!;
        var rawActual = sample.GetProperty("RawActual").Deserialize<TrainCombatState>()!;
        Compare(TrainCombatModel.ProcessRemovals(original.Train), before.Train);
        Compare(TrainCombatModel.ProcessRemovals(rawActual), sample.GetProperty("Actual").Deserialize<TrainCombatState>()!);
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        var result = CompanionBossModel.Resolve(before, sample.GetProperty("Phase").GetString() == "BossActionPreCombat");
        Require(result.Supported, "Native companion action unsupported: " + result.UnsupportedReason);
        Compare(result.State, sample.GetProperty("Actual").Deserialize<TrainCombatState>());
        Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Native companion phase changed its parent.");
    }
    private static void Compare<T>(T predicted, T actual)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, "Companion native state differs: " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
