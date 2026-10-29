using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ReverseCopyBeforeConsumeChecks
{
    private const string Reason = "Copy-on-play requires Consume to precede the copy callback, as in the original calibrated upgrade order.";

    internal static void Check(FixtureValue first, FixtureValue second, FixtureValue repaired, FixtureValue repeated, bool endTurn)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(first, Reason);
        FixtureValue[] independent = NativeBranchObservationChecks.Replay(second, Reason);
        Require(rows.Length == 27 && independent.Length == rows.Length && rows[^1].GetProperty("IsEndTurn").GetBoolean() == endTurn,
            "Expected the 26-input native prefix and requested boundary.");
        for (int index = 0; index < rows.Length; index++)
        foreach (string field in new[] { "Before", "Actual", "Action", "IsEndTurn", "Boundary" })
            Require(rows[index].GetProperty(field).ContentEquals(independent[index].GetProperty(field)), "Native inputs or states differ: " + index + "/" + field);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction? action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>();
        Require(endTurn || action?.CardInstanceId == 17, "Wrong card played at boundary.");
        var context = before.Spawn.Train.Context!;
        Require(context.Cards.Hand.Any(card => card.InstanceId == 17), "Expected original card in hand.");
        if (!endTurn)
            Require(actual.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == context.NextCardId) &&
                actual.Spawn.Train.Context.OtherPiles!.Any(pile => pile.Name == "Exhausted" && pile.Cards.Any(card => card.InstanceId == 17)),
                "Native copy and Consume piles differ.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, branch => {
            BattleTurnState result = BattlePlannerChecks.Replay(before, [new BattleSearchStep(action, endTurn)], out _);
            Require(NativeBranchObservationChecks.Key(result) == NativeBranchObservationChecks.Key(actual) &&
                immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Concurrent branch differs or mutated its input.");
        });
        foreach (string mutation in new[] { "card", "upgrade", "asset", "cost", "interaction" })
        {
            JsonNode changed = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            JsonNode target = changed["Spawn"]!["Train"]!["Context"]!["CardInstances"]!.AsArray()
                .Single(item => item!["InstanceId"]!.GetValue<int>() == 17)!;
            switch (mutation)
            {
                case "card": target["DataId"] = "unknown"; break;
                case "upgrade": target["Permanent"]!["Upgrades"]![1]!["DataId"] = "unknown"; break;
                case "asset": target["Permanent"]!["Upgrades"]![1]!["AssetKey"] = "unknown"; break;
                case "cost": target["Permanent"]!["Upgrades"]![1]!["Stats"]!["Cost"] = -3; break;
                case "interaction": target["ExternalInteractions"]!.AsArray().Add("unknown"); break;
            }
            CardInstanceState altered = changed.Deserialize<BattleTurnState>(ModelJson.Options)!.Spawn.Train.Context!.FindCard(17)!;
            Require(CardCopyOnPlayModel.Validate(altered) != null, "Unknown copy ordering escaped the guard: " + mutation);
        }
        if (!endTurn)
        foreach (string missingPile in new[] { "Standby", "Exhausted" })
        {
            JsonNode changed = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            JsonArray piles = changed["Spawn"]!["Train"]!["Context"]!["OtherPiles"]!.AsArray();
            piles.Remove(piles.Single(pile => pile!["Name"]!.GetValue<string>() == missingPile));
            foreach (JsonNode? room in changed["Spawn"]!["Train"]!["Rooms"]!.AsArray())
            {
                JsonArray roomPiles = room!["Context"]!["OtherPiles"]!.AsArray();
                roomPiles.Remove(roomPiles.Single(pile => pile!["Name"]!.GetValue<string>() == missingPile));
            }
            BattleTurnState altered = changed.Deserialize<BattleTurnState>(ModelJson.Options)!;
            BattleActionResult result = BattleActionModel.PlayCard(altered, action!);
            Require(!result.Supported, "A missing " + missingPile + " pile must reject the branch; input has " +
                string.Join(",", altered.Spawn.Train.Context!.OtherPiles!.Select(pile => pile.Name)) +
                "; output has " + string.Join(",", result.State?.Spawn.Train.Context?.OtherPiles?.Select(pile => pile.Name) ?? []));
        }
        NativeBranchObservationChecks.Replay(repaired, Reason);
        NativeBranchObservationChecks.Replay(repeated, Reason);
        NativeBranchObservationChecks.Pair(first, repaired);
        NativeBranchObservationChecks.Pair(first, repeated);
        NativeBranchObservationChecks.Pair(repaired, repeated, originalPredictionPresent: true, originalPredictionMatched: true);
        Console.WriteLine("REVERSE-COPY-CONSUME PASS: 26-input prefix, " + (endTurn ? "unplayed discard" : "played copy and Consume") +
            ", two historical and two repaired native states/RNG, four immutable branches and five source guards" +
            (endTurn ? "." : ", plus two missing-pile guards."));
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
