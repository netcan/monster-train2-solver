using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalAttackOverridesScenario
    {
        internal const string Name = "original-attack-overrides";
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
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData body = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(body, "attackDamage", 17); Set(body, "health", 100); Set(body, "size", 1); Set(body, "equipmentLimit", 4);
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
                data.name = "PojuAttackOverrideControl" + suffix; Set(data, "id", "ac110057-0000-4000-8000-" + suffix.ToString("D12"));
                Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default);
                Set(data, "targetless", effects[0].GetTargetMode() != TargetMode.DropTargetCharacter); Set(data, "targetsRoom", true);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "triggers", new List<CardTriggerEffectData>()); Set(data, "effectTriggers", new List<CharacterTriggerData>());
                Set(data, "effects", effects.ToList()); ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data); return data;
            }
            CardData battery = Control(1, Effect("CardEffectGainEnergy", 10, Team.Type.None, TargetMode.Room)),
                enemies = Control(2, Effect("CardEffectBuffMaxHealth", 1000, Team.Type.Heroes, TargetMode.Room),
                    Effect("CardEffectAddStatusEffect", 1, Team.Type.Heroes, TargetMode.Room, "immobile")),
                armor = Control(3, Effect("CardEffectAddStatusEffect", 25, Team.Type.Monsters, TargetMode.DropTargetCharacter, "armor")),
                armorAttack = Control(4, Effect("CardEffectAddStatusEffect", 1, Team.Type.Monsters, TargetMode.BackInRoom, "armorattack")),
                buff = Control(5, Effect("CardEffectBuffDamage", 100, Team.Type.Monsters, TargetMode.DropTargetCharacter)),
                rage = Control(6, Effect("CardEffectAddStatusEffect", 2, Team.Type.Monsters, TargetMode.DropTargetCharacter, "buff")),
                wound = Control(7, Effect("CardEffectDamage", 30, Team.Type.Monsters, TargetMode.DropTargetCharacter)),
                attack = Control(8, Effect("CardEffectAttackWithUnit", 0, Team.Type.Monsters, TargetMode.DropTargetCharacter)),
                stripArmor = Control(9, Effect("CardEffectDamage", 54, Team.Type.Monsters, TargetMode.DropTargetCharacter)),
                removeFixed = Control(10, Effect("CardEffectRemoveStatusEffect", 9999, Team.Type.Monsters, TargetMode.DropTargetCharacter, "fixedattack")),
                removeArmorAttack = Control(11, Effect("CardEffectRemoveStatusEffect", 9999, Team.Type.Monsters, TargetMode.DropTargetCharacter, "armorattack")),
                removeEqualizer = Control(12, Effect("CardEffectRemoveStatusEffect", 9999, Team.Type.Monsters, TargetMode.DropTargetCharacter, "equalizer")),
                health = Control(13, Effect("CardEffectBuffMaxHealth", 20, Team.Type.Monsters, TargetMode.DropTargetCharacter));
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            const string iron = "17fa5800-9c48-41f5-be97-86db8d4c5f9e", equalizer = "d42625f2-9c06-45ff-a6d5-38ddc6ce478a";
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(steward.GetID()), Add(steward.GetID()), Add(battery.GetID()), Add(enemies.GetID()),
                Add(iron), Add(equalizer), Add(armor.GetID()), Add(armorAttack.GetID()), Add(equalizer), Add(buff.GetID()), Add(rage.GetID()), Add(wound.GetID()),
                Add(attack.GetID()), Add(attack.GetID()), Add(attack.GetID()), Add(stripArmor.GetID()), Add(attack.GetID()), Add(iron), Add(attack.GetID()),
                Add(removeFixed.GetID()), Add(attack.GetID()), Add(removeArmorAttack.GetID()), Add(attack.GetID()), Add(removeEqualizer.GetID()), Add(attack.GetID()),
                Add(health.GetID()), Add(attack.GetID()) };
            foreach (CardState card in created.Skip(4)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-ATTACK-OVERRIDES-PREPARED unchanged Irontongue/Equalizer; fixed > armor > maximum HP priority, hidden raw buffs, zero armor and status removal.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 4 && source.Spawn.Turn < 1) return null;
            int host = index is 6 or 14 ? 0 : index is 7 or 13 or 15 or 27 or 28 ? 1 : 2;
            int target = index >= 6 && index != 9 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[host]).Id : 0;
            var action = new PlayCardAction(ids[index], index == 3 ? 1 : 0, playerPosition: index < 4 ? (index == 3 ? 0 : index) : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-ATTACK-OVERRIDES-REJECT index=" + index + " reason=" + result.Reason); }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
