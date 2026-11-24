using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ShardtailImpCostChecks
{
    internal static void Native(FixtureValue deployment, FixtureValue observation)
    {
        Require(observation.GetProperty("Boundary").GetString() == "OriginalShardtailImpCost" &&
            observation.GetProperty("Champion").GetString() == CardShardtailImpCostModel.CardId &&
            observation.GetProperty("Upgrade").GetString() == CardShardtailImpCostModel.UpgradeId,
            "Wrong native Shardtail room-cost fixture.");
        var before = observation.GetProperty("BeforeDeployment");
        var deployed = observation.GetProperty("AfterDeployment");
        var beforeImp = observation.GetProperty("BeforeImpPlay");
        var played = observation.GetProperty("AfterImpPlay");
        int floor = observation.GetProperty("Floor").GetInt32();
        int alternateFloor = observation.GetProperty("ControlFloor").GetInt32();
        string impDataId = observation.GetProperty("Imp").GetString();
        VerifyCost(before, 1, 1);
        VerifyCost(deployed, 0, 1);
        VerifyCost(beforeImp, 0, 1);
        Require(before.GetProperty("Rng").ContentEquals(deployed.GetProperty("Rng")) &&
            beforeImp.GetProperty("Rng").ContentEquals(played.GetProperty("Rng")) &&
            beforeImp.GetProperty("Imp").GetProperty("Card").ContentEquals(played.GetProperty("Card")) &&
            beforeImp.GetProperty("Energy").ContentEquals(played.GetProperty("Energy")) && played.GetProperty("Paid").GetInt32() == 0,
            "Native query, RNG or zero-energy Imp execution differs.");

        var root = deployment.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var card = root.Spawn.Train.Context!.CardInstances!.Single(item => item.DataId == CardShardtailImpCostModel.CardId);
        var upgrade = CardShardtailImpCostModel.PermanentSource(card);
        Require(upgrade != null && CardModifierModel.UpgradeInteractions(upgrade).Contains(CardCorruptionAttackUpgradeModel.Guard),
            "Only the original permanent card birth can handle the room modifier.");
        var changed = card.Permanent.Upgrades.Select(item => ReferenceEquals(item, upgrade)
            ? item.WithStats(new CardStatModifier(damage: 21, health: 5)) : item).ToArray();
        var mutated = card.WithModifiers(new CardModifiers(card.Permanent.Offsets, changed, card.Permanent.PersistentHealth,
            card.Permanent.ExternalInteractions), card.Temporary, refreshTraits: false);
        Require(CardShardtailImpCostModel.PermanentSource(mutated) == null, "A changed original upgrade must remain guarded.");

        string rootKey = JsonSerializer.Serialize(root, ModelJson.Options);
        var birth = BattleActionModel.PlayCard(root, new PlayCardAction(card.InstanceId, floor, 0));
        Require(birth.Supported, birth.Reason ?? "Original champion could not deploy.");
        var state = birth.State!;
        var champion = state.Spawn.Train.Rooms.Single(room => room.RoomIndex == floor).Units.Single(unit => unit.SpawnerCardId == card.InstanceId);
        Require(champion.Modifiers!.RoomResources!.Single() is { Type: UnitRoomResourceModel.SubtypeCost, Amount: -1,
            DisallowedInDeployment: false, Subtype: CardShardtailImpCostModel.Subtype }, "Projected native champion birth lacks its imp aura.");
        var train = state.Spawn.Train;
        int controlId = deployed.GetProperty("Control").GetProperty("Card").GetInt32();
        string controlDataId = train.Context!.FindCard(controlId)!.DataId;
        var control = state.PlayRules!.Cards.Single(rule => rule.DataId == controlDataId);
        var allSubtypes = new CombatRelicState(OriginalAllSubtypesModel.OwnerId, OriginalAllSubtypesModel.OwnerKey,
            [OriginalAllSubtypesModel.Effect], isCovenant: false, disallowedInPlacementPhase: false);
        var mutatedTrain = CardSpellModel.WithContext(train, train.Context!.WithRelics(train.Context.Relics!.Append(allSubtypes).ToArray()));
        Require(control.CardType == "Monster" && UnitRoomResourceModel.UnitCostDelta(train, floor, control) == 0 &&
            UnitRoomResourceModel.UnitCostDelta(mutatedTrain, floor, control) == -1 &&
            UnitRoomResourceModel.UnitCostDelta(mutatedTrain, alternateFloor, control) == 0,
            "The native aura must include a non-Imp Monster card template only on its own floor under the original mutator.");
        int combatTurn = beforeImp.GetProperty("Turn").GetInt32();
        while (state.Spawn.Turn < combatTurn)
        {
            var next = BattleTurnModel.EndTurn(state);
            Require(next.Supported && next.State!.Spawn.Turn > state.Spawn.Turn, next.UnsupportedReason ?? "Original turn could not advance.");
            state = next.State!;
        }
        int imp = state.Spawn.Train.Context!.Cards!.Hand.First(item => item.DataId == impDataId).InstanceId;
        string parent = JsonSerializer.Serialize(state, ModelJson.Options);
        Parallel.For(0, 4, index =>
        {
            int room = index % 2 == 0 ? floor : alternateFloor;
            var result = BattleActionModel.PlayCard(state, new PlayCardAction(imp, room, room == floor ? 1 : 0));
            int expected = beforeImp.GetProperty("Imp").GetProperty(room == floor ? "Target" : "Alternate").GetInt32();
            Require(result.Supported && state.Energy - result.State!.Energy == expected,
                result.Reason ?? "Model Imp payment differs from the original room cost.");
            Require(result.State!.Spawn.Train.Context!.FindCard(imp)!.LastPlayedCost == expected,
                "Model paid-cost statistics differ from the original.");
        });
        Require(rootKey == JsonSerializer.Serialize(root, ModelJson.Options) && parent == JsonSerializer.Serialize(state, ModelJson.Options),
            "Shardtail branch changed its original snapshot.");
        Console.WriteLine("SHARDTAIL-IMP-COST PASS: genuine Ring4 deployment, native 1/0 same-floor and 1 alternate cost, non-Imp control, zero-energy paid play/RNG and four immutable model branches.");
    }

    private static void VerifyCost(FixtureValue row, int target, int alternate)
    {
        var imp = row.GetProperty("Imp");
        var control = row.GetProperty("Control");
        Require(imp.GetProperty("Base").GetInt32() == 1 && imp.GetProperty("Target").GetInt32() == target &&
            imp.GetProperty("Alternate").GetInt32() == alternate && imp.GetProperty("TargetDelta").GetInt32() == target - 1 &&
            imp.GetProperty("AlternateDelta").GetInt32() == alternate - 1 && control.GetProperty("TargetDelta").GetInt32() == 0 &&
            control.GetProperty("AlternateDelta").GetInt32() == 0 && control.GetProperty("Base").ContentEquals(control.GetProperty("Target")) &&
            control.GetProperty("Base").ContentEquals(control.GetProperty("Alternate")), "Wrong native Imp or non-Imp cost control.");
    }

    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
