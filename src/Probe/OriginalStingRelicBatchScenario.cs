using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalStingRelicBatchScenario
    {
        internal const string Name = "original-sting-relics";
        private const string StingCardId = "356a00a4-010b-462b-b683-2ef296d076a8";
        private static bool prepared;
        private static bool captureReady;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared => prepared;
        internal static bool CaptureReady => captureReady;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (!Enabled || Prepared) return;
            SaveManager save = managers.GetSaveManager();
            AllGameData data = save.GetAllGameData();
            Acquire("0f6d3edc-d212-4e3a-877f-e9eb2ac78339", "StingBuffPower", "9037e99a-d4cb-43cf-a3dd-e8bf88349be1", 20, false);
            Acquire("2d367bde-7616-4804-a6c9-910060ae5fd7", "StingBuffPiercing", "04128a02-8289-422c-aad8-7a6a80a632ab", 10, true);

            CardData sting = data.FindCardData(StingCardId)!;
            if (sting == null || sting.GetCardType() != CardType.Spell || sting.GetCostType() != CardData.CostType.Default ||
                sting.GetCost() != 0 || !sting.GetEffects().Select(effect => effect.GetEffectStateName())
                    .SequenceEqual(new[] { "CardEffectDamage", "CardEffectDrawAdditionalNextTurn" }))
                throw new InvalidOperationException("Original Sting definition changed.");

            CardManager cards = managers.GetCardManager()!;
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            captureReady = true;
            cards.AddNewCard(sting, CardPile.HandPile, false, false, animate: false);
            RelicManager relicManager = managers.GetRelicManager();
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).ToArray())
            {
                relicManager.ApplyCardStateModifiers(card, resetTempCardModifiers: false);
                relicManager.ApplyCardStateModifiers(card, resetTempCardModifiers: false);
                relicManager.ApplyCardStateModifiers(card);
            }

            prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-STING-RELICS-PREPARED unchanged StingBuffPower and StingBuffPiercing; one native Sting was added to hand for an actual upgraded play.");

            void Acquire(string relicId, string relicKey, string upgradeId, int damage, bool piercing)
            {
                CollectableRelicData relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == relicId);
                RelicEffectData[] relicEffects = relic.GetEffects().ToArray();
                RelicEffectData effect = relicEffects.Single(item => item.GetEffectClassName() == "RelicEffectAddTempUpgrade");
                CardUpgradeData upgrade = effect.GetParamCardUpgradeData();
                string[] traits = upgrade.GetTraitDataUpgrades().Select(trait => trait.GetTraitStateName()).ToArray();
                if (relic.name != relicKey || !relicEffects.Select(item => item.GetEffectClassName())
                        .SequenceEqual(new[] { "RelicEffectAddTempUpgrade", "RelicEffectAddCardsNull" }) ||
                    effect.GetEffectConditions().Count != 0 || effect.GetParamSourceTeam() != Team.Type.Monsters ||
                    upgrade.GetID() != upgradeId || upgrade.GetAssetKey() != relicKey || !upgrade.IsUnique() ||
                    upgrade.GetBonusDamage() != damage || upgrade.GetFilters().Count != 1 ||
                    upgrade.GetFilters()[0].name != "OnlyStings" ||
                    (piercing ? !traits.SequenceEqual(new[] { "CardTraitIgnoreArmor" }) : traits.Length != 0))
                    throw new InvalidOperationException("Original Sting relic identity, filter or upgrade payload changed: " + relicKey);
                save.AddRelic(relic);
            }
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            PlayCardAction? ordinary = BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            if (ordinary != null)
            {
                CardInstanceState? selected = source.Spawn.Train.Context!.FindCard(ordinary.CardInstanceId);
                CardPlayRule? selectedRule = selected == null ? null : source.PlayRules!.Cards.FirstOrDefault(rule => rule.DataId == selected.DataId);
                if (selectedRule?.Effect is "SpawnMonster" or "Equipment") return ordinary;
            }
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play =>
                source.Spawn.Train.Context!.FindCard(play.CardInstanceId)?.DataId == StingCardId) ?? ordinary;
        }
    }
}
