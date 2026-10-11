using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalEndlessReturnScenario
    {
        internal const string Name = "original-endless-return";
        private const string RelicId = "374995fd-7619-4244-9d1f-269429258584";
        private const string UpgradeId = "c62d9641-8948-4c00-b67d-60f0e728c1c1";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }
        internal static int TargetCardId { get; private set; }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            AllGameData data = save.GetAllGameData();
            RelicManager relicManager = managers.GetRelicManager();
            CollectableRelicData relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicId);
            RelicEffectData effect = relic.GetEffects().Single();
            CardUpgradeData upgrade = effect.GetParamCardUpgradeData();
            if (relic.name != "MoreRares_Relic" || effect.GetEffectClassName() != "RelicEffectAddTempUpgrade" ||
                effect.GetEffectConditions().Count != 0 || effect.GetParamSourceTeam() != Team.Type.Monsters ||
                upgrade == null || upgrade.GetID() != UpgradeId || upgrade.name != "AddEndlessToNonMorsels" || upgrade.IsUnique() ||
                upgrade.GetStatusEffectUpgrades().Count != 1 || upgrade.GetStatusEffectUpgrades()[0].statusId != "endless" ||
                upgrade.GetStatusEffectUpgrades()[0].count != 1 || upgrade.GetFilters().Count != 2)
                throw new InvalidOperationException("Original Endless relic identity or payload changed.");

            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardState carrier = owned.FirstOrDefault(card =>
            {
                CardData? cardData = data.FindCardData(card.GetCardDataID());
                return cardData != null && cardData.GetSpawnCharacterData() != null && cardData.GetCost() <= 3 &&
                    upgrade.GetFilters().All(filter => filter.FilterCard(card, relicManager));
            }) ?? throw new InvalidOperationException("Original Endless relic has no eligible owned unit card.");
            CardData victimData = data.FindCardData(carrier.GetCardDataID()) ??
                throw new InvalidOperationException("Original Endless carrier definition disappeared.");
            CharacterData victimUnit = victimData.GetSpawnCharacterData()!;
            Set(victimUnit, "health", 1);
            Set(victimUnit, "attackDamage", 0);
            Set(victimUnit, "size", 1);

            save.AddRelic(relic);
            CaptureReady = true;
            CardState victim = cards.AddNewCard(victimData, CardPile.HandPile, false, false, animate: false);
            TargetCardId = (FullBattleTrace.Active ?? throw new InvalidOperationException("Endless capture requires an active battle trace.")).CardId(victim);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-ENDLESS-RETURN-PREPARED unchanged MoreRares_Relic/AddEndlessToNonMorsels; authored one 1-HP non-Morsel carrier for death routing.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            var plays = BattleActionModel.EnumerateSupportedPlays(source);
            if (source.Spawn.Train.Context!.FindCard(TargetCardId)?.PlayCount == 0)
            {
                PlayCardAction? victim = plays.FirstOrDefault(action => action.CardInstanceId == TargetCardId && action.RoomIndex == 0);
                if (victim != null) return victim;
            }
            PlayCardAction? fallback = BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            return fallback?.CardInstanceId == TargetCardId ? plays.FirstOrDefault(action => action.CardInstanceId != TargetCardId) : fallback;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
