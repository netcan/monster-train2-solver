using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class GreenRecruitRoomPreviewChecks
{
    internal static void Native(FixtureValue fixture)
    {
        var stages = fixture.GetProperty("Stages").EnumerateArray().ToArray();
        var exchange = stages.Single(stage => stage.GetProperty("Index").GetInt32() == 37);
        var before = exchange.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var actual = exchange.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        var birth = stages.Single(stage => stage.GetProperty("Index").GetInt32() == 38)
            .GetProperty("Actual").Deserialize<RoomCombatState>()!;
        CombatUnit fungus = birth.Units.Single(unit => unit.Id == 15);
        Require(exchange.GetProperty("Kind").GetString() == "Exchange" &&
            before.Context is { Enchantments: null, SummonCatalog: null, NextUnitId: 15 } &&
            before.Units.Single(unit => unit.Id == 2) is { AssetKey: "MonsterGenerousHost", Health: 3 } &&
            actual.Context!.NextUnitId == 15 && actual.Context.SpawnPoints!.Units.All(point => point.UnitId < 15) &&
            fungus is { AssetKey: "MonsterFunguy", Team: CombatTeam.Player, SpawnerCardId: 0 } &&
            birth.Context!.NextUnitId == 16 && birth.Context.SpawnPoints!.Units.Single(point => point.UnitId == 15).SpawnedInPreview == false,
            "Lost native unobserved kill-preview versus real subsequent birth15.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string units = JsonSerializer.Serialize(actual.Units, ModelJson.Options);
        string context = JsonSerializer.Serialize(BattleTurnChecks.ComparableContext(actual.Context), ModelJson.Options);
        Parallel.For(0, 4, index =>
        {
            RoomCombatResult result = RoomCombatModel.Exchange(before);
            Require(result.Supported && result.Outcome == RoomOutcome.Exchanged &&
                JsonSerializer.Serialize(result.State!.Units, ModelJson.Options) == units &&
                JsonSerializer.Serialize(BattleTurnChecks.ComparableContext(result.State.Context!), ModelJson.Options) == context,
                "Room-only kill preview differs from its full native exchange.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Room-only kill preview mutated its parent.");

        CombatContext preview = before.Context.WithNextUnitId(16);
        Require(PreviewBirthModel.UnobservedRoomPlayers(before, preview, [fungus]).SequenceEqual([15]),
            "Calibrated cardless temporary player identity was not recognized.");
        CombatUnit Changed(string key, CombatTeam team, int spawner = 0, int id = 15) => new(id, key, team,
            fungus.BaseAttack, fungus.Health, fungus.MaxHealth, fungus.CanAttack, false, false, fungus.Statuses,
            fungus.Triggers, spawner);
        CombatUnit[][] unknown = [[], [Changed("unknown", CombatTeam.Player)], [Changed("MonsterFunguy", CombatTeam.Enemy)],
            [Changed("MonsterFunguy", CombatTeam.Player, 99)], [Changed("MonsterFunguy", CombatTeam.Player, id: 16)]];
        foreach (CombatUnit[] actors in unknown)
            Require(PreviewBirthModel.UnobservedRoomPlayers(before, preview, actors).Length == 0,
                "An unknown, owned, enemy, missing or out-of-range actor was forgotten.");
        Require(PreviewBirthModel.UnobservedRoomPlayers(before, preview.WithNextUnitId(17), [fungus, fungus]).Length == 0 &&
            PreviewBirthModel.UnobservedRoomPlayers(before, preview.WithNextCardId(preview.NextCardId + 1), [fungus]).Length == 0 &&
            PreviewBirthModel.UnobservedRoomPlayers(before.WithContext(before.Context.WithSummonCatalog(new([], [], []))), preview, [fungus]).Length == 0,
            "Duplicate births, new card identity or an explicit summon observer bypassed retention.");
        Console.WriteLine("GREEN-RECRUIT-ROOM-PREVIEW PASS: native exchange/real birth15, four immutable branches and eight unknown-scope retention controls; original52-stage archive unchanged.");
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
