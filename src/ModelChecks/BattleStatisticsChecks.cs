using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class BattleStatisticsChecks
{
    internal static void Run()
    {
        var values = new List<CardStatisticValue> { new(2, "ThisTurn", "TimesDrawn", 0) };
        var root = new BattleStatistics(values, [], [], [], [], [], [], 0, 0, 0, 12, 0);
        values.Add(new CardStatisticValue(2, "ThisTurn", "TimesPlayed", 40));
        Require(root.Values.Count == 0, "Native query zero keys and caller list leaked into statistics.");
        string original = JsonSerializer.Serialize(root);
        var played = root.Increment(2, "TimesDrawn").Increment(2, "TimesPlayed").Increment(2, "TimesPlayed")
            .Increment(2, "TimesDiscarded").Spawn(1, ["champion", "dragon"]).Death(true, 2)
            .WithEndTurnEnergy(3).WithLastAttackDamage(18);
        Require(played.CardsPlayedThisTurn.SequenceEqual(new[] { 2, 2 }) && played.Value(2, "TimesPlayed") == 2 &&
            played.Value(2, "TimesPlayed", "ThisBattle") == 2 && played.MonstersDeadThisTurn == 1 &&
            played.SubtypesSpawnedThisTurn.Count == 2, "Play history, death or subtype spawn totals differ.");
        var next = played.NextTurn(17).Increment(2, "TimesDrawn");
        Require(next.Value(2, "TimesPlayed", "PreviousTurn") == 2 && next.Value(2, "TimesPlayed") == 0 &&
            next.Value(2, "TimesDrawn", "ThisBattle") == 2 && next.SpawnedThisTurnPerFloor.Count == 0 &&
            next.SpawnedThisBattlePerFloor.Single().Value == 1 && next.MonstersDeadThisTurn == 0 &&
            next.MonstersDeadThisBattle == 1 && next.EnergyRemainingEndOfTurn == 0 && next.GoldStartOfThisTurn == 17 &&
            next.LastAttackDamageDealt == 18 && next.CardsPlayedThisTurn.Count == 0, "Cross-turn statistics differ.");
        Require(next.NextTurn(18).Value(2, "TimesPlayed", "PreviousTurn") == 0, "Older previous-turn statistics survived.");
        var any = root.TrackCards([2, 3]).Increment(2, "TimesDrawn", 4);
        Require(any.Value(2, "TimesDrawn") == 4 && any.Value(2, "AnyCardDrawn") == 2 && any.Value(3, "AnyCardDrawn") == 1,
            "Native Any counters must increment per event and count the source twice.");

        var rng = UnityRng.Seed(1);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 10, 10, statistics: root);
        var enemy = new CombatUnit(1, "enemy", CombatTeam.Enemy, 12, 6, 6, true, false, false, []);
        var player = new CombatUnit(2, "player", CombatTeam.Player, 2, 9, 9, true, false, false,
            [new CombatStatus("armor", 2, 1, removeWhenTriggered: true)], spawnerCardId: 4);
        var room = new RoomCombatState(0, false, [enemy, player], [], context);
        var result = RoomCombatModel.Exchange(room);
        var stats = result.State!.Context!.Statistics!;
        Require(stats.LastAttackDamageDealt == 12 && stats.MonstersDeadThisTurn == 1 &&
            stats.Value(4, "SpawnedMonsterDeaths") == 1 && stats.Value(4, "TimesExhausted") == 1,
            "Blocked/overkill attack or unit death attribution differs.");
        var preview = RoomCombatModel.Resolve(new RoomCombatState(0, false, [enemy, player], [], context, preview: true));
        Require(preview.State!.Context!.Statistics!.MonstersDeadThisTurn == 0 &&
            preview.State.Context.Statistics.Value(4, "TimesExhausted") == 0 &&
            preview.State.Context.Statistics.LastAttackDamageDealt == 12,
            "Native preview must preserve death counters while updating last attack damage.");
        var spell = CardSpellModel.Apply(new RoomCombatState(0, false, [enemy], [], context),
            [new CardActionEffect("Damage", "DropTargetCharacter", 7, true, false, [])], 1, 7);
        Require(spell.State!.Context!.Statistics!.Value(7, "HeroesKilled") == 1 &&
            spell.State.Context.Statistics.LastAttackDamageDealt == 0, "Spell kill attribution or attack statistic differs.");
        var status = CardSpellModel.Apply(new RoomCombatState(0, false, [enemy], [], context),
            [new CardActionEffect("AddStatus", "DropTargetCharacter", 0, true, false, [new CombatStatus("pyregel", 3, 1)])], 1, 7);
        Require(status.State!.Context!.Statistics!.Value(7, "AnyStatusEffectStacksAdded") == 3, "Status stack attribution differs.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(root.Increment(2, "TimesDrawn").Increment(2, "TimesPlayed")
            .Increment(2, "TimesPlayed").Increment(2, "TimesDiscarded").Spawn(1, ["champion", "dragon"]).Death(true, 2)
            .WithEndTurnEnergy(3).WithLastAttackDamage(18)) == JsonSerializer.Serialize(played), "Parallel statistics differ."));
        Require(JsonSerializer.Serialize(root) == original, "A child changed parent statistics.");
        Console.WriteLine("STATISTICS-CHECKS PASS: attribution, duration rollover, overkill/blocked damage, stack counts and parallel isolation.");
    }
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
