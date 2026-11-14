using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DevoteBattleScenario
    {
        internal const string Name = "devote-battle";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static bool Running { get; private set; }
        internal static string? Error { get; private set; }
        private static int finisher, persistent, infusion, frozen, ordinary, devotes;
        private static bool deployed, recording;
        private static readonly List<object> observations = new List<object>();
        private static readonly List<string> notifications = new List<string>();
        private static readonly List<object> gates = new List<object>();
        private static AllGameManagers Managers => AllGameManagers.Instance!;
        internal static object Evidence => new { Prepared, Finisher = finisher, Persistent = persistent, Infusion = infusion,
            Frozen = frozen, Ordinary = ordinary, Devotes = devotes, Gates = gates, Observations = observations, Error };
        private static object Call(string method, params object[] args) =>
            AccessTools.Method(typeof(CharacterState).Assembly.GetType("MonolithHelper"), method).Invoke(null, args);
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (!ConditionalActivationProbe.HasVanguard || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Devote requires isolated 2.3.1 deployment.");
            var all = managers.GetAllGameData(); var cards = managers.GetCardManager();
            CardData steward = all.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678");
            CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "health", 100); Set(unit, "attackDamage", 60); Set(unit, "size", 1);
            Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(unit, "unitAbility", null!);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            int Add(int index, string? trait)
            {
                CardData card = UnityEngine.Object.Instantiate(all.FindCardData(DevotionSpawnBattleScenario.VenerateId));
                card.name = "PojuDevoteControl" + index;
                Set(card, "id", "fc231de0-0000-4000-8000-00000000000" + index); Set(card, "cost", 0);
                Set(card, "effects", new List<CardEffectData>()); Set(card, "triggers", new List<CardTriggerEffectData>());
                Set(card, "effectTriggers", new List<CharacterTriggerData>()); Set(card, "startingUpgrades", new List<CardUpgradeData>());
                Set(card, "targetless", true); Set(card, "targetsRoom", true);
                var traits = new List<CardTraitData>();
                if (trait != null) { var data = new CardTraitData(); data.Setup(trait); traits.Add(data); }
                Set(card, "traits", traits);
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(card);
                CardState state = cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false)!;
                int id = FullBattleTrace.Active!.CardId(state); cards.MoveCardFromHandToTopOfDeck(state); return id;
            }
            ordinary = Add(1, null); frozen = Add(2, "CardTraitFreeze");
            persistent = Add(3, "CardTraitPersistent"); infusion = Add(4, "CardTraitInfusion");
            finisher = FullBattleTrace.Active!.CardId(cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false)!);
            managers.GetCardStatistics().GetStatValue(new CardStatistics.StatValueData { trackedValue = CardStatistics.TrackedValueType.TimesPlayed,
                cardState = cards.GetHand()[0], entryDuration = CardStatistics.EntryDuration.ThisBattle, cardTypeTarget = CardStatistics.CardTypeTarget.Any });
            FullBattleTrace.Active.InvalidateCardRules(); Prepared = true;
            ObserveRejection("deployment");
            Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("DEVOTE-PREPARED one finisher, persistent/infusion exclusions, frozen/ordinary random discards.");
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (!deployed) { deployed = true; return new PlayCardAction(finisher, 2, playerPosition: 0); }
            if (state.Spawn.Turn < 1 || state.Spawn.Turn > 5) return null;
            string gate = Call("GetDevoteError", Managers.GetCoreManagers()).ToString();
            if (gate != "None") { ObserveRejection("turn-" + state.Spawn.Turn); return null; }
            if (devotes == 0)
            {
                // Invalid native invocations must leave complete state and RNG unchanged.
                var player = Managers.GetPlayerManager(); int energy = player.GetEnergy();
                player.RemoveEnergy(energy);
                ObserveRejection("zero-energy");
                player.AddEnergy(energy);
            }
            devotes++;
            return new PlayCardAction(0, state.Spawn.Turn % 3, operation: DevoteModel.Operation);
        }
        private static void ObserveRejection(string label)
        {
            string gate = Call("GetDevoteError", Managers.GetCoreManagers()).ToString();
            TrainCombatState before = FullBattleTrace.Active!.CaptureTrain(); object rngBefore = HalfHealthLifecycleCalibration.Rng();
            var operation = (IEnumerator)Call("Devote", Managers.GetCoreManagers());
            bool yielded;
            try { yielded = operation.MoveNext(); }
            finally { (operation as IDisposable)?.Dispose(); }
            TrainCombatState after = FullBattleTrace.Active.CaptureTrain(); object rngAfter = HalfHealthLifecycleCalibration.Rng();
            string? difference = HalfHealthLifecycleCalibration.Difference(before, after) ??
                HalfHealthLifecycleCalibration.Difference(rngBefore, rngAfter);
            gates.Add(new { Label = label, Error = gate, Yielded = yielded, Before = before, After = after,
                RngBefore = rngBefore, RngAfter = rngAfter, Difference = difference });
            if (gate == "None" || yielded || difference != null) throw new InvalidOperationException("Rejected Devote mutated native state: " + label);
        }
        internal static void Start(ManualLogSource log)
        {
            Running = true; recording = true; notifications.Clear();
            Managers.GetSaveManager().StartCoroutine(Run(log));
        }
        private static IEnumerator Run(ManualLogSource log)
        {
            var stack = new Stack<IEnumerator>(); stack.Push((IEnumerator)Call("Devote", Managers.GetCoreManagers()));
            try
            {
                while (stack.Count > 0)
                {
                    IEnumerator item = stack.Peek(); bool next;
                    try { next = item.MoveNext(); }
                    catch (Exception error) { Error = error.ToString(); log.LogError(Error); break; }
                    if (!next) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                    if (item.Current is IEnumerator child) stack.Push(child); else yield return item.Current;
                }
                observations.Add(new { Phase = "Completed", Number = devotes, Notifications = notifications.ToArray(),
                    Hand = Managers.GetCardManager().GetHand().Select(FullBattleTrace.Active!.CardId).ToArray(),
                    Energy = Managers.GetPlayerManager().GetEnergy(), Devotion = DevotionProbe.Capture(Managers) });
            }
            finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); Running = false; recording = false; }
        }
        [HarmonyPatch(typeof(CardManager), "DiscardCard")]
        private static class DiscardPatch
        {
            // The room override field is new in 2.3.1. Older Mono must never
            // compile this prefix, even when its first branch would return.
            private static bool Prepare() => Enabled;
            private static void Prefix(CardManager.DiscardCardParams discardCardParams)
            {
                if (!recording || !Enabled) return;
                observations.Add(new { Phase = "Discard", Number = devotes, Card = FullBattleTrace.Active!.CardId(discardCardParams.discardCard),
                    discardCardParams.wasPlayed, discardCardParams.triggeredByCard, discardCardParams.handDiscarded,
                    Room = discardCardParams.roomIndexOverride, Energy = Managers.GetPlayerManager().GetEnergy(), Devotion = DevotionProbe.Capture(Managers) });
            }
        }
        [HarmonyPatch(typeof(CardManager), "OnDevotionLevelChanged")]
        private static class CardPatch
        {
            private static bool Prepare() => ConditionalActivationProbe.HasVanguard;
            private static void Prefix() { if (recording) notifications.Add("Cards"); }
        }
        [HarmonyPatch(typeof(MonsterManager), "OnDevotionLevelChanged")]
        private static class MonsterPatch
        {
            private static bool Prepare() => ConditionalActivationProbe.HasVanguard;
            private static void Prefix() { if (recording) notifications.Add("Monsters"); }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
