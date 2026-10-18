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
    internal static class AdaptiveMutationScenario
    {
        internal const string Name = "adaptive-mutation";
        internal const string DataId = "6f23bf7b-49c3-4a30-aa84-542ab9f939d0";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<Record> Records = new List<Record>();
        internal static string ControlId(int index) => "f15d0005-0000-4000-8000-00000000000" + index;
        internal sealed class Record
        {
            public int CardId { get; set; }
            public int[] Targets { get; set; } = Array.Empty<int>();
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? After { get; set; }
            public CardActionEffect Effect { get; set; } = null!;
            public BattlePlayRules Definitions { get; set; } = null!;
            public bool Completed { get; set; }
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); CardManager cards = managers.GetCardManager()!;
            AllGameData all = save.GetAllGameData();
            HealingScenario.RegisterHealingStatus("heal immunity", "StatusEffectHealImmunityState", 0);
            CardData steward = all.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!;
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            for (int index = 1; index <= 3; index++)
            {
                CardData card = UnityEngine.Object.Instantiate(steward); card.name = "PojuAdaptiveUnit" + index;
                Set(card, "id", ControlId(index)); Reset(card);
                CharacterData unit = UnityEngine.Object.Instantiate(steward.GetSpawnCharacterData()); unit.name = card.name + "Character";
                Set(unit, "attackDamage", index == 1 ? 40 : index == 2 ? 35 : 50); Set(unit, "health", index == 1 ? 2000 : 100); Set(unit, "size", 1);
                Set(unit, "unitAbility", null!);
                var triggers = new List<CharacterTriggerData> { HealingScenario.HealGold(5, false, false) };
                if (index == 3)
                {
                    var heal = Effect("CardEffectHeal", 17, TargetMode.Room);
                    triggers.Add(new CharacterTriggerData(CharacterTriggerData.Trigger.PostCombatHealing, heal));
                }
                Set(unit, "triggers", triggers);
                Set(unit, "startingStatusEffects", index == 2 ? new[] { new StatusEffectStackData { statusId = "heal immunity", count = 1 } } : Array.Empty<StatusEffectStackData>());
                var spawn = Effect("CardEffectSpawnMonster", 0, TargetMode.Room); Set(spawn, "paramCharacterData", unit);
                Set(card, "effects", new List<CardEffectData> { spawn }); definitions.Add(card);
                cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false);
            }
            CardData spellTemplate = all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell);
            for (int index = 4; index <= 5; index++)
            {
                CardData control = UnityEngine.Object.Instantiate(spellTemplate); control.name = "PojuAdaptiveControl" + index;
                Set(control, "id", ControlId(index)); Reset(control);
                var purge = new CardTraitData(); purge.Setup("CardTraitSelfPurge"); Set(control, "traits", new List<CardTraitData> { purge });
                var effects = new List<CardEffectData> { Effect(index == 4 ? "CardEffectDamage" : "CardEffectBuffDamage",
                    index == 4 ? 30 : 7, index == 4 ? TargetMode.Room : TargetMode.DropTargetCharacter) };
                if (index == 4) effects.Add(Effect("CardEffectDamage", 1470, TargetMode.DropTargetCharacter));
                Set(control, "effects", effects);
                definitions.Add(control); cards.AddNewCard(control, CardPile.HandPile, false, false, animate: false);
            }
            CardData original = all.FindCardData(DataId)!;
            CardIds = Enumerable.Range(0, 3).Select(index => FullBattleTrace.Active!.CardId(
                cards.AddNewCard(original, CardPile.HandPile, false, false, animate: false))).ToArray();
            managers.GetPlayerManager().AddEnergy(10); Prepared = true;
            log.LogInfo("ADAPTIVE-MUTATION-PREPARED three unchanged originals, injured/immune/healer targets, damage buff and OnHeal gold controls.");
        }
        private static CardEffectData Effect(string name, int value, TargetMode target)
        {
            var effect = new CardEffectData(name, null!, Team.Type.Monsters); effect.Cheat_SetTargetMode(target); Set(effect, "paramInt", value); return effect;
        }
        private static void Reset(CardData card)
        {
            Set(card, "cost", 0); Set(card, "costType", CardData.CostType.Default); Set(card, "targetless", false); Set(card, "targetsRoom", true);
            Set(card, "traits", new List<CardTraitData>()); Set(card, "startingUpgrades", new List<CardUpgradeData>());
            Set(card, "triggers", new List<CardTriggerEffectData>()); Set(card, "effectTriggers", new List<CharacterTriggerData>());
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] spells)
        {
            CombatContext context = source.Spawn.Train.Context!; var plays = BattleActionModel.EnumerateSupportedPlays(source);
            foreach (int index in new[] { 1, 2, 3, 4, 5 })
            {
                CombatUnit? normal = source.Spawn.Train.Rooms[0].Units.FirstOrDefault(unit => unit.SpawnerCardId > 0 && context.FindCard(unit.SpawnerCardId)?.DataId == ControlId(1));
                var action = plays.FirstOrDefault(play => play.RoomIndex == 0 && context.FindCard(play.CardInstanceId)!.DataId == ControlId(index) &&
                    (index < 4 || play.TargetUnitId == normal?.Id));
                if (action != null) return action;
            }
            for (int index = 0; index < spells.Length; index++)
            {
                if (context.Statistics!.Value(spells[index], "TimesPlayed", "ThisBattle") > 0) continue;
                CombatUnit? target = source.Spawn.Train.Rooms[0].Units.FirstOrDefault(unit => unit.SpawnerCardId > 0 && context.FindCard(unit.SpawnerCardId)?.DataId == ControlId(index + 1));
                return plays.FirstOrDefault(play => play.CardInstanceId == spells[index] && play.TargetUnitId == target?.Id);
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }
        [HarmonyPatch(typeof(CardEffectAddTempSwapStats), nameof(CardEffectAddTempSwapStats.ApplyEffect))]
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
            FullBattleTrace trace = FullBattleTrace.Active!;
            var row = new Record { CardId = trace.CardId(effect.GetParentCardState()), Before = trace.CaptureTrain(),
                Targets = parameters.targets.Select(trace.UnitId).ToArray(),
                Effect = BattleActionProbe.DescribeTriggerEffect(effect.GetSourceCardEffectData(), new List<string>()),
                Definitions = new BattlePlayRules(Array.Empty<RoomPlayRule>(), trace.KnownCards.Select(card => card.GetCardDataID()).Distinct()
                    .Select(id => BattleActionProbe.Definition(AllGameManagers.Instance!.GetSaveManager().GetAllGameData().FindCardData(id)!)).ToArray()) };
            Records.Add(row);
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); row.After = trace.CaptureTrain(); }
        }
        private static void Set(object target, string field, object value) =>
            (AccessTools.Field(target.GetType(), field) ?? throw new MissingFieldException(target.GetType().Name, field)).SetValue(target, value);
    }
}
