using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalKillResourcesScenario
    {
        internal const string Name = "original-kill-resources";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static string Resource { get; private set; } = "";
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        private static ManualLogSource? logger;
        private static string? lastRejection;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log; Resource = Environment.GetEnvironmentVariable("MT2_PROBE_KILL_RESOURCE") ?? "";
            string original = Resource == "Greed" ? "27e9ee82-fc42-4aa5-b845-90d7e31b61d9" :
                Resource == "GoldenTouch" ? "32449823-6e6a-4ae4-98a2-2db41f38f159" :
                Resource == "Bonk" ? "99492750-a7b9-447c-a248-097a07cd346a" : throw new InvalidOperationException("Unknown kill resource.");
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            if (Resource == "GoldenTouch") { save.SetDragonsHoardCap(3); save.SetDragonsHoardAmount(0); }
            if (Resource == "Bonk") save.SetForgePoints(2);
            Set(all.GetBalanceData(), "maxHandSize", 30);
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 60); Set(anchor, "health", 500); Set(anchor, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            int damage = Resource == "Greed" ? 20 : Resource == "GoldenTouch" ? 5 : 9;
            CardData Victim(int suffix, int health, string? status = null)
            {
                CardData data = UnityEngine.Object.Instantiate(steward); data.name = "PojuKillResourceVictim" + suffix;
                Set(data, "id", "ac110043-0000-4000-8000-00000000000" + suffix);
                CharacterData body = UnityEngine.Object.Instantiate(anchor); body.name = data.name + "Body";
                Set(body, "attackDamage", 0); Set(body, "health", health);
                Set(body, "startingStatusEffects", status == null ? Array.Empty<StatusEffectStackData>() : new[] { new StatusEffectStackData { statusId = status, count = 1 } });
                var spawn = new CardEffectData("CardEffectSpawnMonster", body, Team.Type.Monsters); spawn.Cheat_SetTargetMode(steward.GetEffects()[0].GetTargetMode());
                Set(data, "effects", new List<CardEffectData> { spawn });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data); return data;
            }
            CardData battery = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            battery.name = "PojuKillResourceBattery"; Set(battery, "id", "ac110043-0000-4000-8000-000000000005");
            Set(battery, "cost", 0); Set(battery, "costType", CardData.CostType.Default); Set(battery, "targetless", true); Set(battery, "targetsRoom", true);
            Set(battery, "traits", new List<CardTraitData>()); Set(battery, "startingUpgrades", new List<CardUpgradeData>());
            Set(battery, "triggers", new List<CardTriggerEffectData>()); Set(battery, "effectTriggers", new List<CharacterTriggerData>());
            var gain = new CardEffectData("CardEffectGainEnergy", null!, Team.Type.None); gain.Cheat_SetTargetMode(TargetMode.Room); Set(gain, "paramInt", 20);
            Set(battery, "effects", new List<CardEffectData> { gain });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(battery);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            var created = new List<CardState> { Add(steward.GetID()), Add(Victim(1, damage - 1).GetID()),
                Add(Victim(2, damage * 2).GetID()), Add(Victim(3, damage - 1, "undying").GetID()), Add(Victim(4, damage - 1, "damage shield").GetID()) };
            if (Resource == "Greed") { created.Add(Add(battery.GetID())); created.AddRange(Enumerable.Range(0, 2).Select(_ => Add(original))); }
            else { created.Add(Add(original)); created.Add(Add(battery.GetID())); created.AddRange(Enumerable.Range(0, 6).Select(_ => Add(original))); }
            foreach (CardState card in created.Skip(1)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-KILL-RESOURCES-PREPARED unchanged " + Resource + ", lethal/nonlethal/shield/Undying and resource controls.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids, string resource)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || (resource == "Greed" ? index >= 1 : index >= 6) && source.Spawn.Turn < 1) return null;
            int victim = resource == "Greed" ? -1 : index switch { 5 => 1, 7 or 8 => 2, 9 or 12 => 3, 10 or 11 => 4, _ => -1 };
            int target = victim < 0 ? 0 : source.Spawn.Train.Rooms[1].Units.Single(unit => unit.SpawnerCardId == ids[victim]).Id;
            bool battery = index == (resource == "Greed" ? 5 : 6);
            var action = new PlayCardAction(ids[index], index == 0 || battery ? 0 : 1,
                playerPosition: index == 0 ? 0 : index < 5 ? index - 1 : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-KILL-RESOURCES-REJECT index=" + index + " reason=" + result.Reason); }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
