#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSmallDeckDamageScenario
    {
        internal const string Name = "original-small-deck-damage";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static object? Definition { get; private set; }
        internal static object? CurrentDefinition => original == null ? null : Describe(original);
        internal static object? Witness { get; private set; }
        internal static readonly List<Callback> Callbacks = new();

        private static EndlessMutatorData? original;
        private static int[] ids = Array.Empty<int>();
        private static Callback? active;

        internal sealed class Callback
        {
            public string Kind { get; set; } = "";
            public bool? Passed { get; set; }
            public bool Completed { get; set; }
            public object Before { get; set; } = null!;
            public object? After { get; set; }
            public List<object> DamageCalls { get; } = new();
        }

        private sealed class Snapshot
        {
            public int Turn { get; }
            public bool Placement { get; }
            public bool Preview { get; }
            public int DrawPileCount { get; }
            public int VisibleDeckCount { get; }
            public int SaveHP { get; }
            public int? PyreHP { get; }
            public int MaxHP { get; }

            internal Snapshot(AllGameManagers managers)
            {
                var save = managers.GetSaveManager();
                var combat = managers.GetCombatManager();
                Turn = combat.GetTurnCount();
                Placement = combat.IsPlacementPhase;
                Preview = save.PreviewMode;
                DrawPileCount = managers.GetCardManager().GetNumCardsInDeck();
                VisibleDeckCount = save.GetVisibleDeckCount();
                SaveHP = save.GetTowerHP();
                PyreHP = managers.GetRoomManager().GetPyreRoom().GetPyreHeart()?.GetHP();
                MaxHP = save.GetMaxTowerHP();
            }
        }

        private static object Describe(EndlessMutatorData owner)
        {
            var effect = owner.GetEffects().Single();
            return new { Id = owner.GetID(), Key = owner.name, Type = effect.GetEffectClassName(),
                Threshold = effect.GetParamInt(), PerCard = effect.GetParamInt2(), Placement = owner.DisallowInDeploymentPhase,
                Conditions = effect.GetEffectConditions().Count, ControlledAcquisition = true };
        }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (original != null) return;
            var save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetMutators().Count != 0 || save.GetCovenants().Count != 0 || save.GetTowerHP() <= 30)
                throw new InvalidOperationException("Original small-deck damage requires an isolated first battle with enough Pyre health.");
            (string id, string key, int threshold, int perCard) = Environment.GetEnvironmentVariable("MT2_PROBE_SMALL_DECK_DAMAGE") switch
            {
                "Threshold30" => ("8e9444da-bd9b-4f00-b348-bb3e21ad1d1d", "DamagePerCardUnderX_Artifact_EndlessMutatorData", 30, 1),
                "Threshold15" => ("2fd1f357-9e96-409a-8979-2f3d1f5e776c", "DamageFromCardsUnderEndlessMutatorData", 15, 2),
                _ => throw new InvalidOperationException("Unknown original small-deck damage source.")
            };
            var owner = save.GetAllGameData().FindEndlessMutatorData(id)!;
            var effect = owner.GetEffects().Single();
            if (owner.name != key || owner.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != nameof(RelicEffectDamageForNumCardsStartOfBattle) ||
                effect.GetParamInt() != threshold || effect.GetParamInt2() != perCard || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original small-deck damage definition changed.");

            var deck = save.GetDeckState().ToArray();
            foreach (var card in deck.Reverse().Where(card => !card.IsChampionCard()).Take(Math.Max(0, deck.Length - 10)))
                save.RemoveCardFromDeck(card);
            if (save.GetDeckState().Count == 0 || save.GetDeckState().Count > 10)
                throw new InvalidOperationException("The isolated permanent deck does not meet the small-deck witness bound.");
            original = owner;
            Definition = Describe(owner);
            save.AddRelic(owner);
            log.LogInfo("ORIGINAL-SMALL-DECK-DAMAGE-ACQUIRED " + key + " through native controlled relic acquisition.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var application = Callbacks.SingleOrDefault(row => row.Kind == "Apply" && row.Completed);
            var firstDecision = new Snapshot(managers);
            if (original == null || !firstDecision.Placement || firstDecision.Preview ||
                application?.Before is not Snapshot before || application.After is not Snapshot after ||
                after.SaveHP <= 0 || after.SaveHP >= before.SaveHP ||
                after.SaveHP != firstDecision.SaveHP || after.SaveHP != firstDecision.PyreHP)
                throw new InvalidOperationException("The first settled input must contain the surviving Pyre after native opening damage.");
            var controls = OriginalRandomRoomCapacityScenario.CreateDeploymentControls(managers, 3, 1);
            ids = controls.Select(FullBattleTrace.Active!.CardId).ToArray();
            Witness = new { CardIds = ids, Floors = new[] { 0, 1, 2 }, FirstDecision = firstDecision };
            Prepared = true;
            OriginalRandomRoomCapacityScenario.RefreshDeploymentControls(managers);
            log.LogInfo($"ORIGINAL-SMALL-DECK-DAMAGE-PREPARED native Pyre HP {before.SaveHP}->{firstDecision.SaveHP}.");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            int next = Array.FindIndex(ids, id => state.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (next < 0) return null;
            if (state.Spawn.Turn != 0) throw new InvalidOperationException("Small-deck controls must deploy on turn zero.");
            return BattleActionModel.EnumerateSupportedPlays(state, ids[next]).First(action => action.RoomIndex == next);
        }

        internal static bool IsScenarioAction(PlayCardAction action) => ids.Contains(action.CardInstanceId);

        private static bool Observe(RelicEffectDamageForNumCardsStartOfBattle effect) => Enabled && original != null &&
            effect.GetRelicDataId() == original.GetID() && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;

        [HarmonyPatch(typeof(RelicEffectDamageForNumCardsStartOfBattle), nameof(RelicEffectDamageForNumCardsStartOfBattle.TestEffectStartOfBattle))]
        private static class TestPatch
        {
            private static void Prefix(RelicEffectDamageForNumCardsStartOfBattle __instance, out Callback? __state)
            {
                __state = null;
                if (!Observe(__instance)) return;
                __state = new Callback { Kind = "Test", Before = new Snapshot(AllGameManagers.Instance!) };
                Callbacks.Add(__state);
            }
            private static void Postfix(bool __result, Callback? __state)
            {
                if (__state == null) return;
                __state.Passed = __result;
                __state.After = new Snapshot(AllGameManagers.Instance!);
                __state.Completed = true;
            }
        }

        [HarmonyPatch(typeof(RelicEffectDamageForNumCardsStartOfBattle), nameof(RelicEffectDamageForNumCardsStartOfBattle.ApplyEffectStartOfBattle))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectDamageForNumCardsStartOfBattle __instance, ref IEnumerator __result)
            {
                if (Observe(__instance)) __result = Wrap(__result);
            }
        }

        private static IEnumerator Wrap(IEnumerator native)
        {
            var row = new Callback { Kind = "Apply", Before = new Snapshot(AllGameManagers.Instance!) };
            Callbacks.Add(row);
            active = row;
            try
            {
                while (native.MoveNext()) yield return native.Current;
                row.After = new Snapshot(AllGameManagers.Instance!);
                row.Completed = true;
            }
            finally { active = null; (native as IDisposable)?.Dispose(); }
        }

        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.AdjustTowerHP), new[] { typeof(int), typeof(bool) })]
        private static class DamagePatch
        {
            private static void Prefix(int addHP, bool triggerDamageTakenRelicEffects) =>
                active?.DamageCalls.Add(new { Amount = addHP, TriggerDamageTakenRelicEffects = triggerDamageTakenRelicEffects });
        }
    }
}
#endif
