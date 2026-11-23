using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HallGraftedBirthChecks
{
    private const string HallId = "83734ffd-cbaa-47f1-9809-cc4c4dac568d";
    private const string AssistantId = "3a8240bd-8b96-4d6e-b2d1-49ff8b5d8022";
    private const string GraftId = "0a928329-666c-4ce6-a1c3-413977e37e46";
    private const string ExtraSpawnId = "559f443d-bbeb-40fc-aa86-772ef4c2c2dc";
    private const string ExtraSpawnEffect = "RelicEffectExtraSpawnsFirstUnitPerTurn";
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

    internal static void NativeExtraSpawn(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 2 &&
            root.GetProperty("Boundary").GetString() == "NativeOriginalHallGraftedBirth" &&
            root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            root.GetProperty("ExtraSpawn").GetBoolean() && root.GetProperty("CaptureFailures").GetInt32() == 0 &&
            root.GetProperty("Pending").GetInt32() == 0, "Expected the pinned native DoubleUnits component.");
        FixtureValue hall = root.GetProperty("Hall"), birth = root.GetProperty("Birth");
        var hallBefore = hall.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var before = birth.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = birth.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var install = hall.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var play = birth.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext prior = before.Spawn.Train.Context!, actual = after.Spawn.Train.Context!;
        var owner = prior.Relics!.Single(relic => relic.DataId == ExtraSpawnId);
        Require(owner.AssetKey == "DoubleUnits" && owner.EffectTypes.SequenceEqual([ExtraSpawnEffect]) &&
            owner.DisallowedInPlacementPhase == false && actual.Relics!.Any(relic => relic.DataId == ExtraSpawnId &&
                relic.EffectTypes.SequenceEqual([ExtraSpawnEffect])) &&
            hallBefore.Spawn.Train.Context!.FindCard(install.CardInstanceId)?.DataId == HallId &&
            prior.FindCard(play.CardInstanceId)?.DataId == AssistantId && hallBefore.Spawn.Turn == 1 &&
            before.Spawn.Turn == 1 && after.Spawn.Turn == 1 && hallBefore.Energy - before.Energy == 2 &&
            before.Energy - after.Energy == 1 && install.RoomIndex == play.RoomIndex &&
            hall.GetProperty("Index").GetInt32() + 1 == birth.GetProperty("Index").GetInt32() &&
            hall.GetProperty("Actual").ContentEquals(birth.GetProperty("Before")) &&
            birth.GetProperty("ActualOutcome").Deserialize<RoomOutcome>() == RoomOutcome.Exchanged &&
            prior.RoomCapacities!.Single(room => room.RoomIndex == play.RoomIndex).Attachments!
                .Any(attachment => attachment.DataId == HallId && attachment.SpawnCopySubtype == ""),
            "Expected one original Hall and one paid Assistant in the same native turn.");
        Require(prior.BattleRng.Equals(actual.BattleRng) && prior.Cards.Rng.Equals(actual.Cards.Rng),
            "The four native births changed Battle or CardDraw RNG.");

        CombatUnit[] units = root.GetProperty("NewUnits").Deserialize<CombatUnit[]>()!.OrderBy(unit => unit.Id).ToArray();
        var settledRoom = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == play.RoomIndex);
        Require(units.Length == 4 && units.Select(unit => unit.Modifiers?.IsClone).SequenceEqual([false, true, false, true]) &&
            units.Select(unit => unit.Status("cardless")?.Stacks ?? 0).SequenceEqual([0, 1, 1, 1]) &&
            units.All(unit => unit.Team == CombatTeam.Player && unit.AssetKey == "MonsterTwistedAssistant" &&
                unit.Modifiers?.Graft?.Card.DataId == GraftId && unit.Status("unit_grafted_equipment")?.Stacks == 1 &&
                unit.Health > 0 && Json(unit) == Json(settledRoom.Units.Single(actor => actor.Id == unit.Id))) &&
            units[0].SpawnerCardId == play.CardInstanceId && prior.NextUnitId == units[0].Id &&
            units.Select(unit => unit.Id).SequenceEqual(Enumerable.Range(units[0].Id, 4)) &&
            prior.LastSpawnedUnitId == 0 && actual.LastSpawnedUnitId == units[3].Id && actual.NextUnitId == units[3].Id + 1,
            "Expected the paid birth, first Hall clone, extra cardless birth and second Hall clone in creation order.");
        var priorPoints = prior.SpawnPoints!.Groups.Single(group => group.RoomIndex == play.RoomIndex &&
            group.Team == CombatTeam.Player && group.PreviewCopyId == null);
        var settledPoints = actual.SpawnPoints!.Groups.Single(group => group.RoomIndex == play.RoomIndex &&
            group.Team == CombatTeam.Player && group.PreviewCopyId == null);
        int[] occupants = [units[0].Id, units[2].Id, units[3].Id, units[1].Id];
        Require(play.PlayerPosition == 0 && priorPoints.Occupants.All(id => id == 0) &&
            settledPoints.Occupants.Take(4).SequenceEqual(occupants) && settledPoints.Occupants.Skip(4).All(id => id == 0),
            "The inserted extra birth and its Hall clone did not retain their native positions.");

        int firstGear = units[0].EquipmentCards!.Single();
        int[] identities = units.SelectMany(unit => new[] { unit.SpawnerCardId, unit.EquipmentCards!.Single() }).ToArray();
        var standby = after.OtherPiles!.Single(pile => pile.Name == "Standby");
        var equipmentEvents = root.GetProperty("Equipment").EnumerateArray().ToArray();
        Require(identities.Distinct().Count() == 8 && prior.FindCard(play.CardInstanceId)?.GraftedEquipmentCardId == firstGear &&
            equipmentEvents.Length == 4, "Each native unit needs a separate source and attached graft.");
        foreach (var (unit, attachment) in units.Zip(equipmentEvents))
        {
            int gear = unit.EquipmentCards!.Single();
            var source = actual.CardRegistry!.Single(card => card.InstanceId == unit.SpawnerCardId);
            var equipped = actual.CardRegistry!.Single(card => card.InstanceId == gear);
            bool paid = unit.Id == units[0].Id;
            Require(source.DataId == AssistantId && source.GraftedEquipmentCardId == firstGear &&
                actual.CardInstances!.Any(card => card.InstanceId == source.InstanceId) == paid &&
                standby.Cards.Any(card => card.InstanceId == source.InstanceId) == paid &&
                equipped.DataId == GraftId && equipped.EquippedUnitId == unit.Id &&
                standby.EquipmentConditions!.Any(condition => condition.CardId == gear && condition.HostUnitId == unit.Id) &&
                attachment.GetProperty("Completed").GetBoolean() && attachment.GetProperty("Grafted").GetBoolean() &&
                !attachment.GetProperty("Remove").GetBoolean() && attachment.GetProperty("UnitId").GetInt32() == unit.Id &&
                attachment.GetProperty("CardId").GetInt32() == gear,
                "Native creation-order graft ownership, source aliases or standby conditions differ.");
        }
        VerifyCloneCallbacks(root, 2);
        VerifyExtraSpawnDefinition(root, root.GetProperty("ExtraSpawnDefinition"));
        Require(owner.ResourceEffects is [{ Conditions: [{ Triggered: false, DurationTriggerCount: 0 }] }] &&
            actual.Relics!.Single(relic => relic.DataId == ExtraSpawnId).ResourceEffects is
                [{ Conditions: [{ Triggered: true, DurationTriggerCount: 1, ValueAtLastTrigger: 0 }] }],
            "The native extra birth did not consume exactly one turn-scoped trigger.");
        BattleActionResult simulated = BattleActionModel.PlayCard(before, play);
        Require(simulated.Supported && simulated.Outcome == RoomOutcome.Exchanged, simulated.Reason ??
            "The model failed to complete the original additional birth.");
        RecordedBattleChecks.Compare(simulated.State!, after, false, "Native DoubleUnits and Hall birth");
        RelicResourceResult sameTurn = RelicUnitSpawnCountModel.Evaluate(actual, CombatTeam.Player);
        RelicResourceResult nextTurn = RelicUnitSpawnCountModel.Evaluate(RelicConditionModel.EndDuration(actual, "ThisTurn"), CombatTeam.Player);
        Require(sameTurn is { Supported: true, Value: 0 } && nextTurn is { Supported: true, Value: 1 },
            "The modeled extra birth failed its same-turn or next-turn guard.");
        Console.WriteLine("NATIVE-HALL-EXTRA-SPAWN PASS: two native Hall clones, distinct grafts and full model state/RNG.");
    }

    private static void VerifyExtraSpawnDefinition(FixtureValue root, FixtureValue definition)
    {
        Require(definition.GetProperty("Id").GetString() == ExtraSpawnId && definition.GetProperty("AssetKey").GetString() == "DoubleUnits" &&
            !definition.GetProperty("DisallowedInPlacementPhase").GetBoolean() &&
            definition.GetProperty("EffectTypes").Deserialize<string[]>()!.SequenceEqual([ExtraSpawnEffect]) &&
            definition.GetProperty("Amount").GetInt32() == 1 && definition.GetProperty("Team").GetString() == "Monsters" &&
            definition.GetProperty("Target").GetString() == "FrontInRoom" &&
            definition.GetProperty("RequiredMutatorId").ValueKind == FixtureKind.Null,
            "Original DoubleUnits owner or effect getters differ.");
        FixtureValue condition = definition.GetProperty("Conditions").EnumerateArray().Single();
        Require(condition.GetProperty("TrackedValue").GetString() == "SubtypeInDeck" &&
            condition.GetProperty("CardType").GetString() == "Any" && condition.GetProperty("TrackTriggerCount").GetBoolean() &&
            condition.GetProperty("Duration").GetString() == "ThisTurn" && condition.GetProperty("Comparator").GetInt32() == 6 &&
            condition.GetProperty("Int").GetInt32() == 1 && !condition.GetProperty("AllowMultiple").GetBoolean() &&
            condition.GetProperty("Subtype").GetString() == "SubtypesData_None", "Original DoubleUnits ordered condition differs.");
        FixtureValue query = root.GetProperty("ExtraSpawnCountQueries").EnumerateArray().Single();
        Require(query.GetProperty("Unit").GetString() == "MonsterTwistedAssistant" && query.GetProperty("Team").GetString() == "Monsters" &&
            query.GetProperty("Turn").GetInt32() == 1 && query.GetProperty("CountBefore").GetInt32() == 1 &&
            query.GetProperty("CountAfter").GetInt32() == 2, "Original paid Assistant count query did not request an additional birth.");
        Console.WriteLine("NATIVE-HALL-EXTRA-SPAWN-DEFINITION PASS: original ordered condition, absent required mutator and spawn count 1 to 2.");
    }

    internal static void NativeOrdinaryExtraSpawn(FixtureValue root)
    {
        Require(root.GetProperty("Scope").GetString() == "NativeOriginalDoubleUnitsOrdinary" &&
            root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            root.GetProperty("CaptureFailures").GetInt32() == 0 && root.GetProperty("Pending").GetInt32() == 0,
            "Expected the original ordinary DoubleUnits component.");
        var plays = root.GetProperty("OrdinaryPlays").EnumerateArray().ToArray();
        var queries = root.GetProperty("ExtraSpawnCountQueries").EnumerateArray().ToArray();
        Require(plays.Length == 4 && queries.Length == plays.Length, "Expected two original unit plays per turn over two turns.");
        var definitions = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < plays.Length; index++)
        {
            FixtureValue row = plays[index], query = queries[index];
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatContext prior = before.Spawn.Train.Context!, settled = after.Spawn.Train.Context!;
            int turn = index / 2 + 1, births = index % 2 == 0 ? 2 : 1;
            var conditionBefore = prior.Relics!.Single(owner => owner.DataId == ExtraSpawnId).ResourceEffects!.Single().Conditions.Single();
            var conditionAfter = settled.Relics!.Single(owner => owner.DataId == ExtraSpawnId).ResourceEffects!.Single().Conditions.Single();
            CombatUnit[] born = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex).Units
                .Where(unit => unit.Id >= before.Spawn.NextUnitId).OrderBy(unit => unit.Id).ToArray();
            Require(before.Spawn.Turn == turn && after.Spawn.Turn == turn && before.Energy - after.Energy == 1 &&
                before.PlayRules!.Rooms.All(room => prior.RoomCapacities!.Single(capacity => capacity.RoomIndex == room.RoomIndex)
                    .Attachments?.Any(attachment => attachment.DataId == HallId) != true) &&
                query.GetProperty("Turn").GetInt32() == turn && query.GetProperty("Team").GetString() == "Monsters" &&
                query.GetProperty("CountBefore").GetInt32() == 1 && query.GetProperty("CountAfter").GetInt32() == births &&
                conditionBefore.DurationTriggerCount == (births == 2 ? 0 : 1) && conditionAfter.DurationTriggerCount == 1 &&
                born.Length == births && born[0].SpawnerCardId == action.CardInstanceId &&
                born.All(actor => actor.Modifiers is { IsClone: false, Graft: null }),
                "Original ordinary birth count, source, payment or turn-scoped trigger diverged on play " + (index + 1));
            definitions.Add(prior.FindCard(action.CardInstanceId)!.DataId);
            if (births == 2)
            {
                var detached = settled.FindCard(born[1].SpawnerCardId);
                Require(born[0].Status("cardless") == null && born[1].Status("cardless")?.Stacks == 1 &&
                    detached?.DataId == prior.FindCard(action.CardInstanceId)?.DataId &&
                    settled.CardInstances!.All(card => card.InstanceId != detached!.InstanceId),
                    "The additional ordinary unit needs its own detached spawner and Cardless status.");
            }
            BattleActionResult result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && result.Outcome == row.GetProperty("ActualOutcome").Deserialize<RoomOutcome>(),
                result.Reason ?? "Ordinary DoubleUnits replay failed on play " + (index + 1));
            RecordedBattleChecks.Compare(result.State!, after, false, "Ordinary DoubleUnits play " + (index + 1));
        }
        Require(definitions.Count == 2, "Expected two distinct original ordinary card definitions.");
        Console.WriteLine("NATIVE-DOUBLE-UNITS-ORDINARY PASS: four full native successors, first-play extra births, same-turn suppression and reset.");
    }

    internal static void NativeSpawnOrStatusExtraSpawn(FixtureValue root)
    {
        Require(root.GetProperty("Scope").GetString() == "NativeOriginalDoubleUnitsSpawnOrStatus" &&
            root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            root.GetProperty("CaptureFailures").GetInt32() == 0 && root.GetProperty("Pending").GetInt32() == 0,
            "Expected the original DoubleUnits spawn-or-status component.");
        var plays = root.GetProperty("OrdinaryPlays").EnumerateArray().ToArray();
        var queries = root.GetProperty("ExtraSpawnCountQueries").EnumerateArray().ToArray();
        Require(plays.Length == 2 && queries.Length == 2 &&
            plays[0].GetProperty("Actual").ContentEquals(plays[1].GetProperty("Before")),
            "Expected two consecutive original Eager Conscript plays.");
        int bornId = 0;
        for (int index = 0; index < plays.Length; index++)
        {
            FixtureValue row = plays[index], query = queries[index];
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatContext prior = before.Spawn.Train.Context!, actual = after.Spawn.Train.Context!;
            CombatUnit[] funguy = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex).Units
                .Where(unit => unit.AssetKey == "MonsterFunguy").ToArray();
            Require(before.Spawn.Turn == 1 && after.Spawn.Turn == 1 && before.Energy - after.Energy == 1 &&
                prior.FindCard(action.CardInstanceId)?.DataId == "0c8ff108-478c-4c1c-9c49-f6a297b4e0d8" &&
                query.GetProperty("Unit").GetString() == "MonsterFunguy" && query.GetProperty("Turn").GetInt32() == 1 &&
                query.GetProperty("Team").GetString() == "Monsters" && query.GetProperty("CountBefore").GetInt32() == 1 &&
                query.GetProperty("CountAfter").GetInt32() == 2 - index &&
                funguy is [{ SpawnerCardId: 0 }] && funguy[0].Status("horde")?.Stacks == 2 + index &&
                prior.BattleRng.Equals(actual.BattleRng) && prior.Cards.Rng.Equals(actual.Cards.Rng),
                "Expected a single cardless Funguy with 2 then 3 Horde and unchanged gameplay RNG.");
            if (index == 0) bornId = funguy[0].Id;
            else Require(funguy[0].Id == bornId && prior.NextUnitId == actual.NextUnitId,
                "The second original card must grow the same Funguy.");
            BattleActionResult result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && result.Outcome == row.GetProperty("ActualOutcome").Deserialize<RoomOutcome>(),
                result.Reason ?? "DoubleUnits spawn-or-status replay failed on play " + (index + 1));
            RecordedBattleChecks.Compare(result.State!, after, false, "DoubleUnits spawn-or-status play " + (index + 1));
        }
        Console.WriteLine("NATIVE-DOUBLE-UNITS-SPAWN-OR-STATUS PASS: one Funguy with 2 then 3 Horde, exact model state and RNG.");
    }

    private static void VerifyCloneCallbacks(FixtureValue root, int count = 1)
    {
        FixtureValue[] observations = root.GetProperty("CloneObservations").EnumerateArray().ToArray();
        Require(observations.Length == count, "Unexpected number of completed native CloneMonsterState coroutines.");
        foreach (var observation in observations) VerifyCloneCallback(observation);
        Console.WriteLine("NATIVE-HALL-GRAFT-ORDER PASS: source copy, cardless birth, graft copy, graft attachment, exact native object aliases.");
    }

    private static void VerifyCloneCallback(FixtureValue observation)
    {
        Require(observation.GetProperty("Completed").GetBoolean(), "Native CloneMonsterState coroutine did not complete.");
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
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
