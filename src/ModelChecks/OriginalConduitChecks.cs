using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalConduitChecks
{
    private static readonly (string Key, string Id, string Trigger, int Amount)[] Owners = [
        ("Inkbound_Clepsydranium", "b0972732-ea21-42d0-844d-05fb5ff028e6", "OnKill", 1),
        ("EquipmentBoon_DH", "394a559b-0c45-4bac-880a-522b04f1780c", "OnEquipmentAdded", 4),
        ("AddConduitOnSpawn", "357c7265-74d0-4567-b793-d2fbac464d82", "", 4)
    ];
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-conduit-relics" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "Original Conduit battle failed native acceptance.");
        var first = root.GetProperty("Turns")[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        bool physical = first.Spawn.Train.Context!.SpawnPoints != null;
        if (physical)
        {
            Require(first.Spawn.Train.Context.ConduitRetained != null && root.GetProperty("PhysicalCompactions").GetArrayLength() > 0,
                "Physical Conduit battle omitted retained tracker capture or native compactions.");
            BattleSpawnPointChecks.DecisionReferences(root);
        }
        ConduitChecks.Run(first.Spawn.Train.Context!.MagicPower![0].Conduit!.Upgrade);
        var definitions = new Dictionary<string, CombatTrigger>();
        void Definitions(CombatContext context)
        {
            foreach (var item in Owners)
            {
                var owner = context.Relics!.Single(relic => relic.DataId == item.Id);
                Require(owner.AssetKey == item.Key && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
                    owner.EffectTypes.SequenceEqual([item.Trigger == "" ? RelicModel.AddStatusOnSpawn : RelicModel.AddTrigger]),
                    "Original Conduit owner changed: " + item.Key);
                if (item.Trigger == "")
                {
                    var rule = owner.SpawnStatuses!.Single(); var condition = rule.Conditions.Single();
                    Require(rule.EffectIndex == 0 && rule.TargetPlayer && !rule.TargetEnemy && rule.SubtypeIsNone && !rule.SubtypeIsPyre &&
                        rule.Statuses.Single().Id == "conduit" && rule.Statuses.Single().Stacks == 4 &&
                        rule.CharacterAssetKeys.Count == 0 && rule.ExcludedSubtypes.Count == 0 && rule.RestrictedRoom == null &&
                        rule.HpPercentAsStacks == 0 && !rule.RequireGraft && !rule.ApplyToTeam &&
                        condition.Query.Type == "SubtypeInDeck" && condition.Query.Duration == "ThisTurn" && condition.Query.SubtypeIsNone &&
                        condition.TrackTriggerCount && condition.Comparator == 6 && condition.Value == 1 && !condition.AllowMultiple,
                        "Original first-summon Conduit filter or condition changed.");
                    continue;
                }
                var addition = owner.TriggerAdditions!.Single(); var trigger = addition.Triggers.Single(); var effect = trigger.Effects.Single();
                Require(addition.EffectIndex == 0 && addition.SourcePlayer && !addition.SourceEnemy && addition.SubtypeIsNone &&
                    !addition.SubtypeIsPyre && addition.NoCountModifiersAllowed && addition.CharacterAssetKeys.Count == 0 &&
                    addition.Conditions.Count == 0 && addition.ExternalInteractions.Count == 0 &&
                    trigger.Kind == item.Trigger && !trigger.Once && !trigger.HasTriggered && trigger.IgnoreSilence &&
                    trigger.FireCount == 1 && trigger.Origin?.OnlyIfEquipped == false && trigger.SkipDuringDeployment == false &&
                    effect.Type == "CardEffectAddStatusEffect" && effect.Action is { Type: "AddStatus", Target: "Self", AllowPlayer: true } action &&
                    action.AllowEnemy == (item.Trigger == "OnEquipmentAdded") && action.Statuses.Single().Id == "conduit" &&
                    action.Statuses.Single().Stacks == item.Amount && effect.StatusScaling?.IgnoreDualism == (item.Trigger == "OnEquipmentAdded"),
                    "Original Conduit trigger definition changed: " + item.Key);
                if (definitions.TryGetValue(item.Id, out var previous)) Equal(previous, trigger, "stable trigger definition");
                else definitions.Add(item.Id, trigger);
            }
        }
        int births = 0, enemies = 0, firstSummons = 0, laterSummons = 0, slays = 0, equipment = 0, scaledSpells = 0;
        foreach (var row in root.GetProperty("RelicSpawnStatuses").EnumerateArray())
        {
            var before = row.GetProperty("BeforeTrain").Deserialize<TrainCombatState>()!;
            var actual = row.GetProperty("AfterTrain").Deserialize<TrainCombatState>()!;
            var queue = row.GetProperty("QueuedBefore").Deserialize<UnitCloneCallback[]>()!;
            Definitions(before.Context!);
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            var result = RelicBirthModel.CharacterAdded(before, row.GetProperty("UnitId").GetInt32(),
                row.GetProperty("FromCardId").GetInt32(), row.GetProperty("OnlyCovenants").GetBoolean(),
                row.GetProperty("DeferCallbacks").GetBoolean(), queue);
            Require(result.Supported, "Conduit birth unsupported: " + result.UnsupportedReason);
            Equal(result.State, actual, "birth " + births);
            Equal(result.Queued, row.GetProperty("QueuedAfter").Deserialize<UnitCloneCallback[]>(), "birth queue");
            Equal(result.Dispatched, row.GetProperty("Dispatched").Deserialize<UnitCloneCallback[]>(), "birth dispatches");
            var unit = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == row.GetProperty("UnitId").GetInt32());
            var control = RelicBirthModel.CharacterAdded(Remove(before, Owners[2].Id), unit.Id,
                row.GetProperty("FromCardId").GetInt32(), row.GetProperty("OnlyCovenants").GetBoolean(),
                row.GetProperty("DeferCallbacks").GetBoolean(), queue);
            Require(control.Supported, "First-summon Conduit ablation unsupported.");
            int delta = Positive(actual.Rooms.SelectMany(room => room.Units), control.State!.Rooms.SelectMany(room => room.Units), 4);
            if (unit.Team == CombatTeam.Enemy) { Require(delta == 0, "Enemy birth received Conduit."); enemies++; }
            else if (delta > 0) firstSummons++; else laterSummons++;
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Conduit birth mutated its parent.");
            births++;
        }
        foreach (var row in root.GetProperty("Stages").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            Definitions(before.Context!);
            var without = Remove(before, Owners[0].Id);
            var result = row.GetProperty("Kind").GetString() == "Exchange" ? RoomCombatModel.Exchange(without) : RoomCombatModel.Resolve(without);
            Require(result.Supported, "Slay Conduit ablation unsupported: " + result.UnsupportedReason);
            slays += Positive(actual.Units, result.State!.Units, 1);
        }
        foreach (var row in root.GetProperty("Actions").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Definitions(before.Spawn.Train.Context!);
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            if (before.PlayRules!.Cards.Single(rule => rule.DataId == before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.DataId).Effect == "Equipment")
            {
                var ablated = BattleActionModel.PlayCard(Remove(before, Owners[1].Id), action);
                Require(ablated.Supported, "Equipment Conduit ablation unsupported: " + ablated.Reason);
                equipment += Positive(actual.Spawn.Train.Rooms.SelectMany(room => room.Units),
                    ablated.State!.Spawn.Train.Rooms.SelectMany(room => room.Units), 4);
            }
            if (before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.Temporary.Upgrades.Any(upgrade =>
                upgrade.DataId.StartsWith("conduit:", StringComparison.Ordinal) && upgrade.Stats.Damage > 0)) scaledSpells++;
        }
        Require(firstSummons > 0 && laterSummons > 0 && enemies > 0 && slays > 0 && equipment > 0 && scaledSpells > 0,
            $"Missing positive Conduit owner, spell upgrade or birth controls: first={firstSummons},later={laterSummons},enemy={enemies},slay={slays},equipment={equipment},scaled={scaledSpells}.");
        BattleActionChecks.Native(root);
        Console.WriteLine($"ORIGINAL-CONDUIT-NATIVE PASS: {births} exact births; first={firstSummons}, later={laterSummons}, enemy={enemies}; positive owner ablations slay={slays}, equipment={equipment}; {scaledSpells} upgraded spells; physical={physical}.");
    }
    private static int Positive(IEnumerable<CombatUnit> actual, IEnumerable<CombatUnit> without, int amount)
    {
        var controls = without.ToDictionary(unit => unit.Id);
        return actual.Count(unit => controls.TryGetValue(unit.Id, out var other) &&
            (unit.Status("conduit")?.Stacks ?? 0) - (other.Status("conduit")?.Stacks ?? 0) >= amount);
    }
    private static RoomCombatState Remove(RoomCombatState state, string id)
    {
        var owner = state.Context!.Relics!.Single(relic => relic.DataId == id);
        var definition = owner.TriggerAdditions?.Single().Triggers.Single();
        return new(state.RoomIndex, state.Deployment, state.Units.Select(unit => unit.WithTriggers(unit.Triggers.Where(trigger =>
            definition == null || trigger.Kind != definition.Kind || JsonSerializer.Serialize(trigger.Effects) != JsonSerializer.Serialize(definition.Effects)).ToArray())).ToArray(),
            state.ExternalInteractions, state.Context.WithRelics(state.Context.Relics.Where(relic => relic.DataId != id).ToArray()),
            state.Preview, state.ActiveCompanionBossIds);
    }
    private static TrainCombatState Remove(TrainCombatState train, string id)
    {
        var rooms = train.Rooms.Select(room => Remove(room, id)).ToArray();
        return new(rooms, train.Movement, train.EnemySlotsPerRoom, rooms[0].Context);
    }
    private static BattleTurnState Remove(BattleTurnState state, string id)
    {
        var spawn = state.Spawn;
        var copied = new EnemySpawnState(Remove(spawn.Train, id), spawn.Waves, spawn.SelectedGroups, spawn.Phase, spawn.Looping,
            spawn.Rng, spawn.NextUnitId, spawn.Treasures, spawn.TreasuresRemaining, spawn.TreasureEnabled, spawn.FirstTreasureTurn,
            spawn.FirstTreasureRoom, spawn.Turn, spawn.ExternalInteractions, spawn.CanonicalDecisionReferences, spawn.PendingDestroyedUnitIds);
        return new(copied, state.Energy, state.EnergyPerTurn, state.DrawPerTurn, state.ForgePoints, state.DragonsHoard,
            state.MoonPhase, state.RngStreams, state.OtherPiles, state.ExternalInteractions, state.PlayRules, state.BattlePreviewEnabled,
            state.UiRngIsolated, state.CanonicalDecisionReferences, state.CanonicalPhysicalReferences, state.SelectedRoom, state.MoonPhaseTrackingActive);
    }
    private static void Equal<T>(T a, T b, string name) => Require(JsonSerializer.Serialize(a, ModelJson.Options) == JsonSerializer.Serialize(b, ModelJson.Options),
        "Conduit " + name + " differs: " + ModelJson.Difference(JsonSerializer.Serialize(a, ModelJson.Options), JsonSerializer.Serialize(b, ModelJson.Options)));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
