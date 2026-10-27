using System.Text.Json;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TroopAuraBirthWorldChecks
{
    internal const string Reason = "Battle preview floor 2: Triggered enemy birth requires its original single-spawn effect and complete lifecycle world.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 6 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Incomplete original troop birth prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 0 && actual.Spawn.Turn == 1 && before.Spawn.Train.Context?.Enchantments is
            { AutomaticLifecycle: true, EnchanterIds.Count: 0 } &&
            before.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Modifiers?.TroopAura != null) &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.AssetKey == "Boss_Level3_Fleshweaver_Corruption"),
            "Missing original troop aura and enemy summoner preview.");
        string parent = NativeBranchObservationChecks.Key(before);
        Parallel.For(0, 4, _ => {
            var ended = BattleTurnModel.EndTurn(before);
            Require(ended.Supported && NativeBranchObservationChecks.Key(ended.State!) == NativeBranchObservationChecks.Key(actual),
                "Troop write lost its automatic lifecycle world.");
        });
        Require(parent == NativeBranchObservationChecks.Key(before), "Troop birth-world branch mutated its parent.");
        var room = actual.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.AssetKey == "Boss_Level3_Fleshweaver_Corruption"));
        var rule = room.Units.Single(unit => unit.AssetKey == "Boss_Level3_Fleshweaver_Corruption").Triggers
            .SelectMany(trigger => trigger.Effects).Single(effect => effect.Summon != null).Summon!;
        foreach (string control in new[] { "World", "Automatic", "Count", "Pool" })
        {
            var node = JsonSerializer.SerializeToNode(room, ModelJson.Options)!;
            var changedRule = JsonSerializer.SerializeToNode(rule, ModelJson.Options)!;
            if (control == "World") node["Context"]!["Enchantments"] = null;
            if (control == "Automatic") node["Context"]!["Enchantments"]!["AutomaticLifecycle"] = false;
            if (control == "Count") changedRule["Count"] = 2;
            if (control == "Pool") changedRule["Pool"]!.AsArray().Add(rule.CharacterId);
            Require(TriggeredSummonModel.Validate(node.Deserialize<RoomCombatState>(ModelJson.Options)!,
                changedRule.Deserialize<TriggeredSummonRule>(ModelJson.Options)!) != null, "Enemy birth guard lost: " + control);
        }
        if (replay.HasValue) { NativeBranchObservationChecks.Replay(replay.Value, Reason); NativeBranchObservationChecks.Pair(source, replay.Value); }
        Console.WriteLine("TROOP-BIRTH-WORLD PASS:six native inputs/full state/forecast/RNG; four immutable branches/four guard controls; independentPair=" + replay.HasValue);
    }
    internal static void ExportRequest(string sourcePath, string output)
    {
        using var source = ModelJson.ReadFixture(sourcePath);
        var root = source.RootElement.GetProperty("Rows")[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var current = root;
        var prefix = new List<BattleSearchStep>();
        for (int index = 0; index < 64; index++)
        {
            var chosen = BattleHeuristicPolicy.Choose(current, out _, 2);
            var step = new BattleSearchStep(chosen?.Action, chosen == null);
            if (step.IsEndTurn)
            {
                var ended = BattleTurnModel.EndTurn(current);
                if (!ended.Supported)
                {
                    Require(ended.UnsupportedReason == Reason && prefix.Count == 5 && current.Spawn.Turn == 0, "Wrong troop birth-world boundary: " + ended.UnsupportedReason);
                    using var document = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "ModelUnsupportedBranch",
                        SourceArchive = Path.GetFileName(sourcePath), Root = root, Prefix = prefix, FailedRoot = current,
                        Action = (PlayCardAction?)null, IsEndTurn = true, Reason, NativeValidationRequired = true });
                    using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write);
                    document.Write(stream);
                    Console.WriteLine("TROOP-BIRTH-WORLD-REQUEST prefix5 preserved; native validation required.");
                    return;
                }
            }
            current = BattlePlannerChecks.Replay(current, [step], out var outcome);
            Require(outcome == RoomOutcome.Exchanged, "Policy ended before the original troop birth-world boundary.");
            prefix.Add(step);
        }
        throw new InvalidOperationException("Original troop birth-world refusal was not found.");
    }
    private static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
}
