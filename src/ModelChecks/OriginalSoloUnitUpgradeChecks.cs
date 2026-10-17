using System;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalSoloUnitUpgradeChecks
{
    private const string OwnerId = "724a1c19-192f-48c1-8099-8ae3ecb9c1e5";
    private const string Solo = "fc119000-0000-4000-8000-000000000601";
    private const string SoloSecond = "fc119000-0000-4000-8000-000000000602";
    private const string PeerOne = "fc119000-0000-4000-8000-000000000603";
    private const string PeerTwo = "fc119000-0000-4000-8000-000000000604";

    internal static PlayCardAction? Choose(BattleTurnState state)
    {
        CombatContext context = state.Spawn.Train.Context!;
        var plays = BattleActionModel.EnumerateSupportedPlays(state);
        PlayCardAction? Find(string dataId, int roomIndex)
        {
            RoomCombatState room = state.Spawn.Train.Rooms.Single(item => item.RoomIndex == roomIndex);
            int position = room.Units.Count(unit => unit.Team == CombatTeam.Player && !unit.IsPyre);
            return plays.FirstOrDefault(play => context.FindCard(play.CardInstanceId)?.DataId == dataId &&
                play.RoomIndex == roomIndex && play.PlayerPosition == position);
        }
        foreach ((string card, int room) in new[] { (Solo, 0), (PeerOne, 1), (PeerTwo, 1) })
        {
            if (state.Spawn.Train.Rooms[room].Units.Any(unit =>
                context.FindCard(unit.SpawnerCardId)?.DataId == card)) continue;
            PlayCardAction? action = Find(card, room);
            if (action != null) return action;
        }
        if (state.Spawn.Turn >= 2 && !state.Spawn.Train.Rooms[0].Units.Any(unit =>
            context.FindCard(unit.SpawnerCardId)?.DataId == SoloSecond))
            return Find(SoloSecond, 0);
        return null;
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 183 &&
            root.GetProperty("ModifierScenario").GetString() == "original-add-stats-to-solo-units" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "AddStatsToSoloUnits native battle failed acceptance.");
        FixtureValue evidence = root.GetProperty("OriginalSoloUnitUpgrade");
        Require(evidence.GetProperty("OwnerId").GetString() == OwnerId, "Original solo-unit owner changed.");

        var tests = root.GetProperty("RelicSoloUnitUpgradeTests").EnumerateArray().ToArray();
        Require(tests.Any(row => row.GetProperty("Phase").GetString() == "TestEffectStartOfBattle" &&
                !row.GetProperty("Returned").GetBoolean()) &&
            tests.Count(row => row.GetProperty("Phase").GetString() == "TestEffectTurnPhaseTiming" &&
                row.GetProperty("Returned").GetBoolean()) >= 4,
            "Missing native start-of-battle rejection or repeated start-of-turn eligibility.");
        foreach (var row in tests)
            ValidateOwner(row.GetProperty("Context").Deserialize<CombatContext>()!);

        var callbacks = root.GetProperty("RelicSoloUnitUpgrades").EnumerateArray().ToArray();
        Require(callbacks.Length >= 5 && callbacks.All(row => row.GetProperty("Owner").GetString() == OwnerId &&
            row.GetProperty("Completed").GetBoolean() && row.GetProperty("Actual").ValueKind != FixtureKind.Null),
            "Native solo-unit callback records are incomplete.");
        var turns = root.GetProperty("Turns").EnumerateArray().Select(row =>
            (Before: row.GetProperty("Before").Deserialize<BattleTurnState>()!,
                Actual: row.GetProperty("Actual").Deserialize<BattleTurnState>()!))
            .OrderBy(row => row.Before.Spawn.Turn).ToArray();
        Require(turns.Length >= 4, "Native battle did not retain enough turn snapshots.");

        CombatRelicState definition = ValidateOwner(tests[0].GetProperty("Context").Deserialize<CombatContext>()!);
        VerifyCopies(definition);
        int[] callbackTurns = callbacks.Select(row => row.GetProperty("Turn").GetInt32()).Order().ToArray();
        Require(callbackTurns.Take(4).SequenceEqual(new[] { 0, 1, 2, 3 }), "Native callback timing missed consecutive player turns.");
        int soloUnitId = SoloUnit(turns.Single(row => row.Before.Spawn.Turn == 1).Before, Solo).Id;
        foreach (var row in callbacks)
        {
            int turn = row.GetProperty("Turn").GetInt32();
            CombatContext actual = row.GetProperty("Actual").Deserialize<CombatContext>()!;
            CombatRelicState owner = ValidateOwner(actual);
            int[] tracked = owner.SoloUnitUpgrades!.Single().TrackedUnitIds.ToArray();
            Require(tracked.Length == 4 && tracked.Skip(1).All(id => id == 0), "Native tracker selected a multi-unit or empty floor.");
            if (turn == 0) Require(tracked[0] == 0, "The initial empty floor was tracked before any unit was played.");
            if (turn is 1 or 2) Require(tracked[0] == soloUnitId, "The native solo floor did not retain its original unit tracker.");
            if (turn >= 3) Require(tracked[0] == 0, "The native tracker did not clear after the floor became multi-unit.");
        }

        foreach (var row in turns)
        {
            int turn = row.Before.Spawn.Turn;
            int soloStacks = Math.Min(turn, 2);
            CombatUnit solo = SoloUnit(row.Before, Solo);
            Require(solo.Attack == 5 + 5 * soloStacks && solo.MaxHealth == 120 + 5 * soloStacks &&
                solo.Modifiers!.Upgrades.Count == soloStacks,
                $"Native turn {turn} did not retain the expected repeated +5/+5 solo upgrade.");
            int floorZero = PlayerUnits(row.Before, 0).Length;
            int floorOne = PlayerUnits(row.Before, 1).Length;
            Require(floorZero == (turn < 2 ? 1 : 2) && floorOne == 2 &&
                PlayerUnits(row.Before, 2).Length == 0 && PlayerUnits(row.Before, 3).Length == 0,
                $"Native turn {turn} did not preserve the solo/multi/empty-floor controls.");

            if (turn > 1) continue;
            BattleTurnResult withOwner = BattleTurnModel.EndTurn(row.Before);
            BattleTurnResult withoutOwner = BattleTurnModel.EndTurn(RemoveOwner(row.Before));
            Require(withOwner.Supported && withoutOwner.Supported,
                "Solo-unit owner positive/control branch was unsupported: " +
                (withOwner.UnsupportedReason ?? withoutOwner.UnsupportedReason));
            CombatUnit positive = SoloUnit(withOwner.State!, Solo);
            CombatUnit control = SoloUnit(withoutOwner.State!, Solo);
            Require(positive.Attack - control.Attack == 5 && positive.MaxHealth - control.MaxHealth == 5 &&
                positive.Modifiers!.Upgrades.Count - control.Modifiers!.Upgrades.Count == 1,
                $"Turn {turn} owner ablation did not remove exactly one +5/+5 upgrade.");
        }
        BattleActionChecks.Native(root);
        Console.WriteLine($"ORIGINAL-SOLO-UNIT-UPGRADE-NATIVE PASS: {callbacks.Length} native turns, exact +5/+5 repeat, multi/empty controls, tracker reset, clone preservation and positive owner ablations.");
    }

    private static CombatRelicState ValidateOwner(CombatContext context)
    {
        CombatRelicState owner = context.Relics!.Single(relic => relic.DataId == OwnerId);
        RelicSoloUnitUpgrade rule = owner.SoloUnitUpgrades!.Single();
        CardUpgradeModifier upgrade = rule.Upgrade!;
        Require(owner.AssetKey == "AddStatsToSoloUnits" && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
            owner.EffectTypes.SequenceEqual([RelicSoloUnitUpgradeModel.Effect]) && rule.EffectIndex == 0 &&
            rule.SourceMonsters && rule.SubtypeIsNone && !rule.Removable && rule.ConditionCount == 0 &&
            rule.TrackedUnitIds.Count == 4 && upgrade.AssetKey == "AddStatsToSoloUnits_Upgrade" &&
            upgrade.DataId == "83591a87-a491-4ea8-a27c-f4e29577365c" && !upgrade.Unique &&
            upgrade.Stats.Damage == 5 && upgrade.Stats.Health == 5 && upgrade.Stats.Cost == 0 &&
            upgrade.Stats.Size == 0 && upgrade.Stats.Heal == 0 && upgrade.Stats.XCost == 0 &&
            upgrade.Stats.EquipmentLimit == 0 && upgrade.Stats.UpgradeSlotCount == 0 && upgrade.Statuses.Count == 0 &&
            upgrade.ExternalInteractions.Count == 0,
            "Original AddStatsToSoloUnits owner, filter or +5/+5 payload changed.");
        return owner;
    }

    private static void VerifyCopies(CombatRelicState owner)
    {
        CombatRelicState[] copies =
        [
            owner.WithSoloUnitUpgrades(owner.SoloUnitUpgrades!), owner.WithSpawnStatuses([]),
            owner.WithRoomEntryStatuses([]), owner.WithCardModifiers([]), owner.WithTriggerAdditions([]),
            owner.WithStatusStackAdditions([]), owner.WithStatusMagnitudes([]), owner.WithResourceEffects([]),
            owner.WithMagicPowerModifiers([]), owner.WithCardPlayMagicPowerModifiers([]), owner.WithUnitUpgradeTriggers([]),
            owner.WithForgePointModifiers([]), owner.WithCardHealthModifiers([]), owner.WithCardAttackModifiers([]),
            owner.WithCardDrawOnCardPlayModifiers([]), owner.WithStatusAttackModifiers([]),
            owner.WithCardPlayRoomCapacityModifiers([]), owner.WithMoonPhaseHealRules([]),
            owner.WithRoomAttachmentReplications([]), owner.WithSummonDamageRules([]),
            owner.WithCheaperSpellUpgradeModifiers([]), owner.WithUnstableRebalances([]),
            owner.WithCopyCardOnCardPlayModifiers([])
        ];
        string expected = JsonSerializer.Serialize(owner.SoloUnitUpgrades, ModelJson.Options);
        Require(copies.All(copy => JsonSerializer.Serialize(copy.SoloUnitUpgrades, ModelJson.Options) == expected),
            "A CombatRelicState clone discarded solo-unit tracking state.");
    }

    private static CombatUnit SoloUnit(BattleTurnState state, string cardId) => PlayerUnits(state, 0)
        .Single(unit => state.Spawn.Train.Context!.FindCard(unit.SpawnerCardId)?.DataId == cardId);

    private static CombatUnit[] PlayerUnits(BattleTurnState state, int roomIndex) => state.Spawn.Train.Rooms
        .Single(room => room.RoomIndex == roomIndex).Units.Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre).ToArray();

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
