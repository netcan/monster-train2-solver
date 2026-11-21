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

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSpurnedScenario
    {
        internal const string Name = "original-spurned";
        internal const string FullBattleName = "original-spurned-battle";
        private const string ScenarioKey = "SoulSavior_R4_BossBattle_EstrangedChild_2";
        private const string BossKey = "Boss_SoulSavior_R4_Spurned_ArmorPerDebuff";
        private const string WitherbloomScenarioKey = "SoulSavior_R4_BossBattle_EstrangedChild_3";
        private const string WitherbloomBossKey = "Boss_SoulSavior_R4_Spurned_WitherbloomOnAction";
        private const string DefenderId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private const int RetainPilesEndTurnOnlyFrom = 4;
        internal static bool Component => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool FullBattle => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == FullBattleName;
        internal static bool Enabled => Component || FullBattle;
        private static bool RetainPiles => FullBattle && Environment.GetEnvironmentVariable("MT2_PROBE_SPURNED_CHALLENGE") == "3" &&
            Environment.GetEnvironmentVariable("MT2_PROBE_SPURNED_RETAIN_PILES") == "1";
        internal static bool EndTurnOnly(int turn) => RetainPiles && turn >= RetainPilesEndTurnOnlyFrom;
        internal static object? FullBattleEvidence { get; private set; }
        internal static bool Completed { get; private set; }
        private static bool requested;
        private static bool started;
        private static bool unitPlayed;
        private static bool observingTick;
        private static readonly List<DamageRecord> TickDamage = new();

        private sealed class DamageRecord
        {
            public int Amount { get; set; }
            public CombatUnit Before { get; set; } = null!;
            public CombatUnit? After { get; set; }
        }

        internal static void Request(SaveManager save, ManualLogSource log)
        {
            var (scenarioKey, bossKey) = RequestedBattle();
            if (requested || typeof(CardState).Module.ModuleVersionId != new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") ||
                save.GetAllGameData().FindScenarioDataByName(scenarioKey) is not ScenarioData scenario ||
                scenario.GetOuterTrainBoss(false)?.GetAssetKey() != bossKey)
                throw new InvalidOperationException("Expected the installed original Soul Savior R4 Spurned scenario.");
            requested = true;
            AccessTools.Method(typeof(CheatManager), "Command_StartBattle", new[] { typeof(string) }).Invoke(null, new object[] { scenarioKey });
            log.LogInfo("ORIGINAL-SPURNED-REQUEST installed scenario " + scenarioKey);
        }

        internal static bool Tick(AllGameManagers managers, ManualLogSource log)
        {
            if (started || Completed) return true;
            SaveManager save = managers.GetSaveManager();
            if (!save.InTestScenario || save.GetCurrentScenarioData()?.name != ScenarioKey)
                throw new InvalidOperationException("Original native debug scenario did not load.");
            var boss = managers.GetHeroManager().GetOuterTrainBossCharacter();
            if (boss == null || boss.GetCurrentRoomIndex() < 0) return false;
            if (boss.GetSourceCharacterData().GetAssetKey() != BossKey)
                throw new InvalidOperationException("Native debug scenario loaded another Spurned variant.");
            var rooms = managers.GetRoomManager();
            var room = boss.GetCurrentRoom();
            if (rooms.GetSelectedRoom() != room.GetRoomIndex())
            {
                save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(room.GetRoomIndex()));
                return true;
            }
            var monsters = new List<CharacterState>();
            room.AddCharactersToList(monsters, Team.Type.Monsters);
            var target = monsters.FirstOrDefault(unit => unit.IsAlive && !unit.IsPyreHeart());
            if (target == null)
            {
                if (unitPlayed) throw new InvalidOperationException("The original defender did not reach Spurned's room.");
                var cards = managers.GetCardManager();
                int index = cards.GetHand().FindIndex(card => card.GetCardDataID() == DefenderId);
                if (index < 0) return false;
                var point = room.GetFirstEmptyMonsterPoint();
                if (point == null || !cards.CanPlayHandCard(cards.GetHand()[index], room.GetRoomIndex(), point, null, null, out _) ||
                    !Play(cards, index, point)) throw new InvalidOperationException("The original Steward cannot be deployed beside Spurned.");
                unitPlayed = true;
                return true;
            }
            started = true;
            save.StartCoroutine(Capture(managers, boss, target, log));
            return true;
        }

        internal static void ObserveFullBattle(SaveManager save, ManualLogSource log)
        {
            if (FullBattleEvidence != null) return;
            var (scenarioKey, bossKey) = RequestedBattle();
            ScenarioData scenario = save.GetCurrentScenarioData();
            string boss = scenario.GetOuterTrainBoss(false)?.GetAssetKey() ?? "";
            if (!requested || !save.InTestScenario || scenario.name != scenarioKey || boss != bossKey)
                throw new InvalidOperationException("Expected the installed Spurned battle to enter its original TestScenario.");
            var characters = new HashSet<CharacterData>();
            scenario.GetSpawnPattern().CollectReferencedCharacters(characters);
            string[] companions = characters.Where(character => character.IsCompanionBoss())
                .Select(character => character.GetAssetKey()).OrderBy(key => key, StringComparer.Ordinal).ToArray();
            FullBattleEvidence = new { Scenario = scenario.name, BossKey = boss, CompanionKeys = companions, TestScenario = save.InTestScenario,
                EndTurnOnlyFromTurn = RetainPiles ? RetainPilesEndTurnOnlyFrom : (int?)null };
            log.LogInfo("ORIGINAL-SPURNED-BATTLE loaded " + scenario.name + " boss=" + boss +
                " companions=[" + string.Join(",", companions) + "] testScenario=" + save.InTestScenario);
        }

        private static (string Scenario, string Boss) RequestedBattle()
        {
            if (Component) return (ScenarioKey, BossKey);
            return Environment.GetEnvironmentVariable("MT2_PROBE_SPURNED_CHALLENGE") switch
            {
                null or "2" => (ScenarioKey, BossKey),
                "3" => (WitherbloomScenarioKey, WitherbloomBossKey),
                _ => throw new InvalidOperationException("Unknown original Spurned challenge.")
            };
        }

        private static bool Play(CardManager cards, int index, SpawnPoint point)
        {
            CommonSelectionBehavior.SelectionError error = default;
            return cards.PlayCard(index, point, ref error);
        }

        private static IEnumerator Capture(AllGameManagers managers, CharacterState boss, CharacterState target, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager();
            FullBattleTrace trace = FullBattleTrace.Active!;
            object Snapshot() => new { Boss = trace.CaptureUnit(boss), Target = trace.CaptureUnit(target), Train = trace.CaptureCanonicalTrain() };
            var original = boss.GetSourceCharacterData();
            var attackTrigger = original.GetTriggers().Single(trigger => trigger.GetTrigger() == CharacterTriggerData.Trigger.OnAttacking);
            var definition = attackTrigger.GetEffects().Select(RuleCatalogProbe.Effect).ToArray();
            BossState bossState = boss.GetBossState();
            string phaseBefore = bossState.GetCurrentAttackPhase().ToString();
            if (!boss.CheckAttackConditions()) throw new InvalidOperationException("Original Spurned is not attack-ready in " + phaseBefore + ".");
            object beforeMalaise = Snapshot();
            boss.AddStatusEffect("malaise", 2);
            yield return combat.RunTriggerQueue();
            object afterMalaise = Snapshot();
            boss.RemoveStatusEffect("malaise", -1);
            yield return combat.RunTriggerQueue();
            object afterRemoval = Snapshot();
            boss.AddStatusEffect("malaise", 2);
            yield return combat.RunTriggerQueue();
            object beforeAttack = Snapshot();
            int attacks = 0;
            yield return combat.RunUnitTurn(boss, managers.GetHeroManager(), managers.GetMonsterManager(), boss.GetCurrentRoomIndex(),
                allowTheft: true, perAttackVfxCallback: _ => attacks++);
            object afterAttack = Snapshot();
            var statusManager = managers.GetStatusEffectManager();
            var witherbloom = statusManager.GetStatusEffectDataById("witherbloom");
            var malaise = statusManager.GetStatusEffectDataById("malaise");
            var stage = witherbloom.GetPrimaryTriggerStage();
            var tick = new StatusEffectState.OutputTriggerParams();
            observingTick = true;
            try
            {
                yield return statusManager.TriggerStatusEffectOnUnit(stage, target,
                    new StatusEffectState.InputTriggerParams { associatedCharacter = target }, tick);
            }
            finally { observingTick = false; }
            object afterTick = Snapshot();
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "NativeOriginalSpurnedAttack", GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CardState).Module.ModuleVersionId, Scenario = ScenarioKey, BossKey, TestScenario = save.InTestScenario,
                PhaseBefore = phaseBefore, PhaseAtAttack = bossState.GetCurrentAttackPhase().ToString(),
                Definition = definition, BeforeMalaise = beforeMalaise, AfterMalaise = afterMalaise, AfterRemoval = afterRemoval,
                BeforeAttack = beforeAttack, AfterAttack = afterAttack, AfterTick = afterTick, Attacks = attacks,
                TickStage = stage.ToString(), TickFired = tick.statusTriggered,
                MalaiseRule = StatusRule(malaise), WitherbloomRule = StatusRule(witherbloom), TickDamage
            });
            using var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "spurned-attack.mt2f"));
            archive.Write(output);
            Completed = true;
            log.LogInfo("ORIGINAL-SPURNED-NATIVE attacks=" + attacks + " witherbloomTick=" + tick.statusTriggered);
        }

        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.ApplyDamageToTarget))]
        private static class TickDamagePatch
        {
            private static void Postfix(int damage, CharacterState target, CombatManager.ApplyDamageToTargetParameters parameters,
                ref IEnumerator __result)
            {
                if (observingTick && parameters.damageType == Damage.Type.Poison)
                    __result = Observe(__result, damage, target);
            }

            private static IEnumerator Observe(IEnumerator native, int damage, CharacterState target)
            {
                var trace = FullBattleTrace.Active!;
                var sample = new DamageRecord { Amount = damage, Before = trace.CaptureUnit(target) };
                TickDamage.Add(sample);
                try { while (native.MoveNext()) yield return native.Current; }
                finally { (native as IDisposable)?.Dispose(); sample.After = trace.CaptureUnit(target); }
            }
        }

        private static object StatusRule(StatusEffectData data) => new
        {
            ParamInt = data.GetParamInt(), Stage = data.GetPrimaryTriggerStage().ToString(),
            AdditionalStages = data.GetAdditionalTriggerStages().Select(stage => stage.ToString()).ToArray(),
            RemoveOnTrigger = data.GetRemoveWhenTriggered(), RemoveStackAtEnd = data.GetRemoveStackAtEndOfTurn(),
            RemoveAllAtEnd = data.GetRemoveAtEndOfTurn(), RemoveDuringDeployment = data.GetRemoveDuringDeployment()
        };
    }
}
#endif
