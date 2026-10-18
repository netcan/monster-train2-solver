using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class SoldierOfFortuneScenario
    {
        internal const string Name = "soldier-of-fortune";
        internal const string DataId = "fbf7e58a-9369-49d8-ba1a-782fbcaf6498";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static bool EmptyTargetTest { get; private set; }
        internal static readonly List<GoldTest> GoldTests = new List<GoldTest>();
        internal static readonly List<NullRecord> NullRecords = new List<NullRecord>();
        internal sealed class GoldTest
        {
            public int Gold { get; set; }
            public bool Returned { get; set; }
        }
        internal sealed class NullRecord
        {
            public int Targets { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? After { get; set; }
            public bool Completed { get; set; }
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardData steward = save.GetAllGameData().FindCardData(owned.First(card =>
                save.GetAllGameData().FindCardData(card.GetCardDataID()).GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true).GetCardDataID())!;
            AccessTools.Field(typeof(CharacterData), "attackDamage").SetValue(steward.GetSpawnCharacterData(), 50);
            AccessTools.Field(typeof(CharacterData), "health").SetValue(steward.GetSpawnCharacterData(), 100);
            foreach (CardState card in owned.Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            CardData soldier = save.GetAllGameData().FindCardData(DataId)!;
            CardState first = cards.AddNewCard(soldier, CardPile.HandPile, false, false, animate: false);
            cards.AddNewCard(soldier, CardPile.HandPile, false, false, animate: false);
            cards.AddNewCard(soldier, CardPile.HandPile, false, false, animate: false);
            var native = new CardEffectAddTempCardUpgradeToNextDrawnCard();
            foreach (int gold in new[] { 0, 29, 30, 60 })
            {
                save.SetGold(gold);
                GoldTests.Add(new GoldTest { Gold = gold, Returned = native.TestEffect(first.GetEffectStates()[0],
                    new CardEffectParams(), managers.GetCoreManagers()) });
            }
            save.SetGold(60);
            EmptyTargetTest = new CardEffectAddStatusEffectNull().TestEffect(first.GetEffectStates()[2],
                new CardEffectParams(), managers.GetCoreManagers());
            Prepared = true;
            log.LogInfo("SOLDIER-OF-FORTUNE-PREPARED three unchanged originals; 60 gold, two paid typed draws and one unaffordable unupgraded draw; empty-target controls.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            foreach (CardToken card in source.Spawn.Train.Context!.Cards.Hand.Where(card => card.DataId == DataId))
            {
                var action = new PlayCardAction(card.InstanceId, 2);
                if (BattleActionModel.PlayCard(source, action).Supported) return action;
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }

        [HarmonyPatch(typeof(CardEffectAddStatusEffectNull), nameof(CardEffectAddStatusEffectNull.ApplyEffect))]
        private static class ApplyPatch
        {
            private static void Postfix(CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (Enabled && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, cardEffectParams.targets.Count);
            }
        }
        private static IEnumerator Observe(IEnumerator native, int targets)
        {
            var row = new NullRecord { Targets = targets, Before = FullBattleTrace.Active!.CaptureTrain() };
            NullRecords.Add(row);
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); row.After = FullBattleTrace.Active!.CaptureTrain(); }
        }
    }
}
