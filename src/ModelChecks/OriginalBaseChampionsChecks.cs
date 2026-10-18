using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalBaseChampionsChecks
{
    private static readonly string[] Originals = ["0422f6ca-896c-4676-a01f-71d8d4b232fb", "0c43beba-20d6-4917-a624-e0c63a21a93a",
        "15e25a50-5235-4c45-803b-cada23022671", "2cbf61fc-f26b-4a57-a738-79589586dba7", "2e64ddf0-fad1-4bf2-80d2-68fd928ecd28",
        "31f56ada-fd59-4012-93be-88e69e55ba92", "35fcb075-2aeb-4a51-92f9-e3d198784906", "41600212-9122-46e2-a607-545a86830574",
        "49b6e610-56a8-4b97-990d-d9a2a093b266", "7052d604-bf4b-49cb-a1a9-1a0c47033c50", "7cff7749-0803-402c-8a9a-965222c24b4b",
        "8b6c19ce-c8d0-4a77-b33a-e490d0d1b282", "b0def3ad-c519-4ef7-b2d0-4af0467806b9", "b478b414-8d3f-4007-993f-efdf3640bbfb",
        "bd2fe27a-e8d9-4f1b-ae3c-549f1428e441", "d14a50f3-728d-43e1-87f0-ef1b013f6697", "d2a5feba-4ecd-4d5d-a206-ec2ebfb96667",
        "dfc5f5f4-e328-44cd-b4f3-758cbc1d5fe2"];
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
        int room = index < 3 ? index : index < 6 ? index - 3 : (index - 6) / 6;
        var action = new PlayCardAction(ids[index], room, playerPosition: index < 3 ? 0 : index < 6 ? -1 : (index - 6) % 6 + 1);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalBaseChampionsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original base champion witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalBaseChampionsIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 24 && actions.Length == 24, "Missing eighteen unchanged base champion births or capacity controls.");
        for (int i = 6; i < 24; i++)
        {
            BattleTurnState before = actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = actions[i].GetProperty("Action").Deserialize<PlayCardAction>()!;
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == Originals[i - 6]);
            CombatUnit template = rule.SpawnUnit!, unit = after.Spawn.Train.Rooms[action.RoomIndex].Units.Single(unit => unit.SpawnerCardId == ids[i]);
            Require(before.Spawn.Train.Context!.FindCard(ids[i])!.DataId == rule.DataId && rule.Cost == 0 && before.Energy == after.Energy && rule.ExternalInteractions.Count == 0,
                "Original base champion identity, cost or projection differs.");
            Require(template.Modifiers!.IsChampion == true && unit.Modifiers!.IsChampion == true && unit.AssetKey == template.AssetKey && unit.BaseAttack == template.BaseAttack &&
                unit.Health == template.Health && unit.MaxHealth == template.MaxHealth && unit.Size == template.Size && unit.Statuses.Count == 0 && unit.Triggers.Count == (i == 23 ? 1 : 0) && unit.Modifiers.Upgrades.Count == 0 &&
                unit.StatusRegistry!.Count == 0 && unit.StatusDictionary!.Slots.Count == 0,
                "Base champion statistics/classification changed or path upgrades leaked into the original definition witness.");
            if (i is 15 or 22) Require(template.AbilityRules!.AuthoredStartingStatuses.Any(status => status.Id == "rooted" && status.Stacks == 0) && unit.RegisteredStatus("rooted") == null,
                "The original zero-stack Rooted definition must survive as authored metadata without a live status dictionary entry.");
            if (i == 23) Require(unit.Triggers.Single().Kind == "CardSpellPlayed" &&
                unit.Triggers.Single().Effects.Single().Action!.Statuses.Single().Id == "stygian_blessing" &&
                unit.Triggers.Single().Effects.Single().Action!.Statuses.Single().Stacks == 1,
                "Solgard's intrinsic starting Incant upgrade must remain present without a selected champion path upgrade.");
            Require(Json(before.Spawn.Train.Rooms.Where(room => room.RoomIndex != action.RoomIndex).Select(room => room.Units)) ==
                Json(after.Spawn.Train.Rooms.Where(room => room.RoomIndex != action.RoomIndex).Select(room => room.Units)), "Base champion birth changed another floor.");
        }
        BattleTurnState completed = actions[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(completed.Spawn.Train.Rooms.Take(3).All(room => room.Units.Count(unit => unit.Team == CombatTeam.Player) == 7), "All three floors must reach exactly seven friendly physical slots.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-BASE-CHAMPIONS PASS: eighteen unchanged zero-cost base definitions, exact stats/champion classification, no path upgrades, three seven-unit floors and 16 immutable full chains; upgraded champion paths remain separate.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
