using System;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalFrontRoomHealthUpgradeChecks
{
    private const string OwnerId = "cb2c9d18-9a05-4523-acdc-6e4af73152aa";
    private const string UpgradeId = "45bdb6aa-dd9a-42b7-930d-f586a8c7979a";
    private const string FrontZero = "fc119000-0000-4000-8000-000000000701";
    private const string BackZero = "fc119000-0000-4000-8000-000000000702";
    private const string FrontOne = "fc119000-0000-4000-8000-000000000703";
    private const string BackOne = "fc119000-0000-4000-8000-000000000704";

    internal static PlayCardAction? Choose(BattleTurnState state)
    {
        var context = state.Spawn.Train.Context!;
        var plays = BattleActionModel.EnumerateSupportedPlays(state);
        foreach ((string card, int room, int position) in new[]
        {
            (FrontZero, 0, 0), (BackZero, 0, 1), (FrontOne, 1, 0), (BackOne, 1, 1)
        })
        {
            if (state.Spawn.Train.Rooms[room].Units.Any(unit => context.FindCard(unit.SpawnerCardId)?.DataId == card)) continue;
            PlayCardAction? action = plays.FirstOrDefault(play => context.FindCard(play.CardInstanceId)?.DataId == card &&
                play.RoomIndex == room && play.PlayerPosition == position);
            if (action != null) return action;
        }
        return null;
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 207 &&
            root.GetProperty("ModifierScenario").GetString() == "original-front-room-health-upgrade" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "Front-room max-health native battle failed acceptance.");
        Require(root.GetProperty("OriginalFrontRoomHealthUpgrade").GetProperty("OwnerId").GetString() == OwnerId,
            "Original front-room health owner changed.");

        var tests = root.GetProperty("RelicFrontRoomHealthUpgradeTests").EnumerateArray().ToArray();
        Require(tests.Any(row => row.GetProperty("Phase").GetString() == "TestEffectStartOfTurn" &&
                row.GetProperty("Turn").GetInt32() == 0 && !row.GetProperty("Returned").GetBoolean()) &&
            tests.Any(row => row.GetProperty("Phase").GetString() == "TestEffectStartOfTurn" &&
                row.GetProperty("Turn").GetInt32() >= 1 && row.GetProperty("Returned").GetBoolean()),
            "Missing empty-room rejection or populated-floor start-of-turn eligibility.");
        foreach (var row in tests) ValidateOwner(row.GetProperty("Context").Deserialize<CombatContext>()!);

        var callbacks = root.GetProperty("RelicFrontRoomHealthUpgrades").EnumerateArray().ToArray();
        Require(callbacks.Length >= 2 && callbacks.All(row => row.GetProperty("Owner").GetString() == OwnerId &&
            row.GetProperty("Completed").GetBoolean() && row.GetProperty("Actual").ValueKind != FixtureKind.Null &&
            row.GetProperty("ActualRooms").ValueKind == FixtureKind.Array),
            "Native front-room upgrade callbacks are incomplete.");
        foreach (var row in callbacks)
        {
            CombatContext before = row.GetProperty("Before").Deserialize<CombatContext>()!;
            CombatContext actual = row.GetProperty("Actual").Deserialize<CombatContext>()!;
            RoomCombatState[] beforeRooms = row.GetProperty("BeforeRooms").Deserialize<RoomCombatState[]>()!;
            RoomCombatState[] actualRooms = row.GetProperty("ActualRooms").Deserialize<RoomCombatState[]>()!;
            ValidateOwner(before);
            VerifyNativeCallback(before, actual, beforeRooms, actualRooms);
        }

        BattleTurnState eligible = root.GetProperty("Turns").EnumerateArray()
            .Select(row => row.GetProperty("Before").Deserialize<BattleTurnState>()!)
            .OrderBy(state => state.Spawn.Turn).FirstOrDefault(state => state.Spawn.Turn >= 1 &&
                PlayerUnits(state.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0)).Length >= 2 &&
                PlayerUnits(state.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1)).Length >= 2)
            ?? throw new InvalidOperationException("No two-floor front/back turn was retained for pure owner ablation.");
        BattleTurnResult positive = BattleTurnModel.EndTurn(eligible);
        BattleTurnResult control = BattleTurnModel.EndTurn(RemoveOwner(eligible));
        Require(positive.Supported && control.Supported,
            "Front-room relic positive/control model transition was unsupported: " +
            (positive.UnsupportedReason ?? control.UnsupportedReason));
        foreach (int roomIndex in new[] { 0, 1 })
        {
            CombatUnit[] before = PlayerUnits(eligible.Spawn.Train.Rooms.Single(room => room.RoomIndex == roomIndex));
            CombatUnit frontBefore = before[0];
            CombatUnit backBefore = before[1];
            RoomCombatState positiveRoom = positive.State!.Spawn.Train.Rooms.Single(room => room.RoomIndex == roomIndex);
            RoomCombatState controlRoom = control.State!.Spawn.Train.Rooms.Single(room => room.RoomIndex == roomIndex);
            CombatUnit frontPositive = positiveRoom.Units.Single(unit => unit.Id == frontBefore.Id);
            CombatUnit frontControl = controlRoom.Units.Single(unit => unit.Id == frontBefore.Id);
            CombatUnit backPositive = positiveRoom.Units.Single(unit => unit.Id == backBefore.Id);
            CombatUnit backControl = controlRoom.Units.Single(unit => unit.Id == backBefore.Id);
            Require(frontPositive.MaxHealth - frontControl.MaxHealth == 15 && frontPositive.Health - frontControl.Health == 15 &&
                frontPositive.Modifiers!.Upgrades.Count(item => item.DataId == UpgradeId) -
                    frontControl.Modifiers!.Upgrades.Count(item => item.DataId == UpgradeId) == 1,
                $"Room {roomIndex} owner ablation did not remove exactly one +15 HP front upgrade.");
            Require(backPositive.MaxHealth == backControl.MaxHealth && backPositive.Health == backControl.Health &&
                backPositive.Modifiers!.Upgrades.Count(item => item.DataId == UpgradeId) ==
                    backControl.Modifiers!.Upgrades.Count(item => item.DataId == UpgradeId),
                $"Room {roomIndex} relic affected a non-front unit.");
        }
        BattleActionChecks.Native(root);
        Console.WriteLine($"ORIGINAL-FRONT-ROOM-HEALTH-UPGRADE-NATIVE PASS: {callbacks.Length} callbacks, two-floor front-only +15 max/current HP, repeated card modifiers and owner ablation.");
    }

    private static CombatRelicState ValidateOwner(CombatContext context)
    {
        CombatRelicState owner = context.Relics!.Single(relic => relic.DataId == OwnerId);
        RelicSoloUnitUpgrade rule = owner.SoloUnitUpgrades!.Single();
        CardUpgradeModifier upgrade = rule.Upgrade!;
        Require(owner.AssetKey == "UpgradeMaxHpFrontUnit" && !owner.DisallowedInPlacementPhase!.Value &&
            owner.EffectTypes.SequenceEqual([RelicSoloUnitUpgradeModel.FrontRoomEffect]) && rule.EffectIndex == 0 &&
            rule.FrontInEachRoom && rule.SourceMonsters && rule.SubtypeKey == null && rule.SubtypeIsNone &&
            !rule.Removable && rule.ConditionCount == 0 && rule.TrackedUnitIds.Count == 0 &&
            upgrade.AssetKey == "EmblemOfExiles_Upgrade" && upgrade.DataId == UpgradeId && !upgrade.Unique &&
            upgrade.Stats.Damage == 0 && upgrade.Stats.Health == 15 && upgrade.UnhealedHealth == 0 &&
            upgrade.Stats.Cost == 0 && upgrade.Stats.Size == 0 && upgrade.Stats.Heal == 0 &&
            upgrade.Stats.XCost == 0 && upgrade.Stats.EquipmentLimit == 0 && upgrade.Stats.UpgradeSlotCount == 0 &&
            upgrade.Statuses.Count == 0 && upgrade.ExternalInteractions.Count == 0,
            "Original UpgradeMaxHpFrontUnit owner, target mode or +15 HP payload changed.");
        return owner;
    }

    private static void VerifyNativeCallback(CombatContext beforeContext, CombatContext actualContext,
        RoomCombatState[] beforeRooms, RoomCombatState[] actualRooms)
    {
        Require(beforeRooms.Length == 4 && actualRooms.Length == 4, "Native callback did not capture all four floors.");
        foreach (int roomIndex in new[] { 0, 1 })
        {
            RoomCombatState beforeRoom = beforeRooms.Single(room => room.RoomIndex == roomIndex);
            RoomCombatState actualRoom = actualRooms.Single(room => room.RoomIndex == roomIndex);
            CombatUnit[] before = PlayerUnits(beforeRoom);
            CombatUnit[] actual = PlayerUnits(actualRoom);
            Require(before.Length >= 2 && actual.Length >= 2, $"Floor {roomIndex} lost its native front/back controls.");
            CombatUnit frontBefore = before[0];
            CombatUnit frontActual = actual.Single(unit => unit.Id == frontBefore.Id);
            CombatUnit backBefore = before[1];
            CombatUnit backActual = actual.Single(unit => unit.Id == backBefore.Id);
            Require(frontActual.MaxHealth == frontBefore.MaxHealth + 15 && frontActual.Health == frontBefore.Health + 15 &&
                frontActual.Modifiers!.Upgrades.Count(item => item.DataId == UpgradeId) ==
                    frontBefore.Modifiers!.Upgrades.Count(item => item.DataId == UpgradeId) + 1,
                $"Floor {roomIndex} native front target did not receive exact +15 max/current HP.");
            Require(backActual.MaxHealth == backBefore.MaxHealth && backActual.Health == backBefore.Health &&
                backActual.Modifiers!.Upgrades.Count(item => item.DataId == UpgradeId) ==
                    backBefore.Modifiers!.Upgrades.Count(item => item.DataId == UpgradeId),
                $"Floor {roomIndex} native relic affected the back unit.");
            VerifyCardModifier(beforeContext, actualContext, frontBefore, 1);
            VerifyCardModifier(beforeContext, actualContext, backBefore, 0);
        }
        foreach (int roomIndex in new[] { 2, 3 })
            Require(PlayerUnits(beforeRooms.Single(room => room.RoomIndex == roomIndex)).Length == 0 &&
                PlayerUnits(actualRooms.Single(room => room.RoomIndex == roomIndex)).Length == 0,
                $"Native front-room relic unexpectedly found a player Monster on empty floor {roomIndex}.");
    }

    private static void VerifyCardModifier(CombatContext before, CombatContext actual, CombatUnit unit, int increment)
    {
        CardInstanceState beforeCard = before.FindCard(unit.SpawnerCardId)!;
        CardInstanceState actualCard = actual.FindCard(unit.SpawnerCardId)!;
        int beforeCount = beforeCard.Temporary.Upgrades.Count(item => item.DataId == UpgradeId);
        int actualCount = actualCard.Temporary.Upgrades.Count(item => item.DataId == UpgradeId);
        Require(actualCount - beforeCount == increment,
            $"Native source card {beforeCard.DataId} did not preserve the front-only temporary upgrade.");
    }

    private static CombatUnit[] PlayerUnits(RoomCombatState room) => room.Units
        .Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre).ToArray();

    private static BattleTurnState RemoveOwner(BattleTurnState source)
    {
        EnemySpawnState old = source.Spawn;
        CombatContext context = old.Train.Context!.WithRelics(old.Train.Context.Relics!.Where(owner => owner.DataId != OwnerId).ToArray());
        TrainCombatState train = CardSpellModel.WithContext(old.Train, context);
        var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping, old.Rng,
            old.NextUnitId, old.Treasures, old.TreasuresRemaining, old.TreasureEnabled, old.FirstTreasureTurn,
            old.FirstTreasureRoom, old.Turn, old.ExternalInteractions, old.CanonicalDecisionReferences, old.PendingDestroyedUnitIds);
        return new BattleTurnState(spawn, source.Energy, source.EnergyPerTurn, source.DrawPerTurn, source.ForgePoints,
            source.DragonsHoard, source.MoonPhase, source.RngStreams, source.OtherPiles, source.ExternalInteractions,
            source.PlayRules, source.BattlePreviewEnabled, source.UiRngIsolated, source.CanonicalDecisionReferences,
            source.CanonicalPhysicalReferences, source.SelectedRoom, source.MoonPhaseTrackingActive);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
