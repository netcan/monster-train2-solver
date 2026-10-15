using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalUpgradeCountRelicsScenario
    {
        internal const string Name = "original-upgrade-count-relics";
        private static readonly string[] RelicIds = { "4200307a-616d-4cbb-a54e-527c5dc0f492", "fc1f6104-b3c1-4714-ac0f-9c76a6fb1a77" };
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }
        private static readonly List<int> playIds = new List<int>();
        private static readonly HashSet<int> preludePlayed = new HashSet<int>();
        private static CardUpgradeData[] upgrades = null!;

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Upgrade-count relics must be acquired before battle.");
            foreach (string id in RelicIds)
            {
                CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == id);
                CheckOriginal(relic);
                if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == id))
                    throw new InvalidOperationException("Upgrade-count relic already acquired.");
                save.AddRelic(relic);
            }
            AllGameData data = save.GetAllGameData();
            var icon = data.GetAllCardUpgradeData().First(item => item.GetUpgradeIcon() != null).GetUpgradeIcon();
            upgrades = Enumerable.Range(0, 5).Select(index =>
            {
                var upgrade = UnityEngine.ScriptableObject.CreateInstance<CardUpgradeData>();
                upgrade.name = "PojuUpgradeCount" + index;
                Set(upgrade, "id", "fc119000-0000-4000-8000-0000000003" + index.ToString("00"));
                Set(upgrade, "upgradeIcon", index == 3 ? null : icon);
                Set(upgrade, "hideUpgradeIconOnCard", index == 2);
                Set(upgrade, "isRegionRunUpgrade", index == 1);
                Set(upgrade, "upgradeTitleKey", ""); Set(upgrade, "upgradeDescriptionKey", "");
                Set(upgrade, "statusEffectUpgrades", new List<StatusEffectStackData>());
                Set(upgrade, "traitDataUpgrades", new List<CardTraitData>());
                Set(upgrade, "removeTraitUpgrades", new List<string>());
                ((List<CardUpgradeData>)AccessTools.Field(typeof(AllGameData), "cardUpgradeDatas").GetValue(data)).Add(upgrade);
                return upgrade;
            }).ToArray();
            Acquired = true;
            log.LogInfo("ORIGINAL-UPGRADE-COUNT-RELICS-ACQUIRED unchanged GiveSpellStatsForUpgrades_DH and GiveUnitStatsForUpgrades_DH.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Upgrade-count relics missed deployment.");
            SaveManager save = managers.GetSaveManager();
            AllGameData data = save.GetAllGameData();
            foreach (string id in RelicIds)
            {
                CheckOriginal(data.GetAllCollectableRelicData().Single(item => item.GetID() == id));
                if (save.GetCollectedRelics().Count(item => item.GetRelicDataID() == id) != 1)
                    throw new InvalidOperationException("Upgrade-count relic missing or duplicated.");
            }
            CardData[] deck = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID())!).ToArray();
            CardData monster = deck.First(card => card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true);
            CardData spell = deck.First(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDamage"));
            CardData control = data.FindCardData("21038774-c0a4-413c-ab7e-27e458228e5f")!;
            CaptureReady = true;
            CardData excluded = data.FindCardData("10f64bfb-81cd-4b7c-bf46-6fc1fa832008")!;
            if (excluded == null || excluded.GetCardType() != CardType.Spell)
                throw new InvalidOperationException("Original spell exclusion witness changed.");
            foreach (CardData definition in new[] { monster, spell, control, excluded })
            foreach (int count in new[] { 0, 1, 2 })
            {
                var card = new CardState(definition, save, setupStartingUpgrades: false);
                Install(card, count);
                managers.GetRelicManager().ApplyCardStateModifiers(card, resetTempCardModifiers: false);
                managers.GetRelicManager().ApplyCardStateModifiers(card);
                managers.GetRelicManager().ApplyCardStateModifiers(card);
            }
            CardManager cards = managers.GetCardManager()!;
            foreach (CardData definition in new[] { monster, spell })
            {
                if (!cards.GetBelowHandSize()) throw new InvalidOperationException("No hand space for upgraded original card.");
                CardState card = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
                Install(card, 2);
                managers.GetRelicManager().ApplyCardStateModifiers(card);
                playIds.Add(FullBattleTrace.Active!.CardId(card));
            }
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-UPGRADE-COUNT-RELICS-PREPARED 36 detached refreshes; counts=0,1,2; visible region upgrade included; hidden/no-icon/temporary excluded; excluded spell=" + excluded.name + "; played witnesses=" + string.Join(",", playIds));
        }

        private static void Install(CardState card, int count)
        {
            foreach (int index in Enumerable.Range(0, count).Concat(new[] { 2, 3, 4 }))
            {
                var upgrade = new CardUpgradeState(); upgrade.Setup(upgrades[index]);
                (index == 4 ? card.GetTemporaryCardStateModifiers() : card.GetCardStateModifiers()).AddUpgrade(upgrade);
            }
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            foreach (int id in playIds)
                if (!preludePlayed.Contains(id) && state.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == id))
                {
                    PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(state).FirstOrDefault(item => item.CardInstanceId == id);
                    if (action != null) return action;
                }
            return null;
        }
        internal static bool IsScenarioPrelude(PlayCardAction action) =>
            playIds.Contains(action.CardInstanceId) && preludePlayed.Add(action.CardInstanceId);

        private static void CheckOriginal(CollectableRelicData relic)
        {
            bool spell = relic.GetID() == RelicIds[0];
            RelicEffectData[] effects = relic.GetEffects().ToArray();
            if (relic.name != (spell ? "GiveSpellStatsForUpgrades_DH" : "GiveUnitStatsForUpgrades_DH") ||
                relic.DisallowInDeploymentPhase || effects.Length != (spell ? 1 : 2))
                throw new InvalidOperationException("Original upgrade-count relic identity changed.");
            for (int index = 0; index < effects.Length; index++)
            {
                RelicEffectData effect = effects[index];
                if (effect.GetEffectClassName() != (!spell && index == 0 ? "RelicEffectModifyCardHealth" : "RelicEffectModifyCardAttack") ||
                    effect.GetParamInt() != 3 || effect.GetParamInt2() != 0 || effect.GetParamFloat() != 0 ||
                    !effect.GetParamBool() || effect.GetParamBool2() || effect.GetParamBool3() || effect.GetParamBool4() ||
                    effect.GetParamString().Length != 0 || effect.GetParamCharacterSubtype()?.IsNone != true ||
                    effect.GetParamCardType() != (spell ? CardType.Spell : CardType.Monster) ||
                    (spell ? effect.GetParamCardFilter()?.name != relic.name + "_CardFilter" : effect.GetParamCardFilter() != null) ||
                    effect.GetEffectConditions().Count != 0)
                    throw new InvalidOperationException("Original upgrade-count payload changed: " + relic.name + "/" + index +
                        " type=" + effect.GetEffectClassName() + " ints=" + effect.GetParamInt() + "," + effect.GetParamInt2() +
                        " float=" + effect.GetParamFloat() + " bools=" + effect.GetParamBool() + "," + effect.GetParamBool2() + "," +
                        effect.GetParamBool3() + "," + effect.GetParamBool4() + " name=" + effect.GetParamString() +
                        " subtype=" + effect.GetParamCharacterSubtype()?.Key + " card=" + effect.GetParamCardType() +
                        " filter=" + effect.GetParamCardFilter()?.name + " conditions=" + effect.GetEffectConditions().Count);
                if (spell)
                {
                    var expected = new CardUpgradeMaskRule("GiveSpellStatsForUpgrades_DH_CardFilter",
                        rarities: new UpgradeMaskContent<string>(requiredOperator: 1),
                        disallowedPools: new[] { new[] { "10f64bfb-81cd-4b7c-bf46-6fc1fa832008" } });
                    if (!Newtonsoft.Json.Linq.JToken.DeepEquals(Newtonsoft.Json.Linq.JToken.FromObject(expected),
                        Newtonsoft.Json.Linq.JToken.FromObject(CardUpgradeMaskProbe.Rule(effect.GetParamCardFilter()))))
                        throw new InvalidOperationException("Original upgrade-count spell filter changed.");
                }
            }
        }
        private static void Set(object target, string field, object? value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
