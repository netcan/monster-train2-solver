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
    internal static class ZealStatusCalibration
    {
        internal const string OwnerId = "ee429978-fa41-4c66-93c6-3d3c7d5a4733";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ZEAL_STATUS") == "1";
        internal static bool Horde => Environment.GetEnvironmentVariable("MT2_PROBE_ZEAL_HORDE") == "1";
        internal static bool HordeMaximum => Environment.GetEnvironmentVariable("MT2_PROBE_HORDE_MAXIMUM") == "1";
        internal static bool Started { get; private set; }
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }
        private static bool recording;
        private static AllGameManagers Managers => AllGameManagers.Instance!;
        private static FullBattleTrace Trace => FullBattleTrace.Active!;
        private static readonly Type StateType = AccessTools.Inner(typeof(CharacterState), "StateInformation");
        private static readonly List<CharacterTriggerDispatch> dispatches = new List<CharacterTriggerDispatch>();
        private static object Get(object target, string field) => AccessTools.Field(target.GetType(), field).GetValue(target);
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
        private static object Copy(object value) => AccessTools.Method(StateType, "DeepCopyForPreview").Invoke(value, null);
        private static Queue<CombatManager.TriggerQueueData> Queue =>
            (Queue<CombatManager.TriggerQueueData>)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(Managers.GetCombatManager());
        private static CharacterState[] Actors()
        {
            var result = new List<CharacterState>();
            for (int room = 0; room < Managers.GetRoomManager().GetNumRooms(); room++)
                Managers.GetRoomManager().GetRoom(room).AddCharactersToList(result, Team.Type.Monsters | Team.Type.Heroes);
            return result.Where(actor => actor.IsAlive && !actor.IsDestroyed).ToArray();
        }
        internal static bool Eligible => ConditionalActivationProbe.HasVanguard && Queue.Count == 0 &&
            Actors().Any(actor => actor.GetTeamType() == Team.Type.Monsters && !actor.IsPyreHeart() &&
                actor.GetSourceCharacterData().GetAssetKey().StartsWith("TrainSteward", StringComparison.Ordinal) &&
                Actors().Any(enemy => enemy.GetTeamType() == Team.Type.Heroes && enemy.GetCurrentRoomIndex() == actor.GetCurrentRoomIndex()));
        internal static void Start(ManualLogSource log)
        { Started = true; Managers.GetSaveManager().StartCoroutine(Protect(Run(log), log)); }
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
                    if (!next) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                    if (step.Current is IEnumerator child) stack.Push(child); else yield return step.Current;
                }
            }
            finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); recording = false; }
            Completed = Error == null;
        }
        internal sealed class Input
        {
            public string Label { get; set; } = "";
            public bool Enemy { get; set; }
            public string Operation { get; set; } = "Add";
            public int Starting { get; set; }
            public int Amount { get; set; } = 2;
            public int Hp { get; set; } = 11;
            public int Maximum { get; set; } = 20;
            public bool Active { get; set; }
            public string? Blocker { get; set; }
            public int Horde { get; set; }
            public bool Heal { get; set; } = true;
            public bool TriggerOnHeal { get; set; }
            public bool DecreaseHp { get; set; } = true;
            public int Floor { get; set; } = 1;
            public bool CanHeal { get; set; } = true;
            public int HealDamage { get; set; }
            public int GainDamage { get; set; }
            public int LossDamage { get; set; }
        }
        private static Input[] HordeMaximumCases()
        {
            var cases = new[] {
                new Input { Label = "horde-buff-heal-leaves-half", Hp = 7, Maximum = 15, Active = true },
                new Input { Label = "horde-buff-heal-remains-half", Hp = 3, Maximum = 15, Active = true },
                new Input { Label = "horde-buff-noheal-enters-half", Hp = 8, Maximum = 15, Heal = false },
                new Input { Label = "horde-buff-cannotheal-enters-half", Hp = 8, Maximum = 15, CanHeal = false },
                new Input { Label = "horde-buff-onheal-before-loss", Hp = 7, Maximum = 15, Active = true, TriggerOnHeal = true },
                new Input { Label = "horde-buff-heal-multiplier", Hp = 3, Maximum = 15, Active = true, Amount = 3, Blocker = "heal multiplier" },
                new Input { Label = "horde-buff-ignores-heal-immunity", Hp = 7, Maximum = 15, Active = true, Blocker = "heal immunity" },
                new Input { Label = "horde-debuff-one", Operation = "DebuffMaximum", Hp = 10, Maximum = 15 },
                new Input { Label = "horde-enemy-debuff-one", Enemy = true, Operation = "DebuffMaximum", Hp = 10, Maximum = 15 },
                new Input { Label = "horde-debuff-two-leaves-half", Operation = "DebuffMaximum", Hp = 5, Maximum = 18, Amount = 1, DecreaseHp = false, Active = true },
                new Input { Label = "horde-debuff-two-remains-half", Operation = "DebuffMaximum", Hp = 3, Maximum = 18, Amount = 1, DecreaseHp = false, Active = true },
                new Input { Label = "horde-debuff-clips-hp", Operation = "DebuffMaximum", Hp = 11, Maximum = 12, DecreaseHp = false },
                new Input { Label = "horde-debuff-floor-retains-one", Operation = "DebuffMaximum", Hp = 1, Maximum = 2, Amount = 99, DecreaseHp = false, Active = true },
                new Input { Label = "horde-debuff-zero-keeps-wounded", Operation = "DebuffMaximum", Hp = 5, Maximum = 15, Amount = 0, Active = true },
                new Input { Label = "horde-debuff-negative-keeps-wounded", Operation = "DebuffMaximum", Hp = 5, Maximum = 15, Amount = -2, Active = true },
                new Input { Label = "horde-debuff-healthy", Operation = "DebuffMaximum", Hp = 15, Maximum = 18, Amount = 1 },
                new Input { Label = "horde-buff-zero-stale-half", Hp = 5, Maximum = 15, Amount = 0 },
                new Input { Label = "horde-buff-negative-stale-half", Hp = 5, Maximum = 15, Amount = -2 },
                new Input { Label = "horde-buff-capped", Hp = 50000, Maximum = 99999 },
                new Input { Label = "horde-buff-onheal-damages-troops", Hp = 7, Maximum = 15, Active = true, TriggerOnHeal = true, HealDamage = 2 },
                new Input { Label = "horde-buff-gain-damages-troops", Hp = 8, Maximum = 15, Heal = false, GainDamage = 1 },
                new Input { Label = "horde-buff-loss-damages-troops", Hp = 7, Maximum = 15, Active = true, LossDamage = 2 }
            };
            foreach (Input input in cases) { input.Horde = 3; if (input.Operation == "Add") input.Operation = "BuffMaximum"; }
            return cases;
        }
        private static Input[] HordeCases()
        {
            var cases = new[] {
                new Input { Label = "horde-add", Maximum = 15 },
                new Input { Label = "horde-enemy-add", Enemy = true, Maximum = 15 },
                new Input { Label = "horde-add-wounded-retains-troops", Hp = 5, Maximum = 15, Active = true },
                new Input { Label = "horde-remove-healthy", Operation = "Remove", Starting = 3, Hp = 14, Maximum = 18 },
                new Input { Label = "horde-remove-one-at-threshold", Operation = "Remove", Starting = 3, Hp = 11, Maximum = 12 },
                new Input { Label = "horde-enemy-remove-one", Enemy = true, Operation = "Remove", Starting = 3, Hp = 11, Maximum = 12 },
                new Input { Label = "horde-remove-two-leaves-half", Operation = "Remove", Starting = 3, Amount = 1, Hp = 5, Maximum = 18, Active = true },
                new Input { Label = "horde-remove-two-remains-half", Operation = "Remove", Starting = 3, Amount = 1, Hp = 1, Maximum = 18, Active = true },
                new Input { Label = "horde-remove-floor-retains-one", Operation = "Remove", Starting = 3, Amount = -1, Hp = 1, Maximum = 2 },
                new Input { Label = "horde-remove-zero-keeps-wounded", Operation = "Remove", Starting = 3, Amount = 0, Hp = 5, Maximum = 18, Active = true },
                new Input { Label = "horde-remove-negative-keeps-wounded", Operation = "Remove", Starting = 3, Amount = -2, Hp = 5, Maximum = 18, Active = true },
                new Input { Label = "horde-single-remove", Operation = "Remove", Starting = 3, Hp = 5, Maximum = 8 },
                new Input { Label = "horde-remove-all-healthy", Operation = "Remove", Starting = 3, Amount = -1, Hp = 18, Maximum = 18 }
            };
            foreach (Input input in cases) input.Horde = input.Label == "horde-single-remove" ? 1 : 3;
            return cases;
        }
        private static Input[] Cases() => new[] {
            new Input { Label = "add-enters-half" },
            new Input { Label = "enemy-add-enters-half", Enemy = true },
            new Input { Label = "add-remains-half", Hp = 9, Active = true },
            new Input { Label = "add-above-half", Hp = 20 },
            new Input { Label = "add-zero", Amount = 0 },
            new Input { Label = "add-negative-retains-stats", Starting = 3, Amount = -2, Maximum = 23 },
            new Input { Label = "add-saturates-stacks", Starting = 9998, Amount = 5, Maximum = 10018, Hp = 10018 },
            new Input { Label = "add-saturates-health", Maximum = 99998, Hp = 99998, Amount = 3 },
            new Input { Label = "immune-add", Blocker = "immune" },
            new Input { Label = "purify-blocks-add", Blocker = "purify" },
            new Input { Label = "remove-leaves-half", Operation = "Remove", Starting = 3, Maximum = 23, Active = true },
            new Input { Label = "enemy-remove-leaves-half", Enemy = true, Operation = "Remove", Starting = 3, Maximum = 23, Active = true },
            new Input { Label = "remove-remains-half", Operation = "Remove", Starting = 3, Hp = 9, Maximum = 23, Active = true },
            new Input { Label = "remove-clips-current", Operation = "Remove", Starting = 3, Hp = 23, Maximum = 23 },
            new Input { Label = "remove-all", Operation = "Remove", Starting = 3, Amount = -1, Maximum = 23, Active = true },
            new Input { Label = "remove-health-floor", Operation = "Remove", Starting = 3, Amount = -1, Hp = 1, Maximum = 2 },
            new Input { Label = "remove-zero", Operation = "Remove", Starting = 3, Amount = 0, Maximum = 23, Active = true },
            new Input { Label = "remove-missing", Operation = "Remove", Amount = -1 },
            new Input { Label = "purify-clears-zeal", Operation = "Purify", Starting = 3, Maximum = 23, Active = true }
        };
        private static IEnumerator Run(ManualLogSource log)
        {
            if (HordeMaximum)
            {
                HealingScenario.RegisterHealingStatus("heal immunity", "StatusEffectHealImmunityState", 0);
                HealingScenario.RegisterHealingStatus("heal multiplier", "StatusEffectHealMultiplierState", 2);
            }
            var save = Managers.GetSaveManager(); var rooms = Managers.GetRoomManager();
            RelicData owner = save.GetAllGameData().GetAllCollectableRelicData().Single(data => data.GetID() == OwnerId);
            RelicEffectData effect = owner.GetEffects().Single();
            var definition = new { Id = owner.GetID(), Key = owner.name, Placement = owner.DisallowInDeploymentPhase,
                Type = effect.GetEffectClassName(), Int = effect.GetParamInt(), Team = effect.GetParamSourceTeam().ToString(),
                Conditions = effect.GetEffectConditions().Count, Statuses = effect.GetParamStatusEffects() };
            if (definition.Key != "ZealDamageIncrease" || definition.Placement || definition.Type != "RelicEffectModifyStatusMagnitude" ||
                definition.Int != 1 || definition.Team != "Monsters" || definition.Conditions != 0 ||
                definition.Statuses.Length != 1 || definition.Statuses[0].statusId != "zeal" || definition.Statuses[0].count != 0 ||
                save.GetCollectedRelics().Any(relic => relic.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("Changed original ZealDamageIncrease owner.");
            save.AddRelic(owner);
            CharacterState[] actors = Actors();
            CharacterState player = actors.First(actor => actor.GetTeamType() == Team.Type.Monsters && !actor.IsPyreHeart() &&
                actor.GetSourceCharacterData().GetAssetKey().StartsWith("TrainSteward", StringComparison.Ordinal));
            CharacterState enemy = actors.First(actor => actor.GetTeamType() == Team.Type.Heroes && actor.GetCurrentRoomIndex() == player.GetCurrentRoomIndex());
            TrainCombatState original = Trace.CaptureTrain(); object rngOriginal = HalfHealthLifecycleCalibration.Rng();
            object[] states = actors.Select(actor => Get(actor, "_primaryStateInformation")).ToArray();
            object[] triggers = actors.Select(actor => Get(actor, "triggers")).ToArray();
            CharacterData[] sources = actors.Select(actor => actor.GetSourceCharacterData()).ToArray();
            var controls = Horde ? new[] { player, enemy }.ToDictionary(actor => actor, actor => UnityEngine.Object.Instantiate(actor.GetSourceCharacterData())) :
                new Dictionary<CharacterState, CharacterData>();
            foreach (CharacterData data in controls.Values) { Set(data, "health", 5); Set(data, "attackDamage", 3); }
            var statistics = Managers.GetCardStatistics();
            var statFields = typeof(CardStatistics).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            object?[] statValues = statFields.Select(field => field.GetValue(statistics)).ToArray();
            object saveData = AccessTools.Property(typeof(SaveManager), "ActiveSaveData").GetValue(save);
            object runStats = Get(saveData, "runStats");
            bool[] enchanters = actors.Select(actor => actor.IsEnchanter).ToArray();
            var identities = (Dictionary<CharacterState, Dictionary<CharacterTriggerState, int>>)Get(Trace, "triggerIdentities");
            var originalIds = actors.Select(actor => Trace.TriggerStates(actor).ToDictionary(pair => pair.Key, pair => pair.Value)).ToArray();
            var streams = Enum.GetValues(typeof(RngId)).Cast<RngId>().ToDictionary(id => id,
                id => new HadesRNG((HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { id })));
            UnityEngine.Random.State unity = UnityEngine.Random.state;
            bool allow = rooms.AllowEnchantmentUpdates; int gold = save.GetGold();
            var samples = new List<object>(); int mismatches = 0;
            try
            {
                Set(rooms, "_allowEnchantmentUpdates", false);
                foreach (Input input in HordeMaximum ? HordeMaximumCases() : Horde ? HordeCases() : Cases())
                {
                    Set(saveData, "runStats", Get(((SaveData)saveData).CreatePreviewCopy(), "runStats"));
                    for (int index = 0; index < statFields.Length; index++)
                    {
                        object? value = statValues[index];
                        if (value is Dictionary<CardState, CardStatsEntry> deck) value = HalfHealthLifecycleCalibration.ShadowDeck(deck);
                        else if (value is IDictionary map)
                        { var copy = (IDictionary)Activator.CreateInstance(value.GetType()); foreach (DictionaryEntry pair in map) copy.Add(pair.Key, pair.Value); value = copy; }
                        else if (value is IList list) value = HalfHealthLifecycleCalibration.CopyList(list);
                        statFields[index].SetValue(statistics, value);
                    }
                    for (int index = 0; index < actors.Length; index++)
                    {
                        Set(actors[index], "characterData", sources[index]);
                        Set(actors[index], "_primaryStateInformation", Copy(states[index]));
                        Set(actors[index], "triggers", new List<CharacterTriggerState>());
                        AccessTools.Property(typeof(CharacterState), "IsEnchanter").SetValue(actors[index], false);
                        identities[actors[index]] = new Dictionary<CharacterTriggerState, int>();
                    }
                    Queue.Clear(); save.SetGold(gold);
                    CharacterState host = input.Enemy ? enemy : player;
                    if (Horde) Set(host, "characterData", controls[host]);
                    var primary = Get(host, "_primaryStateInformation");
                    var statuses = (Dictionary<string, CharacterState.StatusEffectStack>)Get(primary, "statusEffects");
                    foreach (var status in statuses.Values) status.Count = 0;
                    Set(primary, "deathwishSources", new HashSet<CharacterTriggerState>());
                    Set(primary, "vanguardSources", new HashSet<CharacterTriggerState>());
                    AccessTools.Property(StateType, "DamageBuffFromStatusEffects").SetValue(primary, 0);
                    if (HordeMaximum) Set(primary, "canBeHealed", input.CanHeal);
                    host.SetHealth(20, 20);
                    if (input.Horde > 0) host.AddStatusEffect("horde", input.Horde);
                    if (input.Starting > 0) host.AddStatusEffect("zeal", input.Starting);
                    if (input.Blocker != null) host.AddStatusEffect(input.Blocker, 1);
                    foreach (var row in new[] { ("OnDeathwish", 5), ("OnDeathwishLost", 7), ("OnHeal", 11) })
                    {
                        CharacterTriggerData data = HealingScenario.HealGold(row.Item2, false, false);
                        Set(data, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), row.Item1));
                        int damageAmount = row.Item1 == "OnHeal" ? input.HealDamage : row.Item1 == "OnDeathwish" ? input.GainDamage : input.LossDamage;
                        if (damageAmount > 0)
                        {
                            var damage = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters | Team.Type.Heroes);
                            damage.Cheat_SetTargetMode(TargetMode.Self); Set(damage, "paramInt", damageAmount);
                            ((List<CardEffectData>)Get(data, "effects")).Add(damage);
                        }
                        var trigger = new CharacterTriggerState(); trigger.Setup(data, host.GetTeamType(), save); host.GetTriggers().Add(trigger);
                    }
                    if (input.Active) { host.SetHealth(10, 20); AccessTools.Method(typeof(CharacterState), "TryUpdateDeathwish").Invoke(host, null); }
                    if (Horde)
                        foreach (CharacterState observer in actors.Where(actor => actor != host && actor.GetCurrentRoomIndex() == host.GetCurrentRoomIndex()))
                        foreach (var row in new[] { ("OnAnyMonsterDeathOnFloor", 1), ("OnAnyUnitDeathOnFloor", 2) })
                        {
                            CharacterTriggerData data = HealingScenario.HealGold(row.Item2, false, false);
                            Set(data, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), row.Item1));
                            var trigger = new CharacterTriggerState(); trigger.Setup(data, observer.GetTeamType(), save); observer.GetTriggers().Add(trigger);
                        }
                    host.SetHealth(input.Hp, input.Maximum); Queue.Clear();
                    RoomCombatState before = Trace.Capture(host.GetCurrentRoom()); object rngBefore = HalfHealthLifecycleCalibration.Rng();
                    CombatStatus statusDefinition = before.Context!.StatusRules.Single(status => status.Id == "zeal");
                    RoomCombatResult predicted = Predict(before, Trace.UnitId(host), input, statusDefinition);
                    dispatches.Clear(); recording = true;
                    RoomCombatState api; CharacterTriggerDispatch[] apiDispatches; EnchantmentCallback[] pending; int[] pendingCounts; object rngApi;
                    try
                    {
                        if (input.Operation == "BuffMaximum") yield return host.BuffMaxHP(input.Amount, input.TriggerOnHeal, heal: input.Heal);
                        else if (input.Operation == "DebuffMaximum") yield return host.DebuffMaxHP(input.Amount, input.Floor, input.DecreaseHp);
                        else if (input.Operation == "Remove") host.RemoveStatusEffect("zeal", input.Amount);
                        else host.AddStatusEffect(input.Operation == "Purify" ? "purify" : "zeal", input.Operation == "Purify" ? 1 : input.Amount);
                        api = Trace.Capture(host.GetCurrentRoom()); apiDispatches = dispatches.ToArray(); rngApi = HalfHealthLifecycleCalibration.Rng();
                        pending = Queue.Select(item => new EnchantmentCallback(item.character.GetCurrentRoomIndex(), Trace.UnitId(item.character),
                            item.trigger.ToString(), item.fireTriggersData?.paramInt ?? 0, item.fireTriggersData?.paramInt2 ?? 0,
                            item.fireTriggersData?.paramString, item.fireTriggersData?.overrideTargetCharacter == null ? (int?)null : Trace.UnitId(item.fireTriggersData.overrideTargetCharacter),
                            item.dyingCharacter == null ? (int?)null : Trace.UnitId(item.dyingCharacter), item.canFireTriggers,
                            item.exclusiveTrigger == null ? (int?)null : Trace.TriggerStateId(item.character, item.exclusiveTrigger))).ToArray();
                        pendingCounts = Queue.Select(item => item.triggerCount).ToArray();
                        yield return Managers.GetCombatManager().RunTriggerQueue();
                    }
                    finally { recording = false; }
                    RoomCombatState actual = Trace.Capture(host.GetCurrentRoom()); object rngAfter = HalfHealthLifecycleCalibration.Rng();
                    RoomCombatResult? drained = predicted.Supported ? RoomCombatModel.DrainCharacterCallbacks(predicted.State!, predicted.PendingCallbacks, true) : null;
                    string? difference = predicted.UnsupportedReason ?? HalfHealthLifecycleCalibration.Difference(predicted.State, api) ??
                        HalfHealthLifecycleCalibration.Difference(predicted.PendingCallbacks.Select(EnchantmentCallback.From).ToArray(), pending) ??
                        HalfHealthLifecycleCalibration.Difference(predicted.PendingCallbacks.Select(item => item.TriggerCount).ToArray(), pendingCounts) ??
                        HalfHealthLifecycleCalibration.Difference(predicted.Dispatches, apiDispatches) ?? drained?.UnsupportedReason ??
                        (drained == null ? "Missing model successor." : HalfHealthLifecycleCalibration.Difference(drained.State, actual) ??
                            HalfHealthLifecycleCalibration.Difference(predicted.Dispatches.Concat(drained.Dispatches).ToArray(), dispatches));
                    bool rngSame = JToken.DeepEquals(HalfHealthLifecycleCalibration.GameplayRng(rngBefore), HalfHealthLifecycleCalibration.GameplayRng(rngApi)) &&
                        JToken.DeepEquals(HalfHealthLifecycleCalibration.GameplayRng(rngBefore), HalfHealthLifecycleCalibration.GameplayRng(rngAfter));
                    difference ??= rngSame ? null : "Gameplay RNG changed.";
                    if (Queue.Count != 0 || actors.Any(actor => !actor.IsAlive || actor.IsDestroyed)) throw new InvalidOperationException("Zeal component did not settle.");
                    if (difference != null) mismatches++;
                    samples.Add(new { Label = input.Label, Input = input, ActorId = Trace.UnitId(host), Status = statusDefinition,
                        Before = before, ApiActual = api, ApiDispatches = apiDispatches, ApiCallbacks = pending, ApiTriggerCounts = pendingCounts, Actual = actual,
                        Dispatches = dispatches.ToArray(), RngBefore = rngBefore, RngAfterApi = rngApi, RngAfter = rngAfter,
                        GameplayRngUnchanged = rngSame, PendingAfterDrain = Queue.Count, Difference = difference, Completed = true });
                    log.LogInfo("ZEAL-STATUS " + input.Label + " " + (difference ?? "MATCH"));
                }
            }
            finally
            {
                recording = false; Queue.Clear();
                for (int index = 0; index < actors.Length; index++)
                {
                    Set(actors[index], "_primaryStateInformation", states[index]); Set(actors[index], "triggers", triggers[index]);
                    Set(actors[index], "characterData", sources[index]);
                    AccessTools.Property(typeof(CharacterState), "IsEnchanter").SetValue(actors[index], enchanters[index]); identities[actors[index]] = originalIds[index];
                }
                save.SetGold(gold); Set(rooms, "_allowEnchantmentUpdates", allow);
                for (int index = 0; index < statFields.Length; index++) statFields[index].SetValue(statistics, statValues[index]);
                Set(saveData, "runStats", runStats);
                foreach (CharacterData control in controls.Values) UnityEngine.Object.Destroy(control);
                foreach (var pair in streams) ((HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { pair.Key })).Init(pair.Value);
                UnityEngine.Random.state = unity;
            }
            string? restoration = HalfHealthLifecycleCalibration.Difference(original, Trace.CaptureTrain()) ??
                HalfHealthLifecycleCalibration.Difference(rngOriginal, HalfHealthLifecycleCalibration.Rng());
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId, Boundary = HordeMaximum ? "NativeHordeMaximumHealthLifecycle" : Horde ? "NativeZealHordeLifecycle" : "NativeZealStatusLifecycle",
                OwnerDefinition = definition, LiveContextUnchanged = restoration == null, RestorationDifference = restoration, EffectsDrained = true,
                RngBefore = rngOriginal, RngAfter = HalfHealthLifecycleCalibration.Rng(), Mismatches = mismatches, Samples = samples }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, HordeMaximum ? "horde-maximum.mt2f" : Horde ? "zeal-horde.mt2f" : "zeal-status.mt2f"))) document.Write(output);
            if (restoration != null || mismatches != 0) throw new InvalidOperationException("Zeal differs: " + mismatches + "; restoration: " + restoration);
        }
        private static RoomCombatResult Predict(RoomCombatState before, int actorId, Input input, CombatStatus zeal) =>
            input.Operation is "BuffMaximum" or "DebuffMaximum" ? UnitHealthModel.ApplyNative(before, actorId, input.Amount,
                input.Operation == "DebuffMaximum", input.Heal, input.TriggerOnHeal, input.Floor, input.DecreaseHp, captureAllDispatches: true) :
            input.Operation == "Remove" ? StatusRemovalModel.Remove(before, actorId, "zeal", input.Amount) :
                StatusApplicationModel.Apply(before, actorId, input.Operation == "Purify" ?
                    before.Context!.StatusRules.Single(status => status.Id == "purify").WithStacks(1) : zeal.WithStacks(input.Amount));
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class FirePatch
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
