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
    internal static class OriginalGoldieScenario
    {
        internal const string Name = "original-goldie";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static object? Definition { get; private set; }
        internal static object? CurrentDefinition => original == null ? null : Describe(original);
        internal static object? Witness => !Prepared ? null : new { VentId = ventId, VentDataId = vent!.GetCardDataID(),
            DefenderIds = defenders, DeploymentRetain = deployment, FirstNormalDecision = firstNormal, RetainReturn = retainReturn };
        internal static readonly List<Record> Callbacks = new();

        private static SoulData? original;
        private static CardState? vent;
        private static int ventId;
        private static int[] defenders = Array.Empty<int>();
        private static bool castRequested;
        private static object? deployment;
        private static object? firstNormal;
        private static object? retainReturn;

        internal sealed class Record
        {
            public string Kind { get; set; } = "";
            public bool? WasPlayed { get; set; }
            public bool Completed { get; set; }
            public object Before { get; set; } = null!;
            public object? After { get; set; }
        }

        private static object Snapshot()
        {
            var managers = AllGameManagers.Instance!;
            var save = managers.GetSaveManager();
            var cards = managers.GetCardManager();
            var combat = managers.GetCombatManager();
            var trait = vent!.GetTraitStates().OfType<CardTraitScalingRewardGold>().Single();
            return new { Turn = combat.GetTurnCount(), Placement = combat.IsPlacementPhase, Gold = save.GetGold(),
                Energy = managers.GetPlayerManager().GetEnergy(), CardId = ventId,
                InHand = cards.GetHand().Any(card => ReferenceEquals(vent, card)),
                InDiscard = cards.GetDiscardPile().Any(card => ReferenceEquals(vent, card)),
                InSavedDeck = save.GetDeckState().Any(card => ReferenceEquals(vent, card)),
                UnmodifiedPlayedCost = managers.GetCardStatistics().GetStatValue(trait.StatValueData) };
        }

        private static object Describe(SoulData soul)
        {
            var relic = soul.GetEffects().Single();
            var upgrade = soul.GetSoulCardUpgrade()!;
            var reward = upgrade.GetTraitDataUpgrades().Single(trait => trait.GetTraitStateName() == nameof(CardTraitScalingRewardGold));
            return new { Id = soul.GetID(), Key = soul.name, Tier = soul.GetTierLevel(), Type = relic.GetEffectClassName(),
                UpgradeId = upgrade.GetID(), UpgradeKey = upgrade.name,
                Traits = upgrade.GetTraitDataUpgrades().Select(trait => trait.GetTraitStateName()).ToArray(),
                CoinsPerUnit = reward.GetParamInt(), Stat = reward.GetParamTrackedValue().ToString(),
                Duration = reward.GetParamEntryDuration().ToString(), CardType = reward.GetParamCardType().ToString(),
                OnCast = upgrade.GetCardTriggerUpgrades().Where(trigger => trigger.GetTrigger() == CardTriggerType.OnCast)
                    .SelectMany(trigger => trigger.GetCardEffects()).Select(effect => new { Type = effect.GetEffectStateName(),
                        Target = effect.GetTargetMode().ToString(), Team = effect.GetTargetTeamType().ToString(),
                        Amount = effect.GetParamInt(), Required = effect.GetParamBool() }).ToArray() };
        }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (original != null) return;
            var save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetSelectedSouls().Count != 0 || save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Original Goldie requires a fresh isolated save.");
            int tier = int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_GOLDIE_TIER")!);
            string id = tier switch
            {
                1 => "63603526-0d67-42a1-8582-ab4a167a9560",
                2 => "8b42f714-7a5d-4b27-8afd-09b329cb02dc",
                3 => "ff530466-0030-41e4-b054-57e3f5dcec27",
                _ => throw new InvalidOperationException("Expected Goldie tier 1, 2 or 3.")
            };
            var all = save.GetAllGameData();
            var soul = all.FindSoulData(id)!;
            var upgrade = soul.GetSoulCardUpgrade()!;
            var reward = upgrade.GetTraitDataUpgrades().Single(trait => trait.GetTraitStateName() == nameof(CardTraitScalingRewardGold));
            var relic = soul.GetEffects().Single();
            if (soul.name != "GoldieT" + tier || soul.GetTierLevel() != tier || relic.GetEffectClassName() != nameof(RelicEffectCardUpgrade) ||
                relic.GetParamCardType() != CardType.Spell || reward.GetParamInt() != 10 * tier ||
                reward.GetParamTrackedValue() != CardStatistics.TrackedValueType.UnmodifiedPlayedCost ||
                reward.GetParamEntryDuration() != CardStatistics.EntryDuration.ThisTurn ||
                reward.GetParamCardType() != CardStatistics.CardTypeTarget.Any)
                throw new InvalidOperationException("The original Goldie definition changed.");
            var originalVent = all.FindCardData(OriginalXDamageCardsScenario.VentId)!;
            if (originalVent.name != "Vent" || originalVent.GetCostType() != CardData.CostType.ConsumeRemainingEnergy)
                throw new InvalidOperationException("Original Goldie control Vent must retain its native X cost.");
            vent = save.AddCardToDeck(originalVent, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddSelectedSoul(soul);
            SoulHelper.AttachSoulEffectToCard(save.GetSelectedSouls().Single(owner => owner.GetRelicDataID() == id), vent, save);
            if (!vent.GetCardStateModifiers().HasUpgrade(upgrade.GetID()))
                throw new InvalidOperationException("Native Soul helper did not attach the original Goldie upgrade.");
            original = soul;
            Definition = Describe(soul);
            log.LogInfo("ORIGINAL-GOLDIE-ACQUIRED " + soul.name + " on original Vent using the native Soul helper.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager();
            var cards = managers.GetCardManager();
            if (original == null || vent == null || !managers.GetCombatManager().IsPlacementPhase ||
                !cards.GetAllCards(new List<CardState>()).Any(card => ReferenceEquals(vent, card)))
                throw new InvalidOperationException("Original Goldie Vent did not reach the native deployment input.");
            var controls = OriginalRandomRoomCapacityScenario.CreateDeploymentControls(managers, 3, 1);
            if (!cards.DrawSpecificCard(vent, useEffectDelay: false))
                throw new InvalidOperationException("Could not draw the original Goldie Vent.");
            // Freeze only the deployment draw. Goldie's Retain applies after Vent is played.
            var freeze = new CardTraitData();
            freeze.Setup("CardTraitFreeze");
            vent.AddTemporaryTrait(freeze, managers.GetCoreManagers());
            ventId = FullBattleTrace.Active!.CardId(vent);
            defenders = controls.Select(FullBattleTrace.Active!.CardId).ToArray();
            deployment = Snapshot();
            Prepared = true;
            OriginalRandomRoomCapacityScenario.RefreshDeploymentControls(managers);
            log.LogInfo("ORIGINAL-GOLDIE-PREPARED Vent is ready for three Ember on the first normal turn.");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            var statistics = state.Spawn.Train.Context!.Statistics!;
            for (int floor = 0; floor < defenders.Length; floor++)
            {
                if (statistics.Value(defenders[floor], "TimesPlayed", "ThisBattle") == 0)
                    return BattleActionModel.EnumerateSupportedPlays(state, defenders[floor]).First(action => action.RoomIndex == floor);
            }
            if (state.Spawn.Turn == 0) return null;
            if (castRequested)
            {
                if (state.Spawn.Turn == 2 && retainReturn == null) retainReturn = Snapshot();
                return null;
            }
            if (state.Spawn.Turn != 1 || state.Energy != 3)
                throw new InvalidOperationException("Original Goldie Vent must spend the first normal turn's three Ember.");
            firstNormal = Snapshot();
            var play = BattleActionModel.EnumerateSupportedPlays(state, ventId).First(action =>
                state.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex).Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0));
            castRequested = true;
            return play;
        }

        internal static bool IsScenarioAction(PlayCardAction action) => action.CardInstanceId == ventId || defenders.Contains(action.CardInstanceId);

        [HarmonyPatch(typeof(CardTraitScalingRewardGold), nameof(CardTraitScalingRewardGold.OnCardDiscarded))]
        private static class DiscardPatch
        {
            private static void Postfix(CardManager.DiscardCardParams discardCardParams, ref IEnumerator __result)
            {
                if (Prepared && ReferenceEquals(discardCardParams.discardCard, vent) && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Wrap(__result, "PlayedDiscard", discardCardParams.wasPlayed);
            }
        }

        [HarmonyPatch(typeof(CardEffectRewardGold), nameof(CardEffectRewardGold.ApplyEffect))]
        private static class OnCastGoldPatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (Prepared && castRequested && !AllGameManagers.Instance!.GetSaveManager().PreviewMode &&
                    (ReferenceEquals(cardEffectState.GetParentCardState(), vent) || ReferenceEquals(cardEffectParams.playedCard, vent)))
                    __result = Wrap(__result, "OnCastReward", null);
            }
        }

        private static IEnumerator Wrap(IEnumerator native, string kind, bool? wasPlayed)
        {
            var row = new Record { Kind = kind, WasPlayed = wasPlayed, Before = Snapshot() };
            Callbacks.Add(row);
            try
            {
                while (native.MoveNext()) yield return native.Current;
                row.After = Snapshot();
                row.Completed = true;
            }
            finally { (native as IDisposable)?.Dispose(); }
        }
    }
}
#endif
