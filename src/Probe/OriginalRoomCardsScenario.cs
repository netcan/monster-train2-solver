using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalRoomCardsScenario
    {
        internal const string Name = "original-room-cards";
        internal const string FightClubId = "6bc6afc8-a113-4f03-b090-d4436f41d822";
        internal const string SanctumId = "5af36c0f-ab98-4a49-903a-b08d93705af1";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            Set(steward, "cost", 0); Set(steward.GetSpawnCharacterData(), "attackDamage", 60);
            Set(steward.GetSpawnCharacterData(), "health", 200); Set(steward.GetSpawnCharacterData(), "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardData spell = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            spell.name = "PojuRoomCostGold"; Set(spell, "id", "ac110010-0000-4000-8000-000000000001"); Set(spell, "cost", 2);
            Set(spell, "costType", CardData.CostType.Default); Set(spell, "traits", new List<CardTraitData>());
            Set(spell, "startingUpgrades", new List<CardUpgradeData>()); Set(spell, "triggers", new List<CardTriggerEffectData>());
            Set(spell, "effectTriggers", new List<CharacterTriggerData>()); Set(spell, "targetless", false); Set(spell, "targetsRoom", true);
            var reward = new CardEffectData("CardEffectRewardGold", null!, Team.Type.Monsters); reward.Cheat_SetTargetMode(TargetMode.Room); Set(reward, "paramInt", 1);
            Set(spell, "effects", new List<CardEffectData> { reward });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(spell);
            CardState first = Add(steward.GetID()), outside = Add(steward.GetID()), fight = Add(FightClubId), later = Add(steward.GetID());
            CardState sanctum = Add(SanctumId), paidUnit = Add(steward.GetID()); paidUnit.GetCardStateModifiers().IncrementAdditionalCost(2);
            CardState insideSpell = Add(spell.GetID()), outsideSpell = Add(spell.GetID()), restored = Add(FightClubId), restoredSpell = Add(spell.GetID());
            CardIds = new[] { first, outside, fight, later, sanctum, paidUnit, insideSpell, outsideSpell, restored, restoredSpell }.Select(FullBattleTrace.Active!.CardId).ToArray();
            foreach (string id in new[] { FightClubId, SanctumId })
            {
                CardPlayRule rule = BattleActionProbe.Definition(all.FindCardData(id)!);
                if (rule.ExternalInteractions.Count > 0 || rule.Effects.Single().RoomAttachment == null)
                    throw new InvalidOperationException("Original room card projection incomplete: " + rule.AssetKey + ": " + string.Join(";", rule.ExternalInteractions));
            }
            managers.GetPlayerManager().AddEnergy(15); Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-ROOM-CARDS-PREPARED two original Fight Clubs and Spell Sanctum; existing/later births, replacement and paid spell/unit/floor controls.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            for (int index = 0; index < ids.Length; index++)
                if (context.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                    return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index is 1 or 7 ? 1 : 0));
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
