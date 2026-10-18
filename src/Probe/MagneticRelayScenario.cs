using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class MagneticRelayScenario
    {
        internal const string Name = "magnetic-relay";
        internal const string DataId = "ee166f41-20e0-4bc5-b238-f4082fe72a17";
        internal const string ControlId = "f15d0008-0000-4000-8000-000000000001";
        internal const string FullNoOverflowId = "f15d0008-0000-4000-8000-000000000003";
        internal const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static int[] UnitCardIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<Record> Records = new List<Record>();
        internal sealed class Record
        {
            public int CardId { get; set; }
            public int TargetId { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? After { get; set; }
            public UnitReturnRule Rule { get; set; } = null!;
            public bool Completed { get; set; }
            public bool TestReturned { get; set; }
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); CardManager cards = managers.GetCardManager()!;
            AllGameData all = save.GetAllGameData(); CardData steward = all.FindCardData(StewardId)!;
            Set(steward.GetSpawnCharacterData(), "attackDamage", 60); Set(steward.GetSpawnCharacterData(), "health", 100);
            Set(steward.GetSpawnCharacterData(), "size", 1); Set(steward, "cost", 0);
            var gold = new CardEffectData("CardEffectRewardGold", null!, Team.Type.None); gold.Cheat_SetTargetMode(TargetMode.Room); Set(gold, "paramInt", 5);
            Set(steward.GetSpawnCharacterData(), "triggers", new List<CharacterTriggerData> { new CharacterTriggerData(CharacterTriggerData.Trigger.OnDeath, gold) });
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == StewardId)) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            CardIds = Enumerable.Range(0, 2).Select(_ => FullBattleTrace.Active!.CardId(cards.AddNewCard(all.FindCardData(DataId)!, CardPile.HandPile, false, false, animate: false))).ToArray();
            var units = Enumerable.Range(0, 2).Select(_ => cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false)).ToArray();
            UnitCardIds = units.Select(FullBattleTrace.Active!.CardId).ToArray();
            foreach (CardState card in units)
            {
                var permanent = new CardUpgradeState(); permanent.Setup(); permanent.SetAttackDamage(3); permanent.SetAdditionalHP(4);
                card.ApplyPermanentUpgrade(permanent, save);
                var temporary = new CardUpgradeState(); temporary.Setup(); temporary.SetAttackDamage(2);
                card.ApplyTemporaryUpgrade(temporary, save);
                Set(card.GetTemporaryCardStateModifiers(), "additionalDamage", 4);
                Set(card.GetTemporaryCardStateModifiers(), "additionalCost", 1);
            }
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            CardData filler = UnityEngine.Object.Instantiate(all.FindCardData("b776421c-915c-4c3d-87dc-0fe1a9e8584c")!);
            filler.name = "PojuRecallFiller"; Set(filler, "id", "f15d0008-0000-4000-8000-000000000002");
            Set(filler, "cardType", CardType.Junk); Set(filler, "cost", 999);
            Set(filler, "startingUpgrades", new List<CardUpgradeData>()); Set(filler, "triggers", new List<CardTriggerEffectData>());
            Set(filler, "effectTriggers", new List<CharacterTriggerData>()); Set(filler, "targetless", false); Set(filler, "targetsRoom", true);
            var infusion = new CardTraitData(); infusion.Setup("CardTraitInfusion"); Set(filler, "traits", new List<CardTraitData> { infusion });
            var noop = new CardEffectData("CardEffectNULL", null!, Team.Type.None); noop.Cheat_SetTargetMode(TargetMode.Room); Set(noop, "paramBool2", true);
            Set(filler, "effects", new List<CardEffectData> { noop }); definitions.Add(filler);
            CardPool pool = ScriptableObject.CreateInstance<CardPool>(); pool.name = "PojuRecallFillPool";
            object list = AccessTools.Field(typeof(CardPool), "cardDataList").GetValue(pool);
            AccessTools.Method(list.GetType(), "Add", new[] { typeof(CardData) }).Invoke(list, new object[] { filler });
            CardData control = UnityEngine.Object.Instantiate(all.FindCardData(DataId)!); control.name = "PojuRecallOverflowClear";
            Set(control, "id", ControlId); Set(control, "cost", 0); Set(control, "traits", new List<CardTraitData>());
            var generate = new CardEffectData("CardEffectAddBattleCard", null!, Team.Type.None); generate.Cheat_SetTargetMode(TargetMode.Room);
            Set(generate, "paramInt", (int)CardPile.HandPile); Set(generate, "additionalParamInt", 10); Set(generate, "paramCardPool", pool);
            var recall = new CardEffectData("CardEffectReturnUnitToHand", null!, Team.Type.Monsters);
            recall.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(recall, "paramBool2", true); Set(recall, "paramBool3", true);
            Set(control, "effects", new List<CardEffectData> { generate, recall }); definitions.Add(control);
            cards.AddNewCard(control, CardPile.HandPile, false, false, animate: false);
            CardData blocked = UnityEngine.Object.Instantiate(control); blocked.name = "PojuRecallFullNoOverflow"; Set(blocked, "id", FullNoOverflowId);
            var noOverflow = new CardEffectData("CardEffectReturnUnitToHand", null!, Team.Type.Monsters);
            noOverflow.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(noOverflow, "paramBool", true); Set(noOverflow, "paramBool3", true);
            Set(blocked, "effects", new List<CardEffectData> { generate, noOverflow }); definitions.Add(blocked);
            cards.AddNewCard(blocked, CardPile.HandPile, false, false, animate: false);
            while (cards.GetHand().Count < cards.GetMaxHandSize()) cards.AddNewCard(filler, CardPile.HandPile, false, false, animate: false);
            managers.GetPlayerManager().AddEnergy(10); Prepared = true;
            log.LogInfo("MAGNETIC-RELAY-PREPARED original return-before-Kill, full-hand overflow/refusal, temporary upgrade/offset reset and unfired death-gold controls.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] cards, int[] units)
        {
            CombatContext context = source.Spawn.Train.Context!; var plays = BattleActionModel.EnumerateSupportedPlays(source);
            var steward = plays.FirstOrDefault(play => play.RoomIndex == 0 && context.FindCard(play.CardInstanceId)!.DataId == StewardId);
            if (steward != null) return steward;
            int Target(int cardId) => source.Spawn.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.SpawnerCardId == cardId)?.Id ?? -1;
            if (context.Statistics!.Value(cards[0], "TimesPlayed", "ThisBattle") == 0)
                return plays.FirstOrDefault(play => play.CardInstanceId == cards[0] && play.TargetUnitId == Target(units[0]));
            CardInstanceState control = context.CardInstances!.First(card => card.DataId == ControlId);
            if (context.Statistics!.Value(control.InstanceId, "TimesPlayed", "ThisBattle") == 0)
                return plays.FirstOrDefault(play => play.CardInstanceId == control.InstanceId && play.TargetUnitId == Target(units[0]));
            CardInstanceState blocked = context.CardInstances!.First(card => card.DataId == FullNoOverflowId);
            if (context.Statistics!.Value(blocked.InstanceId, "TimesPlayed", "ThisBattle") == 0)
                return plays.FirstOrDefault(play => play.CardInstanceId == blocked.InstanceId && play.TargetUnitId == Target(units[1]));
            if (context.Statistics!.Value(cards[1], "TimesPlayed", "ThisBattle") == 0)
                return plays.FirstOrDefault(play => play.CardInstanceId == cards[1] && play.TargetUnitId == Target(units[1]));
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source, card => card.DataId != DataId && card.DataId != ControlId && card.DataId != FullNoOverflowId);
        }
        [HarmonyPatch(typeof(CardEffectReturnUnitToHand), nameof(CardEffectReturnUnitToHand.ApplyEffect))]
        private static class ApplyPatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (Enabled && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, cardEffectState, cardEffectParams);
            }
        }
        private static IEnumerator Observe(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
        {
            FullBattleTrace trace = FullBattleTrace.Active!; AllGameManagers managers = AllGameManagers.Instance!;
            var row = new Record { CardId = trace.CardId(effect.GetParentCardState()), TargetId = trace.UnitId(parameters.targets[0]),
                Before = trace.CaptureTrain(),
                Rule = new UnitReturnRule(effect.GetParamBool(), effect.GetParamBool2(), effect.GetParamBool3()),
                TestReturned = new CardEffectReturnUnitToHand().TestEffect(effect, parameters, managers.GetCoreManagers()) };
            Records.Add(row);
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); row.After = trace.CaptureTrain(); }
        }
        private static void Set(object target, string field, object value) =>
            (AccessTools.Field(target.GetType(), field) ?? throw new MissingFieldException(target.GetType().Name, field)).SetValue(target, value);
    }
}
