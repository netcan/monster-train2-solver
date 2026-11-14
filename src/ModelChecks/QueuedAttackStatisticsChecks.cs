using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class QueuedAttackStatisticsChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        var root = document.RootElement;
        void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
        Require(root.GetProperty("Boundary").GetString() == "NativeQueuedAttackStatistics" &&
            root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("NativeCompleted").GetBoolean(), "Incomplete native attack statistics evidence.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero source " + key);
        int pyre = 0, ordinary = 0;
        foreach (var stage in root.GetProperty("Stages").EnumerateArray())
        {
            var before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            bool queued = before.Units.Any(unit => unit.IsPyre);
            Require(before.Units.Any(unit => unit.Status("spikes")?.Stacks == 2) &&
                actual.Context!.Statistics!.LastAttackDamageDealt == (queued ? 45 : 2), "Missing queued/ordinary retaliation witness.");
            string frozen = JsonSerializer.Serialize(before, ModelJson.Options);
            string expectedUnits = JsonSerializer.Serialize(actual.Units, ModelJson.Options);
            string expectedContext = JsonSerializer.Serialize(BattleTurnChecks.ComparableContext(actual.Context), ModelJson.Options);
            void Match()
            {
                var result = stage.GetProperty("Kind").GetString() == "Exchange"
                    ? RoomCombatModel.Exchange(before) : RoomCombatModel.Resolve(before);
                Require(result.Supported && JsonSerializer.Serialize(result.State!.Units, ModelJson.Options) == expectedUnits &&
                    JsonSerializer.Serialize(BattleTurnChecks.ComparableContext(result.State.Context!), ModelJson.Options) == expectedContext &&
                    JsonSerializer.Serialize(before, ModelJson.Options) == frozen,
                    "Native queued attack statistics successor differs at stage " + stage.GetProperty("Index").GetInt32());
            }
            Match(); Parallel.For(0, 32, _ => Match());
            if (queued) pyre++; else ordinary++;
        }
        Require(pyre == 8 && ordinary == 2, "Missing attack statistics control stages.");
        Console.WriteLine("QUEUED-ATTACK-STATISTICS PASS: eight queued Pyre and two ordinary retaliation stages, full units/context and 32 immutable branches each.");
    }
}
