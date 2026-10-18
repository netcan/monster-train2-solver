using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicCardPlayDamageChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(734);
        var effect = new RelicResourceEffect(0, RelicCardPlayDamageModel.Effect, 3, 0, 3, false,
            cardPlayDamage: new("Spell", "RandomInRoom", "Heroes", []));
        var owner = new CombatRelicState(RelicCardPlayDamageModel.DataId, RelicCardPlayDamageModel.AssetKey,
            [RelicCardPlayDamageModel.Effect], resourceEffects: [effect], disallowedInPlacementPhase: false);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10, relics: [owner],
            energyState: new(10, 0, 0, "MonsterTurn", true),
            resources: new(3, 0, 5, 10, 10, "pyre", 0, 80, 0, 0, [], [], []));
        CombatUnit Unit(int id, CombatTeam team) => new(id, "damage-carrier", team, 0, 4, 4, false, false, false, [], []);
        var train = new TrainCombatState([new(0, true, [Unit(1, CombatTeam.Enemy), Unit(2, CombatTeam.Enemy), Unit(3, CombatTeam.Player)], [], context)], [], 6, context);
        CardPlayRule Card(string? type, bool? ability) => new("test", "test", 0, "Null", "Discard", null, [],
            cardType: type, isAnyAbility: ability);
        string parent = JsonSerializer.Serialize(train, ModelJson.Options);
        RelicCardPlayDamageSelection selected = RelicCardPlayDamageModel.Select(train, Card("Spell", false), 0, 0, 0);
        Require(selected.Supported && selected.Targets.Count == 1 && selected.Targets.Single() is 1 or 2 &&
            !selected.State!.Context!.BattleRng.Equals(rng),
            "The relic Test must choose an enemy and commit Battle RNG even though the played Spell has no effects.");
        foreach (CardPlayRule control in new[] { Card("Monster", false), Card("Spell", true) })
        {
            RelicCardPlayDamageSelection ignored = RelicCardPlayDamageModel.Select(train, control, 0, 0, 0);
            Require(ignored.Supported && ignored.Targets.Count == 0 && ReferenceEquals(ignored.State, train),
                "Monster cards and abilities must not target or consume RNG.");
        }
        var empty = new TrainCombatState([new(0, true, [Unit(3, CombatTeam.Player)], [], context)], [], 6, context);
        RelicCardPlayDamageSelection noEnemy = RelicCardPlayDamageModel.Select(empty, Card("Spell", false), 0, 0, 0);
        Require(noEnemy.Supported && noEnemy.Targets.Count == 0 && ReferenceEquals(noEnemy.State, empty) &&
            ReferenceEquals(RelicCardPlayDamageModel.Select(train, Card("Spell", false), 0, 0, 0, preview: true).State, train),
            "Empty rooms and random-effect preview gating must preserve the state and RNG.");
        Require(!RelicCardPlayDamageModel.Select(train, Card("Spell", null), 0, 0, 0).Supported &&
            RelicModel.Validate([new CombatRelicState("other-owner", RelicCardPlayDamageModel.AssetKey,
                [RelicCardPlayDamageModel.Effect], resourceEffects: [effect], disallowedInPlacementPhase: false)]) != null,
            "Missing ability metadata or a changed owner must not inherit this effect.");
        Parallel.For(0, 16, _ => {
            RelicCardPlayDamageSelection branch = RelicCardPlayDamageModel.Select(train, Card("Spell", false), 0, 0, 0);
            Require(branch.Supported && branch.Targets.SequenceEqual(selected.Targets) &&
                branch.State!.Context!.BattleRng.Equals(selected.State!.Context!.BattleRng), "Parallel random selection differs.");
        });
        Require(JsonSerializer.Serialize(train, ModelJson.Options) == parent, "Random relic Test mutated its parent.");
        Console.WriteLine("CARD-PLAY-DAMAGE-PURE PASS: Spell and ability gates, enemy-only random target, committed Test RNG, empty-room/preview controls and immutable parallel branches.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 220 && root.GetProperty("OriginalCardPlayDamageSchema").GetInt32() == 220 &&
            root.GetProperty("ModifierScenario").GetString() == "original-card-play-damage" &&
            root.GetProperty("OriginalCardPlayDamagePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalCardPlayDamageCaptureFailures",
                "OriginalCardPlayDamageMismatches", "OriginalCardPlayDamageUnsupported", "OriginalCardPlayDamagePending" }
                .All(name => root.GetProperty(name).GetInt32() == 0), "Native card-play damage fixture did not pass acceptance.");
        FixtureValue evidence = root.GetProperty("OriginalCardPlayDamage");
        int[] cards = evidence.GetProperty("CardIds").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        int[] enemyIds = evidence.GetProperty("EnemyUnitIds").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        int[] scenarioPlays = root.GetProperty("Actions").EnumerateArray().Where(row => row.GetProperty("ScenarioAction").GetBoolean())
            .Select(row => row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).ToArray();
        Require(cards.Length == 7 && enemyIds.Length == 2 && scenarioPlays.SequenceEqual(cards),
            "Native card-play damage witness actions were incomplete or reordered.");
        var controls = new HashSet<int>();
        int positive = 0, multiTarget = 0;
        foreach (FixtureValue row in root.GetProperty("RelicCardPlayDamageTests").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            CardPlayRule rule = row.GetProperty("CardRule").Deserialize<CardPlayRule>()!;
            int ownerIndex = before.Context!.Relics!.Select((owner, index) => (owner, index))
                .Single(item => item.owner.DataId == RelicCardPlayDamageModel.DataId).index;
            RelicCardPlayDamageSelection selected = RelicCardPlayDamageModel.Select(before, rule,
                row.GetProperty("RoomIndex").GetInt32(), ownerIndex, 0);
            Require(selected.Supported && row.GetProperty("Result").GetBoolean() == (selected.Targets.Count > 0),
                "Native Test eligibility differs: " + selected.UnsupportedReason);
            Equal(selected.State, actual, "native Test/committed Battle RNG");
            if (selected.Targets.Count == 0) controls.Add(row.GetProperty("CardId").GetInt32());
            else
            {
                Require(row.GetProperty("CachedTargets").EnumerateArray().Select(item => item.GetInt32()).SequenceEqual(selected.Targets),
                    "Native random cached target differs from independent model selection.");
                int candidates = before.Rooms.Single(room => room.RoomIndex == 0).Units.Count(unit => unit.Team == CombatTeam.Enemy);
                if (candidates == 2) multiTarget++;
                positive++;
            }
        }
        Require(positive == 4 && multiTarget >= 1 && controls.IsSupersetOf([cards[0], cards[1], cards[6]]),
            "Missing random multi-candidate, Monster, empty-room or post-kill reset Test evidence.");
        int nonlethal = 0, kills = 0;
        foreach (FixtureValue row in root.GetProperty("RelicCardPlayDamageApplications").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean(), "Native damage callback did not finish.");
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            int[] targets = row.GetProperty("Targets").EnumerateArray().Select(item => item.GetInt32()).ToArray();
            Require(targets.Length == 1 && enemyIds.Contains(targets[0]), "Damage callback targeted a non-witness enemy.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            TrainCombatResult predicted = RelicCardPlayDamageModel.ApplySelected(before, targets, deferPhysicalRemoval: true);
            Require(predicted.Supported, "Native relic damage/death queue unsupported: " + predicted.UnsupportedReason);
            Equal(predicted.State, actual, "native relic damage/death queue");
            CombatUnit victim = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == targets[0]);
            CombatUnit? after = actual.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.Id == targets[0]);
            if (after == null || after.Health <= 0) kills++;
            else { Require(victim.Health == 4 && after.Health == 1, "Relic damage did not reduce 4 HP to 1."); nonlethal++; }
            Require(before.Context!.BattleRng.Equals(actual.Context!.BattleRng), "Apply consumed a second random-target draw.");
            Parallel.For(0, 16, _ => Equal(RelicCardPlayDamageModel.ApplySelected(before, targets, deferPhysicalRemoval: true).State, actual, "parallel damage branch"));
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Damage branches mutated their parent.");
        }
        Require(nonlethal == 2 && kills == 2, "Missing both 4->1 transitions and both native enemy deaths.");
        Console.WriteLine("ORIGINAL-CARD-PLAY-DAMAGE-NATIVE PASS: unchanged owner, four committed random-target Tests, Monster/empty-room/reset controls, two 4->1 hits, two lethal callbacks, complete state/death queues and immutable parallel branches.");
    }

    private static void Equal<T>(T predicted, T actual, string label)
    {
        string a = JsonSerializer.Serialize(predicted, ModelJson.Options), b = JsonSerializer.Serialize(actual, ModelJson.Options);
        Require(a == b, "Card-play damage " + label + " differs: " + ModelJson.Difference(a, b));
    }
    private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
}
