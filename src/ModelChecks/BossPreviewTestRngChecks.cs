using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BossPreviewTestRngChecks
{
    private const string Reason = "Real combat advances BattleTest outside the guaranteed chance application.";
    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, (historical, actual) =>
        {
            var json = JsonSerializer.SerializeToNode(historical, ModelJson.Options)!;
            json["Spawn"]!["Train"]!["Context"]!["Enchantments"]!["TestRng"] =
                JsonSerializer.SerializeToNode(actual.Spawn.Train.Context!.Enchantments!.TestRng, ModelJson.Options);
            Require(NativeBranchObservationChecks.Key(json.Deserialize<BattleTurnState>(ModelJson.Options)!) ==
                NativeBranchObservationChecks.Key(actual), "Historical mismatch exceeded the captured preview test RNG.");
        });
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var context = before.Spawn.Train.Context!;
        var champion = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2).Units.Single(unit => unit.Id == 2);
        Require(rows.Length == 13 && last.GetProperty("IsEndTurn").GetBoolean() &&
            before.Spawn.Turn == 1 && after.Spawn.Turn == 2 && context.IsolatedBattlePreview != true &&
            context.Enchantments is { AutomaticLifecycle: true } world &&
            champion.AssetKey == "MonsterStygianChampionTethys" &&
            champion.Triggers.Single(trigger => trigger.Kind == "OnAttacking").Effects.Single().Action is
                { Value: 100, Range: null, Target: "LastAttackedCharacter" } &&
            after.Spawn.Train.Context!.Enchantments!.TestRng.Equals(world.TestRng.Range(0, 100).State) &&
            after.Spawn.Train.Context.BattleRng.Equals(context.BattleRng.Range(0, 100).State),
            "Missing the independent boss-preview test draw and real-combat Battle draw.");
        var draws = fixture.GetProperty("BattleTestRng").EnumerateArray().ToArray();
        Require(draws.Length == 8 && draws.All(draw => draw.GetProperty("Preview").GetBoolean() &&
            draw.GetProperty("Min").GetInt32() == 0 && draw.GetProperty("Max").GetInt32() == 100 &&
            draw.GetProperty("After").Deserialize<UnityRng>().Equals(
                draw.GetProperty("Before").Deserialize<UnityRng>().Range(0, 100).State)) &&
            draws[^1].GetProperty("Before").Deserialize<UnityRng>().Equals(context.Enchantments!.TestRng) &&
            draws[^1].GetProperty("After").Deserialize<UnityRng>().Equals(after.Spawn.Train.Context.Enchantments!.TestRng),
            "Missing passive native chance-call provenance.");
        Console.WriteLine("BOSS-PREVIEW-TEST-RNG PASS:13 native inputs/full successors/forecasts/RNG; passive8 preview draws; boss preview retains test RNG; real Battle advances independently; immutable parents.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        NativeBranchObservationChecks.Pair(source, replay, originalPredictionPresent: true);
        Console.WriteLine("BOSS-PREVIEW-TEST-RNG-PAIR PASS:original mismatching forecast and independent repaired forecast preserved.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
