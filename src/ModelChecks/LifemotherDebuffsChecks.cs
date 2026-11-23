using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LifemotherDebuffsChecks
{
    internal static void MixedPreview(string observedTransferPath, string observedCleanupPath, string battlePath)
    {
        const string sourceHash = "02fd04bd37210d6e3bc947b13377d97015394bd39747612b27324b183ee164e9";
        using var sourceStream = File.OpenRead(battlePath);
        string actualSourceHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(sourceStream));
        Require(actualSourceHash.Equals(sourceHash, StringComparison.OrdinalIgnoreCase), "Expected the exact source battle.");
        CheckPaidPreview(observedTransferPath, sourceHash, reverse: false);
        CheckPaidPreview(observedCleanupPath, sourceHash, reverse: true);
        sourceStream.Position = 0;
        using var battle = FixtureDocument.Read(sourceStream);
        var (_, _, matched, unsupported) = BattleSearchChecks.RecordedFrontiers(battle.RootElement, requireZeroUnsupported: true);
        Require(matched == 11 && unsupported == 0, "Expected all recorded card frontiers.");
        BattleSearchChecks.RecordedEndTurnFrontiers(battle.RootElement, choiceBudget: 64);
        Console.WriteLine("NATIVE-LIFEMOTHER-MIXED PASS: two 11-input native branches, four ordered callbacks, 11 card frontiers, 4 end-turn frontiers.");
    }

    private static void CheckPaidPreview(string observationPath, string sourceHash, bool reverse)
    {
        using var document = FixtureDocument.Read(observationPath);
        FixtureValue observation = document.RootElement;
        FixtureValue request = observation.GetProperty("Request");
        string rejected = reverse ? "Battle preview floor 2: Lifemother death transfer requires an observed ordered debuff set." :
            "Battle preview floor 2: Lifemother death transfer requires observed Malaise-only status effects.";
        Require(request.GetProperty("SourceSHA256").GetString() == sourceHash &&
            observation.GetProperty("PlayerAdvisor").ValueKind == FixtureKind.Null &&
            observation.GetProperty("NativeSelectionRejection").ValueKind == FixtureKind.Null,
            "Expected the original development capture and exact registered source battle.");
        FixtureValue[] inputs = NativeBranchObservationChecks.Replay(observation, rejected,
            expectedGameModule: Guid.Parse(Game231FixtureCatalog.Module), requirePlayerProjection: false);
        Require(inputs.Length == 11 && request.GetProperty("Action").Deserialize<PlayCardAction>() is
            { CardInstanceId: 31, RoomIndex: 2 } action &&
            action.TargetUnitId == (reverse ? 14 : 3), "Expected the original paid Torch alternative.");

        FixtureValue[] callbacks = observation.GetProperty("LifemotherMixedTransfers").EnumerateArray().ToArray();
        Require(callbacks.Length == 2, "Expected direct transfer and recipient-free preview cleanup.");
        for (int index = 0; index < callbacks.Length; index++)
        {
            FixtureValue callback = callbacks[index];
            FixtureValue before = callback.GetProperty("Before"), actual = callback.GetProperty("Actual");
            var boss = before.GetProperty("Boss").Deserialize<CombatUnit>()!;
            var effect = callback.GetProperty("Effect").Deserialize<CardActionEffect>()!;
            const string death = "OnAnyMonsterDeathOnFloor";
            Require(callback.GetProperty("Kind").GetString() == death && callback.GetProperty("Turn").GetInt32() == 3 &&
                callback.GetProperty("SourceRoom").GetInt32() == 2 && callback.GetProperty("SelectedRoom").GetInt32() == 2 &&
                TriggeredStatusTransferModel.IsLifemotherDeath(boss, death, effect) &&
                before.GetProperty("Battle").ContentEquals(actual.GetProperty("Battle")) &&
                before.GetProperty("BattleTest").ContentEquals(actual.GetProperty("BattleTest")),
                "Original mixed transfer owner, callback, room or RNG changed.");
            var source = before.GetProperty("Targets")[0].Deserialize<CombatUnit>()!;
            var after = actual.GetProperty("Targets")[0].Deserialize<CombatUnit>()!;
            (string Id, int Stacks)[] orderedStatuses = (reverse, index) switch
            {
                (true, 0) => [("malaise", 24)],
                (true, 1) => [("pyregel", 2), ("malaise", 24)],
                _ => [("malaise", 24), ("pyregel", 2)]
            };
            Require(source.Id == (index == 0 ? 3 : 14) && after.Id == source.Id && source.Health == 0 && after.Health == 0 &&
                source.StatusDictionary!.Order(source.StatusRegistry!).Where(status => status.DisplayCategory == "Negative" && status.Stacks > 0)
                    .Select(status => (status.Id, status.Stacks)).SequenceEqual(orderedStatuses) &&
                after.Status("malaise") is not { Stacks: > 0 } && after.Status("pyregel") is not { Stacks: > 0 } &&
                after.MaxHealth == source.MaxHealth + 24, "The retained dying actor lost a different debuff set.");
            var beforeRoster = before.GetProperty("FriendlyRoster").Deserialize<CombatUnit[]>()!;
            var actualRoster = actual.GetProperty("FriendlyRoster").Deserialize<CombatUnit[]>()!;
            Require(index == 0
                ? beforeRoster is [{ Id: 14, MaxHealth: 25 } priorRecipient] && priorRecipient.Health == (reverse ? 24 : 25) &&
                  actualRoster is [{ Id: 14, Health: 1, MaxHealth: 1 } receiver] &&
                  receiver.Status("malaise") is { Stacks: 24 } && receiver.Status("pyregel") is { Stacks: 2 } &&
                  (priorRecipient.Status("pyregel")?.Stacks ?? 0) == (reverse ? 2 : 0)
                : beforeRoster.Length == 0 && actualRoster.Length == 0,
                "Expected the exact live recipient followed by its recipient-free death.");
        }
    }

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
