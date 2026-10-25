using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CardCorruptionAttackUpgradeChecks
{
    internal static void NaturalQueries(FixtureValue source)
    {
        int queries = 0, positive = 0;
        foreach (FixtureValue record in source.GetProperty("UnitSelfAttackQueries").EnumerateArray())
        {
            RoomCombatState room = record.GetProperty("Room").Deserialize<RoomCombatState>()!;
            foreach (FixtureValue query in record.GetProperty("Attacks").EnumerateArray())
            {
                CombatUnit unit = room.Units.Single(item => item.Id == query.GetProperty("UnitId").GetInt32());
                CardInstanceState? card = room.Context?.FindCard(unit.SpawnerCardId);
                if (card?.Permanent.Upgrades.Any(CardCorruptionAttackUpgradeModel.Known) != true) continue;
                Require(unit.Modifiers?.SelfAttackRules is { Count: 1 } rules &&
                    rules[0].Type == CardCorruptionAttackUpgradeModel.Type && rules[0].Coefficient == 5 && rules[0].Divisor == 1,
                    "Natural merchant-upgrade host lost its exact corruption rule.");
                int expected = RelicResourceModel.AttackDamage(unit, room.Context), actual = query.GetProperty("Attack").GetInt32();
                Require(expected == actual,
                    $"Native merchant-upgrade attack getter differs: unit={unit.Id}, room={room.RoomIndex}, expected={expected}, actual={actual}.");
                queries++;
                if (UnitSelfAttackModel.Additional(unit, room.Context) > 0) positive++;
            }
        }
        Require(queries > 0 && positive > 0, "Natural calibration needs independent native attack getters at positive corruption.");
        Console.WriteLine($"CARD-CORRUPTION-ATTACK-QUERIES PASS: {queries} exact native attack getters, {positive} at positive corruption on the original merchant-upgrade host.");
    }

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, CardCorruptionAttackUpgradeModel.Guard);
        Require(rows.Length == 1, "Wrong original merchant-upgrade boundary.");
        BattleTurnState before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CardUpgradeModifier upgrade = before.Spawn.Train.Context!.FindCard(5)!.Permanent.Upgrades
            .Single(item => item.DataId == CardCorruptionAttackUpgradeModel.DataId);
        Require(CardCorruptionAttackUpgradeModel.Known(upgrade) &&
            rows[0].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == 5,
            "Changed original merchant-upgrade signature.");
        CombatUnit unit = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(item => item.SpawnerCardId == 5);
        Require(unit.Id == 2 && unit.Modifiers!.SelfAttackRules is { Count: 1 } rules &&
            rules[0].Type == CardCorruptionAttackUpgradeModel.Type && rules[0].Coefficient == 5 &&
            rules[0].Divisor == 1 && unit.BaseAttack == 45 && unit.Health == 75,
            "Original card-birth corruption attack differs.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 8, _ =>
        {
            BattleActionResult result = BattleActionModel.PlayCard(before,
                rows[0].GetProperty("Action").Deserialize<PlayCardAction>()!);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Parallel native birth state/RNG differs.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Birth mutated its parent.");
        // This fixture calibrates card birth. The captured unsupported payload
        // intentionally keeps live-unit upgrade/removal guarded.
        Require(upgrade.ExternalInteractions.SequenceEqual([CardCorruptionAttackUpgradeModel.Guard]) &&
            !CardCorruptionAttackUpgradeModel.Known(upgrade.WithStats(new CardStatModifier(damage: 1))),
            "Unknown signatures or live-unit APIs lost their guard.");
        if (replay is FixtureValue second)
        {
            NativeBranchObservationChecks.Replay(second, CardCorruptionAttackUpgradeModel.Guard);
            NativeBranchObservationChecks.Pair(source, second);
        }
        Console.WriteLine("CARD-CORRUPTION-ATTACK-UPGRADE PASS: original native FirstOfKin birth inherits5 attack per room corruption; complete state/RNG; eight immutable parallel branches; changed signatures and live-unit upgrade APIs guarded; independentPair=" + replay.HasValue);
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
