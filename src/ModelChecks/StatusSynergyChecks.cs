using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class StatusSynergyChecks
{
    internal static void Native(FixtureValue root, IReadOnlyCollection<string> originals)
    {
        if (!originals.Contains("a83756a0-7264-47d1-a661-d8ab2420fe29")) return;
        int birth = 0, added = 0, controls = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            string played = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.DataId;
            CombatUnit? old = before.Spawn.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.AssetKey == "MonsterApexImp");
            CombatUnit? next = after.Spawn.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.AssetKey == "MonsterApexImp");
            if (next == null) continue;
            CardActionEffect synergy = next.Triggers.Single(trigger => trigger.Kind == "OnArmorAdded").Effects.Single().Action!;
            string rage = synergy.Statuses[1].Id;
            if (played == "a83756a0-7264-47d1-a661-d8ab2420fe29")
            {
                Require(next.Status("armor")?.Stacks == 5 && next.Status(rage)?.Stacks == 5,
                    "Original Apex Imp must convert its five OnSpawn Armor into five Rage.");
                birth++;
            }
            if (played == "64beabe3-0260-44e2-a47a-b07192b52f25")
            {
                Require(next.Status("armor")?.Stacks == old!.Status("armor")!.Stacks + 10 &&
                    next.Status(rage)?.Stacks == old.Status(rage)!.Stacks + 16,
                    "Enflame must add six Rage and ten new-Armor Rage, without reusing the five birth Armor.");
                added++;
            }
            if (played == "141494fe-0ac3-41ad-b1e6-05aeafe060f6")
            {
                Require(JsonSerializer.Serialize(old, ModelJson.Options) == JsonSerializer.Serialize(next, ModelJson.Options),
                    "Armor added to an independent front unit must leave Apex Imp and its synergy cache unchanged.");
                controls++;
            }
            Require(next.Modifiers!.SynergizeStatuses != null && next.Modifiers.SynergizeStatuses.All(value => value.Value == 0),
                "Every synergy execution must clear all accumulated native status counters, including the newly added Rage.");
        }
        Require(birth == 1 && added == 1 && controls == 1, "Missing original birth, repeated Armor conversion or independent target control.");
        Console.WriteLine("ORIGINAL-STATUS-SYNERGY PASS: original +5 birth conversion, +10 later conversion alongside six direct Rage, independent Armor target and complete native cache resets.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
