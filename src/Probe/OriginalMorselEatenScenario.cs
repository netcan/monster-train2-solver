using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalMorselEatenScenario
    {
        internal const string Name = "original-morsel-eaten";
        internal static readonly string[] OriginalIds = { "0347419a-2e3a-492d-a054-f6e340be3db2", "e16040b1-eac0-4769-bc89-90bc2eb31944",
            "9c8d3e46-1758-4ade-a4b4-cd2bf649a4d6", "b24a8e4f-294f-4d24-9bc2-9c14474d83d1", "536e6de0-7ccf-412a-9a4e-ec55ab3988e5" };
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static string VoidIgnoredUpgradeId { get; private set; } = "";
        internal static bool VoidIgnoredCopy { get; private set; }
        internal static bool VoidIgnoredClassFilter { get; private set; }
        private static ManualLogSource? logger;
        private static string? lastRejection;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 60); Set(anchor, "health", 500); Set(anchor, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            var created = new List<CardState>();
            for (int i = 0; i < 3; i++) created.Add(cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false));
            foreach (string id in OriginalIds)
            {
                CardState card = cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
                var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); created.Add(card);
            }
            CardEffectData effect = all.FindCardData(OriginalIds[3])!.GetSpawnCharacterData().GetTriggers().SelectMany(trigger => trigger.GetEffects())
                .Single(item => item.GetEffectStateName() == "CardEffectAddStatusEffect");
            VoidIgnoredUpgradeId = effect.GetParamCardUpgradeData()?.GetID() ?? "";
            VoidIgnoredCopy = effect.GetCopyModifiersFromSource(); VoidIgnoredClassFilter = effect.GetFilterBasedOnMainSubClass();
            if (VoidIgnoredUpgradeId.Length == 0 && !VoidIgnoredCopy && !VoidIgnoredClassFilter) throw new InvalidOperationException("Missing original unused status-effect fields.");
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-MORSEL-EATEN-PREPARED five unchanged originals, ordinary summons, original upgrades and Void lifesteal with unused native fields retained.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
            var action = new PlayCardAction(ids[index], index < 3 ? index : (index - 3) / 2,
                playerPosition: index < 3 ? 0 : (index - 3) % 2 + 1);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-MORSEL-EATEN-REJECT index=" + index + " reason=" + result.Reason); }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
