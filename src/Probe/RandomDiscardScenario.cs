using System;
using System.Collections;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class RandomDiscardScenario
    {
        internal static readonly System.Collections.Generic.List<Record> Records = new System.Collections.Generic.List<Record>();
        internal static bool Prepared => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "random-discard";

        internal sealed class Record
        {
            public int CardId { get; set; }
            public int EffectIndex { get; set; }
            public int DiscardCount { get; set; }
            public HandRemovalScenario.Snapshot Before { get; set; } = null!;
            public HandRemovalResult Predicted { get; set; } = null!;
            public HandRemovalScenario.Snapshot? Actual { get; set; }
            public string? Difference { get; set; }
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            CardState[] spells = cards.GetAllCards(new System.Collections.Generic.List<CardState>())
                .Where(card => card.GetCardType() == CardType.Spell &&
                    card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange"))
                .ToArray();
            if (spells.Length == 0) throw new InvalidOperationException("Random discard scenario needs a starting FloorRearrange spell.");

            CardData data = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            data.GetEffects().Clear();
            CardEffectData randomDiscard = new CardEffectData("CardEffectRandomDiscard", null!, Team.Type.None);
            randomDiscard.Cheat_SetTargetMode(TargetMode.Hand);
            Set(randomDiscard, "paramInt", 1);
            data.GetEffects().Add(randomDiscard);
            Set(data, "targetless", true);
            Set(data, "targetsRoom", false);
            foreach (CardState card in spells) card.Setup(data, save);
            if (!cards.GetHand().Contains(spells[0])) cards.DrawSpecificCard(spells[0]);
            managers.GetPlayerManager().AddEnergy(1);
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("RANDOM-DISCARD-PREPARED FloorRearrange card=" + FullBattleTrace.Active!.CardId(spells[0]) +
                ", copies=" + spells.Length + ", targetless spell discards one random hand card.");
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);

        private static IEnumerator Wrap(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
        {
            Record? record = null;
            try
            {
                if (parameters.playedCard != null && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                {
                    BattlePlayRules rules = FullBattleTrace.Active.CaptureDecision().PlayRules
                        ?? throw new InvalidOperationException("Random discard capture has no current card rules.");
                    HandRemovalScenario.Snapshot before = HandRemovalScenario.Capture();
                    int cardId = FullBattleTrace.Active.CardId(parameters.playedCard);
                    int effectIndex = parameters.playedCard.GetEffectStates().IndexOf(effect);
                    int count = effect.GetParamInt();
                    record = new Record
                    {
                        CardId = cardId,
                        EffectIndex = effectIndex,
                        DiscardCount = count,
                        Before = before,
                        Predicted = RandomDiscardModel.Apply(before.Context, count, cardId, effectIndex, rules, before.OtherPiles)
                    };
                    Records.Add(record);
                }
            }
            catch (Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }

            while (native.MoveNext()) yield return native.Current;
            if (record == null) yield break;
            try
            {
                record.Actual = HandRemovalScenario.Capture();
                record.Difference = !record.Predicted.Supported ? record.Predicted.UnsupportedReason :
                    !JToken.DeepEquals(HandRemovalScenario.Comparable(record.Predicted.Context!, record.Predicted.OtherPiles),
                        HandRemovalScenario.Comparable(record.Actual.Context, record.Actual.OtherPiles))
                        ? "Random hand discard selection, RNG or discard callbacks differ" : null;
                UnityEngine.Debug.Log("RANDOM-DISCARD-" + (record.Difference == null ? "MATCH" : "MISMATCH") +
                    " count=" + record.DiscardCount + " effect=" + record.EffectIndex);
            }
            catch (Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
        }

        [HarmonyPatch(typeof(CardEffectRandomDiscard), nameof(CardEffectRandomDiscard.ApplyEffect))]
        private static class RandomDiscardPatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "random-discard")
                    __result = Wrap(__result, cardEffectState, cardEffectParams);
            }
        }
    }
}
