using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalBalatroEventYorickScenario
    {
        internal const string Name = "original-balatro-event-yorick";
        internal const string OwnerId = "2bd14ebc-1589-4652-a002-54013f0eace4";
        internal const string OwnerKey = "BalatroEventYorick";
        internal const string CarrierTrait = "CardTraitExhaustState";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static bool PolicyActionPlayed { get; private set; }
        internal static int CarrierInstanceId { get; private set; }
        internal static string CarrierDataId { get; private set; } = "";
        internal static string? TestLabel { get; private set; }
        internal static readonly List<TestRecord> Tests = new List<TestRecord>();

        internal sealed class TestRecord
        {
            public string Label { get; set; } = "";
            public string CardDataId { get; set; } = "";
            public int CardInstanceId { get; set; }
            public int PlayCount { get; set; }
            public int PlayCountModifier { get; set; }
            public int ExhaustTraitCount { get; set; }
            public bool Result { get; set; }
            public string? CaptureError { get; set; }
        }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("BalatroEventYorick must be acquired once before battle.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == OwnerId);
            CheckOriginal(relic);
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-BALATRO-EVENT-YORICK-ACQUIRED unchanged owner=" + relic.name + " id=" + relic.GetID() +
                " gold=" + relic.GetEffects().Single().GetParamInt() + ".");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager()!;
            if (Prepared || !Acquired || save.PreviewMode || combat.GetTurnCount() != 0 ||
                save.GetCollectedRelics().Count(item => item.GetRelicDataID() == OwnerId) != 1)
                throw new InvalidOperationException("BalatroEventYorick requires its isolated deployment state.");

            AllGameData gameData = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardData carrier = owned.Select(card => gameData.FindCardData(card.GetCardDataID())!)
                .FirstOrDefault(card => card?.GetCardType() == CardType.Spell && !card.IsUnitAbility() && !card.IsRoomAbility())
                ?? throw new InvalidOperationException("BalatroEventYorick fixture needs an owned Spell card.");
            CardTraitData exhaust = gameData.GetAllCardData().Where(card => card != null).SelectMany(card => card.GetTraits())
                .FirstOrDefault(trait => trait.GetTraitStateName() == CarrierTrait)
                ?? throw new InvalidOperationException("Native CardTraitExhaustState data is unavailable.");
            Set(carrier, "cardType", CardType.Spell);
            Set(carrier, "effects", new List<CardEffectData>());
            Set(carrier, "targetless", true);
            Set(carrier, "targetsRoom", false);
            Set(carrier, "isUnitAbility", false);
            Set(carrier, "isRoomAbility", false);
            Set(carrier, "traits", carrier.GetTraits().Where(trait => trait.GetTraitStateName() != CarrierTrait)
                .Append(exhaust).ToList());
            Set(carrier, "cost", 0);
            foreach (CardState card in owned.Where(card => card.GetCardDataID() == carrier.GetID()))
                card.Setup(carrier, save, setupStartingUpgrades: false);

            CardState? inHand = cards.GetHand().FirstOrDefault(card => card.GetCardDataID() == carrier.GetID());
            if (inHand == null)
            {
                inHand = cards.AddNewCard(carrier, CardPile.HandPile, false, false, animate: false);
                if (inHand == null || !cards.GetHand().Contains(inHand))
                    throw new InvalidOperationException("Inert exhaust-trait Spell did not enter the native hand.");
            }
            CarrierDataId = carrier.GetID();
            CarrierInstanceId = FullBattleTrace.Active!.CardId(inHand);
            Prepared = true;

            List<RelicState> current = new List<RelicState>();
            AccessTools.Method(typeof(RelicManager), "GetCurrentRelics")!.Invoke(managers.GetRelicManager(), new object[] { current });
            RelicEffectAddGoldOnExhaust effect = current.Single(relic => relic.GetRelicDataID() == OwnerId)
                .GetEffects().OfType<RelicEffectAddGoldOnExhaust>().Single();
            CardState control = owned.FirstOrDefault(card => !ReferenceEquals(card, inHand) &&
                !card.GetTraitStates().Any(trait => trait is CardTraitExhaustState))
                ?? throw new InvalidOperationException("BalatroEventYorick fixture needs an owned non-Consume control card.");
            Test(effect, control, managers, "ManualNoExhaust");
            Test(effect, inHand, managers, "ManualExhaust");

            AccessTools.Field(typeof(CombatManager), "combatStateChanged")!.SetValue(combat, true);
            log.LogInfo("ORIGINAL-BALATRO-EVENT-YORICK-PREPARED unchanged owner; inert zero-cost Spell carrier=" + CarrierInstanceId +
                " control=" + FullBattleTrace.Active.CardId(control) + " amount=" + effect.SourceRelicEffectData.GetParamInt() + ".");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (!Prepared) return null;
            if (PolicyActionPlayed) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(item =>
                item.CardInstanceId == CarrierInstanceId && item.RoomIndex == 0);
            if (action != null) PolicyActionPlayed = true;
            return action;
        }

        internal static void MarkPolicyActionPlayed() => PolicyActionPlayed = true;
        internal static bool IsScenarioAction(PlayCardAction action) => Prepared && action.CardInstanceId == CarrierInstanceId;

        private static void Test(RelicEffectAddGoldOnExhaust effect, CardState card,
            AllGameManagers managers, string label)
        {
            RelicManager relicManager = managers.GetRelicManager();
            TestLabel = label;
            try
            {
                effect.TestEffectOnCardPlayed(new CardPlayedRelicEffectParams(new RelicEffectParams
                {
                    relicManager = relicManager,
                    saveManager = managers.GetSaveManager(),
                    screenManager = managers.GetScreenManager()
                }) { cardState = card, roomIndex = 0 }, managers.GetCoreManagers());
            }
            finally { TestLabel = null; }
        }

        internal static void RecordTest(CardState? card, bool result)
        {
            try
            {
                if (card == null) throw new InvalidOperationException("Card-play gold-on-exhaust test had no card.");
                RelicManager manager = AllGameManagers.Instance!.GetRelicManager();
                Tests.Add(new TestRecord
                {
                    Label = TestLabel ?? "Natural",
                    CardDataId = card.GetCardDataID(),
                    CardInstanceId = FullBattleTrace.Active!.CardId(card),
                    PlayCount = card.GetCurrentScenarioPlayCount(),
                    PlayCountModifier = manager.GetPlayCountForExhaustModifier(),
                    ExhaustTraitCount = card.GetTraitStates().OfType<CardTraitExhaustState>().Count(),
                    Result = result
                });
            }
            catch (Exception error)
            {
                Tests.Add(new TestRecord { Label = TestLabel ?? "Natural", CaptureError = error.ToString() });
                FullBattleTrace.Active?.CaptureFailure(error);
            }
        }

        internal static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData[] effects = relic.GetEffects().ToArray();
            if (relic.GetID() != OwnerId || relic.name != OwnerKey || effects.Length != 1 ||
                effects[0].GetEffectClassName() != RelicCardPlayGoldOnExhaustModel.Effect ||
                effects[0].GetEffectConditions().Count != 0 || effects[0].GetTraits().Count != 0)
                throw new InvalidOperationException("The original BalatroEventYorick definition changed.");
        }

        private static void Set(object target, string field, object value) =>
            AccessTools.Field(target.GetType(), field)!.SetValue(target, value);
    }
}
