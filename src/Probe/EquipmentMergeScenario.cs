using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class EquipmentMergeScenario
    {
        internal const string Name = "fused-weaponry";
        internal const string DataId = "e900f129-b9f5-4716-ba6f-f39980e54a5c";
        internal const string OutputId = "71d29f23-3ce3-468b-9ca9-61a681d9370a";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static int[] SourceIds { get; private set; } = Array.Empty<int>();
        internal static int[] UnitIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<Record> Records = new List<Record>();
        internal static readonly List<Test> Tests = new List<Test>();
        private static Record? current;
        internal sealed class Test
        {
            public CombatContext Context { get; set; } = null!;
            public bool Returned { get; set; }
            public EquipmentMergeRule Rule { get; set; } = null!;
        }
        internal sealed class Record
        {
            public int CardId { get; set; }
            public int[] Selected { get; set; } = Array.Empty<int>();
            public EquipmentMergeRule Rule { get; set; } = null!;
            public CardUpgradeModifier? Upgrade { get; set; }
            public BattlePlayRules Definitions { get; set; } = null!;
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
            public bool Completed { get; set; }
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); CardManager cards = managers.GetCardManager()!; AllGameData all = save.GetAllGameData();
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            Set(steward, "cost", 0); Set(steward.GetSpawnCharacterData(), "attackDamage", 60);
            Set(steward.GetSpawnCharacterData(), "health", 100); Set(steward.GetSpawnCharacterData(), "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            CardState original = cards.AddNewCard(all.FindCardData(DataId)!, CardPile.HandPile, false, false, animate: false);
            CardActionEffect rule = BattleActionProbe.Definition(all.FindCardData(DataId)!).Effects.Single();
            void ObserveTest() => Tests.Add(new Test { Context = FullBattleTrace.Active!.CaptureContext(), Rule = rule.EquipmentMerge!,
                Returned = new CardEffectMergeEquipment().TestEffect(original.GetEffectStates()[0], new CardEffectParams { playedCard = original }, managers.GetCoreManagers()) });
            ObserveTest();
            CardData Author(string id, string name, int cost)
            {
                CardData data = UnityEngine.Object.Instantiate(all.FindCardData("b776421c-915c-4c3d-87dc-0fe1a9e8584c")!);
                data.name = name; Set(data, "id", id); Set(data, "cost", cost);
                Set(data, "startingUpgrades", new List<CardUpgradeData>()); Set(data, "traits", new List<CardTraitData>());
                Set(data, "triggers", new List<CardTriggerEffectData>()); Set(data, "effectTriggers", new List<CharacterTriggerData>());
                var attach = new CardEffectData("CardEffectAttachEquipment", null!, Team.Type.Monsters);
                attach.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
                CardUpgradeData payload = DynamicUpgradeScenario.Upgrade(name + "Upgrade", id.Replace("00000000000", "10000000000"), 15, 15, 0, 0, "armor", 0);
                payload.GetStatusEffectUpgrades().Clear();
                Set(attach, "paramCardUpgradeData", payload);
                Set(data, "effects", new List<CardEffectData> { attach });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data); return data;
            }
            CardData a = Author("f15d0010-0000-4000-8000-000000000001", "PojuMergeA", 3);
            CardData b = Author("f15d0010-0000-4000-8000-000000000002", "PojuMergeB", 2);
            CardState a1 = cards.AddNewCard(a, CardPile.HandPile, false, false, animate: false); ObserveTest();
            CardState a2 = cards.AddNewCard(a, CardPile.DeckPile, false, false, animate: false); ObserveTest(); // Test accepts two identities of one definition; UI cannot pair them.
            CardState b1 = cards.AddNewCard(b, CardPile.DiscardPile, false, false, animate: false);
            CardState b2 = cards.AddNewCard(b, CardPile.DiscardPile, false, false, animate: false); ObserveTest();
            void Upgrade(CardState card, int attack, int hp, int reduction, bool temporary)
            {
                var upgrade = new CardUpgradeState(); upgrade.Setup(); upgrade.SetAttackDamage(attack); upgrade.SetAdditionalHP(hp); upgrade.SetCostReduction(reduction);
                if (temporary) card.ApplyTemporaryUpgrade(upgrade, save); else card.ApplyPermanentUpgrade(upgrade, save);
            }
            Upgrade(a1, 3, 4, 1, false); Upgrade(a1, 7, 8, -1, true);
            Upgrade(a2, 6, 7, 2, false); Upgrade(a2, 9, 10, -2, true);
            SourceIds = new[] { a1, b1, a2, b2 }.Select(FullBattleTrace.Active!.CardId).ToArray();
            CardIds = new[] { original }.Concat(Enumerable.Range(0, 2).Select(_ => cards.AddNewCard(all.FindCardData(DataId)!, CardPile.HandPile, false, false, animate: false)))
                .Select(FullBattleTrace.Active!.CardId).ToArray();
            UnitIds = Enumerable.Range(0, 2).Select(_ => FullBattleTrace.Active!.CardId(cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false))).ToArray();
            managers.GetPlayerManager().AddEnergy(25); Prepared = true;
            log.LogInfo("FUSED-WEAPONRY-PREPARED original cancel, hand/discard merge and draw/discard merge, same-definition Test control, distinct permanent/temporary upgrades and merged-equipment play.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] spells, int[] inputs, int[] units)
        {
            CombatContext context = source.Spawn.Train.Context!; var plays = BattleActionModel.EnumerateSupportedPlays(source);
            foreach (int id in units)
                if (context.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0)
                    return plays.FirstOrDefault(play => play.CardInstanceId == id && play.RoomIndex == 0);
            for (int index = 0; index < spells.Length; index++)
                if (context.Statistics!.Value(spells[index], "TimesPlayed", "ThisBattle") == 0)
                {
                    int[] selected = index == 0 ? Array.Empty<int>() : inputs.Skip((index - 1) * 2).Take(2).ToArray();
                    return plays.FirstOrDefault(play => play.CardInstanceId == spells[index] && play.RoomIndex == 0 &&
                        play.RecursionSelections.Single().CardInstanceIds.SequenceEqual(selected));
                }
            return plays.FirstOrDefault(play => context.FindCard(play.CardInstanceId)!.DataId == OutputId && play.RoomIndex == 0 &&
                source.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == play.TargetUnitId && unit.EquipmentCards?.Count == 0));
        }
        [HarmonyPatch(typeof(CardEffectMergeEquipment), nameof(CardEffectMergeEquipment.ApplyEffect))]
        private static class ApplyPatch
        {
            private static void Postfix(CardEffectState cardEffectState, ref IEnumerator __result)
            { if (Enabled && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode) __result = Observe(__result, cardEffectState); }
        }
        private static IEnumerator Observe(IEnumerator native, CardEffectState state)
        {
            FullBattleTrace trace = FullBattleTrace.Active!; int id = trace.CardId(state.GetParentCardState()); int index = Array.IndexOf(CardIds, id);
            CardActionEffect effect = BattleActionProbe.DescribeTriggerEffect(state.GetSourceCardEffectData(), new List<string>());
            var row = new Record { CardId = id, Rule = effect.EquipmentMerge!, Upgrade = effect.Upgrade,
                Selected = index == 0 ? Array.Empty<int>() : SourceIds.Skip((index - 1) * 2).Take(2).ToArray(),
                Definitions = trace.CaptureDecision().PlayRules!, Before = trace.CaptureContext() };
            Records.Add(row); current = row;
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { current = null; (native as IDisposable)?.Dispose(); row.After = trace.CaptureContext(); }
        }
        [HarmonyPatch(typeof(EquipmentMergeRewardData), nameof(EquipmentMergeRewardData.GrantTemporaryReward))]
        private static class SelectPatch
        {
            private static bool Prefix(EquipmentMergeRewardData __instance, Action<List<CardState>> cardsChosenCallback, Func<CardState, bool> additionalFilter, ref IEnumerator __result)
            {
                if (!Enabled || current == null) return true;
                __result = Select(__instance, cardsChosenCallback, additionalFilter, current); return false;
            }
        }
        private static IEnumerator Select(EquipmentMergeRewardData reward, Action<List<CardState>> chosen, Func<CardState, bool> filter, Record row)
        {
            if (row.Selected.Length == 0) yield break;
            AllGameManagers managers = AllGameManagers.Instance!; var selected = new List<CardState>();
            foreach (int id in row.Selected)
            {
                CardState card = managers.GetCardManager()!.GetAllCards(new List<CardState>()).Single(card => FullBattleTrace.Active!.CardId(card) == id);
                if (reward.FilterFunc(card, selected, filter, managers.GetRelicManager())) throw new InvalidOperationException("Native merge rejected selection.");
                selected.Add(card);
            }
            chosen(selected);
        }
        [HarmonyPatch(typeof(CardManager), nameof(CardManager.AddNonPermanentCardToDeck))]
        private static class IdentityPatch
        { private static void Prefix(CardState cardState) { if (Enabled && current != null) FullBattleTrace.Active!.CardId(cardState); } }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
