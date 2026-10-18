using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalMonsterHandCostsScenario
    {
        internal const string Name = "original-monster-hand-costs";
        internal const string CorruptionId = "a27a19b7-9c21-48c3-85b7-5e4b643740be";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<object> Samples = new List<object>();
        internal static readonly List<object> DeploymentQueries = new List<object>();
        internal static bool QueriesUnchanged { get; private set; }
        private static CardState[] queryCards = Array.Empty<CardState>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 200); Set(unit, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(OriginalUnpurgableCardsScenario.BlightId), Add(CorruptionId),
                Add(steward.GetID()), Add(CorruptionId), Add(steward.GetID()) };
            queryCards = created.Concat(new[] { Add(EngineUpgradeScenario.EngineId), Add("bae01a8c-25dd-4a08-b08a-5f17a5f3f52a") }).ToArray();
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            managers.GetPlayerManager().AddEnergy(10);
            JToken before = JToken.FromObject(FullBattleTrace.Active!.CaptureDecision()); int frame = UnityEngine.Time.frameCount;
            JObject rng = RngStates();
            foreach (CardState card in queryCards) DeploymentQueries.Add(new { CardId = FullBattleTrace.Active!.CardId(card),
                Deployable = cards.IsDeployableCard(card), SourceTraits = CardSourceTraitProbe.Capture(card) });
            QueriesUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(rng, RngStates()) &&
                JToken.DeepEquals(before, JToken.FromObject(FullBattleTrace.Active!.CaptureDecision()));
            if (!QueriesUnchanged) throw new InvalidOperationException("Deployment eligibility queries changed decision/RNG/frame.");
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-MONSTER-HAND-COSTS-PREPARED two unchanged Titan Corruptions, Monster-only stacking/removal, Spell/room/Blight controls and native deployment eligibility. Initial draw ordering is outside this witness.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            int index = Array.FindIndex(ids, id => context.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (Samples.Count == (index < 0 ? ids.Length : index))
            {
                AllGameManagers managers = AllGameManagers.Instance!;
                Samples.Add(new { Context = context, Costs = queryCards.Select(card => new { CardId = FullBattleTrace.Active!.CardId(card),
                    Cost = card.GetCost(managers.GetCardStatistics(), managers.GetMonsterManager(), managers.GetRelicManager()) }).ToArray() });
            }
            if (index < 0) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 3 ? 1 : 0));
        }
        private static JObject RngStates()
        {
            var states = new JObject();
            foreach (RngId id in Enum.GetValues(typeof(RngId))) if (id != RngId.NonDeterministic && id != RngId.Chatter)
                states[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
            return states;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
