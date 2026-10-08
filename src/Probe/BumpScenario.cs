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
    internal static class BumpScenario
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
            public int OverrideTargetId { get; set; }
            public int LastSpawnedOverrideUnitId { get; set; }
        }
        internal sealed class Record
        {
            public string Label { get; set; } = "";
            public int Amount { get; set; }
            public int SourceCardId { get; set; }
            public int[] Targets { get; set; } = Array.Empty<int>();
            public RoomPlayRule[] Rooms { get; set; } = Array.Empty<RoomPlayRule>();
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? After { get; set; }
            public CombatUnit[]? TargetsAfter { get; set; }
            public int QueueAfter { get; set; }
            public List<Callback> Dispatched { get; set; } = new List<Callback>();
        }
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "bump";
        internal static readonly List<Record> Records = new List<Record>();
        internal static bool Started, Completed;
        internal static string? Error;
        private static string? label;
        private static Record? current;
        private static CardState source = null!;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            source = cards.GetAllCards(new List<CardState>()).First(card => card.GetEffects().Any(effect =>
                effect.GetEffectStateName() == "CardEffectDamage" && effect.GetTargetMode() == TargetMode.DropTargetCharacter));
            CardData steward = save.GetAllGameData().FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!;
            CharacterData unit = steward.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            Set(unit, "health", 80); Set(unit, "attackDamage", 12);
            Set(unit, "triggers", unit.GetTriggers().Concat(Triggers()).ToList());
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            Configure(managers, 1, TargetMode.DropTargetCharacter, Team.Type.Heroes | Team.Type.Monsters, false);
            log.LogInfo("BUMP-PREPARED real paid cards and hosts, room movement/attempt/Shift/Sentry/troop callbacks; original Boss/waves intact.");
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
            (native as IDisposable)?.Dispose(); label = null; Completed = Error == null;
        }
        private static IEnumerator Run(AllGameManagers managers)
        {
            CardManager cards = managers.GetCardManager()!; RoomManager rooms = managers.GetRoomManager()!;
            CombatManager combat = managers.GetCombatManager()!;
            for (int floor = 0; floor < 2; floor++)
            {
                yield return rooms.GetRoomUI().SetSelectedRoom(floor);
                int hand = cards.GetHand().FindIndex(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678");
                if (hand < 0) throw new InvalidOperationException("Bump needs two starting Stewards.");
                SpawnPoint drop = rooms.GetRoom(floor).GetMonsterPoint(0);
                if (!cards.CanPlayHandCard(cards.GetHand()[hand], floor, drop, null, null, out var error) || !cards.PlayCard(hand, drop, ref error))
                    throw new InvalidOperationException("Bump paid host failed: " + error);
                while (!Ready()) yield return null;
            }
            var players = new List<CharacterState>(); rooms.GetRoom(0).AddCharactersToList(players, Team.Type.Monsters);
            CharacterState first = players.Single(); players.Clear(); rooms.GetRoom(1).AddCharactersToList(players, Team.Type.Monsters);
            CharacterState second = players.Single();
            bool previous = combat.SuppressCombatPreviewUpdates; combat.SuppressCombatPreviewUpdates = true;
            try
            {
                var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
                var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(managers.GetSaveManager(), waves);
                CharacterData prototype = waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups")
                    .GetValue(wave)).Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(managers.GetSaveManager().GetCovenantsForSpawnPattern(),
                    managers.GetSaveManager(), managers.GetSaveManager().GetGeneratedBoss())).First(unit => !unit.IsMiniboss());
                CharacterState[] observers = new CharacterState[3];
                for (int i = 0; i < observers.Length; i++)
                {
                    CharacterData data = UnityEngine.Object.Instantiate(prototype); data.name = "PojuBumpObserver" + i;
                    Set(data, "id", "c2f6ed7f-18ce-4070-b65f-7dd9f520000" + i); Set(data, "attackDamage", 1); Set(data, "health", 20);
                    Set(data, "startingStatusEffects", Array.Empty<StatusEffectStackData>()); Set(data, "triggers", Triggers());
                    Set(data, "loopsBetweenTrainFloors", i == 2);
                    yield return managers.GetHeroManager()!.SpawnHeroInRoom(data, i, null!);
                    var now = new List<CharacterState>(); rooms.GetRoom(i).AddCharactersToList(now, Team.Type.Heroes);
                    observers[i] = now.Single(unit => unit.GetSourceCharacterData() == data);
                }
                yield return combat.RunTriggerQueue();
                yield return Cast("player-up", first, 1);
                yield return Cast("player-down", first, -1);
                yield return Cast("player-partial-up-ignores-range", first, 99, ranged: true);
                yield return Cast("player-pyre-blocked", first, 1);
                yield return Cast("player-zero", first, 0);
                yield return Cast("player-clamped-down", first, -99);
                yield return Cast("enemy-up", observers[0], 1);
                yield return Cast("enemy-down", observers[0], -1);
                observers[0].AddStatusEffect("rooted", 2); yield return combat.RunTriggerQueue();
                yield return Cast("enemy-rooted", observers[0], 1);
                observers[0].AddStatusEffect("immobile", 1); yield return combat.RunTriggerQueue();
                yield return Cast("enemy-immobile-before-rooted", observers[0], 1);
                observers[0].RemoveStatusEffect("immobile", 1); observers[0].RemoveStatusEffect("rooted", 1); yield return combat.RunTriggerQueue();
                yield return Cast("enemy-loop-up-to-bottom", observers[2], 1);
                yield return Cast("enemy-room-multiple-targets", observers[0], 1, room: true);
                yield return Cast("enemy-to-top-before-merge", observers[0], 1);
                first.AddStatusEffect("horde", 2); second.AddStatusEffect("horde", 2);
                observers[0].AddStatusEffect("horde", 2); observers[1].AddStatusEffect("horde", 2); yield return combat.RunTriggerQueue();
                yield return Cast("player-cross-room-horde-merge", first, 1);
                yield return Cast("enemy-cross-room-horde-merge", observers[1], 1);
                yield return Cast("enemy-into-pyre", observers[0], 1);
                CharacterData filler = UnityEngine.Object.Instantiate(prototype); filler.name = "PojuBumpFiller";
                Set(filler, "id", "c2f6ed7f-18ce-4070-b65f-7dd9f5200004"); Set(filler, "attackDamage", 0); Set(filler, "health", 1);
                Set(filler, "startingStatusEffects", Array.Empty<StatusEffectStackData>()); Set(filler, "triggers", new List<CharacterTriggerData>());
                Set(filler, "loopsBetweenTrainFloors", false);
                for (int slot = 0; slot < combat.NumSpawnPointsPerFloor(Team.Type.Heroes); slot++)
                    yield return managers.GetHeroManager()!.SpawnHeroInRoom(filler, 2, null!);
                yield return combat.RunTriggerQueue();
                yield return Cast("enemy-full-room-blocked", observers[2], 1);
                yield return managers.GetHeroManager()!.SpawnHeroInRoom(filler, 0, null!);
                yield return combat.RunTriggerQueue();
                var bottom = new List<CharacterState>(); rooms.GetRoom(0).AddCharactersToList(bottom, Team.Type.Heroes);
                yield return Cast("enemy-partial-full-room", bottom.Single(unit => unit.GetSourceCharacterData() == filler), 2);
                // Return the same authored card to a simple down mode for the subsequent ordinary policy.
                Configure(managers, -1, TargetMode.DropTargetCharacter, Team.Type.Heroes | Team.Type.Monsters, false);
                Set(combat, "combatStateChanged", true);
            }
            finally { label = null; combat.SuppressCombatPreviewUpdates = previous; }
            while (!Ready()) yield return null;

            IEnumerator Cast(string name, CharacterState target, int amount, bool room = false, bool ranged = false)
            {
                Configure(managers, amount, room ? TargetMode.Room : TargetMode.DropTargetCharacter,
                    room ? Team.Type.Heroes : Team.Type.Heroes | Team.Type.Monsters, ranged);
                if (!cards.GetHand().Contains(source) && !cards.DrawSpecificCard(source)) throw new InvalidOperationException("Bump redraw failed.");
                while (!Ready()) yield return null;
                int floor = target.GetCurrentRoomIndex(); yield return rooms.GetRoomUI().SetSelectedRoom(floor);
                int hand = cards.GetHand().IndexOf(source); SpawnPoint point = target.GetSpawnPoint();
                label = name;
                if (!cards.CanPlayHandCard(source, floor, point, null, null, out var error))
                    throw new InvalidOperationException("Bump paid card failed " + name + ": " + error);
                FullBattleTrace.Active!.BeginCardPlay(new PlayCardAction(FullBattleTrace.Active.CardId(source), floor,
                    targetUnitId: room ? 0 : FullBattleTrace.Active.UnitId(target)), scenarioAction: true);
                if (!cards.PlayCard(hand, point, ref error)) throw new InvalidOperationException("Bump paid card failed " + name + ": " + error);
                while (!Ready()) yield return null;
                // A decision boundary includes the normal combat-preview refresh. Raw effect
                // observations above remain before that refresh, as in an ordinary card play.
                combat.SuppressCombatPreviewUpdates = false;
                while (!Ready()) yield return null;
                FullBattleTrace.Active.CompleteCardPlay();
                combat.SuppressCombatPreviewUpdates = true;
                label = null;
            }
            bool Ready() => !managers.GetSaveManager().PreviewMode && !managers.GetReplayManager().IsCardPlaying() &&
                !combat.IsRunningTriggerQueue && Count(combat) == 0 && !(managers.GetHandUI()?.AreAnyCardsAnimating() ?? false) &&
                (combat.SuppressCombatPreviewUpdates || !(bool)AccessTools.Field(typeof(CombatManager), "combatStateChanged").GetValue(combat));
        }
        private static void Configure(AllGameManagers managers, int amount, TargetMode mode, Team.Type team, bool ranged)
        {
            CardData data = managers.GetSaveManager().GetAllGameData().FindCardData(source.GetCardDataID())!;
            var effect = new CardEffectData("CardEffectBump", null!, team); effect.Cheat_SetTargetMode(mode); Set(effect, "paramInt", amount);
            Set(effect, "useIntRange", ranged); Set(effect, "paramMinInt", -20); Set(effect, "paramMaxInt", -10);
            data.GetEffects().Clear(); data.GetEffects().Add(effect); data.GetTraits().Clear();
            Set(data, "overrideDescriptionKey", "");
            Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default); Set(data, "targetless", false); Set(data, "targetsRoom", true);
            foreach (CardState card in managers.GetCardManager()!.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == data.GetID()))
                card.Setup(data, managers.GetSaveManager());
            FullBattleTrace.Active?.InvalidateCardRules();
        }
        private static List<CharacterTriggerData> Triggers()
        {
            var list = new List<CharacterTriggerData>();
            foreach (var item in new[] { (CharacterTriggerData.Trigger.OnShift, 3), (CharacterTriggerData.Trigger.OnSentry, 5),
                (CharacterTriggerData.Trigger.PostAscension, 7), (CharacterTriggerData.Trigger.PostDescension, 11),
                (CharacterTriggerData.Trigger.PostAttemptedAscension, 13), (CharacterTriggerData.Trigger.PostAttemptedDescension, 17),
                (CharacterTriggerData.Trigger.OnTrainRoomLoop, 19), (CharacterTriggerData.Trigger.OnStatusEffectChanged, 23),
                (CharacterTriggerData.Trigger.OnTroopAdded, 29), (CharacterTriggerData.Trigger.OnSpawn, 31),
                (CharacterTriggerData.Trigger.CardMonsterPlayed, 37), (CharacterTriggerData.Trigger.OnDeath, 41) })
            { var trigger = HealingScenario.HealGold(item.Item2, false, true); Set(trigger, "trigger", item.Item1); list.Add(trigger); }
            return list;
        }
        private static int Count(CombatManager combat) => ((ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat)).Count;
        private static void Set(object target, string name, object value) => AccessTools.Field(target.GetType(), name).SetValue(target, value);
        [HarmonyPatch(typeof(CardEffectBump), nameof(CardEffectBump.ApplyEffect))]
        private static class EffectPatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            { if (Enabled && label != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode) __result = Observe(__result, cardEffectState, cardEffectParams); }
            private static IEnumerator Observe(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
            {
                FullBattleTrace trace = FullBattleTrace.Active!;
                CharacterState[] actors = parameters.targets.ToArray();
                var record = new Record { Label = label!, Amount = effect.GetSourceCardEffectData().GetParamInt(),
                    SourceCardId = parameters.playedCard == null ? 0 : trace.CardId(parameters.playedCard), Targets = actors.Select(trace.UnitId).ToArray(),
                    Before = trace.CaptureTrain(), Rooms = trace.CaptureDecision().PlayRules!.Rooms.ToArray() };
                Records.Add(record); current = record;
                try { while (native.MoveNext()) yield return native.Current; }
                finally
                {
                    (native as IDisposable)?.Dispose(); current = null;
                    record.After = trace.CaptureTrain(); record.QueueAfter = Count(AllGameManagers.Instance!.GetCombatManager()!);
                    record.TargetsAfter = actors.Select(actor => {
                        using (new CharacterState.SetAllowDestroyedAccessHelper(actor, onlyIfDestroyed: true)) return trace.CaptureUnit(actor);
                    }).ToArray();
                }
            }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class FirePatch
        {
            private static void Prefix(CharacterState __instance, CharacterTriggerData.Trigger trigger, CharacterState dyingCharacter,
                CharacterState.FireTriggersData fireTriggersData, int triggerCount, bool fromRunningTriggerQueue)
            {
                if (current == null || !fromRunningTriggerQueue) return;
                FullBattleTrace trace = FullBattleTrace.Active!;
                current.Dispatched.Add(new Callback { ActorId = trace.UnitId(__instance), Kind = trigger.ToString(), ParamInt = fireTriggersData?.paramInt ?? 0,
                    ParamInt2 = fireTriggersData?.paramInt2 ?? 0, ParamString = fireTriggersData?.paramString, TriggerCount = triggerCount,
                    DyingId = dyingCharacter == null ? 0 : trace.UnitId(dyingCharacter),
                    OverrideTargetId = fireTriggersData?.overrideTargetCharacter == null ? 0 : trace.UnitId(fireTriggersData.overrideTargetCharacter),
                    LastSpawnedOverrideUnitId = fireTriggersData?.overrideLastSpawnedCharacter == null ? 0 : trace.UnitId(fireTriggersData.overrideLastSpawnedCharacter) });
            }
        }
    }
}
