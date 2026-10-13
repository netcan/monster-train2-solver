using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class SacrificeChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(722);
        var card = CardInstanceState.Empty(8, "sacrifice-source");
        CombatContext Context(UnityRng battleRng, int nextUnitId, BattleSpawnPoints? points = null) => new(
            new CardCycleState([], [], [], battleRng, 0, []), battleRng, 0, 9, 10,
            statistics: BattleStatistics.Empty().TrackCards([8]), cardInstances: [card], cardRegistry: [card],
            allScenarioBossesDead: false, nextUnitId: nextUnitId, spawnPoints: points);
        CombatUnit Unit(int id, CombatTeam team, IReadOnlyList<CombatTrigger>? triggers = null,
            IReadOnlyList<string>? subtypes = null) => new(id, "sacrifice-" + id, team, 2, 5, 5, true, false, false, [],
                triggers, nextTriggerId: null, subtypes: subtypes,
                deathState: new UnitDeathState(false, false, true, isSacrifice: false, statisticsListenerOnce: true,
                    isDespawned: false, isDestroyed: false));
        CardActionEffect Effect(string target, bool doNotFullyRemove = false, string subtype = "") => new(
            "Sacrifice", target, 0, false, true, [], doNotFullyRemove: doNotFullyRemove, sacrificeSubtype: subtype);

        var death = new CombatTrigger("OnDeath", true, false, false, 1,
            [new("CardEffectRewardGold", 7, 0, "", 0, [], false)]);
        var target = Unit(2, CombatTeam.Player, [death]);
        var room = new RoomCombatState(0, false, [Unit(1, CombatTeam.Enemy), target], [], Context(rng, 3));
        RoomCombatResult killed = CardSpellModel.Apply(room, [Effect("DropTargetCharacter")], target.Id, 8);
        Require(killed.Supported && killed.State!.Units.Select(unit => unit.Id).SequenceEqual([1]) &&
            killed.State.Context!.Gold == 5 && killed.State.Context.Statistics!.Value(8, "LastSacrificedMonsterStats") == 1,
            $"CardEffectSacrifice death mismatch: supported={killed.Supported}, reason={killed.UnsupportedReason}, " +
            $"units={string.Join(',', killed.State?.Units.Select(unit => unit.Id) ?? [])}, " +
            $"gold={killed.State?.Context?.Gold}, last-sacrifice={killed.State?.Context?.Statistics?.Value(8, "LastSacrificedMonsterStats")}");

        CardActionEffect killEffect = new("Kill", "DropTargetCharacter", 0, false, true, [],
            tests: new CardEffectTests(true, false, false, false, mustHaveValidTargets: true));
        RoomCombatResult killedByKill = CardSpellModel.Apply(room, [killEffect], target.Id, 8);
        Require(killedByKill.Supported && killedByKill.State!.Units.Select(unit => unit.Id).SequenceEqual([1]) &&
            killedByKill.State.Context!.Gold == 5 && killedByKill.State.Context.Statistics!.Value(8, "LastSacrificedMonsterStats") == 1,
            $"CardEffectKill death mismatch: supported={killedByKill.Supported}, reason={killedByKill.UnsupportedReason}, " +
            $"units={string.Join(',', killedByKill.State?.Units.Select(unit => unit.Id) ?? [])}, gold={killedByKill.State?.Context?.Gold}");

        var emptyRoom = new RoomCombatState(0, false, [], [], Context(rng, 1));
        CardActionEffect optionalTargetKill = new("Kill", "Room", 0, true, true, [],
            tests: new CardEffectTests(true, false, false, false));
        CardActionEffect requiredTargetKill = new("Kill", "Room", 0, true, true, [],
            tests: new CardEffectTests(true, false, false, false, mustHaveValidTargets: true));
        SpellCastCheck emptyKillAllowed = CardSpellModel.TestPlay(emptyRoom, [optionalTargetKill], 0);
        SpellCastCheck emptyKillRejected = CardSpellModel.TestPlay(emptyRoom, [requiredTargetKill], 0);
        RoomCombatResult emptyKill = CardSpellModel.Apply(emptyRoom, [optionalTargetKill], 0, 8);
        Require(emptyKillAllowed.CanPlay && !emptyKillRejected.CanPlay && emptyKill.Supported && emptyKill.State!.Units.Count == 0,
            $"CardEffectKill no-target flag mismatch: optional={emptyKillAllowed.CanPlay}, required={emptyKillRejected.CanPlay}, " +
            $"apply-supported={emptyKill.Supported}, reason={emptyKill.UnsupportedReason}");

        SpawnPointReference point = new(0, CombatTeam.Player, 0);
        BattleSpawnPoints points = new([
            new SpawnPointGroupState(0, CombatTeam.Player, 1, 1, [1], [false]),
            new SpawnPointGroupState(0, CombatTeam.Enemy, 1, 1, [0], [false])
        ], [new UnitSpawnPointState(1, point, point)]);
        CombatUnit occupant = Unit(1, CombatTeam.Player);
        var occupiedRoom = new RoomCombatState(0, false, [occupant], [], Context(rng, 2, points));
        RoomCombatResult retained = CardSpellModel.Apply(occupiedRoom, [Effect("DropTargetCharacter", true)], 1, 8);
        Require(retained.Supported && retained.State!.Units.Count == 0 &&
            retained.State.Context!.SpawnPoints!.Group(0, CombatTeam.Player)!.Occupants[0] == 1 &&
            retained.State.Context.SpawnPoints.FirstEmpty(0, CombatTeam.Player) == -1,
            "DoNotFullyRemove did not retain the dead occupant's physical slot.");
        var retainedTrain = new TrainCombatState([retained.State!], [], 1, retained.State!.Context);
        BattleSpawnPoints afterFrame = TrainCombatModel.CompleteFrameRemovals(retainedTrain).Context!.SpawnPoints!;
        Require(afterFrame.Group(0, CombatTeam.Player)!.Occupants[0] == 1 &&
            afterFrame.Units.Single(unit => unit.UnitId == 1).Current == null &&
            afterFrame.Units.Single(unit => unit.UnitId == 1).LastKnown == null,
            "Canonical cleanup must clear the retired owner's pointers while retaining its DoNotFullyRemove slot.");
        RoomCombatResult removed = CardSpellModel.Apply(occupiedRoom, [Effect("DropTargetCharacter")], 1, 8);
        Require(removed.Supported && removed.State!.Context!.SpawnPoints!.FirstEmpty(0, CombatTeam.Player) == 0,
            "Ordinary sacrifice did not release its physical slot: " + removed.UnsupportedReason);

        var subtypeRng = UnityRng.Seed(913);
        CombatUnit[] candidates = [Unit(1, CombatTeam.Player, subtypes: ["imp"]), Unit(2, CombatTeam.Player, subtypes: ["imp"]),
            Unit(3, CombatTeam.Player, subtypes: ["steward"])];
        var randomRoom = new RoomCombatState(0, false, candidates, [], Context(subtypeRng, 5));
        var randomPyre = new CombatUnit(4, "pyre", CombatTeam.Player, 0, 80, 80, true, true, false, []);
        var train = new TrainCombatState([randomRoom, new RoomCombatState(1, false, [randomPyre], [], randomRoom.Context)], [], 4, randomRoom.Context);
        RngDraw expected = subtypeRng.Range(0, 2);
        TrainSpellResult random = CardSpellModel.Apply(train, 0, [Effect("RandomInRoom", subtype: "imp")], 0, 8);
        Require(random.Supported && random.TargetCollections.Single().UnitIds.Single() == candidates[expected.Value].Id &&
            random.State!.Rooms[0].Units.Any(unit => unit.Id == 3) &&
            JsonSerializer.Serialize(random.State.Context!.BattleRng) == JsonSerializer.Serialize(expected.State),
            $"Random sacrifice mismatch: supported={random.Supported}, reason={random.UnsupportedReason}, " +
            $"targets={string.Join(',', random.TargetCollections.SelectMany(item => item.UnitIds))}, expected={candidates[expected.Value].Id}, " +
            $"units={string.Join(',', random.State?.Rooms[0].Units.Select(unit => unit.Id) ?? [])}, " +
            $"actual-rng={JsonSerializer.Serialize(random.State?.Context?.BattleRng)}, expected-rng={JsonSerializer.Serialize(expected.State)}");

        var own = new CombatTrigger("OnOwnAbilityActivated", true, false, false, 1,
            [new("CardEffectRewardGold", 4, 0, "", 0, [], false)], skipDuringDeployment: false);
        var kill = new CombatTrigger("OnKill", true, false, false, 1,
            [new("CardEffectRewardGold", 6, 0, "", 0, [], false)], skipDuringDeployment: false);
        CombatUnit activator = Unit(1, CombatTeam.Player, [own, kill]);
        CombatContext abilityContext = Context(rng, 3);
        var abilityRoom = new RoomCombatState(0, false, [activator], [], abilityContext);
        var abilityPyre = new CombatUnit(2, "pyre", CombatTeam.Player, 0, 80, 80, true, true, false, []);
        var abilityTrain = new TrainCombatState([abilityRoom, new RoomCombatState(1, false, [abilityPyre], [], abilityContext)], [], 1, abilityContext);
        TrainSpellResult ability = CardSpellModel.Apply(abilityTrain, 0, [Effect("Self")], 0, 8, selfUnitId: 1);
        Require(ability.Supported && ability.State!.Rooms[0].Units.Count == 0 && ability.State.Context!.Gold == 10,
            $"Self-sacrifice callbacks mismatch: supported={ability.Supported}, reason={ability.UnsupportedReason}, " +
            $"room-units={string.Join(',', ability.State?.Rooms[0].Units.Select(unit => unit.Id) ?? [])}, gold={ability.State?.Context?.Gold}");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
