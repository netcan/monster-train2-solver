using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CopyBeforeConsumeNotificationChecks
{
    private const string Reason = "Copy-on-play requires Consume to precede the copy callback, as in the original calibrated upgrade order.";

    internal static void Native(FixtureValue first, FixtureValue second)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(first, Reason);
        FixtureValue[] repeated = NativeBranchObservationChecks.Replay(second, Reason);
        Require(rows.Length == 19 && repeated.Length == rows.Length && rows[^1].GetProperty("IsEndTurn").GetBoolean(),
            "Expected 18 native inputs and the original draw boundary.");
        for (int index = 0; index < rows.Length; index++)
        foreach (string field in new[] { "Before", "Actual", "Action", "IsEndTurn", "Boundary" })
            Require(rows[index].GetProperty(field).ContentEquals(repeated[index].GetProperty(field)), "Independent native chain differs: " + index + "/" + field);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Train.Context!.Cards.Draw.Any(token => token.InstanceId == 17) &&
            actual.Spawn.Train.Context!.Cards.Hand.Any(token => token.InstanceId == 17), "The native draw did not move the guarded card to hand.");
        Require(BattleActionModel.EnumerateActions(actual).UnsupportedAttempts.Any(attempt =>
            attempt.Action?.CardInstanceId == 17 && attempt.Reason == Reason), "Playing the uncalibrated card must remain guarded.");
        BattleTurnResult discard = BattleTurnModel.EndTurn(actual);
        Require(!discard.Supported && discard.UnsupportedReason?.Contains(Reason, StringComparison.Ordinal) == true,
            "Discarding the uncalibrated card must remain guarded.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual) &&
                immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Concurrent draw boundary differs or mutated its input.");
        });
        foreach (string mutation in new[] { "relic", "effect", "card", "upgrade", "duplicate" })
        {
            JsonNode changed = JsonSerializer.SerializeToNode(actual, ModelJson.Options)!;
            JsonNode context = changed["Spawn"]!["Train"]!["Context"]!;
            JsonNode card = context["CardInstances"]!.AsArray().Single(item => item!["InstanceId"]!.GetValue<int>() == 17)!;
            switch (mutation)
            {
                case "relic": context["Relics"]![0]!["DataId"] = "unknown"; break;
                case "effect": context["Relics"]![0]!["EffectTypes"]![0] = "unknown"; break;
                case "card": card["DataId"] = "unknown"; break;
                case "upgrade": card["Permanent"]!["Upgrades"]![1]!["DataId"] = "unknown"; break;
                case "duplicate": context["Cards"]!["Draw"]!.AsArray().Add(context["Cards"]!["Hand"]!.AsArray()
                    .Single(item => item!["InstanceId"]!.GetValue<int>() == 17)!.DeepClone()); break;
            }
            CombatContext mutated = changed.Deserialize<BattleTurnState>(ModelJson.Options)!.Spawn.Train.Context!;
            Require(!CardCopyOnPlayModel.CanDeferDrawNotification(mutated, mutated.FindCard(17)!, Reason),
                "Unknown draw notification escaped the guard: " + mutation);
        }
        Console.WriteLine("COPY-BEFORE-CONSUME-NOTIFICATION PASS: 18-step independent native prefix, full draw successor/RNG, four immutable branches and five guards.");
    }

    internal static void Repaired(FixtureValue first, FixtureValue second, FixtureValue repaired, FixtureValue repeated)
    {
        Native(first, second);
        NativeBranchObservationChecks.Replay(repaired, Reason);
        NativeBranchObservationChecks.Replay(repeated, Reason);
        NativeBranchObservationChecks.Pair(first, repaired);
        NativeBranchObservationChecks.Pair(first, repeated);
        NativeBranchObservationChecks.Pair(repaired, repeated, originalPredictionPresent: true, originalPredictionMatched: true);
        Console.WriteLine("COPY-BEFORE-CONSUME-NOTIFICATION-REPAIRED PASS: historical rejection and two matching native forecasts.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
