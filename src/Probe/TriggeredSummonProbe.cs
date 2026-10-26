using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class TriggeredSummonProbe
    {
        private static readonly Dictionary<string, CharacterData> characters = new Dictionary<string, CharacterData>(StringComparer.Ordinal);
        private static readonly HashSet<string> enemyCharacters = new HashSet<string>(StringComparer.Ordinal);
        private static TriggeredSummonCatalog? catalog;
        private static FullBattleTrace? catalogTrace;
        internal sealed class Record
        {
            public string Kind { get; set; } = "";
            public bool QueueRunning { get; set; }
            public int SourceCardId { get; set; }
            public int EquipmentCardId { get; set; }
            public int TriggerStateId { get; set; }
            public int EffectIndex { get; set; }
            public List<RevivalProbe.Callback> Queued { get; set; } = new List<RevivalProbe.Callback>();
            public List<UnitCloneCallback> FullQueued { get; set; } = new List<UnitCloneCallback>();
            public TriggeredSummonRule RuleBefore { get; set; } = null!;
            public TriggeredSummonRule? RuleAfter { get; set; }
            public CombatUnit ActorBefore { get; set; } = null!;
            public CombatUnit? ActorAfter { get; set; }
            public CombatUnit[] RetainedBefore { get; set; } = Array.Empty<CombatUnit>();
            public CombatUnit[] RetainedAfter { get; set; } = Array.Empty<CombatUnit>();
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public bool Completed { get; set; }
            public string? Error { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        private static Record? current;
        internal static bool HasEnemyBirths => DiscoverEnemyBirths();
        // Player decks need the same authored child definitions as isolated
        // summon scenarios, including eggs that have not entered play yet.
        internal static bool HasPlayerBirths
        {
            get
            {
                var cards = AllGameManagers.Instance?.GetCardManager()?.GetAllCards(new List<CardState>());
                if (cards == null) return false;
                bool found = false;
                foreach (CardState card in cards)
                foreach (CardEffectData effect in card.GetSpawnCharacterData()?.GetTriggers()
                    .SelectMany(trigger => trigger.GetEffects()) ?? Enumerable.Empty<CardEffectData>())
                    if (effect.GetEffectStateName() == "CardEffectSpawnMonster")
                    { Definition(effect); found = true; }
                return found;
            }
        }
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
        internal static bool Enabled => DiscoverEnemyBirths() ||
            (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? "").StartsWith("triggered-summon", StringComparison.Ordinal) ||
            SpawnStatusRelicScenario.OriginalEggTransformation || OriginalEggCardScenario.Enabled ||
            EnchantmentBattleScenario.ChildSummons || OriginalSupportUnitsScenario.CapturesTriggeredSummons ||
            OriginalSupportSpellsScenario.CapturesTriggeredSummons || UnitCardCopyProbe.Enabled || JackInTheBoxProbe.Enabled;
        private static bool DiscoverEnemyBirths()
        {
            if (enemyCharacters.Count > 0) return true;
            HeroManager? heroes = AllGameManagers.Instance?.GetHeroManager();
            if (heroes == null) return false;
            var pattern = (SpawnPatternData?)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(heroes);
            if (pattern == null) return false;
            bool harder = (bool)AccessTools.Field(typeof(HeroManager), "harderOuterTrainBossEnabled").GetValue(heroes);
            CharacterData? data = pattern.GetOuterTrainBossCharacter(harder);
            if (data == null || !OuterBossDefinitions.IsFleshweaver(data.GetAssetKey())) return false;
            foreach (CardEffectData effect in data.GetTriggers().SelectMany(trigger => trigger.GetEffects())
                .Where(effect => effect.GetEffectStateName() == "CardEffectSpawnHero")) Definition(effect);
            return enemyCharacters.Count > 0;
        }
        internal static string Register(CharacterData? unit)
        {
            if (unit == null) return "";
            if (!characters.ContainsKey(unit.GetID())) catalog = null;
            characters[unit.GetID()] = unit;
            return unit.GetID();
        }
        internal static TriggeredSummonRule? Definition(CardEffectData effect)
        {
            if (effect.GetEffectStateName() == "CardEffectSpawnHero")
            {
                if (effect.GetTargetMode() != TargetMode.Room || effect.GetTargetTeamType() != Team.Type.None) return null;
                string Enemy(CharacterData? data)
                {
                    string id = Register(data);
                    if (id.Length > 0 && enemyCharacters.Add(id)) catalog = null;
                    return id;
                }
                return new TriggeredSummonRule(Enemy(effect.GetParamCharacterData()), "",
                    (effect.GetParamCharacterDataPool() ?? new List<CharacterData>()).Select(Enemy).ToArray(),
                    1, true, false, null, new CardEffectTests(effect.GetShouldTest(), effect.GetShouldFailToCastIfTestFails(),
                        effect.GetShouldCancelSubsequentEffectsIfTestFails(), false, false), enemyHero: true);
            }
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
            if (effect.GetCardEffect() is CardEffectSpawnHero) return Definition(effect.GetSourceCardEffectData());
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
            if (!ReferenceEquals(catalogTrace, FullBattleTrace.Active))
            { catalog = null; catalogTrace = FullBattleTrace.Active; }
            if (catalog != null) return catalog;
            AllGameManagers managers = AllGameManagers.Instance!;
            AllGameData all = managers.GetSaveManager().GetAllGameData();
            var sources = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Select(card => all.FindCardData(card.GetCardDataID())!).ToList();
            // The catalog also carries equipment definitions. Native Hydra's
            // OnEquipmentAdded consumes its equipment without summoning a unit;
            // ordinary player captures must retain that catalog as well.
            if (!Enabled && !HasPlayerBirths && !sources.Any(card => card.GetSpawnCharacterData()?.GetTriggers()
                .SelectMany(trigger => trigger.GetEffects())
                .Any(effect => effect.GetEffectStateName() == "CardEffectRemoveEquipment") == true)) return null;
            // Collect referenced units before building values. Capturing their triggers may
            // discover further references, without nesting a definition inside itself.
            var units = new List<SummonUnitDefinition>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            while (characters.Keys.Any(id => !seen.Contains(id)))
            {
                CharacterData character = characters.Values.First(unit => !seen.Contains(unit.GetID()));
                seen.Add(character.GetID());
                if (enemyCharacters.Contains(character.GetID()))
                {
                    EnemyDefinition raw = EnemySpawningProbe.Definition(character, true);
                    var definition = new EnemyDefinition(raw.Unit, raw.Ascends, raw.Loops, raw.ExternalInteractions, raw.CompanionBoss,
                        CovenantBirthStatProbe.Capture(character));
                    units.Add(new SummonUnitDefinition(character.GetID(), raw.Unit, null, raw.ExternalInteractions, definition));
                    continue;
                }
                CardData? fallback = all.GetAllCardData().FirstOrDefault(card => card != null && card.IsSpawnerCard() &&
                    card.GetSpawnCharacterData() != null && card.GetSpawnCharacterData()!.GetID() == character.GetID());
                var interactions = new List<string>();
                CombatUnit template = BattleActionProbe.SpawnTemplate(character, true, interactions);
                units.Add(new SummonUnitDefinition(character.GetID(), template,
                    fallback == null ? null : CardGenerationProbe.Creation(fallback), interactions));
                if (fallback != null) sources.Add(fallback);
            }
            // Native grafting creates equipment on death that is absent from
            // the initial live deck. Include those authored definitions before
            // caching the catalog used by equipment removal callbacks.
            var sourceIds = new HashSet<string>(sources.Select(card => card.GetID()), StringComparer.Ordinal);
            for (int index = 0; index < sources.Count; index++)
            {
                CardData? graft = sources[index].GetSpawnCharacterData()?.GetGraftedEquipment();
                if (graft != null && sourceIds.Add(graft.GetID())) sources.Add(graft);
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
            CharacterState[] retained = Array.Empty<CharacterState>();
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
                var absent = (record.Before.Context?.SpawnPoints?.Groups.Where(group => group.RoomIndex == record.Before.RoomIndex &&
                    !group.PreviewCopyId.HasValue).SelectMany(group => group.Occupants) ?? Enumerable.Empty<int>())
                    .Where(id => id > 0 && id != record.ActorBefore.Id && !record.Before.Units.Any(unit => unit.Id == id)).Distinct().OrderBy(id => id).ToArray();
                retained = absent.Select(id => trace.KnownUnits.Single(unit => trace.TryGetUnitId(unit, out int known) && known == id)).ToArray();
                record.RetainedBefore = CaptureRetained(trace, retained);
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
                    record.RetainedAfter = CaptureRetained(trace, retained);
                }
                catch (Exception error) { record.Error = error.ToString(); }
                if (EnchantmentBattleScenario.ChildSummons && Records.Count >= 2 && Records.All(item => item.Completed)) WriteAuraCalibration();
            }
        }
        private static CombatUnit[] CaptureRetained(FullBattleTrace trace, IEnumerable<CharacterState> retained)
            => retained.Select(unit =>
            {
                using (new CharacterState.SetAllowDestroyedAccessHelper(unit, onlyIfDestroyed: true)) return trace.CaptureUnit(unit);
            }).ToArray();
        private static void WriteAuraCalibration()
        {
            string? Difference(Record record)
            {
                if (record.Error != null || record.After == null) return record.Error ?? "Missing native completion.";
                RoomCombatResult result = RoomCombatModel.ApplyNativeTriggeredSummon(record.Before, record.ActorBefore,
                    record.TriggerStateId, record.EffectIndex, out TriggeredSummonRule? rule, retainedOccupants: record.RetainedBefore);
                if (!result.Supported) return result.UnsupportedReason;
                CombatUnit actor = result.State!.Units.Concat(result.RetainedUnits).Single(unit => unit.Id == record.ActorBefore.Id);
                UnitCloneCallback[] callbacks = result.PendingCallbacks.Where(item => item.Unit.Triggers.Any(trigger => trigger.Kind == item.Kind))
                    .Select(UnitCloneCallback.From).ToArray();
                return JToken.DeepEquals(JToken.FromObject(result.State), JToken.FromObject(record.After)) &&
                    JToken.DeepEquals(JToken.FromObject(actor), JToken.FromObject(record.ActorAfter!)) &&
                    JToken.DeepEquals(JToken.FromObject(record.RetainedBefore.Select(retained => result.RetainedUnits.Single(unit => unit.Id == retained.Id))),
                        JToken.FromObject(record.RetainedAfter)) &&
                    JToken.DeepEquals(JToken.FromObject(rule!), JToken.FromObject(record.RuleAfter!)) &&
                    JToken.DeepEquals(JToken.FromObject(callbacks), JToken.FromObject(record.FullQueued)) ? null : "Native queued aura summon differs.";
            }
            var comparisons = Records.Select(record => new { Native = record, Difference = Difference(record) }).ToArray();
            string path = Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "enchantment-summon-calibration.mt2f");
            using (var archive = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId,
                Boundary = "OriginalQueuedSpawnMonster", GameplaySuppressed = false, WholeBattleVerified = false,
                ModifierScenario = Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS"),
                Mismatches = comparisons.Count(item => item.Difference != null), Samples = comparisons, SourceClones = UnitBirthProbe.Clones }))
            using (var stream = File.Create(path)) archive.Write(stream);
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
                {
                    current.Queued.Add(RevivalProbe.CaptureCallback(character, trigger, dyingCharacter, fireTriggersData, triggerCount));
                    FullBattleTrace trace = FullBattleTrace.Active!;
                    current.FullQueued.Add(new UnitCloneCallback(trace.UnitId(character), trigger.ToString(), fireTriggersData?.paramInt ?? 0,
                        fireTriggersData?.paramInt2 ?? 0, fireTriggersData?.paramString, triggerCount, dyingCharacter == null ? 0 : trace.UnitId(dyingCharacter),
                        fireTriggersData?.overrideTargetCharacter == null ? 0 : trace.UnitId(fireTriggersData.overrideTargetCharacter),
                        fireTriggersData?.overrideLastSpawnedCharacter == null ? 0 : trace.UnitId(fireTriggersData.overrideLastSpawnedCharacter)));
                }
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
