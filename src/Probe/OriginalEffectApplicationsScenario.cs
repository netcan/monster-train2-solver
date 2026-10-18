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
    internal static class OriginalEffectApplicationsScenario
    {
        internal const string Name = "original-effect-applications";
        internal const string CardId = "8916c550-474d-49db-89bf-75bacdc95db4";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<object> Counts = new List<object>();
        internal sealed class ApplicationSample
        {
            public int CardId { get; set; }
            public int[] Targets { get; set; } = Array.Empty<int>();
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? After { get; set; }
            public bool Completed { get; set; }
        }
        internal static readonly List<ApplicationSample> Applications = new List<ApplicationSample>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            CharacterData guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack"); Set(guard, "health", 15);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData Energy(string name, string id, int amount)
            {
                CardData data = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
                data.name = name; Set(data, "id", id); Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default);
                Set(data, "targetless", true); Set(data, "targetsRoom", true); Set(data, "traits", new List<CardTraitData>());
                Set(data, "startingUpgrades", new List<CardUpgradeData>()); Set(data, "triggers", new List<CardTriggerEffectData>());
                Set(data, "effectTriggers", new List<CharacterTriggerData>());
                var energy = new CardEffectData(amount < 0 ? "CardEffectAdjustEnergy" : "CardEffectGainEnergy", null!, Team.Type.None);
                energy.Cheat_SetTargetMode(TargetMode.Room); Set(energy, "paramInt", amount); Set(data, "effects", new List<CardEffectData> { energy });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data); return data;
            }
            CardData drain = Energy("PojuApplicationsDrain", "ac110024-0000-4000-8000-000000000001", -99);
            CardData one = Energy("PojuApplicationsOne", "ac110024-0000-4000-8000-000000000002", 1);
            CardData three = Energy("PojuApplicationsThree", "ac110024-0000-4000-8000-000000000003", 3);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(drain.GetID()), Add(CardId), Add(one.GetID()), Add(CardId), Add(three.GetID()), Add(CardId) };
            foreach (CardState card in created) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-EFFECT-APPLICATIONS-PREPARED original Moonbeam, enemy-present zero X, single armor hit, three applications with lethal front retarget and empty-tail control.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || source.Spawn.Turn < 1) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
        }
        [HarmonyPatch(typeof(CardEffectDamage), nameof(CardEffectDamage.GetEffectApplicationCount))]
        private static class CountPatch
        {
            private static void Postfix(CardEffectParams _unused1, int __result)
            {
                CardState card = _unused1.playedCard;
                if (!Enabled || !Prepared || AllGameManagers.Instance!.GetSaveManager().PreviewMode || card?.GetCardDataID() != CardId) return;
                FullBattleTrace trace = FullBattleTrace.Active!; CombatContext before = trace.CaptureContext(); int frame = UnityEngine.Time.frameCount;
                int direct = card.GetTraitStates().OfType<CardTraitScalingAddEffectApplications>().Single().GetApplyCount(AllGameManagers.Instance.GetCardStatistics());
                Counts.Add(new { CardId = trace.CardId(card), ActualCount = __result, DirectCount = direct, Context = before,
                    StateUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(JToken.FromObject(before), JToken.FromObject(trace.CaptureContext())) });
            }
        }
        [HarmonyPatch(typeof(CardEffectDamage), nameof(CardEffectDamage.ApplyEffect))]
        private static class ApplicationPatch
        {
            private static void Postfix(CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (Enabled && Prepared && !AllGameManagers.Instance!.GetSaveManager().PreviewMode && cardEffectParams.playedCard?.GetCardDataID() == CardId)
                    __result = Observe(__result, cardEffectParams);
            }
        }
        private static IEnumerator Observe(IEnumerator native, CardEffectParams args)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            var row = new ApplicationSample { CardId = trace.CardId(args.playedCard), Targets = args.targets.Select(trace.UnitId).ToArray(), Before = trace.CaptureCanonicalTrain() };
            Applications.Add(row);
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); row.After = trace.CaptureCanonicalTrain(); }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
