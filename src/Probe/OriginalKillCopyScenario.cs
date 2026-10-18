using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalKillCopyScenario
    {
        internal const string Name = "original-kill-copy";
        internal const string OriginalId = "e48d1711-fdff-41d4-b2a0-bc8f8b2bf8dd";
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
            CharacterData guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all)).Single(character => character.name == "ShieldGuardT1_BasicNonAttack");
            Set(guard, "health", 8);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            void Register(CardData data) => ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data);
            CardData Victim(int suffix, int health, string? status = null)
            {
                CardData data = UnityEngine.Object.Instantiate(steward); data.name = "PojuKillCopyVictim" + suffix;
                Set(data, "id", "ac110045-0000-4000-8000-00000000000" + suffix);
                CharacterData body = UnityEngine.Object.Instantiate(anchor); body.name = data.name + "Body";
                Set(body, "attackDamage", 0); Set(body, "health", health);
                if (suffix is 1 or 3) Set(body, "subtypeKeys", new List<string> { "SubtypesData_FunGuy" });
                Set(body, "startingStatusEffects", status == null ? Array.Empty<StatusEffectStackData>() : new[] { new StatusEffectStackData { statusId = status, count = 1 } });
                var spawn = new CardEffectData("CardEffectSpawnMonster", body, Team.Type.Monsters); spawn.Cheat_SetTargetMode(steward.GetEffects()[0].GetTargetMode());
                Set(data, "effects", new List<CardEffectData> { spawn }); Register(data); return data;
            }
            CardData battery = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            battery.name = "PojuKillCopyBattery"; Set(battery, "id", "ac110045-0000-4000-8000-000000000004");
            Set(battery, "cost", 0); Set(battery, "costType", CardData.CostType.Default); Set(battery, "targetless", true); Set(battery, "targetsRoom", true);
            Set(battery, "traits", new List<CardTraitData>()); Set(battery, "startingUpgrades", new List<CardUpgradeData>());
            Set(battery, "triggers", new List<CardTriggerEffectData>()); Set(battery, "effectTriggers", new List<CharacterTriggerData>());
            var energy = new CardEffectData("CardEffectGainEnergy", null!, Team.Type.None); energy.Cheat_SetTargetMode(TargetMode.Room); Set(energy, "paramInt", 20);
            Set(battery, "effects", new List<CardEffectData> { energy }); Register(battery);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = new[] { Add(steward.GetID()), Add(Victim(1, 2).GetID()), Add(Victim(2, 8, "damage shield").GetID()),
                Add(Victim(3, 4, "undying").GetID()), Add(battery.GetID()), Add(OriginalId), Add(OriginalId), Add(OriginalId), Add(OriginalId), Add(OriginalId), Add(OriginalId) };
            foreach (CardState card in created.Skip(1)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            CardTraitData suppression = all.FindCardData(OriginalId)!.GetTraits().Single(trait => trait.GetTraitStateName() == CardKillTriggerModel.SuppressTrait);
            log.LogInfo("ORIGINAL-KILL-COPY-PREPARED unchanged Surgical Strike; suppression team=" + suppression.GetParamTeamType() + "; subtype=" + suppression.GetParamSubtype().Key + "; copy-self/armor/nonlethal/shield/Undying controls.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            var context = source.Spawn.Train.Context!;
            int index = Array.FindIndex(ids, id => context.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            int cardId;
            if (index < 0)
            {
                var copy = context.CardRegistry!.Where(card => card.DataId == OriginalId && !ids.Contains(card.InstanceId)).OrderBy(card => card.InstanceId).FirstOrDefault();
                if (copy == null || context.Statistics!.Value(copy.InstanceId, "TimesPlayed", "ThisBattle") != 0) return null;
                index = ids.Length; cardId = copy.InstanceId;
            }
            else cardId = ids[index];
            if (index >= 4 && source.Spawn.Turn < 1) return null;
            int target = index is 5 or 6 or 7 or 8 ? source.Spawn.Train.Rooms[1].Units.Single(unit => unit.SpawnerCardId == ids[index == 8 ? 2 : index - 4]).Id :
                index is 9 or 10 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack").Id :
                index == ids.Length ? source.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0).OrderBy(unit => unit.Health).First().Id : 0;
            var action = new PlayCardAction(cardId, index is > 0 and < 4 or 5 or 6 or 7 or 8 ? 1 : 0,
                playerPosition: index == 0 ? 0 : index < 4 ? index - 1 : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-KILL-COPY-REJECT index=" + index + " reason=" + result.Reason); }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
