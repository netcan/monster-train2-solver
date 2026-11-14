using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DamageReactionBattleScenario
    {
        internal const string Name = "damage-reaction-battle";
        internal const string OwnerId = "5253721f-c51c-4148-9958-d45550f08316";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        private static int[] cards = Array.Empty<int>();
        private static int step;
        private static ManualLogSource? logger;
        private static string? rejection;
        private static readonly List<Transition> transitions = new List<Transition>();
        private static readonly List<RelicApplication> applications = new List<RelicApplication>();
        private static string[] piercingEffectTypes = Array.Empty<string>(), ownerRuntimeEffectTypes = Array.Empty<string>();
        internal static object Evidence => new { Prepared, OwnerId, ControlCardIds = cards, CompletedControlPlays = step,
            Transitions = transitions, Applications = applications, PiercingEffectTypes = piercingEffectTypes, OwnerRuntimeEffectTypes = ownerRuntimeEffectTypes };
        private sealed class RelicApplication
        {
            public bool Preview { get; set; }
            public CombatUnit[] Before { get; set; } = Array.Empty<CombatUnit>();
            public CombatUnit[] After { get; set; } = Array.Empty<CombatUnit>();
            public bool Completed { get; set; }
        }
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
            var save = managers.GetSaveManager(); var data = save.GetAllGameData(); var manager = managers.GetCardManager();
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0 ||
                save.GetCollectedRelics().Any(owner => owner.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("Damage reaction battle requires isolated 2.3.1 deployment acquisition.");
            RelicData owner = data.GetAllCollectableRelicData().Single(relic => relic.GetID() == OwnerId);
            RelicEffectData rule = owner.GetEffects().Single();
            if (owner.name != "DamageFriendlyOnSpawn" || owner.DisallowInDeploymentPhase ||
                rule.GetEffectClassName() != "RelicEffectDamageEnemyOnFriendlyTrigger" ||
                rule.GetParamTrigger().ToString() != "OnPunish" || rule.GetTargetMode() != TargetMode.FrontInRoom ||
                rule.GetParamSourceTeam() != Team.Type.Monsters || rule.GetParamInt() != 5 ||
                !rule.GetParamCharacterSubtype().IsNone || rule.GetParamStatusEffects().Length != 0 || rule.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original Punish retaliation relic changed.");
            CardData steward = data.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678");
            CharacterData unit = steward.GetSpawnCharacterData();
            Set(unit, "health", 12); Set(unit, "size", 1);
            var triggers = unit.GetTriggers().ToList();
            foreach (var entry in new[] { ("OnBloodshed", 5), ("OnHit", 10), ("OnPunish", 15), ("OnReanimated", 20), ("OnDeath", 25) })
            {
                CharacterTriggerData trigger = HealingScenario.HealGold(entry.Item2, false, false);
                Set(trigger, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), entry.Item1)); triggers.Add(trigger);
            }
            CharacterTriggerData begin = HealingScenario.HealGold(0, false, false);
            Set(begin, "trigger", CharacterTriggerData.Trigger.OnTurnBegin);
            Set(begin, "effects", new List<CardEffectData> { Effect("CardEffectDamage", 6, TargetMode.Self) });
            triggers.Add(begin); Set(unit, "triggers", triggers);
            Set(unit, "startingStatusEffects", unit.GetStartingStatusEffects().Concat(new[] {
                new StatusEffectStackData { statusId = "undying", count = 1 } }).ToArray());
            var owned = new HashSet<string>(manager.GetAllCards(new List<CardState>()).Select(card => card.GetCardDataID()));
            CardData[] spells = data.GetAllCardData().Where(card => card.GetCardType() == CardType.Spell && !owned.Contains(card.GetID()) &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDamage")).Take(3).ToArray();
            if (spells.Length != 3) throw new InvalidOperationException("Missing unowned damage/heal controls.");
            for (int index = 0; index < spells.Length; index++)
            {
                Set(spells[index], "effects", new List<CardEffectData> { Effect(index == 1 ? "CardEffectHeal" : "CardEffectDamage",
                    index == 0 ? 7 : index == 1 ? 8 : 1, TargetMode.DropTargetCharacter) });
                Set(spells[index], "traits", new List<CardTraitData>()); Set(spells[index], "cost", 0);
            }
            foreach (CardState card in manager.GetHand().ToArray()) manager.MoveCardFromHandToTopOfDeck(card);
            save.AddRelic(owner);
            piercingEffectTypes = data.GetBalanceData().GetRelicEffectStateNamesThatDoPiercingDamage().ToArray();
            ownerRuntimeEffectTypes = save.GetCollectedRelics().Single(relic => relic.GetRelicDataID() == OwnerId)
                .GetEffects().Select(effect => effect.GetType().Name).ToArray();
            CardData[] sequence = { steward, steward, spells[0], spells[1], spells[2], spells[0], spells[1] };
            CardState[] controls = sequence.Select(card => manager.AddNewCard(card, CardPile.HandPile, false, false, animate: false)).ToArray();
            if (controls.Any(card => card == null || !manager.GetHand().Contains(card))) throw new InvalidOperationException("Damage reaction controls missing.");
            cards = controls.Select(FullBattleTrace.Active!.CardId).ToArray(); logger = log; Prepared = true;
            Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("DAMAGE-REACTION-BATTLE-PREPARED original Punish retaliation owner; two paid same-room 12-HP Undying carriers, five wound/heal controls and natural self-damage turns.");
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (step >= cards.Length) return BattleActionModel.ChooseUnitSpellAndJunkPlay(state);
            int target = step < 2 ? 0 : state.Spawn.Train.Rooms[0].Units.FirstOrDefault(unit =>
                unit.SpawnerCardId == cards[step >= 5 ? 1 : 0])?.Id ?? 0;
            var action = new PlayCardAction(cards[step], 0, playerPosition: step < 2 ? 0 : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(state, action);
            if (!result.Supported)
            {
                if (rejection != result.Reason) { rejection = result.Reason; logger?.LogInfo("DAMAGE-REACTION-BATTLE-REJECT " + step + ": " + rejection); }
                return null;
            }
            step++; return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action) => cards.Contains(action.CardInstanceId);
        private static CardEffectData Effect(string type, int amount, TargetMode target)
        {
            var effect = new CardEffectData(type, null!, Team.Type.Monsters); effect.Cheat_SetTargetMode(target);
            Set(effect, "paramInt", amount); return effect;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
        [HarmonyPatch(typeof(RelicEffectDamageEnemyOnFriendlyTrigger), nameof(RelicEffectDamageEnemyOnFriendlyTrigger.ApplyCharacterTriggerEffect))]
        private static class RelicPatch
        {
            private static void Postfix(RelicEffectDamageEnemyOnFriendlyTrigger __instance, ref IEnumerator __result)
            {
                if (Enabled && Prepared && __instance.SourceRelicState.GetRelicDataID() == OwnerId)
                    __result = Observe(__result, __instance);
            }
            private static IEnumerator Observe(IEnumerator native, RelicEffectDamageEnemyOnFriendlyTrigger effect)
            {
                var trace = FullBattleTrace.Active!;
                CharacterState[] targets = ((List<CharacterState>)AccessTools.Field(effect.GetType(), "_targets").GetValue(effect)).ToArray();
                var row = new RelicApplication { Preview = AllGameManagers.Instance!.GetSaveManager().PreviewMode,
                    Before = targets.Select(target => trace.CaptureUnit(target)).ToArray() };
                applications.Add(row);
                try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
                finally { (native as IDisposable)?.Dispose(); row.After = targets.Select(target => trace.CaptureUnit(target)).ToArray(); }
            }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class ObservePatch
        {
            private static void Postfix(CharacterState __instance, CharacterTriggerData.Trigger trigger, CharacterState dyingCharacter,
                bool fromRunningTriggerQueue, CharacterState.FireTriggersData fireTriggersData, int triggerCount, CharacterTriggerState exclusiveTrigger,
                ref IEnumerator __result)
            {
                if (!Enabled || !Prepared || !fromRunningTriggerQueue || trigger.ToString() is not
                    ("OnBloodshed" or "OnPunish" or "OnHit" or "OnReanimated" or "OnDeath")) return;
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
