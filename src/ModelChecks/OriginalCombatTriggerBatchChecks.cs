using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalCombatTriggerBatchChecks
{
    private static readonly (string Key, string Id, string Kind, string Target, string Status, int Amount)[] Owners = [
        ("ShroomsGainSapDecay", "3accd42e-3f82-4bf1-9026-29a624d3dde5", "OnSpawn", "Room", "decay", 1)
    ];
    private const string Shroom = "SubtypesData_Shroom";
    internal static PlayCardAction? Choose(BattleTurnState state)
    {
        bool IsShroom(int cardId) => state.PlayRules!.Cards.FirstOrDefault(rule =>
            rule.DataId == state.Spawn.Train.Context!.FindCard(cardId)?.DataId)?.SpawnUnit?.Subtypes.Contains(Shroom) == true;
        if (state.Spawn.Turn > 0)
        {
            var positive = BattleActionModel.EnumerateSupportedPlays(state).FirstOrDefault(play => IsShroom(play.CardInstanceId) &&
                state.Spawn.Train.Rooms.Single(room => room.RoomIndex == play.RoomIndex).Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0));
            if (positive != null) return positive;
        }
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(state, card => !IsShroom(card.InstanceId));
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-combat-trigger-relics" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "Combat trigger battle failed acceptance.");
        var definitions = new Dictionary<string, CombatTrigger>();
        void Definitions(CombatContext context)
        {
            foreach (var item in Owners)
            {
                var owner = context.Relics!.Single(relic => relic.DataId == item.Id);
                Require(owner.AssetKey == item.Key && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
                    owner.EffectTypes.SequenceEqual([RelicModel.AddTrigger]), "Original combat trigger owner changed.");
                var rule = owner.TriggerAdditions!.Single(); var trigger = rule.Triggers.Single(); var effect = trigger.Effects.Single();
                Require(rule.EffectIndex == 0 && rule.SourcePlayer && !rule.SourceEnemy && rule.NoCountModifiersAllowed &&
                    rule.SubtypeIsNone == (item.Status != "decay") && !rule.SubtypeIsPyre &&
                    (item.Status != "decay" || rule.RequiredSubtype == Shroom) &&
                    rule.CharacterAssetKeys.Count == 0 && rule.Conditions.Count == 0 && rule.ExternalInteractions.Count == 0 &&
                    trigger.Kind == item.Kind && !trigger.Once && trigger.IgnoreSilence && trigger.Origin?.OnlyIfEquipped == false &&
                    effect.Type == "CardEffectAddStatusEffect" && effect.Action is { Type: "AddStatus" } action &&
                    action.Target == item.Target && action.AllowEnemy == (item.Key != "Inkbound_Clepsydranium") && action.AllowPlayer == (item.Status != "decay") &&
                    action.Statuses.Single().Id == item.Status && action.Statuses.Single().Stacks == item.Amount,
                    "Original combat trigger filter or action payload changed: " + item.Key);
                if (definitions.TryGetValue(item.Id, out var previous)) Equal(previous, trigger, "stable original trigger");
                else definitions.Add(item.Id, trigger);
            }
        }
        int births = 0, enemies = 0, nonShrooms = 0, shrooms = 0;
        foreach (var row in root.GetProperty("RelicSpawnStatuses").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "Incomplete or mismatched original trigger birth.");
            var before = row.GetProperty("BeforeTrain").Deserialize<TrainCombatState>()!;
            var actual = row.GetProperty("AfterTrain").Deserialize<TrainCombatState>()!;
            Definitions(before.Context!);
            var queue = row.GetProperty("QueuedBefore").Deserialize<UnitCloneCallback[]>()!;
            string parent = Serialize(new { before, queue });
            int id = row.GetProperty("UnitId").GetInt32();
            var result = RelicBirthModel.CharacterAdded(before, id, row.GetProperty("FromCardId").GetInt32(),
                row.GetProperty("OnlyCovenants").GetBoolean(), row.GetProperty("DeferCallbacks").GetBoolean(), queue);
            Require(result.Supported, "Original trigger birth unsupported: " + result.UnsupportedReason);
            Equal(result.State, actual, "complete native birth/train state");
            Equal(result.Queued, row.GetProperty("QueuedAfter").Deserialize<UnitCloneCallback[]>(), "native birth queue");
            Equal(result.Dispatched, row.GetProperty("Dispatched").Deserialize<UnitCloneCallback[]>(), "native birth dispatches");
            var priorUnit = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            var afterUnit = actual.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            bool player = priorUnit.Team == CombatTeam.Player && !priorUnit.IsPyre;
            if (!player) enemies++; else if (priorUnit.Subtypes.Contains(Shroom)) shrooms++; else nonShrooms++;
            foreach (var item in Owners)
            {
                bool eligible = player && (item.Status != "decay" || priorUnit.Subtypes.Contains(Shroom));
                var added = afterUnit.Triggers.Where(trigger => !priorUnit.Triggers.Any(old => old.StateId == trigger.StateId) &&
                    Matches(trigger, definitions[item.Id])).ToArray();
                Require(added.Length == (eligible ? 1 : 0) && added.All(trigger => trigger.NoCountModifiersAllowed == true && trigger.FireCount == 1),
                    "Original trigger installation/subtype/team gate differs: " + item.Key);
            }
            Require(parent == Serialize(new { before, queue }), "Birth replay mutated native parent."); births++;
        }
        var witnesses = Owners.ToDictionary(item => item.Id, _ => 0);
        foreach (var row in root.GetProperty("Actions").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Definitions(before.Spawn.Train.Context!);
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            foreach (var item in Owners.Where(item => item.Kind != "OnKill"))
            {
                var ablated = BattleActionModel.PlayCard(Remove(before, item.Id, definitions[item.Id]), action);
                Require(ablated.Supported, "Original trigger action ablation unsupported: " + ablated.Reason);
                witnesses[item.Id] += Positive(actual.Spawn.Train.Rooms.SelectMany(room => room.Units),
                    ablated.State!.Spawn.Train.Rooms.SelectMany(room => room.Units), item.Status, item.Amount);
            }
        }
        int damageStages = 0;
        foreach (var row in root.GetProperty("Stages").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            var result = row.GetProperty("Kind").GetString() == "Exchange" ? RoomCombatModel.Exchange(before) : RoomCombatModel.Resolve(before);
            Require(result.Supported, "Original decay stage unsupported: " + result.UnsupportedReason);
            if (result.Events.Any(item => item.Kind == "Decay" && item.Amount > 0)) damageStages++;
        }
        Require(births > 0 && enemies > 0 && shrooms > 0 && nonShrooms > 0 && damageStages > 0 && witnesses.Values.All(count => count > 0),
            "Missing decay damage, positive owner ablation, enemy or subtype controls: " + string.Join(",", witnesses.Select(pair => pair.Key + "=" + pair.Value)));
        BattleActionChecks.Native(root);
        Console.WriteLine($"ORIGINAL-COMBAT-TRIGGER-NATIVE PASS: {births} exact births, {enemies} enemy/{nonShrooms} non-Shroom controls, unchanged owner with positive future-effect ablations: " +
            string.Join(", ", Owners.Select(item => item.Key + "=" + witnesses[item.Id])) + $"; {damageStages} damaging decay stages");
    }
    private static int Positive(IEnumerable<CombatUnit> actual, IEnumerable<CombatUnit> without, string status, int amount)
    {
        var controls = without.ToDictionary(unit => unit.Id);
        return actual.Count(unit => controls.TryGetValue(unit.Id, out var control) &&
            (unit.Status(status)?.Stacks ?? 0) - (control.Status(status)?.Stacks ?? 0) >= amount);
    }
    private static bool Matches(CombatTrigger trigger, CombatTrigger definition) => trigger.Kind == definition.Kind &&
        trigger.Origin?.UpgradeId == "" && trigger.Origin.EquipmentCardId == 0 && Serialize(trigger.Effects) == Serialize(definition.Effects);
    private static RoomCombatState Remove(RoomCombatState state, string id, CombatTrigger definition) => new(state.RoomIndex, state.Deployment,
        state.Units.Select(unit => unit.WithTriggers(unit.Triggers.Where(trigger => !Matches(trigger, definition)).ToArray())).ToArray(),
        state.ExternalInteractions, state.Context!.WithRelics(state.Context.Relics!.Where(owner => owner.DataId != id).ToArray()), state.Preview, state.ActiveCompanionBossIds);
    private static BattleTurnState Remove(BattleTurnState state, string id, CombatTrigger definition)
    {
        var context = state.Spawn.Train.Context!.WithRelics(state.Spawn.Train.Context.Relics!.Where(owner => owner.DataId != id).ToArray());
        var train = new TrainCombatState(state.Spawn.Train.Rooms.Select(room => Remove(room, id, definition)).ToArray(),
            state.Spawn.Train.Movement, state.Spawn.Train.EnemySlotsPerRoom, context);
        var spawn = state.Spawn;
        var copied = new EnemySpawnState(train, spawn.Waves, spawn.SelectedGroups, spawn.Phase, spawn.Looping, spawn.Rng,
            spawn.NextUnitId, spawn.Treasures, spawn.TreasuresRemaining, spawn.TreasureEnabled, spawn.FirstTreasureTurn,
            spawn.FirstTreasureRoom, spawn.Turn, spawn.ExternalInteractions, spawn.CanonicalDecisionReferences, spawn.PendingDestroyedUnitIds);
        return new(copied, state.Energy, state.EnergyPerTurn, state.DrawPerTurn, state.ForgePoints, state.DragonsHoard,
            state.MoonPhase, state.RngStreams, state.OtherPiles, state.ExternalInteractions, state.PlayRules, state.BattlePreviewEnabled,
            state.UiRngIsolated, state.CanonicalDecisionReferences, state.CanonicalPhysicalReferences, state.SelectedRoom, state.MoonPhaseTrackingActive);
    }
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal<T>(T a, T b, string name) => Require(Serialize(a) == Serialize(b), "Original combat trigger " + name + " differs: " + ModelJson.Difference(Serialize(a), Serialize(b)));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
