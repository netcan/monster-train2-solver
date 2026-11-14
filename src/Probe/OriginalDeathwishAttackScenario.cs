using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalDeathwishAttackScenario
    {
        internal const string Name = "original-deathwish-attack";
        internal const string Id = "437ca81a-8c1b-4e53-adfd-e64381e1b0d9";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        private static int[] cards = Array.Empty<int>();
        private static int step;
        private static ManualLogSource? logger;
        private static string? rejection;
        internal static object Evidence => new { Prepared, OwnerId = Id, ControlCardIds = cards, CompletedControlPlays = step,
            Transitions = HalfHealthBattleScenario.CallbackEvidence };

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var data = save.GetAllGameData(); var manager = managers.GetCardManager();
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0 ||
                save.GetCollectedRelics().Any(owner => owner.GetRelicDataID() == Id))
                throw new InvalidOperationException("DeathwishAttack requires isolated 2.3.1 deployment acquisition.");
            RelicData owner = data.GetAllCollectableRelicData().Single(relic => relic.GetID() == Id);
            RelicEffectData effect = owner.GetEffects().Single();
            CharacterTriggerData[] triggers = effect.GetTriggers().ToArray();
            if (owner.name != "DeathwishAttack" || owner.DisallowInDeploymentPhase || effect.GetEffectClassName() != "RelicEffectAddTrigger" ||
                effect.GetParamSourceTeam() != Team.Type.Monsters || !effect.GetParamCharacterSubtype().IsNone || effect.GetParamBool() ||
                effect.GetEffectConditions().Count != 0 || triggers.Length != 2 ||
                !triggers.Select(trigger => trigger.GetTrigger().ToString()).SequenceEqual(new[] { "OnDeathwish", "OnDeathwishLost" }) ||
                !triggers.Select(trigger => trigger.GetEffects().Single().GetEffectStateName()).SequenceEqual(new[] {
                    "CardEffectAddTempCardUpgradeToUnits", "CardEffectRemoveTempUpgradeFromUnit" }) ||
                triggers.Any(trigger => !trigger.GetHideVisualAndIgnoreSilence() || trigger.GetEffects().Count != 1 ||
                    trigger.GetEffects()[0].GetParamCardUpgradeData()?.name != "Plus15Attack"))
                throw new InvalidOperationException("Original DeathwishAttack definition changed.");
            // Only carriers/control spells are authored. The owner and its native
            // gain/remove upgrade definitions remain unchanged.
            CardData steward = data.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678");
            CharacterData carrier = steward.GetEffects().Single(item => item.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            Set(carrier, "health", 20);
            Set(carrier, "startingStatusEffects", carrier.GetStartingStatusEffects().Concat(new[] {
                new StatusEffectStackData { statusId = "undying", count = 1 } }).ToArray());
            var heal = new CardEffectData("CardEffectHeal", null!, Team.Type.Monsters);
            heal.Cheat_SetTargetMode(TargetMode.Self); Set(heal, "paramInt", 8);
            CharacterTriggerData attack = HealingScenario.HealGold(0, true, false);
            Set(attack, "trigger", CharacterTriggerData.Trigger.OnAttacking);
            Set(attack, "effects", new List<CardEffectData> { heal });
            Set(carrier, "triggers", carrier.GetTriggers().Concat(new[] { attack }).ToList());
            var owned = new HashSet<string>(manager.GetAllCards(new List<CardState>()).Select(card => card.GetCardDataID()));
            CardData[] spells = data.GetAllCardData().Where(card => card.GetCardType() == CardType.Spell && !owned.Contains(card.GetID()) &&
                card.GetEffects().Any(item => item.GetEffectStateName() == "CardEffectDamage")).Take(3).ToArray();
            if (spells.Length != 3) throw new InvalidOperationException("Missing independent unowned control spell definitions.");
            for (int index = 0; index < spells.Length; index++)
            {
                var action = new CardEffectData(index == 2 ? "CardEffectHeal" : "CardEffectDamage", null!, Team.Type.Monsters);
                action.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(action, "paramInt", index == 0 ? 10 : 1);
                Set(spells[index], "effects", new List<CardEffectData> { action });
                Set(spells[index], "traits", new List<CardTraitData>()); Set(spells[index], "cost", 0);
            }
            foreach (CardState card in manager.GetHand().ToArray()) manager.MoveCardFromHandToTopOfDeck(card);
            save.AddRelic(owner);
            CardData[] sequence = { steward, steward, spells[0], spells[2], spells[1], spells[0], spells[2] };
            CardState[] controls = sequence.Select(card => manager.AddNewCard(card, CardPile.HandPile, false, false, animate: false)).ToArray();
            if (controls.Any(card => card == null || !manager.GetHand().Contains(card))) throw new InvalidOperationException("DeathwishAttack controls missing.");
            cards = controls.Select(FullBattleTrace.Active!.CardId).ToArray(); logger = log; Prepared = true;
            Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-DEATHWISH-ATTACK-PREPARED unchanged owner and +15 upgrade; two paid 20-HP Undying carriers, once-only attack healing and five damage/heal controls.");
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (step >= cards.Length) return BattleActionModel.ChooseUnitSpellAndJunkPlay(state);
            int room = step == 1 || step >= 5 ? 1 : 0;
            int target = step < 2 ? 0 : state.Spawn.Train.Rooms[room].Units.FirstOrDefault(unit =>
                unit.SpawnerCardId == cards[step >= 5 ? 1 : 0])?.Id ?? 0;
            var action = new PlayCardAction(cards[step], room, playerPosition: step < 2 ? 0 : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(state, action);
            if (!result.Supported)
            {
                if (rejection != result.Reason) { rejection = result.Reason; logger?.LogInfo("ORIGINAL-DEATHWISH-ATTACK-REJECT " + step + ": " + rejection); }
                return null;
            }
            step++; return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action) => cards.Contains(action.CardInstanceId);
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
