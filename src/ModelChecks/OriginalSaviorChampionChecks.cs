using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalSaviorChampionChecks
{
    private const string Fel = "19705414-816d-4549-bcf5-866f338a551f";
    private static readonly string[] AbilityIds =
    [
        "1cbb4a0b-9ad5-4d05-91e5-d94da55d21e5",
        "4252e8ed-4f8f-474f-80be-622eb6dae7fa",
        "2479c736-4463-4346-8464-d517dae23f40"
    ];

    internal static void Native(FixtureValue root)
    {
        int rank = Int("Rank");
        Require(Int("Schema") == 1 && Value("Boundary") == "OriginalSaviorChampionAbility" &&
            Value("GameVersion") == "2.3.1" && Value("GameModuleMvid") == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            Value("ChampionDataId") == Fel && rank is 2 or 3 && Value("AbilityDataId") == AbilityIds[rank - 1],
            "Original ranked Savior component source differs.");
        string abilityId = AbilityIds[rank - 1];
        var upgrades = root.GetProperty("Upgrades").EnumerateArray().ToArray();
        string?[] upgradeIds = upgrades.Select(row => row.GetProperty("Id").GetString()).ToArray();
        Require(upgrades.Select(row => row.GetProperty("AbilityDataId").GetString()).SequenceEqual(AbilityIds.Take(rank)) &&
            upgradeIds.All(id => Guid.TryParse(id, out _)) && upgradeIds.Distinct().Count() == rank &&
            upgrades.Skip(1).All(row => !row.GetProperty("AlreadyOwned").GetBoolean()),
            "Original Fel upgrade path is missing or out of order.");

        var before = root.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = root.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = root.GetProperty("Action").Deserialize<PlayCardAction>()!;
        int actorId = Int("ActorId"), sameFloorAllyId = Int("SameFloorAllyId"), otherFloorAllyId = Int("OtherFloorAllyId");
        int championId = Int("ChampionCardInstanceId");
        var context = before.Spawn.Train.Context!;
        var owner = context.FindCard(championId);
        var floor = before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
        var remote = before.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == otherFloorAllyId);
        Require(before.Spawn.Turn == 1 && actual.Spawn.Turn == 1 && before.Energy == actual.Energy &&
            before.SelectedRoom == 0 && actual.SelectedRoom == 0 && action.RoomIndex == 0 &&
            action.ActivatorUnitId == actorId && action.TargetUnitId == 0 && action.TargetSpawnPointIndex < 0 &&
            actorId != otherFloorAllyId && sameFloorAllyId != otherFloorAllyId && remote.Team == CombatTeam.Player &&
            floor.Select(unit => unit.Id).SequenceEqual(new[] { sameFloorAllyId, actorId }) &&
            floor[1].AssetKey == "MonsterBanishedChampion1" && floor[1].SpawnerCardId == championId &&
            owner?.DataId == Fel && owner.Permanent.Upgrades.Select(upgrade => upgrade.DataId).SequenceEqual(upgradeIds.TakeLast(1)) &&
            owner.Permanent.Upgrades[0].AssetKey == upgrades[^1].GetProperty("AssetKey").GetString() &&
            context.AbilityCardCache?.Any(entry => entry.DataId == abilityId && entry.InstanceId == action.CardInstanceId) == true,
            "The selected native skill is not the paid cached ability of the correctly upgraded Fel.");
        Require(floor.All(unit => unit.Health > 0 && (unit.Status("armor")?.Stacks ?? 0) == 0) &&
            (floor[1].Status("valor")?.Stacks ?? 0) == 0 && remote.Health > 0 && (remote.Status("armor")?.Stacks ?? 0) == 0,
            "Original Savior prestate lacks the independent zero-stack controls.");
        UnitStatusAbilityChecks.Native(before, actual, action, abilityId);

        var predicted = BattleActionModel.PlayCard(before, action);
        Require(predicted.Supported, "Original ranked Savior activation rejected: " + predicted.Reason);
        string? difference = ModelJson.Difference(Json(predicted.State), Json(actual));
        Require(difference == null, "Original ranked Savior successor differs: " + difference);
        Console.WriteLine("ORIGINAL-SAVIOR-CHAMPION PASS rank=" + rank + " exact native activation, Armor scope and complete settled state/RNG.");

        int Int(string name) => root.GetProperty(name).GetInt32();
        string? Value(string name) => root.GetProperty(name).GetString();
        static string Json(BattleTurnState? state) => JsonSerializer.Serialize(state, ModelJson.Options);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
