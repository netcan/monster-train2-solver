using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalBigDamageFromManyMorselsChecks
{
    private const string DataId = "15f3c641-14ba-4305-8c9f-cdb2ebb4e572";
    private const string Snack = "SubtypesData_Snack";
    private const string ControlCard = "fc119000-0000-4000-8000-000000000753";
    private const string SnackCard = "fc119000-0000-4000-8000-000000000751";

    internal static PlayCardAction? Choose(BattleTurnState state)
    {
        CombatContext? context = state.Spawn.Train.Context;
        if (context == null) return null;
        bool Played(string dataId) => context.Statistics?.Values.Any(value => value.Type == "TimesPlayed" &&
            value.Duration == "ThisBattle" && value.Value > 0 && context.FindCard(value.CardId)?.DataId == dataId) == true;
        bool controlPlayed = Played(ControlCard), snackPlayed = Played(SnackCard);
        bool targetReady = state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0);
        BattleActionEnumeration actions = BattleActionModel.EnumerateActions(state);
        PlayCardAction? snackPlay = actions.SupportedActions.FirstOrDefault(play => play.RoomIndex == 0 &&
            context.FindCard(play.CardInstanceId)?.DataId == SnackCard) ?? actions.SupportedActions.FirstOrDefault(play =>
            context.FindCard(play.CardInstanceId)?.DataId == SnackCard);
        string? wanted = !controlPlayed ? ControlCard : !snackPlayed && targetReady ? SnackCard : null;
        if (wanted != null)
            return wanted == SnackCard ? snackPlay : actions.SupportedActions.FirstOrDefault(play => play.RoomIndex == 0 &&
                context.FindCard(play.CardInstanceId)?.DataId == wanted) ?? actions.SupportedActions.FirstOrDefault(play =>
                context.FindCard(play.CardInstanceId)?.DataId == wanted);
        PlayCardAction? fallback = BattleActionModel.ChooseUnitAndJunkPlay(state);
        if (!snackPlayed && !targetReady && fallback != null &&
            context.FindCard(fallback.CardInstanceId)?.DataId == SnackCard) return null;
        return fallback;
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 169 &&
            root.GetProperty("ModifierScenario").GetString() == "original-big-damage-many-morsels" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "BigDamageFromManyMorsels native battle is incomplete or from another game build.");
        Replay(root);
    }

    internal static void Replay(FixtureValue root)
    {
        var births = root.GetProperty("RelicSpawnStatuses").EnumerateArray()
            .Where(row => row.TryGetProperty("BeforeTrain", out _) && row.TryGetProperty("AfterTrain", out _) &&
                !row.GetProperty("OnlyCovenants").GetBoolean()).ToArray();
        var control = births.Select(row => (Row: row, Before: Read<TrainCombatState>(row.GetProperty("BeforeTrain"))))
            .FirstOrDefault(item => CardData(item.Before, item.Row.GetProperty("UnitId").GetInt32()) == ControlCard);
        var snack = births.Select(row => (Row: row, Before: Read<TrainCombatState>(row.GetProperty("BeforeTrain"))))
            .FirstOrDefault(item => CardData(item.Before, item.Row.GetProperty("UnitId").GetInt32()) == SnackCard);
        Require(control.Row.ValueKind != FixtureKind.Undefined && snack.Row.ValueKind != FixtureKind.Undefined,
            "Native birth archive is missing the explicit non-Snack or 20th-Snack witness.");

        CheckDefinition(control.Before);
        Require(SnackCount(control.Before) == 19, "The non-Snack control did not begin at 19 Snacks this battle.");
        Require(SnackCount(snack.Before) == 20, "CardStatistics did not count the Snack before RelicManager.CharacterAdded.");
        string frozen = JsonSerializer.Serialize(snack.Before, ModelJson.Options);
        var controlResult = RelicSummonDamageModel.CharacterAdded(control.Before, control.Row.GetProperty("UnitId").GetInt32());
        Require(controlResult.Supported && controlResult.TargetUnitIds.Count == 0 && controlResult.Notifications.Count == 0,
            "The exact-19 native control triggered the relic.");
        var snackResult = RelicSummonDamageModel.CharacterAdded(snack.Before, snack.Row.GetProperty("UnitId").GetInt32());
        Require(snackResult.Supported && snackResult.TargetUnitIds.Count > 0,
            "The exact-20 Snack witness did not damage any Tower target.");
        Require(snackResult.Notifications.Count == snackResult.TargetUnitIds.Count +
            snackResult.TargetUnitIds.Select(id => Floor(snack.Before, id)).Distinct().Count(),
            "Room and character NotifyRelicTriggered calls were not modeled separately.");
        int[] targetFloors = snackResult.TargetUnitIds.Select(id => Floor(snack.Before, id)).ToArray();
        Require(targetFloors.SequenceEqual(targetFloors.OrderByDescending(value => value)),
            "Tower targets were not visited from highest floor down.");
        Require(frozen == JsonSerializer.Serialize(snack.Before, ModelJson.Options), "Summon-damage replay mutated its parent input.");

        var oneAbove = RelicSummonDamageModel.CharacterAdded(WithSnackCount(control.Before, 21),
            control.Row.GetProperty("UnitId").GetInt32());
        Require(oneAbove.Supported && oneAbove.TargetUnitIds.Count == 0 && oneAbove.Notifications.Count == 0,
            "The relic treated 21 Snacks as a greater-than threshold instead of exact equality.");
        int pyreId = control.Before.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Id;
        var pyre = RelicSummonDamageModel.CharacterAdded(WithSnackCount(control.Before, 20), pyreId);
        Require(pyre.Supported && pyre.TargetUnitIds.Count == 0, "The Pyre was treated as a summoned Monster.");
        var enemyWitness = births.Select(row => Read<TrainCombatState>(row.GetProperty("BeforeTrain")))
            .SelectMany(state => state.Rooms.SelectMany(room => room.Units.Select(unit => (State: state, Unit: unit))))
            .FirstOrDefault(item => item.Unit.Team == CombatTeam.Enemy);
        Require(enemyWitness.Unit != null, "Native births did not capture an enemy-Hero gate witness.");
        var enemy = RelicSummonDamageModel.CharacterAdded(WithSnackCount(enemyWitness.State!, 20), enemyWitness.Unit!.Id);
        Require(enemy.Supported && enemy.TargetUnitIds.Count == 0, "An enemy Hero birth was treated as a player Monster summon.");

        int checkedBirths = 0;
        foreach (var row in births)
        {
            TrainCombatState before = Read<TrainCombatState>(row.GetProperty("BeforeTrain"));
            TrainCombatState actual = Read<TrainCombatState>(row.GetProperty("AfterTrain"));
            UnitCloneCallback[] queued = Read<UnitCloneCallback[]>(row.GetProperty("QueuedBefore"));
            UnitCloneCallback[] queuedAfter = Read<UnitCloneCallback[]>(row.GetProperty("QueuedAfter"));
            RelicBirthResult replay = RelicBirthModel.CharacterAdded(before, row.GetProperty("UnitId").GetInt32(),
                row.GetProperty("FromCardId").GetInt32(), row.GetProperty("OnlyCovenants").GetBoolean(),
                row.GetProperty("DeferCallbacks").GetBoolean(), queued);
            Require(replay.Supported, "Native birth replay unsupported: " + replay.UnsupportedReason);
            Equal(replay.State, actual, "complete train after birth");
            Equal(replay.Queued, queuedAfter, "retained callback queue");
            Equal(BehavioralDispatches(before, replay.Dispatched),
                BehavioralDispatches(before, Read<UnitCloneCallback[]>(row.GetProperty("Dispatched"))), "behaviorful callback dispatches");
            checkedBirths++;
        }
        Require(checkedBirths >= 2, "Expected multiple complete-train native birth replays.");
        Console.WriteLine($"ORIGINAL-BIG-DAMAGE PASS: exact 19/20/21 Snack conditions, player/Pyre/enemy gates, {snackResult.TargetUnitIds.Count} Tower targets, {snackResult.Notifications.Count} room/character relic notifications, and {checkedBirths} native complete-train birth replays.");
    }

    private static void CheckDefinition(TrainCombatState state)
    {
        CombatRelicState owner = state.Context!.Relics!.Single(relic => relic.DataId == DataId);
        RelicSummonDamageRule rule = owner.SummonDamageRules!.Single();
        RelicConditionState condition = rule.Conditions.Single();
        Require(owner.AssetKey == "BigDamageFromManyMorsels" && owner.IsCovenant == false &&
            owner.DisallowedInPlacementPhase == false && owner.EffectTypes.SequenceEqual([RelicModel.DamageOnMonsterSummoned]) &&
            rule.EffectIndex == 0 && rule.DamageAmount == 200 && rule.TargetTeam == "Heroes" &&
            rule.TargetMode == "Tower" && rule.CanApplyInPreview && rule.ConditionCount == 1 &&
            condition.Query.Type == "MonsterSubtypePlayed" && condition.Query.Duration == "ThisBattle" &&
            condition.Query.Subtype == Snack &&
            !condition.TrackTriggerCount && condition.Comparator == 2 && condition.Value == 20 && !condition.AllowMultiple,
            "Captured BigDamageFromManyMorsels owner or condition changed.");
    }

    private static int SnackCount(TrainCombatState state) => state.Context!.Statistics!.SubtypesSpawnedThisBattle
        .FirstOrDefault(item => item.Key == Snack)?.Value ?? 0;

    private static string? CardData(TrainCombatState state, int unitId)
    {
        CombatUnit? unit = state.Rooms.SelectMany(room => room.Units).FirstOrDefault(item => item.Id == unitId);
        return unit == null ? null : state.Context!.FindCard(unit.SpawnerCardId)?.DataId;
    }

    private static int Floor(TrainCombatState state, int unitId) =>
        state.Rooms.Single(room => room.Units.Any(unit => unit.Id == unitId)).RoomIndex;

    private static UnitCloneCallback[] BehavioralDispatches(TrainCombatState before,
        IEnumerable<UnitCloneCallback> callbacks) => callbacks.Where(callback => before.Rooms.SelectMany(room => room.Units)
        .FirstOrDefault(unit => unit.Id == callback.ActorId) is CombatUnit actor &&
        (actor.Statuses.Count > 0 || actor.Triggers.Any(trigger => trigger.Kind == callback.Kind))).ToArray();

    private static TrainCombatState WithSnackCount(TrainCombatState source, int value)
    {
        JsonObject node = JsonNode.Parse(JsonSerializer.Serialize(source, ModelJson.Options))!.AsObject();
        Set(node["Context"]!.AsObject());
        foreach (JsonNode room in node["Rooms"]!.AsArray()) Set(room["Context"]!.AsObject());
        return JsonSerializer.Deserialize<TrainCombatState>(node.ToJsonString(), ModelJson.Options)!;

        void Set(JsonObject context)
        {
            JsonArray counts = context["Statistics"]!["SubtypesSpawnedThisBattle"]!.AsArray();
            JsonObject? count = counts.Select(item => item!.AsObject()).FirstOrDefault(item => item["Key"]!.GetValue<string>() == Snack);
            if (count == null) counts.Add(new JsonObject { ["Key"] = Snack, ["Value"] = value });
            else count["Value"] = value;
        }
    }

    private static T Read<T>(FixtureValue value) => value.Deserialize<T>()!;
    private static void Equal<T>(T actual, T expected, string label) =>
        Require(JsonSerializer.Serialize(actual, ModelJson.Options) == JsonSerializer.Serialize(expected, ModelJson.Options),
            "Native summon-damage " + label + " differs. actual=" +
            JsonSerializer.Serialize(actual, ModelJson.Options) + " expected=" +
            JsonSerializer.Serialize(expected, ModelJson.Options));
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
