using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EquipmentConsumptionChecks
{
    private const string Hydra = "6d6c72ab-890b-4275-bb52-2544befbc12b", Soul = "bee94cde-06f4-4bee-b616-8d1116fbb803";
    internal static void Native(FixtureValue root, IReadOnlyCollection<string> originals)
    {
        if (!originals.Contains(Hydra) && !originals.Contains(Soul)) return;
        int consumed = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatUnit? host = before.Spawn.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == action.TargetUnitId);
            if (host?.AssetKey != "MonsterHydraArmedHorror") continue;
            CombatUnit next = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == host.Id);
            CardInstanceState card = after.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
            Require(next.Status("multistrike")?.Stacks == (host.Status("multistrike")?.Stacks ?? 0) + 1 &&
                next.EquipmentCards!.Count == 0 && card.EquippedUnitId == 0 && next.Attack == host.Attack &&
                after.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Any(token => token.InstanceId == card.InstanceId),
                "Hydra must retain one Multistrike, remove the new weapon and its stats, and leave its detached source in native Standby until removal processing.");
            consumed++;
        }
        if (originals.Contains(Hydra)) Require(consumed > 0, "Missing Hydra equipment-consumption witness.");
        int souls = 0, hoard = 0;
        if (originals.Contains(Soul))
        {
            FixtureValue[] records = root.GetProperty("OriginalEquipmentGrowthEncounterComplete").EnumerateArray().ToArray();
            Require(records.Length == 1 && records[0].GetProperty("Completed").GetBoolean(), "Missing complete Soulsmelter settlement.");
            TrainCombatState before = records[0].GetProperty("Before").Deserialize<TrainCombatState>()!,
                after = records[0].GetProperty("After").Deserialize<TrainCombatState>()!;
            TrainCombatResult predicted = EncounterCompleteModel.Apply(before, records[0].GetProperty("ActiveMonsterOrder").Deserialize<int[]>()!);
            Require(predicted.Supported, predicted.UnsupportedReason ?? "Unsupported Soulsmelter encounter completion.");
            Require(ModelJson.Difference(Json(predicted.State), Json(after)) == null, "Soulsmelter complete native settlement differs.");
            foreach (CombatUnit unit in before.Rooms.SelectMany(room => room.Units))
            foreach (CombatEffect effect in unit.Triggers.Where(trigger => trigger.Kind == "OnEncounterComplete").SelectMany(trigger => trigger.Effects)
                .Where(effect => effect.Type == "CardEffectAdjustDragonsHoardPerStatusEffect"))
            {
                int count = unit.Status("captured_soul")?.Stacks ?? 0;
                souls += count; hoard += count / effect.Action!.Statuses.Single().Stacks;
            }
            Require(souls > 0 && hoard > 0 && after.Context!.QueryFrame!.DragonsHoard == Math.Min(
                before.Context!.Resources!.DragonsHoardCap!.Value, before.Context.QueryFrame!.DragonsHoard!.Value + hoard),
                "Soulsmelter must positively collect native enemy deaths and award the integer status quotient within the Hoard cap.");
        }
        Console.WriteLine("ORIGINAL-EQUIPMENT-CONSUMPTION PASS: " + consumed + " Hydra weapon conversions, " + souls + " collected souls and " + hoard + " encounter-complete Hoard; full native settlement and immutable battle chains.");
    }
    private static string Json(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
