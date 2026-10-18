using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalScaledEnergyScenario
    {
        internal const string Name = "original-scaled-energy";
        internal const string CardId = "001ff190-4559-42b9-9ca0-73e557656e85";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal sealed class DiscardSample
        {
            public int CardId { get; set; }
            public bool WasPlayed { get; set; }
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
            public bool Completed { get; set; }
        }
        internal static readonly List<DiscardSample> Samples = new List<DiscardSample>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 200); Set(unit, "size", 1);
            CharacterData guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack"); Set(guard, "health", 400);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData drain = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            drain.name = "PojuScaledEnergyDrain"; Set(drain, "id", "ac110022-0000-4000-8000-000000000001");
            Set(drain, "cost", 0); Set(drain, "costType", CardData.CostType.Default); Set(drain, "targetless", true); Set(drain, "targetsRoom", true);
            Set(drain, "traits", new List<CardTraitData>()); Set(drain, "startingUpgrades", new List<CardUpgradeData>());
            Set(drain, "triggers", new List<CardTriggerEffectData>()); Set(drain, "effectTriggers", new List<CharacterTriggerData>());
            var remove = new CardEffectData("CardEffectAdjustEnergy", null!, Team.Type.None); remove.Cheat_SetTargetMode(TargetMode.Room); Set(remove, "paramInt", -99);
            Set(drain, "effects", new List<CardEffectData> { remove });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(drain);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(CardId), Add(drain.GetID()), Add(CardId), Add(CardId) };
            foreach (CardState card in created.Skip(2).Take(3)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-SCALED-ENERGY-PREPARED original GainXEnergy, positive/zero paid X, current-turn gain before paid-cost clear, unplayed discard/redraw and Consume controls.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids.Take(5).ToArray(), id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
        }
        [HarmonyPatch(typeof(CardTraitScalingAddEnergy), nameof(CardTraitScalingAddEnergy.OnCardDiscarded))]
        private static class DiscardPatch
        {
            private static void Postfix(CardManager.DiscardCardParams discardCardParams, ref IEnumerator __result)
            {
                if (Enabled && Prepared && !AllGameManagers.Instance!.GetSaveManager().PreviewMode && discardCardParams.discardCard.GetCardDataID() == CardId)
                    __result = Observe(__result, discardCardParams);
            }
        }
        private static IEnumerator Observe(IEnumerator native, CardManager.DiscardCardParams args)
        {
            var row = new DiscardSample { CardId = FullBattleTrace.Active!.CardId(args.discardCard), WasPlayed = args.wasPlayed, Before = FullBattleTrace.Active.CaptureContext() };
            Samples.Add(row);
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); row.After = FullBattleTrace.Active!.CaptureContext(); }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
