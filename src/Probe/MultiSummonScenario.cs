using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class MultiSummonScenario
    {
        internal static bool Prepared, ZeroStarted, ZeroCompleted;
        internal static string? Error;
        private static bool holding;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); CardManager cards = managers.GetCardManager()!;
            CardData data = save.GetAllGameData().FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!;
            CardEffectData spawn = data.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster");
            Set(spawn, "paramInt", 4);
            string scenario = Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? "";
            if (scenario.StartsWith("multi-summon-upgrade", StringComparison.Ordinal))
            {
                bool restricted = scenario == "multi-summon-upgrade-restricted";
                var extra = DynamicUpgradeScenario.Upgrade("PojuMultiSummonExtra", SpawnUpgradeProbe.UpgradeDataId,
                    2, 3, restricted ? 1 : 0, 0, "armor", 2);
                Set(extra, "isUnique", scenario == "multi-summon-upgrade-unique");
                Set(extra, "restrictSizeToRoomCapacity", restricted);
                Set(spawn, "paramCardUpgradeData", extra);
            }
            CharacterData unit = spawn.GetParamCharacterData(); Set(unit, "size", 1);
            var born = Gold(CharacterTriggerData.Trigger.OnSpawn, 1);
            var unscaled = Gold(CharacterTriggerData.Trigger.OnUnscaledSpawn, 2);
            var noCard = Gold(CharacterTriggerData.Trigger.OnSpawnNotFromCard, 1000);
            var rally = Gold(CharacterTriggerData.Trigger.CardMonsterPlayed, 3);

            var armor = DynamicUpgradeScenario.Upgrade("PojuMultiSummonArmor", "c2f6ed7f-18ce-4070-b65f-7dd9f5190021", 0, 0, 0, 0, "armor", 1);
            var effect = new CardEffectData("CardEffectAddTempCardUpgradeToUnits", null!, Team.Type.Monsters);
            effect.Cheat_SetTargetMode(TargetMode.LastSpawnedCharacter); Set(effect, "paramCardUpgradeData", armor);
            Set(effect, "additionalParamInt1", (int)UnitUpgradeLifetime.TemporaryUntilUnitDeath);
            var grown = DynamicUpgradeScenario.Upgrade("PojuMultiSummonLiveSource", "c2f6ed7f-18ce-4070-b65f-7dd9f5190024", 1, 1, 0, 0, "armor", 0);
            var growth = new CardEffectData("CardEffectAddTempCardUpgradeToUnits", null!, Team.Type.Monsters);
            growth.Cheat_SetTargetMode(TargetMode.Self); Set(growth, "paramCardUpgradeData", grown);
            Set(growth, "additionalParamInt1", (int)UnitUpgradeLifetime.TemporaryUntilEndOfBattle);
            Set(rally, "effects", new List<CardEffectData> { new CardEffectData("CardEffectRewardGold", null!, Team.Type.None), effect, growth });
            Set(rally.GetEffects()[0], "paramInt", 3); rally.GetEffects()[0].Cheat_SetTargetMode(TargetMode.Room);
            Set(unit, "triggers", unit.GetTriggers().Concat(new[] { born, unscaled, noCard, rally }).ToList());
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == data.GetID()))
            {
                card.Setup(data, save);
                Add(card, DynamicUpgradeScenario.Upgrade("PojuMultiSummonPermanent", "c2f6ed7f-18ce-4070-b65f-7dd9f5190022", 2, 3, 0, 0, "armor", 0), false);
                Add(card, DynamicUpgradeScenario.Upgrade("PojuMultiSummonTemporary", "c2f6ed7f-18ce-4070-b65f-7dd9f5190023", 1, 2, 0, 0, "armor", 0), true);
                var excludedPermanent = DynamicUpgradeScenario.Upgrade("PojuMultiSummonExcludedPermanent", "c2f6ed7f-18ce-4070-b65f-7dd9f5190025", 6, 4, 0, 0, "armor", 0);
                var excludedTemporary = DynamicUpgradeScenario.Upgrade("PojuMultiSummonExcludedTemporary", "c2f6ed7f-18ce-4070-b65f-7dd9f5190026", 3, 2, 0, 0, "armor", 0);
                Set(excludedPermanent, "excludeFromClones", true); Set(excludedTemporary, "excludeFromClones", true);
                Add(card, excludedPermanent, false); Add(card, excludedTemporary, true);
            }
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("MULTI-SUMMON-PREPARED four one-size Stewards per real card, copied/excluded upgrades and changing live source upgrades, cardless Rally armor/rewards and separate spawn/unscaled/no-card rewards; original Boss/waves retained.");
        }
        internal static void StartZero(AllGameManagers managers, ManualLogSource log)
        { ZeroStarted = true; managers.GetSaveManager().StartCoroutine(Protect(Zero(managers), log)); }
        private static IEnumerator Protect(IEnumerator native, ManualLogSource log)
        {
            while (true)
            {
                object? current;
                try { if (!native.MoveNext()) break; current = native.Current; }
                catch (Exception error) { Error = error.ToString(); holding = false; log.LogError(Error); yield break; }
                yield return current;
            }
            ZeroCompleted = true; holding = false;
        }
        private static IEnumerator Zero(AllGameManagers managers)
        {
            holding = true;
            SaveManager save = managers.GetSaveManager();
            CardState source = managers.GetCardManager()!.GetHand().First(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678");
            CardState copied = CardManager.CopyCardState(source, save, save.GetAllGameData(), managers.GetCardStatistics());
            CharacterState host = null!;
            yield return managers.GetMonsterManager()!.CreateMonsterState(source.GetSpawnCharacterData(), copied, 2, unit => host = unit,
                SpawnMode.SelectedSlot, managers.GetRoomManager()!.GetRoom(2).GetMonsterPoint(0), isCardless: true);
            RallyScenario.Label = "multi-summon-zero-dispatch";
            yield return managers.GetCombatManager()!.QueueAndRunTrigger(host, CharacterTriggerData.Trigger.CardMonsterPlayed, triggerCount: 0);
            RallyScenario.Label = null;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
        }
        [HarmonyPatch(typeof(CombatManager), "ProcessEffectsQueue")]
        private static class BackgroundPatch
        {
            private static bool Prefix(ref IEnumerator __result)
            { if (!holding) return true; __result = Hold(); return false; }
            private static IEnumerator Hold() { yield return null; }
        }
        internal static PlayCardAction? Choose(AllGameManagers managers)
        {
            CardManager cards = managers.GetCardManager()!; RoomManager rooms = managers.GetRoomManager()!;
            foreach (CardState card in cards.GetHand().Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678"))
            foreach (int room in new[] { 0, 1, 2 })
            {
                var units = new List<CharacterState>(); rooms.GetRoom(room).AddCharactersToList(units, Team.Type.Monsters);
                int position = units.Count > 0 ? 1 : 0;
                SpawnPoint point = rooms.GetRoom(room).GetMonsterPoint(position);
                if (cards.CanPlayHandCard(card, room, point, null, null, out _))
                    return new PlayCardAction(FullBattleTrace.Active!.CardId(card), room, position);
            }
            return null;
        }
        private static CharacterTriggerData Gold(CharacterTriggerData.Trigger kind, int amount)
        { var trigger = HealingScenario.HealGold(amount, false, true); Set(trigger, "trigger", kind); return trigger; }
        private static void Add(CardState card, CardUpgradeData data, bool temporary)
        { var state = new CardUpgradeState(); state.Setup(data); (temporary ? card.GetTemporaryCardStateModifiers() : card.GetCardStateModifiers()).AddUpgrade(state); }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
