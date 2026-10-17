using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalRageOnBurnoutStackLossChecks
{
    private const string RelicId = "85667d60-8053-4204-8c02-ba101dab7945";
    private const string RelicKey = "RageOnBurnoutStackLoss";
    private const string Effect = "RelicEffectAddStatusEffectOnOtherStatusRemoved";

    internal static void Run()
    {
        CombatStatus Ephemeral(int count) => new("ephemeral", count, stackable: false, hidden: true,
            displayCategory: "Persistent");
        CombatStatus Buff(int count) => new("buff", count, paramInt: 2, stackable: true, hidden: false,
            displayCategory: "Positive");
        RelicStatusStacks Rule(string team = "Monsters", string trigger = "ephemeral", int stacks = 3,
            CombatStatus? added = null, IReadOnlyList<RelicConditionState>? conditions = null) =>
            new(0, team, "buff", stacks, 0, conditions, trigger, added ?? Buff(stacks));
        CombatRelicState Owner(RelicStatusStacks? rule = null) => new(RelicId, RelicKey, [Effect],
            isCovenant: false, disallowedInPlacementPhase: false, statusStackAdditions: [rule ?? Rule()]);
        CombatUnit Unit(int id, CombatTeam team, params CombatStatus[] statuses) => new(id, "carrier", team,
            2, 20, 20, true, false, false, statuses, statusRegistry: statuses,
            statusDictionary: new StatusDictionaryState(statuses.Select(status => (string?)status.Id).ToArray(), []));
        RoomCombatState Room(CombatRelicState owner, CombatUnit unit, bool preview = false) => new(0, false,
            [unit], [], new CombatContext(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(13), 0, 1, 10,
                relics: [owner]), preview);
        int Count(RoomCombatState room, string id) => room.Units.Single().StatusRegistry?.FirstOrDefault(status => status.Id == id)?.Stacks ??
            room.Units.Single().Statuses.FirstOrDefault(status => status.Id == id)?.Stacks ?? 0;

        CombatRelicState owner = Owner();
        Require(RelicModel.Validate([owner]) == null, "The original status-removal owner was rejected.");
        RoomCombatState positiveRoot = Room(owner, Unit(1, CombatTeam.Player, Ephemeral(1)));
        string parent = Serialize(positiveRoot);
        RoomCombatResult positive = StatusRemovalModel.Remove(positiveRoot, 1, "ephemeral", 1);
        Require(positive.Supported && Count(positive.State!, "ephemeral") == 0 && Count(positive.State!, "buff") == 3 &&
            positive.State!.Units.Single().Attack == 8,
            positive.UnsupportedReason ?? "One friendly Ephemeral loss did not add three Rage before removal.");
        Require(Serialize(positiveRoot) == parent, "A removal relic mutated its immutable input.");

        RoomCombatResult oversized = StatusRemovalModel.Remove(positiveRoot, 1, "ephemeral", 4);
        Require(oversized.Supported && Count(oversized.State!, "ephemeral") == 0 && Count(oversized.State!, "buff") == 12,
            oversized.UnsupportedReason ?? "The relic used actual removed stacks instead of the raw native request.");
        RoomCombatState missingRoot = Room(owner, Unit(1, CombatTeam.Player));
        RoomCombatResult missing = StatusRemovalModel.Remove(missingRoot, 1, "ephemeral", 1);
        Require(missing.Supported && Count(missing.State!, "buff") == 3,
            missing.UnsupportedReason ?? "A raw removal callback with no status entry was skipped.");

        RoomCombatResult all = StatusRemovalModel.Remove(Room(owner, Unit(1, CombatTeam.Player, Ephemeral(1), Buff(5))),
            1, "ephemeral", -1);
        Require(all.Supported && Count(all.State!, "ephemeral") == 0 && Count(all.State!, "buff") == 2,
            all.UnsupportedReason ?? "The all-stacks sentinel was not multiplied as the native -1 request.");
        RoomCombatResult zero = StatusRemovalModel.Remove(missingRoot, 1, "ephemeral", 0);
        Require(zero.Supported && Count(zero.State!, "buff") == 0 && zero.State!.Units.Single().RegisteredStatus("buff") != null,
            zero.UnsupportedReason ?? "A zero-count native AddStatusEffect did not register its status definition.");

        RoomCombatResult wrongStatus = StatusRemovalModel.Remove(Room(owner, Unit(1, CombatTeam.Player, Ephemeral(1))),
            1, "armor", 1);
        RoomCombatResult wrongTeam = StatusRemovalModel.Remove(Room(owner, Unit(2, CombatTeam.Enemy, Ephemeral(1))),
            2, "ephemeral", 1);
        Require(wrongStatus.Supported && Count(wrongStatus.State!, "buff") == 0 &&
            wrongTeam.Supported && Count(wrongTeam.State!, "buff") == 0,
            "The removed-status or Monsters-only target gate was ignored.");

        foreach (string excluded in new[] { "CardEffectTransferAllStatusEffects", "CardEffectMultiplyStatusEffect",
            "CardEffectTransferAllStatusEffectsFromUnit" })
        {
            RoomCombatResult control = StatusRemovalModel.Remove(positiveRoot, 1, "ephemeral", 1, fromEffectType: excluded);
            Require(control.Supported && Count(control.State!, "buff") == 0 && Count(control.State!, "ephemeral") == 0,
                control.UnsupportedReason ?? "An excluded native transfer/multiply source triggered the relic or skipped the removal: " + excluded);
        }
        RoomCombatResult preview = StatusRemovalModel.Remove(new RoomCombatState(0, false, positiveRoot.Units, [],
            positiveRoot.Context, preview: true), 1, "ephemeral", 1);
        Require(preview.Supported && Count(preview.State!, "buff") == 3,
            preview.UnsupportedReason ?? "The preview-capable relic was suppressed in preview.");

        CombatRelicState missingDefinition = new(RelicId, RelicKey, [Effect], isCovenant: false,
            disallowedInPlacementPhase: false, statusStackAdditions: [new(0, "Monsters", "buff", 3, 0,
                triggerStatusId: "ephemeral")]);
        Require(RelicModel.Validate([missingDefinition]) != null &&
            RelicModel.Validate([Owner(Rule(conditions: [new(new("TimesPlayed", "ThisTurn"), true, 1, 1, false)]))]) != null,
            "An uncaptured status definition or unsupported condition passed relic validation.");

        Parallel.For(0, 32, _ => Require(Serialize(StatusRemovalModel.Remove(positiveRoot, 1, "ephemeral", 1).State) ==
            Serialize(positive.State), "Parallel status-removal relic branches differed."));
        Console.WriteLine("ORIGINAL-RAGE-ON-BURNOUT-STACK-LOSS PURE PASS: raw request scaling, missing/zero callbacks, all-stacks sentinel, target team/status, excluded sources, preview, validation and immutable parallel branches.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 177 &&
            root.GetProperty("ModifierScenario").GetString() == "original-rage-on-burnout-stack-loss" &&
            root.GetProperty("OriginalRageOnBurnoutStackLossPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0),
            "Original RageOnBurnoutStackLoss battle is incomplete or has native/model failures.");

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray()
            .Where(row => row.GetProperty("ScenarioAction").GetBoolean()).OrderBy(row => row.GetProperty("Index").GetInt32()).ToArray();
        Require(actions.Length == 1, "The native RageOnBurnoutStackLoss scenario must mark exactly one Ephemeral-removal action.");
        FixtureValue actionRecord = actions[0];
        BattleTurnState removalBefore = actionRecord.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState removalAfter = actionRecord.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction removalAction = actionRecord.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext beforeContext = removalBefore.Spawn.Train.Context!;
        CombatRelicState owner = beforeContext.Relics!.Single(relic => relic.DataId == RelicId);
        RelicStatusStacks rule = owner.StatusStackAdditions!.Single();
        CombatUnit beforeCarrier = removalBefore.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.Id == removalAction.TargetUnitId);
        CombatUnit afterCarrier = removalAfter.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.Id == removalAction.TargetUnitId);
        Require(beforeContext.FindCard(removalAction.CardInstanceId)?.DataId == "fc119000-0000-4000-8000-000000000857" &&
            beforeCarrier.Team == CombatTeam.Player && removalAction.TargetUnitId == beforeCarrier.Id &&
            actionRecord.GetProperty("Actual").ValueKind != FixtureKind.Null &&
            actionRecord.GetProperty("Difference").ValueKind == FixtureKind.Null,
            "The authored Ephemeral-removal spell did not target its native carrier or replay exactly.");
        Require(owner.AssetKey == RelicKey && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
            owner.EffectTypes.SequenceEqual([Effect]) && rule.EffectIndex == 0 && rule.SourceTeam == "Monsters" &&
            rule.TriggerStatusId == "ephemeral" && rule.StatusId == "buff" && rule.AdditionalStacks == 3 &&
            rule.AddedStatusDefinition?.Id == "buff" && rule.Conditions.Count == 0 && RelicModel.Validate(beforeContext) == null,
            "Captured original RageOnBurnoutStackLoss owner or native payload changed.");
        Require(Count(beforeCarrier, "ephemeral") == 1 && Count(beforeCarrier, "buff") == 0 &&
            Count(afterCarrier, "ephemeral") == 0 && Count(afterCarrier, "buff") == 3 && afterCarrier.Attack == 26,
            "Removing one native Ephemeral stack did not add exactly three native Rage before action completion.");
        BattleActionChecks.Native(root);
        Console.WriteLine("ORIGINAL-RAGE-ON-BURNOUT-STACK-LOSS NATIVE PASS: unchanged owner; one native carrier and removal spell; 1 Ephemeral removed for exactly 3 Rage; all actions and turns matched.");
    }

    private static int Count(CombatUnit unit, string id) => unit.StatusRegistry?.FirstOrDefault(status => status.Id == id)?.Stacks ??
        unit.Statuses.FirstOrDefault(status => status.Id == id)?.Stacks ?? 0;
    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
