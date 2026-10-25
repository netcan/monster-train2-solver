using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PyreMaxHealthChecks
{
    private const string Effect = "RelicEffectModifyTowerMaxHealth";
    private const string Reason = "Unmodeled relic effect RelicEffectModifyTowerMaxHealth on PyreHeartFather.";
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        var rows = NativeBranchObservationChecks.Replay(replay, Reason);
        var original = source.GetProperty("Rows");
        Require(source.GetProperty("NativeCompleted").GetBoolean() &&
            source.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            source.GetProperty("Request").GetProperty("Reason").GetString() == Reason &&
            source.GetProperty("Request").GetProperty("Prefix").GetArrayLength() == 0 &&
            original.GetArrayLength() == 1 && original[0].GetProperty("Expected").ValueKind == FixtureKind.Null &&
            original[0].GetProperty("UnsupportedReason").GetString() == Reason &&
            original[0].GetProperty("IsEndTurn").GetBoolean() && original[0].GetProperty("Boundary").GetBoolean() &&
            rows.Length == 1 && rows[0].GetProperty("IsEndTurn").GetBoolean(),
            "Expected the preserved native rejection and independently repaired first end turn.");
        foreach (string field in new[] { "Before", "Actual" })
            Require(NativeBranchObservationChecks.Key(WithoutOriginalGuard(original[0].GetProperty(field).Deserialize<BattleTurnState>()!)) ==
                NativeBranchObservationChecks.Key(rows[0].GetProperty(field).Deserialize<BattleTurnState>()!),
                "Independent native state/RNG differs beyond the removed capture diagnostic: " + field);
        var before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var pyre = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre);
        var changed = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre);
        var owner = before.Spawn.Train.Context!.Relics!.Single(relic => relic.DataId == "fe768ab5-3f58-4f96-9afc-48f0f30c7b21");
        Require(owner.AssetKey == "PyreHeartFather" && owner.EffectTypes.SequenceEqual([Effect]) &&
            before.Spawn.Turn == 0 && after.Spawn.Turn == 1 && pyre.Health == 105 && pyre.MaxHealth == 105 &&
            changed.Health == 105 && changed.MaxHealth == 105 && RelicModel.Validate([owner]) == null,
            "Native acquired +25 Pyre HP/max HP was missing or applied twice in battle.");
        var unknown = new CombatRelicState(owner.DataId, owner.AssetKey, [Effect, "RelicEffectUnknownCombatCallback"]);
        Require(RelicModel.Validate([unknown])?.Contains("RelicEffectUnknownCombatCallback") == true,
            "A known acquisition effect bypassed another unknown combat callback.");
        Console.WriteLine("PYRE-MAX-HEALTH PASS: independent native first end turn, complete state/RNG and immutable parents; captured105/105 retained without reapplying acquisition; original refusal and unknown-callback guard preserved.");
    }
    private static BattleTurnState WithoutOriginalGuard(BattleTurnState state)
    {
        // The previous capture projected its global guard into all eight room
        // diagnostic lists. Keep the original archive, and remove only this
        // exact diagnostic when comparing the two physical native executions.
        var node = JsonSerializer.SerializeToNode(state, ModelJson.Options)!;
        JsonNode train = node["Spawn"]!["Train"]!;
        int removed = 0;
        foreach (JsonNode rooms in new[] { train["Rooms"]!, train["Context"]!["Enchantments"]!["Rooms"]! })
        foreach (JsonNode? room in rooms.AsArray())
        {
            var diagnostics = room!["ExternalInteractions"]!.AsArray();
            var guard = diagnostics.Single(item => item?.GetValue<string>() == Reason);
            diagnostics.Remove(guard);
            removed++;
        }
        Require(removed == 8, "Original projected room guards changed.");
        return node.Deserialize<BattleTurnState>(ModelJson.Options)!;
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
