using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SweepBossPreviewStopChecks
{
    private const string Reason = "Ice Spear post-card boss preview advances one excess BattleTest draw.";

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, (historical, actual) =>
        {
            var expectedRng = historical.Spawn.Train.Context!.Enchantments!.TestRng;
            Require(expectedRng.Equals(actual.Spawn.Train.Context!.Enchantments!.TestRng.Next()),
                "Historical preview must differ by exactly one extra test draw.");
            var node = JsonSerializer.SerializeToNode(historical, ModelJson.Options)!;
            node["Spawn"]!["Train"]!["Context"]!["Enchantments"]!["TestRng"] =
                JsonSerializer.SerializeToNode(actual.Spawn.Train.Context.Enchantments.TestRng, ModelJson.Options);
            Require(NativeBranchObservationChecks.Key(node.Deserialize<BattleTurnState>(ModelJson.Options)!) ==
                NativeBranchObservationChecks.Key(actual), "Historical mismatch exceeded the preview test stream.");
        });
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var context = before.Spawn.Train.Context!;
        var room = actual.Spawn.Train.Rooms[0];
        var champion = room.Units.Single(unit => unit.Id == 2);
        Require(rows.Length == 25 && !last.GetProperty("IsEndTurn").GetBoolean() &&
            before.Spawn.Turn == 4 && actual.Spawn.Turn == 4 &&
            action.CardInstanceId == 18 && action.RoomIndex == 0 &&
            context.FindCard(18)!.DataId == "5839cfaa-4784-4ec7-99f0-3dda42b939dc" &&
            before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 15).Health == 180 &&
            room.Units.Single(unit => unit.Id == 15) is { AssetKey: "Boss_Level2_BigStats", Health: 160 } &&
            room.Units.Single(unit => unit.Id == 16) is { AssetKey: "AttackerT1_Spearman", Health: 2 } &&
            champion is { AssetKey: "MonsterStygianChampionTethys", Health: 10, Attack: 10 } &&
            champion.Status("sweep")?.Stacks == 1 &&
            champion.Triggers.Single(trigger => trigger.Kind == "OnAttacking").Effects.Single().Action is
                { Target: "LastAttackedCharacter", Value: 100, Range: null } &&
            context.IsolatedBattlePreview != true && context.Enchantments!.AutomaticLifecycle &&
            context.BattleRng.Equals(actual.Spawn.Train.Context!.BattleRng),
            "Missing native Ice Spear/Tethys sweep lethal-preview signature or live RNG preservation.");
        UnityRng rng = context.Enchantments!.TestRng;
        var draws = source.GetProperty("BattleTestRng").EnumerateArray().ToArray();
        Require(draws.Length == 71, "Missing passive native preview calls across the complete prefix.");
        foreach (var draw in draws.TakeLast(6))
        {
            Require(draw.GetProperty("Preview").GetBoolean() && draw.GetProperty("Min").GetInt32() == 0 &&
                draw.GetProperty("Max").GetInt32() == 100 &&
                draw.GetProperty("Before").Deserialize<UnityRng>().Equals(rng), "Native preview draw order differs.");
            rng = rng.Range(0, 100).State;
            Require(draw.GetProperty("After").Deserialize<UnityRng>().Equals(rng), "Native preview draw transition differs.");
        }
        Require(rng.Equals(actual.Spawn.Train.Context!.Enchantments!.TestRng),
            "Lethal preview executed an attack status effect after the boss-removal cancellation.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual) &&
                parent == JsonSerializer.Serialize(before, ModelJson.Options), "Sweep preview mutated a parent or changed state/RNG.");
        });
        if (replay.HasValue)
        {
            Native(replay.Value);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("SWEEP-BOSS-PREVIEW-STOP PASS:25 native inputs/full successors/forecasts/RNG; six final preview draws, " +
            "cancelled queued lethal sweep effect; live RNG unchanged; four immutable branches; independentPair=" + replay.HasValue);
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
