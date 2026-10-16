using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicStartBattleGenerationProbe
    {
        internal static readonly List<Record> Records = new List<Record>();
        internal sealed class Record
        {
            public string OwnerId { get; set; } = "";
            public int EffectIndex { get; set; }
            public CombatContext Before { get; set; } = null!;
            public CombatContext? Actual { get; set; }
            public CardToken[] GeneratedCards { get; set; } = Array.Empty<CardToken>();
            public bool Completed { get; set; }
        }

        [HarmonyPatch(typeof(RelicEffectAddCardsStartOfBattle), nameof(RelicEffectAddCardsStartOfBattle.ApplyEffectStartOfBattle))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectAddCardsStartOfBattle __instance, ref IEnumerator __result)
            {
                if (!OriginalPartFactoryScenario.Acquired || FullBattleTrace.Active == null ||
                    AllGameManagers.Instance!.GetSaveManager().PreviewMode ||
                    __instance.SourceRelicState.GetRelicDataID() != OriginalPartFactoryScenario.DataId) return;
                try
                {
                    var owner = __instance.SourceRelicState;
                    var record = new Record { OwnerId = owner.GetRelicDataID(),
                        EffectIndex = owner.GetEffects().IndexOf(__instance),
                        Before = FullBattleTrace.Active.CaptureContext() };
                    if (record.EffectIndex < 0 || Records.Count != 0)
                        throw new InvalidOperationException("Unexpected PartFactory effect index or duplicate start callback.");
                    Records.Add(record);
                    __result = Wrap(__result, record);
                }
                catch (Exception error) { FullBattleTrace.Active.CaptureFailure(error); }
            }
        }

        private static IEnumerator Wrap(IEnumerator native, Record record)
        {
            try
            {
                while (native.MoveNext()) yield return native.Current;
                record.Completed = true;
            }
            finally
            {
                (native as IDisposable)?.Dispose();
                try
                {
                    record.Actual = FullBattleTrace.Active!.CaptureContext();
                    var prior = (record.Before.CardRegistry ?? record.Before.CardInstances ?? Array.Empty<CardInstanceState>())
                        .Select(card => card.InstanceId).ToHashSet();
                    record.GeneratedCards = (record.Actual.CardRegistry ?? record.Actual.CardInstances ?? Array.Empty<CardInstanceState>())
                        .Where(card => !prior.Contains(card.InstanceId)).Select(card => new CardToken(card.InstanceId, card.DataId)).ToArray();
                }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }
    }
}
