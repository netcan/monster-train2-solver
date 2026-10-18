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
    internal static class RestorationDetonationScenario
    {
        internal const string Name = "restoration-detonation";
        internal const string DataId = "5f4f426e-3ca6-4a04-bede-3eab9a3723a9";
        internal const string InjuryId = "f15d0000-0000-4000-8000-000000000004";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] HealCardIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<Record> Records = new List<Record>();
        private static Record? current;
        internal sealed class DamageRecord
        {
            public int Target { get; set; }
            public int Amount { get; set; }
            public int Gold { get; set; }
        }
        internal sealed class Record
        {
            public int CardId { get; set; }
            public int[] Targets { get; set; } = Array.Empty<int>();
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? After { get; set; }
            public CardActionEffect Effect { get; set; } = null!;
            public BattlePlayRules Definitions { get; set; } = null!;
            public bool Completed { get; set; }
            public List<DamageRecord> Damages { get; set; } = new List<DamageRecord>();
        }
        internal static string UnitId(int index) => "f15d0000-0000-4000-8000-00000000000" + index;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); CardManager cards = managers.GetCardManager()!;
            AllGameData data = save.GetAllGameData();
            HealingScenario.RegisterHealingStatus("heal multiplier", "StatusEffectHealMultiplierState", 2);
            HealingScenario.RegisterHealingStatus("heal immunity", "StatusEffectHealImmunityState", 0);
            CardData steward = data.GetAllCardData().First(card => card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data);
            for (int index = 1; index <= 3; index++)
            {
                CardData card = UnityEngine.Object.Instantiate(steward); card.name = "PojuRestorationUnit" + index;
                Set(card, "id", UnitId(index)); Reset(card, 0);
                CharacterData unit = UnityEngine.Object.Instantiate(steward.GetSpawnCharacterData()); unit.name = card.name + "Character";
                Set(unit, "attackDamage", 50); Set(unit, "health", 100); Set(unit, "size", 1); Set(unit, "unitAbility", null!);
                Set(unit, "triggers", new List<CharacterTriggerData> { HealingScenario.HealGold(5, false, false) });
                Set(unit, "startingStatusEffects", index == 1 ? Array.Empty<StatusEffectStackData>() : new[] {
                    new StatusEffectStackData { statusId = index == 2 ? "heal immunity" : "heal multiplier", count = 1 } });
                var spawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters);
                spawn.Cheat_SetTargetMode(TargetMode.Room); Set(spawn, "paramCharacterData", unit);
                Set(card, "effects", new List<CardEffectData> { spawn }); definitions.Add(card);
                cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false);
            }
            CardData injury = UnityEngine.Object.Instantiate(data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            injury.name = "PojuRestorationInjury"; Set(injury, "id", InjuryId); Reset(injury, 0);
            var purge = new CardTraitData(); purge.Setup("CardTraitSelfPurge"); Set(injury, "traits", new List<CardTraitData> { purge });
            var hurt = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters);
            hurt.Cheat_SetTargetMode(TargetMode.Room); Set(hurt, "paramInt", 30);
            Set(injury, "effects", new List<CardEffectData> { hurt }); definitions.Add(injury);
            cards.AddNewCard(injury, CardPile.HandPile, false, false, animate: false);
            CardData original = data.FindCardData(DataId)!;
            HealCardIds = Enumerable.Range(0, 4).Select(index =>
            {
                CardState added = cards.AddNewCard(original, CardPile.HandPile, false, false, animate: false);
                // Keep the two positive controls through the deployment discard.
                // This is an instance modifier; the original definition stays intact.
                if (index >= 2) { var frozen = new CardTraitData(); frozen.Setup("CardTraitFreeze"); cards.AddTemporaryTraitToCard(added, frozen); }
                return FullBattleTrace.Active!.CardId(added);
            }).ToArray();
            managers.GetPlayerManager().AddEnergy(6);
            Prepared = true;
            log.LogInfo("RESTORATION-DETONATION-PREPARED four unchanged originals; full HP, immunity, real healing, double healing and OnHeal gold controls.");
        }
        private static void Reset(CardData card, int cost)
        {
            Set(card, "cost", cost); Set(card, "costType", CardData.CostType.Default); Set(card, "targetless", false); Set(card, "targetsRoom", true);
            Set(card, "traits", new List<CardTraitData>()); Set(card, "startingUpgrades", new List<CardUpgradeData>());
            Set(card, "triggers", new List<CardTriggerEffectData>()); Set(card, "effectTriggers", new List<CharacterTriggerData>());
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            CombatContext context = source.Spawn.Train.Context!;
            var plays = BattleActionModel.EnumerateSupportedPlays(source);
            foreach (int index in new[] { 1, 2, 3 })
            {
                PlayCardAction? unit = plays.FirstOrDefault(play => play.RoomIndex == 0 && context.FindCard(play.CardInstanceId)!.DataId == UnitId(index));
                if (unit != null) return unit;
            }
            CombatUnit? Target(int index) => source.Spawn.Train.Rooms[0].Units.FirstOrDefault(unit => unit.SpawnerCardId > 0 && context.FindCard(unit.SpawnerCardId)?.DataId == UnitId(index));
            PlayCardAction? Heal(int index, int target) => context.Statistics!.Value(HealCardIds[index], "TimesPlayed", "ThisBattle") == 0
                ? plays.FirstOrDefault(play => play.CardInstanceId == HealCardIds[index] && play.TargetUnitId == target) : null;
            CombatUnit? normal = Target(1), immune = Target(2), doubled = Target(3);
            if (normal == null || immune == null || doubled == null) return null;
            PlayCardAction? full = Heal(0, normal.Id); if (full != null && normal.Health == normal.MaxHealth) return full;
            PlayCardAction? injury = plays.FirstOrDefault(play => play.RoomIndex == 0 && context.FindCard(play.CardInstanceId)!.DataId == InjuryId);
            if (injury != null) return injury;
            PlayCardAction? blocked = Heal(1, immune.Id); if (blocked != null) return blocked;
            if (!source.Spawn.Train.Rooms[0].Deployment && source.Spawn.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Enemy))
                return Heal(2, normal.Id) ?? Heal(3, doubled.Id);
            return null;
        }
        [HarmonyPatch(typeof(CardEffectHealAndDamageRelative), nameof(CardEffectHealAndDamageRelative.ApplyEffect))]
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
                Effect = new CardActionEffect("HealRelative", effect.GetTargetMode().ToString(), effect.GetParamInt(), false, true,
                    Array.Empty<CombatStatus>(), healDamageMultiplier: effect.GetAdditionalParamInt()),
                Definitions = new BattlePlayRules(Array.Empty<RoomPlayRule>(), trace.KnownCards.Select(card => card.GetCardDataID()).Distinct()
                    .Select(id => BattleActionProbe.Definition(AllGameManagers.Instance!.GetSaveManager().GetAllGameData().FindCardData(id)!)).ToArray()) };
            Records.Add(row);
            Record? parent = current; current = row;
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); row.After = trace.CaptureTrain(); current = parent; }
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.ApplyDamageToTarget))]
        private static class DamagePatch
        {
            private static void Prefix(int damage, CharacterState target)
            {
                if (current != null && target != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    current.Damages.Add(new DamageRecord { Target = FullBattleTrace.Active!.UnitId(target), Amount = damage,
                        Gold = AllGameManagers.Instance.GetSaveManager().GetGold() });
            }
        }
        private static void Set(object value, string field, object data) => AccessTools.Field(value.GetType(), field).SetValue(value, data);
    }
}
