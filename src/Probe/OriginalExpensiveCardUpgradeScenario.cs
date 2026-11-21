using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalExpensiveCardUpgradeScenario
    {
        internal const string Name = "original-expensive-card-upgrade";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static int[] Units { get; private set; } = Array.Empty<int>();
        internal static int[] Carriers { get; private set; } = Array.Empty<int>();
        internal static readonly List<Record> Records = new List<Record>();
        private static int step;
        private static bool mark;
        private static bool control;
        private static bool observing;
        private static bool? copyExcludesPermafrost;
        internal static object Evidence => new { OwnerId = RelicExpensiveCardUpgradeModel.DataId, Units, Carriers,
            Roles = new[] { "three-cost-spell", "two-cost-spell", "zero-X-spell", "retained-four-cost", "cost-threshold-cycle", "three-cost-monster" },
            CopyExcludesPermafrost = copyExcludesPermafrost };
        internal sealed class Record
        {
            public string Kind { get; set; } = "";
            public bool Control { get; set; }
            public int CardId { get; set; }
            public bool? Returned { get; set; }
            public bool? ExpectedReturn { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Predicted { get; set; }
            public TrainCombatState? Actual { get; set; }
            public string? Difference { get; set; }
            public string? Error { get; set; }
            public string? UnsupportedReason { get; set; }
            public bool Completed { get; set; }
        }
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle) throw new InvalidOperationException("Expected pre-battle Permafrost acquisition.");
            save.AddRelic(data.GetAllCollectableRelicData().Single(relic => relic.GetID() == RelicExpensiveCardUpgradeModel.DataId));
            string? error = RelicModel.Validate(new[] { RelicProbe.Capture(managers).Single(owner => owner.DataId == RelicExpensiveCardUpgradeModel.DataId) });
            if (error != null) throw new InvalidOperationException(error);
            Acquired = true; log.LogInfo("ORIGINAL-EXPENSIVE-CARD-UPGRADE-ACQUIRED unchanged cost>=3 or X Permafrost owner.");
        }
        internal static void PrepareDeployment(AllGameManagers managers)
        {
            if (Prepared || managers.GetCombatManager()!.GetTurnCount() != 0) throw new InvalidOperationException("Expected one Permafrost deployment setup.");
            CardManager cards = managers.GetCardManager()!;
            SaveManager save = managers.GetSaveManager();
            AllGameData data = save.GetAllGameData();
            CardState[] units = cards.GetHand().Where(card => card.GetSpawnCharacterData() != null).Take(2).ToArray();
            if (units.Length != 2) throw new InvalidOperationException("Two original unit cards are required in the deployment hand.");
            Units = units.Select(FullBattleTrace.Active!.CardId).ToArray();
            foreach (CardState unit in units)
            {
                var combat = new CardUpgradeState();
                combat.Setup();
                combat.SetAdditionalHP(70);
                combat.SetAttackDamage(30);
                unit.ApplyPermanentUpgrade(combat, save, ignoreUpgradeAnimation: true);
            }
            CardData torch = data.GetAllCardData().Single(card => card.name == "StarterTorch");
            CardData vent = data.GetAllCardData().Single(card => card.name == "Vent");
            CardData steward = data.FindCardData(units[1].GetCardDataID())!;
            if (torch.GetCost() != 1 || vent.GetCostType() != CardData.CostType.ConsumeRemainingEnergy)
                throw new InvalidOperationException("The original Permafrost Torch or Vent input changed.");
            CardState Add(CardData definition, int? cost)
            {
                CardState card = cards.AddNewCard(definition, CardPile.HandPile, false, true, animate: false)
                    ?? throw new InvalidOperationException("Could not add original Permafrost carrier " + definition.name);
                if (cost.HasValue)
                {
                    var adjustment = new CardUpgradeState();
                    adjustment.Setup();
                    adjustment.SetCostReduction(definition.GetCost() - cost.Value);
                    card.ApplyPermanentUpgrade(adjustment, save, ignoreUpgradeAnimation: true);
                }
                return card;
            }
            CardState[] created = {
                Add(torch, 3), Add(torch, 2), Add(vent, null), Add(torch, 4), Add(torch, 2),
                Add(steward, 3)
            };
            Carriers = created.Select(FullBattleTrace.Active!.CardId).ToArray(); Prepared = true;
            FullBattleTrace.Active!.InvalidateCardRules();
            var effect = managers.GetSaveManager().GetCollectedRelics().Single(owner => owner.GetRelicDataID() == RelicExpensiveCardUpgradeModel.DataId)
                .GetEffects().OfType<RelicEffectAddCardUpgradeToExpensiveCards>().Single();
            RelicManager relics = managers.GetRelicManager(); control = true;
            try
            {
                foreach (CardState card in created) { relics.ApplyCardStateModifiers(card, false); effect.HasAppliedCardStateModifiers(card); }
                var ignoredCopyUpgrades = new List<string>();
                created[0].GetTemporaryCardStateModifiers().TryGetIgnoredUpgrades(ignoredCopyUpgrades);
                copyExcludesPermafrost = ignoredCopyUpgrades.Contains(RelicExpensiveCardUpgradeModel.Effect);
                void ChangeCost(int reduction)
                {
                    var upgrade = new CardUpgradeState(); upgrade.SetCostReduction(reduction);
                    created[4].ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
                    relics.OnCardPermanentUpgradeApplied(created[4], cards);
                }
                ChangeCost(-1); ChangeCost(1); ChangeCost(-1);
                relics.ApplyCardStateModifiers(created[0], true);
                created[0].RemoveUpgrade(RelicExpensiveCardUpgradeModel.Effect, created[0].GetTemporaryCardStateModifiers()); created[0].UpdateCardBodyText();
                effect.PreviewPermanentCardStateModifiers(created[0], relics, save);
                effect.PreviewPermanentCardStateModifiers(created[0], relics, save);
            }
            finally { control = false; }
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (step >= 4 || step >= 2 && source.Spawn.Turn == 0 || step == 3 && source.Spawn.Turn == 1) return null;
            int id = step < 2 ? Units[step] : Carriers[step == 2 ? 0 : 2];
            int? junker = step == 2 ? source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .FirstOrDefault(unit => unit.AssetKey == "MageT1_Junker")?.Id : null;
            PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(source).Where(play => play.CardInstanceId == id)
                .OrderByDescending(play => junker.HasValue && play.TargetUnitId == junker.Value)
                .ThenBy(play => play.RoomIndex == 0 ? 0 : 1).FirstOrDefault();
            if (action == null) throw new InvalidOperationException("Missing Permafrost play " + step);
            step++; mark = true; return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action) { if (!mark) return false; mark = false; return true; }
        private static Record? Begin(string kind, CardState card)
        {
            if (!Prepared || FullBattleTrace.Active == null || observing || !control && (kind == "Has" || kind == "Preview")) return null;
            var row = new Record { Kind = kind, Control = control, CardId = FullBattleTrace.Active.CardId(card) }; Records.Add(row);
            observing = true;
            try
            {
                row.Before = FullBattleTrace.Active.CaptureTrain(); CombatContext context = row.Before.Context!;
                CardInstanceState before = context.FindCard(row.CardId)!;
                var rule = context.Relics!.Single(owner => owner.DataId == RelicExpensiveCardUpgradeModel.DataId).CardModifiers!.Single().ExpensiveCardUpgrade!;
                CardInstanceState predicted;
                if (kind == "Has") { row.ExpectedReturn = RelicExpensiveCardUpgradeModel.Interested(before, rule) && RelicExpensiveCardUpgradeModel.HasUpgrade(before); predicted = before; }
                else if (kind == "TraitDrawn") predicted = RelicExpensiveCardUpgradeModel.Freeze(before);
                else { predicted = RelicExpensiveCardUpgradeModel.Refresh(before, rule, kind != "Preview", out bool returned); if (kind != "Permanent") row.ExpectedReturn = returned; }
                row.Predicted = CardSpellModel.WithContext(row.Before, context.WithCard(predicted));
            }
            catch (Exception error) { row.Error = error.ToString(); }
            finally { observing = false; }
            return row;
        }
        private static void Finish(Record? row, bool? returned = null)
        {
            if (row == null) return;
            observing = true;
            try { row.Returned = returned; row.Actual = FullBattleTrace.Active!.CaptureTrain(); row.Completed = true;
                if (returned != row.ExpectedReturn || row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual))) row.Difference = "Native expensive-card callback state/return differs."; }
            catch (Exception error) { row.Error = error.ToString(); }
            finally { observing = false; }
        }
        [HarmonyPatch(typeof(RelicEffectAddCardUpgradeToExpensiveCards), nameof(RelicEffectAddCardUpgradeToExpensiveCards.ApplyCardStateModifiers))]
        private static class ApplyPatch
        { private static void Prefix(CardState cardState, out Record? __state) => __state = Begin("Apply", cardState);
            private static void Postfix(bool __result, Record? __state) => Finish(__state, __result); }
        [HarmonyPatch(typeof(RelicEffectAddCardUpgradeToExpensiveCards), nameof(RelicEffectAddCardUpgradeToExpensiveCards.OnCardAdded))]
        private static class AddedPatch
        { private static void Prefix(CardAddedToHandRelicEffectParams relicEffectParams, out Record? __state) => __state = Begin("Added", relicEffectParams.cardState);
            private static void Postfix(bool __result, Record? __state) => Finish(__state, __result); }
        [HarmonyPatch(typeof(RelicEffectAddCardUpgradeToExpensiveCards), nameof(RelicEffectAddCardUpgradeToExpensiveCards.OnPermanentCardUpgradeApplied))]
        private static class PermanentPatch
        { private static void Prefix(CardState cardState, out Record? __state) => __state = Begin("Permanent", cardState);
            private static void Postfix(Record? __state) => Finish(__state); }
        [HarmonyPatch(typeof(RelicEffectAddCardUpgradeToExpensiveCards), nameof(RelicEffectAddCardUpgradeToExpensiveCards.HasAppliedCardStateModifiers))]
        private static class HasPatch
        { private static void Prefix(CardState cardState, out Record? __state) => __state = Begin("Has", cardState);
            private static void Postfix(bool __result, Record? __state) => Finish(__state, __result); }
        [HarmonyPatch(typeof(RelicEffectAddCardUpgradeToExpensiveCards), nameof(RelicEffectAddCardUpgradeToExpensiveCards.PreviewPermanentCardStateModifiers))]
        private static class PreviewPatch
        { private static void Prefix(CardState cardState, out Record? __state) => __state = Begin("Preview", cardState);
            private static void Postfix(bool __result, Record? __state) => Finish(__state, __result); }
        [HarmonyPatch(typeof(CardTraitPermafrost), nameof(CardTraitPermafrost.OnCardDrawn))]
        private static class DrawPatch
        { private static void Prefix(CardState thisCard, out Record? __state) => __state = Enabled ? Begin("TraitDrawn", thisCard) : null;
            private static void Postfix(Record? __state) => Finish(__state); }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field)!.SetValue(target, value);
    }
}
