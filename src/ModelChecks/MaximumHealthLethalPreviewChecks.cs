using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MaximumHealthLethalPreviewChecks
{
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Same(TrainCombatState expected, TrainCombatState actual, string stage)
    {
        string? difference = ModelJson.Difference(Json(expected), Json(actual));
        Require(difference == null, stage + ": " + difference);
    }
    private static bool GameplayRng(FixtureValue before, FixtureValue after)
    {
        var left = before.GetProperty("Streams").EnumerateArray().ToArray();
        var right = after.GetProperty("Streams").EnumerateArray().ToArray();
        return left.Length == 16 && right.Length == 16 && before.GetProperty("Unity").GetArrayLength() == 4 &&
            before.GetProperty("Unity").ContentEquals(after.GetProperty("Unity")) && left.Zip(right).All(pair =>
                pair.First.GetProperty("Id").GetString() == pair.Second.GetProperty("Id").GetString() &&
                (pair.First.GetProperty("Id").GetString() is "NonDeterministic" or "Chatter" || pair.First.ContentEquals(pair.Second)));
    }
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == "NativeMaximumHealthLethalCardHover" &&
            root.GetProperty("NativeCompleted").GetBoolean() && root.GetProperty("AuthoredControls").GetBoolean() &&
            root.GetProperty("OriginalSuppression").GetInt32() == root.GetProperty("FinalSuppression").GetInt32(),
            "Incomplete native lethal maximum-health hover evidence.");
        var rows = root.GetProperty("Samples").EnumerateArray().ToArray();
        string teamName = root.TryGetProperty("TargetTeam", out var team) ? team.GetString()! : "Player";
        Require(teamName is "Player" or "Enemy", "Unknown native lethal-hover target team.");
        CombatTeam targetTeam = teamName == "Enemy" ? CombatTeam.Enemy : CombatTeam.Player;
        Require(rows.Length == 16 && rows.Select(row => (row.GetProperty("Horde").GetBoolean(), row.GetProperty("Undying").GetBoolean(),
            row.GetProperty("ActiveHalfHealth").GetBoolean(), row.GetProperty("ZeroMaximum").GetBoolean())).Distinct().Count() == 16,
            "Missing Horde/Undying/half-health/zero-maximum lethal hover controls.");
        foreach (var row in rows)
        {
            string label = row.GetProperty("Label").GetString()!;
            int targetId = row.GetProperty("TargetId").GetInt32();
            var before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var native = row.GetProperty("CopiedActual").Deserialize<TrainCombatState>()!;
            var restored = row.GetProperty("After").Deserialize<TrainCombatState>()!;
            var rule = row.GetProperty("Rule").Deserialize<CardPlayRule>()!;
            var action = new PlayCardAction(row.GetProperty("CardId").GetInt32(), row.GetProperty("RoomIndex").GetInt32(), targetUnitId: targetId);
            var target = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == targetId);
            Require(target.Team == targetTeam, "Native lethal-hover target team differs: " + label);
            var observed = row.GetProperty("ActualTarget").Deserialize<CombatUnit>()!;
            bool undying = row.GetProperty("Undying").GetBoolean(), horde = row.GetProperty("Horde").GetBoolean();
            int expectedMax = row.GetProperty("ZeroMaximum").GetBoolean() ? undying ? 1 : 0 : target.MaxHealth - row.GetProperty("Amount").GetInt32();
            var dispatches = row.GetProperty("Dispatches").EnumerateArray().ToArray();
            string deathKind = targetTeam == CombatTeam.Enemy ? "OnAnyHeroDeathOnFloor" : "OnAnyMonsterDeathOnFloor";
            string otherDeathKind = targetTeam == CombatTeam.Enemy ? "OnAnyMonsterDeathOnFloor" : "OnAnyHeroDeathOnFloor";
            int observerCount = before.Rooms.Single(room => room.RoomIndex == action.RoomIndex).Units.Count - (undying ? 0 : 1);
            Require(dispatches.Count(dispatch => dispatch.GetProperty("Kind").GetString() == deathKind) == observerCount &&
                !dispatches.Any(dispatch => dispatch.GetProperty("Kind").GetString() == otherDeathKind) &&
                dispatches.Where(dispatch => dispatch.GetProperty("Kind").GetString() == deathKind).All(dispatch =>
                    dispatch.GetProperty("TriggerCount").GetInt32() == (targetTeam == CombatTeam.Player && horde && !undying ? 2 : 1)) &&
                dispatches.Count(dispatch => dispatch.GetProperty("Kind").GetString() == "OnAnyUnitDeathOnFloor") == observerCount,
                "Native team-specific death observers or Horde multiplicity differs: " + label);
            Require(target.Triggers.Where(HalfHealthDeathwishModel.IsTrigger).All(trigger =>
                    trigger.DeathwishActive == row.GetProperty("ActiveHalfHealth").GetBoolean()) &&
                dispatches.Count(dispatch => dispatch.GetProperty("Kind").GetString() == "OnDeath") == 1 &&
                dispatches.Count(dispatch => dispatch.GetProperty("Kind").GetString() == "OnReanimated") == (undying ? 1 : 0) &&
                dispatches.Where(dispatch => dispatch.GetProperty("Kind").GetString() == "OnAnyUnitDeathOnFloor")
                    .All(dispatch => dispatch.GetProperty("TriggerCount").GetInt32() == (horde && !undying ? 2 : 1)) &&
                (undying || !dispatches.Any(dispatch => dispatch.GetProperty("Kind").GetString() is "OnDeathwish" or "OnDeathwishLost")),
                "Native lethal shortcut, revival or Horde observer multiplicity differs: " + label);
            Require(row.GetProperty("NativeReturned").GetBoolean() && row.GetProperty("QueueEmpty").GetBoolean() &&
                row.GetProperty("TemporaryDisabled").GetBoolean() && row.GetProperty("EnableCount").GetInt32() == 1 &&
                row.GetProperty("DisableCount").GetInt32() == 1 && observed.Health == (undying ? 1 : 0) &&
                observed.MaxHealth == expectedMax && observed.DeathState?.IsSacrifice == !undying &&
                observed.DeathState.HasFinishedDying == !undying && (observed.Status("horde")?.Stacks ?? 0) == (horde ? 2 : 0) &&
                observed.Status("undying") == null && GameplayRng(row.GetProperty("RngBefore"), row.GetProperty("RngAfter")),
                "Native lethal hover lifecycle or gameplay RNG differs: " + label);
            string frozen = Json(before);
            var result = CardHoverPreviewModel.Apply(before, rule, action);
            Require(result.Supported, "Lethal hover unsupported: " + label + ": " + result.UnsupportedReason);
            Same(result.CopiedBefore!, row.GetProperty("CopiedBefore").Deserialize<TrainCombatState>()!, label + " copied before");
            Same(result.AfterEffects!, native, label + " after lethal effects");
            var predictedTarget = result.AfterEffects!.Rooms.SelectMany(room => room.Units)
                .Concat(result.TemporaryRemovedUnits).Single(unit => unit.Id == targetId);
            string? targetDifference = ModelJson.Difference(Json(predictedTarget), Json(observed));
            Require(targetDifference == null, label + " temporary target: " + targetDifference);
            Same(result.Restored!, restored, label + " restored primary");
            Same(result.Restored!, row.GetProperty("Cleared").Deserialize<TrainCombatState>()!, label + " cleared visuals");
            Require(result.CopiedIds.SequenceEqual(row.GetProperty("CopiedIds").EnumerateArray().Select(id => id.GetInt32())) &&
                Json(before) == frozen, "Lethal hover changed its parent or copied different actors: " + label);
            string expected = Json(result.AfterEffects!);
            string expectedRemoved = Json(result.TemporaryRemovedUnits);
            Parallel.For(0, 16, branch =>
            {
                var fork = CardHoverPreviewModel.Apply(before, rule, action);
                Require(fork.Supported && Json(fork.AfterEffects!) == expected && Json(fork.Restored!) == Json(restored) &&
                    Json(fork.TemporaryRemovedUnits) == expectedRemoved && Json(before) == frozen,
                    "Lethal hover immutable branch differs: " + label + "/" + branch);
            });
        }
        Console.WriteLine("MAXIMUM-HEALTH-LETHAL-PREVIEW PASS: 16 native " + teamName + " card hovers, ordinary/Horde/Undying/half-health and zero/positive maximum, " +
            "complete copy/effect/cancel states, gameplay RNG and 16 immutable branches per root.");
    }
}
