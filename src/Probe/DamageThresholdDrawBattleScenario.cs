using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DamageThresholdDrawBattleScenario
    {
        internal const string Name = "damage-threshold-draw-battle";
        internal const string RevengeId = "2adea00a-87e9-4a45-9822-de04104ae108";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        private static int[] cards = Array.Empty<int>();
        private static int step;
        private static ManualLogSource? logger;
        private static string? rejection;
        private static object[] definitions = Array.Empty<object>();
        private static readonly List<Transition> transitions = new List<Transition>();
        internal static object Evidence => new { Prepared, ControlCardIds = cards, CompletedControlPlays = step,
            OwnerDefinitions = definitions, Transitions = transitions };
        private sealed class Transition
        {
            public int Turn { get; set; }
            public bool Preview { get; set; }
            public string Kind { get; set; } = "";
            public CombatUnit Before { get; set; } = null!;
            public CombatUnit? After { get; set; }
            public int GoldBefore { get; set; }
            public int GoldAfter { get; set; }
            public bool Completed { get; set; }
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var data = save.GetAllGameData(); var manager = managers.GetCardManager();
            string[] ids = { RelicCardDrawOnHealModel.DamageThresholdId, RevengeId };
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0 ||
                save.GetCollectedRelics().Any(owner => ids.Contains(owner.GetRelicDataID())))
                throw new InvalidOperationException("Damage-threshold battle requires isolated 2.3.1 deployment acquisition.");
            RelicData[] owners = ids.Select(id => data.GetAllCollectableRelicData().Single(relic => relic.GetID() == id)).ToArray();
            definitions = owners.Select(owner => {
                RelicEffectData effect = owner.GetEffects().Single();
                return (object)new { Id = owner.GetID(), Key = owner.name, Placement = owner.DisallowInDeploymentPhase,
                    Type = effect.GetEffectClassName(), Int = effect.GetParamInt(), Int2 = effect.GetParamInt2(),
                    AllTriggers = effect.GetParamBool(), Heroes = effect.GetParamBool2(), Trigger = effect.GetParamTrigger().ToString(),
                    Team = effect.GetParamSourceTeam().ToString(), Conditions = effect.GetEffectConditions().Count,
                    Excluded = effect.GetCardTriggers().Select(trigger => trigger.ToString()).ToArray() };
            }).ToArray();
            if (owners[0].name != RelicCardDrawOnHealModel.DamageThresholdKey || owners[0].DisallowInDeploymentPhase ||
                owners[0].GetEffects()[0].GetEffectClassName() != RelicCardDrawOnHealModel.DamageThresholdEffect ||
                owners[1].name != "RevengeTriggerTwice" || owners[1].DisallowInDeploymentPhase ||
                owners[1].GetEffects()[0].GetEffectClassName() != "RelicEffectModifyTriggerCount" ||
                owners[1].GetEffects()[0].GetParamTrigger() != CharacterTriggerData.Trigger.OnHit ||
                owners[1].GetEffects()[0].GetParamInt() != 1 || owners[1].GetEffects()[0].GetParamBool() || owners[1].GetEffects()[0].GetParamBool2())
                throw new InvalidOperationException("Original damage-threshold/revenge owners changed.");
            CardData steward = data.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678");
            CharacterData unit = steward.GetSpawnCharacterData();
            Set(unit, "health", 100); Set(unit, "attackDamage", 60); Set(unit, "size", 1); Set(steward, "cost", 0);
            var hit = HealingScenario.HealGold(1, false, false); Set(hit, "trigger", CharacterTriggerData.Trigger.OnHit);
            var heal = HealingScenario.HealGold(1, false, false);
            var begin = HealingScenario.HealGold(0, false, false); Set(begin, "trigger", CharacterTriggerData.Trigger.OnTurnBegin);
            Set(begin, "effects", new List<CardEffectData> { Effect("CardEffectDamage", 6, TargetMode.Self) });
            Set(unit, "triggers", new List<CharacterTriggerData> { hit, heal, begin });
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>()); Set(unit, "unitAbility", null!);
            CardData template = data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDamage"));
            CardData[] spells = new int[] { 3, 6, 1, 8, 10 }.Select((amount, index) => {
                CardData spell = UnityEngine.Object.Instantiate(template); spell.name = "PojuDamageThresholdControl" + index;
                Set(spell, "id", "fc231d10-0000-4000-8000-00000000000" + (index + 1)); Set(spell, "cost", 0);
                Set(spell, "effects", new List<CardEffectData> { Effect(index == 3 ? "CardEffectHeal" : "CardEffectDamage", amount, TargetMode.DropTargetCharacter) });
                Set(spell, "traits", new List<CardTraitData>()); Set(spell, "triggers", new List<CardTriggerEffectData>());
                Set(spell, "effectTriggers", new List<CharacterTriggerData>()); Set(spell, "startingUpgrades", new List<CardUpgradeData>());
                Set(spell, "targetless", false); Set(spell, "targetsRoom", true); Set(spell, "costType", CardData.CostType.Default);
                Set(spell, "isUnitAbility", false); Set(spell, "isRoomAbility", false);
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(spell);
                return spell;
            }).ToArray();
            foreach (CardState card in manager.GetHand().ToArray()) manager.MoveCardFromHandToTopOfDeck(card);
            foreach (RelicData owner in owners) save.AddRelic(owner);
            CardData[] sequence = { steward, steward, steward, spells[0], spells[1], spells[2], spells[2], spells[3], spells[4] };
            cards = sequence.Select(card => FullBattleTrace.Active!.CardId(manager.AddNewCard(card, CardPile.HandPile, false, false, animate: false))).ToArray();
            FullBattleTrace.Active!.InvalidateCardRules(); logger = log; Prepared = true;
            Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("DAMAGE-THRESHOLD-DRAW-PREPARED two unchanged new relics; three carriers, shared 9/10/11 damage, heal/recross and natural turn resets.");
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (step >= cards.Length) return null;
            int target = step < 3 ? 0 : state.Spawn.Train.Rooms[2].Units.FirstOrDefault(unit =>
                unit.SpawnerCardId == cards[step is 4 or 6 ? 1 : 0])?.Id ?? 0;
            var action = new PlayCardAction(cards[step], 2, playerPosition: step < 3 ? 0 : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(state, action);
            if (!result.Supported)
            {
                if (rejection != result.Reason) { rejection = result.Reason; logger?.LogInfo("DAMAGE-THRESHOLD-DRAW-REJECT " + step + ": " + rejection); }
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
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class TriggerPatch
        {
            private static void Postfix(CharacterState __instance, CharacterTriggerData.Trigger trigger, bool fromRunningTriggerQueue, ref IEnumerator __result)
            {
                if (Enabled && Prepared && fromRunningTriggerQueue && trigger is CharacterTriggerData.Trigger.OnHit or CharacterTriggerData.Trigger.OnHeal)
                    __result = Observe(__result, __instance, trigger);
            }
            private static IEnumerator Observe(IEnumerator native, CharacterState actor, CharacterTriggerData.Trigger kind)
            {
                var managers = AllGameManagers.Instance!; var trace = FullBattleTrace.Active!;
                var row = new Transition { Turn = managers.GetCombatManager().GetTurnCount(), Preview = managers.GetSaveManager().PreviewMode,
                    Kind = kind.ToString(), Before = trace.CaptureUnit(actor), GoldBefore = managers.GetSaveManager().GetGold() };
                transitions.Add(row);
                try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
                finally { (native as IDisposable)?.Dispose(); row.After = trace.CaptureUnit(actor); row.GoldAfter = managers.GetSaveManager().GetGold(); }
            }
        }
    }
}
