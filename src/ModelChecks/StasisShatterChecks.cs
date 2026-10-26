using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class StasisShatterChecks
{
    private const string Reason = "Stasis shatter removes its own ability and restores the room upgrade.";
    private static CombatUnit Unit(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 2);
    internal static void Native(FixtureValue battle, FixtureValue? source = null, FixtureValue? replay = null)
    {
        AbilityLifecycleChecks.Run();
        var rows = battle.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length == 46, "Missing natural46-input shatter counterexample.");
        var row = rows[^1];
        var before = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(action.ActivatorUnitId == 2 && action.CardInstanceId == 38 && before.Spawn.Turn == 7 &&
            Unit(before).Status("untouchable")?.Stacks == 1 && Unit(before).Ability?.DataId == "91fc348d-5b7a-4114-ad4b-a234b3dfd8d1" &&
            Unit(actual).Ability == null && Unit(actual).Status("cooldown") == null && Unit(actual).BaseAttack == 35 &&
            Unit(actual).Triggers.Count == 2 && Unit(actual).EquipmentCards!.SequenceEqual([9]), "Wrong native shatter boundary.");
        var room = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0);
        var change = before.PlayRules!.Cards.Single(rule => rule.DataId == Unit(before).Ability!.DataId).Effects[1].AbilityChange!;
        var direct = AbilityLifecycleModel.Apply(room, 2, change, remove: true);
        Require(direct.Supported && direct.State!.Units.Single(unit => unit.Id == 2).Ability == null &&
            direct.State.Units.Single(unit => unit.Id == 2).Triggers.Count == 2, "Direct self ability removal differs.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && BattleTurnChecks.Comparable(result.State!, false) == BattleTurnChecks.Comparable(actual, false),
                "Shatter differs from full native state/RNG: " + (result.Reason ?? ModelJson.Difference(
                    JsonSerializer.Serialize(result.State, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options))));
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Shatter changed parent.");
        foreach (string control in new[] { "AssetKey", "DataId", "Damage", "Health" })
        {
            JsonNode changed = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            var attachment = changed["Spawn"]!["Train"]!["Context"]!["RoomCapacities"]!.AsArray()
                .Single(room => room!["RoomIndex"]!.GetValue<int>() == 0)!["Attachments"]![0]!;
            if (control == "AssetKey") attachment["AssetKey"] = "OtherFightClub";
            if (control == "DataId") attachment["CharacterUpgrades"]![0]!["DataId"] = "other-upgrade";
            if (control == "Damage") attachment["CharacterUpgrades"]![0]!["Stats"]!["Damage"] = 16;
            if (control == "Health") attachment["CharacterUpgrades"]![0]!["Stats"]!["Health"] = 1;
            Require(!BattleActionModel.PlayCard(changed.Deserialize<BattleTurnState>(ModelJson.Options)!, action).Supported,
                "Shatter accepted uncalibrated room payload: " + control);
        }
        if (source.HasValue || replay.HasValue)
        {
            Require(source.HasValue && replay.HasValue, "Independent pair missing.");
            NativeBranchObservationChecks.Replay(source!.Value, Reason, (historical, native) =>
                Require(Unit(historical).BaseAttack == 20 && Unit(native).BaseAttack == 35 &&
                    Unit(historical).Ability != null && Unit(native).Ability == null, "Original shatter counterexample changed."));
            NativeBranchObservationChecks.Replay(replay!.Value, Reason);
            NativeBranchObservationChecks.Pair(source.Value, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("STASIS-SHATTER PASS: exact self removal and room +15 attack; complete state/forecast/RNG, four immutable branches; independentPair=" + source.HasValue);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
