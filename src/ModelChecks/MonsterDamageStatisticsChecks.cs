using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MonsterDamageStatisticsChecks
{
    internal static void Native(string path)
    {
        using var fixture = ModelJson.ReadFixture(path);
        FixtureValue root = fixture.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == "IsolatedNativeOnMonstersDamaged" &&
            root.GetProperty("LiveContextUnchanged").GetBoolean() && root.GetProperty("BattleEndCountersZero").GetBoolean(),
            "Unexpected or incomplete monster damage component.");
        FixtureValue[] rows = root.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == 43, "Missing monster damage/reset cases.");
        int previews = 0, refreshes = 0, overflows = 0;
        foreach (FixtureValue row in rows)
        {
            BattleStatistics before = row.GetProperty("Before").Deserialize<BattleStatistics>()!;
            BattleStatistics actual = row.GetProperty("Actual").Deserialize<BattleStatistics>()!;
            string unchanged = JsonSerializer.Serialize(before, ModelJson.Options);
            BattleStatistics Replay()
            {
                if (row.GetProperty("Kind").GetString() == "NextTurn") return before.NextTurn(actual.GoldStartOfThisTurn);
                if (row.GetProperty("Preview").GetBoolean()) return before;
                return before.RecordMonsterDamage(row.GetProperty("Amount").GetInt32(),
                    row.GetProperty("FriendlyAmount").GetInt32(), before.TrackedCards);
            }
            Require(ModelJson.Difference(JsonSerializer.Serialize(Replay(), ModelJson.Options),
                JsonSerializer.Serialize(actual, ModelJson.Options)) == null, "Native monster damage counter/cache transition differs.");
            Parallel.For(0, 32, _ => Require(ModelJson.Difference(JsonSerializer.Serialize(Replay(), ModelJson.Options),
                JsonSerializer.Serialize(actual, ModelJson.Options)) == null, "Monster damage parallel branch differs."));
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == unchanged, "Monster damage child mutated its parent.");
            if (row.GetProperty("Kind").GetString() == "Increment")
            {
                bool preview = row.GetProperty("Preview").GetBoolean();
                if (preview) previews++;
                int amount = row.GetProperty("Amount").GetInt32(), friendly = row.GetProperty("FriendlyAmount").GetInt32();
                string[] signals = preview ? [] : (amount > 0 ? new[] { "DamageDealtToMonsters" } : [])
                    .Concat(friendly > 0 ? new[] { "FriendlyDamageDealtToMonsters" } : []).ToArray();
                Require(row.GetProperty("Signals").EnumerateArray().Select(value => value.GetString()).SequenceEqual(signals),
                    "Native monster damage signal order differs.");
                if (actual.StoredCards!.Count > before.StoredCards!.Count) refreshes++;
                if (amount > 0 && actual.MonsterDamage!.ThisTurn < before.MonsterDamage!.ThisTurn) overflows++;
            }
        }
        FixtureValue[] queries = root.GetProperty("Queries").EnumerateArray().ToArray();
        Require(queries.Length == 6 && previews == 21 && refreshes == 15 && overflows > 0, "Missing control/cache/overflow coverage.");
        foreach (FixtureValue query in queries)
        {
            BattleStatistics before = query.GetProperty("Before").Deserialize<BattleStatistics>()!;
            BattleStatistics actual = query.GetProperty("Actual").Deserialize<BattleStatistics>()!;
            UnityRng rng = UnityRng.Seed(1);
            var cards = new CardCycleState(before.TrackedCards.Select(id => new CardToken(id, "counter-query")).ToArray(), [], [], rng, 0, []);
            var context = new CombatContext(cards, rng, 0, 100, 10, statistics: before, otherPiles: []);
            StatisticQueryResult result = StatisticQueryModel.Evaluate(context,
                new CardStatisticQuery(query.GetProperty("Type").GetString()!, query.GetProperty("Duration").GetString()!),
                query.GetProperty("SourceCardId").GetInt32());
            Require(result.Supported && result.Value == query.GetProperty("Value").GetInt32() &&
                ModelJson.Difference(JsonSerializer.Serialize(result.Context!.Statistics, ModelJson.Options),
                    JsonSerializer.Serialize(actual, ModelJson.Options)) == null, "Native monster damage scalar query differs.");
        }
        DamageBoundaries();
        Console.WriteLine("MONSTER-DAMAGE-STATISTICS PASS: 42 native increments, preview/no-op controls, lazy cache, signed overflow, turn/battle reset, 6 native queries and 32 immutable branches per transition.");
    }

    private static void DamageBoundaries()
    {
        UnityRng rng = UnityRng.Seed(1);
        var stats = new BattleStatistics([], [], [], [], [], [], [], 0, 0, 0, 0, 0,
            trackedCards: [1, 2], storedCards: [1], deckCards: [1, 2], monsterDamage: new(0, 0, 0, 0));
        var context = new CombatContext(new CardCycleState([new CardToken(1, "one"), new CardToken(2, "two")], [], [], rng, 0, []),
            rng, 0, 3, 10, statistics: stats);
        var target = new CombatUnit(2, "player", CombatTeam.Player, 0, 4, 4, true, false, false,
            [new CombatStatus("armor", 3, 1, removeWhenTriggered: true)]);
        foreach (bool preview in new[] { false, true })
        foreach (bool friendly in new[] { false, true })
        {
            var actor = new CombatUnit(1, "attacker", friendly ? CombatTeam.Player : CombatTeam.Enemy, 10, 10, 10, true, false, false, []);
            var room = new RoomCombatState(0, false, [actor, target], [], context, preview);
            RoomCombatResult result = RoomCombatModel.ApplyCardDamage(room, 2, 10, sourceCardId: 1, attackerUnitId: 1);
            Require(result.Supported, result.UnsupportedReason ?? "Damage boundary unsupported.");
            MonsterDamageStatistics damage = result.State!.Context!.Statistics!.MonsterDamage!;
            Require(damage.ThisTurn == (preview ? 0 : 7) && damage.FriendlyThisTurn == (!preview && friendly ? 7 : 0),
                "Armor/overkill, preview or attacker precedence differs.");
        }
        Require(stats.StoredCards!.SequenceEqual(new[] { 1 }) && stats.MonsterDamage!.ThisTurn == 0, "Damage changed parent counters/cache.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
