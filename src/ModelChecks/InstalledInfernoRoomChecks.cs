using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InstalledInfernoRoomChecks
{
    internal static void Native(FixtureValue root)
    {
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(counter).GetInt32() == 0, "Nonzero native " + counter + ".");
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() && root.GetProperty("NativeWon").GetBoolean(),
            "Expected a settled original 2.3.1 victory.");

        FixtureValue evidence = root.GetProperty("OriginalInstalledInfernoRooms");
        FixtureValue definition = evidence.GetProperty("Before");
        Require(definition.ContentEquals(evidence.GetProperty("After")) &&
            definition.GetProperty("Id").GetString() == RelicStartCombatAttachmentModel.InfernoMutatorId &&
            definition.GetProperty("Key").GetString() == RelicStartCombatAttachmentModel.InfernoMutatorKey &&
            !definition.GetProperty("PlacementExcluded").GetBoolean(),
            "The original Inferno definition changed.");

        FixtureValue[] effects = definition.GetProperty("Effects").EnumerateArray().ToArray();
        FixtureValue[] roomWitnesses = evidence.GetProperty("Rooms").EnumerateArray().ToArray();
        Require(effects.Length == 3 && roomWitnesses.Length == 3, "Expected three original effects and settled room attachments.");
        var attachmentIds = new HashSet<Guid>();
        for (int floor = 0; floor < 3; floor++)
        {
            FixtureValue effect = effects[floor];
            FixtureValue room = roomWitnesses[floor];
            Require(effect.GetProperty("Type").GetString() == RelicStartCombatAttachmentModel.Effect &&
                effect.GetProperty("RoomIndex").GetInt32() == floor && effect.GetProperty("AttachType").GetInt32() == 0 &&
                effect.GetProperty("SourceTeam").GetString() == "None" && effect.GetProperty("Conditions").GetInt32() == 0 &&
                effect.GetProperty("UpgradeId").GetString() == RelicStartCombatAttachmentModel.InfernoUpgradeId &&
                effect.GetProperty("UpgradeKey").GetString() == RelicStartCombatAttachmentModel.InfernoUpgradeKey,
                "Original Inferno effect order or target changed.");
            FixtureValue modifier = effect.GetProperty("Modifiers").EnumerateArray().Single();
            FixtureValue damage = modifier.GetProperty("Effects").EnumerateArray().Single();
            Require(modifier.GetProperty("Type").GetString() == "RoomStateAddEffectPreCombatModifier" &&
                modifier.GetProperty("DisallowedInDeploymentPhase").GetBoolean() &&
                damage.GetProperty("Type").GetString() == "CardEffectDamage" &&
                damage.GetProperty("Target").GetString() == "Room" &&
                damage.GetProperty("Team").GetString() == "Heroes, Monsters" &&
                damage.GetProperty("Int").GetInt32() == 50 && !damage.GetProperty("UseRange").GetBoolean(),
                "Original Inferno installed damage changed.");
            RoomCapacityAttachmentState projection = room.GetProperty("Projection").Deserialize<RoomCapacityAttachmentState>()!;
            Require(room.GetProperty("RoomIndex").GetInt32() == floor && room.GetProperty("AttachType").GetInt32() == 0 &&
                room.GetProperty("Team").GetString() == "Heroes, Monsters" &&
                room.GetProperty("UpgradeId").GetString() == RelicStartCombatAttachmentModel.InfernoUpgradeId &&
                room.GetProperty("AttachmentsAddedFromCards").GetInt32() == 0 &&
                projection.AttachmentId == room.GetProperty("Guid").GetString() &&
                Guid.TryParse(projection.AttachmentId, out Guid identity) && identity != Guid.Empty && attachmentIds.Add(identity) &&
                projection.DataId == RelicStartCombatAttachmentModel.InfernoUpgradeId && RoomAttachmentUnitModel.InfernoPayload(projection),
                "Opening Inferno did not capture three original cardless room installations.");
        }

        FixtureValue actions = root.GetProperty("Actions");
        CombatContext context = (actions.GetArrayLength() > 0 ? actions : root.GetProperty("Turns"))[0]
            .GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
        Require(RelicStartCombatAttachmentModel.Validate(context) == null &&
            context.Relics!.Count(relic => relic.DataId == RelicStartCombatAttachmentModel.InfernoMutatorId) == 1,
            "The advisor did not receive the settled original owner.");
        RoomCapacityState[] missingFloor = context.RoomCapacities!.Where(room => room.RoomIndex != 1).ToArray();
        Require(RelicStartCombatAttachmentModel.Validate(context.WithRoomCapacities(missingFloor)) != null,
            "The advisor admitted a missing opening Inferno floor.");
        var missingEffect = new CombatRelicState(RelicStartCombatAttachmentModel.InfernoMutatorId,
            RelicStartCombatAttachmentModel.InfernoMutatorKey, [RelicStartCombatAttachmentModel.Effect,
                RelicStartCombatAttachmentModel.Effect], isCovenant: false, disallowedInPlacementPhase: false);
        Require(RelicStartCombatAttachmentModel.Validate(missingEffect) != null,
            "The advisor admitted an incomplete opening Inferno effect roster.");

        var hitFloors = new HashSet<int>();
        var hitTeams = new HashSet<CombatTeam>();
        int exactHits = 0;
        foreach (FixtureValue record in root.GetProperty("RoomPreCombatRecords").EnumerateArray())
        {
            if (!record.GetProperty("Completed").GetBoolean() || record.GetProperty("Turn").GetInt32() == 0) continue;
            int floor = record.GetProperty("RoomIndex").GetInt32();
            TrainCombatState before = record.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState after = record.GetProperty("After").Deserialize<TrainCombatState>()!;
            RoomCombatState beforeRoom = before.Rooms.Single(room => room.RoomIndex == floor);
            RoomCombatState afterRoom = after.Rooms.Single(room => room.RoomIndex == floor);
            foreach (CombatUnit target in beforeRoom.Units)
            {
                CombatUnit? actual = afterRoom.Units.FirstOrDefault(unit => unit.Id == target.Id);
                if (actual == null || target.Health - actual.Health != 50) continue;
                hitFloors.Add(floor);
                hitTeams.Add(target.Team);
                exactHits++;
            }
        }
        Require(hitFloors.Count > 0 && exactHits > 0, "No native Inferno pre-combat 50 damage was observed.");
        Console.WriteLine($"INSTALLED-INFERNO PASS: three original ordered effects and room attachments, {exactHits} native exact-50 hits " +
            $"on floors {string.Join(",", hitFloors.Order())} and teams {string.Join(",", hitTeams.Order())}.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
