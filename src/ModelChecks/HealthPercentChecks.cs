using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HealthPercentChecks
{
    private static readonly int[] Maximums = [1, 3, 17, 99999];
    private static readonly int[] Percents = [-100, 0, 1, 33, 50, 100, 101, 200, int.MinValue, int.MaxValue];
    private const string OriginalId = "5f14ee85-d078-439c-b64e-2fa7ce18e13f";
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    internal static void Component(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("Boundary").GetString() == "NativeSetHealthPercent" &&
            root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("NativeCompleted").GetBoolean() && root.GetProperty("PrimaryRestored").GetBoolean() &&
            root.GetProperty("GameplayRngRestored").GetBoolean(), "Incomplete native raw percentage health evidence.");
        var rows = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(rows.Length == 320 && rows.Select(row => (row.GetProperty("Maximum").GetInt32(), row.GetProperty("Percent").GetInt32(),
            row.GetProperty("StatusCase").GetInt32(), row.GetProperty("Wounded").GetBoolean())).Distinct().Count() == 320,
            "Incomplete percentage health matrix.");
        int increased = 0, decreased = 0, unchanged = 0, dead = 0, capped = 0, rooms = 0, retainedBoundaries = 0;
        foreach (var row in rows)
        {
            int maximum = row.GetProperty("Maximum").GetInt32(), percent = row.GetProperty("Percent").GetInt32(), statusCase = row.GetProperty("StatusCase").GetInt32();
            bool wounded = row.GetProperty("Wounded").GetBoolean();
            var before = row.GetProperty("Before").Deserialize<CombatUnit>()!;
            var actual = row.GetProperty("Actual").Deserialize<CombatUnit>()!;
            var room = row.GetProperty("RoomBefore").Deserialize<RoomCombatState>()!;
            var roomActual = row.GetProperty("RoomAfter").Deserialize<RoomCombatState>()!;
            string label = $"max={maximum}, percent={percent}, statuses={statusCase}, wounded={wounded}";
            Require(Maximums.Contains(maximum) && Percents.Contains(percent) && statusCase is >= 0 and <= 3 &&
                before.MaxHealth == maximum && before.Health == (wounded ? maximum / 2 : maximum) &&
                (before.Status("horde")?.Stacks ?? 0) == ((statusCase & 1) != 0 ? 2 : 0) &&
                (before.Status("undying")?.Stacks ?? 0) == ((statusCase & 2) != 0 ? 1 : 0) &&
                before.Status("armor")?.Stacks == 7 && before.Status("heal immunity")?.Stacks == 1 &&
                new[] { "OnHit", "OnHeal", "OnDeathwish", "OnDeathwishLost", "OnDeath", "OnReanimated" }
                    .All(kind => before.Triggers.Any(trigger => trigger.Kind == kind)), "Invalid percentage control setup: " + label);
            Require(row.GetProperty("NativeReturned").GetBoolean() && row.GetProperty("RangeIgnored").GetBoolean() &&
                row.GetProperty("QueueEmpty").GetBoolean() && row.GetProperty("HpNotifications").GetInt32() == 0 &&
                Json(row.GetProperty("RngBefore").ToObjectGraph()) == Json(row.GetProperty("RngAfter").ToObjectGraph()),
                "Raw percentage setter yielded, notified, queued or consumed RNG: " + label);
            string frozen = Json(before), frozenRoom = Json(room), expected = Json(actual), expectedRoom = Json(roomActual);
            void Match()
            {
                var unit = UnitHealthPercentModel.Apply(before, percent);
                string? difference = ModelJson.Difference(Json(unit), expected);
                Require(difference == null, "Native percentage unit differs: " + label + ": " + difference);
                var result = UnitHealthPercentModel.Apply(room, before.Id, percent);
                if (before.Health <= 0 || actual.Health <= 0)
                {
                    Require(!result.Supported && Json(before) == frozen && Json(room) == frozenRoom,
                        "Missing retained actor was silently accepted or parent changed: " + label);
                    return;
                }
                Require(result.Supported, result.UnsupportedReason ?? "Percentage room unsupported.");
                difference = ModelJson.Difference(Json(result.State), expectedRoom);
                Require(difference == null && result.Events.Count == 0 && Json(before) == frozen && Json(room) == frozenRoom,
                    "Native percentage room differs or parent changed: " + label + ": " + difference);
            }
            Match(); Parallel.For(0, 16, _ => Match());
            if (before.Health > 0 && actual.Health > 0) rooms++; else retainedBoundaries++;
            if (actual.Health > before.Health) increased++; else if (actual.Health < before.Health) decreased++; else unchanged++;
            if (actual.Health <= 0) dead++; if (actual.Health == 99999) capped++;
        }
        Require(increased > 0 && decreased > 0 && unchanged > 0 && dead > 0 && capped > 0, "Missing signed/capped/dead setter controls.");
        Console.WriteLine($"HEALTH-PERCENT PASS: 320 native raw unit successors, {rooms} full room/context successors, {retainedBoundaries} explicit retained-actor boundaries, 16 immutable branches each; increase={increased}, decrease={decreased}, unchanged={unchanged}, nonpositive={dead}, capped={capped}; no damage/heal/death callbacks, HP events or RNG.");
    }

    internal static void Battle(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("OriginalArbitraryVerdictPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete original Arbitrary Verdict battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        int[] ids = root.GetProperty("OriginalArbitraryVerdictIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 7 && actions.Length == 7, "Missing original percentage spell plays.");
        int raised = 0, lowered = 0, same = 0, enemyTargets = 0;
        for (int index = 4; index < 7; index++)
        {
            var before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            var card = before.Spawn.Train.Context!.FindCard(ids[index])!;
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == OriginalId);
            Require(action.CardInstanceId == ids[index] && before.UiRngIsolated && card.DataId == OriginalId && rule.AssetKey == "ArbitraryVerdict" &&
                rule.Cost == 2 && before.Energy - after.Energy == 2 && rule.ExternalInteractions.Count == 0 && rule.OnCastEffects.Count == 0 &&
                rule.Effects.Count == 1 && rule.Effects[0].Type == UnitHealthPercentModel.Effect && rule.Effects[0].Target == "Room" &&
                rule.Effects[0].Value == 50 && rule.Effects[0].Range == null && rule.Effects[0].AllowPlayer && rule.Effects[0].AllowEnemy &&
                card.MaskDescriptor!.SourceTraits!.Any(trait => trait.Type == "CardTraitExhaustState") &&
                after.Spawn.Train.Context!.OtherPiles!.Any(pile => pile.Name == "Exhausted" && pile.Cards.Any(token => token.InstanceId == ids[index])),
                "Original spell identity, cost, targets, percentage or exhaustion differs.");
            foreach (var room in before.Spawn.Train.Rooms)
            foreach (var unit in room.Units)
            {
                var current = after.Spawn.Train.Rooms.Single(candidate => candidate.RoomIndex == room.RoomIndex).Units.Single(candidate => candidate.Id == unit.Id);
                var expected = room.RoomIndex == action.RoomIndex && unit.Health > 0 ? UnitHealthPercentModel.Apply(unit, 50) : unit;
                Require(Json(current) == Json(expected), "Original percentage room/team/non-target unit differs.");
                if (room.RoomIndex != action.RoomIndex || unit.Health <= 0) continue;
                if (current.Health > unit.Health) raised++; else if (current.Health < unit.Health) lowered++; else same++;
                if (unit.Team == CombatTeam.Enemy) enemyTargets++;
            }
        }
        Require(raised > 0 && lowered > 0 && same > 0 && enemyTargets > 0, "Missing original positive decrease, increase, repeat or enemy target.");
        RecordedBattleChecks.Native(root);
        BattleSearchChecks.Native(root);
        Console.WriteLine("ORIGINAL-ARBITRARY-VERDICT PASS: unchanged original spell, three plays reduce/repeat/restore, both teams, exact cost/exhaustion and full winning chain/16 branches; serial/parallel search and plan replay.");
    }
}
