using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class TriggeredSummonProbe
    {
        private static readonly Dictionary<string, CharacterData> characters = new Dictionary<string, CharacterData>(StringComparer.Ordinal);
        private static TriggeredSummonCatalog? catalog;
        internal sealed class Record
        {
            public string Kind { get; set; } = "";
            public bool QueueRunning { get; set; }
            public int SourceCardId { get; set; }
            public int EquipmentCardId { get; set; }
            public int TriggerStateId { get; set; }
            public int EffectIndex { get; set; }
            public List<RevivalProbe.Callback> Queued { get; set; } = new List<RevivalProbe.Callback>();
            public TriggeredSummonRule RuleBefore { get; set; } = null!;
            public TriggeredSummonRule? RuleAfter { get; set; }
            public CombatUnit ActorBefore { get; set; } = null!;
            public CombatUnit? ActorAfter { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public bool Completed { get; set; }
            public string? Error { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        private static Record? current;
        internal sealed class CacheRecord
        {
            public int Turn { get; set; }
            public bool Preview { get; set; }
            public int SourceCardId { get; set; }
            public int[] Units { get; set; } = Array.Empty<int>();
        }
        internal static readonly List<CacheRecord> Caches = new List<CacheRecord>();
        internal sealed class DamageRecord
        {
            public int TargetId { get; set; }
            public int SourceCardId { get; set; }
            public int AttackerUnitId { get; set; }
            public int Damage { get; set; }
            public bool QueueRunning { get; set; }
            public CombatUnit[] PendingDeathsBefore { get; set; } = Array.Empty<CombatUnit>();
            public int[] PendingDeathRooms { get; set; } = Array.Empty<int>();
            public TrainCombatState BeforeTrain { get; set; } = null!;
            public TrainCombatState? AfterTrain { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public CombatUnit? TargetAfter { get; set; }
            public bool Completed { get; set; }
            public string? Error { get; set; }
        }
        internal static readonly List<DamageRecord> Damages = new List<DamageRecord>();
        internal static bool Enabled => (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? "").StartsWith("triggered-summon", StringComparison.Ordinal);
        private static string Register(CharacterData? unit)
        { if (unit == null) return ""; characters[unit.GetID()] = unit; return unit.GetID(); }
        internal static TriggeredSummonRule? Definition(CardEffectData effect)
        {
            if (effect.GetEffectStateName() != "CardEffectSpawnMonster") return null;
            CardUpgradeModifier? upgrade = null;
            if (effect.GetParamCardUpgradeData() != null)
            { var state = new CardUpgradeState(); state.Setup(effect.GetParamCardUpgradeData()); upgrade = CardModifierProbe.Upgrade(state); }
            return new TriggeredSummonRule(Register(effect.GetParamCharacterData()), Register(effect.GetParamAdditionalCharacterData()),
                (effect.GetParamCharacterDataPool() ?? new List<CharacterData>()).Select(Register).ToArray(),
                effect.GetParamInt(), effect.GetParamBool(), false, upgrade,
                new CardEffectTests(effect.GetShouldTest(), effect.GetShouldFailToCastIfTestFails(),
                    effect.GetShouldCancelSubsequentEffectsIfTestFails(), false, false));
        }
        internal static TriggeredSummonRule? Capture(CardEffectState effect)
        {
            if (!(effect.GetCardEffect() is CardEffectSpawnMonster native)) return null;
            TriggeredSummonRule rule = Definition(effect.GetSourceCardEffectData())!;
            CharacterState? spawned = native.GetSpawnedMonster();
            // Decision projections already clear removed attacker/spawner references.
            // The effect's weak first-birth cache follows the same rule; raw effect
            // boundaries keep a dead object until native destruction completes.
            if (FullBattleTrace.Active!.CanonicalDecisionCapture && spawned != null && (!spawned.IsAlive || spawned.IsDestroyed)) spawned = null;
            return new TriggeredSummonRule(Register(effect.GetParamCharacterData()), Register(effect.GetParamAdditionalCharacterData()),
                (effect.GetParamCharacterDataPool() ?? new List<CharacterData>()).Select(Register).ToArray(),
                effect.GetParamInt(), effect.GetParamBool(), effect.GetParentCardState() != null, rule.Upgrade, rule.Tests,
                spawned == null ? 0 : FullBattleTrace.Active!.UnitId(spawned));
        }
        internal static TriggeredSummonCatalog? Catalog()
        {
            if (!Enabled) return null;
            if (catalog != null) return catalog;
            AllGameManagers managers = AllGameManagers.Instance!;
            AllGameData all = managers.GetSaveManager().GetAllGameData();
            // Collect referenced units before building values. Capturing their triggers may
            // discover further references, without nesting a definition inside itself.
            var units = new List<SummonUnitDefinition>();
            var sources = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Select(card => all.FindCardData(card.GetCardDataID())!).ToList();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            while (characters.Keys.Any(id => !seen.Contains(id)))
            {
                CharacterData character = characters.Values.First(unit => !seen.Contains(unit.GetID()));
                seen.Add(character.GetID());
                CardData? fallback = all.GetAllCardData().FirstOrDefault(card => card != null && card.IsSpawnerCard() &&
                    card.GetSpawnCharacterData() != null && card.GetSpawnCharacterData()!.GetID() == character.GetID());
                var interactions = new List<string>();
                CombatUnit template = BattleActionProbe.SpawnTemplate(character, true, interactions);
                units.Add(new SummonUnitDefinition(character.GetID(), template,
                    fallback == null ? null : CardGenerationProbe.Creation(fallback), interactions));
                if (fallback != null) sources.Add(fallback);
            }
            var rooms = Enumerable.Range(0, managers.GetRoomManager()!.GetNumRooms()).Select(index =>
            {
                RoomState room = managers.GetRoomManager()!.GetRoom(index);
                CapacityInfo capacity = room.GetCapacityInfo(Team.Type.Monsters);
                return new RoomPlayRule(index, capacity.max, capacity.numSpawnPoints, room.IsRoomEnabled(),
                    room.IsRoomSummonBlocked(managers.GetRelicManager()), room.GetIsPyreRoom(), room.GetCapacityInfo(Team.Type.Heroes).max);
            }).ToArray();
            catalog = new TriggeredSummonCatalog(units, sources.GroupBy(card => card.GetID()).Select(group => group.First())
                .Select(card => new SummonCardDefinition(CardGenerationProbe.Creation(card), card.GetSpawnCharacterData()?.GetID() ?? "",
                    card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectAttachEquipment")
                        ? BattleActionProbe.Definition(card).Equipment : null)).ToArray(), rooms);
            return catalog;
        }
        private static IEnumerator Observe(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
        {
            var record = new Record(); Records.Add(record);
            Record? previous = current; current = record;
            FullBattleTrace trace = FullBattleTrace.Active!;
            CharacterState actor = parameters.selfTarget!;
            RoomState room = parameters.GetSelectedRoom(AllGameManagers.Instance!.GetRoomManager()!)!;
            try
            {
                record.Kind = parameters.sourceCharacterTriggerState?.GetTrigger().ToString() ?? "";
                record.TriggerStateId = trace.TriggerStateId(actor, parameters.sourceCharacterTriggerState!);
                record.EffectIndex = parameters.sourceCharacterTriggerState!.GetEffectStates().FindIndex(item => item == effect);
                record.SourceCardId = parameters.playedCard == null ? 0 : trace.CardId(parameters.playedCard);
                record.EquipmentCardId = effect.GetParentEquipment() == null ? 0 : trace.CardId(effect.GetParentEquipment()!);
                record.QueueRunning = AllGameManagers.Instance!.GetCombatManager()!.IsRunningTriggerQueue;
                record.RuleBefore = Capture(effect)!;
                using (new CharacterState.SetAllowDestroyedAccessHelper(actor, onlyIfDestroyed: true)) record.ActorBefore = trace.CaptureUnit(actor);
                record.Before = trace.Capture(room);
            }
            catch (Exception error) { record.Error = error.ToString(); }
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose(); current = previous;
                try
                {
                    record.RuleAfter = Capture(effect);
                    using (new CharacterState.SetAllowDestroyedAccessHelper(actor, onlyIfDestroyed: true)) record.ActorAfter = trace.CaptureUnit(actor);
                    record.After = trace.Capture(room);
                }
                catch (Exception error) { record.Error = error.ToString(); }
            }
        }
        private static IEnumerator ObserveDamage(IEnumerator native, int damage, CharacterState target, CombatManager.ApplyDamageToTargetParameters parameters)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            RoomState room = target.GetCurrentRoom();
            var record = new DamageRecord { TargetId = trace.UnitId(target), Damage = damage,
                SourceCardId = trace.CardId(parameters.playedCard!),
                AttackerUnitId = parameters.selfTarget == null ? 0 : trace.UnitId(parameters.selfTarget),
                QueueRunning = AllGameManagers.Instance!.GetCombatManager()!.IsRunningTriggerQueue };
            Damages.Add(record);
            try
            {
                record.Before = trace.Capture(room);
                record.BeforeTrain = trace.CaptureTrain();
                var deaths = trace.KnownUnits.Where(unit => unit != null && !unit.IsDestroyed && unit.HasFinishedDying &&
                    unit.GetHP() <= 0 && unit.GetSpawnPoint(allowLastKnownSpawnPoint: true) != null)
                    .Select(unit => new { Unit = trace.CaptureUnit(unit), Room = unit.GetCurrentRoom(allowLastKnownRoom: true).GetRoomIndex() })
                    .Where(item => item.Unit.DeathState?.IsBeingRemoved == false).ToArray();
                record.PendingDeathsBefore = deaths.Select(item => item.Unit).ToArray();
                record.PendingDeathRooms = deaths.Select(item => item.Room).ToArray();
            }
            catch (Exception error) { record.Error = error.ToString(); }
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose();
                try
                {
                    record.After = trace.Capture(room);
                    record.AfterTrain = trace.CaptureTrain();
                    using (new CharacterState.SetAllowDestroyedAccessHelper(target, onlyIfDestroyed: true)) record.TargetAfter = trace.CaptureUnit(target);
                }
                catch (Exception error) { record.Error = error.ToString(); }
            }
        }
        [HarmonyPatch(typeof(CardState), nameof(CardState.CacheRoomStateCharactersAtTimeOfCardPlay))]
        private static class CachePatch
        {
            private static void Postfix(CardState __instance)
            {
                FullBattleTrace? trace = FullBattleTrace.Active;
                if (!Enabled || trace == null || !trace.KnownCards.Contains(__instance)) return;
                var known = new HashSet<CharacterState>(trace.KnownUnits);
                var cached = (IEnumerable<WeakRef<CharacterState>>)AccessTools.Field(typeof(CardState), "charactersInRoomAtTimeOfCardPlay").GetValue(__instance);
                Caches.Add(new CacheRecord { Turn = AllGameManagers.Instance!.GetCombatManager()!.GetTurnCount(),
                    Preview = AllGameManagers.Instance.GetSaveManager().PreviewMode, SourceCardId = trace.CardId(__instance),
                    Units = cached.Select(reference => reference.Ref).Where(unit => unit != null && known.Contains(unit))
                        .Select(unit => trace.UnitId(unit)).OrderBy(id => id).ToArray() });
            }
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.QueueTrigger), new[] { typeof(CharacterState), typeof(CharacterTriggerData.Trigger),
            typeof(CharacterState), typeof(bool), typeof(bool), typeof(CharacterState.FireTriggersData), typeof(int), typeof(CharacterTriggerState) })]
        private static class QueuePatch
        {
            private static void Prefix(CombatManager __instance, out int __state) => __state = Count(__instance);
            private static void Postfix(CombatManager __instance, CharacterState character, CharacterTriggerData.Trigger trigger,
                CharacterState dyingCharacter, CharacterState.FireTriggersData fireTriggersData, int triggerCount, int __state)
            {
                if (current != null && Count(__instance) > __state && character.GetTriggers().Any(state => state.GetTrigger() == trigger))
                    current.Queued.Add(RevivalProbe.CaptureCallback(character, trigger, dyingCharacter, fireTriggersData, triggerCount));
            }
            private static int Count(CombatManager combat) => ((ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat)).Count;
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.ApplyDamageToTarget))]
        private static class DamagePatch
        {
            private static void Postfix(int damage, CharacterState target, CombatManager.ApplyDamageToTargetParameters parameters, ref IEnumerator __result)
            {
                if (Enabled && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode &&
                    parameters.playedCard != null && target.IsAlive)
                    __result = ObserveDamage(__result, damage, target, parameters);
            }
        }
        [HarmonyPatch(typeof(CardEffectSpawnMonster), nameof(CardEffectSpawnMonster.ApplyEffect))]
        private static class EffectPatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (Enabled && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode &&
                    cardEffectParams.selfTarget != null)
                    __result = Observe(__result, cardEffectState, cardEffectParams);
            }
        }
    }
}
