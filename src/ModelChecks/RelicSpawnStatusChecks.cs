using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicSpawnStatusChecks
{
    internal static void Run()
    {
        var armor = new CombatStatus("armor", 5, hidden: false, displayCategory: "Positive");
        var shield = new CombatStatus("damage shield", 2, hidden: false, displayCategory: "Positive");
        var limit = new RelicConditionState(new("SubtypeInDeck"), true, 6, 1, false);
        var rule = new RelicSpawnStatus(0, true, false, [armor, shield], conditions: [limit]);
        var actor = new CombatUnit(1, "player", CombatTeam.Player, 2, 20, 20, true, false, false, [],
            statusRegistry: [], statusDictionary: new([], []));
        var relic = Relic(rule);
        var room = Room(actor, relic);
        string parent = Serialize(room);
        var first = RelicSpawnStatusModel.CharacterAdded(room, 1, 0, false);
        Require(first.Supported, first.UnsupportedReason ?? "First spawn failed.");
        Require(first.State!.Context!.BattleRng.Equals(room.Context!.BattleRng.Next()) &&
            first.State.Context.Relics![0].SpawnStatuses![0].Conditions[0].Triggered &&
            first.State.Context.Relics[0].SpawnStatuses![0].Conditions[0].DurationTriggerCount == 1 && first.PendingCallbacks.Count == 3,
            "Native RNG, condition notification or status callbacks were lost.");
        var second = RelicSpawnStatusModel.CharacterAdded(first.State, 1, 0, false);
        Require(Serialize(second.State) == Serialize(first.State) && second.PendingCallbacks.Count == 0,
            "A spent first-unit condition consumed RNG or added statuses.");
        var resetContext = RelicSpawnStatusModel.EndDuration(first.State.Context, "ThisTurn");
        var reset = new RoomCombatState(0, false, first.State.Units, [], resetContext);
        Require(RelicSpawnStatusModel.CharacterAdded(reset, 1, 0, false).State!.Context!.BattleRng.Equals(resetContext.BattleRng.Next()),
            "Turn rollover did not reset native trigger conditions.");
        var mismatch = RelicSpawnStatusModel.CharacterAdded(Room(actor, Relic(new(0, true, false, [armor],
            requiredSubtype: "missing", subtypeIsNone: false))), 1, 0, false);
        Require(mismatch.Supported && mismatch.State!.Units[0].Statuses.Count == 0 &&
            mismatch.State.Context!.BattleRng.Equals(room.Context.BattleRng.Next()), "Subtype rejection occurred before native random selection.");
        foreach (var skipped in new[] { new RelicSpawnStatus(0, false, true, [armor]),
            new RelicSpawnStatus(0, true, false, [armor], restrictedRoom: 2),
            new RelicSpawnStatus(0, true, false, [armor], sourceKind: "OnlyFromCard"),
            new RelicSpawnStatus(0, true, false, [armor], characterAssetKeys: ["other"]) })
        {
            var blocked = RelicSpawnStatusModel.CharacterAdded(Room(actor, Relic(skipped)), 1, 0, false);
            Require(blocked.Supported && blocked.State!.Context!.BattleRng.Equals(room.Context.BattleRng), "An early birth gate consumed RNG.");
        }
        var percentage = RelicSpawnStatusModel.CharacterAdded(Room(actor, Relic(new(0, true, false, [armor], hpPercentAsStacks: 33))), 1, 0, false);
        Require(percentage.State!.Units[0].Status("armor")!.Stacks == 6, "Native single-precision max-HP percentage differed.");
        var immune = new CombatUnit(1, "player", CombatTeam.Player, 2, 20, 20, true, false, false, [], statusImmunities: ["armor"],
            statusRegistry: [], statusDictionary: new([], []));
        var immuneResult = RelicSpawnStatusModel.CharacterAdded(Room(immune, Relic(new(0, true, false, [armor], conditions: [limit]))), 1, 0, false);
        Require(immuneResult.State!.Units[0].Statuses.Count == 0 && immuneResult.State.Context!.Relics![0].SpawnStatuses![0].Conditions[0].Triggered,
            "Per-status immunity suppressed the relic's native notification.");
        Parallel.For(0, 32, _ => Require(Serialize(RelicSpawnStatusModel.CharacterAdded(room, 1, 0, false).State) == Serialize(first.State),
            "Relic spawn status parallel branch diverged."));
        Require(Serialize(room) == parent, "Relic spawn status branches mutated their parent.");
        Require(!RelicSpawnStatusModel.CharacterAdded(Room(actor, new("bad", "bad", ["RelicEffectAddStatusEffectOnSpawn"])), 1, 0, false).Supported,
            "Missing native effect parameters were guessed.");
        var callbackActor = actor.WithTriggers([new("OnStatusEffectChanged", false, false, true, 1,
            [new("CardEffectRewardGold", 3, 0, "", 0, [], false)], false), new("OnArmorAdded", false, false, true, 1,
            [new("CardEffectRewardGold", 11, 0, "", 0, [], false)], false)]);
        var other = new CombatUnit(2, "other", CombatTeam.Player, 1, 20, 20, true, false, false, [],
            [new("OnUnitAbilityAvailable", false, false, true, 1, [new("CardEffectRewardGold", 19, 0, "", 0, [], false)], false)],
            statusRegistry: [], statusDictionary: new([], []));
        var birthRoom = Room(callbackActor, Relic(new(0, true, false, [armor], conditions: [limit])));
        var birthTrain = new TrainCombatState([birthRoom, new(1, false, [other], [], birthRoom.Context)], [], 7, birthRoom.Context);
        UnitCloneCallback[] prior = [new(2, "OnUnitAbilityAvailable", 0, 0, null, 1, 0, 0, 0)];
        string trainParent = Serialize(birthTrain);
        var settledBirth = RelicBirthModel.CharacterAdded(birthTrain, 1, 0, false, false, prior);
        var deferredBirth = RelicBirthModel.CharacterAdded(birthTrain, 1, 0, false, true, prior);
        // Native reward rounding gives 20 + 5 + 10 for these three callbacks.
        Require(settledBirth.Supported && settledBirth.State!.Context!.Gold == 35 && settledBirth.Queued.Count == 0 &&
            settledBirth.Dispatched.Count == 4 && settledBirth.Dispatched[0].ActorId == 2 &&
            settledBirth.Dispatched[2].Kind == "OnArmorAdded", $"Relic yields lost the shared cross-room FIFO queue: {settledBirth.UnsupportedReason}; gold={settledBirth.State?.Context?.Gold}; queued={settledBirth.Queued.Count}; dispatched={string.Join(",", settledBirth.Dispatched.Select(item => item.Kind))}");
        Require(deferredBirth.Supported && deferredBirth.State!.Context!.Gold == 0 && deferredBirth.Dispatched.Count == 0 &&
            deferredBirth.Queued.Count == 4, "A running native queue drained relic callbacks recursively.");
        Parallel.For(0, 32, _ => Require(Serialize(RelicBirthModel.CharacterAdded(birthTrain, 1, 0, false, false, prior).State) ==
            Serialize(settledBirth.State), "Settled relic birth branches differed."));
        Require(Serialize(birthTrain) == trainParent, "Settling a relic birth changed its parent.");
        Console.WriteLine("RELIC-SPAWN-STATUS-CHECKS PASS: native selection/gating order, immutable condition counters, duration reset, immunity notification, HP percentage, queued callbacks and 32 branches.");

        static CombatRelicState Relic(RelicSpawnStatus rule) => new("spawn", "spawn", ["RelicEffectAddStatusEffectOnSpawn"], [rule], false, false);
        static RoomCombatState Room(CombatUnit actor, CombatRelicState relic) => new(0, false, [actor], [],
            new(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(13), 0, 1, 10, relics: [relic]));
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out var scenario) || scenario.GetString() is not ("spawn-status-relics" or "spawn-status-relics-clones")) return;
        bool clones = scenario.GetString() == "spawn-status-relics-clones";
        int schema = fixture.GetProperty("Schema").GetInt32();
        Require((clones ? schema is 110 or 111 : schema is 109 or 110) && fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0, "Incomplete relic spawn status native recording.");
        var records = fixture.GetProperty("RelicSpawnStatuses").EnumerateArray().ToArray();
        int players = 0, enemies = 0, shieldTriggers = 0, shieldSkips = 0;
        var turns = new HashSet<int>();
        foreach (var record in records)
        {
            var before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = record.GetProperty("After").Deserialize<RoomCombatState>()!;
            int id = record.GetProperty("UnitId").GetInt32();
            string? difference;
            if (clones)
            {
                var train = record.GetProperty("BeforeTrain").Deserialize<TrainCombatState>()!;
                string protocol = record.GetProperty("SettlementProtocol").GetString()!;
                bool independent = record.GetProperty("StandaloneScenarioActive").GetBoolean() && !record.GetProperty("ReplayCardPlayingBefore").GetBoolean();
                Require(protocol is "standalone-scenario-birth" or "enclosing-native-coroutine" &&
                    (protocol == "standalone-scenario-birth") == independent &&
                    record.GetProperty("DeferCallbacks").GetBoolean() == (!independent || record.GetProperty("QueueRunningBefore").GetBoolean()),
                    "Relic birth settlement protocol and native queue state disagree.");
                Require(record.GetProperty("UnsupportedQueueReason").ValueKind == FixtureKind.Null, "Unsupported native relic queue flags.");
                var predicted = RelicBirthModel.CharacterAdded(train, id, record.GetProperty("FromCardId").GetInt32(),
                    record.GetProperty("OnlyCovenants").GetBoolean(), record.GetProperty("DeferCallbacks").GetBoolean(),
                    record.GetProperty("QueuedBefore").Deserialize<UnitCloneCallback[]>()!);
                Require(predicted.Supported, "Independent relic birth unsupported: " + predicted.UnsupportedReason);
                difference = ModelJson.Difference(Serialize(predicted.State), Serialize(record.GetProperty("AfterTrain").Deserialize<TrainCombatState>()));
                string? queueDifference = ModelJson.Difference(Serialize(predicted.Queued), Serialize(record.GetProperty("QueuedAfter").Deserialize<UnitCloneCallback[]>()));
                string? dispatchDifference = ModelJson.Difference(Serialize(predicted.Dispatched), Serialize(record.GetProperty("Dispatched").Deserialize<UnitCloneCallback[]>()));
                Require(queueDifference == null && dispatchDifference == null,
                    $"Independent relic birth {id} differs: queue={queueDifference}; dispatch={dispatchDifference}; train={difference}");
                Require(Serialize(train.Rooms.Single(room => room.RoomIndex == before.RoomIndex)) == Serialize(before) &&
                    Serialize(predicted.State!.Rooms.Single(room => room.RoomIndex == after.RoomIndex)) == Serialize(after),
                    "Native relic room/train observations disagree.");
            }
            else
            {
                RoomCombatResult predicted = RelicSpawnStatusModel.CharacterAdded(before, id, record.GetProperty("FromCardId").GetInt32(),
                    record.GetProperty("OnlyCovenants").GetBoolean());
                difference = predicted.Supported ? ModelJson.Difference(Serialize(predicted.State), Serialize(after)) : predicted.UnsupportedReason;
            }
            Require(difference == null, "Independent relic birth differs: " + difference);
            Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "Native relic birth observation was incomplete or differed.");
            var relics = before.Context!.Relics!;
            Require(relics.Any(relic => relic.AssetKey == "SpawnWithArmor" && relic.DataId == "68ef2523-5c2e-4660-b96d-00b1c0485f54") &&
                relics.Any(relic => relic.AssetKey == "FirstUnitGainDamageShield" && relic.DataId == "60a2a8a3-5f7a-4a9d-b427-5f261145fa1f") &&
                relics.Any(relic => relic.AssetKey == "FrostbiteOnEnemies" && relic.DataId == "270356af-16bd-4433-a0b2-3e5bb94ef890"),
                "Original relic identities were not retained.");
            CombatUnit actor = after.Units.Single(unit => unit.Id == id);
            if (actor.Team == CombatTeam.Player)
            {
                CombatUnit initial = before.Units.Single(unit => unit.Id == id);
                players++; Require(actor.Status("armor")?.Stacks == Math.Min(9999, (initial.Status("armor")?.Stacks ?? 0) + 5),
                    "Original armor relic did not add five stacks.");
                int oldCount = before.Context.Relics!.Single(relic => relic.AssetKey == "FirstUnitGainDamageShield").SpawnStatuses![0].Conditions[0].DurationTriggerCount;
                int newCount = after.Context!.Relics!.Single(relic => relic.AssetKey == "FirstUnitGainDamageShield").SpawnStatuses![0].Conditions[0].DurationTriggerCount;
                if (oldCount == 0) { shieldTriggers++; turns.Add(before.Context.QueryFrame!.Turn!.Value); Require(newCount == 1 && actor.Status("damage shield")?.Stacks == 2, "First-unit shield failed."); }
                else { shieldSkips++; Require(newCount == oldCount && actor.Status("damage shield")?.Stacks == initial.Status("damage shield")?.Stacks,
                    "First-unit shield repeated within a turn."); }
                if (clones)
                {
                    Require(before.Context.TriggerCounts?.Modifiers.Any(modifier => modifier.Kind == "OnSpawn" && modifier.Value == 1) == true &&
                        before.Context.Relics!.Any(relic => relic.AssetKey == "ExtraSpawnTrigger" && relic.DataId == "9e0deb69-6196-44a6-8220-85bd0df25f77"),
                        "Original extra-spawn relic cache/identity is missing.");
                    foreach (var trigger in actor.Triggers)
                        Require(TriggerCountModel.Query(after.Context, actor, trigger, out var error) == trigger.FireCount && error == null,
                            "Copied-unit native trigger count differs.");
                }
            }
            else { enemies++; Require(actor.Status("poison")?.Stacks == 2, "Original enemy frostbite relic did not add two stacks."); }
            if (clones)
            {
                var train = record.GetProperty("BeforeTrain").Deserialize<TrainCombatState>()!;
                string parent = Serialize(train);
                Parallel.For(0, 32, _ =>
                {
                    var child = RelicBirthModel.CharacterAdded(train, id, record.GetProperty("FromCardId").GetInt32(),
                        record.GetProperty("OnlyCovenants").GetBoolean(), record.GetProperty("DeferCallbacks").GetBoolean(),
                        record.GetProperty("QueuedBefore").Deserialize<UnitCloneCallback[]>()!);
                    Require(child.Supported && Serialize(child.State) == Serialize(record.GetProperty("AfterTrain").Deserialize<TrainCombatState>()) &&
                        Serialize(child.Queued) == Serialize(record.GetProperty("QueuedAfter").Deserialize<UnitCloneCallback[]>()) &&
                        Serialize(child.Dispatched) == Serialize(record.GetProperty("Dispatched").Deserialize<UnitCloneCallback[]>()),
                        "Parallel native relic birth train/queue/dispatch branch differed.");
                });
                Require(Serialize(train) == parent, "Native relic birth changed its parent.");
            }
            else Parallel.For(0, 32, _ => Require(Serialize(RelicSpawnStatusModel.CharacterAdded(before, id,
                record.GetProperty("FromCardId").GetInt32(), record.GetProperty("OnlyCovenants").GetBoolean()).State) == Serialize(after),
                "Parallel native relic birth branch differed."));
        }
        Require(players > 1 && enemies > 0 && shieldTriggers > 0 && shieldSkips > 0 &&
            (clones || shieldTriggers > 1 && turns.Count > 1),
            "Native relic spawn status team/first-unit/turn-reset coverage incomplete.");
        if (clones)
        {
            var copyRecords = fixture.GetProperty("UnitCloneOperations").EnumerateArray().ToArray();
            Require(copyRecords.Length == 13 && copyRecords.Any(record => record.GetProperty("Cardless").GetBoolean() && record.GetProperty("UnitId").GetInt32() > 0),
                "Native relic clone/cardless coverage is incomplete.");
            Require(records.Any(record => record.GetProperty("Before").Deserialize<RoomCombatState>()!.Units.Single(unit =>
                unit.Id == record.GetProperty("UnitId").GetInt32()).Status("cardless") != null), "No native cardless relic birth was recorded.");
            Require(records.Any(record => !record.GetProperty("DeferCallbacks").GetBoolean() && record.GetProperty("Dispatched").GetArrayLength() > 0) &&
                records.Any(record => record.GetProperty("DeferCallbacks").GetBoolean() && record.GetProperty("QueuedAfter").GetArrayLength() > 0),
                "Native relic birth lacks both settled and deferred queue boundaries.");
        }
        Console.WriteLine($"NATIVE-RELIC-SPAWN-STATUS-CHECKS PASS: {records.Length} exact birth phases, {players} player/{enemies} enemy births, {shieldTriggers} first-unit triggers/{shieldSkips} skips across {turns.Count} turns and 32 isolated branches.");
    }
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
