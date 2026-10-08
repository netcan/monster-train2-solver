using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class TriggeredSummonEquipmentScenario
    {
        internal static bool Enabled => (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? "").Contains("equipment");
        internal static bool Owned => (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? "").Contains("equipment-owned");
        internal static bool Started { get; private set; }
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }
        private static CardState[] gear = Array.Empty<CardState>();

        internal static void Prepare(AllGameManagers managers, CardEffectData liveSummon, CardEffectData deathSummon)
        {
            SaveManager save = managers.GetSaveManager();
            var owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            var normal = owned.Where(card => card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDamage" &&
                effect.GetTargetMode() == TargetMode.DropTargetCharacter)).ToArray();
            var returning = owned.Where(card => card.GetCardDataID() == "19705414-816d-4549-bcf5-866f338a551f").ToArray();
            if (normal.Length < 2 || returning.Length == 0)
                throw new InvalidOperationException("Triggered equipment fixture needs two damage cards and the real champion source.");
            var upgrade = DynamicUpgradeScenario.Upgrade("PojuTriggeredEquipment", "383fbf10-6400-4000-8000-000000000002",
                2, 4, 0, 0, "armor", 2);
            var once = HealingScenario.HealGold(7, true, true);
            Set(once, "trigger", CharacterTriggerData.Trigger.OnEquipmentAdded);
            upgrade.GetCharacterTriggerUpgrades().Add(once);
            Configure(save, normal, upgrade, false);
            var returningUpgrade = upgrade;
            if (Owned)
            {
                returningUpgrade = DynamicUpgradeScenario.Upgrade("PojuEquipmentOwnedSummons", "383fbf10-6400-4000-8000-000000000003",
                    2, 4, 0, 0, "armor", 2);
                returningUpgrade.GetCharacterTriggerUpgrades().Add(once);
                foreach (var pair in new[] { (CharacterTriggerData.Trigger.OnTurnBegin, liveSummon),
                    (CharacterTriggerData.Trigger.OnDeath, deathSummon) })
                {
                    var trigger = HealingScenario.HealGold(0, true, true);
                    Set(trigger, "trigger", pair.Item1); Set(trigger, "effects", new List<CardEffectData> { pair.Item2 });
                    returningUpgrade.GetCharacterTriggerUpgrades().Add(trigger);
                }
            }
            Configure(save, returning, returningUpgrade, true);
            gear = normal.Take(2).Concat(returning.Take(1)).ToArray();
            gear[0].GetTemporaryCardStateModifiers().IncrementAdditionalDamage(1);
            gear[0].GetTemporaryCardStateModifiers().IncrementAdditionalHP(3);
            var anonymous = new CardUpgradeState(); anonymous.Setup(); anonymous.SetAttackDamage(1);
            anonymous.AddStatusEffectUpgradeStacks("spikes", 1);
            gear[1].ApplyPermanentUpgrade(anonymous, save, ignoreUpgradeAnimation: true);
        }

        private static void Configure(SaveManager save, CardState[] cards, CardUpgradeData upgrade, bool returning)
        {
            CardData data = save.GetAllGameData().FindCardData(cards[0].GetCardDataID())!;
            var attach = new CardEffectData("CardEffectAttachEquipment", null!, Team.Type.Monsters);
            attach.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(attach, "paramCardUpgradeData", upgrade);
            data.GetEffects().Clear(); data.GetEffects().Add(attach); data.GetTraits().Clear();
            if (returning)
            { var trait = new CardTraitData(); trait.Setup("CardTraitReturnToHandEquipment"); data.GetTraits().Add(trait); }
            Set(data, "cardType", CardType.Equipment); Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default);
            Set(data, "targetless", false); Set(data, "targetsRoom", true);
            foreach (CardState card in cards) card.Setup(data, save);
        }

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
            CardManager cards = managers.GetCardManager()!; RoomManager rooms = managers.GetRoomManager()!;
            yield return rooms.GetRoomUI().SetSelectedRoom(0);
            int index = cards.GetHand().FindIndex(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678");
            if (index < 0) throw new InvalidOperationException("Triggered equipment setup needs an initial host.");
            CardState source = cards.GetHand()[index]; var point = rooms.GetRoom(0).GetMonsterPoint(0);
            if (!cards.CanPlayHandCard(source, 0, point, null, null, out var error) || !cards.PlayCard(index, point, ref error))
                throw new InvalidOperationException("Triggered equipment setup native summon failed: " + error);
            while (managers.GetReplayManager().IsCardPlaying() || managers.GetCombatManager()!.IsRunningTriggerQueue ||
                (managers.GetHandUI()?.AreAnyCardsAnimating() ?? false))
                yield return null;
            var units = new List<CharacterState>(); rooms.GetRoom(0).AddCharactersToList(units, Team.Type.Monsters);
            CharacterState host = units.Single(unit => unit.GetSpawnerCard() == source);
            foreach (CardState card in gear)
            {
                yield return host.AddEquipment(card, managers.GetCoreManagers(), grafted: false);
                CardState captured = card;
                cards.MoveToStandByPile(card, wasPlayed: true, wasExhausted: false,
                    new RemoveFromStandByCondition(() => cards.CheckEquipmentRemoveFromStandByCondition(host, captured)));
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("TRIGGERED-SUMMON-EQUIPMENT-PREPARED two transferring cards, one return-to-hand exclusion, live/dead copies, modifiers and child replacement.");
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
