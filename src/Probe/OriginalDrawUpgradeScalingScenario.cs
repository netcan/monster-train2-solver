using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalDrawUpgradeScalingScenario
    {
        internal const string Name = "original-draw-upgrade-scaling";
        internal const string CardId = "2e77fd27-8322-46ce-a56f-d45844ee577e";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<object> Samples = new List<object>();
        internal static readonly List<object> Queries = new List<object>();
        private static bool querying;
        internal sealed class Sample
        {
            public int CardId { get; set; }
            public int TargetCardId { get; set; }
            public CardUpgradeModifier Input { get; set; } = null!;
            public CardUpgradeModifier? Actual { get; set; }
            public bool Returned { get; set; }
            public CombatContext Context { get; set; } = null!;
            public CombatContext? After { get; set; }
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData battery = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            battery.name = "PojuDrawUpgradeBattery"; Set(battery, "id", "ac110029-0000-4000-8000-000000000001");
            Set(battery, "cost", 0); Set(battery, "costType", CardData.CostType.Default); Set(battery, "targetless", true); Set(battery, "targetsRoom", true);
            Set(battery, "traits", new List<CardTraitData>()); Set(battery, "startingUpgrades", new List<CardUpgradeData>());
            Set(battery, "triggers", new List<CardTriggerEffectData>()); Set(battery, "effectTriggers", new List<CharacterTriggerData>());
            var energy = new CardEffectData("CardEffectGainEnergy", null!, Team.Type.None); energy.Cheat_SetTargetMode(TargetMode.Room); Set(energy, "paramInt", 10);
            Set(battery, "effects", new List<CardEffectData> { energy });
            CardData recipient = UnityEngine.Object.Instantiate(steward); recipient.name = "PojuDrawUpgradeRecipient";
            Set(recipient, "id", "ac110029-0000-4000-8000-000000000002"); Set(recipient, "cost", 4);
            CharacterData target = UnityEngine.Object.Instantiate(unit); target.name = "PojuDrawUpgradeRecipientUnit";
            Set(target, "attackDamage", 5); Set(target, "health", 40); Set(target, "size", 1);
            Set(recipient, "traits", new List<CardTraitData>()); Set(recipient, "startingUpgrades", new List<CardUpgradeData>());
            Set(recipient, "triggers", new List<CardTriggerEffectData>()); Set(recipient, "effectTriggers", new List<CharacterTriggerData>());
            var spawn = new CardEffectData("CardEffectSpawnMonster", target, Team.Type.Monsters); spawn.Cheat_SetTargetMode(steward.GetEffects()[0].GetTargetMode());
            Set(recipient, "effects", new List<CardEffectData> { spawn });
            CardData blank = UnityEngine.Object.Instantiate(battery); blank.name = "PojuDrawUpgradeBlank";
            Set(blank, "id", "ac110029-0000-4000-8000-000000000003");
            var empty = new CardEffectData("CardEffectNULL", null!, Team.Type.None); empty.Cheat_SetTargetMode(TargetMode.Room); Set(empty, "paramBool2", true);
            Set(blank, "effects", new List<CardEffectData> { empty });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).AddRange(new[] { battery, recipient, blank });
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(CardId), Add(CardId), Add(battery.GetID()), Add(CardId) };
            foreach (CardState card in created.Skip(2)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardState blankCard = cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            for (int index = 0; index < 9; index++) cards.AddNewCard(recipient, CardPile.DeckPile, false, false, animate: false);
            // Five natural top draws leave the Spell above the remaining Monsters.
            // Each typed draw must skip it, rather than merely drawing a top Monster.
            cards.GetDrawPile().Remove(blankCard); cards.GetDrawPile().Insert(4, blankCard);
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-DRAW-UPGRADE-SCALING-PREPARED original Prism Retrieval, X three/zero/ten, original +five/-one upgrade, typed Monster draw with Spell control, queue identities/reset and Consume.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
        }
        [HarmonyPatch(typeof(CardTraitScalingTempCardUpgradeToCard), nameof(CardTraitScalingTempCardUpgradeToCard.OnCardBeingUpgraded))]
        private static class ScalePatch
        {
            private static void Prefix(CardTraitScalingTempCardUpgradeToCard __instance, CardState cardState, CardUpgradeState cardUpgradeState, out Sample? __state)
            {
                __state = null;
                if (!Enabled || !Prepared || querying || AllGameManagers.Instance!.GetSaveManager().PreviewMode || __instance.GetCard().GetCardDataID() != CardId) return;
                FullBattleTrace trace = FullBattleTrace.Active!;
                __state = new Sample { CardId = trace.CardId(__instance.GetCard()), TargetCardId = trace.CardId(cardState),
                    Input = CardModifierProbe.Upgrade(cardUpgradeState), Context = trace.CaptureContext() };
                Samples.Add(__state);
            }
            private static void Postfix(CardTraitScalingTempCardUpgradeToCard __instance, CardState cardState, CardState upgradingCardSource,
                CardUpgradeState cardUpgradeState, bool __result, Sample? __state)
            {
                if (__state == null) return;
                FullBattleTrace trace = FullBattleTrace.Active!; __state.Actual = CardModifierProbe.Upgrade(cardUpgradeState);
                __state.Returned = __result; __state.After = trace.CaptureContext();
                querying = true;
                try
                {
                    var fresh = new CardUpgradeState(); fresh.Setup(AllGameManagers.Instance!.GetSaveManager().GetAllGameData().FindCardData(CardId)!.GetEffects()[0].GetParamCardUpgradeData());
                    CardUpgradeModifier input = CardModifierProbe.Upgrade(fresh); CombatContext before = trace.CaptureContext(); int frame = UnityEngine.Time.frameCount;
                    bool returned = __instance.OnCardBeingUpgraded(cardState, upgradingCardSource, fresh, AllGameManagers.Instance.GetCoreManagers());
                    Queries.Add(new { CardId = trace.CardId(__instance.GetCard()), Input = input, Actual = CardModifierProbe.Upgrade(fresh), Returned = returned, Context = before,
                        StateUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(JToken.FromObject(before), JToken.FromObject(trace.CaptureContext())) });
                }
                finally { querying = false; }
            }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
