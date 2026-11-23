using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PersistentMonsterHpChecks
{
    private const string OwnerId = "4d5b56f1-65b8-4206-ace7-3629d623e0ec";
    private const string OwnerKey = "Echoes_FriendlyUnitHealthPersistsBetweenBattles";
    private const string Effect = "RelicEffectPersistMonsterHp";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("Boundary").GetString() == "OriginalPersistentMonsterHealthOnHit" &&
            root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            root.GetProperty("OwnerId").GetString() == OwnerId && root.GetProperty("OwnerKey").GetString() == OwnerKey,
            "Expected the original 2.3.1 persistent monster health component.");

        string cardDataId = root.GetProperty("CardDataId").GetString()!;
        int firstActor = Int(root, "ActorId");
        PlayCardAction deployment = root.GetProperty("Deployment").Deserialize<PlayCardAction>()!;
        BattleTurnState beforeDeployment = ReadState(root, "BeforeDeployment");
        BattleTurnState beforeHit = ReadState(root, "BeforeHit");
        BattleTurnState afterHit = ReadState(root, "AfterHit");
        FixtureValue hit = root.GetProperty("Hit");
        int damage = Int(hit, "Damage"), originalHealth = Int(hit, "BeforeHealth"), stored = Int(hit, "AfterPersistent");
        Require(beforeDeployment.Spawn.Turn == 0 && damage == 1 && originalHealth > damage &&
            Int(hit, "BeforePersistent") == 0 && Int(hit, "AfterHealth") == originalHealth - damage && stored == originalHealth - damage,
            "Native hit changed its deployment, damage or stored-health protocol.");
        Require(Card(beforeDeployment, deployment.CardInstanceId, cardDataId).Permanent.PersistentHealth == 0 &&
            Card(beforeHit, deployment.CardInstanceId, cardDataId).Permanent.PersistentHealth == 0 &&
            Card(afterHit, deployment.CardInstanceId, cardDataId).Permanent.PersistentHealth == stored,
            "Native hit should write current HP to the original paid card's permanent modifier.");
        CombatUnit hitActor = Actor(beforeHit, deployment, firstActor, originalHealth);
        Require(Actor(afterHit, deployment, firstActor, stored).MaxHealth == hitActor.MaxHealth,
            "Native one-point hit changed the health cap.");
        ReplayPlay("fresh paid deployment", beforeDeployment, deployment, beforeHit);
        ReplayHealthChange("attackerless one-point hit", beforeHit, afterHit, deployment.RoomIndex, firstActor, damage, healing: false);

        FixtureValue restored = root.GetProperty("RestoredBirth");
        PlayCardAction restoredAction = restored.GetProperty("Action").Deserialize<PlayCardAction>()!;
        BattleTurnState beforeRestoredBirth = ReadState(restored, "Before"), afterRestoredBirth = ReadState(restored, "After");
        int restoredActor = Int(restored, "ActorId"), maximum = Int(restored, "MaximumHealth");
        bool aboveMaximum = restored.GetProperty("Setup").GetString() switch
        {
            "ObservedHpSeededIntoSecondOwnedHandCard" => false,
            "StoredHpOneAboveOriginalMaximum" => true,
            _ => throw new InvalidOperationException("Unknown stored-health setup at native paid birth.")
        };
        int seed = aboveMaximum ? checked(hitActor.MaxHealth + 1) : stored;
        Require(restoredActor != firstActor && restoredAction.CardInstanceId != deployment.CardInstanceId &&
            Int(restored, "SeedPersistentHealth") == seed && Int(restored, "Health") == seed &&
            maximum == (aboveMaximum ? seed : hitActor.MaxHealth) &&
            Card(beforeRestoredBirth, restoredAction.CardInstanceId, cardDataId).Permanent.PersistentHealth == seed &&
            Card(afterRestoredBirth, restoredAction.CardInstanceId, cardDataId).Permanent.PersistentHealth == seed &&
            Actor(afterRestoredBirth, restoredAction, restoredActor, seed).MaxHealth == maximum,
            "Native paid birth must restore the captured card's stored HP and grow its cap only when needed.");
        ReplayPlay("paid deployment with stored HP", beforeRestoredBirth, restoredAction, afterRestoredBirth);

        FixtureValue followUp = root.GetProperty(aboveMaximum ? "Death" : "Heal");
        Require(followUp.GetProperty("Before").ContentEquals(restored.GetProperty("After")) &&
            Int(followUp, "ActorId") == restoredActor && Int(followUp, "BeforeHealth") == seed && Int(followUp, "BeforePersistent") == seed,
            "The native follow-up must start with the restored owned unit and its stored HP.");
        BattleTurnState afterFollowUp = ReadState(followUp, "After");
        int amount = Int(followUp, aboveMaximum ? "Damage" : "Amount");
        int finalPersistent = Int(followUp, "AfterPersistent");
        Require(Card(afterFollowUp, restoredAction.CardInstanceId, cardDataId).Permanent.PersistentHealth == finalPersistent,
            "The native follow-up must write the same card's permanent state.");
        if (aboveMaximum)
        {
            Require(amount == seed && finalPersistent == 0 && afterFollowUp.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .All(unit => unit.Id != restoredActor || unit.Health == 0), "Native lethal damage must remove the live actor and clear its stored HP.");
        }
        else
        {
            int healed = Int(followUp, "AfterHealth");
            Require(amount == 1 && healed == seed + amount && finalPersistent == healed &&
                Actor(afterFollowUp, restoredAction, restoredActor, healed).MaxHealth == maximum,
                "Native healing must restore one current HP and leave the cap unchanged.");
        }
        string operation = aboveMaximum ? "lethal damage" : "one-point heal";
        ReplayHealthChange(operation, afterRestoredBirth, afterFollowUp, restoredAction.RoomIndex, restoredActor, amount, healing: !aboveMaximum);
        Console.WriteLine($"NATIVE-PERSISTENT-MONSTER-HP PASS: paid births, nonlethal hit and {operation}, permanent card ownership, RNG, immutable inputs.");
    }

    private static void ReplayPlay(string label, BattleTurnState before, PlayCardAction action, BattleTurnState actual)
    {
        string parent = Json(before);
        BattleActionResult result = BattleActionModel.PlayCard(before, action);
        Require(result.State != null && result.Outcome == RoomOutcome.Exchanged, label + ": " + result.Reason);
        Equal(BattleTurnChecks.Comparable(result.State!), BattleTurnChecks.Comparable(actual), label);
        Require(Json(before) == parent, label + " mutated its input.");
    }

    private static void ReplayHealthChange(string label, BattleTurnState before, BattleTurnState actual,
        int roomIndex, int actorId, int amount, bool healing)
    {
        RoomCombatState input = Room(before, roomIndex);
        string parent = Json(before);
        RoomCombatResult result = healing ? RoomCombatModel.ApplyCardHeal(input, actorId, amount) :
            RoomCombatModel.ApplyCardDamage(input, actorId, amount);
        Require(result.State != null && result.Outcome == RoomOutcome.Exchanged, label + ": " + result.UnsupportedReason);
        TrainCombatState train = CardSpellModel.ReplaceRoom(before.Spawn.Train, result.State!);
        if (before.CanonicalPhysicalReferences) train = TrainCombatModel.CompleteFrameRemovals(train, Array.Empty<int>());
        else if (before.CanonicalDecisionReferences) train = TrainCombatModel.ProcessRemovals(train);
        Equal(ComparableTrain(train), ComparableTrain(actual.Spawn.Train), label);
        Require(before.CanonicalPhysicalReferences == actual.CanonicalPhysicalReferences &&
            before.CanonicalDecisionReferences == actual.CanonicalDecisionReferences &&
            Json(before.RngStreams) == Json(actual.RngStreams) && Json(before) == parent,
            label + " changed native RNG or mutated its input.");
    }

    private static BattleTurnState ReadState(FixtureValue row, string name)
    {
        BattleTurnState state = row.GetProperty(name).Deserialize<BattleTurnState>()!;
        CombatRelicState owned = state.Spawn.Train.Context!.Relics!.Single(relic => relic.DataId == OwnerId);
        Require(state.Spawn.Turn == 0 && owned.AssetKey == OwnerKey && owned.IsCovenant == false && owned.DisallowedInPlacementPhase == false &&
            owned.EffectTypes.SequenceEqual([Effect]), name + " lost the original deployment-eligible owner.");
        return state;
    }

    private static CardInstanceState Card(BattleTurnState state, int id, string dataId)
    {
        CombatContext context = state.Spawn.Train.Context!;
        CardInstanceState card = context.CardInstances!.Single(card => card.InstanceId == id);
        CardInstanceState registered = context.CardRegistry!.Single(card => card.InstanceId == id);
        Require(card.DataId == dataId && card.Temporary.PersistentHealth == 0 && Json(card) == Json(registered),
            "The stored health must belong to the same owned and registered permanent card state.");
        return card;
    }

    private static CombatUnit Actor(BattleTurnState state, PlayCardAction action, int id, int health)
    {
        CombatUnit actor = Room(state, action.RoomIndex).Units.Single(unit => unit.Id == id);
        Require(actor.Team == CombatTeam.Player && !actor.IsPyre && actor.SpawnerCardId == action.CardInstanceId && actor.Health == health,
            "Native actor lost its paid card, team or current health.");
        return actor;
    }

    private static RoomCombatState Room(BattleTurnState state, int index) => state.Spawn.Train.Rooms.Single(room => room.RoomIndex == index);
    private static string ComparableTrain(TrainCombatState train) => Json(new
    {
        Rooms = train.Rooms.Select(room => room.WithContext(null)),
        train.Movement,
        train.EnemySlotsPerRoom,
        Context = BattleTurnChecks.ComparableContext(train.Context)
    });
    private static int Int(FixtureValue row, string name) => row.GetProperty(name).GetInt32();
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal(string predicted, string actual, string label)
    {
        string? difference = ModelJson.Difference(predicted, actual);
        Require(difference == null, label + ": " + difference);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
