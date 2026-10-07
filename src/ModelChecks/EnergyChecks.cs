using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EnergyChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(317);
        CardActionEffect Effect(string kind, int value, CardEffectRange? range = null, CardEffectTests? tests = null) =>
            new(kind, "Room", value, false, false, [], tests: tests, range: range);
        CombatEffect TriggerEffect(string kind, int value, CardEffectRange? range = null, bool cancel = false) =>
            new("CardEffect" + (kind == "GainEnergyMonsterTurn" ? "GainEnergy" : kind), value, 0, "", 0, [], false,
                action: new(kind, "Self", value, true, true, [], range: range, tests: new(true, false, cancel, false, false)));
        CombatTrigger Trigger(string kind, params CombatEffect[] effects) => new(kind, false, false, true, 1, effects, false);
        CardPileState[] piles = [new("Standby", []), new("Exhausted", []), new("DiscardBuffer", []), new("Eaten", []), new("Purged", [])];
        CardActionEffect[] spell = [Effect("GainEnergy", 8), Effect("AdjustEnergy", -20), Effect("AdjustEnergy", 3),
            Effect("GainEnergyNextTurn", 2), Effect("GainEnergyEveryTurn", 1), Effect("GainEnergy", 0), Effect("GainEnergy", -4),
            Effect("GainEnergyNextTurn", -4), Effect("GainEnergyEveryTurn", -4)];
        BattleTurnState Root(CardActionEffect[] effects, BattleEnergyState? energy = null)
        {
            var context = new CombatContext(new([new(1, "energy")], [], [], rng, 0, []), rng, 0, 2, 10,
                statistics: new([], [], [], [], [], [], [], 0, 0, 0, 0, 0, [1], [1]), cardInstances: [CardInstanceState.Empty(1, "energy")],
                otherPiles: piles, queryFrame: new(2, true, 2, 0, 0, 1, 0), energyState: energy ?? new(9, 0, 0, "MonsterTurn", true));
            var unit = new CombatUnit(10, "energy-unit", CombatTeam.Player, 1, 100, 100, true, false, false, [],
                [Trigger("EndTurnPreHandDiscard", TriggerEffect("GainEnergy", 2), TriggerEffect("AdjustEnergy", -1)),
                    Trigger("OnTurnBegin", TriggerEffect("GainEnergy", 4), TriggerEffect("AdjustEnergy", -2),
                        TriggerEffect("GainEnergyMonsterTurn", 0, new(1, 100, 1), true), TriggerEffect("GainEnergyEveryTurn", 99)),
                    Trigger("PreCombat", TriggerEffect("GainEnergy", 3), TriggerEffect("AdjustEnergy", -1))]);
            var enemy = new CombatUnit(11, "enemy", CombatTeam.Enemy, 0, 500, 500, false, false, false, []);
            var pyre = new CombatUnit(12, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, []);
            var train = new TrainCombatState([new(0, false, [enemy, unit], [], context), new(1, false, [pyre], [], context)],
                [new(11, 0, false, false)], 7, context);
            var spawn = new EnemySpawnState(train, [new([new([])]), new([new([])]), new([new([])])], [-1, -1, -1],
                0, false, rng, 13, [], 0, false, 0, 0, 2, []);
            return new(spawn, 2, 3, 5, 0, 0, "New", [new("Spawning", 317, rng)], piles, [],
                new([new(0, 5, 7, true, false, false)], [new("energy", "energy", 1, "Spell", "Discard", null, [], effects)]));
        }
        BattleTurnState root = Root(spell);
        string parent = JsonSerializer.Serialize(root);
        BattleActionResult played = BattleActionModel.PlayCard(root, new(1, 0));
        Require(played.Supported && played.State!.Energy == 3 && played.State.Spawn.Train.Context!.QueryFrame!.Energy == 3 &&
            played.State!.Spawn.Train.Context!.EnergyState!.NextTurn == 2 && played.State!.Spawn.Train.Context!.EnergyState!.EveryTurn == 1,
            "Payment, cap, subtraction floor, nonpositive no-ops or source routing lost energy state: " + played.Reason);
        BattleTurnResult next = BattleTurnModel.EndTurn(played.State!);
        Require(next.Supported && next.State!.Energy == 9 && next.State.Spawn.Train.Context!.Statistics!.EnergyRemainingEndOfTurn == 0 &&
            next.State!.Spawn.Train.Context!.EnergyState!.NextTurn == 0 && next.State!.Spawn.Train.Context!.EnergyState!.EveryTurn == 1 &&
            next.State!.Spawn.Train.Context!.EnergyState!.Phase == "MonsterTurn" && next.State!.Spawn.Train.Context!.BattleRng.Equals(rng),
            "Phase-specific delays, late end-turn energy, carried combat gain, next-turn reset, cap or gated range cancellation differs: " + next.UnsupportedReason);
        // EnergyRemainingEndOfTurn is duration-dependent: the native rollover resets it to zero.
        BattleTurnResult following = BattleTurnModel.EndTurn(next.State!);
        Require(following.Supported && following.State!.Energy == 7 && following.State.Spawn.Train.Context!.EnergyState!.EveryTurn == 1 &&
            following.State!.Spawn.Train.Context!.EnergyState!.NextTurn == 0,
            "Nonpositive next-turn income removed carried energy or a temporary modifier was reused.");
        string expected = JsonSerializer.Serialize(following.State);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(BattleTurnModel.EndTurn(
            BattleTurnModel.EndTurn(BattleActionModel.PlayCard(root, new(1, 0)).State!).State!).State) == expected,
            "Parallel energy branches diverged."));
        Require(JsonSerializer.Serialize(root) == parent, "Energy resolution mutated its parent.");
        var rangedRoot = Root([Effect("GainEnergy", 0, new(-2, 5, .75f))]);
        var ranged = BattleActionModel.PlayCard(rangedRoot, new(1, 0));
        RngDraw sample = new CardEffectRange(-2, 5, .75f).Sample(rng);
        Require(ranged.Supported && ranged.State!.Energy == 1 + Math.Max(0, sample.Value) &&
            ranged.State.Spawn.Train.Context!.BattleRng.Equals(sample.State), "Energy quantity tests sampled RNG or application missed its range.");
        var outside = Root([Effect("GainEnergyMonsterTurn", 0, new(1, 100, 1), new(true, false, true, false, false)), Effect("GainEnergyEveryTurn", 99)],
            new(9, 0, 0, "Combat", true));
        var direct = CardSpellModel.Apply(outside.Spawn.Train, 0, outside.PlayRules!.Cards[0].Effects, 0);
        Require(direct.Supported && direct.State!.Context!.EnergyState!.EveryTurn == 0 && direct.State.Context.BattleRng.Equals(rng),
            "Rejected energy phase sampled its range or continued cancelled effects.");
        var context = root.Spawn.Train.Context!;
        var stopped = new CombatContext(context.Cards, rng, 0, 2, 10, queryFrame: new(2, false), energyState: new(9, 0, 0, "Combat", true));
        var dead = new CombatContext(context.Cards, rng, 0, 2, 10, queryFrame: new(2, true), energyState: new(9, 0, 0, "Combat", false));
        Require(!EnergyModel.Test(stopped, "AdjustEnergy") && !EnergyModel.Test(dead, "AdjustEnergy"), "Adjust energy ignored stopped combat or Pyre death.");
        var overflow = new CombatContext(context.Cards, rng, 0, 2, 10, queryFrame: new(int.MaxValue, true),
            energyState: new(9, int.MaxValue, int.MaxValue, "MonsterTurn", true));
        Require(EnergyModel.Apply(overflow, "GainEnergy", 1).QueryFrame!.Energy == int.MinValue &&
            EnergyModel.Apply(overflow, "GainEnergyNextTurn", 1).EnergyState!.NextTurn == int.MinValue &&
            EnergyModel.Apply(overflow, "GainEnergyEveryTurn", 1).EnergyState!.EveryTurn == int.MinValue,
            "Energy incorrectly saturated native signed arithmetic before its final clamp.");
        Require(EnergyModel.Validate(new CombatContext(context.Cards, rng, 0, 2, 10)) != null,
            "Unknown energy state was guessed.");
        var boss = new CombatUnit(11, "boss", CombatTeam.Enemy, 0, 1, 1, false, false, true, [], isBoss: true);
        var terminalTrain = new TrainCombatState(root.Spawn.Train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
            room.Units.Select(unit => unit.Id == 11 ? boss : unit).ToArray(), [], context)).ToArray(), [], 7, context);
        var terminalSpawn = new EnemySpawnState(terminalTrain, [], [], 0, false, rng, 13, [], 0, false, 0, 0, 2, []);
        var terminalRoot = new BattleTurnState(terminalSpawn, 2, 3, 5, 0, 0, "New", root.RngStreams, piles, [], root.PlayRules);
        var terminalPlayed = BattleActionModel.PlayCard(terminalRoot, new(1, 0));
        var won = BattleTurnModel.EndTurn(terminalPlayed.State!);
        Require(won.Supported && won.Outcome == RoomOutcome.BattleWon && won.State!.Energy == 4 &&
            won.State.Spawn.Train.Context!.Statistics!.EnergyRemainingEndOfTurn == 5 &&
            won.State!.Spawn.Train.Context!.EnergyState!.NextTurn == -1 && won.State!.Spawn.Train.Context!.EnergyState!.EveryTurn == 1 &&
            won.State!.Spawn.Train.Context!.EnergyState!.Phase == "Combat",
            "Terminal combat erased carried energy, recorded pre-trigger end-turn energy or reset pending income early: " + won.UnsupportedReason);
        var afterBossEffects = new[] { new CardActionEffect("Damage", "DropTargetCharacter", 1, true, false, []),
            Effect("GainEnergy", 100, new(1, 99, 1)), Effect("AdjustEnergy", -1), Effect("GainEnergyNextTurn", 5), Effect("GainEnergyEveryTurn", 6) };
        var lethalRoot = new BattleTurnState(terminalSpawn, 2, 3, 5, 0, 0, "New", root.RngStreams, piles, [],
            new(root.PlayRules!.Rooms, [new("energy", "energy", 1, "Spell", "Discard", null, [], afterBossEffects)]));
        var killed = BattleActionModel.PlayCard(lethalRoot, new(1, 0, targetUnitId: 11));
        Require(killed.Supported && killed.Outcome == RoomOutcome.BattleWon && killed.State!.Energy == 1 &&
            killed.State.Spawn.Train.Context!.EnergyState!.NextTurn == 0 && killed.State!.Spawn.Train.Context!.EnergyState!.EveryTurn == 0 &&
            killed.State!.Spawn.Train.Context!.BattleRng.Equals(rng), "Post-boss energy applied or consumed quantity RNG: " + killed.Reason);
        Console.WriteLine("ENERGY-CHECKS PASS: payment/caps, signed no-ops and wrap, late end-turn counters, phase gates/cancellation, retained combat gains, temporary/persistent income, RNG and 32 parallel branches.");
    }
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("EnergyEffects", out var records) || records.GetArrayLength() == 0) return;
        var kinds = new HashSet<string>(); var phases = new HashSet<string>(); var triggers = new HashSet<string>();
        int count = 0, ranged = 0, zero = 0, negative = 0, capped = 0;
        foreach (var record in records.EnumerateArray())
        {
            Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("Sampled").GetBoolean(), "Incomplete native energy effect.");
            CombatContext before = record.GetProperty("Before").Deserialize<CombatContext>()!;
            CombatContext after = record.GetProperty("After").Deserialize<CombatContext>()!;
            CardActionEffect effect = record.GetProperty("Effect").Deserialize<CardActionEffect>()!;
            RngDraw? sample = effect.Range?.Sample(before.BattleRng);
            int amount = sample?.Value ?? effect.Value;
            Require(EnergyModel.Validate(before) == null && EnergyModel.Test(before, effect.Type) && amount == record.GetProperty("Amount").GetInt32(),
                "Native energy gate or quantity differs at " + record.GetProperty("Sequence").GetInt32());
            var sampled = new CombatContext(before.Cards, sample?.State ?? before.BattleRng, before.Gold, before.NextCardId, before.MaxHandSize,
                before.StatusRules, before.Statistics, before.CardInstances, before.CardRegistry, before.AllScenarioBossesDead,
                before.NextAddedTemporaryUpgrades, before.OtherPiles, before.QueryFrame, before.KillCamActivated, before.MagicPower,
                before.IsolatedBattlePreview, before.EnergyState);
            CombatContext predicted = EnergyModel.Apply(sampled, effect.Type, amount);
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted), JsonSerializer.Serialize(after));
            Require(difference == null, "Native energy context differs at " + record.GetProperty("Sequence").GetInt32() + ": " + difference);
            count++; kinds.Add(effect.Type); phases.Add(before.EnergyState!.Phase); triggers.Add(record.GetProperty("Trigger").GetString()!);
            if (effect.Range != null) ranged++; if (amount == 0) zero++; if (amount < 0) negative++;
            if (after.QueryFrame!.Energy == after.EnergyState!.Maximum && amount > 0) capped++;
        }
        Require(kinds.Count == 5 && phases.Count >= 3 && triggers.Contains("") && triggers.Contains("PreCombat") &&
            triggers.Contains("EndTurnPreHandDiscard") && ranged > 0 && zero > 0 && negative > 0 && capped > 0,
            "Native energy coverage incomplete.");
        if (fixture.GetProperty("ModifierScenario").GetString() == "energy-effects-lethal")
        {
            var winning = fixture.GetProperty("Actions").EnumerateArray().Single(record => record.GetProperty("ActualOutcome").GetInt32() == (int)RoomOutcome.BattleWon);
            var before = winning.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = winning.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            int id = winning.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            string dataId = before.Spawn.Train.Context!.Cards.Hand.Single(card => card.InstanceId == id).DataId;
            var rule = before.PlayRules!.Cards.Single(card => card.DataId == dataId);
            Require(rule.Effects[0].Type == "Damage" && rule.Effects.Skip(1).Count(effect => EnergyModel.IsEffect(effect.Type)) == 4 &&
                after.Energy == before.Energy - rule.Cost && after.Spawn.Train.Context!.EnergyState!.NextTurn == before.Spawn.Train.Context.EnergyState!.NextTurn &&
                after.Spawn.Train.Context.EnergyState.EveryTurn == before.Spawn.Train.Context.EnergyState.EveryTurn &&
                after.Spawn.Train.Context.BattleRng.Equals(before.Spawn.Train.Context.BattleRng), "Native winning card did not exercise all post-boss energy gates.");
        }
        Console.WriteLine($"NATIVE-ENERGY-CHECKS PASS: {count} exact contexts, {kinds.Count} modes, {phases.Count} phases, {ranged} ranges, {zero} zero/{negative} negative samples and {capped} capped results.");
    }
    private static void Require(bool pass, string message) { if (!pass) throw new InvalidOperationException(message); }
}
