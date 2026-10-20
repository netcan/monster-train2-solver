using System.Collections;
using System.Collections.Generic;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class TriggeredTrainHealProbe
    {
        internal sealed class Record
        {
            public int SourceCardId { get; set; }
            public bool Preview { get; set; }
            public int Amount { get; set; }
            public int Before { get; set; }
            public int After { get; set; }
            public int Maximum { get; set; }
            public bool Disabled { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        internal sealed class TestRecord
        {
            public bool Preview { get; set; }
            public bool? SelectedPyreRoom { get; set; }
            public bool PyreTarget { get; set; }
            public bool Result { get; set; }
        }
        internal static readonly List<TestRecord> Tests = new List<TestRecord>();
        private static IEnumerator Observe(IEnumerator original, CardEffectState effect, CardEffectParams parameters,
            ICoreGameManagers managers)
        {
            SaveManager save = managers.GetSaveManager();
            var record = new Record { SourceCardId = parameters.playedCard == null ? 0 : FullBattleTrace.Active!.CardId(parameters.playedCard),
                Preview = save.PreviewMode, Amount = effect.GetParamInt(), Before = managers.GetPlayerManager().GetTowerHP(),
                Maximum = save.GetMaxTowerHP(), Disabled = save.GetRelicModifiedRules().GetTowerHealingDisabled() };
            while (original.MoveNext()) yield return original.Current;
            record.After = managers.GetPlayerManager().GetTowerHP();
            Records.Add(record);
        }
        [HarmonyPatch(typeof(CardEffectHealTrain), nameof(CardEffectHealTrain.ApplyEffect))]
        private static class Patch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams,
                ICoreGameManagers coreGameManagers, ref IEnumerator __result)
            {
                if (FullBattleTrace.Active != null && OriginalSupportSpellsScenario.IsolatedIds != null)
                    __result = Observe(__result, cardEffectState, cardEffectParams, coreGameManagers);
            }
        }
        [HarmonyPatch(typeof(CardEffectHealTrain), nameof(CardEffectHealTrain.TestEffectOnTarget))]
        private static class TestPatch
        {
            private static void Postfix(CardEffectParams cardEffectParams, CharacterState target,
                ICoreGameManagers coreGameManagers, bool __result)
            {
                if (FullBattleTrace.Active != null && OriginalSupportSpellsScenario.IsolatedIds != null)
                    Tests.Add(new TestRecord { Preview = coreGameManagers.GetSaveManager().PreviewMode,
                        SelectedPyreRoom = cardEffectParams.GetSelectedRoom(coreGameManagers.GetRoomManager())?.GetIsPyreRoom(),
                        PyreTarget = target.IsPyreHeart(), Result = __result });
            }
        }
    }
}
