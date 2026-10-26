using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OuterBossAttachmentBirthChecks
{
    private const string Reason = "Outer boss birth omits the original Spell Sanctum character upgrade.";
    private const string Upgrade = "35178e4a-0880-440b-a929-de29de6e88f8";

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, (historical, actual) =>
        {
            Require(historical.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == 8).Modifiers!.Upgrades.Count == 0,
                "Historical boss birth must omit exactly the attachment upgrade.");
            var node = JsonSerializer.SerializeToNode(historical, ModelJson.Options)!;
            foreach (var rooms in new[] { node["Spawn"]!["Train"]!["Rooms"]!,
                node["Spawn"]!["Train"]!["Context"]!["Enchantments"]!["Rooms"]! })
            {
                var boss = rooms[1]!["Units"]!.AsArray().Single(unit => unit!["Id"]!.GetValue<int>() == 8)!;
                Require(boss["Modifiers"]!["Upgrades"]!.AsArray().Count == 0,
                    "Historical shared boss must omit the same attachment.");
                boss["Modifiers"]!["Upgrades"] = JsonSerializer.SerializeToNode(
                    actual.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == 8).Modifiers!.Upgrades, ModelJson.Options);
            }
            Require(NativeBranchObservationChecks.Key(node.Deserialize<BattleTurnState>(ModelJson.Options)!) ==
                NativeBranchObservationChecks.Key(actual), "Historical mismatch exceeded the missing boss attachment upgrade.");
        });
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var boss = actual.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == 8);
        var attachment = actual.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 1).Attachments!.Single();
        Guid guid = Guid.Empty;
        Require(rows.Length == 5 && rows[^1].GetProperty("IsEndTurn").GetBoolean() &&
            before.Spawn.Turn == 0 && actual.Spawn.Turn == 1 && before.Spawn.QueuedOuterBoss != null &&
            boss is { AssetKey: "Boss_Level3_Fleshweaver_Spike", Attack: 16, Health: 500 } &&
            boss.Modifiers!.Upgrades.Single() is { DataId: Upgrade, AssetKey: "SpellSanctum_Upgrade" } &&
            attachment is { DataId: Upgrade, AffectsHeroes: true, AffectsMonsters: true, IgnoreBosses: false, SpellCostDelta: -1 } &&
            Guid.TryParse(attachment.AttachmentId, out guid) && guid != Guid.Empty &&
            before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 1).Attachments!.Single().AttachmentId == attachment.AttachmentId,
            "Missing original outer-boss birth into a permanent Spell Sanctum.");
        var bindings = source.GetProperty("PlayerAdvisor").GetProperty("ReplayAttachmentBindings").EnumerateArray().ToArray();
        Require(bindings.Length == 1 && bindings[0].GetProperty("Room").GetInt32() == 1 &&
            bindings[0].GetProperty("DataId").GetString() == Upgrade &&
            bindings[0].GetProperty("ReplayGuid").GetString() == attachment.AttachmentId &&
            Guid.TryParse(bindings[0].GetProperty("NativeGuid").GetString(), out Guid native) && native != guid,
            "Missing isolated binding of the original nondeterministic constructor identity.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual) &&
                parent == JsonSerializer.Serialize(before, ModelJson.Options), "Boss attachment birth changed a parent or state/RNG.");
        });
        if (replay.HasValue)
        {
            Native(replay.Value);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("OUTER-BOSS-ATTACHMENT-BIRTH PASS:five native inputs/full successors/forecasts/RNG; native GUID binding, " +
            "Spell Sanctum boss upgrade and shared world; four immutable branches; independentPair=" + replay.HasValue);
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
