using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalMineCollapseScenario
    {
        internal const string Name = "original-mine-collapse";
        internal const string CardDataId = "786b57d8-9790-4812-8c2a-d3463fe739d9";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData mineCollapse = all.FindCardData(CardDataId)!;
            CardTriggerEffectData[] originalTriggers = ((List<CardTriggerEffectData>)AccessTools.Field(typeof(CardData), "triggers").GetValue(mineCollapse)).ToArray();
            if (mineCollapse.name != "MineCollapse" || mineCollapse.GetCardType() != CardType.Spell || mineCollapse.GetCost() != 0 ||
                mineCollapse.GetCostType() != CardData.CostType.ConsumeRemainingEnergy || mineCollapse.GetEffects().Count != 1 ||
                mineCollapse.GetEffects()[0].GetEffectStateName() != "CardEffectDamage" ||
                mineCollapse.GetEffects()[0].GetTargetMode().ToString() != "DropTargetCharacter" || mineCollapse.GetTraits().Count != 1 ||
                originalTriggers.Length != 1 || originalTriggers[0].GetTrigger() != CardTriggerType.OnKill ||
                originalTriggers[0].GetCardEffects().Count != 1 ||
                originalTriggers[0].GetCardEffects()[0].GetEffectStateName() != "CardEffectGainEnergy" ||
                originalTriggers[0].GetCardEffects()[0].GetParamInt() != 2)
                throw new InvalidOperationException("Original Mine Collapse definition changed.");

            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            CharacterData anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0);
            Set(anchor, "attackDamage", 9999);
            Set(anchor, "health", 9999);
            Set(anchor, "size", 1);
            Set(anchor, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID()))
                card.Setup(steward, save);

            CardData Victim(int suffix)
            {
                CardData data = UnityEngine.Object.Instantiate(steward);
                data.name = "PojuMineCollapseVictim" + suffix;
                Set(data, "id", "ac110050-0000-4000-8000-00000000000" + suffix);
                CharacterData body = UnityEngine.Object.Instantiate(anchor);
                body.name = data.name + "Body";
                Set(body, "attackDamage", 0);
                Set(body, "health", 9);
                var spawn = new CardEffectData("CardEffectSpawnMonster", body, Team.Type.Monsters);
                spawn.Cheat_SetTargetMode(steward.GetEffects()[0].GetTargetMode());
                Set(data, "effects", new List<CardEffectData> { spawn });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data);
                return data;
            }

            CardData sink = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            sink.name = "PojuMineCollapseEmberSink";
            Set(sink, "id", "ac110050-0000-4000-8000-000000000004");
            Set(sink, "cost", 2);
            Set(sink, "costType", CardData.CostType.Default);
            Set(sink, "targetless", true);
            Set(sink, "targetsRoom", true);
            Set(sink, "traits", new List<CardTraitData>());
            Set(sink, "startingUpgrades", new List<CardUpgradeData>());
            Set(sink, "triggers", new List<CardTriggerEffectData>());
            Set(sink, "effectTriggers", new List<CharacterTriggerData>());
            var noop = new CardEffectData("CardEffectNULL", null!, Team.Type.None);
            noop.Cheat_SetTargetMode(TargetMode.Room);
            Set(noop, "paramBool2", true);
            Set(sink, "effects", new List<CardEffectData> { noop });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(sink);

            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created =
            {
                Add(steward.GetID()), Add(steward.GetID()), Add(steward.GetID()),
                Add(Victim(1).GetID()), Add(Victim(2).GetID()), Add(CardDataId), Add(sink.GetID()), Add(CardDataId)
            };
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-MINE-COLLAPSE-PREPARED unchanged Mine Collapse at X=3/0, friendly lethal OnKill refund and fixed-cost ember sink.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0) return null;
            if (index >= 5 && source.Spawn.Turn < 1) return null;
            if (index < 5)
            {
                int room = index < 3 ? index : 1;
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == room);
            }
            int victimIndex = index == 5 ? 3 : index == 7 ? 4 : -1;
            int target = victimIndex < 0 ? 0 : source.Spawn.Train.Rooms[1].Units.Single(unit => unit.SpawnerCardId == ids[victimIndex]).Id;
            var action = new PlayCardAction(ids[index], 1, playerPosition: -1, targetUnitId: target);
            return BattleActionModel.PlayCard(source, action).Supported ? action : null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
