using System;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalTribouletChecks
{
    private const string OwnerId = "dfe0d28c-ff6a-4a8b-8290-aefb32d0c1d6";
    private const string OwnerKey = "BalatroEventTriboulet";
    private const string WitnessDataId = "fc119000-0000-4000-8000-000000000514";
    private const string WitnessAssetKey = "PojuTribouletCardlessWitness";

    internal static void Run()
    {
        CombatRelicState owner = Owner();
        Require(RelicModel.Validate([owner]) == null, "The exact Triboulet owner or ordered rarity-health payload was rejected.");

        CardInstanceState rare = Card(1, "triboulet-rare", "Spell", "Rare");
        CardInstanceState monster = Card(2, "triboulet-monster", "Monster", "Common");
        CardInstanceState spell = Card(3, "triboulet-spell", "Spell", "Rare");
        CardInstanceState[] cards = [rare, monster, spell];
        CombatContext oneRare = Context(cards, [rare.InstanceId], owner);

        CardInstanceState? buffedMonster = RelicCardHealthModel.Apply(monster, owner.CardHealthModifiers![0],
            oneRare, out bool monsterReturned, out string? monsterError);
        CardInstanceState? filteredSpell = RelicCardHealthModel.Apply(spell, owner.CardHealthModifiers[0],
            oneRare, out bool spellReturned, out string? spellError);
        Require(monsterError == null && monsterReturned && buffedMonster?.Temporary.Offsets.Health == 3 &&
            spellError == null && !spellReturned && ReferenceEquals(filteredSpell, spell),
            "Triboulet did not add +3 temporary health per Rare deck card to Monster cards only.");

        CombatContext noRare = Context(cards, [monster.InstanceId], owner);
        CardInstanceState? unchanged = RelicCardHealthModel.Apply(monster, owner.CardHealthModifiers[0],
            noRare, out bool noRareReturned, out string? noRareError);
        Require(noRareError == null && !noRareReturned && ReferenceEquals(unchanged, monster),
            "A deck with no Rare cards changed Monster card health.");

        CombatContext incompleteDeck = Context(cards, [rare.InstanceId, 99], owner);
        CardInstanceState? missingMetadata = RelicCardHealthModel.Apply(monster, owner.CardHealthModifiers[0],
            incompleteDeck, out _, out string? metadataError);
        Require(missingMetadata == null && metadataError != null && metadataError.Contains("metadata", StringComparison.Ordinal),
            "An uncaptured deck-card rarity was guessed instead of rejected.");

        CombatUnit cardless = Unit(10, CombatTeam.Player, 11, cardless: true);
        RoomCombatState birth = Room(cardless, oneRare);
        Require(RelicRarityHealthModel.TryApply(birth, cardless.Id, 0, owner.CardHealthModifiers[1],
                out RoomCombatState first, out string? firstError) && firstError == null &&
            UnitIn(first, cardless.Id).Health == 13 && UnitIn(first, cardless.Id).MaxHealth == 13,
            "The per-rarity CharacterAdded max-health callback did not heal a cardless unit by +2.");
        Require(RelicRarityHealthModel.TryApply(first, cardless.Id, 0, owner.CardHealthModifiers[2],
                out RoomCombatState second, out string? secondError) && secondError == null &&
            UnitIn(second, cardless.Id).Health == 15 && UnitIn(second, cardless.Id).MaxHealth == 15,
            "The OnSpawnNotFromCard callback did not add the second +2 max/current health.");

        CombatUnit cardlessFromCard = Unit(11, CombatTeam.Player, 11, cardless: true);
        RoomCombatState fromCardBirth = Room(cardlessFromCard, oneRare);
        Require(!RelicRarityHealthModel.Applies(cardlessFromCard, spell.InstanceId, owner.CardHealthModifiers[1]) &&
            RelicRarityHealthModel.Applies(cardlessFromCard, spell.InstanceId, owner.CardHealthModifiers[2]) &&
            RelicRarityHealthModel.TryApply(fromCardBirth, cardlessFromCard.Id, spell.InstanceId,
                owner.CardHealthModifiers[2], out RoomCombatState fromCardResult, out string? fromCardError) &&
            fromCardError == null && UnitIn(fromCardResult, cardlessFromCard.Id).Health == 13 &&
            fromCardResult.Context!.FindCard(spell.InstanceId)!.Temporary.Offsets.Health == 2,
            "A cardless unit spawned from a source card did not receive only the trigger bonus and source-card health.");

        CombatUnit ordinary = Unit(12, CombatTeam.Player, 11, cardless: false);
        Require(!RelicRarityHealthModel.Applies(ordinary, spell.InstanceId, owner.CardHealthModifiers[1]) &&
            !RelicRarityHealthModel.Applies(ordinary, spell.InstanceId, owner.CardHealthModifiers[2]) &&
            !RelicRarityHealthModel.Applies(Unit(13, CombatTeam.Enemy, 11, cardless: true), 0,
                owner.CardHealthModifiers[1]),
            "A card-backed ordinary unit or enemy incorrectly received Triboulet's Monster callback.");

        CombatRelicState malformed = Owner(healthPerRare: 4);
        Require(RelicModel.Validate([malformed]) != null,
            "A changed Triboulet owner payload passed validation.");
        Console.WriteLine("ORIGINAL-TRIBOULET-CHECKS PASS: Rare deck scaling, Monster filter, cardless callback order, source-card trigger and metadata rejection.");
    }

    internal static void Native(FixtureValue root)
    {
        int schema = root.GetProperty("Schema").GetInt32();
        Require(schema == 217 && root.GetProperty("OriginalTribouletSchema").GetInt32() == 217 &&
            root.GetProperty("OriginalTribouletPrepared").GetBoolean() &&
            root.GetProperty("ModifierScenario").GetString() == "original-balatro-event-triboulet" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0),
            "The Triboulet battle is incomplete or contains native/model differences: " +
                string.Join(",", new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                    .Select(name => name + ":" + root.GetProperty(name).GetInt32())));

        FixtureValue evidence = root.GetProperty("OriginalTriboulet");
        Require(evidence.GetProperty("OwnerId").GetString() == OwnerId &&
            evidence.GetProperty("AssetKey").GetString() == OwnerKey &&
            evidence.GetProperty("RareDeckCount").GetInt32() == 1 &&
            Math.Abs(evidence.GetProperty("MonsterHealthAfterOwner").GetSingle() -
                evidence.GetProperty("MonsterHealthBeforeOwner").GetSingle() - 3f) < 0.01f,
            "Native Triboulet acquisition did not apply exactly +3 Monster card health for one Rare card.");

        FixtureValue[] scenarioActions = root.GetProperty("Actions").EnumerateArray()
            .Where(entry => entry.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(scenarioActions.Length == 1, "The cardless Monster-spawn witness must be played exactly once.");
        FixtureValue entry = scenarioActions[0];
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState predicted = entry.GetProperty("Predicted").GetProperty("State").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        int cardId = entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
        CombatContext contextBefore = before.Spawn.Train.Context!;
        CombatContext contextActual = actual.Spawn.Train.Context!;
        Require(contextBefore.FindCard(cardId)?.DataId == WitnessDataId &&
            ModelJson.Difference(BattleTurnChecks.Comparable(predicted), BattleTurnChecks.Comparable(actual)) == null,
            "The native cardless-spawn play or its predicted battle state differs.");
        CombatUnit spawned = actual.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .SingleOrDefault(unit => unit.AssetKey == WitnessAssetKey)
            ?? throw new InvalidOperationException("The native cardless Monster-spawn witness is missing.");
        CombatUnit predictedSpawn = predicted.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .SingleOrDefault(unit => unit.AssetKey == WitnessAssetKey)
            ?? throw new InvalidOperationException("The modeled cardless Monster-spawn witness is missing.");
        Require(spawned.Team == CombatTeam.Player && spawned.SpawnerCardId == 0 &&
            spawned.Statuses.Any(status => status.Id == "cardless" && status.Stacks == 1) &&
            spawned.Health == 15 && spawned.MaxHealth == 15 && predictedSpawn.Health == 15 && predictedSpawn.MaxHealth == 15 &&
            contextActual.Cards.Discard.Any(card => card.InstanceId == cardId) &&
            contextActual.OtherPiles!.Single(pile => pile.Name == "Standby").UnitConditions!.All(binding => binding.CardId != cardId),
            "Native/model cardless health or spell-discard routing differs (expected 15/15 and no Monster standby binding).");

        FixtureValue[] witnessBirths = root.GetProperty("RelicSpawnStatuses").EnumerateArray()
            .Where(record => record.GetProperty("BeforeTrain").GetProperty("Rooms").EnumerateArray()
                .SelectMany(room => room.GetProperty("Units").EnumerateArray())
                .Any(unit => unit.GetProperty("Id").GetInt32() == record.GetProperty("UnitId").GetInt32() &&
                    unit.GetProperty("AssetKey").GetString() == WitnessAssetKey)).ToArray();
        Require(witnessBirths.Length == 1 && witnessBirths[0].GetProperty("Difference").ValueKind == FixtureKind.Null,
            "The cardless witness must have one matching native CharacterAdded callback capture.");
        Console.WriteLine("ORIGINAL-TRIBOULET-NATIVE PASS: exact owner payload, one 15/15 cardless spawn, spell discard, callback and full-battle differential.");
    }

    private static CombatRelicState Owner(int healthPerRare = 2) => new(OwnerId, OwnerKey,
        [RelicModel.ModifyCardHealthPerRarityType, RelicModel.ModifyCharacterMaxHealthPerRarityType,
            RelicModel.ModifyCharacterMaxHealthPerRarityTypeTrigger],
        cardHealthModifiers: [
            new RelicCardHealthModifier(0, "", "", 3, 0, "Monster", false,
                effectType: RelicModel.ModifyCardHealthPerRarityType, cardRarityType: "Rare"),
            new RelicCardHealthModifier(1, "", "", healthPerRare, 0, "", false,
                effectType: RelicModel.ModifyCharacterMaxHealthPerRarityType, cardRarityType: "Rare", sourceTeam: "Monsters"),
            new RelicCardHealthModifier(2, "", "", 2, 0, "", false,
                effectType: RelicModel.ModifyCharacterMaxHealthPerRarityTypeTrigger, cardRarityType: "Rare",
                sourceTeam: "Monsters", trigger: "OnSpawnNotFromCard")
        ]);

    private static CardInstanceState Card(int id, string dataId, string cardType, string rarity)
    {
        var definition = new CardMaskDefinition(dataId, cardType, rarity, false, false, false,
            [], [], [], [], null, false, 0, 0, false, false, false, isChampionCard: false);
        var descriptor = new CardMaskDescriptor(definition, 0, [], []);
        return new CardInstanceState(id, dataId, CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            maskDescriptor: descriptor);
    }

    private static CombatContext Context(CardInstanceState[] cards, int[] deckIds, CombatRelicState owner) =>
        new(new CardCycleState([], [], [], UnityRng.Seed(741), 0, []), UnityRng.Seed(914), 0,
            cards.Max(card => card.InstanceId) + 1, 10, statistics: BattleStatistics.Empty(deckCards: deckIds),
            cardInstances: cards, cardRegistry: cards, relics: [owner]);

    private static CombatUnit Unit(int id, CombatTeam team, int health, bool cardless) => new(id,
        "TribouletWitness", team, 0, health, health, false, false, false,
        cardless ? [new CombatStatus("cardless", 1)] : []);

    private static RoomCombatState Room(CombatUnit unit, CombatContext context) =>
        new(0, true, [unit], [], context);

    private static CombatUnit UnitIn(RoomCombatState room, int id) => room.Units.Single(unit => unit.Id == id);

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
