#if DEVELOPMENT_PROBE
using System;
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
    internal static class OriginalRandomJustCauseWitness
    {
        private const string CardId = "c8b39244-83c1-4eb3-b474-8898226d4280";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_RANDOM_JUST_CAUSE_WITNESS") == "1";
        private static CardState? nativeCard;
        private static BattleTurnState? before;
        private static PlayCardAction? action;
        private static int[]? candidates;
        private static int floor = -1;
        private static int moveTarget, statusTarget;

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager();
            var cards = managers.GetCardManager();
            var rooms = managers.GetRoomManager();
            if (!OriginalRandomSpellTargetsWitness.Acquired || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 3)
                throw new InvalidOperationException("Expected the original turn-three Just Cause decision.");
            if (before == null)
            {
                if (floor < 0)
                {
                    BattleTurnState ready = trace.CaptureDecision();
                    CombatContext context = ready.Spawn.Train.Context!;
                    var eligible = ready.Spawn.Train.Rooms.Where(room => room.RoomIndex != ready.Spawn.Train.Rooms.Count - 1 &&
                        ready.PlayRules!.Rooms.Any(rule => rule.RoomIndex == room.RoomIndex && rule.Enabled))
                        .Select(room => new { room.RoomIndex, Units = room.Units.Where(unit => unit.Health > 0 &&
                            !unit.IsPyre && unit.Status("untouchable") == null).ToArray() }).ToArray();
                    floor = eligible.FirstOrDefault(room => room.Units.Length >= 2 &&
                        room.Units.All(unit => unit.Team == room.Units[0].Team) &&
                        context.BattleRng.Range(0, room.Units.Length).Value > 0)?.RoomIndex ?? -1;
                    if (floor < 0)
                        throw new InvalidOperationException("No original floor can demonstrate a nonfront random target: " +
                            string.Join(";", eligible.Select(room => room.RoomIndex + "=" +
                                string.Join(",", room.Units.Select(unit => unit.Team + "/" + unit.Id)) +
                                (room.Units.Length > 0 ? " pick=" + context.BattleRng.Range(0, room.Units.Length).Value : ""))));
                }
                if (rooms.GetSelectedRoom() != floor)
                {
                    save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(floor));
                    return false;
                }
                nativeCard = cards.GetHand().First(card => card.GetCardDataID() == CardId);
                var effects = nativeCard.GetEffectStates();
                if (!nativeCard.GetTargetless() || effects.Count != 2 ||
                    effects[0].GetCardEffect() is not CardEffectFloorRearrange || effects[0].GetTargetMode() != TargetMode.RandomInRoom ||
                    effects[1].GetCardEffect() is not CardEffectAddStatusEffect || effects[1].GetTargetMode() != TargetMode.LastTargetedCharacters)
                    throw new InvalidOperationException("Original Just Cause did not retain its observed random and last-target modes.");
                var actors = new List<CharacterState>();
                RoomState room = rooms.GetRoom(floor);
                room.AddCharactersToList(actors, Team.Type.Heroes);
                room.AddCharactersToList(actors, Team.Type.Monsters);
                candidates = actors.Where(unit => unit.IsAlive && !unit.IsDestroyed && !unit.HasStatusEffect("untouchable"))
                    .Select(trace.UnitId).ToArray();
                if (candidates.Length < 2)
                    throw new InvalidOperationException("Expected two living native random targets on the selected floor.");
                int handIndex = cards.GetHand().IndexOf(nativeCard);
                if (handIndex < 0 || !cards.CanPlayHandCard(nativeCard, floor, null, null, null, out var error))
                    throw new InvalidOperationException("Original non-targeted Just Cause cannot be played.");
                before = trace.CaptureDecision();
                action = new PlayCardAction(trace.CardId(nativeCard), floor);
                if (!cards.PlayCard(handIndex, null, ref error))
                    throw new InvalidOperationException("Original Just Cause rejected its targetless input: " + error);
                return false;
            }

            BattleTurnState after = trace.CaptureDecision();
            CombatContext prior = before.Spawn.Train.Context!, settled = after.Spawn.Train.Context!;
            RoomCombatState oldRoom = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == floor);
            RoomCombatState newRoom = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == floor);
            if (moveTarget <= 0 || statusTarget != moveTarget || !candidates!.Contains(moveTarget) ||
                oldRoom.Units.Select(unit => unit.Id).SequenceEqual(newRoom.Units.Select(unit => unit.Id)) ||
                (newRoom.Units.Single(unit => unit.Id == moveTarget).Status("valor")?.Stacks ?? 0) <=
                (oldRoom.Units.Single(unit => unit.Id == moveTarget).Status("valor")?.Stacks ?? 0) ||
                prior.BattleRng.Equals(settled.BattleRng))
                throw new InvalidOperationException("Original Just Cause did not move and grant Valor to the same randomly chosen actor.");
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalRandomJustCause", GameVersion = Application.version,
                GameModuleMvid = typeof(CardState).Module.ModuleVersionId, Before = before, Action = action, After = after,
                Candidates = candidates, MoveTarget = moveTarget, StatusTarget = statusTarget
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "random-just-cause.mt2f"));
            archive.Write(stream);
            log.LogInfo("ORIGINAL-RANDOM-JUST-CAUSE PASS candidates=" + candidates.Length + " target=" + moveTarget);
            return true;
        }

        private static int Target(CardEffectParams parameters)
        {
            if (before == null || !ReferenceEquals(parameters.playedCard, nativeCard) ||
                AllGameManagers.Instance.GetSaveManager().PreviewMode) return 0;
            return FullBattleTrace.Active!.UnitId(parameters.targets.Single());
        }

        [HarmonyPatch(typeof(CardEffectFloorRearrange), nameof(CardEffectFloorRearrange.ApplyEffect))]
        private static class Move
        {
            private static bool Prepare() => Enabled;
            private static void Prefix(CardEffectParams cardEffectParams) { if (Target(cardEffectParams) is int target && target > 0) moveTarget = target; }
        }

        [HarmonyPatch(typeof(CardEffectAddStatusEffect), nameof(CardEffectAddStatusEffect.ApplyEffect))]
        private static class Status
        {
            private static bool Prepare() => Enabled;
            private static void Prefix(CardEffectParams cardEffectParams) { if (Target(cardEffectParams) is int target && target > 0) statusTarget = target; }
        }
    }
}
#endif
