using System;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicCardPlayCorruptionChecks
{
    private const string RelicId = "217d553b-cddf-4a8c-a4c9-cfdb3c99389c";
    private const string RelicKey = "2ChargesOnCardConsumed";
    private const string CarrierTrait = "CardTraitExhaustState";

    internal static void Run()
    {
        CardMaskDescriptor Descriptor(string id, params string[] traitNames)
        {
            var definition = new CardMaskDefinition(id, "Monster", "Common", false, false, false,
                [], [], [], [], null, false, 0, 0, false, false, false);
            CardTraitValue[] traits = traitNames.Select(name => new CardTraitValue(name, name, 0, 0, true, 0)).ToArray();
            return new CardMaskDescriptor(definition, 0, traits, []);
        }
        CardInstanceState Card(int id, string key, params string[] traits) => new(id, key,
            CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [], maskDescriptor: Descriptor(key, traits));

        CombatRelicState relic = new(RelicId, RelicKey, [RelicModel.ModifyCorruptionOnCardPlayed],
            cardPlayCorruptionModifiers: [new RelicCardPlayCorruptionModifier(0, 1, [CarrierTrait],
                canApplyInPreviewMode: true)]);
        Require(RelicModel.Validate([relic]) == null, "Valid card-play corruption relic was rejected.");

        CardInstanceState carrier = Card(1, "carrier", CarrierTrait);
        CardInstanceState control = Card(2, "control");
        UnityRng rng = UnityRng.Seed(673);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 6, 10,
            cardInstances: [carrier, control], relics: [relic],
            roomCapacities: [new RoomCapacityState(0, 5, 4, false, 1, [], 4, 0),
                new RoomCapacityState(1, 5, 4, false, 4, [], 4, 2)]);
        TrainCombatState Train(CombatContext state) => new([
            new RoomCombatState(0, false, [], [], state), new RoomCombatState(1, false, [], [], state)], [], 1, state);
        int Corruption(CombatContext state, int room) => state.RoomCapacities!.Single(item => item.RoomIndex == room).Corruption!.Value;
        string parent = JsonSerializer.Serialize(context);

        RelicCardPlayMagicPowerResult positive = RelicCardPlayMagicPowerModel.Apply(context, carrier.InstanceId,
            selectedRoomIndex: 1, trainState: Train(context));
        Require(positive.Supported && Corruption(positive.Context, 1) == 5 && Corruption(positive.Context, 0) == 1 &&
            positive.Context.RoomCapacities!.Single(item => item.RoomIndex == 1).MaxCorruption == 4,
            "Eligible consumed-card play did not add one to only the selected room without capping at maximum.");

        RelicCardPlayMagicPowerResult negative = RelicCardPlayMagicPowerModel.Apply(context, control.InstanceId,
            selectedRoomIndex: 0, trainState: Train(context));
        Require(negative.Supported && Corruption(negative.Context, 0) == 1 && Corruption(negative.Context, 1) == 4,
            "A card without the required Exhaust trait triggered card-play corruption.");

        RelicCardPlayMagicPowerResult preview = RelicCardPlayMagicPowerModel.Apply(context, carrier.InstanceId,
            selectedRoomIndex: 0, preview: true, trainState: Train(context));
        Require(preview.Supported && Corruption(preview.Context, 0) == 2,
            "The native preview-enabled gate failed to apply the effect to the selected room.");

        CombatRelicState disallowed = new(RelicId, RelicKey, [RelicModel.ModifyCorruptionOnCardPlayed],
            disallowedInPlacementPhase: true,
            cardPlayCorruptionModifiers: [new RelicCardPlayCorruptionModifier(0, 1, [CarrierTrait], canApplyInPreviewMode: true)]);
        RelicCardPlayMagicPowerResult placement = RelicCardPlayMagicPowerModel.Apply(
            context.WithRelics([disallowed]), carrier.InstanceId, selectedRoomIndex: 0,
            placement: true, trainState: Train(context.WithRelics([disallowed])));
        Require(placement.Supported && Corruption(placement.Context, 0) == 1,
            "Placement-phase relic gate was not honored.");

        Require(context.WithRelics([relic.WithRoomEntryStatuses([])]).Relics![0].CardPlayCorruptionModifiers?.Single().Amount == 1,
            "A relic immutable update discarded its card-play corruption definition.");
        CombatRelicState malformed = new(RelicId, RelicKey, [RelicModel.ModifyCorruptionOnCardPlayed]);
        Require(RelicModel.Validate([malformed])?.Contains("ordered native card-play corruption", StringComparison.Ordinal) == true,
            "An uncaptured native card-play corruption payload passed validation.");
        CombatRelicState incomplete = new(RelicId, RelicKey, [RelicModel.ModifyCorruptionOnCardPlayed],
            cardPlayCorruptionModifiers: [new RelicCardPlayCorruptionModifier(0, 1, [CarrierTrait], conditionCount: 1)]);
        Require(RelicModel.Validate([incomplete])?.Contains("Incomplete native card-play corruption", StringComparison.Ordinal) == true,
            "An incompletely captured corruption condition passed validation.");
        Require(JsonSerializer.Serialize(context) == parent,
            "Card-play corruption mutated its parent branch.");
        Console.WriteLine("RELIC-CARD-PLAY-CORRUPTION-CHECKS PASS: required Exhaust trait, selected-room +1, no maximum cap, preview and placement gates, immutable branches and missing metadata guards.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 195 &&
            root.GetProperty("ModifierScenario").GetString() == "original-two-charges-on-card-consumed" &&
            root.GetProperty("OriginalTwoChargesOnCardConsumedPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("CaptureFailures").GetInt32() == 0 &&
            root.GetProperty("Mismatches").GetInt32() == 0 && root.GetProperty("Unsupported").GetInt32() == 0 &&
            root.GetProperty("Pending").GetInt32() == 0,
            "Original 2ChargesOnCardConsumed battle is incomplete or has native/model failures.");

        FixtureValue evidence = root.GetProperty("OriginalTwoChargesOnCardConsumedCoverage");
        int controlId = evidence.GetProperty("ControlCardInstanceId").GetInt32();
        int carrierId = evidence.GetProperty("CarrierCardInstanceId").GetInt32();
        int controlRoom = evidence.GetProperty("ControlRoomIndex").GetInt32();
        int targetRoom = evidence.GetProperty("TargetRoomIndex").GetInt32();
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray()
            .Where(entry => entry.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(evidence.GetProperty("Prepared").GetBoolean() && evidence.GetProperty("ControlCompleted").GetBoolean() &&
            evidence.GetProperty("CarrierCompleted").GetBoolean() && evidence.GetProperty("Error").ValueKind == FixtureKind.Null &&
            controlRoom != targetRoom && controlId != carrierId && actions.Length == 2,
            "The isolated non-Exhaust and Exhaust carrier card plays did not both complete.");
        Require(evidence.GetProperty("ControlRoomInitialCorruption").GetInt32() == evidence.GetProperty("ControlAfterCorruption").GetInt32() &&
            evidence.GetProperty("TargetRoomAfterCorruption").GetInt32() == evidence.GetProperty("TargetRoomInitialCorruption").GetInt32() + 1,
            "Native control/positive corruption totals differ from the selected-room effect.");

        int controls = 0, carriers = 0;
        foreach (FixtureValue entry in actions)
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatContext beforeContext = before.Spawn.Train.Context!;
            CombatContext actualContext = actual.Spawn.Train.Context!;
            CombatRelicState relic = beforeContext.Relics!.Single(item => item.DataId == RelicId);
            Require(relic.AssetKey == RelicKey && relic.EffectTypes.SequenceEqual([RelicModel.ModifyCorruptionOnCardPlayed]) &&
                relic.CardPlayCorruptionModifiers?.Count == 1 && relic.CardPlayCorruptionModifiers[0].Amount == 1 &&
                relic.CardPlayCorruptionModifiers[0].RequiredTraits.SequenceEqual([CarrierTrait]) &&
                relic.CardPlayCorruptionModifiers[0].Conditions.Count == 0 && relic.CardPlayCorruptionModifiers[0].CanApplyInPreviewMode,
                "The fixture did not retain the unchanged native 2ChargesOnCardConsumed payload.");

            int cardId = entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            int roomIndex = entry.GetProperty("Action").GetProperty("RoomIndex").GetInt32();
            CardInstanceState played = beforeContext.CardInstances!.Single(card => card.InstanceId == cardId);
            int? currentBefore = beforeContext.RoomCapacities!.Single(room => room.RoomIndex == roomIndex).Corruption;
            int? currentAfter = actualContext.RoomCapacities!.Single(room => room.RoomIndex == roomIndex).Corruption;
            if (cardId == controlId)
            {
                Require(roomIndex == controlRoom && played.MaskDescriptor?.BaseTraits.All(trait => trait.RuntimeType != CarrierTrait) == true &&
                    currentAfter == currentBefore,
                    "The non-Exhaust native control changed its selected room corruption.");
                controls++;
            }
            else
            {
                Require(cardId == carrierId && roomIndex == targetRoom &&
                    played.MaskDescriptor?.BaseTraits.Any(trait => trait.RuntimeType == CarrierTrait) == true &&
                    currentAfter == currentBefore + 1,
                    "The native Exhaust carrier did not add exactly one to its selected room.");
                CardPileState? exhausted = actualContext.OtherPiles?.SingleOrDefault(pile => pile.Name == "Exhausted");
                Require(exhausted?.Cards.Any(card => card.InstanceId == carrierId) == true,
                    "The positive native card play did not consume the Exhaust-trait carrier.");
                foreach (RoomCapacityState otherRoom in beforeContext.RoomCapacities.Where(room => room.RoomIndex != targetRoom))
                    Require(actualContext.RoomCapacities!.Single(room => room.RoomIndex == otherRoom.RoomIndex).Corruption == otherRoom.Corruption,
                        "The relic changed corruption on a room other than the selected floor.");
                carriers++;
            }
        }
        Require(controls == 1 && carriers == 1,
            "Missing native nonmatching control or matching consumed-card play.");
        BattleActionChecks.Native(root);
        Console.WriteLine("NATIVE-2-CHARGES-CHECKS PASS: unchanged owner, non-Exhaust control, exact +1 on selected room, other-floor isolation and consumed-pile confirmation.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
