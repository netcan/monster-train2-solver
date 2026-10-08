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
    internal static class UnitCopyScenario
    {
        internal sealed class Record
        {
            public string Label { get; set; } = "";
            public int Count { get; set; }
            public int RoomIndex { get; set; }
            public int SourceCardId { get; set; }
            public int[] Targets { get; set; } = Array.Empty<int>();
            public UnitCopyCatalog Catalog { get; set; } = null!;
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? After { get; set; }
            public List<UnitCloneCallback> BeforeQueue { get; set; } = new List<UnitCloneCallback>();
            public List<UnitCloneCallback> AfterQueue { get; set; } = new List<UnitCloneCallback>();
            public List<UnitCloneCallback> Dispatched { get; set; } = new List<UnitCloneCallback>();
            public int AutomaticQueueDeferrals { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        internal static bool Started, Completed;
        internal static string? Error;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "unit-copy";
        private static CardState source = null!, gearCard = null!;
        private static CharacterData unitData = null!;
        private static CardData changedAbility = null!;
        private static string? label;
        private static Record? current;
        private static bool intrinsic;
        private static bool suppressBackground;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData();
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            source = owned.First(card => card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDamage" && effect.GetTargetMode() == TargetMode.DropTargetCharacter));
            gearCard = owned.First(card => card.GetCardDataID() != source.GetCardDataID() && card.GetEffects().All(effect =>
                effect.GetEffectStateName() != "CardEffectSpawnMonster"));
            CardData steward = all.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!;
            unitData = steward.GetSpawnCharacterData()!;
            Set(unitData, "health", 90); Set(unitData, "attackDamage", 8); Set(unitData, "size", 1); Set(unitData, "equipmentLimit", 2);
            var callbacks = new List<CharacterTriggerData>();
            foreach (var item in new[] { (CharacterTriggerData.Trigger.OnSpawn, 3), (CharacterTriggerData.Trigger.OnUnscaledSpawn, 5),
                (CharacterTriggerData.Trigger.OnSpawnNotFromCard, 7), (CharacterTriggerData.Trigger.OnStatusEffectChanged, 11),
                (CharacterTriggerData.Trigger.OnUnitAbilityAvailable, 13), (CharacterTriggerData.Trigger.OnUnitAbilityUnavailable, 17),
                (CharacterTriggerData.Trigger.CardMonsterPlayed, 19) })
            { var trigger = HealingScenario.HealGold(item.Item2, false, true); Set(trigger, "trigger", item.Item1); callbacks.Add(trigger); }
            Set(unitData, "triggers", callbacks);
            CardData originalAbility = Ability("PojuCopyNaturalAbility", "c2f6ed7f-18ce-4070-b65f-7dd9f5220001", 3, 2);
            changedAbility = Ability("PojuCopyReplacementAbility", "c2f6ed7f-18ce-4070-b65f-7dd9f5220002", 7, 4);
            Set(unitData, "unitAbility", originalAbility);
            foreach (CardState card in owned.Where(card => card.GetCardDataID() == steward.GetID()))
            {
                card.Setup(steward, save);
                var upgrade = new CardUpgradeState(); upgrade.Setup(); upgrade.SetAttackDamage(3); upgrade.SetAdditionalHP(5);
                upgrade.AddStatusEffectUpgradeStacks("armor", 2); upgrade.SetExcludeFromClones(true);
                card.ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
                var temporary = new CardUpgradeState(); temporary.Setup(); temporary.SetAttackDamage(2); temporary.SetAdditionalHP(3);
                temporary.AddStatusEffectUpgradeStacks("regen", 3); temporary.SetExcludeFromClones(true); card.ApplyTemporaryUpgrade(temporary);
            }
            CardData gear = all.FindCardData(gearCard.GetCardDataID())!;
            var attach = new CardEffectData("CardEffectAttachEquipment", null!, Team.Type.Monsters); attach.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            Set(attach, "paramCardUpgradeData", DynamicUpgradeScenario.Upgrade("PojuCopyGear", "c2f6ed7f-18ce-4070-b65f-7dd9f5220003", 1, 3, 0, 0, "armor", 2));
            gear.GetEffects().Clear(); gear.GetEffects().Add(attach); gear.GetTraits().Clear(); Set(gear, "overrideDescriptionKey", "");
            Set(gear, "cardType", CardType.Equipment); Set(gear, "cost", 0); Set(gear, "targetless", false); Set(gear, "targetsRoom", true);
            foreach (CardState card in owned.Where(card => card.GetCardDataID() == gear.GetID())) card.Setup(gear, save);
            Configure(managers, 1, false);
            log.LogInfo("UNIT-COPY-PREPARED real paid copying, multi-target/failed births, queued statuses/abilities, gear and Horde; original Boss/waves intact.");

            CardData Ability(string name, string id, int cooldown, int spawn)
            {
                CardData data = UnityEngine.Object.Instantiate(all.FindCardData(source.GetCardDataID())!); data.name = name; Set(data, "id", id);
                Set(data, "isUnitAbility", true); Set(data, "cooldownAfterActivated", cooldown); Set(data, "cooldownAtSpawn", spawn);
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data); return data;
            }
        }
        internal static void Start(AllGameManagers managers, ManualLogSource log)
        { Started = true; managers.GetSaveManager().StartCoroutine(Protect(Run(managers), log)); }
        private static IEnumerator Protect(IEnumerator routine, ManualLogSource log)
        {
            while (true)
            {
                bool next;
                try { next = routine.MoveNext(); }
                catch (Exception error) { Error = error.ToString(); log.LogError(Error); break; }
                if (!next) break; yield return routine.Current;
            }
            (routine as IDisposable)?.Dispose(); current = null; label = null; suppressBackground = false; Completed = Error == null;
        }
        private static IEnumerator Run(AllGameManagers managers)
        {
            CardManager cards = managers.GetCardManager()!; RoomManager rooms = managers.GetRoomManager()!;
            MonsterManager monsters = managers.GetMonsterManager()!; CombatManager combat = managers.GetCombatManager()!;
            FullBattleTrace trace = FullBattleTrace.Active!; var hosts = new List<CharacterState>();
            for (int floor = 0; floor < 2; floor++)
            {
                CardState card = cards.GetAllCards(new List<CardState>()).First(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678" &&
                    !hosts.Any(host => host.GetSpawnerCard() == card));
                if (!cards.GetHand().Contains(card) && !cards.DrawSpecificCard(card)) throw new InvalidOperationException("Copy host redraw failed.");
                while (!Ready()) yield return null; yield return rooms.GetRoomUI().SetSelectedRoom(floor);
                int hand = cards.GetHand().IndexOf(card); SpawnPoint point = rooms.GetRoom(floor).GetMonsterPoint(0);
                if (!cards.CanPlayHandCard(card, floor, point, null, null, out var error) || !cards.PlayCard(hand, point, ref error))
                    throw new InvalidOperationException("Paid copy host failed: " + error);
                while (!Ready()) yield return null;
                var born = new List<CharacterState>(); rooms.GetRoom(floor).AddCharactersToList(born, Team.Type.Monsters);
                hosts.Add(born.Single(unit => unit.GetSpawnerCard() == card));
            }
            CharacterState third = null!;
            yield return monsters.CloneMonsterState(hosts[0], 2, unit => third = unit, managers.GetCoreManagers(), isCardless: true);
            if (third == null) throw new InvalidOperationException("Native third-floor host clone failed.");
            hosts.Add(third); while (!Ready()) yield return null;
            bool previous = combat.SuppressCombatPreviewUpdates; combat.SuppressCombatPreviewUpdates = true;
            try
            {
                yield return Cast("zero-targeted", hosts[0], 0);
                yield return Cast("negative-room", hosts[0], -2, room: true);
                yield return Cast("single-targeted", hosts[0], 1);
                yield return Cast("multiple-targeted", hosts[1], 2);
                var first = new List<CharacterState>(); rooms.GetRoom(0).AddCharactersToList(first, Team.Type.Monsters);
                yield return Cast("clone-of-clone-ignores-range", first.First(unit => unit != hosts[0]), 1, ranged: true);
                yield return Cast("status-before-copy", hosts[1], 1, status: true);
                yield return Cast("ability-before-copy", hosts[1], 1, ability: true);
                hosts[2].SetHealth(41, hosts[2].GetMaxHP()); hosts[2].BuffDamage(9);
                yield return hosts[2].AddEquipment(gearCard, managers.GetCoreManagers(), grafted: false);
                cards.MoveToStandByPile(gearCard, wasPlayed: true, wasExhausted: false,
                    new RemoveFromStandByCondition(() => cards.CheckEquipmentRemoveFromStandByCondition(hosts[2], gearCard)));
                yield return combat.RunTriggerQueue();
                yield return Cast("equipped-source", hosts[2], 1);
                CharacterData filler = UnityEngine.Object.Instantiate(unitData); filler.name = "PojuCopyFiller";
                Set(filler, "id", "c2f6ed7f-18ce-4070-b65f-7dd9f5220004"); Set(filler, "attackDamage", 0); Set(filler, "health", 1);
                Set(filler, "triggers", new List<CharacterTriggerData>()); Set(filler, "unitAbility", null!);
                CharacterState cardless = null!; yield return monsters.CreateMonsterState(filler, null!, 2, unit => cardless = unit, isCardless: true);
                yield return Cast("cardless-source", cardless, 1);
                yield return Cast("room-multiple-targets", hosts[0], 1, room: true);
                while (rooms.GetRoom(2).GetRemainingSpawnPointCount(Team.Type.Monsters) > 0)
                    yield return monsters.CloneMonsterState(hosts[2], 2, null!, managers.GetCoreManagers(), SpawnMode.BackSlot, isCardless: true);
                yield return combat.RunTriggerQueue();
                yield return Cast("full-room-two-card-allocations", hosts[2], 2);
                var full = new List<CharacterState>(); rooms.GetRoom(2).AddCharactersToList(full, Team.Type.Monsters);
                CharacterState last = full.Single(unit => unit.GetSpawnPoint().GetIndexInRoom() == combat.NumSpawnPointsPerFloor(Team.Type.Monsters) - 1);
                yield return Cast("selected-last-no-allocation", last, 1);
                while (rooms.GetRoom(1).GetRemainingSpawnPointCount(Team.Type.Monsters) > 1)
                    yield return monsters.CreateMonsterState(filler, null!, 1, null!, isCardless: true);
                yield return combat.RunTriggerQueue();
                yield return Cast("partial-room-many-targets", hosts[1], 1, room: true);
                hosts[0].AddStatusEffect("horde", 1); yield return combat.RunTriggerQueue();
                yield return Cast("horde-no-birth", hosts[0], 2);
                Configure(managers, 1, false); Set(combat, "combatStateChanged", true);
            }
            finally { label = null; current = null; suppressBackground = false; combat.SuppressCombatPreviewUpdates = previous; }
            while (!Ready()) yield return null;

            IEnumerator Cast(string name, CharacterState target, int count, bool room = false, bool ranged = false, bool status = false, bool ability = false)
            {
                Configure(managers, count, room, ranged, status, ability);
                if (!cards.GetHand().Contains(source) && !cards.DrawSpecificCard(source)) throw new InvalidOperationException("Copy card redraw failed.");
                while (!Ready()) yield return null;
                int floor = target.GetCurrentRoomIndex(); yield return rooms.GetRoomUI().SetSelectedRoom(floor);
                SpawnPoint point = target.GetSpawnPoint(); label = name; suppressBackground = true;
                if (!cards.CanPlayHandCard(source, floor, point, null, null, out var error)) throw new InvalidOperationException("Copy cast failed " + name + ": " + error);
                trace.BeginCardPlay(new PlayCardAction(trace.CardId(source), floor, targetUnitId: room ? 0 : trace.UnitId(target)), scenarioAction: true);
                if (!cards.PlayCard(cards.GetHand().IndexOf(source), point, ref error)) throw new InvalidOperationException("Copy play failed " + name + ": " + error);
                while (!Ready()) yield return null;
                combat.SuppressCombatPreviewUpdates = false; while (!Ready()) yield return null;
                trace.CompleteCardPlay(); combat.SuppressCombatPreviewUpdates = true; label = null;
            }
            bool Ready() => !managers.GetSaveManager().PreviewMode && !managers.GetReplayManager().IsCardPlaying() &&
                !combat.IsRunningTriggerQueue && Queue().Count == 0 && !(managers.GetHandUI()?.AreAnyCardsAnimating() ?? false) &&
                (combat.SuppressCombatPreviewUpdates || !(bool)AccessTools.Field(typeof(CombatManager), "combatStateChanged").GetValue(combat));
        }
        private static void Configure(AllGameManagers managers, int count, bool room, bool ranged = false, bool status = false, bool ability = false)
        {
            CardData data = managers.GetSaveManager().GetAllGameData().FindCardData(source.GetCardDataID())!;
            var effects = new List<CardEffectData>();
            if (status)
            {
                var add = new CardEffectData("CardEffectAddStatusEffect", null!, Team.Type.Monsters); add.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
                Set(add, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = "armor", count = 1 } }); effects.Add(add);
            }
            if (ability)
            {
                var add = new CardEffectData("CardEffectSetUnitAbility", null!, Team.Type.Monsters); add.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
                Set(add, "paramCardData", changedAbility); effects.Add(add);
            }
            var copy = new CardEffectData("CardEffectCopyUnits", null!, Team.Type.Monsters); copy.Cheat_SetTargetMode(room ? TargetMode.Room : TargetMode.DropTargetCharacter);
            Set(copy, "paramInt", count); Set(copy, "useIntRange", ranged); Set(copy, "paramMinInt", -20); Set(copy, "paramMaxInt", -10); effects.Add(copy);
            data.GetEffects().Clear(); data.GetEffects().AddRange(effects); data.GetTraits().Clear(); Set(data, "overrideDescriptionKey", "");
            Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default); Set(data, "targetless", room); Set(data, "targetsRoom", true);
            foreach (CardState card in managers.GetCardManager()!.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == data.GetID())) card.Setup(data, managers.GetSaveManager());
            FullBattleTrace.Active?.InvalidateCardRules();
        }
        private static List<UnitCloneCallback> Queue() => ((IEnumerable)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(AllGameManagers.Instance!.GetCombatManager()))
            .Cast<CombatManager.TriggerQueueData>().Select(data => Payload(data.character, data.trigger, data.dyingCharacter, data.fireTriggersData, data.triggerCount)).ToList();
        private static UnitCloneCallback Payload(CharacterState actor, CharacterTriggerData.Trigger kind, CharacterState? dying, CharacterState.FireTriggersData? data, int count)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            return new UnitCloneCallback(trace.UnitId(actor), kind.ToString(), data?.paramInt ?? 0, data?.paramInt2 ?? 0, data?.paramString, count,
                dying == null ? 0 : trace.UnitId(dying), data?.overrideTargetCharacter == null ? 0 : trace.UnitId(data.overrideTargetCharacter),
                data?.overrideLastSpawnedCharacter == null ? 0 : trace.UnitId(data.overrideLastSpawnedCharacter));
        }
        private static void Set(object target, string name, object value) => AccessTools.Field(target.GetType(), name).SetValue(target, value);
        [HarmonyPatch(typeof(CardEffectCopyUnits), nameof(CardEffectCopyUnits.ApplyEffect))]
        private static class CopyPatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            { if (Enabled && label != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode) __result = Observe(__result, cardEffectState, cardEffectParams); }
            private static IEnumerator Observe(IEnumerator routine, CardEffectState effect, CardEffectParams parameters)
            {
                FullBattleTrace trace = FullBattleTrace.Active!;
                var record = new Record { Label = label!, Count = effect.GetParamInt(), RoomIndex = parameters.selectedRoom,
                    SourceCardId = trace.CardId(parameters.playedCard ?? throw new InvalidOperationException("Copy observation requires a paid card.")), Targets = parameters.targets.Select(trace.UnitId).ToArray(),
                    Before = trace.CaptureTrain(), Catalog = trace.CaptureDecision().PlayRules!.UnitCopyCatalog!, BeforeQueue = Queue() };
                Records.Add(record); current = record;
                try { while (routine.MoveNext()) yield return routine.Current; }
                finally { (routine as IDisposable)?.Dispose(); record.After = trace.CaptureTrain(); record.AfterQueue = Queue(); current = null; suppressBackground = false; }
            }
        }
        [HarmonyPatch(typeof(CardManager), nameof(CardManager.CopyCardState))]
        private static class CardIdentityPatch
        {
            private static void Postfix(CardState __result)
            { if (Enabled && !AllGameManagers.Instance!.GetSaveManager().PreviewMode) FullBattleTrace.Active!.CardId(__result); }
        }
        [HarmonyPatch(typeof(MonsterManager), nameof(MonsterManager.CreateMonsterState))]
        private static class BirthPatch
        {
            private static void Postfix(ref IEnumerator __result) { if (current != null) __result = Observe(__result); }
            private static IEnumerator Observe(IEnumerator routine)
            {
                bool previous = intrinsic; intrinsic = true;
                try { while (routine.MoveNext()) yield return routine.Current; }
                finally { (routine as IDisposable)?.Dispose(); intrinsic = previous; }
            }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.ApplyCardUpgrade))]
        private static class UpgradePatch
        {
            private static void Postfix(ref IEnumerator __result) { if (current != null) __result = Observe(__result); }
            private static IEnumerator Observe(IEnumerator routine)
            {
                bool previous = intrinsic; intrinsic = true;
                try { while (routine.MoveNext()) yield return routine.Current; }
                finally { (routine as IDisposable)?.Dispose(); intrinsic = previous; }
            }
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.RunTriggerQueue))]
        private static class QuietPatch
        {
            private static bool Prefix(CombatManager __instance, ref IEnumerator __result)
            { if (!suppressBackground || intrinsic || __instance.IsRunningTriggerQueue) return true; if (current != null) current.AutomaticQueueDeferrals++; __result = Empty(); return false; }
            private static IEnumerator Empty() { yield break; }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class FirePatch
        {
            private static void Prefix(CharacterState __instance, CharacterTriggerData.Trigger trigger, CharacterState dyingCharacter,
                CharacterState.FireTriggersData fireTriggersData, int triggerCount, bool fromRunningTriggerQueue)
            { if (current != null && fromRunningTriggerQueue) current.Dispatched.Add(Payload(__instance, trigger, dyingCharacter, fireTriggersData, triggerCount)); }
        }
    }
}
