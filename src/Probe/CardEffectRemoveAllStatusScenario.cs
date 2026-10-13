using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardEffectRemoveAllStatusScenario
    {
        private const string CardId = "974f9021-d1e3-4b8b-b348-d737a1868ed1";
        private static ManualLogSource? logger;
        private static int fullCardInstanceId;
        private static int halfCardInstanceId;
        private static bool fullActionChosen;
        private static bool halfActionChosen;
        private static bool fullActionRecorded;
        private static bool halfActionRecorded;
        private static CharacterState? playerTarget;
        private static CharacterState? enemyTarget;
        private static string? hiddenStatusId;
        private static Team.Type hiddenStatusTeam;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "remove-all-status-effects";
        internal static bool Prepared { get; private set; }

        internal static void Prepare(ManualLogSource log)
        {
            logger = log;
            fullCardInstanceId = 0;
            halfCardInstanceId = 0;
            fullActionChosen = false;
            halfActionChosen = false;
            fullActionRecorded = false;
            halfActionRecorded = false;
            playerTarget = null;
            enemyTarget = null;
            hiddenStatusId = null;
            Prepared = true;
            log.LogInfo("REMOVE-ALL-STATUS-PREPARED native ResinRemoval full and half category removal");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (!Prepared || halfActionChosen) return null;
            CardManager cards = AllGameManagers.Instance!.GetCardManager()!;
            if (fullCardInstanceId == 0)
            {
                if (!TryGetTargets(source, out RoomState? nativeRoom, out CharacterState? player, out CharacterState? enemy) ||
                    !cards.GetBelowHandSize()) return null;
                CardData definition = AllGameManagers.Instance.GetSaveManager().GetAllGameData().FindCardData(CardId) ??
                    throw new InvalidOperationException("Remove-all status fixture requires native ResinRemoval.");
                CardState added = AddCard(cards, definition);
                playerTarget = player;
                enemyTarget = enemy;
                SeedStatuses(AllGameManagers.Instance, playerTarget!, enemyTarget!);
                fullCardInstanceId = FullBattleTrace.Active!.CardId(added);
                SetCombatStateChanged(AllGameManagers.Instance);
                source = CaptureDecision();
                logger!.LogInfo("REMOVE-ALL-STATUS-SEEDED card=" + fullCardInstanceId + " room=" + nativeRoom!.GetRoomIndex() +
                    " player=" + FullBattleTrace.Active.UnitId(playerTarget!) + " enemy=" + FullBattleTrace.Active.UnitId(enemyTarget!) +
                    " hidden=" + (hiddenStatusId ?? "<none>"));
            }

            if (halfCardInstanceId == 0 && source.Spawn.Train.Context!.Cards.Hand.All(card => card.InstanceId != fullCardInstanceId))
            {
                CardData definition = AllGameManagers.Instance.GetSaveManager().GetAllGameData().FindCardData(CardId)!;
                foreach (CardEffectData effect in definition.GetEffects().Where(effect => effect.GetEffectStateName() == "CardEffectRemoveAllStatusEffects"))
                    Set(effect, "paramBool", true);
                if (!cards.GetBelowHandSize()) return null;
                CardState added = AddCard(cards, definition);
                halfCardInstanceId = FullBattleTrace.Active!.CardId(added);
                SeedStatuses(AllGameManagers.Instance, playerTarget!, enemyTarget!);
                SetCombatStateChanged(AllGameManagers.Instance);
                source = CaptureDecision();
                logger!.LogInfo("REMOVE-ALL-STATUS-HALF-PREPARED card=" + halfCardInstanceId + " removeHalf=true");
            }

            int targetCard = fullActionChosen ? halfCardInstanceId : fullCardInstanceId;
            if (!source.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == targetCard)) return null;
            var action = new PlayCardAction(targetCard, 0);
            BattleActionResult predicted = BattleActionModel.PlayCard(source, action);
            if (!predicted.Supported)
            {
                logger!.LogInfo((targetCard == fullCardInstanceId ? "REMOVE-ALL-STATUS-FULL-PLAY" : "REMOVE-ALL-STATUS-HALF-PLAY") +
                    " supported=False reason=" + predicted.Reason);
                return null;
            }
            if (targetCard == fullCardInstanceId) fullActionChosen = true;
            else halfActionChosen = true;
            logger!.LogInfo((targetCard == fullCardInstanceId ? "REMOVE-ALL-STATUS-FULL-PLAY" : "REMOVE-ALL-STATUS-HALF-PLAY") + " supported=True");
            return action;
        }

        internal static bool ConsumeScenarioActionMark(PlayCardAction action)
        {
            if (action.CardInstanceId == fullCardInstanceId && !fullActionRecorded)
            {
                fullActionRecorded = true;
                return true;
            }
            if (action.CardInstanceId == halfCardInstanceId && !halfActionRecorded)
            {
                halfActionRecorded = true;
                return true;
            }
            return false;
        }

        private static bool TryGetTargets(BattleTurnState source, out RoomState? nativeRoom,
            out CharacterState? player, out CharacterState? enemy)
        {
            AllGameManagers managers = AllGameManagers.Instance!;
            nativeRoom = managers.GetRoomManager()!.GetRoom(0);
            var characters = new System.Collections.Generic.List<CharacterState>();
            nativeRoom.AddCharactersToList(characters, Team.Type.Heroes | Team.Type.Monsters);
            player = characters.FirstOrDefault(unit => unit.GetTeamType() == Team.Type.Monsters && !unit.IsPyreHeart());
            enemy = characters.FirstOrDefault(unit => unit.GetTeamType() == Team.Type.Heroes && !unit.IsPyreHeart());
            return source.Spawn.Train.Rooms.Any(room => room.RoomIndex == 0 &&
                room.Units.Any(unit => unit.Team == CombatTeam.Player && !unit.IsPyre) &&
                room.Units.Any(unit => unit.Team == CombatTeam.Enemy && !unit.IsPyre)) && player != null && enemy != null;
        }

        private static CardState AddCard(CardManager cards, CardData definition)
        {
            CardState added = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
            if (added == null || !cards.GetHand().Contains(added))
                throw new InvalidOperationException("Remove-all status fixture could not place ResinRemoval in hand.");
            AccessTools.Field(typeof(CardState), "cost").SetValue(added, 0);
            return added;
        }

        private static void SeedStatuses(AllGameManagers managers, CharacterState player, CharacterState enemy)
        {
            RegisterHiddenPositiveStatus(managers);
            AddStatus(player, "armor", 4);
            AddStatus(player, "poison", 3);
            AddStatus(enemy, "armor", 4);
            AddStatus(enemy, "poison", 3);
            if (hiddenStatusId != null)
                AddStatus(hiddenStatusTeam == Team.Type.Heroes ? enemy : player, hiddenStatusId, 2);
        }

        private static void RegisterHiddenPositiveStatus(AllGameManagers managers)
        {
            const string id = "buff";
            StatusEffectManager manager = managers.GetStatusEffectManager();
            StatusEffectData status = manager.GetStatusEffectDataById(id) ??
                throw new InvalidOperationException("Remove-all status fixture requires native attack-buff status data.");
            if (status.GetDisplayCategory() != StatusEffectData.DisplayCategory.Positive)
                throw new InvalidOperationException("Native attack-buff status is not in the positive display category.");
            Set(status, "hidden", true);
            hiddenStatusId = id;
            hiddenStatusTeam = Team.Type.Heroes;
        }

        private static void AddStatus(CharacterState target, string id, int amount) =>
            target.AddStatusEffect(id, amount, triggeredByCharacter: null, allowModification: false, isFromHiddenTrigger: true);

        private static BattleTurnState CaptureDecision()
        {
            FullBattleTrace.Active!.InvalidateCardRules();
            return FullBattleTrace.Active.CaptureDecision();
        }

        private static void SetCombatStateChanged(AllGameManagers managers) =>
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);

        private static void Set(object target, string field, object value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
