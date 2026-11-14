using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class LethalMaximumBattleScenario
    {
        internal const string Prefix = "lethal-maximum-battle-";
        internal static string Mode => (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? "").Substring(Prefix.Length);
        internal static bool Enabled => (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? "").StartsWith(Prefix, StringComparison.Ordinal);
        internal static bool Prepared { get; private set; }
        private static int[] cards = Array.Empty<int>();
        private static int step;
        private static readonly List<object> callbacks = new List<object>();
        internal static object Evidence => new { Prepared, Mode, ControlCardIds = cards, CompletedControlPlays = step, Callbacks = callbacks };
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);
        private static CardEffectData Effect(string type, int amount)
        {
            var effect = new CardEffectData(type, null!, Team.Type.Monsters); effect.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            Set(effect, "paramInt", amount);
            if (type == "CardEffectBuffMaxHealth") Set(effect, "additionalParamInt1", (int)UnitUpgradeLifetimeTempOnly.TemporaryUntilEndOfBattle);
            return effect;
        }
        private static CharacterTriggerData Gold(string kind, int amount)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(amount, false, false);
            Set(trigger, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), kind)); return trigger;
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (!ConditionalActivationProbe.HasVanguard || managers.GetSaveManager().PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Lethal maximum-health controls require isolated 2.3.1 deployment.");
            var all = managers.GetAllGameData(); var manager = managers.GetCardManager();
            bool horde = Mode.Contains("horde"), undying = Mode.Contains("undying"), zero = Mode.EndsWith("-zero", StringComparison.Ordinal);
            CardData observerCard = all.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678");
            CharacterData observer = observerCard.GetSpawnCharacterData();
            Set(observerCard, "cost", 0); Set(observer, "health", 100); Set(observer, "attackDamage", 60); Set(observer, "size", 1);
            Set(observer, "triggers", new List<CharacterTriggerData> { Gold("OnAnyUnitDeathOnFloor", 2) });
            Set(observer, "startingStatusEffects", Array.Empty<StatusEffectStackData>()); Set(observer, "unitAbility", null!);
            var victimCard = UnityEngine.Object.Instantiate(observerCard); victimCard.name = "PojuLethalMaximumVictim";
            Set(victimCard, "id", "fc231b50-0000-4000-8000-000000000001");
            var victim = UnityEngine.Object.Instantiate(observer); victim.name = "PojuLethalMaximumVictimUnit";
            Set(victim, "id", "fc231b51-0000-4000-8000-000000000001"); Set(victim, "health", 10); Set(victim, "attackDamage", 3);
            Set(victim, "triggers", new List<CharacterTriggerData> { Gold("OnDeathwish", 5), Gold("OnDeathwishLost", 7),
                Gold("OnReanimated", 11), Gold("OnDeath", 13) });
            var statuses = new List<StatusEffectStackData>();
            if (horde) statuses.Add(new StatusEffectStackData { statusId = "horde", count = 2 });
            if (undying) statuses.Add(new StatusEffectStackData { statusId = "undying", count = 1 });
            Set(victim, "startingStatusEffects", statuses.ToArray()); Set(victimCard.GetEffects().Single(), "paramCharacterData", victim);
            ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all)).Add(victim);
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(victimCard);
            CardData Spell(int index, params CardEffectData[] effects)
            {
                var card = UnityEngine.Object.Instantiate(all.FindCardData(DevotionSpawnBattleScenario.VenerateId)); card.name = "PojuLethalMaximumInput" + index;
                Set(card, "id", "fc231b52-0000-4000-8000-00000000000" + index); Set(card, "cost", 0);
                Set(card, "effects", effects.ToList()); Set(card, "traits", new List<CardTraitData>());
                Set(card, "triggers", new List<CardTriggerEffectData>()); Set(card, "effectTriggers", new List<CharacterTriggerData>());
                Set(card, "startingUpgrades", new List<CardUpgradeData>()); Set(card, "targetless", false); Set(card, "targetsRoom", true);
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(card); return card;
            }
            CardData wound = horde ? Spell(1, Effect("CardEffectBuffMaxHealth", 20), Effect("CardEffectDamage", 21)) :
                Spell(1, Effect("CardEffectDamage", 6));
            CardData kill = Spell(2, Effect("CardEffectDebuffMaxHealth", zero ? 99999 : horde ? 20 : 5));
            CardData after = Spell(3, Effect("CardEffectBuffMaxHealth", 2));
            foreach (CardState card in manager.GetHand().ToArray()) manager.MoveCardFromHandToTopOfDeck(card);
            manager.GetDrawPile().Clear();
            cards = new[] { observerCard, victimCard, wound, kill, after }.Select(data => FullBattleTrace.Active!.CardId(
                manager.AddNewCard(data, CardPile.HandPile, false, false, animate: false) ?? throw new InvalidOperationException("Missing lethal health control."))).ToArray();
            managers.GetCardStatistics().GetStatValue(new CardStatistics.StatValueData { trackedValue = CardStatistics.TrackedValueType.TimesPlayed,
                cardState = manager.GetHand()[0], entryDuration = CardStatistics.EntryDuration.ThisBattle, cardTypeTarget = CardStatistics.CardTypeTarget.Any });
            FullBattleTrace.Active!.InvalidateCardRules(); Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("LETHAL-MAXIMUM-BATTLE-PREPARED mode=" + Mode + "; authored unit/cards, original bosses retained.");
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (step >= cards.Length) return null;
            int targetCard = step == 4 ? cards[0] : cards[1];
            int target = step < 2 ? 0 : state.Spawn.Train.Rooms[2].Units.Single(unit => unit.SpawnerCardId == targetCard).Id;
            var action = new PlayCardAction(cards[step], 2, playerPosition: step < 2 ? 0 : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(state, action);
            if (!result.Supported) throw new InvalidOperationException("Lethal maximum-health input " + step + ": " + result.Reason);
            step++; return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action) => cards.Contains(action.CardInstanceId);
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class FirePatch
        {
            private static void Prefix(CharacterState __instance, CharacterTriggerData.Trigger trigger, CharacterState dyingCharacter,
                bool fromRunningTriggerQueue, CharacterState.FireTriggersData fireTriggersData, int triggerCount)
            {
                if (!Enabled || !Prepared || !fromRunningTriggerQueue || AllGameManagers.Instance!.GetSaveManager().PreviewMode ||
                    trigger.ToString() is not ("OnDeathwish" or "OnDeathwishLost" or "OnReanimated" or "OnDeath" or "OnAnyUnitDeathOnFloor")) return;
                var trace = FullBattleTrace.Active!;
                callbacks.Add(new { Step = step, ActorId = trace.UnitId(__instance), Kind = trigger.ToString(), Count = triggerCount,
                    ParamInt = fireTriggersData?.paramInt ?? 0, DyingId = dyingCharacter == null ? 0 : trace.UnitId(dyingCharacter),
                    Before = trace.CaptureUnit(__instance), Gold = AllGameManagers.Instance.GetSaveManager().GetGold() });
            }
        }
    }
}
