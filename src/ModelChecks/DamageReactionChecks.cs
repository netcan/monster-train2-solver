using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DamageReactionChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        FixtureValue[] samples = Capture(root, revival: false);
        foreach (FixtureValue sample in samples) Verify(sample);
        var byLabel = samples.ToDictionary(sample => sample.GetProperty("Label").GetString()!);
        foreach (string source in new[] { "None", "Card", "Friendly", "Enemy", "Self", "Relic", "EnemyCard", "FriendlyCard", "EnemyRelic" })
        {
            FixtureValue sample = byLabel["player-source-" + source];
            bool friendly = source is "Card" or "Friendly" or "Self" or "Relic" or "FriendlyCard";
            Expect(sample, 13, friendly ? 50 : 30, 2, friendly ? 1 : 0);
            FixtureValue[] blood = sample.GetProperty("Dispatches").EnumerateArray().Where(item => item.GetProperty("Kind").GetString() == "OnBloodshed").ToArray();
            int target = sample.GetProperty("TargetId").GetInt32();
            Require(blood.All(item => item.GetProperty("ParamInt").GetInt32() == 2 && item.GetProperty("ParamInt2").GetInt32() == 0 &&
                item.GetProperty("ParamString").GetString() == "" && item.GetProperty("OverrideTargetId").GetInt32() == target),
                "Bloodshed did not retain the shared damaged-character payload.");
            Require(blood.Select(item => item.GetProperty("ActorId").GetInt32()).Distinct().Count() == 2, "Bloodshed missed a same-floor friendly actor.");
            string[] kinds = Kinds(sample);
            Require(kinds.Take(3).SequenceEqual(new[] { "OnBloodshed", "OnBloodshed", "OnHit" }) && (!friendly || kinds[3] == "OnPunish"),
                "Bloodshed/OnHit/Punish order differs for " + source);
        }
        foreach (string source in new[] { "None", "Card", "Friendly", "Enemy" })
            Expect(byLabel["enemy-source-" + source], 13, source is "Card" or "Friendly" ? 35 : 15, 0, source is "Card" or "Friendly" ? 1 : 0);
        Expect(byLabel["zero"], 15, 0, 0, 0);
        Expect(byLabel["armor-blocked"], 15, 50, 2, 1);
        Expect(byLabel["armor-partial"], 13, 50, 2, 1);
        Expect(byLabel["damage-shield"], 15, 50, 2, 1);
        Expect(byLabel["titanskin"], 14, 50, 2, 1);
        Expect(byLabel["threshold-below"], 13, 15, 2, 1);
        Expect(byLabel["threshold-equal"], 13, 50, 2, 1);
        Expect(byLabel["threshold-armor-blocked"], 15, 15, 2, 1);
        Expect(byLabel["once-two-hits"], 11, 65, 4, 2);
        Expect(byLabel["silenced-host"], 13, 10, 2, 1);
        Expect(byLabel["silenced-witness"], 13, 40, 2, 1);
        Expect(byLabel["muted-host"], 13, 10, 2, 1);
        Expect(byLabel["ignored-silence"], 13, 50, 2, 1);
        Expect(byLabel["bloodshed-heals-damaged-target"], 15, 50, 2, 1);
        Expect(byLabel["bloodshed-damages-target"], 12, 90, 4, 2);
        Expect(byLabel["punish-heals-self"], 15, 50, 2, 1);
        Expect(byLabel["punish-damages-self"], 12, 80, 4, 2);
        Expect(byLabel["damage-enters-half"], 10, 75, 2, 1);
        Expect(byLabel["bloodshed-heals-before-half"], 12, 50, 2, 1);
        Expect(byLabel["punish-heals-before-half"], 12, 50, 2, 1);
        RoomCombatState modern = samples[0].GetProperty("Before").Deserialize<RoomCombatState>()!;
        RoomCombatState missingStatistics = modern.WithContext(modern.Context!.WithStatistics(null));
        Require(RoomCombatModel.Validate(missingStatistics)?.Contains("2.3.1 damage lifecycle", StringComparison.Ordinal) == true,
            "Unversioned authored Bloodshed/Punish was accepted.");
        Parallel.For(0, 32, _ => { foreach (FixtureValue sample in samples) Verify(sample); });
        Console.WriteLine("DAMAGE-REACTIONS PASS: 33 native complete successors, two paid same-floor units, source precedence, shields/thresholds/once/silence, nested effects and half-health ordering; every dispatch/RNG and 32 immutable parallel branches match.");
    }
    internal static void NativeRevival(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue[] samples = Capture(document.RootElement, revival: true);
        foreach (FixtureValue sample in samples) Verify(sample);
        var byLabel = samples.ToDictionary(sample => sample.GetProperty("Label").GetString()!);
        foreach (string source in new[] { "Card", "Enemy", "Self", "Relic", "Friendly" })
            Expect(byLabel["revival-source-" + source], 1, source == "Enemy" ? 105 : 125, 2, source == "Enemy" ? 0 : 1);
        Expect(byLabel["revival-enemy-card"], 1, 110, 0, 1);
        Expect(byLabel["revival-enemy-source"], 1, 90, 0, 0);
        foreach (string label in new[] { "revival-bloodshed-heal", "revival-punish-heal", "revival-bloodshed-nested-damage", "revival-punish-nested-damage" })
        {
            Expect(byLabel[label], 1, 125, 2, 1);
            Require(!Kinds(byLabel[label]).Contains("OnHeal"), "A dead damage target was healed before Undying revival.");
        }
        Expect(byLabel["revival-half-gain"], 1, 150, 2, 1);
        Expect(byLabel["revival-half-gain-lost-one-maxhp"], 1, 180, 2, 1);
        Expect(byLabel["revival-half-gain-heals"], 3, 150, 2, 1);
        Expect(byLabel["revival-reanimated-heals"], 3, 125, 2, 1);
        Expect(byLabel["revival-death-heals"], 3, 125, 2, 1);
        Expect(byLabel["revival-twice-listener-once"], 1, 250, 4, 2);
        Expect(byLabel["revival-reanimated-heals-leaves-half"], 2, 180, 2, 1);
        Expect(byLabel["revival-active-half-remains"], 1, 125, 2, 1);
        Expect(byLabel["revival-active-half-heal-leaves"], 2, 155, 2, 1);
        foreach (FixtureValue sample in samples)
        {
            int id = sample.GetProperty("TargetId").GetInt32(), hits = sample.GetProperty("Input").GetProperty("Hits").GetInt32();
            RoomCombatState before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = sample.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            CombatUnit unit = actual.Units.Single(actor => actor.Id == id);
            bool player = unit.Team == CombatTeam.Player;
            Require(unit.Health > 0 && (unit.Status("undying")?.Stacks ?? 0) == 0 && unit.DeathState?.HasFinishedDying == false &&
                unit.DeathState.IsBeingRemoved == false && unit.DeathState.IsDestroyed != true &&
                actual.Context!.Statistics!.MonstersDeadThisTurn - before.Context!.Statistics!.MonstersDeadThisTurn == (player ? 1 : 0) &&
                (!player || unit.DeathState.HasStatisticsListener == false && unit.DeathState.StatisticsListenerOnce == false),
                "Revival state or the death listener lifetime differs: " + sample.GetProperty("Label").GetString());
            FixtureValue[] deaths = sample.GetProperty("Dispatches").EnumerateArray().Where(item => item.GetProperty("Kind").GetString() == "OnDeath").ToArray();
            Require(deaths.Length == hits && deaths.All(item => item.GetProperty("ParamInt").GetInt32() == 1 &&
                item.GetProperty("ParamString").GetString() == "") && Kinds(sample).Count(kind => kind == "OnReanimated") == hits,
                "Undying death/reanimated payloads differ.");
            if (unit.SpawnerCardId > 0)
                Require(actual.Context.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Any(card => card.InstanceId == unit.SpawnerCardId),
                    "Undying routed a living unit's spawner out of Standby.");
        }
        string[] oneHp = Kinds(byLabel["revival-half-gain-lost-one-maxhp"]);
        Require(oneHp.Count(kind => kind == "OnDeathwish") == 2 && oneHp.TakeLast(2).All(kind => kind == "OnDeathwishLost") &&
            Array.IndexOf(oneHp, "OnDeathwish") < Array.IndexOf(oneHp, "OnReanimated") &&
            Array.IndexOf(oneHp, "OnDeath") < Array.IndexOf(oneHp, "OnDeathwishLost"), "Revival gain/loss queue order differs.");
        Parallel.For(0, 32, _ => { foreach (FixtureValue sample in samples) Verify(sample); });
        Require(!Kinds(byLabel["revival-active-half-remains"]).Any(kind => kind is "OnDeathwish" or "OnDeathwishLost") &&
            !Kinds(byLabel["revival-active-half-heal-leaves"]).Contains("OnDeathwish"), "Active half-health revival repeated its gain callback.");
        Console.WriteLine("DAMAGE-REACTION-REVIVAL PASS: 20 full native Undying successors, lethal friendly/enemy/self/relic sources, dead callback target controls, half-health queue before revival, one-HP gain/loss, active-half controls, reanimated/death healing, twice-revived once-listener, full dispatch/RNG/state and 32 immutable branches. Full battle/preview acceptance is checked separately.");
    }
    internal static void NativeFinalDeath(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue[] samples = Capture(document.RootElement, revival: false, finalDeath: true);
        foreach (FixtureValue sample in samples) Verify(sample, finalDeath: true);
        var byLabel = samples.ToDictionary(sample => sample.GetProperty("Label").GetString()!);
        foreach (string source in new[] { "None", "Card", "Enemy", "Self", "Relic", "Friendly" })
            Expect(byLabel["final-source-" + source], 0, source is "None" or "Enemy" ? 30 : 50, 2, source is "None" or "Enemy" ? 0 : 1);
        Expect(byLabel["final-enemy-card"], 0, 35, 0, 1);
        Expect(byLabel["final-enemy-source"], 0, 15, 0, 0);
        foreach (string label in new[] { "final-bloodshed-heal", "final-punish-heal", "final-bloodshed-nested-damage",
            "final-punish-nested-damage", "final-active-half-remains", "final-active-half-one" })
            Expect(byLabel[label], 0, 50, 2, 1);
        foreach (FixtureValue sample in samples)
        {
            RoomCombatState before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = sample.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            CombatUnit dead = sample.GetProperty("ActualTarget").Deserialize<CombatUnit>()!;
            Require(dead.Health == 0 && dead.DeathState?.HasFinishedDying == true && dead.DeathState.IsDestroyed != true &&
                !actual.Units.Any(unit => unit.Id == dead.Id) &&
                actual.Context!.SpawnPoints!.Units.Single(unit => unit.UnitId == dead.Id).Current != null &&
                actual.Context.Statistics!.MonstersDeadThisTurn - before.Context!.Statistics!.MonstersDeadThisTurn ==
                    (dead.Team == CombatTeam.Player ? 1 : 0), "Final damage removed its physical point or lost death statistics.");
            string[] kinds = Kinds(sample);
            Require(!kinds.Any(kind => kind is "OnDeath" or "OnReanimated" or "OnHeal" or "OnDeathwishLost"),
                "Final damage ran a removal/revival/heal callback prematurely.");
            bool gain = sample.GetProperty("Input").GetProperty("Half").GetBoolean() &&
                !sample.GetProperty("Input").GetProperty("ActiveHalf").GetBoolean();
            Require(kinds.Count(kind => kind == "OnDeathwish") == (gain ? 2 : 0), "Final half-health callback request count differs.");
            if (dead.Team == CombatTeam.Player)
                Require(actual.Context.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Any(card => card.InstanceId == dead.SpawnerCardId),
                    "Damage exhausted its spawner before physical removal.");
            if (sample.GetProperty("Label").GetString()!.StartsWith("final-half", StringComparison.Ordinal))
                Expect(sample, 0, 50, 2, 1);
        }
        Parallel.For(0, 32, _ => { foreach (FixtureValue sample in samples) Verify(sample, finalDeath: true); });
        FixtureValue root = document.RootElement;
        Require(root.GetProperty("RestorationBoundary").GetString() == "BeforePhysicalRemovalSequence", "Missing matrix restoration checkpoint.");
        FixtureValue[] removals = root.GetProperty("Removals").EnumerateArray().ToArray();
        Require(removals.Length == 2 && removals.All(item => item.GetProperty("Completed").GetBoolean() &&
            item.GetProperty("Difference").ValueKind == FixtureKind.Null && item.GetProperty("PendingAfterDrain").GetInt32() == 0),
            "Incomplete native physical removal sequence.");
        foreach (FixtureValue item in removals) VerifyRemoval(item);
        RoomCombatState firstAfter = removals[0].GetProperty("Actual").Deserialize<RoomCombatState>()!;
        RoomCombatState secondBefore = removals[1].GetProperty("Before").Deserialize<RoomCombatState>()!;
        Require(firstAfter.Units.All(unit => unit.Id != removals[0].GetProperty("TargetId").GetInt32()) &&
            secondBefore.Units.All(unit => unit.Id != removals[0].GetProperty("TargetId").GetInt32()) &&
            firstAfter.Context!.Gold == secondBefore.Context!.Gold, "Physical removal sequence lost its first death.");
        Parallel.For(0, 32, _ => { foreach (FixtureValue item in removals) VerifyRemoval(item); });
        Console.WriteLine("DAMAGE-REACTION-FINAL-DEATH PASS: 18 native damage/death-signal successors plus two real physical removals, half-health queue before death, retained actors, death effects/Harvest/exhaustion, full dispatch/state/RNG and 32 immutable branches.");
    }
    private static void VerifyRemoval(FixtureValue sample)
    {
        RoomCombatState before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        string frozen = JsonSerializer.Serialize(before, ModelJson.Options);
        int id = sample.GetProperty("TargetId").GetInt32();
        RoomCombatResult result = RoomCombatModel.ApplyNativeCardDamageAfterTraits(before, id,
            sample.GetProperty("Amount").GetInt32(), sample.GetProperty("SourceCardId").GetInt32(), captureAllDispatches: true);
        Require(result.Supported && result.PendingCallbacks.Count == 1, "Missing final damage/removal boundary: " + result.UnsupportedReason);
        var dispatches = result.Dispatches.ToList(); var queue = result.PendingCallbacks.ToList();
        Require(RoomCombatModel.DrainCharacterQueue(queue, item => {
            result = RoomCombatModel.ApplyQueuedCharacterTrigger(result.State!, item, queue.Add);
            dispatches.AddRange(result.Dispatches); return result.Supported;
        }, item => {
            result = item.CompletePhysicalRemovalAfterQueue ? RoomCombatModel.SettleQueuedSpawnerAndCenter(result.State!, item.Unit) :
                RoomCombatModel.SettleQueuedSpawner(result.State!, item.Unit);
            return result.Supported;
        }, () => { result = EnchantmentWorldModel.CompleteQueuedRemovals(result.State!, queue.Add); return result.Supported; },
            () => result.State?.Context, message => result = new RoomCombatResult(null, RoomOutcome.Unsupported, 0, [], message)),
            "Final removal failed: " + result.UnsupportedReason);
        RoomCombatState actual = sample.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, "Final removal state differs: " + difference);
        Require(ModelJson.Difference(JsonSerializer.Serialize(dispatches, ModelJson.Options),
            JsonSerializer.Serialize(sample.GetProperty("Dispatches").ToObjectGraph())) == null, "Final removal dispatches differ.");
        Require(ModelJson.Difference(HalfHealthLifecycleChecks.GameplayRng(sample.GetProperty("RngBefore")),
            HalfHealthLifecycleChecks.GameplayRng(sample.GetProperty("RngAfter"))) == null && JsonSerializer.Serialize(before, ModelJson.Options) == frozen,
            "Final removal mutated its parent or changed gameplay/Unity RNG.");
        CombatUnit target = before.Units.Single(unit => unit.Id == id);
        CombatUnit removed = sample.GetProperty("ActualTarget").Deserialize<CombatUnit>()!;
        Require(removed.Health == 0 && removed.DeathState?.HasFinishedDying == true && removed.DeathState.IsBeingRemoved &&
            !removed.DeathState.HasStatisticsListener && !actual.Units.Any(unit => unit.Id == removed.Id), "Native physical death did not finish removal.");
        bool player = target.Team == CombatTeam.Player;
        Require(!actual.Units.Any(unit => unit.Id == id) && actual.Context!.SpawnPoints!.Units.Single(unit => unit.UnitId == id).Current == null &&
            actual.Context.Gold - before.Context!.Gold == (player ? 190 : 180), "Final removal retained its point or lost gold.");
        if (player) Require(actual.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == target.SpawnerCardId),
            "Final physical death did not exhaust the player spawner.");
        string[] kinds = Kinds(sample);
        Require(kinds.Count(kind => kind == "OnDeath") == 1 && kinds.Count(kind => kind == "OnDeathwish") == 2 &&
            Array.IndexOf(kinds, "OnDeathwish") < Array.IndexOf(kinds, "OnDeath") && !kinds.Contains("OnHeal") &&
            kinds.Contains(player ? "OnAnyMonsterDeathOnFloor" : "OnAnyHeroDeathOnFloor") && kinds.Contains("OnAnyUnitDeathOnFloor"),
            "Final removal callback lifecycle differs.");
    }
    private static FixtureValue[] Capture(FixtureValue root, bool revival, bool finalDeath = false)
    {
        FixtureValue[] samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == (finalDeath ? "NativeBloodshedPunishFinalDeathDamage" : revival ? "NativeBloodshedPunishUndyingLifecycle" : "NativeBloodshedPunishDamageLifecycle") &&
            root.GetProperty("LiveContextUnchanged").GetBoolean() && root.GetProperty("EffectsDrained").GetBoolean() &&
            root.GetProperty("RestorationDifference").ValueKind == FixtureKind.Null && root.GetProperty("Mismatches").GetInt32() == 0 &&
            samples.Length == (finalDeath ? 18 : revival ? 20 : 33) && samples.All(sample => sample.GetProperty("Completed").GetBoolean() &&
                sample.GetProperty("Difference").ValueKind == FixtureKind.Null && sample.GetProperty("GameplayRngUnchanged").GetBoolean() &&
                sample.GetProperty("PendingAfterDrain").GetInt32() == 0), "Incomplete native Bloodshed/Punish calibration.");
        Require(Difference(root.GetProperty("RngBefore"), root.GetProperty("RngAfter")) == null, "Calibration did not restore all RNG streams.");
        return samples;
    }
    private static void Verify(FixtureValue sample, bool finalDeath = false)
    {
        string label = sample.GetProperty("Label").GetString()!;
        RoomCombatState before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        RoomCombatState state = before;
        var dispatches = new List<CharacterTriggerDispatch>();
        FixtureValue input = sample.GetProperty("Input");
        for (int hit = 0; hit < input.GetProperty("Hits").GetInt32(); hit++)
        {
            string frozen = JsonSerializer.Serialize(state, ModelJson.Options);
            RoomCombatResult result = RoomCombatModel.ApplyNativeCardDamageAfterTraits(state, sample.GetProperty("TargetId").GetInt32(),
                input.GetProperty("Amount").GetInt32(), sample.GetProperty("SourceCardId").GetInt32(), sample.GetProperty("AttackerId").GetInt32(),
                captureAllDispatches: true, relicSourced: sample.GetProperty("RelicSourced").GetBoolean());
            Require(result.Supported && result.PendingCallbacks.Count == (finalDeath ? 1 : 0), label + ": " + result.UnsupportedReason);
            if (finalDeath)
            {
                CombatUnit dead = sample.GetProperty("ActualTarget").Deserialize<CombatUnit>()!;
                Require(ModelJson.Difference(JsonSerializer.Serialize(result.RetainedUnits.Single(unit => unit.Id == dead.Id), ModelJson.Options),
                    JsonSerializer.Serialize(dead, ModelJson.Options)) == null, label + ": retained dead actor differs.");
                RoomCombatModel.QueuedCharacterTrigger removal = result.PendingCallbacks.Single();
                Require(removal.Unit.Id == dead.Id && removal.RemovalLifecycle && removal.DeferUntilRemoval &&
                    removal.CompletePhysicalRemovalAfterQueue && removal.HarvestAfterDeath, label + ": deferred physical removal differs.");
            }
            Require(JsonSerializer.Serialize(state, ModelJson.Options) == frozen, label + ": damage mutated an intermediate parent.");
            state = result.State!; dispatches.AddRange(result.Dispatches);
        }
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(state, ModelJson.Options),
            JsonSerializer.Serialize(sample.GetProperty("Actual").Deserialize<RoomCombatState>(), ModelJson.Options));
        Require(difference == null, label + ": state differs: " + difference);
        Require(ModelJson.Difference(JsonSerializer.Serialize(dispatches, ModelJson.Options),
            JsonSerializer.Serialize(sample.GetProperty("Dispatches").ToObjectGraph())) == null, label + ": dispatch payload/order differs.");
        Require(ModelJson.Difference(HalfHealthLifecycleChecks.GameplayRng(sample.GetProperty("RngBefore")),
            HalfHealthLifecycleChecks.GameplayRng(sample.GetProperty("RngAfter"))) == null, label + ": gameplay/Unity RNG changed.");
        Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, label + ": parent mutated.");
    }
    private static void Expect(FixtureValue sample, int hp, int gold, int blood, int punish)
    {
        RoomCombatState before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        RoomCombatState actual = sample.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        int target = sample.GetProperty("TargetId").GetInt32();
        string[] kinds = Kinds(sample);
        CombatUnit unit = actual.Units.FirstOrDefault(unit => unit.Id == target) ?? sample.GetProperty("ActualTarget").Deserialize<CombatUnit>()!;
        Require(unit.Health == hp && actual.Context!.Gold - before.Context!.Gold == gold &&
            kinds.Count(kind => kind == "OnBloodshed") == blood && kinds.Count(kind => kind == "OnPunish") == punish,
            "Native positive/control evidence differs: " + sample.GetProperty("Label").GetString());
    }
    private static string[] Kinds(FixtureValue sample) => sample.GetProperty("Dispatches").EnumerateArray().Select(item => item.GetProperty("Kind").GetString()!).ToArray();
    private static string? Difference(FixtureValue left, FixtureValue right) => ModelJson.Difference(JsonSerializer.Serialize(left.ToObjectGraph()), JsonSerializer.Serialize(right.ToObjectGraph()));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
