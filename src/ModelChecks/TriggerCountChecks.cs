using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TriggerCountChecks
{
    private const string Kind = "CardSpellPlayed";
    internal static void Run()
    {
        var modifiers = new[] { new TriggerCountModifier(Kind, 1) };
        var cache = new TriggerCountState(modifiers, [], ["OnDiscard"], [new("gear", [Kind])]);
        modifiers[0] = new(Kind, 99);
        var context = Context(cache);
        var actor = Actor(1, CombatTeam.Player, [Gold(5), Gold(11, once: true)]);
        var room = new RoomCombatState(0, false, [actor], [], context);
        string parent = Serialize(room);
        var first = Fire(room, actor);
        Require(first.Supported && first.State!.Context!.Gold == 30 && first.State.Units[0].Triggers.All(t => t.FireCount == 2 && t.HasTriggered),
            "Compiled relic count did not repeat the entire once/repeat effect batch: " + first.UnsupportedReason);
        var second = Fire(first.State!, first.State!.Units[0]);
        Require(second.Supported && second.State!.Context!.Gold == 40, "Spent once trigger repeated on a later dispatch.");
        Require(Serialize(room) == parent && cache.Modifiers[0].Value == 1, "Relic count cache or parent was mutated.");
        var blocked = Actor(1, CombatTeam.Player, [Gold(5, blocked: true)]);
        Require(Fire(new(0, false, [blocked], [], context), blocked).State!.Context!.Gold == 5, "Per-trigger count exclusion was ignored.");
        var enemy = Actor(2, CombatTeam.Enemy, [Gold(5)]);
        Require(Fire(new(0, false, [enemy], [], context), enemy).State!.Context!.Gold == 5, "Player-only relic modified an enemy.");
        foreach (int modifier in new[] { -1, -2, int.MaxValue })
        {
            var nonpositive = Context(new([new(Kind, modifier)], [], [], []));
            var once = Actor(1, CombatTeam.Player, [Gold(5, once: true)]);
            var result = Fire(new(0, false, [once], [], nonpositive), once);
            Require(result.Supported && result.State!.Context!.Gold == 0 && result.State.Units[0].Triggers[0].HasTriggered &&
                result.State.Units[0].Triggers[0].FireCount == unchecked(1 + modifier), "Non-positive/wrapped count lost native once marking.");
        }
        var permissive = new TriggerCountState([new(Kind, 2)], [Kind], [], [new("gear", [Kind]), new("empty", [])]);
        var cards = new[] { new CardInstanceState(7, "gear", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, []),
            new CardInstanceState(8, "empty", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, []) };
        var withGear = Context(permissive, cards);
        foreach (var test in new[] { (Equipment: new[] { 7 }, Expected: 3), (Equipment: new[] { 8 }, Expected: 1), (Equipment: Array.Empty<int>(), Expected: 1) })
        {
            var equipped = Actor(2, CombatTeam.Enemy, [Gold(5)], test.Equipment);
            Require(TriggerCountModel.Query(withGear, equipped, equipped.Triggers[0], out var error) == test.Expected && error == null,
                "Enemy count eligibility ignored the original equipment trigger definitions.");
        }
        var missingEquipment = Actor(2, CombatTeam.Enemy, [Gold(5)], [999]);
        TriggerCountModel.Query(withGear, missingEquipment, missingEquipment.Triggers[0], out var missingError);
        Require(missingError != null, "Missing enemy equipment was guessed.");
        var missingFlag = Actor(1, CombatTeam.Player, [new(Kind, false, false, true, 1, [], false)]);
        Require(!Fire(new(0, false, [missingFlag], [], context), missingFlag).Supported, "Missing native count flag was guessed.");
        Require(RelicModel.Validate(Context(null)) != null, "Known relic without its registration cache was accepted.");
        var duplicate = new TriggerCountState([new(Kind, 1), new(Kind, 2)], [], [], []);
        Require(TriggerCountModel.Validate(duplicate) != null, "Malformed duplicate cache entries were accepted.");
        string expected = Serialize(second.State);
        Parallel.For(0, 32, _ =>
        {
            var child = Fire(room, actor);
            Require(Serialize(Fire(child.State!, child.State!.Units[0]).State) == expected, "Parallel trigger count branches differed.");
        });
        Require(Serialize(room) == parent, "Parallel trigger count branches mutated the parent.");
        Console.WriteLine("RELIC-TRIGGER-COUNT-CHECKS PASS: registration cache, fresh/once/repeat/excluded triggers, both teams, original gear eligibility, signed/wrapped counts, complete context and 32 branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out var scenario) || scenario.GetString() is not ("incant-relic" or "incant-relic-combined")) return;
        bool combined = scenario.GetString() == "incant-relic-combined";
        Require(fixture.GetProperty("Schema").GetInt32() is 108 or 109 && fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0, "Incomplete original relic native recording.");
        int queries = 0, players = 0, enemies = 0, spawnQueries = 0, deathQueries = 0;
        foreach (var record in fixture.GetProperty("IncantTriggers").EnumerateArray())
        {
            var before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actor = record.GetProperty("Actor").Deserialize<CombatUnit>()!;
            ValidateContext(before.Context!);
            foreach (var trigger in actor.Triggers)
            {
                int predicted = TriggerCountModel.Query(before.Context, actor, trigger, out string? error);
                Require(error == null && predicted == trigger.FireCount, "Independent native trigger count differs: " + error);
                queries++;
                if (combined && trigger.Kind is "OnSpawn" or "OnDeath")
                {
                    Require(predicted == (actor.Team == CombatTeam.Player ? 2 : 1), "Combined relic spawn/death query differs.");
                    if (trigger.Kind == "OnSpawn") spawnQueries++; else deathQueries++;
                }
                if (trigger.Kind != Kind) continue;
                if (actor.Team == CombatTeam.Player) { Require(predicted == 2, "Original player Incant count is not two."); players++; }
                else { Require(predicted == 1, "Original enemy Incant count is not one."); enemies++; }
            }
        }
        var first = fixture.GetProperty("Actions")[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        ValidateContext(first.Spawn.Train.Context!);
        Require(first.PlayRules!.Cards.Any(card => card.SpawnUnit?.Team == CombatTeam.Player && card.SpawnUnit.Triggers.Any(t =>
            t.Kind == Kind && t.FireCount == 1 && t.NoCountModifiersAllowed == false)), "Fixture lacks a fresh single-count raw unit template.");
        Require(players > 0 && enemies > 0 && queries > 20, "Native trigger count coverage is incomplete.");
        Console.WriteLine($"NATIVE-RELIC-TRIGGER-COUNT-CHECKS PASS: {queries} original queries, fresh template births, player count2/enemy count1, original acquired relic and registration cache; complete callbacks/policies checked separately.");

        if (combined)
        {
            Require(spawnQueries > 0 && deathQueries > 0 && fixture.GetProperty("Turns").EnumerateArray().Any(record =>
                record.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.Statistics!.MonstersDeadThisBattle > 0),
                "Combined relic scene lacks actual spawn/death paths.");
            Console.WriteLine($"NATIVE-COMBINED-RELIC-COUNT-CHECKS PASS: {spawnQueries} spawn/{deathQueries} death queries, three original artifacts, new paid births and actual player deaths; all complete native transitions checked separately.");
        }

        void ValidateContext(CombatContext context)
        {
            Require(RelicModel.Validate(context) == null && context.Relics!.Count(relic => relic.DataId == "410ba540-7c4f-4dc5-a84f-b1d8af508891" &&
                relic.AssetKey == "ExtraSpellCastTrigger" && relic.EffectTypes.SequenceEqual([RelicModel.ModifyTriggerCount])) == 1 &&
                context.TriggerCounts!.Modifiers.Count == (combined ? 3 : 1) && context.TriggerCounts.Modifiers[0].Kind == Kind && context.TriggerCounts.Modifiers[0].Value == 1 &&
                context.TriggerCounts.EnemyAllowedKinds.Count == 0 && context.TriggerCounts.ExcludedCardTriggers.Count == 0 && context.TriggerCounts.EquipmentDefinitions.Count > 0,
                "Original acquired relic/cache/definitions differ from the catalog.");
            if (combined)
                Require(context.Relics!.Count(relic => relic.DataId == "9e0deb69-6196-44a6-8220-85bd0df25f77" && relic.AssetKey == "ExtraSpawnTrigger" &&
                    relic.EffectTypes.SequenceEqual([RelicModel.ModifyTriggerCount])) == 1 && context.Relics!.Count(relic =>
                    relic.DataId == "a5d67620-a9ec-4257-91b5-305336e11987" && relic.AssetKey == "ExtraDeathTrigger" &&
                    relic.EffectTypes.SequenceEqual([RelicModel.ModifyTriggerCount])) == 1 && context.TriggerCounts!.Modifiers.Any(item => item.Kind == "OnSpawn" && item.Value == 1) &&
                    context.TriggerCounts.Modifiers.Any(item => item.Kind == "OnDeath" && item.Value == 1), "Original combined relic registration differs.");
        }
    }
    private static CombatTrigger Gold(int amount, bool once = false, bool blocked = false) => new(Kind, once, false, true, 1,
        [new("CardEffectRewardGold", amount, 0, "", 0, [], false)], false, noCountModifiersAllowed: blocked);
    private static CombatUnit Actor(int id, CombatTeam team, CombatTrigger[] triggers, int[]? equipment = null) =>
        new(id, "actor", team, 0, 20, 20, false, false, false, [], triggers, equipmentCards: equipment);
    private static CombatContext Context(TriggerCountState? cache, CardInstanceState[]? cards = null)
    {
        var rng = UnityRng.Seed(17);
        return new(new([], [], [], rng, 0, []), rng, 0, 1, 10, cardInstances: cards,
            relics: [new("410ba540-7c4f-4dc5-a84f-b1d8af508891", "ExtraSpellCastTrigger", [RelicModel.ModifyTriggerCount])], triggerCounts: cache);
    }
    private static RoomCombatResult Fire(RoomCombatState room, CombatUnit actor) => RoomCombatModel.ApplyQueuedCharacterTrigger(room,
        new(room.RoomIndex, actor, Kind, admission: RoomCombatModel.CharacterTriggerAdmission.Accepted), _ => { });
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
