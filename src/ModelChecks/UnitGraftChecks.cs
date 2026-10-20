using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnitGraftChecks
{
    private static readonly string[] Owners = ["31faf1a8-4838-4c5f-9c4b-605baa37b5ef", "3a8240bd-8b96-4d6e-b2d1-49ff8b5d8022",
        "7227d3ea-343a-45b2-ae2c-6f05383fcc7c", "d105aac6-843e-43eb-b1b0-098302e7a09a", "eee797b2-ff2a-4d2d-9b23-d62a114388a5"];
    internal static void Native(FixtureValue root, IReadOnlyCollection<string> originals)
    {
        string[] expected = Owners.Intersect(originals).ToArray();
        if (expected.Length == 0) return;
        var births = new HashSet<string>();
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardInstanceState source = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
            if (!expected.Contains(source.DataId)) continue;
            CombatContext next = after.Spawn.Train.Context!;
            CardInstanceState played = next.FindCard(source.InstanceId)!;
            CombatUnit host = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == source.InstanceId);
            int equipmentId = played.GraftedEquipmentCardId ?? throw new InvalidOperationException("Missing graft source reference.");
            CardInstanceState gear = next.FindCard(equipmentId)!;
            Require(equipmentId != source.InstanceId && host.EquipmentCards!.SequenceEqual(new[] { equipmentId }) &&
                gear.EquippedUnitId == host.Id && host.Status("unit_grafted_equipment")?.Stacks == 1 &&
                gear.DataId == host.Modifiers!.Graft!.Card.DataId && CardGraftedEquipmentModel.Validate(gear) == null &&
                gear.MaskDescriptor!.BaseTraits.Single(trait => trait.RuntimeType == CardGraftedEquipmentModel.Trait).GraftSevered == false,
                "Birth must attach the source-linked intact authored graft with an independent card identity and native marker.");
            CardPileState standby = next.OtherPiles!.Single(pile => pile.Name == "Standby");
            Require(next.CardInstances!.Any(card => card.InstanceId == equipmentId) &&
                standby.Cards.Any(card => card.InstanceId == equipmentId) && standby.EquipmentConditions!.Any(condition =>
                    condition.CardId == equipmentId && condition.HostUnitId == host.Id && !condition.ReturnToHand),
                "Graft must become an owned standby card bound to its host, independently of the source unit card.");
            foreach (CombatUnit control in before.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.Modifiers?.Graft == null))
            {
                CombatUnit current = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == control.Id);
                Require(current.Status("unit_grafted_equipment") == null && current.EquipmentCards!.SequenceEqual(control.EquipmentCards!),
                    "Automatic graft must preserve unrelated units' equipment and marker state.");
            }
            Require(births.Add(source.DataId), "Duplicate original birth witness.");
        }
        int returns = 0;
        foreach (FixtureValue row in root.GetProperty("Stages").EnumerateArray())
        {
            RoomCombatState before = row.GetProperty("Before").Deserialize<RoomCombatState>()!, after = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            foreach (CombatUnit host in before.Units.Where(unit => unit.Modifiers?.Graft != null && !after.Units.Any(next => next.Id == unit.Id)))
            foreach (int id in host.EquipmentCards!)
            {
                // Native attack/kill-camera snapshots can temporarily clear the
                // owned piles; the enclosing exchange observes the standby return.
                if (!after.Context!.CardInstances!.Any(card => card.InstanceId == id)) continue;
                CardInstanceState gear = after.Context!.FindCard(id)!;
                Require(after.Context.Cards.Hand.Concat(after.Context.Cards.Draw).Any(card => card.InstanceId == id) &&
                    gear.MaskDescriptor!.BaseTraits.Single(trait => trait.RuntimeType == CardGraftedEquipmentModel.Trait).GraftSevered == true,
                    "Host death must return its intact graft to hand/deck and sever the original source link.");
                returns++;
            }
        }
        Require(births.SetEquals(expected) && returns > 0, "Missing original graft births or native host-death return control.");
        EquipmentChecks.Native(root);
        Console.WriteLine("ORIGINAL-UNIT-GRAFT PASS: " + births.Count + " unchanged original births, independent source/equipment ownership, native marker, unrelated controls and " + returns + " host-death returns.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
