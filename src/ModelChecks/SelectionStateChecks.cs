using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class SelectionStateChecks
{
    internal static void Run()
    {
        PreviewSelection();
        foreach (bool captured in new[] { false, true })
        {
            BattleTurnState root = DeathRoot(captured);
            string parent = JsonSerializer.Serialize(root);
            BattleActionResult card = BattleActionModel.PlayCard(root, new(3, 0));
            Require(card.Supported, "Cross-floor death card rejected: " + card.Reason);
            Require(card.State!.SelectedRoom == (captured ? 1 : 0) &&
                card.State.Spawn.Train.Context!.QueryFrame!.SelectedRoomIndex == (captured ? 1 : (int?)null) &&
                card.State.Spawn.Train.Rooms.All(room => room.Units.All(unit => unit.Id != 11)),
                "Card completion lost its death-selected room or introduced legacy frame state.");
            BattleActionResult ability = BattleActionModel.PlayCard(root, new(2, 0, activatorUnitId: 10));
            Require(ability.Supported, "Cross-floor death ability rejected: " + ability.Reason);
            Require(ability.State!.SelectedRoom == (captured ? 1 : 0) &&
                ability.State.Spawn.Train.Context!.QueryFrame!.SelectedRoomIndex == (captured ? 1 : (int?)null),
                "Ability completion lost its death-selected room or introduced legacy frame state.");
            string expected = JsonSerializer.Serialize(card.State);
            Parallel.For(0, 16, _ => Require(JsonSerializer.Serialize(BattleActionModel.PlayCard(root, new(3, 0)).State) == expected,
                "Selection branches diverged."));
            Require(JsonSerializer.Serialize(root) == parent, "A death selection mutated its parent decision.");
        }
        foreach (bool captured in new[] { false, true })
        {
            BattleTurnState root = QuietRoot(captured ? 1 : null, 0, preview: false);
            string parent = JsonSerializer.Serialize(root);
            BattleTurnResult turn = BattleTurnModel.EndTurn(root);
            Require(turn.Supported, "Quiet selection EndTurn rejected: " + turn.UnsupportedReason);
            Require(turn.State!.SelectedRoom == (captured ? 1 : 0) &&
                turn.State.Spawn.Train.Context!.QueryFrame!.SelectedRoomIndex == (captured ? 1 : (int?)null) &&
                JsonSerializer.Serialize(root) == parent,
                "EndTurn discarded its final live frame selection or changed a legacy decision.");
        }
        Console.WriteLine("SELECTION-STATE-CHECKS PASS: death-selected card/ability completion, EndTurn live-frame authority, exact preview refresh counts, legacy unknown frames and 16 immutable branches.");
    }

    private static void PreviewSelection()
    {
        BattleTurnState root = QuietRoot(1, 0, preview: true);
        string parent = JsonSerializer.Serialize(root);
        BattleTurnResult changed = BattleTurnModel.SelectRoom(root, 0);
        Require(changed.Supported && changed.State!.SelectedRoom == 0 &&
            changed.State.Spawn.Train.Context!.QueryFrame!.SelectedRoomIndex == 0 &&
            changed.State.Spawn.Train.Context.SpawnPoints!.NextPreviewCopyId == 23,
            "Returning to the stale outer room missed the native six-plane preview refresh: " + changed.UnsupportedReason);
        Require(ReferenceEquals(BattleTurnModel.SelectRoom(changed.State!, 0).State, changed.State),
            "An unchanged selected room refreshed the preview twice.");
        BattleTurnResult alreadySelected = BattleTurnModel.SelectRoom(root, 1);
        Require(alreadySelected.Supported && alreadySelected.State!.SelectedRoom == 1 &&
            alreadySelected.State.Spawn.Train.Context!.SpawnPoints!.NextPreviewCopyId == 17,
            "Repairing a stale outer room allocated a preview despite an unchanged live selection.");
        BattleTurnState legacy = QuietRoot(null, 0, preview: true);
        BattleTurnResult oldSelection = BattleTurnModel.SelectRoom(legacy, 1);
        Require(oldSelection.Supported && oldSelection.State!.SelectedRoom == 1 &&
            oldSelection.State.Spawn.Train.Context!.QueryFrame!.SelectedRoomIndex == null &&
            oldSelection.State.Spawn.Train.Context.SpawnPoints!.NextPreviewCopyId == 23,
            "A legacy selection lost its preview refresh or invented an uncaptured frame value.");
        Require(JsonSerializer.Serialize(root) == parent, "Room selection mutated its source frame or preview allocator.");
    }

    private static BattleTurnState DeathRoot(bool captured)
    {
        BattleTurnState source = UnitAbilityChecks.Root();
        CombatContext context = source.Spawn.Train.Context!.WithQueryFrame(
            source.Spawn.Train.Context.QueryFrame!.With(selectedRoomIndex: captured ? 0 : null));
        CombatUnit actor = source.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 10).WithTriggers([]);
        CombatUnit enemy = source.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 11);
        var train = new TrainCombatState([
            new(0, false, [actor], [], context), new(1, false, [enemy], [], context),
            new(2, false, source.Spawn.Train.Rooms[2].Units, [], context)
        ], [], 7, context);
        CardActionEffect[] effects = [new("Damage", "Tower", 99, true, false, [])];
        var rules = new BattlePlayRules(source.PlayRules!.Rooms, [
            new("skill", "skill", 1, "Spell", "Discard", null, [], effects, upgradeInteractions: [],
                ability: new("Unit", false, true), cardType: "Spell", isAnyAbility: true),
            new("ordinary", "ordinary", 0, "Spell", "Discard", null, [], effects, upgradeInteractions: [])
        ]);
        return Copy(source, train, rules, 0, false);
    }

    private static BattleTurnState QuietRoot(int? framedRoom, int outerRoom, bool preview)
    {
        BattleTurnState source = UnitAbilityChecks.Root();
        CombatUnit pyre = source.Spawn.Train.Rooms[2].Units.Single();
        SpawnPointGroupState[] groups = Enumerable.Range(0, 3).SelectMany(room => new[] { CombatTeam.Enemy, CombatTeam.Player }
            .Select(team => new SpawnPointGroupState(room, team, 7, 7,
                room == 2 && team == CombatTeam.Player ? new[] { 12, 0, 0, 0, 0, 0, 0 } : new int[7], new bool[7]))).ToArray();
        CombatContext context = source.Spawn.Train.Context!.WithQueryFrame(
            source.Spawn.Train.Context.QueryFrame!.With(selectedRoomIndex: framedRoom))
            .WithNextUnitId(source.Spawn.NextUnitId)
            .WithSpawnPoints(new BattleSpawnPoints(groups, [new(12, new(2, CombatTeam.Player, 0), null)], 17));
        var train = new TrainCombatState(Enumerable.Range(0, 3).Select(room => new RoomCombatState(room, false,
            room == 2 ? new[] { pyre } : [], [], context)).ToArray(), [], 7, context);
        return Copy(source, train, source.PlayRules!, outerRoom, preview);
    }

    private static BattleTurnState Copy(BattleTurnState source, TrainCombatState train, BattlePlayRules rules, int selectedRoom, bool preview)
    {
        EnemySpawnState old = source.Spawn;
        var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping, old.Rng, old.NextUnitId,
            old.Treasures, old.TreasuresRemaining, old.TreasureEnabled, old.FirstTreasureTurn, old.FirstTreasureRoom, old.Turn,
            old.ExternalInteractions, old.CanonicalDecisionReferences, old.PendingDestroyedUnitIds);
        return new BattleTurnState(spawn, source.Energy, source.EnergyPerTurn, source.DrawPerTurn, source.ForgePoints,
            source.DragonsHoard, source.MoonPhase, source.RngStreams.Append(new BattleRngStream("Spawning", 84, old.Rng)).ToArray(),
            source.OtherPiles, source.ExternalInteractions, rules, preview, source.UiRngIsolated,
            source.CanonicalDecisionReferences, source.CanonicalPhysicalReferences, selectedRoom, source.MoonPhaseTrackingActive);
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
