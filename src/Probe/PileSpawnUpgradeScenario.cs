#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class PileSpawnUpgradeScenario
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "pile-spawn-upgrade";
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static object? Definition { get; private set; }
        private static MutatorData? original;
        internal static object? CurrentDefinition => original == null ? null : Describe(original);
        private static object Describe(MutatorData data) => new { Id = data.GetID(), Key = data.name,
            Effects = data.GetEffects().Select(effect => new { Type = effect.GetEffectClassName(), Pile = effect.GetParamMinInt(),
                Threshold = effect.GetParamMaxInt(), Team = effect.GetParamSourceTeam().ToString(),
                Upgrade = effect.GetParamCardUpgradeData()?.GetID(), Conditions = effect.GetEffectConditions().Count }).ToArray() };
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var cards = managers.GetCardManager(); var all = save.GetAllGameData();
            if (save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0 || save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Pile spawn upgrades require isolated deployment.");
            original = all.FindMutatorData("2467694d-249b-456d-950c-b02f01a73f1b");
            if (original == null || original.name != "Echoes_GainStatsPerDeckSize" || original.GetEffects().Count != 1 ||
                original.GetEffects()[0].GetEffectClassName() != RelicPileSpawnUpgradeModel.Effect || original.GetEffects()[0].GetParamMaxInt() != 10)
                throw new InvalidOperationException("Original deck-size birth upgrade changed.");
            Definition = Describe(original); save.AddMutator(original);
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            var steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            AccessTools.Field(typeof(CardData), "cost").SetValue(steward, 0);
            AccessTools.Field(typeof(CharacterData), "attackDamage").SetValue(steward.GetSpawnCharacterData(), 80);
            AccessTools.Field(typeof(CharacterData), "health").SetValue(steward.GetSpawnCharacterData(), 5000);
            foreach (var card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            var ids = new List<int>();
            for (int i = 0; i < 3; i++)
            {
                var card = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
                var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers());
                ids.Add(FullBattleTrace.Active!.CardId(card));
            }
            CardIds = ids.ToArray(); Prepared = true;
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("PILE-SPAWN-UPGRADE-PREPARED unchanged original deck-size mutator; three authored durable unit controls.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int index = Array.FindIndex(CardIds, card => source.Spawn.Train.Context!.Statistics!.Value(card, "TimesPlayed", "ThisBattle") == 0);
            return index < 0 ? null : BattleActionModel.EnumerateSupportedPlays(source, CardIds[index]).FirstOrDefault(play => play.RoomIndex == index);
        }
    }
}
#endif
