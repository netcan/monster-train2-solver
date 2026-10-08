using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class SpawnEnchantScenario
    {
        internal sealed class Record
        {
            public int ActorId { get; set; }
            public bool CanFire { get; set; }
            public int TriggerCount { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public List<UnitCloneCallback> BeforeQueue { get; set; } = new List<UnitCloneCallback>();
            public List<UnitCloneCallback> AfterQueue { get; set; } = new List<UnitCloneCallback>();
            public bool Completed { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        internal static bool Prepared;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); CardManager cards = managers.GetCardManager()!;
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardData shield = save.GetAllGameData().FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!;
            CharacterData shieldUnit = shield.GetSpawnCharacterData()!;
            CharacterData swordUnit = owned.Select(card => card.GetSpawnCharacterData()).First(unit => unit != null && unit != shieldUnit &&
                unit.name.StartsWith("TrainSteward", StringComparison.Ordinal))!;
            CardData sword = save.GetAllGameData().FindCardData(owned.First(card => card.GetSpawnCharacterData() == swordUnit).GetCardDataID())!;
            foreach (CardData data in new[] { shield, sword })
            {
                CharacterData unit = data.GetSpawnCharacterData()!;
                Set(unit, "size", 1); Set(unit, "health", 18); Set(unit, "attackDamage", 6);
                var enchant = new CardEffectData("CardEffectAddStatusEffect", null!, Team.Type.Monsters);
                enchant.Cheat_SetTargetMode(TargetMode.Self);
                Set(enchant, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = "horde", count = 1 },
                    new StatusEffectStackData { statusId = "armor", count = 2 } });
                var callbacks = new List<CharacterTriggerData>();
                foreach (var item in new[] { (CharacterTriggerData.Trigger.OnSpawn, 11), (CharacterTriggerData.Trigger.OnUnscaledSpawn, 13),
                    (CharacterTriggerData.Trigger.OnSpawnNotFromCard, 17), (CharacterTriggerData.Trigger.OnStatusEffectChanged, 7),
                    (CharacterTriggerData.Trigger.CardMonsterPlayed, 19) })
                {
                    CharacterTriggerData trigger = HealingScenario.HealGold(item.Item2, false, true);
                    Set(trigger, "trigger", item.Item1); callbacks.Add(trigger);
                }
                CharacterTriggerData after = HealingScenario.HealGold(5, false, true);
                Set(after, "trigger", CharacterTriggerData.Trigger.AfterSpawnEnchant);
                Set(after, "effects", new[] { enchant }.Concat(after.GetEffects()).ToList()); callbacks.Add(after);
                Set(unit, "triggers", callbacks);
                Set(data.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster"), "paramInt", data == shield ? 1 : 2);
                foreach (CardState card in owned.Where(card => card.GetCardDataID() == data.GetID())) card.Setup(data, save);
            }
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("SPAWN-ENCHANT-PREPARED ordinary and repeated paid births, player-wide Horde/armor enchant, status and Horde child queues, spawning flags and resolving-card caches; original Boss/waves retained.");
        }
        private static List<UnitCloneCallback> Queue() => ((IEnumerable)AccessTools.Property(typeof(CombatManager), "TriggerQueue")
            .GetValue(AllGameManagers.Instance!.GetCombatManager())).Cast<CombatManager.TriggerQueueData>()
            .Select(data => Payload(data.character, data.trigger, data.dyingCharacter, data.fireTriggersData, data.triggerCount)).ToList();
        private static UnitCloneCallback Payload(CharacterState actor, CharacterTriggerData.Trigger kind, CharacterState? dying,
            CharacterState.FireTriggersData? data, int count)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            return new UnitCloneCallback(trace.UnitId(actor), kind.ToString(), data?.paramInt ?? 0, data?.paramInt2 ?? 0, data?.paramString, count,
                dying == null ? 0 : trace.UnitId(dying), data?.overrideTargetCharacter == null ? 0 : trace.UnitId(data.overrideTargetCharacter),
                data?.overrideLastSpawnedCharacter == null ? 0 : trace.UnitId(data.overrideLastSpawnedCharacter));
        }
        private static IEnumerator Observe(IEnumerator native, CharacterState actor, bool canFire, int count)
        {
            FullBattleTrace trace = FullBattleTrace.Active!; RoomState room = actor.GetCurrentRoom();
            var record = new Record { ActorId = trace.UnitId(actor), Before = trace.Capture(room), CanFire = canFire,
                TriggerCount = count, BeforeQueue = Queue() }; Records.Add(record);
            var pending = new List<RoomCombatModel.QueuedCharacterTrigger>();
            var queued = new RoomCombatModel.QueuedCharacterTrigger(room.GetRoomIndex(), record.Before.Units.Single(unit => unit.Id == record.ActorId),
                "AfterSpawnEnchant", canFireTriggers: canFire, triggerCount: count);
            RoomCombatResult predicted = RoomCombatModel.ApplyQueuedCharacterTrigger(record.Before, queued, pending.Add);
            record.UnsupportedReason = predicted.UnsupportedReason;
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose(); record.After = trace.Capture(room); record.AfterQueue = Queue();
                record.Difference = predicted.Supported ? JToken.DeepEquals(JToken.FromObject(predicted.State!), JToken.FromObject(record.After)) ? null : "Enchant actor/room state differs" : predicted.UnsupportedReason;
                if (record.Difference == null)
                    record.Difference = JToken.DeepEquals(JToken.FromObject(record.BeforeQueue.Concat(pending.Select(UnitCloneCallback.From)).ToArray()), JToken.FromObject(record.AfterQueue)) ? null : "Enchant child queue payload/order differs";
            }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class TriggerPatch
        {
            private static void Postfix(CharacterState __instance, CharacterTriggerData.Trigger trigger, bool canFireTriggers,
                bool fromRunningTriggerQueue, int triggerCount, ref IEnumerator __result)
            {
                if (Prepared && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode && fromRunningTriggerQueue &&
                    trigger == CharacterTriggerData.Trigger.AfterSpawnEnchant && __instance.GetTriggers().Any(state => state.GetTrigger() == trigger))
                    __result = Observe(__result, __instance, canFireTriggers, triggerCount);
            }
        }
    }
}
