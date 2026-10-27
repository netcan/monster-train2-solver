using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CaelMoonBirthChecks
{
    private const string Reason = "Outer-boss instantiation requires the calibrated original definition without creation modifiers or moon tracking.";
    internal static void Native(FixtureValue source, FixtureValue birth, FixtureValue? replay = null, FixtureValue? replayBirth = null,
        bool lifesteal = false)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == (lifesteal ? 2 : 1) && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing original first EndTurn.");
        OuterBossBirthChecks.Native(birth);
        var row = birth.GetProperty("Births")[0];
        var input = row.GetProperty("Instantiation").Deserialize<OuterBossInstantiationState>()!;
        var created = row.GetProperty("Before").Deserialize<OuterBossCreationState>()!;
        string asset = lifesteal ? "Boss_Level6_Cael_Lifesteal" : "Boss_Level6_Cael_DamageShield";
        Require(input.Battle.MoonPhaseTrackingActive == true && input.Battle.MoonPhase == "Full" &&
            input.Definition.Actor.Unit.AssetKey == asset && input.Definition.Actor.Unit is { BaseAttack: 22, Health: 1800, MoonPhaseMask: 4 } &&
            created.CreatedUnit.AssetKey == asset && created.CreatedUnit.Id == (lifesteal ? 6 : 4) &&
            created.CreatedUnit is { MoonPhaseMask: 6, Health: 1800 } &&
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
        string[] controls = lifesteal ? ["Moon", "Variant", "Trigger", "Attack", "Health", "FeedDamage", "FeedStatus"] : ["Moon", "Variant", "Trigger"];
        foreach (string control in controls)
        {
            var changed = JsonSerializer.SerializeToNode(input, ModelJson.Options)!;
            if (control == "Moon") changed["Battle"]!["Spawn"]!["Train"]!["Context"]!["QueryFrame"]!["MoonPhase"] = 1;
            if (control == "Variant")
            {
                string uncalibrated = lifesteal ? "Boss_Level6_Cael_AtkHP" : "Boss_Level6_Cael_Lifesteal";
                changed["Definition"]!["Actor"]!["Unit"]!["AssetKey"] = uncalibrated;
                changed["Definition"]!["Boss"]!["AssetKey"] = uncalibrated;
            }
            if (control == "Trigger") changed["Definition"]!["Actor"]!["Unit"]!["Triggers"]![0]!["Kind"] = "OnMoonLit";
            if (control == "Attack") changed["Definition"]!["Actor"]!["Unit"]!["BaseAttack"] = 23;
            if (control == "Health") changed["Definition"]!["Actor"]!["Unit"]!["MaxHealth"] = 1801;
            if (control == "FeedDamage") changed["Definition"]!["Actor"]!["Unit"]!["Triggers"]![0]!["Effects"]![0]!["UnitUpgrade"]!["Upgrade"]!["Stats"]!["Damage"] = 8;
            if (control == "FeedStatus") changed["Definition"]!["Actor"]!["Unit"]!["Triggers"]![0]!["Effects"]![0]!["UnitUpgrade"]!["Upgrade"]!["Statuses"]![0]!["Stacks"] = 2;
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
        Console.WriteLine($"CAEL-MOON-BIRTH PASS: {asset}, original first EndTurn/full state/forecast/RNG; full-moon22/1800/identity{created.CreatedUnit.Id}; four immutable branches/{controls.Length} strict guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
