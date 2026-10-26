using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TroopPreviewAdditionChecks
{
    internal const string Reason = "Battle preview floor 1: Combined troop upgrades and enchantment source ordering require native calibration.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 18 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing complete eighteen-input attachment prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 3 && rows[^1].GetProperty("Action").Deserialize<PlayCardAction>() is
            { CardInstanceId: 11, RoomIndex: 1 } && actual.Spawn.Train.Context!.Enchantments!.EnchanterIds.SequenceEqual([13, 16]),
            "Lost attachment choice and mixed player Conduit/enemy Haste owners.");
        var updates = rows[^1].GetProperty("TroopUpdates").EnumerateArray().ToArray();
        var writes = updates.Where(update => update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 17)).ToArray();
        Require(writes.Length == 2, "Missing both uncached native preview writes.");
        for (int index = 0; index < writes.Length; index++)
        {
            var left = writes[index].GetProperty("Before"); var right = writes[index].GetProperty("After");
            var first = left.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 17);
            var last = right.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 17);
            Require(left.GetProperty("Room").GetInt32() == 1 && right.GetProperty("Preview").GetBoolean() &&
                left.GetProperty("Cache").GetArrayLength() == 0 && right.GetProperty("Cache").GetArrayLength() == 0 &&
                first.GetProperty("Attack").GetInt32() == 3 + index * 2 && last.GetProperty("Attack").GetInt32() == 5 + index * 2 &&
                first.GetProperty("Trample").GetInt32() == index && last.GetProperty("Trample").GetInt32() == index + 1 &&
                last.GetProperty("Horde").GetInt32() == 1 && last.GetProperty("Upgrades").GetArrayLength() == 2 + index,
                "Native preview did not add one upgrade/status while preserving the primary target cache.");
        }
        var birth = actual.Spawn.Train.Context!.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == 17);
        Require(birth.Unit.DeathState is { IsDestroyed: true, HasStatisticsListener: false } && birth.Unit.Health == 4 &&
            birth.Unit.Modifiers!.Upgrades.Count == 0 && birth.Unit.Statuses.Count == 0 &&
            actual.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == 6).Modifiers!.TroopAura!.Targets.Count == 0,
            "Preview leaked its numeric modifiers, cache or temporary actor into primary combat.");
        Controls(actual, birth.Unit);
        BattleTurnState current = source.GetProperty("Request").GetProperty("Root").Deserialize<BattleTurnState>()!;
        foreach (var row in rows)
        {
            current = BattlePlannerChecks.Replay(current, [new BattleSearchStep(row.GetProperty("Action").Deserialize<PlayCardAction>(),
                row.GetProperty("IsEndTurn").GetBoolean())], out var outcome);
            Require(outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(current) ==
                NativeBranchObservationChecks.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!),
                "Continuously owned native prefix differs.");
        }
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var played = BattleActionModel.PlayCard(before, rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!);
            Require(played.Supported && NativeBranchObservationChecks.Key(played.State!) == NativeBranchObservationChecks.Key(actual) &&
                immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Preview changed its parent, full successor or RNG.");
        });
        if (replay.HasValue)
        {
            Native(replay.Value);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("TROOP-PREVIEW-ADDITION PASS:18 native inputs, continuous owned state/RNG, native3/5/7 attack and0/1/2 Trample, " +
            "unchanged primary cache and restored preview birth, four immutable branches; independentPair=" + replay.HasValue);
    }

    private static void Controls(BattleTurnState actual, CombatUnit retiredBirth)
    {
        var context = actual.Spawn.Train.Context!;
        var frame = context.Enchantments!.Frame(context);
        var node = JsonSerializer.SerializeToNode(retiredBirth, ModelJson.Options)!;
        node["DeathState"]!["IsDestroyed"] = false;
        node["Statuses"] = JsonSerializer.SerializeToNode(new[] { context.StatusRules.Single(status => status.Id == "horde").WithStacks(1) }, ModelJson.Options);
        var birth = node.Deserialize<CombatUnit>(ModelJson.Options)!;
        var retained = frame.RetainedUnits.Where(actor => actor.Unit.Id != birth.Id).ToArray();
        foreach (string control in new[] { "ObservedAddition", "PrimaryWrite", "Departure", "UnknownUpgrade", "MissingPlayerAura", "PrimaryBirth" })
        {
            CombatContext scoped = frame.Train.Context!;
            if (control == "PrimaryBirth")
                scoped = scoped.WithSpawnPoints(scoped.SpawnPoints!.WithState(scoped.SpawnPoints.Groups,
                    scoped.SpawnPoints.Units.Select(point => point.UnitId == birth.Id ?
                        new UnitSpawnPointState(point.UnitId, point.Current, point.LastKnown, point.OuterBoss, false) : point).ToArray()));
            var rooms = frame.Train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units.Select(unit =>
                {
                    if (unit.Id != 6) return unit;
                    var rule = unit.Modifiers!.TroopAura!;
                    if (control == "Departure") rule = rule.WithTargets([6]);
                    if (control == "UnknownUpgrade") rule = new TroopAuraState(rule.Subtype,
                        rule.Upgrade.WithRuntimeIdentity("unknown"), rule.Statuses, rule.Targets);
                    return unit.WithModifiers(unit.Modifiers.WithTroopAura(rule));
                }).Concat(room.RoomIndex == 1 ? new[] { birth } : Array.Empty<CombatUnit>()).ToArray(),
                room.ExternalInteractions, scoped, preview: control != "PrimaryWrite", room.ActiveCompanionBossIds)).ToArray();
            var changed = new EnchantmentCombatState(new TrainCombatState(rooms, frame.Train.Movement, frame.Train.EnemySlotsPerRoom, scoped),
                retained, control == "MissingPlayerAura" ? [16] : frame.EnchanterIds, frame.AllowUpdates, false,
                control != "PrimaryWrite", frame.TestRng);
            var result = TroopAuraModel.UpdateAll(changed, [6]);
            if (control == "ObservedAddition") Require(result.Supported && result.State!.Train.Rooms[1].Units.Single(unit => unit.Id == birth.Id)
                .Modifiers!.Upgrades.Count(upgrade => upgrade.DataId == "troop-aura:6") == 1,
                "Observed preview addition did not reach the actual upgrade write.");
            else Require(!result.Supported && result.UnsupportedReason == TroopEnchantmentNoOpChecks.Reason,
                "Uncalibrated troop write escaped its ordering guard: " + control);
        }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
