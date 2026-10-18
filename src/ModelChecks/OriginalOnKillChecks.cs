using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalOnKillChecks
{
    private const string OriginalId = "24a256cd-715e-4423-b2e9-c0925b285fd4";
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 6 && source.Spawn.Turn < 1) return null;
        int victim = index switch { 5 => 1, 7 or 8 => 2, 9 or 12 => 3, 10 or 11 => 4, _ => -1 };
        int target = victim < 0 ? 0 : source.Spawn.Train.Rooms[1].Units.Single(unit => unit.SpawnerCardId == ids[victim]).Id;
        var action = new PlayCardAction(ids[index], index is 0 or 6 ? 0 : 1, playerPosition: index == 0 ? 0 : index < 5 ? index - 1 : -1, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalOnKillPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original OnKill witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalOnKillIds").Deserialize<int[]>()!;
        bool deploymentAllowed = root.GetProperty("OriginalOnKillDeploymentAllowed").GetBoolean();
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 13 && actions.Length == 13, "Missing original spell plays or lethal/nonlethal controls.");
        BattleTurnState Before(int i) => actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int i) => actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Victim(BattleTurnState state, int index) => state.Spawn.Train.Rooms[1].Units.Single(unit => unit.SpawnerCardId == ids[index]);
        foreach (int i in new[] { 5, 7, 8, 9, 10, 11, 12 })
        {
            var before = Before(i); var after = After(i); var card = before.Spawn.Train.Context!.FindCard(ids[i])!;
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == OriginalId);
            Require(card.DataId == OriginalId && rule.Cost == 2 && rule.ExternalInteractions.Count == 0 &&
                card.MaskDescriptor!.KillTriggers is { Count: 1 } triggers && CardKillTriggerModel.Supported(triggers[0]), "Original cost, trigger metadata or projection differs.");
            Require(card.MaskDescriptor!.KillTriggers![0].DeploymentAllowed == deploymentAllowed && (i != 5 || before.Spawn.Turn == 0),
                "Missing captured native deployment trigger rule or positive deployment kill.");
            Require(before.Energy - after.Energy == (i is 7 or 10 || i == 5 && !deploymentAllowed ? 2 : 0), "Bloody Moon must refund two only after an allowed lethal damage trigger, including Undying revival.");
            Require(after.Spawn.Train.Context!.Cards.Discard.Any(token => token.InstanceId == ids[i]), "Played original spell must enter discard.");
            Require(Json(before.Spawn.Train.Rooms[0].Units) == Json(after.Spawn.Train.Rooms[0].Units), "Original spell changed its neighboring anchor floor.");
            if (i == 7) Require(Victim(before, 2).Health == 50 && Victim(after, 2).Health == 25, "Nonlethal control must deal positive 25 damage without a reward.");
            if (i == 9) Require(Victim(before, 3).Health == 10 && Victim(before, 3).Status("undying")?.Stacks == 1 &&
                Victim(after, 3).Health == 1 && Victim(after, 3).Status("undying") == null, "Lethal Undying hit must revive before the card kill trigger.");
            if (i == 10) Require(Victim(before, 4).Status("damage shield")?.Stacks == 1 && Victim(after, 4).Health == 10 &&
                Victim(after, 4).Status("damage shield") == null, "Shield control must consume the shield without a kill reward.");
        }
        foreach ((int action, int victim) in new[] { (5, 1), (8, 2), (11, 4), (12, 3) })
            Require(!After(action).Spawn.Train.Rooms[1].Units.Any(unit => unit.SpawnerCardId == ids[victim]), "A positive kill did not remove its victim.");
        var sample = Before(5); var source = sample.Spawn.Train.Context!.FindCard(ids[5])!; var descriptor = source.MaskDescriptor!;
        var definition = sample.PlayRules!.Cards.Single(rule => rule.DataId == OriginalId);
        CardInstanceState Replace(IReadOnlyList<CardLifecycleTrigger>? triggers) => source.WithMaskDescriptor(new CardMaskDescriptor(
            descriptor.Definition, descriptor.BaseCost, descriptor.BaseTraits, descriptor.AuthoredCastEffects, descriptor.Purified,
            descriptor.PermanentGraft, descriptor.TemporaryTraits, descriptor.PermanentReplacements, descriptor.SourceTraits, triggers));
        Require(CardModifierModel.Resolve(definition, Replace(null)).ExternalInteractions.Any(message => message.Contains("OnKill")), "Missing kill payload was silently accepted.");
        var bad = new CardLifecycleTrigger("OnKill", null, ["CardEffectDamage"], actions: [new CardActionEffect("Damage", "Room", 2, true, false, [])]);
        Require(CardModifierModel.Resolve(definition, Replace([bad])).ExternalInteractions.Any(message => message.Contains("OnKill")), "Unknown kill payload was silently accepted.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-ON-KILL PASS: seven original Bloody Moon plays, native deployment gate, overkill/exact kill/refund, nonlethal/shield no-refund, Undying revival and repeated death, payload rejection and 16 immutable full chains.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
