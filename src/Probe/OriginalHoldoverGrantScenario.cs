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
    internal static class OriginalHoldoverGrantScenario
    {
        internal const string Name = "original-holdover-grant";
        internal const string TorchId = "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b";
        internal const string StingId = "356a00a4-010b-462b-b683-2ef296d076a8";
        private const string StingRelic = "5e5d863b-514c-4026-a9b0-ffe56af2a3eb";
        private static MutatorData? original;
        private static int[] defenders = Array.Empty<int>();
        internal static int TorchCardId { get; private set; }
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired => original != null;
        internal static bool Prepared { get; private set; }
        internal static object? Definition { get; private set; }
        internal static object? CurrentDefinition => original == null ? null : Describe(original);
        internal static List<Callback> Callbacks { get; } = new();

        internal sealed class Card
        {
            public int Id { get; set; }
            public string DataId { get; set; } = "";
            public string Type { get; set; } = "";
            public bool Champion { get; set; }
            public bool[] Holdover { get; set; } = Array.Empty<bool>();
        }

        internal sealed class Callback
        {
            public string Kind { get; set; } = "";
            public int Turn { get; set; }
            public Card[] Before { get; set; } = Array.Empty<Card>();
            public Card[]? After { get; set; }
            public bool? Fired { get; set; }
        }

        private static object Describe(MutatorData owner)
        {
            var effect = owner.GetEffects().Single();
            return new { Id = owner.GetID(), Key = owner.name, PlacementExcluded = owner.DisallowInDeploymentPhase,
                Type = effect.GetEffectClassName(), CardType = effect.GetParamCardType().ToString(),
                SourceTeam = effect.GetParamSourceTeam().ToString(), Conditions = effect.GetEffectConditions().Count,
                Traits = effect.GetTraits().Select(trait => trait.GetTraitStateName()).ToArray(),
                ExcludedTraits = effect.GetExcludedTraits().Select(trait => trait.GetTraitStateName()).ToArray() };
        }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            var save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("The original Holdover mutator must be acquired before battle.");
            var all = save.GetAllGameData();
            var owner = all.FindMutatorData(RelicHoldoverGrantModel.OwnerId) ??
                throw new InvalidOperationException("Missing original Holdover mutator.");
            var effect = owner.GetEffects().Single();
            if (owner.name != RelicHoldoverGrantModel.OwnerKey || owner.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != nameof(RelicEffectAddTraitToHand) || effect.GetParamCardType() != CardType.Spell ||
                effect.GetParamSourceTeam() != Team.Type.Monsters || effect.GetEffectConditions().Count != 0 ||
                effect.GetTraits().Single().GetTraitStateName() != CardRetainModel.Trait || effect.GetExcludedTraits().Count != 0 ||
                save.GetMutators().Any(item => item.GetRelicDataID() == RelicHoldoverGrantModel.OwnerId))
                throw new InvalidOperationException("Original Holdover mutator differs.");
            var sting = all.GetAllCollectableRelicData().Single(relic => relic.GetID() == StingRelic);
            if (sting.name != "AddStingToHand" || !sting.DisallowInDeploymentPhase ||
                sting.GetEffects().Single().GetEffectClassName() != nameof(RelicEffectAddBattleCardToHand) ||
                save.GetCollectedRelics().Any(item => item.GetRelicDataID() == StingRelic))
                throw new InvalidOperationException("Original after-draw Sting data differs.");
            save.AddRelic(sting);
            original = owner;
            Definition = Describe(owner);
            save.AddMutator(owner);
            log.LogInfo("ORIGINAL-HOLDOVER-GRANT-ACQUIRED native Holdover mutator and after-draw Sting.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (!Acquired || !managers.GetCombatManager().IsPlacementPhase)
                throw new InvalidOperationException("Holdover controls require the first deployment.");
            var trace = FullBattleTrace.Active!;
            defenders = OriginalRandomRoomCapacityScenario.CreateDeploymentControls(managers, 3, 1)
                .Select(trace.CardId).ToArray();
            CardManager cards = managers.GetCardManager();
            CardData torch = managers.GetSaveManager().GetAllGameData().FindCardData(TorchId)!;
            var carrier = cards.AddNewCard(torch, CardPile.DeckPileTop, false, false, animate: false);
            if (carrier == null || carrier.HasTrait<CardTraitRetain>())
                throw new InvalidOperationException("Native Torch must start in the deck without Holdover.");
            TorchCardId = trace.CardId(carrier);
            OriginalRandomRoomCapacityScenario.RefreshDeploymentControls(managers);
            Prepared = true;
            log.LogInfo("ORIGINAL-HOLDOVER-GRANT-PREPARED unchanged original Torch and durable Steward controls.");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            var context = state.Spawn.Train.Context!;
            int floor = Array.FindIndex(defenders, id => context.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (floor >= 0)
                return BattleActionModel.EnumerateActions(state, defenders[floor]).SupportedActions.FirstOrDefault(action =>
                    action.RoomIndex == floor);
            if (context.Statistics!.Value(TorchCardId, "TimesPlayed", "ThisBattle") != 0 ||
                !context.Cards.Hand.Any(card => card.InstanceId == TorchCardId)) return null;
            var heroes = state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0).Select(unit => unit.Id).ToHashSet();
            return BattleActionModel.EnumerateActions(state, TorchCardId).SupportedActions.FirstOrDefault(action =>
                heroes.Contains(action.TargetUnitId));
        }

        internal static bool IsScenarioAction(PlayCardAction action) =>
            action.CardInstanceId == TorchCardId || defenders.Contains(action.CardInstanceId);

        private static bool Observed(RelicEffectAddTraitToHand effect) =>
            Enabled && original != null && effect.GetRelicDataId() == RelicHoldoverGrantModel.OwnerId &&
            FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;

        private static Card Snapshot(CardState card)
        {
            return new Card { Id = FullBattleTrace.Active!.CardId(card), DataId = card.GetCardDataID(),
                Type = card.GetCardType().ToString(), Champion = card.IsChampionCard(),
                Holdover = card.GetTemporaryCardStateModifiers().GetTemporaryTraits().OfType<CardTraitRetain>()
                    .Select(trait => CardTraitCompositionProbe.Value(trait).HoldoverPending!.Value).ToArray() };
        }

        [HarmonyPatch(typeof(RelicEffectAddTraitToHand), nameof(RelicEffectAddTraitToHand.OnCardAdded))]
        private static class CardAddedPatch
        {
            private static void Prefix(RelicEffectAddTraitToHand __instance,
                CardAddedToHandRelicEffectParams relicEffectParams, out Callback? __state)
            {
                __state = null;
                if (!Observed(__instance)) return;
                __state = new Callback { Kind = "CardAdded",
                    Turn = AllGameManagers.Instance!.GetCombatManager().GetTurnCount(),
                    Before = new[] { Snapshot(relicEffectParams.cardState) } };
                Callbacks.Add(__state);
            }

            private static void Postfix(CardAddedToHandRelicEffectParams relicEffectParams, bool __result, Callback? __state)
            {
                if (__state == null) return;
                __state.After = new[] { Snapshot(relicEffectParams.cardState) };
                __state.Fired = __result;
            }
        }

        [HarmonyPatch(typeof(RelicEffectAddTraitToHand), nameof(RelicEffectAddTraitToHand.ApplyEffectTurnPhaseTiming))]
        private static class AfterDrawPatch
        {
            private static void Postfix(RelicEffectAddTraitToHand __instance, ref IEnumerator __result)
            {
                if (Observed(__instance)) __result = ObserveDraw(__result, AllGameManagers.Instance!);
            }
        }

        private static IEnumerator ObserveDraw(IEnumerator native, AllGameManagers managers)
        {
            var cards = managers.GetCardManager();
            var callback = new Callback { Kind = "AfterDraw", Turn = managers.GetCombatManager().GetTurnCount(),
                Before = cards.GetHand().Select(Snapshot).ToArray() };
            Callbacks.Add(callback);
            try
            {
                while (native.MoveNext()) yield return native.Current;
                callback.After = cards.GetHand().Select(Snapshot).ToArray();
            }
            finally { (native as IDisposable)?.Dispose(); }
        }
    }
}
#endif
