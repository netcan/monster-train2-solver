using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalEquipmentGrowthChecks
{
    private const string GearId = "fc119000-0000-4000-8000-000000000391";
    private const string WoundId = "fc119000-0000-4000-8000-000000000393";
    private const string HealId = "fc119000-0000-4000-8000-000000000394";
    private const string SacrificeId = "fc119000-0000-4000-8000-000000000395";
    private static readonly (string Key, string Id, string[] Kinds)[] Owners = [
        ("UpgradeEquipmentOnDeath", "b3e7888f-8f5e-4082-89b0-078d80ce4a9b", ["OnKill"]),
        ("AdvancedGrowth", "449f6464-8164-48ad-bf66-6107b10f7f5c", ["OnDeath"]),
        ("FriendlyUnitsGainDeathwishValor", "19f0d8e3-a3f8-4cac-b73d-46dc189a70ac", ["OnDeathwish", "OnDeathwishLost"])
    ];
    private const string ValorUpgrade = "01abbd02-c6cb-48f2-9799-1f78367a0386";
    internal static PlayCardAction? Choose(BattleTurnState state)
    {
        var context = state.Spawn.Train.Context!;
        var plays = BattleActionModel.EnumerateSupportedPlays(state);
        string? Data(int cardId) => context.FindCard(cardId)?.DataId;
        bool Played(string data) => context.Statistics!.Values.Any(value => value.Type == "TimesPlayed" && value.Duration == "ThisBattle" &&
            value.Value > 0 && context.FindCard(value.CardId)?.DataId == data);
        var players = state.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.Team == CombatTeam.Player && unit.Health > 0).ToArray();
        var room = state.Spawn.Train.Rooms[0];
        foreach (bool strong in new[] { true, false })
        {
            bool Matches(CombatUnit unit) => unit.MaxHealth == (strong ? 80 : 12);
            if (room.Units.Any(unit => unit.Team == CombatTeam.Player && Matches(unit))) continue;
            if (!strong && Played(SacrificeId)) continue;
            var summon = plays.FirstOrDefault(play => play.RoomIndex == 0 && play.PlayerPosition == room.Units.Count(unit => unit.Team == CombatTeam.Player) &&
                state.PlayRules!.Cards.FirstOrDefault(rule => rule.DataId == Data(play.CardInstanceId))?.SpawnUnit is CombatUnit template && Matches(template));
            if (summon != null) return summon;
        }
        foreach (var unit in players.OrderByDescending(unit => unit.MaxHealth))
        {
            if (unit.EquipmentCards?.Count != 0) continue;
            var gear = plays.FirstOrDefault(play => Data(play.CardInstanceId) == GearId && play.TargetUnitId == unit.Id);
            if (gear != null) return gear;
        }
        var fragile = players.FirstOrDefault(unit => unit.MaxHealth == 12 && unit.EquipmentCards?.Count > 0);
        if (fragile != null)
        {
            string? wanted = !Played(WoundId) && fragile.Health == 12 ? WoundId :
                Played(WoundId) && !Played(HealId) && fragile.Health < 6 ? HealId : Played(HealId) ? SacrificeId : null;
            var action = plays.FirstOrDefault(play => wanted != null && Data(play.CardInstanceId) == wanted && play.TargetUnitId == fragile.Id);
            if (action != null) return action;
        }
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(state, card => Data(card.InstanceId) != GearId && Data(card.InstanceId) != WoundId &&
            Data(card.InstanceId) != HealId && Data(card.InstanceId) != SacrificeId);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-equipment-growth-relics" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "Original equipment growth battle failed native acceptance.");
        TriggeredBuffChecks(root);
        var definitions = new Dictionary<string, string>();
        void Definitions(CombatContext context)
        {
            foreach (var item in Owners)
            {
                var owner = context.Relics!.Single(relic => relic.DataId == item.Id);
                var addition = owner.TriggerAdditions!.Single();
                Require(owner.AssetKey == item.Key && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
                    owner.EffectTypes.SequenceEqual([RelicModel.AddTrigger]) && addition.EffectIndex == 0 && addition.SourcePlayer &&
                    addition.SourceEnemy == (item.Key == "AdvancedGrowth") && addition.SubtypeIsNone && !addition.SubtypeIsPyre &&
                    addition.NoCountModifiersAllowed && addition.CharacterAssetKeys.Count == 0 && addition.Conditions.Count == 0 &&
                    addition.ExternalInteractions.Count == 0 && addition.Triggers.Select(trigger => trigger.Kind).SequenceEqual(item.Kinds),
                    "Original equipment growth owner or source filter changed: " + item.Key);
                foreach (var trigger in addition.Triggers)
                {
                    Require(!trigger.Once && !trigger.HasTriggered && trigger.IgnoreSilence && trigger.FireCount == 1 &&
                        trigger.Origin?.OnlyIfEquipped == (item.Key == "UpgradeEquipmentOnDeath") && trigger.SkipDuringDeployment == false &&
                        trigger.Effects.Count == 1, "Original growth trigger gate changed.");
                    var effect = trigger.Effects.Single(); var action = effect.UnitUpgrade!; var upgrade = action.Upgrade!;
                    bool valor = item.Key == "FriendlyUnitsGainDeathwishValor";
                    Require(action.Target == "Self" && action.AllowEnemy && action.AllowPlayer && !action.ScaleUpgrade &&
                        action.Statuses.Count == 0 && action.UpgradeFilters.Count == 0 && upgrade.ExternalInteractions.Count == 0 &&
                        upgrade.Statuses.Count == 0 && !upgrade.RemoveOnDiscard && upgrade.Unique == valor &&
                        upgrade.Stats.Health == 0 && upgrade.Stats.Heal == 0 && upgrade.Stats.Cost == 0 && upgrade.Stats.Size == 0,
                        "Original equipment growth action or upgrade flags changed.");
                    if (!valor)
                        Require(effect.Type == "CardEffectBuffEquipment" && action.Type == "BuffEquipment" &&
                            action.Lifetime == "TemporaryUntilUnitDeath" && upgrade.Stats.Damage == (item.Key == "AdvancedGrowth" ? 4 : 2) &&
                            upgrade.AssetKey == (item.Key == "AdvancedGrowth" ? "AdvancedGrowth_Upgrade" : "ArtifactBuffEquipment"),
                            "Original equipment buff payload changed.");
                    else
                    {
                        var precombat = upgrade.TriggerUpgrades!.Single(); var status = precombat.Effects.Single().Action!;
                        Require(upgrade.DataId == ValorUpgrade && upgrade.AssetKey == "ValorOnTurnStart" && upgrade.Stats.Damage == 0 &&
                            action.Type == (trigger.Kind == "OnDeathwish" ? "UnitUpgrade" : "RemoveUnitUpgrade") &&
                            effect.Type == (trigger.Kind == "OnDeathwish" ? "CardEffectAddTempCardUpgradeToUnits" : "CardEffectRemoveTempUpgradeFromUnit") &&
                            action.Lifetime == (trigger.Kind == "OnDeathwish" ? "TemporaryUntilUnitDeath" : "TemporaryUntilEndOfBattle") &&
                            precombat.Kind == "PreCombat" && precombat.IgnoreSilence && !precombat.Once &&
                            status.Type == "AddStatus" && status.Target == "Self" && status.AllowEnemy && status.AllowPlayer &&
                            status.Statuses.Single().Id == "valor" && status.Statuses.Single().Stacks == 2,
                            "Original Deathwish Valor upgrade or PreCombat payload changed.");
                    }
                }
                string serialized = JsonSerializer.Serialize(addition, ModelJson.Options);
                if (definitions.TryGetValue(item.Id, out var previous)) Require(previous == serialized, "Original growth definition drifted.");
                else definitions.Add(item.Id, serialized);
            }
        }
        int births = 0, enemies = 0, friends = 0, exclusiveBirths = 0, slays = 0, deaths = 0, valor = 0, enters = 0, exits = 0;
        foreach (var row in root.GetProperty("RelicSpawnStatuses").EnumerateArray())
        {
            var before = row.GetProperty("BeforeTrain").Deserialize<TrainCombatState>()!;
            var actual = row.GetProperty("AfterTrain").Deserialize<TrainCombatState>()!;
            Definitions(before.Context!);
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            var result = RelicBirthModel.CharacterAdded(before, row.GetProperty("UnitId").GetInt32(),
                row.GetProperty("FromCardId").GetInt32(), row.GetProperty("OnlyCovenants").GetBoolean(),
                row.GetProperty("DeferCallbacks").GetBoolean(), row.GetProperty("QueuedBefore").Deserialize<UnitCloneCallback[]>()!);
            Require(result.Supported, "Equipment growth birth unsupported: " + result.UnsupportedReason);
            Equal(result.State, actual, "birth " + births);
            Equal(result.Queued, row.GetProperty("QueuedAfter").Deserialize<UnitCloneCallback[]>(), "birth queue");
            Equal(result.Dispatched, row.GetProperty("Dispatched").Deserialize<UnitCloneCallback[]>(), "birth dispatches");
            int id = row.GetProperty("UnitId").GetInt32();
            var unit = actual.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            bool enemy = unit.Team == CombatTeam.Enemy;
            Require(unit.Triggers.Any(trigger => trigger.Kind == "OnDeath" && trigger.Effects.Any(effect => effect.UnitUpgrade?.Upgrade?.AssetKey == "AdvancedGrowth_Upgrade")),
                "Both-team AdvancedGrowth installation missing.");
            Require(unit.Triggers.Any(trigger => trigger.Kind == "OnDeathwish" && trigger.Effects.Any(effect => effect.UnitUpgrade?.Upgrade?.DataId == ValorUpgrade)) == !enemy &&
                unit.Triggers.Any(trigger => trigger.Kind == "OnKill" && trigger.Effects.Any(effect => effect.UnitUpgrade?.Upgrade?.AssetKey == "ArtifactBuffEquipment")) == !enemy,
                "Friendly-only growth installation filter differs.");
            if (enemy) enemies++; else friends++;
            if (result.Queued.Any(callback => callback.ExclusiveTriggerStateId.HasValue)) exclusiveBirths++;
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Growth birth mutated its parent.");
            births++;
        }
        foreach (var row in root.GetProperty("Stages").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            Definitions(before.Context!);
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            var without = Remove(before, 0);
            var result = row.GetProperty("Kind").GetString() == "Exchange" ? RoomCombatModel.Exchange(without) : RoomCombatModel.Resolve(without);
            Require(result.Supported, "Growth stage ablation unsupported: " + result.UnsupportedReason);
            slays += CardGain(actual.Context!, result.State!.Context!, "ArtifactBuffEquipment", 2);
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Growth stage mutated its parent.");
        }
        foreach (var row in root.GetProperty("TriggeredStatuses").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("Interactions").GetArrayLength() == 0,
                "Growth native triggered status capture incomplete.");
            var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            var effect = row.GetProperty("Effect").Deserialize<CombatEffect>()!;
            int actor = row.GetProperty("ActorId").GetInt32();
            var exact = TriggeredStatusModel.Apply(before, actor, effect, row.GetProperty("Targets").Deserialize<int[]>()!,
                row.GetProperty("SourceCardId").GetInt32());
            Require(exact.Supported, "Growth native status unsupported: " + exact.UnsupportedReason);
            Equal(exact.State, actual, "native triggered status");
            if (row.GetProperty("Kind").GetString() != "PreCombat" || effect.Action!.Statuses.Single().Id != "valor") continue;
            Require(before.Units.Single(unit => unit.Id == actor).Triggers.Any(trigger =>
                trigger.Kind == "PreCombat" && trigger.Origin?.UpgradeId == ValorUpgrade), "Valor source is not the original installed upgrade.");
            var control = RoomCombatModel.ApplyPreCombat(Remove(before, 2), actor);
            Require(control.Supported, "Deathwish PreCombat ablation unsupported: " + control.UnsupportedReason);
            valor += actual.Units.Count(unit => control.State!.Units.Any(other => other.Id == unit.Id &&
                (unit.Status("valor")?.Stacks ?? 0) - (other.Status("valor")?.Stacks ?? 0) >= 2));
        }
        foreach (var row in root.GetProperty("Actions").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            Definitions(before.Spawn.Train.Context!);
            if (before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.DataId == SacrificeId)
            {
                var control = BattleActionModel.PlayCard(Remove(before, 1), action);
                Require(control.Supported, "Growth equipped death ablation unsupported: " + control.Reason);
                deaths += CardGain(actual.Spawn.Train.Context!, control.State!.Spawn.Train.Context!, "AdvancedGrowth_Upgrade", 4);
            }
            var previous = before.Spawn.Train.Rooms.SelectMany(room => room.Units).ToDictionary(unit => unit.Id);
            foreach (var unit in actual.Spawn.Train.Rooms.SelectMany(room => room.Units))
                if (previous.TryGetValue(unit.Id, out var old))
                {
                    int delta = ValorCount(unit) - ValorCount(old);
                    if (delta > 0) enters++; else if (delta < 0 && unit.Health > 0) exits++;
                }
        }
        Require(enemies > 0 && friends > 0 && exclusiveBirths > 0 && slays > 0 && deaths > 0 && valor > 0 && enters > 0 && exits > 0,
            $"Missing original growth controls: friend={friends},enemy={enemies},exclusive={exclusiveBirths},slay={slays},death={deaths},valor={valor},enter={enters},exit={exits}.");
        BattleActionChecks.Native(root);
        Console.WriteLine($"ORIGINAL-EQUIPMENT-GROWTH-NATIVE PASS: {births} exact births ({friends} friendly/{enemies} enemy, {exclusiveBirths} exclusive queues); owner ablations slay={slays}, death={deaths}, PreCombat Valor={valor}; Deathwish enter={enters}, exit={exits}.");
    }
    private static int ValorCount(CombatUnit unit) => unit.Modifiers!.Upgrades.Count(upgrade => upgrade.DataId == ValorUpgrade);
    private static void TriggeredBuffChecks(FixtureValue root)
    {
        var sample = root.GetProperty("Actions").EnumerateArray().Select(row => row.GetProperty("Before").Deserialize<BattleTurnState>()!)
            .First(state => state.Spawn.Train.Rooms.Any(room => room.Units.Count(unit => unit.EquipmentCards?.Count > 0) == 2));
        var native = sample.Spawn.Train.Rooms.First(room => room.Units.Count(unit => unit.EquipmentCards?.Count > 0) == 2);
        var host = native.Units.First(unit => unit.MaxHealth == 80);
        var gear = native.Units.SelectMany(unit => unit.EquipmentCards ?? []).Distinct().ToArray();
        var definition = native.Context!.Relics!.Single(relic => relic.DataId == Owners[0].Id).TriggerAdditions!.Single().Triggers.Single();
        foreach (bool preview in new[] { false, true })
        foreach (bool bound in new[] { false, true })
        {
            var context = native.Context.WithRelics([]);
            foreach (int id in gear)
            {
                var card = context.FindCard(id)!;
                var descriptor = card.MaskDescriptor!.WithBaseTraits([new("CardTraitStrongerMagicPower", "CardTraitStrongerMagicPower", 0, 0, true, 0)]);
                context = context.WithCard(new(id, card.DataId, card.Permanent, card.Temporary, card.LastPlayedCost, card.LastForgedAmount,
                    card.PlayCount, card.ExternalInteractions, equippedUnitId: host.Id, maskDescriptor: descriptor));
            }
            var trigger = new CombatTrigger("OnKill", false, false, true, 1, definition.Effects, false, 0,
                new("", bound ? gear[0] : 0, bound, true), 0, noCountModifiersAllowed: true);
            var actor = UnitCloneModel.Copy(host, host.Modifiers!, host.Attack, host.Health, host.MaxHealth, gear).WithTriggers([trigger]);
            var source = new RoomCombatState(native.RoomIndex, native.Deployment, [actor], [], context, preview);
            string parent = JsonSerializer.Serialize(source, ModelJson.Options);
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            var result = RoomCombatModel.ApplyQueuedCharacterTrigger(source,
                new(native.RoomIndex, actor, "OnKill"), callbacks.Add);
            Require(result.Supported && callbacks.Count == 0, "Triggered equipment isolation failed: " + result.UnsupportedReason);
            int affected = bound ? 1 : 2;
            Require(result.State!.Units.Single().Attack - actor.Attack == 2 * affected,
                "Triggered host upgrades reused the card's strong-magic scaling or ignored parent equipment binding.");
            foreach (int id in gear)
            {
                int delta = result.State.Context!.FindCard(id)!.Permanent.Upgrades.Sum(upgrade => upgrade.Stats.Damage) -
                    context.FindCard(id)!.Permanent.Upgrades.Sum(upgrade => upgrade.Stats.Damage);
                Require(delta == (!preview && (!bound || id == gear[0]) ? 10 : 0), "Triggered equipment permanent/preview or trait scaling differs.");
            }
            Require(JsonSerializer.Serialize(source, ModelJson.Options) == parent, "Triggered equipment mutated its parent.");
        }
    }
    private static int CardGain(CombatContext actual, CombatContext control, string key, int amount) => actual.CardRegistry!.Count(card =>
        card.Permanent.Upgrades.Where(upgrade => upgrade.AssetKey == key).Sum(upgrade => upgrade.Stats.Damage) -
        (control.FindCard(card.InstanceId)?.Permanent.Upgrades.Where(upgrade => upgrade.AssetKey == key).Sum(upgrade => upgrade.Stats.Damage) ?? 0) >= amount);
    private static RoomCombatState Remove(RoomCombatState state, int index)
    {
        var owner = state.Context!.Relics!.Single(relic => relic.DataId == Owners[index].Id);
        var kinds = owner.TriggerAdditions!.Single().Triggers;
        return new(state.RoomIndex, state.Deployment, state.Units.Select(unit => unit.WithTriggers(unit.Triggers.Where(trigger =>
            !(index == 2 && trigger.Kind == "PreCombat" && trigger.Origin?.UpgradeId == ValorUpgrade) &&
            !kinds.Any(definition => trigger.Kind == definition.Kind && JsonSerializer.Serialize(trigger.Effects) == JsonSerializer.Serialize(definition.Effects))).ToArray())).ToArray(),
            state.ExternalInteractions, state.Context.WithRelics(state.Context.Relics.Where(relic => relic.DataId != owner.DataId).ToArray()),
            state.Preview, state.ActiveCompanionBossIds);
    }
    private static BattleTurnState Remove(BattleTurnState state, int index)
    {
        var spawn = state.Spawn; var train = spawn.Train;
        var rooms = train.Rooms.Select(room => Remove(room, index)).ToArray();
        var copied = new EnemySpawnState(new(rooms, train.Movement, train.EnemySlotsPerRoom, rooms[0].Context), spawn.Waves,
            spawn.SelectedGroups, spawn.Phase, spawn.Looping, spawn.Rng, spawn.NextUnitId, spawn.Treasures, spawn.TreasuresRemaining,
            spawn.TreasureEnabled, spawn.FirstTreasureTurn, spawn.FirstTreasureRoom, spawn.Turn, spawn.ExternalInteractions,
            spawn.CanonicalDecisionReferences, spawn.PendingDestroyedUnitIds);
        return new(copied, state.Energy, state.EnergyPerTurn, state.DrawPerTurn, state.ForgePoints, state.DragonsHoard,
            state.MoonPhase, state.RngStreams, state.OtherPiles, state.ExternalInteractions, state.PlayRules, state.BattlePreviewEnabled,
            state.UiRngIsolated, state.CanonicalDecisionReferences, state.CanonicalPhysicalReferences, state.SelectedRoom, state.MoonPhaseTrackingActive);
    }
    private static void Equal<T>(T a, T b, string name) => Require(JsonSerializer.Serialize(a, ModelJson.Options) == JsonSerializer.Serialize(b, ModelJson.Options),
        "Equipment growth " + name + " differs: " + ModelJson.Difference(JsonSerializer.Serialize(a, ModelJson.Options), JsonSerializer.Serialize(b, ModelJson.Options)));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
