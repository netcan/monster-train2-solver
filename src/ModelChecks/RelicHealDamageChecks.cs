using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicHealDamageChecks
{
    private static CombatRelicState Owner(string id = RelicHealDamageModel.DataId, int multiplier = 1,
        string target = "FrontInRoom") => new(id, RelicHealDamageModel.AssetKey, [RelicHealDamageModel.Effect],
        resourceEffects: [new(0, RelicHealDamageModel.Effect, multiplier, 0, multiplier, true,
            unitTrigger: new("OnHeal", "Monsters", null, []), unitDamage: new(target))], disallowedInPlacementPhase: false);

    internal static void Run()
    {
        UnityRng rng = UnityRng.Seed(621);
        var points = new BattleSpawnPoints([new(0, CombatTeam.Enemy, 2, 2, [1, 2], [false, false]),
            new(0, CombatTeam.Player, 1, 1, [3], [false]), new(1, CombatTeam.Enemy, 1, 1, [4], [false])],
            [new(1, new(0, CombatTeam.Enemy, 0), null), new(2, new(0, CombatTeam.Enemy, 1), null),
             new(3, new(0, CombatTeam.Player, 0), null), new(4, new(1, CombatTeam.Enemy, 0), null)]);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10, relics: [Owner()],
            energyState: new(10, 0, 0, "MonsterTurn", true),
            resources: new(3, 0, 5, 10, 10, "pyre", 0, 80, 0, 0, [], [], []), spawnPoints: points);
        CombatUnit Unit(int id, CombatTeam team, int hp, bool pyre = false) => new(id, "carrier", team, 0, hp, 80, false, pyre, false, [], []);
        CombatUnit actor = Unit(3, CombatTeam.Player, 80);
        var train = new TrainCombatState([new(0, false, [Unit(1, CombatTeam.Enemy, 12), Unit(2, CombatTeam.Enemy, 6), actor], [], context),
            new(1, false, [Unit(4, CombatTeam.Enemy, 1)], [], context)], [], 6, context);
        string parent = JsonSerializer.Serialize(train, ModelJson.Options);
        Parallel.For(0, 16, _ => {
            RelicCardPlayDamageSelection selected = RelicHealDamageModel.Select(train, actor, "OnHeal", 0, 0);
            Require(selected.Supported && selected.Targets.SequenceEqual([1]) && ReferenceEquals(selected.State, train),
                "Heal must select the current floor's front enemy, independently of HP: " + selected.UnsupportedReason);
        });
        Require(RelicHealDamageModel.Select(train, actor, "OnHit", 0, 0).Targets.Count == 0 &&
            RelicHealDamageModel.Select(train, Unit(1, CombatTeam.Enemy, 12), "OnHeal", 0, 0).Targets.Count == 0,
            "Wrong trigger/team must not apply.");
        Require(RelicHealDamageModel.Select(train, Unit(3, CombatTeam.Player, 80, true), "OnHeal", 0, 0).Targets.SequenceEqual([1]),
            "This effect has no subtype or Pyre exclusion.");
        var missingPoint = new BattleSpawnPoints(points.Groups, points.Units.Select(point => point.UnitId == 3
            ? new UnitSpawnPointState(3, null, point.Current) : point).ToArray());
        Require(RelicHealDamageModel.Select(CardSpellModel.WithContext(train, context.WithSpawnPoints(missingPoint)), actor, "OnHeal", 0, 0)
            .Targets.Count == 0, "A last-known point cannot replace the required current point.");
        var retained = new TrainCombatState([new(0, false, [Unit(1, CombatTeam.Enemy, 0), Unit(2, CombatTeam.Enemy, 6), actor], [], context)], [], 6, context);
        Require(RelicHealDamageModel.Select(retained, actor, "OnHeal", 0, 0).Targets.SequenceEqual([2]), "Dead front must be ignored.");
        var empty = new TrainCombatState([new(0, false, [actor], [], context)], [], 6, context);
        Require(RelicHealDamageModel.Select(empty, actor, "OnHeal", 0, 0).Targets.Count == 0, "Empty target cache must reset.");
        foreach (CombatRelicState changed in new[] { Owner("other"), Owner(multiplier: 2), Owner(target: "Weakest") })
            Require(RelicModel.Validate([changed]) != null, "Changed owner/multiplier/target must be rejected.");
        Require(JsonSerializer.Serialize(train, ModelJson.Options) == parent, "Heal-damage selection mutated its parent.");
        Console.WriteLine("HEAL-DAMAGE-PURE PASS: current-point-only front selection, dead/empty/off-floor/team/trigger controls, Pyre eligibility, exact definitions and immutable parallel branches without RNG.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 225 && root.GetProperty("OriginalHealDamageSchema").GetInt32() == 225 &&
            root.GetProperty("ModifierScenario").GetString() == "original-heal-to-damage" && root.GetProperty("OriginalHealDamagePrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalHealDamageCaptureFailures",
                "OriginalHealDamageMismatches", "OriginalHealDamageUnsupported", "OriginalHealDamagePending" }
                .All(name => root.GetProperty(name).GetInt32() == 0), "Native HealToDamage acceptance failed.");
        FixtureValue evidence = root.GetProperty("OriginalHealDamage");
        int[] cards = evidence.GetProperty("CardIds").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        int[] enemies = evidence.GetProperty("EnemyUnitIds").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(cards.Length == 9 && enemies.Length == 2 && actions.Length == 9 && actions.All(row => row.GetProperty("ScenarioAction").GetBoolean()) &&
            actions.Select(row => row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(cards) &&
            actions.Select((row, index) => row.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32() == (index < 2 ? 0 : 1)).All(value => value),
            "HealToDamage requires complete deployment and turn-one controlled actions.");
        int positive = 0, wrongTrigger = 0, enemyHeal = 0, empty = 0;
        var positiveHeals = new List<int>();
        foreach (FixtureValue row in root.GetProperty("RelicHealDamageTests").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            CombatUnit actor = row.GetProperty("Actor").Deserialize<CombatUnit>()!;
            string trigger = row.GetProperty("Trigger").GetString()!;
            int heal = row.GetProperty("ActualHeal").GetInt32();
            int owner = before.Context!.Relics!.Select((item, index) => (item, index)).Single(item => item.item.DataId == RelicHealDamageModel.DataId).index;
            RelicCardPlayDamageSelection selected = RelicHealDamageModel.Select(before, actor, trigger, owner, 0);
            Require(selected.Supported && row.GetProperty("Result").GetBoolean() == (selected.Targets.Count > 0), "Heal Test differs: " + selected.UnsupportedReason);
            Equal(selected.State, actual, "complete Test state");
            Require(before.Context.BattleRng.Equals(actual.Context!.BattleRng), "Front selection consumed Battle RNG.");
            if (selected.Targets.Count > 0)
            {
                positive++; positiveHeals.Add(heal);
                Require(selected.Targets.SequenceEqual(row.GetProperty("CachedTargets").EnumerateArray().Select(item => item.GetInt32())), "Native cached front target differs.");
            }
            else if (trigger != "OnHeal") wrongTrigger++;
            else if (actor.Team == CombatTeam.Enemy && heal == 3) enemyHeal++;
            else if (actor.Team == CombatTeam.Player) empty++;
        }
        Require(positive == 4 && positiveHeals.SequenceEqual([0, 4, 8, 6]) && wrongTrigger > 0 && enemyHeal == 1 && empty >= 2,
            "Missing actual-heal magnitudes, zero-heal, enemy-heal or empty/reset native controls.");
        int applications = 0, nonlethal = 0, kills = 0, zero = 0;
        foreach (FixtureValue row in root.GetProperty("RelicHealDamageApplications").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            int heal = row.GetProperty("ActualHeal").GetInt32();
            int[] targets = row.GetProperty("Targets").EnumerateArray().Select(item => item.GetInt32()).ToArray();
            Require(row.GetProperty("Completed").GetBoolean() && targets.Length == 1 && targets[0] == (applications < 3 ? enemies[0] : enemies[1]),
                "Missing ordered front-target application.");
            TrainCombatResult predicted = RelicHealDamageModel.ApplySelected(before, targets, heal, true);
            Require(predicted.Supported, "Heal damage unsupported: " + predicted.UnsupportedReason);
            Equal(predicted.State, actual, "damage/death queue");
            CombatUnit victim = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == targets[0]);
            CombatUnit? after = actual.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.Id == targets[0]);
            if (heal == 0) { zero++; Require(after?.Health == victim.Health, "Zero actual heal dealt damage."); }
            else if (after == null || after.Health <= 0) kills++;
            else { nonlethal++; Require(victim.Health - after.Health == heal, "Damage must use actual healed HP."); }
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            Parallel.For(0, 16, _ => Equal(RelicHealDamageModel.ApplySelected(before, targets, heal, true).State, actual, "parallel damage"));
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Heal damage mutated its parent.");
            applications++;
        }
        Require(applications == 4 && zero == 1 && nonlethal == 1 && kills == 2, "Missing zero damage, nonlethal hit or both front/back kills.");
        Console.WriteLine("ORIGINAL-HEAL-DAMAGE-NATIVE PASS: unchanged owner, actual heals 0/4/8/6, one nonlethal hit/two kills, zero-heal and enemy/empty/reset controls, current-floor front targets and immutable parallel replay.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string a = JsonSerializer.Serialize(predicted, ModelJson.Options), b = JsonSerializer.Serialize(actual, ModelJson.Options);
        Require(a == b, "Heal damage " + label + " differs: " + ModelJson.Difference(a, b));
    }
    private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
}
