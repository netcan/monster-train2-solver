#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class JuiceUpgradeScenario
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "juice-upgrade";
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static object? Definitions { get; private set; }
        internal static object? CurrentDefinitions => upgrades.Select(Describe).ToArray();
        private static CardUpgradeData[] upgrades = Array.Empty<CardUpgradeData>();
        private static readonly HashSet<CardState> sources = new HashSet<CardState>();
        internal sealed class Sample
        {
            public int SourceId { get; set; }
            public int TargetId { get; set; }
            public CardUpgradeModifier Input { get; set; } = null!;
            public CardUpgradeModifier? Actual { get; set; }
            public bool Returned { get; set; }
            public IReadOnlyList<NextDrawUpgradeRule>? QueueAfter { get; set; }
        }
        internal static readonly List<Sample> Samples = new List<Sample>();
        private static object Describe(CardUpgradeData data) { var state = new CardUpgradeState(); state.Setup(data); return CardModifierProbe.Upgrade(state); }
        private static void Set(object target, string name, object value) => AccessTools.Field(target.GetType(), name).SetValue(target, value);
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var cards = managers.GetCardManager();
            if (save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0) throw new InvalidOperationException("Juice upgrade scenario requires isolated deployment.");
            upgrades = all.GetAllCardUpgradeData().Where(data => data.GetTraitDataUpgrades().Any(trait => CardJuiceModel.IsTrait(trait.GetTraitStateName())))
                .OrderBy(data => data.GetID(), StringComparer.Ordinal).ToArray();
            if (upgrades.Length != 5) throw new InvalidOperationException("Juice upgrade family changed.");
            Definitions = upgrades.Select(Describe).ToArray();
            var steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            Set(steward, "cost", 0); Set(steward.GetSpawnCharacterData(), "attackDamage", 80); Set(steward.GetSpawnCharacterData(), "health", 5000);
            foreach (var card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear(); Set(all.GetBalanceData(), "maxHandSize", 40);
            var created = new List<CardState>();
            CardState Add(CardData data, CardPile pile) => cards.AddNewCard(data, pile, false, false, animate: false);
            created.Add(Add(steward, CardPile.HandPile)); created.Add(Add(steward, CardPile.HandPile));
            var payload = DynamicUpgradeScenario.Upgrade("PojuJuiceDrawValor", "fc231ba2-0000-4000-8000-000000000002", 0, 0, 0, 0, "valor", 1);
            var filtered = DynamicUpgradeScenario.Upgrade("PojuJuiceRejectedArmor", "fc231ba2-0000-4000-8000-000000000003", 0, 0, 0, 0, "armor", 2);
            var mask = ScriptableObject.CreateInstance<CardUpgradeMaskData>(); mask.name = "PojuJuiceSpellOnly";
            Set(mask, "cardType", CardType.Spell); Set(filtered, "filters", new List<CardUpgradeMaskData> { mask });
            var skipped = DynamicUpgradeScenario.Upgrade("PojuJuiceSkippedScorch", "fc231ba2-0000-4000-8000-000000000004", 0, 0, 0, 0, "scorch", 3);
            var template = all.FindCardData("3020f56f-71a5-4155-8bed-48ada7e7e0de")!;
            for (int i = 0; i <= upgrades.Length; i++)
            {
                var source = UnityEngine.Object.Instantiate(template); source.name = "PojuJuiceDrawControl" + i;
                Set(source, "id", "fc231ba2-0000-4000-8001-" + (i + 1).ToString("D12")); Set(source, "cost", 0);
                Set(source, "traits", new List<CardTraitData>()); Set(source, "triggers", new List<CardTriggerEffectData>());
                Set(source, "effectTriggers", new List<CharacterTriggerData>()); Set(source, "startingUpgrades", new List<CardUpgradeData>());
                Set(source, "targetless", true); Set(source, "targetsRoom", true);
                var next = new CardEffectData("CardEffectAddTempCardUpgradeToNextDrawnCard", null!, Team.Type.None);
                next.Cheat_SetTargetMode(TargetMode.Room); Set(next, "paramCardUpgradeData", payload);
                var draw = new CardEffectData("CardEffectDraw", null!, Team.Type.None); draw.Cheat_SetTargetMode(TargetMode.Room); Set(draw, "paramInt", 2);
                var effects = new List<CardEffectData> { next };
                if (i == upgrades.Length)
                    foreach (var extra in new[] { filtered, skipped })
                    {
                        var entry = new CardEffectData("CardEffectAddTempCardUpgradeToNextDrawnCard", null!, Team.Type.None);
                        entry.Cheat_SetTargetMode(TargetMode.Room); Set(entry, "paramCardUpgradeData", extra); effects.Add(entry);
                    }
                effects.Add(draw); Set(source, "effects", effects);
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(source);
                var card = Add(source, CardPile.HandPile); var upgrade = new CardUpgradeState(); upgrade.Setup(upgrades[i % upgrades.Length]);
                card.ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
                var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers());
                created.Add(card); sources.Add(card);
            }
            for (int i = 0; i < 20; i++) Add(steward, CardPile.DeckPile);
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("JUICE-UPGRADE-PREPARED five unchanged original upgrades; two-draw controls plus filtered shared-alias/short-circuit case.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int index = Array.FindIndex(CardIds, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
            return BattleActionModel.EnumerateSupportedPlays(source, CardIds[index]).FirstOrDefault(play => play.RoomIndex == (index == 1 ? 2 : 0));
        }
        [HarmonyPatch(typeof(CardTraitJuice), nameof(CardTraitJuice.OnCardBeingUpgraded))]
        private static class CallbackPatch
        {
            private static void Prefix(CardTraitJuice __instance, CardState cardState, CardState? upgradingCardSource,
                CardUpgradeState cardUpgradeState, out Sample? __state)
            {
                __state = null;
                if (!Enabled || !Prepared || !sources.Contains(__instance.GetCard()) || AllGameManagers.Instance!.GetSaveManager().PreviewMode) return;
                var trace = FullBattleTrace.Active!;
                __state = new Sample { SourceId = trace.CardId(upgradingCardSource!), TargetId = trace.CardId(cardState), Input = CardModifierProbe.Upgrade(cardUpgradeState) };
                Samples.Add(__state);
            }
            private static void Postfix(CardUpgradeState cardUpgradeState, bool __result, Sample? __state)
            {
                if (__state == null) return;
                __state.Actual = CardModifierProbe.Upgrade(cardUpgradeState); __state.Returned = __result;
                __state.QueueAfter = TypedDrawProbe.CaptureQueue(AllGameManagers.Instance!.GetCardManager());
            }
        }
    }
}
#endif
