#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalShuffleUnitsWitness
    {
        private const string OwnerId = "afcb1af2-64f5-4887-9f7b-7e509e326604";
        private const string OwnerKey = "ShuffleUnits";
        private const string SpearSteward = "38e70e6a-20f3-4e36-8d31-c5110be07f87";
        private const string Dreg = "63a48bad-07da-4687-bfef-2f7c6653af5e";
        private const int Floor = 2;
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_SHUFFLE_UNITS_WITNESS") == "1";
        internal static bool ThreeHosts => Environment.GetEnvironmentVariable("MT2_PROBE_SHUFFLE_UNITS_THREE_HOSTS") == "1";
        private static int RequiredHosts => ThreeHosts ? 3 : 2;
        private static RelicEffectShuffleUnitsEndOfTurn? effect;
        internal static bool Acquired => effect != null;
        private static int deploymentPlays;
        private static BattleTurnState? deploymentBefore, before;
        private static Application? active;
        private static readonly List<object> Tests = new();
        private static readonly List<Application> Applications = new();

        internal static void Acquire(SaveManager save)
        {
            if (UnityEngine.Application.version != "2.3.1" || typeof(CardState).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetCurrentDistance() != 0 || save.GetCovenants().Count != 0 || save.GetMutators().Count != 0)
                throw new InvalidOperationException("Expected the pinned original rank-zero Ring 1 run before combat.");
            var definition = save.GetAllGameData().FindMutatorData(OwnerId)
                ?? throw new InvalidOperationException("Original ShuffleUnits mutator was missing.");
            var data = definition.GetEffects().Single();
            if (definition.name != OwnerKey || !definition.DisallowInDeploymentPhase ||
                data.GetEffectClassName() != nameof(RelicEffectShuffleUnitsEndOfTurn) ||
                data.GetEffectConditions().Count != 0 || data.GetParamSourceTeam() != Team.Type.None ||
                data.GetTargetMode() != TargetMode.Room)
                throw new InvalidOperationException("Original ShuffleUnits authored payload changed.");
            save.AddMutator(definition);
            var activeOwner = save.GetMutators().Single();
            if (activeOwner.GetRelicDataID() != OwnerId ||
                activeOwner.GetEffects().Single() is not RelicEffectShuffleUnitsEndOfTurn installed)
                throw new InvalidOperationException("Native ShuffleUnits effect did not initialize.");
            effect = installed;
        }

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers, ManualLogSource log, Action advanceTurn)
        {
            var combat = managers.GetCombatManager();
            var rooms = managers.GetRoomManager();
            var save = managers.GetSaveManager();
            if (!Acquired || save.PreviewMode)
                throw new InvalidOperationException("Expected a settled live decision with the original ShuffleUnits mutator.");
            if (combat.IsPlacementPhase)
            {
                if (rooms.GetSelectedRoom() != Floor)
                {
                    save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(Floor));
                    return false;
                }
                if (deploymentPlays < RequiredHosts)
                {
                    var originalCard = ThreeHosts ? deploymentPlays == 0 ? SpearSteward : Dreg : null;
                    if (!TryPlaceHost(managers, originalCard))
                        throw new InvalidOperationException("No original monster card can fill the next host position.");
                    deploymentPlays++;
                    return false;
                }
                deploymentBefore = trace.CaptureDecision();
                advanceTurn();
                return false;
            }
            if (before == null)
            {
                int count = CaptureRoster(rooms.GetRoom(Floor)).Count(unit => unit.Team == "Monsters" && unit.Movable);
                if (deploymentBefore == null || combat.GetTurnCount() != 1 || count != RequiredHosts)
                    throw new InvalidOperationException("Expected " + RequiredHosts + " original movable hosts at the first ordinary decision.");
                before = trace.CaptureDecision();
                advanceTurn();
                return false;
            }
            if (combat.GetTurnCount() != 2)
                throw new InvalidOperationException("The original first ordinary EndTurn did not settle.");
            var positive = Applications.SingleOrDefault(row => !row.Preview && row.Turn == 1 && row.Room == Floor);
            if (Applications.Any(row => !row.Preview && row.Turn == 0) ||
                positive == null || positive.After == null || positive.RngAfter == null ||
                positive.RngBefore.SequenceEqual(positive.RngAfter) ||
                !(ThreeHosts ? MovedThreeHosts(positive) : MovedHosts(positive)))
                throw new InvalidOperationException("Expected original upper-floor hosts to change physical points, advance Battle RNG and dispatch Shift.");
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = ThreeHosts ? 2 : 1, Boundary = "OriginalShuffleUnits", GameVersion = UnityEngine.Application.version, GameModuleMvid = Module,
                Owner = new { Id = OwnerId, AssetKey = OwnerKey, EffectType = effect!.GetType().Name,
                    DisallowedInPlacementPhase = true, effect.CanApplyInPreviewMode },
                DeploymentBefore = deploymentBefore, Before = before, After = trace.CaptureDecision(), Tests, Applications
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                ThreeHosts ? "shuffle-units-three-hosts.mt2f" : "shuffle-units.mt2f"));
            archive.Write(stream);
            log.LogInfo("ORIGINAL-SHUFFLE-UNITS OBSERVED room=" + positive.Room + " callbacks=" + positive.Callbacks.Count);
            return true;
        }

        private static bool TryPlaceHost(AllGameManagers managers, string? originalCard)
        {
            var cards = managers.GetCardManager();
            var drop = managers.GetRoomManager().GetRoom(Floor).GetFirstEmptyMonsterPoint();
            if (drop == null) return false;
            var hand = cards.GetHand();
            foreach (var card in hand.Where(card => card.GetCardType() == CardType.Monster &&
                (originalCard == null || card.GetCardDataID() == originalCard)))
            {
                if (!cards.CanPlayHandCard(card, Floor, drop, null, null, out var error)) continue;
                if (!cards.PlayCard(hand.IndexOf(card), drop, ref error))
                    throw new InvalidOperationException("Original host placement was rejected: " + error);
                return true;
            }
            return false;
        }

        private static bool MovedHosts(Application row)
        {
            var original = row.Before.Where(unit => unit.Team == "Monsters" && unit.Movable).ToArray();
            var actual = row.After!.Where(unit => unit.Team == "Monsters" && unit.Movable).ToArray();
            if (original.Length != 2 || actual.Length != 2 || original.Any(unit => !unit.Point.HasValue)) return false;
            return actual.Any(unit => unit.Actor == original[0].Actor && unit.Point == original[1].Point) &&
                actual.Any(unit => unit.Actor == original[1].Actor && unit.Point == original[0].Point) &&
                original.All(unit => row.Callbacks.Any(callback => callback.Kind == "ShiftCharacterTrigger" && callback.Actor == unit.Actor));
        }

        private static uint[] BattleRng() => RngCalibration.Words(RandomManager.GetState(RngId.Battle));

        private static bool MovedThreeHosts(Application row)
        {
            var original = row.Before.Where(unit => unit.Team == "Monsters" && unit.Movable).ToArray();
            var actual = row.After!.Where(unit => unit.Team == "Monsters" && unit.Movable).ToArray();
            return original.Length == 3 && actual.Length == 3 && original.Any(unit =>
                actual.Any(next => next.Actor == unit.Actor && next.Point != unit.Point)) &&
                original.All(unit => row.Callbacks.Any(callback => callback.Kind == "ShiftCharacterTrigger" && callback.Actor == unit.Actor)) &&
                row.RangeCount >= 2 && row.PointCount >= 4;
        }

        private sealed class Unit
        {
            public Unit(int actor, string team, int? point, bool movable, bool alive) =>
                (Actor, Team, Point, Movable, Alive) = (actor, team, point, movable, alive);
            public int Actor { get; }
            public string Team { get; }
            public int? Point { get; }
            public bool Movable { get; }
            public bool Alive { get; }
        }

        private sealed class Callback
        {
            public Callback(string kind, int actor, int? point) => (Kind, Actor, Point) = (kind, actor, point);
            public string Kind { get; }
            public int Actor { get; }
            public int? Point { get; }
        }

        private sealed class Application
        {
            public int Frame { get; set; }
            public int Turn { get; set; }
            public int Room { get; set; }
            public bool Preview { get; set; }
            public int SelectedBefore { get; set; }
            public int? SelectedAfter { get; set; }
            public Unit[] Before { get; set; } = null!;
            public Unit[]? After { get; set; }
            public uint[] RngBefore { get; set; } = null!;
            public uint[]? RngAfter { get; set; }
            public List<Callback> Callbacks { get; } = new();
            public List<object> Events { get; } = new();
            public bool ShouldSerializeEvents() => ThreeHosts;
            internal int RangeCount { get; set; }
            internal int PointCount { get; set; }
        }

        private static Unit[] CaptureRoster(RoomState room)
        {
            var trace = FullBattleTrace.Active!;
            var roster = new List<Unit>();
            foreach (var team in new[] { Team.Type.Monsters, Team.Type.Heroes })
            {
                var units = new List<CharacterState>();
                var movable = new List<CharacterState>();
                room.AddCharactersToList(units, team);
                room.AddMovableCharactersToList(movable, team);
                foreach (var unit in units)
                    roster.Add(new Unit(trace.UnitId(unit), team.ToString(), unit.GetSpawnPoint()?.GetIndexInRoom(),
                        movable.Contains(unit), unit.IsAlive));
            }
            return roster.ToArray();
        }

        private static IEnumerator Observe(IEnumerator routine, RoomState room)
        {
            var managers = AllGameManagers.Instance!;
            var row = new Application
            {
                Frame = Time.frameCount, Turn = managers.GetCombatManager().GetTurnCount(), Room = room.GetRoomIndex(),
                Preview = managers.GetSaveManager().PreviewMode, SelectedBefore = managers.GetRoomManager().GetSelectedRoom(),
                RngBefore = BattleRng(), Before = CaptureRoster(room)
            };
            Applications.Add(row);
            var previous = active;
            active = row;
            try { yield return routine; }
            finally { active = previous; }
            row.SelectedAfter = managers.GetRoomManager().GetSelectedRoom();
            row.RngAfter = BattleRng();
            row.After = CaptureRoster(room);
        }

        [HarmonyPatch(typeof(RelicEffectShuffleUnitsEndOfTurn), nameof(RelicEffectShuffleUnitsEndOfTurn.TestEffectEndOfTurn))]
        private static class TestPatch
        {
            private static bool Prepare() => Enabled;
            private static void Postfix(RelicEffectShuffleUnitsEndOfTurn __instance, EndOfTurnRelicEffectParams relicEffectParams, bool __result)
            {
                if (deploymentBefore == null || !ReferenceEquals(__instance, effect) || relicEffectParams.roomState == null) return;
                var managers = AllGameManagers.Instance!;
                var room = relicEffectParams.roomState;
                Tests.Add(new { Frame = Time.frameCount, Turn = managers.GetCombatManager().GetTurnCount(), Room = room.GetRoomIndex(),
                    Preview = managers.GetSaveManager().PreviewMode, Result = __result, Roster = CaptureRoster(room) });
            }
        }

        [HarmonyPatch(typeof(RelicEffectShuffleUnitsEndOfTurn), nameof(RelicEffectShuffleUnitsEndOfTurn.ApplyEffectEndOfTurn))]
        private static class ApplyPatch
        {
            private static bool Prepare() => Enabled;
            private static void Postfix(RelicEffectShuffleUnitsEndOfTurn __instance, EndOfTurnRelicEffectParams relicEffectParams,
                ref IEnumerator __result)
            {
                if (deploymentBefore != null && ReferenceEquals(__instance, effect) && relicEffectParams.roomState != null)
                    __result = Observe(__result, relicEffectParams.roomState);
            }
        }

        private static void RecordCallback(string kind, CharacterState actor)
        {
            if (active == null || actor.GetCurrentRoomIndex() != active.Room ||
                AllGameManagers.Instance!.GetSaveManager().PreviewMode != active.Preview) return;
            var callback = new Callback(kind, FullBattleTrace.Active!.UnitId(actor), actor.GetSpawnPoint()?.GetIndexInRoom());
            active.Callbacks.Add(callback);
            if (ThreeHosts) active.Events.Add(callback);
        }

        [HarmonyPatch(typeof(RandomManager), nameof(RandomManager.Range), new[] { typeof(int), typeof(int), typeof(RngId) })]
        private static class RangePatch
        {
            private static bool Prepare() => Enabled && ThreeHosts;
            private static void Prefix(RngId rngId, out uint[]? __state) => __state =
                rngId == RngId.Battle && DetailedApplication() != null ? BattleRng() : null;

            private static void Postfix(int min, int max, int __result, uint[]? __state)
            {
                if (__state == null) return;
                active!.RangeCount++;
                active.Events.Add(new
                {
                    Kind = "BattleRange", Min = min, Max = max, Result = __result, Before = __state, After = BattleRng()
                });
            }
        }

        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.SetSpawnPoint), new[] { typeof(SpawnPoint), typeof(bool),
            typeof(bool), typeof(Action), typeof(float) })]
        private static class PointPatch
        {
            private static bool Prepare() => Enabled && ThreeHosts;
            private static void Prefix(CharacterState __instance, SpawnPoint setSpawnPoint, bool animate, out int? __state)
            {
                __state = null;
                var row = DetailedApplication();
                if (row == null || !animate || setSpawnPoint == null || __instance.GetCurrentRoomIndex() != row.Room ||
                    setSpawnPoint.GetRoomOwner().OrNull()?.GetRoomIndex() != row.Room) return;
                __state = __instance.GetSpawnPoint()?.GetIndexInRoom();
            }

            private static void Postfix(CharacterState __instance, int? __state)
            {
                if (!__state.HasValue) return;
                active!.PointCount++;
                active.Events.Add(new
                {
                    Kind = "SetSpawnPoint", Actor = FullBattleTrace.Active!.UnitId(__instance), BeforePoint = __state.Value,
                    AfterPoint = __instance.GetSpawnPoint()?.GetIndexInRoom()
                });
            }
        }

        private static Application? DetailedApplication() =>
            active != null && AllGameManagers.Instance!.GetSaveManager().PreviewMode == active.Preview ? active : null;

        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.CharacterFloorRearranged))]
        private static class RearrangedPatch
        {
            private static bool Prepare() => Enabled;
            private static void Prefix(CharacterState characterState) => RecordCallback("CharacterFloorRearranged", characterState);
        }

        [HarmonyPatch(typeof(HeroManager), nameof(HeroManager.ShiftCharacterTrigger))]
        private static class ShiftPatch
        {
            private static bool Prepare() => Enabled;
            private static void Prefix(CharacterState charState) => RecordCallback("ShiftCharacterTrigger", charState);
        }
    }
}
#endif
