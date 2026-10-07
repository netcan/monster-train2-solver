using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class StatusCallbackChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(774);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 10, 1, 10);
        CombatTrigger Gold(string kind, int value, bool once = false, int threshold = 0, bool ignore = true) =>
            new(kind, once, false, ignore, 1, [new("CardEffectRewardGold", value, 0, "", 0, [], false)], false, threshold);
        var registry = new[] { new CombatStatus("regen", 0, 1, hidden: false, displayCategory: "Positive") };
        var unit = new CombatUnit(1, "callbacks", CombatTeam.Player, 0, 20, 20, false, false, false, registry,
            [Gold("OnStatusEffectChanged", 1, threshold: 3), Gold("OnArmorAdded", 2, once: true), Gold("OnNewStatusEffectAdded", 7, threshold: 2)],
            modifiers: new(0, 0, 0, 1, 1, true, false, []), isBoss: false, lastAttackerId: 0, statusRegistry: registry);
        var root = new RoomCombatState(0, false, [unit], [], context);
        string original = JsonSerializer.Serialize(root);
        var zeroArmor = new CombatStatus("armor", 0, 1, hidden: false, displayCategory: "Positive");
        var applied = StatusApplicationModel.Apply(root, 1, zeroArmor);
        Require(applied.Supported && applied.State!.Context!.Gold == 10 &&
            applied.PendingCallbacks.Select(item => item.Kind).SequenceEqual(["OnStatusEffectChanged", "OnArmorAdded", "OnNewStatusEffectAdded"]) &&
            applied.PendingCallbacks[0].ParamInt == 0 && applied.PendingCallbacks[0].ParamInt2 == 0 && applied.PendingCallbacks[0].ParamString == "armor" &&
            applied.PendingCallbacks[2].ParamInt == 2, "Zero addition lost native queue order, payload or deferred execution.");
        // Spells target a live chosen unit with a native DropTargetCharacter collection.
        var effect = new CardActionEffect("AddStatus", "DropTargetCharacter", 0, false, true, [zeroArmor]);
        var first = CardSpellModel.Apply(root, [effect], 1);
        int expected = 10 + GoldRewardModel.Adjust(2) + GoldRewardModel.Adjust(7);
        Require(first.Supported && first.State!.Context!.Gold == expected && first.State.Units[0].Triggers[1].HasTriggered &&
            !first.State.Units[0].Triggers[0].HasTriggered, "Callbacks ignored native threshold arguments or failed to drain after the effect.");
        var second = CardSpellModel.Apply(first.State!, [effect], 1);
        Require(second.Supported && second.State!.Context!.Gold == expected + GoldRewardModel.Adjust(7),
            "A zero-count readdition lost the new-status callback or fired a once-only armor trigger again.");
        Parallel.For(0, 32, _ => Require(CardSpellModel.Apply(root, [effect], 1).State!.Context!.Gold == expected,
            "Parallel callback state differed."));
        Require(JsonSerializer.Serialize(root) == original, "Callback execution mutated a parent or sibling.");
        CombatUnit Unit(int id, CombatStatus[] statuses, CombatTrigger[] triggers, CombatTeam team = CombatTeam.Player,
            int hp = 20, bool boss = false, string[]? immunities = null) => new(id, "callbacks", team, 0, hp, 20, false, false, false,
                statuses, triggers, modifiers: new(0, 0, 0, 1, 1, true, false, []), isBoss: boss, lastAttackerId: 0,
                statusImmunities: immunities, statusRegistry: statuses);
        RoomCombatState Room(CombatUnit actor, CombatContext? scope = null) => new(0, false, [actor], [], scope ?? context);
        var armor = new CombatStatus("armor", 5, 1, stackable: true, hidden: false, displayCategory: "Positive");
        var valor = new CombatStatus("valor", 2, 1, stackable: true, hidden: false, displayCategory: "Positive");
        var valorRoot = Room(Unit(2, [armor, valor], [Gold("OnStatusEffectChanged", 1, threshold: 3)]),
            new(context.Cards, rng, 10, 1, 10, statusRules: [zeroArmor]));
        var valorEnd = RoomCombatModel.Resolve(valorRoot);
        Require(valorEnd.Supported && valorEnd.State!.Units[0].Status("armor")!.Stacks == 5 &&
            valorEnd.State.Context!.Gold == 10 + GoldRewardModel.Adjust(1), "Valor skipped the native zero armor addition callback.");
        var silence = new CombatStatus("silenced", 1, removeAllAtEnd: true, stackable: false, hidden: false, displayCategory: "Negative");
        var silent = Room(Unit(3, [silence], [Gold("OnSilence", 3), Gold("OnSilenceLost", 6, ignore: false)]));
        var negative = StatusApplicationModel.Apply(silent, 3, silence.WithStacks(-40000));
        Require(negative.Supported && negative.PendingCallbacks.Select(item => item.Kind).SequenceEqual(["OnStatusEffectChanged", "OnSilence"]) &&
            negative.PendingCallbacks[0].ParamInt == 0 && negative.PendingCallbacks[0].ParamInt2 == -1,
            "A negative AddStatusEffect incorrectly used the removal-only SilenceLost callback.");
        var cleared = RoomCombatModel.Resolve(silent);
        Require(cleared.Supported && cleared.State!.Units[0].StatusRegistry!.Count == 0 &&
            cleared.State.Context!.Gold == 10 + GoldRewardModel.Adjust(6), "Cleanup did not remove silence before firing its non-ignoring callback.");
        var immune = StatusApplicationModel.Apply(Room(Unit(4, [], [], immunities: ["armor"])), 4, zeroArmor);
        Require(immune.Supported && immune.PendingCallbacks.Count == 0 && immune.State!.Units[0].StatusRegistry!.Count == 0,
            "An immune no-op queued callbacks or created a definition.");
        var nonstackable = new CardUpgradeModifier("piercing", "piercing", new(),
            [new("piercing", 2, stackable: false, hidden: false, displayCategory: "Positive")], false, false, false, 0, 0, []);
        var upgradeContext = new CombatContext(context.Cards, rng, 10, 1, 10, cardInstances: [], cardRegistry: []);
        var capped = UnitModifierModel.Apply(Room(Unit(4, [], [Gold("OnStatusEffectChanged", 1, threshold: 1)]), upgradeContext),
            4, nonstackable, "TemporaryUntilUnitDeath");
        Require(capped.Supported && capped.State!.Units[0].Status("piercing")!.Stacks == 1 && capped.State.Context!.Gold == 10 + GoldRewardModel.Adjust(1),
            "Unit-upgrade callbacks used an unclamped nonstackable status count.");
        var starting = StatusCallbackModel.MergeStartingStatuses([armor.WithStacks(3)], [armor.WithStacks(1), zeroArmor]);
        var startingActor = Unit(9, [armor.WithStacks(4)], [Gold("OnStatusEffectChanged", 1, threshold: 3)]);
        var startingContext = new CombatContext(context.Cards, rng, 10, 1, 10, cardInstances: [], cardRegistry: []);
        var startingResult = RoomCombatModel.ApplySpawnTriggers(Room(startingActor, startingContext), 9, fromCard: false, startingApplications: starting);
        Require(startingResult.Supported && startingResult.State!.Context!.Gold == 10 + GoldRewardModel.Adjust(1),
            "Merged starting armor rewarded two additions instead of the single native application.");
        var afterPermanent = StatusCallbackModel.MergeStartingStatuses([armor], [armor.WithStacks(-8), armor.WithStacks(2)]);
        var afterTemporary = StatusCallbackModel.MergeStartingStatuses(afterPermanent, [armor.WithStacks(-3), armor.WithStacks(1)]);
        Require(afterPermanent.Single().Stacks == 2 && afterTemporary.Single().Stacks == 1,
            "Starting-status merging lost per-entry nonnegative clamps or modifier-group boundaries.");
        var missing = new RoomCombatState(0, false, [new CombatUnit(5, "legacy", CombatTeam.Player, 0, 20, 20, false, false, false, [],
            [Gold("OnNewStatusEffectAdded", 1)])], [], context);
        Require(!StatusApplicationModel.Apply(missing, 5, zeroArmor).Supported, "Uncaptured native status-presence thresholds were guessed.");
        foreach (bool boss in new[] { false, true })
        {
            var dead = Unit(6, [], [Gold("OnArmorAdded", 2, once: true)], hp: 0, boss: boss);
            var queued = new RoomCombatModel.QueuedCharacterTrigger(0, dead, "OnArmorAdded");
            var fired = RoomCombatModel.ApplyQueuedCharacterTrigger(new(0, false, [], [], context), queued, _ => { });
            Require(fired.Supported && fired.State!.Units.Count == 0 && fired.State.Context!.Gold == 10 &&
                queued.Unit.Triggers[0].HasTriggered == !boss, "Retained dying callbacks lost the native boss gate or marked/ran dead effects incorrectly.");
        }
        var enemy = Unit(0, [new("regen", 3, 1, hidden: false, displayCategory: "Positive"), zeroArmor],
            [Gold("OnStatusEffectChanged", 1, once: true, threshold: 3), Gold("OnNewStatusEffectAdded", 2, once: true), Gold("OnSpawn", 3)], CombatTeam.Enemy);
        var train = new TrainCombatState([new(0, false, [], [], context), new(1, false, [], [], context)], [], 7, context);
        var spawning = new EnemySpawnState(train, [new([new([new(enemy, true, false, []), new(enemy, true, false, [])])])],
            [0], 0, false, rng, 10, [], 0, false, 1, 1, 1, []);
        var spawned = EnemySpawningModel.Spawn(spawning, false);
        Require(spawned.Supported && spawned.State!.Train.Context!.Gold == 10 + 2 * (GoldRewardModel.Adjust(1) + GoldRewardModel.Adjust(2) + GoldRewardModel.Adjust(3)) &&
            spawned.State.Train.Rooms[0].Units.All(actor => actor.Triggers.All(trigger => trigger.HasTriggered)),
            "Enemy group creation failed to dispatch initial status callbacks before spawn triggers.");
        CombatEffect StatusEffect(CombatStatus status) => new("CardEffectAddStatusEffect", 0, 0, "", 0, [], false,
            action: new("AddStatus", "Self", 0, true, false, [status], tests: new(true, false, false, false)));
        CombatTrigger EffectTrigger(string kind, CombatEffect effect, bool once = false) => new(kind, once, false, true, 1, [effect], false);
        var nested = Unit(0, [new("regen", 3, 1, hidden: false, displayCategory: "Positive")],
            [EffectTrigger("OnStatusEffectChanged", StatusEffect(zeroArmor.WithStacks(1)), once: true),
             EffectTrigger("OnArmorAdded", StatusEffect(valor.WithStacks(1)), once: true),
             EffectTrigger("OnSpawn", new("CardEffectDamage", 1, 0, "", 0, [], false,
                action: new("Damage", "Self", 1, true, false, [], tests: new(true, false, false, false)), damageStatusMultiplier: "valor"))], CombatTeam.Enemy);
        var nestedSpawn = EnemySpawningModel.Spawn(new(train, [new([new([new(nested, true, false, [])])])], [0],
            0, false, rng, 10, [], 0, false, 1, 1, 1, []), false);
        Require(nestedSpawn.Supported && nestedSpawn.State!.Train.Rooms[0].Units.Single().Health == 20 &&
            nestedSpawn.State.Train.Rooms[0].Units.Single().Status("valor")!.Stacks == 1,
            "Nested initial callbacks ran before the already queued enemy OnSpawn phase.");
        Console.WriteLine("STATUS-CALLBACK-CHECKS PASS: zero/signed payloads, dictionary thresholds, deferred boundaries, once/silence/dead-boss gates, cleanup, valor no-op, enemy initialization and 32 isolated branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("StatusCallbackFires", out var fires) || fires.GetArrayLength() == 0) return;
        var queued = fixture.GetProperty("StatusCallbacks").EnumerateArray().ToArray();
        var dispatches = fires.EnumerateArray().ToArray();
        Require(queued.Length == dispatches.Length, "Native status callback queue did not fully drain.");
        int reward = 0, zero = 0, negative = 0, dying = 0, enemy = 0, silenceLost = 0, sameArmor = 0, once = 0, silenceGate = 0;
        int nested = 0, damage = 0, heals = 0, upgrades = 0, copies = 0, rooms = 0, standaloneCopies = 0, insideCopies = 0;
        var kinds = new HashSet<string>();
        for (int index = 0; index < dispatches.Length; index++)
        {
            var record = dispatches[index]; var enqueued = queued[index];
            foreach (string key in new[] { "ActorId", "Kind", "ParamInt", "ParamInt2", "ParamString" })
                Require(record.GetProperty(key).ContentEquals(enqueued.GetProperty(key)), "Native status FIFO payload changed at index " + index + " field " + key);
            Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("Interactions").GetArrayLength() == 0 &&
                record.GetProperty("ActualUnit").ValueKind == FixtureKind.Object, "Incomplete native status callback dispatch.");
            var before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actor = record.GetProperty("BeforeUnit").Deserialize<CombatUnit>()!;
            var actual = record.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            var actualActor = record.GetProperty("ActualUnit").Deserialize<CombatUnit>()!;
            string kind = record.GetProperty("Kind").GetString()!;
            var callback = new RoomCombatModel.QueuedCharacterTrigger(before.RoomIndex, actor, kind,
                paramInt: record.GetProperty("ParamInt").GetInt32(), paramInt2: record.GetProperty("ParamInt2").GetInt32(),
                paramString: record.GetProperty("ParamString").GetString());
            var generated = new List<RoomCombatModel.QueuedCharacterTrigger>();
            var result = RoomCombatModel.ApplyQueuedCharacterTrigger(before, callback, generated.Add);
            string? difference = result.Supported ? ModelJson.Difference(JsonSerializer.Serialize(result.State!.Context), JsonSerializer.Serialize(actual.Context)) : result.UnsupportedReason;
            difference ??= ModelJson.Difference(JsonSerializer.Serialize(callback.Unit), JsonSerializer.Serialize(actualActor));
            Require(result.Supported && difference == null, "Independent native callback differs at sequence " + record.GetProperty("Sequence").GetInt32() + ": " + difference);
            if (record.TryGetProperty("Generated", out var nativeGenerated))
            {
                var statusGenerated = generated.Where(item => !item.DeferUntilRemoval).ToArray();
                var statusNative = nativeGenerated.EnumerateArray().ToArray();
                Require(statusGenerated.Length == statusNative.Length, "Nested native status callback count differs at sequence " + record.GetProperty("Sequence").GetInt32());
                for (int queuedIndex = 0; queuedIndex < statusGenerated.Length; queuedIndex++)
                {
                    var expected = statusGenerated[queuedIndex]; var observed = statusNative[queuedIndex];
                    Require(expected.Unit.Id == observed.GetProperty("ActorId").GetInt32() && expected.Kind == observed.GetProperty("Kind").GetString() &&
                        expected.ParamInt == observed.GetProperty("ParamInt").GetInt32() && expected.ParamInt2 == observed.GetProperty("ParamInt2").GetInt32() &&
                        expected.ParamString == observed.GetProperty("ParamString").GetString(), "Nested native status payload differs at sequence " + record.GetProperty("Sequence").GetInt32());
                }
                // A retained actor without a room is compared separately through ActualUnit.
                var roomUnits = result.State!.Units.Where(unit => unit.Id != actor.Id || before.Units.Any(original => original.Id == actor.Id) || actual.Units.Any(original => original.Id == actor.Id)).ToArray();
                string? roomDifference = ModelJson.Difference(JsonSerializer.Serialize(roomUnits), JsonSerializer.Serialize(actual.Units));
                Require(roomDifference == null, "Native callback room units differ at sequence " + record.GetProperty("Sequence").GetInt32() + ": " + roomDifference);
                rooms++; nested += statusNative.Length;
                var liveEffects = actor.Health > 0 && actualActor.Health > 0 ? actor.Triggers.Where((trigger, triggerIndex) =>
                    trigger.Kind == kind && trigger.Once && !trigger.HasTriggered && actualActor.Triggers[triggerIndex].HasTriggered)
                    .SelectMany(trigger => trigger.Effects).ToArray() : [];
                damage += liveEffects.Count(effect => effect.Action?.Type == "Damage");
                heals += liveEffects.Count(effect => effect.Action?.Type == "Heal");
                upgrades += liveEffects.Count(effect => effect.UnitUpgrade != null);
                copies += liveEffects.Count(effect => effect.Generation?.CopyModifiers == true);
                var newCards = (actual.Context!.CardInstances ?? []).Where(card => before.Context!.FindCard(card.InstanceId) == null).ToArray();
                const string standaloneMarker = "6daaac75-57bc-4c02-9ca5-c011baac0002", insideMarker = "6daaac75-57bc-4c02-9ca5-c011baac0001";
                var sourceCard = before.Context!.FindCard(actor.SpawnerCardId);
                if (kind == "OnPyregelAdded" && newCards.Length > 0 && actor.Modifiers!.Upgrades.Any(upgrade => upgrade.DataId == standaloneMarker) &&
                    sourceCard != null && sourceCard.Permanent.Upgrades.All(upgrade => upgrade.DataId != standaloneMarker))
                {
                    Require(newCards.All(card => card.Permanent.Upgrades.All(upgrade => upgrade.DataId != standaloneMarker)), "Standalone callback copied a not-yet-written source upgrade.");
                    standaloneCopies++;
                }
                insideCopies += liveEffects.Any(effect => effect.UnitUpgrade?.Upgrade?.DataId == insideMarker) &&
                    newCards.Any(card => card.Permanent.Upgrades.Any(upgrade => upgrade.DataId == insideMarker)) ? 1 : 0;
            }
            else Require(generated.Count == 0, "Legacy status callback generated uncaptured nested work.");
            int gain = record.GetProperty("GoldAfter").GetInt32() - record.GetProperty("GoldBefore").GetInt32();
            reward += gain > 0 ? 1 : 0;
            kinds.Add(kind); dying += actor.Health == 0 ? 1 : 0; enemy += actor.Team == CombatTeam.Enemy ? 1 : 0;
            once += actor.Triggers.Count(trigger => trigger.Kind == kind && trigger.Once && trigger.HasTriggered);
            silenceGate += actor.Triggers.Count(trigger => trigger.Kind == kind && !trigger.IgnoreSilence && actor.Status("silenced") != null);
            silenceLost += kind == "OnSilenceLost" && gain > 0 ? 1 : 0;
            if (kind == "OnStatusEffectChanged")
            {
                int delta = callback.ParamInt2;
                zero += delta == 0 ? 1 : 0; negative += delta < 0 ? 1 : 0;
                sameArmor += delta == 0 && callback.ParamString == "armor" && gain > 0 ? 1 : 0;
            }
        }
        Require(kinds.IsSupersetOf(StatusCallbackModel.Kinds) && reward > 0 && zero > 0 && negative > 0 && dying > 0 && enemy > 0 &&
            silenceLost > 0 && sameArmor > 0 && once > 0 && silenceGate > 0,
            $"Native status callback coverage incomplete: kinds={string.Join(',', kinds)}, rewards={reward}, zero={zero}, negative={negative}, dying={dying}, enemy={enemy}, silenceLost={silenceLost}, sameArmor={sameArmor}, once={once}, silenceGate={silenceGate}.");
        Console.WriteLine($"NATIVE-STATUS-CALLBACKS PASS: {dispatches.Length} FIFO dispatches with exact payloads/unit/context, seven kinds, {reward} rewards, {zero} zero/{negative} negative deltas, {dying} dying actors, {enemy} enemies, {silenceLost} rewarded silence losses, {sameArmor} same-armor rewards, {once} once/{silenceGate} silence gates.");
        if (fixture.TryGetProperty("StatusCallbackActions", out var actions) && actions.GetBoolean())
        {
            Require(nested > 0 && rooms == dispatches.Length && damage > 0 && heals > 0 && upgrades > 0 && copies > 0 && standaloneCopies > 0 && insideCopies > 0, "Native status callback action coverage incomplete.");
            Console.WriteLine($"NATIVE-STATUS-CALLBACK-ACTIONS PASS: {rooms} exact room scopes, {nested} nested queue payloads, {damage} damage/{heals} healing/{upgrades} upgrade/{copies} source-copy effects, {standaloneCopies} standalone/{insideCopies} running-queue source boundaries.");
        }
    }
    private static void Require(bool pass, string message) { if (!pass) throw new InvalidOperationException(message); }
}
