using System;
namespace MonsterTrain2Poju.Probe
{
#if !DEVELOPMENT_PROBE
    internal static class RoomHealthCardHoverCalibration { internal static bool Enabled => false; }
#else
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using BepInEx.Logging;
    using HarmonyLib;
    using MonsterTrain2Poju.Capture;
    using MonsterTrain2Poju.Model;

    internal static class RoomHealthCardHoverCalibration
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ROOM_HEALTH_CARD_HOVER") == "1";
        internal static bool Started { get; private set; }
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }
        private static AllGameManagers Managers => AllGameManagers.Instance!;
        private static FullBattleTrace Trace => FullBattleTrace.Active!;
        private static readonly List<Record> Samples = new List<Record>();
        private static readonly List<CharacterTriggerDispatch> Dispatches = new List<CharacterTriggerDispatch>();
        private static Record? active;
        private static CharacterState host = null!;
        private static CharacterState[] actors = Array.Empty<CharacterState>();
        internal sealed class AttackQuery
        {
            public int UnitId, Attack;
        }
        internal sealed class Record
        {
            public string Label = "", NativeType = "";
            public int Amount, CardId, TargetId, DropTargetId, RoomIndex;
            public CardPlayRule Rule = null!;
            public TrainCombatState Before = null!, After = null!, Cleared = null!;
            public TrainCombatState? CopiedBefore, CopiedActual;
            public CombatUnit? ActualTarget;
            public CombatUnit[] ActualTargets = Array.Empty<CombatUnit>();
            public AttackQuery[] BeforeAttacks = Array.Empty<AttackQuery>(), CopiedAttacks = Array.Empty<AttackQuery>(), ActualAttacks = Array.Empty<AttackQuery>();
            public int[] CopiedIds = Array.Empty<int>();
            public int EnableCount, DisableCount;
            public bool NativeReturned, QueueEmpty, TemporaryDisabled;
            public object RngBefore = null!, RngAfter = null!;
            public CharacterTriggerDispatch[] Dispatches = Array.Empty<CharacterTriggerDispatch>();
        }
        private static CharacterState[] Actors()
        {
            var result = new List<CharacterState>();
            for (int room = 0; room < Managers.GetRoomManager().GetNumRooms(); room++)
                Managers.GetRoomManager().GetRoom(room).AddCharactersToList(result, Team.Type.Monsters | Team.Type.Heroes);
            return result.Where(actor => actor != null && !actor.IsDestroyed).Distinct().ToArray();
        }
        private static bool Source(CharacterState actor) => actor.GetRoomStateModifiers()
            .Any(modifier => modifier.GetType().Name == RoomHealthDamageModel.FriendlyOwn);
        internal static bool Eligible => OriginalRoomHealthAttackScenario.Prepared && !Managers.GetSaveManager().PreviewMode &&
            !Managers.GetReplayManager().IsCardPlaying() && !Managers.GetCombatManager().IsRunningTriggerQueue &&
            !(Managers.GetHandUI()?.AreAnyCardsAnimating() ?? false) && CardAnimationSettlement.Ready() &&
            Actors().Count(actor => actor.GetCurrentRoomIndex() == 1 && Source(actor)) == 3;
        internal static void Start(ManualLogSource log)
        { Started = true; Managers.GetSaveManager().StartCoroutine(Protect(Capture(log), log)); }
        private static IEnumerator Protect(IEnumerator root, ManualLogSource log)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(root);
            try
            {
                while (stack.Count > 0)
                {
                    var step = stack.Peek(); bool next;
                    try { next = step.MoveNext(); }
                    catch (Exception error) { Error = error.ToString(); log.LogError(Error); break; }
                    if (!next) { (step as IDisposable)?.Dispose(); stack.Pop(); continue; }
                    if (step.Current is IEnumerator nested) stack.Push(nested); else yield return step.Current;
                }
                Completed = Error == null;
            }
            finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); }
        }
        private static TrainCombatState Snapshot(IReadOnlyList<CharacterState>? copied = null)
        {
            var state = Trace.Capture(host.GetCurrentRoom()); var context = state.Context!;
            if (copied != null) context = context.WithEnchantments(context.Enchantments!.WithTemporaryPreviewUnits(copied.Select(Trace.UnitId).ToArray()));
            var world = context.Enchantments!;
            return new TrainCombatState(world.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units, room.ExternalInteractions, context, room.Preview, room.ActiveCompanionBossIds)).ToArray(),
                world.Movement, world.EnemySlotsPerRoom, context);
        }
        private static AttackQuery[] Attacks(TrainCombatState state)
        {
            int[] living = state.Rooms.SelectMany(room => room.Units).Where(unit => unit.Health > 0).Select(unit => unit.Id).ToArray();
            return actors.Where(actor => !actor.IsDestroyed && living.Contains(Trace.UnitId(actor)))
                .Select(actor => new AttackQuery { UnitId = Trace.UnitId(actor), Attack = actor.GetAttackDamage() }).ToArray();
        }
        private static IEnumerator Capture(ManualLogSource log)
        {
            var cards = Managers.GetCardManager()!; var combat = Managers.GetCombatManager(); var all = Managers.GetAllGameData();
            actors = Actors(); var targets = actors.Where(actor => actor.GetCurrentRoomIndex() == 1 && actor.GetTeamType() == Team.Type.Monsters)
                .OrderBy(Trace.UnitId).ToArray();
            if (targets.Length != 5 || targets.Count(Source) != 3) throw new InvalidOperationException("Expected three unchanged original aura champions and two recipients.");
            var definitions = OriginalRoomHealthAttackScenario.CurrentDefinitions;
            var cases = new[] { ("CardEffectDamage", 3), ("CardEffectDamage", 9999), ("CardEffectHeal", 3), ("CardEffectHeal", 9999),
                ("CardEffectDebuffMaxHealth", 3), ("CardEffectDebuffMaxHealth", 9999), ("CardEffectBuffMaxHealth", 3) };
            var controls = new Dictionary<(string Type, int Value, bool Room), CardState>();
            int suffix = 0;
            foreach (bool room in new[] { false, true })
            foreach (var item in cases)
            {
                var data = UnityEngine.Object.Instantiate(all.FindCardData(DevotionSpawnBattleScenario.VenerateId));
                data.name = "PojuRoomHealthHover" + (++suffix);
                void Set(string name, object value) => AccessTools.Field(typeof(CardData), name).SetValue(data, value);
                Set("id", "fc231b93-0000-4000-8000-" + suffix.ToString("D12")); Set("cost", 0); Set("costType", CardData.CostType.Default);
                Set("traits", new List<CardTraitData>()); Set("triggers", new List<CardTriggerEffectData>());
                Set("effectTriggers", new List<CharacterTriggerData>()); Set("startingUpgrades", new List<CardUpgradeData>());
                Set("targetless", room); Set("targetsRoom", true);
                var effect = new CardEffectData(item.Item1, null!, Team.Type.Monsters);
                effect.Cheat_SetTargetMode(room ? TargetMode.Room : TargetMode.DropTargetCharacter);
                AccessTools.Field(typeof(CardEffectData), "paramInt").SetValue(effect, item.Item2);
                Set("effects", new List<CardEffectData> { effect });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data);
                var card = cards.AddNewCard(data, CardPile.DeckPile, false, false, animate: false);
                Trace.CardId(card); cards.GetDrawPile().Remove(card); controls.Add((item.Item1, item.Item2, room), card);
            }
            Trace.InvalidateCardRules();
            bool suppression = CombatPreviewControl.Save(combat);
            int originalSuppression = (int)AccessTools.Field(typeof(CombatManager), "combatPreviewSuppressionCount").GetValue(combat);
            CombatPreviewControl.Set(combat, true);
            var queue = (Queue<CombatManager.TriggerQueueData>)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat);
            try
            {
                foreach (bool room in new[] { false, true })
                foreach (int floor in room ? new[] { 1, 2 } : new[] { 1 })
                foreach (var target in room ? actors.Where(actor => actor.GetCurrentRoomIndex() == floor && actor.GetTeamType() == Team.Type.Monsters).Take(1) : targets)
                foreach (var item in cases)
                {
                    host = target; yield return Managers.GetRoomManager().GetRoomUI().SetSelectedRoom(floor);
                    var card = controls[(item.Item1, item.Item2, room)]; cards.GetHand().Add(card);
                    active = new Record { Label = item.Item1 + "-" + item.Item2 + "-floor" + floor + "-target" + Trace.UnitId(target) + (room ? "-room" : "-drop"),
                        NativeType = item.Item1, Amount = item.Item2, CardId = Trace.CardId(card), TargetId = Trace.UnitId(target),
                        DropTargetId = room ? 0 : Trace.UnitId(target), RoomIndex = floor, Rule = BattleActionProbe.Definition(all.FindCardData(card.GetCardDataID())) };
                    active.Before = Snapshot(); active.BeforeAttacks = Attacks(active.Before);
                    active.RngBefore = HalfHealthLifecycleCalibration.Rng(); Dispatches.Clear();
                    combat.PreviewCardPlay(true, card, Managers.GetRoomManager().GetRoom(floor), null!, room ? null! : target.GetSpawnPoint(), null!, null!);
                    active.NativeReturned = true; active.After = Snapshot();
                    combat.PreviewCardPlay(false, card, Managers.GetRoomManager().GetRoom(floor), null!, room ? null! : target.GetSpawnPoint(), null!, null!);
                    active.Cleared = Snapshot(); active.RngAfter = HalfHealthLifecycleCalibration.Rng();
                    active.Dispatches = Dispatches.ToArray(); active.QueueEmpty = queue.Count == 0;
                    active.TemporaryDisabled = !Managers.GetSaveManager().PreviewMode && actors.All(actor => !actor.PreviewMode && !actor.TemporaryPreviewsEnabled);
                    Samples.Add(active);
                    if (active.EnableCount != 1 || active.DisableCount != 1 || active.CopiedBefore == null || active.CopiedActual == null ||
                        !active.QueueEmpty || !active.TemporaryDisabled || actors.Any(actor => actor.IsDestroyed || !actor.IsAlive))
                        throw new InvalidOperationException("Room-health hover failed to restore primary actors: " + active.Label);
                    log.LogInfo("ROOM-HEALTH-CARD-HOVER sample=" + Samples.Count + " label=" + active.Label);
                    active = null; cards.GetHand().Remove(card);
                }
            }
            finally { active = null; CombatPreviewControl.Set(combat, suppression); }
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId, Boundary = "NativeOriginalRoomHealthCardHover",
                NativeCompleted = true, Definitions = definitions, DefinitionsAfter = OriginalRoomHealthAttackScenario.CurrentDefinitions,
                OriginalSuppression = originalSuppression, FinalSuppression = (int)AccessTools.Field(typeof(CombatManager), "combatPreviewSuppressionCount").GetValue(combat), Samples }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "room-health-card-hover.mt2f"))) document.Write(output);
        }
        [HarmonyPatch(typeof(CombatManager), "SetTemporaryStateEnabled")]
        private static class CopyPatch
        {
            private static void Prefix(bool enable, List<CharacterState> characters, bool isCardPreview)
            {
                if (active == null || !isCardPreview || enable) return;
                active.CopiedActual = Snapshot(characters); active.ActualTarget = Trace.CaptureUnit(host);
                active.ActualTargets = characters.Select(actor => Trace.CaptureUnit(actor)).ToArray(); active.ActualAttacks = Attacks(active.CopiedActual); active.DisableCount++;
            }
            private static void Postfix(bool enable, List<CharacterState> characters, bool isCardPreview)
            {
                if (active == null || !isCardPreview || !enable) return;
                active.CopiedBefore = Snapshot(characters); active.CopiedIds = characters.Select(Trace.UnitId).ToArray();
                active.CopiedAttacks = Attacks(active.CopiedBefore); active.EnableCount++;
            }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class DispatchPatch
        {
            private static void Prefix(CharacterState __instance, CharacterTriggerData.Trigger trigger, CharacterState dyingCharacter,
                bool fromRunningTriggerQueue, CharacterState.FireTriggersData fireTriggersData, int triggerCount, CharacterTriggerState exclusiveTrigger)
            {
                if (active != null && fromRunningTriggerQueue)
                    Dispatches.Add(new CharacterTriggerDispatch(Trace.UnitId(__instance), trigger.ToString(), dyingCharacter == null ? 0 : Trace.UnitId(dyingCharacter),
                        triggerCount, fireTriggersData?.paramInt ?? 0, fireTriggersData?.paramInt2 ?? 0, fireTriggersData?.paramString,
                        fireTriggersData?.overrideTargetCharacter == null ? 0 : Trace.UnitId(fireTriggersData.overrideTargetCharacter),
                        exclusiveTriggerStateId: exclusiveTrigger == null ? (int?)null : Trace.TriggerStateId(__instance, exclusiveTrigger)));
            }
        }
    }
#endif
}
