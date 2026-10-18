using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalLimitedRangeScenario
    {
        internal const string Name = "original-limited-range";
        internal const string IceId = "27f9d3dc-b5aa-4a26-abae-aa197bedb6f2";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<object> Queries = new List<object>();
        internal static bool QueriesUnchanged { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 200); Set(unit, "size", 1);
            CharacterData guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack");
            Set(guard, "health", 400);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(IceId), Add(IceId) };
            foreach (CardState card in created.Skip(1)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            RoomManager rooms = managers.GetRoomManager()!; RoomUI ui = rooms.GetRoomUI(); int selected = rooms.GetSelectedRoom();
            JToken before = JToken.FromObject(FullBattleTrace.Active!.CaptureDecision()); int frame = UnityEngine.Time.frameCount;
            JObject rng = RngStates();
            try
            {
                foreach (string id in new[] { IceId, "8ccceee7-0eb9-4aa0-9268-ff83b6f6c54e", "8d65e688-d93d-4bfa-975d-8c5f046fe2db" })
                {
                    CardData data = all.FindCardData(id)!; CardTraitData definition = data.GetTraits().Single(trait => trait.GetTraitStateName() == CardPlayRangeModel.Trait);
                    var trait = new CardTraitLimitedRange(); trait.Setup(definition, created[1]);
                    for (int floor = 0; floor < rooms.GetNumRooms(); floor++)
                    {
                        Set(ui, "selectedRoom", floor);
                        bool fromHand = trait.GetIsPlayableFromHand(cards, rooms, 0, out var handError);
                        bool fromPlay = trait.GetIsPlayableFromPlay(cards, rooms, 0, out var playError);
                        Queries.Add(new { DataId = id, SourceTrait = CardSourceTraitProbe.Capture(definition), RoomIndex = floor,
                            PyreRoomIndex = rooms.GetPyreRoom().GetRoomIndex(), FromHand = fromHand, FromPlay = fromPlay,
                            HandError = handError.ToString(), PlayError = playError.ToString() });
                    }
                }
            }
            finally { Set(ui, "selectedRoom", selected); }
            QueriesUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(rng, RngStates()) &&
                JToken.DeepEquals(before, JToken.FromObject(FullBattleTrace.Active!.CaptureDecision()));
            if (!QueriesUnchanged) throw new InvalidOperationException("Native Limited Range queries changed state/RNG/frame.");
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-LIMITED-RANGE-PREPARED original Ice and Pyre at top/Pyre floors, lower-floor rejection, three original source trait queries from hand/play with restoration.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            for (int index = 0; index < ids.Length; index++)
                if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                {
                    int room = index == 2 ? 3 : 2;
                    if (index == 1 && !source.Spawn.Train.Rooms.Single(value => value.RoomIndex == room).Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0)) return null;
                    return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == room);
                }
            return null;
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
