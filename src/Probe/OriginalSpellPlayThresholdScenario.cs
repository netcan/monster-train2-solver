using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSpellPlayThresholdScenario
    {
        internal const string Name = "original-spell-play-threshold-relics";
        internal static readonly string[] Ids = { "76166da7-d9aa-4c68-9a78-64914ec49f81", "4fbb0cca-26cd-4510-af02-66694c7edfd3" };
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Spell-play threshold owners must precede battle.");
            foreach (string id in Ids)
            {
                var relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == id);
                var effect = relic.GetEffects().Single();
                var condition = effect.GetEffectConditions().Single();
                bool magic = id == Ids[0];
                object Field(string name) => AccessTools.Field(typeof(RelicEffectCondition), name).GetValue(condition);
                if (relic.name != (magic ? "IncreaseMagicPowerOnSpellsPlayed" : "DrawCardWhenConsumePlayed") ||
                    relic.DisallowInDeploymentPhase || effect.GetParamInt() != 1 || effect.GetParamCardType() != CardType.Spell ||
                    effect.GetEffectClassName() != (magic ? "RelicEffectModifyCardMagicPowerOnCardPlayed" : "RelicEffectCardDrawOnCardPlay") ||
                    (magic ? effect.GetTraits().Count != 0 : !effect.GetTraits().Select(trait => trait.GetTraitStateName()).SequenceEqual(new[] { "CardTraitExhaustState" })) || Field("paramTrackedValue").ToString() != (magic ? "AnyCardPlayed" : "SubtypeInDeck") ||
                    Field("paramCardType").ToString() != (magic ? "Spell" : "Any") ||
                    (bool)Field("paramTrackTriggerCount") != !magic || Field("paramEntryDuration").ToString() != "ThisTurn" ||
                    Convert.ToInt32(Field("paramComparator")) != 6 || (int)Field("paramInt") != (magic ? 3 : 1) ||
                    (bool)Field("allowMultipleTriggersPerDuration"))
                    throw new InvalidOperationException("Original spell-play threshold payload changed: " + relic.name + " traits=" + string.Join(",", effect.GetTraits().Select(trait => trait.GetTraitStateName())));
                if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == id))
                    throw new InvalidOperationException("Spell-play threshold owner already acquired.");
                save.AddRelic(relic);
            }
            Acquired = true;
            log.LogInfo("ORIGINAL-SPELL-PLAY-THRESHOLDS-ACQUIRED two unchanged original owners; draw requires Exhaust, magic requires no trait.");
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Spell threshold carriers require deployment.");
            var save = managers.GetSaveManager(); var data = save.GetAllGameData();
            CardData template = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID())!)
                .First(card => card.GetCardType() == CardType.Spell);
            CardData carrier = UnityEngine.Object.Instantiate(template);
            carrier.name = "PojuSpellThresholdCarrier";
            Set(carrier, "id", "fc119000-0000-4000-8000-000000000361");
            Set(carrier, "cost", 0); Set(carrier, "costType", CardData.CostType.Default);
            Set(carrier, "targetless", false); Set(carrier, "targetsRoom", true);
            Set(carrier, "isUnitAbility", false); Set(carrier, "isRoomAbility", false);
            Set(carrier, "traits", new List<CardTraitData>());
            Set(carrier, "effects", new List<CardEffectData>());
            Set(carrier, "effectTriggers", new List<CharacterTriggerData>());
            Set(carrier, "triggers", new List<CardTriggerEffectData>());
            Set(carrier, "startingUpgrades", new List<CardUpgradeData>());
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(carrier);
            for (int index = 0; index < 3; index++)
            {
                if (index == 2)
                {
                    carrier = UnityEngine.Object.Instantiate(carrier);
                    carrier.name = "PojuSpellThresholdConsumeCarrier";
                    Set(carrier, "id", "fc119000-0000-4000-8000-000000000362");
                    Set(carrier, "traits", new List<CardTraitData> { data.GetAllCardData().SelectMany(card => card.GetTraits())
                        .First(trait => trait.GetTraitStateName() == "CardTraitExhaustState") });
                    ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(carrier);
                }
                var card = managers.GetCardManager()!.AddNewCard(carrier, CardPile.HandPile, false, false, animate: false);
                if (card == null || !managers.GetCardManager()!.GetHand().Contains(card))
                    throw new InvalidOperationException("Threshold carrier did not enter hand.");
            }
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-SPELL-PLAY-THRESHOLDS-PREPARED three zero-cost spells: two non-consuming controls and one consuming carrier; original owners unchanged.");
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
