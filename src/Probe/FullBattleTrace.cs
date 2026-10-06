using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    // Capture the actual whole battle while comparing the independently modeled room stages.
    // An unsupported stage is evidence of missing coverage, never counted as a pass.
    internal sealed class FullBattleTrace
    {
        private readonly ManualLogSource log;
        private readonly UnitPlayModelProbe projection;
        private readonly CardCycleProbe cardCycles;
        private readonly TrainCombatProbe trainCombat;
        private readonly Dictionary<CharacterState, int> identities = new Dictionary<CharacterState, int>();
        private readonly List<StageRecord> stages = new List<StageRecord>();
        private readonly List<object> checkpoints = new List<object>();
        private int nextId = 1;
        private bool calibrated;
        internal static FullBattleTrace? Active { get; private set; }
        internal bool? NativeWon { get; private set; }
        internal int CaptureFailures { get; private set; }
        internal int Mismatches => stages.Count(stage => stage.Difference != null) + cardCycles.Mismatches + trainCombat.Mismatches;
        internal int Unsupported => stages.Count(stage => !stage.Predicted.Supported) + cardCycles.Unsupported + trainCombat.Unsupported;
        internal int Pending => stages.Count(stage => stage.Actual == null) + cardCycles.Records.Count(record => record.Actual == null) +
            trainCombat.Records.Count(record => record.Actual == null);

        internal FullBattleTrace(ManualLogSource log)
        {
            this.log = log;
            projection = new UnitPlayModelProbe(log);
            cardCycles = new CardCycleProbe(log);
            trainCombat = new TrainCombatProbe(log, this);
            Active = this;
        }

        internal void Checkpoint(string label)
        {
            AllGameManagers? managers = AllGameManagers.Instance;
            if (managers == null) return;
            SaveManager save = managers.GetSaveManager();
            CombatManager? combat = managers.GetCombatManager();
            CardManager? cards = managers.GetCardManager();
            if (save == null || combat == null || cards == null || save.PreviewMode) return;
            if (!calibrated)
            {
                RngCalibration.Capture();
                calibrated = true;
            }
            checkpoints.Add(new { Label = label, State = projection.Capture(managers, save, combat, cards) });
        }

        internal RoomCombatState Capture(RoomState room)
        {
            AllGameManagers managers = AllGameManagers.Instance ?? throw new InvalidOperationException("No game managers.");
            var interactions = new List<string>();
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager() ?? throw new InvalidOperationException("No combat manager.");
            HeroManager heroes = managers.GetHeroManager() ?? throw new InvalidOperationException("No enemy manager.");
            if (!room.GetCombatAllowed()) interactions.Add("Disabled room combat");
            if (save.GetCollectedRelics().Count != 0) interactions.Add("Collected relic effects");
            if (save.GetMutators().Count != 0) interactions.Add("Mutator effects");
            if (room.Attachments.Count > 0) interactions.Add("Room attachments");
            if (room.GetCurrentCorruption() > 0) interactions.Add("Room corruption");
            CharacterState? outerBoss = heroes.GetOuterTrainBossCharacter();
            if (outerBoss != null && outerBoss.IsAlive && outerBoss.GetCurrentRoomIndex() == room.GetRoomIndex())
                interactions.Add("Outer train boss");
            if (heroes.HasNextBoss()) interactions.Add("Queued final bosses");
            var characters = new List<CharacterState>();
            room.AddCharactersToList(characters, Team.Type.Heroes);
            room.AddCharactersToList(characters, Team.Type.Monsters);
            var units = new List<CombatUnit>();
            foreach (CharacterState character in characters)
            {
                if (!character.IsAlive || character.IsDestroyed) continue;
                if (!identities.TryGetValue(character, out int id)) identities.Add(character, id = nextId++);
                if (character.GetTriggers().Count > 0)
                    interactions.Add(character.GetSourceCharacterData().GetAssetKey() + " triggers: " +
                        string.Join(",", character.GetTriggers().Select(trigger => trigger.GetTrigger().ToString())));
                if (character.GetRoomStateModifiers().Count > 0)
                    interactions.Add(character.GetSourceCharacterData().GetAssetKey() + " room modifiers");
                if (character.GetEquipment().Count > 0)
                    interactions.Add(character.GetSourceCharacterData().GetAssetKey() + " equipment");
                CardState? card = character.GetSpawnerCard();
                if (card != null && (card.GetTraitStates().Count > 0 || card.GetTriggers().Count > 0))
                    interactions.Add(character.GetSourceCharacterData().GetAssetKey() + " card traits/triggers");
                var nativeStatuses = new List<CharacterState.StatusEffectStack>();
                character.GetStatusEffects(ref nativeStatuses);
                var statuses = new List<CombatStatus>();
                foreach (CharacterState.StatusEffectStack status in nativeStatuses)
                {
                    StatusEffectState rule = status.State;
                    if (status.Count <= 0) continue;
                    if (rule.GetRemoveWhenTriggeredAfterCardPlayed() || rule.GetRemoveAtEndOfTurnIfTriggered())
                        interactions.Add(rule.GetStatusId() + " delayed status removal");
                    statuses.Add(new CombatStatus(rule.GetStatusId(), status.Count, rule.GetParamInt(),
                        rule.GetRemoveWhenTriggered(), rule.GetRemoveStackAtEndOfTurn(), rule.GetRemoveAtEndOfTurn(),
                        rule.GetRemoveAtEndOfTurnAfterPostCombat(), rule.PreventRemovalDuringRelentlessPhase,
                        rule.GetSkipTriggerDuringDeployment(), rule.GetRemoveDuringDeployment()));
                }
                CombatTeam team = character.GetTeamType() == Team.Type.Heroes ? CombatTeam.Enemy : CombatTeam.Player;
                bool endsBattle = team == CombatTeam.Enemy &&
                    (character.IsMiniboss() || character.IsOuterTrainBoss()) &&
                    heroes.FindPairedCompanionBoss(character) == null;
                units.Add(new CombatUnit(id, character.GetSourceCharacterData()?.GetAssetKey() ?? "",
                    team, character.GetAttackDamageWithoutStatusEffectBuffs(), character.GetHP(), character.GetMaxHP(),
                    character.GetCanAttack(), character.IsPyreHeart(), endsBattle, statuses));
            }
            return new RoomCombatState(room.GetRoomIndex(), combat.IsPlacementPhase, units,
                interactions.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        private IEnumerator Wrap(IEnumerator native, RoomState room, string kind)
        {
            StageRecord? record = null;
            try
            {
                AllGameManagers? managers = AllGameManagers.Instance;
                if (managers != null && !managers.GetSaveManager().PreviewMode && NativeWon == null)
                {
                    RoomCombatState before = Capture(room);
                    record = new StageRecord
                    {
                        Index = stages.Count,
                        Turn = managers.GetCombatManager()!.GetTurnCount(),
                        Kind = kind,
                        Room = room,
                        Before = before,
                        Predicted = kind == "Exchange" ? RoomCombatModel.Exchange(before) : RoomCombatModel.Resolve(before)
                    };
                    stages.Add(record);
                }
            }
            catch (Exception error) { CaptureFailure(error); }
            try
            {
                while (native.MoveNext()) yield return native.Current;
            }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (record != null) Complete(record);
            }
        }

        private void Complete(StageRecord stage)
        {
            if (stage.Actual != null) return;
            try
            {
                stage.Actual = Capture(stage.Room);
                if (stage.Predicted.Supported)
                {
                    JToken expected = JToken.FromObject(stage.Predicted.State!.Units);
                    JToken actual = JToken.FromObject(stage.Actual.Units);
                    stage.Difference = JToken.DeepEquals(expected, actual) ? null : "Unit states differ";
                    log.LogInfo("BATTLE-MODEL-" + (stage.Difference == null ? "MATCH" : "MISMATCH") +
                        " index=" + stage.Index + " kind=" + stage.Kind + " turn=" + stage.Turn +
                        " room=" + stage.Before.RoomIndex + " rounds=" + stage.Predicted.Rounds);
                }
                else log.LogInfo("BATTLE-MODEL-UNSUPPORTED index=" + stage.Index + " reason=" +
                    stage.Predicted.UnsupportedReason);
            }
            catch (Exception error) { CaptureFailure(error); }
        }

        internal void CaptureFailure(Exception error)
        {
            CaptureFailures++;
            log.LogError("BATTLE-CAPTURE-FAIL " + error);
        }

        private void Stop(bool won)
        {
            if (NativeWon != null) return;
            NativeWon = won;
            foreach (StageRecord stage in stages.Where(stage => stage.Actual == null).ToArray()) Complete(stage);
            trainCombat.CompletePending();
            Checkpoint("terminal");
            log.LogInfo("BATTLE-TERMINAL won=" + won + " pyre=" + AllGameManagers.Instance!.GetSaveManager().GetTowerHP());
            Write();
        }

        internal string Write()
        {
            string path = Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "full-battle.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(new
            {
                Schema = 1,
                GameVersion = Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                NativeWon,
                CaptureFailures,
                Mismatches,
                Unsupported,
                Pending,
                Stages = stages,
                CardCycles = cardCycles.Records,
                TrainPhases = trainCombat.Records,
                Checkpoints = checkpoints
            }, Formatting.Indented));
            return path;
        }

        private sealed class StageRecord
        {
            public int Index { get; set; }
            public int Turn { get; set; }
            public string Kind { get; set; } = "";
            [JsonIgnore] public RoomState Room { get; set; } = null!;
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatResult Predicted { get; set; } = null!;
            public RoomCombatState? Actual { get; set; }
            public string? Difference { get; set; }
        }

        [HarmonyPatch(typeof(CombatManager), "DoUnitCombat")]
        private static class ExchangePatch
        {
            private static void Postfix(RoomState room, ref IEnumerator __result)
            { if (Active != null) __result = Active.Wrap(__result, room, "Exchange"); }
        }

        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.DoRoomCombat))]
        private static class RoomPatch
        {
            private static void Postfix(RoomState room, ref IEnumerator __result)
            { if (Active != null) __result = Active.Wrap(__result, room, "Room"); }
        }

        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.StopCombat))]
        private static class StopPatch
        {
            private static void Postfix(bool combatWon, ref IEnumerator __result)
            { if (Active != null) __result = Stop(__result, combatWon); }

            private static IEnumerator Stop(IEnumerator native, bool won)
            {
                Active?.Stop(won);
                while (native.MoveNext()) yield return native.Current;
            }
        }
    }
}
