using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class EquipmentScenario
    {
        internal static bool Started { get; private set; }
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }
        internal static void Start(AllGameManagers managers, ManualLogSource log)
        {
            Started = true;
            managers.GetSaveManager().StartCoroutine(Protect(Run(managers, log), log));
        }
        private static IEnumerator Protect(IEnumerator native, ManualLogSource log)
        {
            while (true)
            {
                bool next;
                try { next = native.MoveNext(); }
                catch (Exception error) { Error = error.ToString(); log.LogError(Error); break; }
                if (!next) break;
                yield return native.Current;
            }
            (native as IDisposable)?.Dispose();
            if (Error == null) Completed = true;
        }
        private static IEnumerator Run(AllGameManagers managers, ManualLogSource log)
        {
            CardManager cards = managers.GetCardManager()!; SaveManager save = managers.GetSaveManager();
            bool exhausted = Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "equipment-exhausted";
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardState[] gear = owned.Where(card => card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDamage" &&
                effect.GetTargetMode() == TargetMode.DropTargetCharacter)).ToArray();
            if (gear.Length < 3) { Error = "Equipment setup needs three owned targeted damage cards."; yield break; }
            CardData data = save.GetAllGameData().FindCardData(gear[0].GetCardDataID())!;
            CardUpgradeData upgrade = DynamicUpgradeScenario.Upgrade("PojuEquipment", "383fbf10-6400-4000-8000-000000000001", 2, 4, 0, 2, "armor", 2);
            var attach = new CardEffectData("CardEffectAttachEquipment", null!, Team.Type.Monsters);
            attach.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(attach, "paramCardUpgradeData", upgrade);
            data.GetEffects().Clear(); data.GetEffects().Add(attach); data.GetTraits().Clear();
            if (!exhausted)
            { var returnTrait = new CardTraitData(); returnTrait.Setup("CardTraitReturnToHandEquipment"); data.GetTraits().Add(returnTrait); }
            Set(data, "cardType", CardType.Equipment); Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default);
            Set(data, "targetless", false); Set(data, "targetsRoom", true);
            foreach (CardState card in gear) card.Setup(data, save);
            CardData rally = save.GetAllGameData().FindCardData(owned.First(card => card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).GetCardDataID())!;
            rally.GetEffects().Clear(); var remove = new CardEffectData("CardEffectRemoveEquipment", null!, Team.Type.Monsters);
            remove.Cheat_SetTargetMode(TargetMode.Room); rally.GetEffects().Add(remove); Set(rally, "targetless", false); Set(rally, "targetsRoom", true);
            foreach (CardState card in owned.Where(card => card.GetCardDataID() == rally.GetID())) card.Setup(rally, save);
            CardData steward = save.GetAllGameData().FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!;
            CharacterData unitData = steward.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            Set(unitData, "equipmentLimit", 2); Set(unitData, "health", 35);
            var triggers = unitData.GetTriggers().ToList();
            foreach (var kind in new[] { CharacterTriggerData.Trigger.OnEquipmentAddedToAny, CharacterTriggerData.Trigger.OnEquipmentAdded, CharacterTriggerData.Trigger.OnEquipmentRemoved })
            { var trigger = HealingScenario.HealGold(1, false, true); Set(trigger, "trigger", kind); triggers.Add(trigger); }
            Set(unitData, "triggers", triggers);
            foreach (CardState card in owned.Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            gear[0].GetTemporaryCardStateModifiers().IncrementAdditionalDamage(1);
            gear[0].GetTemporaryCardStateModifiers().IncrementAdditionalHP(3);
            var bonus = new CardUpgradeState(); bonus.Setup(); bonus.SetAttackDamage(1); bonus.AddStatusEffectUpgradeStacks("spikes", 1);
            gear[1].ApplyPermanentUpgrade(bonus, save, ignoreUpgradeAnimation: true);
            RoomManager rooms = managers.GetRoomManager()!; yield return rooms.GetRoomUI().SetSelectedRoom(0);
            int index = cards.GetHand().FindIndex(card => card.GetCardDataID() == steward.GetID());
            if (index < 0) { Error = "Equipment setup needs an initial Steward."; yield break; }
            CardState source = cards.GetHand()[index]; var drop = rooms.GetRoom(0).GetMonsterPoint(0);
            if (!cards.CanPlayHandCard(source, 0, drop, null, null, out var error) || !cards.PlayCard(index, drop, ref error))
            { Error = "Equipment setup native summon failed: " + error; yield break; }
            while (managers.GetReplayManager().IsCardPlaying() || managers.GetCombatManager()!.IsRunningTriggerQueue ||
                (managers.GetHandUI()?.AreAnyCardsAnimating() ?? false)) yield return null;
            var monsters = new List<CharacterState>(); rooms.GetRoom(0).AddCharactersToList(monsters, Team.Type.Monsters);
            CharacterState host = monsters.Single(unit => unit.GetSpawnerCard() == source);
            foreach (CardState card in gear.Take(3))
            {
                yield return host.AddEquipment(card, managers.GetCoreManagers(), grafted: false);
                CardState equipment = card;
                cards.MoveToStandByPile(card, wasPlayed: true, wasExhausted: false,
                    new RemoveFromStandByCondition(() => cards.CheckEquipmentRemoveFromStandByCondition(host, equipment)));
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            Completed = true;
            log.LogInfo("EQUIPMENT-PREPARED native three-card attachment/replacement, temporary/permanent modifiers, equipment callbacks, return=" +
                (exhausted ? "Exhausted" : "Hand") + " and original Boss/waves.");
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
