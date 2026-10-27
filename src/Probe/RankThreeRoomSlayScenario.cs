using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    // An explicitly controlled combat witness, never a natural campaign run.
    // Keep the saved champion and its upgrades; only author its supporting tank.
    internal static class RankThreeRoomSlayScenario
    {
        internal const string Name = "rank-three-room-slay-execution";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardState champion = cards.GetHand().Single(card =>
                all.FindCardData(card.GetCardDataID())?.GetSpawnCharacterData()?.name == "MonsterChampionUnderlegion1");
            foreach (CardState card in cards.GetHand().ToArray())
                if (card != champion) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();

            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            CharacterData tank = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0);
            Set(tank, "attackDamage", 60);
            Set(tank, "health", 500);
            Set(tank, "size", 1);
            Set(tank, "startingStatusEffects", new[] { new StatusEffectStackData { statusId = "regen", count = 10 } });
            CardState anchor = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardIds = new[] { FullBattleTrace.Active!.CardId(anchor), FullBattleTrace.Active.CardId(champion) };
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("RANK-THREE-ROOM-SLAY-PREPARED saved champion upgrades unchanged; controlled60ATK/500HP/regen10 tank, two-card combat witness.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int index = Array.FindIndex(CardIds, id =>
                source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0) return null;
            var action = new PlayCardAction(CardIds[index], 0, playerPosition: index);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (!result.Supported) throw new InvalidOperationException("Controlled room slay deployment failed: " + result.Reason);
            return action;
        }

        private static void Set(object target, string field, object value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
