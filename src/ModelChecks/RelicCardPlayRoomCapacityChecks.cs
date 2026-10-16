using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicCardPlayRoomCapacityChecks
{
    private const string RelicId = "19e15240-37ca-460d-8230-693626fc7f7e";
    private const string RelicKey = "CarriageJack";
    private const string AttachmentId = "attachment";

    internal static void Run()
    {
        var relic = new CombatRelicState(RelicId, RelicKey, [RelicModel.ModifyRoomCapacityOnCardPlay],
            cardPlayRoomCapacityModifiers: [new RelicCardPlayRoomCapacityModifier(0, 1, "TrainRoomAttachment")]);
        Require(RelicModel.Validate([relic]) == null, "Valid card-play room-capacity relic was rejected.");

        CardMaskDescriptor Descriptor(string id, string type) => new(
            new CardMaskDefinition(id, type, "Common", false, false, false, [], [], [], [], null,
                false, 0, 0, false, false, false), 1, [], []);
        CardInstanceState Card(int id, string dataId, string type) => new(id, dataId,
            CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            maskDescriptor: Descriptor(dataId, type));

        CardInstanceState attachment = Card(1, AttachmentId, "TrainRoomAttachment");
        CardInstanceState spell = Card(2, "spell", "Spell");
        UnityRng rng = UnityRng.Seed(151);
        CombatContext context = new(new CardCycleState([new(1, AttachmentId), new(2, "spell")], [], [], rng, 0, []),
            rng, 0, 3, 10, cardInstances: [attachment, spell], relics: [relic],
            roomCapacities: [new RoomCapacityState(0, 5, 4, false), new RoomCapacityState(1, 6, 7, false)]);
        var definitions = new BattlePlayRules([], [
            new CardPlayRule(AttachmentId, "attachment", 0, "Null", "Discard", null, [], cardType: "TrainRoomAttachment"),
            new CardPlayRule("spell", "spell", 0, "Null", "Discard", null, [], cardType: "Spell")
        ]);
        string parent = JsonSerializer.Serialize(context);

        RelicCardPlayMagicPowerResult attachmentPlayed = RelicCardPlayMagicPowerModel.Apply(
            context, attachment.InstanceId, definitions, selectedRoomIndex: 1);
        Require(attachmentPlayed.Supported && attachmentPlayed.Context.RoomCapacities!.Single(room => room.RoomIndex == 1).PlayerMaximum == 7 &&
            attachmentPlayed.Context.RoomCapacities.Single(room => room.RoomIndex == 1).EnemyMaximum == 7 &&
            attachmentPlayed.Context.RoomCapacities.Single(room => room.RoomIndex == 0).PlayerMaximum == 5,
            "The matching attachment did not add capacity only to the selected player floor.");

        RelicCardPlayMagicPowerResult ordinaryPlayed = RelicCardPlayMagicPowerModel.Apply(
            context, spell.InstanceId, definitions, selectedRoomIndex: 1);
        Require(ordinaryPlayed.Supported && JsonSerializer.Serialize(ordinaryPlayed.Context) == parent,
            "A nonmatching card type changed capacity or mutated its parent.");
        RelicCardPlayMagicPowerResult noRoom = RelicCardPlayMagicPowerModel.Apply(context, attachment.InstanceId, definitions);
        Require(!noRoom.Supported && noRoom.UnsupportedReason!.Contains("selected room", StringComparison.Ordinal),
            "A matching card with no selected-room evidence was silently accepted.");

        CombatRelicState malformed = new(RelicId, RelicKey, [RelicModel.ModifyRoomCapacityOnCardPlay]);
        Require(RelicModel.Validate([malformed])?.Contains("ordered native card-play room-capacity", StringComparison.Ordinal) == true,
            "An uncaptured native card-play room-capacity payload passed validation.");
        Require(relic.WithRoomEntryStatuses([]).CardPlayRoomCapacityModifiers?.Single().Amount == 1,
            "A relic-state update discarded its card-play room-capacity definition.");
        Console.WriteLine("RELIC-CARD-PLAY-ROOM-CAPACITY-CHECKS PASS: ordered definition, exact card-type gate, selected floor, player-only adjustment, missing selection and immutable branches.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 165 && root.GetProperty("OriginalCarriageJackPrepared").GetBoolean() &&
            root.GetProperty("ModifierScenario").GetString() == "original-carriage-jack" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("CaptureFailures").GetInt32() == 0 &&
            root.GetProperty("Mismatches").GetInt32() == 0 && root.GetProperty("Unsupported").GetInt32() == 0 &&
            root.GetProperty("Pending").GetInt32() == 0,
            "Original CarriageJack battle is incomplete or has native/model failures.");

        FixtureValue evidence = root.GetProperty("OriginalCarriageJackCoverage");
        string attachmentId = evidence.GetProperty("AttachmentCardDataId").GetString()!;
        int roomIndex = evidence.GetProperty("RoomIndex").GetInt32();
        int normalRoomIndex = evidence.GetProperty("NormalRoomIndex").GetInt32();
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray()
            .Where(entry => entry.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(evidence.GetProperty("Prepared").GetBoolean() && evidence.GetProperty("NormalCompleted").GetBoolean() &&
            evidence.GetProperty("AttachmentCompleted").GetBoolean() && roomIndex != normalRoomIndex && actions.Length == 2,
            "The controlled ordinary-card and attachment plays did not both complete.");
        Require(evidence.GetProperty("NormalAfterCapacity").GetInt32() == evidence.GetProperty("NormalInitialCapacity").GetInt32(),
            "The ordinary-card negative control changed capacity on its own floor.");
        Require(evidence.GetProperty("ReroutedDeployments").GetInt32() > 0,
            "The native battle did not exercise post-attachment floor avoidance.");

        int attachmentActionIndex = actions[1].GetProperty("Index").GetInt32();
        foreach (FixtureValue entry in root.GetProperty("Actions").EnumerateArray()
            .Where(entry => entry.GetProperty("Index").GetInt32() > attachmentActionIndex))
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            int cardId = entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            CardInstanceState? card = before.Spawn.Train.Context!.FindCard(cardId);
            CardPlayRule? rule = card == null ? null : before.PlayRules!.Cards.FirstOrDefault(item => item.DataId == card.DataId);
            Require(rule?.Effect != "SpawnMonster" || entry.GetProperty("Action").GetProperty("RoomIndex").GetInt32() != roomIndex,
                "The native battle deployed a player unit into the attached room after CarriageJack coverage was recorded.");
        }

        int ordinary = 0, attachment = 0;
        for (int index = 0; index < actions.Length; index++)
        {
            BattleTurnState before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatContext beforeContext = before.Spawn.Train.Context!;
            CombatContext actualContext = actual.Spawn.Train.Context!;
            int cardInstanceId = actions[index].GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            CardInstanceState played = beforeContext.FindCard(cardInstanceId)!;
            CombatRelicState relic = beforeContext.Relics!.Single(item => item.DataId == RelicId);
            Require(relic.AssetKey == RelicKey && relic.EffectTypes.SequenceEqual([RelicModel.ModifyRoomCapacityOnCardPlay]) &&
                relic.CardPlayRoomCapacityModifiers?.Count == 1 && relic.CardPlayRoomCapacityModifiers[0].Amount == 1 &&
                relic.CardPlayRoomCapacityModifiers[0].CardType == "TrainRoomAttachment",
                "The battle did not retain the unchanged CarriageJack effect definition.");

            RoomCapacityState beforeRoom = beforeContext.RoomCapacities!.Single(room => room.RoomIndex == roomIndex);
            RoomCapacityState afterRoom = actualContext.RoomCapacities!.Single(room => room.RoomIndex == roomIndex);
            if (played.DataId == attachmentId)
            {
                Require(index == 1 && actions[index].GetProperty("Action").GetProperty("RoomIndex").GetInt32() == roomIndex &&
                    before.PlayRules!.Cards.Single(card => card.DataId == attachmentId).CardType == "TrainRoomAttachment" &&
                    afterRoom.PlayerMaximum == beforeRoom.PlayerMaximum + 5 && afterRoom.EnemyMaximum == beforeRoom.EnemyMaximum,
                    "The native attachment did not combine its +4 with CarriageJack's +1 on the selected floor.");
                attachment++;
            }
            else
            {
                RoomCapacityState normalBefore = beforeContext.RoomCapacities!.Single(room => room.RoomIndex == normalRoomIndex);
                RoomCapacityState normalAfter = actualContext.RoomCapacities!.Single(room => room.RoomIndex == normalRoomIndex);
                Require(index == 0 && actions[index].GetProperty("Action").GetProperty("RoomIndex").GetInt32() == normalRoomIndex &&
                    played.MaskDescriptor?.Definition.CardType != "TrainRoomAttachment" &&
                    normalAfter.PlayerMaximum == normalBefore.PlayerMaximum && normalAfter.EnemyMaximum == normalBefore.EnemyMaximum &&
                    afterRoom.PlayerMaximum == beforeRoom.PlayerMaximum && afterRoom.EnemyMaximum == beforeRoom.EnemyMaximum,
                    "A non-attachment card incorrectly triggered CarriageJack on its floor or the separate attachment floor.");
                ordinary++;
            }
            foreach (RoomCapacityState room in beforeContext.RoomCapacities.Where(room => room.RoomIndex != roomIndex))
            {
                RoomCapacityState? unchanged = actualContext.RoomCapacities.SingleOrDefault(item => item.RoomIndex == room.RoomIndex);
                Require(unchanged?.PlayerMaximum == room.PlayerMaximum && unchanged.EnemyMaximum == room.EnemyMaximum,
                    "CarriageJack changed capacity on a floor other than the selected room.");
            }
        }
        Require(ordinary == 1 && attachment == 1, "Missing native positive or negative card-type control.");
        BattleActionChecks.Native(root);
        Console.WriteLine("NATIVE-CARRIAGE-JACK-CHECKS PASS: ordinary-card negative and native TrainRoomAttachment positive (+4 card, +1 relic) on the selected floor.");
    }

    internal static PlayCardAction? Choose(BattleTurnState source, int attachmentRoomIndex)
    {
        PlayCardAction? preferred = BattleActionModel.ChooseUnitAndJunkPlay(source);
        if (preferred == null || preferred.RoomIndex != attachmentRoomIndex) return preferred;
        CardInstanceState? card = source.Spawn.Train.Context?.FindCard(preferred.CardInstanceId);
        CardPlayRule? rule = card == null ? null : source.PlayRules?.Cards.FirstOrDefault(item => item.DataId == card.DataId);
        if (rule?.Effect != "SpawnMonster") return preferred;
        IReadOnlyList<PlayCardAction> plays = BattleActionModel.EnumerateSupportedPlays(source);
        return plays.FirstOrDefault(action => action.CardInstanceId == preferred.CardInstanceId &&
            action.RoomIndex != attachmentRoomIndex) ?? plays.FirstOrDefault(action => action.RoomIndex != attachmentRoomIndex);
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
