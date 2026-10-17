using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalPropagateOnStatusEffectAppliedChecks
{
    private const string OwnerId = "274bc27a-9be2-4908-aa84-641944188c08";
    private const string OwnerKey = "PropagateOnStatusEffectApplied";
    private const string EffectType = "RelicEffectAddEffectOnStatusAdded";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 215 &&
            root.GetProperty("OriginalPropagateOnStatusEffectAppliedSchema").GetInt32() == 215 &&
            root.GetProperty("ModifierScenario").GetString() == "original-propagate-on-status-effect-applied" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "PropagateOnStatusEffectApplied battle is incomplete or from an unexpected capture schema.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(field).GetInt32() == 0, "PropagateOnStatusEffectApplied battle has nonzero " + field + ".");

        FixtureValue evidence = root.GetProperty("OriginalPropagateOnStatusEffectApplied");
        FixtureValue definition = evidence.GetProperty("Definition");
        Require(definition.GetProperty("OwnerId").GetString() == OwnerId &&
            definition.GetProperty("AssetKey").GetString() == OwnerKey &&
            definition.GetProperty("EffectType").GetString() == EffectType &&
            !definition.GetProperty("IsCovenant").GetBoolean() &&
            !definition.GetProperty("DisallowedInPlacementPhase").GetBoolean() &&
            definition.GetProperty("SourceTeam").GetInt32() == 255 &&
            definition.GetProperty("RequiredStatusCount").GetInt32() == 3 &&
            definition.GetProperty("ExactCount").GetBoolean() &&
            definition.GetProperty("ConditionCount").GetInt32() == 0 &&
            definition.GetProperty("OuterTargetMode").GetString() == "Room" &&
            definition.GetProperty("CanApplyInPreviewMode").GetBoolean() &&
            definition.GetProperty("CardEffectType").GetString() == "CardEffectAdjustAllStatusEffects" &&
            definition.GetProperty("TargetMode").GetString() == "Self" &&
            definition.GetProperty("TargetTeam").GetString() == "Heroes, Monsters" &&
            definition.GetProperty("Adjustment").GetInt32() == 2 &&
            definition.GetProperty("StatusFilter").GetInt32() == 1 &&
            !definition.GetProperty("RequireValidTargets").GetBoolean() &&
            definition.GetProperty("StatusIds").GetArrayLength() == 0,
            "Native PropagateOnStatusEffectApplied owner or action payload changed.");

        FixtureValue[] tests = evidence.GetProperty("TestCalls").EnumerateArray().ToArray();
        Require(tests.Any(item => item.GetProperty("ActorAssetKey").GetString() == "TrainStewardBig" &&
                item.GetProperty("ParamInt").GetInt32() == 3 && item.GetProperty("Result").GetBoolean()) &&
            tests.Any(item => item.GetProperty("ActorAssetKey").GetString() == "TrainStewardBig" &&
                item.GetProperty("ParamInt").GetInt32() == 4 && !item.GetProperty("Result").GetBoolean()),
            "Native exact-three positive or over-three control was not observed on the configured steward.");

        FixtureValue[] applications = evidence.GetProperty("Applications").EnumerateArray().ToArray();
        FixtureValue application = applications.FirstOrDefault(item =>
            item.GetProperty("ActorAssetKey").GetString() == "TrainStewardBig" && item.GetProperty("ParamInt").GetInt32() == 3);
        Require(application.ValueKind != FixtureKind.Undefined,
            "Native exact-three callback did not execute the relic effect.");
        Require(application.GetProperty("Completed").GetBoolean(),
            "Native exact-three relic application did not finish its asynchronous effect queue.");
        var before = Statuses(application.GetProperty("Before"));
        var after = Statuses(application.GetProperty("After"));
        foreach (string id in new[] { "valor", "spikes", "regen", "damage shield" })
            Require(before.GetValueOrDefault(id) == 1 && after.GetValueOrDefault(id) == 3,
                "Native propagated status did not gain exactly two stacks: " + id + ".");
        Console.WriteLine("ORIGINAL-PROPAGATE-ON-STATUS-EFFECT-APPLIED-NATIVE PASS: exact-three, over-count control and four-status self-only +2 propagation.");
    }

    internal static void Run()
    {
        var rng = UnityRng.Seed(91018);
        CombatStatus Status(string id, int stacks, string category, bool propagatable, bool excludeHero = false,
            bool excludeMonster = false) => new(id, stacks, stackable: true, hidden: false, displayCategory: category,
                propagatable: propagatable, excludeHeroPropagation: excludeHero, excludeMonsterPropagation: excludeMonster);
        CombatStatus valor = Status("valor", 1, "Positive", true, excludeHero: true);
        CombatStatus regen = Status("regen", 1, "Positive", true, excludeHero: true);
        CombatStatus weakness = Status("spell weakness", 1, "Negative", true, excludeMonster: true);
        CombatStatus persistent = Status("ephemeral", 1, "Persistent", false);
        var action = new RelicStatusAddedEffect(3, true, "Room", true, "CardEffectAdjustAllStatusEffects",
            "Self", true, true, 2, 1, false, []);
        var trigger = new RelicCharacterTriggers(0, true, true, [], false,
            requiredSubtype: "SubtypesData_None", subtypeIsNone: true, statusAddedEffect: action);
        var owner = new CombatRelicState(OwnerId, OwnerKey, [EffectType], isCovenant: false,
            disallowedInPlacementPhase: false, triggerAdditions: [trigger]);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10,
            statusRules: [Status("armor", 0, "Positive", true, excludeHero: true)], relics: [owner]);
        CombatUnit Unit(int id, CombatTeam team, CombatStatus[] statuses) => new(id, "status-propagator",
            team, 0, 20, 20, false, false, false, statuses, statusRegistry: statuses);
        RoomCombatState Room(CombatUnit unit, bool preview = false, CombatContext? scope = null) =>
            new(0, false, [unit], [], scope ?? context, preview);
        CombatUnit actor = Unit(1, CombatTeam.Player, [valor, regen, persistent]);
        RoomCombatState parent = Room(actor);
        string parentJson = JsonSerializer.Serialize(parent);
        RoomCombatResult hit = ApplyWithCallbacks(parent, weakness, actor.Id);
        Require(hit.Supported && hit.State!.Units.Single().Status("valor")?.Stacks == 3 &&
            hit.State.Units.Single().Status("regen")?.Stacks == 3 &&
            hit.State.Units.Single().Status("spell weakness")?.Stacks == 1 &&
            hit.State.Units.Single().Status("ephemeral")?.Stacks == 1,
            "Exact-three failed, included a team-excluded/nonpropagatable status, or changed the persistent status.");

        CombatUnit spawnActor = Unit(6, CombatTeam.Player, [valor, regen, weakness]);
        RoomCombatResult born = RoomCombatModel.ApplySpawnTriggers(Room(spawnActor), spawnActor.Id, fromCard: false,
            startingApplications: [valor, regen, weakness]);
        string birthState = born.Supported
            ? string.Join(",", born.State!.Units.Single().Statuses.Select(status => status.Id + ":" + status.Stacks))
            : born.UnsupportedReason!;
        Require(born.Supported && born.State!.Units.Single().Status("valor")?.Stacks == 3 &&
            born.State.Units.Single().Status("regen")?.Stacks == 3 &&
            born.State.Units.Single().Status("spell weakness")?.Stacks == 1,
            "Local birth callbacks did not deliver the status-added relic event after all starting statuses were installed: " + birthState);

        RoomCombatResult removed = RemoveWithCallbacks(hit.State!, weakness.Id, actor.Id);
        RoomCombatResult reset = removed.Supported ? ApplyWithCallbacks(removed.State!, weakness, actor.Id) : removed;
        Require(reset.Supported && reset.State!.Units.Single().Status("valor")?.Stacks == 5 &&
            reset.State.Units.Single().Status("regen")?.Stacks == 5 &&
            reset.State.Units.Single().Status("spell weakness")?.Stacks == 1,
            "Removing and re-adding a zero-count dictionary status did not retrigger the exact-three effect.");

        CombatStatus shield = Status("damage shield", 1, "Positive", true, excludeHero: true);
        RoomCombatResult over = ApplyWithCallbacks(Room(Unit(2, CombatTeam.Player, [valor, regen, shield, persistent])), weakness, 2);
        Require(over.Supported && over.State!.Units.Single().Status("valor")?.Stacks == 1 &&
            over.State.Units.Single().Status("regen")?.Stacks == 1 && over.State.Units.Single().Status("damage shield")?.Stacks == 1,
            "The exact-count effect fired after the actor already had three visible status types.");

        CombatStatus poison = Status("corrupt_poison", 1, "Negative", true, excludeMonster: true);
        CombatStatus meleeWeakness = Status("melee weakness", 1, "Negative", true, excludeMonster: true);
        CombatStatus spellWeakness = Status("spell weakness", 1, "Negative", true, excludeMonster: true);
        RoomCombatResult hero = ApplyWithCallbacks(Room(Unit(3, CombatTeam.Enemy,
            [spellWeakness, poison, persistent])), meleeWeakness, 3);
        Require(hero.Supported && hero.State!.Units.Single().Status("spell weakness")?.Stacks == 3 &&
            hero.State.Units.Single().Status("corrupt_poison")?.Stacks == 3 &&
            hero.State.Units.Single().Status("melee weakness")?.Stacks == 3,
            "The source-team mask did not include native Heroes (the model's Enemy team).");

        CombatUnit belowActor = Unit(4, CombatTeam.Player, [valor, persistent]);
        RoomCombatResult below = ApplyWithCallbacks(Room(belowActor), weakness, belowActor.Id);
        Require(below.Supported && below.State!.Units.Single().Status("valor")?.Stacks == 1,
            "The relic fired below its required unique status count.");
        RoomCombatResult preview = ApplyWithCallbacks(Room(actor, preview: true), weakness, actor.Id);
        Require(preview.Supported && preview.State!.Units.Single().Status("valor")?.Stacks == 3,
            "The native preview-enabled callback was suppressed in preview state.");

        CombatStatus unknownPropagation = new("spikes", 1, stackable: true, hidden: false,
            displayCategory: "Positive", propagatable: true);
        CombatUnit unknownActor = Unit(5, CombatTeam.Player, [valor, unknownPropagation]);
        RoomCombatResult unknown = ApplyWithCallbacks(Room(unknownActor), weakness, unknownActor.Id);
        Require(!unknown.Supported && unknown.UnsupportedReason!.Contains("team-propagation metadata", StringComparison.Ordinal),
            "Unknown team-propagation metadata was guessed instead of rejected.");

        var malformed = new CombatRelicState(OwnerId, OwnerKey, [EffectType], isCovenant: false,
            disallowedInPlacementPhase: false, triggerAdditions: [new RelicCharacterTriggers(0, true, true, [], false,
                subtypeIsNone: true, statusAddedEffect: new RelicStatusAddedEffect(3, false, "Room", true,
                    "CardEffectAdjustAllStatusEffects", "Room", true, true, 2, 1, false, []))]);
        var malformedContext = new CombatContext(context.Cards, rng, 0, 1, 10,
            statusRules: context.StatusRules, relics: [malformed]);
        RoomCombatResult rejected = ApplyWithCallbacks(Room(actor, scope: malformedContext), weakness, actor.Id);
        Require(!rejected.Supported && JsonSerializer.Serialize(parent) == parentJson,
            "Malformed target/count metadata passed validation or changed the source branch.");
        Parallel.For(0, 24, _ =>
        {
            RoomCombatResult branch = ApplyWithCallbacks(parent, weakness, actor.Id);
            Require(branch.Supported && branch.State!.Units.Single().Status("valor")?.Stacks == 3 &&
                JsonSerializer.Serialize(parent) == parentJson, "Concurrent propagated-status branch changed its parent.");
        });
        Console.WriteLine("ORIGINAL-PROPAGATE-ON-STATUS-EFFECT-APPLIED-CHECKS PASS: exact/under/over count, both teams, preview, reset/reapply, team exclusions, unknown metadata and immutable branches.");
    }

    private static System.Collections.Generic.Dictionary<string, int> Statuses(FixtureValue statuses) =>
        statuses.EnumerateArray().ToDictionary(item => item.GetProperty("Id").GetString()!,
            item => item.GetProperty("Stacks").GetInt32(), System.StringComparer.Ordinal);

    private static RoomCombatResult ApplyWithCallbacks(RoomCombatState source, CombatStatus status, int targetId) =>
        DrainCallbacks(StatusApplicationModel.Apply(source, targetId, status));

    private static RoomCombatResult RemoveWithCallbacks(RoomCombatState source, string statusId, int targetId) =>
        DrainCallbacks(StatusRemovalModel.Remove(source, targetId, statusId, int.MaxValue));

    private static RoomCombatResult DrainCallbacks(RoomCombatResult applied)
    {
        if (!applied.Supported) return applied;
        var callbacks = new System.Collections.Generic.Queue<RoomCombatModel.QueuedCharacterTrigger>(applied.PendingCallbacks);
        RoomCombatResult settled = applied;
        int processed = 0;
        while (callbacks.Count > 0)
        {
            if (++processed > 64)
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0,
                    new System.Collections.Generic.List<CombatEvent>(), "Status callback queue did not settle.");
            RoomCombatModel.QueuedCharacterTrigger queued = callbacks.Dequeue();
            RoomCombatResult callback = RoomCombatModel.ApplyQueuedCharacterTrigger(settled.State!, queued, callbacks.Enqueue);
            if (!callback.Supported) return callback;
            settled = callback;
        }
        return settled;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}
