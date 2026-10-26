using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ArkionMoonBirthChecks
{
    private const string Reason = "Outer-boss instantiation requires the calibrated original definition without creation modifiers or moon tracking.";

    internal static void Native(FixtureValue source, FixtureValue birth, FixtureValue? replay = null, FixtureValue? replayBirth = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 1 && rows[0].GetProperty("IsEndTurn").GetBoolean(),
            "Missing the untouched natural deployment/first end-turn boundary.");
        OuterBossBirthChecks.Native(birth);
        var row = birth.GetProperty("Births")[0];
        var before = row.GetProperty("Before").Deserialize<OuterBossCreationState>()!;
        var input = row.GetProperty("Instantiation").Deserialize<OuterBossInstantiationState>()!;
        Require(input.Battle.MoonPhaseTrackingActive == true && input.Battle.MoonPhase == "Full" &&
            input.Definition.Actor.Unit is { AssetKey: "Boss_Level3_Arkion_SiphonRage", MoonPhaseMask: 4 } &&
            input.Definition.Actor.Unit.Statuses.Count == 0 &&
            before.CreatedUnit is { Id: 5, AssetKey: "Boss_Level3_Arkion_SiphonRage", MoonPhaseMask: 6, Attack: 7, Health: 425 } &&
            before.CreatedUnit.Status("fullmoon")?.Stacks == 1 &&
            before.CreatedUnit.StatusRegistry!.Select(status => status.Id).SequenceEqual(["fullmoon"]),
            "Lost original Arkion definition, full-moon setup, identity5 or registration order.");
        string immutable = JsonSerializer.Serialize(input, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = OuterBossInstantiationModel.Apply(input);
            Require(result.Supported && JsonSerializer.Serialize(result.State, ModelJson.Options) ==
                JsonSerializer.Serialize(before, ModelJson.Options) &&
                immutable == JsonSerializer.Serialize(input, ModelJson.Options), "Creation changed the parent or native result.");
        });
        var changed = JsonSerializer.SerializeToNode(input, ModelJson.Options)!;
        changed["Battle"]!["Spawn"]!["Train"]!["Context"]!["QueryFrame"]!["MoonPhase"] = 1;
        Require(!OuterBossInstantiationModel.Apply(changed.Deserialize<OuterBossInstantiationState>(ModelJson.Options)!).Supported,
            "Uncalibrated new-moon birth must stay guarded.");
        changed = JsonSerializer.SerializeToNode(input, ModelJson.Options)!;
        changed["Definition"]!["Actor"]!["Unit"]!["AssetKey"] = "Boss_Level3_Arkion_PushAttack";
        changed["Definition"]!["Boss"]!["AssetKey"] = "Boss_Level3_Arkion_PushAttack";
        Require(!OuterBossInstantiationModel.Apply(changed.Deserialize<OuterBossInstantiationState>(ModelJson.Options)!).Supported,
            "Different boss definitions must stay guarded.");
        changed = JsonSerializer.SerializeToNode(input, ModelJson.Options)!;
        changed["Definition"]!["Actor"]!["Unit"]!["Triggers"] = JsonSerializer.SerializeToNode(new[] {
            new CombatTrigger("OnMoonLit", false, false, false, 0, Array.Empty<CombatEffect>()) }, ModelJson.Options);
        Require(!OuterBossInstantiationModel.Apply(changed.Deserialize<OuterBossInstantiationState>(ModelJson.Options)!).Supported,
            "Uncalibrated moon callbacks must stay guarded.");
        if (replay.HasValue)
        {
            Require(replayBirth.HasValue, "Independent creation capture required.");
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
            OuterBossBirthChecks.Native(replayBirth!.Value);
        }
        Console.WriteLine("ARKION-MOON-BIRTH PASS: natural first end-turn/full state/forecast/RNG; original full-moon " +
            "creation/initial action, identity5 and registry; four immutable branches and strict guards; independentPair=" + replay.HasValue);
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
