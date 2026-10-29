using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CopyBeforeConsumeDrawChecks
{
    private const string Reason = "Copy-on-play requires Consume to precede the copy callback, as in the original calibrated upgrade order.";

    internal static void Native(FixtureValue first, FixtureValue second)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(first, Reason);
        FixtureValue[] repeated = NativeBranchObservationChecks.Replay(second, Reason);
        Require(rows.Length == 1 && repeated.Length == 1 && rows[0].GetProperty("IsEndTurn").GetBoolean(), "Expected the original deployment EndTurn.");
        foreach (string field in new[] { "Before", "Actual", "Action", "IsEndTurn", "Boundary" })
            Require(rows[0].GetProperty(field).ContentEquals(repeated[0].GetProperty(field)), "Independent native boundary differs: " + field);
        BattleTurnState before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CardInstanceState card = before.Spawn.Train.Context!.FindCard(17)!;
        Require(card.DataId == "8de281af-ab9a-4d3d-ac5f-89cd43ff6c84" &&
            card.Permanent.Upgrades.Select(upgrade => upgrade.DataId).SequenceEqual(new[] {
                CardCopyOnPlayModel.UpgradeId, "fb416958-813a-435b-b7ea-e428127ece20" }) &&
            before.Spawn.Train.Context.Cards.Draw.Any(token => token.InstanceId == 17) &&
            actual.Spawn.Train.Context!.Cards.Draw.Any(token => token.InstanceId == 17), "Original inactive draw-pile card changed.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual) &&
                immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Concurrent EndTurn differs or mutated its input.");
        });
        BattlePlanResult plan = ParallelBattlePlanner.Plan(before,
            new BattlePlannerOptions(256, 3, 8, 12, TimeSpan.FromSeconds(3), allowShorterHorizon: true,
                searchToBattleEnd: true, maxSteps: 256), 2);
        Require(plan.Steps.Count > 0 && (plan.CompletedHorizon || plan.UsedShorterHorizon),
            "The protected card must not abort the whole search.");
        foreach (string mutation in new[] { "hand", "other-pile", "card", "upgrade" })
        {
            JsonNode changed = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            JsonNode context = changed["Spawn"]!["Train"]!["Context"]!;
            JsonNode target = context["CardInstances"]!.AsArray().Single(item => item!["InstanceId"]!.GetValue<int>() == 17)!;
            JsonArray draw = context["Cards"]!["Draw"]!.AsArray();
            JsonNode token = draw.Single(item => item!["InstanceId"]!.GetValue<int>() == 17)!.DeepClone();
            switch (mutation)
            {
                case "hand": context["Cards"]!["Hand"]!.AsArray().Add(token); break;
                case "other-pile": context["OtherPiles"]![0]!["Cards"]!.AsArray().Add(token); break;
                case "card": target["DataId"] = "unknown"; break;
                case "upgrade": target["Permanent"]!["Upgrades"]![1]!["DataId"] = "unknown"; break;
            }
            BattleTurnState altered = changed.Deserialize<BattleTurnState>(ModelJson.Options)!;
            CombatContext alteredContext = altered.Spawn.Train.Context!;
            Require(!CardCopyOnPlayModel.CanDeferUnplayedDrawCard(alteredContext, alteredContext.FindCard(17)!,
                altered.Spawn.Train.Rooms.SelectMany(room => room.Units), Reason),
                "Unknown or active card escaped the guard: " + mutation);
        }
        Console.WriteLine("COPY-BEFORE-CONSUME-DRAW PASS: two original native successors/RNG, four immutable branches and four guards.");
    }

    internal static void Repaired(FixtureValue first, FixtureValue second, FixtureValue repaired, FixtureValue repeated)
    {
        Native(first, second);
        NativeBranchObservationChecks.Replay(repaired, Reason);
        NativeBranchObservationChecks.Replay(repeated, Reason);
        NativeBranchObservationChecks.Pair(first, repaired);
        NativeBranchObservationChecks.Pair(first, repeated);
        NativeBranchObservationChecks.Pair(repaired, repeated, originalPredictionPresent: true, originalPredictionMatched: true);
        Console.WriteLine("COPY-BEFORE-CONSUME-DRAW-REPAIRED PASS: original rejection and two matching repaired native forecasts.");
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
