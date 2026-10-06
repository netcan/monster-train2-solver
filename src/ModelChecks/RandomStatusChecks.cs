using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class RandomStatusChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(92);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 1, 10, cardInstances: []);
        CombatUnit Unit(int id, bool immune = false) => new(id, "unit" + id, CombatTeam.Player, 3, 10, 10, true, false, false,
            immune ? [new("immune", 1)] : []);
        CardActionEffect Effect(int chance, params CombatStatus[] statuses) => new("AddStatus", "Room", chance, false, true, statuses);
        var root = new RoomCombatState(0, false, [Unit(1), Unit(2)], [], context);
        var pool = Effect(50, new("armor", 2, 1, removeWhenTriggered: true), new("regen", 3, 1, removeWhenTriggered: true));
        string parent = JsonSerializer.Serialize(root);
        var child = CardSpellModel.Apply(root, [pool], 0);
        // Seed 92 selects pool entry 1, then chance values 42 and 98 in reverse target order.
        Require(child.Supported && child.State!.Units.Single(unit => unit.Id == 2).Statuses.Single().Id == "regen" &&
            child.State.Units.Single(unit => unit.Id == 2).Statuses.Single().Stacks == 3 &&
            child.State.Units.Single(unit => unit.Id == 1).Statuses.Count == 0 && child.State.Context!.BattleRng.Equals(rng.Next().Next().Next()),
            "Status selection was per-target, chance order was forwards or RNG state differs.");
        var all = CardSpellModel.Apply(root, [Effect(0, pool.Statuses.ToArray())], 0);
        Require(all.Supported && all.State!.Context!.BattleRng.Equals(rng.Next()) &&
            all.State.Units.All(unit => unit.Statuses.Single().Id == "regen"),
            "Zero probability parameter consumed chance draws or failed to apply one shared status.");
        var immuneRoom = new RoomCombatState(0, false, [Unit(1), Unit(2, true)], [], context);
        var blocked = CardSpellModel.Apply(immuneRoom, [pool], 0);
        Require(blocked.Supported && blocked.State!.Context!.BattleRng.Equals(rng.Next().Next().Next()) &&
            blocked.State.Units.Single(unit => unit.Id == 2).Statuses.Single().Id == "immune" &&
            blocked.State.Units.Single(unit => unit.Id == 1).Statuses.Count == 0,
            "An immune target skipped a chance draw or a failed chance added a status.");
        var empty = new RoomCombatState(0, false, [], [], context);
        Require(CardSpellModel.Apply(empty, [pool], 0).State!.Context!.BattleRng.Equals(rng.Next()) &&
            CardSpellModel.Apply(empty, [Effect(50, new CombatStatus("armor", 2))], 0).State!.Context!.BattleRng.Equals(rng),
            "Empty collections lost their pool draw or consumed a per-target chance draw.");
        var full = CardSpellModel.Apply(immuneRoom, [Effect(100, new CombatStatus("armor", 1))], 0);
        Require(full.Supported && full.State!.Context!.BattleRng.Equals(rng.Next().Next()) &&
            full.State.Units.Single(unit => unit.Id == 1).Statuses.Single().Id == "armor",
            "A guaranteed nonzero probability skipped native chance draws.");
        var randomizedTarget = new CardActionEffect("AddStatus", "RandomInRoom", 50, false, true, pool.Statuses);
        var combined = CardSpellModel.Apply(root, [randomizedTarget], 0);
        Require(combined.Supported && combined.State!.Context!.BattleRng.Equals(rng.Next().Next().Next()) &&
            combined.State.Units.All(unit => unit.Statuses.Count == 0),
            "Target, status and chance selection did not consume gameplay RNG in native order.");
        Require(CardSpellModel.TestPlay(root, [pool], 0).CanPlay &&
            !CardSpellModel.Apply(root, [Effect(0)], 0).Supported,
            "Status casting tests consumed gameplay RNG or an empty pool was accepted.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(CardSpellModel.Apply(root, [pool], 0).State) ==
            JsonSerializer.Serialize(child.State), "Parallel random status branches differ."));
        Require(JsonSerializer.Serialize(root) == parent, "Random status application mutated its parent.");
        Console.WriteLine("RANDOM-STATUS-CHECKS PASS: one shared pool choice, reverse chances, zero/full probability, immunity, empty collections, composed RNG and parallel isolation.");
    }

    internal static void Native(JsonElement actions)
    {
        int plays = 0, immunePlays = 0, terminalPlays = 0;
        foreach (JsonElement entry in actions.EnumerateArray())
        {
            var before = entry.GetProperty("Before").Deserialize<BattleTurnState>(ModelJson.Options)!;
            var actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>(ModelJson.Options)!;
            var action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            var card = before.Spawn.Train.Context!.Cards.Hand.Single(card => card.InstanceId == action.CardInstanceId);
            CardPlayRule rule = before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
            if (!rule.Effects.Any(effect => effect.Statuses.Count > 1)) continue;
            plays++;
            var room = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
            var after = actual.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
            CombatUnit[] candidates = room.Units.Where(unit => !unit.IsPyre && unit.Statuses.All(status => status.Id != "untouchable")).ToArray();
            int players = candidates.Count(unit => unit.Team == CombatTeam.Player), enemies = candidates.Length - players;
            UnityRng expected = before.Spawn.Train.Context.BattleRng;
            for (int draw = 0; draw < 3 + 3 * players + enemies; draw++) expected = expected.Next();
            Require(expected.Equals(actual.Spawn.Train.Context!.BattleRng),
                "Native status pool/chance draw count differs, including the empty final enemy collection.");
            Require(rule.Effects.Where(effect => effect.Type == "AddStatus").Select(effect => effect.Value)
                .Distinct().OrderBy(value => value).SequenceEqual([0, 50, 100]), "Native probability coverage is missing.");
            foreach (CombatUnit immune in candidates.Where(unit => unit.Statuses.Any(status => status.Id == "immune")))
            {
                var retained = after.Units.Single(unit => unit.Id == immune.Id);
                Require(retained.Statuses.All(status => status.Id != "armor" && status.Id != "regen"),
                    "Native immune targets unexpectedly gained a random status.");
                immunePlays++;
            }
            if (entry.GetProperty("ActualOutcome").GetInt32() == (int)RoomOutcome.BattleWon) terminalPlays++;
        }
        Require(plays > 0 && immunePlays > 0 && terminalPlays > 0, "Native random statuses lack live immunity or post-kill coverage.");
        Console.WriteLine($"NATIVE-RANDOM-STATUS-COVERAGE PASS: {plays} plays, {immunePlays} immune targets, empty pool selections, post-kill application and exact chance RNG.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
