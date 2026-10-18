using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalOnKillGenerationScenario
    {
        internal const string Name = "original-on-kill-generation";
        internal const string OriginalId = "fe73da55-8250-4f96-b3e0-91c19b3e60de";
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
            void Register(CardData data) => ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data);
            CardData Victim(int suffix, int health, string? status = null)
            {
                CardData data = UnityEngine.Object.Instantiate(steward); data.name = "PojuKillGenerationVictim" + suffix;
                Set(data, "id", "ac110042-0000-4000-8000-00000000000" + suffix);
                CharacterData body = UnityEngine.Object.Instantiate(anchor); body.name = data.name + "Body";
                Set(body, "attackDamage", 0); Set(body, "health", health);
                Set(body, "startingStatusEffects", status == null ? Array.Empty<StatusEffectStackData>() : new[] { new StatusEffectStackData { statusId = status, count = 1 } });
                var spawn = new CardEffectData("CardEffectSpawnMonster", body, Team.Type.Monsters); spawn.Cheat_SetTargetMode(steward.GetEffects()[0].GetTargetMode());
                Set(data, "effects", new List<CardEffectData> { spawn }); Register(data); return data;
            }
            CardData Control(int suffix, string type, int value)
            {
                CardData data = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
                data.name = "PojuKillGenerationControl" + suffix; Set(data, "id", "ac110042-0000-4000-8000-00000000000" + suffix);
                Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default); Set(data, "targetless", true); Set(data, "targetsRoom", true);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "triggers", new List<CardTriggerEffectData>()); Set(data, "effectTriggers", new List<CharacterTriggerData>());
                var effect = new CardEffectData(type, null!, Team.Type.None); effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramInt", value);
                if (type == "CardEffectNULL") Set(effect, "paramBool2", true);
                Set(data, "effects", new List<CardEffectData> { effect }); Register(data); return data;
            }
            CardData battery = Control(5, "CardEffectGainEnergy", 20), filler = Control(6, "CardEffectNULL", 0), fill = Control(7, "CardEffectAddBattleCard", (int)CardPile.HandPile);
            var pool = UnityEngine.ScriptableObject.CreateInstance<CardPool>(); pool.name = "PojuKillGenerationFillerPool";
            object entries = AccessTools.Field(typeof(CardPool), "cardDataList").GetValue(pool);
            AccessTools.Method(entries.GetType(), "Add", new[] { typeof(CardData) }).Invoke(entries, new object[] { filler });
            Set(fill.GetEffects()[0], "paramCardPool", pool); Set(fill.GetEffects()[0], "additionalParamInt", 30);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            var created = new List<CardState> { Add(steward.GetID()), Add(Victim(1, 2).GetID()), Add(Victim(2, 10).GetID()),
                Add(Victim(3, 2, "undying").GetID()), Add(Victim(4, 2, "damage shield").GetID()), Add(OriginalId), Add(battery.GetID()),
                Add(OriginalId), Add(OriginalId), Add(OriginalId), Add(fill.GetID()), Add(OriginalId), Add(OriginalId), Add(OriginalId) };
            foreach (CardState card in created.Skip(1)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-ON-KILL-GENERATION-PREPARED unchanged Antumbra Assault, weighted two-Morsel rolls, deployment/nonlethal/shield/Undying controls and one-slot hand overflow preserving both RNG draws.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 6 && source.Spawn.Turn < 1) return null;
            int victim = index switch { 5 => 1, 7 or 8 => 2, 9 or 13 => 3, 11 or 12 => 4, _ => -1 };
            int target = victim < 0 ? 0 : source.Spawn.Train.Rooms[1].Units.Single(unit => unit.SpawnerCardId == ids[victim]).Id;
            var action = new PlayCardAction(ids[index], index is 0 or 6 or 10 ? 0 : 1, playerPosition: index == 0 ? 0 : index < 5 ? index - 1 : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-ON-KILL-GENERATION-REJECT index=" + index + " reason=" + result.Reason); }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
