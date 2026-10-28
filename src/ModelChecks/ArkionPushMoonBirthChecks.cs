using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ArkionPushMoonBirthChecks
{
    private const string Reason = "Outer-boss instantiation requires the calibrated original definition without creation modifiers or moon tracking.";

    internal static void Native(FixtureValue first, FixtureValue birth, FixtureValue second, FixtureValue secondBirth)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(first, Reason);
        FixtureValue[] other = NativeBranchObservationChecks.Replay(second, Reason);
        Require(rows.Length == 1 && other.Length == 1 && rows[0].GetProperty("IsEndTurn").GetBoolean(),
            "Expected the untouched first end-turn boundary.");
        foreach (string field in new[] { "Before", "Actual", "Action", "IsEndTurn", "Boundary" })
            Require(rows[0].GetProperty(field).ContentEquals(other[0].GetProperty(field)), "Independent native boundary differs: " + field);
        OuterBossBirthChecks.Native(birth);
        OuterBossBirthChecks.Native(secondBirth);
        FixtureValue row = birth.GetProperty("Births")[0], repeated = secondBirth.GetProperty("Births")[0];
        foreach (string field in new[] { "Instantiation", "Before", "Actual", "NativeReturned" })
            Require(row.GetProperty(field).ContentEquals(repeated.GetProperty(field)), "Independent native birth differs: " + field);
        var input = row.GetProperty("Instantiation").Deserialize<OuterBossInstantiationState>()!;
        var created = row.GetProperty("Before").Deserialize<OuterBossCreationState>()!;
        Require(input.Battle.MoonPhaseTrackingActive == true && input.Battle.MoonPhase == "Full" &&
            input.Definition.Actor.Unit is { AssetKey: "Boss_Level3_Arkion_PushAttack", BaseAttack: 13, Health: 425, MoonPhaseMask: 4 } &&
            created.CreatedUnit is { Id: 5, AssetKey: "Boss_Level3_Arkion_PushAttack", Attack: 13, Health: 425, MoonPhaseMask: 6 } &&
            created.CreatedUnit.Status("fullmoon")?.Stacks == 1 &&
            created.CreatedUnit.StatusRegistry!.Select(status => status.Id).SequenceEqual(["fullmoon"]),
            "Original full-moon Arkion creation changed.");
        string immutable = JsonSerializer.Serialize(input, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            OuterBossInstantiationResult result = OuterBossInstantiationModel.Apply(input);
            Require(result.Supported && JsonSerializer.Serialize(result.State, ModelJson.Options) == JsonSerializer.Serialize(created, ModelJson.Options) &&
                immutable == JsonSerializer.Serialize(input, ModelJson.Options), "Concurrent native birth differs or mutated its input.");
        });
        foreach (string mutation in new[] { "moon", "asset", "health", "trigger", "callback" })
        {
            JsonNode changed = JsonSerializer.SerializeToNode(input, ModelJson.Options)!;
            JsonNode actor = changed["Definition"]!["Actor"]!["Unit"]!;
            switch (mutation)
            {
                case "moon": changed["Battle"]!["Spawn"]!["Train"]!["Context"]!["QueryFrame"]!["MoonPhase"] = 1; break;
                case "asset": actor["AssetKey"] = "unknown"; changed["Definition"]!["Boss"]!["AssetKey"] = "unknown"; break;
                case "health": actor["Health"] = 426; actor["MaxHealth"] = 426; break;
                case "trigger": actor["Triggers"]![0]!["Kind"] = "OnHit"; break;
                case "callback": actor["Triggers"] = JsonSerializer.SerializeToNode(new[] {
                    new CombatTrigger("OnMoonLit", false, false, false, 0, Array.Empty<CombatEffect>()) }, ModelJson.Options); break;
            }
            Require(!OuterBossInstantiationModel.Apply(changed.Deserialize<OuterBossInstantiationState>(ModelJson.Options)!).Supported,
                "Unknown Arkion moon birth escaped the guard: " + mutation);
        }
        Console.WriteLine("ARKION-PUSH-MOON-BIRTH PASS: two native birth and full successor/RNG observations, four immutable branches, five guards.");
    }

    internal static void Repaired(FixtureValue original, FixtureValue originalBirth, FixtureValue first, FixtureValue firstBirth,
        FixtureValue second, FixtureValue secondBirth)
    {
        Native(original, originalBirth, first, firstBirth);
        NativeBranchObservationChecks.Replay(second, Reason);
        OuterBossBirthChecks.Native(secondBirth);
        NativeBranchObservationChecks.Pair(original, first);
        NativeBranchObservationChecks.Pair(original, second);
        NativeBranchObservationChecks.Pair(first, second, originalPredictionPresent: true, originalPredictionMatched: true);
        foreach (string field in new[] { "Instantiation", "Before", "Actual", "NativeReturned" })
            Require(firstBirth.GetProperty("Births")[0].GetProperty(field).ContentEquals(secondBirth.GetProperty("Births")[0].GetProperty(field)),
                "Independent repaired creation differs: " + field);
        Console.WriteLine("ARKION-PUSH-MOON-REPAIRED PASS: historical rejection and two independently matching repaired native forecasts and births.");
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
