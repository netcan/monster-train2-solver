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
        internal static object Evidence => new { OwnerId = RelicExpensiveCardUpgradeModel.DataId, Units, Carriers,
            Roles = new[] { "three-cost-spell", "two-cost-spell", "zero-X-spell", "retained-four-cost", "cost-threshold-cycle", "three-cost-monster" } };
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
            CardData steward = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)!;
            CharacterData unit = steward.GetSpawnCharacterData()!;
            Set(unit, "health", 80); Set(unit, "attackDamage", 30); Set(unit, "size", 1); Set(unit, "unitAbility", null!);
            Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(steward, "cost", 0); Set(steward, "traits", new List<CardTraitData>()); save.GetDeckState().Clear();
            for (int i = 0; i < 2; i++) save.AddCardToDeck(steward, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddRelic(data.GetAllCollectableRelicData().Single(relic => relic.GetID() == RelicExpensiveCardUpgradeModel.DataId));
            string? error = RelicModel.Validate(new[] { RelicProbe.Capture(managers).Single(owner => owner.DataId == RelicExpensiveCardUpgradeModel.DataId) });
            if (error != null) throw new InvalidOperationException(error);
            Acquired = true; log.LogInfo("ORIGINAL-EXPENSIVE-CARD-UPGRADE-ACQUIRED unchanged cost>=3 or X Permafrost owner.");
        }
        internal static void PrepareDeployment(AllGameManagers managers)
        {
            if (Prepared || managers.GetCombatManager()!.GetTurnCount() != 0) throw new InvalidOperationException("Expected one Permafrost deployment setup.");
            CardManager cards = managers.GetCardManager()!; AllGameData data = managers.GetSaveManager().GetAllGameData();
            Units = cards.GetHand().Select(FullBattleTrace.Active!.CardId).ToArray();
            CardData spell = data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell);
            CardData steward = data.FindCardData(cards.GetHand()[0].GetCardDataID())!;
            var created = new List<CardState>(); int[] costs = { 3, 2, 0, 4, 2, 3 };
            for (int i = 0; i < costs.Length; i++)
            {
                CardData card = UnityEngine.Object.Instantiate(i == 5 ? steward : spell); card.name = "PojuExpensiveCard" + i;
                Set(card, "id", "bc057005-0000-4000-8000-00000000000" + i); Set(card, "cost", costs[i]);
                Set(card, "costType", i == 2 ? CardData.CostType.ConsumeRemainingEnergy : CardData.CostType.Default);
                Set(card, "targetless", true); Set(card, "targetsRoom", true); Set(card, "isUnitAbility", false); Set(card, "isRoomAbility", false);
                Set(card, "traits", new List<CardTraitData>()); Set(card, "startingUpgrades", new List<CardUpgradeData>());
                Set(card, "triggers", new List<CardTriggerEffectData>()); Set(card, "effectTriggers", new List<CharacterTriggerData>());
                if (i != 5) Set(card, "effects", new List<CardEffectData>());
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(card);
                CardState added = cards.AddNewCard(card, i == 5 ? CardPile.DeckPile : CardPile.HandPile, false, false, animate: false)
                    ?? throw new InvalidOperationException("Could not create Permafrost carrier.");
                created.Add(added);
            }
            Carriers = created.Select(FullBattleTrace.Active!.CardId).ToArray(); Prepared = true;
            FullBattleTrace.Active!.InvalidateCardRules();
            var effect = managers.GetSaveManager().GetCollectedRelics().Single(owner => owner.GetRelicDataID() == RelicExpensiveCardUpgradeModel.DataId)
                .GetEffects().OfType<RelicEffectAddCardUpgradeToExpensiveCards>().Single();
            RelicManager relics = managers.GetRelicManager(); SaveManager save = managers.GetSaveManager(); control = true;
            try
            {
                foreach (CardState card in created) { relics.ApplyCardStateModifiers(card, false); effect.HasAppliedCardStateModifiers(card); }
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
                created[2].RemoveUpgrade(RelicExpensiveCardUpgradeModel.Effect, created[2].GetTemporaryCardStateModifiers()); created[2].UpdateCardBodyText();
                relics.ApplyCardAddedToHandRelicEffects(created[2]);
            }
            finally { control = false; }
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (step >= 4 || step >= 2 && source.Spawn.Turn == 0) return null;
            int id = step < 2 ? Units[step] : Carriers[step == 2 ? 0 : 2];
            PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == id && play.RoomIndex == 0);
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
