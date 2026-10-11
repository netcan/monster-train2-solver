using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalLifecycleRelicBatchChecks
{
    private const string Shroom = "SubtypesData_Shroom";
    private static readonly (string Key, string Id, string Kind, string Target, string Status, int Amount)[] Artifacts =
    [
        ("IncreaseArmorStacks_DH", "c9b18f6f-20d1-4228-bfab-b740609db8dc", "OnOwnAbilityActivated", "Room", "armor", 3),
        ("AvariceOnSummon", "644749c2-6ee3-4ce6-91ed-420077ee0193", "OnOwnAbilityActivated", "Room", "", 5),
        ("ShroomsGainRegen", "bbe03be9-dae9-40bb-bb46-ab7553a17bd9", "OnAnyUnitDeathOnFloor", "Self", "regen", 1),
        ("DamageOnGoingDownFloor", "19147050-9859-4cf1-b826-f258f6c48c42", "OnDeath", "FrontInRoom", "armor", 5),
        ("LifeInsurer", "c1ae061c-5f95-4f1d-a8f0-6e027636a81e", "OnReanimated", "Self", "armor", 14)
    ];

    internal static IReadOnlyDictionary<string, int>? Native(FixtureValue root, bool required = false,
        bool replayBattle = false)
    {
        bool eligible = root.TryGetProperty("ModifierScenario", out var scenario) &&
            scenario.GetString() == "original-lifecycle-relic-batch";
        Require(eligible || !required, "Fixture lacks the original lifecycle relic batch.");
        if (!eligible) return null;
        Require(root.GetProperty("Schema").GetInt32() == 115 &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4",
            "Native lifecycle relic schema or build changed.");
        Require(root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("Policy").GetString() == "units-spells-and-junk",
            "Native lifecycle relic battle lacks a settled win and independently replayed policy.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Native lifecycle relic capture has nonzero " + field + ".");

        var definitions = new Dictionary<string, CombatTrigger>();
        var installs = Artifacts.ToDictionary(item => item.Key, _ => 0);
        var rejections = Artifacts.ToDictionary(item => item.Key, _ => 0);
        var witnesses = Artifacts.ToDictionary(item => item.Key, _ => 0);
        int births = 0, enemyBirths = 0, shroomBirths = 0, rejectedSubtypes = 0;
        foreach (FixtureValue row in root.GetProperty("RelicSpawnStatuses").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "Native lifecycle relic birth is incomplete or differs.");
            var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = row.GetProperty("After").Deserialize<RoomCombatState>()!;
            CheckDefinitions(before.Context!.Relics!, definitions);
            int id = row.GetProperty("UnitId").GetInt32(), fromCard = row.GetProperty("FromCardId").GetInt32();
            bool covenants = row.GetProperty("OnlyCovenants").GetBoolean();
            var train = row.GetProperty("BeforeTrain").Deserialize<TrainCombatState>()!;
            var actualTrain = row.GetProperty("AfterTrain").Deserialize<TrainCombatState>()!;
            var priorQueue = row.GetProperty("QueuedBefore").Deserialize<UnitCloneCallback[]>()!;
            var actualQueue = row.GetProperty("QueuedAfter").Deserialize<UnitCloneCallback[]>()!;
            var actualDispatches = row.GetProperty("Dispatched").Deserialize<UnitCloneCallback[]>()!;
            bool defer = row.GetProperty("DeferCallbacks").GetBoolean();
            bool independent = row.GetProperty("StandaloneScenarioActive").GetBoolean() &&
                !row.GetProperty("ReplayCardPlayingBefore").GetBoolean();
            string? protocol = row.GetProperty("SettlementProtocol").GetString();
            Require(protocol is "standalone-scenario-birth" or "enclosing-native-coroutine" &&
                (protocol == "standalone-scenario-birth") == independent &&
                defer == (!independent || row.GetProperty("QueueRunningBefore").GetBoolean()) &&
                row.GetProperty("UnsupportedQueueReason").ValueKind == FixtureKind.Null,
                "Lifecycle relic birth settlement protocol or native queue flags differ.");
            Equal(train.Rooms.Single(room => room.RoomIndex == before.RoomIndex), before, "native birth room/train input");
            Equal(actualTrain.Rooms.Single(room => room.RoomIndex == after.RoomIndex), after, "native birth room/train output");
            string parent = Serialize(new { train, priorQueue });
            void Check()
            {
                RelicBirthResult result = RelicBirthModel.CharacterAdded(train, id, fromCard, covenants, defer, priorQueue);
                Require(result.Supported, "Lifecycle relic birth unsupported: " + result.UnsupportedReason);
                Equal(result.State, actualTrain, "complete native lifecycle relic birth");
                Equal(result.Queued, actualQueue, "native birth callback FIFO");
                Equal(result.Dispatched, actualDispatches, "native birth dispatch FIFO");
            }
            Check(); Parallel.For(0, 32, _ => Check());
            Require(Serialize(new { train, priorQueue }) == parent, "Parallel lifecycle births mutated their parent or queue.");

            CombatUnit prior = before.Units.Single(unit => unit.Id == id), actor = after.Units.Single(unit => unit.Id == id);
            if (actor.Team == CombatTeam.Enemy) enemyBirths++;
            if (actor.Team == CombatTeam.Player && actor.Subtypes.Contains(Shroom)) shroomBirths++;
            if (actor.Team == CombatTeam.Player && !actor.IsPyre && !actor.Subtypes.Contains(Shroom)) rejectedSubtypes++;
            var expectedOrder = new List<string>();
            foreach (var item in Artifacts)
            {
                CombatTrigger[] added = actor.Triggers.Where(trigger => Matches(trigger, definitions[item.Key]) &&
                    !prior.Triggers.Any(old => old.StateId == trigger.StateId)).ToArray();
                bool expected = !covenants && !before.Preview && actor.Team == CombatTeam.Player && !actor.IsPyre &&
                    (item.Key != "ShroomsGainRegen" || actor.Subtypes.Contains(Shroom));
                Require(added.Length == (expected ? 1 : 0), "Original lifecycle installation/team/subtype gate differs: " + item.Key);
                if (!expected) { rejections[item.Key]++; continue; }
                Require(added[0].NoCountModifiersAllowed == true && added[0].FireCount == 1 &&
                    added[0].StateId.HasValue && added[0].Origin is
                    { UpgradeId: "", EquipmentCardId: 0, IsFromEquipment: false } && !added[0].HasTriggered,
                    "Original lifecycle fresh trigger identity/count/origin differs: " + item.Key);
                installs[item.Key]++;
            }
            foreach (CombatRelicState relic in before.Context.Relics!)
            {
                var item = Artifacts.FirstOrDefault(item => item.Id == relic.DataId);
                if (item.Key != null && !covenants && !before.Preview && actor.Team == CombatTeam.Player && !actor.IsPyre &&
                    (item.Key != "ShroomsGainRegen" || actor.Subtypes.Contains(Shroom))) expectedOrder.Insert(0, item.Key);
            }
            string[] nativeOrder = actor.Triggers.Where(trigger => !prior.Triggers.Any(old => old.StateId == trigger.StateId))
                .Select(trigger => Artifacts.FirstOrDefault(item => Matches(trigger, definitions[item.Key])).Key)
                .Where(key => key != null).ToArray()!;
            Require(nativeOrder.SequenceEqual(expectedOrder), "Native relic additions lost manager order or prepend order.");
            births++;
        }
        Require(births > 0 && enemyBirths > 0 && shroomBirths > 0 && rejectedSubtypes > 0 &&
            installs.Values.All(count => count > 0) && rejections.Values.All(count => count > 0),
            "Missing lifecycle original installs, enemy rejections or Shroom subtype admission/rejection.");

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        FixtureValue[] stages = root.GetProperty("Stages").EnumerateArray().ToArray();
        FixtureValue[] turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        Require(actions.Length > 0 && stages.Length > 0 && turns.Length > 0 &&
            (turns[^1].GetProperty("ActualOutcome").GetInt32() == (int)RoomOutcome.BattleWon ||
                actions.Any(row => row.TryGetProperty("ActualOutcome", out var outcome) && outcome.GetInt32() == (int)RoomOutcome.BattleWon)),
            "Lifecycle relic archive lacks actual plays, combat turns or its terminal win.");
        int activations = 0, futureRevivals = 0;
        foreach (FixtureValue row in actions)
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CheckDefinitions(before.Spawn.Train.Context!.Relics!, definitions);
            string parent = Serialize(before);
            BattleActionResult modeled = BattleActionModel.PlayCard(before, action);
            Require(modeled.Supported, "Lifecycle native action unsupported: " + modeled.Reason);
            Equal(BattleTurnChecks.Comparable(modeled.State!), BattleTurnChecks.Comparable(actual), "complete lifecycle native action");
            if (row.TryGetProperty("ActualOutcome", out var outcome))
                Require(modeled.Outcome == (RoomOutcome)outcome.GetInt32(), "Lifecycle native action outcome differs.");
            CombatUnit[] prior = Units(before), actualUnits = Units(actual);
            int[] revivedIds = prior.Where(unit => unit.Status("undying")?.Stacks > 0 && actualUnits.Any(after =>
                after.Id == unit.Id && after.Health == 1 && (after.Status("undying")?.Stacks ?? 0) == unit.Status("undying")!.Stacks - 1))
                .Select(unit => unit.Id).ToArray();
            futureRevivals += revivedIds.Length;
            if (action.ActivatorUnitId > 0) activations++;
            foreach (var item in Artifacts)
            {
                if (!prior.Any(unit => unit.Triggers.Any(trigger => Matches(trigger, definitions[item.Key])))) continue;
                BattleTurnState withoutOwner = Remove(before, item.Id, definitions[item.Key]);
                CheckPreserved(prior, Units(withoutOwner), definitions[item.Key]);
                CheckCaches(before.Spawn.Train.Context!, withoutOwner.Spawn.Train.Context!);
                BattleActionResult ablated = BattleActionModel.PlayCard(withoutOwner, action);
                Require(ablated.Supported, "Lifecycle action owner ablation unsupported: " + item.Key + " " + ablated.Reason);
                bool relevant = item.Kind != "OnOwnAbilityActivated" || action.ActivatorUnitId > 0 &&
                    prior.Any(unit => unit.Id == action.ActivatorUnitId && unit.Triggers.Any(trigger => Matches(trigger, definitions[item.Key])));
                if (relevant) witnesses[item.Key] += Positive(item.Key, item.Amount, prior, actualUnits, Units(ablated.State!), revivedIds);
            }
            Require(Serialize(before) == parent, "Lifecycle action or owner ablation mutated its native parent.");
        }
        foreach (FixtureValue row in stages)
        {
            var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            CheckDefinitions(before.Context!.Relics!, definitions);
            string parent = Serialize(before);
            bool exchange = row.GetProperty("Kind").GetString() == "Exchange";
            RoomCombatResult Run(RoomCombatState frame) => exchange ? RoomCombatModel.Exchange(frame) : RoomCombatModel.Resolve(frame);
            RoomCombatResult modeled = Run(before);
            Require(modeled.Supported, "Lifecycle native combat unsupported: " + modeled.UnsupportedReason);
            Equal(modeled.State, actual, "complete native lifecycle combat stage");
            int[] revivedIds = modeled.Events.Where(effect => effect.Kind == "Revive").Select(effect => effect.Target).Distinct().ToArray();
            futureRevivals += revivedIds.Length;
            foreach (var item in Artifacts)
            {
                if (!before.Units.Any(unit => unit.Triggers.Any(trigger => Matches(trigger, definitions[item.Key])))) continue;
                RoomCombatState withoutOwner = Remove(before, item.Id, definitions[item.Key]);
                CheckPreserved(before.Units, withoutOwner.Units, definitions[item.Key]); CheckCaches(before.Context!, withoutOwner.Context!);
                RoomCombatResult ablated = Run(withoutOwner);
                Require(ablated.Supported, "Lifecycle combat owner ablation unsupported: " + item.Key + " " + ablated.UnsupportedReason);
                witnesses[item.Key] += Positive(item.Key, item.Amount, before.Units, actual.Units, ablated.State!.Units, revivedIds);
            }
            Require(Serialize(before) == parent, "Lifecycle combat or owner ablation mutated its native parent.");
        }

        FixtureValue[] revivals = root.GetProperty("Revivals").EnumerateArray().ToArray();
        Require(revivals.Length > 0 && futureRevivals > 0 && activations > 0 &&
            root.GetProperty("RevivalOperations").GetArrayLength() == 0,
            "Lifecycle batch requires actual ability activations and natural Reanimated events.");
        foreach (FixtureValue row in revivals)
        {
            var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            int id = row.GetProperty("ActorId").GetInt32();
            CombatUnit prior = before.Units.Single(unit => unit.Id == id);
            CombatUnit after = row.GetProperty("AfterActor").Deserialize<CombatUnit>()!;
            var queue = row.GetProperty("Queued").EnumerateArray().Select(callback =>
                (callback.GetProperty("ActorId").GetInt32(), callback.GetProperty("Kind").GetString(), callback.GetProperty("ParamInt").GetInt32())).ToArray();
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("Error").ValueKind == FixtureKind.Null &&
                row.GetProperty("Label").GetString() == "natural:revival" && row.GetProperty("AutomaticQueueDeferrals").GetInt32() == 0 &&
                prior.Health == 0 && prior.Status("undying")?.Stacks > 0 && after.Health == 1 &&
                (after.Status("undying")?.Stacks ?? 0) == prior.Status("undying")!.Stacks - 1 &&
                prior.Triggers.Any(trigger => Matches(trigger, definitions["LifeInsurer"])) &&
                queue.TakeLast(2).SequenceEqual([(id, "OnReanimated", 0), (id, "OnDeath", 1)]),
                "Native lifecycle revival lacks its live owner, consumed stack or Reanimated/death FIFO.");
        }
        Require(witnesses.Values.All(count => count > 0), "Some lifecycle originals lack positive future effects: " +
            string.Join(", ", witnesses.Select(item => item.Key + "=" + item.Value)));
        if (replayBattle)
        {
            RevivalChecks.Native(root);
            BattleTurnChecks.Native(root);
            BattleActionChecks.Native(root);
        }
        Console.WriteLine("ORIGINAL-LIFECYCLE-RELIC-WITNESSES " + string.Join(", ", witnesses.Select(item => item.Key + "=" + item.Value)));
        Console.WriteLine($"NATIVE-ORIGINAL-LIFECYCLE-RELICS PASS: five unchanged originals, {births} exact birth/train/queue phases, {enemyBirths} enemy gates/{shroomBirths} Shroom births/{rejectedSubtypes} other subtypes, {actions.Length} plays/{activations} abilities, {stages.Length} complete combat stages/{revivals.Length} natural revivals; five positive owner ablations preserve existing upgrades, raw stats and other caches, with 32 immutable birth branches.");
        return witnesses;
    }

    private static void CheckDefinitions(IReadOnlyList<CombatRelicState> relics, Dictionary<string, CombatTrigger> definitions)
    {
        foreach (var item in Artifacts)
        {
            CombatRelicState[] owners = relics.Where(relic => relic.DataId == item.Id).ToArray();
            Require(owners.Length == 1, "Original lifecycle relic missing or duplicated: " + item.Key);
            CombatRelicState relic = owners[0];
            Require(relic.AssetKey == item.Key && relic.EffectTypes.SequenceEqual([RelicModel.AddTrigger]) &&
                relic.IsCovenant == false && relic.DisallowedInPlacementPhase == false && relic.TriggerAdditions?.Count == 1 &&
                relic.SpawnStatuses == null && relic.CardModifiers == null && relic.StatusStackAdditions == null && relic.StatusMagnitudes == null,
                "Original lifecycle relic identity/effects changed: " + item.Key);
            RelicCharacterTriggers rule = relic.TriggerAdditions![0];
            Require(rule.EffectIndex == 0 && rule.SourcePlayer && !rule.SourceEnemy && !rule.SubtypeIsPyre &&
                rule.SubtypeIsNone == (item.Key != "ShroomsGainRegen") &&
                (item.Key != "ShroomsGainRegen" || rule.RequiredSubtype == Shroom) && rule.NoCountModifiersAllowed &&
                rule.CharacterAssetKeys.Count == 0 && rule.Conditions.Count == 0 && rule.ExternalInteractions.Count == 0 && rule.Triggers.Count == 1,
                "Original lifecycle installation filters/count flags changed: " + item.Key);
            CombatTrigger trigger = rule.Triggers[0];
            Require(trigger.Kind == item.Kind && !trigger.Once && trigger.IgnoreSilence && trigger.TriggerAtThreshold == 0 &&
                trigger.Conditions == null && !trigger.RemoveOnRelentlessChange && trigger.Origin?.OnlyIfEquipped == false && trigger.Effects.Count == 1,
                "Original lifecycle authored trigger changed: " + item.Key);
            CombatEffect effect = trigger.Effects[0];
            bool upgrade = item.Key == "AvariceOnSummon";
            CardActionEffect action = (upgrade ? effect.UnitUpgrade : effect.Action) ??
                throw new InvalidDataException("Missing native lifecycle action descriptor: " + item.Key);
            Require(effect.Type == (upgrade ? "CardEffectAddTempCardUpgradeToUnits" : "CardEffectAddStatusEffect") &&
                action.Type == (upgrade ? "UnitUpgrade" : "AddStatus") && action.Target == item.Target && action.AllowPlayer &&
                action.AllowEnemy == (item.Key == "LifeInsurer") && action.Value == (item.Key == "DamageOnGoingDownFloor" ? 100 : 0) &&
                !action.ScaleUpgrade && action.Range == null && action.Filters is { Health: "Both", IgnoreBosses: false } &&
                action.Filters.Subtype == (item.Key == "ShroomsGainRegen" ? Shroom : "") &&
                action.Filters.RequiredStatuses.Count == 0 && action.Filters.ExcludedStatuses.Count == 0 && action.Filters.ExcludedSubtypes.Count == 0,
                "Original lifecycle target/payload/filters changed: " + item.Key);
            if (upgrade)
            {
                CardUpgradeModifier? payload = action!.Upgrade;
                Require(action.Statuses.Count == 0 && action.Lifetime == "TemporaryUntilEndOfBattle" &&
                    payload is { AssetKey: "Plus5Attack", Stats.Damage: 5, Unique: false } &&
                    payload.DataId == "069b201b-ee72-4daf-99d8-914d56f1bad5" &&
                    payload.Stats.Health == 0 && payload.Stats.Cost == 0 && payload.Stats.Heal == 0 && payload.Stats.Size == 0 &&
                    payload.Stats.XCost == 0 && payload.Stats.EquipmentLimit == 0 && payload.Stats.UpgradeSlotCount == 0 &&
                    payload.Statuses.Count == 0 && payload.DamageBuff == 0 && payload.UnhealedHealth == 0 &&
                    payload.ExternalInteractions.Count == 0 && payload.AbilityUpgrade == null && (payload.TriggerUpgrades?.Count ?? 0) == 0,
                    "Original Avarice upgrade/lifetime metadata is missing or changed.");
            }
            else Require(action!.Upgrade == null && action.Statuses.Count == 1 && action.Statuses[0].Id == item.Status &&
                action.Statuses[0].Stacks == item.Amount && action.Tests?.StrictTargets == false,
                "Original lifecycle status payload changed: " + item.Key);
            if (definitions.TryGetValue(item.Key, out CombatTrigger? old)) Equal(old, trigger, "stable full original lifecycle trigger descriptor");
            else definitions.Add(item.Key, trigger);
        }
    }

    private static int Positive(string key, int amount, IEnumerable<CombatUnit> priorUnits, IEnumerable<CombatUnit> actualUnits,
        IEnumerable<CombatUnit> withoutUnits, IReadOnlyCollection<int> revivedIds)
    {
        var prior = priorUnits.ToDictionary(unit => unit.Id);
        var without = withoutUnits.ToDictionary(unit => unit.Id);
        return actualUnits.Count(unit => unit.Team == CombatTeam.Player && prior.ContainsKey(unit.Id) &&
            without.TryGetValue(unit.Id, out CombatUnit? ablated) && (key switch
            {
                "AvariceOnSummon" => unit.AttackBeforeStatuses >= ablated.AttackBeforeStatuses + amount &&
                    unit.Modifiers?.AttackDamageAdded >= ablated.Modifiers?.AttackDamageAdded + amount,
                "ShroomsGainRegen" => unit.Subtypes.Contains(Shroom) && (unit.Status("regen")?.Stacks ?? 0) >= (ablated.Status("regen")?.Stacks ?? 0) + amount,
                "LifeInsurer" => revivedIds.Contains(unit.Id) && (unit.Status("armor")?.Stacks ?? 0) >= (ablated.Status("armor")?.Stacks ?? 0) + amount,
                _ => (unit.Status("armor")?.Stacks ?? 0) >= (ablated.Status("armor")?.Stacks ?? 0) + amount
            }));
    }
    private static bool Matches(CombatTrigger trigger, CombatTrigger definition) => trigger.Kind == definition.Kind &&
        trigger.Origin?.UpgradeId == "" && trigger.Origin.EquipmentCardId == 0 && !trigger.Origin.IsFromEquipment &&
        Serialize(trigger.Effects) == Serialize(definition.Effects);
    private static void CheckPreserved(IEnumerable<CombatUnit> before, IEnumerable<CombatUnit> without, CombatTrigger definition) =>
        Equal(before.Select(unit => unit.WithTriggers(unit.Triggers.Where(trigger => !Matches(trigger, definition)).ToArray())).ToArray(),
            without.ToArray(), "owner ablation preserves raw status damage, existing upgrades, abilities and other callbacks");
    private static void CheckCaches(CombatContext before, CombatContext without)
    {
        Equal(before.TriggerCounts, without.TriggerCounts, "other relic trigger count cache");
        Equal(before.AbilityCardCache, without.AbilityCardCache, "existing shared ability cards");
        Equal(before.CardInstances, without.CardInstances, "existing source-card upgrades");
        Equal(before.CardRegistry, without.CardRegistry, "existing source-card registry");
        Require(before.BattleRng.Equals(without.BattleRng), "Owner ablation changed the starting RNG.");
    }
    private static CombatUnit Remove(CombatUnit unit, CombatTrigger definition) =>
        unit.WithTriggers(unit.Triggers.Where(trigger => !Matches(trigger, definition)).ToArray());
    private static CombatContext Remove(CombatContext context, string id, CombatTrigger definition)
    {
        CombatContext changed = context.WithRelics(context.Relics!.Where(relic => relic.DataId != id).ToArray());
        EnchantmentWorld? world = context.Enchantments;
        if (world == null) return changed;
        return changed.WithEnchantments(new EnchantmentWorld(world.Rooms.Select(room => new RoomCombatState(room.RoomIndex,
            room.Deployment, room.Units.Select(unit => Remove(unit, definition)).ToArray(), room.ExternalInteractions, null, room.Preview)).ToArray(),
            world.Movement, world.EnemySlotsPerRoom, world.RetainedUnits.Select(actor =>
                new EnchantmentRetainedUnit(Remove(actor.Unit, definition), actor.RoomIndex, actor.Preview)).ToArray(),
            world.EnchanterIds, world.AllowUpdates, world.Updating, world.Preview, world.TestRng, world.AutomaticLifecycle));
    }
    private static RoomCombatState Remove(RoomCombatState source, string id, CombatTrigger definition) => new(source.RoomIndex,
        source.Deployment, source.Units.Select(unit => Remove(unit, definition)).ToArray(), source.ExternalInteractions,
        Remove(source.Context!, id, definition), source.Preview);
    private static BattleTurnState Remove(BattleTurnState source, string id, CombatTrigger definition)
    {
        EnemySpawnState old = source.Spawn;
        CombatContext context = Remove(old.Train.Context!, id, definition);
        var train = new TrainCombatState(old.Train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
            room.Units.Select(unit => Remove(unit, definition)).ToArray(), room.ExternalInteractions, context, room.Preview)).ToArray(),
            old.Train.Movement, old.Train.EnemySlotsPerRoom, context);
        var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping, old.Rng, old.NextUnitId,
            old.Treasures, old.TreasuresRemaining, old.TreasureEnabled, old.FirstTreasureTurn, old.FirstTreasureRoom, old.Turn,
            old.ExternalInteractions, old.CanonicalDecisionReferences, old.PendingDestroyedUnitIds);
        return new(spawn, source.Energy, source.EnergyPerTurn, source.DrawPerTurn, source.ForgePoints, source.DragonsHoard,
            source.MoonPhase, source.RngStreams, source.OtherPiles, source.ExternalInteractions, source.PlayRules,
            source.BattlePreviewEnabled, source.UiRngIsolated, source.CanonicalDecisionReferences, source.CanonicalPhysicalReferences, source.SelectedRoom);
    }
    private static CombatUnit[] Units(BattleTurnState source) => source.Spawn.Train.Rooms.SelectMany(room => room.Units).ToArray();

    // Match the native scene's policy using only captured authored rules and live state.
    internal static PlayCardAction? Choose(BattleTurnState source)
    {
        if (source.PlayRules == null || source.Spawn.Train.Context == null) return null;
        IReadOnlyList<PlayCardAction> plays = BattleActionModel.EnumerateSupportedPlays(source);
        CombatContext context = source.Spawn.Train.Context;
        for (int index = 0; index < 2; index++)
        foreach (CardToken card in context.Cards.Hand)
        {
            CombatUnit? template = source.PlayRules.Cards.FirstOrDefault(rule => rule.DataId == card.DataId)?.SpawnUnit;
            bool match = template != null && (index == 0 ? template.Subtypes.Contains(Shroom) :
                !template.Subtypes.Contains(Shroom) && HasStatus(template, "undying"));
            if (!match) continue;
            int position = index == 0 ? 0 : source.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Count(unit => unit.Team == CombatTeam.Player);
            PlayCardAction? summon = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId &&
                action.RoomIndex == 0 && action.PlayerPosition == position);
            if (summon != null) return summon;
        }
        CombatUnit[] players = source.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Where(unit => unit.Team == CombatTeam.Player && unit.Health > 0).ToArray();
        CombatUnit? woundTarget = players.FirstOrDefault(unit => unit.Subtypes.Contains(Shroom) &&
            unit.Health == unit.MaxHealth && !HasStatus(unit, "armor"));
        CombatUnit? reviveTarget = players.FirstOrDefault(unit => !unit.Subtypes.Contains(Shroom) &&
            unit.Health <= 12 && HasStatus(unit, "undying") && !HasStatus(unit, "armor"));
        foreach (CombatUnit? target in new[] { woundTarget, reviveTarget })
        {
            if (target == null) continue;
            PlayCardAction? damage = plays.FirstOrDefault(action => action.TargetUnitId == target.Id && IsWound(source, action.CardInstanceId));
            if (damage != null) return damage;
        }
        PlayCardAction? activation = plays.FirstOrDefault(action => action.ActivatorUnitId != 0);
        if (activation != null) return activation;
        return plays.FirstOrDefault(action => !IsWound(source, action.CardInstanceId));
    }
    private static bool IsWound(BattleTurnState source, int instanceId)
    {
        CardInstanceState? card = source.Spawn.Train.Context!.FindCard(instanceId);
        CardPlayRule? rule = source.PlayRules!.Cards.FirstOrDefault(item => item.DataId == card?.DataId);
        return rule?.Effects.Count == 1 && rule.Effects[0].Type == "Damage" && rule.Effects[0].Value == 12 &&
            rule.Effects[0].Target == "DropTargetCharacter" && rule.Effects[0].AllowPlayer && !rule.Effects[0].AllowEnemy;
    }
    private static bool HasStatus(CombatUnit unit, string id) => unit.Statuses.Any(status => status.Id == id && status.Stacks > 0);
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal(string expected, string actual, string label)
    { string? difference = ModelJson.Difference(expected, actual); Require(difference == null, label + ": " + difference); }
    private static void Equal<T>(T expected, T actual, string label)
    { string? difference = ModelJson.Difference(Serialize(expected), Serialize(actual)); Require(difference == null, label + ": " + difference); }
    private static void Require(bool value, string message) { if (!value) throw new InvalidDataException(message); }
}
