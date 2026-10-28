using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FightClubDyingPreviewChecks
{
    private const string Reason = "Train combat floor 2: Boss kill preview: Combined troop upgrades and enchantment source ordering require native calibration.";
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 24 && rows[^1].GetProperty("IsEndTurn").GetBoolean() &&
            rows[^1].GetProperty("TroopUpdates").GetArrayLength() == 0, "Missing native chain or unexpected internal write capture.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var context = before.Spawn.Train.Context!;
        var frame = context.Enchantments!.Frame(context);
        var dying = frame.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 15)
            .WithHealth(0).WithDeathState(new UnitDeathState(true, true, false, isDespawned: false, isDestroyed: false));
        var rooms = frame.Train.Rooms.Select(room => room.WithUnits(room.Units.Where(unit => unit.Id != 15).ToArray())).ToArray();
        var train = new TrainCombatState(rooms, frame.Train.Movement, frame.Train.EnemySlotsPerRoom, context);
        EnchantmentCombatState Pending(int room, bool actorPreview, CombatUnit unit) => new(train,
            frame.RetainedUnits.Append(new EnchantmentRetainedUnit(unit, room, actorPreview)).ToArray(), frame.EnchanterIds,
            frame.AllowUpdates, frame.Updating, true, frame.TestRng, frame.TemporaryPreviewUnitIds);
        Require(!TroopAuraModel.OnlyFragileBuffSource(Pending(2, true, dying)) &&
            TroopAuraModel.OnlyFragileBuffSource(Pending(2, true, dying), pendingDyingRoom: 2, pendingDyingPreview: true) &&
            !TroopAuraModel.OnlyFragileBuffSource(Pending(1, true, dying), pendingDyingRoom: 2, pendingDyingPreview: true) &&
            !TroopAuraModel.OnlyFragileBuffSource(Pending(2, false, dying), pendingDyingRoom: 2, pendingDyingPreview: true) &&
            !TroopAuraModel.OnlyFragileBuffSource(Pending(2, true, dying.WithHealth(1)), pendingDyingRoom: 2, pendingDyingPreview: true) &&
            !TroopAuraModel.OnlyFragileBuffSource(Pending(2, true, dying.WithDeathState(new UnitDeathState(false, true, false, isDespawned: false, isDestroyed: false))), pendingDyingRoom: 2, pendingDyingPreview: true) &&
            !TroopAuraModel.OnlyFragileBuffSource(Pending(2, true, dying.WithDeathState(new UnitDeathState(true, false, false, isDespawned: false, isDestroyed: false))), pendingDyingRoom: 2, pendingDyingPreview: true) &&
            !TroopAuraModel.OnlyFragileBuffSource(Pending(2, true, dying.WithDeathState(new UnitDeathState(true, true, false, isDespawned: true, isDestroyed: false))), pendingDyingRoom: 2, pendingDyingPreview: true) &&
            !TroopAuraModel.OnlyFragileBuffSource(Pending(2, true, dying.WithDeathState(new UnitDeathState(true, true, false, isDespawned: false, isDestroyed: true))), pendingDyingRoom: 2, pendingDyingPreview: true),
            "Pending preview death escaped its room, lifetime, health or preview guard.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Concurrent successor differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin", "ExilesDiadem_Upgrade", "FightClub", "MonsterSycophant", "MonsterGenerousHost", "MonsterSneezyWhelp" })
        {
            JsonNode altered = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            void Rewrite(JsonNode? node)
            {
                if (node is JsonObject obj)
                {
                    if (obj["AssetKey"]?.GetValueKind() == JsonValueKind.String && obj["AssetKey"]!.GetValue<string>() == control)
                        obj["AssetKey"] = "unknown";
                    foreach (var entry in obj.ToArray()) Rewrite(entry.Value);
                }
                else if (node is JsonArray array) foreach (var child in array) Rewrite(child);
            }
            Rewrite(altered);
            var result = BattleTurnModel.EndTurn(altered.Deserialize<BattleTurnState>(ModelJson.Options)!);
            Require(!result.Supported || NativeBranchObservationChecks.Key(result.State!) != NativeBranchObservationChecks.Key(actual),
                "Unknown source incorrectly received native result: " + control);
        }
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("FIGHT-CLUB-DYING-PREVIEW PASS: 24 native inputs/full state/RNG, internal writes not captured, four immutable branches and lifecycle/source controls; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
