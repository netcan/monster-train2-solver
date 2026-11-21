using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelentlessBattleScenario
    {
        internal static string Name => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? "";
        internal static bool Enabled => Name is "relentless-battle-abandon" or "relentless-battle-continue";
        internal static bool Abandon => Name == "relentless-battle-abandon";
        internal static bool Prepared { get; private set; }
        private static IReadOnlyList<RelentlessDecision>? controlledPlan;
        internal static IReadOnlyList<RelentlessDecision>? CurrentPlan =>
#if DEVELOPMENT_PROBE
            ReplayRelentlessChoices.Armed ? ReplayRelentlessChoices.Planned :
#endif
            controlledPlan;
        private static int cardId, currentRoom;
        private static readonly Dictionary<int, int> rounds = new Dictionary<int, int>();
        private static readonly List<object> dialogs = new List<object>();
        private static readonly List<object> roomStates = new List<object>();
        private static int observedTurn = -1;
        internal static object Evidence => new { Prepared, Abandon, ControlCardId = cardId, Dialogs = dialogs, RoomStates = roomStates };
        private static AllGameManagers Managers => AllGameManagers.Instance!;
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (!ConditionalActivationProbe.HasVanguard || managers.GetSaveManager().PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Relentless battle requires isolated 2.3.1 deployment.");
            var all = managers.GetAllGameData(); var cards = managers.GetCardManager();
            CardData steward = all.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678");
            CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "health", 100); Set(unit, "attackDamage", 1); Set(unit, "size", 1);
            Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(unit, "unitAbility", null!);
            foreach (string key in new[] { "MageT1_Junker", "ShieldGuardT1_BasicNonAttack" })
            {
                CharacterData enemy = all.GetAllCharacterData().Single(data => data.name == key);
                Set(enemy, "health", 8); Set(enemy, "attackDamage", 1); Set(enemy, "triggers", new List<CharacterTriggerData>());
                Set(enemy, "startingStatusEffects", key == "MageT1_Junker" ? new[] {
                    new StatusEffectStackData { statusId = "relentless", count = 1 } } : Array.Empty<StatusEffectStackData>());
            }
            Set(managers.GetCombatManager().GetBalanceData(), "relentlessIterationsUntilNotification", 3);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            cardId = FullBattleTrace.Active!.CardId(cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false) ??
                throw new InvalidOperationException("Unable to add Relentless control unit."));
            managers.GetCardStatistics().GetStatValue(new CardStatistics.StatValueData { trackedValue = CardStatistics.TrackedValueType.TimesPlayed,
                cardState = cards.GetHand().Single(), entryDuration = CardStatistics.EntryDuration.ThisBattle, cardTypeTarget = CardStatistics.CardTypeTarget.Any });
            FullBattleTrace.Active.InvalidateCardRules(); Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("RELENTLESS-BATTLE-PREPARED " + (Abandon ? "Abandon" : "Continue") + "; explicit three-round control; original bosses retained.");
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            ObserveTurn(state.Spawn.Turn);
            return state.Spawn.Turn == 0 && state.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == cardId)
                ? new PlayCardAction(cardId, 0, playerPosition: 0) : null;
        }
        internal static void ObserveTurn(int nativeTurn)
        {
            if (observedTurn != nativeTurn)
            {
                observedTurn = nativeTurn;
                roomStates.Add(new { Turn = observedTurn, Enabled = Enumerable.Range(0, Managers.GetRoomManager().GetNumRooms())
                    .Select(index => Managers.GetRoomManager().GetRoom(index).IsRoomEnabled()).ToArray() });
            }
        }
        internal static (int Room, int Rounds) CurrentExchange() => (currentRoom, rounds[currentRoom]);
        internal static void RecordDialog(int turn, RelentlessDecision choice) => dialogs.Add(new
        { Turn = turn, choice.RoomIndex, choice.CompletedRounds, choice.Continue });
        internal static bool IsScenarioAction(PlayCardAction action) => action.CardInstanceId == cardId;
        internal static IReadOnlyList<RelentlessDecision>? Plan(BattleTurnState before, bool record = true)
        {
            if (!Enabled) return null;
            var plan = new List<RelentlessDecision>();
            for (int count = 0; count < 64; count++)
            {
                BattleTurnResult result = BattleTurnModel.EndTurn(before, plan);
                if (result.Supported)
                {
                    var decisions = Array.AsReadOnly(plan.ToArray());
                    if (record) controlledPlan = decisions;
                    return decisions;
                }
                if (result.RelentlessChoice == null) throw new InvalidOperationException("Controlled Relentless turn: " + result.UnsupportedReason);
                plan.Add(result.RelentlessChoice.Choose(!Abandon));
            }
            throw new InvalidOperationException("Controlled Relentless plan exceeded 64 dialogs.");
        }
        private static bool TrackCombat => Enabled
#if DEVELOPMENT_PROBE
            || ReplayRelentlessChoices.Armed
#endif
            ;
        private static bool InstallTracking => Enabled
#if DEVELOPMENT_PROBE
            || ReplayRelentlessChoices.Enabled
#endif
            ;
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.DoRoomCombat))]
        private static class RoomPatch
        {
            private static bool Prepare() => InstallTracking;
            private static void Prefix(RoomState room)
            { if (TrackCombat && !Managers.GetSaveManager().PreviewMode) rounds[room.GetRoomIndex()] = 0; }
        }
        [HarmonyPatch(typeof(CombatManager), "DoUnitCombat")]
        private static class ExchangePatch
        {
            private static bool Prepare() => InstallTracking;
            private static void Prefix(RoomState room)
            { if (TrackCombat && !Managers.GetSaveManager().PreviewMode) { currentRoom = room.GetRoomIndex(); rounds[currentRoom] = rounds.TryGetValue(currentRoom, out int count) ? count + 1 : 1; } }
        }
        [HarmonyPatch(typeof(ScreenManager), "WaitForDialog")]
        private static class DialogPatch
        {
            private static bool Prepare() => Enabled;
            private static bool Prefix(Dialog.Data data, ref IEnumerator __result)
            {
#if DEVELOPMENT_PROBE
                if (ReplayRelentlessChoices.Armed) return true;
#endif
                if (Managers.GetSaveManager().PreviewMode || data.content != "Combat_Relentless_Description".Localize()) return true;
                int count = rounds[currentRoom];
                RelentlessDecision choice = CurrentPlan?.SingleOrDefault(item => item.RoomIndex == currentRoom && item.CompletedRounds == count) ??
                    throw new InvalidOperationException("Unplanned controlled Relentless dialog.");
                RecordDialog(Managers.GetCombatManager().GetTurnCount(), choice);
                if (choice.Continue) data.callback2.Invoke(); else data.callback1.Invoke();
                __result = Empty(); return false;
            }
            private static IEnumerator Empty() { yield break; }
        }
    }
}
