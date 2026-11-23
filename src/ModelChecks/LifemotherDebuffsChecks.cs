using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LifemotherDebuffsChecks
{
    internal static void Native(string path)
    {
        using var document = FixtureDocument.Read(path);
        var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("Boundary").GetString() == "OriginalLifemotherDebuffs" &&
            root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            root.GetProperty("Scenario").GetString() == "SoulSavior_R5_BossBattle_Lifemother_Debuffs" &&
            root.GetProperty("ScenarioId").GetString() == "9928005f-f7da-42c7-887a-b26b38ae8bcb" &&
            root.GetProperty("BossId").GetString() == "9dc77d80-0ab2-471e-af73-135080c7d4a6" &&
            root.GetProperty("OwnerId").GetString() == "93efdaa9-7496-46d3-ae86-7a08223b8f94" &&
            root.GetProperty("Complete").GetBoolean(), "Expected the pinned original Lifemother Debuffs capture.");
        FixtureValue[] rows = root.GetProperty("Applications").EnumerateArray().ToArray();
        string[] kinds = ["OnTeamTurnBegin", "OnAttacking", "OnAttacking", "OnAnyMonsterDeathOnFloor"];
        Require(rows.Length == kinds.Length, "Expected four ordered native callbacks.");
        for (int index = 0; index < rows.Length; index++) Replay(rows[index], index, kinds[index]);
        Console.WriteLine("NATIVE-LIFEMOTHER-DEBUFFS PASS: zero Dazed, two native Malaise applications, original death transfer, and unchanged RNG.");
    }

    private static void Replay(FixtureValue row, int index, string kind)
    {
        Require(row.GetProperty("Sequence").GetInt32() == index + 1 && row.GetProperty("Turn").GetInt32() == 1 &&
            row.GetProperty("SourceRoom").GetInt32() == 2 && row.GetProperty("SelectedRoom").GetInt32() == 2 &&
            row.GetProperty("Kind").GetString() == kind, "Original Lifemother callback order or floor changed.");
        FixtureValue before = row.GetProperty("Before"), actual = row.GetProperty("Actual");
        Require(before.GetProperty("Battle").ContentEquals(actual.GetProperty("Battle")) &&
            before.GetProperty("BattleTest").ContentEquals(actual.GetProperty("BattleTest")), "A native callback advanced RNG.");
        CombatUnit boss = before.GetProperty("Boss").Deserialize<CombatUnit>()!;
        CombatUnit source = before.GetProperty("Targets")[0].Deserialize<CombatUnit>()!;
        CombatUnit observedSource = actual.GetProperty("Targets")[0].Deserialize<CombatUnit>()!;
        CombatUnit[] observedRoster = actual.GetProperty("FriendlyRoster").Deserialize<CombatUnit[]>()!;
        var room = before.GetProperty("Room").Deserialize<RoomCombatState>()!;
        var nativeRoom = actual.GetProperty("Room").Deserialize<RoomCombatState>()!;
        var payload = row.GetProperty("Effect").Deserialize<CardActionEffect>()!;
        Require(boss.AssetKey == TriggeredStatusTransferModel.LifemotherDebuffer && observedSource.Id == source.Id &&
            boss.Triggers.Single(trigger => trigger.Kind == kind).Effects.Single().Action is { } triggerAction &&
            Serialize(triggerAction) == Serialize(payload), "The captured original effect or actor changed.");
        int ExpectedMalaise(CombatUnit unit) => unit.Status("malaise")?.Stacks ?? 0;
        if (index == 0)
            Require(source.Id == boss.Id && payload.Statuses is [{ Id: "dazed", Stacks: 5 }] && source.Status("dazed") == null &&
                observedSource.Status("dazed") == null, "Expected a zero-Dazed native control.");
        else if (kind == "OnAttacking")
            Require(source.Id == index + 1 && ExpectedMalaise(source) == 0 && ExpectedMalaise(observedSource) == 12 &&
                observedSource.MaxHealth == source.MaxHealth - 12 && observedSource.Health == source.Health,
                "Expected original Malaise on the attacked actor and the Trample actor.");
        else Require(source.Id == 2 && source.Health == 0 && ExpectedMalaise(source) == 12 && ExpectedMalaise(observedSource) == 0 &&
            observedSource.MaxHealth == 20 && observedRoster is [{ Id: 3, Health: 10, MaxHealth: 16 } receiver] &&
            ExpectedMalaise(receiver) == 24, "Expected native transfer from retained champion to the surviving front Avenger.");

        string immutable = Serialize(room);
        RoomCombatState capturedWorldRoom = room.Context!.Enchantments!.Rooms.Single(floor => floor.RoomIndex == room.RoomIndex);
        Require(room.ExternalInteractions is ["Outer train boss"] &&
            nativeRoom.ExternalInteractions.SequenceEqual(room.ExternalInteractions) && capturedWorldRoom.ExternalInteractions.Count == 0,
            "Expected the separate outer scheduler marker in raw room capture.");
        // Replay the already selected callback with the room metadata captured
        // in its shared world. The outer scheduler is outside this observation.
        var scope = new RoomCombatState(room.RoomIndex, room.Deployment, room.Units, capturedWorldRoom.ExternalInteractions,
            room.Context, room.Preview, room.ActiveCompanionBossIds);
        var queue = new List<RoomCombatModel.QueuedCharacterTrigger>();
        var callback = new RoomCombatModel.QueuedCharacterTrigger(room.RoomIndex, boss, kind,
            overrideTarget: kind == "OnAttacking" ? source : null,
            dyingCharacter: kind == "OnAnyMonsterDeathOnFloor" ? source : null);
        RoomCombatResult result = RoomCombatModel.ApplyQueuedCharacterTrigger(scope, callback, queue.Add);
        Require(result.Supported, kind + " failed: " + result.UnsupportedReason);
        CombatUnit[] replayed = result.State!.Units.Concat(result.RetainedUnits).ToArray();
        foreach (CombatUnit unit in observedRoster.Concat(new[] { observedSource, actual.GetProperty("Boss").Deserialize<CombatUnit>()! }).DistinctBy(unit => unit.Id))
            Match(replayed.First(expected => expected.Id == unit.Id), unit, "actor " + unit.Id);
        Match(result.State.Context, nativeRoom.Context, "context and RNG");
        Require(Serialize(room) == immutable, "Native replay changed its input.");

        void Match<T>(T expected, T observed, string subject)
        {
            string? difference = ModelJson.Difference(Serialize(expected), Serialize(observed));
            Require(difference == null, $"Lifemother {index + 1} {kind} differs in {subject} at {difference}.");
        }
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}
