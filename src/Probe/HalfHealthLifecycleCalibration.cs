using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class HalfHealthLifecycleCalibration
    {
        internal static bool Started { get; private set; }
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }
        internal static bool MaximumHealth => Environment.GetEnvironmentVariable("MT2_PROBE_HALF_HEALTH_MAXIMUM") == "1";
        internal static bool UnitUpgrades => Environment.GetEnvironmentVariable("MT2_PROBE_HALF_HEALTH_UPGRADE") == "1";
        internal static bool UpgradeEffects => Environment.GetEnvironmentVariable("MT2_PROBE_HALF_HEALTH_UPGRADE_EFFECTS") == "1";
        private static bool recording;
        private static readonly List<CharacterTriggerDispatch> dispatches = new List<CharacterTriggerDispatch>();
        private static AllGameManagers Managers => AllGameManagers.Instance!;
        private static SaveManager Save => Managers.GetSaveManager();
        private static FullBattleTrace Trace => FullBattleTrace.Active!;
        private static readonly Type StateType = AccessTools.Inner(typeof(CharacterState), "StateInformation");
        private static object Get(object target, string field) => AccessTools.Field(target.GetType(), field).GetValue(target);
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
        private static Queue<CombatManager.TriggerQueueData> Queue =>
            (Queue<CombatManager.TriggerQueueData>)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(Managers.GetCombatManager());
        private static CharacterState[] Actors()
        {
            var actors = new List<CharacterState>();
            for (int room = 0; room < Managers.GetRoomManager().GetNumRooms(); room++)
                Managers.GetRoomManager().GetRoom(room).AddCharactersToList(actors, Team.Type.Monsters | Team.Type.Heroes);
            return actors.Where(actor => actor.IsAlive && !actor.IsDestroyed).ToArray();
        }
        private static CharacterState? Host() => UpgradeEffects ? Actors().FirstOrDefault(actor => actor.GetTeamType() == Team.Type.Monsters &&
            actor.GetSourceCharacterData().GetAssetKey().StartsWith("TrainSteward", StringComparison.Ordinal) && actor.GetSpawnerCard() != null) :
            Actors().FirstOrDefault(actor => actor.GetTeamType() == Team.Type.Heroes &&
            Actors().Any(other => other.GetTeamType() == Team.Type.Monsters && other.GetCurrentRoomIndex() == actor.GetCurrentRoomIndex() &&
                other.GetSourceCharacterData().GetAssetKey().StartsWith("TrainSteward", StringComparison.Ordinal)));
        internal static bool Eligible => ConditionalActivationProbe.HasVanguard && Host() != null && Queue.Count == 0 &&
            (!UpgradeEffects || !Managers.GetReplayManager().IsCardPlaying() && !Managers.GetCombatManager().IsRunningTriggerQueue &&
                !(Managers.GetHandUI()?.AreAnyCardsAnimating() ?? false));
        internal static void Start(ManualLogSource log)
        { Started = true; Save.StartCoroutine(Protect(Run(log), log)); }
        private static IEnumerator Protect(IEnumerator root, ManualLogSource log)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(root);
            try
            {
                while (stack.Count > 0)
                {
                    IEnumerator step = stack.Peek(); bool next;
                    try { next = step.MoveNext(); }
                    catch (Exception error) { Error = error.ToString(); log.LogError(Error); break; }
                    if (!next) { (step as IDisposable)?.Dispose(); stack.Pop(); continue; }
                    if (step.Current is IEnumerator child) stack.Push(child); else yield return step.Current;
                }
            }
            finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); recording = false; }
            Completed = Error == null;
        }
        internal static object Rng() => new { Unity = RngCalibration.Words(UnityEngine.Random.state),
            Streams = Enum.GetValues(typeof(RngId)).Cast<RngId>().Select(id =>
                new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray() };
        private static HadesRNG NativeRng(RngId id) => (HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { id });
        internal static JToken GameplayRng(object snapshot)
        {
            var value = JObject.FromObject(snapshot);
            value["Streams"] = new JArray(value["Streams"]!.Where(item => (string?)item["Id"] != "NonDeterministic" && (string?)item["Id"] != "Chatter"));
            return value;
        }
        private static IEnumerator Run(ManualLogSource log)
        {
            if (MaximumHealth || UnitUpgrades || UpgradeEffects)
            {
                HealingScenario.RegisterHealingStatus("heal immunity", "StatusEffectHealImmunityState", 0);
                HealingScenario.RegisterHealingStatus("heal multiplier", "StatusEffectHealMultiplierState", 2);
            }
            CharacterState[] actors = Actors(); CharacterState host = Host()!;
            RoomManager rooms = Managers.GetRoomManager();
            TrainCombatState original = Trace.CaptureTrain(); object originalRng = Rng();
            var originalStreams = Enum.GetValues(typeof(RngId)).Cast<RngId>().ToDictionary(id => id, id => new HadesRNG(NativeRng(id)));
            UnityEngine.Random.State originalUnity = UnityEngine.Random.state;
            object[] states = actors.Select(actor => Get(actor, "_primaryStateInformation")).ToArray();
            CardState? spawner = UpgradeEffects ? host.GetSpawnerCard() : null;
            FieldInfo[] cardFields = typeof(CardState).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            object?[] cardValues = spawner == null ? Array.Empty<object?>() : cardFields.Select(field => field.GetValue(spawner)).ToArray();
            object[] triggers = actors.Select(actor => Get(actor, "triggers")).ToArray();
            bool[] enchanters = actors.Select(actor => actor.IsEnchanter).ToArray();
            bool[] clones = actors.Select(actor => actor.GetIsClone()).ToArray();
            var identities = (Dictionary<CharacterState, Dictionary<CharacterTriggerState, int>>)Get(Trace, "triggerIdentities");
            var originalIds = actors.Select(actor => Trace.TriggerStates(actor).ToDictionary(item => item.Key, item => item.Value)).ToArray();
            bool allow = rooms.AllowEnchantmentUpdates;
            int originalGold = Save.GetGold();
            CardStatistics statistics = Managers.GetCardStatistics();
            int originalLastAttackDamage = (int)Get(statistics, "lastAttackDamageDealt");
            string[] damageFields = UpgradeEffects ? new[] { "damageDealtToMonstersThisTurn", "damageDealtToMonstersThisBattle",
                "friendlyDamageDealtToMonstersThisTurn", "friendlyDamageDealtToMonstersThisBattle" } : Array.Empty<string>();
            object[] originalDamage = damageFields.Select(field => Get(statistics, field)).ToArray();
            var originalDeck = UpgradeEffects ? (Dictionary<CardState, CardStatsEntry>)Get(statistics, "deckStats") : null;
            var samples = new List<object>(); int mismatches = 0;
            try
            {
                foreach (CharacterState actor in actors)
                {
                    Set(actor, "_primaryStateInformation", AccessTools.Method(StateType, "DeepCopyForPreview").Invoke(Get(actor, "_primaryStateInformation"), null));
                    Set(actor, "triggers", new List<CharacterTriggerState>());
                    AccessTools.Property(typeof(CharacterState), "IsEnchanter").SetValue(actor, false);
                    identities[actor] = new Dictionary<CharacterTriggerState, int>();
                }
                object clean = AccessTools.Method(StateType, "DeepCopyForPreview").Invoke(Get(host, "_primaryStateInformation"), null);
                var maximumCases = MaximumCases().ToDictionary(item => item.Label);
                var upgradeCases = UpgradeCases().ToDictionary(item => item.Label);
                var effectCases = UpgradeEffectCases().ToDictionary(item => item.Label);
                foreach (string label in UpgradeEffects ? effectCases.Keys.ToArray() : UnitUpgrades ? upgradeCases.Keys.ToArray() : MaximumHealth ? maximumCases.Keys.ToArray() : new[] { "damage-enters-half", "damage-remains-half", "damage-zero-enters-half", "damage-armor-blocked",
                    "heal-leaves-half", "heal-remains-half", "heal-zero-enters-half", "heal-clipped", "heal-zero-leaves-half",
                    "damage-onhit-heals-first", "heal-onheal-damages-first", "gain-callback-heals-and-loses", "lost-callback-damages-and-gains" })
                {
                    Set(host, "_primaryStateInformation", AccessTools.Method(StateType, "DeepCopyForPreview").Invoke(clean, null));
                    var statuses = (Dictionary<string, CharacterState.StatusEffectStack>)Get(Get(host, "_primaryStateInformation"), "statusEffects");
                    foreach (var status in statuses.Values) status.Count = 0;
                    Set(host, "triggers", new List<CharacterTriggerState>()); identities[host] = new Dictionary<CharacterTriggerState, int>();
                    Set(host, "isClone", clones[Array.IndexOf(actors, host)]);
                    if (UpgradeEffects)
                    {
                        Set(statistics, "deckStats", ShadowDeck(originalDeck!));
                        for (int field = 0; field < damageFields.Length; field++) Set(statistics, damageFields[field], originalDamage[field]);
                        for (int field = 0; field < cardFields.Length; field++)
                        {
                            object? value = cardValues[field];
                            cardFields[field].SetValue(spawner, value is CardStateModifiers ? ShadowLists(value) : CopyList(value));
                        }
                        yield return UpgradeEffectSample(effectCases[label], statuses);
                        continue;
                    }
                    if (UnitUpgrades)
                    {
                        yield return UpgradeSample(upgradeCases[label], statuses);
                        continue;
                    }
                    if (MaximumHealth)
                    {
                        yield return MaximumSample(maximumCases[label], statuses);
                        continue;
                    }
                    // SetHealth is deliberately raw: stale membership controls prove that
                    // the lifecycle API checks even after zero damage/healing.
                    host.SetHealth(11, 20);
                    Add("OnHit", 2, label == "damage-onhit-heals-first" ? "CardEffectHeal" : null);
                    Add("OnHeal", 3, label == "heal-onheal-damages-first" ? "CardEffectDamage" : null);
                    Add("OnDeathwish", 5, label == "gain-callback-heals-and-loses" ? "CardEffectHeal" : null);
                    Add("OnDeathwishLost", 7, label == "lost-callback-damages-and-gains" ? "CardEffectDamage" : null);
                    bool heal = label.StartsWith("heal-", StringComparison.Ordinal) || label.StartsWith("lost-", StringComparison.Ordinal);
                    bool active = label == "heal-leaves-half" || label == "heal-remains-half" || label == "heal-onheal-damages-first" ||
                        label == "lost-callback-damages-and-gains" || label == "heal-zero-leaves-half" || label == "damage-remains-half";
                    if (active) { host.SetHealth(10, 20); AccessTools.Method(typeof(CharacterState), "TryUpdateDeathwish").Invoke(host, null); Queue.Clear(); }
                    int hp = active || label == "damage-zero-enters-half" || label == "heal-zero-enters-half" ? 10 : 11;
                    if (label == "heal-remains-half") hp = 8;
                    if (label == "heal-clipped") hp = 20;
                    if (label == "heal-zero-leaves-half") hp = 11;
                    host.SetHealth(hp, 20);
                    if (label == "damage-armor-blocked")
                    {
                        if (statuses.TryGetValue("armor", out var armor)) armor.Count = 3;
                        else statuses.Add("armor", new CharacterState.StatusEffectStack(Managers.GetStatusEffectManager().Create("armor", host), 3));
                    }
                    int amount = label.Contains("zero") ? 0 : 1;
                    RoomCombatState before = Trace.Capture(host.GetCurrentRoom()); object rngBefore = Rng();
                    RoomCombatResult result = heal ? RoomCombatModel.ApplyCardHeal(before, Trace.UnitId(host), amount, captureAllDispatches: true) :
                        RoomCombatModel.ApplyCardDamage(before, Trace.UnitId(host), amount, captureAllDispatches: true);
                    dispatches.Clear(); recording = true;
                    try
                    {
                        if (heal) yield return host.ApplyHeal(amount);
                        else yield return Managers.GetCombatManager().ApplyDamageToTarget(amount, host,
                            new CombatManager.ApplyDamageToTargetParameters { damageType = Damage.Type.Default, finalEffectInSequence = true });
                    }
                    finally { recording = false; }
                    if (actors.Any(actor => !actor.IsAlive || actor.IsDestroyed) || Queue.Count != 0)
                        throw new InvalidOperationException("Half-health lifecycle calibration left a dead actor or pending queue.");
                    RoomCombatState actual = Trace.Capture(host.GetCurrentRoom()); object rngAfter = Rng();
                    bool gameplayRngUnchanged = JToken.DeepEquals(GameplayRng(rngBefore), GameplayRng(rngAfter));
                    string? difference = result.UnsupportedReason ?? Difference(result.State, actual) ?? Difference(result.Dispatches, dispatches) ??
                        (gameplayRngUnchanged ? null : "Native gameplay RNG or Unity RNG changed.");
                    if (difference != null) { mismatches++; log.LogWarning(label + ": " + difference); }
                    samples.Add(new { Label = label, Operation = heal ? "Heal" : "Damage", Amount = amount, ActorId = Trace.UnitId(host),
                        Before = before, Actual = actual, Dispatches = dispatches.ToArray(), RngBefore = rngBefore, RngAfter = rngAfter, GameplayRngUnchanged = gameplayRngUnchanged,
                        Predicted = difference == null ? null : result.State, Difference = difference, Completed = true });
                }
            }
            finally
            {
                recording = false; Queue.Clear(); Set(rooms, "_allowEnchantmentUpdates", false);
                for (int index = 0; index < actors.Length; index++)
                {
                    Set(actors[index], "_primaryStateInformation", states[index]); Set(actors[index], "triggers", triggers[index]);
                    AccessTools.Property(typeof(CharacterState), "IsEnchanter").SetValue(actors[index], enchanters[index]); identities[actors[index]] = originalIds[index];
                    Set(actors[index], "isClone", clones[index]);
                }
                Set(statistics, "lastAttackDamageDealt", originalLastAttackDamage);
                if (originalDeck != null) Set(statistics, "deckStats", originalDeck);
                for (int field = 0; field < damageFields.Length; field++) Set(statistics, damageFields[field], originalDamage[field]);
                if (spawner != null)
                    for (int field = 0; field < cardFields.Length; field++) cardFields[field].SetValue(spawner, cardValues[field]);
                Save.SetGold(originalGold); Set(rooms, "_allowEnchantmentUpdates", allow);
                foreach (var pair in originalStreams) NativeRng(pair.Key).Init(pair.Value);
                UnityEngine.Random.state = originalUnity;
            }
            string? restoration = Difference(original, Trace.CaptureTrain()) ?? Difference(originalRng, Rng());
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId,
                Boundary = UpgradeEffects ? "NativeHalfHealthUnitUpgradeEffects" : UnitUpgrades ? "NativeHalfHealthUnitUpgradeLifecycle" : MaximumHealth ? "NativeHalfHealthMaximumHealthLifecycle" : "NativeHalfHealthDamageHealLifecycle",
                LiveContextUnchanged = restoration == null, RestorationDifference = restoration, EffectsDrained = true,
                RngBefore = originalRng, RngAfter = Rng(), CosmeticStreams = new[] { "NonDeterministic", "Chatter" }, Mismatches = mismatches, Samples = samples }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                UpgradeEffects ? "half-health-upgrade-effects.mt2f" : UnitUpgrades ? "half-health-upgrade.mt2f" : MaximumHealth ? "half-health-maximum.mt2f" : "half-health-lifecycle.mt2f"))) document.Write(output);
            if (restoration != null || mismatches != 0) throw new InvalidOperationException("Half-health lifecycle differs: " + mismatches + "; restoration: " + restoration);

            IEnumerator MaximumSample(MaximumInput input, Dictionary<string, CharacterState.StatusEffectStack> statuses)
            {
                host.SetHealth(11, 20);
                Add("OnHit", 2, null);
                Add("OnHeal", 3, input.Label == "buff-onheal-damages-first" ? "CardEffectDamage" : null);
                Add("OnDeathwish", 5, input.Label == "buff-gain-heals-loses" ? "CardEffectHeal" :
                    input.Label == "buff-gain-adds-maxhealth" ? "CardEffectBuffMaxHealth" : null);
                Add("OnDeathwishLost", 7, input.Label == "debuff-lost-damages-gains" ? "CardEffectDamage" : null);
                if (input.Active) { host.SetHealth(10, 20); AccessTools.Method(typeof(CharacterState), "TryUpdateDeathwish").Invoke(host, null); Queue.Clear(); }
                host.SetHealth(input.Hp, input.MaxHp);
                Set(Get(host, "_primaryStateInformation"), "canBeHealed", input.CanHeal);
                if (input.Status != null)
                {
                    if (statuses.TryGetValue(input.Status, out var status)) status.Count = 1;
                    else statuses.Add(input.Status, new CharacterState.StatusEffectStack(Managers.GetStatusEffectManager().Create(input.Status, host), 1));
                }
                RoomCombatState before = Trace.Capture(host.GetCurrentRoom()); object rngBefore = Rng();
                RoomCombatResult predicted = UnitHealthModel.ApplyNative(before, Trace.UnitId(host), input.Amount, input.Debuff,
                    input.Heal, input.TriggerOnHeal, input.Floor, input.DecreaseHp, captureAllDispatches: true);
                dispatches.Clear(); recording = true;
                RoomCombatState apiActual; object rngAfterApi; CharacterTriggerDispatch[] apiDispatches; EnchantmentCallback[] apiCallbacks;
                try
                {
                    if (input.Debuff) yield return host.DebuffMaxHP(input.Amount, input.Floor, input.DecreaseHp);
                    else yield return host.BuffMaxHP(input.Amount, input.TriggerOnHeal, heal: input.Heal);
                    apiActual = Trace.Capture(host.GetCurrentRoom()); rngAfterApi = Rng(); apiDispatches = dispatches.ToArray();
                    apiCallbacks = Queue.Select(item => new EnchantmentCallback(item.character.GetCurrentRoomIndex(), Trace.UnitId(item.character),
                        item.trigger.ToString(), item.fireTriggersData?.paramInt ?? 0, item.fireTriggersData?.paramInt2 ?? 0,
                        item.fireTriggersData?.paramString, item.fireTriggersData?.overrideTargetCharacter == null ? (int?)null : Trace.UnitId(item.fireTriggersData.overrideTargetCharacter),
                        item.dyingCharacter == null ? (int?)null : Trace.UnitId(item.dyingCharacter), item.canFireTriggers,
                        item.exclusiveTrigger == null ? (int?)null : Trace.TriggerStateId(item.character, item.exclusiveTrigger))).ToArray();
                    yield return Managers.GetCombatManager().RunTriggerQueue();
                }
                finally { recording = false; }
                RoomCombatState actual = Trace.Capture(host.GetCurrentRoom()); object rngAfter = Rng();
                if (actors.Any(actor => !actor.IsAlive || actor.IsDestroyed) || Queue.Count != 0)
                    throw new InvalidOperationException("Maximum-health lifecycle left a dead actor or pending queue.");
                string? difference = predicted.UnsupportedReason ?? Difference(predicted.State, apiActual) ??
                    Difference(predicted.Dispatches, apiDispatches) ?? Difference(predicted.PendingCallbacks.Select(EnchantmentCallback.From).ToArray(), apiCallbacks);
                RoomCombatResult? drained = predicted.Supported ? RoomCombatModel.DrainCharacterCallbacks(predicted.State!, predicted.PendingCallbacks, true) : null;
                difference ??= drained?.UnsupportedReason ?? (drained == null ? "Missing maximum-health successor" : Difference(drained.State, actual) ??
                    Difference(predicted.Dispatches.Concat(drained.Dispatches).ToArray(), dispatches));
                bool gameplayRngUnchanged = JToken.DeepEquals(GameplayRng(rngBefore), GameplayRng(rngAfterApi)) &&
                    JToken.DeepEquals(GameplayRng(rngBefore), GameplayRng(rngAfter));
                difference ??= gameplayRngUnchanged ? null : "Native gameplay RNG or Unity RNG changed.";
                if (difference != null) { mismatches++; log.LogWarning(input.Label + ": " + difference); }
                samples.Add(new { Label = input.Label, Input = input, ActorId = Trace.UnitId(host), Before = before,
                    ApiActual = apiActual, ApiCallbacks = apiCallbacks, ApiDispatches = apiDispatches,
                    Actual = actual, Dispatches = dispatches.ToArray(), RngBefore = rngBefore, RngAfterApi = rngAfterApi, RngAfter = rngAfter,
                    GameplayRngUnchanged = gameplayRngUnchanged, PendingAfterDrain = Queue.Count,
                    PredictedApi = difference == null ? null : predicted.State, Predicted = difference == null ? null : drained?.State,
                    Difference = difference, Completed = true });
            }

            IEnumerator UpgradeSample(UpgradeInput input, Dictionary<string, CharacterState.StatusEffectStack> statuses)
            {
                var upgrade = new CardUpgradeState(); upgrade.Setup();
                upgrade.SetAttackDamage(2); upgrade.SetAdditionalHP(input.Health);
                Set(upgrade, "additionalUnhealedHP", input.Unhealed);
                const string upgradeId = "half-health-upgrade";
                // Negative upgrades must be installed on a safe preparation state;
                // authored HP and membership are established only after installation.
                host.SetHealth(1000, 1000);
                if (input.Remove) yield return host.ApplyCardUpgrade(upgrade, upgradeId: upgradeId);
                host.SetHealth(11, 20);
                Add("OnHit", 2, null); Add("OnHeal", 3, null);
                Add("OnDeathwish", 5, input.GainEffect);
                Add("OnDeathwishLost", 7, input.LostEffect);
                if (input.Active) { host.SetHealth(10, 20); AccessTools.Method(typeof(CharacterState), "TryUpdateDeathwish").Invoke(host, null); Queue.Clear(); }
                host.SetHealth(input.Hp, input.MaxHp);
                Set(Get(host, "_primaryStateInformation"), "canBeHealed", input.CanHeal);
                if (input.AssociatedHealth != 0)
                    ((Dictionary<string, int>)Get(Get(host, "_primaryStateInformation"), "maxHpFromUpgrades"))[upgradeId] = input.AssociatedHealth;
                if (input.Status != null)
                {
                    if (statuses.TryGetValue(input.Status, out var status)) status.Count = 1;
                    else statuses.Add(input.Status, new CharacterState.StatusEffectStack(Managers.GetStatusEffectManager().Create(input.Status, host), 1));
                }
                int? anonymousIndex = input.Remove ? (int?)host.GetAppliedCardUpgrades().ToList().FindIndex(item => ReferenceEquals(item, upgrade)) : null;
                CardUpgradeModifier definition = CardModifierProbe.Upgrade(upgrade);
                RoomCombatState before = Trace.Capture(host.GetCurrentRoom()); object rngBefore = Rng();
                RoomCombatResult predicted = RoomCombatModel.ApplyDirectUnitUpgrade(before, Trace.UnitId(host), definition, input.Remove,
                    upgradeId, anonymousIndex, captureAllDispatches: true);
                dispatches.Clear(); recording = true;
                RoomCombatState apiActual; object rngAfterApi; CharacterTriggerDispatch[] apiDispatches; EnchantmentCallback[] apiCallbacks;
                try
                {
                    yield return input.Remove ? host.RemoveCardUpgrade(upgrade, upgradeId) : host.ApplyCardUpgrade(upgrade, upgradeId: upgradeId);
                    apiActual = Trace.Capture(host.GetCurrentRoom()); rngAfterApi = Rng(); apiDispatches = dispatches.ToArray();
                    apiCallbacks = Queue.Select(item => new EnchantmentCallback(item.character.GetCurrentRoomIndex(), Trace.UnitId(item.character),
                        item.trigger.ToString(), item.fireTriggersData?.paramInt ?? 0, item.fireTriggersData?.paramInt2 ?? 0,
                        item.fireTriggersData?.paramString, item.fireTriggersData?.overrideTargetCharacter == null ? (int?)null : Trace.UnitId(item.fireTriggersData.overrideTargetCharacter),
                        item.dyingCharacter == null ? (int?)null : Trace.UnitId(item.dyingCharacter), item.canFireTriggers,
                        item.exclusiveTrigger == null ? (int?)null : Trace.TriggerStateId(item.character, item.exclusiveTrigger))).ToArray();
                    yield return Managers.GetCombatManager().RunTriggerQueue();
                }
                finally { recording = false; }
                RoomCombatState actual = Trace.Capture(host.GetCurrentRoom()); object rngAfter = Rng();
                if (actors.Any(actor => !actor.IsAlive || actor.IsDestroyed) || Queue.Count != 0)
                    throw new InvalidOperationException("Upgrade lifecycle left a dead actor or pending queue.");
                string? difference = predicted.UnsupportedReason ?? Difference(predicted.State, apiActual) ?? Difference(predicted.Dispatches, apiDispatches) ??
                    Difference(predicted.PendingCallbacks.Select(EnchantmentCallback.From).ToArray(), apiCallbacks);
                RoomCombatResult? drained = predicted.Supported ? RoomCombatModel.DrainCharacterCallbacks(predicted.State!, predicted.PendingCallbacks, true) : null;
                difference ??= drained?.UnsupportedReason ?? (drained == null ? "Missing upgrade successor" : Difference(drained.State, actual) ??
                    Difference(predicted.Dispatches.Concat(drained.Dispatches).ToArray(), dispatches));
                bool gameplayRngUnchanged = JToken.DeepEquals(GameplayRng(rngBefore), GameplayRng(rngAfterApi)) && JToken.DeepEquals(GameplayRng(rngBefore), GameplayRng(rngAfter));
                difference ??= gameplayRngUnchanged ? null : "Native gameplay RNG or Unity RNG changed.";
                if (difference != null) { mismatches++; log.LogWarning(input.Label + ": " + difference); }
                samples.Add(new { Label = input.Label, Input = input, Upgrade = definition, UpgradeId = upgradeId, AnonymousRemovalIndex = anonymousIndex,
                    ActorId = Trace.UnitId(host), Before = before, ApiActual = apiActual, ApiCallbacks = apiCallbacks, ApiDispatches = apiDispatches,
                    Actual = actual, Dispatches = dispatches.ToArray(), RngBefore = rngBefore, RngAfterApi = rngAfterApi, RngAfter = rngAfter,
                    GameplayRngUnchanged = gameplayRngUnchanged, PendingAfterDrain = Queue.Count,
                    PredictedApi = difference == null ? null : predicted.State, Predicted = difference == null ? null : drained?.State, Difference = difference, Completed = true });
                log.LogInfo("HALF-HEALTH-UPGRADE " + input.Label + " " + (difference ?? "MATCH"));
            }

            IEnumerator UpgradeEffectSample(UpgradeEffectInput input, Dictionary<string, CharacterState.StatusEffectStack> statuses)
            {
                CardUpgradeData data = DynamicUpgradeScenario.Upgrade("PojuHalfHealthEffectUpgrade", "3e33dcf7-5235-4387-9e3e-bb115e501001",
                    2, input.Health, 0, input.Unhealed, "armor", 0);
                Set(data, "statusEffectUpgrades", new List<StatusEffectStackData>());
                Set(data, "isUnique", input.Unique); Set(data, "excludeFromClones", input.ExcludeClone);
                var upgrade = new CardUpgradeState(); upgrade.Setup(data);
                CardEffectState addition = UpgradeEffect(data, false, input.Lifetime);
                CardEffectState operation = input.Remove ? UpgradeEffect(data, true, input.Lifetime) : addition;
                CardEffectParams parameters = new CardEffectParams { targets = new List<CharacterState> { host }, selectedRoom = host.GetCurrentRoomIndex() };
                host.SetHealth(1000, 1000);
                for (int copy = 0; copy < input.Copies; copy++)
                    yield return addition.GetCardEffect().ApplyEffect(addition, parameters, Managers.GetCoreManagers(), Managers.GetSystemManagers());
                if (input.CardOnly) spawner!.GetTemporaryCardStateModifiers().AddUpgrade(upgrade);
                host.SetHealth(11, 20);
                Add("OnHit", 2, null); Add("OnHeal", 3, null);
                Add("OnDeathwish", 5, input.GainEffect, data, input.Lifetime);
                Add("OnDeathwishLost", 7, input.LostEffect, data, input.Lifetime);
                if (input.Active) { host.SetHealth(10, 20); AccessTools.Method(typeof(CharacterState), "TryUpdateDeathwish").Invoke(host, null); Queue.Clear(); }
                host.SetHealth(input.Hp, input.MaxHp);
                Set(Get(host, "_primaryStateInformation"), "canBeHealed", input.CanHeal);
                if (input.Clone) host.SetIsClone();
                if (input.AssociatedHealth != 0)
                    ((Dictionary<string, int>)Get(Get(host, "_primaryStateInformation"), "maxHpFromUpgrades"))[data.GetID()] = input.AssociatedHealth;
                if (input.Status != null)
                {
                    if (statuses.TryGetValue(input.Status, out var status)) status.Count = 1;
                    else statuses.Add(input.Status, new CharacterState.StatusEffectStack(Managers.GetStatusEffectManager().Create(input.Status, host), 1));
                }
                CardUpgradeModifier definition = CardModifierProbe.Upgrade(upgrade);
                RoomCombatState before = Trace.Capture(host.GetCurrentRoom()); object rngBefore = Rng();
                RoomCombatResult predicted = UnitModifierModel.Apply(before, Trace.UnitId(host), definition, input.Lifetime, input.Remove,
                    captureAllDispatches: true);
                dispatches.Clear(); recording = true;
                RoomCombatState apiActual; object rngAfterApi; CharacterTriggerDispatch[] apiDispatches; EnchantmentCallback[] apiCallbacks;
                try
                {
                    yield return operation.GetCardEffect().ApplyEffect(operation, parameters, Managers.GetCoreManagers(), Managers.GetSystemManagers());
                    apiActual = Trace.Capture(host.GetCurrentRoom()); rngAfterApi = Rng(); apiDispatches = dispatches.ToArray();
                    apiCallbacks = Queue.Select(item => new EnchantmentCallback(item.character.GetCurrentRoomIndex(), Trace.UnitId(item.character),
                        item.trigger.ToString(), item.fireTriggersData?.paramInt ?? 0, item.fireTriggersData?.paramInt2 ?? 0,
                        item.fireTriggersData?.paramString, item.fireTriggersData?.overrideTargetCharacter == null ? (int?)null : Trace.UnitId(item.fireTriggersData.overrideTargetCharacter),
                        item.dyingCharacter == null ? (int?)null : Trace.UnitId(item.dyingCharacter), item.canFireTriggers,
                        item.exclusiveTrigger == null ? (int?)null : Trace.TriggerStateId(item.character, item.exclusiveTrigger))).ToArray();
                    yield return Managers.GetCombatManager().RunTriggerQueue();
                }
                finally { recording = false; }
                RoomCombatState actual = Trace.Capture(host.GetCurrentRoom()); object rngAfter = Rng();
                if (actors.Any(actor => !actor.IsAlive || actor.IsDestroyed) || Queue.Count != 0)
                    throw new InvalidOperationException("Upgrade effect left a dead actor or pending queue.");
                string? difference = predicted.UnsupportedReason ?? Difference(predicted.State, apiActual) ?? Difference(predicted.Dispatches, apiDispatches) ??
                    Difference(predicted.PendingCallbacks.Select(EnchantmentCallback.From).ToArray(), apiCallbacks);
                RoomCombatResult? drained = predicted.Supported ? RoomCombatModel.DrainCharacterCallbacks(predicted.State!, predicted.PendingCallbacks, true) : null;
                difference ??= drained?.UnsupportedReason ?? (drained == null ? "Missing upgrade effect successor" : Difference(drained.State, actual) ??
                    Difference(predicted.Dispatches.Concat(drained.Dispatches).ToArray(), dispatches));
                bool gameplayRngUnchanged = JToken.DeepEquals(GameplayRng(rngBefore), GameplayRng(rngAfterApi)) && JToken.DeepEquals(GameplayRng(rngBefore), GameplayRng(rngAfter));
                difference ??= gameplayRngUnchanged ? null : "Native gameplay RNG or Unity RNG changed.";
                if (difference != null) { mismatches++; log.LogWarning(input.Label + ": " + difference); }
                samples.Add(new { Label = input.Label, Input = input, Upgrade = definition, ActorId = Trace.UnitId(host),
                    SpawnerId = Trace.CardId(spawner!), Before = before, ApiActual = apiActual, ApiCallbacks = apiCallbacks, ApiDispatches = apiDispatches,
                    Actual = actual, Dispatches = dispatches.ToArray(), RngBefore = rngBefore, RngAfterApi = rngAfterApi, RngAfter = rngAfter,
                    GameplayRngUnchanged = gameplayRngUnchanged, PendingAfterDrain = Queue.Count,
                    PredictedApi = difference == null ? null : predicted.State, Predicted = difference == null ? null : drained?.State, Difference = difference, Completed = true });
                log.LogInfo("HALF-HEALTH-UPGRADE-EFFECT " + input.Label + " " + (difference ?? "MATCH"));
            }

            void Add(string kind, int gold, string? effectType, CardUpgradeData? upgrade = null, string lifetime = "TemporaryUntilEndOfBattle")
            {
                CharacterTriggerData data = HealingScenario.HealGold(gold, effectType != null, false);
                Set(data, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), kind));
                if (effectType != null)
                {
                    var effect = new CardEffectData(effectType == "RemoveUpgrade" ? "CardEffectRemoveTempUpgradeFromUnit" :
                        effectType == "AddUpgrade" ? "CardEffectAddCardUpgradeToUnits" : effectType, null!, host.GetTeamType());
                    effect.Cheat_SetTargetMode(TargetMode.Self); Set(effect, "paramInt", 1);
                    if (upgrade != null && effectType is "RemoveUpgrade" or "AddUpgrade")
                    { Set(effect, "paramCardUpgradeData", upgrade); Set(effect, "additionalParamInt1", (int)Enum.Parse(typeof(UnitUpgradeLifetime), lifetime)); }
                    Set(data, "effects", data.GetEffects().Concat(new[] { effect }).ToList());
                }
                var trigger = new CharacterTriggerState(); trigger.Setup(data, host.GetTeamType(), Save); host.GetTriggers().Add(trigger);
            }
        }
        internal static object? CopyList(object? value)
        {
            if (value is Array array) return array.Clone();
            if (!(value is IList list)) return value;
            var copy = (IList)Activator.CreateInstance(value.GetType()); foreach (object item in list) copy.Add(item); return copy;
        }
        internal static object ShadowLists(object value)
        {
            object copy = AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(value, null);
            foreach (FieldInfo field in value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                if (field.GetValue(value) is IList list) field.SetValue(copy, CopyList(list));
            return copy;
        }
        internal static Dictionary<CardState, CardStatsEntry> ShadowDeck(Dictionary<CardState, CardStatsEntry> values) => values.ToDictionary(pair => pair.Key, pair =>
        {
            var copy = new CardStatsEntry();
            var entries = (Dictionary<CardStatistics.EntryDuration, Dictionary<CardStatistics.TrackedValueType, int>>)Get(pair.Value, "entryValues");
            Set(copy, "entryValues", entries.ToDictionary(item => item.Key, item => new Dictionary<CardStatistics.TrackedValueType, int>(item.Value)));
            return copy;
        });
        private static CardEffectState UpgradeEffect(CardUpgradeData upgrade, bool remove, string lifetime)
        {
            var data = new CardEffectData(remove ? "CardEffectRemoveTempUpgradeFromUnit" :
                lifetime == "Permanent" ? "CardEffectAddCardUpgradeToUnits" : "CardEffectAddTempCardUpgradeToUnits", null!, Team.Type.Monsters);
            data.Cheat_SetTargetMode(TargetMode.Self); Set(data, "paramCardUpgradeData", upgrade);
            Set(data, "additionalParamInt1", (int)Enum.Parse(typeof(UnitUpgradeLifetime), lifetime));
            var effect = new CardEffectState(); effect.Setup(data, Team.Type.Monsters, Save); return effect;
        }

        private sealed class UpgradeEffectInput : UpgradeInput
        {
            public string Lifetime { get; }
            public int Copies { get; }
            public bool Unique { get; }
            public bool ExcludeClone { get; }
            public bool Clone { get; }
            public bool CardOnly { get; }
            internal UpgradeEffectInput(string label, int hp, bool active, int health, int unhealed = 0, bool remove = false,
                string lifetime = "TemporaryUntilEndOfBattle", int copies = 0, bool unique = false, bool excludeClone = false,
                bool clone = false, bool cardOnly = false, int associatedHealth = 0, bool canHeal = true, string? status = null,
                string? gainEffect = null, string? lostEffect = null)
                : base(label, hp, active, health, unhealed, remove, associatedHealth, canHeal, status, gainEffect, lostEffect)
            { Lifetime = lifetime; Copies = copies; Unique = unique; ExcludeClone = excludeClone; Clone = clone; CardOnly = cardOnly; }
        }
        private static UpgradeEffectInput[] UpgradeEffectCases() => new[] {
            new UpgradeEffectInput("temporary-add-healed-loss", 10, true, 2),
            new UpgradeEffectInput("permanent-add-healed-loss", 10, true, 2, lifetime: "Permanent"),
            new UpgradeEffectInput("death-only-add-healed-loss", 10, true, 2, lifetime: "TemporaryUntilUnitDeath"),
            new UpgradeEffectInput("temporary-add-unhealed-gain", 11, false, 0, 2),
            new UpgradeEffectInput("temporary-add-two-phase-loss-gain", 10, true, 2, 4),
            new UpgradeEffectInput("temporary-add-negative-gain", 11, false, -2),
            new UpgradeEffectInput("temporary-add-gain-heals", 11, false, 0, 2, gainEffect: "CardEffectHeal"),
            new UpgradeEffectInput("temporary-add-loss-removes-before-write", 10, true, 2, lostEffect: "RemoveUpgrade"),
            new UpgradeEffectInput("permanent-add-loss-removes-before-write", 10, true, 2, lifetime: "Permanent", lostEffect: "RemoveUpgrade"),
            new UpgradeEffectInput("temporary-add-gain-buffs-max", 11, false, 0, 2, gainEffect: "CardEffectBuffMaxHealth"),
            new UpgradeEffectInput("temporary-remove-healed-loss", 10, true, 2, remove: true, copies: 1),
            new UpgradeEffectInput("permanent-remove-keeps-permanent-card-record", 10, true, 2, remove: true, lifetime: "Permanent", copies: 1),
            new UpgradeEffectInput("temporary-remove-two-copies", 10, true, 2, remove: true, copies: 2),
            new UpgradeEffectInput("temporary-remove-two-negative-copies", 10, true, -2, remove: true, copies: 2),
            new UpgradeEffectInput("temporary-remove-unhealed-gain", 11, false, 0, -2, remove: true, copies: 1),
            new UpgradeEffectInput("temporary-remove-loss-readds-after-write", 10, true, 2, remove: true, copies: 1, lostEffect: "AddUpgrade"),
            new UpgradeEffectInput("temporary-remove-restoration-readds-before-write", 10, true, -2, remove: true, copies: 1, lostEffect: "AddUpgrade"),
            new UpgradeEffectInput("unique-repeat-stale-half", 10, false, 2, copies: 1, unique: true),
            new UpgradeEffectInput("excluded-clone-noop", 10, true, 2, excludeClone: true, clone: true),
            new UpgradeEffectInput("unique-add-loss", 10, true, 2, unique: true),
            new UpgradeEffectInput("remove-missing-unit-clears-card", 10, true, 2, remove: true, cardOnly: true),
            new UpgradeEffectInput("temporary-add-cannotheal", 11, false, 2, canHeal: false),
            new UpgradeEffectInput("temporary-add-heal-multiplier", 8, true, 3, status: "heal multiplier"),
            new UpgradeEffectInput("temporary-add-ignores-heal-immunity", 10, true, 2, status: "heal immunity"),
            new UpgradeEffectInput("temporary-remove-zero-stale-half", 10, false, 0, remove: true, copies: 1),
            new UpgradeEffectInput("death-only-remove-loss", 10, true, 2, remove: true, lifetime: "TemporaryUntilUnitDeath", copies: 1),
            new UpgradeEffectInput("temporary-add-loss-damages-before-second", 10, true, 2, 4, lostEffect: "CardEffectDamage"),
            new UpgradeEffectInput("temporary-remove-ledger-gain-loss", 11, false, 0, -2, remove: true, copies: 1, associatedHealth: 4) };

        private class UpgradeInput
        {
            public string Label { get; }
            public int Hp { get; }
            public int MaxHp { get; }
            public bool Active { get; }
            public int Health { get; }
            public int Unhealed { get; }
            public bool Remove { get; }
            public int AssociatedHealth { get; }
            public bool CanHeal { get; }
            public string? Status { get; }
            public string? GainEffect { get; }
            public string? LostEffect { get; }
            internal UpgradeInput(string label, int hp, bool active, int health, int unhealed = 0, bool remove = false,
                int associatedHealth = 0, bool canHeal = true, string? status = null, string? gainEffect = null, string? lostEffect = null, int maxHp = 20)
            { Label = label; Hp = hp; MaxHp = maxHp; Active = active; Health = health; Unhealed = unhealed;
                Remove = remove; AssociatedHealth = associatedHealth; CanHeal = canHeal; Status = status; GainEffect = gainEffect; LostEffect = lostEffect; }
        }
        private static UpgradeInput[] UpgradeCases() => new[] {
            new UpgradeInput("add-heal-leaves-half", 10, true, 2),
            new UpgradeInput("add-unhealed-enters-half", 11, false, 0, 2),
            new UpgradeInput("add-negative-enters-half", 11, false, -2),
            new UpgradeInput("add-negative-unhealed-enters-half", 11, false, 0, -2),
            new UpgradeInput("add-two-phase-loss-gain", 10, true, 2, 4),
            new UpgradeInput("add-gain-heals-after-second", 11, false, -2, 2, gainEffect: "CardEffectHeal"),
            new UpgradeInput("add-loss-damages-before-second", 10, true, 2, 4, lostEffect: "CardEffectDamage"),
            new UpgradeInput("add-zero-stale-half", 10, false, 0),
            new UpgradeInput("add-cannotheal-enters-half", 11, false, 2, canHeal: false),
            new UpgradeInput("add-ignores-heal-immunity", 10, true, 2, status: "heal immunity"),
            new UpgradeInput("add-heal-multiplier", 8, true, 3, status: "heal multiplier"),
            new UpgradeInput("add-capped", 49999, true, 2, 2, maxHp: 99999),
            new UpgradeInput("add-two-phase-remains-half", 5, true, 2, 4),
            new UpgradeInput("remove-healed-leaves-half", 10, true, 2, remove: true),
            new UpgradeInput("remove-unhealed-leaves-half", 10, true, 0, 2, remove: true),
            new UpgradeInput("remove-negative-healed-leaves-half", 10, true, -2, remove: true),
            new UpgradeInput("remove-negative-unhealed-enters-half", 11, false, 0, -2, remove: true),
            new UpgradeInput("remove-two-phase-enters-half", 11, false, -2, -4, remove: true),
            new UpgradeInput("remove-two-phase-loss-gain", 10, true, 2, -4, remove: true),
            new UpgradeInput("remove-loss-damages-before-second", 10, true, -2, -4, remove: true, lostEffect: "CardEffectDamage"),
            new UpgradeInput("remove-associated-health", 10, true, 0, remove: true, associatedHealth: 2),
            new UpgradeInput("remove-ledger-after-gain-loss", 11, false, 0, -2, remove: true, associatedHealth: 4),
            new UpgradeInput("remove-three-phase-loss-gain-loss", 10, true, -2, -4, remove: true, associatedHealth: 4),
            new UpgradeInput("remove-cannotheal-enters-half", 11, false, -2, remove: true, canHeal: false),
            new UpgradeInput("remove-zero-stale-half", 10, false, 0, remove: true),
            new UpgradeInput("add-gain-buffs-max-after-second", 11, false, -2, 2, gainEffect: "CardEffectBuffMaxHealth"),
            new UpgradeInput("remove-gain-buffs-max-and-loses", 11, false, 0, -2, remove: true, gainEffect: "CardEffectBuffMaxHealth") };

        private sealed class MaximumInput
        {
            public string Label { get; }
            public int Hp { get; }
            public int MaxHp { get; }
            public bool Active { get; }
            public int Amount { get; }
            public bool Debuff { get; }
            public bool Heal { get; }
            public bool TriggerOnHeal { get; }
            public int Floor { get; }
            public bool DecreaseHp { get; }
            public bool CanHeal { get; }
            public string? Status { get; }
            internal MaximumInput(string label, int hp, int maxHp, bool active, int amount, bool debuff = false, bool heal = true,
                bool triggerOnHeal = false, int floor = 0, bool decreaseHp = true, bool canHeal = true, string? status = null)
            { Label = label; Hp = hp; MaxHp = maxHp; Active = active; Amount = amount; Debuff = debuff; Heal = heal;
                TriggerOnHeal = triggerOnHeal; Floor = floor; DecreaseHp = decreaseHp; CanHeal = canHeal; Status = status; }
        }
        private static MaximumInput[] MaximumCases() => new[] {
            new MaximumInput("buff-heal-leaves-half", 10, 20, true, 2),
            new MaximumInput("buff-heal-remains-half", 5, 20, true, 2),
            new MaximumInput("buff-noheal-enters-half", 11, 20, false, 2, heal: false),
            new MaximumInput("buff-noheal-leaves-stale-half", 12, 20, true, 2, heal: false),
            new MaximumInput("buff-zero", 10, 20, false, 0),
            new MaximumInput("buff-negative", 10, 20, false, -1),
            new MaximumInput("debuff-enters-half", 11, 20, false, 2, debuff: true),
            new MaximumInput("debuff-remains-half", 9, 20, true, 2, debuff: true),
            new MaximumInput("debuff-nohp-leaves-half", 10, 20, true, 2, debuff: true, decreaseHp: false),
            new MaximumInput("debuff-clamps-hp", 19, 20, false, 4, debuff: true, decreaseHp: false),
            new MaximumInput("debuff-zero", 10, 20, false, 0, debuff: true),
            new MaximumInput("debuff-negative", 10, 20, false, -2, debuff: true),
            new MaximumInput("buff-capped", 49999, 99999, true, 2),
            new MaximumInput("buff-capped-noheal", 50000, 99999, false, 2, heal: false),
            new MaximumInput("buff-ignores-heal-immunity", 10, 20, true, 2, status: "heal immunity"),
            new MaximumInput("buff-cannotheal-enters-half", 11, 20, false, 2, canHeal: false),
            new MaximumInput("buff-onheal-damages-first", 10, 20, true, 2, triggerOnHeal: true),
            new MaximumInput("buff-gain-heals-loses", 11, 20, false, 2, heal: false),
            new MaximumInput("debuff-lost-damages-gains", 10, 20, true, 2, debuff: true, decreaseHp: false),
            new MaximumInput("debuff-floor-nohp", 10, 20, true, 30, debuff: true, floor: 6, decreaseHp: false),
            new MaximumInput("buff-onheal-before-loss", 10, 20, true, 2, triggerOnHeal: true),
            new MaximumInput("buff-heal-multiplier", 8, 20, true, 3, status: "heal multiplier"),
            new MaximumInput("buff-gain-adds-maxhealth", 11, 20, false, 2, heal: false) };
        internal static string? Difference(object? left, object? right) => Difference(left == null ? JValue.CreateNull() : JToken.FromObject(left),
            right == null ? JValue.CreateNull() : JToken.FromObject(right), "$");
        private static string? Difference(JToken left, JToken right, string path)
        {
            if (JToken.DeepEquals(left, right)) return null;
            if (left is JObject a && right is JObject b)
            {
                foreach (string name in a.Properties().Select(property => property.Name).Union(b.Properties().Select(property => property.Name)))
                { string? value = Difference(a[name] ?? JValue.CreateNull(), b[name] ?? JValue.CreateNull(), path + "." + name); if (value != null) return value; }
            }
            if (left is JArray x && right is JArray y && x.Count == y.Count)
                for (int index = 0; index < x.Count; index++) { string? value = Difference(x[index], y[index], path + "[" + index + "]"); if (value != null) return value; }
            return path + ": model=" + left.ToString(Newtonsoft.Json.Formatting.None) + ", native=" + right.ToString(Newtonsoft.Json.Formatting.None);
        }

        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class DispatchPatch
        {
            private static void Prefix(CharacterState __instance, CharacterTriggerData.Trigger trigger, CharacterState dyingCharacter,
                bool fromRunningTriggerQueue, CharacterState.FireTriggersData fireTriggersData, int triggerCount, CharacterTriggerState exclusiveTrigger)
            {
                if (!recording || !fromRunningTriggerQueue) return;
                dispatches.Add(new CharacterTriggerDispatch(Trace.UnitId(__instance), trigger.ToString(), dyingCharacter == null ? 0 : Trace.UnitId(dyingCharacter),
                    triggerCount, fireTriggersData?.paramInt ?? 0, fireTriggersData?.paramInt2 ?? 0, fireTriggersData?.paramString,
                    fireTriggersData?.overrideTargetCharacter == null ? 0 : Trace.UnitId(fireTriggersData.overrideTargetCharacter),
                    exclusiveTriggerStateId: exclusiveTrigger == null ? (int?)null : Trace.TriggerStateId(__instance, exclusiveTrigger)));
            }
        }
    }
}
