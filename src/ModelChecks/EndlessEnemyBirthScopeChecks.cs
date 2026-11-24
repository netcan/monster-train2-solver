using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EndlessEnemyBirthScopeChecks
{
    internal static void Run(FixtureValue witness)
    {
        var captured = witness.GetProperty("Deployment").Deserialize<BattleTurnState>()!;
        string oldGuard = $"Unmodeled relic effect {RelicModel.EndlessEnemyBirthEffect} on {RelicModel.EndlessEnemyBirthOwnerKey}.";
        Require(captured.ExternalInteractions.Contains(oldGuard) && captured.Spawn.ExternalInteractions.Contains(oldGuard),
            "Expected the original deployment recorded before the birth guard was scoped.");
        var source = captured.Spawn;
        var context = source.Train.Context!;
        CombatRelicState owner = context.Relics!.Single(RelicModel.IsOriginalEndlessEnemyBirth);
        Require(RelicModel.Validate([owner]) == null && RelicModel.Validate([owner, owner]) != null &&
            RelicModel.Validate([new CombatRelicState(owner.DataId, "UnexpectedOwner", owner.EffectTypes,
                isCovenant: false, disallowedInPlacementPhase: false)]) != null,
            "Only a single genuine original Endless enemy birth owner may pass settled relic validation.");

        // A fresh capture derives these lists from current relic validation.
        // Retain unrelated diagnostics when reading the older native archive.
        string[] Reproject(IReadOnlyList<string> interactions) => interactions.Where(reason => reason != oldGuard).ToArray();
        var rooms = source.Train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment, room.Units,
            Reproject(room.ExternalInteractions), context, room.Preview, room.ActiveCompanionBossIds)).ToArray();
        var train = new TrainCombatState(rooms, source.Train.Movement, source.Train.EnemySlotsPerRoom, context);
        var spawn = new EnemySpawnState(train, source.Waves, source.SelectedGroups, source.Phase, source.Looping, source.Rng,
            source.NextUnitId, source.Treasures, source.TreasuresRemaining, source.TreasureEnabled, source.FirstTreasureTurn,
            source.FirstTreasureRoom, source.Turn, Reproject(source.ExternalInteractions), source.CanonicalDecisionReferences,
            source.PendingDestroyedUnitIds, source.OuterBoss, source.QueuedOuterBoss, source.QueuedTrueFinalBoss, source.TrueFinalBoss);
        var opening = new BattleTurnState(spawn, captured.Energy, captured.EnergyPerTurn, captured.DrawPerTurn, captured.ForgePoints,
            captured.DragonsHoard, captured.MoonPhase, captured.RngStreams, captured.OtherPiles, Reproject(captured.ExternalInteractions),
            captured.PlayRules, captured.BattlePreviewEnabled, captured.UiRngIsolated, captured.CanonicalDecisionReferences,
            captured.CanonicalPhysicalReferences, captured.SelectedRoom, captured.MoonPhaseTrackingActive);
        using var keys = new BattleStateKeys();
        string key = keys.Key(opening);
        int stewardId = context.Cards.Hand.First(card => card.DataId == "d14a50f3-728d-43e1-87f0-ef1b013f6678").InstanceId;
        PlayCardAction action = BattleActionModel.EnumerateSupportedPlays(opening, stewardId).FirstOrDefault()
            ?? throw new InvalidOperationException("The original Train Steward lost its supported deployment.");
        BattleActionResult placed = BattleActionModel.PlayCard(opening, action);
        Require(placed.Supported && placed.State!.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Any(unit => unit.Team == CombatTeam.Player && unit.SpawnerCardId == stewardId),
            "Expected the original player birth to complete before the first unsupported wave.");
        BattleTurnResult end = BattleTurnModel.EndTurn(placed.State!);
        Require(!end.Supported && end.UnsupportedReason == RelicSpawnStatusModel.UnsupportedEndlessEnemyBirth,
            end.UnsupportedReason ?? "The new enemy wave must retain the original scaling guard.");
        Require(keys.Key(opening) == key, "The supported play or unsupported wave changed its immutable source.");

        CombatUnit hero = witness.GetProperty("FirstDecision").Deserialize<BattleTurnState>()!.Spawn.Train.Rooms
            .SelectMany(room => room.Units).First(unit => unit.Team == CombatTeam.Enemy);
        EnemyDefinition heroDefinition = source.Waves[0].Candidates.SelectMany(group => group.Units)
            .First(definition => definition.Unit.AssetKey == hero.AssetKey);
        UnitCloneResult triggered = HeroUnitBirthModel.Apply(train, 0, 0, null, false, [], heroDefinition);
        Require(!triggered.Supported && triggered.UnsupportedReason == RelicSpawnStatusModel.UnsupportedEndlessEnemyBirth,
            triggered.UnsupportedReason ?? "Triggered enemy creation must reach the same scaling guard as a scheduled wave.");
        foreach (bool preview in new[] { false, true })
        {
            var birth = new RoomCombatState(0, false, [hero], [], context, preview);
            RoomCombatResult covenant = RelicSpawnStatusModel.CharacterAdded(birth, hero.Id, 0, onlyCovenants: true);
            RoomCombatResult added = RelicSpawnStatusModel.CharacterAdded(birth, hero.Id, 0, onlyCovenants: false);
            Require(covenant.Supported && !added.Supported && added.UnsupportedReason == RelicSpawnStatusModel.UnsupportedEndlessEnemyBirth,
                "The enemy-birth guard must cover the native non-Covenant notification in live play and preview.");
        }
        Console.WriteLine("ENDLESS-ENEMY-BIRTH-SCOPE PASS: original Steward deployment, scheduled and triggered enemy birth, preview, owner and immutable root.");
    }

    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}
