using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RedCrownEquipmentChecks
{
    internal static void Native(FixtureValue archive)
    {
        FixtureValue request = archive.GetProperty("Request");
        Require(NativeBranchIdentity.ObservationModule(archive) == new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") &&
            archive.GetProperty("NativeCompleted").GetBoolean() && request.GetProperty("Prefix").GetArrayLength() == 0 &&
            archive.GetProperty("Rows").GetArrayLength() == 1, "Expected the original one-input red crown observation.");
        FixtureValue player = archive.GetProperty("PlayerAdvisor");
        FixtureValue overlay = player.GetProperty("Overlay");
        Require(player.GetProperty("Assembly").GetString() == "MonsterTrain2Poju.Advisor" &&
            overlay.GetProperty("Captures").GetInt32() > 0 && overlay.GetProperty("PurityFailures").GetInt32() == 0 &&
            overlay.GetProperty("Failures").GetInt32() == 0 && !overlay.GetProperty("InTestScenario").GetBoolean(),
            "The original player capture was not pure.");
        FixtureValue row = archive.GetProperty("Rows")[0];
        FixtureValue witness = archive.GetProperty("RedCrownEquipment");
        Require(row.GetProperty("Action").ContentEquals(request.GetProperty("Action")) && row.GetProperty("Boundary").GetBoolean() &&
            !row.GetProperty("IsEndTurn").GetBoolean() && row.GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null,
            "The original red crown deployment was not predicted.");
        var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var expected = row.GetProperty("Expected").Deserialize<BattleTurnState>()!;
        var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        BattleTurnState replayed = BattlePlannerChecks.Replay(before, [new BattleSearchStep(action, false)], out RoomOutcome outcome);
        string actualKey = NativeBranchObservationChecks.Key(actual);
        Require(outcome == RoomOutcome.Exchanged && row.GetProperty("Outcome").GetInt32() == (int)outcome &&
            parent == JsonSerializer.Serialize(before, ModelJson.Options) && NativeBranchObservationChecks.Key(expected) == actualKey,
            "Red crown mutated its source or differs from the recorded forecast.");
        string replayedKey = NativeBranchObservationChecks.Key(replayed);
        Require(replayedKey == actualKey, "Red crown complete replay differs: " + ModelJson.Difference(replayedKey, actualKey));

        int equipmentId = witness.GetProperty("CardId").GetInt32();
        int hostId = witness.GetProperty("HostUnitId").GetInt32();
        string dataId = witness.GetProperty("SelectedCardDataId").GetString()!;
        string[] pool = witness.GetProperty("OrderedCardDataIds").Deserialize<string[]>()!;
        CombatContext start = before.Spawn.Train.Context!;
        CombatContext finish = actual.Spawn.Train.Context!;
        var effect = start.FindCard(action.CardInstanceId)!.Permanent.Upgrades.SelectMany(upgrade => upgrade.TriggerUpgrades ?? [])
            .SelectMany(trigger => trigger.Effects).Single(effect => effect.Type == EquipmentPoolGrantModel.NativeEffect);
        Require(effect.Generation!.Pool.Select(card => card.DataId).SequenceEqual(pool) &&
            pool.All(id => start.SummonCatalog!.Cards.Any(card => card.Creation.DataId == id && card.GeneratedEquipmentRule?.Equipment != null)),
            "Native selection pool and forecast equipment templates differ.");
        RngDraw selection = start.BattleRng.Range(0, pool.Length);
        Require(pool[selection.Value] == dataId && selection.State.Equals(finish.BattleRng) &&
            start.Cards.Rng.Equals(finish.Cards.Rng), "Red crown selection did not advance only the original battle stream.");
        var host = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == hostId);
        var standby = actual.OtherPiles.Single(pile => pile.Name == "Standby");
        Require(host.SpawnerCardId == action.CardInstanceId && host.EquipmentCards!.SequenceEqual([equipmentId]) &&
            standby.EquipmentConditions!.Any(condition => condition.CardId == equipmentId && condition.HostUnitId == hostId && !condition.ReturnToHand) &&
            standby.Cards.Select(card => card.InstanceId).SequenceEqual([equipmentId, action.CardInstanceId]) &&
            !before.PlayRules!.Cards.Any(card => card.DataId == dataId) && actual.PlayRules!.Cards.Any(card => card.DataId == dataId),
            "The original attachment, standby order or selected play rules differ.");
        Console.WriteLine($"RED-CROWN-EQUIPMENT PASS pool={pool.Length} selectedIndex={selection.Value} fullStateAndRng=true");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
