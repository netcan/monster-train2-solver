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
    internal static class HordeMergeScenario
    {
        internal sealed class Record
        {
            public string Label { get; set; } = "";
            public string Operation { get; set; } = "";
            public bool FromBump { get; set; }
            public int SourceId { get; set; }
            public int TargetId { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? AfterApi { get; set; }
            public RoomCombatState? After { get; set; }
            public CombatUnit? SourceAfter { get; set; }
            public CombatUnit? SourceAfterDrain { get; set; }
            public RoomCombatState? PrimaryBeforePreview { get; set; }
            public RoomCombatState? PrimaryAfterPreview { get; set; }
            public bool SourceDespawned { get; set; }
            public bool SourceDestroyed { get; set; }
            public int AutomaticQueueDeferrals { get; set; }
            public int QueueAfterApi { get; set; }
            public int QueueAfter { get; set; }
            public List<HordeStatusScenario.Callback> Queued { get; set; } = new List<HordeStatusScenario.Callback>();
            public List<HordeStatusScenario.Callback> Dispatched { get; set; } = new List<HordeStatusScenario.Callback>();
        }
        internal sealed class Selection
        {
            public int SourceId { get; set; }
            public int RoomIndex { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public int[] Candidates { get; set; } = Array.Empty<int>();
            public bool Accepted { get; set; }
            public int TargetId { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        internal static readonly List<Selection> Selections = new List<Selection>();
        internal static bool Started, Completed;
        internal static string? Error;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "horde-merge";
        private static Record? current, draining;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            HordeStatusScenario.Prepare(managers, log);
            SaveManager save = managers.GetSaveManager();
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            CardData steward = save.GetAllGameData().FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!;
            CharacterData unit = steward.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            Set(unit, "equipmentLimit", 2);
            Set(unit, "triggers", unit.GetTriggers().Concat(new[] {
                Gold(CharacterTriggerData.Trigger.OnSpawn, 3), Gold(CharacterTriggerData.Trigger.OnUnscaledSpawn, 5, true),
                Gold(CharacterTriggerData.Trigger.OnSpawnNotFromCard, 7), Gold(CharacterTriggerData.Trigger.CardMonsterPlayed, 11),
                Gold(CharacterTriggerData.Trigger.OnDeath, 13), Gold(CharacterTriggerData.Trigger.OnStatusEffectChanged, 17) }).ToList());
            var normal = owned.Where(card => card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDamage" &&
                effect.GetTargetMode() == TargetMode.DropTargetCharacter)).ToArray();
            var returning = owned.Where(card => card.GetCardDataID() == "19705414-816d-4549-bcf5-866f338a551f").ToArray();
            if (normal.Length == 0 || returning.Length == 0) throw new InvalidOperationException("Horde merge needs normal and returning equipment.");
            Gear(normal, false); Gear(returning, true);
            foreach (CardState card in owned.Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            log.LogInfo("HORDE-MERGE-PREPARED real paid hosts, mixed equipment, troop/respawn/death callbacks and unchanged Boss/waves.");

            void Gear(CardState[] cards, bool returns)
            {
                CardData data = save.GetAllGameData().FindCardData(cards[0].GetCardDataID())!;
                var effect = new CardEffectData("CardEffectAttachEquipment", null!, Team.Type.Monsters);
                effect.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
                Set(effect, "paramCardUpgradeData", DynamicUpgradeScenario.Upgrade("PojuHordeMergeGear", "c2f6ed7f-18ce-4070-b65f-7dd9f5190093", 1, 2, 0, 0, "armor", 1));
                data.GetEffects().Clear(); data.GetEffects().Add(effect); data.GetTraits().Clear();
                if (returns) { var trait = new CardTraitData(); trait.Setup("CardTraitReturnToHandEquipment"); data.GetTraits().Add(trait); }
                Set(data, "cardType", CardType.Equipment); Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default);
                Set(data, "targetless", false); Set(data, "targetsRoom", true);
                foreach (CardState card in cards) card.Setup(data, save);
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
            RoomManager rooms = managers.GetRoomManager()!; CardManager cards = managers.GetCardManager()!;
            CombatManager combat = managers.GetCombatManager()!; FullBattleTrace trace = FullBattleTrace.Active!;
            RoomState room = rooms.GetRoom(0);
            yield return rooms.GetRoomUI().SetSelectedRoom(0);
            for (int slot = 0; slot < 2; slot++)
            {
                int hand = cards.GetHand().FindIndex(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678");
                if (hand < 0) throw new InvalidOperationException("Horde merge requires two initial Stewards.");
                CardState card = cards.GetHand()[hand]; var point = room.GetMonsterPoint(slot);
                if (!cards.CanPlayHandCard(card, 0, point, null, null, out var error) || !cards.PlayCard(hand, point, ref error))
                    throw new InvalidOperationException("Horde merge paid summon failed: " + error);
                while (!Ready()) yield return null;
            }
            var players = new List<CharacterState>(); room.AddCharactersToList(players, Team.Type.Monsters);
            CharacterState[] paidHosts = players.Where(unit => unit.GetSpawnerCard()?.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            if (paidHosts.Length != 2) throw new InvalidOperationException("Horde merge did not create both paid hosts.");
            CharacterState incoming = paidHosts[0], recipient = paidHosts[1];
            foreach (bool returning in new[] { false, true })
            {
                int hand = cards.GetHand().FindIndex(card => card.GetCardType() == CardType.Equipment &&
                    card.HasTrait<CardTraitReturnToHandEquipment>() == returning);
                if (hand < 0)
                {
                    CardState equipment = cards.GetAllCards(new List<CardState>()).First(card => card.GetCardType() == CardType.Equipment &&
                        card.HasTrait<CardTraitReturnToHandEquipment>() == returning);
                    yield return incoming.AddEquipment(equipment, managers.GetCoreManagers(), grafted: false);
                    cards.MoveToStandByPile(equipment, wasPlayed: true, wasExhausted: false,
                        new RemoveFromStandByCondition(() => cards.CheckEquipmentRemoveFromStandByCondition(incoming, equipment)));
                    continue;
                }
                CardState card = cards.GetHand()[hand]; SpawnPoint point = incoming.GetSpawnPoint();
                if (!cards.CanPlayHandCard(card, 0, point, null, null, out var error) || !cards.PlayCard(hand, point, ref error))
                    throw new InvalidOperationException("Horde merge paid equipment failed: " + error);
                while (!Ready()) yield return null;
            }
            bool previous = combat.SuppressCombatPreviewUpdates; combat.SuppressCombatPreviewUpdates = true;
            try
            {
                SaveManager save = managers.GetSaveManager();
                var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
                var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
                CharacterData prototype = waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups")
                    .GetValue(wave)).Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(),
                    save, save.GetGeneratedBoss())).First(unit => !unit.IsMiniboss());
                CharacterState[] added = new CharacterState[5];
                for (int i = 0; i < added.Length; i++)
                {
                    CharacterData definition = UnityEngine.Object.Instantiate(prototype);
                    definition.name = "PojuHordeMergeEnemy" + i;
                    AccessTools.Field(typeof(GameData), "id").SetValue(definition, "c2f6ed7f-18ce-4070-b65f-7dd9f51900" + (90 + i + (i >= 3 ? 1 : 0)));
                    Set(definition, "attackDamage", 2 + i); Set(definition, "health", 6 + i);
                    Set(definition, "startingStatusEffects", i != 2 ? new[] { new StatusEffectStackData { statusId = "horde", count = i == 1 ? 2 : 1 } } : Array.Empty<StatusEffectStackData>());
                    Set(definition, "triggers", new List<CharacterTriggerData> {
                        Gold(CharacterTriggerData.Trigger.OnSpawn, 3, true), Gold(CharacterTriggerData.Trigger.OnTroopAdded, 5),
                        Gold(CharacterTriggerData.Trigger.CardMonsterPlayed, 7), Gold(CharacterTriggerData.Trigger.OnStatusEffectChanged, 11),
                        Gold(CharacterTriggerData.Trigger.OnDeath, 13) });
                    yield return managers.GetHeroManager()!.SpawnHeroInRoom(definition, 0, null!);
                    var now = new List<CharacterState>(); room.AddCharactersToList(now, Team.Type.Heroes);
                    added[i] = now.Single(unit => unit.GetSourceCharacterData() == definition);
                }
                yield return combat.RunTriggerQueue();
                yield return Preview("preview-player-clone", "Clone", recipient, null);
                yield return Preview("preview-player-bump-merge-with-equipment", "Merge", incoming, recipient, true);
                yield return Preview("preview-enemy-ordinary-merge", "Merge", added[0], added[1]);
                yield return Preview("preview-enemy-self-merge", "Merge", added[1], added[1]);
                Select(incoming); Select(added[0]); Select(added[2]);
                yield return Apply("player-clone-adds-one-without-birth", "Clone", recipient, null);
                // An opposing team's gap tests the room-wide centering performed during removal.
                added[4].SetSpawnPoint(room.GetHeroPoint(6), animate: false, setPosition: false);
                yield return Apply("player-bump-merge-mixed-equipment", "Merge", incoming, recipient, true);
                yield return Apply("enemy-clone-adds-one-without-birth", "Clone", added[1], null);
                yield return Apply("enemy-ordinary-merge-different-definition", "Merge", added[0], added[1]);
                yield return Apply("null-source-merge", "Merge", null, recipient);
                yield return Apply("null-target-merge", "Merge", recipient, null);
                yield return Apply("non-horde-source-merge", "Merge", added[2], recipient);
                yield return Apply("non-horde-target-merge", "Merge", recipient, added[2]);
                Select(recipient); Select(added[1]);
                yield return Apply("enemy-self-merge-removes-positive-hp-recipient", "Merge", added[1], added[1]);
                yield return Apply("cross-team-direct-merge", "Merge", added[3], recipient);
                recipient.AddStatusEffect("immune", 1);
                yield return combat.RunTriggerQueue();
                yield return Apply("immune-target-still-removes-source", "Merge", added[4], recipient);
                yield return Apply("immune-clone-no-growth", "Clone", recipient, null);
                recipient.RemoveStatusEffect("immune", 1);
                yield return combat.RunTriggerQueue();
                yield return Apply("null-source-clone", "Clone", null, null);
            }
            finally { combat.SuppressCombatPreviewUpdates = previous; current = draining = null; }
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(combat, true);

            void Select(CharacterState source)
            {
                var candidates = new List<CharacterState>();
                if (source.GetTeamType() == Team.Type.Monsters) managers.GetMonsterManager()!.AddCharactersInRoomToList(candidates, 0);
                else managers.GetHeroManager()!.AddCharactersInRoomToList(candidates, 0);
                bool accepted = StatusEffectHordeState.CanMergeCharactersInRoom(source, room, managers.GetCoreManagers(), out var target);
                Selections.Add(new Selection { SourceId = trace.UnitId(source), RoomIndex = 0, Before = trace.Capture(room),
                    Candidates = candidates.Select(trace.UnitId).ToArray(), Accepted = accepted, TargetId = target == null ? 0 : trace.UnitId(target) });
            }
            IEnumerator Preview(string label, string operation, CharacterState source, CharacterState? target, bool bump = false)
            {
                var actors = new List<CharacterState>(); room.AddCharactersToList(actors, Team.Type.Heroes | Team.Type.Monsters);
                RoomCombatState primary = trace.Capture(room);
                foreach (CharacterState actor in actors) actor.TemporaryPreviewsEnabled = true;
                managers.GetSaveManager().PreviewMode = true;
                Record? record = null;
                try
                {
                    yield return Apply(label, operation, source, target, bump);
                    record = Records.Last(); record.PrimaryBeforePreview = primary;
                }
                finally
                {
                    managers.GetSaveManager().PreviewMode = false;
                    foreach (CharacterState actor in actors) actor.TemporaryPreviewsEnabled = false;
                }
                if (record != null) record.PrimaryAfterPreview = trace.Capture(room);
            }
            IEnumerator Apply(string label, string operation, CharacterState? source, CharacterState? target, bool bump = false)
            {
                var record = new Record { Label = label, Operation = operation, FromBump = bump,
                    SourceId = source == null ? 0 : trace.UnitId(source), TargetId = target == null ? 0 : trace.UnitId(target), Before = trace.Capture(room) };
                Records.Add(record); current = record;
                if (operation == "Clone")
                    yield return managers.GetMonsterManager()!.CloneMonsterState(source!, 0, null!, managers.GetCoreManagers());
                else yield return StatusEffectHordeState.MergeCharactersInRoom(source!, target!, bump ? new CardEffectBump() : null, managers.GetCoreManagers());
                record.AfterApi = trace.Capture(room); record.QueueAfterApi = Count();
                if (source != null)
                    using (new CharacterState.SetAllowDestroyedAccessHelper(source, onlyIfDestroyed: true))
                    {
                        record.SourceAfter = trace.CaptureUnit(source); record.SourceDestroyed = source.IsDestroyed;
                        record.SourceDespawned = (bool)AccessTools.Property(typeof(CharacterState), "FiredDespawnEvent").GetValue(source);
                    }
                foreach (CombatManager.TriggerQueueData data in (IEnumerable)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat))
                    record.Queued.Add(Payload(data.character, data.trigger, data.dyingCharacter, data.fireTriggersData, data.triggerCount));
                current = null; draining = record;
                yield return combat.RunTriggerQueue(); while (combat.IsRunningTriggerQueue) yield return null;
                record.QueueAfter = Count(); record.After = trace.Capture(room); draining = null;
                if (source != null)
                    using (new CharacterState.SetAllowDestroyedAccessHelper(source, onlyIfDestroyed: true))
                        record.SourceAfterDrain = trace.CaptureUnit(source);
            }
            int Count() => ((ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat)).Count;
            bool Ready() => !managers.GetSaveManager().PreviewMode && !managers.GetReplayManager().IsCardPlaying() &&
                !combat.IsRunningTriggerQueue && Count() == 0 && !(managers.GetHandUI()?.AreAnyCardsAnimating() ?? false) &&
                !(bool)AccessTools.Field(typeof(CombatManager), "combatStateChanged").GetValue(combat);
        }
        private static HordeStatusScenario.Callback Payload(CharacterState actor, CharacterTriggerData.Trigger trigger,
            CharacterState? dying, CharacterState.FireTriggersData? data, int count)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            return new HordeStatusScenario.Callback { ActorId = trace.UnitId(actor), Kind = trigger.ToString(),
                ParamInt = data?.paramInt ?? 0, ParamInt2 = data?.paramInt2 ?? 0, ParamString = data?.paramString,
                TriggerCount = count, DyingId = dying == null ? 0 : trace.UnitId(dying),
                LastSpawnedOverrideUnitId = data?.overrideLastSpawnedCharacter == null ? 0 : trace.UnitId(data.overrideLastSpawnedCharacter) };
        }
        private static CharacterTriggerData Gold(CharacterTriggerData.Trigger kind, int amount, bool once = false)
        { var trigger = HealingScenario.HealGold(amount, once, true); Set(trigger, "trigger", kind); return trigger; }
        private static void Set(object target, string name, object value) => AccessTools.Field(target.GetType(), name).SetValue(target, value);
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.RunTriggerQueue))]
        private static class QuietPatch
        {
            private static bool Prefix(CombatManager __instance, ref IEnumerator __result)
            {
                if (current == null || __instance.IsRunningTriggerQueue) return true;
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
