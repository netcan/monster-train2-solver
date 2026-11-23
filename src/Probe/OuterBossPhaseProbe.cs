using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OuterBossPhaseProbe
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_OUTER_BOSS_PHASES") == "1";
        internal static bool TurnsEnabled => PlayerOuterBossProbe.Enabled || Environment.GetEnvironmentVariable("MT2_PROBE_OUTER_BOSS_TURNS") == "1";
        internal static bool CaptureAnchors => Enabled || TurnsEnabled || TrueFinalBossTurnProbe.CaptureEnabled || TrueFinalBossDeathCalibration.Enabled
#if DEVELOPMENT_PROBE
            || OriginalUnrulyWitness.Enabled
#endif
            ;
        internal static bool ModelsLiveBoss
        {
            get
            {
                CharacterState? boss = AllGameManagers.Instance?.GetHeroManager()?.GetOuterTrainBossCharacter();
                return boss != null && boss.IsAlive && !boss.IsDestroyed &&
                    (!OuterBossDefinitions.IsIndulgentTrample(boss.GetSourceCharacterData().GetAssetKey()) ||
                        boss.GetBossState().GetCurrentAttackPhase() == BossState.AttackPhase.Casting) &&
                    (TurnsEnabled && Supports(boss.GetSourceCharacterData()) || TrueFinalBossTurnProbe.CaptureEnabled &&
                        boss.IsTrueFinalBoss() && boss.GetSourceCharacterData().GetAssetKey() == "Boss_Level9_Entropy_Top_V2");
            }
        }
        private static bool ForceRelentless => Environment.GetEnvironmentVariable("MT2_PROBE_OUTER_BOSS_RELENTLESS") == "1";
        internal const string LifemotherDebuffsId = "9dc77d80-0ab2-471e-af73-135080c7d4a6";
        internal static bool Supports(CharacterData? data)
        {
            if (data == null) return false;
            var groups = data.GetBossActionData();
            string assetKey = data.GetAssetKey();
            bool supported = OuterBossDefinitions.Supports(assetKey);
            if (OuterBossDefinitions.IsIndulgentTrample(assetKey))
            {
                SaveManager? save = AllGameManagers.Instance?.GetSaveManager();
                supported = save is { InTestScenario: false } && save.GetAscensionLevel() == 0 &&
                    IndulgentTrampleCaptureProbe.Supports(save.GetCurrentScenarioData(), data);
#if DEVELOPMENT_PROBE
                supported |= OriginalUnrulyWitness.SupportsIndulgentTrampleCasting(data);
#endif
            }
            if (!supported || groups.Count != 1 ||
                groups[0].GetNumRepeats() != 0 || groups[0].GetActions().Count != 1) return false;
            BossActionData action = groups[0].GetActions()[0];
            bool target = OuterBossDefinitions.IsArkionPyre(data.GetAssetKey())
                ? action.GetActionBehaviorName() == "RandomBossActionBehavior" && action.GetParamTeamType() == Team.Type.None
                : action.GetActionBehaviorName() == "RandomOccupiedBossActionBehavior" && action.GetParamTeamType() == Team.Type.Monsters;
            return target && action.GetActionEffectData().Count == 0 &&
                data.GetRoomModifiersData().Count == 0 &&
                (data.GetAssetKey() != OuterBossDefinitions.UnrulyScourge ||
                    data.GetTriggers() is [var trigger] && trigger.GetTrigger() == CharacterTriggerData.Trigger.OnAnyHeroDeathOnFloor &&
                    !trigger.GetTriggerOnce() && trigger.GetEffects() is [var effect] && ScourgeAward(effect)) &&
                (data.GetAssetKey() != OuterBossDefinitions.UnrulySniperAdvance ||
                    data.GetTriggers() is [var sniper] && sniper.GetTrigger() == CharacterTriggerData.Trigger.OnAttacking &&
                    !sniper.GetTriggerOnce() && !sniper.GetHideVisualAndIgnoreSilence() &&
                    sniper.GetEffects() is [var movement] && SniperAdvance(movement)) &&
                (!OuterBossDefinitions.IsDutiful(data.GetAssetKey()) ||
                    data.GetTriggers() is [var dutiful] && !dutiful.GetTriggerOnce() &&
                    !dutiful.GetHideVisualAndIgnoreSilence() && !dutiful.GetOnlyTriggerIfEquipped() &&
                    dutiful.GetTriggerAtThreshold() == 0 && dutiful.GetEffects() is [var grant] &&
                    DutifulGrant(data.GetAssetKey(), dutiful.GetTrigger(), grant)) &&
                (data.GetAssetKey() != OuterBossDefinitions.IndulgentTitanskin || SupportsIndulgentTitanskin(data)) &&
                (data.GetAssetKey() != OuterBossDefinitions.LifemotherReanimate || LifemotherDefinition(data)) &&
                (data.GetAssetKey() != TriggeredStatusTransferModel.LifemotherDebuffer || LifemotherDebuffsDefinition(data));
        }

        private static bool LifemotherDebuffsDefinition(CharacterData data) => data.GetID() == LifemotherDebuffsId &&
            data.GetAttackDamage() == 50 && data.GetHealth() == 9000 && data.GetSize() == 5 &&
            data.GetStartingStatusEffects() is [{ statusId: "trample", count: 1 }] &&
            data.GetTriggers() is [var attack, var death, var turn] &&
            LifemotherDebuffsTrigger(attack, CharacterTriggerData.Trigger.OnAttacking) &&
            LifemotherDebuffsTrigger(death, CharacterTriggerData.Trigger.OnAnyMonsterDeathOnFloor) &&
            LifemotherDebuffsTrigger(turn, CharacterTriggerData.Trigger.OnTeamTurnBegin);

        private static bool LifemotherDebuffsTrigger(CharacterTriggerData trigger, CharacterTriggerData.Trigger kind) =>
            LifemotherTrigger(trigger, kind) && trigger.GetRequiredStatusEffects().Count == 0 &&
            trigger.GetRequiredStatusEffectsForDyingCharacter().Count == 0 &&
            trigger.GetEffects() is [var effect] && LifemotherDebuffsEffect(effect, kind);

        internal static bool LifemotherDebuffsEffect(CardEffectData effect, CharacterTriggerData.Trigger kind) =>
            !effect.GetUseIntRange() && kind switch
            {
                CharacterTriggerData.Trigger.OnAttacking => effect.GetEffectStateName() == nameof(CardEffectAddStatusEffect) &&
                    effect.GetTargetMode() == TargetMode.LastAttackedCharacter && effect.GetTargetTeamType() == Team.Type.Monsters &&
                    effect.GetParamInt() == 0 && effect.GetParamStatusEffects() is [{ statusId: "malaise", count: 12 }],
                CharacterTriggerData.Trigger.OnAnyMonsterDeathOnFloor =>
                    effect.GetEffectStateName() == nameof(CardEffectTransferAllStatusEffectsFromUnit) &&
                    effect.GetTargetMode() == TargetMode.DyingCharacter && effect.GetTargetTeamType() == (Team.Type.Heroes | Team.Type.Monsters) &&
                    effect.GetParamInt() == (int)CardEffectTransferAllStatusEffectsFromUnit.TransferTarget.FrontFriendlyUnit &&
                    effect.GetAdditionalParamInt() == (int)CardEffectTransferAllStatusEffectsFromUnit.AllowedStatusEffectType.DebuffsOnly &&
                    effect.GetAdditionalParamInt1() == (int)CardEffectTransferAllStatusEffectsFromUnit.TransferRoomType.SourceRoomOnly &&
                    effect.GetParamFloat() == 0 && !effect.GetParamBool() && !effect.GetParamBool2() &&
                    effect.GetParamStatusEffects().Length == 0 && effect.GetTargetCharacterSubtype().IsNone &&
                    !effect.GetTargetIgnoreBosses(),
                CharacterTriggerData.Trigger.OnTeamTurnBegin => effect.GetEffectStateName() == nameof(CardEffectRemoveStatusEffect) &&
                    effect.GetTargetMode() == TargetMode.Self && effect.GetTargetTeamType() == (Team.Type.Heroes | Team.Type.Monsters) &&
                    effect.GetParamInt() == 5 && effect.GetParamStatusEffects() is [{ statusId: "dazed", count: 5 }],
                _ => false
            };

        internal static bool ScourgeAward(CardEffectData effect) =>
            effect.GetEffectStateName() == nameof(CardEffectAddBattleCard) &&
            effect.GetTargetMode() == TargetMode.Room && effect.GetTargetTeamType() == Team.Type.Heroes &&
            (CardPile)effect.GetParamInt() == CardPile.HandPile && effect.GetAdditionalParamInt() == 1 &&
            !effect.GetParamBool() && !effect.GetParamBool2() && !effect.GetCopyModifiersFromSource() &&
            !effect.GetFilterBasedOnMainSubClass() && effect.GetParamCardUpgradeData() == null &&
            effect.GetParamCardPool()?.GetFilteredChoices() is [var card] && card.GetID() == OuterBossDefinitions.UnrulyScourgeCard;

        internal static bool SniperAdvance(CardEffectData effect) =>
            effect.GetEffectStateName() == nameof(CardEffectFloorRearrange) &&
            effect.GetTargetMode() == TargetMode.LastAttackedCharacter && effect.GetTargetTeamType() == Team.Type.Monsters &&
            effect.GetParamInt() == (int)SpawnMode.FrontSlot && !effect.GetUseIntRange();

        internal static bool DutifulGrant(string assetKey, CharacterTriggerData.Trigger trigger, CardEffectData effect)
        {
            if (effect.GetTargetMode() != TargetMode.Room || effect.GetTargetTeamType() != Team.Type.Heroes ||
                effect.GetUseIntRange() || effect.GetParamStatusEffects() is not [var status]) return false;
            return assetKey switch
            {
                OuterBossDefinitions.DutifulBurst => trigger == CharacterTriggerData.Trigger.PostCombat &&
                    effect.GetEffectStateName() == nameof(CardEffectAddStatusEffect) && status.statusId == "burst" && status.count == 1,
                OuterBossDefinitions.DutifulArmor => trigger == CharacterTriggerData.Trigger.PostCombat &&
                    effect.GetEffectStateName() == nameof(CardEffectAddStatusEffectPerSelfAttack) && effect.GetParamInt() == 1 &&
                    !effect.GetParamBool2() && status.statusId == "armor" && status.count == 1,
                OuterBossDefinitions.DutifulAscendDescend => trigger == CharacterTriggerData.Trigger.OnShift &&
                    effect.GetEffectStateName() == nameof(CardEffectAddStatusEffect) && status.statusId == "buff" && status.count == 3,
                _ => false
            };
        }
        private static bool SupportsIndulgentTitanskin(CharacterData data) =>
            data.GetAttackDamage() == 12 && data.GetHealth() == 1000 && data.GetSize() == 5 &&
            data.GetStartingStatusEffects() is [var trample, var skin] &&
            trample.statusId == "trample" && trample.count == 1 && skin.statusId == "titanskin" && skin.count == 3 &&
            data.GetTriggers() is [var feed] && feed.GetTrigger() == CharacterTriggerData.Trigger.OnFeed &&
            !feed.GetTriggerOnce() && !feed.GetHideVisualAndIgnoreSilence() && !feed.GetOnlyTriggerIfEquipped() &&
            feed.GetTriggerAtThreshold() == 0 && feed.GetEffects() is [var rage, var protection] &&
            IndulgentGrant(rage, "buff") && IndulgentGrant(protection, "titanskin");

        private static bool IndulgentGrant(CardEffectData effect, string id) =>
            effect.GetEffectStateName() == nameof(CardEffectAddStatusEffect) && effect.GetTargetMode() == TargetMode.Self &&
            effect.GetTargetTeamType() == (Team.Type.Heroes | Team.Type.Monsters) && !effect.GetUseIntRange() &&
            effect.GetParamStatusEffects() is [var status] && status.statusId == id && status.count == 3;

        private static bool LifemotherDefinition(CharacterData data) =>
            data.GetID() == "5111391a-4817-4d2c-862b-9750895bf402" &&
            data.GetTriggers() is [var postCombat, var turnBegin] &&
            LifemotherTrigger(postCombat, CharacterTriggerData.Trigger.PostCombat) &&
            postCombat.GetEffects() is [var witherbloom, var undying] &&
            LifemotherGrant(witherbloom, Team.Type.Monsters, "witherbloom", 10) &&
            LifemotherGrant(undying, Team.Type.Heroes, "undying", 2) &&
            LifemotherTrigger(turnBegin, CharacterTriggerData.Trigger.OnTeamTurnBegin) &&
            turnBegin.GetEffects() is [var remove] &&
            remove.GetEffectStateName() == nameof(CardEffectRemoveStatusEffect) && remove.GetTargetMode() == TargetMode.Self &&
            remove.GetTargetTeamType() == (Team.Type.Monsters | Team.Type.Heroes) &&
            remove.GetParamInt() == 0 && !remove.GetUseIntRange() &&
            remove.GetParamStatusEffects() is [var dazed] && dazed.statusId == "dazed" && dazed.count == 5;

        private static bool LifemotherTrigger(CharacterTriggerData trigger, CharacterTriggerData.Trigger kind) =>
            trigger.GetTrigger() == kind && !trigger.GetTriggerOnce() && !trigger.GetHideVisualAndIgnoreSilence() &&
            !trigger.GetOnlyTriggerIfEquipped() && trigger.GetTriggerAtThreshold() == 0;

        internal static bool LifemotherGrant(CardEffectData effect, Team.Type team, string id, int stacks) =>
            effect.GetEffectStateName() == nameof(CardEffectAddStatusEffect) && effect.GetTargetMode() == TargetMode.FrontInRoom &&
            effect.GetTargetTeamType() == team && effect.GetParamInt() == 0 && !effect.GetUseIntRange() &&
            effect.GetParamStatusEffects() is [var status] && status.statusId == id && status.count == stacks;
        internal sealed class Record
        {
            public string Phase { get; set; } = "";
            public OuterBossPhaseState Before { get; set; } = null!;
            public OuterBossPhaseState? Actual { get; set; }
            public bool NativeReturned { get; set; }
            public bool? TerminalWon { get; set; }
        }
        private static readonly List<Record> Records = new List<Record>();
        internal static IReadOnlyList<Record> CapturedActions => Records;
        private static readonly List<string> Errors = new List<string>();
        private static readonly List<object> QueueControls = new List<object>();
        internal static IReadOnlyList<object> Controls => QueueControls;
        private static Record? pending;
        private static T Field<T>(object owner, string name) => (T)AccessTools.Field(owner.GetType(), name).GetValue(owner);
        private static bool Capturing => Enabled && FullBattleTrace.Active != null && !AllGameManagers.Instance.GetSaveManager().PreviewMode;
        internal static int[]? AnchorSlots(CharacterState unit, BossState.AttackPhase phase)
        {
            if (!CaptureAnchors || !unit.IsOuterTrainBoss()) return null;
            return AnchorSlots(phase);
        }
        internal static int[] AnchorSlots(BossState.AttackPhase phase)
        {
            RoomManager rooms = AllGameManagers.Instance.GetRoomManager();
            return Enumerable.Range(0, rooms.GetNumRooms()).Select(index =>
            {
                RoomState room = rooms.GetRoom(index);
                ShinyShoe.BossAnchor? anchor = Field<ShinyShoe.BossAnchor[]>(room, "_bossAnchors").FirstOrDefault(value => value.AttackPhase == phase);
                var group = Field<SpawnPointGroup>(room, "heroSpawnPointGroup");
                return anchor == null ? -1 : Field<List<SpawnPoint>>(group, "_primarySpawnPoints").IndexOf(anchor.SpawnPoint);
            }).ToArray();
        }
        internal static OuterBossProgress CaptureProgress() => CaptureProgress(AllGameManagers.Instance.GetHeroManager().GetOuterTrainBossCharacter());
        internal static OuterBossProgress CaptureProgress(CharacterState boss)
        {
            BossState state = boss.GetBossState();
            BossActionController controller = Field<BossActionController>(state, "_bossActionController");
            object primary = AccessTools.Property(typeof(CharacterState), "PrimaryStateInformation").GetValue(boss);
            return CaptureProgress(controller, FullBattleTrace.Active!.UnitId(boss), boss.GetSourceCharacterData(),
                state.GetCurrentAttackPhase().ToString(), (int)Field<BossState.AttackPhase>(boss, "validBossAttackPhase"),
                Field<bool>(primary, "canAttack"), boss.GetCurrentRoomIndex(), boss.GetMovementSpeed(), state.GetVisitedRoomRecord());
        }
        internal static OuterBossProgress Definition(CharacterData data) => CaptureProgress(new BossActionController(data.GetBossActionData()),
            0, data, "Casting", (int)data.GetValidBossAttackPhase(), data.GetCanAttack(), -1, 1, Array.Empty<int>());
        private static OuterBossProgress CaptureProgress(BossActionController controller, int id, CharacterData data,
            string phase, int validPhases, bool canAttack, int room, int speed, IReadOnlyList<int> visits)
        {
            OuterBossActionProgress Action(BossActionState action)
            {
                object behavior = Field<object>(action, "_actionBehavior");
                Team.Type team = behavior is RandomOccupiedBossActionBehavior ? Field<Team.Type>(behavior, "_teamType") : Team.Type.Monsters;
                return new OuterBossActionProgress(behavior.GetType().Name, team == Team.Type.Monsters ? CombatTeam.Player : CombatTeam.Enemy,
                    action.GetTargetedRoomIndex(), action.GetPostActionRoomIndex(), Field<bool>(action, "_setupAsInitialAction"),
                    action.IsRoomDestroyAction(), action.IsEmptyAction());
            }
            return new OuterBossProgress(id, data.GetAssetKey(), phase, validPhases, canAttack, room, speed, visits,
                BossActionControllerProbe.Snapshot(controller, AllGameManagers.Instance.GetRoomManager()),
                Field<List<ActionGroupState>>(controller, "_actionGroups").Select(group => (IReadOnlyList<OuterBossActionProgress>)group.GetActions().Select(Action).ToArray()).ToArray(),
                Field<ActionGroupState>(controller, "_roomDestroyActionGroup").GetActions().Select(Action).ToArray());
        }
        internal static OuterBossPhaseState Capture()
        {
            HeroManager heroes = AllGameManagers.Instance.GetHeroManager();
            TrueFinalBossPhaseFlags? flags = heroes.GetOuterTrainBossCharacter().IsTrueFinalBoss() ? new TrueFinalBossPhaseFlags(
                Field<bool>(heroes, "hasHellforgedBoss"), Field<bool>(heroes, "hasHellforgedBossWaitedOnRelentless"),
                Field<List<int>>(heroes, "queuedTrueFinalBosses").Count) : null;
            return new OuterBossPhaseState(FullBattleTrace.Active!.CaptureDecision(
                includeOuterBossForBump: true, ignoreOuterBossSpawnInteraction: true), CaptureProgress(), flags);
        }
        private static IEnumerator Observe(IEnumerator native)
        {
            Record? row = null;
            if (Capturing && AllGameManagers.Instance.GetHeroManager().GetOuterTrainBossCharacter()?.IsAlive == true)
                try
                {
                    if (pending != null) throw new InvalidOperationException("Previous native outer-boss phase has not settled.");
                    CombatManager combat = AllGameManagers.Instance.GetCombatManager();
                    if (ForceRelentless && QueueControls.Count == 0 && combat.GetTurnCount() == 2 &&
                        combat.GetCombatPhase() == CombatManager.Phase.BossActionPreCombat)
                    {
                        HeroManager heroes = AllGameManagers.Instance.GetHeroManager();
                        SpawnPatternData pattern = Field<SpawnPatternData>(heroes, "spawnPattern");
                        var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(AllGameManagers.Instance.GetSaveManager(), waves);
                        int previous = Field<int>(heroes, "spawnPatternPhase");
                        AccessTools.Field(typeof(HeroManager), "spawnPatternPhase").SetValue(heroes, waves.Count);
                        QueueControls.Add(new { Kind = "ForceSpawnQueueEmpty", Turn = combat.GetTurnCount(),
                            PhaseBefore = previous, PhaseAfter = waves.Count, NativeWaveCount = waves.Count });
                        if (int.TryParse(Environment.GetEnvironmentVariable("MT2_PROBE_OUTER_BOSS_RELENTLESS_PYRE_HEALTH"), out int pyreHealth) && pyreHealth > 0)
                        {
                            CharacterState pyre = AllGameManagers.Instance.GetRoomManager().GetPyreRoom().GetPyreHeart();
                            int healthBefore = pyre.GetHP(), maximumBefore = pyre.GetMaxHP();
                            SaveManager save = AllGameManagers.Instance.GetSaveManager();
                            int savedBefore = save.GetTowerHP();
                            save.AdjustTowerMaxHP(pyreHealth - save.GetMaxTowerHP());
                            pyre.SetHealth(pyreHealth, pyreHealth);
                            save.AdjustTowerHP(pyreHealth - save.GetTowerHP());
                            QueueControls.Add(new { Kind = "SetPyreHealth", Turn = combat.GetTurnCount(),
                                HealthBefore = healthBefore, MaxHealthBefore = maximumBefore,
                                HealthAfter = pyre.GetHP(), MaxHealthAfter = pyre.GetMaxHP(),
                                SavedHealthBefore = savedBefore, SavedHealthAfter = save.GetTowerHP(), SavedMaxHealthAfter = save.GetMaxTowerHP() });
                        }
                        if (int.TryParse(Environment.GetEnvironmentVariable("MT2_PROBE_OUTER_BOSS_RELENTLESS_HEALTH"), out int health) && health > 0)
                        {
                            CharacterState boss = heroes.GetOuterTrainBossCharacter();
                            int beforeHealth = boss.GetHP();
                            boss.SetHealth(health, boss.GetMaxHP());
                            QueueControls.Add(new { Kind = "SetOuterBossHealth", Turn = combat.GetTurnCount(),
                                HealthBefore = beforeHealth, HealthAfter = boss.GetHP(), MaxHealth = boss.GetMaxHP() });
                        }
                    }
                    row = new Record { Phase = AllGameManagers.Instance.GetCombatManager().GetCombatPhase().ToString(), Before = Capture() };
                    pending = row; Records.Add(row);
                }
                catch (Exception error) { Errors.Add(error.ToString()); }
            bool completed = false;
            try { while (native.MoveNext()) yield return native.Current; completed = true; }
            finally { (native as IDisposable)?.Dispose(); if (row != null) row.NativeReturned = completed; }
        }
        private static IEnumerator Settle(IEnumerator native, Record row)
        {
            try { while (native.MoveNext()) yield return native.Current; }
            finally
            {
                (native as IDisposable)?.Dispose();
                try { row.Actual = Capture(); pending = null; }
                catch (Exception error) { Errors.Add(error.ToString()); }
            }
        }
        internal static void CompleteTerminal(bool won)
        {
            if (!TrueFinalBossRelentlessScenario.Enabled || pending == null) return;
            try
            {
                pending.Actual = Capture();
                pending.TerminalWon = won;
                pending = null;
            }
            catch (Exception error) { Errors.Add(error.ToString()); }
        }
        internal static void Write(ManualLogSource log)
        {
            if (!Enabled) return;
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                Boundary = "AfterBossActionRemoveDeadCharacters", ForcedSpawnQueueEmpty = ForceRelentless,
                QueueControls, Actions = Records, Errors }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "outer-boss-phase-calibration.mt2f"))) document.Write(output);
            int incomplete = Records.Count(row => row.Actual == null);
            log.LogInfo("OUTER-BOSS-PHASE-CAPTURE actions=" + Records.Count + " pending=" + incomplete + " errors=" + Errors.Count);
            if (Records.Count == 0 || incomplete != 0 || Errors.Count != 0) throw new InvalidOperationException("Native outer-boss phase capture failed.");
            if (ForceRelentless && QueueControls.Count(value => value.GetType().GetProperty("Kind")?.GetValue(value) as string == "ForceSpawnQueueEmpty") != 1)
                throw new InvalidOperationException("Missing explicit native spawn-queue control.");
        }
        [HarmonyPatch(typeof(HeroManager), nameof(HeroManager.PerformBossAction))]
        private static class ActionPatch
        { private static void Postfix(ref IEnumerator __result) { if (Enabled) __result = Observe(__result); } }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.RemoveDeadCharacters))]
        private static class SettlePatch
        {
            private static void Postfix(ref IEnumerator __result)
            { if (Capturing && pending?.NativeReturned == true) __result = Settle(__result, pending); }
        }
    }
}
