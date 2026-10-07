using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class TeamTurnBeginChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(84);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 3, 10,
            statistics: BattleStatistics.Empty(deckCards: [1, 2]).TrackCards([1, 2]),
            cardInstances: [CardInstanceState.Empty(1, "unit"), CardInstanceState.Empty(2, "unit")],
            otherPiles: [new("Standby", [new(1, "unit"), new(2, "unit")], [0, 1], []), new("Exhausted", [])]);
        CombatEffect Upgrade(string id, int damage, bool room = false, bool enemy = false) => new("CardEffectAddTempCardUpgradeToUnits", 0, 0, "", 0, [], false,
            unitUpgrade: new("UnitUpgrade", room ? "Room" : "Self", 0, enemy, !enemy, [],
                new(id, id, new(damage: damage), [], false, false, false, 0, 0, []), "TemporaryUntilUnitDeath"));
        CombatEffect Gold() => new("CardEffectRewardGold", 1, 0, "", 0, [], false);
        CombatTrigger Trigger(CombatEffect effect, bool once = false, bool ignored = false) => new("OnTeamTurnBegin", once, false, ignored, 1, [effect], false);
        CombatUnit Player(int id, int attack, CombatTrigger[] triggers, CombatStatus[]? statuses = null) =>
            new(id, "unit", CombatTeam.Player, attack, 20, 20, true, false, false, statuses ?? [], triggers, id - 1, 1,
                modifiers: new(attack, 0, 0, 1, 1, true, false, []));
        var enemy = new CombatUnit(4, "enemy", CombatTeam.Enemy, 0, 100, 100, true, false, false, []);
        RoomCombatState Room(CombatUnit[] players, CombatUnit? hero = null, bool preview = false) => new(0, false,
            new[] { hero ?? enemy }.Concat(players).ToArray(), [], context, preview);
        var front = Player(2, 1, [Trigger(Upgrade("front", 2, room: true))]);
        var back = Player(3, 0, [Trigger(Upgrade("back", 3, room: true))]);
        var root = Room([front, back]); string parent = JsonSerializer.Serialize(root);
        var allFirst = RoomCombatModel.Exchange(root);
        Require(allFirst.Supported && allFirst.State!.Units.Single(unit => unit.Id == 4).Health == 89 &&
            allFirst.State.Units.Single(unit => unit.Id == 2).BaseAttack == 6 && allFirst.State.Units.Single(unit => unit.Id == 3).BaseAttack == 5,
            "Team triggers interleaved with attacks instead of completing the whole team first.");
        var enemyBegin = RoomCombatModel.ApplyTeamTurnBegin(root, CombatTeam.Enemy);
        var enemyTurn = RoomCombatModel.ApplyUnitTurn(enemyBegin.State!, 4);
        var playerBegin = RoomCombatModel.ApplyTeamTurnBegin(enemyTurn.State!, CombatTeam.Player);
        var frontTurn = RoomCombatModel.ApplyUnitTurn(playerBegin.State!, 2);
        var backTurn = RoomCombatModel.ApplyUnitTurn(frontTurn.State!, 3);
        Require(JsonSerializer.Serialize(backTurn.State) == JsonSerializer.Serialize(allFirst.State), "Team/unit phase decomposition differed from exchange.");
        var strongEnemy = new CombatUnit(4, "enemy", CombatTeam.Enemy, 0, 100, 100, true, false, false, [],
            [Trigger(Upgrade("hero", 3, enemy: true))], modifiers: new(0, 0, 0, 1, 1, true, false, []));
        var quick = Player(2, 1, [Trigger(Upgrade("after-ambush", 10))], [new("ambush", 1, removeWhenTriggered: true)]);
        var ambushRoot = Room([quick, Player(3, 0, [Trigger(Gold())])], strongEnemy);
        var ambush = RoomCombatModel.Exchange(ambushRoot);
        Require(ambush.State!.Units.Single(unit => unit.Id == 4).Health == 99 && ambush.State.Units.Single(unit => unit.Id == 2).Health == 17 &&
            ambush.State.Units.Single(unit => unit.Id == 2).BaseAttack == 11 && ambush.State.Context!.Gold == 5,
            "Ambush ran after team buffs, repeated in the regular phase, or enemy team buffs ran after attacks.");
        var onceRoot = Room([Player(2, 0, [Trigger(Upgrade("once", 2), once: true), Trigger(Gold())])]);
        var first = RoomCombatModel.ApplyTeamTurnBegin(onceRoot, CombatTeam.Player);
        var second = RoomCombatModel.ApplyTeamTurnBegin(first.State!, CombatTeam.Player);
        Require(second.State!.Units.Single(unit => unit.Id == 2).BaseAttack == 2 && second.State.Context!.Gold == 10,
            "Repeated team phases lost their once/repeat state.");
        var prevented = Room([Player(2, 0, [Trigger(Upgrade("ordinary", 2)), Trigger(Upgrade("ignored", 1), ignored: true)],
            [new("silenced", 1)])]);
        Require(RoomCombatModel.ApplyTeamTurnBegin(prevented, CombatTeam.Player).State!.Units.Single(unit => unit.Id == 2).BaseAttack == 1,
            "Team phase ignored silence or blocked its ignored-silence exception.");
        var dazed = Room([Player(2, 0, [Trigger(Upgrade("before-daze", 2))], [new("dazed", 1, removeStackAtEnd: true)])]);
        Require(RoomCombatModel.ApplyTeamTurnBegin(dazed, CombatTeam.Player).State!.Units.Single(unit => unit.Id == 2).BaseAttack == 2,
            "Unit-turn daze prevention leaked into the earlier team phase.");
        var deployed = new RoomCombatState(0, true, [Player(2, 0, [new("OnTeamTurnBegin", true, false, true, 1, [Gold()], true)])], [], context);
        Require(RoomCombatModel.ApplyTeamTurnBegin(deployed, CombatTeam.Player).State!.Context!.Gold == 0 &&
            !RoomCombatModel.ApplyTeamTurnBegin(root, (CombatTeam)99).Supported, "Team deployment/identity gates differ.");
        var despawn = Room([Player(2, 0, [Trigger(new("CardEffectDespawnCharacter", 1, 1, "", 0, [], false))]), Player(3, 0, [Trigger(Gold())])]);
        var removed = RoomCombatModel.ApplyTeamTurnBegin(despawn, CombatTeam.Player);
        Require(removed.State!.Units.All(unit => unit.Id != 2) && removed.State.Context!.Gold == 5 &&
            removed.State.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Single().InstanceId == 1,
            "Team snapshot lost later actors or removed-source routing.");
        var preview = RoomCombatModel.ApplyTeamTurnBegin(new(0, false, onceRoot.Units, [], context, preview: true), CombatTeam.Player);
        Require(preview.State!.Units.Single(unit => unit.Id == 2).BaseAttack == 2 && preview.State.Context!.Gold == 0,
            "Team preview lost upgrades or awarded live gold.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(RoomCombatModel.Exchange(root)) == JsonSerializer.Serialize(allFirst) &&
            JsonSerializer.Serialize(RoomCombatModel.Exchange(ambushRoot)) == JsonSerializer.Serialize(ambush), "Parallel team phases differed."));
        Require(JsonSerializer.Serialize(root) == parent, "Team phases changed their parent.");
        Console.WriteLine("TEAM-TURN-BEGIN-CHECKS PASS: whole-team-before-attacks, ambush/enemy/player ordering, phase decomposition, once/repeat, silent/dazed/deployment gates, removal, preview and 32 parallel branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out FixtureValue scenario) || scenario.GetString() != "team-turn-begin") return;
        int phases = 0, enemy = 0, player = 0, roomBuffs = 0, callbacks = 0, unitTurns = 0, ambush = 0;
        var records = new List<(int Sequence, CombatTeam Team, RoomCombatState Before)>();
        foreach (FixtureValue phase in fixture.GetProperty("TeamTurnBegins").EnumerateArray())
        {
            RoomCombatState before = phase.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState after = phase.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            CombatTeam team = (CombatTeam)phase.GetProperty("Team").GetInt32();
            var predicted = RoomCombatModel.ApplyTeamTurnBegin(before, team);
            Require(predicted.Supported && phase.GetProperty("Difference").ValueKind == FixtureKind.Null &&
                ModelJson.Difference(JsonSerializer.Serialize(predicted.State), JsonSerializer.Serialize(after)) == null,
                "Independent native team phase differs.");
            records.Add((phase.GetProperty("Sequence").GetInt32(), team, before));
            phases++; enemy += team == CombatTeam.Enemy ? 1 : 0; player += team == CombatTeam.Player ? 1 : 0;
            roomBuffs += after.Units.Count(unit => unit.Modifiers?.Upgrades.Any(upgrade => upgrade.AssetKey is "PojuTeamRoom" or "PojuTeamEnemyRoom") == true);
        }
        foreach (FixtureValue turn in fixture.GetProperty("UnitTurns").EnumerateArray())
        {
            RoomCombatState before = turn.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState after = turn.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            int id = turn.GetProperty("ActorId").GetInt32();
            var predicted = RoomCombatModel.ApplyUnitTurn(before, id);
            Require(predicted.Supported && turn.GetProperty("Difference").ValueKind == FixtureKind.Null &&
                ModelJson.Difference(JsonSerializer.Serialize(predicted.State), JsonSerializer.Serialize(after)) == null &&
                predicted.Events.Where(item => item.Kind == "Attack" && item.Actor == id).Select(item => item.Target)
                    .SequenceEqual(turn.GetProperty("AttackedTargetIds").Deserialize<int[]>()!), "Native team fixture unit turn/attack differs.");
            unitTurns++;
            if (before.Units.Single(unit => unit.Id == id).Statuses.All(status => status.Id != "ambush")) continue;
            int sequence = turn.GetProperty("Sequence").GetInt32();
            var heroPhase = records.Where(record => record.Team == CombatTeam.Enemy && record.Before.RoomIndex == before.RoomIndex &&
                record.Sequence > sequence).OrderBy(record => record.Sequence).FirstOrDefault();
            if (heroPhase.Before != null && records.Any(record => record.Team == CombatTeam.Player && record.Before.RoomIndex == before.RoomIndex &&
                record.Sequence > heroPhase.Sequence && record.Before.Context!.QueryFrame!.Turn == before.Context!.QueryFrame!.Turn)) ambush++;
        }
        foreach (FixtureValue sample in fixture.GetProperty("UnitUpgradeScaling").EnumerateArray())
        {
            CombatContext before = sample.GetProperty("Before").Deserialize<CombatContext>()!;
            CombatContext after = sample.GetProperty("After").Deserialize<CombatContext>()!;
            ScalingUnitUpgradeTrait trait = sample.GetProperty("Trait").Deserialize<ScalingUnitUpgradeTrait>()!;
            CardUpgradeModifier original = sample.GetProperty("BeforeUpgrade").Deserialize<CardUpgradeModifier>()!;
            CardUpgradeModifier actual = sample.GetProperty("AfterUpgrade").Deserialize<CardUpgradeModifier>()!;
            string kind = sample.GetProperty("TriggerKind").GetString()!;
            var predicted = UnitUpgradeScalingModel.ApplyTrait(before, trait, sample.GetProperty("OwnerCardId").GetInt32(), original, kind);
            Require(predicted.Supported && sample.GetProperty("Difference").ValueKind == FixtureKind.Null &&
                sample.GetProperty("CaptureError").ValueKind == FixtureKind.Null && JsonSerializer.Serialize(predicted.Upgrade) == JsonSerializer.Serialize(actual) &&
                JsonSerializer.Serialize(predicted.Context) == JsonSerializer.Serialize(after), "Native team callback differs.");
            callbacks += kind == "OnTeamTurnBegin" ? 1 : 0;
        }
        Require(phases > 0 && enemy > 0 && player > 0 && roomBuffs > 0 && callbacks >= 6 && unitTurns > 0 && ambush > 0,
            "Native team fixture lacks both teams, room buffs, scaling callbacks, exact unit turns or ambush phase order.");
        Console.WriteLine($"NATIVE-TEAM-TURN-BEGIN-CHECKS PASS: {phases} exact phases ({enemy} enemy/{player} player), {unitTurns} unit turns, {callbacks} team scaling callbacks and {ambush} ambush order records; complete states.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
