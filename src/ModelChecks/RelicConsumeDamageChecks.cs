using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicConsumeDamageChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(962);
        var effect = new RelicResourceEffect(0, RelicConsumeDamageModel.Effect, 30, 0, 30, true,
            cardPlayDamage: new("Spell", "FrontInRoom", "Heroes", []));
        var owner = new CombatRelicState(RelicConsumeDamageModel.DataId, RelicConsumeDamageModel.AssetKey,
            [RelicConsumeDamageModel.Effect], resourceEffects: [effect], disallowedInPlacementPhase: false);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 5, 10, relics: [owner],
            energyState: new(10, 0, 0, "MonsterTurn", true),
            resources: new(3, 0, 5, 10, 10, "pyre", 0, 80, 0, 0, [], [], []));
        CardInstanceState Card(string type, bool consume, int threshold = 0)
        {
            var descriptor = new CardMaskDescriptor(new("consume", type, "Common", type == "Monster", type == "Monster", true,
                [], [], [], [], null, false, 0, 0, false, false, false), 0,
                consume ? [new CardTraitValue("CardTraitExhaustState", "CardTraitExhaustState", 0, 0, true, 0)] : [], []);
            return CardExhaustModel.Initialize(new(1, "consume", CardModifiers.Empty(), CardModifiers.Empty(),
                0, 0, 0, [], maskDescriptor: descriptor), threshold);
        }
        TrainCombatState Train(CardInstanceState card, bool enemies = true)
        {
            CombatContext owned = context.WithCardInstances([card]);
            CombatUnit Unit(int id, CombatTeam team) => new(id, "carrier", team, 0, 31, 31, false, false, false, [], []);
            CombatUnit[] units = enemies ? [Unit(1, CombatTeam.Enemy), Unit(2, CombatTeam.Enemy), Unit(3, CombatTeam.Player)] : [Unit(3, CombatTeam.Player)];
            return new([new(0, true, units, [], owned)], [], 6, owned);
        }
        foreach (string type in new[] { "Spell", "Monster" })
        {
            TrainCombatState source = Train(Card(type, true));
            string parent = JsonSerializer.Serialize(source, ModelJson.Options);
            Parallel.For(0, 16, _ => {
                RelicCardPlayDamageSelection selected = RelicConsumeDamageModel.Select(source, 1, 0, 0, 0);
                Require(selected.Supported && selected.Targets.SequenceEqual([1]) && ReferenceEquals(selected.State, source),
                    "Consume eligibility must ignore the card-type parameter and select the front enemy without RNG.");
            });
            Require(JsonSerializer.Serialize(source, ModelJson.Options) == parent, "Consume selection mutated its parent.");
        }
        foreach (TrainCombatState control in new[] { Train(Card("Spell", false)), Train(Card("Spell", true, 1)), Train(Card("Spell", true), false) })
        {
            RelicCardPlayDamageSelection selected = RelicConsumeDamageModel.Select(control, 1, 0, 0, 0);
            Require(selected.Supported && selected.Targets.Count == 0 && ReferenceEquals(selected.State, control),
                "Non-Consume, delayed Consume and empty-room controls must do nothing.");
        }
        Require(RelicConsumeDamageModel.Select(Train(Card("Spell", true, 1).OnDiscard(true)), 1, 0, 0, 0).Targets.SequenceEqual([1]),
            "The delayed Consume threshold must become eligible on its next scenario play.");
        TrainCombatState retained = Train(Card("Spell", true));
        RoomCombatState retainedRoom = retained.Rooms[0];
        retained = new([new(0, true, retainedRoom.Units.Select(unit => unit.Id == 1 ? unit.WithHealth(0) : unit).ToArray(),
            [], retained.Context)], [], 6, retained.Context);
        Require(RelicConsumeDamageModel.Select(retained, 1, 0, 0, 0).Targets.SequenceEqual([2]),
            "Dead retained front characters must not hide the next living enemy.");
        Require(!RelicConsumeDamageModel.Select(Train(new(1, "unknown", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [])), 1, 0, 0, 0).Supported &&
            RelicModel.Validate([new CombatRelicState("other", RelicConsumeDamageModel.AssetKey, [RelicConsumeDamageModel.Effect],
                resourceEffects: [effect], disallowedInPlacementPhase: false)]) != null,
            "Missing private trait state or a changed owner must be rejected.");
        Console.WriteLine("CONSUME-DAMAGE-PURE PASS: Spell/Monster Consume eligibility, next-play threshold/reset controls, front-enemy targeting without RNG, metadata/owner guards and immutable parallel branches.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 221 && root.GetProperty("OriginalConsumeDamageSchema").GetInt32() == 221 &&
            root.GetProperty("ModifierScenario").GetString() == "original-consume-damage" &&
            root.GetProperty("OriginalConsumeDamagePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalConsumeDamageCaptureFailures",
                "OriginalConsumeDamageMismatches", "OriginalConsumeDamageUnsupported", "OriginalConsumeDamagePending" }
                .All(name => root.GetProperty(name).GetInt32() == 0), "Native Consume damage fixture did not pass acceptance.");
        FixtureValue evidence = root.GetProperty("OriginalConsumeDamage");
        int[] cards = evidence.GetProperty("CardIds").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        int[] enemyIds = evidence.GetProperty("EnemyUnitIds").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        int[] scenarioPlays = root.GetProperty("Actions").EnumerateArray().Where(row => row.GetProperty("ScenarioAction").GetBoolean())
            .Select(row => row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).ToArray();
        Require(cards.Length == 8 && enemyIds.Length == 2 && scenarioPlays.SequenceEqual(cards),
            "Native Consume damage witness actions were incomplete or reordered.");
        var controls = new HashSet<int>();
        int positive = 0, multiTarget = 0;
        foreach (FixtureValue row in root.GetProperty("RelicConsumeDamageTests").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            int ownerIndex = before.Context!.Relics!.Select((owner, index) => (owner, index))
                .Single(item => item.owner.DataId == RelicConsumeDamageModel.DataId).index;
            RelicCardPlayDamageSelection selected = RelicConsumeDamageModel.Select(before, row.GetProperty("CardId").GetInt32(),
                row.GetProperty("RoomIndex").GetInt32(), ownerIndex, 0);
            Require(selected.Supported && row.GetProperty("Result").GetBoolean() == (selected.Targets.Count > 0),
                "Native Test eligibility differs: " + selected.UnsupportedReason);
            Equal(selected.State, actual, "native Test/unchanged Battle RNG");
            if (selected.Targets.Count == 0) controls.Add(row.GetProperty("CardId").GetInt32());
            else
            {
                Require(row.GetProperty("CachedTargets").EnumerateArray().Select(item => item.GetInt32()).SequenceEqual(selected.Targets),
                    "Native cached front target differs from independent model selection.");
                Require(before.Context!.BattleRng.Equals(actual.Context!.BattleRng), "Front targeting advanced Battle RNG.");
                Require(selected.Targets[0] == before.Rooms.Single(room => room.RoomIndex == 0).Units
                    .First(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0).Id, "Native target was not the front enemy.");
                int candidates = before.Rooms.Single(room => room.RoomIndex == 0).Units.Count(unit => unit.Team == CombatTeam.Enemy);
                if (candidates == 2) multiTarget++;
                positive++;
            }
        }
        Require(positive == 4 && multiTarget >= 1 && controls.IsSupersetOf([cards[0], cards[1], cards[2], cards[7]]),
            "Missing multi-enemy front targeting, non-Consume, empty-room or post-kill reset Test evidence.");
        int nonlethal = 0, kills = 0;
        foreach (FixtureValue row in root.GetProperty("RelicConsumeDamageApplications").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean(), "Native damage callback did not finish.");
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            int[] targets = row.GetProperty("Targets").EnumerateArray().Select(item => item.GetInt32()).ToArray();
            Require(targets.Length == 1 && enemyIds.Contains(targets[0]), "Damage callback targeted a non-witness enemy.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            TrainCombatResult predicted = RelicConsumeDamageModel.ApplySelected(before, targets, deferPhysicalRemoval: true);
            Require(predicted.Supported, "Native relic damage/death queue unsupported: " + predicted.UnsupportedReason);
            Equal(predicted.State, actual, "native relic damage/death queue");
            CombatUnit victim = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == targets[0]);
            CombatUnit? after = actual.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.Id == targets[0]);
            if (after == null || after.Health <= 0) kills++;
            else { Require(victim.Health == 31 && after.Health == 1, "Relic damage did not reduce 31 HP to 1."); nonlethal++; }
            Require(before.Context!.BattleRng.Equals(actual.Context!.BattleRng), "Front-targeted Apply advanced Battle RNG.");
            Parallel.For(0, 16, _ => Equal(RelicConsumeDamageModel.ApplySelected(before, targets, deferPhysicalRemoval: true).State, actual, "parallel damage branch"));
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Damage branches mutated their parent.");
        }
        Require(nonlethal == 2 && kills == 2, "Missing both 31->1 transitions and both native enemy deaths.");
        Console.WriteLine("ORIGINAL-CONSUME-DAMAGE-NATIVE PASS: unchanged owner, four deterministic front-target Tests, Monster/non-Consume/empty-room/reset controls, two 31->1 hits, two lethal callbacks, complete state/death queues and immutable parallel branches.");
    }

    private static void Equal<T>(T predicted, T actual, string label)
    {
        string a = JsonSerializer.Serialize(predicted, ModelJson.Options), b = JsonSerializer.Serialize(actual, ModelJson.Options);
        Require(a == b, "Consume damage " + label + " differs: " + ModelJson.Difference(a, b));
    }
    private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
}
