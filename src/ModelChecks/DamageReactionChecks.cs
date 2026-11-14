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
        RoomCombatState preview = new RoomCombatState(modern.RoomIndex, modern.Deployment, modern.Units,
            modern.ExternalInteractions, modern.Context, true, modern.ActiveCompanionBossIds);
        Require(RoomCombatModel.Validate(preview)?.Contains("preview callbacks", StringComparison.Ordinal) == true,
            "Uncalibrated Bloodshed/Punish preview effects were accepted.");
        RoomCombatResult lethal = RoomCombatModel.ApplyNativeCardDamageAfterTraits(modern, samples[0].GetProperty("TargetId").GetInt32(), 99999, 0);
        Require(!lethal.Supported && lethal.UnsupportedReason?.Contains("death/revival", StringComparison.Ordinal) == true,
            "Uncalibrated lethal Bloodshed/Punish effects were accepted.");
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
        Console.WriteLine("DAMAGE-REACTION-REVIVAL PASS: 20 full native Undying successors, lethal friendly/enemy/self/relic sources, dead callback target controls, half-health queue before revival, one-HP gain/loss, active-half controls, reanimated/death healing, twice-revived once-listener, full dispatch/RNG/state and 32 immutable branches. Final death and preview remain guarded.");
    }
    private static FixtureValue[] Capture(FixtureValue root, bool revival)
    {
        FixtureValue[] samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == (revival ? "NativeBloodshedPunishUndyingLifecycle" : "NativeBloodshedPunishDamageLifecycle") &&
            root.GetProperty("LiveContextUnchanged").GetBoolean() && root.GetProperty("EffectsDrained").GetBoolean() &&
            root.GetProperty("RestorationDifference").ValueKind == FixtureKind.Null && root.GetProperty("Mismatches").GetInt32() == 0 &&
            samples.Length == (revival ? 20 : 33) && samples.All(sample => sample.GetProperty("Completed").GetBoolean() &&
                sample.GetProperty("Difference").ValueKind == FixtureKind.Null && sample.GetProperty("GameplayRngUnchanged").GetBoolean() &&
                sample.GetProperty("PendingAfterDrain").GetInt32() == 0), "Incomplete native Bloodshed/Punish calibration.");
        Require(Difference(root.GetProperty("RngBefore"), root.GetProperty("RngAfter")) == null, "Calibration did not restore all RNG streams.");
        return samples;
    }
    private static void Verify(FixtureValue sample)
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
            Require(result.Supported && result.PendingCallbacks.Count == 0, label + ": " + result.UnsupportedReason);
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
        Require(actual.Units.Single(unit => unit.Id == target).Health == hp && actual.Context!.Gold - before.Context!.Gold == gold &&
            kinds.Count(kind => kind == "OnBloodshed") == blood && kinds.Count(kind => kind == "OnPunish") == punish,
            "Native positive/control evidence differs: " + sample.GetProperty("Label").GetString());
    }
    private static string[] Kinds(FixtureValue sample) => sample.GetProperty("Dispatches").EnumerateArray().Select(item => item.GetProperty("Kind").GetString()!).ToArray();
    private static string? Difference(FixtureValue left, FixtureValue right) => ModelJson.Difference(JsonSerializer.Serialize(left.ToObjectGraph()), JsonSerializer.Serialize(right.ToObjectGraph()));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
