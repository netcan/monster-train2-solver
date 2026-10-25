using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class GuaranteedStatusTestRngChecks
{
    private const string Reason = "Champion first-turn combat changes enchantment test RNG.";
    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, (historical, actual) =>
        {
            var json = JsonSerializer.SerializeToNode(historical, ModelJson.Options)!;
            json["Spawn"]!["Train"]!["Context"]!["Enchantments"]!["TestRng"] =
                JsonSerializer.SerializeToNode(actual.Spawn.Train.Context!.Enchantments!.TestRng, ModelJson.Options);
            Require(NativeBranchObservationChecks.Key(json.Deserialize<BattleTurnState>(ModelJson.Options)!) ==
                NativeBranchObservationChecks.Key(actual), "Historical difference exceeded the single captured test RNG stream.");
        });
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var context = before.Spawn.Train.Context!;
        var champion = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2).Units.Single(unit => unit.Id == 2);
        var effect = champion.Triggers.Single(trigger => trigger.Kind == "OnAttacking").Effects.Single();
        Require(rows.Length == 6 && last.GetProperty("IsEndTurn").GetBoolean() &&
            before.Spawn.Turn == 0 && after.Spawn.Turn == 1 && context.IsolatedBattlePreview != true &&
            champion.AssetKey == "MonsterStygianChampionTethys" &&
            effect.Type == "CardEffectAddStatusEffect" && effect.Action is { Value: 100, Range: null, Target: "LastAttackedCharacter" } &&
            effect.Action.Statuses.Single() is { Id: "poison", Stacks: 15 } &&
            context.Enchantments is { AutomaticLifecycle: true } world &&
            after.Spawn.Train.Context!.Enchantments!.TestRng.Equals(world.TestRng.Range(0, 100).State) &&
            context.BattleRng.Equals(after.Spawn.Train.Context.BattleRng),
            "Missing the single guaranteed preview chance draw with unchanged live Battle RNG.");
        Console.WriteLine("GUARANTEED-STATUS-TEST-RNG PASS:6 original native inputs/full successors/forecasts/RNG; Tethys poison100 advances captured preview test stream once; live Battle unchanged; immutable parents.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        NativeBranchObservationChecks.Pair(source, replay, originalPredictionPresent: true);
        Console.WriteLine("GUARANTEED-STATUS-TEST-RNG-PAIR PASS:original wrong stream and independent repaired forecast retained.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
