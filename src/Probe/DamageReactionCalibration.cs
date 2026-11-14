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
    // Runs only in an isolated native worker. Each case starts from fresh unit,
    // trigger and statistics copies; the full train and every RNG stream are restored.
    internal static class DamageReactionCalibration
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_DAMAGE_REACTIONS") == "1";
        internal static bool Started { get; private set; }
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }
        private const string Steward = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private static AllGameManagers Managers => AllGameManagers.Instance!;
        private static FullBattleTrace Trace => FullBattleTrace.Active!;
        private static readonly Type StateType = AccessTools.Inner(typeof(CharacterState), "StateInformation");
        private static bool recording;
        private static readonly List<CharacterTriggerDispatch> dispatches = new List<CharacterTriggerDispatch>();
        private static object Get(object target, string field) => AccessTools.Field(target.GetType(), field).GetValue(target);
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
        private static object Copy(object state) => AccessTools.Method(StateType, "DeepCopyForPreview").Invoke(state, null);
        private static Queue<CombatManager.TriggerQueueData> Queue =>
            (Queue<CombatManager.TriggerQueueData>)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(Managers.GetCombatManager());
        private static CharacterState[] Actors()
        {
            var list = new List<CharacterState>();
            for (int room = 0; room < Managers.GetRoomManager().GetNumRooms(); room++)
                Managers.GetRoomManager().GetRoom(room).AddCharactersToList(list, Team.Type.Heroes | Team.Type.Monsters);
            return list.Where(actor => actor.IsAlive && !actor.IsDestroyed && !actor.IsPyreHeart()).ToArray();
        }
        private static bool Ready => !Managers.GetSaveManager().PreviewMode && !PreviewRngIsolation.Active &&
            NativeEffectSettlement.Ready(Managers.GetCombatManager()) &&
            !Managers.GetReplayManager().IsCardPlaying() && !Managers.GetCombatManager().IsRunningTriggerQueue &&
            !(Managers.GetHandUI()?.AreAnyCardsAnimating() ?? false) &&
            !(bool)Get(Managers.GetCombatManager(), "combatStateChanged") && Queue.Count == 0;
        private static CharacterState? Host => Actors().FirstOrDefault(actor => actor.GetTeamType() == Team.Type.Monsters &&
            actor.GetSpawnerCard()?.GetCardDataID() == Steward && Actors().Any(enemy => enemy.GetTeamType() == Team.Type.Heroes &&
                enemy.GetCurrentRoomIndex() == actor.GetCurrentRoomIndex()));
        internal static bool Eligible => ConditionalActivationProbe.HasVanguard && Ready && Host != null &&
            Managers.GetCardManager().GetHand().Any(card => card.GetCardDataID() == Steward);
        internal static void Start(ManualLogSource log)
        { Started = true; Managers.GetSaveManager().StartCoroutine(Protect(Run(log), log)); }
        private static IEnumerator Protect(IEnumerator root, ManualLogSource log)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(root);
            try
            {
                while (stack.Count > 0)
                {
                    IEnumerator next = stack.Peek(); bool advanced;
                    try { advanced = next.MoveNext(); }
                    catch (Exception error) { Error = error.ToString(); log.LogError(Error); break; }
                    if (!advanced) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                    if (next.Current is IEnumerator child) stack.Push(child); else yield return next.Current;
                }
            }
            finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); recording = false; }
            Completed = Error == null;
        }
        private static IEnumerator Run(ManualLogSource log)
        {
            CharacterState host = Host!;
            RoomManager rooms = Managers.GetRoomManager(); SaveManager save = Managers.GetSaveManager();
            CardManager cards = Managers.GetCardManager();
            int roomIndex = host.GetCurrentRoomIndex();
            yield return rooms.GetRoomUI().SetSelectedRoom(roomIndex);
            // The fixture uses the three-capacity Steward variant. Expand this
            // private board through the native API before the second paid play.
            yield return rooms.GetRoom(roomIndex).AdjustCapacity(Team.Type.Monsters, 3, showPipsEffect: false);
            int handIndex = cards.GetHand().FindIndex(card => card.GetCardDataID() == Steward);
            CardState secondCard = cards.GetHand()[handIndex];
            var drop = rooms.GetRoom(roomIndex).GetMonsterPoint(0);
            if (!cards.CanPlayHandCard(secondCard, roomIndex, drop, null, null, out var playError) || !cards.PlayCard(handIndex, drop, ref playError))
                throw new InvalidOperationException("Second paid native Steward rejected: " + playError);
            float deadline = UnityEngine.Time.realtimeSinceStartup + 20;
            while (!Ready)
            {
                if (UnityEngine.Time.realtimeSinceStartup > deadline) throw new TimeoutException("Second Steward did not settle.");
                yield return null;
            }
            CombatManager combat = Managers.GetCombatManager();
            bool previewSuppression = CombatPreviewControl.Save(combat);
            CombatPreviewControl.Set(combat, true);
            CharacterState[] actors = Actors();
            CharacterState witness = actors.Single(actor => actor.GetSpawnerCard() == secondCard);
            CharacterState enemy = actors.First(actor => actor.GetTeamType() == Team.Type.Heroes && actor.GetCurrentRoomIndex() == roomIndex);
            if (witness.GetCurrentRoomIndex() != roomIndex) throw new InvalidOperationException("Bloodshed needs two real same-floor units.");
            var original = Trace.CaptureTrain(); object originalRng = HalfHealthLifecycleCalibration.Rng();
            var streams = Enum.GetValues(typeof(RngId)).Cast<RngId>().ToDictionary(id => id, id => new HadesRNG(NativeRng(id)));
            UnityEngine.Random.State unity = UnityEngine.Random.state;
            object[] states = actors.Select(actor => Get(actor, "_primaryStateInformation")).ToArray();
            CardState[] spawners = actors.Select(actor => actor.GetSpawnerCard()).Where(card => card != null).Distinct().ToArray();
            FieldInfo[] cardFields = typeof(CardState).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            object?[][] cardValues = spawners.Select(card => cardFields.Select(field => field.GetValue(card)).ToArray()).ToArray();
            object[] originalTriggers = actors.Select(actor => Get(actor, "triggers")).ToArray();
            bool[] enchanters = actors.Select(actor => actor.IsEnchanter).ToArray();
            string[] damageFields = { "lastDamageTaken", "lastDamageTakenBeforeArmor", "lastArmorBlockedOnHit", "lastLethalDamagePreventedOnHit" };
            object[][] damageValues = actors.Select(actor => damageFields.Select(field => Get(actor, field)).ToArray()).ToArray();
            var identities = (Dictionary<CharacterState, Dictionary<CharacterTriggerState, int>>)Get(Trace, "triggerIdentities");
            var originalIds = actors.Select(actor => Trace.TriggerStates(actor).ToDictionary(pair => pair.Key, pair => pair.Value)).ToArray();
            CardStatistics statistics = Managers.GetCardStatistics();
            string[] counterFields = { "lastAttackDamageDealt", "damageDealtToMonstersThisTurn", "damageDealtToMonstersThisBattle",
                "friendlyDamageDealtToMonstersThisTurn", "friendlyDamageDealtToMonstersThisBattle" };
            object[] counters = counterFields.Select(field => Get(statistics, field)).ToArray();
            var deck = (Dictionary<CardState, CardStatsEntry>)Get(statistics, "deckStats");
            int gold = save.GetGold(); bool allow = rooms.AllowEnchantmentUpdates;
            var relics = new List<RelicState>(); save.GetAllRelics(relics);
            RelicState relic = relics.First();
            var samples = new List<object>(); int mismatches = 0;
            try
            {
                foreach (Input input in Cases())
                {
                    Queue.Clear(); Set(rooms, "_allowEnchantmentUpdates", false);
                    for (int index = 0; index < actors.Length; index++)
                    {
                        CharacterState actor = actors[index];
                        Set(actor, "_primaryStateInformation", Copy(states[index]));
                        Set(actor, "triggers", new List<CharacterTriggerState>()); identities[actor] = new Dictionary<CharacterTriggerState, int>();
                        AccessTools.Property(typeof(CharacterState), "IsEnchanter").SetValue(actor, false);
                        foreach (var status in Statuses(actor).Values) status.Count = 0;
                        actor.SetHealth(20, 20);
                        for (int field = 0; field < damageFields.Length; field++) Set(actor, damageFields[field], damageValues[index][field]);
                    }
                    save.SetGold(gold); Set(statistics, "deckStats", HalfHealthLifecycleCalibration.ShadowDeck(deck));
                    for (int card = 0; card < spawners.Length; card++)
                        for (int field = 0; field < cardFields.Length; field++)
                        {
                            object? value = cardValues[card][field];
                            cardFields[field].SetValue(spawners[card], value is CardStateModifiers ?
                                HalfHealthLifecycleCalibration.ShadowLists(value) : HalfHealthLifecycleCalibration.CopyList(value));
                        }
                    for (int field = 0; field < counterFields.Length; field++) Set(statistics, counterFields[field], counters[field]);
                    Set(rooms, "_allowEnchantmentUpdates", allow);
                    CharacterState target = input.EnemyTarget ? enemy : host;
                    CharacterState? attacker = input.Source.StartsWith("Enemy", StringComparison.Ordinal) ? enemy :
                        input.Source.StartsWith("Friendly", StringComparison.Ordinal) ? witness : input.Source == "Self" ? target : null;
                    CardState? sourceCard = input.Source.Contains("Card") ? host.GetSpawnerCard() : null;
                    RelicState? sourceRelic = input.Source.Contains("Relic") ? relic : null;
                    target.SetHealth(input.Hp, 20);
                    if (input.Status != null) Raw(target, input.Status, input.Stacks);
                    if (input.Label == "silenced-witness") Raw(witness, "silenced", 1);
                    foreach (CharacterState actor in actors.Where(actor => actor.GetCurrentRoomIndex() == roomIndex))
                    {
                        if (actor.GetTeamType() == Team.Type.Monsters)
                            Add(actor, "OnBloodshed", actor == host ? 5 : 10, input,
                                actor == witness ? input.BloodEffect : null, TargetMode.LastAttackedCharacter);
                        Add(actor, "OnHit", 15, input);
                        Add(actor, "OnPunish", 20, input, actor == target ? input.PunishEffect : null, TargetMode.Self);
                    }
                    if (input.Half)
                    {
                        Add(target, "OnDeathwish", 25, input);
                        Add(target, "OnDeathwishLost", 30, input);
                    }
                    RoomCombatState before = Trace.Capture(target.GetCurrentRoom()); object rngBefore = HalfHealthLifecycleCalibration.Rng();
                    RoomCombatResult predicted = Predict(before, Trace.UnitId(target), input.Amount, sourceCard == null ? 0 : Trace.CardId(sourceCard),
                        attacker == null ? 0 : Trace.UnitId(attacker), sourceRelic != null);
                    var predictedDispatches = predicted.Dispatches.ToList();
                    dispatches.Clear(); recording = true;
                    try
                    {
                        for (int hit = 0; hit < input.Hits; hit++)
                        {
                            yield return target.ApplyDamage(input.Amount, new CharacterState.ApplyDamageParams {
                                attacker = attacker!, damageSourceCard = sourceCard!, damageSourceRelic = sourceRelic!,
                                damageType = Damage.Type.Default, damageSourceCardFinishingResolution = true }, Managers.GetPlayerManager(), statistics);
                            if (hit + 1 < input.Hits && predicted.Supported)
                            {
                                predicted = Predict(predicted.State!, Trace.UnitId(target), input.Amount, sourceCard == null ? 0 : Trace.CardId(sourceCard),
                                    attacker == null ? 0 : Trace.UnitId(attacker), sourceRelic != null);
                                predictedDispatches.AddRange(predicted.Dispatches);
                            }
                        }
                    }
                    finally { recording = false; }
                    RoomCombatState actual = Trace.Capture(target.GetCurrentRoom()); object rngAfter = HalfHealthLifecycleCalibration.Rng();
                    if (actors.Any(actor => !actor.IsAlive || actor.IsDestroyed) || Queue.Count != 0)
                        throw new InvalidOperationException("Damage-reaction case left a dead actor or pending queue: " + input.Label);
                    bool rngUnchanged = JToken.DeepEquals(HalfHealthLifecycleCalibration.GameplayRng(rngBefore), HalfHealthLifecycleCalibration.GameplayRng(rngAfter));
                    string? difference = predicted.UnsupportedReason ?? HalfHealthLifecycleCalibration.Difference(predicted.State, actual) ??
                        HalfHealthLifecycleCalibration.Difference(predictedDispatches, dispatches) ?? (rngUnchanged ? null : "Gameplay/Unity RNG changed.");
                    if (difference != null) { mismatches++; log.LogWarning(input.Label + ": " + difference); }
                    samples.Add(new { Label = input.Label, Input = input, TargetId = Trace.UnitId(target), HostId = Trace.UnitId(host),
                        WitnessId = Trace.UnitId(witness), AttackerId = attacker == null ? 0 : Trace.UnitId(attacker),
                        SourceCardId = sourceCard == null ? 0 : Trace.CardId(sourceCard), RelicSourced = sourceRelic != null,
                        Before = before, Actual = actual, Dispatches = dispatches.ToArray(), RngBefore = rngBefore, RngAfter = rngAfter,
                        GameplayRngUnchanged = rngUnchanged, PendingAfterDrain = Queue.Count, Difference = difference,
                        Predicted = difference == null ? null : predicted.State, Completed = true });
                    log.LogInfo("DAMAGE-REACTION " + input.Label + " " + (difference ?? "MATCH"));
                }
            }
            finally
            {
                recording = false; Queue.Clear(); Set(rooms, "_allowEnchantmentUpdates", false);
                for (int index = 0; index < actors.Length; index++)
                {
                    Set(actors[index], "_primaryStateInformation", states[index]); Set(actors[index], "triggers", originalTriggers[index]);
                    identities[actors[index]] = originalIds[index];
                    AccessTools.Property(typeof(CharacterState), "IsEnchanter").SetValue(actors[index], enchanters[index]);
                    for (int field = 0; field < damageFields.Length; field++) Set(actors[index], damageFields[field], damageValues[index][field]);
                }
                Set(statistics, "deckStats", deck);
                for (int card = 0; card < spawners.Length; card++)
                    for (int field = 0; field < cardFields.Length; field++) cardFields[field].SetValue(spawners[card], cardValues[card][field]);
                for (int field = 0; field < counterFields.Length; field++) Set(statistics, counterFields[field], counters[field]);
                save.SetGold(gold); Set(rooms, "_allowEnchantmentUpdates", allow);
                foreach (var pair in streams) NativeRng(pair.Key).Init(pair.Value);
                UnityEngine.Random.state = unity;
                CombatPreviewControl.Set(combat, previewSuppression);
            }
            string? restoration = HalfHealthLifecycleCalibration.Difference(original, Trace.CaptureTrain()) ??
                HalfHealthLifecycleCalibration.Difference(originalRng, HalfHealthLifecycleCalibration.Rng());
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId, Boundary = "NativeBloodshedPunishDamageLifecycle",
                LiveContextUnchanged = restoration == null, RestorationDifference = restoration, EffectsDrained = true,
                RngBefore = originalRng, RngAfter = HalfHealthLifecycleCalibration.Rng(), Mismatches = mismatches, Samples = samples }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "damage-reactions.mt2f"))) document.Write(output);
            if (restoration != null || mismatches != 0) throw new InvalidOperationException("Damage reactions differ: " + mismatches + "; restoration: " + restoration);
        }
        private static HadesRNG NativeRng(RngId id) => (HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { id });
        private static Dictionary<string, CharacterState.StatusEffectStack> Statuses(CharacterState actor) =>
            (Dictionary<string, CharacterState.StatusEffectStack>)Get(Get(actor, "_primaryStateInformation"), "statusEffects");
        private static void Raw(CharacterState actor, string id, int amount)
        {
            var statuses = Statuses(actor);
            if (statuses.TryGetValue(id, out var stack)) stack.Count = amount;
            else statuses.Add(id, new CharacterState.StatusEffectStack(Managers.GetStatusEffectManager().Create(id, actor), amount));
        }
        private static void Add(CharacterState actor, string kind, int gold, Input input, string? nested = null, TargetMode mode = TargetMode.Self)
        {
            bool reaction = kind == "OnBloodshed" || kind == "OnPunish";
            CharacterTriggerData data = HealingScenario.HealGold(gold, nested != null || reaction && input.Once, input.IgnoreSilence);
            Set(data, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), kind));
            Set(data, "triggerAtThreshold", reaction ? input.Threshold : 0);
            if (nested != null)
            {
                var effect = new CardEffectData(nested, null!, actor.GetTeamType()); effect.Cheat_SetTargetMode(mode);
                Set(effect, "paramInt", nested == "CardEffectHeal" ? 2 : 1);
                Set(data, "effects", data.GetEffects().Concat(new[] { effect }).ToList());
            }
            var trigger = new CharacterTriggerState(); trigger.Setup(data, actor.GetTeamType(), Managers.GetSaveManager()); actor.GetTriggers().Add(trigger);
        }
        private static RoomCombatResult Predict(RoomCombatState before, int target, int amount, int card, int attacker, bool relic) =>
            RoomCombatModel.ApplyNativeCardDamageAfterTraits(before, target, amount, card, attacker, captureAllDispatches: true, relicSourced: relic);
        private sealed class Input
        {
            public string Label { get; }
            public string Source { get; }
            public bool EnemyTarget { get; }
            public int Amount { get; }
            public int Hp { get; }
            public string? Status { get; }
            public int Stacks { get; }
            public int Threshold { get; }
            public bool Once { get; }
            public int Hits { get; }
            public bool IgnoreSilence { get; }
            public string? BloodEffect { get; }
            public string? PunishEffect { get; }
            public bool Half { get; }
            internal Input(string label, string source = "Card", bool enemyTarget = false, int amount = 2, int hp = 15,
                string? status = null, int stacks = 1, int threshold = 0, bool once = false, int hits = 1, bool ignoreSilence = false,
                string? bloodEffect = null, string? punishEffect = null, bool half = false)
            { Label = label; Source = source; EnemyTarget = enemyTarget; Amount = amount; Hp = hp; Status = status; Stacks = stacks;
                Threshold = threshold; Once = once; Hits = hits; IgnoreSilence = ignoreSilence; BloodEffect = bloodEffect; PunishEffect = punishEffect; Half = half; }
        }
        private static IEnumerable<Input> Cases()
        {
            foreach (string source in new[] { "None", "Card", "Friendly", "Enemy", "Self", "Relic", "EnemyCard", "FriendlyCard", "EnemyRelic" })
                yield return new Input("player-source-" + source, source);
            foreach (string source in new[] { "None", "Card", "Friendly", "Enemy" }) yield return new Input("enemy-source-" + source, source, enemyTarget: true);
            yield return new Input("zero", amount: 0);
            yield return new Input("armor-blocked", status: "armor", stacks: 5);
            yield return new Input("armor-partial", status: "armor", amount: 3);
            yield return new Input("damage-shield", status: "damage shield");
            yield return new Input("titanskin", status: "titanskin");
            yield return new Input("threshold-below", threshold: 3);
            yield return new Input("threshold-equal", threshold: 2);
            yield return new Input("threshold-armor-blocked", status: "armor", stacks: 5, threshold: 1);
            yield return new Input("once-two-hits", once: true, hits: 2);
            yield return new Input("silenced-host", status: "silenced");
            yield return new Input("silenced-witness");
            yield return new Input("muted-host", status: "muted");
            yield return new Input("ignored-silence", status: "silenced", ignoreSilence: true);
            yield return new Input("bloodshed-heals-damaged-target", bloodEffect: "CardEffectHeal");
            yield return new Input("bloodshed-damages-target", bloodEffect: "CardEffectDamage");
            yield return new Input("punish-heals-self", punishEffect: "CardEffectHeal");
            yield return new Input("punish-damages-self", punishEffect: "CardEffectDamage");
            yield return new Input("damage-enters-half", hp: 11, amount: 1, half: true);
            yield return new Input("bloodshed-heals-before-half", hp: 11, amount: 1, half: true, bloodEffect: "CardEffectHeal");
            yield return new Input("punish-heals-before-half", hp: 11, amount: 1, half: true, punishEffect: "CardEffectHeal");
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
