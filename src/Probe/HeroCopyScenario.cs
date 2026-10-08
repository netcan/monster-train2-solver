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
    internal static class HeroCopyScenario
    {
        internal sealed class Record
        {
            public string Label { get; set; } = "";
            public int Count { get; set; }
            public int RoomIndex { get; set; }
            public bool HeroBirth { get; set; }
            public bool CopyStats { get; set; }
            public int SourceCardId { get; set; }
            public int[] Targets { get; set; } = Array.Empty<int>();
            public UnitCopyCatalog Catalog { get; set; } = null!;
            public EnemyDefinition[] HeroDefinitions { get; set; } = Array.Empty<EnemyDefinition>();
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
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "hero-copy";
        private static CardState source = null!, gearCard = null!;
        private static CharacterData unitData = null!;
        private static CardData changedAbility = null!;
        private static string? label;
        private static Record? current;
        private static bool intrinsic, suppressBackground;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData();
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            source = owned.First(card => card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDamage" && effect.GetTargetMode() == TargetMode.DropTargetCharacter));
            gearCard = owned.First(card => card.GetCardDataID() != source.GetCardDataID() && card.GetEffects().All(effect => effect.GetEffectStateName() != "CardEffectSpawnMonster"));
            CardData steward = all.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!;
            unitData = steward.GetSpawnCharacterData()!;
            Set(unitData, "health", 90); Set(unitData, "attackDamage", 8); Set(unitData, "size", 1); Set(unitData, "equipmentLimit", 2);
            var callbacks = new List<CharacterTriggerData>();
            foreach (var item in new[] { (CharacterTriggerData.Trigger.OnSpawn, 3), (CharacterTriggerData.Trigger.OnUnscaledSpawn, 5),
                (CharacterTriggerData.Trigger.OnSpawnNotFromCard, 7), (CharacterTriggerData.Trigger.OnStatusEffectChanged, 11),
                (CharacterTriggerData.Trigger.OnUnitAbilityAvailable, 13), (CharacterTriggerData.Trigger.OnUnitAbilityUnavailable, 17),
                (CharacterTriggerData.Trigger.AfterSpawnEnchant, 23), (CharacterTriggerData.Trigger.CardMonsterPlayed, 19) })
            { var trigger = HealingScenario.HealGold(item.Item2, false, true); Set(trigger, "trigger", item.Item1); callbacks.Add(trigger); }
            Set(unitData, "triggers", callbacks);
            Set(unitData, "unitAbility", Ability("PojuHeroCopyNaturalAbility", "c2f6ed7f-18ce-4070-b65f-7dd9f5221001", 3, 2));
            changedAbility = Ability("PojuHeroCopyReplacementAbility", "c2f6ed7f-18ce-4070-b65f-7dd9f5221002", 7, 4);
            foreach (CardState card in owned.Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            CardData gear = all.FindCardData(gearCard.GetCardDataID())!;
            var attach = new CardEffectData("CardEffectAttachEquipment", null!, Team.Type.Heroes); attach.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            Set(attach, "paramCardUpgradeData", DynamicUpgradeScenario.Upgrade("PojuHeroCopyGear", "c2f6ed7f-18ce-4070-b65f-7dd9f5221003", 1, 3, 0, 0, "armor", 2));
            gear.GetEffects().Clear(); gear.GetEffects().Add(attach); gear.GetTraits().Clear(); Set(gear, "overrideDescriptionKey", "");
            Set(gear, "cardType", CardType.Equipment); Set(gear, "cost", 0); Set(gear, "targetless", false); Set(gear, "targetsRoom", true);
            foreach (CardState card in owned.Where(card => card.GetCardDataID() == gear.GetID())) card.Setup(gear, save);
            Configure(managers, 1, false, false);
            log.LogInfo("HERO-COPY-PREPARED real paid hero copying, stats toggle, incoming queues, gear, natural/replaced skills, allocation and mixed-mask branches; original Boss/waves intact.");

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
            var stack = new Stack<IEnumerator>(); stack.Push(routine);
            while (stack.Count > 0)
            {
                bool next; object? yielded;
                IEnumerator active = stack.Peek();
                try { next = active.MoveNext(); yielded = next ? active.Current : null; }
                catch (Exception error) { Error = error.ToString(); log.LogError(Error); break; }
                if (!next) { stack.Pop(); (active as IDisposable)?.Dispose(); continue; }
                if (yielded is IEnumerator child) { stack.Push(child); continue; }
                yield return yielded;
            }
            while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
            current = null; label = null; suppressBackground = false; Completed = Error == null;
        }
        private static IEnumerator Run(AllGameManagers managers)
        {
            CardManager cards = managers.GetCardManager()!; RoomManager rooms = managers.GetRoomManager()!;
            HeroManager heroes = managers.GetHeroManager()!; CombatManager combat = managers.GetCombatManager()!;
            FullBattleTrace trace = FullBattleTrace.Active!; var players = new List<CharacterState>(); var hosts = new List<CharacterState>();
            for (int floor = 0; floor < 2; floor++)
            {
                CardState card = cards.GetAllCards(new List<CardState>()).First(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678" && !players.Any(host => host.GetSpawnerCard() == card));
                if (!cards.GetHand().Contains(card) && !cards.DrawSpecificCard(card)) throw new InvalidOperationException("Hero-copy player host redraw failed.");
                while (!Ready()) yield return null; yield return rooms.GetRoomUI().SetSelectedRoom(floor);
                SpawnPoint point = rooms.GetRoom(floor).GetMonsterPoint(0);
                if (!cards.CanPlayHandCard(card, floor, point, null, null, out var error) || !cards.PlayCard(cards.GetHand().IndexOf(card), point, ref error)) throw new InvalidOperationException("Hero-copy player host failed: " + error);
                while (!Ready()) yield return null;
                players.Add(Actors(floor, Team.Type.Monsters).Single(unit => unit.GetSpawnerCard() == card));
            }
            for (int floor = 0; floor < 3; floor++)
            {
                CharacterState[] previousActors = Actors(floor, Team.Type.Heroes);
                yield return heroes.SpawnHeroInRoom(unitData, floor, null!); while (!Ready()) yield return null;
                hosts.Add(Actors(floor, Team.Type.Heroes).Single(unit => !previousActors.Contains(unit)));
            }
            bool previous = combat.SuppressCombatPreviewUpdates; combat.SuppressCombatPreviewUpdates = true;
            try
            {
                yield return Cast("zero-targeted", hosts[0], 0, false);
                yield return Cast("negative-room", hosts[0], -2, false, room: true);
                foreach (CharacterState host in hosts) { host.SetHealth(41, host.GetMaxHP()); host.BuffDamage(9); host.AddStatusEffect("armor", 3); }
                yield return combat.RunTriggerQueue();
                yield return Cast("raw-source-no-stats", hosts[0], 1, false);
                yield return Cast("copy-live-stats", hosts[0], 1, true);
                yield return Cast("multiple-targeted", hosts[1], 2, true);
                CharacterState copied = Actors(0, Team.Type.Heroes).First(unit => unit != hosts[0]);
                yield return Cast("clone-of-copy-ignores-range", copied, 1, true, ranged: true);
                yield return Cast("status-before-copy", hosts[1], 1, true, status: true);
                yield return Cast("ability-before-copy", hosts[1], 1, true, ability: true);
                CharacterState equippedHost = hosts[2];
                yield return equippedHost.AddEquipment(gearCard, managers.GetCoreManagers(), grafted: false);
                cards.MoveToStandByPile(gearCard, wasPlayed: true, wasExhausted: false, new RemoveFromStandByCondition(() => cards.CheckEquipmentRemoveFromStandByCondition(equippedHost, gearCard)));
                yield return combat.RunTriggerQueue();
                yield return Cast("equipped-no-stats", hosts[2], 1, false);
                yield return Cast("equipped-live-stats", hosts[2], 1, true);
                yield return Cast("room-multiple-targets", hosts[0], 1, false, room: true);
                int last = combat.NumSpawnPointsPerFloor(Team.Type.Heroes) - 1;
                hosts[2].SetSpawnPoint(rooms.GetRoom(2).GetHeroPoint(last));
                hosts[2].AddStatusEffect("horde", 3); yield return combat.RunTriggerQueue();
                yield return Cast("horde-selected-last", hosts[2], 1, true);
                CharacterData filler = UnityEngine.Object.Instantiate(unitData); filler.name = "PojuHeroCopyFiller";
                Set(filler, "id", "c2f6ed7f-18ce-4070-b65f-7dd9f5221004"); Set(filler, "attackDamage", 0); Set(filler, "health", 1);
                Set(filler, "triggers", new List<CharacterTriggerData>()); Set(filler, "unitAbility", null!);
                while (rooms.GetRoom(1).GetRemainingSpawnPointCount(Team.Type.Heroes) > 0) yield return heroes.SpawnHeroInRoom(filler, 1, null!);
                yield return combat.RunTriggerQueue();
                yield return Cast("full-room-no-allocations", hosts[1], 2, true);
                while (rooms.GetRoom(2).GetRemainingSpawnPointCount(Team.Type.Heroes) > 1) yield return heroes.SpawnHeroInRoom(filler, 2, null!);
                yield return combat.RunTriggerQueue();
                yield return Cast("partial-room-many-targets", hosts[2], 1, false, room: true);
                yield return Cast("mixed-mask-enemy-to-player", hosts[1], 1, false, mixed: true);
                yield return Cast("mixed-mask-player", players[0], 1, true, mixed: true);
                Configure(managers, 1, false, true); Set(combat, "combatStateChanged", true);
            }
            finally { label = null; current = null; suppressBackground = false; combat.SuppressCombatPreviewUpdates = previous; }
            while (!Ready()) yield return null;

            CharacterState[] Actors(int floor, Team.Type team)
            { var actors = new List<CharacterState>(); rooms.GetRoom(floor).AddCharactersToList(actors, team); return actors.ToArray(); }
            IEnumerator Cast(string name, CharacterState target, int count, bool copyStats, bool room = false, bool ranged = false, bool status = false, bool ability = false, bool mixed = false)
            {
                Configure(managers, count, room, copyStats, ranged, status, ability, mixed);
                if (!cards.GetHand().Contains(source) && !cards.DrawSpecificCard(source)) throw new InvalidOperationException("Hero copy card redraw failed.");
                while (!Ready()) yield return null;
                int floor = target.GetCurrentRoomIndex(); yield return rooms.GetRoomUI().SetSelectedRoom(floor);
                SpawnPoint point = target.GetSpawnPoint(); label = name; suppressBackground = true;
                if (!cards.CanPlayHandCard(source, floor, point, null, null, out var error)) throw new InvalidOperationException("Hero copy cast failed " + name + ": " + error);
                trace.BeginCardPlay(new PlayCardAction(trace.CardId(source), floor, targetUnitId: room ? 0 : trace.UnitId(target)), scenarioAction: true);
                if (!cards.PlayCard(cards.GetHand().IndexOf(source), point, ref error)) throw new InvalidOperationException("Hero copy play failed " + name + ": " + error);
                while (!Ready()) yield return null;
                combat.SuppressCombatPreviewUpdates = false; while (!Ready()) yield return null;
                trace.CompleteCardPlay(); combat.SuppressCombatPreviewUpdates = true; label = null;
            }
            bool Ready()
            {
                if (trace.CaptureFailures > 0) throw new InvalidOperationException("Hero-copy capture failed while waiting for " + (label ?? "setup") + "; inspect the preceding capture error.");
                return !managers.GetSaveManager().PreviewMode && !managers.GetReplayManager().IsCardPlaying() && !combat.IsRunningTriggerQueue && Queue().Count == 0 && !(managers.GetHandUI()?.AreAnyCardsAnimating() ?? false) &&
                    (combat.SuppressCombatPreviewUpdates || !(bool)AccessTools.Field(typeof(CombatManager), "combatStateChanged").GetValue(combat));
            }
        }
        private static void Configure(AllGameManagers managers, int count, bool room, bool copyStats, bool ranged = false, bool status = false, bool ability = false, bool mixed = false)
        {
            CardData data = managers.GetSaveManager().GetAllGameData().FindCardData(source.GetCardDataID())!;
            Team.Type team = mixed ? Team.Type.Heroes | Team.Type.Monsters : Team.Type.Heroes;
            var effects = new List<CardEffectData>();
            if (status)
            { var add = new CardEffectData("CardEffectAddStatusEffect", null!, team); add.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(add, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = "armor", count = 1 } }); effects.Add(add); }
            if (ability)
            { var add = new CardEffectData("CardEffectSetUnitAbility", null!, team); add.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(add, "paramCardData", changedAbility); effects.Add(add); }
            var copy = new CardEffectData("CardEffectCopyUnits", null!, team); copy.Cheat_SetTargetMode(room ? TargetMode.Room : TargetMode.DropTargetCharacter);
            Set(copy, "paramInt", count); Set(copy, "paramBool", copyStats); Set(copy, "useIntRange", ranged); Set(copy, "paramMinInt", -20); Set(copy, "paramMaxInt", -10); effects.Add(copy);
            data.GetEffects().Clear(); data.GetEffects().AddRange(effects); data.GetTraits().Clear(); Set(data, "overrideDescriptionKey", "");
            Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default); Set(data, "targetless", room); Set(data, "targetsRoom", true);
            foreach (CardState card in managers.GetCardManager()!.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == data.GetID())) card.Setup(data, managers.GetSaveManager());
            FullBattleTrace.Active?.InvalidateCardRules();
        }
        private static List<UnitCloneCallback> Queue() => ((IEnumerable)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(AllGameManagers.Instance!.GetCombatManager())).Cast<CombatManager.TriggerQueueData>()
            .Select(data => Payload(data.character, data.trigger, data.dyingCharacter, data.fireTriggersData, data.triggerCount)).ToList();
        private static UnitCloneCallback Payload(CharacterState actor, CharacterTriggerData.Trigger kind, CharacterState? dying, CharacterState.FireTriggersData? data, int count)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            return new UnitCloneCallback(trace.UnitId(actor), kind.ToString(), data?.paramInt ?? 0, data?.paramInt2 ?? 0, data?.paramString, count,
                dying == null ? 0 : trace.UnitId(dying), data?.overrideTargetCharacter == null ? 0 : trace.UnitId(data.overrideTargetCharacter), data?.overrideLastSpawnedCharacter == null ? 0 : trace.UnitId(data.overrideLastSpawnedCharacter));
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
                UnitCopyCatalog catalog = trace.CaptureDecision().PlayRules!.UnitCopyCatalog!;
                var characters = new List<CharacterState>();
                AllGameManagers managers = AllGameManagers.Instance!;
                for (int index = 0; index < managers.GetRoomManager()!.GetNumRooms(); index++) managers.GetRoomManager()!.GetRoom(index).AddCharactersToList(characters, Team.Type.Heroes | Team.Type.Monsters);
                var assets = new HashSet<string>(catalog.Births.Select(birth => birth.Unit.AssetKey), StringComparer.Ordinal);
                var definitions = managers.GetSaveManager().GetAllGameData().GetAllCharacterData().Where(data => data != null && assets.Contains(data.GetAssetKey())).Concat(characters.Select(unit => unit.GetSourceCharacterData()))
                    .GroupBy(data => data.GetAssetKey()).Select(group => group.Last()).OrderBy(data => data.GetAssetKey(), StringComparer.Ordinal).Select(EnemySpawningProbe.Definition).ToArray();
                var record = new Record { Label = label!, Count = effect.GetParamInt(), RoomIndex = parameters.selectedRoom, HeroBirth = effect.GetTargetTeamType() == Team.Type.Heroes, CopyStats = effect.GetParamBool(),
                    SourceCardId = trace.CardId(parameters.playedCard ?? throw new InvalidOperationException("Hero-copy observation requires a paid card.")), Targets = parameters.targets.Select(trace.UnitId).ToArray(),
                    Before = trace.CaptureTrain(), Catalog = catalog, HeroDefinitions = definitions, BeforeQueue = Queue() };
                Records.Add(record); current = record;
                try { while (routine.MoveNext()) yield return routine.Current; }
                finally { (routine as IDisposable)?.Dispose(); record.After = trace.CaptureTrain(); record.AfterQueue = Queue(); current = null; suppressBackground = false; }
            }
        }
        [HarmonyPatch(typeof(CardManager), nameof(CardManager.CopyCardState))]
        private static class CardIdentityPatch
        { private static void Postfix(CardState __result) { if (Enabled && !AllGameManagers.Instance!.GetSaveManager().PreviewMode) FullBattleTrace.Active!.CardId(__result); } }
        [HarmonyPatch(typeof(HeroManager), nameof(HeroManager.SpawnHeroInRoom))]
        private static class HeroBirthPatch
        { private static void Postfix(ref IEnumerator __result) { if (current != null) __result = Intrinsic(__result); } }
        [HarmonyPatch(typeof(MonsterManager), nameof(MonsterManager.CreateMonsterState))]
        private static class MonsterBirthPatch
        { private static void Postfix(ref IEnumerator __result) { if (current != null) __result = Intrinsic(__result); } }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.ApplyCardUpgrade))]
        private static class UpgradePatch
        { private static void Postfix(ref IEnumerator __result) { if (current != null) __result = Intrinsic(__result); } }
        private static IEnumerator Intrinsic(IEnumerator routine)
        {
            bool previous = intrinsic; intrinsic = true;
            try { while (routine.MoveNext()) yield return routine.Current; }
            finally { (routine as IDisposable)?.Dispose(); intrinsic = previous; }
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
            private static void Prefix(CharacterState __instance, CharacterTriggerData.Trigger trigger, CharacterState dyingCharacter, CharacterState.FireTriggersData fireTriggersData, int triggerCount, bool fromRunningTriggerQueue)
            { if (current != null && fromRunningTriggerQueue) current.Dispatched.Add(Payload(__instance, trigger, dyingCharacter, fireTriggersData, triggerCount)); }
        }
    }
}
