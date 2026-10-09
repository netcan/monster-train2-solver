using System.Text.Json;
using MonsterTrain2Poju.Model;
using MonsterTrain2Poju.Fixtures;

internal static class IncantChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(173);
        CombatTrigger Reward(int value, bool once = false, bool ignoreSilence = true, int fireCount = 1, int? threshold = null) => new("CardSpellPlayed", once, false,
            ignoreSilence, fireCount, [new("CardEffectRewardGold", value, 0, "", 0, [], false)], false, threshold);
        CombatUnit Actor(int id, CombatTeam team, params CombatStatus[] statuses) => new(id, "incant", team, 0, 20, 20,
            false, false, false, statuses, [Reward(5)], size: 1);
        BattleTurnState Root(CardPlayRule rule, CombatUnit[]? actors = null)
        {
            var cards = new CardCycleState([new(1, rule.DataId), new(2, rule.DataId)], [], [], rng, 0, []);
            var context = new CombatContext(cards, rng, 0, 3, 10, statistics: BattleStatistics.Empty().TrackCards([1, 2]),
                purifyBlockedTriggers: ["CardSpellPlayed"]);
            var train = new TrainCombatState([
                new(0, false, actors ?? [Actor(10, CombatTeam.Player), Actor(11, CombatTeam.Enemy)], [], context),
                new(1, false, [Actor(12, CombatTeam.Player)], [], context), new(2, false, [], [], context),
                new(3, false, [new(19, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, [])], [], context)], [], 7, context);
            var spawn = new EnemySpawnState(train, [new([new([])])], [-1], 0, false, rng, 20, [], 0, false, 2, 1, 3, []);
            var rules = new BattlePlayRules([new(0, 5, 7, true, false, false), new(1, 5, 7, true, false, false),
                new(2, 5, 7, true, false, false), new(3, 0, 7, true, true, true)], [rule]);
            return new(spawn, 3, 3, 5, 0, 0, "New", [new("Spawning", 173, rng)],
                [new("Standby", []), new("Exhausted", []), new("Purged", []), new("DiscardBuffer", [])], [], rules);
        }
        CardPlayRule Null(string? type, bool? ability = false) => new("null", "null", 0, "Null", "Discard", null, [],
            cardType: type, isAnyAbility: ability);
        var root = Root(Null("Spell")); string parent = Serialize(root);
        var first = BattleActionModel.PlayCard(root, new(1, 0));
        Require(first.Supported && first.State!.Spawn.Train.Context!.Gold == 10 &&
            first.State.Spawn.Train.Context.Statistics!.Value(1, "TimesPlayed") == 1 &&
            first.State.Spawn.Train.Rooms[0].Units.All(unit => unit.Triggers[0].HasTriggered) &&
            !first.State.Spawn.Train.Rooms[1].Units[0].Triggers[0].HasTriggered,
            "An empty-effect spell lost Incant, both teams, played statistics or original-floor cache: " + first.Reason);
        var second = BattleActionModel.PlayCard(first.State!, new(2, 0));
        Require(second.Supported && second.State!.Spawn.Train.Context!.Gold == 20, "Repeated ordinary Incant did not fire twice.");
        foreach (var rule in new[] { Null("Equipment"), Null("Monster"), Null("Spell", true) })
        {
            var skipped = BattleActionModel.PlayCard(Root(rule), new(1, 0));
            Require(skipped.Supported && skipped.State!.Spawn.Train.Context!.Gold == 0,
                "Equipment, monsters or ability spells incorrectly triggered Incant: " + rule.CardType + "/" + skipped.Reason);
        }
        foreach (var rule in new[] { Null(null), Null("Spell", null) })
        {
            var unknown = BattleActionModel.PlayCard(Root(rule), new(1, 0));
            Require(!unknown.Supported && unknown.Reason!.Contains("captured card type"), "Missing card/ability metadata was guessed.");
        }
        var visibility = new CombatUnit(10, "visibility", CombatTeam.Player, 0, 20, 20, false, false, false,
            [new("silenced", 1)], [Reward(5), Reward(10, ignoreSilence: false), Reward(15, once: true)], size: 1);
        var visibleRoot = Root(Null("Spell"), [visibility]);
        var visibleFirst = BattleActionModel.PlayCard(visibleRoot, new(1, 0));
        var visibleSecond = BattleActionModel.PlayCard(visibleFirst.State!, new(2, 0));
        Require(visibleFirst.Supported && visibleSecond.Supported && visibleFirst.State!.Spawn.Train.Context!.Gold == 20 &&
            visibleSecond.State!.Spawn.Train.Context!.Gold == 25 &&
            visibleSecond.State.Spawn.Train.Rooms[0].Units[0].Triggers.Select(trigger => trigger.HasTriggered).SequenceEqual([true, false, true]),
            "Incant lost repeat/once flags or visible/ignored silence behavior.");
        var purify = new CombatStatus("purify", 1, stackable: false, hidden: false, displayCategory: "Positive");
        var purified = BattleActionModel.PlayCard(Root(Null("Spell"), [Actor(10, CombatTeam.Player, purify), Actor(11, CombatTeam.Enemy)]), new(1, 0));
        Require(purified.Supported && purified.State!.Spawn.Train.Context!.Gold == 5 &&
            !purified.State.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 10).Triggers[0].HasTriggered,
            "Purify failed to reject a new Incant callback.");
        var born = Actor(0, CombatTeam.Player);
        var spellSummonRule = new CardPlayRule("summon", "spell-summon", 0, "SpawnMonster", "Standby", born, [],
            cardType: "Spell", isAnyAbility: false);
        var spellSummon = BattleActionModel.PlayCard(Root(spellSummonRule), new(1, 0, 1));
        Require(spellSummon.Supported && spellSummon.State!.Spawn.Train.Context!.Gold == 10 &&
            !spellSummon.State.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 20).Triggers[0].HasTriggered,
            "A summon-effect spell lost Incant classification or included its new birth: " + spellSummon.Reason);
        var movedTrain = root.Spawn.Train;
        var movedContext = movedTrain.Context!;
        movedTrain = new([new(0, false, [Actor(11, CombatTeam.Enemy)], [], movedContext),
            new(1, false, [Actor(10, CombatTeam.Player), Actor(12, CombatTeam.Player)], [], movedContext),
            movedTrain.Rooms[2], movedTrain.Rooms[3]], [], 7, movedContext);
        var moved = CardPlayedTriggerModel.Spell(movedTrain, CombatTeam.Player, [10]);
        Require(moved.Supported && moved.State!.Context!.Gold == 5 && moved.State.Rooms[1].Units.Single(unit => unit.Id == 10).Triggers[0].HasTriggered &&
            !moved.State.Rooms[1].Units.Single(unit => unit.Id == 12).Triggers[0].HasTriggered,
            "Movement changed cached Incant identity or included a new room's actor.");
        var purifier = new CombatUnit(10, "purifier", CombatTeam.Player, 0, 20, 20, false, false, false, [],
            [new("CardSpellPlayed", false, false, true, 1, [new("CardEffectAddStatusEffect", 0, 0, "", 0, [], false,
                action: new("AddStatus", "Room", 0, true, true, [purify]))], false)], size: 1);
        var batch = BattleActionModel.PlayCard(Root(Null("Spell"), [purifier, Actor(13, CombatTeam.Player), Actor(11, CombatTeam.Enemy)]), new(1, 0));
        Require(batch.Supported && batch.State!.Spawn.Train.Context!.Gold == 5 &&
            batch.State.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 13).Triggers[0].HasTriggered &&
            !batch.State.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 11).Triggers[0].HasTriggered,
            "Incant did not admit the entire player batch before effects, or admitted enemies before player effects: " + batch.Reason);
        var resolved = CardModifierModel.Resolve(spellSummonRule.WithSpawn(born), new CardInstanceState(3, "summon",
            CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, []));
        Require(resolved.CardType == "Spell" && resolved.IsAnyAbility == false, "Modifiers/birth definition dropped card type or ability metadata.");
        var thresholdActor = new CombatUnit(10, "thresholds", CombatTeam.Player, 0, 20, 20, false, false, false, [],
            [Reward(5, once: true, fireCount: 2, threshold: 1), Reward(10, once: true, threshold: -2),
                Reward(3, fireCount: 3, threshold: 0), Reward(31, once: true, fireCount: 0, threshold: 0)], size: 1);
        var thresholdRoot = Root(Null("Spell"), [thresholdActor]);
        string thresholdParent = Serialize(thresholdRoot);
        var thresholdFirst = BattleActionModel.PlayCard(thresholdRoot, new(1, 0));
        var thresholdSecond = BattleActionModel.PlayCard(thresholdFirst.State!, new(2, 0));
        Require(thresholdFirst.Supported && thresholdSecond.Supported &&
            thresholdFirst.State!.Spawn.Train.Context!.Gold == 25 && thresholdSecond.State!.Spawn.Train.Context!.Gold == 40 &&
            thresholdSecond.State.Spawn.Train.Rooms[0].Units[0].Triggers.Select(trigger => trigger.HasTriggered).SequenceEqual([false, true, true, true]),
            "Ordinary Incant lost default-zero arguments, signed thresholds, per-repeat gold or zero-count once flags: " + thresholdFirst.Reason);
        var thresholdRoom = thresholdRoot.Spawn.Train.Rooms[0];
        RoomCombatResult Dispatch(int argument, int count, RoomCombatState room)
        {
            var request = new RoomCombatModel.QueuedCharacterTrigger(0, room.Units[0], "CardSpellPlayed", paramInt: argument,
                triggerCount: count, admission: RoomCombatModel.CharacterTriggerAdmission.Accepted);
            return RoomCombatModel.ApplyQueuedCharacterTrigger(room, request, _ => { });
        }
        var equal = Dispatch(1, 2, thresholdRoom);
        var above = Dispatch(2, 2, thresholdRoom);
        var negativeArgument = Dispatch(-9, 1, thresholdRoom);
        Require(equal.Supported && above.Supported && equal.State!.Context!.Gold == 70 && above.State!.Context!.Gold == 70 &&
            equal.State.Units[0].Triggers.All(trigger => trigger.HasTriggered), "Incant equality/above threshold or multiplicative repeat counts differ.");
        Require(negativeArgument.Supported && negativeArgument.State!.Context!.Gold == 25 &&
            negativeArgument.State.Units[0].Triggers.Select(trigger => trigger.HasTriggered).SequenceEqual([false, true, true, true]),
            "A negative argument incorrectly gated a non-positive Incant threshold.");
        foreach (int count in new[] { 0, -2 })
        {
            var empty = Dispatch(1, count, thresholdRoom);
            Require(empty.Supported && empty.State!.Context!.Gold == 0 && empty.State.Units[0].Triggers.All(trigger => trigger.HasTriggered),
                "Zero/negative Incant counts did not mark once-only triggers before the empty effect loop.");
            var repeated = Dispatch(1, 1, empty.State!);
            Require(repeated.Supported && repeated.State!.Context!.Gold == 15, "Incant replay after an empty batch repeated spent once effects.");
        }
        Parallel.For(0, 32, _ => Require(Serialize(Dispatch(1, 2, thresholdRoom).State) == Serialize(equal.State), "Parallel threshold Incant branches differ."));
        Require(Serialize(thresholdRoot) == thresholdParent, "Threshold Incant changed its parent.");
        Console.WriteLine("INCANT-THRESHOLD-CHECKS PASS: default-zero/signed arguments, positive/equal/above thresholds, per-repeat gold, zero/negative counts, consumed once flags and 32 branches.");
        string expected = Serialize(second.State);
        Parallel.For(0, 32, _ => Require(Serialize(BattleActionModel.PlayCard(BattleActionModel.PlayCard(root, new(1, 0)).State!, new(2, 0)).State) == expected,
            "Parallel Incant branches differed."));
        Require(Serialize(root) == parent, "Incant mutated its parent.");
        Console.WriteLine("INCANT-CHECKS PASS: actual card/ability types, empty and summon spells, cached identities, both team phases, once/silence/Purify, metadata guards and 32 branches.");
    }
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out var scenario) || scenario.GetString() is not ("incant" or "incant-thresholds" or "incant-relic" or "incant-relic-combined")) return;
        var phases = fixture.GetProperty("IncantPhases").EnumerateArray().ToArray();
        var triggers = fixture.GetProperty("IncantTriggers").EnumerateArray().ToArray();
        var admissions = fixture.GetProperty("PurifyQueueAdmissions").EnumerateArray()
            .Where(record => record.GetProperty("Kind").GetString() == "CardSpellPlayed").ToArray();
        Require(phases.Length >= 10 && triggers.Length > 5 && admissions.Length > triggers.Length,
            "Native Incant lacks complete team, actor or original admission boundaries.");
        Require(phases.Select(record => record.GetProperty("Team").Deserialize<CombatTeam>()).Distinct().Count() == 2 &&
            triggers.Select(record => record.GetProperty("Actor").Deserialize<CombatUnit>()!.Team).Distinct().Count() == 2,
            "Native Incant did not fire on both teams.");
        foreach (var phase in phases) VerifyPhase(phase);
        foreach (var trigger in triggers) VerifyTrigger(trigger);
        foreach (var admission in admissions) VerifyAdmission(admission);
        int rejected = admissions.Count(record => record.GetProperty("Purified").GetBoolean() &&
            record.GetProperty("QueueBefore").GetInt32() == record.GetProperty("QueueAfter").GetInt32());
        Require(rejected > 0, "Native Incant lacks an actual Purify admission rejection.");
        var actors = triggers.Select(record => record.GetProperty("Actor").Deserialize<CombatUnit>()!).ToArray();
        var afterActors = triggers.Select(record => record.GetProperty("AfterActor").Deserialize<CombatUnit>()!).ToArray();
        if (scenario.GetString() == "incant-thresholds")
        {
            Require(triggers.All(record => record.GetProperty("ParamInt").GetInt32() == 0 && record.GetProperty("TriggerCount").GetInt32() == 1),
                "Native ordinary Incant did not preserve default-zero arguments and one queued batch.");
            var thresholds = afterActors.SelectMany(actor => actor.Triggers).Where(trigger => trigger.Kind == "CardSpellPlayed").ToArray();
            bool Gold(CombatTrigger trigger, int value) => trigger.Effects.Any(effect => effect.Type == "CardEffectRewardGold" && effect.Value == value);
            Require(thresholds.Any(trigger => trigger.TriggerAtThreshold == 1 && Gold(trigger, 23)) &&
                thresholds.Where(trigger => trigger.TriggerAtThreshold > 0).All(trigger => !trigger.HasTriggered) &&
                thresholds.Any(trigger => trigger.TriggerAtThreshold == 0 && Gold(trigger, 31) && trigger.HasTriggered) &&
                thresholds.Any(trigger => trigger.TriggerAtThreshold == -3 && Gold(trigger, 13) && trigger.Once && trigger.HasTriggered) &&
                actors.Any(actor => actor.Triggers.Any(trigger => trigger.Kind == "CardSpellPlayed" && trigger.TriggerAtThreshold == -3 &&
                    Gold(trigger, 13) && trigger.Once && !trigger.HasTriggered)) &&
                actors.Any(actor => actor.Triggers.Any(trigger => trigger.Kind == "CardSpellPlayed" && trigger.TriggerAtThreshold == -3 &&
                    Gold(trigger, 13) && trigger.Once && trigger.HasTriggered)),
                "Native Incant threshold/negative-once/repeated-spent coverage is incomplete.");
            Console.WriteLine("NATIVE-INCANT-THRESHOLDS PASS: default-zero arguments, skipped positive thresholds, fired zero/negative thresholds and spent once-only replays.");
        }
        Require(actors.Any(actor => actor.Status("silenced")?.Stacks > 0 && actor.Triggers.Any(trigger =>
                trigger.Kind == "CardSpellPlayed" && !trigger.IgnoreSilence && !trigger.HasTriggered)) &&
            afterActors.Any(actor => actor.Status("silenced") == null && actor.Triggers.Any(trigger =>
                trigger.Kind == "CardSpellPlayed" && !trigger.IgnoreSilence && trigger.HasTriggered)) &&
            actors.Any(actor => actor.Triggers.Any(trigger => trigger.Kind == "CardSpellPlayed" && trigger.Once && trigger.HasTriggered)),
            "Native Incant lacks repeated once-only actors or the visible silence/removal transition.");
        Require(actors.Any(actor => actor.Triggers.Any(trigger => trigger.Kind == "CardSpellPlayed" &&
                trigger.Conditions?.RequiredStatuses.Contains("silenced") == true)) &&
            afterActors.Any(actor => actor.Status("silenced")?.Stacks > 0 && actor.Triggers.Any(trigger =>
                trigger.Kind == "CardSpellPlayed" && trigger.Conditions?.RequiredStatuses.Contains("silenced") == true && trigger.HasTriggered)),
            "Native Incant lacks a fired status-presence condition.");
        int emptySpells = 0;
        var actionRecords = fixture.GetProperty("Actions").EnumerateArray().ToArray();
        foreach (var record in actionRecords)
        {
            var before = record.GetProperty("Before").Deserialize<BattleTurnState>()!;
            int cardId = record.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            var card = before.Spawn.Train.Context!.Cards.Hand.Single(card => card.InstanceId == cardId);
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
            var cardPhases = phases.Where(phase => phase.GetProperty("CardId").GetInt32() == cardId).ToArray();
            if (rule.CardType == "Spell" && rule.IsAnyAbility == false && rule.Effect == "Null")
            {
                emptySpells++;
                Require(cardPhases.Length >= 2, "Paid empty-effect spell lost its native player/enemy phases.");
                var action = record.GetProperty("Action").Deserialize<PlayCardAction>()!;
                string parent = Serialize(before);
                var result = BattleActionModel.PlayCard(before, action);
                Require(result.Supported, "Paid empty spell unsupported: " + result.Reason);
                var actual = record.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(result.State!), BattleTurnChecks.Comparable(actual));
                Require(difference == null && result.Outcome == record.GetProperty("ActualOutcome").Deserialize<RoomOutcome>(),
                    "Paid empty spell lost its complete Incant/routing outcome: " + difference);
                Require(Serialize(before) == parent, "Paid Incant spell mutated its parent.");
            }
            Require(cardPhases.All(phase => phase.GetProperty("CardType").GetString() == rule.CardType &&
                phase.GetProperty("IsAnyAbility").GetBoolean() == rule.IsAnyAbility),
                "Captured card rule disagrees with native type/ability classification.");
        }
        Require(emptySpells > 0, "Native Incant did not play an actual empty-effect spell.");
        foreach (var group in phases.GroupBy(phase => phase.GetProperty("CardId").GetInt32()))
        {
            var ordered = group.OrderBy(phase => phase.GetProperty("Sequence").GetInt32()).ToArray();
            Require(ordered.Length % 2 == 0, "An Incant card is missing a complete team pair.");
            for (int i = 0; i < ordered.Length; i += 2)
            {
                Require(ordered[i].GetProperty("Team").Deserialize<CombatTeam>() == CombatTeam.Player &&
                    ordered[i + 1].GetProperty("Team").Deserialize<CombatTeam>() == CombatTeam.Enemy,
                    "Native Incant did not drain players before admitting enemies.");
                Compare(ordered[i].GetProperty("After").Deserialize<TrainCombatState>(),
                    ordered[i + 1].GetProperty("Before").Deserialize<TrainCombatState>(), "Incant team continuity");
            }
        }
        Parallel.For(0, 32, _ =>
        {
            foreach (var phase in phases) VerifyPhase(phase);
            foreach (var trigger in triggers) VerifyTrigger(trigger);
            foreach (var admission in admissions) VerifyAdmission(admission);
        });
        Console.WriteLine($"NATIVE-INCANT-CHECKS PASS: {phases.Length} complete team phases, {triggers.Length} actor boundaries, " +
            $"{admissions.Length} original admissions/{rejected} Purify rejections, {emptySpells} paid empty spells, " +
            "once/repeat/silence/status children, exact card types, team continuity and 32 independent branches.");
    }
    internal static void VerifyPhase(FixtureValue record)
    {
        Require(record.GetProperty("Completed").GetBoolean(), "Incomplete native Incant team phase.");
        var before = record.GetProperty("Before").Deserialize<TrainCombatState>()!;
        string parent = Serialize(before);
        TrainCombatState predicted = before;
        if (record.GetProperty("CardType").GetString() == "Spell" && !record.GetProperty("IsAnyAbility").GetBoolean())
        {
            var result = CardPlayedTriggerModel.Spell(before, record.GetProperty("Team").Deserialize<CombatTeam>(),
                record.GetProperty("CachedUnitIds").Deserialize<int[]>()!, record.TryGetProperty("PrecedingCallbacks", out var prefix)
                    ? prefix.Deserialize<CardPlayedQueueEntry[]>()?.Select(entry => entry.ToQueued()).ToArray() : null);
            Require(result.Supported, "Independent Incant team phase unsupported: " + result.UnsupportedReason);
            predicted = result.State!;
        }
        else if (record.GetProperty("IsAnyAbility").GetBoolean())
        {
            var result = CardPlayedTriggerModel.Ability(before, record.GetProperty("Team").Deserialize<CombatTeam>(),
                record.GetProperty("CachedUnitIds").Deserialize<int[]>()!, record.GetProperty("ActivatorUnitId").GetInt32(),
                record.GetProperty("PrecedingCallbacks").Deserialize<CardPlayedQueueEntry[]>()?.Select(entry => entry.ToQueued()).ToArray());
            Require(result.Supported, "Independent ability Incant team phase unsupported: " + result.UnsupportedReason);
            predicted = result.State!;
        }
        Compare(predicted, record.GetProperty("After").Deserialize<TrainCombatState>(), "Incant team phase");
        Require(Serialize(before) == parent, "Incant team phase mutated its parent.");
    }
    internal static void VerifyTrigger(FixtureValue record)
    {
        Require(record.GetProperty("Completed").GetBoolean(), "Incomplete native Incant actor dispatch.");
        var before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var actor = record.GetProperty("Actor").Deserialize<CombatUnit>()!;
        string parent = Serialize(new { before, actor });
        var queued = new RoomCombatModel.QueuedCharacterTrigger(before.RoomIndex, actor, "CardSpellPlayed",
            paramInt: record.GetProperty("ParamInt").GetInt32(), canFireTriggers: record.GetProperty("CanFire").GetBoolean(),
            triggerCount: record.GetProperty("TriggerCount").GetInt32(), admission: RoomCombatModel.CharacterTriggerAdmission.Accepted);
        var result = RoomCombatModel.ApplyQueuedCharacterTrigger(before, queued, _ => { });
        Require(result.Supported, "Independent Incant actor dispatch unsupported: " + result.UnsupportedReason);
        Compare(result.State, record.GetProperty("After").Deserialize<RoomCombatState>(), "Incant actor room");
        Compare(queued.Unit, record.GetProperty("AfterActor").Deserialize<CombatUnit>(), "Incant actor");
        Require(Serialize(new { before, actor }) == parent, "Incant dispatch mutated its parent.");
    }
    internal static void VerifyAdmission(FixtureValue record)
    {
        Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("Interactions").GetArrayLength() == 0,
            "Incomplete native Incant admission request.");
        var actor = record.GetProperty("Actor").Deserialize<CombatUnit>()!;
        string parent = Serialize(actor);
        bool purified = record.GetProperty("Purified").GetBoolean();
        Require(purified == (actor.Status("purify")?.Stacks > 0), "Incant native Purify query differs from its status.");
        string overload = record.GetProperty("Overload").GetString()!;
        Require(overload is "Character" or "QueueData", "Unknown Incant queue overload.");
        string[] purify = record.GetProperty("PurifyBlockedTriggers").Deserialize<string[]>()
            ?? throw new InvalidDataException("Incant admission lacks Purify balance rules.");
        string[] deployment = record.GetProperty("DeploymentBlockedTriggers").Deserialize<string[]>()
            ?? throw new InvalidDataException("Incant admission lacks deployment balance rules.");
        bool blockedDeployment = record.GetProperty("Turn").GetInt32() == 0 && deployment.Contains("CardSpellPlayed");
        bool blockedPurify = overload == "Character" && purified && purify.Contains("CardSpellPlayed");
        int delta = record.GetProperty("QueueAfter").GetInt32() - record.GetProperty("QueueBefore").GetInt32();
        Require(delta == (blockedDeployment || blockedPurify ? 0 : 1), "Native Incant queue delta differs from its admission rules.");
        if (!blockedDeployment)
        {
            var rng = UnityRng.Seed(173);
            var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10, purifyBlockedTriggers: purify);
            var request = new RoomCombatModel.QueuedCharacterTrigger(0, actor, "CardSpellPlayed",
                admission: overload == "QueueData" ? RoomCombatModel.CharacterTriggerAdmission.Accepted : RoomCombatModel.CharacterTriggerAdmission.Pending);
            string? error = StatusCallbackModel.Admit(context, request, out var admitted);
            Require(error == null && (admitted.Admission == RoomCombatModel.CharacterTriggerAdmission.Rejected) == (delta == 0),
                "Independent Incant admission differs from native: " + error);
        }
        Require(Serialize(actor) == parent, "Incant admission mutated its actor.");
    }
    private static void Compare<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(Serialize(predicted), Serialize(actual));
        Require(difference == null, label + ": " + difference);
    }
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
