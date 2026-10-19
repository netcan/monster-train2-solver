using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalXStatusCardsScenario
    {
        internal const string Name = "original-x-status-cards";
        internal const string DataId = "dc7b0d83-2a0c-460b-ae92-42ee144005a5";
        internal const string PrismalDustId = "9f4dd6b6-4a03-4e2d-9a52-8a17b1dd2c93";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData prismalDust = all.FindCardData(PrismalDustId)!;
            if (prismalDust.name != "PrismalDust" || prismalDust.GetCardType() != CardType.Spell || prismalDust.GetCost() != 0 ||
                prismalDust.GetCostType() != CardData.CostType.ConsumeRemainingEnergy || prismalDust.GetEffects().Count != 1 ||
                prismalDust.GetEffects()[0].GetEffectStateName() != "CardEffectAddStatusEffect" ||
                prismalDust.GetEffects()[0].GetTargetMode() != TargetMode.DropTargetCharacter || prismalDust.GetTraits().Count != 2 ||
                prismalDust.GetTraits()[0].GetTraitStateName() != "CardTraitExhaustState" ||
                prismalDust.GetTraits()[1].GetTraitStateName() != "CardTraitScalingAddStatusEffect")
                throw new InvalidOperationException("Original Prismal Dust definition changed.");
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 200); Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", new[] { new StatusEffectStackData { statusId = "buff", count = 3 },
                new StatusEffectStackData { statusId = "regen", count = 2 }, new StatusEffectStackData { statusId = "armor", count = 4 } });
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData refill = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            refill.name = "PojuXStatusEnergyRefill"; Set(refill, "id", "ac110049-0000-4000-8000-000000000009");
            Set(refill, "cost", 0); Set(refill, "costType", CardData.CostType.Default); Set(refill, "traits", new List<CardTraitData>());
            Set(refill, "startingUpgrades", new List<CardUpgradeData>()); Set(refill, "triggers", new List<CardTriggerEffectData>());
            Set(refill, "effectTriggers", new List<CharacterTriggerData>()); Set(refill, "targetless", true); Set(refill, "targetsRoom", true);
            var gain = new CardEffectData("CardEffectGainEnergy", null!, Team.Type.None); gain.Cheat_SetTargetMode(TargetMode.Room);
            Set(gain, "paramInt", 4); Set(refill, "effects", new List<CardEffectData> { gain });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(refill);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardIds = new[] { Add(steward.GetID()), Add(steward.GetID()), Add(steward.GetID()), Add(DataId), Add(DataId),
                Add(PrismalDustId), Add(refill.GetID()), Add(PrismalDustId) }
                .Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-X-STATUS-CARDS-PREPARED unchanged Spike of the Legion and Prismal Dust at positive/zero X; Consume routing, scaling traits and target/floor/armor controls.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            for (int index = 0; index < ids.Length; index++)
                if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                {
                    int target = index >= 3 && index != 6 ? source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
                    return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] &&
                        play.RoomIndex == (index == 2 ? 1 : 0) && (target == 0 || play.TargetUnitId == target));
                }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
