using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    // Authored calibration on real paid cards. Definitions change only in the
    // private worker; plays, costs, triggers and later turns remain native.
    internal static class HalfHealthBattleScenario
    {
        internal const string Name = "half-health-battle";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        private static int[] cards = Array.Empty<int>();
        private static int step;
        private static ManualLogSource? logger;
        private static string? rejection;
        internal static object Evidence => new { Prepared, ControlCardIds = cards, CompletedControlPlays = step, Transitions = transitions };
        private static readonly List<Transition> transitions = new List<Transition>();
        private sealed class Transition
        {
            public int Turn { get; set; }
            public bool Preview { get; set; }
            public CharacterTriggerDispatch Dispatch { get; set; } = null!;
            public CombatUnit Before { get; set; } = null!;
            public CombatUnit? After { get; set; }
            public int GoldBefore { get; set; }
            public int GoldAfter { get; set; }
            public bool Completed { get; set; }
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (!ConditionalActivationProbe.HasVanguard || managers.GetSaveManager().PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Half-health battle requires real 2.3.1 deployment.");
            var save = managers.GetSaveManager(); var data = save.GetAllGameData(); var manager = managers.GetCardManager();
            CardData steward = data.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678");
            CharacterData unit = steward.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            Set(unit, "health", 12);
            var triggers = unit.GetTriggers().ToList();
            triggers.Add(Gold("OnDeathwish", 5)); triggers.Add(Gold("OnDeathwishLost", 7));
            triggers.Add(Gold("OnReanimated", 11)); triggers.Add(Gold("OnDeath", 13));
            var begin = Gold("OnTurnBegin", 0);
            Set(begin, "effects", new List<CardEffectData> { Effect("CardEffectHeal", 8, TargetMode.Self), Effect("CardEffectDamage", 6, TargetMode.Self) });
            triggers.Add(begin); Set(unit, "triggers", triggers);
            var statuses = unit.GetStartingStatusEffects().ToList();
            statuses.Add(new StatusEffectStackData { statusId = "undying", count = 1 });
            Set(unit, "startingStatusEffects", statuses.ToArray());
            var owned = new HashSet<string>(manager.GetAllCards(new List<CardState>()).Select(card => card.GetCardDataID()));
            CardData wound = data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell && !owned.Contains(card.GetID()) &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDamage"));
            Set(wound, "effects", new List<CardEffectData> { Effect("CardEffectDamage", 7, TargetMode.DropTargetCharacter),
                Effect("CardEffectHeal", 8, TargetMode.LastTargetedCharacters) });
            Set(wound, "traits", new List<CardTraitData>()); Set(wound, "cost", 0);
            foreach (CardState card in manager.GetHand().ToArray()) manager.MoveCardFromHandToTopOfDeck(card);
            var controls = Enumerable.Range(0, 2).Select(_ => manager.AddNewCard(steward, CardPile.HandPile, false, false, animate: false))
                .Concat(Enumerable.Range(0, 3).Select(_ => manager.AddNewCard(wound, CardPile.HandPile, false, false, animate: false))).ToArray();
            if (controls.Any(card => card == null || !manager.GetHand().Contains(card))) throw new InvalidOperationException("Half-health control cards missing.");
            cards = controls.Select(FullBattleTrace.Active!.CardId).ToArray(); logger = log; Prepared = true;
            Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("HALF-HEALTH-BATTLE-PREPARED two paid 12-HP Undying Stewards; gain/lost gold with nested damage/heal, heal/damage turn trigger, three friendly damage/heal spells.");
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (step >= cards.Length) return BattleActionModel.ChooseUnitSpellAndJunkPlay(state);
            int room = step == 1 || step == 3 ? 1 : 0;
            int target = step < 2 ? 0 : state.Spawn.Train.Rooms[room].Units.FirstOrDefault(unit =>
                unit.SpawnerCardId == cards[step == 3 ? 1 : 0])?.Id ?? 0;
            var action = new PlayCardAction(cards[step], room, playerPosition: step < 2 ? 0 : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(state, action);
            if (!result.Supported)
            {
                if (rejection != result.Reason) { rejection = result.Reason; logger?.LogInfo("HALF-HEALTH-BATTLE-REJECT " + step + ": " + rejection); }
                return null;
            }
            step++; return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action) => cards.Contains(action.CardInstanceId);
        private static CharacterTriggerData Gold(string kind, int gold)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(gold, false, false);
            Set(trigger, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), kind));
            if (kind == "OnDeathwish" || kind == "OnDeathwishLost")
                // A larger wound also settles when a stale loss callback heals
                // across the threshold before an already queued gain callback.
                Set(trigger, "effects", trigger.GetEffects().Concat(new[] { Effect(kind == "OnDeathwish" ? "CardEffectDamage" : "CardEffectHeal", kind == "OnDeathwish" ? 2 : 1, TargetMode.Self) }).ToList());
            return trigger;
        }
        private static CardEffectData Effect(string kind, int amount, TargetMode mode)
        {
            var effect = new CardEffectData(kind, null!, Team.Type.Monsters); effect.Cheat_SetTargetMode(mode);
            Set(effect, "paramInt", amount); return effect;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class HalfPatch
        {
            private static void Postfix(CharacterState __instance, CharacterTriggerData.Trigger trigger, CharacterState dyingCharacter,
                bool fromRunningTriggerQueue, CharacterState.FireTriggersData fireTriggersData, int triggerCount, CharacterTriggerState exclusiveTrigger,
                ref IEnumerator __result)
            {
                if (!Enabled || !Prepared || !fromRunningTriggerQueue || trigger.ToString() is not ("OnDeathwish" or "OnDeathwishLost")) return;
                __result = Observe(__result, __instance, trigger, dyingCharacter, fireTriggersData, triggerCount, exclusiveTrigger);
            }
            private static IEnumerator Observe(IEnumerator native, CharacterState actor, CharacterTriggerData.Trigger kind,
                CharacterState dying, CharacterState.FireTriggersData parameters, int count, CharacterTriggerState exclusive)
            {
                var managers = AllGameManagers.Instance!; var trace = FullBattleTrace.Active!;
                var row = new Transition { Turn = managers.GetCombatManager().GetTurnCount(), Preview = managers.GetSaveManager().PreviewMode,
                    Before = trace.CaptureUnit(actor), GoldBefore = managers.GetSaveManager().GetGold(),
                    Dispatch = new CharacterTriggerDispatch(trace.UnitId(actor), kind.ToString(), dying == null ? 0 : trace.UnitId(dying),
                        count, parameters?.paramInt ?? 0, parameters?.paramInt2 ?? 0, parameters?.paramString,
                        parameters?.overrideTargetCharacter == null ? 0 : trace.UnitId(parameters.overrideTargetCharacter),
                        exclusiveTriggerStateId: exclusive == null ? (int?)null : trace.TriggerStateId(actor, exclusive)) };
                transitions.Add(row);
                try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
                finally { (native as IDisposable)?.Dispose(); row.After = trace.CaptureUnit(actor); row.GoldAfter = managers.GetSaveManager().GetGold(); }
            }
        }
    }
}
