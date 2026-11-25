#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalArmoredUnitRecallWitness
    {
        internal const string Boundary = "OriginalArmoredUnitRecall";
        internal const string FileName = "armored-unit-recall.mt2f";
        private const int Floor = 1;
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ARMORED_UNIT_RECALL") == "1";
        private static CardState? steward, welder, relay;
        private static BattleTurnState? before, after;
        private static PlayCardAction? recall, replay;
        private static int survivor;

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers, Action advanceTurn)
        {
            var combat = managers.GetCombatManager();
            var save = managers.GetSaveManager();
            var rooms = managers.GetRoomManager();
            var cards = managers.GetCardManager();
            if (Application.version != "2.3.1" || typeof(CardState).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.InTestScenario || save.GetCurrentDistance() != 0 ||
                save.GetCovenants().Count != 0 || save.GetMutators().Count != 0)
                throw new InvalidOperationException("Expected the original 2.3.1 first battle.");
            if (rooms.GetSelectedRoom() != Floor)
            {
                save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(Floor));
                return false;
            }
            var room = rooms.GetRoom(Floor);
            if (combat.IsPlacementPhase)
            {
                if (steward == null)
                {
                    steward = cards.GetHand().First(card => card.GetCardDataID() == MagneticRelayScenario.StewardId);
                    Play(cards, steward, room.GetMonsterPoint(0));
                }
                else
                {
                    save.MarkOneTimeMessageComplete(OneTimeMessage.EndTurnWithoutPlayingChampion);
                    advanceTurn();
                }
                return false;
            }
            if (steward == null || combat.GetTurnCount() != 1)
                throw new InvalidOperationException("Expected the first ordinary turn after deploying the original Train Steward.");
            if (welder == null)
            {
                var data = save.GetAllGameData();
                welder = cards.AddNewCard(data.FindCardData(TranscendImpScenario.WelderHelperDataId), CardPile.HandPile,
                    false, false, animate: false);
                relay = cards.AddNewCard(data.FindCardData(MagneticRelayScenario.DataId), CardPile.HandPile,
                    false, false, animate: false);
                return false;
            }
            if (cards.GetHand().Contains(welder))
            {
                Play(cards, welder, room.GetMonsterPoint(1));
                return false;
            }
            if (recall == null)
            {
                var actors = new List<CharacterState>();
                room.AddCharactersToList(actors, Team.Type.Monsters);
                var target = actors.Single(actor => ReferenceEquals(actor.GetSpawnerCard(), steward));
                var source = actors.Single(actor => ReferenceEquals(actor.GetSpawnerCard(), welder));
                if (target.GetStatusEffectStacks("armor") <= 0 || !ReferenceEquals(target.GetSpawnPoint(), room.GetMonsterPoint(0)) ||
                    !ReferenceEquals(source.GetSpawnPoint(), room.GetMonsterPoint(1)))
                    throw new InvalidOperationException("Original Welder Helper did not armor the physical front unit.");
                before = trace.CaptureDecision();
                survivor = trace.UnitId(source);
                recall = new PlayCardAction(trace.CardId(relay!), Floor, targetUnitId: trace.UnitId(target));
                Play(cards, relay!, target.GetSpawnPoint());
                return false;
            }
            if (replay == null)
            {
                after = trace.CaptureDecision();
                var context = after.Spawn.Train.Context!;
                if (!cards.GetHand().Contains(steward) || context.SpawnPoints!.Group(Floor, CombatTeam.Player)!.Occupants[0] != survivor)
                    throw new InvalidOperationException("Original Magnetic Relay did not return the source and compact the remaining unit.");
                replay = new PlayCardAction(trace.CardId(steward), Floor, 1);
                Play(cards, steward, room.GetMonsterPoint(1));
                return false;
            }
            var redeployed = trace.CaptureDecision();
            var unit = redeployed.Spawn.Train.Rooms.Single(item => item.RoomIndex == Floor).Units
                .Single(item => item.SpawnerCardId == replay.CardInstanceId);
            if (unit.Id == recall.TargetUnitId || unit.Status("armor")?.Stacks > 0)
                throw new InvalidOperationException("Original recalled unit carried living Armor into redeployment.");
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary, GameVersion = Application.version, GameModuleMvid = Module,
                Before = before, Action = recall, After = after, Survivor = survivor, RedeployAction = replay, Redeployed = redeployed
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, FileName));
            archive.Write(stream);
            return true;
        }

        private static void Play(CardManager cards, CardState card, SpawnPoint drop)
        {
            if (!cards.CanPlayHandCard(card, Floor, drop, null, null, out var error) ||
                !cards.PlayCard(cards.GetHand().IndexOf(card), drop, ref error))
                throw new InvalidOperationException($"Original {card.GetCardDataID()} play was rejected: {error}.");
        }
    }
}
#endif
