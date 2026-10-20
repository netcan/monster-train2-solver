using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RoomSpawnCopyChecks
{
    internal static void Native(FixtureValue root, IReadOnlyCollection<string> originals)
    {
        UnitNative(root, originals);
        if (!originals.Contains("83734ffd-cbaa-47f1-9809-cc4c4dac568d")) return;
        int positive = 0, replaced = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            if (before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.DataId != "63a48bad-07da-4687-bfef-2f7c6653af5e") continue;
            CombatContext prior = before.Spawn.Train.Context, next = after.Spawn.Train.Context!;
            bool active = prior.RoomCapacities!.Single(room => room.RoomIndex == action.RoomIndex).Attachments!
                .Any(attachment => attachment.SpawnCopySubtype != null);
            CombatUnit[] born = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit =>
                before.Spawn.Train.Rooms.SelectMany(room => room.Units).All(old => old.Id != unit.Id)).ToArray();
            if (active)
            {
                Require(born.Length == 2 && born.All(unit => unit.AssetKey == "Dreg") && next.NextUnitId == prior.NextUnitId + 2 &&
                    next.NextCardId == prior.NextCardId + 1, "Hall of Mirrors must allocate one ordinary Dreg, one clone and one detached copied source card.");
                CombatUnit original = born.Single(unit => unit.SpawnerCardId == action.CardInstanceId), clone = born.Single(unit => unit.Modifiers!.IsClone);
                Require(clone.Status("cardless")?.Stacks == 1 && clone.SpawnerCardId != original.SpawnerCardId &&
                    next.FindCard(clone.SpawnerCardId)?.DataId == next.FindCard(original.SpawnerCardId)?.DataId &&
                    clone.Health == original.Health && clone.MaxHealth == original.MaxHealth && clone.BaseAttack == original.BaseAttack &&
                    clone.Status("burnout")?.Stacks == original.Status("burnout")?.Stacks,
                    "Native birth copying must preserve original stats/statuses and independent source identity, with clone/cardless markers.");
                positive++;
            }
            else
            {
                Require(born.Length == 1 && !born[0].Modifiers!.IsClone && next.NextCardId == prior.NextCardId,
                    "Replacing Hall of Mirrors must remove its birth-copy callback.");
                replaced++;
            }
            foreach (RoomCombatState room in before.Spawn.Train.Rooms.Where(room => room.RoomIndex != action.RoomIndex))
                Require(after.Spawn.Train.Rooms.Single(next => next.RoomIndex == room.RoomIndex).Units.Select(unit => unit.Id).SequenceEqual(room.Units.Select(unit => unit.Id)),
                    "Room birth copies must leave other floors unchanged.");
        }
        Require(positive + replaced > 0, "Hall of Mirrors needs an original paid birth witness.");
        Require(originals.Contains("6bc6afc8-a113-4f03-b090-d4436f41d822") ? replaced > 0 : positive > 0,
            "Hall of Mirrors positive fixture must execute its native copy, and the replacement fixture must disable it.");
        Console.WriteLine($"ORIGINAL-ROOM-BIRTH-COPY PASS: {positive} positive births, {replaced} replacement controls, independent native identity/stat/status deltas.");
    }
    private static void UnitNative(FixtureValue root, IReadOnlyCollection<string> originals)
    {
        if (!originals.Contains("d255dfe4-3fa7-4a47-873a-84d441889544")) return;
        int positive = 0, otherFloor = 0, otherSubtype = 0, self = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatContext prior = before.Spawn.Train.Context!, next = after.Spawn.Train.Context!;
            CardPlayRule definition = before.PlayRules!.Cards.Single(rule => rule.DataId == prior.FindCard(action.CardInstanceId)!.DataId);
            if (definition.SpawnUnit == null || !originals.Contains(definition.DataId)) continue;
            CombatUnit[] born = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit =>
                before.Spawn.Train.Rooms.SelectMany(room => room.Units).All(old => old.Id != unit.Id)).ToArray();
            if (definition.AssetKey == "MorselMiner")
            {
                Require(action.RoomIndex == 0 && before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Any(unit =>
                    unit.Modifiers?.SpawnCopySubtype == "SubtypesData_Snack"), "A paid food unit must execute the owner's native same-floor copy callback.");
                Require(born.Length == 2 && next.NextUnitId == prior.NextUnitId + 2 && next.NextCardId == prior.NextCardId + 1,
                    "Morsel copying must allocate exactly one clone and one detached source card.");
                CombatUnit original = born.Single(unit => unit.SpawnerCardId == action.CardInstanceId), clone = born.Single(unit => unit.Modifiers!.IsClone);
                Require(clone.Status("cardless")?.Stacks == 1 && clone.SpawnerCardId != original.SpawnerCardId &&
                    next.FindCard(clone.SpawnerCardId)?.DataId == definition.DataId && clone.Health == original.Health &&
                    clone.MaxHealth == original.MaxHealth && clone.BaseAttack == original.BaseAttack &&
                    original.Statuses.All(status => clone.Status(status.Id)?.Stacks == status.Stacks),
                    "Native morsel copies need independent source identity and preserved stats/statuses with clone/cardless markers.");
                positive++;
            }
            else
            {
                Require(born.Length == 1 && !born[0].Modifiers!.IsClone && next.NextCardId == prior.NextCardId,
                    "Unit birth copying must exclude self births, other subtypes and other floors.");
                if (definition.AssetKey == "CopyMorselsUnit")
                {
                    Require(born[0].Modifiers!.SpawnCopySubtype == "SubtypesData_Snack", "The owner must retain its native food subtype filter.");
                    self++;
                }
                if (definition.DataId == "63a48bad-07da-4687-bfef-2f7c6653af5e") otherSubtype++;
                if (definition.AssetKey == "ShadowMorsel")
                { Require(action.RoomIndex == 1, "The food floor-exclusion control must be on another floor."); otherFloor++; }
            }
        }
        Require(positive > 0 && otherFloor > 0 && otherSubtype > 0 && self > 0,
            "Copy Morsels needs a positive food birth plus self, subtype and other-floor controls.");
        Console.WriteLine($"ORIGINAL-UNIT-BIRTH-COPY PASS: {positive} food births, {self} self, {otherSubtype} subtype and {otherFloor} floor controls.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
