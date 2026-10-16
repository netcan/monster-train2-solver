using System;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class RelicRoomAttachmentReplicationChecks
{
    private const string RelicId = "e55235f7-1d84-43f0-b8f5-037b64b8f582";
    private const string RelicKey = "FirstRoomCardOnAllFloors";
    private const string AttachmentId = "capacity-attachment";
    private const string UnitId = "subtyped-unit";
    private const string UnitSubtype = "SubtypesData_Test";

    internal static void Run()
    {
        CardInstanceState Card(int id, string dataId, string type, string[]? subtypes = null, params string[] traits)
        {
            var definition = new CardMaskDefinition(dataId, type, "Common", false, false, false,
                subtypes ?? [], [], [], [], null, false, 0, 0, false, false, false);
            CardTraitValue[] baseTraits = traits.Select(name => new CardTraitValue(name, name, 0, 0, true, 0)).ToArray();
            return new CardInstanceState(id, dataId, CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
                maskDescriptor: new CardMaskDescriptor(definition, 0, baseTraits, []));
        }

        var condition = new RelicConditionState(new CardStatisticQuery("SubtypeInDeck", "ThisBattle"), true, 2, 1, false);
        var relic = new CombatRelicState(RelicId, RelicKey, [RelicModel.FirstRoomCardOnAllFloors],
            roomAttachmentReplications: [new RelicRoomAttachmentReplication(0, 0, [condition])]);
        Require(RelicModel.Validate([relic]) == null, "Valid original room-attachment replication relic was rejected.");

        CardInstanceState attachmentCard = Card(1, AttachmentId, "TrainRoomAttachment");
        CardInstanceState unit = Card(2, UnitId, "Monster", [UnitSubtype]);
        CardInstanceState spell = Card(3, "ordinary-spell", "Spell");
        var attachment = new RoomCapacityAttachmentState(AttachmentId, "IncreaseRoomCapacity", 0, 4);
        var attachmentEffect = new CardActionEffect("AttachRoomCapacity", "Room", 0, false, true, [], roomAttachment: attachment);
        var rules = new BattlePlayRules([], [
            new CardPlayRule(AttachmentId, "IncreaseRoomCapacity", 0, "Null", "Discard", null, [],
                [attachmentEffect], cardType: "TrainRoomAttachment"),
            new CardPlayRule("ordinary-spell", "OrdinarySpell", 0, "Null", "Discard", null, [], cardType: "Spell")
        ]);
        var rng = UnityRng.Seed(1507);
        var statistics = new BattleStatistics([], [], [], [], [], [], [], 0, 0, 0, 0, 0, [1, 2, 3], [1, 2, 3], [1, 2, 3]);
        CombatContext Context(CombatRelicState relicState, CardInstanceState selectedAttachment) => new(
            new CardCycleState([new(1, AttachmentId), new(3, "ordinary-spell")], [new(2, UnitId)], [], rng, 0, []),
            rng, 0, 4, 10, statistics: statistics, cardInstances: [selectedAttachment, unit, spell],
            otherPiles: [new("Standby", []), new("Exhausted", []), new("Purged", []), new("Eaten", [])],
            relics: [relicState], roomCapacities: [new(0, 5, 5, false, attachments: [], attachmentsAddedFromCards: 0),
                new(1, 6, 6, false, attachments: [], attachmentsAddedFromCards: 0),
                new(2, 7, 7, false, attachments: [], attachmentsAddedFromCards: 0),
                new(3, 0, 0, true, attachments: [], attachmentsAddedFromCards: 0)]);

        CombatContext parent = Context(relic, attachmentCard);
        string parentJson = JsonSerializer.Serialize(parent);
        RelicCardPlayMagicPowerResult ordinary = RelicCardPlayMagicPowerModel.Apply(parent, 3, rules, selectedRoomIndex: 0);
        Require(ordinary.Supported && JsonSerializer.Serialize(ordinary.Context) == parentJson,
            "An ordinary card changed rooms or relic condition state.");

        RoomCapacityResult selectedRoom = RoomCapacityModel.Attach(parent, 0, attachment);
        Require(selectedRoom.Supported, "Could not model the selected-floor attachment before the relic callback.");
        RelicCardPlayMagicPowerResult replicated = RelicCardPlayMagicPowerModel.Apply(
            selectedRoom.Context!, 1, rules, selectedRoomIndex: 0);
        Require(replicated.Supported && replicated.Context.RoomCapacities!.Select(room => room.PlayerMaximum).SequenceEqual([9, 10, 11, 0]) &&
            replicated.Context.RoomCapacities.Select(room => room.AttachmentsAddedFromCards).SequenceEqual<int?>([1, 1, 1, 0]) &&
            replicated.Context.Relics![0].RoomAttachmentReplications![0].Conditions[0].Triggered &&
            replicated.Context.Relics[0].RoomAttachmentReplications[0].Conditions[0].ValueAtLastTrigger == 0 &&
            replicated.Context.Relics[0].RoomAttachmentReplications[0].Conditions[0].DurationTriggerCount == 1,
            "The first attachment did not replicate to all non-Pyre floors and record the native condition.");

        RoomCapacityResult replaced = RoomCapacityModel.Attach(replicated.Context, 0, attachment);
        RelicCardPlayMagicPowerResult second = RelicCardPlayMagicPowerModel.Apply(replaced.Context!, 1, rules, selectedRoomIndex: 0);
        Require(replaced.Supported && second.Supported &&
            second.Context.RoomCapacities!.Select(room => room.PlayerMaximum).SequenceEqual([9, 10, 11, 0]) &&
            second.Context.RoomCapacities.Select(room => room.AttachmentsAddedFromCards).SequenceEqual<int?>([2, 1, 1, 0]) &&
            second.Context.Relics![0].RoomAttachmentReplications![0].Conditions[0].DurationTriggerCount == 1,
            "A second attachment duplicated floor effects or retriggered the once-per-battle native condition.");

        CombatRelicState freshCondition = new(RelicId, RelicKey, [RelicModel.FirstRoomCardOnAllFloors],
            roomAttachmentReplications: [new RelicRoomAttachmentReplication(0, 0, [condition])]);
        CombatContext overLimitContext = replicated.Context.WithRelics([freshCondition]);
        RelicCardPlayMagicPowerResult overLimit = RelicCardPlayMagicPowerModel.Apply(overLimitContext, 1, rules, selectedRoomIndex: 0);
        Require(overLimit.Supported && overLimit.Context.RoomCapacities!.Select(room => room.PlayerMaximum).SequenceEqual([9, 10, 11, 0]) &&
            !overLimit.Context.Relics![0].RoomAttachmentReplications![0].Conditions[0].Triggered,
            "The independent cumulative attachment-count gate did not suppress a fresh relic condition.");

        RelicConditionState unmet = new(new CardStatisticQuery("SubtypeInDeck", "ThisBattle"), true, 2, 1, false,
            triggered: true, durationTriggerCount: 1);
        CombatRelicState gatedRelic = new(RelicId, RelicKey, [RelicModel.FirstRoomCardOnAllFloors],
            roomAttachmentReplications: [new RelicRoomAttachmentReplication(0, 0, [unmet])]);
        RoomCapacityResult gatedSelected = RoomCapacityModel.Attach(Context(gatedRelic, attachmentCard), 0, attachment);
        RelicCardPlayMagicPowerResult gated = RelicCardPlayMagicPowerModel.Apply(gatedSelected.Context!, 1, rules, selectedRoomIndex: 0);
        Require(gated.Supported && gated.Context.RoomCapacities!.Select(room => room.PlayerMaximum).SequenceEqual([9, 6, 7, 0]) &&
            gated.Context.Relics![0].RoomAttachmentReplications![0].Conditions[0].Triggered &&
            gated.Context.Relics[0].RoomAttachmentReplications[0].Conditions[0].DurationTriggerCount == 1,
            "The spent native trigger budget was cleared or allowed attachment replication.");

        CardInstanceState limited = Card(1, AttachmentId, "TrainRoomAttachment", traits: ["CardTraitLimitedRange"]);
        RoomCapacityResult limitedSelected = RoomCapacityModel.Attach(Context(relic, limited), 0, attachment);
        RelicCardPlayMagicPowerResult limitedResult = RelicCardPlayMagicPowerModel.Apply(limitedSelected.Context!, 1, rules, selectedRoomIndex: 0);
        Require(limitedResult.Supported && limitedResult.Context.RoomCapacities!.Select(room => room.PlayerMaximum).SequenceEqual([9, 6, 7, 0]) &&
            limitedResult.Context.Relics![0].RoomAttachmentReplications![0].Conditions[0].Triggered,
            "Limited Range did not suppress copies while retaining the successful native relic trigger.");

        CombatRelicState malformed = new(RelicId, RelicKey, [RelicModel.FirstRoomCardOnAllFloors]);
        Require(RelicModel.Validate([malformed])?.Contains("room-attachment replication", StringComparison.Ordinal) == true,
            "An uncaptured room-attachment relic payload passed validation.");
        Require(JsonSerializer.Serialize(parent) == parentJson, "Room-attachment branches mutated their shared parent.");
        Console.WriteLine("RELIC-ROOM-ATTACHMENT-REPLICATION-CHECKS PASS: once-per-battle trigger budget, non-Pyre copies, cumulative count gate, Limited Range, immutable parent and missing payload.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
