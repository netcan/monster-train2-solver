using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalHandCostsScenario
    {
        internal const string Name = "original-hand-costs";
        internal const string BaneId = "13981930-b732-4c42-9bd3-1950d7a553b5";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static int[] QueryIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<object> Samples = new List<object>();
        private static CardState[] queryCards = Array.Empty<CardState>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            Set(save.GetBalanceData(), "maxHandSize", 20);
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 200); Set(unit, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData Author(int index, CardType type, int cost)
            {
                CardData data = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
                data.name = "PojuHandCost" + index; Set(data, "id", "ac110016-0000-4000-8000-" + index.ToString("D12"));
                Set(data, "cost", cost); Set(data, "cardType", type); Set(data, "costType", CardData.CostType.Default);
                Set(data, "targetless", type != CardType.Equipment); Set(data, "targetsRoom", true);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "triggers", new List<CardTriggerEffectData>()); Set(data, "effectTriggers", new List<CharacterTriggerData>());
                var effect = new CardEffectData(type == CardType.Equipment ? "CardEffectAttachEquipment" : "CardEffectNULL", null!,
                    type == CardType.Equipment ? Team.Type.Monsters : Team.Type.None);
                effect.Cheat_SetTargetMode(type == CardType.Equipment ? TargetMode.DropTargetCharacter : TargetMode.Room);
                if (type != CardType.Equipment) Set(effect, "paramBool2", true);
                if (type == CardType.Equipment)
                {
                    CardUpgradeData payload = DynamicUpgradeScenario.Upgrade("PojuHandCostGear", "ac110016-0000-4000-8000-100000000001", 1, 1, 0, 0, "armor", 0);
                    payload.GetStatusEffectUpgrades().Clear(); Set(effect, "paramCardUpgradeData", payload);
                }
                Set(data, "effects", new List<CardEffectData> { effect });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data); return data;
            }
            CardData spell = Author(1, CardType.Spell, 0), gear = Author(2, CardType.Equipment, 0), junk = Author(3, CardType.Junk, 0), high = Author(4, CardType.Spell, 98);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(spell.GetID()), Add(gear.GetID()), Add(EngineUpgradeScenario.EngineId),
                Add(OriginalUnpurgableCardsScenario.BlightId), Add(junk.GetID()), Add(BaneId), Add(steward.GetID()), Add(spell.GetID()),
                Add(gear.GetID()), Add(BaneId), Add(spell.GetID()), Add(steward.GetID()), Add(EngineUpgradeScenario.EngineId) };
            void Reduce(CardState card, int amount) { var upgrade = new CardUpgradeState(); upgrade.Setup(); upgrade.SetCostReduction(amount); card.ApplyPermanentUpgrade(upgrade, save); }
            Reduce(created[2], 1); Reduce(created[9], 1);
            CardState highHand = Add(high.GetID()), highOutside = cards.AddNewCard(high, CardPile.DeckPile, false, false, animate: false);
            Reduce(highHand, 98); Reduce(highOutside, 98);
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray(); queryCards = created.Concat(new[] { highHand, highOutside }).ToArray();
            QueryIds = queryCards.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            managers.GetPlayerManager().AddEnergy(30); Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-HAND-COSTS-PREPARED two unchanged Titan Banes; four type surcharges, Blight/Junk controls, ordered cost reduction, upper clamp, outside-hand control and source removal.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            int index = Array.FindIndex(ids, id => context.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (Samples.Count == (index < 0 ? ids.Length : index))
            {
                AllGameManagers managers = AllGameManagers.Instance!;
                Samples.Add(new { Context = context, Costs = queryCards.Select(card => new { CardId = FullBattleTrace.Active!.CardId(card),
                    Cost = card.GetCost(managers.GetCardStatistics(), managers.GetMonsterManager(), managers.GetRelicManager()) }).ToArray() });
            }
            if (index < 0) return null;
            int room = index is 1 or 13 or 14 ? 1 : 0;
            int targetCard = index == 3 ? ids[0] : index == 10 ? ids[8] : 0;
            int target = targetCard == 0 ? 0 : source.Spawn.Train.Rooms.SelectMany(value => value.Units).Single(value => value.SpawnerCardId == targetCard).Id;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == room && (target == 0 || play.TargetUnitId == target));
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
