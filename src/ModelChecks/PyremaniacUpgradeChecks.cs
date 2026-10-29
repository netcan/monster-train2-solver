using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PyremaniacUpgradeChecks
{
    internal static void NativeSecond(FixtureValue source)
    {
        FixtureValue[] rows = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        var birth = rows[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        var born = rows[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var upgrade = birth.Spawn.Train.Context!.FindCard(8)!.Permanent.Upgrades.Single();
        Require(CardPyremaniacUpgradeModel.Known(upgrade) && upgrade.DataId == CardPyremaniacUpgradeModel.SecondDataId &&
            !CardPyremaniacUpgradeModel.Known(upgrade.WithStats(new CardStatModifier(damage: 16, health: 20))) &&
            !CardPyremaniacUpgradeModel.KnownIdentity(upgrade.DataId, CardPyremaniacUpgradeModel.AssetKey),
            "Second tier identity and changed-signature guards differ.");
        var champion = born.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == 8);
        Require(champion is { BaseAttack: 30, Health: 40, MaxHealth: 40 } && champion.Modifiers?.RoomStatuses?.SingleOrDefault() is
            { Type: UnitRoomStatusModel.StackModifier, StatusId: "pyregel", Amount: 2 }, "Second tier native birth differs.");
        var action = rows[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var modelBirth = BattleActionModel.PlayCard(birth, action);
        Require(modelBirth.Supported && NativeBranchObservationChecks.Key(modelBirth.State!) == NativeBranchObservationChecks.Key(born),
            "Second tier birth differs from native state.");

        foreach ((int index, int expected) in new[] { (8, 4), (10, 6) })
        {
            var before = rows[index].GetProperty("Root").Deserialize<BattleTurnState>()!;
            var after = rows[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var input = rows[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            var target = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == input.TargetUnitId);
            var changed = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == input.TargetUnitId);
            Require((changed.Status("pyregel")?.Stacks ?? 0) - (target.Status("pyregel")?.Stacks ?? 0) == expected,
                "Second tier native spell or ability stack delta differs.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            Parallel.For(0, 4, _ =>
            {
                var result = BattleActionModel.PlayCard(before, input);
                Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(after),
                    "Second tier native spell or ability successor differs.");
            });
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Second tier branches mutated their parent.");
        }
        var root = rows[8].GetProperty("Root").Deserialize<BattleTurnState>()!;
        var floor = root.Spawn.Train.Rooms[0];
        foreach (int id in new[] { 7, champion.Id })
        {
            var modified = StatusApplicationModel.Apply(floor, id, new CombatStatus("pyregel", 1));
            var unmodified = StatusApplicationModel.Apply(floor, id, new CombatStatus("pyregel", 1), allowStackModifiers: false);
            Require(modified.Supported && unmodified.Supported &&
                modified.State!.Units.Single(unit => unit.Id == id).Status("pyregel")!.Stacks == 3 &&
                unmodified.State!.Units.Single(unit => unit.Id == id).Status("pyregel")!.Stacks == 1,
                "Second tier same-floor status bonus differs.");
        }
        var otherFloor = root.Spawn.Train.Rooms[1];
        var other = otherFloor.Units.First(unit => unit.Health > 0);
        var separate = StatusApplicationModel.Apply(otherFloor, other.Id, new CombatStatus("pyregel", 1));
        Require(separate.Supported && separate.State!.Units.Single(unit => unit.Id == other.Id).Status("pyregel")!.Stacks == 1,
            "Second tier aura leaked into another floor.");
        Console.WriteLine("PYREMANIAC2 PASS: original birth, spell and ability; same-floor teams, other floor, disabled modification, changed signatures and immutable branches.");
    }

    internal static void Native(FixtureValue source)
    {
        FixtureValue[] rows = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        var birth = rows[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        var born = rows[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var upgrade = birth.Spawn.Train.Context!.FindCard(3)!.Permanent.Upgrades.Single();
        Require(CardPyremaniacUpgradeModel.Known(upgrade) &&
            !CardPyremaniacUpgradeModel.Known(upgrade.WithStats(new CardStatModifier(damage: 6, health: 10))),
            "Champion upgrade identity and changed-signature guard differ.");
        var champion = born.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == 3);
        Require(champion.Modifiers?.RoomStatuses?.SingleOrDefault() is
            { Type: UnitRoomStatusModel.StackModifier, StatusId: "pyregel", Amount: 1 }, "Champion birth lost its native room modifier.");
        var birthAction = rows[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var actualBirth = BattleActionModel.PlayCard(birth, birthAction);
        Require(actualBirth.Supported && NativeBranchObservationChecks.Key(actualBirth.State!) == NativeBranchObservationChecks.Key(born),
            "Champion birth differs from native state.");

        var before = rows[7].GetProperty("Root").Deserialize<BattleTurnState>()!;
        var after = rows[7].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[7].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(action.CardInstanceId == 16 && action.TargetUnitId == 5, "Wrong original champion ability witness.");
        var targetBefore = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 5);
        var targetAfter = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 5);
        Require((targetAfter.Status("pyregel")?.Stacks ?? 0) - (targetBefore.Status("pyregel")?.Stacks ?? 0) == 5,
            "Native ability did not add four stacks plus the room bonus.");
        string original = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var replay = BattleActionModel.PlayCard(before, action);
            Require(replay.Supported && NativeBranchObservationChecks.Key(replay.State!) == NativeBranchObservationChecks.Key(after),
                "Champion ability differs from native state.");
        });
        Require(original == JsonSerializer.Serialize(before, ModelJson.Options), "Ability branches mutated their parent.");

        var room = before.Spawn.Train.Rooms[0];
        foreach (int id in new[] { 5, champion.Id })
        {
            var applied = StatusApplicationModel.Apply(room, id, new CombatStatus("pyregel", 1));
            var unmodified = StatusApplicationModel.Apply(room, id, new CombatStatus("pyregel", 1), allowStackModifiers: false);
            Require(applied.Supported && unmodified.Supported &&
                applied.State!.Units.Single(unit => unit.Id == id).Status("pyregel")!.Stacks == 2 &&
                unmodified.State!.Units.Single(unit => unit.Id == id).Status("pyregel")!.Stacks == 1,
                "Same-floor friendly/enemy status bonus or disabled modifier differs.");
        }
        var otherRoom = before.Spawn.Train.Rooms[1];
        var other = otherRoom.Units.First(unit => unit.Health > 0);
        var separate = StatusApplicationModel.Apply(otherRoom, other.Id, new CombatStatus("pyregel", 1));
        Require(separate.Supported && separate.State!.Units.Single(unit => unit.Id == other.Id).Status("pyregel")!.Stacks == 1,
            "Champion aura leaked into another floor.");
        Console.WriteLine("PYREMANIAC PASS: original champion birth and native ability; same-floor teams, other floor, disabled stack modification, immutable parallel branches and changed signature.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
