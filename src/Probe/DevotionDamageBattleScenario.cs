using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DevotionDamageBattleScenario
    {
        internal const string Name = "devotion-damage-battle";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        private static int[] cards = Array.Empty<int>();
        private static int step;
        private static ManualLogSource? logger;
        private static string? rejection;
        private static object? ownerDefinition, venerateDefinition;
        private static readonly List<object> transitions = new List<object>();
        private static readonly List<object> notifications = new List<object>();
        internal static object Evidence => new { Prepared, ControlCardIds = cards, CompletedControlPlays = step,
            OwnerDefinition = ownerDefinition, VenerateDefinition = venerateDefinition, Transitions = transitions, Notifications = notifications };

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var data = save.GetAllGameData(); var manager = managers.GetCardManager();
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0 ||
                save.GetCollectedRelics().Any(owner => owner.GetRelicDataID() == RelicDevotionOnDamageModel.Id) ||
                DevotionProbe.Read(save, "GetDevotionLevel") != 0 || DevotionProbe.Read(save, "GetDevotionPoints") != 0)
                throw new InvalidOperationException("Devotion damage battle requires isolated 2.3.1 deployment acquisition at zero Devotion.");
            RelicData owner = data.GetAllCollectableRelicData().Single(relic => relic.GetID() == RelicDevotionOnDamageModel.Id);
            RelicEffectData relicEffect = owner.GetEffects().Single();
            ownerDefinition = new { Id = owner.GetID(), Key = owner.name, Placement = owner.DisallowInDeploymentPhase,
                Type = relicEffect.GetEffectClassName(), Int = relicEffect.GetParamInt(), Int2 = relicEffect.GetParamInt2(),
                Trigger = relicEffect.GetParamTrigger().ToString(), Team = relicEffect.GetParamSourceTeam().ToString(),
                Conditions = relicEffect.GetEffectConditions().Count };
            if (owner.name != RelicDevotionOnDamageModel.Key || owner.DisallowInDeploymentPhase ||
                relicEffect.GetEffectClassName() != RelicDevotionOnDamageModel.Effect || relicEffect.GetParamInt() != 1 ||
                relicEffect.GetParamInt2() != 10 || relicEffect.GetEffectConditions().Count != 1)
                throw new InvalidOperationException("Original Devotion damage owner changed.");
            CardData venerate = data.FindCardData("96c9ed37-5e59-4c4f-a914-ae7040d24755");
            CardEffectData gain = venerate.GetEffects().Single();
            venerateDefinition = new { Id = venerate.GetID(), Key = venerate.name, Cost = venerate.GetCost(),
                Type = gain.GetEffectStateName(), Int = gain.GetParamInt(), PerLevel = gain.GetAdditionalParamInt(),
                Traits = venerate.GetTraits().Select(trait => trait.GetTraitStateName()).ToArray() };
            if (venerate.name != "Venerate" || venerate.GetCost() != 3 || gain.GetEffectStateName() != "CardEffectGainDevotionPoints" ||
                gain.GetParamInt() != 6 || gain.GetAdditionalParamInt() != 0 ||
                !venerate.GetTraits().Select(trait => trait.GetTraitStateName()).SequenceEqual(new[] { "CardTraitExhaustState" }))
                throw new InvalidOperationException("Original Venerate changed.");
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
            CardData[] spells = new[] { 3, 6, 1, 8, 25, 4, 10 }.Select((amount, index) => {
                CardData spell = UnityEngine.Object.Instantiate(template); spell.name = "PojuDevotionDamageControl" + index;
                Set(spell, "id", "fc231d20-0000-4000-8000-00000000000" + (index + 1)); Set(spell, "cost", 0);
                Set(spell, "effects", new List<CardEffectData> { Effect(index == 3 ? "CardEffectHeal" : "CardEffectDamage", amount, TargetMode.DropTargetCharacter) });
                Set(spell, "traits", new List<CardTraitData>()); Set(spell, "triggers", new List<CardTriggerEffectData>());
                Set(spell, "effectTriggers", new List<CharacterTriggerData>()); Set(spell, "startingUpgrades", new List<CardUpgradeData>());
                Set(spell, "targetless", false); Set(spell, "targetsRoom", true); Set(spell, "costType", CardData.CostType.Default);
                Set(spell, "isUnitAbility", false); Set(spell, "isRoomAbility", false);
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(spell);
                return spell;
            }).ToArray();
            foreach (CardState card in manager.GetHand().ToArray()) manager.MoveCardFromHandToTopOfDeck(card);
            Set(data.GetBalanceData(), "maxHandSize", 20);
            save.AddRelic(owner);
            CardData[] sequence = { steward, steward, steward, spells[0], spells[1], spells[2], spells[2], spells[3], spells[4], spells[5], spells[6], venerate };
            cards = sequence.Select(card => {
                CardState added = manager.AddNewCard(card, CardPile.HandPile, false, false, animate: false);
                if (added == null) throw new InvalidOperationException("Unable to add native Devotion control " + card.name);
                return FullBattleTrace.Active!.CardId(added);
            }).ToArray();
            FullBattleTrace.Active!.InvalidateCardRules(); logger = log; Prepared = true;
            Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("DEVOTION-DAMAGE-PREPARED unchanged owner/Venerate; 9/10/11, 25-damage single award, heal, 36/40/50 and natural turn carry.");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (step >= cards.Length) return null;
            int target = step < 3 || step == cards.Length - 1 ? 0 : state.Spawn.Train.Rooms[2].Units.FirstOrDefault(unit =>
                unit.SpawnerCardId == cards[step is 4 or 6 or 9 ? 1 : 0])?.Id ?? 0;
            var action = new PlayCardAction(cards[step], 2, playerPosition: step < 3 ? 0 : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(state, action);
            if (!result.Supported)
            {
                if (rejection != result.Reason) { rejection = result.Reason; logger?.LogInfo("DEVOTION-DAMAGE-REJECT " + step + ": " + rejection); }
                return null;
            }
            step++; return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action) => cards.Contains(action.CardInstanceId);
        private static CardEffectData Effect(string type, int amount, TargetMode target)
        { var effect = new CardEffectData(type, null!, Team.Type.Monsters); effect.Cheat_SetTargetMode(target); Set(effect, "paramInt", amount); return effect; }
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
                int turn = managers.GetCombatManager().GetTurnCount(); bool preview = managers.GetSaveManager().PreviewMode;
                CombatUnit before = trace.CaptureUnit(actor); DevotionState devotionBefore = DevotionProbe.Capture(managers)!;
                bool completed = false;
                try { while (native.MoveNext()) yield return native.Current; completed = true; }
                finally { (native as IDisposable)?.Dispose(); transitions.Add(new { Turn = turn, Preview = preview, Kind = kind.ToString(),
                    Before = before, After = trace.CaptureUnit(actor), DevotionBefore = devotionBefore,
                    DevotionAfter = DevotionProbe.Capture(managers), Completed = completed }); }
            }
        }
        private static void Notify(string kind)
        {
            if (Enabled && Prepared) notifications.Add(new { Kind = kind, Turn = AllGameManagers.Instance!.GetCombatManager().GetTurnCount(),
                Preview = AllGameManagers.Instance.GetSaveManager().PreviewMode, Devotion = DevotionProbe.Capture(AllGameManagers.Instance) });
        }
        [HarmonyPatch(typeof(CardManager), "OnDevotionLevelChanged")]
        private static class CardPatch
        {
            private static bool Prepare() => ConditionalActivationProbe.HasVanguard;
            private static void Prefix() => Notify("Cards");
        }
        [HarmonyPatch(typeof(MonsterManager), "OnDevotionLevelChanged")]
        private static class MonsterPatch
        {
            private static bool Prepare() => ConditionalActivationProbe.HasVanguard;
            private static void Prefix() => Notify("Monsters");
        }
    }
}
