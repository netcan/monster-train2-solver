using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class OriginalPenitentChecks
{
    private const string Owner = "f4360a0a-2be3-40c0-b1f1-da59ca42ffac";
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("OriginalPenitentPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete original Penitent Sanctuary battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        var definition = root.GetProperty("OriginalPenitentDefinition");
        var effect = definition.GetProperty("Effects").EnumerateArray().Single();
        var nativeUpgrade = definition.GetProperty("Upgrade").Deserialize<CardUpgradeModifier>()!;
        var punish = nativeUpgrade.TriggerUpgrades!.Single();
        Require(definition.GetProperty("Id").GetString() == Owner && definition.GetProperty("Cost").GetInt32() == 1 &&
            definition.GetProperty("Traits").GetArrayLength() == 0 && effect.GetProperty("Type").GetString() == "CardEffectAttachTrainRoomAttachment" &&
            effect.GetProperty("Target").GetString() == "Room" && effect.GetProperty("Team").GetString() == "Monsters" &&
            effect.GetProperty("AttachmentUpgrade").GetProperty("RoomModifiers").GetArrayLength() == 0 &&
            nativeUpgrade is { DataId: "d6977cb9-57ae-405d-a3dd-1eaccd7766c9", ExternalInteractions.Count: 0 } &&
            punish is { Kind: "OnPunish", Once: false, IgnoreSilence: true, Effects.Count: 2 } &&
            punish.Effects[0] is { Type: "CardEffectHeal", Value: 3, Action: { Target: "Self" } } &&
            punish.Effects[1] is { Type: "CardEffectAddTempCardUpgradeToUnits", UnitUpgrade: { Type: "UnitUpgrade", Target: "Self", Value: 1,
                Lifetime: "TemporaryUntilEndOfBattle" } },
            "Original room cost, friendly target, ordered heal or upgrade definition changed.");
        var growth = punish.Effects[1].UnitUpgrade!.Upgrade!;
        Require(growth is { DataId: "e2fb539a-be4a-4c99-9187-9dc89b0a90f6", AssetKey: "Plus3Attack",
                Stats.Damage: 3, Stats.Health: 0, Statuses.Count: 0, ExternalInteractions.Count: 0 },
            "Original Punish attack-growth payload differs.");
        int[] ids = root.GetProperty("OriginalPenitentIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 25 && ids.Distinct().Count() == 25 && actions.Length == 25 &&
            actions.Select(row => row.GetProperty("Action").Deserialize<PlayCardAction>()!.CardInstanceId).SequenceEqual(ids),
            "Missing ordered original casts and independent controls.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Unit(BattleTurnState state, int card = 1) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.SpawnerCardId == ids[card]);
        RoomCapacityAttachmentState Attachment(BattleTurnState state) => state.Spawn.Train.Context!.RoomCapacities!
            .Single(room => room.RoomIndex == 0).Attachments!.Single();
        int RoomTriggers(BattleTurnState state, int card = 1) => Unit(state, card).Triggers.Count(trigger => trigger.Kind == "OnPunish");
        foreach (int index in new[] { 3, 13, 17, 20 })
        {
            var state = After(index); var attachment = Attachment(state);
            Require(attachment.DataId == Owner && Guid.TryParse(attachment.AttachmentId, out _) && RoomTriggers(state) == 1 &&
                Unit(state).Triggers.Single(trigger => trigger.Kind == "OnPunish").Origin?.UpgradeId == attachment.AttachmentId,
                "Room trigger must retain exactly one live attachment origin at " + index);
        }
        Require(Attachment(Before(13)).AttachmentId != Attachment(After(13)).AttachmentId &&
            Attachment(After(15)).DataId != Owner && RoomTriggers(After(15)) == 0 && RoomTriggers(After(18)) == 0 &&
            RoomTriggers(After(22), 22) == 1 && RoomTriggers(After(22), 2) == 0,
            "Replacement/removal/movement or late-birth trigger synchronization differs.");
        foreach (int index in new[] { 5, 9, 11, 14, 21, 23 })
        {
            int card = index == 23 ? 22 : 1;
            var before = Unit(Before(index), card); var after = Unit(After(index), card);
            int damage = Math.Max(0, 4 - (before.Status("armor")?.Stacks ?? 0));
            Require(after.Attack == before.Attack + growth.Stats.Damage && after.Health == Math.Min(before.MaxHealth, before.Health - damage + 3),
                "Punish must heal 3 and grow attack once at " + index);
        }
        foreach (int index in new[] { 6, 7, 16, 19 })
        {
            int card = index == 6 ? 2 : 1;
            var before = Unit(Before(index), card); var after = Unit(After(index), card);
            int damage = index == 7 ? 0 : Math.Max(0, 4 - (before.Status("armor")?.Stacks ?? 0));
            Require(after.Attack == before.Attack && after.Health == before.Health - damage,
                "Outside/zero/removed trigger exclusion differs at " + index);
        }
        Require(Unit(Before(9)).Status("armor")?.Stacks == 4 && Unit(After(9)).Status("armor") == null &&
            Unit(Before(11)).Status("silenced")?.Stacks == 1 && Unit(After(12)).Status("silenced") == null &&
            Unit(Before(15)).Attack == Unit(After(15)).Attack &&
            Unit(Before(18)).Attack == Unit(After(18)).Attack,
            "Missing armor/silence controls or earned growth was removed with the room.");
        var target = actions[24].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit Enemy(BattleTurnState state) => state.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == target.TargetUnitId);
        Require(Enemy(After(24)).Status("armor")!.Stacks == Enemy(Before(24)).Status("armor")!.Stacks - 1 &&
            Enemy(After(24)).Triggers.All(trigger => trigger.Kind != "OnPunish") &&
            Unit(Before(24)).Health == Unit(After(24)).Health && Unit(Before(24)).Attack == Unit(After(24)).Attack,
            "Enemy target inherited a friendly room trigger or altered the bystander.");
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        BattleSearchChecks.FrontierDiagnostics(root); BattleSearchChecks.Native(root, requireZeroUnsupported: true);
        Console.WriteLine($"ORIGINAL-PENITENT PASS: original friendly room, ordered heal 3/attack +{growth.Stats.Damage}, armor/zero/ignore-silence/external-floor controls, replacement/removal/reinstall, movement/late birth, retained earned growth, full chain/16 branches and zero-unsupported serial/parallel search.");
    }
}
