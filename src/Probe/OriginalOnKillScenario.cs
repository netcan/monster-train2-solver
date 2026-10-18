using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalOnKillScenario
    {
        internal const string Name = "original-on-kill";
        internal const string OriginalId = "24a256cd-715e-4423-b2e9-c0925b285fd4";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static bool DeploymentAllowed { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        private static ManualLogSource? logger;
        private static string? lastRejection;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            DeploymentAllowed = !all.GetBalanceData().GetDisallowedDeploymentPhaseCardTriggers().Contains(CardTriggerType.OnKill);
            Set(all.GetBalanceData(), "maxHandSize", 30);
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 60); Set(anchor, "health", 500); Set(anchor, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData Victim(int suffix, int health, string? status = null)
            {
                CardData data = UnityEngine.Object.Instantiate(steward); data.name = "PojuOnKillVictim" + suffix;
                Set(data, "id", "ac110041-0000-4000-8000-00000000000" + suffix);
                CharacterData body = UnityEngine.Object.Instantiate(anchor); body.name = data.name + "Body";
                Set(body, "attackDamage", 0); Set(body, "health", health);
                Set(body, "startingStatusEffects", status == null ? Array.Empty<StatusEffectStackData>() : new[] { new StatusEffectStackData { statusId = status, count = 1 } });
                var spawn = new CardEffectData("CardEffectSpawnMonster", body, Team.Type.Monsters); spawn.Cheat_SetTargetMode(steward.GetEffects()[0].GetTargetMode());
                Set(data, "effects", new List<CardEffectData> { spawn });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data); return data;
            }
            CardData battery = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            battery.name = "PojuOnKillBattery"; Set(battery, "id", "ac110041-0000-4000-8000-000000000005");
            Set(battery, "cost", 0); Set(battery, "costType", CardData.CostType.Default); Set(battery, "targetless", true); Set(battery, "targetsRoom", true);
            Set(battery, "traits", new List<CardTraitData>()); Set(battery, "startingUpgrades", new List<CardUpgradeData>());
            Set(battery, "triggers", new List<CardTriggerEffectData>()); Set(battery, "effectTriggers", new List<CharacterTriggerData>());
            var gain = new CardEffectData("CardEffectGainEnergy", null!, Team.Type.None); gain.Cheat_SetTargetMode(TargetMode.Room); Set(gain, "paramInt", 20);
            Set(battery, "effects", new List<CardEffectData> { gain });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(battery);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            var created = new List<CardState> { Add(steward.GetID()), Add(Victim(1, 10).GetID()),
                Add(Victim(2, 50).GetID()), Add(Victim(3, 10, "undying").GetID()), Add(Victim(4, 10, "damage shield").GetID()),
                Add(OriginalId), Add(battery.GetID()) };
            created.AddRange(Enumerable.Range(0, 6).Select(_ => Add(OriginalId)));
            foreach (CardState card in created.Skip(1)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-ON-KILL-PREPARED unchanged Bloody Moon, overkill/exact kill, nonlethal/shield controls, Undying revival and second death.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 6 && source.Spawn.Turn < 1) return null;
            int victim = index switch { 5 => 1, 7 or 8 => 2, 9 or 12 => 3, 10 or 11 => 4, _ => -1 };
            int target = victim < 0 ? 0 : source.Spawn.Train.Rooms[1].Units.Single(unit => unit.SpawnerCardId == ids[victim]).Id;
            var action = new PlayCardAction(ids[index], index is 0 or 6 ? 0 : 1, playerPosition: index == 0 ? 0 : index < 5 ? index - 1 : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-ON-KILL-REJECT index=" + index + " reason=" + result.Reason); }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
