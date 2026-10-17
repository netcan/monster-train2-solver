using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalMoonPhaseRoomsGainMagicPowerScenario
    {
        internal const string Name = "original-moon-phase-rooms-magic-power";
        internal const string OwnerId = "e96497a1-3c1e-4adf-b897-8ef09b931d1d";
        internal const string AssetKey = "MoonPhaseRoomsGainMagicPower";
        internal const string Effect = "RelicEffectApplyCardUpgradeToHandEndOfTurnOnMoon";

        private sealed class Witness
        {
            public int Turn { get; set; }
            public int InstanceId { get; set; }
            public string DataId { get; set; } = "";
            public bool ExpectedMatch { get; set; }
            public string Role { get; set; } = "";
            public string[] DefinitionTraits { get; set; } = Array.Empty<string>();
        }

        private static readonly List<Witness> witnesses = new List<Witness>();
        private static readonly List<object> callbacks = new List<object>();
        private static object? definitionEvidence;
        private static int lastPreparedTurn = -1;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static object Evidence => new { Definition = definitionEvidence, Witnesses = witnesses, Callbacks = callbacks };

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Moon-phase hand-upgrade relic must be acquired before battle setup.");
            CollectableRelicData data = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == OwnerId);
            RelicEffectData effect = data.GetEffects().Single();
            if (data.name != AssetKey || effect.GetEffectClassName() != Effect ||
                save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("Original MoonPhaseRoomsGainMagicPower owner or effect changed.");

            save.AddRelic(data);
            CombatRelicState owner = RelicProbe.Capture(managers).Single(item => item.DataId == OwnerId);
            if (owner.AssetKey != AssetKey || owner.EffectTypes.Count != 1 || owner.EffectTypes[0] != Effect ||
                owner.CardModifiers?.Count != 1 || owner.CardModifiers[0].MoonPhaseHandUpgrade == null)
                throw new InvalidOperationException("Native moon-phase hand-upgrade payload was not captured.");
            RelicMoonPhaseHandUpgrade payload = owner.CardModifiers[0].MoonPhaseHandUpgrade;
            definitionEvidence = new
            {
                OwnerId,
                AssetKey = data.name,
                EffectType = effect.GetEffectClassName(),
                IsCovenant = owner.IsCovenant,
                DisallowedInPlacementPhase = owner.DisallowedInPlacementPhase,
                ParamInt2 = effect.GetParamInt2(),
                ParamBool = effect.GetParamBool(),
                ConditionCount = effect.GetEffectConditions().Count,
                MoonPhaseMask = payload.MoonPhaseMask,
                RemoveOnDiscard = payload.RemoveOnDiscard,
                CanApplyInPreviewMode = payload.CanApplyInPreviewMode,
                Upgrade = payload.Upgrade,
                Filters = payload.Filters
            };
            witnesses.Clear(); callbacks.Clear(); lastPreparedTurn = -1;
            Acquired = true;
            log.LogInfo("ORIGINAL-MOON-PHASE-HAND-UPGRADE-ACQUIRED unchanged=" + AssetKey + " phaseMask=" +
                payload.MoonPhaseMask + " removeOnDiscard=" + payload.RemoveOnDiscard + " filters=" + payload.Filters.Count + ".");
        }

        internal static void PrepareTurn(AllGameManagers managers, ManualLogSource log)
        {
            if (!Acquired) throw new InvalidOperationException("Moon-phase hand-upgrade relic was not acquired before battle setup.");
            int turn = managers.GetCombatManager()!.GetTurnCount();
            if (turn <= 0 || turn == lastPreparedTurn) return;
            CombatRelicState owner = RelicProbe.Capture(managers).Single(item => item.DataId == OwnerId);
            RelicMoonPhaseHandUpgrade rule = owner.CardModifiers!.Single().MoonPhaseHandUpgrade!;
            RelicEffectData effect = managers.GetSaveManager().GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == OwnerId).GetEffects().Single();
            CardUpgradeData upgradeData = effect.GetParamCardUpgradeData();
            List<CardUpgradeMaskData> filters = upgradeData.GetFilters();
            RelicManager relicManager = managers.GetRelicManager()!;
            var ownedIds = new HashSet<string>(managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Select(card => card.GetCardDataID()), System.StringComparer.Ordinal);
            var candidates = managers.GetSaveManager().GetAllGameData().GetAllCardData()
                .Where(card => card != null && !ownedIds.Contains(card.GetID()) && IsSafeWitnessDefinition(card))
                .OrderBy(card => card.GetID(), System.StringComparer.Ordinal).ToArray();
            CardData? positive = candidates.FirstOrDefault(card => Matches(card, filters, relicManager, 99));
            if (positive == null)
                throw new InvalidOperationException("No native card matches the moon-phase hand-upgrade filters at cost 99.");
            CardData? control = candidates.FirstOrDefault(card => card.GetID() != positive.GetID() &&
                !Matches(card, filters, relicManager, 99));
            if (control == null && filters.Count > 0)
                throw new InvalidOperationException("No native nonmatching card is available as a filter control.");
            AddWitness(managers, positive, turn, expectedMatch: true, "positive");
            if (control != null && managers.GetCardManager()!.GetHand().Count < managers.GetCardManager()!.GetMaxHandSize())
                AddWitness(managers, control, turn, expectedMatch: false, "filter-control");
            lastPreparedTurn = turn;
            Prepared = true;
            FullBattleTrace.Active!.InvalidateCardRules();
            log.LogInfo("ORIGINAL-MOON-PHASE-HAND-UPGRADE-PREPARED turn=" + turn + " moon=" +
                managers.GetPlayerManager()!.CurrentMoonPhase + " positive=" + positive.GetID() + " control=" + (control?.GetID() ?? "none") + ".");
        }

        private static bool Matches(CardData card, IReadOnlyList<CardUpgradeMaskData> filters, RelicManager relicManager, int cost)
        {
            object costValue = AccessTools.Field(typeof(CardData), "cost").GetValue(card);
            try
            {
                Set(card, "cost", cost);
                return filters.All(filter => filter.FilterCard(card, relicManager));
            }
            finally { Set(card, "cost", costValue); }
        }

        private static bool IsSafeWitnessDefinition(CardData card) => !card.GetTraits().Any(trait =>
            trait.GetTraitStateName() is "CardTraitScalingAddEnergy" or "CardTraitAddCardUpgradeOnMoonPhase");

        private static void AddWitness(AllGameManagers managers, CardData card, int turn, bool expectedMatch, string role)
        {
            Set(card, "cost", 99);
            CardState? instance = managers.GetCardManager()!.AddNewCard(card, CardPile.HandPile, false, false, animate: false);
            if (instance == null || !managers.GetCardManager()!.GetHand().Contains(instance))
                throw new InvalidOperationException("Moon-phase hand-upgrade " + role + " card did not enter the native hand.");
            witnesses.Add(new Witness
            {
                Turn = turn,
                InstanceId = FullBattleTrace.Active!.CardId(instance),
                DataId = card.GetID(),
                ExpectedMatch = expectedMatch,
                Role = role,
                DefinitionTraits = card.GetTraits().Select(trait => trait.GetTraitStateName()).ToArray()
            });
        }

        private static void CaptureCallback()
        {
            if (!Enabled || !Acquired || !Prepared || FullBattleTrace.Active == null) return;
            try
            {
                AllGameManagers managers = AllGameManagers.Instance!;
                int turn = managers.GetCombatManager()!.GetTurnCount();
                CardManager cards = managers.GetCardManager()!;
                CardState[] all = cards.GetAllCards(new List<CardState>()).ToArray();
                object[] observed = witnesses.Where(item => item.Turn == turn).Select(item =>
                {
                    CardState card = all.Single(state => FullBattleTrace.Active.CardId(state) == item.InstanceId);
                    CardModifiers temporary = CardModifierProbe.Modifiers(card.GetTemporaryCardStateModifiers());
                    return (object)new
                    {
                        item.InstanceId,
                        item.DataId,
                        item.Role,
                        item.ExpectedMatch,
                        InHand = cards.GetHand().Contains(card),
                        TemporaryUpgrades = temporary.Upgrades
                    };
                }).ToArray();
                callbacks.Add(new
                {
                    Turn = turn,
                    Phase = managers.GetPlayerManager()!.CurrentMoonPhase.ToString(),
                    HandCount = cards.GetHand().Count,
                    Witnesses = observed
                });
            }
            catch (System.Exception error) { FullBattleTrace.Active.CaptureFailure(error); }
        }

        private static void Set(object target, string field, object? value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);

        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.ApplyEndOfTurnPreDiscardRelicEffects))]
        private static class EndTurnPreDiscardPatch
        {
            private static void Postfix(ref IEnumerator __result)
            {
                if (Enabled && Acquired && Prepared) __result = Wrap(__result);
            }

            private static IEnumerator Wrap(IEnumerator native)
            {
                try { while (native.MoveNext()) yield return native.Current; }
                finally { CaptureCallback(); }
            }
        }
    }
}
