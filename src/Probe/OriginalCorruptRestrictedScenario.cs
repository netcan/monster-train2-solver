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
    internal static class OriginalCorruptRestrictedScenario
    {
        internal const string Name = "original-corrupt-restricted";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<object> Queries = new List<object>();
        internal static bool QueriesUnchanged { get; private set; }
        internal sealed class PrePlayRecord
        {
            public int CardId { get; set; }
            public int RoomIndex { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? After { get; set; }
            public bool Completed { get; set; }
        }
        internal static readonly List<PrePlayRecord> PrePlays = new List<PrePlayRecord>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            CharacterData guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack"); Set(guard, "health", 400);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(SoulSiphonScenario.CardId), Add("096def92-500b-4b1f-89f7-43e96aa1e54a"),
                Add("afbbafc8-fd5a-42af-ac79-a677ab0185d8"), Add(SoulSiphonScenario.CardId), Add("4de04eeb-76d9-4ad0-b5cd-8c2b997395f5") };
            foreach (CardState card in created.Skip(2)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            RoomManager rooms = managers.GetRoomManager()!; RoomState room = rooms.GetRoom(0); RoomUI ui = rooms.GetRoomUI(); int selected = rooms.GetSelectedRoom();
            object stats = AccessTools.Field(typeof(RoomState), "corruptionStats").GetValue(room), preview = AccessTools.Field(typeof(RoomState), "previewCorruptionStats").GetValue(room);
            int current = (int)AccessTools.Field(stats.GetType(), "currentCorruption").GetValue(stats), previewCurrent = (int)AccessTools.Field(preview.GetType(), "currentCorruption").GetValue(preview);
            JToken before = JToken.FromObject(FullBattleTrace.Active.CaptureDecision()); int frame = UnityEngine.Time.frameCount; JObject rng = RngStates();
            try
            {
                Set(ui, "selectedRoom", 0);
                foreach (CardState card in new[] { created[3], created[4], created[6] })
                {
                    CardTraitCorruptRestricted trait = card.GetTraitStates().OfType<CardTraitCorruptRestricted>().Single(); int amount = trait.GetParamInt();
                    foreach (int value in new[] { 0, amount - 1, amount, amount + 1 })
                    {
                        Set(stats, "currentCorruption", value); Set(preview, "currentCorruption", value);
                        BattleTurnState queryBefore = FullBattleTrace.Active.CaptureDecision();
                        bool hand = trait.GetIsPlayableFromHand(cards, rooms, 1, out var handError), play = trait.GetIsPlayableFromPlay(cards, rooms, 1, out var playError);
                        Queries.Add(new { CardId = FullBattleTrace.Active.CardId(card), Before = queryBefore, RoomIndex = 0, SourceRoomIndex = 1,
                            Required = amount, Corruption = value, FromHand = hand, FromPlay = play, HandError = handError.ToString(), PlayError = playError.ToString() });
                    }
                }
            }
            finally { Set(stats, "currentCorruption", current); Set(preview, "currentCorruption", previewCurrent); Set(ui, "selectedRoom", selected); }
            QueriesUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(rng, RngStates()) &&
                JToken.DeepEquals(before, JToken.FromObject(FullBattleTrace.Active.CaptureDecision()));
            if (!QueriesUnchanged) throw new InvalidOperationException("Corrupt Restricted queries failed to restore decision/RNG/frame.");
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-CORRUPT-RESTRICTED-PREPARED original Echo Infusion/Transfer/Proclamation, zero/below/at/above queries, source-floor control, positive pre-effect consumption and refill.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
            if (index == 6 && !source.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0)) return null;
            int target = index is 3 or 4 ? source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0) && play.TargetUnitId == target);
        }
        [HarmonyPatch(typeof(CardTraitCorruptRestricted), nameof(CardTraitCorruptRestricted.OnPreCardPlayed))]
        private static class PrePlayPatch
        {
            private static void Postfix(CardState cardState, ref IEnumerator __result)
            {
                if (Enabled && Prepared && !AllGameManagers.Instance!.GetSaveManager().PreviewMode) __result = Observe(__result, cardState);
            }
        }
        private static IEnumerator Observe(IEnumerator native, CardState card)
        {
            var row = new PrePlayRecord { CardId = FullBattleTrace.Active!.CardId(card), RoomIndex = AllGameManagers.Instance!.GetRoomManager()!.GetSelectedRoom(), Before = FullBattleTrace.Active.CaptureCanonicalTrain() };
            PrePlays.Add(row);
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); row.After = FullBattleTrace.Active!.CaptureCanonicalTrain(); }
        }
        private static JObject RngStates()
        {
            var states = new JObject();
            foreach (RngId id in Enum.GetValues(typeof(RngId))) if (id != RngId.NonDeterministic && id != RngId.Chatter)
                states[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
            return states;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
