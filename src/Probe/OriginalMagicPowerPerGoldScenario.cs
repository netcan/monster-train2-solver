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
    internal static class OriginalMagicPowerPerGoldScenario
    {
        internal const string Name = "original-magic-power-per-gold";
        private const string OwnerId = "74291ecc-cd0d-4958-9311-98fcf0a90b3f";
        private const string AssetKey = "Echoes_AdditionalMagicPowerPerGold";
        private const string UpgradeId = "ModifyMagicPowerPerGold";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static object? Definition { get; private set; }
        internal static object? CurrentDefinition => original == null ? null : Describe(original);
        internal static object? AfterPayment { get; private set; }
        internal static readonly List<Callback> Callbacks = new();

        private static MutatorData? original;
        private static CardState? permanent;
        private static CardState? generated;
        private static int permanentId;
        private static int[] defenders = Array.Empty<int>();
        private static RelicEffectModifyMagicPowerPerGold? liveEffect;

        internal sealed class Callback
        {
            public string Kind { get; set; } = "";
            public bool? Passed { get; set; }
            public bool Completed { get; set; }
            public object Before { get; set; } = null!;
            public object? After { get; set; }
        }

        private sealed class Snapshot
        {
            public int Turn { get; }
            public int Gold { get; }
            public bool Placement { get; }
            public bool Preview { get; }
            public CardObservation Permanent { get; }
            public CardObservation? Generated { get; }

            internal Snapshot(AllGameManagers managers, RelicEffectModifyMagicPowerPerGold effect)
            {
                var save = managers.GetSaveManager();
                var combat = managers.GetCombatManager();
                var tracked = (Dictionary<CardState, CardUpgradeState>)AccessTools.Field(effect.GetType(), "modifiedCardMagicPower").GetValue(effect);
                var deck = save.GetDeckState();
                Turn = combat.GetTurnCount();
                Gold = save.GetGold();
                Placement = combat.IsPlacementPhase;
                Preview = save.PreviewMode;
                Permanent = new CardObservation(permanent!, deck, tracked);
                Generated = generated == null ? null : new CardObservation(generated, deck, tracked);
            }
        }

        private sealed class CardObservation
        {
            public int Id { get; }
            public string DataId { get; }
            public bool InDeck { get; }
            public bool Eligible { get; }
            public bool TrackedByRelic { get; }
            public object[] Upgrades { get; }

            internal CardObservation(CardState card, IReadOnlyList<CardState> deck, Dictionary<CardState, CardUpgradeState> tracked)
            {
                Id = FullBattleTrace.Active!.CardId(card);
                DataId = card.GetCardDataID();
                InDeck = deck.Any(member => ReferenceEquals(member, card));
                Eligible = card.CanScaleWithMagicPower();
                TrackedByRelic = tracked.ContainsKey(card);
                Upgrades = card.GetTemporaryCardStateModifiers().GetCardUpgrades().Where(upgrade => upgrade.GetCardUpgradeDataId() == UpgradeId)
                    .Select(upgrade => new { Id = upgrade.GetCardUpgradeDataId(), Damage = upgrade.GetAttackDamage(),
                        Heal = upgrade.GetAdditionalHeal(), Unique = upgrade.IsUnique(), RemoveOnDiscard = upgrade.GetRemoveOnDiscard(),
                        ExcludeFromClones = upgrade.GetExcludeFromClones() }).ToArray();
            }
        }

        private static object Describe(MutatorData owner)
        {
            var effect = owner.GetEffects().Single();
            return new { Id = owner.GetID(), Key = owner.name, Placement = owner.DisallowInDeploymentPhase,
                Type = effect.GetEffectClassName(), Amount = effect.GetParamInt(), Divisor = effect.GetParamInt2(),
                Conditions = effect.GetEffectConditions().Count };
        }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (original != null) return;
            var save = managers.GetSaveManager();
            var all = save.GetAllGameData();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Original gold-to-power mutator requires an isolated pre-combat save.");
            var mutator = SpawnStatusScalingMechanismBatch.Owners(all).OfType<MutatorData>().Single(owner => owner.GetID() == OwnerId);
            var effect = mutator.GetEffects().Single();
            if (mutator.name != AssetKey || mutator.DisallowInDeploymentPhase || effect.GetEffectClassName() != nameof(RelicEffectModifyMagicPowerPerGold) ||
                effect.GetParamInt() != 1 || effect.GetParamInt2() != 100 || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original gold-to-power mutator definition changed.");
            var treasure = all.FindCardData(OriginalSmeltedTreasureScenario.CardId)!;
            if (treasure.name != "SmeltedTreasure" || treasure.GetEffects()[0].GetEffectStateName() != "CardEffectRewardGold" ||
                treasure.GetEffects()[0].GetParamInt() != -50)
                throw new InvalidOperationException("Original Smelted Treasure no longer spends 50 gold.");
            permanent = save.AddCardToDeck(treasure, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            original = mutator;
            Definition = Describe(mutator);
            save.AddMutator(mutator);
            save.SetGold(100, isTotalReset: true);
            log.LogInfo("ORIGINAL-MAGIC-POWER-PER-GOLD-ACQUIRED unchanged Echoes mutator, permanent Smelted Treasure, and 100 gold.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager();
            var cards = managers.GetCardManager();
            if (permanent == null || liveEffect == null || !managers.GetCombatManager().IsPlacementPhase ||
                !Callbacks.Any(row => row.Kind == "Apply" && row.Completed && row.After is Snapshot after && after.Turn == 0) ||
                save.GetGold() != 100)
                throw new InvalidOperationException("Original gold-to-power mutator did not settle for deployment.");
            var units = OriginalRandomRoomCapacityScenario.CreateDeploymentControls(managers, 3, 1);
            if (!cards.DrawSpecificCard(permanent, useEffectDelay: false))
                throw new InvalidOperationException("Could not draw the permanent gold control.");
            generated = cards.AddNewCard(save.GetAllGameData().FindCardData(permanent.GetCardDataID())!,
                CardPile.HandPile, false, false, animate: false);
            permanentId = FullBattleTrace.Active!.CardId(permanent);
            defenders = units.Select(FullBattleTrace.Active!.CardId).ToArray();
            Prepared = true;
            OriginalRandomRoomCapacityScenario.RefreshDeploymentControls(managers);
            log.LogInfo("ORIGINAL-MAGIC-POWER-PER-GOLD-PREPARED permanent and generated copies of the same spell.");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            var statistics = state.Spawn.Train.Context!.Statistics!;
            for (int floor = 0; floor < defenders.Length; floor++)
            {
                if (statistics.Value(defenders[floor], "TimesPlayed", "ThisBattle") != 0) continue;
                return BattleActionModel.EnumerateSupportedPlays(state, defenders[floor]).First(action => action.RoomIndex == floor);
            }
            if (statistics.Value(permanentId, "TimesPlayed", "ThisBattle") == 0)
            {
                int target = state.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units
                    .Single(unit => unit.SpawnerCardId == defenders[0]).Id;
                return BattleActionModel.EnumerateSupportedPlays(state, permanentId).First(action => action.RoomIndex == 0 && action.TargetUnitId == target);
            }
            if (AfterPayment == null) AfterPayment = new Snapshot(AllGameManagers.Instance!, liveEffect!);
            return null;
        }

        internal static bool IsScenarioAction(PlayCardAction action) => action.CardInstanceId == permanentId || defenders.Contains(action.CardInstanceId);

        private static bool Observe(RelicEffectModifyMagicPowerPerGold effect) => Enabled && permanent != null &&
            effect.GetRelicDataId() == OwnerId && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;

        [HarmonyPatch(typeof(RelicEffectModifyMagicPowerPerGold), nameof(RelicEffectModifyMagicPowerPerGold.TestEffectTurnPhaseTiming))]
        private static class TestPatch
        {
            private static void Prefix(RelicEffectModifyMagicPowerPerGold __instance, out Callback? __state)
            {
                __state = null;
                if (!Observe(__instance)) return;
                liveEffect = __instance;
                __state = new Callback { Kind = "Test", Before = new Snapshot(AllGameManagers.Instance!, __instance) };
                Callbacks.Add(__state);
            }
            private static void Postfix(RelicEffectModifyMagicPowerPerGold __instance, bool __result, Callback? __state)
            {
                if (__state == null) return;
                __state.Passed = __result;
                __state.After = new Snapshot(AllGameManagers.Instance!, __instance);
                __state.Completed = true;
            }
        }

        [HarmonyPatch(typeof(RelicEffectModifyMagicPowerPerGold), nameof(RelicEffectModifyMagicPowerPerGold.ApplyEffectTurnPhaseTiming))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectModifyMagicPowerPerGold __instance, ref IEnumerator __result)
            {
                if (Observe(__instance)) __result = Wrap(__result, __instance);
            }
        }

        private static IEnumerator Wrap(IEnumerator native, RelicEffectModifyMagicPowerPerGold effect)
        {
            var callback = new Callback { Kind = "Apply", Before = new Snapshot(AllGameManagers.Instance!, effect) };
            Callbacks.Add(callback);
            try
            {
                while (native.MoveNext()) yield return native.Current;
                callback.After = new Snapshot(AllGameManagers.Instance!, effect);
                callback.Completed = true;
            }
            finally { (native as IDisposable)?.Dispose(); }
        }
    }
}
#endif
