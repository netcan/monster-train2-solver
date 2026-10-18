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
    internal static class OriginalCardTypeSequenceScenario
    {
        internal const string Name = "original-card-type-sequence";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static int[] Units { get; private set; } = Array.Empty<int>();
        internal static readonly List<Record> Records = new List<Record>();
        private static int deployment;
        private static int lastTurn = -1;
        private static int turnStep;
        private static bool mark;
        private static bool control;
        private static bool observing;
        private static Record? applying;
        internal static object Evidence => new { Units, Types = new[] { "Spell", "Junk", "Blight" }, Turns = new[] { 1, 2 } };
        internal sealed class Record
        {
            public string Kind { get; set; } = "";
            public bool Control { get; set; }
            public int CardId { get; set; }
            public string? CardType { get; set; }
            public bool? IsAbility { get; set; }
            public bool? Returned { get; set; }
            public bool? ExpectedReturn { get; set; }
            public int Notifications { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Predicted { get; set; }
            public TrainCombatState? Actual { get; set; }
            public bool Completed { get; set; }
            public string? Error { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
        }
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Expected pre-battle card-type sequence acquisition.");
            CardData steward = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)!;
            CharacterData unit = UnityEngine.Object.Instantiate(steward.GetSpawnCharacterData());
            unit.name = "PojuSequenceUnit"; Set(unit, "id", "bc052371-0000-4000-8000-000000000000");
            Set(unit, "health", 80); Set(unit, "attackDamage", 30); Set(unit, "size", 1);
            Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>()); Set(unit, "unitAbility", null!);
            ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data)).Add(unit);
            var definitions = new List<CardData>();
            foreach (CardType type in new[] { CardType.Monster, CardType.Spell, CardType.Junk, CardType.Blight })
            {
                CardData card = UnityEngine.Object.Instantiate(steward); card.name = "PojuSequence" + type;
                Set(card, "id", "bc052370-0000-4000-8000-00000000000" + definitions.Count);
                Set(card, "cardType", type); Set(card, "cost", 0); Set(card, "traits", new List<CardTraitData>());
                Set(card, "startingUpgrades", new List<CardUpgradeData>()); Set(card, "triggers", new List<CardTriggerEffectData>());
                Set(card, "effectTriggers", new List<CharacterTriggerData>()); Set(card, "isUnitAbility", false); Set(card, "isRoomAbility", false);
                if (type == CardType.Monster)
                {
                    var effect = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters);
                    effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramInt", 1); Set(effect, "paramCharacterData", unit);
                    Set(card, "effects", new List<CardEffectData> { effect });
                }
                else { Set(card, "effects", new List<CardEffectData>()); Set(card, "targetless", true); Set(card, "targetsRoom", true); }
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(card); definitions.Add(card);
            }
            save.GetDeckState().Clear();
            foreach (CardData card in new[] { definitions[0], definitions[0], definitions[1], definitions[2], definitions[3] })
                save.AddCardToDeck(card, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddRelic(data.GetAllCollectableRelicData().Single(owner => owner.GetID() == RelicCardTypeSequenceModel.DataId));
            string? error = RelicModel.Validate(new[] { RelicProbe.Capture(managers).Single(owner => owner.DataId == RelicCardTypeSequenceModel.DataId) });
            if (error != null) throw new InvalidOperationException(error);
            Acquired = true; log.LogInfo("ORIGINAL-CARD-TYPE-SEQUENCE-ACQUIRED unchanged three-type owner and trigger-count condition.");
        }
        internal static void PrepareDeployment(AllGameManagers managers)
        {
            CardManager cards = managers.GetCardManager()!;
            if (Prepared || managers.GetCombatManager()!.GetTurnCount() != 0) throw new InvalidOperationException("Expected card-type sequence deployment.");
            Units = cards.GetHand().Where(card => card.GetCardType() == CardType.Monster).Select(FullBattleTrace.Active!.CardId).ToArray();
            if (Units.Length != 2) throw new InvalidOperationException("Missing sequence deployment carriers.");
            CardState Carrier(CardType type, int definition)
            {
                CardState? found = cards.GetHand().Concat(cards.GetDrawPile(true)).Concat(cards.GetDiscardPile(true))
                    .SingleOrDefault(card => card.GetCardType() == type);
                if (found == null) return cards.AddNewCard(managers.GetSaveManager().GetAllGameData()
                    .FindCardData("bc052370-0000-4000-8000-00000000000" + definition)!, CardPile.HandPile, false, false, animate: false)!;
                if (!cards.GetHand().Contains(found)) cards.DrawSpecificCard(found);
                return found;
            }
            CardState spell = Carrier(CardType.Spell, 1);
            CardData spellData = managers.GetSaveManager().GetAllGameData().FindCardData(spell.GetCardDataID())!;
            cards.AddNewCard(spellData, CardPile.HandPile, false, false, animate: false);
            CardState junk = Carrier(CardType.Junk, 2);
            CardState blight = Carrier(CardType.Blight, 3);
            CardData abilityData = UnityEngine.Object.Instantiate(spellData); Set(abilityData, "isUnitAbility", true);
            var ability = new CardState(abilityData, managers.GetSaveManager(), setupStartingUpgrades: false);
            var effect = managers.GetSaveManager().GetCollectedRelics().Single(owner => owner.GetRelicDataID() == RelicCardTypeSequenceModel.DataId)
                .GetEffects().OfType<RelicEffectEnergyAndCardDrawOnCardPlaySequence>().Single();
            Prepared = true; FullBattleTrace.Active!.InvalidateCardRules(); control = true;
            try
            {
                foreach (CardState card in new[] { ability, spell, spell, junk, blight, spell })
                {
                    var parameters = new CardPlayedRelicEffectParams(new RelicEffectParams { relicManager = managers.GetRelicManager() })
                        { cardState = card, cardStatistics = managers.GetCardStatistics(), roomIndex = 0 };
                    if (effect.TestEffectOnCardPlayed(parameters, managers.GetCoreManagers())) Drain(effect.ApplyEffectOnCardPlayed(parameters, managers.GetCoreManagers()));
                }
                Drain(effect.ApplyEffectStartOfTurn(new RelicEffectParams { relicManager = managers.GetRelicManager() }, managers.GetCoreManagers()));
                effect.TestEffectStartOfTurn(new RelicEffectParams(), managers.GetCoreManagers());
                effect.TestEffectOnCardPlayed(new CardPlayedRelicEffectParams(new RelicEffectParams()) { cardState = spell }, managers.GetCoreManagers());
                Drain(effect.ApplyEffectStartOfTurn(new RelicEffectParams(), managers.GetCoreManagers()));
            }
            finally { control = false; }
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (deployment < 2)
            {
                PlayCardAction? play = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action => action.CardInstanceId == Units[deployment] && action.RoomIndex == 0);
                if (play == null) throw new InvalidOperationException("Missing sequence deployment " + deployment);
                deployment++; mark = true; return play;
            }
            int turn = source.Spawn.Turn;
            if (turn < 1 || turn > 2) return null;
            if (lastTurn != turn) { lastTurn = turn; turnStep = 0; }
            string[] order = turn == 1 ? new[] { "Spell", "Spell", "Junk", "Blight" } : new[] { "Spell", "Junk", "Blight", "Spell" };
            if (turnStep == order.Length) return null;
            PlayCardAction? next = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
                source.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.MaskDescriptor?.Definition.CardType == order[turnStep] && action.RoomIndex == 0);
            if (next == null) throw new InvalidOperationException("Missing sequence play " + turn + "/" + turnStep + ": " + order[turnStep]);
            turnStep++; mark = true; return next;
        }
        internal static bool IsScenarioAction(PlayCardAction action) { if (!mark) return false; mark = false; return true; }
        private static Record? Begin(string kind, CardState? card = null)
        {
            if (!Prepared || observing || AllGameManagers.Instance!.GetSaveManager().PreviewMode) return null;
            observing = true; var row = new Record { Kind = kind, Control = control }; Records.Add(row);
            try
            {
                row.CardId = card == null ? 0 : FullBattleTrace.Active!.CardId(card); row.CardType = card?.GetCardType().ToString(); row.IsAbility = card?.IsAnyAbility();
                row.Before = FullBattleTrace.Active!.CaptureTrain(); RelicFirstCardCostResult predicted;
                if (kind == "Test") { predicted = RelicCardTypeSequenceModel.Test(row.Before.Context!, row.CardType, row.IsAbility, out bool result); row.ExpectedReturn = result; }
                else if (kind == "Apply") predicted = RelicCardTypeSequenceModel.Apply(row.Before.Context!, row.CardId);
                else if (kind == "Start") predicted = RelicCardTypeSequenceModel.StartTurn(row.Before.Context!);
                else { predicted = new RelicFirstCardCostResult(row.Before.Context!); row.ExpectedReturn = true; }
                row.UnsupportedReason = predicted.UnsupportedReason;
                if (predicted.Supported) row.Predicted = CardSpellModel.WithContext(row.Before, predicted.Context);
            }
            catch (Exception error) { row.Error = error.ToString(); }
            finally { observing = false; }
            return row;
        }
        private static void Finish(Record? row, bool? result = null)
        {
            if (row == null) return; observing = true;
            try { row.Returned = result; row.Actual = FullBattleTrace.Active!.CaptureTrain(); row.Completed = true;
                if (result != row.ExpectedReturn || row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual))) row.Difference = "Native card-type sequence state or result differs."; }
            catch (Exception error) { row.Error = error.ToString(); }
            finally { observing = false; }
        }
        [HarmonyPatch(typeof(RelicEffectEnergyAndCardDrawOnCardPlaySequence), nameof(RelicEffectEnergyAndCardDrawOnCardPlaySequence.TestEffectOnCardPlayed))]
        private static class TestPatch
        { private static void Prefix(CardPlayedRelicEffectParams relicEffectParams, out Record? __state) => __state = Begin("Test", relicEffectParams.cardState);
            private static void Postfix(bool __result, Record? __state) => Finish(__state, __result); }
        [HarmonyPatch(typeof(RelicEffectEnergyAndCardDrawOnCardPlaySequence), nameof(RelicEffectEnergyAndCardDrawOnCardPlaySequence.TestEffectStartOfTurn))]
        private static class StartTestPatch
        { private static void Prefix(out Record? __state) => __state = Begin("StartTest"); private static void Postfix(bool __result, Record? __state) => Finish(__state, __result); }
        [HarmonyPatch(typeof(RelicEffectEnergyAndCardDrawOnCardPlaySequence), nameof(RelicEffectEnergyAndCardDrawOnCardPlaySequence.ApplyEffectOnCardPlayed))]
        private static class ApplyPatch
        { private static void Postfix(CardPlayedRelicEffectParams relicEffectParams, ref IEnumerator __result) => __result = Observe(__result, "Apply", relicEffectParams.cardState); }
        [HarmonyPatch(typeof(RelicEffectEnergyAndCardDrawOnCardPlaySequence), nameof(RelicEffectEnergyAndCardDrawOnCardPlaySequence.ApplyEffectStartOfTurn))]
        private static class StartPatch
        { private static void Postfix(ref IEnumerator __result) => __result = Observe(__result, "Start"); }
        private static IEnumerator Observe(IEnumerator native, string kind, CardState? card = null)
        { Record? row = Begin(kind, card); applying = row; try { while (native.MoveNext()) yield return native.Current; }
            finally { (native as IDisposable)?.Dispose(); applying = null; Finish(row); } }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggered), new[] { typeof(RelicState), typeof(IRelicEffect) })]
        private static class NotifyPatch
        { private static void Prefix(IRelicEffect triggeredEffect) { if (applying != null && triggeredEffect is RelicEffectEnergyAndCardDrawOnCardPlaySequence) applying.Notifications++; } }
        private static void Drain(IEnumerator native) { try { while (native.MoveNext()) { if (native.Current is IEnumerator child) Drain(child); } } finally { (native as IDisposable)?.Dispose(); } }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
