using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicChecks
{
    internal static void Run()
    {
        var effects = new[] { RelicModel.AbilityIncant };
        var relic = new CombatRelicState("ability-incant", "ability-incant", effects);
        effects[0] = "RelicEffectDamageOnUnitAbility";
        var relics = new[] { relic };
        var root = WithIncant(UnitAbilityChecks.Root(), relics);
        relics[0] = new("unknown", "unknown", ["UnknownEffect"]);
        string parent = Serialize(root);
        var action = new PlayCardAction(2, 0, activatorUnitId: 10);
        var result = UnitAbilityModel.Activate(root, action);
        Require(result.Supported && result.State!.Spawn.Train.Context!.Gold == 10 &&
            result.State.Spawn.Train.Context.Relics!.Single().EffectTypes.Single() == RelicModel.AbilityIncant &&
            result.State.Spawn.Train.Context.CardRegistry!.Single(card => card.InstanceId == 2).PlayedRoomUnitIds!.SequenceEqual([10, 11]) &&
            result.State.Spawn.Train.Rooms[0].Units.All(unit => unit.Triggers.Any(trigger => trigger.Kind == "CardSpellPlayed" && trigger.HasTriggered)) &&
            result.State.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 10).Ability!.Resolving == false,
            "Relic ability play lost both teams, native room cache, resolving settlement or descriptor isolation: " + result.Reason);
        foreach (CombatRelicState[]? absent in new CombatRelicState[]?[] { null, [], [new("empty", "empty", [])] })
        {
            var skipped = UnitAbilityModel.Activate(WithIncant(UnitAbilityChecks.Root(), absent), action);
            Require(skipped.Supported && skipped.State!.Spawn.Train.Context!.Gold == 0 &&
                skipped.State.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 10).Triggers.Any(trigger =>
                    trigger.Kind == "OnOwnAbilityActivated" && trigger.HasTriggered), "An absent marker relic changed Own or enabled Incant.");
        }
        var repeated = UnitAbilityModel.Activate(WithIncant(UnitAbilityChecks.Root(), [relic, relic]), action);
        Require(repeated.Supported && repeated.State!.Spawn.Train.Context!.Gold == 10,
            "Duplicate marker relics multiplied the boolean native query.");
        var unknown = WithIncant(UnitAbilityChecks.Root(), [new("unknown", "unknown", ["RelicEffectDamageOnUnitAbility"])]);
        Require(UnitAbilityModel.Activate(unknown, action) is { Supported: false, Rejection: ActionRejection.Unsupported } refusal &&
            refusal.Reason!.Contains("Unmodeled relic effect"), "An unimplemented relic was silently ignored without an external guard.");
        Require(RelicModel.Validate(root.Spawn.Train.Context!.WithRelics([new("", "empty", [])])) != null,
            "A missing relic identity was accepted.");
        var untyped = UnitAbilityModel.Activate(WithIncant(UnitAbilityChecks.Root(typed: false), [relic]), action);
        Require(!untyped.Supported && untyped.Reason!.Contains("captured Spell/ability classification"),
            "A relic ability guessed missing native card classification.");
        var preLethal = UnitAbilityModel.Activate(WithIncant(UnitAbilityChecks.Root(), [relic], preLethal: true), action);
        Require(preLethal.Supported && preLethal.State!.Spawn.Train.Context!.CardRegistry!.Single(card => card.InstanceId == 2)
            .PlayedRoomUnitIds!.SequenceEqual([10]), "Ability room cache was captured before PreOwn removed its enemy: " + preLethal.Reason);

        // Lower creation IDs enqueue Incant before the activator's Own callback.
        // Own and Incant of the activator then run in that order in the same batch.
        // Purify from the first callback cannot cancel accepted player callbacks,
        // but prevents enemy callbacks admitted by the later manager phase.
        var rng = UnityRng.Seed(721);
        var armor = new CombatStatus("armor", 1, stackable: true);
        var purify = new CombatStatus("purify", 1, stackable: false, hidden: false, displayCategory: "Positive");
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10,
            purifyBlockedTriggers: ["CardSpellPlayed"], relics: [relic]);
        var first = Actor(10, CombatTeam.Player, [new("CardSpellPlayed", false, false, true, 1,
            [new("CardEffectAddStatusEffect", 0, 0, "", 0, [], false,
                action: new("AddStatus", "Room", 0, true, true, [armor])),
             new("CardEffectAddStatusEffect", 0, 0, "", 0, [], false,
                action: new("AddStatus", "Room", 0, true, true, [purify]))], false)]);
        var own = Gold(5, "OnOwnAbilityActivated", ["purify"]);
        own = own.WithEffects(own.Effects.Concat([
            new CombatEffect("CardEffectRemoveStatusEffect", 0, 0, "", 0, [], false,
                action: new("RemoveStatus", "Self", -1, false, true, [purify])),
            new CombatEffect("CardEffectAddStatusEffect", 0, 0, "", 0, [], false,
                action: new("AddStatus", "Self", 0, false, true, [armor]))]).ToArray());
        var caster = Actor(12, CombatTeam.Player, [own, Gold(5, required: ["armor"])]);
        var other = Actor(13, CombatTeam.Player, [Gold(5)]);
        var enemy = Actor(11, CombatTeam.Enemy, [Gold(5)]);
        var newActor = Actor(14, CombatTeam.Player, [Gold(5)]);
        var orderRoot = new TrainCombatState([new(0, false, [caster, other, enemy, first], [], context),
            new(1, false, [newActor], [], context), new(2, false, [], [], context)], [], 7, context);
        string orderParent = Serialize(orderRoot);
        var players = CardPlayedTriggerModel.Ability(orderRoot, CombatTeam.Player, [10, 11, 12, 13], 12);
        var enemies = CardPlayedTriggerModel.Ability(players.State!, CombatTeam.Enemy, [10, 11, 12, 13], 12);
        Require(players.Supported && enemies.Supported && enemies.State!.Context!.Gold == 15 &&
            enemies.State.Rooms[0].Units.Single(unit => unit.Id == 12).Triggers.All(trigger => trigger.HasTriggered) &&
            !enemies.State.Rooms[0].Units.Single(unit => unit.Id == 11).Triggers[0].HasTriggered &&
            !enemies.State.Rooms[1].Units[0].Triggers[0].HasTriggered,
            "Own/Incant creation order, whole-team admission, later enemy Purify or cached membership differs: " +
            players.UnsupportedReason + "/" + enemies.UnsupportedReason + "; gold=" + enemies.State?.Context?.Gold + "; units=" +
            Serialize(enemies.State?.Rooms[0].Units.Select(unit => new { unit.Id, Flags = unit.Triggers.Select(trigger => trigger.HasTriggered) })));
        var prefixRoot = new TrainCombatState([new(0, false, [caster], [], context), new(1, false, [], [], context)], [], 7, context);
        var prefix = new RoomCombatModel.QueuedCharacterTrigger(0, caster, "OnOwnAbilityActivated",
            admission: RoomCombatModel.CharacterTriggerAdmission.Accepted);
        // An accepted prefix stays ahead of card-play admission and is not mutated.
        var prefixed = CardPlayedTriggerModel.Ability(prefixRoot, CombatTeam.Player, [12], 0, [prefix]);
        Require(prefixed.Supported && prefixed.State!.Context!.Gold == 0 && prefix.Unit.Triggers.All(trigger => !trigger.HasTriggered),
            "A preceding accepted callback changed its source or lost its position.");
        var statusActor = first.WithTriggers([new("OnStatusEffectChanged", false, false, true, 1, first.Triggers[0].Effects, false)]);
        var statusRoot = new TrainCombatState(orderRoot.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
            room.Units.Select(unit => unit.Id == statusActor.Id ? statusActor : unit).ToArray(), room.ExternalInteractions, context)).ToArray(), [], 7, context);
        var statusPrefix = new RoomCombatModel.QueuedCharacterTrigger(0, statusActor, "OnStatusEffectChanged",
            admission: RoomCombatModel.CharacterTriggerAdmission.Accepted);
        var ordinaryPrefix = CardPlayedTriggerModel.Spell(statusRoot, CombatTeam.Player, [12, 13], [statusPrefix]);
        Require(ordinaryPrefix.Supported && ordinaryPrefix.State!.Context!.Gold == 5 &&
            ordinaryPrefix.State.Rooms[0].Units.Single(unit => unit.Id == 13).Triggers[0].HasTriggered,
            "Ordinary Incant was admitted after draining a preexisting callback instead of joining its batch.");
        var moved = CardPlayedTriggerModel.Ability(orderRoot, CombatTeam.Player, [14], 12);
        Require(moved.Supported && moved.State!.Context!.Gold == 5 &&
            !moved.State.Rooms[0].Units.Single(unit => unit.Id == 12).Triggers[0].HasTriggered,
            "The activator incorrectly required selected-room membership or a cached actor was omitted.");
        var uncachedRoot = WithIncant(UnitAbilityChecks.Root(emptyCache: true), [relic]);
        var allocated = UnitAbilityModel.Activate(uncachedRoot, new(4, 0, activatorUnitId: 10));
        Require(allocated.Supported && allocated.State!.Spawn.Train.Context!.Relics!.Count == 1 &&
            uncachedRoot.Spawn.Train.Context!.AbilityCardCache!.Count == 0,
            "Ability-card allocation discarded the relic descriptor or mutated its parent.");
        string expected = Serialize(result.State);
        Parallel.For(0, 32, _ => {
            Require(Serialize(UnitAbilityModel.Activate(root, action).State) == expected, "Parallel relic ability branches differ.");
            var branch = CardPlayedTriggerModel.Ability(orderRoot, CombatTeam.Player, [10, 11, 12, 13], 12);
            Require(branch.Supported && branch.State!.Context!.Gold == 15, "Parallel interleaved queue branches differ.");
        });
        Require(Serialize(root) == parent && Serialize(orderRoot) == orderParent, "Relic ability simulation mutated its parent.");
        Console.WriteLine("RELIC-CHECKS PASS: captured marker query, duplicate/non-marker gates, cached skills, both managers, interleaved Own/Incant admission, Purify, immutable descriptors and 32 branches.");
    }
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out var scenario) || scenario.GetString() != "ability-incant") return;
        var phases = fixture.GetProperty("IncantPhases").EnumerateArray().ToArray();
        var triggers = fixture.GetProperty("IncantTriggers").EnumerateArray().ToArray();
        var admissions = fixture.GetProperty("PurifyQueueAdmissions").EnumerateArray()
            .Where(entry => entry.GetProperty("Kind").GetString() == "CardSpellPlayed").ToArray();
        var abilities = phases.Where(entry => entry.GetProperty("IsAnyAbility").GetBoolean()).ToArray();
        var actions = fixture.GetProperty("Actions").EnumerateArray().Where(entry =>
            entry.GetProperty("Action").GetProperty("ActivatorUnitId").GetInt32() > 0).ToArray();
        Require(abilities.Length >= 4 && triggers.Length > 0 && admissions.Length > 0 && actions.Length >= 2 &&
            actions.Select(entry => entry.GetProperty("Action").GetProperty("ActivatorUnitId").GetInt32()).Distinct().Count() >= 2 &&
            actions.Select(entry => entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).Distinct().Count() == 1,
            "Native ability Incant lacks two actual activators sharing one skill card.");
        foreach (var phase in phases)
        {
            var before = phase.GetProperty("Before").Deserialize<TrainCombatState>()!;
            Require(before.Context!.Relics != null && RelicModel.Validate(before.Context) == null &&
                RelicModel.AbilitiesTriggerIncant(before.Context), "Native phase omitted the actual marker relic or has unmodeled relic effects.");
            Require(phase.GetProperty("PrecedingCallbacks").ValueKind != FixtureKind.Null,
                "Native relic phase omitted its queued prefix.");
            IncantChecks.VerifyPhase(phase);
            if (phase.GetProperty("IsAnyAbility").GetBoolean())
            {
                int actorId = phase.GetProperty("ActivatorUnitId").GetInt32();
                var actor = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == actorId);
                Require(actor.Ability?.Resolving == true && phase.GetProperty("CardType").GetString() == "Spell",
                    "Native skill settled resolving before its team callbacks or lost Spell classification.");
            }
        }
        foreach (var fire in triggers) IncantChecks.VerifyTrigger(fire);
        foreach (var admission in admissions) IncantChecks.VerifyAdmission(admission);
        foreach (var sample in actions)
        {
            var before = sample.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var action = sample.GetProperty("Action").Deserialize<PlayCardAction>()!;
            string parent = Serialize(before);
            var result = UnitAbilityModel.Activate(before, action);
            Require(result.Supported, "Independent relic skill activation unsupported: " + result.Reason);
            var actual = sample.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(result.State!), BattleTurnChecks.Comparable(actual));
            Require(difference == null && result.Outcome == sample.GetProperty("ActualOutcome").Deserialize<RoomOutcome>(),
                "Complete native relic skill action differs: " + difference);
            Require(Serialize(before) == parent, "Relic skill action mutated its native input.");
            var cardPhases = abilities.Where(phase => phase.GetProperty("CardId").GetInt32() == action.CardInstanceId &&
                phase.GetProperty("ActivatorUnitId").GetInt32() == action.ActivatorUnitId).ToArray();
            Require(cardPhases.Any(phase => phase.GetProperty("Team").Deserialize<CombatTeam>() == CombatTeam.Player) &&
                cardPhases.Any(phase => phase.GetProperty("Team").Deserialize<CombatTeam>() == CombatTeam.Enemy),
                "Native relic skill lost a manager phase.");
        }
        Require(triggers.Any(entry => entry.GetProperty("Actor").Deserialize<CombatUnit>()!.Team == CombatTeam.Player) &&
            triggers.Any(entry => entry.GetProperty("Actor").Deserialize<CombatUnit>()!.Team == CombatTeam.Enemy),
            "Native relic Incant lacks both teams.");
        var ordered = phases.OrderBy(entry => entry.GetProperty("Sequence").GetInt32()).ToArray();
        Require(ordered.Length % 2 == 0, "Native relic card phase is missing a manager pair.");
        for (int i = 0; i < ordered.Length; i += 2)
        {
            Require(ordered[i].GetProperty("Team").Deserialize<CombatTeam>() == CombatTeam.Player &&
                ordered[i + 1].GetProperty("Team").Deserialize<CombatTeam>() == CombatTeam.Enemy,
                "Native relic card did not process player before enemy managers.");
            Require(ModelJson.Difference(Serialize(ordered[i].GetProperty("After").Deserialize<TrainCombatState>()),
                Serialize(ordered[i + 1].GetProperty("Before").Deserialize<TrainCombatState>())) == null,
                "Native relic manager continuity differs.");
        }
        Parallel.For(0, 32, _ => {
            foreach (var phase in phases) IncantChecks.VerifyPhase(phase);
            foreach (var trigger in triggers) IncantChecks.VerifyTrigger(trigger);
        });
        Console.WriteLine($"NATIVE-ABILITY-INCANT-CHECKS PASS: {actions.Length} real shared-skill activations, {abilities.Length} ability/{phases.Length} total team phases, " +
            $"{triggers.Length} exact Incant dispatches, native relic identities, resolving/cache timing, manager continuity and 32 immutable branches.");
    }
    private static BattleTurnState WithIncant(BattleTurnState source, IReadOnlyList<CombatRelicState>? relics, bool preLethal = false)
    {
        var context = source.Spawn.Train.Context!.WithRelics(relics);
        var skill = context.CardRegistry!.FirstOrDefault(card => card.DataId == "skill");
        if (skill != null) context = context.WithCard(skill.WithRoomCacheState([], []));
        var train = CardSpellModel.WithContext(source.Spawn.Train, context);
        train = new(train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
            room.Units.Select(unit => unit.IsPyre ? unit : unit.WithTriggers(unit.Triggers.Select(trigger =>
                preLethal && unit.Id == 10 && trigger.Kind == "OnPreOwnAbilityActivated" ? trigger.WithEffects(trigger.Effects.Concat([
                    new CombatEffect("CardEffectDamage", 999, 0, "", 0, [], false,
                        action: new("Damage", "FrontInRoom", 999, true, false, []))]).ToArray()) : trigger).Concat([Gold(5)]).ToArray())).ToArray(),
            room.ExternalInteractions, context, room.Preview)).ToArray(), train.Movement, train.EnemySlotsPerRoom, context);
        var old = source.Spawn;
        var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping, old.Rng, old.NextUnitId,
            old.Treasures, old.TreasuresRemaining, old.TreasureEnabled, old.FirstTreasureTurn, old.FirstTreasureRoom, old.Turn,
            old.ExternalInteractions, old.CanonicalDecisionReferences);
        return new(spawn, source.Energy, source.EnergyPerTurn, source.DrawPerTurn, source.ForgePoints, source.DragonsHoard, source.MoonPhase,
            source.RngStreams, source.OtherPiles, source.ExternalInteractions, source.PlayRules, source.BattlePreviewEnabled,
            source.UiRngIsolated, source.CanonicalDecisionReferences, source.CanonicalPhysicalReferences);
    }
    private static CombatUnit Actor(int id, CombatTeam team, CombatTrigger[] triggers) =>
        new(id, "queue-actor", team, 0, 20, 20, false, false, false, [], triggers, size: 1);
    private static CombatTrigger Gold(int value, string kind = "CardSpellPlayed", string[]? required = null) =>
        new(kind, false, false, true, 1, [new("CardEffectRewardGold", value, 0, "", 0, [], false)], false,
            conditions: required == null ? null : new(required, []));
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
