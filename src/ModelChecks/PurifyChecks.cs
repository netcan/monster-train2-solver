using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PurifyChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(149);
        var context = new CombatContext(new([new(3, "spell")], [], [], rng, 0, []), rng, 0, 5, 10,
            statistics: BattleStatistics.Empty().TrackCards([3, 4]), purifyBlockedTriggers: []);
        CombatStatus Status(string id, int count, bool hidden = false, string category = "Positive") =>
            new(id, count, 1, stackable: id != "purify" && id != "silenced" && id != "unit_ability",
                hidden: hidden, displayCategory: category);
        CombatStatus purify = Status("purify", 1);
        CombatStatus[] registry = [Status("armor", 3), Status("silenced", 1, category: "Negative"),
            Status("regen", 0), Status("cooldown", 2, true, "Persistent"), Status("unit_ability", 1, true, "Persistent")];
        CombatUnit Unit(CombatStatus[] statuses, string[]? immunities = null) => new(1, "purify", CombatTeam.Player,
            1, 20, 20, true, false, false, statuses, statusImmunities: immunities, isBoss: false, lastAttackerId: 0,
            statusRegistry: statuses, statusDictionary: new(statuses.Select(status => (string?)status.Id).ToArray(), []),
            ability: new("ability", 3, 2));
        RoomCombatState Room(CombatUnit unit, CombatContext? scope = null) => new(0, false, [unit], [], scope ?? context);
        var root = Room(Unit(registry)); string parent = JsonSerializer.Serialize(root);
        var applied = StatusApplicationModel.Apply(root, 1, purify, 3);
        Require(applied.Supported, applied.UnsupportedReason ?? "Purify application was rejected.");
        var actor = applied.State!.Units.Single();
        Require(actor.Statuses.Count == 1 && actor.Status("purify")?.Stacks == 1 &&
            actor.StatusRegistry!.Where(status => status.Id != "purify").All(status => status.Stacks == 0) &&
            actor.StatusDictionary!.Slots.SequenceEqual(registry.Select(status => (string?)status.Id).Append("purify")) &&
            actor.StatusDictionary.FreeSlots.Count == 0,
            "Purify lost zero definitions, dictionary order or its own status.");
        Require(applied.PendingCallbacks.Select(item => item.Kind).SequenceEqual(new[] {
            "OnStatusEffectChanged", "OnNewStatusEffectAdded", "OnStatusEffectChanged", "OnStatusEffectChanged", "OnSilenceLost",
            "OnStatusEffectChanged", "OnUnitAbilityAvailable", "OnStatusEffectChanged" }) &&
            applied.PendingCallbacks[1].ParamInt == 4 &&
            applied.PendingCallbacks.Where(item => item.Kind == "OnStatusEffectChanged").Select(item => item.ParamString)
                .SequenceEqual(new[] { "purify", "armor", "silenced", "cooldown", "unit_ability" }) &&
            applied.PendingCallbacks.Where(item => item.Kind == "OnStatusEffectChanged").Select(item => item.ParamInt2)
                .SequenceEqual(new[] { 1, -3, -1, -2, -1 }),
            "Purify callbacks differ: " + string.Join("; ", applied.PendingCallbacks.Select(item =>
                $"{item.Kind}/{item.ParamString}/{item.ParamInt}/{item.ParamInt2}")));
        Require(applied.State.Context!.Statistics!.Value(3, "AnyStatusEffectStacksAdded") == 1 &&
            applied.State.Context.Statistics.Value(3, "AnyStatusEffectStacksRemoved") == 0,
            "Implicit Purify clears attributed removed stacks to the played card.");
        var blockingScope = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10, cardInstances: [], cardRegistry: []);
        var blockingRoot = Room(actor, blockingScope);
        foreach (int amount in new[] { 3, 0, -1, int.MinValue })
            foreach (string id in new[] { "armor", "purify" })
            {
                var blocked = StatusApplicationModel.Apply(blockingRoot, 1, Status(id, amount), 123, overrideImmunity: true);
                Require(blocked.Supported && blocked.PendingCallbacks.Count == 0 &&
                    JsonSerializer.Serialize(blocked.State) == JsonSerializer.Serialize(blockingRoot),
                    "Existing Purify did not block signed/zero/repeated additions before source lookup.");
            }
        foreach (int amount in new[] { 0, -1 })
        {
            var zero = StatusApplicationModel.Apply(root, 1, purify.WithStacks(amount), 3);
            Require(zero.Supported && zero.State!.Units[0].Statuses.Count == 0 &&
                zero.State.Units[0].RegisteredStatus("purify")?.Stacks == 0 &&
                zero.State.Units[0].StatusRegistry!.All(status => status.Stacks == 0),
                "A zero/negative new Purify application failed to clear the other definitions.");
        }
        var immune = Room(Unit(registry, ["purify"]));
        var refused = StatusApplicationModel.Apply(immune, 1, purify, 3);
        Require(refused.Supported && refused.PendingCallbacks.Count == 0 &&
            JsonSerializer.Serialize(refused.State) == JsonSerializer.Serialize(immune), "Purify immunity still cleared existing statuses.");
        Require(StatusApplicationModel.Apply(immune, 1, purify, 3, overrideImmunity: true).State!.Units[0].Statuses.Single().Id == "purify",
            "An immunity override failed to permit initial Purify.");
        var removed = StatusRemovalModel.Remove(applied.State, 1, "purify", -1, 3);
        var restored = StatusApplicationModel.Apply(removed.State!, 1, Status("armor", 2), 3);
        Require(restored.Supported && restored.State!.Units[0].Status("armor")?.Stacks == 2,
            "Explicit Purify removal did not re-enable status application.");

        var horde = new CombatUnit(1, "horde", CombatTeam.Player, 16, 50, 50, true, false, false,
            [Status("horde", 2), Status("armor", 3), Status("silenced", 1, category: "Negative")], spawnerCardId: 4,
            modifiers: new(16, 0, 0, 1, 0, true, false, []), hordeDefinition: new(8, 25),
            statusRegistry: [Status("horde", 2), Status("armor", 3), Status("silenced", 1, category: "Negative")]);
        var hordeRoot = Room(horde);
        var restrictedContext = new CombatContext(context.Cards, rng, 0, 5, 10,
            statistics: BattleStatistics.Empty().TrackCards([3]),
            purifyBlockedTriggers: ["OnStatusEffectChanged", "OnNewStatusEffectAdded", "OnArmorAdded", "OnUnitAbilityAvailable"]);
        var restricted = StatusApplicationModel.Apply(Room(Unit(registry), restrictedContext), 1, purify, 3);
        Require(restricted.Supported && restricted.PendingCallbacks.Select(callback => callback.Kind).SequenceEqual(["OnSilenceLost"]),
            "Purify API queued blocked addition/removal notifications or lost an allowed silence-loss callback.");
        var unrestrictedRemoval = StatusRemovalModel.Remove(restricted.State!, 1, "purify", -1, 3);
        Require(unrestrictedRemoval.Supported && unrestrictedRemoval.PendingCallbacks.Single().Kind == "OnStatusEffectChanged" &&
            unrestrictedRemoval.PendingCallbacks[0].ParamInt2 == -1,
            "Removing Purify incorrectly retained its former callback restrictions.");
        var clearedHorde = StatusApplicationModel.Apply(hordeRoot, 1, purify, 3);
        Require(clearedHorde.Supported, clearedHorde.UnsupportedReason ?? "Purify Horde clear was rejected.");
        var corpse = clearedHorde.RetainedUnits.Single(unit => unit.Id == 1);
        Require(clearedHorde.State!.Units.Count == 0 && corpse.Health == 0 && corpse.Statuses.Single().Id == "purify" &&
            corpse.RegisteredStatus("armor")?.Stacks == 0 && corpse.RegisteredStatus("silenced")?.Stacks == 0 &&
            clearedHorde.State.Context!.Statistics!.MonstersDeadThisBattle == 2 &&
            clearedHorde.State.Context.Statistics.Value(4, "TimesExhausted") == 0 &&
            clearedHorde.State.Context.Statistics.Value(3, "AnyStatusEffectStacksRemoved") == 0,
            "Purify lost the retained zero-HP Horde, later clears, raw casualty counts or non-sacrifice semantics.");

        foreach (CombatStatus[] starting in new[] { new[] { Status("armor", 3), purify, Status("regen", 2) },
            new[] { Status("horde", 2), purify, Status("armor", 3) } })
        {
            CombatUnit template = starting[0].Id == "horde" ? horde : Unit(starting);
            CombatUnit? initialized = null; var queue = new List<RoomCombatModel.QueuedCharacterTrigger>();
            string? error = StatusCallbackModel.Initialize(Room(template), template, starting, queue, value => initialized = value);
            Require(error == null && initialized?.Statuses.Single().Id == "purify" &&
                initialized.RegisteredStatus(starting[0].Id)?.Stacks == 0 && initialized.Status("armor") == null,
                "Starting Purify did not preserve its final initialized actor: " + error);
        }
        var aura = new EnchantmentRule(new("Enchant", "Room", 0, false, true, []), [Status("armor", 2), Status("buff", 1)], bound: true);
        var source = new CombatUnit(10, "aura", CombatTeam.Enemy, 0, 1, 1, false, false, false, [],
            [new("OnSpawn", false, false, false, 1, [new("CardEffectEnchant", 0, 0, "", 0, [], false, enchantment: aura)], false)]);
        var frame = new EnchantmentCombatState(new([new(0, false, [source, actor], [], context)], [], 7, context), [], [10], true, false, false, rng);
        var enchanted = EnchantmentCombatModel.UpdateAll(frame);
        Require(enchanted.Supported && enchanted.State!.Train.Rooms[0].Units.Single(unit => unit.Id == 1).Status("armor") == null &&
            enchanted.State.Train.Context!.BattleRng.Equals(rng.Range(0, 2).State) && enchanted.PendingCallbacks.Count == 0 &&
            enchanted.State.Train.Rooms[0].Units.Single(unit => unit.Id == 10).Triggers[0].Effects[0].Enchantment!.State.PrimaryTargets
                .Single(target => target.UnitId == 1).IsEnchanted,
            "A blocked aura addition lost its cache membership/RNG draw or added a status/callback: " + enchanted.UnsupportedReason);
        string expected = JsonSerializer.Serialize(applied.State);
        string expectedHorde = JsonSerializer.Serialize(clearedHorde.RetainedUnits);
        Parallel.For(0, 32, _ =>
        {
            Require(JsonSerializer.Serialize(StatusApplicationModel.Apply(root, 1, purify, 3).State) == expected &&
                JsonSerializer.Serialize(StatusApplicationModel.Apply(hordeRoot, 1, purify, 3).RetainedUnits) == expectedHorde,
                "Parallel Purify applications diverged.");
        });
        Require(JsonSerializer.Serialize(root) == parent, "Purify mutated its parent.");
        var gatedContext = new CombatContext(context.Cards, rng, 0, 5, 10,
            purifyBlockedTriggers: ["PreCombat", "OnAttacking", "OnUnscaledSpawn"]);
        var gatedUnit = new CombatUnit(1, "gated", CombatTeam.Player, 1, 10, 10, true, false, false, [purify],
            [new("PreCombat", true, false, true, 1, [new("CardEffectRewardGold", 10, 0, "", 0, [], false)], false)]);
        var gatedRoot = new RoomCombatState(0, false, [gatedUnit], [], gatedContext);
        string gatedParent = JsonSerializer.Serialize(gatedRoot, ModelJson.Options);
        TrainCombatState Train(CombatContext value, params CombatUnit[] actors) => new([
            new(0, false, actors, [], value), new(1, false, [], [], value), new(2, false, [], [], value),
            new(3, false, [new(99, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, [])], [], value)], [], 7, value);
        var gatedTrain = Train(gatedContext, gatedUnit);
        var prevented = TrainCombatModel.PreCombat(gatedTrain, CombatTeam.Player);
        Require(prevented.Supported && prevented.State!.Context!.Gold == 0 &&
            !prevented.State.Rooms[0].Units[0].Triggers[0].HasTriggered &&
            prevented.State.Context.PurifyBlockedTriggers!.SequenceEqual(gatedContext.PurifyBlockedTriggers!),
            "Purify failed to prevent trigger enqueueing or changed the trigger once flag: " + JsonSerializer.Serialize(prevented, ModelJson.Options));
        var accepted = RoomCombatModel.ApplyQueuedCharacterTrigger(gatedRoot,
            new RoomCombatModel.QueuedCharacterTrigger(0, gatedUnit, "PreCombat"), _ => { });
        Require(accepted.Supported && accepted.State!.Context!.Gold == 10 && accepted.State.Units[0].Triggers[0].HasTriggered &&
            accepted.State.Context.PurifyBlockedTriggers!.SequenceEqual(gatedContext.PurifyBlockedTriggers!),
            "Purify cancelled an already accepted trigger or discarded queue restrictions during gold mutation.");
        var missingContext = new CombatContext(context.Cards, rng, 0, 5, 10);
        var missing = TrainCombatModel.PreCombat(Train(missingContext, gatedUnit), CombatTeam.Player);
        Require(!missing.Supported && missing.UnsupportedReason!.Contains("Purify requires"), "Missing Purify queue metadata was guessed.");
        var purifier = new CombatUnit(1, "purifier", CombatTeam.Player, 0, 10, 10, true, false, false, [],
            [new("PreCombat", true, false, true, 1,
                [new("CardEffectAddStatusEffect", 0, 0, "", 0, [], false,
                    action: new("AddStatus", "Room", 0, false, true, [purify]))], false)]);
        var follower = new CombatUnit(2, "queued", CombatTeam.Player, 0, 10, 10, true, false, false, [], gatedUnit.Triggers);
        var acceptedTeam = TrainCombatModel.PreCombat(Train(gatedContext, purifier, follower), CombatTeam.Player);
        Require(acceptedTeam.Supported && acceptedTeam.State!.Context!.Gold == 10 &&
            acceptedTeam.State.Rooms[0].Units.All(unit => unit.Status("purify")?.Stacks > 0 && unit.Triggers[0].HasTriggered),
            "Later purification cancelled a team callback queued before its first actor fired: " + acceptedTeam.UnsupportedReason);
        var startingContext = new CombatContext(context.Cards, rng, 0, 5, 10,
            purifyBlockedTriggers: ["OnSpawn", "OnUnscaledSpawn", "OnStatusEffectChanged", "OnArmorAdded", "OnSilence", "OnNewStatusEffectAdded"]);
        CombatTrigger GoldTrigger(string kind) => new(kind, true, false, true, 1,
            [new("CardEffectRewardGold", 1, 0, "", 0, [], false)], false);
        var startingActor = new CombatUnit(1, "starting", CombatTeam.Player, 0, 10, 10, true, false, false, [],
            [GoldTrigger("OnStatusEffectChanged"), GoldTrigger("OnSilenceLost"), GoldTrigger("OnSpawn")],
            spawnerCardId: 3, statusRegistry: [], statusDictionary: new([], []));
        var startingSpawn = RoomCombatModel.ApplySpawnTriggers(new(0, false, [startingActor], [], startingContext), 1, true,
            [Status("armor", 3), Status("silenced", 1, category: "Negative"), purify, Status("buff", 2)]);
        Require(startingSpawn.Supported && startingSpawn.State!.Context!.Gold == 10 &&
            startingSpawn.State.Units[0].Triggers.Select(trigger => trigger.HasTriggered).SequenceEqual([true, true, false]) &&
            startingSpawn.State.Units[0].Status("purify")?.Stacks == 1 && startingSpawn.State.Units[0].RegisteredStatus("buff") == null,
            "Purify discarded starting callbacks when every ordinary spawn trigger was blocked: " + startingSpawn.UnsupportedReason);
        Parallel.For(0, 32, _ =>
        {
            Require(JsonSerializer.Serialize(TrainCombatModel.PreCombat(gatedTrain, CombatTeam.Player).State, ModelJson.Options) ==
                JsonSerializer.Serialize(prevented.State, ModelJson.Options), "Purify queue gates diverged across branches.");
        });
        Require(JsonSerializer.Serialize(gatedRoot, ModelJson.Options) == gatedParent, "Purify queue gate mutated its parent.");
        string[] manualKinds = ["OnSpawn", "OnUnscaledSpawn", "AfterSpawnEnchant", "CardMonsterPlayed",
            "OnSentry", "OnShift", "OnDeath", "OnAnyMonsterDeathOnFloor", "OnAnyUnitDeathOnFloor"];
        var manualContext = new CombatContext(context.Cards, rng, 0, 5, 10, purifyBlockedTriggers: manualKinds);
        var manualActor = new CombatUnit(1, "manual", CombatTeam.Player, 0, 20, 20, false, false, false, [purify],
            manualKinds.Append("OnSilenceLost").Select(GoldTrigger).ToArray());
        var manualRoot = Train(manualContext, manualActor);
        string manualParent = JsonSerializer.Serialize(manualRoot, ModelJson.Options);
        foreach (string kind in manualKinds)
        {
            var requests = new List<RoomCombatModel.QueuedCharacterTrigger> { new(0, manualActor, kind) };
            var blocked = TrainCombatModel.ApplyCharacterQueue(manualRoot, requests);
            Require(blocked.Supported && blocked.State!.Context!.Gold == 0 &&
                requests.Count == 0 && blocked.State.Rooms[0].Units[0].Triggers.All(trigger => !trigger.HasTriggered),
                "A fresh manually assembled callback bypassed Purify: " + kind + "/" + blocked.UnsupportedReason);
        }
        var allowed = TrainCombatModel.ApplyCharacterQueue(manualRoot, [new(0, manualActor, "OnSilenceLost")]);
        Require(allowed.Supported && allowed.State!.Context!.Gold == 5, "Purify blocked an allowed manually queued callback: " + allowed.UnsupportedReason);
        var rally = CardPlayedTriggerModel.Rally(manualRoot, CombatTeam.Player, [1]);
        Require(rally.Supported && rally.State!.Context!.Gold == 0, "Purify failed to gate Rally's cached actor queue: " + rally.UnsupportedReason);
        var entrant = new CombatUnit(9, "entrant", CombatTeam.Enemy, 0, 20, 20, false, false, false, []);
        var sentry = TrainCombatModel.Sentry(Train(manualContext, manualActor, entrant), entrant.Id);
        Require(sentry.Supported && sentry.State!.Context!.Gold == 0, "Purify failed to gate Sentry's opposing actor queue.");
        var missingManual = TrainCombatModel.ApplyCharacterQueue(Train(missingContext, manualActor), [new(0, manualActor, "OnShift")]);
        Require(!missingManual.Supported && missingManual.UnsupportedReason!.Contains("Purify requires"),
            "Missing queue restrictions yielded a guessed manual callback.");
        var manualPurifier = new CombatUnit(1, "purifier", CombatTeam.Player, 0, 20, 20, false, false, false, [],
            [new("AfterSpawnEnchant", true, false, true, 1, purifier.Triggers[0].Effects, false)]);
        var manualFollower = new CombatUnit(2, "follower", CombatTeam.Player, 0, 20, 20, false, false, false, [],
            [GoldTrigger("AfterSpawnEnchant")]);
        var manualAccepted = TrainCombatModel.ApplyCharacterQueue(Train(manualContext, manualPurifier, manualFollower),
            [new(0, manualPurifier, "AfterSpawnEnchant"), new(0, manualFollower, "AfterSpawnEnchant")]);
        Require(manualAccepted.Supported && manualAccepted.State!.Context!.Gold == 5 &&
            manualAccepted.State.Rooms[0].Units.All(unit => unit.Status("purify")?.Stacks > 0 && unit.Triggers[0].HasTriggered),
            "A batch callback accepted before purification was cancelled: " + manualAccepted.UnsupportedReason);
        var deathContext = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 5, 10,
            statistics: BattleStatistics.Empty(), otherPiles: [new("Standby", [new(4, "unit")]), new("Exhausted", [])],
            purifyBlockedTriggers: manualKinds);
        var victim = new CombatUnit(1, "victim", CombatTeam.Player, 0, 1, 1, false, false, false, [purify],
            [GoldTrigger("OnDeath")], spawnerCardId: 4, deathState: new(false, false, true, isSacrifice: false, statisticsListenerOnce: true));
        var blockedHarvester = new CombatUnit(2, "blocked-harvester", CombatTeam.Player, 0, 20, 20, false, false, false, [purify],
            [GoldTrigger("OnAnyUnitDeathOnFloor")]);
        var liveHarvester = new CombatUnit(3, "harvester", CombatTeam.Player, 0, 20, 20, false, false, false, [],
            [GoldTrigger("OnAnyUnitDeathOnFloor")]);
        var deathRoot = Train(deathContext, victim, blockedHarvester, liveHarvester);
        string deathParent = JsonSerializer.Serialize(deathRoot, ModelJson.Options);
        var death = CardSpellModel.Apply(deathRoot, 0, [new("Damage", "Room", 1, false, true, [])], 0);
        Require(death.Supported && death.State!.Context!.Gold == 5 &&
            death.State.Rooms[0].Units.Select(unit => unit.Id).SequenceEqual([2, 3]) &&
            death.State.Context.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Count == 0 &&
            death.State.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Single().InstanceId == 4 &&
            death.State.Context.Statistics!.Value(4, "TimesExhausted") == 1 &&
            !death.State.Rooms[0].Units.Single(unit => unit.Id == 2).Triggers[0].HasTriggered &&
            death.State.Rooms[0].Units.Single(unit => unit.Id == 3).Triggers[0].HasTriggered,
            "Purify lost death removal/spawner settlement, admitted OnDeath or blocked a living Harvest recipient: " + death.UnsupportedReason);
        string expectedDeath = JsonSerializer.Serialize(death.State, ModelJson.Options);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(CardSpellModel.Apply(deathRoot, 0,
            [new("Damage", "Room", 1, false, true, [])], 0).State, ModelJson.Options) == expectedDeath,
            "Purified physical death diverged across parallel branches."));
        Require(JsonSerializer.Serialize(manualRoot, ModelJson.Options) == manualParent &&
            JsonSerializer.Serialize(deathRoot, ModelJson.Options) == deathParent, "Manual Purify queues/death settlement mutated a parent.");
        foreach (bool startsPurified in new[] { false, true })
        {
            CombatEffect toggle = new(startsPurified ? "CardEffectRemoveStatusEffect" : "CardEffectAddStatusEffect", 0, 0, "", 0, [], false,
                action: new(startsPurified ? "RemoveStatus" : "AddStatus", "Self", 0, false, true, [purify]));
            var dyingActor = new CombatUnit(10, "dying", CombatTeam.Player, 0, 0, 1, false, false, false,
                startsPurified ? [purify] : [], [new("OnHit", true, false, true, 1, [toggle], false), GoldTrigger("OnDeath")],
                statusRegistry: startsPurified ? [purify] : [], statusDictionary: new(startsPurified ? ["purify"] : [], []),
                deathState: new(true, false, false, isSacrifice: false, statisticsListenerOnce: false));
            var pendingDeath = new RoomCombatModel.QueuedCharacterTrigger(0, dyingActor, deferUntilRemoval: true, removalLifecycle: true);
            var acceptedHit = new RoomCombatModel.QueuedCharacterTrigger(0, dyingActor, "OnHit",
                admission: RoomCombatModel.CharacterTriggerAdmission.Accepted);
            var settled = TrainCombatModel.ApplyCharacterQueue(Train(manualContext), [acceptedHit, pendingDeath]);
            Require(settled.Supported && settled.State!.Context!.Gold == (startsPurified ? 5 : 0),
                "Deferred OnDeath admission used the lethal-damage status instead of the later removal status: " + settled.UnsupportedReason);
            Require(pendingDeath.Unit == dyingActor && acceptedHit.Unit == dyingActor && pendingDeath.Admission == RoomCombatModel.CharacterTriggerAdmission.Pending,
                "Draining a branch mutated its input death/accepted callback objects.");
        }
        Console.WriteLine("PURIFY-QUEUE-CHECKS PASS: fresh birth/Rally/Sentry/shift/Harvest gates, accepted batch lifetime, missing metadata, physical death/spawner settlement and 32 branches.");
        Console.WriteLine("PURIFY-CHECKS PASS: signed/zero additions, early block, immunity overrides, ordered callbacks/zero definitions, unattributed clears, removal/reapplication, retained Horde casualties, starting statuses and 32 immutable branches.");
    }
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out var scenario) || scenario.GetString() is not ("purify" or "purify-queues")) return;
        var records = fixture.GetProperty("TriggeredStatuses").EnumerateArray().ToArray();
        Require(records.Length > 0, "Native Purify status boundaries are missing.");
        int positiveBlocked = 0, zeroBlocked = 0, negativeBlocked = 0, zeroClears = 0, clears = 0;
        var kinds = new HashSet<string>();
        foreach (var record in records)
        {
            Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("Interactions").GetArrayLength() == 0 &&
                record.GetProperty("Actual").ValueKind == FixtureKind.Object, "Incomplete native Purify status effect.");
            var before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = record.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            var retained = record.GetProperty("BeforeUnits").Deserialize<CombatUnit[]>()!;
            var actualUnits = record.GetProperty("ActualUnits").Deserialize<CombatUnit[]>()!;
            var effect = record.GetProperty("Effect").Deserialize<CombatEffect>()!;
            int actor = record.GetProperty("ActorId").GetInt32(), source = record.GetProperty("SourceCardId").GetInt32();
            var targets = record.GetProperty("Targets").Deserialize<int[]>()!;
            var scope = new RoomCombatState(before.RoomIndex, before.Deployment,
                before.Units.Concat(retained.Where(unit => before.Units.All(live => live.Id != unit.Id))).ToArray(),
                before.ExternalInteractions, before.Context, before.Preview);
            string parent = JsonSerializer.Serialize(scope, ModelJson.Options);
            var result = TriggeredStatusModel.Apply(scope, actor, effect, targets, source);
            Require(result.Supported, "Native Purify status unsupported: " + result.UnsupportedReason);
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(result.State!.Context, ModelJson.Options),
                JsonSerializer.Serialize(actual.Context, ModelJson.Options));
            foreach (var unit in actualUnits)
            {
                var predicted = result.State.Units.FirstOrDefault(value => value.Id == unit.Id) ??
                    result.RetainedUnits.FirstOrDefault(value => value.Id == unit.Id);
                difference ??= ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options),
                    JsonSerializer.Serialize(unit, ModelJson.Options));
            }
            Require(difference == null, "Independent native Purify differs at sequence " + record.GetProperty("Sequence") + ": " + difference);
            Require(JsonSerializer.Serialize(scope, ModelJson.Options) == parent, "Native Purify mutated its input.");
            kinds.Add(record.GetProperty("Kind").GetString()!);
            foreach (var unit in retained.Where(value => targets.Contains(value.Id)))
            {
                var status = effect.Action!.Statuses.Single();
                if (unit.Status("purify")?.Stacks > 0)
                {
                    positiveBlocked += status.Stacks > 0 ? 1 : 0;
                    zeroBlocked += status.Stacks == 0 ? 1 : 0;
                    negativeBlocked += status.Stacks < 0 ? 1 : 0;
                    var observed = actualUnits.Single(value => value.Id == unit.Id);
                    Require(ModelJson.Difference(JsonSerializer.Serialize(unit, ModelJson.Options),
                        JsonSerializer.Serialize(observed, ModelJson.Options)) == null, "Purified native target accepted a status addition.");
                }
                else if (status.Id == "purify" && unit.Statuses.Any(value => value.Id != "purify" && value.Stacks > 0))
                {
                    var observed = actualUnits.Single(value => value.Id == unit.Id);
                    Require(observed.Statuses.All(value => value.Id == "purify") &&
                        observed.StatusDictionary!.Slots.SequenceEqual(result.State.Units.Single(value => value.Id == unit.Id).StatusDictionary!.Slots),
                        "Native Purify failed to clear statuses while retaining dictionary definitions.");
                    zeroClears += status.Stacks == 0 ? 1 : 0;
                    clears += status.Stacks > 0 ? 1 : 0;
                }
            }
        }
        var removals = fixture.GetProperty("AbilityCooldownEffects").EnumerateArray().Where(record =>
            record.GetProperty("Effect").Deserialize<CardActionEffect>() is { Type: "RemoveStatus" } effect &&
            effect.Statuses.Any(status => status.Id == "purify")).ToArray();
        Require(removals.Any(record => record.GetProperty("TargetActors").Deserialize<CombatUnit[]>()!.Any(unit => unit.Status("purify")?.Stacks > 0)) &&
            removals.All(record => record.GetProperty("Completed").GetBoolean() &&
                record.GetProperty("AfterTargetActors").Deserialize<CombatUnit[]>()!.All(unit => unit.Status("purify") == null)),
            "Native Purify explicit removal did not execute.");
        var actions = fixture.GetProperty("Actions").EnumerateArray().Where(record =>
        {
            var decision = record.GetProperty("Before").Deserialize<BattleTurnState>()!;
            int card = record.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            var instance = decision.Spawn.Train.Context!.CardInstances!.First(value => value.InstanceId == card);
            return decision.PlayRules!.Cards.First(value => value.DataId == instance.DataId).Effects.Any(effect => effect.Statuses.Any(status => status.Id == "purify"));
        }).ToArray();
        Require(positiveBlocked > 0 && zeroBlocked > 0 && negativeBlocked > 0 && zeroClears > 0 && clears > 0 && actions.Length > 0 &&
            kinds.Contains("OnSilenceLost") && fixture.GetProperty("Actions").EnumerateArray().Any(record =>
                record.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Status("purify")?.Stacks > 0 && unit.Triggers.Any(trigger =>
                        trigger.Kind == "PreCombat" && !trigger.HasTriggered))),
            $"Native Purify coverage incomplete: blocked={positiveBlocked}/{zeroBlocked}/{negativeBlocked}, clears={clears}/{zeroClears}, paid={actions.Length}, kinds={string.Join(',', kinds)}.");
        Console.WriteLine($"NATIVE-PURIFY-CHECKS PASS: {records.Length} exact status boundaries, {removals.Length} explicit removals, {actions.Length} paid room spells; positive/zero/negative blocks, zero clears, ordered callbacks and retained definitions.");
        if (scenario.GetString() == "purify-queues") NativeQueues(fixture);
    }
    private static void NativeQueues(FixtureValue fixture)
    {
        var records = fixture.GetProperty("PurifyQueueAdmissions").EnumerateArray().ToArray();
        Require(records.Length > 0, "Native Purify queue requests were not recorded.");
        var rejected = new HashSet<string>();
        foreach (var record in records)
        {
            Verify(record);
            if (record.GetProperty("Purified").GetBoolean() && record.GetProperty("Overload").GetString() == "Character" &&
                record.GetProperty("QueueAfter").GetInt32() == record.GetProperty("QueueBefore").GetInt32())
                rejected.Add(record.GetProperty("Kind").GetString()!);
        }
        Require(new[] { "OnSpawn", "OnUnscaledSpawn", "AfterSpawnEnchant", "CardMonsterPlayed", "OnSentry", "OnDeath", "OnAnyUnitDeathOnFloor" }
            .All(rejected.Contains), "Native Purify queue coverage incomplete: " + string.Join(',', rejected));
        Parallel.For(0, 32, _ => { foreach (var record in records) Verify(record); });
        int dataRequests = records.Count(record => record.GetProperty("Overload").GetString() == "QueueData");
        Console.WriteLine($"NATIVE-PURIFY-QUEUE-CHECKS PASS: {records.Length} original native admission requests ({dataRequests} queue-data overload), " +
            "birth/Rally/Sentry/Harvest/death restrictions, exact queue deltas and 32 branches.");
        static void Verify(FixtureValue record)
        {
            Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("Interactions").GetArrayLength() == 0,
                "Incomplete native Purify queue request.");
            var actor = record.GetProperty("Actor").Deserialize<CombatUnit>()!;
            string parent = JsonSerializer.Serialize(actor, ModelJson.Options);
            bool purified = record.GetProperty("Purified").GetBoolean();
            Require(purified == (actor.Status("purify")?.Stacks > 0), "Captured Purify status disagrees with the native character query.");
            string kind = record.GetProperty("Kind").GetString()!;
            string overload = record.GetProperty("Overload").GetString()!;
            Require(overload is "Character" or "QueueData", "Unknown native queue overload.");
            string[]? purify = record.GetProperty("PurifyBlockedTriggers").Deserialize<string[]>();
            string[]? deployment = record.GetProperty("DeploymentBlockedTriggers").Deserialize<string[]>();
            bool deploymentBlocked = record.GetProperty("Turn").GetInt32() == 0 && deployment?.Contains(kind) == true;
            bool purifyBlocked = overload == "Character" && purified && purify?.Contains(kind) == true;
            int delta = record.GetProperty("QueueAfter").GetInt32() - record.GetProperty("QueueBefore").GetInt32();
            Require(delta == (deploymentBlocked || purifyBlocked ? 0 : 1), "Native admission disagrees with captured balance restrictions: " + kind);
            if (!deploymentBlocked && purify != null)
            {
                var rng = UnityRng.Seed(149);
                var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10, purifyBlockedTriggers: purify);
                var request = new RoomCombatModel.QueuedCharacterTrigger(0, actor, kind,
                    admission: overload == "QueueData" ? RoomCombatModel.CharacterTriggerAdmission.Accepted : RoomCombatModel.CharacterTriggerAdmission.Pending);
                string? error = StatusCallbackModel.Admit(context, request, out var admitted);
                Require(error == null && (admitted.Admission == RoomCombatModel.CharacterTriggerAdmission.Rejected) == (delta == 0),
                    "Independent admission differs from native queue delta: " + kind + "/" + error);
            }
            Require(JsonSerializer.Serialize(actor, ModelJson.Options) == parent, "Purify queue admission mutated its actor.");
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
