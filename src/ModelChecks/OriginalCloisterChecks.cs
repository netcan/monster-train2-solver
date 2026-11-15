using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class OriginalCloisterChecks
{
    private const string Owner = "8e11f688-eb5f-48b5-98f6-cf40e60c842d";
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("OriginalCloisterPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete original Cloister native battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        var definition = root.GetProperty("OriginalCloisterDefinition");
        var effects = definition.GetProperty("Effects").EnumerateArray().ToArray();
        var modifier = effects[0].GetProperty("AttachmentUpgrade").GetProperty("RoomModifiers").EnumerateArray().Single();
        Require(definition.GetProperty("Id").GetString() == Owner && definition.GetProperty("Cost").GetInt32() == 1 &&
            definition.GetProperty("Traits").GetArrayLength() == 0 && effects.Length == 2 &&
            effects[0].GetProperty("Type").GetString() == "CardEffectAttachTrainRoomAttachment" &&
            effects[1].GetProperty("Type").GetString() == "CardEffectAddStatusEffect" &&
            effects.All(effect => effect.GetProperty("Target").GetString() == "Room" && effect.GetProperty("Team").GetString() == "Heroes") &&
            modifier.GetProperty("Type").GetString() == RoomEnemyEntryModel.Type &&
            modifier.GetProperty("Statuses")[0].GetProperty("statusId").GetString() == "chained" &&
            modifier.GetProperty("Statuses")[0].GetProperty("count").GetInt32() == 1,
            "Original Cloister cost, targets, immediate effect or entry modifier changed.");
        int[] ids = root.GetProperty("OriginalCloisterIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 16 && ids.Distinct().Count() == 16 && actions.Length == 16 &&
            actions.Select(row => row.GetProperty("Action").Deserialize<PlayCardAction>()!.CardInstanceId).SequenceEqual(ids),
            "Missing ordered original casts, replacement/removal or movement controls.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        RoomCapacityAttachmentState Attachment(BattleTurnState state, int room) => state.Spawn.Train.Context!.RoomCapacities!
            .Single(capacity => capacity.RoomIndex == room).Attachments!.Single();
        CombatUnit Guard(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack");
        foreach (int index in new[] { 1, 2, 6, 13 })
        {
            int room = index == 2 ? 1 : 0;
            var installed = Attachment(After(index), room);
            Require(installed.DataId == Owner && installed.EnemyEntry?.Count == 1 &&
                RoomEnemyEntryModel.Validate(installed.EnemyEntry.Single()) == null,
                "Original attachment lost captured entry payload.");
        }
        Require(Guid.TryParse(Attachment(After(1), 0).AttachmentId, out _) &&
            Attachment(After(2), 0).AttachmentId != Attachment(After(2), 1).AttachmentId &&
            Attachment(Before(6), 0).AttachmentId != Attachment(After(6), 0).AttachmentId,
            "Distinct floors and replacements must retain distinct native attachment identities.");
        Require(Guard(After(6)).Status("chained")!.Stacks == Guard(Before(6)).Status("chained")!.Stacks + 1,
            "Replacing the attachment must apply its immediate effect once.");
        foreach (int index in new[] { 7, 8, 11, 14, 15 })
            Require(Guard(After(index)).Status("chained")!.Stacks == Guard(Before(index)).Status("chained")!.Stacks + 1,
                "Enemy movement did not add exactly one entry stack at action " + index);
        Require(Attachment(After(10), 0).DataId != Owner && Attachment(After(10), 0).EnemyEntry == null &&
            Guard(After(12)).Status("chained")!.Stacks == Guard(Before(12)).Status("chained")!.Stacks &&
            Guard(After(13)).Status("chained")!.Stacks == Guard(Before(13)).Status("chained")!.Stacks + 1,
            "Removal/reset must unregister the old listener and restore a single listener.");
        var damageAction = actions[9].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var friendlyBefore = Before(9).Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == damageAction.TargetUnitId);
        var friendlyAfter = After(9).Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == damageAction.TargetUnitId);
        Require(friendlyBefore.Team == CombatTeam.Player && friendlyAfter.Health == friendlyBefore.Health - 1 &&
            Guard(After(9)).Health == Guard(Before(9)).Health &&
            Guard(After(9)).Status("armor")!.Stacks == Guard(Before(9)).Status("armor")!.Stacks - 2 &&
            After(9).Spawn.Train.Rooms.SelectMany(room => room.Units).Count(unit => unit.AssetKey == "MageT1_Junker") <
            Before(9).Spawn.Train.Rooms.SelectMany(room => room.Units).Count(unit => unit.AssetKey == "MageT1_Junker"),
            "Missing immediate chained propagation/death control.");
        int spawn = 0, ascend = 0, descend = 0, friendly = 0, wrongKind = 0;
        foreach (var record in root.GetProperty("RoomEnemyEntries").EnumerateArray())
        {
            Require(record.GetProperty("Completed").GetBoolean(), "Incomplete native room-entry callback.");
            string kind = record.GetProperty("Trigger").GetString()!; int id = record.GetProperty("ActorId").GetInt32();
            var before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = record.GetProperty("After").Deserialize<RoomCombatState>()!;
            string parent = Json(before); var expected = RoomEnemyEntryModel.Apply(before, id, kind);
            Require(expected.Supported && Json(expected.State!) == Json(after) && Json(before) == parent,
                "Full native room/context entry successor differs for " + kind + " actor " + id);
            var actor = before.Units.Single(unit => unit.Id == id);
            int delta = (after.Units.Single(unit => unit.Id == id).Status("chained")?.Stacks ?? 0) - (actor.Status("chained")?.Stacks ?? 0);
            if (actor.Team == CombatTeam.Player) { Require(delta == 0, "Friendly entry gained chained."); friendly++; }
            else if (kind == "OnTurnBegin") { Require(delta == 0, "Unrelated trigger applied entry status."); wrongKind++; }
            else if (kind == "AfterSpawnEnchant" && delta == 1) spawn++;
            else if (kind == "PostAscension" && delta == 1) ascend++;
            else if (kind == "PostDescension" && delta == 1) descend++;
        }
        Require(spawn > 0 && ascend > 0 && descend >= 2 && friendly >= 2 && wrongKind > 0,
            "Missing native spawn/ascend/descend/friendly/unrelated-trigger witnesses.");
        RecordedBattleChecks.Native(root); BattleSearchChecks.FrontierDiagnostics(root); BattleSearchChecks.Native(root, requireZeroUnsupported: true);
        Console.WriteLine($"ORIGINAL-CLOISTER PASS: unchanged room card, {spawn} enemy births, {ascend}/{descend} ascents/descents, {friendly}/{wrongKind} exclusions, replacement/removal/reset, chained propagation/death, complete chain/16 branches and zero-unsupported serial/parallel search.");
    }
}
