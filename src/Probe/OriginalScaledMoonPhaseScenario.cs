using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalScaledMoonPhaseScenario
    {
        internal const string Name = "original-scaled-moon-phase";
        internal const string CardId = "e9fa09e5-13bd-4826-a8e5-969332cbfbbf";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<object> Counts = new List<object>();
        internal static readonly List<object> Queries = new List<object>();
        internal static readonly List<object> StatusSamples = new List<object>();
        internal static readonly List<PhaseSample> Phases = new List<PhaseSample>();
        private static bool querying;
        internal sealed class PhaseSample
        {
            public int CardId { get; set; }
            public bool Completed { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? After { get; set; }
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear(); Set(managers.GetPlayerManager(), "isMoonPhaseTrackingActive", true);
            CardData battery = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            battery.name = "PojuScaledMoonBattery"; Set(battery, "id", "ac110030-0000-4000-8000-000000000001");
            Set(battery, "cost", 0); Set(battery, "costType", CardData.CostType.Default); Set(battery, "targetless", true); Set(battery, "targetsRoom", true);
            Set(battery, "traits", new List<CardTraitData>()); Set(battery, "startingUpgrades", new List<CardUpgradeData>());
            Set(battery, "triggers", new List<CardTriggerEffectData>()); Set(battery, "effectTriggers", new List<CharacterTriggerData>());
            var energy = new CardEffectData("CardEffectGainEnergy", null!, Team.Type.None); energy.Cheat_SetTargetMode(TargetMode.Room); Set(energy, "paramInt", 10);
            Set(battery, "effects", new List<CardEffectData> { energy });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(battery);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(CardId), Add(CardId), Add(battery.GetID()), Add(CardId) };
            foreach (CardState card in created.Skip(2)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-SCALED-MOON-PHASE-PREPARED original Lunacy, X three/zero/ten moon advances then Conduit, pure matched/unmatched status controls and complete phase-effect train boundaries.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
            int target = index is 2 or 3 or 5 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] &&
                play.RoomIndex == (index == 1 ? 1 : 0) && (target == 0 || play.TargetUnitId == target));
        }
        [HarmonyPatch(typeof(CardTraitScalingAdvanceMoonPhase), nameof(CardTraitScalingAdvanceMoonPhase.OnMoonPhaseAdvanced))]
        private static class CountPatch
        {
            private static void Postfix(CardTraitScalingAdvanceMoonPhase __instance, int __result)
            {
                if (!Enabled || !Prepared || querying || AllGameManagers.Instance!.GetSaveManager().PreviewMode || __instance.GetCard().GetCardDataID() != CardId) return;
                FullBattleTrace trace = FullBattleTrace.Active!; CardState card = __instance.GetCard();
                Counts.Add(new { CardId = trace.CardId(card), ActualCount = __result, Context = trace.CaptureContext() });
                querying = true;
                try
                {
                    CardTraitScalingAddStatusEffect trait = card.GetTraitStates().OfType<CardTraitScalingAddStatusEffect>().Single();
                    foreach (string status in new[] { "conduit", "armor" })
                    foreach (int input in new[] { 0, 99 })
                    {
                        CombatContext before = trace.CaptureContext(); int frame = UnityEngine.Time.frameCount;
                        int actual = trait.OnStatusEffectApplied(null!, card, AllGameManagers.Instance.GetCoreManagers(), status, input);
                        Queries.Add(new { CardId = trace.CardId(card), StatusId = status, InputStacks = input, ActualBonus = actual, Context = before,
                            StateUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(JToken.FromObject(before), JToken.FromObject(trace.CaptureContext())) });
                    }
                }
                finally { querying = false; }
            }
        }
        [HarmonyPatch(typeof(CardTraitScalingAddStatusEffect), nameof(CardTraitScalingAddStatusEffect.OnStatusEffectApplied))]
        private static class StatusPatch
        {
            private static void Postfix(CardTraitScalingAddStatusEffect __instance, string statusId, int sourceStacks, int __result)
            {
                if (!Enabled || !Prepared || querying || AllGameManagers.Instance!.GetSaveManager().PreviewMode || __instance.GetCard().GetCardDataID() != CardId) return;
                FullBattleTrace trace = FullBattleTrace.Active!;
                StatusSamples.Add(new { CardId = trace.CardId(__instance.GetCard()), StatusId = statusId, InputStacks = sourceStacks, ActualBonus = __result, Context = trace.CaptureContext() });
            }
        }
        [HarmonyPatch(typeof(CardEffectAdvanceMoonPhase), nameof(CardEffectAdvanceMoonPhase.ApplyEffect))]
        private static class PhasePatch
        {
            private static void Postfix(CardEffectParams cardEffectParams, ref IEnumerator __result)
            { if (Enabled && Prepared && !AllGameManagers.Instance!.GetSaveManager().PreviewMode && cardEffectParams.playedCard?.GetCardDataID() == CardId) __result = Observe(__result, cardEffectParams.playedCard); }
        }
        private static IEnumerator Observe(IEnumerator native, CardState card)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            var row = new PhaseSample { CardId = trace.CardId(card), Before = trace.CaptureCanonicalTrain() }; Phases.Add(row);
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); row.After = trace.CaptureCanonicalTrain(); }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
