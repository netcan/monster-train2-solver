using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class RandomSpellChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(92);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 1, 10, cardInstances: []);
        CombatUnit Unit(int id, CombatTeam team, int health = 10, CombatStatus[]? statuses = null) =>
            new(id, "unit" + id, team, 3, health, 10, true, false, false, statuses ?? [],
                modifiers: new(3, 0, 0, 1, 1, true, false, []));
        CardActionEffect Random(string type, int value, bool enemy, bool player, CardEffectTests? tests = null) =>
            new(type, "RandomInRoom", value, enemy, player, [], tests: tests);
        var root = new RoomCombatState(0, false, [Unit(1, CombatTeam.Enemy), Unit(2, CombatTeam.Enemy), Unit(3, CombatTeam.Player)], [], context);
        CardActionEffect[] chain = [Random("Damage", 2, true, false),
            new("AddStatus", "LastTargetedCharacters", 0, true, false, [new("armor", 3, 1, removeWhenTriggered: true)]),
            Random("Heal", 0, false, true)];
        string parent = JsonSerializer.Serialize(root);
        var cast = CardSpellModel.TestPlay(root, chain, 0);
        var child = CardSpellModel.Apply(root, chain, 0);
        Require(cast.Supported && cast.CanPlay && child.Supported && child.State!.Context!.BattleRng.Equals(rng.Next().Next()) &&
            child.State.Units.Count(unit => unit.Team == CombatTeam.Enemy && unit.Health == 8 && unit.Statuses.FirstOrDefault(status => status.Id == "armor")?.Stacks == 3) == 1 &&
            child.State.Units.Single(unit => unit.Team == CombatTeam.Player).Statuses.All(status => status.Id != "armor"),
            "Random selection, gameplay draw count, zero-heal draw or sticky last target differs.");
        var empty = new RoomCombatState(0, false, [], [], context);
        Require(CardSpellModel.Apply(empty, [Random("Damage", 2, true, false)], 0).State!.Context!.BattleRng.Equals(rng),
            "An empty random target collection consumed gameplay RNG.");
        var filtered = new RoomCombatState(0, false, [Unit(1, CombatTeam.Enemy, statuses: [new("untouchable", 1)]),
            Unit(2, CombatTeam.Player), new(3, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, [])], [], context);
        var one = CardSpellModel.Apply(filtered, [Random("Damage", 2, true, true)], 0);
        Require(one.Supported && one.State!.Units.Single(unit => unit.Id == 2).Health == 8 &&
            one.State.Context!.BattleRng.Equals(rng.Next()) && one.State.Units.Single(unit => unit.Id == 3).Health == 80,
            "Single-candidate random selection skipped a draw or included untouchable/Pyre targets.");
        var boss = new CombatUnit(4, "boss", CombatTeam.Enemy, 7, 1, 125, true, false, true, []);
        var terminal = new RoomCombatState(0, false, [boss, Unit(3, CombatTeam.Player)], [], context);
        var skipped = CardSpellModel.Apply(terminal, [Random("Damage", 1, true, false),
            Random("Heal", 0, false, true, new(true, false, false, false, false)), Random("Heal", 0, false, true)], 0);
        Require(skipped.Supported && skipped.Outcome == RoomOutcome.BattleWon && skipped.State!.Context!.BattleRng.Equals(rng.Next().Next()),
            "A failed post-kill runtime test consumed Battle RNG or stopped a permitted following effect.");
        Require(!CardSpellModel.TestPlay(root, [Random("Damage", 2, true, true),
            new("Heal", "LastTargetedCharacters", 1, false, true, [], tests: new(true, true, false, false))], 0).Supported,
            "Auxiliary-dependent mandatory casting tests returned a search child.");
        Parallel.For(0, 32, _ =>
        {
            for (int attempt = 0; attempt < 3; attempt++) Require(CardSpellModel.TestPlay(root, chain, 0).CanPlay, "Repeated random cast checks differ.");
            Require(JsonSerializer.Serialize(CardSpellModel.Apply(root, chain, 0).State) == JsonSerializer.Serialize(child.State),
                "Parallel random spell branches differ.");
        });
        Require(JsonSerializer.Serialize(root) == parent, "Random casting or child simulation mutated the parent.");
        Console.WriteLine("RANDOM-SPELL-CHECKS PASS: candidates, empty/single draws, test isolation, sticky last targets, failed runtime gates and parallel branches.");
    }

    internal static void Native(JsonElement actions)
    {
        int plays = 0, emptyEnemy = 0, multiEnemy = 0;
        var drawCounts = new HashSet<int>();
        foreach (JsonElement entry in actions.EnumerateArray())
        {
            var before = entry.GetProperty("Before").Deserialize<BattleTurnState>(ModelJson.Options)!;
            var actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>(ModelJson.Options)!;
            var action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardToken card = before.Spawn.Train.Context!.Cards.Hand.Single(card => card.InstanceId == action.CardInstanceId);
            CardPlayRule rule = before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
            if (!rule.Effects.Any(effect => effect.Target == "RandomInRoom")) continue;
            plays++;
            Require(before.RngStreams.All(stream => stream.Name != "BattleTest"), "The random oracle includes UI-dependent test RNG.");
            var room = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
            int enemies = room.Units.Count(unit => unit.Team == CombatTeam.Enemy);
            if (enemies == 0 || rule.Effects.Last().Target == "RandomInRoom" && rule.Effects.Last().AllowEnemy &&
                !rule.Effects.Last().AllowPlayer && actual.Spawn.Train.Rooms.Single(next => next.RoomIndex == action.RoomIndex)
                    .Units.All(unit => unit.Team != CombatTeam.Enemy)) emptyEnemy++;
            if (enemies >= 2) multiEnemy++;
            UnityRng rng = before.Spawn.Train.Context.BattleRng;
            int draws = 0;
            while (!rng.Equals(actual.Spawn.Train.Context!.BattleRng) && draws < 16) { rng = rng.Next(); draws++; }
            Require(draws < 16, "Native random effects advanced an unexplained gameplay RNG stream.");
            drawCounts.Add(draws);
        }
        Require(plays > 0 && emptyEnemy > 0 && multiEnemy > 0 && drawCounts.Contains(4),
            "The random oracle lacks empty/multiple enemy collections or the expected four live draws.");
        Console.WriteLine($"NATIVE-RANDOM-COVERAGE PASS: {plays} plays, {emptyEnemy} empty-enemy collections, {multiEnemy} multiple-enemy rooms and complete gameplay RNG states.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
