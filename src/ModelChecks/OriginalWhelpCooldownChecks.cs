using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalWhelpCooldownChecks
{
    private static CombatStatus Cooldown(int count) => new("cooldown", count, removeStackAtEnd: true,
        preventRemovalDuringRelentless: true, stackable: true, hidden: true, displayCategory: "Persistent");
    private static CombatRelicState Owner(int amount = 2)
    {
        CardActionEffect Action(string target) => new("AddStatus", target, 0, false, true, [Cooldown(-amount)],
            tests: new(true, false, false, false, true), filters: new("Both", [], [], false, "", []));
        return new(RelicUnitSpawnEffectModel.DataId, RelicUnitSpawnEffectModel.AssetKey, [RelicUnitSpawnEffectModel.Effect],
            isCovenant: false, disallowedInPlacementPhase: false,
            resourceEffects: [new(0, RelicUnitSpawnEffectModel.Effect, amount, 0, amount, false,
                conditions: [new(new("SubtypeInDeck"), true, 6, 1, false)],
                unitSpawnEffect: new("Monsters", RelicUnitSpawnEffectModel.Subtype, Action("Tower"), [],
                    [new("OnSpawn", false, false, false, [Action("Room")])]))]);
    }
    internal static void Run()
    {
        Require(RelicModel.Validate([Owner()]) == null && RelicModel.Validate([Owner(3)]) != null, "Original Whelp payload guard failed.");
        var rng = UnityRng.Seed(263);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10, [Cooldown(0)],
            relics: [Owner()], queryFrame: new(energy: 0, runningCombat: true, turn: 0, selectedRoomIndex: 0),
            energyState: new(99, 0, 0, "MonsterTurn", true),
            resources: new(3, 0, 5, 99, 10, "pyre", 20, 80, 0, 20, [], [], []));
        CombatUnit Unit(int id, CombatTeam team, int stacks, bool whelp = false, bool pyre = false) => new(id, "unit" + id,
            team, 1, 20, 20, true, pyre, false, [Cooldown(stacks)], subtypes: whelp ? [RelicUnitSpawnEffectModel.Subtype] : []);
        var actor = Unit(1, CombatTeam.Player, 8, true);
        var root = new TrainCombatState([new(0, true, [actor], [], context),
            new(1, true, [Unit(2, CombatTeam.Player, 1), Unit(3, CombatTeam.Enemy, 8)], [], context),
            new(2, true, [], [], context),
            new(3, true, [Unit(4, CombatTeam.Player, 8, pyre: true)], [], context)], [], 6, context);
        string parent = JsonSerializer.Serialize(root, ModelJson.Options);
        var bonus = RelicUnitSpawnEffectModel.Apply(root, 1, "OnSpawn", true);
        Require(bonus.Supported, bonus.UnsupportedReason!);
        int Stacks(TrainCombatState state, int id) => state.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id).Status("cooldown")?.Stacks ?? 0;
        Require(Stacks(bonus.State!, 1) == 6 && Stacks(bonus.State!, 2) == 0 && Stacks(bonus.State!, 3) == 8 && Stacks(bonus.State!, 4) == 8,
            "Whelp spawn must shorten friendly Tower cooldown, clamp zero and exclude enemy/Pyre.");
        Require(Resource(bonus.State!).Conditions.Single().DurationTriggerCount == 2, "Raw effect and manager notifications must both record.");
        Equal(RelicUnitSpawnEffectModel.Apply(bonus.State!, 1, "OnSpawn", true).State, bonus.State, "repeat manager gate");
        var next = CardSpellModel.WithContext(bonus.State!, RelicConditionModel.EndDuration(bonus.State!.Context!, "ThisTurn"));
        var again = RelicUnitSpawnEffectModel.Apply(next, 1, "OnSpawn", true);
        Require(again.Supported && Stacks(again.State!, 1) == 4, "Next-turn reset did not restore eligibility.");
        var raw = RelicUnitSpawnEffectModel.Apply(bonus.State!, 1, "OnDeath", false);
        Require(raw.Supported && Stacks(raw.State!, 1) == 4 && Resource(raw.State!).Conditions.Single().DurationTriggerCount == 3,
            "Raw Apply incorrectly reused Test/condition gates.");
        Require(!RelicUnitSpawnEffectModel.Test(actor, "OnDeath") && !RelicUnitSpawnEffectModel.Test(null, "OnSpawn") &&
            !RelicUnitSpawnEffectModel.Test(Unit(5, CombatTeam.Enemy, 3, true), "OnSpawn") &&
            !RelicUnitSpawnEffectModel.Test(Unit(6, CombatTeam.Player, 3), "OnSpawn"), "Native trigger/team/subtype/null gates failed.");
        Require(!RelicUnitSpawnEffectModel.Check(root.Context!, actor, "OnSpawn", true).Passed, "Preview manager gate failed.");
        TrainCombatState removed = CardSpellModel.WithContext(root, root.Context!.WithRelics([]));
        Equal(RelicUnitSpawnEffectModel.Apply(removed, 1, "OnSpawn", true).State, removed, "owner ablation");
        Parallel.For(0, 16, _ => Equal(RelicUnitSpawnEffectModel.Apply(root, 1, "OnSpawn", true).State, bonus.State, "parallel spawn"));
        Require(JsonSerializer.Serialize(root, ModelJson.Options) == parent, "Whelp effect mutated its parent.");
        Console.WriteLine("WHELP-COOLDOWN-PURE PASS: exact definition, Tower targets, signed clamp, deployment, repeat/reset, raw/managed notifications, Test/preview controls and 16 immutable branches.");
    }
    private static RelicResourceEffect Resource(TrainCombatState state) => state.Context!.Relics!.Single(owner => owner.DataId == RelicUnitSpawnEffectModel.DataId).ResourceEffects!.Single();
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 238 && root.GetProperty("OriginalWhelpCooldownPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete Whelp cooldown battle.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalWhelpCooldownCaptureFailures",
            "OriginalWhelpCooldownMismatches", "OriginalWhelpCooldownUnsupported", "OriginalWhelpCooldownPending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Nonzero " + field);
        int tests = 0, applications = 0, managers = 0, activeManagers = 0, clamped = 0, abilityClamped = 0, crossRoom = 0, repeats = 0, rejected = 0;
        var turns = new HashSet<int>();
        bool controlPositive = false, nullControl = false, triggerControl = false, subtypeControl = false, teamControl = false;
        foreach (var row in root.GetProperty("RelicWhelpCooldownRecords").EnumerateArray())
        {
            string kind = row.GetProperty("Kind").GetString()!, trigger = row.GetProperty("Trigger").GetString()!;
            int actorId = row.GetProperty("ActorId").GetInt32();
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            CombatUnit? actor = before.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == actorId);
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                if (kind == "Test") { Require(RelicUnitSpawnEffectModel.Test(actor, trigger) == row.GetProperty("Returned").GetBoolean(), "Native Test result differs."); Equal(before, actual, "Test state"); }
                else { var result = RelicUnitSpawnEffectModel.Apply(before, actorId, trigger, kind == "Manager");
                    Require(result.Supported, result.UnsupportedReason!); Equal(result.State, actual, "native " + kind); }
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Native replay mutated its parent.");
            if (kind == "Test")
            {
                tests++; bool returned = row.GetProperty("Returned").GetBoolean(); if (!returned) rejected++;
                if (row.GetProperty("Control").GetBoolean())
                {
                    controlPositive |= returned && actor?.Subtypes.Contains(RelicUnitSpawnEffectModel.Subtype) == true && Resource(before).Conditions.Single().Triggered;
                    nullControl |= actorId == 0 && !returned;
                    triggerControl |= trigger == "OnDeath" && actor?.Subtypes.Contains(RelicUnitSpawnEffectModel.Subtype) == true && !returned;
                    subtypeControl |= trigger == "OnSpawn" && actor?.Team == CombatTeam.Player && !actor.Subtypes.Contains(RelicUnitSpawnEffectModel.Subtype) && !returned;
                }
                teamControl |= trigger == "OnSpawn" && actor?.Team == CombatTeam.Enemy && !returned;
            }
            if (kind == "Manager")
            {
                managers++;
                if (row.GetProperty("Notifications").GetInt32() > 0) { activeManagers++; Require(row.GetProperty("Notifications").GetInt32() == 2, "Manager notification count differs."); }
                else if (trigger == "OnSpawn" && RelicUnitSpawnEffectModel.Test(actor, trigger) && Resource(before).Conditions.Single().Triggered) repeats++;
            }
            if (kind != "Apply") continue;
            applications++; turns.Add(before.Context!.QueryFrame!.Turn!.Value);
            Require(row.GetProperty("Notifications").GetInt32() == 1 && Resource(actual).Conditions.Single().DurationTriggerCount == Resource(before).Conditions.Single().DurationTriggerCount + 1,
                "Raw effect notification count differs.");
            int roomIndex = before.Rooms.Single(room => room.Units.Any(unit => unit.Id == actorId)).RoomIndex;
            foreach (CombatUnit prior in before.Rooms.SelectMany(room => room.Units).Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre))
            {
                CombatUnit changed = actual.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == prior.Id);
                int old = prior.Status("cooldown")?.Stacks ?? 0, value = changed.Status("cooldown")?.Stacks ?? 0;
                Require(value == Math.Max(0, old - 2), "Tower cooldown amount differs.");
                if (old == 1 && value == 0) clamped++;
                if (old == 1 && value == 0 && prior.Ability?.HasAbility == true)
                {
                    Require(prior.Triggers.Any(trigger => trigger.Kind == "OnUnitAbilityAvailable") &&
                        prior.Triggers.Any(trigger => trigger.Kind == "OnUnitAbilityUnavailable") &&
                        actual.Context!.Gold == before.Context!.Gold &&
                        changed.Status("unit_ability_available")?.Stacks == prior.Status("unit_ability_available")?.Stacks,
                        "Signed cooldown addition must not fire ability-ready callbacks or change its native availability marker.");
                    abilityClamped++;
                }
                if (old > value && before.Rooms.Single(room => room.Units.Any(unit => unit.Id == prior.Id)).RoomIndex != roomIndex) crossRoom++;
            }
        }
        Require(tests > 0 && rejected >= 3 && controlPositive && nullControl && triggerControl && subtypeControl && teamControl &&
            applications == 2 && activeManagers == 2 && repeats >= 2 && clamped > 0 && abilityClamped > 0 && crossRoom > 0 && turns.SetEquals([0, 1]) &&
            root.GetProperty("Actions").GetArrayLength() == 6, "Missing deployment/reset, repeat, control, zero clamp or Tower witnesses.");
        Console.WriteLine($"ORIGINAL-WHELP-COOLDOWN-NATIVE PASS: {tests} Tests, {applications} applications, {managers} manager calls, two turn gates, real ability zero without ready callbacks, cross-floor/clamp controls and 16 immutable branches.");
    }
    private static void Equal<T>(T expected, T actual, string label)
    { string a = JsonSerializer.Serialize(expected, ModelJson.Options), b = JsonSerializer.Serialize(actual, ModelJson.Options); Require(a == b, label + ": " + ModelJson.Difference(a, b)); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
