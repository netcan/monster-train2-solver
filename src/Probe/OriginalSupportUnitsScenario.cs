using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSupportUnitsScenario
    {
        internal const string Name = "original-support-units";
        internal const string DivineDancerName = "original-divine-dancer";
        internal const string IsolatedUnitBatchName = "original-isolated-unit-batch";
        private static readonly string[] IsolatedUnitBatchIds = { "276fdd51-4bb8-4810-9bbf-cb663522b70f" };
        internal static readonly string[] Originals = { "4aa37169-c5cf-4a5b-b927-dc31b05f6e24", "82e21764-56cb-4e41-882f-ab3dd0c1dcbc",
            "c8b428ac-a1e9-4e52-96e0-5eed522195a2", "0c68b12a-0779-4c3c-bf64-6bc5bcf83e9f", "8c404be7-7179-461c-8e7d-b5fdb296174c",
            "d76be2d0-f851-4d43-bdc0-7c7a7779724f", "600ef4bf-59ec-45b6-a3e7-273cbb43c7bb", "85bddcfb-8e7a-4797-a209-c7d62f40ecc7",
            "61d9f4f1-80f5-416b-bf22-1836925c8ccd", "5a8399dd-a958-4317-b814-de0e2972ca3d", "a736e605-a4f4-4cc1-8eba-63cb0cf3515e",
            "64a3fd58-b7b3-466a-be8d-78bb2b9bdea4", "d653e83e-3f70-419d-b3f9-4b920d5c9e56", "dcfdf8cc-0b0b-41e2-97a2-383fe303e617",
            "69fcbe85-a5b7-4a83-bf6d-21888604d99e" };
        private const string BlightedTotemId = "21ad4f07-1568-4461-8be4-6fb2670bd90d";
        private const string ProsperityTotemId = "21567cc4-f684-48db-b377-b8289729dedc";
        private const string BattleDancerId = "a0b051b2-7898-454a-8bd8-300b72b99c93";
        private const string HeroInTrainingId = "57ffb1c3-0728-4254-ab35-7e34adab459f";
        private const string DivineDancerId = "a1e9a37b-504a-4a04-99e2-6d933727b427";
        private static bool DivineDancerOnly => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == DivineDancerName;
        private static bool IsolatedUnitBatchOnly => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == IsolatedUnitBatchName;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name || DivineDancerOnly || IsolatedUnitBatchOnly;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        private static ManualLogSource? logger;
        private static string? lastRejection;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            if (DivineDancerOnly) { PrepareDivineDancer(managers, log); return; }
            if (IsolatedUnitBatchOnly) { PrepareIsolatedUnitBatch(managers, log); return; }
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            Set(all.GetBalanceData(), "maxHandSize", 40);
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 0); Set(anchor, "health", 500); Set(anchor, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData Control(int suffix, string effectName, int value, Team.Type team, TargetMode mode)
            {
                CardData data = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
                data.name = "PojuSupportUnitControl" + suffix; Set(data, "id", "ac110058-0000-4000-8000-" + suffix.ToString("D12"));
                Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default); Set(data, "targetless", mode != TargetMode.DropTargetCharacter); Set(data, "targetsRoom", true);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "triggers", new List<CardTriggerEffectData>()); Set(data, "effectTriggers", new List<CharacterTriggerData>());
                var effect = new CardEffectData(effectName, null!, team); effect.Cheat_SetTargetMode(mode); Set(effect, "paramInt", value);
                Set(data, "effects", new List<CardEffectData> { effect });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data); return data;
            }
            CardData energy = Control(1, "CardEffectGainEnergy", 30, Team.Type.None, TargetMode.Room),
                capacity = Control(2, "CardEffectAdjustRoomCapacity", 30, Team.Type.Monsters, TargetMode.Room),
                enemyHealth = Control(3, "CardEffectBuffMaxHealth", 200, Team.Type.Heroes, TargetMode.Room),
                wound = Control(4, "CardEffectDamage", 10, Team.Type.Monsters, TargetMode.DropTargetCharacter),
                heal = Control(5, "CardEffectHeal", 10, Team.Type.Monsters, TargetMode.DropTargetCharacter),
                hit = Control(6, "CardEffectDamage", 1, Team.Type.Monsters, TargetMode.DropTargetCharacter),
                attack = Control(7, "CardEffectAttackWithUnit", 0, Team.Type.Monsters, TargetMode.DropTargetCharacter),
                killAlchemical = Control(8, "CardEffectDamage", 30, Team.Type.Monsters, TargetMode.DropTargetCharacter);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState AddToDiscard(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.DiscardPile, false, false, animate: false);
            CardState[] created = new[] { Add(steward.GetID()), Add(steward.GetID()), Add(steward.GetID()), Add(energy.GetID()), Add(capacity.GetID()), Add(capacity.GetID()), Add(capacity.GetID()), Add(enemyHealth.GetID()) }
                .Concat(Originals.Select(Add)).Concat(new[] { Add(wound.GetID()), Add(heal.GetID()), Add(wound.GetID()), Add(heal.GetID()), Add(hit.GetID()), Add(attack.GetID()), Add(attack.GetID()),
                    AddToDiscard("f1c2a098-e20d-4587-b8de-5bef315de42d"), Add(killAlchemical.GetID()) })
                .Append(Add(BlightedTotemId)).Append(Add(ProsperityTotemId)).Append(Add(BattleDancerId))
                .Append(Add(HeroInTrainingId)).Append(Add(DivineDancerId)).ToArray();
            foreach (CardState card in created.Skip(3)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            save.SetDragonsHoardCap(4); save.SetDragonsHoardAmount(2);
            log.LogInfo("ORIGINAL-SUPPORT-UNITS-PREPARED twenty unchanged originals; positive spawn, rally, heal, hit, attack, equipment growth, unmatched typed-draw control, Alchemical death Hoard callback, Avenger OnHit, two CardSpellPlayed, Battle Dancer/Hero in Training OnShift and Divine Dancer full-room boundary.");
        }
        private static void PrepareDivineDancer(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            Set(all.GetBalanceData(), "maxHandSize", 40);
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 1000); Set(anchor, "health", 10000); Set(anchor, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardState[] created = new[] { cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false),
                cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false),
                cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false),
                cards.AddNewCard(all.FindCardData(DivineDancerId)!, CardPile.HandPile, false, false, animate: false) };
            var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); created[3].AddTemporaryTrait(freeze, managers.GetCoreManagers());
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-DIVINE-DANCER-PREPARED three safe native anchor controls and one original 7/14 Divine Dancer in an empty room.");
        }
        private static void PrepareIsolatedUnitBatch(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            Set(all.GetBalanceData(), "maxHandSize", 40);
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 1000); Set(anchor, "health", 10000); Set(anchor, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardState[] anchors = Enumerable.Range(0, 3).Select(_ => cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false)).ToArray();
            CardState[] units = IsolatedUnitBatchIds.Select(id => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false)).ToArray();
            foreach (CardState card in units) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = anchors.Concat(units).Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-ISOLATED-UNIT-BATCH-PREPARED three safe native anchors plus one original Branded Warrior.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
            if (DivineDancerOnly || IsolatedUnitBatchOnly)
            {
                int isolatedRoom = index < 3 ? index : 0;
                int isolatedPosition = index < 3 ? 0 : source.Spawn.Train.Rooms.Single(currentRoom => currentRoom.RoomIndex == 0).Units.Count(unit => unit.Team == CombatTeam.Player);
                var isolatedAction = new PlayCardAction(ids[index], isolatedRoom, playerPosition: isolatedPosition);
                BattleActionResult isolatedResult = BattleActionModel.PlayCard(source, isolatedAction);
                if (isolatedResult.Supported) return isolatedAction;
                if (lastRejection != isolatedResult.Reason) { lastRejection = isolatedResult.Reason; logger?.LogInfo("ORIGINAL-ISOLATED-UNIT-BATCH-REJECT index=" + index + " reason=" + isolatedResult.Reason); }
                return null;
            }
            int room = index < 3 ? index : index == 5 || index is >= 14 and <= 18 || index is 35 or 36 ? 1 :
                index is 6 or 19 or 20 or 21 or 22 or 30 or 31 or 32 or 33 or 34 ? 2 : 0;
            int position = index < 3 ? 0 : index is >= 8 and <= 13 ? index - 7 : index is >= 14 and <= 18 ? index - 13 : index is >= 19 and <= 22 ? index - 18 :
                index is 32 or 33 or 34 or 35 or 36 ? source.Spawn.Train.Rooms.Single(currentRoom => currentRoom.RoomIndex == room).Units.Count(unit => unit.Team == CombatTeam.Player) : -1;
            int host = index is 23 or 24 ? 11 : index is 25 or 26 ? 12 : index == 27 ? 9 : index == 28 ? 8 : index == 29 ? 10 : index == 31 ? 21 : 19;
            int target = index >= 23 && index != 32 && index != 33 && index != 34 && index != 35 && index != 36
                ? source.Spawn.Train.Rooms.SelectMany(floor => floor.Units).Single(unit => unit.SpawnerCardId == ids[host]).Id : 0;
            var action = new PlayCardAction(ids[index], room, playerPosition: position, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-SUPPORT-UNITS-REJECT index=" + index + " reason=" + result.Reason); }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
