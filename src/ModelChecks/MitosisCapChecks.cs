using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class MitosisCapChecks
{
    internal static void Native(FixtureValue root, int[] ids)
    {
        FixtureValue[] records = root.GetProperty("OriginalEquipmentGrowthEncounterComplete").EnumerateArray().ToArray();
        Require(records.Length == 1 && records[0].GetProperty("Completed").GetBoolean() &&
            records[0].GetProperty("CaptureError").GetString() == null, "Mitosis needs a completed native encounter callback.");
        TrainCombatState before = records[0].GetProperty("Before").Deserialize<TrainCombatState>()!, after = records[0].GetProperty("After").Deserialize<TrainCombatState>()!;
        int[] order = records[0].GetProperty("ActiveMonsterOrder").Deserialize<int[]>()!;
        TrainCombatResult replay = EncounterCompleteModel.Apply(before, order);
        Require(replay.Supported, "Mitosis encounter callback is unsupported: " + replay.UnsupportedReason);
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(replay.State, ModelJson.Options), JsonSerializer.Serialize(after, ModelJson.Options));
        Require(difference == null, "Complete native Mitosis encounter state differs: " + difference);
        CombatContext a = before.Context!, b = after.Context!;
        CombatUnit host = before.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.SpawnerCardId == ids[0]);
        Require(host.EquipmentCards!.Contains(ids[4]) && host.MaxHealth == 10015 && host.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "MitosisCapUpgrade"),
            "The original Mitosis attachment must grant its host fifteen health and the encounter callback.");
        CardInstanceState copied = b.FindCard(a.NextCardId)!;
        Require(b.NextCardId == a.NextCardId + 1 && copied.DataId == a.FindCard(ids[0])!.DataId &&
            b.Cards.Discard.Any(card => card.InstanceId == copied.InstanceId) && b.CardInstances!.Count == a.CardInstances!.Count + 1 &&
            b.Statistics!.DeckCards!.Count == a.Statistics!.DeckCards!.Count + 1 &&
            b.Statistics.DeckCards.Contains(copied.InstanceId) && b.Cards.Rng.Equals(a.Cards.Rng) && b.BattleRng.Equals(a.BattleRng),
            "Mitosis must add one independently identified permanent source-card copy to discard and the run deck without random selection.");
        Require(after.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == host.Id).Triggers.Any(trigger =>
            trigger.Kind == "OnEncounterComplete" && trigger.HasTriggered && trigger.Origin?.EquipmentCardId == ids[4]),
            "Mitosis needs an observed original equipment encounter callback.");
        BattleTurnState initial = root.GetProperty("Actions")[4].GetProperty("Before").Deserialize<BattleTurnState>()!;
        CardActionEffect attach = initial.PlayRules!.Cards.Single(rule => rule.DataId == "c71b2d2b-a2c5-449f-8cf5-2abef684ff33").Effects.Single();
        Require(attach.UpgradeFilters.Count == 1 && attach.UpgradeFilters[0].Subtypes.Excluded.Contains("SubtypesData_Champion_83f21cbe-9d9b-4566-a2c3-ca559ab8ff34") &&
            attach.UpgradeFilters[0].Statuses.Excluded.Single().Id == "cardless", "Mitosis must retain its native champion and cardless equipment filters.");
        Console.WriteLine("ORIGINAL-MITOSIS-CAP PASS: native permanent source-card copy, ownership/deck/pile identities, equipment filters and full encounter callback state.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
