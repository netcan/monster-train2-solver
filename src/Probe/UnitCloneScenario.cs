using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitCloneScenario
    {
        internal sealed class Boundary
        {
            public string Label { get; set; } = "";
            public TrainCombatState State { get; set; } = null!;
            public int QueueCount { get; set; }
            public List<UnitCloneCallback> Queued { get; set; } = new List<UnitCloneCallback>();
        }
        internal sealed class Record
        {
            public string Label { get; set; } = "";
            public int SourceId { get; set; }
            public int RoomIndex { get; set; }
            public string SpawnMode { get; set; } = "";
            public SpawnPointReference? Location { get; set; }
            public bool Cardless { get; set; }
            public UnitCloneRule? Rule { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? AfterApi { get; set; }
            public TrainCombatState? After { get; set; }
            public int UnitId { get; set; }
            public int QueueAfterApi { get; set; }
            public int QueueAfter { get; set; }
            public int AutomaticQueueDeferrals { get; set; }
            public List<Boundary> Boundaries { get; set; } = new List<Boundary>();
            public List<UnitCloneCallback> Queued { get; set; } = new List<UnitCloneCallback>();
            public List<UnitCloneCallback> Dispatched { get; set; } = new List<UnitCloneCallback>();
        }
        internal static readonly List<Record> Records = new List<Record>();
        internal static bool Started, Completed;
        internal static string? Error;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "unit-clone";
        private static Record? current;
        private static Record? draining;
        private static bool intrinsicQueue;
        private static int sourceCopies;
        private static CardData steward = null!, originalAbility = null!, changedAbility = null!, gear = null!;
        private static CharacterData unitData = null!;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData();
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            steward = all.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!;
            unitData = steward.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            Set(unitData, "health", 90); Set(unitData, "attackDamage", 8); Set(unitData, "size", 1); Set(unitData, "equipmentLimit", 2);
            var callbacks = new List<CharacterTriggerData>();
            foreach (var item in new[] { (CharacterTriggerData.Trigger.OnSpawn, 5), (CharacterTriggerData.Trigger.OnUnscaledSpawn, 7),
                (CharacterTriggerData.Trigger.OnSpawnNotFromCard, 11), (CharacterTriggerData.Trigger.OnStatusEffectChanged, 13),
                (CharacterTriggerData.Trigger.OnUnitAbilityAvailable, 17), (CharacterTriggerData.Trigger.OnUnitAbilityUnavailable, 19),
                (CharacterTriggerData.Trigger.CardMonsterPlayed, 23) })
            { var callback = HealingScenario.HealGold(item.Item2, false, true); Set(callback, "trigger", item.Item1); callbacks.Add(callback); }
            Set(unitData, "triggers", callbacks);
            CardData spell = all.FindCardData(owned.First(card => card.GetEffects().Any(effect =>
                effect.GetEffectStateName() == "CardEffectDamage" && effect.GetTargetMode() == TargetMode.DropTargetCharacter)).GetCardDataID())!;
            originalAbility = Ability("PojuCloneAbility", "c2f6ed7f-18ce-4070-b65f-7dd9f5210001", 3, 2);
            changedAbility = Ability("PojuCloneChangedAbility", "c2f6ed7f-18ce-4070-b65f-7dd9f5210002", 7, 4);
            Set(unitData, "unitAbility", originalAbility);
            foreach (CardState card in owned.Where(card => card.GetCardDataID() == steward.GetID()))
            {
                card.Setup(steward, save);
                var permanent = new CardUpgradeState(); permanent.Setup(); permanent.SetAttackDamage(3);
                permanent.SetAdditionalHP(5); permanent.AddStatusEffectUpgradeStacks("armor", 2); permanent.SetExcludeFromClones(true);
                card.ApplyPermanentUpgrade(permanent, save, ignoreUpgradeAnimation: true);
                var temporary = new CardUpgradeState(); temporary.Setup(); temporary.SetAttackDamage(2);
                temporary.SetAdditionalHP(3); temporary.AddStatusEffectUpgradeStacks("regen", 3); temporary.SetExcludeFromClones(true);
                card.ApplyTemporaryUpgrade(temporary);
            }
            gear = all.FindCardData(owned.First(card => card.GetCardDataID() == spell.GetID()).GetCardDataID())!;
            var effect = new CardEffectData("CardEffectAttachEquipment", null!, Team.Type.Monsters); effect.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            Set(effect, "paramCardUpgradeData", DynamicUpgradeScenario.Upgrade("PojuCloneGear", "c2f6ed7f-18ce-4070-b65f-7dd9f5210003", 1, 3, 0, 0, "armor", 2));
            gear.GetEffects().Clear(); gear.GetEffects().Add(effect); gear.GetTraits().Clear();
            Set(gear, "overrideDescriptionKey", ""); Set(gear, "cardType", CardType.Equipment); Set(gear, "cost", 0);
            Set(gear, "targetless", false); Set(gear, "targetsRoom", true);
            foreach (CardState card in owned.Where(card => card.GetCardDataID() == gear.GetID())) card.Setup(gear, save);
            log.LogInfo("UNIT-CLONE-PREPARED native ordinary birth, excluded upgrades, stats, gear and skill copies; original Boss/waves intact.");

            CardData Ability(string name, string id, int cooldown, int spawn)
            {
                CardData data = UnityEngine.Object.Instantiate(spell); data.name = name; Set(data, "id", id);
                Set(data, "isUnitAbility", true); Set(data, "cooldownAfterActivated", cooldown); Set(data, "cooldownAtSpawn", spawn);
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data); return data;
            }
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
            (native as IDisposable)?.Dispose(); current = draining = null; Completed = Error == null;
        }
        private static IEnumerator Run(AllGameManagers managers)
        {
            CardManager cards = managers.GetCardManager()!; RoomManager rooms = managers.GetRoomManager()!;
            CombatManager combat = managers.GetCombatManager()!; FullBattleTrace trace = FullBattleTrace.Active!;
            MonsterManager monsters = managers.GetMonsterManager()!;
            yield return rooms.GetRoomUI().SetSelectedRoom(0);
            int hand = cards.GetHand().FindIndex(card => card.GetCardDataID() == steward.GetID());
            if (hand < 0) throw new InvalidOperationException("Clone requires a paid host.");
            CardState hostCard = cards.GetHand()[hand]; SpawnPoint drop = rooms.GetRoom(0).GetMonsterPoint(0);
            if (!cards.CanPlayHandCard(hostCard, 0, drop, null, null, out var error) || !cards.PlayCard(hand, drop, ref error))
                throw new InvalidOperationException("Clone paid host failed: " + error);
            while (!Ready()) yield return null;
            var actors = new List<CharacterState>(); rooms.GetRoom(0).AddCharactersToList(actors, Team.Type.Monsters);
            CharacterState host = actors.Single(unit => unit.GetSpawnerCard() == hostCard), firstClone = null!;
            bool previous = combat.SuppressCombatPreviewUpdates; combat.SuppressCombatPreviewUpdates = true;
            try
            {
                yield return Clone("null-source", null, 0);
                yield return Clone("invalid-room", host, 99);
                yield return Clone("ordinary-front", host, 0, created: unit => firstClone = unit);
                yield return Clone("ordinary-back-cardless", host, 1, SpawnMode.BackSlot, cardless: true);
                host.BuffDamage(9); host.SetHealth(41, host.GetMaxHP()); host.AddStatusEffect("armor", 4);
                host.AddStatusEffect("regen", 2); host.AddStatusEffect("silenced", 1); host.SetUnitAbilityCooldown(6);
                yield return combat.RunTriggerQueue();
                yield return Clone("wounded-buffed-excluded", host, 1, SpawnMode.SelectedSlot, host.GetSpawnPoint(), true);
                host.RemoveStatusEffect("silenced", 1); host.DebuffDamage(30); yield return combat.RunTriggerQueue();
                yield return Clone("negative-damage-buff", host, 1);
                host.BuffDamage(30);
                CardState equipment = cards.GetAllCards(new List<CardState>()).First(card => card.GetCardDataID() == gear.GetID());
                yield return host.AddEquipment(equipment, managers.GetCoreManagers(), grafted: false);
                cards.MoveToStandByPile(equipment, wasPlayed: true, wasExhausted: false,
                    new RemoveFromStandByCondition(() => cards.CheckEquipmentRemoveFromStandByCondition(host, equipment)));
                yield return combat.RunTriggerQueue();
                yield return Clone("equipped-source", host, 2, cardless: true);
                yield return host.SetUnitAbility(changedAbility, isFromEquipment: true); host.SetUnitAbilityCooldown(11);
                yield return combat.RunTriggerQueue();
                yield return Clone("runtime-equipment-ability", host, 2, SpawnMode.SelectedSlot, host.GetSpawnPoint(), true);
                yield return Clone("clone-of-clone", firstClone, 2, SpawnMode.BackSlot);
                CharacterData filler = UnityEngine.Object.Instantiate(unitData); filler.name = "PojuCloneFiller";
                Set(filler, "id", "c2f6ed7f-18ce-4070-b65f-7dd9f5210004"); Set(filler, "attackDamage", 0); Set(filler, "health", 1);
                Set(filler, "triggers", new List<CharacterTriggerData>()); Set(filler, "unitAbility", null!);
                CharacterState cardless = null!;
                yield return monsters.CreateMonsterState(filler, null!, 0, unit => cardless = unit, isCardless: true);
                yield return Clone("cardless-source", cardless, 1, cardless: true);
                while (rooms.GetRoom(2).GetRemainingSpawnPointCount(Team.Type.Monsters) > 0)
                    yield return monsters.CreateMonsterState(filler, null!, 2, null!, isCardless: true);
                yield return Clone("full-room-card-allocation", host, 2);
                var full = new List<CharacterState>(); rooms.GetRoom(2).AddCharactersToList(full, Team.Type.Monsters);
                CharacterState last = full.Single(unit => unit.GetSpawnPoint().GetIndexInRoom() == combat.NumSpawnPointsPerFloor(Team.Type.Monsters) - 1);
                yield return Clone("selected-no-adjacent", last, 2, SpawnMode.SelectedSlot, last.GetSpawnPoint(), true);
                host.AddStatusEffect("horde", 1); yield return combat.RunTriggerQueue();
                yield return Clone("horde-no-birth", host, 0, cardless: true);
                Set(combat, "combatStateChanged", true);
            }
            finally { current = null; combat.SuppressCombatPreviewUpdates = previous; }
            while (!Ready()) yield return null;

            IEnumerator Clone(string label, CharacterState? source, int room, SpawnMode mode = SpawnMode.FrontSlot,
                SpawnPoint? location = null, bool cardless = false, Action<CharacterState>? created = null)
            {
                var record = new Record { Label = label, SourceId = source == null ? 0 : trace.UnitId(source), RoomIndex = room,
                    SpawnMode = mode.ToString(), Cardless = cardless, Rule = source == null ? null : CaptureRule(source),
                    Location = location == null ? null : new SpawnPointReference(location.GetRoomOwner()!.GetRoomIndex(),
                        source!.GetTeamType() == Team.Type.Heroes ? CombatTeam.Enemy : CombatTeam.Player, location.GetIndexInRoom()), Before = trace.CaptureTrain() };
                Records.Add(record); current = record; sourceCopies = 0;
                yield return monsters.CloneMonsterState(source!, room, unit => { record.UnitId = unit == null ? 0 : trace.UnitId(unit); if (unit != null) created?.Invoke(unit); },
                    managers.GetCoreManagers(), mode, location!, isCardless: cardless);
                record.AfterApi = trace.CaptureTrain(); record.QueueAfterApi = Count(); record.Queued = Queue(); current = null; draining = record;
                yield return combat.RunTriggerQueue(); while (combat.IsRunningTriggerQueue) yield return null;
                record.After = trace.CaptureTrain(); record.QueueAfter = Count(); draining = null;
            }
            bool Ready() => !managers.GetSaveManager().PreviewMode && !managers.GetReplayManager().IsCardPlaying() &&
                !combat.IsRunningTriggerQueue && Count() == 0 && !(managers.GetHandUI()?.AreAnyCardsAnimating() ?? false) &&
                (combat.SuppressCombatPreviewUpdates || !(bool)AccessTools.Field(typeof(CombatManager), "combatStateChanged").GetValue(combat));
        }
        private static UnitCloneRule CaptureRule(CharacterState source)
        {
            FullBattleTrace trace = FullBattleTrace.Active!; AllGameData all = AllGameManagers.Instance!.GetSaveManager().GetAllGameData();
            CardState? card = source.GetSpawnerCard(); CardData data = card == null ? steward : all.FindCardData(card.GetCardDataID())!;
            var equipment = source.GetEquipment().Select(item => new UnitCloneGearRule(trace.CardId(item),
                CardGenerationProbe.Creation(all.FindCardData(item.GetCardDataID())!), item.GetEffects().Where(effect => effect.GetParamCardUpgradeData() != null)
                    .Select(effect => { var upgrade = new CardUpgradeState(); upgrade.Setup(effect.GetParamCardUpgradeData()); return CardModifierProbe.Upgrade(upgrade); }).ToArray(),
                item.HasTrait<CardTraitGraftedEquipment>())).ToArray();
            return new UnitCloneRule(BattleActionProbe.BirthDefinition(data, source.GetSourceCharacterData(), card == null),
                card == null ? null : CardGenerationProbe.Creation(data), equipment,
                source.GetUnitAbility() == null ? null : AbilityLifecycleProbe.Change(source.GetUnitAbility(), source.GetUnitAbilityIsFromEquipment()),
                source.GetSourceCharacterData().GetGraftedEquipment() != null);
        }
        private static void Mark(string label)
        { if (current != null) current.Boundaries.Add(new Boundary { Label = label, State = FullBattleTrace.Active!.CaptureTrain(), QueueCount = Count(), Queued = Queue() }); }
        private static List<UnitCloneCallback> Queue() => ((IEnumerable)AccessTools.Property(typeof(CombatManager), "TriggerQueue")
            .GetValue(AllGameManagers.Instance!.GetCombatManager())).Cast<CombatManager.TriggerQueueData>()
            .Select(data => Payload(data.character, data.trigger, data.dyingCharacter, data.fireTriggersData, data.triggerCount)).ToList();
        private static UnitCloneCallback Payload(CharacterState actor, CharacterTriggerData.Trigger kind, CharacterState? dying,
            CharacterState.FireTriggersData? data, int count)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            return new UnitCloneCallback(trace.UnitId(actor), kind.ToString(), data?.paramInt ?? 0, data?.paramInt2 ?? 0, data?.paramString,
                count, dying == null ? 0 : trace.UnitId(dying), data?.overrideTargetCharacter == null ? 0 : trace.UnitId(data.overrideTargetCharacter),
                data?.overrideLastSpawnedCharacter == null ? 0 : trace.UnitId(data.overrideLastSpawnedCharacter));
        }
        private static int Count() => ((ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(AllGameManagers.Instance!.GetCombatManager())).Count;
        private static void Set(object target, string name, object value) => AccessTools.Field(target.GetType(), name).SetValue(target, value);
        [HarmonyPatch(typeof(CardManager), nameof(CardManager.CopyCardState))]
        private static class CopyPatch
        { private static void Postfix(CardState __result) { if (current == null) return; FullBattleTrace.Active!.CardId(__result); Mark(sourceCopies++ == 0 ? "source-card-copied" : "gear-card-copied"); } }
        [HarmonyPatch(typeof(MonsterManager), nameof(MonsterManager.CreateMonsterState))]
        private static class BirthPatch
        {
            private static void Postfix(ref IEnumerator __result) { if (current != null) __result = Observe(__result); }
            private static IEnumerator Observe(IEnumerator native)
            {
                Mark("before-birth"); bool previous = intrinsicQueue; intrinsicQueue = true;
                try { while (native.MoveNext()) yield return native.Current; }
                finally { (native as IDisposable)?.Dispose(); intrinsicQueue = previous; Mark("after-birth"); }
            }
        }
        [HarmonyPatch(typeof(CharacterHelper), nameof(CharacterHelper.CopyCharacterStats))]
        private static class StatsPatch
        { private static void Postfix() => Mark("after-stats"); }
        [HarmonyPatch(typeof(CharacterHelper), nameof(CharacterHelper.CopyCharacterAbility))]
        private static class AbilityPatch
        {
            private static void Postfix(ref IEnumerator __result) { if (current != null) __result = Observe(__result); }
            private static IEnumerator Observe(IEnumerator native)
            { try { while (native.MoveNext()) yield return native.Current; } finally { (native as IDisposable)?.Dispose(); Mark("after-ability"); } }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.ApplyCardUpgrade))]
        private static class UpgradePatch
        {
            private static void Postfix(ref IEnumerator __result) { if (current != null) __result = Observe(__result); }
            private static IEnumerator Observe(IEnumerator native)
            {
                bool previous = intrinsicQueue; intrinsicQueue = true;
                try { while (native.MoveNext()) yield return native.Current; }
                finally { (native as IDisposable)?.Dispose(); intrinsicQueue = previous; }
            }
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.RunTriggerQueue))]
        private static class QuietPatch
        {
            private static bool Prefix(CombatManager __instance, ref IEnumerator __result)
            {
                if (current == null || intrinsicQueue || __instance.IsRunningTriggerQueue) return true;
                current.AutomaticQueueDeferrals++; __result = Empty(); return false;
            }
            private static IEnumerator Empty() { yield break; }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class FirePatch
        {
            private static void Prefix(CharacterState __instance, CharacterTriggerData.Trigger trigger, CharacterState dyingCharacter,
                CharacterState.FireTriggersData fireTriggersData, int triggerCount, bool fromRunningTriggerQueue)
            { if (draining != null && fromRunningTriggerQueue) draining.Dispatched.Add(Payload(__instance, trigger, dyingCharacter, fireTriggersData, triggerCount)); }
        }
    }
}
