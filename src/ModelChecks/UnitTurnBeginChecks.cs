using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class UnitTurnBeginChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(67);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 2, 10,
            statistics: BattleStatistics.Empty(deckCards: [1]).TrackCards([1]), cardInstances: [CardInstanceState.Empty(1, "unit")],
            otherPiles: [new("Standby", [new(1, "unit")], [0], []), new("Exhausted", [])]);
        CombatEffect Upgrade(string id, int damage, bool enemy = false) => new("CardEffectAddTempCardUpgradeToUnits", 0, 0, "", 0, [], false,
            unitUpgrade: new("UnitUpgrade", "Self", 0, enemy, !enemy, [],
                new(id, id, new(damage: damage), [], false, false, false, 0, 0, []), "TemporaryUntilUnitDeath"));
        CombatEffect Gold() => new("CardEffectRewardGold", 1, 0, "", 0, [], false);
        CombatTrigger Trigger(bool once, bool ignored, params CombatEffect[] effects) => new("OnTurnBegin", once, false, ignored, 1, effects, false);
        var ordinary = new[] { Trigger(false, false, Upgrade("repeat", 3)), Trigger(true, false, Upgrade("once", 2)), Trigger(true, false, Gold()) };
        var ignored = new[] { Trigger(false, true, Upgrade("ignored", 2), Gold()), Trigger(true, true, Upgrade("ignored-once", 1)) };
        CombatUnit Actor(CombatTrigger[] triggers, CombatStatus[]? statuses = null, bool canAttack = true) =>
            new(2, "actor", CombatTeam.Player, 0, 20, 20, canAttack, false, false, statuses ?? [], triggers, 1, 1,
                modifiers: new(0, 0, 0, 1, 1, true, false, []));
        var enemy = new CombatUnit(3, "enemy", CombatTeam.Enemy, 0, 100, 100, true, false, false, []);
        RoomCombatState Room(CombatUnit actor, bool deployment = false, bool preview = false, bool enemies = true) =>
            new(0, deployment, enemies ? [enemy, actor] : [actor], [], context, preview);
        var root = Room(Actor(ordinary)); string parent = JsonSerializer.Serialize(root);
        var first = RoomCombatModel.Exchange(root);
        Require(first.Supported && first.State!.Units.Single(unit => unit.Id == 2).BaseAttack == 5 &&
            first.State.Units.Single(unit => unit.Id == 3).Health == 95 && first.State.Context!.Gold == 5,
            "Zero attack prevented the turn trigger or the new attack used stale stats.");
        var enemyStep = RoomCombatModel.ApplyUnitTurn(root, 3);
        var playerStep = RoomCombatModel.ApplyUnitTurn(enemyStep.State!, 2);
        Require(playerStep.Supported && JsonSerializer.Serialize(playerStep.State) == JsonSerializer.Serialize(first.State) &&
            !RoomCombatModel.ApplyUnitTurn(root, 99).Supported,
            "Individual unit turns differed from their exchange or accepted a missing actor.");
        var second = RoomCombatModel.Exchange(first.State!);
        Require(second.State!.Units.Single(unit => unit.Id == 2).BaseAttack == 8 &&
            second.State.Units.Single(unit => unit.Id == 3).Health == 87 && second.State.Context!.Gold == 5,
            "Later turns lost repeated or once-only state.");
        var multi = RoomCombatModel.Exchange(Room(Actor(ordinary, [new("multistrike", 1, 2)])));
        Require(multi.State!.Units.Single(unit => unit.Id == 3).Health == 90 && multi.State.Context!.Gold == 5,
            "Turn triggers ran once per strike instead of once per unit turn.");
        var disabled = RoomCombatModel.Exchange(Room(Actor(ordinary, canAttack: false)));
        Require(disabled.State!.Units.Single(unit => unit.Id == 2).BaseAttack == 5 &&
            disabled.State.Units.Single(unit => unit.Id == 3).Health == 100 && disabled.State.Context!.Gold == 5,
            "An incapable attacker skipped turn triggers or attacked after its upgrade.");
        Require(RoomCombatModel.Exchange(Room(Actor(ordinary), enemies: false)).State!.Context!.Gold == 5,
            "An empty opposing team prevented turn triggers.");
        var dazedRoot = Room(Actor(ordinary.Concat(ignored).ToArray(), [new("dazed", 1, removeWhenTriggered: true)]));
        var dazed = RoomCombatModel.Exchange(dazedRoot);
        CombatUnit prevented = dazed.State!.Units.Single(unit => unit.Id == 2);
        Require(prevented.BaseAttack == 3 && prevented.Statuses.Count == 0 &&
            prevented.Triggers.Take(3).All(trigger => !trigger.HasTriggered) && prevented.Triggers.Skip(3).All(trigger => trigger.HasTriggered) &&
            dazed.State.Units.Single(unit => unit.Id == 3).Health == 100 && dazed.State.Context!.Gold == 5,
            "Dazed prevention ignored phase flags or restored this turn's attack.");
        var recovered = RoomCombatModel.Exchange(dazed.State);
        Require(recovered.State!.Units.Single(unit => unit.Id == 2).BaseAttack == 10 &&
            recovered.State.Units.Single(unit => unit.Id == 3).Health == 90 && recovered.State.Context!.Gold == 15,
            "The following turn retained dazed prevention or consumed skipped once triggers.");
        var endRemovedDaze = Room(Actor(ordinary.Concat(ignored).ToArray(), [new("dazed", 1, removeStackAtEnd: true)]));
        var preventedTurn = RoomCombatModel.ApplyUnitTurn(endRemovedDaze, 2);
        Require(preventedTurn.Supported && preventedTurn.State!.Units.Single(unit => unit.Id == 2).Statuses.Single().Id == "dazed" &&
            preventedTurn.State.Units.Single(unit => unit.Id == 2).BaseAttack == 3 &&
            preventedTurn.Events.All(item => item.Kind != "Attack") &&
            RoomCombatModel.Resolve(endRemovedDaze).State!.Units.Single(unit => unit.Id == 2).Statuses.Count == 0,
            "Native end-removed daze was consumed during the unit turn or did not clear after room combat.");
        var silent = RoomCombatModel.Exchange(Room(Actor(ordinary.Concat(ignored).ToArray(), [new("silenced", 1)])));
        Require(silent.State!.Units.Single(unit => unit.Id == 2).BaseAttack == 3 && silent.State.Units.Single(unit => unit.Id == 3).Health == 97,
            "Silence blocked ignored turn triggers or allowed ordinary ones.");
        var deployment = RoomCombatModel.Exchange(Room(Actor(ordinary, [new("dazed", 1, skipDuringDeployment: true)]), deployment: true));
        Require(deployment.State!.Units.Single(unit => unit.Id == 3).Health == 95 &&
            deployment.State.Units.Single(unit => unit.Id == 2).Statuses.Single().Stacks == 1, "Inactive deployment daze prevented triggers/attack.");
        var skipped = Actor([new("OnTurnBegin", true, false, true, 1, [Gold()], true)]);
        Require(!RoomCombatModel.Exchange(Room(skipped, deployment: true)).State!.Units.Single(unit => unit.Id == 2).Triggers.Single().HasTriggered,
            "A disallowed deployment turn trigger consumed its once flag.");
        var upgradedEnemy = new CombatUnit(3, "enemy", CombatTeam.Enemy, 0, 100, 100, true, false, false, [],
            [Trigger(false, false, Upgrade("enemy", 2, enemy: true))], modifiers: new(0, 0, 0, 1, 1, true, false, []));
        var enemyTurn = RoomCombatModel.Exchange(new(0, false, [upgradedEnemy, Actor([])], [], context));
        Require(enemyTurn.State!.Units.Single(unit => unit.Id == 2).Health == 18, "Enemy turn upgrades ran after attack conditions.");
        var despawn = Actor([Trigger(false, false, new CombatEffect("CardEffectDespawnCharacter", 1, 1, "", 0, [], false))]);
        var removed = RoomCombatModel.Exchange(Room(despawn));
        Require(removed.State!.Units.All(unit => unit.Id != 2) && removed.State.Context!.Statistics!.MonstersDeadThisBattle == 0 &&
            removed.State.Context.Statistics.Value(1, "TimesExhausted") == 1 &&
            removed.State.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Single().InstanceId == 1,
            "A turn-start despawn attacked or lost its source routing.");
        var preview = RoomCombatModel.Exchange(Room(Actor(ordinary), preview: true));
        Require(preview.State!.Units.Single(unit => unit.Id == 2).BaseAttack == 5 && preview.State.Context!.Gold == 0,
            "Turn preview skipped physical upgrades or awarded gold.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(RoomCombatModel.Exchange(root)) == JsonSerializer.Serialize(first) &&
            JsonSerializer.Serialize(RoomCombatModel.Exchange(dazedRoot)) == JsonSerializer.Serialize(dazed) &&
            JsonSerializer.Serialize(RoomCombatModel.ApplyUnitTurn(enemyStep.State!, 2).State) == JsonSerializer.Serialize(playerStep.State),
            "Parallel unit turns differed."));
        Require(JsonSerializer.Serialize(root) == parent, "Unit turns changed their parent.");
        Console.WriteLine("UNIT-TURN-BEGIN-CHECKS PASS: zero/incapable attacks, empty targets, phase prevention, ignored/silent/deployment gates, repeat/once/multistrike, enemy turns, despawn, preview and 32 parallel branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out FixtureValue scenario) || scenario.GetString() != "unit-turn-begin") return;
        int callbacks = 0, restricted = 0, scaling = 0, zeroGrowth = 0, dazed = 0, silent = 0, enemyTurns = 0, unitTurns = 0;
        foreach (FixtureValue sample in fixture.GetProperty("UnitUpgradeScaling").EnumerateArray())
        {
            Require(sample.GetProperty("TriggerKind").GetString() == "OnTurnBegin" &&
                sample.GetProperty("Difference").ValueKind == FixtureKind.Null && sample.GetProperty("CaptureError").ValueKind == FixtureKind.Null,
                "Native turn callback capture is incomplete.");
            CombatContext before = sample.GetProperty("Before").Deserialize<CombatContext>()!;
            CombatContext after = sample.GetProperty("After").Deserialize<CombatContext>()!;
            ScalingUnitUpgradeTrait trait = sample.GetProperty("Trait").Deserialize<ScalingUnitUpgradeTrait>()!;
            CardUpgradeModifier original = sample.GetProperty("BeforeUpgrade").Deserialize<CardUpgradeModifier>()!;
            CardUpgradeModifier actual = sample.GetProperty("AfterUpgrade").Deserialize<CardUpgradeModifier>()!;
            var predicted = UnitUpgradeScalingModel.ApplyTrait(before, trait, sample.GetProperty("OwnerCardId").GetInt32(), original, "OnTurnBegin");
            Require(predicted.Supported && JsonSerializer.Serialize(predicted.Upgrade) == JsonSerializer.Serialize(actual) &&
                JsonSerializer.Serialize(predicted.Context) == JsonSerializer.Serialize(after), "Independent native unit turn callback differs.");
            callbacks++; restricted += trait.Restriction == 1 ? 1 : 0;
            scaling += trait.Query.Type == "TurnCount" && actual.Stats.Damage > original.Stats.Damage ? 1 : 0;
        }
        foreach (FixtureValue stage in fixture.GetProperty("Stages").EnumerateArray())
        {
            if (stage.GetProperty("Kind").GetString() != "Exchange") continue;
            RoomCombatState before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState after = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            foreach (CombatUnit unit in before.Units)
            {
                CombatUnit? next = after.Units.FirstOrDefault(candidate => candidate.Id == unit.Id);
                if (next == null) continue;
                int Count(CombatUnit actor, string key) => actor.Modifiers?.Upgrades.Count(upgrade => upgrade.AssetKey == key) ?? 0;
                if (unit.Team == CombatTeam.Enemy && Count(next, "PojuTurnEnemy") > Count(unit, "PojuTurnEnemy")) enemyTurns++;
                if (unit.Team != CombatTeam.Player || !unit.Triggers.Any(trigger => trigger.Kind == "OnTurnBegin")) continue;
                zeroGrowth += unit.BaseAttack == 0 && next.BaseAttack > 0 ? 1 : 0;
                bool ignoredFired = Count(next, "PojuTurnIgnoredRepeat") > Count(unit, "PojuTurnIgnoredRepeat");
                bool ordinarySkipped = Count(next, "PojuTurnTemporary") == Count(unit, "PojuTurnTemporary");
                if (unit.Statuses.Any(status => status.Id == "silenced") && ignoredFired && ordinarySkipped) silent++;
            }
        }
        foreach (FixtureValue turn in fixture.GetProperty("UnitTurns").EnumerateArray())
        {
            RoomCombatState before = turn.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState after = turn.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            int id = turn.GetProperty("ActorId").GetInt32();
            RoomCombatResult predicted = RoomCombatModel.ApplyUnitTurn(before, id);
            Require(predicted.Supported && turn.GetProperty("Difference").ValueKind == FixtureKind.Null &&
                ModelJson.Difference(JsonSerializer.Serialize(predicted.State), JsonSerializer.Serialize(after)) == null &&
                predicted.Events.Where(item => item.Kind == "Attack" && item.Actor == id).Select(item => item.Target)
                    .SequenceEqual(turn.GetProperty("AttackedTargetIds").Deserialize<int[]>()!),
                "Independent native unit-turn state or direct attack sequence differs.");
            unitTurns++;
            CombatUnit unit = before.Units.Single(actor => actor.Id == id);
            CombatUnit? next = after.Units.FirstOrDefault(actor => actor.Id == id);
            if (next == null || !unit.Statuses.Any(status => status.Id == "dazed" && (!before.Deployment || !status.SkipDuringDeployment))) continue;
            int Count(CombatUnit actor, string key) => actor.Modifiers?.Upgrades.Count(upgrade => upgrade.AssetKey == key) ?? 0;
            if (Count(next, "PojuTurnIgnoredRepeat") > Count(unit, "PojuTurnIgnoredRepeat") &&
                Count(next, "PojuTurnTemporary") == Count(unit, "PojuTurnTemporary") &&
                turn.GetProperty("AttackedTargetIds").GetArrayLength() == 0) dazed++;
        }
        Require(callbacks >= 12 && restricted > 0 && scaling > 0 && zeroGrowth > 0 && dazed > 0 && silent > 0 && enemyTurns > 0,
            $"Native unit-turn coverage incomplete: callbacks={callbacks}, restricted={restricted}, scaling={scaling}, zero={zeroGrowth}, dazed={dazed}, silent={silent}, enemy={enemyTurns}.");
        Require(unitTurns > 0, "Native turn capture lacks complete per-unit states.");
        Console.WriteLine($"NATIVE-UNIT-TURN-BEGIN-CHECKS PASS: {callbacks} callbacks, {unitTurns} exact unit turns/attack sequences, {zeroGrowth} zero-attack recoveries, {dazed} dazed/ignored, {silent} silenced/ignored and {enemyTurns} enemy turns; complete states.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
