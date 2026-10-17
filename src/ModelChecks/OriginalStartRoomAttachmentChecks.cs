using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalStartRoomAttachmentChecks
{
    private const string Floor1RelicId = "2c97e1b3-d4bb-4133-9572-e582805c106f";
    private const string Floor1RelicKey = "TrainRoomInstallFloor1AtStartOfCombat";
    private const string Floor2RelicId = "b037ec04-6164-4cd8-8b9d-3bc420c29351";
    private const string Floor2RelicKey = "TrainRoomInstallFloor2AtStartOfCombat";
    private const string Floor1UpgradeId = "05e215c6-600c-4ecb-a75e-c806dd596bcf";
    private const string Floor1UpgradeKey = "FuelCar_Upgrade";
    private const string Floor2UpgradeId = "35178e4a-0880-440b-a929-de29de6e88f8";
    private const string Floor2UpgradeKey = "SpellSanctum_Upgrade";

    internal static void Native(FixtureValue root)
    {
        int schema = root.GetProperty("Schema").GetInt32();
        bool floor2 = schema == 186;
        string relicId = floor2 ? Floor2RelicId : Floor1RelicId;
        string relicKey = floor2 ? Floor2RelicKey : Floor1RelicKey;
        string upgradeId = floor2 ? Floor2UpgradeId : Floor1UpgradeId;
        string upgradeKey = floor2 ? Floor2UpgradeKey : Floor1UpgradeKey;
        int roomIndex = floor2 ? 1 : 0;
        string modifier = floor2 ? "original-start-room-attachment-floor2" : "original-start-room-attachment";
        Require((schema == 168 || floor2) &&
            root.GetProperty("OriginalStartRoomAttachmentPrepared").GetBoolean() &&
            root.GetProperty("ModifierScenario").GetString() == modifier &&
            root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("CaptureFailures").GetInt32() == 0 && root.GetProperty("Mismatches").GetInt32() == 0 &&
            root.GetProperty("Unsupported").GetInt32() == 0 && root.GetProperty("Pending").GetInt32() == 0,
            "Original start-room attachment battle is incomplete or has native/model failures.");

        FixtureValue evidence = root.GetProperty("OriginalStartRoomAttachmentCoverage");
        Require(evidence.GetProperty("Prepared").GetBoolean() &&
            evidence.GetProperty("RelicId").GetString() == relicId &&
            evidence.GetProperty("RelicKey").GetString() == relicKey &&
            evidence.GetProperty("UpgradeId").GetString() == upgradeId &&
            evidence.GetProperty("UpgradeKey").GetString() == upgradeKey &&
            Guid.TryParse(evidence.GetProperty("AttachmentGuid").GetString(), out Guid attachmentId) && attachmentId != Guid.Empty &&
            evidence.GetProperty("RoomIndex").GetInt32() == roomIndex && evidence.GetProperty("AttachmentType").GetInt32() == 0 &&
            evidence.GetProperty("AffectsHeroes").GetBoolean() && evidence.GetProperty("AffectsMonsters").GetBoolean() &&
            evidence.GetProperty("AttachmentsAddedFromCards").GetInt32() == 0 &&
            evidence.GetProperty("CapacityAtSetup").GetInt32() > 0,
            "The unchanged start-room relic did not install its cardless primary attachment with native team and counter semantics.");

        var heroes = new HashSet<int>();
        var monsters = new HashSet<int>();
        var floor2ImpactedUnits = new HashSet<int>();
        int floor2CapacityWitnesses = 0;
        foreach (FixtureValue record in root.GetProperty("Spawns").EnumerateArray())
            CheckTrain(record.GetProperty("Actual").Deserialize<EnemySpawnState>()?.Train);
        foreach (FixtureValue record in root.GetProperty("TrainPhases").EnumerateArray())
            CheckTrain(record.GetProperty("Actual").Deserialize<TrainCombatState>());
        foreach (FixtureValue record in root.GetProperty("Turns").EnumerateArray())
            CheckTrain(record.GetProperty("Actual").Deserialize<BattleTurnState>()?.Spawn.Train);
        Require(evidence.GetProperty("Error").ValueKind == FixtureKind.Null,
            "The native start-room attachment scenario reported an error.");
        if (floor2)
        {
            Require(evidence.GetProperty("RoomModifierClass").GetString() == "RoomStateSpellCostModifier" &&
                evidence.GetProperty("RoomModifierParam").GetInt32() == -1 &&
                evidence.GetProperty("SpellCostWitnessCardId").GetInt32() > 0 &&
                evidence.GetProperty("SpellCostControl").GetInt32() > 0 &&
                evidence.GetProperty("SpellCostTarget").GetInt32() == evidence.GetProperty("SpellCostControl").GetInt32() - 1 &&
                floor2CapacityWitnesses > 0 && floor2ImpactedUnits.Count > 0,
                "SpellSanctum lacks native room-cost, capacity attachment or impacted-character evidence.");
            if (evidence.GetProperty("XCostWitnessCardId").GetInt32() > 0)
                Require(evidence.GetProperty("XCostTargetModifier").GetInt32() - evidence.GetProperty("XCostControlModifier").GetInt32() == 1,
                    "SpellSanctum did not add one to the native X-spell PlayedCost modifier.");
            Console.WriteLine($"ORIGINAL-START-ROOM-ATTACHMENT-CHECKS PASS: room index 1 SpellSanctum cost -1 on card {evidence.GetProperty("SpellCostWitnessCardId").GetInt32()}, {floor2ImpactedUnits.Count} impacted-unit witnesses, {floor2CapacityWitnesses} room-state witnesses, complete winning replay.");
        }
        else
        {
            Require(heroes.Count > 0 && monsters.Count > 0,
                "Native hero and monster snapshots did not both retain the attachment GUID-attributed OnDeath trigger.");
            Console.WriteLine($"ORIGINAL-START-ROOM-ATTACHMENT-CHECKS PASS: floor 0 primary FuelCar attachment, {heroes.Count} hero and {monsters.Count} monster witnesses with matching trigger identity, complete winning battle replay.");
        }

        void CheckTrain(TrainCombatState? train)
        {
            if (train == null) return;
            foreach (RoomCombatState room in train.Rooms)
            {
                if (floor2)
                {
                    RoomCapacityState? capacity = train.Context?.RoomCapacities?.FirstOrDefault(item => item.RoomIndex == room.RoomIndex);
                    RoomCapacityAttachmentState[] attachments = (capacity?.Attachments ?? Array.Empty<RoomCapacityAttachmentState>())
                        .Where(item => item.AttachmentId == evidence.GetProperty("AttachmentGuid").GetString()).ToArray();
                    if (room.RoomIndex == roomIndex && attachments.Length > 0)
                    {
                        Require(attachments.Length == 1 && attachments[0].DataId == upgradeId &&
                            attachments[0].AssetKey == upgradeKey && attachments[0].AttachmentType == 0 &&
                            attachments[0].SpellCostDelta == -1 && attachments[0].AffectsHeroes && attachments[0].AffectsMonsters,
                            "Captured Floor2 room state does not match the native SpellSanctum attachment.");
                        floor2CapacityWitnesses++;
                    }
                    else Require(attachments.Length == 0,
                        "The SpellSanctum attachment was captured on a room other than its native target floor.");

                    foreach (CombatUnit unit in room.Units)
                    {
                        bool applied = unit.Modifiers?.Upgrades.Any(upgrade => upgrade.DataId == upgradeId && upgrade.AssetKey == upgradeKey) == true;
                        if (!applied) continue;
                        Require(room.RoomIndex == roomIndex,
                            "A SpellSanctum character upgrade remained on a unit after it left the attached room.");
                        floor2ImpactedUnits.Add(unit.Id);
                    }
                    continue;
                }

                foreach (CombatUnit unit in room.Units)
                {
                    CombatTrigger[] matching = unit.Triggers.Where(trigger => trigger.Kind == "OnDeath" &&
                        trigger.Origin?.UpgradeId == evidence.GetProperty("AttachmentGuid").GetString()).ToArray();
                    if (matching.Length == 0) continue;
                    Require(matching.Length == 1 && unit.Modifiers?.Upgrades.Any(upgrade => upgrade.DataId == upgradeId) == true,
                        "A character's attachment GUID trigger is duplicated or lacks its FuelCar applied-upgrade record.");
                    if (unit.Team == CombatTeam.Enemy) heroes.Add(unit.Id);
                    else monsters.Add(unit.Id);
                }
            }
        }
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
