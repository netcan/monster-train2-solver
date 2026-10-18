using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalHostileStatusesScenario
    {
        internal const string Name = "original-hostile-statuses";
        internal static readonly string[] Originals = { "28875721-10df-4427-b9ef-4d237e89608e", "479ceb1b-fc8c-420f-8fa0-17683cee4511",
            "4f930128-efce-47c7-ad02-67781d528971", "47df0371-7bd9-4bc2-b8e1-de61cc0060c7", "75ce1442-757b-4e9f-826d-8325f3dc7d9e",
            "9e2edcaf-b882-4b8e-ae02-95470ab63047", "b197fdec-a77d-49f5-bc05-6233b7ce01c7", "e5decf78-0be8-4b05-820a-3e7a94ac1600",
            "61fec066-a70a-45b9-8869-263e4a12bf74", "3799918c-a831-4599-a65f-acfc68f582b9", "1cc57bf5-b4c0-4102-8fb3-0c7e68813472" };
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        private static ManualLogSource? logger;
        private static string? lastRejection;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            Set(all.GetBalanceData(), "maxHandSize", 30);
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 60); Set(anchor, "health", 500); Set(anchor, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData Control(int suffix, string effectName, int value, Team.Type team)
            {
                CardData data = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
                data.name = "PojuHostileStatusControl" + suffix; Set(data, "id", "ac110048-0000-4000-8000-00000000000" + suffix);
                Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default); Set(data, "targetless", true); Set(data, "targetsRoom", true);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "triggers", new List<CardTriggerEffectData>()); Set(data, "effectTriggers", new List<CharacterTriggerData>());
                var effect = new CardEffectData(effectName, null!, team); effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramInt", value);
                if (effectName == "CardEffectAdjustCorruption") Set(effect, "paramBool", true);
                if (effectName == "CardEffectAddStatusEffect") Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = "buff", count = 4 } });
                Set(data, "effects", new List<CardEffectData> { effect });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data); return data;
            }
            CardData battery = Control(1, "CardEffectGainEnergy", 20, Team.Type.None), health = Control(2, "CardEffectBuffMaxHealth", 200, Team.Type.Heroes),
                corruption = Control(3, "CardEffectAdjustCorruption", 3, Team.Type.None), rage = Control(4, "CardEffectAddStatusEffect", 0, Team.Type.Monsters);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = new[] { Add(steward.GetID()), Add(steward.GetID()), Add(steward.GetID()), Add(battery.GetID()), Add(health.GetID()),
                Add(corruption.GetID()), Add(rage.GetID()) }.Concat(Originals.Select(Add)).ToArray();
            foreach (CardState card in created.Skip(3)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-HOSTILE-STATUSES-PREPARED eleven unchanged originals; selected/room/tower statuses, positive multiplication, chance-100 RNG and three-Corruption spending.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
            int target = index is 7 or 9 or 12 or 13 or 14 or 17 ? source.Spawn.Train.Rooms[0].Units.First(unit => unit.Team == CombatTeam.Enemy).Id : 0;
            var action = new PlayCardAction(ids[index], index == 2 ? 1 : 0, playerPosition: index < 2 ? index : index == 2 ? 0 : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-HOSTILE-STATUSES-REJECT index=" + index + " reason=" + result.Reason); }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
