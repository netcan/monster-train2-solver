using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HealGrowthRecordChecks
{
    private const string Reason = "Rolling native state differs after decision 27: $.Context.CardInstances[6].Permanent.Upgrades[3].Stats.Damage";
    private const string Upgrade = "4d25c6e2-e95b-4388-8b65-33fb1ab395ac";
    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, HistoricalMismatch);
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardUpgradeModifier[] Growth(BattleTurnState state) => state.Spawn.Train.Context!.CardInstances!.Single(card => card.InstanceId == 7)
            .Permanent.Upgrades.Where(upgrade => upgrade.DataId == Upgrade).ToArray();
        Require(rows.Length == 27 && before.Spawn.Turn == 4 && action.CardInstanceId == 12 && action.TargetUnitId == 3 &&
            Growth(before).Select(upgrade => upgrade.Stats.Damage).SequenceEqual([18, 3]) &&
            Growth(after).Select(upgrade => upgrade.Stats.Damage).SequenceEqual([18, 6]) &&
            before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 3).Attack == 55 &&
            after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 3).Attack == 58,
            "Heal growth must mutate this spawn's live record while preserving earlier battles'18 attack record.");
        Console.WriteLine("HEAL-GROWTH-RECORD PASS:27 native inputs/full successors/RNG, historical18 retained/current3 to6 and live attack55 to58; immutable parents.");
    }
    private static void HistoricalMismatch(BattleTurnState old, BattleTurnState actual)
    {
        Require(old.Spawn.Train.Context!.CardInstances!.Single(card => card.InstanceId == 7).Permanent.Upgrades
            .Where(upgrade => upgrade.DataId == Upgrade).Select(upgrade => upgrade.Stats.Damage).SequenceEqual([21, 3]),
            "Preserve the original21/3 forecast on card7.");
        JsonNode projected = JsonNode.Parse(NativeBranchObservationChecks.Key(old))!;
        JsonNode observed = JsonNode.Parse(NativeBranchObservationChecks.Key(actual))!;
        int changes = 0;
        void Project(JsonNode? node, JsonNode? native, int? ownerCard = null)
        {
            if (node is JsonObject row && native is JsonObject other)
            {
                if (row["Permanent"] != null && row["InstanceId"] is JsonValue identity) ownerCard = identity.GetValue<int>();
                if (ownerCard == 7 && row["DataId"]?.GetValue<string>() == Upgrade && row["Stats"] is JsonObject stats && other["Stats"] is JsonObject nativeStats &&
                    stats["Damage"]!.GetValue<int>() != nativeStats["Damage"]!.GetValue<int>())
                {
                    int from = stats["Damage"]!.GetValue<int>(), to = nativeStats["Damage"]!.GetValue<int>();
                    Require(from == 21 && to == 18 || from == 3 && to == 6, "Unrelated historical growth change.");
                    stats["Damage"] = to; changes++;
                }
                foreach (var child in row.ToArray()) Project(child.Value, other[child.Key], ownerCard);
            }
            else if (node is JsonArray array && native is JsonArray otherArray && array.Count == otherArray.Count)
                for (int index = 0; index < array.Count; index++) Project(array[index], otherArray[index], ownerCard);
        }
        Project(projected, observed);
        Require(changes > 0 && JsonNode.DeepEquals(projected, observed), "Historical mismatch extends beyond the two growth records.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        NativeBranchObservationChecks.Pair(source, replay, originalPredictionPresent: true);
        Console.WriteLine("HEAL-GROWTH-RECORD-PAIR PASS:original record mismatch and independently repaired forecast preserved.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
