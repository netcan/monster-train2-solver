using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class TerminalDeathChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(109);
        var owner = CardInstanceState.Empty(1, "owner");
        var generated = CardInstanceState.Empty(2, "generated");
        var statistics = new BattleStatistics([new(2, "ThisBattle", "TimesDrawn", 1)], [], [], [], [], [], [],
            0, 0, 0, 0, 0, [1, 2], [1], [1, 2]);
        var context = new CombatContext(new([new(2, "generated")], [], [], rng, 0, []), rng, 0, 3, 10,
            statistics: statistics, cardInstances: [owner, generated], cardRegistry: [owner, generated], allScenarioBossesDead: false,
            otherPiles: [new("Standby", [new(1, "owner")]), new("Exhausted", [])], killCamActivated: false);
        var damage = new CombatEffect("CardEffectDamage", 3, 0, "", 0, [], false, action: new("Damage", "Room", 3, false, true, []));
        CombatTrigger Death(params CombatEffect[] effects) => new("OnDeath", false, false, false, 1, effects, false);
        CombatUnit Boss(CombatTrigger[]? triggers = null) => new(4, "boss", CombatTeam.Enemy, 0, 1, 1, true, false, true, [], triggers ?? [],
            modifiers: new(0, 0, 0, 1, 1, true, false, []), isBoss: true);
        CombatUnit Player(int id = 1, int hp = 2, int attack = 2, CombatTrigger[]? triggers = null, CombatStatus[]? statuses = null) => new(id, "player", CombatTeam.Player,
            attack, hp, hp, true, false, false, statuses ?? [], triggers ?? [], id, modifiers: new(attack, 0, 0, 1, 1, true, false, []), isBoss: false);
        RoomCombatState Room(CombatUnit[] units, CombatContext? c = null, bool preview = false) => new(0, false, units, [], c ?? context, preview);
        var root = Room([Boss([Death(damage)]), Player()]); string parent = JsonSerializer.Serialize(root);
        var result = RoomCombatModel.Resolve(root);
        Require(result.Supported && result.Outcome == RoomOutcome.BattleWon && result.State!.Units.Count == 0 &&
            result.State.Context!.KillCamActivated == true && result.State.Context.AllScenarioBossesDead == true &&
            result.State.Context.CardInstances!.Count == 0 && result.State.Context.CardRegistry!.Count == 2 &&
            result.State.Context.OtherPiles!.All(p => p.Cards.Count == 0), "Terminal chained death lost the kill camera, detached references or empty piles: " + result.UnsupportedReason);
        BattleStatistics settled = result.State!.Context!.Statistics!;
        Require(settled.MonstersDeadThisTurn == 1 && settled.Value(1, "SpawnedMonsterDeaths") == 1 &&
            settled.Value(1, "AnyMonsterDeath") == 2 && settled.Value(1, "TimesExhausted") == 0 &&
            settled.Value(1, "AnyExhausted") == 0 && settled.StoredCards!.SequenceEqual([1]),
            "A post-clear death exhausted its absent standby card or retained generated statistic keys.");
        var noCallback = RoomCombatModel.Exchange(Room([Boss(), Player()]));
        Require(noCallback.State!.Context!.Statistics!.StoredCards!.SequenceEqual([1, 2]) &&
            noCallback.State.Context.CardInstances!.Count == 0, "Unattributed boss death refreshed cached keys without a native stat call.");
        var generatedSpell = RoomCombatModel.ApplyCardDamage(Room([Boss()]), 4, 9, 2);
        Require(generatedSpell.Supported && generatedSpell.State!.Context!.Statistics!.StoredCards!.SequenceEqual([1]) &&
            generatedSpell.State.Context.Statistics.Value(2, "HeroesKilled") == 0 && generatedSpell.State.Context.Statistics.Value(1, "AnyHeroKilled") == 1,
            "Generated terminal source was credited after clearing or lost the global kill event.");
        var permanentSpell = RoomCombatModel.ApplyCardDamage(root, 4, 9, 1);
        Require(permanentSpell.Supported && permanentSpell.State!.Context!.Statistics!.Value(1, "HeroesKilled") == 1 &&
            permanentSpell.State.Context.Statistics.Value(1, "AnyHeroKilled") == 2 &&
            permanentSpell.State.Context.Statistics.Value(1, "TimesExhausted") == 0, "Permanent terminal kill attribution or detached spawner settlement differs.");
        var preview = RoomCombatModel.Exchange(Room(root.Units.ToArray(), preview: true));
        Require(preview.Supported && preview.State!.Context!.KillCamActivated == false && preview.State.Context.CardInstances!.Count == 2 &&
            preview.State.Context.Statistics!.MonstersDeadThisTurn == 0, "Preview activated the live kill camera or death counters.");
        var lastBoss = new CombatUnit(4, "boss", CombatTeam.Enemy, 0, 1, 1, true, false, true, [new("spikes", 99, 1)],
            [Death(new CombatEffect("CardEffectRewardGold", 2, 0, "", 0, [], false))],
            modifiers: new(0, 0, 0, 1, 1, true, false, []), isBoss: true);
        var canceledDeath = Player(triggers: [Death(new CombatEffect("CardEffectRewardGold", 3, 0, "", 0, [], false))], statuses: [new("sweep", 1)]);
        foreach (var canceled in new[] { RoomCombatModel.Resolve(Room([lastBoss, canceledDeath])),
            RoomCombatModel.ApplyUnitTurn(Room([lastBoss, canceledDeath]), 1) })
            Require(canceled.Supported && canceled.Outcome == RoomOutcome.BattleWon && canceled.State!.Units.Count == 0 &&
                canceled.State.Context!.Gold == GoldRewardModel.Adjust(2),
                "Boss removal did not cancel a later player death in the already-selected combat removal batch: " + JsonSerializer.Serialize(canceled));

        // The new stat value is observable by the dying unit's damage trait inside OnDeath.
        var scaledOwner = new CardInstanceState(1, "owner", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            damageScalingTraits: [new(new("AnyMonsterDeath"), 1, 1, true)]);
        var ordinary = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 3, 10,
            statistics: BattleStatistics.Empty().TrackCards([1, 2]), cardInstances: [scaledOwner, generated],
            otherPiles: [new("Standby", [new(1, "owner"), new(2, "generated")]), new("Exhausted", [])], killCamActivated: false);
        var retaliation = new CombatEffect("CardEffectDamage", 1, 0, "", 0, [], false, action: new("Damage", "Room", 1, true, false, []));
        var killer = new CombatUnit(4, "enemy", CombatTeam.Enemy, 9, 30, 30, true, false, false, [], isBoss: false);
        var deathScaled = RoomCombatModel.Exchange(Room([killer, Player(triggers: [Death(retaliation)]), Player(2, 10, 0)], ordinary));
        Require(deathScaled.Supported && deathScaled.State!.Units.Single(u => u.Id == 4).Health == 28 &&
            deathScaled.State.Context!.Statistics!.MonstersDeadThisTurn == 1, "OnDeath queried counters before the native death signal.");

        // A captured kill camera must survive each context reconstruction, even when cards return.
        var alreadyCleared = new CombatContext(context.Cards, rng, 0, 3, 10, statistics: statistics,
            cardInstances: [owner, generated], cardRegistry: [owner, generated], allScenarioBossesDead: false,
            otherPiles: context.OtherPiles, killCamActivated: true);
        var secondTerminal = RoomCombatModel.Exchange(Room([Boss(), Player()], alreadyCleared));
        Require(secondTerminal.Supported && secondTerminal.State!.Context!.CardInstances!.Count == 2 &&
            secondTerminal.State.Context.KillCamActivated == true, "Nested terminal death cleared cards a second time.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(RoomCombatModel.Resolve(root)) == JsonSerializer.Serialize(result),
            "Parallel terminal death branches differ."));
        Require(JsonSerializer.Serialize(root) == parent, "Terminal death simulation mutated its parent.");
        Console.WriteLine("TERMINAL-DEATH PASS: clearing before death signals, detached spawners without exhaustion, cache refresh/attribution, live death scaling, preview, one-shot kill camera and 32 parallel branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out var scenario) || scenario.GetString() != "terminal-death-damage") return;
        int terminal = 0, afterClear = 0, cacheDrops = 0, lethalHits = 0;
        foreach (FixtureValue record in fixture.GetProperty("KillCams").EnumerateArray())
        {
            Require(record.GetProperty("Completed").GetBoolean(), "Native kill camera capture is incomplete.");
            var before = record.GetProperty("Before").Deserialize<CombatContext>()!;
            var actual = record.GetProperty("Actual").Deserialize<CombatContext>()!;
            if (before.KillCamActivated == true)
            {
                Require(actual.KillCamActivated == true, "Repeated kill camera lost its activation gate.");
                continue;
            }
            Require(actual.KillCamActivated == true && before.CardInstances!.Count > 0 && actual.CardInstances!.Count == 0 &&
                actual.OtherPiles!.All(p => p.Cards.Count == 0), "Native kill camera did not remove owned cards and piles.");
            Require(ModelJson.Difference(JsonSerializer.Serialize(before.Statistics), JsonSerializer.Serialize(actual.Statistics)) == null,
                "Passive kill camera clearing refreshed native statistics.");
            terminal++;
        }
        foreach (FixtureValue record in fixture.GetProperty("TerminalDeaths").EnumerateArray())
        {
            Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("FinishedDying").GetBoolean() &&
                !record.GetProperty("Sacrifice").GetBoolean(), "Native death signal capture is incomplete or uses unmodeled sacrifice statistics.");
            var before = record.GetProperty("Before").Deserialize<CombatContext>()!;
            var actual = record.GetProperty("Actual").Deserialize<CombatContext>()!;
            bool clearing = record.GetProperty("Terminal").GetBoolean() && before.KillCamActivated == false;
            int source = record.GetProperty("SourceCardId").GetInt32();
            int responsible = source > 0 ? source : record.GetProperty("SpawnerCardId").GetInt32();
            BattleStatistics expected = before.Statistics!;
            if (clearing)
            {
                // Passive ClearCards removes ownership; existing cached statistic entries survive.
                var cleared = new CombatContext(new([], [], [], before.Cards.Rng, before.Cards.DrawModifier, []), before.BattleRng,
                    before.Gold, before.NextCardId, before.MaxHandSize, statistics: expected, cardInstances: [], cardRegistry: before.CardRegistry);
                expected = cleared.Statistics!;
                terminal++;
                Require(actual.KillCamActivated == true && actual.CardInstances!.Count == 0 && actual.OtherPiles!.All(p => p.Cards.Count == 0),
                    "Native terminal death did not clear cards before its death signal.");
            }
            bool empty = clearing || before.CardInstances!.Count == 0;
            if (empty && responsible > 0) expected = expected.RefreshDeckAfterCardTerminal();
            expected = expected.Death(record.GetProperty("Team").GetInt32() == (int)CombatTeam.Player, responsible, requireTrackedCard: empty);
            Require(ModelJson.Difference(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual.Statistics)) == null,
                "Independent native death signal statistics differ.");
            if (before.KillCamActivated == true && record.GetProperty("Team").GetInt32() == (int)CombatTeam.Player)
            {
                Require(actual.CardInstances!.Count == 0 && actual.OtherPiles!.All(p => p.Cards.Count == 0), "A post-clear spawner returned during its death signal.");
                afterClear++; cacheDrops += before.Statistics!.StoredCards!.Except(actual.Statistics!.StoredCards!).Any() ? 1 : 0;
            }
        }
        foreach (FixtureValue sample in fixture.GetProperty("TriggeredDamage").EnumerateArray())
        {
            if (sample.GetProperty("Stage").GetString() != "Application" || sample.GetProperty("TriggerKind").GetString() != "OnDeath") continue;
            foreach (FixtureValue request in sample.GetProperty("Requests").EnumerateArray())
            {
                var context = request.GetProperty("Context").Deserialize<CombatContext>()!;
                if (context.KillCamActivated != true) continue;
                Require(context.CardInstances!.Count == 0 && context.OtherPiles!.All(p => p.Cards.Count == 0), "Terminal OnDeath damage ran before native card clearing.");
                lethalHits += request.GetProperty("AfterHealth").GetInt32() == 0 ? 1 : 0;
            }
        }
        Require(terminal > 0 && afterClear > 0 && cacheDrops > 0 && lethalHits > 0, "Native terminal death coverage is incomplete.");
        Console.WriteLine($"TERMINAL-DEATH-NATIVE PASS: terminal-clears={terminal}, post-clear-deaths={afterClear}, cache-drops={cacheDrops}, lethal-after-clear-hits={lethalHits}; exact native death signals and no spawner exhaustion.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
