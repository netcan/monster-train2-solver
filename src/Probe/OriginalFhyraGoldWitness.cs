#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalFhyraGoldWitness
    {
        private const string OwnerId = "cc792aac-4b82-40af-bf80-3a83bc511383";
        private const string OwnerKey = "PyreHeartFhyra_ArtifactData";
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_FHYRA_GOLD") == "1";
        internal static bool FullBattle => Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_FHYRA_BATTLE") == "1";
        internal static bool Prepared { get; private set; }
        private static string? pyreId;
        private static int reward;
        private static List<Callback>? recording;

        internal static void Prepare(SaveManager save)
        {
            if (Application.version != "2.3.1" || typeof(CharacterState).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.GetGameSequence() != SaveData.GameSequence.BattleIntro || save.GetMutators().Count != 0)
                throw new InvalidOperationException("Expected a genuine original 2.3.1 battle intro without mutators.");
            var character = save.GetBalanceData().GetPyreHeartCharacterDatas()
                .Single(data => data.GetPyreHeartData().GetPyreArtifact()?.GetID() == OwnerId);
            var definition = character.GetPyreHeartData().GetPyreArtifact();
            var effect = definition.GetEffects().Single();
            pyreId = character.GetID();
            reward = effect.GetParamInt();
            if (character.GetAssetKey() != "PyreHeartFhyra" || !save.GetMetagameSave().HasUnlockedPyreHeart(pyreId) ||
                definition.name != OwnerKey || definition.DisallowInDeploymentPhase || reward != 10 ||
                effect.GetEffectClassName() != nameof(RelicEffectAddGoldOnPyreDamage) || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Expected the unlocked original Fhyra Pyre and its unchanged gold definition.");

            save.SetActiveSavePyreCharacter(pyreId, "", loading: false);
            var active = save.GetActiveSavePyreArtifactState();
            if (save.GetActiveSavePyreCharacterData().GetID() != pyreId || active.GetRelicDataID() != OwnerId ||
                active.GetEffects() is not [RelicEffectAddGoldOnPyreDamage])
                throw new InvalidOperationException("The original Fhyra artifact did not initialize on its actual Pyre.");
            Prepared = true;
        }

        internal static void Write(FullBattleTrace trace, AllGameManagers managers)
        {
            var save = managers.GetSaveManager();
            var combat = managers.GetCombatManager();
            var player = managers.GetPlayerManager();
            var rooms = managers.GetRoomManager();
            var pyre = rooms.GetPyreRoom().GetPyreHeart();
            if (!Prepared || save.PreviewMode || combat.GetTurnCount() != 1 ||
                pyre.GetSourceCharacterData().GetID() != pyreId || save.GetTowerHP() < 3)
                throw new InvalidOperationException("Expected the original live Fhyra Pyre and its first normal turn.");
            var enemies = new List<CharacterState>();
            for (int index = 0; index < rooms.GetNumRooms(); index++)
                rooms.GetRoom(index).AddCharactersToList(enemies, Team.Type.Heroes);
            CharacterState enemy = enemies.First(unit => unit.IsAlive && !unit.IsDestroyed);

            Sample zero = Observe("ZeroFlagged", 0, () => player.AdjustTowerHP(0, triggerDamageTakenRelicEffects: true));
            Sample loss = Observe("UnflaggedLoss", 1, () => player.AdjustTowerHP(-1, triggerDamageTakenRelicEffects: false));
            Sample hit = Observe("NativeAttackTower", 1, () => combat.AttackTower(enemy, save.GetTowerHP() - 1));
            var stages = new[] { "Adjust.Enter", "Dispatcher.Enter", "Fhyra.Enter", "Fhyra.Exit", "Dispatcher.Exit", "Adjust.Exit" };
            if (!zero.Calls.Select(row => row.Stage).SequenceEqual(new[] { "Adjust.Enter", "Adjust.Exit" }) ||
                !loss.Calls.Select(row => row.Stage).SequenceEqual(new[] { "Adjust.Enter", "Adjust.Exit" }) ||
                !hit.Calls.Select(row => row.Stage).SequenceEqual(stages) ||
                hit.Calls[0].Gold != hit.Before.Gold || hit.Calls[1].Health != hit.After.Health ||
                hit.Calls[2].Gold != hit.Before.Gold || hit.Calls[3].Gold != hit.After.Gold ||
                hit.Calls[2].Amount != 1 || hit.After.Gold - hit.Before.Gold != reward ||
                zero.Before.Gold != zero.After.Gold || loss.Before.Gold != loss.After.Gold)
                throw new InvalidOperationException("Native Fhyra gold/health callback order differs.");
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalFhyraGoldOnPyreDamage", GameVersion = Application.version, GameModuleMvid = Module,
                OwnerId, OwnerKey, PyreId = pyreId, PyreUnitId = trace.UnitId(pyre), AttackerId = trace.UnitId(enemy), Reward = reward,
                Rows = new[] { zero, loss, hit }
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "fhyra-gold-on-pyre-damage.mt2f"));
            archive.Write(stream);

            State Snapshot() => new State(save.GetTowerHP(), save.GetGold(), trace.CaptureDecision(), HalfHealthLifecycleCalibration.Rng());
            Sample Observe(string kind, int damage, Action apply)
            {
                State before = Snapshot();
                var calls = new List<Callback>();
                recording = calls;
                try { apply(); }
                finally { recording = null; }
                State after = Snapshot();
                string? rngDifference = HalfHealthLifecycleCalibration.Difference(before.Decision.RngStreams, after.Decision.RngStreams);
                if (before.Health - after.Health != damage || rngDifference != null)
                    throw new InvalidOperationException($"Native Fhyra sample differs: {kind}, HP {before.Health}->{after.Health}, " +
                        $"expected loss {damage}, RNG {rngDifference ?? "unchanged"}.");
                return new Sample(kind, damage, before, after, calls.ToArray());
            }
        }

        private sealed class State(int health, int gold, BattleTurnState decision, object rng)
        {
            public int Health { get; } = health;
            public int Gold { get; } = gold;
            public BattleTurnState Decision { get; } = decision;
            public object Rng { get; } = rng;
        }

        private sealed class Callback(string stage, int amount, bool? triggerDamageTakenRelicEffects, int health, int gold)
        {
            public string Stage { get; } = stage;
            public int Amount { get; } = amount;
            public bool? TriggerDamageTakenRelicEffects { get; } = triggerDamageTakenRelicEffects;
            public int Health { get; } = health;
            public int Gold { get; } = gold;
        }

        private sealed class Sample(string kind, int damage, State before, State after, Callback[] calls)
        {
            public string Kind { get; } = kind;
            public int Damage { get; } = damage;
            public State Before { get; } = before;
            public State After { get; } = after;
            public Callback[] Calls { get; } = calls;
        }

        private static void Record(string stage, int amount, bool? triggers = null)
        {
            if (recording == null) return;
            var save = AllGameManagers.Instance.GetSaveManager();
            recording.Add(new Callback(stage, amount, triggers, save.GetTowerHP(), save.GetGold()));
        }

        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.AdjustTowerHP), new[] { typeof(int), typeof(bool) })]
        private static class Adjust
        {
            private static bool Prepare() => Enabled;
            private static void Prefix(int addHP, bool triggerDamageTakenRelicEffects) => Record("Adjust.Enter", addHP, triggerDamageTakenRelicEffects);
            private static void Postfix(int addHP, bool triggerDamageTakenRelicEffects) => Record("Adjust.Exit", addHP, triggerDamageTakenRelicEffects);
        }

        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.ApplyTowerDamageTakenRelicEffects))]
        private static class Dispatcher
        {
            private static bool Prepare() => Enabled;
            private static void Prefix(int towerDamage) => Record("Dispatcher.Enter", towerDamage);
            private static void Postfix(int towerDamage) => Record("Dispatcher.Exit", towerDamage);
        }

        [HarmonyPatch(typeof(RelicEffectAddGoldOnPyreDamage), nameof(RelicEffectAddGoldOnPyreDamage.TriggerEffectOnDamage))]
        private static class Fhyra
        {
            private static bool Prepare() => Enabled;
            private static void Prefix(int damage) => Record("Fhyra.Enter", damage);
            private static void Postfix(int damage) => Record("Fhyra.Exit", damage);
        }
    }
}
#endif
