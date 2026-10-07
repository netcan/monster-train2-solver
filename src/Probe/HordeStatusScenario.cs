using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class HordeStatusScenario
    {
        internal sealed class Callback
        {
            public int ActorId { get; set; }
            public string Kind { get; set; } = "";
            public int ParamInt { get; set; }
            public int ParamInt2 { get; set; }
            public string? ParamString { get; set; }
            public int TriggerCount { get; set; }
            public int DyingId { get; set; }
            public int LastSpawnedOverrideUnitId { get; set; }
        }
        internal sealed class Record
        {
            public string Label { get; set; } = "";
            public string Operation { get; set; } = "";
            public string StatusId { get; set; } = "";
            public int Amount { get; set; }
            public int ActorId { get; set; }
            public int SourceCardId { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public RoomCombatState? AfterDrain { get; set; }
            public int QueueBeforeDrain { get; set; }
            public int QueueAfterDrain { get; set; }
            public bool RunningBeforeDrain { get; set; }
            public bool ActorPreviewBeforeDrain { get; set; }
            public bool SavePreviewBeforeDrain { get; set; }
            public List<Callback> Callbacks { get; set; } = new List<Callback>();
            public List<Callback> Queued { get; set; } = new List<Callback>();
            public List<Callback> Dispatched { get; set; } = new List<Callback>();
        }
        internal static readonly List<Record> Records = new List<Record>();
        internal static bool Started, Completed;
        internal static string? Error;
        private static Record? current;
        private static Record? draining;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            AbilityCooldownScenario.Prepare(managers, log);
            CardData data = managers.GetSaveManager().GetAllGameData().FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!;
            CharacterData unit = data.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            AccessTools.Field(typeof(CharacterData), "startingStatusEffects").SetValue(unit,
                unit.GetStartingStatusEffects().Concat(new[] { new StatusEffectStackData { statusId = "horde", count = 2 } }).ToArray());
            var triggers = unit.GetTriggers().ToList();
            foreach (var entry in new[] { (CharacterTriggerData.Trigger.OnTroopAdded, 5), (CharacterTriggerData.Trigger.OnTroopRemoved, 10) })
            {
                CharacterTriggerData trigger = HealingScenario.HealGold(entry.Item2, false, true);
                AccessTools.Field(typeof(CharacterTriggerData), "trigger").SetValue(trigger, entry.Item1);
                AccessTools.Field(typeof(CharacterTriggerData), "triggerAtThreshold").SetValue(trigger, 2);
                triggers.Add(trigger);
            }
            AccessTools.Field(typeof(CharacterData), "triggers").SetValue(unit, triggers);
            log.LogInfo("HORDE-STATUS-PREPARED authored two-troop Stewards, real skills, troop callbacks and unchanged Boss/waves.");
        }
        internal static void Start(AllGameManagers managers, ManualLogSource log)
        { Started = true; managers.GetSaveManager().StartCoroutine(Protect(Run(managers), log)); }
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
            (native as IDisposable)?.Dispose(); current = null; Completed = true;
        }
        private static IEnumerator Run(AllGameManagers managers)
        {
            CardManager cards = managers.GetCardManager()!; RoomManager rooms = managers.GetRoomManager()!;
            CombatManager combat = managers.GetCombatManager()!; FullBattleTrace trace = FullBattleTrace.Active!;
            yield return rooms.GetRoomUI().SetSelectedRoom(0);
            int hand = cards.GetHand().FindIndex(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678");
            if (hand < 0) throw new InvalidOperationException("Horde fixture requires an initial Steward.");
            CardState source = cards.GetHand()[hand]; var point = rooms.GetRoom(0).GetMonsterPoint(0);
            if (!cards.CanPlayHandCard(source, 0, point, null, null, out var error) || !cards.PlayCard(hand, point, ref error))
                throw new InvalidOperationException("Native Horde summon failed: " + error);
            while (!Ready()) yield return null;
            var monsters = new List<CharacterState>(); rooms.GetRoom(0).AddCharactersToList(monsters, Team.Type.Monsters);
            CharacterState host = monsters.Single(unit => unit.GetSpawnerCard() == source);
            int observerHand = cards.GetHand().FindIndex(card => card.GetCardDataID() == source.GetCardDataID());
            if (observerHand < 0) throw new InvalidOperationException("Horde fixture requires a second Steward to observe rally/harvest queues.");
            var observerPoint = rooms.GetRoom(0).GetMonsterPoint(1);
            if (!cards.CanPlayHandCard(cards.GetHand()[observerHand], 0, observerPoint, null, null, out error) ||
                !cards.PlayCard(observerHand, observerPoint, ref error))
                throw new InvalidOperationException("Native Horde observer summon failed: " + error);
            while (!Ready()) yield return null;
            // Direct API calls do not hold the normal card-resolution gate. Keep UI preview
            // from switching native queues while these controlled operations yield.
            bool previousSuppression = combat.SuppressCombatPreviewUpdates;
            combat.SuppressCombatPreviewUpdates = true;
            try
            {
                yield return Apply("cooldown-ready", "Remove", "cooldown", -1);
                yield return Apply("troops-grow", "Add", "horde", 2);
                yield return Apply("zero-add", "Add", "horde", 0);
                yield return Apply("negative-add", "Add", "horde", -1);
                yield return Apply("troops-remove", "Remove", "horde", 1);
                yield return Apply("damage-casualty", "Damage", "", 51);
                yield return Apply("troops-regrow", "Add", "horde", 2);
                yield return Apply("maxhp-casualty", "DebuffHealth", "", 30);
            }
            finally { combat.SuppressCombatPreviewUpdates = previousSuppression; current = draining = null; }
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(combat, true);

            IEnumerator Apply(string label, string operation, string status, int amount)
            {
                var record = new Record { Label = label, Operation = operation, StatusId = status, Amount = amount,
                    ActorId = trace.UnitId(host), SourceCardId = operation == "DebuffHealth" ? 0 : trace.CardId(source), Before = trace.Capture(host.GetCurrentRoom()) };
                Records.Add(record); current = record;
                if (operation == "Add") host.AddStatusEffect(status, amount, new CharacterState.AddStatusEffectParams { sourceCardState = source }, allowModification: false);
                else if (operation == "Remove") host.RemoveStatusEffect(status, amount, new CharacterState.RemoveStatusEffectParams { sourceCardState = source }, allowModification: false);
                else if (operation == "Damage") yield return host.ApplyDamage(amount,
                    new CharacterState.ApplyDamageParams { damageSourceCard = source, damageType = Damage.Type.Default }, managers.GetPlayerManager(), managers.GetCardStatistics());
                else yield return host.DebuffMaxHP(amount, 0);
                record.After = trace.Capture(host.GetCurrentRoom()); current = null;
                record.QueueBeforeDrain = QueueCount(); record.RunningBeforeDrain = combat.IsRunningTriggerQueue;
                record.ActorPreviewBeforeDrain = host.PreviewMode;
                record.SavePreviewBeforeDrain = managers.GetSaveManager().PreviewMode;
                foreach (CombatManager.TriggerQueueData data in (System.Collections.IEnumerable)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat))
                    record.Queued.Add(Payload(data.character, data.trigger, data.dyingCharacter, data.fireTriggersData, data.triggerCount));
                draining = record;
                while (combat.IsRunningTriggerQueue) yield return null;
                yield return combat.RunTriggerQueue();
                while (combat.IsRunningTriggerQueue) yield return null;
                record.QueueAfterDrain = QueueCount(); record.AfterDrain = trace.Capture(host.GetCurrentRoom());
                draining = null;
            }
            int QueueCount() => ((System.Collections.ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat)).Count;
            bool Ready() => !managers.GetSaveManager().PreviewMode && !managers.GetReplayManager().IsCardPlaying() &&
                !combat.IsRunningTriggerQueue && QueueCount() == 0 && !(managers.GetHandUI()?.AreAnyCardsAnimating() ?? false) &&
                !(bool)AccessTools.Field(typeof(CombatManager), "combatStateChanged").GetValue(combat);
        }
        private static Callback Payload(CharacterState actor, CharacterTriggerData.Trigger kind, CharacterState dying,
            CharacterState.FireTriggersData data, int count)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            return new Callback { ActorId = trace.UnitId(actor), Kind = kind.ToString(), ParamInt = data?.paramInt ?? 0,
                ParamInt2 = data?.paramInt2 ?? 0, ParamString = data?.paramString, TriggerCount = count,
                DyingId = dying == null ? 0 : trace.UnitId(dying),
                LastSpawnedOverrideUnitId = data?.overrideLastSpawnedCharacter == null ? 0 : trace.UnitId(data.overrideLastSpawnedCharacter) };
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class FirePatch
        {
            private static void Prefix(CharacterState __instance, CharacterTriggerData.Trigger trigger, CharacterState dyingCharacter,
                CharacterState.FireTriggersData fireTriggersData, int triggerCount, bool fromRunningTriggerQueue)
            {
                if (draining != null && fromRunningTriggerQueue)
                    draining.Dispatched.Add(Payload(__instance, trigger, dyingCharacter, fireTriggersData, triggerCount));
            }
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.QueueTrigger), new[] { typeof(CharacterState), typeof(CharacterTriggerData.Trigger), typeof(CharacterState), typeof(bool), typeof(bool), typeof(CharacterState.FireTriggersData), typeof(int), typeof(CharacterTriggerState) })]
        private static class QueuePatch
        {
            private static void Prefix(CharacterState character, CharacterTriggerData.Trigger trigger, CharacterState dyingCharacter,
                CharacterState.FireTriggersData fireTriggersData, int triggerCount)
            {
                if (current == null || FullBattleTrace.Active == null || AllGameManagers.Instance!.GetSaveManager().PreviewMode) return;
                FullBattleTrace trace = FullBattleTrace.Active;
                current.Callbacks.Add(new Callback { ActorId = trace.UnitId(character), Kind = trigger.ToString(),
                    ParamInt = fireTriggersData?.paramInt ?? 0, ParamInt2 = fireTriggersData?.paramInt2 ?? 0,
                    ParamString = fireTriggersData?.paramString, TriggerCount = triggerCount,
                    DyingId = dyingCharacter == null ? 0 : trace.UnitId(dyingCharacter),
                    LastSpawnedOverrideUnitId = fireTriggersData?.overrideLastSpawnedCharacter == null ? 0 : trace.UnitId(fireTriggersData.overrideLastSpawnedCharacter) });
            }
        }
    }
}
