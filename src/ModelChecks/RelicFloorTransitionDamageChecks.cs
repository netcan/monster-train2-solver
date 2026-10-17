using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicFloorTransitionDamageChecks
{
    private const string DataId = "269a6f43-9982-4683-ac39-1a862e481898";

    internal static void Run()
    {
        var rules = new[]
        {
            new RelicFloorTransitionDamageRule(0, "PostAscension", 3),
            new RelicFloorTransitionDamageRule(1, "PostDescension", 3)
        };
        var relic = new CombatRelicState(DataId, "DamageOnFloorTransition",
            [RelicModel.DamageEnemyOnAscend, RelicModel.DamageEnemyOnDescend], isCovenant: false,
            disallowedInPlacementPhase: false, floorTransitionDamageRules: rules);
        Require(RelicFloorTransitionDamageModel.Validate(relic) == null, "Exact floor-transition relic rules failed validation.");
        Require(relic.WithRoomEntryStatuses([]).FloorTransitionDamageRules!.Count == 2,
            "Cloning another relic rule discarded floor-transition damage parameters.");
        var missing = new CombatRelicState(DataId, "DamageOnFloorTransition", relic.EffectTypes);
        Require(RelicFloorTransitionDamageModel.Validate(missing) != null, "Missing native floor-transition parameters were accepted.");
        var reversed = new CombatRelicState(DataId, "DamageOnFloorTransition", relic.EffectTypes,
            floorTransitionDamageRules: [rules[1], rules[0]]);
        Require(RelicFloorTransitionDamageModel.Validate(reversed) != null, "Reordered native floor-transition rules were accepted.");
        System.Console.WriteLine("RELIC-FLOOR-TRANSITION-DAMAGE-CHECKS PASS: ordered ascend/descend rules and clone preservation.");
    }

    internal static void Native(FixtureValue root)
    {
        if (!root.TryGetProperty("OriginalDamageOnFloorTransitionPrepared", out FixtureValue prepared) || !prepared.GetBoolean()) return;
        Require(root.GetProperty("Schema").GetInt32() == 182 && root.GetProperty("ModifierScenario").GetString() == "bump" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "DamageOnFloorTransition native battle is incomplete or from another probe schema.");

        FixtureValue[] records = root.GetProperty("BumpOperations").EnumerateArray().ToArray();
        Require(records.Length == 22, "DamageOnFloorTransition fixture did not retain all native movement boundaries.");
        FixtureValue ascend = Record("enemy-up");
        FixtureValue descend = Record("enemy-down");
        FixtureValue rooted = Record("enemy-rooted");
        FixtureValue immobile = Record("enemy-immobile-before-rooted");
        FixtureValue player = Record("player-up");
        CheckOwner(ascend.GetProperty("Before"));
        CheckDamage(ascend, "PostAscension", 3);
        CheckDamage(descend, "PostDescension", 3);
        CheckNoDamage(rooted);
        CheckNoDamage(immobile);
        CheckNoDamage(player);
        Console.WriteLine("ORIGINAL-DAMAGE-ON-FLOOR-TRANSITION PASS: exact 3 damage on native ascend and descend; rooted, immobile, and player movement controls; complete Bump replay.");

        FixtureValue Record(string label) => records.Single(record => record.GetProperty("Label").GetString() == label);
    }

    private static void CheckOwner(FixtureValue before)
    {
        TrainCombatState state = Read<TrainCombatState>(before);
        CombatRelicState relic = state.Context!.Relics!.Single(item => item.DataId == DataId);
        Require(relic.AssetKey == "DamageOnFloorTransition" && relic.IsCovenant == false && relic.DisallowedInPlacementPhase == false &&
            relic.EffectTypes.SequenceEqual([RelicModel.DamageEnemyOnAscend, RelicModel.DamageEnemyOnDescend]) &&
            relic.FloorTransitionDamageRules!.Select(rule => (rule.EffectIndex, rule.Trigger, rule.DamageAmount))
                .SequenceEqual([(0, "PostAscension", 3), (1, "PostDescension", 3)]),
            "Captured original floor-transition relic owner or ordered effect parameters changed.");
    }

    private static void CheckDamage(FixtureValue record, string trigger, int damage)
    {
        TrainCombatState before = Read<TrainCombatState>(record.GetProperty("Before"));
        int actorId = record.GetProperty("Targets")[0].GetInt32();
        CombatUnit actorBefore = Actor(before, actorId);
        CombatUnit actorAfter = Read<CombatUnit[]>(record.GetProperty("TargetsAfter")).Single(unit => unit.Id == actorId);
        Require(actorAfter.Health == actorBefore.Health - damage &&
            record.GetProperty("Dispatched").EnumerateArray().Any(callback => callback.GetProperty("ActorId").GetInt32() == actorId &&
                callback.GetProperty("Kind").GetString() == trigger),
            trigger + " did not apply exactly " + damage + " damage before its native actor callback.");
        var result = BumpModel.Apply(before, [actorId], record.GetProperty("Amount").GetInt32(),
            Read<RoomPlayRule[]>(record.GetProperty("Rooms")), record.GetProperty("SourceCardId").GetInt32());
        Require(result.Supported && JsonSerializer.Serialize(result.State, ModelJson.Options) ==
            JsonSerializer.Serialize(Read<TrainCombatState>(record.GetProperty("After")), ModelJson.Options),
            trigger + " model replay differs from the native movement operation: " + result.UnsupportedReason);
    }

    private static void CheckNoDamage(FixtureValue record)
    {
        TrainCombatState before = Read<TrainCombatState>(record.GetProperty("Before"));
        int actorId = record.GetProperty("Targets")[0].GetInt32();
        CombatUnit actorBefore = Actor(before, actorId);
        CombatUnit actorAfter = Read<CombatUnit[]>(record.GetProperty("TargetsAfter")).Single(unit => unit.Id == actorId);
        Require(actorAfter.Health == actorBefore.Health &&
            !record.GetProperty("Dispatched").EnumerateArray().Any(callback => callback.GetProperty("ActorId").GetInt32() == actorId &&
                callback.GetProperty("Kind").GetString() is "PostAscension" or "PostDescension"),
            record.GetProperty("Label").GetString() + " incorrectly triggered floor-transition damage.");
    }

    private static CombatUnit Actor(TrainCombatState state, int id) => state.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
    private static T Read<T>(FixtureValue value) => value.Deserialize<T>()!;
    private static void Require(bool valid, string message)
    { if (!valid) throw new System.InvalidOperationException(message); }
}
