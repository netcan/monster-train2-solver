using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class PreviewCopyChecks
{
    internal static void Run()
    {
        var groups = Enumerable.Range(0, 4).SelectMany(room => new[] { CombatTeam.Enemy, CombatTeam.Player }
            .Select(team => new SpawnPointGroupState(room, team, 4, 4,
                room == 0 && team == CombatTeam.Player ? new[] { 1, 0, 2, 0 } : new int[4], new bool[4]))).ToArray();
        var primary = new BattleSpawnPoints(groups, [
            new(1, new(0, CombatTeam.Player, 0), null),
            new(2, new(0, CombatTeam.Player, 2), null)], 17);
        string parent = JsonSerializer.Serialize(primary);
        CombatUnit Unit(int id) => new(id, "copy-test", CombatTeam.Player, 1, 10, 10, true, false, false, []);
        string Verify()
        {
            BattleSpawnPoints selected = primary.BeginPreview();
            Require(selected.NextPreviewCopyId == 25 && selected.ActiveCopies.SequenceEqual(Enumerable.Range(17, 8)) &&
                selected.SelectedGroup(0, CombatTeam.Player)!.PreviewCopyId == 18 &&
                selected.BeginPreview().NextPreviewCopyId == 25,
                "Copy allocation lost native room/team order or repeated while already calculating.");
            var rng = UnityRng.Seed(27);
            var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10,
                nextUnitId: 4, spawnPoints: selected);
            var room = new RoomCombatState(0, false, [Unit(1), Unit(2)], [], context, true);
            var birth = BattleSpawnPointModel.Birth(selected, room, Unit(3), 3, shift: false);
            Require(birth.Supported, birth.Error ?? "Preview birth rejected.");
            var compact = BattleSpawnPointModel.Apply(birth.State!, new RoomCombatState(0, false,
                [Unit(1), Unit(2), Unit(3)], [], context, true), "Compact", CombatTeam.Player);
            Require(compact.Supported && compact.State!.Units.Single(unit => unit.UnitId == 3).Current!.Index == 2,
                "Preview compaction failed to move the selected birth plane.");
            BattleSpawnPoints restored = BattleSpawnPoints.EndPreview(primary, compact.State!, clearBirths: false);
            var born = restored.Units.Single(unit => unit.UnitId == 3);
            Require(born.Current!.PreviewCopyId == 18 && born.Current.Index == 3 && born.LastKnown == null &&
                restored.Groups.Count == 9 && restored.Group(0, CombatTeam.Player)!.Occupants.SequenceEqual([1, 0, 2, 0]) &&
                restored.Group(0, CombatTeam.Player, 18)!.Occupants.SequenceEqual([1, 2, 3, 0]) &&
                restored.NextPreviewCopyId == 25,
                "Temporary restoration aliased primary/copied slots or restored a newborn's selected position over its birth origin.");
            BattleSpawnPoints cleared = BattleSpawnPoints.EndPreview(primary, compact.State!, clearBirths: true);
            Require(cleared.Groups.Count == 8 && cleared.NextPreviewCopyId == 25 &&
                cleared.Units.Single(unit => unit.UnitId == 3).Current == null &&
                restored.BeginPreview().NextPreviewCopyId == 33 &&
                restored.BeginPreview().Group(0, CombatTeam.Player, 18) != null,
                "UI restoration lost its copy counter or a later preview discarded historical retained copies.");
            var frameContext = context.WithSpawnPoints(restored);
            var frame = TrainCombatModel.CompleteFrameRemovals(new TrainCombatState(Enumerable.Range(0, 4)
                .Select(index => new RoomCombatState(index, false, index == 0 ? new[] { Unit(1), Unit(2) } : [], [], frameContext))
                .ToArray(), [], 4, frameContext));
            Require(frame.Context!.SpawnPoints!.Groups.Count == 8 && frame.Context.SpawnPoints.NextPreviewCopyId == 25 &&
                frame.Context.SpawnPoints.Units.Single(unit => unit.UnitId == 3).Current == null,
                "Frame destruction retained an unreferenced copied list or reset the shared allocation counter.");
            Require(JsonSerializer.Serialize(primary) == parent, "A preview branch mutated its primary root.");
            return JsonSerializer.Serialize(restored);
        }
        string expected = Verify();
        Parallel.For(0, 32, _ => Require(Verify() == expected, "Parallel copied-position branches diverged."));
        Require(BattleSpawnPointModel.Validate(new BattleSpawnPoints(groups, primary.Units, 0), 3) != null,
            "An invalid copied-list counter was accepted.");
        Console.WriteLine("PREVIEW-COPY-CHECKS PASS: native room/team allocation order, nested calculation gate, selected compaction, newborn primary origin, historical copies, UI clearing and 32 immutable branches.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
