#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;
using DLC = ShinyShoe.DLC;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalUnrulyWitness
    {
        private static string ScenarioKey => Variant switch
        {
            "Scourge" => "SoulSavior_R3_BossBattle_Scourge",
            "SniperAdvance" => "SoulSavior_R3_BossBattle_StealBuffs1",
            "DutifulBurst" => "SoulSavior_R1_BossBattle_DutifulChild_Burst",
            "DutifulArmor" => "SoulSavior_R1_BossBattle_DutifulChild_Heal",
            "DutifulAscendDescend" => "SoulSavior_R1_BossBattle_DutifulChild_AscendDescend",
            "IndulgentTitanskin" => "SoulSavior_R2_BossBattle_FavoredChild_3",
            "IndulgentCapacity" => "SoulSavior_R2_BossBattle_FavoredChild_2",
            "IndulgentGorgeOnSlay" => "SoulSavior_R2_BossBattle_FavoredChild_1",
            "LifemotherReanimate" => "SoulSavior_R5_BossBattle_Lifemother_Reanimate",
            OriginalLifemotherDebuffsWitness.Variant => OriginalLifemotherDebuffsWitness.Scenario,
            _ => "SoulSavior_R3_BossBattle_DuplicateEnemy"
        };
        private static string BossKey => Variant switch
        {
            "Scourge" => OuterBossDefinitions.UnrulyScourge,
            "SniperAdvance" => OuterBossDefinitions.UnrulySniperAdvance,
            "DutifulBurst" => OuterBossDefinitions.DutifulBurst,
            "DutifulArmor" => OuterBossDefinitions.DutifulArmor,
            "DutifulAscendDescend" => OuterBossDefinitions.DutifulAscendDescend,
            "IndulgentTitanskin" => OuterBossDefinitions.IndulgentTitanskin,
            "IndulgentCapacity" => OuterBossDefinitions.IndulgentReduceCapacity,
            "IndulgentGorgeOnSlay" => GorgeOnSlayBoss,
            "LifemotherReanimate" => OuterBossDefinitions.LifemotherReanimate,
            OriginalLifemotherDebuffsWitness.Variant => OriginalLifemotherDebuffsWitness.Boss,
            _ => TriggeredSummonModel.UnrulyBoss
        };
        private const string DecoyKey = "R3_Horde_T1_Decoy";
        internal const string GorgeOnSlayBoss = "Boss_SoulSavior_R2_Indulgent_GorgeOnSlay";
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_UNRULY") is "component" or "battle";
        internal static bool FullBattle => Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_UNRULY") == "battle";
        private static string? Variant => FullBattle ? Environment.GetEnvironmentVariable("MT2_PROBE_UNRULY_VARIANT") : null;
        internal static bool Scourge => Variant == "Scourge";
        internal static bool DutifulBurst => Variant == "DutifulBurst";
        internal static bool DutifulGrant(CardEffectState effect, CardEffectParams parameters) =>
            FullBattle && OuterBossDefinitions.IsDutiful(BossKey) &&
            parameters.selfTarget?.GetSourceCharacterData().GetAssetKey() == BossKey &&
            parameters.sourceCharacterTriggerState is { } trigger &&
            OuterBossPhaseProbe.DutifulGrant(BossKey, trigger.GetTrigger(), effect.GetSourceCardEffectData());
        internal static bool IndulgentTitanskin => Variant == "IndulgentTitanskin";
        private static bool IndulgentCapacity => Variant == "IndulgentCapacity";
        internal static bool IndulgentGorgeOnSlay => Variant == "IndulgentGorgeOnSlay";
        internal static bool IndulgentTrampleCasting => (IndulgentCapacity || IndulgentGorgeOnSlay) &&
            Environment.GetEnvironmentVariable("MT2_PROBE_INDULGENT_TRAMPLE_CASTING") == "1";
        internal static bool LifemotherReanimate => Variant == "LifemotherReanimate";
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }
        internal static object? FullBattleEvidence { get; private set; }
        private static bool requested, started;
        private static int? pendingEndTurn;
        private static CharacterState? actor;
        private static IReadOnlyList<CardEffectState>? effects;
        private static List<object>? events;
        private static readonly List<object> nativeAdvances = new();
        private static readonly List<object> burstConsumptions = new();
        private static readonly List<object> titanSkinAbsorptions = new();
        private static readonly List<GorgeReplay> gorgeOnSlayReplays = new();

        private sealed class GorgeReplay
        {
            public int Turn { get; set; }
            public int BossId { get; set; }
            public string Trigger { get; set; } = "OnKill";
            public string Replay { get; set; } = "";
            public int QueuedOnFeed { get; set; }
            public bool Completed { get; set; }
            public int? GrantSequence { get; set; }
        }

        internal static void ObserveGorgeOnSlayGrant(TriggeredStatusProbe.Record grant)
        {
            if (!IndulgentGorgeOnSlay || !grant.Completed || grant.Kind != "OnFeed" ||
                !grant.Targets.SequenceEqual(new[] { grant.ActorId })) return;
            int turn = AllGameManagers.Instance!.GetCombatManager().GetTurnCount();
            GorgeReplay? replay = gorgeOnSlayReplays.FirstOrDefault(row => row.Turn == turn && row.BossId == grant.ActorId &&
                row.Completed && row.QueuedOnFeed == 1 && row.GrantSequence == null);
            if (replay != null) replay.GrantSequence = grant.Sequence;
        }

        internal static bool GorgeOnSlayGrant(CardEffectData effect) =>
            effect.GetEffectStateName() == nameof(CardEffectAddStatusEffect) && SelfEffect(effect) &&
            effect.GetParamInt() == 0 && effect.GetParamStatusEffects() is [var status] &&
            status.statusId == "buff" && status.count == 3;

        private static bool GorgeOnSlayReplay(CardEffectData effect) =>
            effect.GetEffectStateName() == nameof(CardEffectPlayUnitTrigger) && SelfEffect(effect) &&
            effect.GetParamInt() == 1 && effect.GetParamTrigger() == CharacterTriggerData.Trigger.OnFeed;

        private static bool SelfEffect(CardEffectData effect) =>
            effect.GetTargetMode() == TargetMode.Self && effect.GetTargetTeamType() == (Team.Type.Heroes | Team.Type.Monsters) &&
            !effect.GetUseIntRange();

        private static bool SupportsGorgeOnSlay(CharacterData? data) =>
            data?.GetAssetKey() == GorgeOnSlayBoss && data.GetTriggers() is [var feed, var kill] &&
            !feed.GetTriggerOnce() && !kill.GetTriggerOnce() && feed.GetTrigger() == CharacterTriggerData.Trigger.OnFeed &&
            kill.GetTrigger() == CharacterTriggerData.Trigger.OnKill &&
            feed.GetEffects() is [var grant] && GorgeOnSlayGrant(grant) &&
            kill.GetEffects() is [var replay] && GorgeOnSlayReplay(replay);

        internal static bool SupportsIndulgentTrampleCasting(CharacterData data) => IndulgentTrampleCasting &&
            data.GetAssetKey() == BossKey && data.GetSize() == 5 && data.GetHealth() == 1100 &&
            data.GetStartingStatusEffects() is [{ statusId: "trample", count: 1 }] &&
            (IndulgentCapacity ? SupportsIndulgentCapacity(data) :
                data.GetAttackDamage() == 13 && SupportsGorgeOnSlay(data));

        private static bool SupportsIndulgentCapacity(CharacterData data) =>
            data.GetAttackDamage() == 12 && data.GetHealth() == 1100 && data.GetSize() == 5 &&
            data.GetStartingStatusEffects() is [{ statusId: "trample", count: 1 }] &&
            data.GetTriggers() is [var feed, var post] && !feed.GetTriggerOnce() && !post.GetTriggerOnce() &&
            feed.GetTrigger() == CharacterTriggerData.Trigger.OnFeed && feed.GetEffects() is [var grant] && GorgeOnSlayGrant(grant) &&
            post.GetTrigger() == CharacterTriggerData.Trigger.PostCombat && post.GetEffects() is [var capacity, var replay] &&
            capacity.GetEffectStateName() == nameof(CardEffectAdjustRoomCapacity) && capacity.GetTargetMode() == TargetMode.Room &&
            capacity.GetTargetTeamType() == (Team.Type.Heroes | Team.Type.Monsters) && capacity.GetParamInt() == -1 &&
            !capacity.GetUseIntRange() && replay.GetEffectStateName() == nameof(CardEffectPlayUnitTrigger) && SelfEffect(replay) &&
            replay.GetParamInt() == -1 && replay.GetParamTrigger() == CharacterTriggerData.Trigger.OnFeed;

        internal static void Request(SaveManager save, ManualLogSource log)
        {
            if (requested || Application.version != "2.3.1" || typeof(CardState).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.GetGameSequence() != SaveData.GameSequence.BattleIntro)
                throw new InvalidOperationException("Expected the original 2.3.1 Soul Savior BattleIntro.");
            if (!save.IsDlcInstalled(DLC.Railforged))
                throw new InvalidOperationException("The native platform did not confirm the installed Railforged DLC.");
            if (save.GetAllGameData().FindScenarioDataByName(ScenarioKey) is not ScenarioData scenario ||
                scenario.GetOuterTrainBoss(false)?.GetAssetKey() != BossKey ||
                scenario.GetOuterTrainBoss(true)?.GetAssetKey() != BossKey ||
                (OriginalLifemotherDebuffsWitness.Enabled ? !OriginalLifemotherDebuffsWitness.Supports(scenario) :
                    FullBattle && !IndulgentGorgeOnSlay && !IndulgentCapacity && !OuterBossPhaseProbe.Supports(scenario.GetOuterTrainBoss(false))) ||
                IndulgentTrampleCasting && (!OuterBossPhaseProbe.Supports(scenario.GetOuterTrainBoss(false)) ||
                    !OuterBossPhaseProbe.Supports(scenario.GetOuterTrainBoss(true))) ||
                IndulgentCapacity && (!SupportsIndulgentCapacity(scenario.GetOuterTrainBoss(false)) ||
                    !SupportsIndulgentCapacity(scenario.GetOuterTrainBoss(true)) || scenario.GetEnemyRelicData() is not [var capacityOwner] ||
                    capacityOwner.GetID() != "0809140a-a5c5-41f7-ab7c-375e6d88fb14" ||
                    capacityOwner.GetEffects() is not [var capacityDescription] ||
                    capacityDescription.GetEffectClassName() != nameof(RelicEffectNull)) ||
                IndulgentGorgeOnSlay && (!SupportsGorgeOnSlay(scenario.GetOuterTrainBoss(false)) ||
                    !SupportsGorgeOnSlay(scenario.GetOuterTrainBoss(true)) || scenario.GetEnemyRelicData() is not [var gorgeOwner] ||
                    gorgeOwner.GetID() != "ce0711e4-7113-43cc-9cbf-ca92bbf1e4b3" ||
                    gorgeOwner.name != "Boss_Railforged_Region2_BossBattle_1" || gorgeOwner.GetEffects().Count != 0) ||
                LifemotherReanimate && (scenario.GetID() != "1199a33a-0049-4084-a2b3-752740e202ca" ||
                    !OuterBossPhaseProbe.Supports(scenario.GetOuterTrainBoss(true))) ||
                IndulgentTitanskin && (scenario.GetEnemyRelicData() is not [var owner] ||
                    owner.GetID() != OuterBossDefinitions.IndulgentTitanskinOwnerId ||
                    owner.name != OuterBossDefinitions.IndulgentTitanskinOwnerKey || owner.GetEffects() is not [var authoring] ||
                    authoring.GetEffectClassName() != nameof(RelicEffectNull)))
                throw new InvalidOperationException("Expected the installed original Soul Savior boss battle.");
            requested = true;
            AccessTools.Method(typeof(CheatManager), "Command_StartBattle", new[] { typeof(string) }).Invoke(null, new object[] { ScenarioKey });
            log.LogInfo("ORIGINAL-UNRULY controlled TestScenario requested " + ScenarioKey);
        }

        private static void CheckScenario(SaveManager save)
        {
            if (!requested || !save.InTestScenario || save.GetCurrentScenarioData()?.name != ScenarioKey ||
                !save.IsDlcInstalled(DLC.Railforged))
                throw new InvalidOperationException("The native DLC check or controlled Unruly scenario changed.");
        }

        internal static void ObserveFullBattle(SaveManager save)
        {
            if (FullBattleEvidence != null) return;
            CheckScenario(save);
            FullBattleEvidence = Variant switch
            {
                "SniperAdvance" => new { Scenario = ScenarioKey, BossKey, RailforgedInstalled = true,
                    ControlledTestScenario = true, Advances = nativeAdvances },
                "DutifulBurst" => new { Scenario = ScenarioKey, BossKey, RailforgedInstalled = true,
                    ControlledTestScenario = true, BurstConsumptions = burstConsumptions },
                "IndulgentTitanskin" => new { Scenario = ScenarioKey, BossKey, RailforgedInstalled = true,
                    ControlledTestScenario = true, TitanSkinAbsorptions = titanSkinAbsorptions,
                    HivemindCallbacks = OriginalHivemindProbe.Records },
                "IndulgentGorgeOnSlay" => new { Scenario = ScenarioKey, BossKey, RailforgedInstalled = true,
                    ControlledTestScenario = true, GorgeOnSlayReplays = gorgeOnSlayReplays,
                    HivemindCallbacks = OriginalHivemindProbe.Records },
                "IndulgentCapacity" when IndulgentTrampleCasting => new { Scenario = ScenarioKey, BossKey,
                    RailforgedInstalled = true, ControlledTestScenario = true, HivemindCallbacks = OriginalHivemindProbe.Records },
                _ => new { Scenario = ScenarioKey, BossKey, RailforgedInstalled = true, ControlledTestScenario = true }
            };
        }

        [HarmonyPatch(typeof(CardEffectPlayUnitTrigger), nameof(CardEffectPlayUnitTrigger.ApplyEffect))]
        private static class GorgeOnSlayReplayPatch
        {
            private static bool Prepare() => IndulgentGorgeOnSlay;

            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                CharacterState? boss = cardEffectParams.selfTarget;
                if (FullBattleTrace.Active == null || AllGameManagers.Instance!.GetSaveManager().PreviewMode ||
                    boss?.GetSourceCharacterData().GetAssetKey() != GorgeOnSlayBoss ||
                    cardEffectParams.sourceCharacterTriggerState?.GetTrigger() != CharacterTriggerData.Trigger.OnKill ||
                    !GorgeOnSlayReplay(cardEffectState.GetSourceCardEffectData()) || cardEffectParams.targets.Count != 1 ||
                    cardEffectParams.targets[0] != boss) return;
                __result = Observe(__result, FullBattleTrace.Active, boss, cardEffectState.GetParamTrigger().ToString());
            }

            private static IEnumerator Observe(IEnumerator native, FullBattleTrace trace, CharacterState boss, string replayKind)
            {
                CombatManager combat = AllGameManagers.Instance!.GetCombatManager();
                int queued = CountPendingOnFeed();
                var replay = new GorgeReplay { Turn = combat.GetTurnCount(), BossId = trace.UnitId(boss), Replay = replayKind };
                gorgeOnSlayReplays.Add(replay);
                try { while (native.MoveNext()) yield return native.Current; }
                finally { (native as IDisposable)?.Dispose(); }
                replay.QueuedOnFeed = CountPendingOnFeed() - queued;
                replay.Completed = true;

                // An OnKill effect is already inside RunTriggerQueue. The original
                // OnFeed executes after this iterator returns.
                int CountPendingOnFeed() => ((IEnumerable<CombatManager.TriggerQueueData>)AccessTools
                    .Property(typeof(CombatManager), "TriggerQueue").GetValue(combat)).Count(entry =>
                    entry.character == boss && entry.trigger == CharacterTriggerData.Trigger.OnFeed);
            }
        }

        [HarmonyPatch(typeof(CharacterState), "DoNotifyDamageWasApplied")]
        private static class TitanSkinAbsorptionPatch
        {
            private static bool Prepare() => IndulgentTitanskin;

            private static void Prefix(CharacterState attacked, int damage, int damageBlocked, int titanSkinSoaked)
            {
                if (titanSkinSoaked <= 0 || FullBattleTrace.Active == null ||
                    attacked.GetSourceCharacterData().GetAssetKey() != BossKey || AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    return;
                titanSkinAbsorptions.Add(new { Turn = AllGameManagers.Instance.GetCombatManager().GetTurnCount(),
                    UnitId = FullBattleTrace.Active.UnitId(attacked), HealthDamage = damage,
                    BlockedDamage = damageBlocked, Absorbed = titanSkinSoaked });
            }
        }

        [HarmonyPatch(typeof(StatusEffectBurstState), "OnTriggered")]
        private static class BurstConsumptionPatch
        {
            private static bool Prepare() => DutifulBurst;

            private static void Postfix(StatusEffectBurstState __instance, ref IEnumerator __result)
            {
                CharacterState? target = __instance.GetAssociatedCharacter();
                if (FullBattleTrace.Active == null || target == null || target.GetTeamType() != Team.Type.Heroes ||
                    AllGameManagers.Instance!.GetSaveManager().PreviewMode) return;
                __result = Observe(__result, target);
            }

            private static IEnumerator Observe(IEnumerator native, CharacterState actor)
            {
                int before = actor.GetStatusEffectStacks("burst");
                try { while (native.MoveNext()) yield return native.Current; }
                finally { (native as IDisposable)?.Dispose(); }
                int after = actor.GetStatusEffectStacks("burst");
                if (after != before - 1) yield break;
                FullBattleTrace trace = FullBattleTrace.Active!;
                int id = trace.UnitId(actor);
                if (!TriggeredStatusProbe.Records.Any(row => row.Completed && row.Kind == "PostCombat" && row.Targets.Contains(id)))
                    yield break;
                burstConsumptions.Add(new { Turn = AllGameManagers.Instance!.GetCombatManager().GetTurnCount(),
                    UnitId = id, Before = before, After = after });
            }
        }

        [HarmonyPatch(typeof(CardEffectFloorRearrange), nameof(CardEffectFloorRearrange.ApplyEffect))]
        private static class SniperAdvancePatch
        {
            private static bool Prepare() => Variant == "SniperAdvance";

            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (AllGameManagers.Instance!.GetSaveManager().PreviewMode ||
                    cardEffectParams.selfTarget?.GetSourceCharacterData().GetAssetKey() != BossKey ||
                    cardEffectParams.sourceCharacterTriggerState?.GetTrigger() != CharacterTriggerData.Trigger.OnAttacking ||
                    !OuterBossPhaseProbe.SniperAdvance(cardEffectState.GetSourceCardEffectData()) ||
                    cardEffectParams.targets.Count == 0) return;
                __result = Observe(__result, cardEffectParams.selfTarget, cardEffectParams.targets[0]);
            }

            private static IEnumerator Observe(IEnumerator native, CharacterState boss, CharacterState target)
            {
                FullBattleTrace trace = FullBattleTrace.Active!;
                int room = target.GetCurrentRoomIndex();
                int id = trace.UnitId(target);
                int[] before = Occupants();
                try { while (native.MoveNext()) yield return native.Current; }
                finally { (native as IDisposable)?.Dispose(); }
                int[] after = Occupants();
                nativeAdvances.Add(new
                {
                    Turn = AllGameManagers.Instance!.GetCombatManager().GetTurnCount(), Room = room,
                    BossId = trace.UnitId(boss), TargetId = id, Before = before, After = after
                });

                int[] Occupants() => BattleSpawnPointProbe.Capture(trace).Group(room, CombatTeam.Player)?.Occupants
                    .Where(occupant => occupant > 0).ToArray() ?? Array.Empty<int>();
            }
        }

        internal static void ArmEndTurn(int turn)
        {
            if (pendingEndTurn.HasValue)
                throw new InvalidOperationException("The previous Unruly EndTurn is still pending.");
            pendingEndTurn = turn;
        }

        [HarmonyPatch(typeof(ReplayManager), nameof(ReplayManager.RecordEntry))]
        private static class AcceptedEndTurnPatch
        {
            private static void Prefix(ReplayEntry replayEntryNew)
            {
                if (replayEntryNew is not ReplayEntryEndTurn || !pendingEndTurn.HasValue) return;
                CombatManager combat = AllGameManagers.Instance!.GetCombatManager();
                if (combat.GetTurnCount() != pendingEndTurn || !combat.IsPlayerActionPhase())
                    throw new InvalidOperationException("Unruly EndTurn did not commit at the expected native player decision.");
                // The confirmed replay entry precedes the native transition to
                // MonsterTurnQueueClear and follows pending player previews.
                pendingEndTurn = null;
                PreviewSchedulingProbe.Observe("EndTurnAccepted");
                FullBattleTrace.Active!.BeginEndTurn();
            }
        }

        internal static bool Tick(AllGameManagers managers, ManualLogSource log)
        {
            if (started) return true;
            var save = managers.GetSaveManager();
            CheckScenario(save);
            CharacterState? boss = managers.GetHeroManager().GetOuterTrainBossCharacter();
            if (boss == null || boss.GetCurrentRoomIndex() < 0) return false;
            if (boss.GetSourceCharacterData().GetAssetKey() != BossKey)
                throw new InvalidOperationException("The native debug scenario loaded a different boss.");
            RoomState room = boss.GetCurrentRoom();
            if (Heroes(room).Any(hero => !ReferenceEquals(boss, hero)) || room.GetFirstEmptyHeroPoint() == null) return false;
            started = true;
            save.StartCoroutine(Observe());
            return true;

            IEnumerator Observe()
            {
                IEnumerator capture = Capture(managers, boss, log);
                try
                {
                    while (true)
                    {
                        object current;
                        try
                        {
                            if (!capture.MoveNext()) break;
                            current = capture.Current;
                        }
                        catch (Exception error) { Error = error.ToString(); break; }
                        yield return current;
                    }
                }
                finally { (capture as IDisposable)?.Dispose(); }
            }
        }

        private static IEnumerator Capture(AllGameManagers managers, CharacterState boss, ManualLogSource log)
        {
            var trigger = boss.GetTriggers().Single(state => state.GetTrigger() == CharacterTriggerData.Trigger.PostCombat &&
                state.GetEffectStates().Any(effect => effect.GetCardEffect() is CardEffectSpawnHeroIfNoOtherHeroUnits));
            var ordered = trigger.GetEffectStates();
            if (ordered.Count != 2 || ordered[0].GetCardEffect() is not CardEffectCopyUnits ||
                ordered[1].GetCardEffect() is not CardEffectSpawnHeroIfNoOtherHeroUnits ||
                ordered[1].GetParamCharacterData()?.GetAssetKey() != DecoyKey ||
                ordered[1].GetParamCharacterDataPool()?.Count > 0)
                throw new InvalidOperationException("The original Unruly PostCombat definition changed.");
            var combat = managers.GetCombatManager();
            FullBattleTrace trace = FullBattleTrace.Active!;
            List<object> samples = new();
            actor = boss;
            effects = ordered;
            try
            {
                for (int pass = 0; pass < 2; pass++)
                {
                    object before = Snapshot();
                    var priorHeroes = Heroes(boss.GetCurrentRoom());
                    var recorded = new List<object>();
                    events = recorded;
                    yield return combat.QueueAndRunTrigger(boss, CharacterTriggerData.Trigger.PostCombat, exclusiveTrigger: trigger);
                    events = null;
                    object after = Snapshot();
                    bool born = Heroes(boss.GetCurrentRoom()).Any(hero => !priorHeroes.Contains(hero) &&
                        hero.GetSourceCharacterData().GetAssetKey() == DecoyKey);
                    samples.Add(new { Control = pass == 0 ? "BossOnly" : "DecoyAlreadyPresent", Before = before, After = after, Events = recorded });
                    if (pass == 0 && (!born || !recorded.Any(item => item is Callback { EffectIndex: 1, Stage: "Apply.Exit" })))
                        throw new InvalidOperationException("The original lone-boss PostCombat did not create its Decoy through the fallback.");
                    if (pass == 1 && recorded.Any(item => item is Callback { EffectIndex: 1, Stage: "Apply.Enter" }))
                        throw new InvalidOperationException("The original fallback ran despite the existing Decoy.");
                }
            }
            finally { events = null; effects = null; actor = null; }
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalUnrulyPostCombat", GameVersion = Application.version, GameModuleMvid = Module,
                Scenario = ScenarioKey, BossKey, DecoyKey, RailforgedInstalled = managers.GetSaveManager().IsDlcInstalled(DLC.Railforged),
                ControlledTestScenario = managers.GetSaveManager().InTestScenario,
                UnitCopyCatalog = trace.CaptureDecision(includeOuterBossForBump: true,
                    ignoreOuterBossSpawnInteraction: true).PlayRules?.UnitCopyCatalog,
                SourceEffects = ordered.Select(effect => new { Type = effect.GetSourceCardEffectData().GetEffectStateName(),
                    Target = effect.GetTargetMode().ToString(), Team = effect.GetTargetTeamType().ToString(),
                    IgnoreBosses = effect.GetSourceCardEffectData().GetTargetIgnoreBosses() }).ToArray(),
                Samples = samples
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "unruly-post-combat.mt2f"));
            archive.Write(stream);
            log.LogInfo("ORIGINAL-UNRULY-POSTCOMBAT PASS native Decoy fallback and populated control.");
            Completed = true;
        }

        private static List<CharacterState> Heroes(RoomState room)
        {
            var characters = new List<CharacterState>();
            room.AddCharactersToList(characters, Team.Type.Heroes);
            return characters;
        }

        private static object Snapshot()
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            return new
            {
                Boss = trace.CaptureUnit(actor!), RoomIndex = actor!.GetCurrentRoomIndex(),
                Heroes = Heroes(actor.GetCurrentRoom()).Select(trace.UnitId).ToArray(),
                Train = trace.CaptureCanonicalTrain(includeOuterBossForBump: true),
                Battle = RngCalibration.Words(RandomManager.GetState(RngId.Battle)),
                BattleTest = RngCalibration.Words(RandomManager.GetState(RngId.BattleTest))
            };
        }

        private sealed class Callback(int effectIndex, string stage, bool? passed, int[] targets, object state)
        {
            public int EffectIndex { get; } = effectIndex;
            public string Stage { get; } = stage;
            public bool? Passed { get; } = passed;
            public int[] Targets { get; } = targets;
            public object State { get; } = state;
        }

        private static int Index(CardEffectState state, CardEffectParams parameters) =>
            actor != null && events != null && !AllGameManagers.Instance.GetSaveManager().PreviewMode &&
            ReferenceEquals(parameters.selfTarget, actor)
                ? ReferenceEquals(state, effects![0]) ? 0 : ReferenceEquals(state, effects[1]) ? 1 : -1 : -1;

        [HarmonyPatch]
        private static class Tests
        {
            private static bool Prepare() => Enabled;
            private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
            {
                yield return AccessTools.DeclaredMethod(typeof(CardEffectCopyUnits), nameof(CardEffectCopyUnits.TestEffect));
                yield return AccessTools.DeclaredMethod(typeof(CardEffectSpawnHeroIfNoOtherHeroUnits), nameof(CardEffectSpawnHeroIfNoOtherHeroUnits.TestEffect));
            }
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, bool __result)
            {
                int index = Index(cardEffectState, cardEffectParams);
                if (index >= 0) events!.Add(new Callback(index, "Test", __result,
                    cardEffectParams.targets.Select(FullBattleTrace.Active!.UnitId).ToArray(), Snapshot()));
            }
        }

        [HarmonyPatch]
        private static class Applications
        {
            private static bool Prepare() => Enabled;
            private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
            {
                yield return AccessTools.DeclaredMethod(typeof(CardEffectCopyUnits), nameof(CardEffectCopyUnits.ApplyEffect));
                yield return AccessTools.DeclaredMethod(typeof(CardEffectSpawnHeroIfNoOtherHeroUnits), nameof(CardEffectSpawnHeroIfNoOtherHeroUnits.ApplyEffect));
            }
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                int index = Index(cardEffectState, cardEffectParams);
                if (index >= 0) __result = Observe(__result, index, cardEffectParams.targets.Select(FullBattleTrace.Active!.UnitId).ToArray(), events!);
            }
            private static IEnumerator Observe(IEnumerator native, int index, int[] targets, List<object> capture)
            {
                capture.Add(new Callback(index, "Apply.Enter", null, targets, Snapshot()));
                try { while (native.MoveNext()) yield return native.Current; }
                finally { (native as IDisposable)?.Dispose(); }
                capture.Add(new Callback(index, "Apply.Exit", null, targets, Snapshot()));
            }
        }
    }
}
#endif
