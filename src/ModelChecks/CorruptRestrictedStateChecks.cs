using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CorruptRestrictedStateChecks
{
    private const string Reason = "Corrupt State's additional eligibility and pre-play ordering require native calibration.";
    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CardInstanceState card = before.Spawn.Train.Context!.FindCard(10)!;
        Require(card.DataId == "ae114dc4-d08f-4909-a2ad-d99215661463" &&
            card.MaskDescriptor!.BaseTraits.Select(trait => trait.RuntimeType).SequenceEqual([
                CardCorruptRestrictedModel.Trait, CardCorruptStateModel.Trait]), "Wrong original merchant Corrupt card.");
        if (rows.Length == 11) Require(
            before.Spawn.Train.Context.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 3 &&
            after.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 5 &&
            before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == 7).Health == 3 &&
            !after.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == 7),
            "Missing native merchant Corrupt/restriction1, pre-play add/subtract and on-kill refill.");
        else
        {
            var upgrade = card.Temporary.Upgrades.Single(item => item.DataId == "2d744be5-cf16-4126-83fa-d6df9d99229c");
            Require(rows.Length == 62 && upgrade.AssetKey == "WurmReturnEnhanceCorrupt" &&
                upgrade.Lifecycle!.AddedTraits.Select(trait => trait.RuntimeType).SequenceEqual([
                    CardCorruptStateModel.Trait, "CardTraitExhaustState"]) &&
                CardExhaustModel.RefreshTraits(card).CombinedTraits!.Count(trait => trait.RuntimeType == CardCorruptStateModel.Trait) == 1 &&
                CardExhaustModel.HasConsume(card) &&
                before.Spawn.Train.Context.RoomCapacities!.Single(room => room.RoomIndex == 1).Corruption == 8 &&
                after.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 1).Corruption == 8 &&
                before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Single(unit => unit.Id == 28).Health == 30 &&
                after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Single(unit => unit.Id == 28).Health == 17 &&
                after.Spawn.Train.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == 10),
                "Returned Corrupt must deduplicate its trait, keep Consume, match damage and exhaust ownership.");
        }
        bool queriesPresent = fixture.TryGetProperty("CorruptRestrictedState", out FixtureValue evidence) && evidence.ValueKind != FixtureKind.Null;
        Require(queriesPresent || last.GetProperty("Expected").ValueKind == FixtureKind.Null,
            "Repaired native run must include independently restored eligibility queries.");
        if (queriesPresent)
        {
            var queries = evidence.GetProperty("Queries").EnumerateArray().ToArray();
            Require(evidence.GetProperty("QueriesUnchanged").GetBoolean() && queries.Length == 4 &&
                queries.Select(query => query.GetProperty("Corruption").GetInt32()).SequenceEqual([0, 1, 4, int.MaxValue]),
                "Native corruption query controls are missing or changed their source.");
            foreach (var query in queries)
            {
                BattleTurnState state = query.GetProperty("Before").Deserialize<BattleTurnState>()!;
                CardInstanceState queried = state.Spawn.Train.Context!.FindCard(10)!;
                string? error = CardCorruptRestrictedModel.Test(state.Spawn.Train.Context, queried,
                    query.GetProperty("RoomIndex").GetInt32(), out bool allowed, query.GetProperty("SourceRoomIndex").GetInt32());
                bool expected = query.GetProperty("Corruption").GetInt32() != int.MaxValue;
                Require(error == null && allowed == expected &&
                    query.GetProperty("FromHand").GetBoolean() == expected && query.GetProperty("FromPlay").GetBoolean() == expected &&
                    query.GetProperty("HandError").GetString() == (expected ? "None" : "InsufficientCorruption") &&
                    query.GetProperty("PlayError").GetString() == (expected ? "None" : "InsufficientCorruption"),
                    "Native/model Corrupt presence credit or integer overflow differs.");
            }
        }
        Console.WriteLine($"CORRUPT-RESTRICTED-STATE PASS:{rows.Length} native inputs/full successors/RNG; merchant/returned Corrupt deduplication, Consume and restored eligibility controls; immutable parents.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        Require(replay.TryGetProperty("CorruptRestrictedState", out var queries) && queries.ValueKind != FixtureKind.Null,
            "Independent repaired process omitted native eligibility controls.");
        NativeBranchObservationChecks.Pair(source, replay);
        Console.WriteLine("CORRUPT-RESTRICTED-STATE-PAIR PASS:original rejection and repaired native forecast, four restored native eligibility controls.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
