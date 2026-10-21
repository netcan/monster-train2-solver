using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UndyingBossPreviewChecks
{
    internal static void Native(FixtureValue fixture)
    {
        FixtureValue turn = fixture.GetProperty("Turns")[0];
        var before = turn.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = turn.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        RoomCombatState room = actual.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2);
        CombatUnit boss = room.Units.Single(unit => unit.AssetKey == "Boss_Level5_Reanimate");
        Require(before.Spawn.Turn == 7 && actual.Spawn.Turn == 8 && boss.Health == 500 &&
            boss.Status("undying")?.Stacks == 8 && actual.Spawn.Train.Context!.Statistics!.LastAttackDamageDealt == 34,
            "Missing the native eight-revival boss preview boundary.");
        FixtureValue[] attacks = fixture.GetProperty("PreviewAttackDiagnostic").EnumerateArray().Where(record =>
            record.GetProperty("turn").GetInt32() == 8 && record.GetProperty("roomIndex").GetInt32() == 2).ToArray();
        Require(attacks.Length == 21 && attacks[^1].GetProperty("actor").GetString() == "MonsterGreedDragon" &&
            attacks[^1].GetProperty("lastDamage").GetInt32() == 34 &&
            attacks[^1].GetProperty("after").EnumerateArray().Single().GetProperty("hp").GetInt32() == 4,
            "Native preview attack order or surviving dragon changed.");
        string parent = JsonSerializer.Serialize(before);
        BattleTurnChecks.Native(fixture);
        string roomParent = JsonSerializer.Serialize(room);
        var preview = new RoomCombatState(room.RoomIndex, room.Deployment, room.Units, room.ExternalInteractions,
            actual.Spawn.Train.Context, preview: true, activeCompanionBossIds: room.ActiveCompanionBossIds);
        RoomCombatResult resolved = RoomCombatModel.Resolve(preview);
        Require(resolved.Supported && resolved.Events.Count(item => item.Kind == "Revive" && item.Target == boss.Id) == 8 &&
            resolved.State!.Context!.Statistics!.LastAttackDamageDealt == 34 &&
            resolved.State.Units.Single().AssetKey == "MonsterGreedDragon" && resolved.State.Units.Single().Health == 4,
            "Attacker upgrades interrupted an Undying victim's native room lifetime.");
        var live = new RoomCombatState(room.RoomIndex, room.Deployment, room.Units, room.ExternalInteractions,
            actual.Spawn.Train.Context, activeCompanionBossIds: room.ActiveCompanionBossIds);
        RoomCombatResult combat = RoomCombatModel.Resolve(live);
        Require(combat.Supported && combat.Outcome == RoomOutcome.BattleWon &&
            combat.Events.Count(item => item.Kind == "Revive" && item.Target == boss.Id) == 8,
            "Live combat declared victory before all Undying revivals finished.");
        Require(JsonSerializer.Serialize(before) == parent && JsonSerializer.Serialize(room) == roomParent,
            "Boss preview or live branch mutated its captured source.");
        Console.WriteLine("UNDYING-BOSS-PREVIEW PASS: exact native EndTurn/RNG, eight revivals, dragon final hit, live victory gate and source isolation.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
