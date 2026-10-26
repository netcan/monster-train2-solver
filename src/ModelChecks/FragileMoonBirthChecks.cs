using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FragileMoonBirthChecks
{
    private const string Reason = "Outer-boss instantiation requires the calibrated original definition without creation modifiers or moon tracking.";
    internal static void Native(FixtureValue source, FixtureValue birth, FixtureValue? replay = null, FixtureValue? replayBirth = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        if (rows.Length != 1 || !rows[0].GetProperty("IsEndTurn").GetBoolean())
            throw new InvalidOperationException("Missing natural first EndTurn.");
        OuterBossBirthChecks.Native(birth);
        var row = birth.GetProperty("Births")[0];
        var input = row.GetProperty("Instantiation").Deserialize<OuterBossInstantiationState>()!;
        var created = row.GetProperty("Before").Deserialize<OuterBossCreationState>()!;
        if (input.Battle.MoonPhaseTrackingActive != true || input.Battle.MoonPhase != "Full" ||
            input.Definition.Actor.Unit is not { AssetKey: "Boss_Level8_Fragile", BaseAttack: 30, Health: 3000, MoonPhaseMask: 4 } ||
            created.CreatedUnit is not { Id: 5, AssetKey: "Boss_Level8_Fragile", MoonPhaseMask: 6, Health: 3000 } ||
            created.CreatedUnit.Status("fullmoon")?.Stacks != 1 ||
            !created.CreatedUnit.StatusRegistry!.Select(status => status.Id).SequenceEqual(new[] { "fullmoon" }))
            throw new InvalidOperationException("Lost natural30/3000/full-moon boss birth, identity5 or registry.");
        string immutable = JsonSerializer.Serialize(input, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = OuterBossInstantiationModel.Apply(input);
            if (!result.Supported || ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options),
                JsonSerializer.Serialize(created, ModelJson.Options)) != null || immutable != JsonSerializer.Serialize(input, ModelJson.Options))
                throw new InvalidOperationException("Detached Fragile moon creation differs or changes parent.");
        });
        foreach (string control in new[] { "Moon", "Variant", "Trigger", "Health" })
        {
            var changed = JsonSerializer.SerializeToNode(input, ModelJson.Options)!;
            if (control == "Moon") changed["Battle"]!["Spawn"]!["Train"]!["Context"]!["QueryFrame"]!["MoonPhase"] = 1;
            if (control == "Variant")
            {
                changed["Definition"]!["Actor"]!["Unit"]!["AssetKey"] = "Boss_Level8_AllyBuffer";
                changed["Definition"]!["Boss"]!["AssetKey"] = "Boss_Level8_AllyBuffer";
            }
            if (control == "Trigger") changed["Definition"]!["Actor"]!["Unit"]!["Triggers"]![0]!["Kind"] = "OnMoonLit";
            if (control == "Health") changed["Definition"]!["Actor"]!["Unit"]!["Health"] = 3001;
            if (OuterBossInstantiationModel.Apply(changed.Deserialize<OuterBossInstantiationState>(ModelJson.Options)!).Supported)
                throw new InvalidOperationException("Unknown Fragile moon signature accepted: " + control);
        }
        if (replay.HasValue)
        {
            if (!replayBirth.HasValue) throw new InvalidOperationException("Independent birth capture required.");
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
            OuterBossBirthChecks.Native(replayBirth.Value);
        }
        Console.WriteLine("FRAGILE-MOON-BIRTH PASS: first native EndTurn/full state/forecast/RNG, full-moon30/3000/identity5, four immutable branches and four strict guards; independentPair=" + replay.HasValue);
    }
}
