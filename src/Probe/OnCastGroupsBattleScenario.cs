using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OnCastGroupsBattleScenario
    {
        internal const string Name = "oncast-groups-battle";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly int[] Modes = { 23, 24, 25, 26, 28, 29, 30, 31 };
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            CharacterData guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack");
            Set(guard, "health", 400);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardState[] troops = Enumerable.Range(0, 2).Select(_ => cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false)).ToArray();
            var controls = new List<CardState>();
            for (int index = 0; index < Modes.Length; index++)
            {
                CardData data = OnCastGroupControls.Create(all, Modes[index], 12, index % 2, Team.Type.Monsters, cost: 0);
                definitions.Add(data);
                var card = cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false);
                var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers());
                controls.Add(card);
            }
            var blank = UnityEngine.Object.Instantiate(all.FindCardData(DevotionSpawnBattleScenario.VenerateId));
            blank.name = "PojuOnCastGroupsFuture"; Set(blank, "id", "fc231b71-0000-4000-8000-000000000001");
            Set(blank, "traits", new List<CardTraitData>()); Set(blank, "triggers", new List<CardTriggerEffectData>());
            var noop = new CardEffectData("CardEffectNULL", null!, Team.Type.None); noop.Cheat_SetTargetMode(TargetMode.Room);
            Set(blank, "effects", new List<CardEffectData> { noop }); definitions.Add(blank);
            for (int index = 0; index < 12; index++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = troops.Concat(controls).Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ONCAST-GROUPS-PREPARED eight controls, cancelled group continuation, room candidates and persistent target history.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int index = Array.FindIndex(CardIds, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
            int target = index >= 2 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == CardIds[0]).Id : 0;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == CardIds[index] &&
                play.RoomIndex == (index == 1 ? 1 : 0) && play.TargetUnitId == target);
        }
    }
}
