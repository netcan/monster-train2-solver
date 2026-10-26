using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FleshweaverMerchantBirthChecks
{
    private const string Reason = "Outer-boss Covenant owners require separate calibration.";
    internal static void Native(FixtureValue source, FixtureValue birth, FixtureValue? replay = null, FixtureValue? replayBirth = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 1 && rows[0].GetProperty("IsEndTurn").GetBoolean(), "Missing natural first EndTurn.");
        OuterBossBirthChecks.Native(birth);
        var row = birth.GetProperty("Births")[0];
        var input = row.GetProperty("Instantiation").Deserialize<OuterBossInstantiationState>()!;
        var created = row.GetProperty("Before").Deserialize<OuterBossCreationState>()!;
        var actual = row.GetProperty("Actual").Deserialize<OuterBossPhaseState>()!;
        var born = actual.Battle.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == actual.Boss.UnitId);
        Require(input.Battle.MoonPhaseTrackingActive != true &&
            input.Definition.Actor.Unit is { AssetKey: "Boss_Level3_Fleshweaver_Corruption", BaseAttack: 6, Health: 500 } &&
            created.CreatedUnit is { Id: 4, BaseAttack: 6, Health: 500 } &&
            created.BirthStats is { Attack: 1, Health: 180 } stats && stats.CovenantIds.SequenceEqual(new[] {
                "d3b53481-f26b-4344-ae8e-ec80b28d6004", "92c18b99-e4c0-4953-ba2c-4815f570a66e",
                "dcb31cd7-4293-4b07-b396-5456a0ce0b6e" }) &&
            born.BaseAttack == 7 && born.Attack == 17 && born.Health == 680 && born.MaxHealth == 680 &&
            born.Status("mass") is { Stacks: 5, ParamInt: 2 }, "Lost original Mass5, birth stats or Covenant origins.");
        string immutable = JsonSerializer.Serialize(input, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = OuterBossInstantiationModel.Apply(input);
            Require(result.Supported && ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options),
                JsonSerializer.Serialize(created, ModelJson.Options)) == null &&
                immutable == JsonSerializer.Serialize(input, ModelJson.Options), "Creation changed its parent or native result.");
        });
        foreach (string asset in new[] { "Boss_Level3_Fleshweaver_MoreMass", "Boss_Level3_Fleshweaver_Spike" })
        {
            var changed = JsonSerializer.SerializeToNode(input, ModelJson.Options)!;
            changed["Definition"]!["Actor"]!["Unit"]!["AssetKey"] = asset;
            changed["Definition"]!["Boss"]!["AssetKey"] = asset;
            Require(!OuterBossInstantiationModel.Apply(changed.Deserialize<OuterBossInstantiationState>(ModelJson.Options)!).Supported,
                "Uncalibrated merchant boss variant must stay guarded.");
        }
        var changedOwner = JsonSerializer.SerializeToNode(input, ModelJson.Options)!;
        var merchant = changedOwner["Battle"]!["Spawn"]!["Train"]!["Context"]!["Relics"]!.AsArray()
            .Single(owner => owner!["AssetKey"]!.GetValue<string>() == "Covenant03_MerchantCostsIncrease")!;
        merchant["EffectTypes"]!.AsArray().Add("RelicEffectAddStatusEffectOnSpawn");
        Require(!OuterBossInstantiationModel.Apply(changedOwner.Deserialize<OuterBossInstantiationState>(ModelJson.Options)!).Supported,
            "An altered Covenant effect signature must stay guarded.");
        if (replay.HasValue)
        {
            Require(replayBirth.HasValue, "Missing independent birth capture.");
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
            OuterBossBirthChecks.Native(replayBirth!.Value);
        }
        Console.WriteLine("FLESHWEAVER-MERCHANT-BIRTH PASS: natural Covenant3 first EndTurn/full state/forecast/RNG; raw6/500 ->7/680/Mass5; four immutable branches and strict owner/variant controls; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
