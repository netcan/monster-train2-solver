using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class DyingUpgradeChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(112);
        var owner = CardInstanceState.Empty(1, "owner");
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 2, 10,
            statistics: BattleStatistics.Empty().TrackCards([1]), cardInstances: [owner], cardRegistry: [owner],
            otherPiles: [new("Standby", [new(1, "owner")]), new("Exhausted", [])], killCamActivated: false);
        CardUpgradeModifier Upgrade(string id, int damage, int health, int armor = 0, int unhealed = 0, bool unique = false, bool excludeClone = false) =>
            new(id, id, new(damage: damage, health: health), armor == 0 ? [] : [new("armor", armor, 1, removeWhenTriggered: true)],
                false, unique, excludeClone, unhealed, 0, []);
        CombatEffect Effect(CardUpgradeModifier upgrade, string lifetime = "Permanent", bool remove = false) =>
            new(remove ? "CardEffectRemoveTempUpgradeFromUnit" : "CardEffectAddCardUpgradeToUnits", 0, 0, "", 0, [], false,
                unitUpgrade: new(remove ? "RemoveUnitUpgrade" : "UnitUpgrade", "Self", 0, false, true, [], upgrade, lifetime));
        CombatEffect Burst() => new("CardEffectDamage", 2, 0, "", 0, [], false,
            action: new("Damage", "Room", 2, true, false, []), damageStatusMultiplier: "armor");
        CombatTrigger Trigger(string kind, params CombatEffect[] effects) => new(kind, false, false, true, 1, effects, false, 0);
        CombatUnit Player(CombatTrigger[] triggers, int armor = 0, UnitModifiers? modifiers = null) =>
            new(1, "player", CombatTeam.Player, modifiers?.AttackDamage ?? 0, 2, 10, true, false, false,
                armor == 0 ? [] : [new("armor", armor, 1, removeWhenTriggered: true)], triggers, 1, 1,
                modifiers: modifiers ?? new(0, 0, 0, 1, 1, true, false, []), isBoss: false);
        var enemy = new CombatUnit(4, "enemy", CombatTeam.Enemy, 0, 30, 30, true, false, false, [], isBoss: false);
        RoomCombatState Room(CombatUnit player, CombatContext? ctx = null) => new(0, false, [enemy, player], [], ctx ?? context);
        RoomCombatResult Kill(RoomCombatState room) => RoomCombatModel.ApplyCardDamage(room, 1, 999);
        var positiveRoot = Room(Player([Trigger("OnHit", Effect(Upgrade("revenge", 2, 3, 2))),
            Trigger("OnDeath", Effect(Upgrade("death", 3, 4, 3), "TemporaryUntilEndOfBattle"), Burst())]));
        string parent = JsonSerializer.Serialize(positiveRoot);
        var positive = Kill(positiveRoot);
        CardInstanceState Source(RoomCombatResult result) => result.State!.Context!.CardInstances!.Single();
        Require(positive.Supported && positive.State!.Units.Single().Health == 20 && Source(positive).Permanent.Upgrades.Single().DataId == "revenge" &&
            Source(positive).Temporary.Upgrades.Single().DataId == "death" && positive.State.Context!.Statistics!.MonstersDeadThisBattle == 1 &&
            positive.State.Context.OtherPiles!.Single(p => p.Name == "Exhausted").Cards.Single().InstanceId == 1,
            "Positive dying upgrades lost source writes/statuses, revived the actor, or repeated death: " + positive.UnsupportedReason + " " + JsonSerializer.Serialize(positive));
        // The hit consumed the starting armor. The failed negative step must skip both
        // its unhealed HP and armor additions; only the later complete death upgrade writes back.
        var partialRoot = Room(Player([Trigger("OnHit", Effect(Upgrade("failed", 4, -1, 7, 5))),
            Trigger("OnDeath", Effect(Upgrade("complete", 1, 2, 2)), Burst())]));
        var partial = Kill(partialRoot);
        Require(partial.Supported && partial.State!.Units.Single().Health == 26 && Source(partial).Permanent.Upgrades.Single().DataId == "complete" &&
            partial.State.Context!.Statistics!.MonstersDeadThisBattle == 1, "Failed dying upgrade applied later statuses/source changes or repeated sacrifice.");
        var unhealed = Kill(Room(Player([Trigger("OnHit", Effect(Upgrade("failed-unhealed", 4, 3, 7, -1))),
            Trigger("OnDeath", Effect(Upgrade("complete", 1, 2, 2)), Burst())])));
        Require(unhealed.Supported && unhealed.State!.Units.Single().Health == 26 && Source(unhealed).Permanent.Upgrades.Single().DataId == "complete",
            "Negative unhealed HP on a dying actor did not stop its upgrade.");

        var temp = Upgrade("temporary", 2, 2, 2);
        var modifiedOwner = new CardInstanceState(1, "owner", CardModifiers.Empty(), new(new(), [temp, temp], 0, []), 0, 0, 0, []);
        var tempContext = new CombatContext(context.Cards, rng, 0, 2, 10, statistics: context.Statistics,
            cardInstances: [modifiedOwner], cardRegistry: [modifiedOwner], otherPiles: context.OtherPiles);
        var removal = Kill(Room(Player([Trigger("OnHit", Effect(Upgrade("revenge", 3, 1, 5))),
            Trigger("OnDeath", Effect(temp, remove: true), Burst())], modifiers: new(4, 4, 0, 1, 1, true, false, [temp, temp])), tempContext));
        Require(removal.Supported && Source(removal).Temporary.Upgrades.Count == 0 && Source(removal).Permanent.Upgrades.Single().DataId == "revenge" &&
            removal.State!.Units.Single().Health == 20 && removal.Events.Count(e => e.Kind == "Death") == 1,
            "Dying removal failed to clear every source copy or removed statuses after its early HP exit.");
        var unique = Upgrade("unique", 1, 2, 9, unique: true);
        var uniqueResult = Kill(Room(Player([Trigger("OnDeath", Effect(unique, "TemporaryUntilEndOfBattle"))],
            modifiers: new(0, 0, 0, 1, 1, true, false, [unique]))));
        Require(uniqueResult.Supported && Source(uniqueResult).Temporary.Upgrades.Single().DataId == "unique",
            "Unique dying unit no-op skipped the independent source-card update.");
        var clone = Kill(Room(Player([Trigger("OnDeath", Effect(Upgrade("excluded", 1, 2, excludeClone: true)))],
            modifiers: new(0, 0, 0, 1, 1, true, true, []))));
        Require(clone.Supported && Source(clone).Permanent.Upgrades.Count == 0, "Clone exclusion wrote a dying source upgrade.");
        var unitOnly = Kill(Room(Player([Trigger("OnDeath", Effect(Upgrade("unit-only", 1, 2), "TemporaryUntilUnitDeath"))])));
        Require(unitOnly.Supported && Source(unitOnly).Permanent.Upgrades.Count == 0 && Source(unitOnly).Temporary.Upgrades.Count == 0,
            "A unit-death lifetime upgrade escaped into the spawner.");
        var preview = Kill(new(0, false, positiveRoot.Units, [], context, preview: true));
        Require(preview.Supported && preview.State!.Context!.CardInstances!.Single().Permanent.Upgrades.Count == 0 &&
            preview.State.Context.CardInstances.Single().Temporary.Upgrades.Count == 0, "Dying preview upgrades changed live card metadata.");

        var bossDamage = new CombatEffect("CardEffectDamage", 9, 0, "", 0, [], false, action: new("Damage", "Room", 9, false, true, []));
        var boss = new CombatUnit(4, "boss", CombatTeam.Enemy, 0, 1, 1, true, false, true, [],
            [Trigger("OnDeath", bossDamage)], modifiers: new(0, 0, 0, 1, 1, true, false, []), isBoss: true);
        var terminal = RoomCombatModel.ApplyUnitTurn(new(0, false, [boss,
            new(1, "player", CombatTeam.Player, 2, 2, 10, true, false, false, [],
                [Trigger("OnDeath", Effect(Upgrade("after-clear", 1, 2)))], 1, 1,
                modifiers: new(2, 0, 0, 1, 1, true, false, []), isBoss: false)], [], context), 1);
        Require(terminal.Supported && terminal.State!.Context!.KillCamActivated == true && terminal.State.Context.CardInstances!.Count == 0 &&
            terminal.State.Context.CardRegistry!.Single().Permanent.Upgrades.Single().DataId == "after-clear",
            "Terminal clearing or BattleWon skipped a dying retained-spawner upgrade.");
        var phase = RoomCombatModel.ApplyPreCombat(Room(Player([Trigger("PreCombat",
            new CombatEffect("CardEffectDamage", 99, 0, "", 0, [], false, action: new("Damage", "Self", 99, false, true, []))),
            Trigger("OnKill", Effect(Upgrade("slay", 1, 1))), Trigger("OnHit", Effect(Upgrade("revenge", 1, 1))),
            Trigger("OnDeath", Effect(Upgrade("death", 1, 1)))])), 1);
        Require(phase.Supported && Source(phase).Permanent.Upgrades.Select(u => u.DataId).SequenceEqual(["slay", "revenge", "death"]),
            "Global dying Slay/Revenge/death dispatch lost its actor or source upgrades.");
        var lifestealBurst = new CombatEffect("CardEffectDamage", 1, 0, "", 0, [], false,
            action: new("Damage", "Room", 1, true, false, []), damageStatusMultiplier: "lifesteal");
        var dyingSweep = RoomCombatModel.ApplyUnitTurn(new(0, false,
            [new(4, "spikes", CombatTeam.Enemy, 0, 30, 30, true, false, false, [new("spikes", 99, 1)], isBoss: false),
             new(5, "back", CombatTeam.Enemy, 0, 30, 30, true, false, false, [], isBoss: false),
             new(1, "sweep", CombatTeam.Player, 8, 2, 10, true, false, false,
                [new("sweep", 1), new("lifesteal", 2, removeWhenTriggered: true)],
                [Trigger("OnDeath", lifestealBurst)], 1, 1, modifiers: new(8, 0, 0, 1, 1, true, false, []), isBoss: false)], [], context), 1);
        Require(dyingSweep.Supported && dyingSweep.State!.Units.All(unit => unit.Health == 22) &&
            dyingSweep.Events.Count(e => e.Kind == "Attack") == 2 && dyingSweep.Events.Count(e => e.Kind == "Spikes") == 1 &&
            dyingSweep.Events.Count(e => e.Kind == "Lifesteal") == 1,
            "Dying sweep lost attack damage/lifesteal consumption, revived its actor or applied more retaliation.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(Kill(positiveRoot)) == JsonSerializer.Serialize(positive) &&
            JsonSerializer.Serialize(Kill(partialRoot)) == JsonSerializer.Serialize(partial), "Parallel dying upgrades differ."));
        Require(JsonSerializer.Serialize(positiveRoot) == parent, "Dying upgrade simulation mutated its parent.");
        Console.WriteLine("DYING-UPGRADE PASS: positive/partial HP changes, dead statuses, permanent/battle/unit lifetimes, duplicate removals, unique/clone rules, retained terminal spawners, global callbacks, preview and 32 parallel branches.");
    }
    internal static void Native(JsonElement fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out var scenario) || scenario.GetString() is not ("dying-upgrades" or "attack-triggers")) return;
        int dying = 0, slays = 0, hits = 0, deaths = 0, removals = 0, negativeHp = 0, negativeUnhealed = 0, unitOnly = 0, turns = 0;
        foreach (JsonElement record in fixture.GetProperty("DyingUpgrades").EnumerateArray())
        {
            Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("Actual").ValueKind == JsonValueKind.Object &&
                record.GetProperty("Interactions").GetArrayLength() == 0, "Native dying upgrade capture is incomplete or unsupported.");
            CombatUnit[] targets = record.GetProperty("BeforeUnits").Deserialize<CombatUnit[]>(ModelJson.Options)!;
            if (targets.Length != 1 || targets[0].Health != 0) continue;
            var before = record.GetProperty("Before").Deserialize<RoomCombatState>(ModelJson.Options)!;
            var after = record.GetProperty("Actual").Deserialize<RoomCombatState>(ModelJson.Options)!;
            var actual = record.GetProperty("ActualUnits").Deserialize<CombatUnit[]>(ModelJson.Options)!.Single();
            var effect = record.GetProperty("Effect").Deserialize<CardActionEffect>(ModelJson.Options)!;
            string kind = record.GetProperty("Kind").GetString()!;
            var scope = new RoomCombatState(before.RoomIndex, before.Deployment, before.Units.Concat(targets).ToArray(),
                before.ExternalInteractions, before.Context);
            CombatUnit projected = targets[0];
            RoomCombatResult result = UnitModifierModel.ApplyWithSettlement(scope, projected.Id, effect.Upgrade!, effect.Lifetime,
                effect.Type == "RemoveUnitUpgrade", null, record.GetProperty("OwnerCardId").GetInt32(), kind, (state, changed) =>
                {
                    projected = changed;
                    return new RoomCombatResult(new(state.RoomIndex, state.Deployment,
                        state.Units.Select(unit => unit.Id == changed.Id ? changed : unit).ToArray(), state.ExternalInteractions, state.Context),
                        RoomOutcome.Exchanged, 0, []);
                }, allowDyingTarget: true);
            string? difference = result.Supported ? ModelJson.Difference(JsonSerializer.Serialize(projected), JsonSerializer.Serialize(actual)) : result.UnsupportedReason;
            string? contextDifference = result.Supported ? ModelJson.Difference(JsonSerializer.Serialize(result.State!.Context), JsonSerializer.Serialize(after.Context)) : null;
            Require(result.Supported && difference == null && contextDifference == null,
                "Independent dying upgrade unit/source/context differs at sequence " + record.GetProperty("Sequence") + ": " + difference + contextDifference);
            dying++; slays += kind == "OnKill" ? 1 : 0; hits += kind == "OnHit" ? 1 : 0; deaths += kind == "OnDeath" ? 1 : 0;
            removals += effect.Type == "RemoveUnitUpgrade" ? 1 : 0;
            negativeHp += effect.Type != "RemoveUnitUpgrade" && effect.Upgrade!.Stats.Health < 0 && actual.MaxHealth < targets[0].MaxHealth &&
                actual.Modifiers!.Upgrades.Any(upgrade => upgrade.DataId == effect.Upgrade.DataId && upgrade.Stats.Health < 0) ? 1 : 0;
            negativeUnhealed += effect.Type != "RemoveUnitUpgrade" && effect.Upgrade!.UnhealedHealth < 0 ? 1 : 0;
            unitOnly += effect.Lifetime == "TemporaryUntilUnitDeath" ? 1 : 0;
        }
        foreach (JsonElement turn in fixture.GetProperty("UnitTurns").EnumerateArray())
        {
            var before = turn.GetProperty("Before").Deserialize<RoomCombatState>(ModelJson.Options)!;
            var after = turn.GetProperty("Actual").Deserialize<RoomCombatState>(ModelJson.Options)!;
            int actor = turn.GetProperty("ActorId").GetInt32();
            var result = RoomCombatModel.ApplyUnitTurn(before, actor);
            Require(result.Supported && turn.GetProperty("Difference").ValueKind == JsonValueKind.Null &&
                ModelJson.Difference(JsonSerializer.Serialize(result.State), JsonSerializer.Serialize(after)) == null &&
                result.Events.Where(e => e.Kind == "Attack" && e.Actor == actor).Select(e => e.Target)
                    .SequenceEqual(turn.GetProperty("AttackedTargetIds").Deserialize<int[]>()!), "Independent dying-upgrade unit turn differs.");
            turns++;
        }
        Require(dying >= 6 && slays > 0 && hits > 0 && deaths > 0 && removals > 0 && negativeHp > 0 && negativeUnhealed > 0 && unitOnly > 0 && turns > 0,
            "Native dying-upgrade coverage is incomplete.");
        Console.WriteLine($"NATIVE-DYING-UPGRADE PASS: {dying} exact dying unit/source contexts, {slays} Slay, {hits} Revenge, {deaths} death, {removals} removals, {negativeHp}/{negativeUnhealed} failed HP stages, {unitOnly} unit-only lifetimes, {turns} independent unit turns.");
    }
    private static void Require(bool condition, string error)
    { if (!condition) throw new InvalidOperationException(error); }
}
