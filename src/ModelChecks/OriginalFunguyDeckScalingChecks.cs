using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalFunguyDeckScalingChecks
{
    private const string RelicId = RelicPileStartTurnModel.DataId;
    private const string RelicKey = RelicPileStartTurnModel.AssetKey;
    private const string Effect = RelicPileStartTurnModel.Effect;
    private static readonly CombatStatus Horde = new("horde", 1, stackable: true, hidden: false, displayCategory: "Positive");

    internal static void Run()
    {
        Require(new[] { 19, 20, 39, 40 }.Select(count => RelicPileStartTurnModel.Applications(count, 20))
                .SequenceEqual(new[] { 0, 1, 1, 2 }),
            "Deck-count thresholds do not use native integer division.");
        var action = new CardActionEffect("SpawnOrStatus", "Room", 1, true, false, [Horde],
            filters: new CardTargetFilters("Both", [], [], false, "SubtypesData_Horde", []),
            spawnFallbackUnit: new CombatUnit(0, "MonsterFunguy", CombatTeam.Player, 5, 10, 10,
                true, false, false, [Horde], subtypes: ["SubtypesData_Horde"],
                modifiers: new UnitModifiers(5, 0, 0, 1, 1, true, false, []),
                hordeDefinition: new HordeBaseStats(5, 10)));
        RelicResourceEffect Resource() => new(0, Effect, 20, 0, 20, true, paramMinInt: 0,
            pileStartTurn: new RelicPileStartTurnRule("DeckPile", 20, true, action));
        CombatRelicState Owner() => new(RelicId, RelicKey, [Effect], isCovenant: false,
            disallowedInPlacementPhase: true, resourceEffects: [Resource()]);
        CombatRelicState owner = Owner();
        Require(RelicModel.Validate([owner]) == null,
            "The exact original Funguy relic payload was rejected.");
        Require(RelicModel.Validate([new CombatRelicState(RelicId, RelicKey, [Effect], isCovenant: false,
            disallowedInPlacementPhase: true, resourceEffects: [new RelicResourceEffect(0, Effect, 20, 0, 20,
                true, paramMinInt: 0)])]) != null,
            "Missing nested effect payload passed validation.");

        CombatContext Context(int visibleDeckCount) => new(new([], [], [], UnityRng.Seed(19), 0, []), UnityRng.Seed(29),
            0, 1, 10, statistics: BattleStatistics.Empty(deckCards: []), otherPiles: [],
            queryFrame: new(energy: 0, runningCombat: true, turn: 1), energyState: new(10, 0, 0, "MonsterTurn", true),
            relics: [owner], resources: new BattleResourceState(3, 1, 5, 10, 10, "pyre", 20, 80, 0, 0,
                [5], [10], [], visibleDeckCount: visibleDeckCount));
        CombatUnit HordeUnit(int id) => new(id, "HordeTarget", CombatTeam.Player, 5, 20, 20,
            true, false, false, [Horde], subtypes: ["SubtypesData_Horde"],
            modifiers: new UnitModifiers(5, 0, 0, 1, 1, true, false, []), isBoss: false,
            statusRegistry: [Horde], statusDictionary: new(["horde"], []), hordeDefinition: new HordeBaseStats(5, 10));
        TrainCombatState Train(CombatContext context) => new(Enumerable.Range(0, 3).Select(index =>
            new RoomCombatState(index, false, [HordeUnit(index + 1)], [], context)).ToArray(), [], 7, context);
        var definitions = new BattlePlayRules(Enumerable.Range(0, 3).Select(index =>
            new RoomPlayRule(index, 7, 7, true, false, false)).ToArray(), []);

        foreach (int deckCount in new[] { 19, 20, 39, 40 })
        {
            CombatContext context = Context(deckCount);
            TrainCombatState parent = Train(context);
            Require(RoomCombatModel.Validate(parent.Rooms[0]) == null,
                "Synthetic Horde precondition is invalid: " + RoomCombatModel.Validate(parent.Rooms[0]));
            string frozen = Serialize(parent);
            TrainSpellResult applied = RelicPileStartTurnModel.Apply(parent, definitions);
            int applications = RelicPileStartTurnModel.Applications(deckCount, 20);
            Require(applied.Supported, "Funguy deck scaling failed at " + deckCount + ": " + applied.UnsupportedReason);
            int[] stacks = applied.State!.Rooms.SelectMany(room => room.Units)
                .Select(unit => unit.Status("horde")?.Stacks ?? 0).ToArray();
            Require(stacks.SequenceEqual(Enumerable.Repeat(1 + applications, 3)) && Serialize(parent) == frozen,
                "Deck-scaled Horde status count, all-room behavior or parent immutability differs at " + deckCount + ".");
        }
        TrainCombatState blocked = Train(Context(40));
        TrainSpellResult placement = RelicPileStartTurnModel.Apply(blocked, definitions, placement: true);
        Require(placement.Supported && Serialize(placement.State) == Serialize(blocked),
            "The original relic ignored its deployment-phase restriction.");
        BattlePlayRules destroyedRoomRules = new(definitions.Rooms.Select(room =>
            room.RoomIndex == 0 ? room.WithEnabled(false) : room).ToArray(), []);
        TrainSpellResult destroyedRoom = RelicPileStartTurnModel.Apply(blocked, destroyedRoomRules);
        Require(destroyedRoom.Supported && destroyedRoom.State!.Rooms[0].Units.Count == blocked.Rooms[0].Units.Count &&
            destroyedRoom.State.Rooms.SelectMany(room => room.Units).Select(unit => unit.Status("horde")?.Stacks ?? 0)
                .SequenceEqual(new[] { 1, 3, 3 }),
            "The relic applied in a destroyed room or failed to apply twice to each live room.");
        Require(Context(20).Resources!.WithVisibleDeckCount(39).VisibleDeckCount == 39 &&
            Context(20).Resources!.WithPyreModifiedBaseAttack(7).VisibleDeckCount == 20,
            "Immutable resource updates lost the visible deck count.");
        var permanentDeckContext = new CombatContext(new([], [], [], UnityRng.Seed(31), 0, []), UnityRng.Seed(37),
            0, 1, 10, cardInstances: [], resources: new BattleResourceState(3, 0, 5, 10, 10,
                "pyre", 20, 80, 0, 0, [], [], [], visibleDeckCount: 40));
        var permanentAddition = CardGenerationModel.Apply(permanentDeckContext,
            new CardGenerationRule("DeckPile", 2, [new CardCreationRule("run-card", CardModifiers.Empty(), null, [])],
                deterministicPoolSelection: true, permanent: true));
        var temporaryAddition = CardGenerationModel.Apply(permanentDeckContext,
            new CardGenerationRule("DeckPile", 2, [new CardCreationRule("battle-card", CardModifiers.Empty(), null, [])],
                deterministicPoolSelection: true, permanent: false));
        Require(permanentAddition.Supported && permanentAddition.AddedCards.Count == 2 &&
            permanentAddition.Context!.Resources!.VisibleDeckCount == 42 && temporaryAddition.Supported &&
            temporaryAddition.Context!.Resources!.VisibleDeckCount == 40,
            "Only permanent SaveManager deck additions should change the visible deck count.");
        foreach (CombatRelicState copy in new[] { owner.WithResourceEffects(owner.ResourceEffects!), owner.WithCardModifiers([]),
            owner.WithStatusMagnitudes([]), owner.WithSpawnStatuses([]) })
            Require(copy.ResourceEffects!.Single().PileStartTurn?.Effect.Type == "SpawnOrStatus",
                "Another relic payload copy discarded the nested pile-start-turn definition.");
        Console.WriteLine("ORIGINAL-FUNGUY-DECK-PURE PASS: 19/20/39/40 thresholds, all-room Horde applications, placement gate and immutable payload/resource copies.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 174 &&
            root.GetProperty("ModifierScenario").GetString() == "original-funguy-deck-scaling" &&
            root.GetProperty("OriginalFunguyDeckScalingPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0),
            "Original Funguy relic battle is incomplete or has native/model failures.");
        FixtureValue[] records = root.GetProperty("RelicPileStartTurn").EnumerateArray().ToArray();
        Require(records.Length >= 2, "The native witness must observe multiple start-of-turn relic callbacks.");
        int spawnCount = 0, hordeIncrease = 0;
        bool positive = false;
        foreach (FixtureValue record in records)
        {
            Require(record.GetProperty("Completed").GetBoolean() &&
                record.GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null &&
                record.GetProperty("Difference").ValueKind == FixtureKind.Null &&
                record.GetProperty("VisibleDeckCount").GetInt32() == 40 &&
                record.GetProperty("ApplicationsPerRoom").GetInt32() == 2,
                "A native deck-scaled relic callback was incomplete or diverged.");
            TrainCombatState before = record.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = record.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            CombatRelicState owner = before.Context!.Relics!.Single(relic => relic.DataId == RelicId);
            RelicResourceEffect resource = owner.ResourceEffects!.Single();
            Require(owner.AssetKey == RelicKey && owner.EffectTypes.SequenceEqual([Effect]) &&
                owner.DisallowedInPlacementPhase == true && resource.CanApplyInPreviewMode && resource.Amount == 20 &&
                resource.PileStartTurn?.Pile == "DeckPile" && resource.PileStartTurn.Threshold == 20 &&
                resource.PileStartTurn.ApplyAllRooms && resource.PileStartTurn.Effect.Type == "SpawnOrStatus" &&
                resource.PileStartTurn.Effect.SpawnFallbackUnit?.AssetKey == "MonsterFunguy" &&
                resource.PileStartTurn.Effect.Statuses.Single().Id == "horde" &&
                resource.PileStartTurn.Effect.Filters?.Subtype == "SubtypesData_Horde" &&
                RelicModel.Validate(before.Context) == null,
                "Captured original Funguy deck-scaling relic definition changed or is incomplete.");
            int beforeFunguys = before.Rooms.SelectMany(room => room.Units).Count(unit => unit.AssetKey == "MonsterFunguy");
            int afterFunguys = actual.Rooms.SelectMany(room => room.Units).Count(unit => unit.AssetKey == "MonsterFunguy");
            int beforeHorde = before.Rooms.SelectMany(room => room.Units).Where(unit => unit.AssetKey == "MonsterFunguy")
                .Sum(unit => unit.Status("horde")?.Stacks ?? 0);
            int afterHorde = actual.Rooms.SelectMany(room => room.Units).Where(unit => unit.AssetKey == "MonsterFunguy")
                .Sum(unit => unit.Status("horde")?.Stacks ?? 0);
            if (afterFunguys > beforeFunguys && afterHorde - beforeHorde > afterFunguys - beforeFunguys)
            { positive = true; spawnCount = afterFunguys - beforeFunguys; hordeIncrease = afterHorde - beforeHorde; }
        }
        Require(positive && hordeIncrease > spawnCount,
            "The witness did not cover both cardless Funguy fallback spawns and status application to Horde units.");
        Console.WriteLine($"ORIGINAL-FUNGUY-DECK NATIVE PASS: {records.Length} callbacks at 40 cards (2/room), spawn={spawnCount}, Horde-stack-delta={hordeIncrease}; exact callback model and full battle matched.");
    }

    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool pass, string message)
    { if (!pass) throw new InvalidOperationException(message); }
}
