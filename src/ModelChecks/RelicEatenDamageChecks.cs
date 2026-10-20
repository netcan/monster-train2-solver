using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicEatenDamageChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(583);
        var effect = new RelicResourceEffect(0, RelicFriendlyTriggerDamageModel.Effect, 5, 0, 5, true,
            unitTrigger: new("OnEaten", "Monsters", null, []), unitDamage: new("Weakest"));
        var owner = new CombatRelicState(RelicFriendlyTriggerDamageModel.DataId, RelicFriendlyTriggerDamageModel.AssetKey,
            [RelicFriendlyTriggerDamageModel.Effect], resourceEffects: [effect], disallowedInPlacementPhase: false);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10, relics: [owner],
            energyState: new(10, 0, 0, "MonsterTurn", true),
            resources: new(3, 0, 5, 10, 10, "pyre", 0, 80, 0, 0, [], [], []));
        CombatUnit Unit(int id, CombatTeam team, int hp, bool pyre = false) => new(id, "carrier", team, 0, hp, hp, false, pyre, false, [], []);
        CombatUnit actor = Unit(3, CombatTeam.Player, 40);
        var train = new TrainCombatState([new(0, true, [Unit(1, CombatTeam.Enemy, 12), Unit(2, CombatTeam.Enemy, 6), actor], [], context)], [], 6, context);
        string parent = JsonSerializer.Serialize(train, ModelJson.Options);
        Parallel.For(0, 16, _ => {
            RelicCardPlayDamageSelection selected = RelicFriendlyTriggerDamageModel.Select(train, actor, "OnEaten", 0, 0, 0);
            Require(selected.Supported && selected.Targets.SequenceEqual([2]) && ReferenceEquals(selected.State, train),
                "OnEaten must select the weakest enemy without RNG, even behind a stronger front unit: " + selected.UnsupportedReason);
        });
        foreach (var (subject, trigger) in new[] { (actor, "OnSpawn"), (Unit(4, CombatTeam.Enemy, 5), "OnEaten"), (Unit(5, CombatTeam.Player, 80, true), "OnEaten") })
            Require(RelicFriendlyTriggerDamageModel.Select(train, subject, trigger, 0, 0, 0).Targets.Count == 0,
                "Wrong-trigger, enemy-actor and Pyre controls must not apply.");
        var empty = new TrainCombatState([new(0, true, [actor], [], context)], [], 6, context);
        Require(RelicFriendlyTriggerDamageModel.Select(empty, actor, "OnEaten", 0, 0, 0).Targets.Count == 0,
            "An empty enemy room must not apply or change RNG.");
        var retained = new TrainCombatState([new(0, true, [Unit(1, CombatTeam.Enemy, 0), Unit(2, CombatTeam.Enemy, 6), actor], [], context)], [], 6, context);
        Require(RelicFriendlyTriggerDamageModel.Select(retained, actor, "OnEaten", 0, 0, 0).Targets.SequenceEqual([2]),
            "A retained dead enemy must not win weakest-target selection.");
        var triggerCard = new CardActionEffect("PlayUnitTrigger", "FrontInRoom", 0, false, true, [], unitTriggerKind: "OnEaten");
        RoomCombatResult deployment = CardSpellModel.Apply(train.Rooms[0], [triggerCard], 0);
        Require(deployment.Supported && deployment.State!.Units[1].Health == 6,
            "Native QueueTrigger must suppress OnEaten damage during deployment: " + deployment.UnsupportedReason);
        SpellCastCheck uncalibrated = CardSpellModel.TestPlay(train.Rooms[0],
            [new CardActionEffect("PlayUnitTrigger", "FrontInRoom", 0, false, true, [], unitTriggerKind: "OnFeed")], 0);
        Require(!uncalibrated.Supported && uncalibrated.UnsupportedReason == "Unit-trigger cards require a calibrated fixed OnEaten or OnDeath payload.",
            "Uncalibrated card-trigger kinds must remain unsupported.");
        Require(RelicModel.Validate([new CombatRelicState("other", RelicFriendlyTriggerDamageModel.AssetKey,
            [RelicFriendlyTriggerDamageModel.Effect], resourceEffects: [effect], disallowedInPlacementPhase: false)]) != null,
            "Other friendly-trigger damage owners require their own calibration.");
        Require(JsonSerializer.Serialize(train, ModelJson.Options) == parent, "OnEaten selection mutated its parent.");
        Console.WriteLine("EATEN-DAMAGE-PURE PASS: weakest living enemy selection, deployment/trigger/team/Pyre/empty-room gates, exact owner and immutable parallel branches without RNG.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 222 && root.GetProperty("OriginalEatenDamageSchema").GetInt32() == 222 &&
            root.GetProperty("ModifierScenario").GetString() == "original-eaten-damage" && root.GetProperty("OriginalEatenDamagePrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalEatenDamageCaptureFailures",
                "OriginalEatenDamageMismatches", "OriginalEatenDamageUnsupported", "OriginalEatenDamagePending" }
                .All(name => root.GetProperty(name).GetInt32() == 0), "Native OnEaten damage fixture did not pass acceptance.");
        FixtureValue evidence = root.GetProperty("OriginalEatenDamage");
        int[] cards = evidence.GetProperty("CardIds").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        int[] enemies = evidence.GetProperty("EnemyUnitIds").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        Require(cards.Length == 10 && enemies.Length == 2 && root.GetProperty("Actions").EnumerateArray()
            .Where(row => row.GetProperty("ScenarioAction").GetBoolean()).Select(row => row.GetProperty("Action")
                .GetProperty("CardInstanceId").GetInt32()).SequenceEqual(cards), "Missing or reordered native OnEaten carrier actions.");
        int positives = 0, behindFront = 0, enemyControls = 0, emptyControls = 0, wrongTriggers = 0;
        foreach (FixtureValue row in root.GetProperty("RelicEatenDamageTests").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            CombatUnit actor = row.GetProperty("Actor").Deserialize<CombatUnit>()!;
            string kind = row.GetProperty("Trigger").GetString()!;
            int roomIndex = row.GetProperty("RoomIndex").GetInt32();
            int owner = before.Context!.Relics!.Select((item, index) => (item, index)).Single(item => item.item.DataId == RelicFriendlyTriggerDamageModel.DataId).index;
            RelicCardPlayDamageSelection selected = RelicFriendlyTriggerDamageModel.Select(before, actor, kind, roomIndex, owner, 0);
            Require(selected.Supported && row.GetProperty("Result").GetBoolean() == (selected.Targets.Count > 0),
                "OnEaten Test eligibility differs: " + selected.UnsupportedReason);
            Equal(selected.State, actual, "Test complete state");
            Require(before.Context.BattleRng.Equals(actual.Context!.BattleRng), "Weakest-target Test changed Battle RNG.");
            if (selected.Targets.Count > 0)
            {
                positives++;
                Require(selected.Targets.SequenceEqual(row.GetProperty("CachedTargets").EnumerateArray().Select(item => item.GetInt32())), "Native cached weakest target differs.");
                if (before.Rooms.Single(room => room.RoomIndex == roomIndex).Units.First(unit => unit.Team == CombatTeam.Enemy).Id != selected.Targets[0]) behindFront++;
            }
            else if (kind != "OnEaten") wrongTriggers++;
            else if (actor.Team == CombatTeam.Enemy) enemyControls++;
            else emptyControls++;
        }
        Require(positives == 5 && behindFront >= 1 && wrongTriggers > 0 && enemyControls >= 1 && emptyControls >= 2,
            "Missing weakest-behind-front, wrong-trigger, enemy-team or empty/reset native controls.");
        int nonlethal = 0, kills = 0;
        var victimOrder = new List<int>();
        foreach (FixtureValue row in root.GetProperty("RelicEatenDamageApplications").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            int[] targets = row.GetProperty("Targets").EnumerateArray().Select(item => item.GetInt32()).ToArray();
            Require(row.GetProperty("Completed").GetBoolean() && targets.Length == 1, "OnEaten damage callback incomplete.");
            victimOrder.Add(targets[0]);
            TrainCombatResult predicted = RelicFriendlyTriggerDamageModel.ApplySelected(before, targets, deferPhysicalRemoval: true);
            Require(predicted.Supported, "OnEaten damage/death unsupported: " + predicted.UnsupportedReason);
            Equal(predicted.State, actual, "damage/death queue");
            CombatUnit victim = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == targets[0]);
            CombatUnit? after = actual.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.Id == targets[0]);
            if (after == null || after.Health <= 0) kills++;
            else { Require(victim.Health - after.Health == 5, "OnEaten damage magnitude differs."); nonlethal++; }
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            Parallel.For(0, 16, _ => Equal(RelicFriendlyTriggerDamageModel.ApplySelected(before, targets, deferPhysicalRemoval: true).State, actual, "parallel application"));
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "OnEaten damage mutated its parent.");
        }
        Require(victimOrder.SequenceEqual([enemies[1], enemies[1], enemies[0], enemies[0], enemies[0]]) && nonlethal == 3 && kills == 2,
            "Missing exact weakest-target sequence, three nonlethal hits or two kills.");
        Console.WriteLine("ORIGINAL-EATEN-DAMAGE-NATIVE PASS: unchanged owner, five weakest-Hero damage callbacks, three nonlethal hits, two kills, wrong-trigger/enemy/empty/reset controls, full state/queues and immutable parallel branches.");
    }
    private static void Equal<T>(T a, T b, string label)
    {
        string predicted = JsonSerializer.Serialize(a, ModelJson.Options), actual = JsonSerializer.Serialize(b, ModelJson.Options);
        Require(predicted == actual, "OnEaten " + label + " differs: " + ModelJson.Difference(predicted, actual));
    }
    private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
}
