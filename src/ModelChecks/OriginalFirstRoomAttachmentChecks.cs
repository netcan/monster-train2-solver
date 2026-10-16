using System;
using System.Linq;
using MonsterTrain2Poju.Fixtures;

internal static class OriginalFirstRoomAttachmentChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 167 &&
            root.GetProperty("OriginalFirstRoomAttachmentPrepared").GetBoolean() &&
            root.GetProperty("ModifierScenario").GetString() == "original-first-room-attachment" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("CaptureFailures").GetInt32() == 0 &&
            root.GetProperty("Mismatches").GetInt32() == 0 && root.GetProperty("Unsupported").GetInt32() == 0 &&
            root.GetProperty("Pending").GetInt32() == 0,
            "Original FirstRoomCardOnAllFloors battle is incomplete or has native/model failures.");

        FixtureValue evidence = root.GetProperty("OriginalFirstRoomAttachmentCoverage");
        int[] AttachmentCards = evidence.GetProperty("AttachmentCardInstanceIds").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        int[] PyreRooms = evidence.GetProperty("PyreRoomIndexes").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        int[] InitialCapacity = Values("InitialCapacities");
        int[] InitialCount = Values("InitialAttachmentCounts");
        int[] NormalCapacity = Values("NormalAfterCapacities");
        int[] NormalCount = Values("NormalAfterAttachmentCounts");
        int[] FirstCapacity = Values("FirstAttachmentAfterCapacities");
        int[] FirstCount = Values("FirstAttachmentAfterCounts");
        int[] SecondCapacity = Values("SecondAttachmentAfterCapacities");
        int[] SecondCount = Values("SecondAttachmentAfterCounts");
        int selectedRoom = evidence.GetProperty("RoomIndex").GetInt32();
        int controlRoom = evidence.GetProperty("ControlRoomIndex").GetInt32();
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray()
            .Where(entry => entry.GetProperty("ScenarioAction").GetBoolean()).ToArray();

        Require(evidence.GetProperty("Prepared").GetBoolean() &&
            evidence.GetProperty("NormalCompleted").GetBoolean() &&
            evidence.GetProperty("FirstAttachmentCompleted").GetBoolean() &&
            evidence.GetProperty("SecondAttachmentCompleted").GetBoolean() &&
            AttachmentCards.Length == 2 && selectedRoom != controlRoom && actions.Length == 3 &&
            evidence.GetProperty("ConditionTriggerLimit").GetInt32() == 1 &&
            evidence.GetProperty("ConditionTriggerCountAtSetup").GetInt32() == 0 &&
            evidence.GetProperty("EnergyAtSetup").GetInt32() >= 8,
            "The condition setup and three controlled card plays did not complete.");
        Require(NormalCapacity.SequenceEqual(InitialCapacity) && NormalCount.SequenceEqual(InitialCount) &&
            !evidence.GetProperty("ConditionTriggeredAfterControl").GetBoolean(),
            "The ordinary-card negative control changed floors or triggered the relic.");
        Require(FirstCapacity.Length == InitialCapacity.Length && FirstCount.Length == InitialCount.Length &&
            FirstCapacity.Where((_, index) => !PyreRooms.Contains(index))
                .SequenceEqual(InitialCapacity.Where((_, index) => !PyreRooms.Contains(index)).Select(value => value + 4)) &&
            FirstCapacity.Where((_, index) => PyreRooms.Contains(index))
                .SequenceEqual(InitialCapacity.Where((_, index) => PyreRooms.Contains(index))) &&
            FirstCount.Where((_, index) => !PyreRooms.Contains(index)).All(value => value == 1) &&
            FirstCount.Where((_, index) => PyreRooms.Contains(index)).All(value => value == 0),
            "The first native attachment did not add four capacity and one card counter on each non-Pyre floor.");
        Require(evidence.GetProperty("ConditionTriggeredAfterFirstAttachment").GetBoolean() &&
            evidence.GetProperty("ConditionTriggerCountAfterFirstAttachment").GetInt32() == 1 &&
            evidence.GetProperty("ConditionValueAtFirstTrigger").GetInt32() == 0,
            "The relic did not record exactly one successful native condition trigger.");
        Require(SecondCapacity.SequenceEqual(FirstCapacity) && SecondCount[selectedRoom] == 2 &&
            SecondCount.Where((_, index) => index != selectedRoom && !PyreRooms.Contains(index)).All(value => value == 1) &&
            SecondCount.Where((_, index) => PyreRooms.Contains(index)).All(value => value == 0) &&
            evidence.GetProperty("ConditionTriggeredAfterSecondAttachment").GetBoolean() &&
            evidence.GetProperty("ConditionTriggerCountAfterSecondAttachment").GetInt32() == 1,
            "The later attachment changed replicated floors or retriggered the relic.");
        Require(evidence.GetProperty("Error").ValueKind == FixtureKind.Null,
            "The native scenario recorded an internal coverage error.");
        Console.WriteLine("ORIGINAL-FIRST-ROOM-ATTACHMENT-CHECKS PASS: original once-per-battle condition, ordinary negative, first attachment replication, cumulative counters and second-attachment gate.");

        int[] Values(string name) => evidence.GetProperty(name).EnumerateArray().Select(item => item.GetInt32()).ToArray();
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
