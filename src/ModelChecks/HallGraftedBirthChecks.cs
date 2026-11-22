using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HallGraftedBirthChecks
{
    private const string HallId = "83734ffd-cbaa-47f1-9809-cc4c4dac568d";
    private const string AssistantId = "3a8240bd-8b96-4d6e-b2d1-49ff8b5d8022";
    private const string GraftId = "0a928329-666c-4ce6-a1c3-413977e37e46";
    private const string Unsupported = "Grafted source-card cloning requires its independent graft identity transition.";
    private sealed record Callback(string Phase, int Card, int Result, int Graft, bool? Cardless);

    internal static void Native(FixtureValue root)
    {
        int schema = root.GetProperty("Schema").GetInt32();
        Require(schema is 1 or 2 &&
            root.GetProperty("Boundary").GetString() == "NativeOriginalHallGraftedBirth" &&
            root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            root.GetProperty("CaptureFailures").GetInt32() == 0 && root.GetProperty("Pending").GetInt32() == 0,
            "Expected a completed original 2.3.1 Hall grafted birth observation.");

        FixtureValue hall = root.GetProperty("Hall"), birth = root.GetProperty("Birth");
        PlayCardAction installHall = hall.GetProperty("Action").Deserialize<PlayCardAction>()!;
        PlayCardAction deploy = birth.GetProperty("Action").Deserialize<PlayCardAction>()!;
        BattleTurnState hallBefore = hall.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState before = birth.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = birth.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext prior = before.Spawn.Train.Context!, settled = actual.Spawn.Train.Context!;
        Require(hall.GetProperty("Index").GetInt32() + 1 == birth.GetProperty("Index").GetInt32() &&
            hall.GetProperty("Actual").ContentEquals(birth.GetProperty("Before")) &&
            hall.GetProperty("Difference").ValueKind == FixtureKind.Null &&
            hall.GetProperty("Predicted").GetProperty("Supported").GetBoolean() &&
            before.Spawn.Turn == 1 && actual.Spawn.Turn == before.Spawn.Turn &&
            deploy.RoomIndex == installHall.RoomIndex &&
            hallBefore.Spawn.Train.Context!.FindCard(installHall.CardInstanceId)?.DataId == HallId &&
            prior.FindCard(deploy.CardInstanceId)?.DataId == AssistantId &&
            prior.RoomCapacities!.Single(room => room.RoomIndex == deploy.RoomIndex).Attachments!
                .Any(attachment => attachment.DataId == HallId && attachment.SpawnCopySubtype == "") &&
            birth.GetProperty("ActualOutcome").Deserialize<RoomOutcome>() == RoomOutcome.Exchanged &&
            before.Energy - actual.Energy == before.PlayRules!.Cards.Single(card => card.DataId == AssistantId).Cost,
            "Expected the unchanged original Hall and paid Assistant to settle in the same room.");
        Require(prior.BattleRng.Equals(settled.BattleRng) && prior.Cards.Rng.Equals(settled.Cards.Rng),
            "The observed birth changed tracked Battle or CardDraw RNG.");

        CombatUnit[] newUnits = root.GetProperty("NewUnits").Deserialize<CombatUnit[]>()!;
        Require(newUnits.Length == 2, "Expected two native births.");
        CombatUnit paid = newUnits.Single(unit => unit.SpawnerCardId == deploy.CardInstanceId);
        CombatUnit clone = newUnits.Single(unit => unit.Id != paid.Id);
        RoomCombatState beforeRoom = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == deploy.RoomIndex);
        RoomCombatState afterRoom = actual.Spawn.Train.Rooms.Single(room => room.RoomIndex == deploy.RoomIndex);
        Require(paid.Modifiers?.IsClone == false && clone.Modifiers?.IsClone == true &&
            paid.Status("cardless") == null && clone.Status("cardless")?.Stacks == 1 &&
            newUnits.All(unit => unit.Team == CombatTeam.Player && unit.AssetKey == "MonsterTwistedAssistant" &&
                unit.Status("unit_grafted_equipment")?.Stacks == 1 && unit.Health > 0 &&
                unit.Modifiers?.Graft?.Card.DataId == GraftId && beforeRoom.Units.All(previous => previous.Id != unit.Id) &&
                Json(unit) == Json(afterRoom.Units.Single(actualUnit => actualUnit.Id == unit.Id))) &&
            paid.Id == prior.NextUnitId && clone.Id == paid.Id + 1 && settled.NextUnitId == clone.Id + 1 &&
            settled.LastSpawnedUnitId == clone.Id,
            "Native birth identity, cardless/graft markers or settled room roster differ.");
        SpawnPointGroupState points = settled.SpawnPoints!.Groups.Single(group => group.RoomIndex == deploy.RoomIndex &&
            group.Team == CombatTeam.Player && group.PreviewCopyId == null);
        Require(points.Occupants[deploy.PlayerPosition] == paid.Id && points.Occupants[deploy.PlayerPosition + 1] == clone.Id &&
            settled.SpawnPoints.Units.Single(item => item.UnitId == paid.Id).Current?.Index == deploy.PlayerPosition &&
            settled.SpawnPoints.Units.Single(item => item.UnitId == clone.Id).Current?.Index == deploy.PlayerPosition + 1,
            "Native physical slots did not retain the distinct paid and mirror births.");

        int paidGear = paid.EquipmentCards!.Single(), cloneGear = clone.EquipmentCards!.Single();
        int[] identities = [paid.SpawnerCardId, clone.SpawnerCardId, paidGear, cloneGear];
        var standby = actual.OtherPiles!.Single(pile => pile.Name == "Standby");
        var priorRegistry = prior.CardRegistry!;
        var settledRegistry = settled.CardRegistry!;
        var settledCards = settled.CardInstances!;
        Require(identities.Distinct().Count() == identities.Length &&
            priorRegistry.All(card => card.InstanceId != cloneGear && card.InstanceId != clone.SpawnerCardId) &&
            cloneGear == prior.NextCardId && clone.SpawnerCardId == cloneGear + 1 && settled.NextCardId == clone.SpawnerCardId + 1 &&
            prior.FindCard(paid.SpawnerCardId)?.GraftedEquipmentCardId == paidGear &&
            priorRegistry.Single(card => card.InstanceId == paidGear).EquippedUnitId == 0,
            "Projected combat IDs must distinguish the clone's detached source and the new graft.");
        foreach (CombatUnit unit in newUnits)
        {
            CardInstanceState source = settledRegistry.Single(card => card.InstanceId == unit.SpawnerCardId);
            int equipmentId = unit.EquipmentCards!.Single();
            CardInstanceState equipment = settledRegistry.Single(card => card.InstanceId == equipmentId);
            bool isPaid = unit.Id == paid.Id;
            Require(source.DataId == AssistantId && source.GraftedEquipmentCardId == paidGear &&
                source.MaskDescriptor?.PermanentGraft == true &&
                settledCards.Any(card => card.InstanceId == source.InstanceId) == isPaid &&
                standby.Cards.Any(card => card.InstanceId == source.InstanceId) == isPaid &&
                equipment.DataId == GraftId && equipment.EquippedUnitId == unit.Id &&
                equipment.MaskDescriptor!.BaseTraits.Single(trait => trait.RuntimeType == CardGraftedEquipmentModel.Trait).GraftSevered == false &&
                settledCards.Any(card => card.InstanceId == equipmentId) &&
                standby.Cards.Any(card => card.InstanceId == equipmentId) &&
                standby.EquipmentConditions!.Any(condition => condition.CardId == equipmentId &&
                    condition.HostUnitId == unit.Id && !condition.ReturnToHand),
                "Native source reference, owned equipment, and standby binding disagree.");
        }

        FixtureValue[] transitions = root.GetProperty("Equipment").EnumerateArray().ToArray();
        Require(transitions.Length == 2, "Expected two native graft attachment callbacks.");
        foreach (var (transition, host) in transitions.Zip(new[] { paid, clone }))
        {
            int gearId = host.EquipmentCards!.Single();
            RoomCombatState input = transition.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState output = transition.GetProperty("After").Deserialize<RoomCombatState>()!;
            Require(transition.GetProperty("Completed").GetBoolean() && transition.GetProperty("Grafted").GetBoolean() &&
                !transition.GetProperty("Remove").GetBoolean() && transition.GetProperty("UnitId").GetInt32() == host.Id &&
                transition.GetProperty("CardId").GetInt32() == gearId &&
                transition.GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null &&
                transition.GetProperty("Difference").ValueKind == FixtureKind.Null &&
                input.Context!.CardRegistry!.Single(card => card.InstanceId == gearId).EquippedUnitId == 0 &&
                output.Context!.CardRegistry!.Single(card => card.InstanceId == gearId).EquippedUnitId == host.Id &&
                output.Units.Single(unit => unit.Id == host.Id).EquipmentCards!.SequenceEqual([gearId]),
                "Native attachment ordering or detached-to-owned transition differs.");
        }

        if (schema == 2) VerifyCloneCallbacks(root);

        string parent = Json(before), observed = Json(actual);
        FixtureValue recordedPrediction = birth.GetProperty("Predicted");
        bool recordedSupported = recordedPrediction.GetProperty("Supported").GetBoolean();
        if (schema == 1 || !recordedSupported)
            Require(!recordedSupported && recordedPrediction.GetProperty("Reason").GetString() == Unsupported,
                "The historical observer must retain its rejected graft copy prediction.");
        if (schema == 2)
        {
            string expected = BattleTurnChecks.Comparable(actual);
            if (recordedSupported)
                Require(birth.GetProperty("Difference").ValueKind == FixtureKind.Null &&
                    BattleTurnChecks.Comparable(recordedPrediction.GetProperty("State").Deserialize<BattleTurnState>()!) == expected,
                    "The fresh native archive did not match its in-game prediction.");
            UnitCopyCatalog catalog = before.PlayRules!.UnitCopyCatalog!;
            foreach (CardInstanceState source in new[] { priorRegistry.Single(card => card.InstanceId == paid.SpawnerCardId),
                priorRegistry.Single(card => card.InstanceId == paidGear) })
                Require(!CardGenerationModel.CloneDetached(prior,
                    catalog.Cards.Single(card => card.DataId == source.DataId), source.InstanceId).Supported,
                    "Ordinary detached copying must still reject both grafted roles.");
            CardPlayRule assistant = before.PlayRules.Cards.Single(card => card.DataId == AssistantId);
            const string guardedBirth = "Generated, triggered, or cloned graft births require their native equipment identity transition.";
            Require(UnitBirthModel.Spawn(beforeRoom, assistant, deploy.CardInstanceId, deploy.PlayerPosition, false)
                .Result.UnsupportedReason == guardedBirth, "Ordinary generated graft births must still reject.");
            Require(UnitBirthModel.SpawnQueued(beforeRoom, assistant, deploy.CardInstanceId, deploy.PlayerPosition,
                false, prior.StatusRules.Single(status => status.Id == "cardless"), _ => { }, beforeRoom.Units)
                .Result.UnsupportedReason == guardedBirth, "Queued graft births must still reject.");
            void Replay()
            {
                BattleActionResult played = BattleActionModel.PlayCard(before, deploy);
                Require(played.Supported && played.State != null, "Paid Hall graft clone unsupported: " + played.Reason);
                string? difference = ModelJson.Difference(Json(played.State!), observed);
                Require(played.Outcome == RoomOutcome.Exchanged && difference == null,
                    "Paid Hall graft clone differs from native state: " + difference);
            }
            Replay();
            Parallel.For(0, 8, _ => Replay());
            Require(Json(before) == parent, "Paid Hall graft clone mutated the parent state.");
        }
        BattleTurnResult continued = BattleTurnModel.EndTurn(actual);
        Require(continued.Supported && continued.State != null && Json(actual) == observed,
            "The advisor could not consume the unchanged observed successor: " + continued.UnsupportedReason);
        Console.WriteLine("NATIVE-HALL-GRAFTED-BIRTH PASS: original paid and cloned units, distinct native graft ownership, source alias, physical slots, unchanged Battle/Draw RNG and observed-root EndTurn.");
    }

    private static void VerifyCloneCallbacks(FixtureValue root)
    {
        FixtureValue[] observations = root.GetProperty("CloneObservations").EnumerateArray().ToArray();
        Require(observations.Length == 1 && observations[0].GetProperty("Completed").GetBoolean(),
            "Expected one completed original CloneMonsterState coroutine.");
        FixtureValue observation = observations[0];
        int paidSpawner = observation.GetProperty("PaidSpawner").GetInt32();
        int paidGraft = observation.GetProperty("PaidGraft").GetInt32();
        Callback[] actual = observation.GetProperty("Callbacks").Deserialize<Callback[]>()!;
        Require(actual.Length == 6, "Expected six ordered native copy, birth and equipment records.");

        int cloneSpawner = actual[1].Result;
        int cloneGraft = actual[4].Result;
        int[] cards = [paidSpawner, paidGraft, cloneSpawner, cloneGraft];
        Require(cards.All(reference => reference > 0) && cards.Distinct().Count() == cards.Length,
            "The original and clone must each have a separate spawner and graft object.");
        Callback[] expected =
        [
            new("CopyCardEnter", paidSpawner, 0, paidGraft, null),
            new("CopyCardExit", paidSpawner, cloneSpawner, paidGraft, null),
            new("CreateMonster", cloneSpawner, 0, paidGraft, true),
            new("CopyCardEnter", paidGraft, 0, 0, null),
            new("CopyCardExit", paidGraft, cloneGraft, 0, null),
            new("AddEquipment", cloneSpawner, cloneGraft, paidGraft, null)
        ];
        Require(actual.SequenceEqual(expected), "Native clone did not preserve the source alias and attach the separate graft in observed order.");
        Console.WriteLine("NATIVE-HALL-GRAFT-ORDER PASS: source copy, cardless birth, graft copy, graft attachment, exact native object aliases.");
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
