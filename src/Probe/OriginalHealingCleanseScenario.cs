using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalHealingCleanseScenario
    {
        internal const string Name = "original-healing-cleanse";
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
            Set(all.GetAllCharacterData().Single(character => character.name == "ShieldGuardT1_BasicNonAttack"), "health", 400);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardEffectData Effect(string type, Team.Type team, int value)
            {
                var effect = new CardEffectData(type, null!, team); effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramInt", value); return effect;
            }
            CardEffectData Status(Team.Type team, string status, int count)
            {
                CardEffectData effect = Effect("CardEffectAddStatusEffect", team, 0);
                Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = status, count = count } }); return effect;
            }
            CardData Control(int suffix, params CardEffectData[] effects)
            {
                CardData data = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
                data.name = "PojuHealingCleanseControl" + suffix; Set(data, "id", "ac110040-0000-4000-8000-00000000000" + suffix);
                Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default); Set(data, "targetless", true); Set(data, "targetsRoom", true);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "triggers", new List<CardTriggerEffectData>()); Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "effects", effects.ToList());
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data); return data;
            }
            CardData battery = Control(1, Effect("CardEffectGainEnergy", Team.Type.None, 20)),
                wound = Control(2, Effect("CardEffectDamage", Team.Type.Monsters, 50)),
                statuses = Control(3, Status(Team.Type.Monsters, "poison", 4), Status(Team.Type.Monsters, "dazed", 1), Status(Team.Type.Heroes, "poison", 3));
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            var created = new List<CardState> { Add(steward.GetID()), Add(steward.GetID()), Add(steward.GetID()), Add(battery.GetID()),
                Add("42d238d8-7180-48b0-b0e8-51f2bbb7ffb4"), Add(wound.GetID()), Add(wound.GetID()),
                Add("d81f1f43-f20e-4459-9fc4-19e5836a876f"), Add("d4366b7c-588c-49aa-b2c6-2aeb30b0e01b"),
                Add("8de281af-ab9a-4d3d-ac5f-89cd43ff6c84"), Add(statuses.GetID()), Add("a82b7b1b-31f1-4634-9cf3-197d17d61ba3"),
                Add("2d5f5f3d-fefb-448c-a819-4c82b0c6b17b"), Add("974f9021-d1e3-4b8b-b348-d737a1868ed1"),
                Add("8de281af-ab9a-4d3d-ac5f-89cd43ff6c84"), Add("4c0f6e0e-ac05-4e0c-a478-590ac06d646f"),
                Add("d861b6a6-d8ef-4d58-8ebd-9f0abc276456"), Add("b1c7b06f-a7fa-491a-804d-9290b87494da"), Add("e2a9868d-1f0e-47b1-abe4-490ee940270b") };
            foreach (CardState card in created.Skip(3)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-HEALING-CLEANSE-PREPARED ten unchanged original spells, both Witchweave teams, positive heal/regen/burnout/cleanse, poison concentration/doubling, risky set-health, full room heal, graft removal/attack upgrade and healed descend.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
            int room = index is 2 or 6 or 18 ? 1 : 0;
            int target = index is 4 or 7 or 8 or 9 or 12 or 15 or 17 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]).Id :
                index is 11 or 14 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack").Id :
                index == 18 ? source.Spawn.Train.Rooms[1].Units.Single(unit => unit.SpawnerCardId == ids[2]).Id : 0;
            var action = new PlayCardAction(ids[index], room, playerPosition: index < 3 ? (index == 1 ? 1 : 0) : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-HEALING-CLEANSE-REJECT index=" + index + " reason=" + result.Reason); }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
