using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FriendlyRelicPiercingChecks
{
    private const string Reason = "Rolling native state differs after decision 40: $.Rooms[2].Units[0].Health";
    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, HistoricalMismatch);
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var heavy = actual.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 23);
        Require(rows.Length == 40 && last.GetProperty("IsEndTurn").GetBoolean() &&
            before.Spawn.Turn == 6 && actual.Spawn.Turn == 7 && heavy.AssetKey == "HeavyT2_Basic" &&
            heavy.Health == 115 && heavy.Status("damage shield")?.Stacks == 4 &&
            before.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == 23).Status("damage shield")?.Stacks == 2 &&
            before.Spawn.Train.Rooms[1].Units.Any(unit => unit.Id == 21 && unit.Status("ephemeral")?.Stacks == 1) &&
            before.Spawn.Train.Context!.Relics!.Any(relic => relic.AssetKey == "DamageOnBurnoutDeath" &&
                relic.EffectTypes.SequenceEqual(new[] { "RelicEffectDamageEnemyOnFriendlyTrigger", "RelicEffectDamageEnemyOnFriendlyTrigger" })),
            "Missing the natural death-relic damage/shield boundary.");
        Console.WriteLine("FRIENDLY-RELIC-PIERCING PASS:40 native inputs/full successors/RNG; death relic deals5 through shield without consuming it; immutable parents.");
    }
    private static void HistoricalMismatch(BattleTurnState old, BattleTurnState actual)
    {
        JsonNode projected = JsonNode.Parse(NativeBranchObservationChecks.Key(old))!;
        JsonNode observed = JsonNode.Parse(NativeBranchObservationChecks.Key(actual))!;
        foreach (JsonNode unit in new[] { projected["Rooms"]![2]!["Units"]![0]!,
            projected["Context"]!["Enchantments"]!["Rooms"]![2]!["Units"]![0]! })
        {
            Require(unit["Id"]!.GetValue<int>() == 23 && unit["Health"]!.GetValue<int>() == 120,
                "Preserve the original shield-blocked target.");
            unit["Health"] = 115;
            foreach (string field in new[] { "Statuses", "StatusRegistry" })
            {
                JsonNode shield = unit[field]!.AsArray().Single(status => status!["Id"]!.GetValue<string>() == "damage shield")!;
                Require(shield["Stacks"]!.GetValue<int>() == 3, "Preserve the original consumed shield.");
                shield["Stacks"] = 4;
            }
        }
        Require(JsonNode.DeepEquals(projected, observed), "Historical piercing mismatch extends beyond target health and shield caches.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        NativeBranchObservationChecks.Pair(source, replay, originalPredictionPresent: true);
        Console.WriteLine("FRIENDLY-RELIC-PIERCING-PAIR PASS:original shield-blocked forecast and independent repaired native prediction preserved.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
