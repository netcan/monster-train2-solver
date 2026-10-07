using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class NumericRangeChecks
{
    internal static void Run()
    {
        UnityRng rng = UnityRng.Seed(92);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 1, 10, cardInstances: []);
        CombatUnit Unit(int id, CombatTeam team, CombatStatus[]? statuses = null) => new(id, "unit" + id, team, 3, 20, 20,
            true, false, false, statuses ?? [], modifiers: new(3, 0, 0, 1, 1, true, false, []));
        var room = new RoomCombatState(0, false, [Unit(1, CombatTeam.Enemy), Unit(2, CombatTeam.Enemy), Unit(3, CombatTeam.Player)], [], context);
        CardActionEffect Effect(string type, int min, int max, float multiplier = 1f, string target = "Room", CardEffectTests? tests = null) =>
            new(type, target, 0, true, false, type == "AddStatus" ? [new("armor", 1, 1)] : [], tests: tests, range: new(min, max, multiplier));
        CardActionEffect damage = Effect("Damage", 0, 4, .75f);
        string parent = JsonSerializer.Serialize(room);
        SpellCastCheck cast = CardSpellModel.TestPlay(room, [damage], 0);
        Require(cast.CanPlay && cast.BattleRngAfterTests!.Value.Equals(rng.Next()), "Initial damage range test omitted its gameplay draw.");
        RoomCombatResult applied = CardSpellModel.Apply(room, [damage], 0);
        RngDraw shared = damage.Range!.Sample(rng.Next());
        Require(applied.Supported && applied.State!.Context!.BattleRng.Equals(shared.State) &&
            applied.State.Units.Where(unit => unit.Team == CombatTeam.Enemy).All(unit => unit.Health == 20 - shared.Value),
            "Runtime damage test/apply draws or shared target quantity differs.");
        var empty = new RoomCombatState(0, false, [], [], context);
        Require(CardSpellModel.Apply(empty, [damage], 0).State!.Context!.BattleRng.Equals(rng.Next().Next()),
            "A valid empty effect failed to sample its amount.");
        Require(!CardSpellModel.TestPlay(empty, [Effect("Damage", 0, 0)], 0).CanPlay &&
            CardSpellModel.Apply(empty, [Effect("Damage", 0, 0)], 0).State!.Context!.BattleRng.Equals(rng),
            "Equal zero damage bounds passed the native positive-maximum gate or consumed RNG.");
        CardActionEffect gated = Effect("Damage", 0, 4, tests: new(true, false, false, false, false));
        var endedContext = new CombatContext(context.Cards, rng, 0, 1, 10, cardInstances: [], allScenarioBossesDead: true);
        var ended = new RoomCombatState(0, false, [], [], endedContext);
        Require(CardSpellModel.Apply(ended, [gated], 0).State!.Context!.BattleRng.Equals(rng.Next()),
            "A post-boss gate suppressed the preceding damage test draw.");
        CardActionEffect skipped = Effect("Damage", 0, 4, tests: new(false, false, false, false));
        SpellCastCheck skippedCast = CardSpellModel.TestPlay(room, [new("Heal", "Room", 0, false, true, []), skipped], 0);
        Require(skippedCast.CanPlay && skippedCast.BattleRngAfterTests!.Value.Equals(rng) &&
            CardSpellModel.Apply(room, [skipped], 0).State!.Context!.BattleRng.Equals(rng.Next().Next()),
            "ShouldTest incorrectly controlled runtime quantity sampling.");
        Require(new CardEffectRange(2, 2, -.75f).Sample(rng).Value == -2 && new CardEffectRange(2, 2).Sample(rng).State.Equals(rng),
            "Equal bounds, negative flooring or zero-draw semantics differs.");
        RngDraw reversed = new CardEffectRange(6, 2, .5f).Sample(rng);
        Require(reversed.Value == (int)Math.Floor(.5f * rng.Range(6, 2).Value) && reversed.State.Equals(rng.Next()),
            "Reversed range direction differs from Unity.");
        Require(new CardEffectRange(16777217, 16777217).Sample(rng).Value == 16777216,
            "Range scaling retained double precision instead of the native float conversion.");
        Require(!CardSpellModel.Apply(room, [Effect("Damage", 0, 4, float.NaN)], 0).Supported &&
            !CardSpellModel.Apply(room, [Effect("Damage", int.MaxValue, int.MaxValue)], 0).Supported,
            "Undefined native float-to-int ranges returned an apparently exact child.");
        var negativeHeal = CardSpellModel.Apply(room, [Effect("Heal", 2, 2, -.5f)], 0);
        Require(negativeHeal.Supported && JsonSerializer.Serialize(negativeHeal.State) == parent,
            "A negative sampled heal changed units, RNG or triggers.");
        var negativeChance = CardSpellModel.Apply(room, [Effect("AddStatus", -1, -1)], 0);
        Require(negativeChance.Supported && negativeChance.State!.Context!.BattleRng.Equals(rng.Next().Next()) &&
            negativeChance.State.Units.All(unit => unit.Statuses.Count == 0), "Negative status chances omitted target draws or applied a status.");
        UnityRng signedRng = Enumerable.Range(0, 64).Select(UnityRng.Seed).First(state => state.Range(0, 2).Value == 0 && state.Next().Range(0, 2).Value == 1);
        var signedContext = new CombatContext(new([], [], [], signedRng, 0, []), signedRng, 0, 1, 10, cardInstances: []);
        var pyregel = new RoomCombatState(0, false, [Unit(1, CombatTeam.Enemy, [new("pyregel", 3, 1)])], [], signedContext);
        var negativeDamage = Effect("Damage", 0, 2, -1f);
        Require(CardSpellModel.Apply(pyregel, [negativeDamage], 0).State!.Units[0].Health == 17 &&
            CardSpellModel.Apply(new RoomCombatState(0, false, [Unit(1, CombatTeam.Enemy)], [], signedContext), [negativeDamage], 0).State!.Units[0].Health == 20,
            "A valid zero damage test followed by a negative sample was not clamped before pyregel or healed its target.");
        var definition = new CardPlayRule("spell", "spell", 0, "Spell", "Discard", null, [], [Effect("Damage", 0, 7), Effect("BuffAttack", -3, 2)]);
        var instance = new CardInstanceState(1, "spell", new(new(damage: -3), [], 0, []), new(new(damage: 2), [], 0, []), 0, 0, 0, []);
        CardPlayRule resolved = CardModifierModel.Resolve(definition, instance);
        Require(resolved.Effects[0].Range!.Min == 2 && resolved.Effects[0].Range!.Max == 6 &&
            resolved.Effects[1].Range!.Min == -3 && resolved.Effects[1].Range!.Max == 2,
            "Effect endpoints missed ordered modifier group clamps or upgraded attack buffs.");
        var token = new CardToken(1, "ranged");
        var ownedContext = new CombatContext(new([token], [], [], rng, 0, []), rng, 0, 2, 10,
            cardInstances: [CardInstanceState.Empty(1, "ranged")]);
        var train = new TrainCombatState([new(0, false, room.Units, [], ownedContext),
            new(1, false, [], [], ownedContext), new(2, false, [], [], ownedContext),
            new(3, false, [new(4, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, [])], [], ownedContext)], [], 7, ownedContext);
        var spawn = new EnemySpawnState(train, [new EnemyWave([new EnemyGroup([])])], [-1], 0, false, rng, 5, [], 0, false, 2, 1, 1, []);
        var rules = new BattlePlayRules([new(0, 5, 7, true, false, false), new(1, 5, 7, true, false, false),
            new(2, 5, 7, true, false, false), new(3, 0, 7, true, true, true)],
            [new("ranged", "ranged", 0, "Spell", "Discard", null, [], [damage])]);
        BattleTurnState Battle(bool isolated) => new(spawn, 3, 3, 5, 0, 0, "New", [new("Spawning", 92, rng), new("Battle", 92, rng)],
            [new("Standby", []), new("Exhausted", []), new("Purged", [])], [], rules, uiRngIsolated: isolated);
        Require(BattleActionModel.PlayCard(Battle(false), new(1, 0)).Rejection == ActionRejection.Unsupported,
            "Uncaptured vanilla UI range draws were accepted as an exact action.");
        BattleActionResult isolatedPlay = BattleActionModel.PlayCard(Battle(true), new(1, 0));
        Require(isolatedPlay.Supported && isolatedPlay.State!.UiRngIsolated && isolatedPlay.State.Spawn.Train.Context!.BattleRng.Equals(rng.Next().Next().Next()),
            "Full card play lost initial-test RNG or the UI isolation boundary: " + isolatedPlay.Reason);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(CardSpellModel.Apply(room, [damage], 0).State) == JsonSerializer.Serialize(applied.State) &&
            CardSpellModel.TestPlay(room, [damage], 0).BattleRngAfterTests!.Value.Equals(cast.BattleRngAfterTests!.Value), "Parallel range branches differ."));
        Require(JsonSerializer.Serialize(room) == parent, "Range tests or child branches mutated the parent.");
        Console.WriteLine("NUMERIC-RANGE-CHECKS PASS: test/apply draws, shared quantities, upgrades, signed/fractional/equal/reversed bounds, empty/gated effects and parallel isolation.");
    }

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] actions = fixture.GetProperty("Actions").EnumerateArray().ToArray();
        FixtureValue[] turns = fixture.GetProperty("Turns").EnumerateArray().ToArray();
        FixtureValue[] samples = fixture.GetProperty("NumericRanges").EnumerateArray().ToArray();
        var phases = new HashSet<string>(); var types = new HashSet<string>();
        int equal = 0, signed = 0, fractional = 0, changed = 0, postBoss = 0;
        foreach (FixtureValue sample in samples)
        {
            int actionIndex = sample.GetProperty("ActionIndex").GetInt32(), effectIndex = sample.GetProperty("EffectIndex").GetInt32();
            string phase = sample.GetProperty("Phase").GetString()!;
            FixtureValue entry = actionIndex >= 0 ? actions[actionIndex] : turns[sample.GetProperty("TurnIndex").GetInt32()];
            BattleTurnState before = entry.GetProperty(phase == "Highlight" ? "Actual" : "Before").Deserialize<BattleTurnState>()!;
            CombatContext context = before.Spawn.Train.Context!;
            CardInstanceState card = context.CardInstances!.Single(card => card.InstanceId == sample.GetProperty("CardId").GetInt32());
            CardPlayRule rule = before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
            CardActionEffect definition = rule.Effects[effectIndex];
            CardActionEffect effect = CardModifierModel.Resolve(rule, card).Effects[effectIndex];
            var range = new CardEffectRange(sample.GetProperty("Min").GetInt32(), sample.GetProperty("Max").GetInt32(), sample.GetProperty("Multiplier").GetSingle());
            Require(range.Min == effect.Range!.Min && range.Max == effect.Range.Max && range.Multiplier == effect.Range.Multiplier,
                "Native range endpoints differ from captured definition and live modifiers.");
            UnityRng rngBefore = sample.GetProperty("Before").Deserialize<UnityRng>();
            UnityRng rngAfter = sample.GetProperty("After").Deserialize<UnityRng>();
            RngDraw expected = range.Sample(rngBefore);
            Require(expected.Value == sample.GetProperty("Value").GetInt32() && expected.State.Equals(rngAfter), "Native range value or complete RNG state differs.");
            phases.Add(phase); types.Add(effect.Type);
            if (phase != "Apply") Require(effect.Type == "Damage", "Non-damage casting/runtime tests sampled a quantity.");
            if (range.Min == range.Max) equal++;
            if (expected.Value < 0) signed++;
            if (range.Multiplier != MathF.Truncate(range.Multiplier)) fractional++;
            if (definition.Range!.Min != range.Min || definition.Range.Max != range.Max) changed++;
            if (phase == "Apply" && fixture.GetProperty("ModifierScenario").GetString() == "numeric-ranges-lethal" &&
                entry.GetProperty("ActualOutcome").GetInt32() == (int)RoomOutcome.BattleWon &&
                effectIndex > rule.Effects.ToList().FindIndex(effect => effect.Range!.Min == 10000)) postBoss++;
        }
        foreach (var group in samples.Where(sample => sample.GetProperty("ActionIndex").GetInt32() >= 0 && sample.GetProperty("Phase").GetString() != "Highlight")
            .GroupBy(sample => sample.GetProperty("ActionIndex").GetInt32()))
        {
            BattleTurnState before = actions[group.Key].GetProperty("Before").Deserialize<BattleTurnState>()!;
            PlayCardAction action = actions[group.Key].GetProperty("Action").Deserialize<PlayCardAction>()!;
            string dataId = before.Spawn.Train.Context!.Cards.Hand.Single(card => card.InstanceId == action.CardInstanceId).DataId;
            var damageIndices = before.PlayRules!.Cards.Single(card => card.DataId == dataId).Effects.Select((effect, index) => (effect, index))
                .Where(item => item.effect.Type == "Damage" && item.effect.Range != null && item.effect.Tests?.ShouldTest != false).Select(item => item.index);
            Require(group.Where(sample => sample.GetProperty("Phase").GetString() == "Cast").Select(sample => sample.GetProperty("EffectIndex").GetInt32())
                .SequenceEqual(damageIndices), "Native action did not run exactly one ordered initial casting test sequence.");
        }
        Require(samples.Length > 0 && phases.SetEquals(["Cast", "Test", "Apply", "Highlight"]) &&
            types.SetEquals(["Damage", "Heal", "AddStatus", "BuffAttack", "DebuffAttack", "BuffHealth", "DebuffHealth"]) &&
            equal > 0 && signed > 0 && fractional > 0 && changed > 0, "Native quantity oracle lacks phases, effect families or range boundaries.");
        if (fixture.GetProperty("ModifierScenario").GetString() == "numeric-ranges-lethal") Require(postBoss > 0, "Range oracle lacks post-boss execution.");
        UiRngIsolationChecks.CheckRecords(fixture.GetProperty("UiRngIsolation"));
        Console.WriteLine($"NATIVE-NUMERIC-RANGE-COVERAGE PASS: {samples.Length} samples, {equal} equal bounds, {signed} signed values, {fractional} fractional multipliers, {changed} modified endpoints and {postBoss} post-boss samples.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
