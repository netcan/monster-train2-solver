using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EventGraftChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing the settled original native encounter.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int births = 0, attached = 0, severed = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardInstanceState source = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
            if (source?.DataId != CardGraftedEquipmentModel.EventHostId) continue;
            Require(source.GraftedEquipmentCardId > 0 && source.MaskDescriptor?.GraftedEquipment?.Card.DataId ==
                CardGraftedEquipmentModel.EventEquipmentId, "Missing instance-owned event equipment definition/reference.");
            BattleTurnState after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatUnit unit = after.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Single(unit => unit.SpawnerCardId == source.InstanceId && unit.Health > 0);
            CardInstanceState equipment = after.Spawn.Train.Context!.FindCard(source.GraftedEquipmentCardId.Value)!;
            Require(unit.Modifiers?.Graft?.Card.DataId == CardGraftedEquipmentModel.EventEquipmentId &&
                equipment.MaskDescriptor?.BaseTraits.Count == 0 && CardGraftedEquipmentModel.Validate(equipment) == null,
                "Missing the original event-graft unit definition or ordinary equipment trait state.");
            if (equipment.EquippedUnitId == unit.Id)
            {
                Require(unit.EquipmentCards!.Contains(equipment.InstanceId) && unit.Status("unit_grafted_equipment")?.Stacks == 1 &&
                    equipment.MaskDescriptor!.TemporaryTraits.Single(trait => trait.RuntimeType == CardGraftedEquipmentModel.Trait).GraftSevered == false &&
                    after.Spawn.Train.Context.OtherPiles!.Single(pile => pile.Name == "Standby").EquipmentConditions!
                        .Any(condition => condition.CardId == equipment.InstanceId && condition.HostUnitId == unit.Id),
                    "Missing temporary graft trait, native status, equipment ownership or standby identity.");
                attached++;
            }
            births++;
        }
        foreach (FixtureValue row in root.GetProperty("Turns").EnumerateArray())
        {
            BattleTurnState state = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            severed += state.Spawn.Train.Context!.CardRegistry!.Count(card => card.DataId == CardGraftedEquipmentModel.EventEquipmentId &&
                card.MaskDescriptor?.TemporaryTraits.Any(trait => trait.RuntimeType == CardGraftedEquipmentModel.Trait && trait.GraftSevered == true) == true);
        }
        Require(births == 2 && attached == 1 && severed > 0,
            "Missing native initial attachment, severed equipment or subsequent host rebirth.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine($"EVENT-GRAFT PASS births={births} attached={attached} severedSnapshots={severed}: unchanged Firebrand/MeatShield, temporary trait, physical birth, ownership and immutable complete native chains.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
