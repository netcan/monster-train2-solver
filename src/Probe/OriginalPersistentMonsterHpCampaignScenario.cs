#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPersistentMonsterHpCampaignScenario
    {
        private const string OwnerId = RelicPersistentMonsterHealthModel.OwnerId;
        private const string CardDataId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private const int ReservedCardId = 2;
        private const int BlockerCardId = 1;
        private const string BlockerDataId = "19705414-816d-4549-bcf5-866f338a551f";
        internal static bool Continue => Environment.GetEnvironmentVariable("MT2_PROBE_PERSISTENT_HP_CAMPAIGN") == "1";
        internal static bool Reload => Environment.GetEnvironmentVariable("MT2_PROBE_PERSISTENT_HP_RELOAD") == "1";

        private static bool reservedPlayed;
        private static int preEndTurnHealth;
        private static int storedHealth;
        private static readonly List<(CardStateModifiers Card, int Health)> postVictoryWrites = new List<(CardStateModifiers, int)>();
        private static CardState? reloadCard;
        private static PlayCardAction? reloadAction;
        private static BattleTurnState? beforeReload;

        internal static void RequireAvailable(SaveManager save, MutatorData definition)
        {
            if (!save.IsDlcAvailableWhenStartingRun(definition.GetRequiredDLC()))
                throw new InvalidOperationException("The original persistent-health mutator is not available for this run's DLC.");
        }

        internal static PlayCardAction? ChooseFirstBattle(BattleTurnState decision)
        {
            if (decision.Spawn.Turn == 0 && !decision.Spawn.Train.Context!.Cards.Hand.Any(card =>
                card.InstanceId == ReservedCardId && card.DataId == CardDataId))
                throw new InvalidOperationException("The fixed-seed original Train Steward reserve changed.");

            if (decision.Spawn.Turn == 0 && decision.Spawn.Train.Context!.Cards.Hand.Any(card =>
                card.InstanceId == BlockerCardId && card.DataId == BlockerDataId))
            {
                PlayCardAction? blocker = BattleActionModel.EnumerateSupportedPlays(decision, BlockerCardId)
                    .FirstOrDefault(action => action.RoomIndex == 2 && action.PlayerPosition >= 0);
                if (blocker == null) throw new InvalidOperationException("The original top-floor champion blocker is not legal.");
                return blocker;
            }

            if (!reservedPlayed && decision.Spawn.Turn >= 6 && decision.Spawn.Train.Context!.Cards.Hand.Any(card =>
                card.InstanceId == ReservedCardId && card.DataId == CardDataId))
            {
                RoomCombatState top = decision.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2);
                bool blocked = top.Units.Any(unit =>
                    unit.Team == CombatTeam.Player && unit.SpawnerCardId == BlockerCardId && unit.Health > 0);
                if (!blocked) throw new InvalidOperationException("The original top-floor blocker did not survive to the final turn.");
                int rear = top.Units.Count(unit => unit.Team == CombatTeam.Player && unit.Health > 0);
                PlayCardAction? paid = BattleActionModel.EnumerateSupportedPlays(decision, ReservedCardId)
                    .FirstOrDefault(action => action.RoomIndex == 2 && action.PlayerPosition == rear);
                if (paid == null) throw new InvalidOperationException("The original reserved Steward cannot be paid behind its top-floor blocker.");
                reservedPlayed = true;
                return paid;
            }

            return BattleActionModel.ChooseUnitSpellAndJunkPlay(decision,
                card => reservedPlayed || card.InstanceId != ReservedCardId);
        }

        internal static void RequireTerminal(FullBattleTrace trace)
        {
            BattleTurnState before = trace.ObservedTurns.Last().Before;
            CombatUnit[] retained = before.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(unit => unit.Team == CombatTeam.Player && unit.SpawnerCardId == ReservedCardId && unit.Health > 0)
                .ToArray();
            if (!reservedPlayed || trace.NativeWon != true || retained.Length != 1 ||
                before.Spawn.Train.Context?.FindCard(ReservedCardId)?.Permanent.PersistentHealth != 0 ||
                !before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2).Units.Any(unit =>
                    unit.Team == CombatTeam.Player && unit.SpawnerCardId == BlockerCardId && unit.Health > 0))
                throw new InvalidOperationException("The original final EndTurn lacks the paid Steward behind its live blocker.");
            preEndTurnHealth = retained[0].Health;
        }

        internal static void CaptureCheckpoint(SaveManager save, string destination, string saveHash)
        {
            if (!Continue || preEndTurnHealth <= 0 || save.GetCurrentDistance() != 1 ||
                save.GetRunState().GetNumBattlesCompleted() != 1 || SaveManager.WasDevCheatUsed)
                throw new InvalidOperationException("The controlled Ring 2 checkpoint has no settled original battle.");
            MutatorData definition = save.GetAllGameData().FindMutatorData(OwnerId)
                ?? throw new InvalidOperationException("The original mutator disappeared after battle.");
            RequireAvailable(save, definition);
            if (save.GetMutators().Count(owner => owner.GetRelicDataID() == OwnerId) != 1)
                throw new InvalidOperationException("The original persistent-health owner was not retained.");

            string path = Path.Combine(destination, "saves", "save-singlePlayer.json");
            JObject disk = JObject.Parse(File.ReadAllText(path));
            JArray deck = (JArray?)disk["deckState"] ?? throw new InvalidOperationException("The Ring 2 save has no deck.");
            int[] stored = deck.Where(card => (string?)card["cardDataID"] == CardDataId)
                .Select(card => (int?)card["cardModifiers"]?["persistentHP"] ?? -1).ToArray();
            int[] live = save.GetDeckState().Where(card => card.GetCardDataID() == CardDataId)
                .Select(card => card.GetCardStateModifiers().GetPersistentHP()).ToArray();
            if (stored.Length != 2 || !stored.SequenceEqual(live) ||
                stored.Count(hp => hp > 0 && hp <= preEndTurnHealth) != 1 || stored.Count(hp => hp == 0) != 1 ||
                disk.Value<int?>("gameSequence") != (int)SaveData.GameSequence.BattleIntro ||
                disk.Value<bool?>("wasDevCheatUsed") != false)
                throw new InvalidOperationException("The native Ring 2 save did not persist the retained Steward's positive HP.");
            CardState carrier = save.GetDeckState().Single(card => card.GetCardDataID() == CardDataId &&
                card.GetCardStateModifiers().GetPersistentHP() > 0);
            int savedHealth = carrier.GetCardStateModifiers().GetPersistentHP();
            int matchedWrites = postVictoryWrites.Count(write =>
                ReferenceEquals(write.Card, carrier.GetCardStateModifiers()) && write.Health == savedHealth);
            if (matchedWrites != 1)
                throw new InvalidOperationException("Original end-of-battle persistent-HP writes=" + postVictoryWrites.Count +
                    ", matching saved card writes=" + matchedWrites + ", saved HP=" + savedHealth);

            string root = Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!;
            string receipt = Path.Combine(root, "persistent-monster-hp-campaign-checkpoint.mt2f");
            using (var archive = NativeFixtureCapture.Capture(new {
                Schema = 1, Boundary = "OriginalPersistentMonsterHealthCampaignCheckpoint",
                GameVersion = Application.version, GameModuleMvid = typeof(RelicEffectPersistMonsterHp).Module.ModuleVersionId,
                OwnerId, RequiredDlc = definition.GetRequiredDLC().ToString(), DlcAvailable = true,
                CardDataId, ReservedCardId, PreEndTurnHealth = preEndTurnHealth, SavedHealth = savedHealth,
                EndOfBattleMatchedWrites = matchedWrites, StoredHealth = stored,
                SaveSha256 = saveHash.ToLowerInvariant(), RunId = disk.Value<string>("id"),
                Battles = save.GetRunState().GetNumBattlesCompleted(), Distance = save.GetCurrentDistance()
            }))
            using (var stream = File.Create(receipt)) archive.Write(stream);
            File.Copy(receipt, Path.Combine(destination, Path.GetFileName(receipt)));
        }

        internal static void ValidateReloadInput(SaveManager save)
        {
            if (!Reload || save.GetCurrentDistance() != 1 || save.GetRunState().GetNumBattlesCompleted() != 1 ||
                save.GetRunType() != RunType.Class || SaveManager.WasDevCheatUsed)
                throw new InvalidOperationException("Expected an uncheated original Ring 2 Class checkpoint.");
            MutatorData definition = save.GetAllGameData().FindMutatorData(OwnerId)
                ?? throw new InvalidOperationException("The original persistent-health mutator is missing.");
            RequireAvailable(save, definition);
            if (save.GetMutators().Count(owner => owner.GetRelicDataID() == OwnerId) != 1)
                throw new InvalidOperationException("The reloaded run lost its original persistent-health owner.");
            int[] values = save.GetDeckState().Where(card => card.GetCardDataID() == CardDataId)
                .Select(card => card.GetCardStateModifiers().GetPersistentHP()).ToArray();
            if (values.Length != 2 || values.Count(value => value > 0) != 1 || values.Count(value => value == 0) != 1)
                throw new InvalidOperationException("The reloaded deck has no unique positive original Steward HP.");
            storedHealth = values.Single(value => value > 0);
        }

        internal enum ReloadStep { Wait, Play, EndTurn, Complete }

        internal static ReloadStep TickReload(FullBattleTrace trace, AllGameManagers managers, out PlayCardAction? action)
        {
            action = null;
            CardManager cards = managers.GetCardManager();
            CombatManager combat = managers.GetCombatManager();
            if (beforeReload != null)
            {
                var units = new System.Collections.Generic.List<CharacterState>();
                managers.GetRoomManager().GetRoom(reloadAction!.RoomIndex).AddCharactersToList(units, Team.Type.Monsters);
                CharacterState actor = units.Single(unit => unit.IsAlive && !unit.IsDestroyed &&
                    ReferenceEquals(unit.GetSpawnerCard(), reloadCard));
                BattleTurnState after = trace.CaptureDecision();
                if (actor.GetHP() != storedHealth || actor.GetMaxHP() < storedHealth ||
                    reloadCard!.GetCardStateModifiers().GetPersistentHP() != storedHealth)
                    throw new InvalidOperationException("The reloaded original paid birth did not restore the saved HP.");
                string path = Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                    "persistent-monster-hp-campaign-reload.mt2f");
                using (var archive = NativeFixtureCapture.Capture(new {
                    Schema = 1, Boundary = "OriginalPersistentMonsterHealthCampaignReload",
                    GameVersion = Application.version, GameModuleMvid = typeof(RelicEffectPersistMonsterHp).Module.ModuleVersionId,
                    OwnerId, CardDataId, StoredHealth = storedHealth, Action = reloadAction,
                    SaveSha256 = Environment.GetEnvironmentVariable("MT2_PROBE_PERSISTENT_HP_INPUT_SHA256"),
                    RunId = Environment.GetEnvironmentVariable("MT2_PROBE_PERSISTENT_HP_INPUT_RUN_ID"),
                    Before = beforeReload, After = after, ActorId = trace.UnitId(actor),
                    ActorHealth = actor.GetHP(), ActorMaxHealth = actor.GetMaxHP(), Turn = combat.GetTurnCount()
                }))
                using (var stream = File.Create(path)) archive.Write(stream);
                return ReloadStep.Complete;
            }

            if (combat.GetTurnCount() > 3)
                throw new InvalidOperationException("The persisted original Steward was not drawn by turn three.");
            CardState? card = cards.GetHand().SingleOrDefault(candidate => candidate.GetCardDataID() == CardDataId &&
                candidate.GetCardStateModifiers().GetPersistentHP() == storedHealth);
            if (card == null) return ReloadStep.EndTurn;

            BattleTurnState before = trace.CaptureDecision();
            int id = trace.CardId(card);
            action = BattleActionModel.EnumerateSupportedPlays(before, id)
                .FirstOrDefault(play => play.PlayerPosition >= 0 && play.RoomIndex < 3);
            if (action == null) throw new InvalidOperationException("The saved original Steward has no legal paid deployment.");
            beforeReload = before; reloadCard = card; reloadAction = action;
            return ReloadStep.Play;
        }

        [HarmonyPatch(typeof(CardStateModifiers), nameof(CardStateModifiers.SetPersistentHP))]
        private static class PostVictoryWritePatch
        {
            private static void Postfix(CardStateModifiers __instance, int __0)
            {
                if (!Continue) return;
                bool endOfBattle = new StackTrace().GetFrames()?.Any(frame =>
                {
                    var method = frame.GetMethod();
                    return method?.DeclaringType?.FullName?.Contains(nameof(RelicEffectPersistMonsterHp)) == true &&
                        (method.Name.Contains("ApplyEffectEndOfBattle") ||
                         method.DeclaringType.FullName.Contains("ApplyEffectEndOfBattle"));
                }) == true;
                if (endOfBattle) postVictoryWrites.Add((__instance, __0));
            }
        }
    }
}
#endif
