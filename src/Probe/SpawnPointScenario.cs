using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class SpawnPointScenario
    {
        internal sealed class Query
        {
            public int UnitId { get; set; }
            public SpawnPointReference? Current { get; set; }
            public SpawnPointReference? LastKnown { get; set; }
        }
        internal sealed class Record
        {
            public string Label { get; set; } = "";
            public string Operation { get; set; } = "";
            public int RoomIndex { get; set; }
            public CombatTeam Team { get; set; }
            public int UnitId { get; set; }
            public int Index { get; set; } = -1;
            public int TargetIndex { get; set; } = -1;
            public SpawnPointReference? Target { get; set; }
            public SpawnPointWorld Before { get; set; } = null!;
            public SpawnPointWorld? After { get; set; }
            public int PivotMoves { get; set; }
            public int Remaining { get; set; }
            public Query[] Queries { get; set; } = Array.Empty<Query>();
            public string? Difference { get; set; }
            public bool Completed { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        internal static bool Prepared, Started, Completed;
        internal static string? Error;
        private static bool holding;
        private static readonly List<CharacterState> actors = new List<CharacterState>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        { Prepared = true; log.LogInfo("SPAWN-POINT-PREPARED native physical layout operations; original Boss/waves retained."); }
        internal static void Start(AllGameManagers managers, ManualLogSource log)
        { Started = true; managers.GetSaveManager().StartCoroutine(Protect(Run(managers), log)); }
        private static IEnumerator Protect(IEnumerator native, ManualLogSource log)
        {
            try
            {
                while (true)
                {
                    object? current;
                    try { if (!native.MoveNext()) break; current = native.Current; }
                    catch (Exception error) { Error = error.ToString(); log.LogError(Error); yield break; }
                    yield return current;
                }
                Completed = true;
            }
            finally { (native as IDisposable)?.Dispose(); holding = false; }
        }
        private static IEnumerator Run(AllGameManagers managers)
        {
            holding = true;
            var rooms = managers.GetRoomManager()!;
            RoomState room = rooms.GetRoom(1);
            CharacterData data = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Select(card => card.GetSpawnCharacterData()).First(unit => unit != null && unit.name.StartsWith("TrainSteward", StringComparison.Ordinal))!;
            for (int i = 0; i < 3; i++)
                yield return managers.GetMonsterManager()!.CreateMonsterState(data, null!, 1, unit => actors.Add(unit),
                    SpawnMode.SelectedSlot, room.GetMonsterPoint(i), isCardless: true);
            CharacterState a = actors[0], b = actors[1], c = actors[2];
            int hpC = c.GetHP(), maxC = c.GetMaxHP();
            RecordAction("remember-initial", "Remember", () => { a.SetLastKnownSpawnPoint(); return 0; }, a);
            RecordAction("remove-keeps-last", "Remove", () => { a.RemoveFromSpawnPoint(); return 0; }, a);
            RecordAction("backward-insertion-shift", "ShiftOccupants", () => { Group(room, Team.Type.Monsters).ShiftPointOccupants(room.GetMonsterPoint(0)); return 0; }, index: 0);
            RecordSet("fill-hole", a, room.GetMonsterPoint(1));
            RecordAction("rearrange-forward-through-hole", "Rearrange", () => { room.RearrangeCharacter(Team.Type.Monsters, 1, 4); return 0; }, index: 1, targetIndex: 4);
            RecordAction("rearrange-backward-through-hole", "Rearrange", () => { room.RearrangeCharacter(Team.Type.Monsters, 4, 0); return 0; }, index: 4, targetIndex: 0);
            Compact("compact-pivot", room, 4);
            RecordSet("overwrite-retains-displaced-reference", a, room.GetMonsterPoint(1));
            RecordAction("remember-displaced", "Remember", () => { b.SetLastKnownSpawnPoint(); return 0; }, b);
            RecordSet("repair-displaced-reference", b, room.GetMonsterPoint(0));
            RecordAction("remember-before-zero-hp", "Remember", () => { c.SetLastKnownSpawnPoint(); return 0; }, c);
            c.SetHealth(0, maxC); Compact("remove-zero-hp", room, 6);
            c.SetHealth(hpC, maxC); RecordSet("live-ignores-old-point", c, room.GetMonsterPoint(5));
            c.AddStatusEffect("undying", 1); c.SetHealth(0, maxC);
            Compact("undying-retains-zero-hp", room, 6);
            c.SetHealth(hpC, maxC); c.RemoveStatusEffect("undying", 1);
            var previewProperty = AccessTools.Property(typeof(CharacterState), nameof(CharacterState.SpawnedInPreviewMode));
            previewProperty.SetValue(b, true); Compact("remove-preview-born", room, -1); previewProperty.SetValue(b, false);
            AccessTools.Field(typeof(CharacterState), "destroyedState").SetValue(b, CharacterState.DestroyedState.InRemoveList);
            using (new CharacterState.SetAllowDestroyedAccessHelper(b, onlyIfDestroyed: true)) RecordSet("destroyed-detached-cannot-reenter", b, room.GetMonsterPoint(4));
            AccessTools.Field(typeof(CharacterState), "destroyedState").SetValue(b, CharacterState.DestroyedState.None);
            RecordSet("restore-detached", b, room.GetMonsterPoint(2));
            RecordSet("move-across-room", c, rooms.GetRoom(2).GetMonsterPoint(4));
            RecordSet("return-across-room", c, room.GetMonsterPoint(3));
            RecordAction("invalid-rearrange-no-op", "Rearrange", () => { room.RearrangeCharacter(Team.Type.Monsters, -1, 2); return 0; }, index: -1, targetIndex: 2);
            Compact("restore-dense-layout", room, -1);
            if (actors.Any(unit => unit.GetSpawnPoint()?.GetCharacterState() != unit))
                throw new InvalidOperationException("Position setup did not restore all three actors to owned points.");
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager()!, true);
        }
        private static void Compact(string label, RoomState room, int pivot) => RecordAction(label, "Compact",
            () => room.ShiftSpawnPoints(Team.Type.Monsters, pivot), index: pivot);
        private static void RecordSet(string label, CharacterState unit, SpawnPoint point) => RecordAction(label, "Set",
            () => { unit.SetSpawnPoint(point, animate: false, setPosition: false); return 0; }, unit, target: Reference(point));
        private static void RecordAction(string label, string operation, Func<int> action, CharacterState? actor = null,
            int index = -1, int targetIndex = -1, SpawnPointReference? target = null)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            var record = new Record { Label = label, Operation = operation, RoomIndex = 1, Team = CombatTeam.Player,
                UnitId = actor == null ? 0 : trace.UnitId(actor), Index = index, TargetIndex = targetIndex, Target = target, Before = Capture() };
            Records.Add(record);
            record.PivotMoves = action(); record.After = Capture(); record.Completed = true;
            RoomState room = AllGameManagers.Instance!.GetRoomManager()!.GetRoom(1);
            record.Remaining = room.GetRemainingSpawnPointCount(Team.Type.Monsters);
            record.Queries = actors.Select(unit =>
            {
                using (new CharacterState.SetAllowDestroyedAccessHelper(unit, onlyIfDestroyed: true))
                    return new Query { UnitId = trace.UnitId(unit), Current = Reference(unit.GetSpawnPoint()),
                        LastKnown = Reference(unit.GetSpawnPoint(allowLastKnownSpawnPoint: true)) };
            }).ToArray();
            var predicted = SpawnPointModel.Apply(record.Before, operation, 1, CombatTeam.Player, record.UnitId, index, targetIndex, target);
            record.Difference = !predicted.Supported ? predicted.UnsupportedReason :
                predicted.PivotMoves != record.PivotMoves || predicted.State!.Remaining(1, CombatTeam.Player) != record.Remaining ||
                !JToken.DeepEquals(JToken.FromObject(predicted.State!), JToken.FromObject(record.After)) ||
                record.Queries.Any(query => !JToken.DeepEquals(query.Current == null ? JValue.CreateNull() : JToken.FromObject(query.Current),
                        predicted.State.Point(query.UnitId) == null ? JValue.CreateNull() : JToken.FromObject(predicted.State.Point(query.UnitId)!)) ||
                    !JToken.DeepEquals(query.LastKnown == null ? JValue.CreateNull() : JToken.FromObject(query.LastKnown),
                        predicted.State.Point(query.UnitId, true) == null ? JValue.CreateNull() : JToken.FromObject(predicted.State.Point(query.UnitId, true)!)))
                ? "Native physical spawn point state differs" : null;
        }
        private static SpawnPointWorld Capture()
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            AllGameManagers managers = AllGameManagers.Instance!;
            SaveManager save = managers.GetSaveManager();
            var groups = new List<SpawnPointGroupState>();
            var units = new HashSet<CharacterState>(actors);
            for (int roomIndex = 0; roomIndex < managers.GetRoomManager()!.GetNumRooms(); roomIndex++)
            foreach (Team.Type team in new[] { Team.Type.Heroes, Team.Type.Monsters })
            {
                RoomState room = managers.GetRoomManager()!.GetRoom(roomIndex);
                SpawnPointGroup group = Group(room, team);
                group.GetAll(out List<SpawnPoint> points, out _);
                foreach (SpawnPoint point in points) if (point.GetCharacterState() is CharacterState unit) units.Add(unit);
                groups.Add(new SpawnPointGroupState(roomIndex, team == Team.Type.Heroes ? CombatTeam.Enemy : CombatTeam.Player,
                    save.GetNumSpawnPointsPerFloor(team), group.NumSpawnPoints,
                    points.Select(point => point.GetCharacterState() == null ? 0 : trace.UnitId(point.GetCharacterState()!)).ToArray(),
                    points.Select(point => point.IsOutsideTrain).ToArray()));
            }
            var occupants = new List<SpawnPointOccupant>();
            foreach (CharacterState unit in units)
            using (new CharacterState.SetAllowDestroyedAccessHelper(unit, onlyIfDestroyed: true))
            {
                object state = AccessTools.Property(typeof(CharacterState), "PrimaryStateInformation").GetValue(unit);
                occupants.Add(new SpawnPointOccupant(trace.UnitId(unit), unit.GetHP(), unit.IsAlive, unit.IsDead, unit.IsDestroyed,
                    unit.GetStatusEffectStacks("undying"), unit.SpawnedInPreviewMode, unit.IsOuterTrainBoss(),
                    Reference((SpawnPoint?)AccessTools.Field(state.GetType(), "spawnPoint").GetValue(state)),
                    Reference((SpawnPoint?)AccessTools.Field(state.GetType(), "lastKnownSpawnPoint").GetValue(state))));
            }
            return new SpawnPointWorld(save.PreviewMode, groups, occupants);
        }
        private static SpawnPointGroup Group(RoomState room, Team.Type team) => (SpawnPointGroup)AccessTools.Field(typeof(RoomState),
            team == Team.Type.Heroes ? "heroSpawnPointGroup" : "monsterSpawnPointGroup").GetValue(room);
        private static SpawnPointReference? Reference(SpawnPoint? point)
        {
            if (point == null) return null;
            AllGameManagers managers = AllGameManagers.Instance!;
            for (int roomIndex = 0; roomIndex < managers.GetRoomManager()!.GetNumRooms(); roomIndex++)
            foreach (Team.Type team in new[] { Team.Type.Heroes, Team.Type.Monsters })
            {
                Group(managers.GetRoomManager()!.GetRoom(roomIndex), team).GetAll(out List<SpawnPoint> points, out _);
                int index = points.IndexOf(point);
                if (index >= 0) return new SpawnPointReference(roomIndex, team == Team.Type.Heroes ? CombatTeam.Enemy : CombatTeam.Player, index);
            }
            throw new InvalidOperationException("Spawn point owner was not captured.");
        }
        [HarmonyPatch(typeof(CombatManager), "ProcessEffectsQueue")]
        private static class BackgroundPatch
        {
            private static bool Prefix(ref IEnumerator __result)
            { if (!holding) return true; __result = Hold(); return false; }
            private static IEnumerator Hold() { yield return null; }
        }
    }
}
