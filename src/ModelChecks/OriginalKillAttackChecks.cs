using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalKillAttackChecks
{
    private static CombatRelicState Owner(int damage = 3, string id = RelicKillAttackModel.DataId) =>
        new(id, RelicKillAttackModel.AssetKey, [RelicModel.ModifyCharacterAttackDamage], isCovenant: false,
            disallowedInPlacementPhase: false, unitUpgradeTriggers: [new(0, "OnKill", true, false, null, true, false,
                RelicUnitUpgradeTriggerModel.UntilEndOfBattle, new(RelicKillAttackModel.UpgradeId,
                    "AddAttackOnKillTriggerUpgrade", new(damage: damage), [], false, false, false, 0, 0, []))]);

    internal static void Run()
    {
        CombatUnit Unit(bool clone = false, bool matches = true, CombatTeam team = CombatTeam.Player, bool pyre = false) =>
            new(1, "carrier", team, 4, 20, 20, true, pyre, false, [], [], spawnerCardId: 8,
                modifiers: new(4, 0, 0, 1, 1, true, clone, [], spawnerMatchesDefinition: matches));
        UnityRng rng = UnityRng.Seed(630);
        RoomCombatState State(CombatUnit unit, bool preview = false, TriggerCountState? counts = null) => new(0, false, [unit], [],
            new(new([], [], [], rng, 0, []), rng, 0, 1, 10, relics: [Owner()],
                cardInstances: [CardInstanceState.Empty(8, "carrier-card")], triggerCounts: counts), preview);
        RoomCombatResult Fire(RoomCombatState room, string kind = "OnKill") => RoomCombatModel.ApplyQueuedCharacterTrigger(room,
            new(0, room.Units.Single(), kind), _ => { });
        RoomCombatState source = State(Unit());
        string parent = JsonSerializer.Serialize(source, ModelJson.Options);
        RoomCombatResult once = Fire(source), twice = Fire(once.State!);
        Require(once.Supported && twice.Supported && once.State!.Units.Single().BaseAttack == 7 &&
            twice.State!.Units.Single().BaseAttack == 10 && twice.State.Context!.FindCard(8)!.Temporary.Upgrades.Count == 2,
            "Kill attack must stack on both unit and source card.");
        Require(Fire(source, "OnHeal").State!.Units.Single().BaseAttack == 4 &&
            Fire(State(Unit(clone: true))).State!.Units.Single().BaseAttack == 4 &&
            Fire(State(Unit(team: CombatTeam.Enemy))).State!.Units.Single().BaseAttack == 4 &&
            !RelicKillAttackModel.Eligible(Unit().WithHealth(0), "OnKill") &&
            !RelicKillAttackModel.Eligible(Unit().WithDeathState(new(false, false, false, isDestroyed: true)), "OnKill"),
            "Missing trigger, clone, team or death gates.");
        RoomCombatResult mismatch = Fire(State(Unit(matches: false))), preview = Fire(State(Unit(), true));
        Require(mismatch.Supported && mismatch.State!.Units.Single().BaseAttack == 7 && mismatch.State.Context!.FindCard(8)!.Temporary.Upgrades.Count == 0 &&
            preview.Supported && preview.State!.Units.Single().BaseAttack == 7 && preview.State.Context!.FindCard(8)!.Temporary.Upgrades.Count == 0,
            "Mismatched spawner/preview must retain unit-only application.");
        Require(RelicKillAttackModel.Eligible(Unit(pyre: true), "OnKill"), "Native Test has no Pyre exclusion.");
        RoomCombatResult counted = Fire(State(Unit(), counts: new([new("OnKill", 2)], [], [], [])));
        Require(counted.Supported && counted.State!.Units.Single().BaseAttack == 7 &&
            counted.State.Context!.FindCard(8)!.Temporary.Upgrades.Count == 1,
            "This relic applies once per callback without querying character trigger-count modifiers.");
        Require(RelicModel.Validate([Owner(damage: 4)]) != null && RelicModel.Validate([Owner(id: "changed")]) != null,
            "Changed original kill-attack payload must be rejected.");
        Parallel.For(0, 16, _ => Require(Fire(source).State!.Units.Single().BaseAttack == 7, "Parallel kill attack diverged."));
        Require(JsonSerializer.Serialize(source, ModelJson.Options) == parent, "Kill attack mutated its parent.");
        Console.WriteLine("KILL-ATTACK-PURE PASS: cumulative unit/source-card upgrades, trigger/team/clone/death gates, preview/mismatched-spawner controls, strict owner payload and immutable parallel branches.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 226 && root.GetProperty("OriginalKillAttackSchema").GetInt32() == 226 &&
            root.GetProperty("ModifierScenario").GetString() == "original-kill-attack" && root.GetProperty("OriginalKillAttackPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalKillAttackCaptureFailures",
                "OriginalKillAttackMismatches", "OriginalKillAttackUnsupported", "OriginalKillAttackPending" }
                .All(name => root.GetProperty(name).GetInt32() == 0), "Native kill-attack acceptance failed.");
        int[] cards = root.GetProperty("OriginalKillAttack").GetProperty("CardIds").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(cards.Length == 2 && actions.Length == 2 && actions.All(row => row.GetProperty("ScenarioAction").GetBoolean()) &&
            actions.Select(row => row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(cards),
            "Missing complete natural-combat deployment.");
        int positives = 0, gated = 0;
        foreach (FixtureValue row in root.GetProperty("RelicKillAttackTests").EnumerateArray())
        {
            CombatUnit actor = row.GetProperty("Actor").Deserialize<CombatUnit>()!;
            string kind = row.GetProperty("Trigger").GetString()!;
            bool result = row.GetProperty("Result").GetBoolean();
            Require(result == RelicKillAttackModel.Eligible(actor, kind), "Native kill-attack Test differs.");
            Equal(row.GetProperty("Before").Deserialize<TrainCombatState>(), row.GetProperty("Actual").Deserialize<TrainCombatState>(), "Test state/RNG");
            if (result) positives++; else gated++;
        }
        var applications = new List<(int Actor, int Attack)>();
        foreach (FixtureValue row in root.GetProperty("RelicKillAttackApplications").EnumerateArray())
        {
            CombatUnit actor = row.GetProperty("Actor").Deserialize<CombatUnit>()!;
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            CombatRelicState owner = before.Context!.Relics!.Single(item => item.DataId == RelicKillAttackModel.DataId);
            Require(RelicKillAttackModel.Validate(owner) == null && row.GetProperty("Completed").GetBoolean() &&
                RelicKillAttackModel.Eligible(actor, "OnKill") && actor.Triggers.Count == 0,
                "Missing unchanged owner or isolated completed natural kill callback.");
            RoomCombatState room = before.Rooms.Single(item => item.Units.Any(unit => unit.Id == actor.Id));
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                var children = new List<RoomCombatModel.QueuedCharacterTrigger>();
                RoomCombatResult fired = RoomCombatModel.ApplyQueuedCharacterTrigger(room,
                    new(room.RoomIndex, actor, "OnKill", admission: RoomCombatModel.CharacterTriggerAdmission.Accepted), children.Add);
                Require(fired.Supported && children.Count == 0, "Kill attack is unsupported or introduced child triggers: " + fired.UnsupportedReason);
                TrainCombatState predicted = CardSpellModel.WithContext(new(before.Rooms.Select(item => item.RoomIndex == room.RoomIndex
                    ? fired.State! : item).ToArray(), before.Movement, before.EnemySlotsPerRoom, fired.State!.Context), fired.State.Context!);
                Equal(predicted, actual, "unit/source-card upgrade");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Kill attack mutated its parent.");
            CombatUnit changed = actual.Rooms.SelectMany(item => item.Units).Single(unit => unit.Id == actor.Id);
            Require(changed.BaseAttack == actor.BaseAttack + 3 &&
                actual.Context!.FindCard(actor.SpawnerCardId)!.Temporary.Upgrades.Count ==
                    before.Context.FindCard(actor.SpawnerCardId)!.Temporary.Upgrades.Count + 1,
                "Natural kill failed to add exactly +3 and one source-card upgrade.");
            applications.Add((actor.Id, actor.BaseAttack));
        }
        Require(applications.Count >= 2 && positives == applications.Count && gated > positives &&
            applications.GroupBy(item => item.Actor).Any(group => group.Select(item => item.Attack).Distinct().Count() > 1),
            "Missing repeated natural-kill stacking and gated controls.");
        Console.WriteLine($"ORIGINAL-KILL-ATTACK-NATIVE PASS: {positives} natural kill callbacks, {gated} gated Tests, cumulative +3 unit/source-card upgrades, full states/RNG and immutable parallel replay.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string a = JsonSerializer.Serialize(predicted, ModelJson.Options), b = JsonSerializer.Serialize(actual, ModelJson.Options);
        Require(a == b, "Kill attack " + label + " differs: " + ModelJson.Difference(a, b));
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
