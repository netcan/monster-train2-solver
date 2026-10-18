using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalHealingCleanseChecks
{
    private static readonly Dictionary<int, string> Originals = new()
    {
        [4] = "42d238d8-7180-48b0-b0e8-51f2bbb7ffb4", [7] = "d81f1f43-f20e-4459-9fc4-19e5836a876f",
        [8] = "d4366b7c-588c-49aa-b2c6-2aeb30b0e01b", [9] = "8de281af-ab9a-4d3d-ac5f-89cd43ff6c84",
        [11] = "a82b7b1b-31f1-4634-9cf3-197d17d61ba3", [12] = "2d5f5f3d-fefb-448c-a819-4c82b0c6b17b",
        [13] = "974f9021-d1e3-4b8b-b348-d737a1868ed1", [14] = "8de281af-ab9a-4d3d-ac5f-89cd43ff6c84",
        [15] = "4c0f6e0e-ac05-4e0c-a478-590ac06d646f", [16] = "d861b6a6-d8ef-4d58-8ebd-9f0abc276456",
        [17] = "b1c7b06f-a7fa-491a-804d-9290b87494da", [18] = "e2a9868d-1f0e-47b1-abe4-490ee940270b"
    };
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
        int room = index is 2 or 6 or 18 ? 1 : 0;
        int target = index is 4 or 7 or 8 or 9 or 12 or 15 or 17 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]).Id :
            index is 11 or 14 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack").Id :
            index == 18 ? source.Spawn.Train.Rooms[1].Units.Single(unit => unit.SpawnerCardId == ids[2]).Id : 0;
        var action = new PlayCardAction(ids[index], room, playerPosition: index < 3 ? (index == 1 ? 1 : 0) : -1, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalHealingCleansePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original healing/cleanse witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalHealingCleanseIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 19 && actions.Length == 19, "Missing original healing/cleanse cards or positive controls.");
        BattleTurnState Before(int i) => actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int i) => actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Ally(BattleTurnState state, int index = 0) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[index]);
        CombatUnit Front(BattleTurnState state) => state.Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack");
        int Stacks(CombatUnit unit, string status) => unit.Status(status)?.Stacks ?? 0;
        foreach ((int i, string dataId) in Originals)
        {
            BattleTurnState before = Before(i), after = After(i); CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[i])!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(rule => rule.DataId == dataId);
            Require(card.DataId == dataId && rule.ExternalInteractions.Count == 0 && before.Energy - after.Energy == rule.Cost &&
                (i == 4 || after.Spawn.Train.Context!.Cards.Discard.Any(token => token.InstanceId == ids[i])), "Original definition, cost or played discard differs.");
            if (i != 18) Require(Json(before.Spawn.Train.Rooms[1].Units) == Json(after.Spawn.Train.Rooms[1].Units), "Original spell changed a neighbor floor.");
        }
        Require(Ally(After(7)).Health - Ally(Before(7)).Health == 4 && Stacks(Ally(After(7)), "regen") == 2, "Starter Heal requires positive healing and two Regen.");
        Require(Ally(After(8)).Health - Ally(Before(8)).Health == 15 && Stacks(Ally(After(8)), "ephemeral") == 3, "Purifying Cleanse requires positive healing followed by three Burnout.");
        Require(Ally(After(9)).Health - Ally(Before(9)).Health == 1 &&
            Front(Before(14)).Health + Stacks(Front(Before(14)), "armor") - Front(After(14)).Health - Stacks(Front(After(14)), "armor") == 1,
            "Witchweave requires positive friendly healing and enemy damage on separate original plays.");
        int poison = Before(11).Spawn.Train.Rooms[0].Units.Sum(unit => Stacks(unit, "poison"));
        Require(poison == 14 && Stacks(Front(After(11)), "poison") == 28 &&
            After(11).Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != Front(After(11)).Id).All(unit => Stacks(unit, "poison") == 0),
            "Frost Channeling must collect both teams' poison, remove the old stacks and double the selected enemy's total.");
        Require(Stacks(Ally(Before(12)), "ephemeral") == 3 && Stacks(Ally(Before(12)), "dazed") == 1 &&
            Stacks(Ally(After(12)), "ephemeral") == 0 && Stacks(Ally(After(12)), "dazed") == 0 && Stacks(Ally(After(12)), "regen") == 2 &&
            Stacks(Ally(After(12), 1), "dazed") == 1, "Wickless Recruitment must clear its target's Burnout/debuffs and preserve Regen/untargeted ally.");
        Require(Stacks(Ally(After(13), 1), "dazed") == 0 && Stacks(Front(Before(13)), "armor") == 25 &&
            Stacks(Front(After(13)), "armor") == 25 && Stacks(Front(Before(13)), "spikes") == 2 && Stacks(Front(After(13)), "spikes") == 0 &&
            Stacks(Front(After(13)), "poison") == 28 && Stacks(Ally(After(13)), "regen") == 2,
            "Resin Removal must clear friendly negative/enemy positive statuses and preserve Regen, poison and persistent Armor.");
        Require(Ally(After(15)).Health == 1 && Ally(After(15)).BaseAttack == Ally(Before(15)).BaseAttack + 10 &&
            Stacks(Ally(After(15)), "lifesteal") == 2, "Risky Operation must set one health before +10 attack and two Lifesteal.");
        Require(Ally(Before(16)).Health == 1 && Ally(Before(16), 1).Health == 450 &&
            new[] { Ally(After(16)), Ally(After(16), 1) }.All(unit => unit.Health == unit.MaxHealth), "Unleash the Wildwood requires two positive full heals.");
        CardInstanceState removed = After(17).Spawn.Train.Context!.FindCard(ids[4])!;
        Require(Ally(Before(17)).EquipmentCards!.SequenceEqual([ids[4]]) && Ally(After(17)).EquipmentCards!.Count == 0 &&
            Ally(After(17)).BaseAttack == Ally(Before(17)).BaseAttack + 30 && Ally(After(17)).MaxHealth == 500 &&
            Stacks(Ally(After(17)), "spikes") == 3 &&
            removed.EquippedUnitId == 0 && removed.MaskDescriptor!.BaseTraits.Single().GraftSevered == true,
            "Weaponized Prosthetic must remove/sever the original graft and apply its original +30 attack upgrade.");
        Require(Ally(After(18), 2).Health == Ally(Before(18), 2).Health + 10 &&
            Before(18).Spawn.Train.Rooms[1].Units.Any(unit => unit.SpawnerCardId == ids[2]) &&
            After(18).Spawn.Train.Rooms[0].Units.Any(unit => unit.SpawnerCardId == ids[2]) && After(18).Spawn.Train.Rooms[1].Units.Count == 0,
            "Restoring Retreat must heal its wounded target before a positive descend.");
        EquipmentChecks.Native(root);
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-HEALING-CLEANSE PASS: ten original spells, both Witchweave teams, positive healing/regen/Burnout, poison concentration/doubling, filtered cleanses, one-health attack/Lifesteal, full room heal, graft removal/+30 attack, healed descend and 16 immutable full chains.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
