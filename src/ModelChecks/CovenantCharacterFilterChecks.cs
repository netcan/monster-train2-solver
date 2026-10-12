using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CovenantCharacterFilterChecks
{
    internal static void Run()
    {
        var titanSkin = new CombatUnit(1, "TitanBoss", CombatTeam.Enemy, 0, 1, 1, false, false, false,
            [new CombatStatus("titanskin", 2, 2)]);
        RoomCombatResult result = RoomCombatModel.Resolve(new RoomCombatState(0, false, [titanSkin], []));
        Require(!result.Supported && result.UnsupportedReason == "Titan Skin damage soak and stack consumption are not modeled.",
            "Titan Skin combat was not rejected explicitly.");
    }

    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("ModifierScenario").GetString() == "covenant-character-filter" &&
            fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Mismatches").GetInt32() == 0 && fixture.GetProperty("Unsupported").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0, "Incomplete Covenant character-filter battle.");

        var spawns = fixture.GetProperty("Spawns").EnumerateArray().ToArray();
        Require(spawns.Length == 11 && spawns.All(record => record.GetProperty("Predicted").GetProperty("Supported").GetBoolean() &&
            record.GetProperty("Difference").ValueKind == FixtureKind.Null),
            "Covenant battle wave generation did not match native state.");

        var records = fixture.GetProperty("RelicSpawnStatuses").EnumerateArray().ToArray();
        Require(records.Length == 8 && records.All(record => record.GetProperty("OnlyCovenants").GetBoolean() &&
            record.GetProperty("Completed").GetBoolean() && record.GetProperty("Difference").ValueKind == FixtureKind.Null),
            "Covenant-only character birth observations are incomplete.");

        int matchedBosses = 0;
        foreach (FixtureValue record in records)
        {
            var before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = record.GetProperty("After").Deserialize<RoomCombatState>()!;
            int unitId = record.GetProperty("UnitId").GetInt32();
            CombatUnit beforeUnit = before.Units.Single(unit => unit.Id == unitId);
            CombatUnit afterUnit = after.Units.Single(unit => unit.Id == unitId);
            RoomCombatResult predicted = RelicSpawnStatusModel.CharacterAdded(before, unitId,
                record.GetProperty("FromCardId").GetInt32(), onlyCovenants: true);
            Require(predicted.Supported && ModelJson.Difference(JsonSerializer.Serialize(predicted.State, ModelJson.Options),
                JsonSerializer.Serialize(after, ModelJson.Options)) == null,
                "Covenant character birth differs for " + afterUnit.AssetKey + ".");

            if (afterUnit.AssetKey == "Boss_Level1_Basic")
            {
                matchedBosses++;
                var relic = before.Context!.Relics!.Single(item => item.AssetKey == "Covenant04_StrongerBosses");
                RelicCharacterTriggers filter = relic.TriggerAdditions!.Single(rule =>
                    rule.CharacterAssetKeys.SequenceEqual(["Boss_Level1_Basic"]));
                Require(filter.Triggers.Any(trigger => trigger.Kind == "OnKill" && trigger.Effects.Any(effect =>
                    effect.Action?.Statuses.Any(status => status.Id == "buff" && status.Stacks == 1 && status.ParamInt == 2) == true)) &&
                    beforeUnit.Status("buff") == null && afterUnit.Status("buff")?.Stacks == 3 &&
                    beforeUnit.Triggers.All(trigger => trigger.Kind != "OnKill") && afterUnit.Triggers.Count(trigger => trigger.Kind == "OnKill") == 1,
                    "The matching Boss did not receive its filtered Covenant trigger and three Rage stacks.");
            }
            else
            {
                Require(JsonSerializer.Serialize(beforeUnit.Statuses, ModelJson.Options) == JsonSerializer.Serialize(afterUnit.Statuses, ModelJson.Options) &&
                    JsonSerializer.Serialize(beforeUnit.Triggers, ModelJson.Options) == JsonSerializer.Serialize(afterUnit.Triggers, ModelJson.Options),
                    "A nonmatching birth was modified by the filtered Covenant effect.");
            }
        }
        Require(matchedBosses == 1, "The Covenant filter did not match exactly one Boss_Level1_Basic birth.");
        Console.WriteLine("COVENANT-CHARACTER-FILTER-CHECKS PASS: 11 native waves, 8 Covenant births, one filtered Boss trigger and Rage application.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}
