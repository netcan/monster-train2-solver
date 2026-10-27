using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HarvestAngelMoonBirthChecks
{
    private const string Reason = "Outer-boss instantiation requires the calibrated original definition without creation modifiers or moon tracking.";
    internal static void Native(FixtureValue source, FixtureValue birth, FixtureValue? replay = null, FixtureValue? replayBirth = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 2 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native champion/first-EndTurn prefix.");
        OuterBossBirthChecks.Native(birth);
        var row = birth.GetProperty("Births")[0];
        var input = row.GetProperty("Instantiation").Deserialize<OuterBossInstantiationState>()!;
        var created = row.GetProperty("Before").Deserialize<OuterBossCreationState>()!;
        Require(input.Battle.MoonPhaseTrackingActive == true && input.Battle.MoonPhase == "Full" &&
            input.Definition.Actor.Unit is { AssetKey: "Boss_Level6_AngelDuo_Entropic_Variant3_HarvestHealth", BaseAttack: 11, Attack: 11,
                Health: 1250, MaxHealth: 1250, MoonPhaseMask: 4 } &&
            created.CreatedUnit is { Id: 5, AssetKey: "Boss_Level6_AngelDuo_Entropic_Variant3_HarvestHealth", MoonPhaseMask: 6,
                Attack: 11, Health: 1250, EndsBattleOnDeath: false } && created.CreatedUnit.BumpRules!.PairedBossUnitId == 3 &&
            created.CreatedUnit.StatusRegistry!.Select(status => status.Id).SequenceEqual(new[] { "titanskin", "fullmoon" }) &&
            created.CreatedUnit.Status("titanskin")?.Stacks == 10 && created.CreatedUnit.Status("fullmoon")?.Stacks == 1,
            "Lost original harvest angel, full-moon initialization, ordered statuses or companion identity.");
        string parent = JsonSerializer.Serialize(input, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = OuterBossInstantiationModel.Apply(input);
            Require(result.Supported && JsonSerializer.Serialize(result.State, ModelJson.Options) == JsonSerializer.Serialize(created, ModelJson.Options),
                "Harvest angel birth differs from full native creation state/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(input, ModelJson.Options), "Birth mutated its parent.");
        foreach (string control in new[] { "Moon", "Variant", "Attack", "Health", "Trigger" })
        {
            var changed = JsonSerializer.SerializeToNode(input, ModelJson.Options)!;
            var actor = changed["Definition"]!["Actor"]!["Unit"]!;
            if (control == "Moon") changed["Battle"]!["Spawn"]!["Train"]!["Context"]!["QueryFrame"]!["MoonPhase"] = 1;
            if (control == "Variant")
            {
                actor["AssetKey"] = "Boss_Level6_AngelDuo_Entropic_Variant2_IncantHealth";
                changed["Definition"]!["Boss"]!["AssetKey"] = "Boss_Level6_AngelDuo_Entropic_Variant2_IncantHealth";
            }
            if (control == "Attack") actor["BaseAttack"] = 12;
            if (control == "Health") actor["Health"] = 1251;
            if (control == "Trigger") actor["Triggers"]![0]!["Kind"] = "OnMoonLit";
            Require(!OuterBossInstantiationModel.Apply(changed.Deserialize<OuterBossInstantiationState>(ModelJson.Options)!).Supported,
                "Uncalibrated harvest angel birth admitted: " + control);
        }
        if (replay.HasValue)
        {
            Require(replayBirth.HasValue, "Missing independent native birth capture.");
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
            OuterBossBirthChecks.Native(replayBirth!.Value);
        }
        Console.WriteLine("HARVEST-ANGEL-MOON-BIRTH PASS: two native inputs/full state/forecast/RNG, original11ATK/1250HP, full-moon mask6 and ordered titan-skin/fullmoon, companion3, four immutable branches and five guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
