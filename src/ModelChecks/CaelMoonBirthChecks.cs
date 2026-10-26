using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CaelMoonBirthChecks
{
    private const string Reason = "Outer-boss instantiation requires the calibrated original definition without creation modifiers or moon tracking.";
    internal static void Native(FixtureValue source, FixtureValue birth, FixtureValue? replay = null, FixtureValue? replayBirth = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 1 && rows[0].GetProperty("IsEndTurn").GetBoolean(), "Missing original first EndTurn.");
        OuterBossBirthChecks.Native(birth);
        var row = birth.GetProperty("Births")[0];
        var input = row.GetProperty("Instantiation").Deserialize<OuterBossInstantiationState>()!;
        var created = row.GetProperty("Before").Deserialize<OuterBossCreationState>()!;
        Require(input.Battle.MoonPhaseTrackingActive == true && input.Battle.MoonPhase == "Full" &&
            input.Definition.Actor.Unit is { AssetKey: "Boss_Level6_Cael_DamageShield", BaseAttack: 22, Health: 1800, MoonPhaseMask: 4 } &&
            created.CreatedUnit is { Id: 4, AssetKey: "Boss_Level6_Cael_DamageShield", MoonPhaseMask: 6, Health: 1800 } &&
            created.CreatedUnit.Status("fullmoon")?.Stacks == 1 &&
            created.CreatedUnit.StatusRegistry!.Select(status => status.Id).SequenceEqual(new[] { "fullmoon" }),
            "Lost original Cael definition, full-moon birth, identity4 or status order.");
        string immutable = JsonSerializer.Serialize(input, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = OuterBossInstantiationModel.Apply(input);
            Require(result.Supported && ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options),
                JsonSerializer.Serialize(created, ModelJson.Options)) == null &&
                immutable == JsonSerializer.Serialize(input, ModelJson.Options), "Creation changed native result or parent.");
        });
        foreach (string control in new[] { "Moon", "Variant", "Trigger" })
        {
            var changed = JsonSerializer.SerializeToNode(input, ModelJson.Options)!;
            if (control == "Moon") changed["Battle"]!["Spawn"]!["Train"]!["Context"]!["QueryFrame"]!["MoonPhase"] = 1;
            if (control == "Variant")
            {
                changed["Definition"]!["Actor"]!["Unit"]!["AssetKey"] = "Boss_Level6_Cael_Lifesteal";
                changed["Definition"]!["Boss"]!["AssetKey"] = "Boss_Level6_Cael_Lifesteal";
            }
            if (control == "Trigger") changed["Definition"]!["Actor"]!["Unit"]!["Triggers"]![0]!["Kind"] = "OnMoonLit";
            Require(!OuterBossInstantiationModel.Apply(changed.Deserialize<OuterBossInstantiationState>(ModelJson.Options)!).Supported,
                "Uncalibrated Cael moon control accepted: " + control);
        }
        if (replay.HasValue)
        {
            Require(replayBirth.HasValue, "Missing independent creation capture.");
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
            OuterBossBirthChecks.Native(replayBirth!.Value);
        }
        Console.WriteLine("CAEL-MOON-BIRTH PASS: original first EndTurn/full state/forecast/RNG; full-moon22/1800/identity4; four immutable branches and three strict guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
