using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalFuelScenario
    {
        internal const string Name = "original-fuel";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        private static ManualLogSource? logger;
        private static string? lastRejection;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            Set(all.GetBalanceData(), "maxHandSize", 40);
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 0); Set(anchor, "health", 500); Set(anchor, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardEffectData Effect(string type, int value, Team.Type team, TargetMode mode, string status = "")
            {
                var effect = new CardEffectData(type, null!, team); effect.Cheat_SetTargetMode(mode); Set(effect, "paramInt", type == "CardEffectAddStatusEffect" ? 0 : value);
                if (status.Length > 0) Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = status, count = value } });
                return effect;
            }
            CardData Control(int suffix, params CardEffectData[] effects)
            {
                CardData data = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
                data.name = "PojuFuelControl" + suffix; Set(data, "id", "ac110059-0000-4000-8000-" + suffix.ToString("D12"));
                Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default); Set(data, "targetless", effects[0].GetTargetMode() != TargetMode.DropTargetCharacter); Set(data, "targetsRoom", true);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "triggers", new List<CardTriggerEffectData>()); Set(data, "effectTriggers", new List<CharacterTriggerData>());
                Set(data, "effects", effects.ToList()); ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data); return data;
            }
            CardData energy = Control(1, Effect("CardEffectGainEnergy", 10, Team.Type.None, TargetMode.Room)),
                capacity = Control(2, Effect("CardEffectAdjustRoomCapacity", 30, Team.Type.Monsters, TargetMode.Room)),
                enemies = Control(3, Effect("CardEffectBuffMaxHealth", 1000, Team.Type.Heroes, TargetMode.Room), Effect("CardEffectAddStatusEffect", 1, Team.Type.Heroes, TargetMode.Room, "immobile")),
                attack = Control(4, Effect("CardEffectAttackWithUnit", 0, Team.Type.Monsters, TargetMode.DropTargetCharacter)),
                heal = Control(5, Effect("CardEffectHeal", 100, Team.Type.Monsters, TargetMode.DropTargetCharacter)),
                refuel = Control(6, Effect("CardEffectAddStatusEffect", 1, Team.Type.Monsters, TargetMode.DropTargetCharacter, "spark"));
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(steward.GetID()), Add(energy.GetID()), Add(capacity.GetID()), Add(enemies.GetID()),
                Add("3db9cb2c-3495-47a7-983e-d717a206460c"), Add(attack.GetID()), Add(attack.GetID()), Add(attack.GetID()),
                Add("0347419a-2e3a-492d-a054-f6e340be3db2"), Add(heal.GetID()), Add(refuel.GetID()) };
            ICoreGameManagers coreManagers = managers.GetCoreManagers() ?? throw new InvalidOperationException("Original fuel scenario requires core game managers.");
            for (int index = 3; index < created.Length; index++)
            {
                CardState card = created[index] ?? throw new InvalidOperationException("Original fuel scenario created a null card at index " + index + ".");
                var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze");
                card.AddTemporaryTrait(freeze, coreManagers);
            }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-FUEL-PREPARED unchanged Alloyed Construct, forced 2/1/0 fuel attacks, empty-fuel combat, original Morsel refueling and selected refill.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 3 && source.Spawn.Turn < 1 || index == 10 && source.Spawn.Turn < 2 || index >= 11 && source.Spawn.Turn < 3) return null;
            int target = index is 7 or 8 or 9 or 11 or 12 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[6]).Id : 0;
            var action = new PlayCardAction(ids[index], index < 3 ? index : 0, playerPosition: index < 3 || index == 6 ? 0 : index == 10 ? 2 : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-FUEL-REJECT index=" + index + " reason=" + result.Reason); }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
