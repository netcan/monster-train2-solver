using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalStartRoomAttachmentChecks
{
    private const string RelicId = "2c97e1b3-d4bb-4133-9572-e582805c106f";
    private const string UpgradeId = "05e215c6-600c-4ecb-a75e-c806dd596bcf";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 168 &&
            root.GetProperty("OriginalStartRoomAttachmentPrepared").GetBoolean() &&
            root.GetProperty("ModifierScenario").GetString() == "original-start-room-attachment" &&
            root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("CaptureFailures").GetInt32() == 0 && root.GetProperty("Mismatches").GetInt32() == 0 &&
            root.GetProperty("Unsupported").GetInt32() == 0 && root.GetProperty("Pending").GetInt32() == 0,
            "Original floor-one start attachment battle is incomplete or has native/model failures.");

        FixtureValue evidence = root.GetProperty("OriginalStartRoomAttachmentCoverage");
        Require(evidence.GetProperty("Prepared").GetBoolean() &&
            evidence.GetProperty("RelicId").GetString() == RelicId &&
            evidence.GetProperty("RelicKey").GetString() == "TrainRoomInstallFloor1AtStartOfCombat" &&
            evidence.GetProperty("UpgradeId").GetString() == UpgradeId &&
            evidence.GetProperty("UpgradeKey").GetString() == "FuelCar_Upgrade" &&
            Guid.TryParse(evidence.GetProperty("AttachmentGuid").GetString(), out Guid attachmentId) && attachmentId != Guid.Empty &&
            evidence.GetProperty("RoomIndex").GetInt32() == 0 && evidence.GetProperty("AttachmentType").GetInt32() == 0 &&
            evidence.GetProperty("AffectsHeroes").GetBoolean() && evidence.GetProperty("AffectsMonsters").GetBoolean() &&
            evidence.GetProperty("AttachmentsAddedFromCards").GetInt32() == 0 &&
            evidence.GetProperty("CapacityAtSetup").GetInt32() > 0,
            "The unchanged floor-one relic did not install its cardless primary FuelCar attachment with native team and counter semantics.");

        var heroes = new HashSet<int>();
        var monsters = new HashSet<int>();
        foreach (FixtureValue record in root.GetProperty("Spawns").EnumerateArray())
            CheckTrain(record.GetProperty("Actual").Deserialize<EnemySpawnState>()?.Train);
        foreach (FixtureValue record in root.GetProperty("TrainPhases").EnumerateArray())
            CheckTrain(record.GetProperty("Actual").Deserialize<TrainCombatState>());
        foreach (FixtureValue record in root.GetProperty("Turns").EnumerateArray())
            CheckTrain(record.GetProperty("Actual").Deserialize<BattleTurnState>()?.Spawn.Train);
        Require(heroes.Count > 0 && monsters.Count > 0 && evidence.GetProperty("Error").ValueKind == FixtureKind.Null,
            "Native hero and monster snapshots did not both retain the attachment GUID-attributed OnDeath trigger.");
        Console.WriteLine($"ORIGINAL-START-ROOM-ATTACHMENT-CHECKS PASS: floor 0 primary FuelCar attachment, {heroes.Count} hero and {monsters.Count} monster witnesses with matching trigger identity, complete winning battle replay.");

        void CheckTrain(TrainCombatState? train)
        {
            if (train == null) return;
            foreach (CombatUnit unit in train.Rooms.SelectMany(room => room.Units))
            {
                CombatTrigger[] matching = unit.Triggers.Where(trigger => trigger.Kind == "OnDeath" &&
                    trigger.Origin?.UpgradeId == evidence.GetProperty("AttachmentGuid").GetString()).ToArray();
                if (matching.Length == 0) continue;
                Require(matching.Length == 1 && unit.Modifiers?.Upgrades.Any(upgrade => upgrade.DataId == UpgradeId) == true,
                    "A character's attachment GUID trigger is duplicated or lacks its FuelCar applied-upgrade record.");
                if (unit.Team == CombatTeam.Enemy) heroes.Add(unit.Id);
                else monsters.Add(unit.Id);
            }
        }
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
