using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPileStatusScenario
    {
        internal const string Name = "original-pile-status";
        internal const string CardId = "3d6898b0-2e34-41a4-a6ff-e301b0d46d5e";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static bool QueriesUnchanged { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<object> Queries = new List<object>();
        internal static readonly List<object> Samples = new List<object>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            while (save.GetVisibleDeckCount() < 30) save.AddCardToDeck(steward, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            if (save.GetVisibleDeckCount() != 30) throw new InvalidOperationException("Pile status witness requires thirty visible permanent cards.");
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData battery = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            battery.name = "PojuPileStatusBattery"; Set(battery, "id", "ac110028-0000-4000-8000-000000000001");
            Set(battery, "cost", 0); Set(battery, "costType", CardData.CostType.Default); Set(battery, "targetless", true); Set(battery, "targetsRoom", true);
            Set(battery, "traits", new List<CardTraitData>()); Set(battery, "startingUpgrades", new List<CardUpgradeData>());
            Set(battery, "triggers", new List<CardTriggerEffectData>()); Set(battery, "effectTriggers", new List<CharacterTriggerData>());
            var energy = new CardEffectData("CardEffectGainEnergy", null!, Team.Type.None); energy.Cheat_SetTargetMode(TargetMode.Room); Set(energy, "paramInt", 1);
            Set(battery, "effects", new List<CardEffectData> { energy });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(battery);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(battery.GetID()), Add(CardId), Add(CardId) };
            foreach (CardState card in created.Skip(2)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            FullBattleTrace trace = FullBattleTrace.Active!; CardIds = created.Select(trace.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            trace.CaptureDecision();
            List<CardState> deck = save.GetDeckState(); CardState[] original = deck.ToArray();
            JToken before = JToken.FromObject(trace.CaptureDecision()); JObject rng = RngStates(); int frame = UnityEngine.Time.frameCount;
            try
            {
                foreach (int count in new[] { 0, 9, 10, 19, 20, 30 })
                {
                    deck.Clear(); deck.AddRange(original.Take(count));
                    foreach (string status in new[] { "horde", "armor" })
                    foreach (int input in new[] { 0, 9 })
                    {
                        CombatContext context = trace.CaptureContext();
                        int actual = created[3].GetTraitStates().OfType<CardTraitScalingStatusEffectPerPile>().Single()
                            .OnStatusEffectApplied(null!, created[3], managers.GetCoreManagers(), status, input);
                        Queries.Add(new { CardId = trace.CardId(created[3]), VisibleDeckCount = save.GetVisibleDeckCount(), StatusId = status,
                            InputStacks = input, ActualBonus = actual, Context = context,
                            StateUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(JToken.FromObject(context), JToken.FromObject(trace.CaptureContext())) });
                    }
                }
            }
            finally { deck.Clear(); deck.AddRange(original); }
            QueriesUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(rng, RngStates()) && JToken.DeepEquals(before, JToken.FromObject(trace.CaptureDecision()));
            if (!QueriesUnchanged) throw new InvalidOperationException("Pile status queries failed to restore deck, decision, RNG or frame.");
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-PILE-STATUS-PREPARED original Dig Deep, six visible permanent-deck thresholds, unmatched status/incoming controls, new Horde and existing-Horde extension.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index < 2 ? index + 1 : 0));
        }
        [HarmonyPatch(typeof(CardTraitScalingStatusEffectPerPile), nameof(CardTraitScalingStatusEffectPerPile.OnStatusEffectApplied))]
        private static class BonusPatch
        {
            private static void Postfix(CardTraitScalingStatusEffectPerPile __instance, string statusId, int sourceStacks, int __result)
            {
                if (!Enabled || !Prepared || AllGameManagers.Instance!.GetSaveManager().PreviewMode || __instance.GetCard().GetCardDataID() != CardId) return;
                FullBattleTrace trace = FullBattleTrace.Active!;
                Samples.Add(new { CardId = trace.CardId(__instance.GetCard()), StatusId = statusId, InputStacks = sourceStacks, ActualBonus = __result, Context = trace.CaptureContext() });
            }
        }
        private static JObject RngStates()
        {
            var result = new JObject();
            foreach (RngId id in Enum.GetValues(typeof(RngId))) if (id != RngId.NonDeterministic && id != RngId.Chatter)
                result[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
            return result;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
