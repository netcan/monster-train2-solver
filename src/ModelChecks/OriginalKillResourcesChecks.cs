using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalKillResourcesChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids, string resource)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || (resource == "Greed" ? index >= 1 : index >= 6) && source.Spawn.Turn < 1) return null;
        int victim = resource == "Greed" ? -1 : index switch { 5 => 1, 7 or 8 => 2, 9 or 12 => 3, 10 or 11 => 4, _ => -1 };
        int target = victim < 0 ? 0 : source.Spawn.Train.Rooms[1].Units.Single(unit => unit.SpawnerCardId == ids[victim]).Id;
        bool battery = index == (resource == "Greed" ? 5 : 6);
        var action = new PlayCardAction(ids[index], index == 0 || battery ? 0 : 1,
            playerPosition: index == 0 ? 0 : index < 5 ? index - 1 : -1, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalKillResourcesPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original kill resource witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        string resource = root.GetProperty("OriginalKillResource").GetString()!;
        string original = resource switch { "Greed" => "27e9ee82-fc42-4aa5-b845-90d7e31b61d9", "GoldenTouch" => "32449823-6e6a-4ae4-98a2-2db41f38f159",
            "Bonk" => "99492750-a7b9-447c-a248-097a07cd346a", _ => throw new InvalidOperationException("Unknown original resource.") };
        int damage = resource == "Greed" ? 20 : resource == "GoldenTouch" ? 5 : 9;
        int cost = resource == "GoldenTouch" ? 2 : 1;
        int[] ids = root.GetProperty("OriginalKillResourcesIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == (resource == "Greed" ? 8 : 13) && actions.Length == ids.Length, "Missing original plays or controls.");
        BattleTurnState Before(int i) => actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int i) => actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Victim(BattleTurnState state, int index) => state.Spawn.Train.Rooms[1].Units.Single(unit => unit.SpawnerCardId == ids[index]);
        foreach (int i in resource == "Greed" ? new[] { 6, 7 } : new[] { 5, 7, 8, 9, 10, 11, 12 })
        {
            var before = Before(i); var after = After(i); var context = before.Spawn.Train.Context!; var changed = after.Spawn.Train.Context!;
            var card = context.FindCard(ids[i])!; var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == original);
            Require(card.DataId == original && rule.Cost == cost && rule.ExternalInteractions.Count == 0 &&
                rule.Effects![0].Value == damage && card.MaskDescriptor!.KillTriggers is { Count: 1 } triggers && CardKillTriggerModel.Supported(triggers[0]),
                "Original card cost, damage or kill payload differs.");
            int lethal = resource == "Greed" ? i == 6 ? 2 : 3 : i is 7 or 10 ? 0 : 1;
            if (before.Spawn.Turn == 0 && card.MaskDescriptor!.KillTriggers![0].DeploymentAllowed == false) lethal = 0;
            Require(before.Energy - after.Energy == cost, "Resource reward changed the source spell energy cost.");
            Require(changed.Gold == context.Gold + (resource == "Greed" ? lethal * GoldRewardModel.Adjust(10, multiplier: context.Resources!.GoldRewardMultiplier) : 0), "Gold reward per lethal hit differs.");
            Require(changed.QueryFrame!.DragonsHoard == (resource == "GoldenTouch" ? Math.Min(context.Resources!.DragonsHoardCap!.Value,
                context.QueryFrame!.DragonsHoard!.Value + lethal) : context.QueryFrame!.DragonsHoard), "Dragon's Hoard reward or cap differs.");
            Require(changed.QueryFrame.ForgePoints == (resource == "Bonk" ? context.QueryFrame!.ForgePoints + lethal * 3 : context.QueryFrame!.ForgePoints), "Forge reward per lethal hit differs.");
            Require(changed.Cards.Discard.Any(token => token.InstanceId == ids[i]), "Played original spell must discard.");
            Require(Json(before.Spawn.Train.Rooms[0].Units) == Json(after.Spawn.Train.Rooms[0].Units), "Resource spell changed its neighboring anchor floor.");
        }
        int first = resource == "Greed" ? 6 : 9, shield = resource == "Greed" ? 6 : 10;
        Require(Victim(Before(first), 3).Status("undying")?.Stacks == 1 && Victim(After(first), 3).Health == 1 &&
            Victim(After(first), 3).Status("undying") == null, "Undying must revive before the positive lethal reward.");
        Require(Victim(Before(shield), 4).Status("damage shield")?.Stacks == 1 && Victim(After(shield), 4).Health == damage - 1 &&
            Victim(After(shield), 4).Status("damage shield") == null, "Shield control must consume shield without rewarding a kill.");
        int nonlethal = resource == "Greed" ? 6 : 7;
        Require(Victim(Before(nonlethal), 2).Health == 2 * damage && Victim(After(nonlethal), 2).Health == damage, "Positive nonlethal damage control differs.");
        foreach (int victim in new[] { 1, 2, 3, 4 }) Require(!After(actions.Length - 1).Spawn.Train.Rooms[1].Units.Any(unit => unit.SpawnerCardId == ids[victim]), "Positive repeated kills did not remove all victims.");
        if (resource == "GoldenTouch") Require(After(9).Spawn.Train.Context!.QueryFrame!.DragonsHoard == 3 &&
            After(12).Spawn.Train.Context!.QueryFrame!.DragonsHoard == 3, "Hoard must reach cap then remain capped across further kills.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-KILL-RESOURCES PASS: unchanged " + resource + ", per-lethal resource rewards, nonlethal/shield controls, Undying/repeated death, resource isolation and 16 immutable full chains.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
