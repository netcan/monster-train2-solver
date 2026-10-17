using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalExtractForEmberAndEchosChecks
{
    internal const string DataId = "4d237444-5beb-43df-a545-c68a7ae86ba6";
    private const string Key = "ExtractForEmberAndEchos";
    private const string EnergyEffect = RelicResourceModel.EnergyOnCorruptRestrictedPlayed;
    private const string CorruptionEffect = RelicResourceModel.CorruptionOnCorruptRestrictedPlayed;

    private static CombatRelicState Owner(bool disallowedInPlacement = false, int energy = 1, int corruption = 1,
        IReadOnlyList<RelicConditionState>? conditions = null) => new(DataId, Key,
        [EnergyEffect, CorruptionEffect], disallowedInPlacementPhase: disallowedInPlacement,
        resourceEffects: [new(0, EnergyEffect, energy, 0, energy, false, conditions: conditions),
            new(1, CorruptionEffect, corruption, 0, corruption, true, conditions: conditions)]);

    private static CardInstanceState Card(int restrictedAmount = 0, bool restricted = true)
    {
        CardTraitValue[] traits = restricted
            ? [new CardTraitValue("CardTraitCorruptRestricted", "CorruptRestricted", 0, restrictedAmount, false, 0)]
            : Array.Empty<CardTraitValue>();
        var definition = new CardMaskDefinition("carrier", "Monster", "Common", false, false, false,
            [], [], [], [], null, false, 0, 0, false, false, false);
        var descriptor = new CardMaskDescriptor(definition, 2, traits, []);
        return new CardInstanceState(1, "carrier", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            maskDescriptor: descriptor);
    }

    private static TrainCombatState Train(int energy, int corruption, CardInstanceState card, CombatRelicState? relic)
    {
        UnityRng rng = UnityRng.Seed(811);
        CombatRelicState[] relics = relic == null ? [] : [relic];
        CombatContext context = new(new([], [], [], rng, 0, []), rng, 0, 2, 10,
            cardInstances: [card], queryFrame: new(energy: energy, runningCombat: true, selectedRoomIndex: 0),
            energyState: new(3, 0, 0, "Combat", true),
            roomCapacities: [new RoomCapacityState(0, 5, 5, false, corruption, [], 10, 0)], relics: relics);
        return new([new RoomCombatState(0, false, [], [], context)], [], 7, context);
    }

    internal static void Run()
    {
        CombatRelicState owner = Owner();
        Require(RelicModel.Validate([owner]) == null, "The captured original ExtractForEmberAndEchos definition was rejected.");
        TrainCombatState source = Train(1, 2, Card(), owner);
        string frozen = JsonSerializer.Serialize(source);
        RelicCardPlayMagicPowerResult applied = RelicCardPlayMagicPowerModel.Apply(source.Context!, 1,
            selectedRoomIndex: 0, trainState: source);
        Require(applied.Supported && applied.Context.QueryFrame!.Energy == 2 &&
            applied.Context.RoomCapacities!.Single().Corruption == 3 && frozen == JsonSerializer.Serialize(source),
            "The ordered Corrupt Restricted callbacks did not grant +1 Ember and +1 selected-room corruption immutably.");

        TrainCombatState capped = Train(3, 2, Card(), owner);
        RelicCardPlayMagicPowerResult cappedResult = RelicCardPlayMagicPowerModel.Apply(capped.Context!, 1,
            selectedRoomIndex: 0, trainState: capped);
        Require(cappedResult.Supported && cappedResult.Context.QueryFrame!.Energy == 3 &&
            cappedResult.Context.RoomCapacities!.Single().Corruption == 3,
            "The capped native AddEnergy call suppressed or altered the corruption callback.");

        TrainCombatState ordinaryCard = Train(1, 2, Card(restricted: false), owner);
        RelicCardPlayMagicPowerResult ordinary = RelicCardPlayMagicPowerModel.Apply(ordinaryCard.Context!, 1,
            selectedRoomIndex: 0, trainState: ordinaryCard);
        Require(ordinary.Supported && JsonSerializer.Serialize(ordinary.Context) == JsonSerializer.Serialize(ordinaryCard.Context),
            "A card without Corrupt Restricted triggered ExtractForEmberAndEchos.");

        TrainCombatState preview = Train(1, 2, Card(), owner);
        RelicCardPlayMagicPowerResult previewResult = RelicCardPlayMagicPowerModel.Apply(preview.Context!, 1,
            selectedRoomIndex: 0, preview: true, trainState: preview);
        Require(previewResult.Supported && previewResult.Context.QueryFrame!.Energy == 1 &&
            previewResult.Context.RoomCapacities!.Single().Corruption == 3,
            "Preview eligibility did not gate only the native preview-disabled Ember effect.");

        TrainCombatState placement = Train(1, 2, Card(), Owner(disallowedInPlacement: true));
        RelicCardPlayMagicPowerResult placementResult = RelicCardPlayMagicPowerModel.Apply(placement.Context!, 1,
            selectedRoomIndex: 0, placement: true, trainState: placement);
        Require(placementResult.Supported && placementResult.Context.QueryFrame!.Energy == 1 &&
            placementResult.Context.RoomCapacities!.Single().Corruption == 2,
            "A placement-disallowed owner fired during deployment.");

        TrainCombatState positiveDebit = Train(1, 2, Card(1), owner);
        RelicCardPlayMagicPowerResult debitResult = RelicCardPlayMagicPowerModel.Apply(positiveDebit.Context!, 1,
            selectedRoomIndex: 0, trainState: positiveDebit);
        Require(!debitResult.Supported && debitResult.UnsupportedReason!.Contains("pre-play corruption", StringComparison.Ordinal),
            "Positive Corrupt Restricted pre-play corruption was not kept outside the supported boundary.");

        RelicConditionState conditional = new(new CardStatisticQuery("CardsPlayed", "ThisBattle"), true, 0, 1, true);
        TrainCombatState conditionalTrain = Train(1, 2, Card(), Owner(conditions: [conditional]));
        RelicCardPlayMagicPowerResult conditionalResult = RelicCardPlayMagicPowerModel.Apply(conditionalTrain.Context!, 1,
            selectedRoomIndex: 0, trainState: conditionalTrain);
        Require(!conditionalResult.Supported && conditionalResult.UnsupportedReason!.Contains("Conditional", StringComparison.Ordinal),
            "An unmodeled conditional Corrupt Restricted relic effect was accepted.");
        TrainCombatState conditionalOrdinary = Train(1, 2, Card(restricted: false), Owner(conditions: [conditional]));
        RelicCardPlayMagicPowerResult conditionalOrdinaryResult = RelicCardPlayMagicPowerModel.Apply(
            conditionalOrdinary.Context!, 1, selectedRoomIndex: 0, trainState: conditionalOrdinary);
        Require(conditionalOrdinaryResult.Supported &&
            JsonSerializer.Serialize(conditionalOrdinaryResult.Context) == JsonSerializer.Serialize(conditionalOrdinary.Context),
            "A conditional Corrupt Restricted owner rejected a nonmatching card that cannot trigger it.");

        CombatRelicState malformed = Owner().WithResourceEffects([
            new(0, EnergyEffect, 2, 0, 1, false), new(1, CorruptionEffect, 1, 0, 1, true)]);
        Require(RelicModel.Validate([malformed])?.Contains("amount", StringComparison.Ordinal) == true,
            "An effect amount that diverged from the native parameter passed relic validation.");
        Console.WriteLine("ORIGINAL-EXTRACT-FOR-EMBER-AND-ECHOS-CHECKS PASS: ordered callbacks, energy cap, card/placement/preview gates, immutable state and explicit debit/condition boundaries.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 179 &&
            root.GetProperty("ModifierScenario").GetString() == "original-extract-for-ember-and-echos" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("OriginalExtractForEmberAndEchosPrepared").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "The original ExtractForEmberAndEchos battle is incomplete or has native/model failures.");

        int carrierId = root.GetProperty("OriginalExtractForEmberAndEchosCarrierId").GetInt32();
        FixtureValue[] callbacks = root.GetProperty("OriginalExtractForEmberAndEchosCallbacks").EnumerateArray()
            .Where(callback => callback.GetProperty("CardInstanceId").GetInt32() == carrierId).ToArray();
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray()
            .Where(action => action.GetProperty("ScenarioAction").GetBoolean() &&
                action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == carrierId).ToArray();
        Require(carrierId > 0 && callbacks.Length >= 2 && actions.Length > 0 &&
            callbacks.All(callback => callback.GetProperty("Completed").GetBoolean()),
            "The prepared Corrupt Restricted carrier did not produce completed native callback and action witnesses.");

        BattleTurnState before = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = actions[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        int selectedRoom = actions[0].GetProperty("Action").GetProperty("RoomIndex").GetInt32();
        CombatContext beforeContext = before.Spawn.Train.Context!;
        CombatContext afterContext = actual.Spawn.Train.Context!;
        CardInstanceState card = beforeContext.FindCard(carrierId)!;
        CombatRelicState relic = beforeContext.Relics!.Single(owner => owner.DataId == DataId);
        Require(relic.AssetKey == Key && relic.EffectTypes.SequenceEqual([EnergyEffect, CorruptionEffect]) &&
            relic.ResourceEffects!.Select(effect => effect.Amount).SequenceEqual([1, 1]) &&
            relic.ResourceEffects.All(effect => effect.Conditions.Count == 0) &&
            RelicCorruptRestrictedCardPlayModel.ValidateCardTraits(card, out bool restricted) == null && restricted,
            "The battle changed the original relic metadata or the carrier lacked the isolated zero-debit trait.");

        FixtureValue energy = callbacks[0];
        FixtureValue corruption = callbacks[1];
        RoomCapacityState beforeRoom = beforeContext.RoomCapacities!.Single(room => room.RoomIndex == selectedRoom);
        RoomCapacityState afterRoom = afterContext.RoomCapacities!.Single(room => room.RoomIndex == selectedRoom);
        Require(energy.GetProperty("Kind").GetString() == "Energy" && energy.GetProperty("Amount").GetInt32() == 1 &&
            energy.GetProperty("RoomIndex").GetInt32() == selectedRoom &&
            energy.GetProperty("AfterEnergy").GetInt32() == Math.Min(beforeContext.EnergyState!.Maximum,
                energy.GetProperty("BeforeEnergy").GetInt32() + 1) &&
            corruption.GetProperty("Kind").GetString() == "Corruption" && corruption.GetProperty("Amount").GetInt32() == 1 &&
            corruption.GetProperty("RoomIndex").GetInt32() == selectedRoom &&
            corruption.GetProperty("BeforeCorruption").GetInt32() == energy.GetProperty("BeforeCorruption").GetInt32() &&
            corruption.GetProperty("AfterCorruption").GetInt32() == corruption.GetProperty("BeforeCorruption").GetInt32() + 1 &&
            beforeRoom.Corruption == corruption.GetProperty("BeforeCorruption").GetInt32() &&
            afterRoom.Corruption == corruption.GetProperty("AfterCorruption").GetInt32(),
            "Native card-play callbacks differed in order, selected room, Ember cap, or corruption delta.");
        Console.WriteLine("ORIGINAL-EXTRACT-FOR-EMBER-AND-ECHOS PASS: unchanged owner, isolated native +1 Ember/+1 selected-floor corruption callbacks, zero-debit carrier and zero replay counters.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
