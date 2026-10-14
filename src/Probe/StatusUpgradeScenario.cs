using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class StatusUpgradeScenario
    {
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal const string AbilityId = "cad96b9a-a638-44ab-a0f9-488f8120b9ce";
        private static ManualLogSource? logger;
        private static int stewardCardId;
        private static int abilityCardId;
        private static bool activated;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "status-upgrade";
        internal static bool IsScenarioAction(PlayCardAction action) => Enabled && action.ActivatorUnitId > 0 &&
            action.CardInstanceId == abilityCardId;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            activated = false;
            SaveManager save = managers.GetSaveManager();
            AllGameData data = save.GetAllGameData();
            CardData ability = data.FindCardData(AbilityId) ??
                throw new InvalidOperationException("Original Solgard Stasis ability is missing.");
            if (!ability.IsUnitAbility() || ability.GetEffects().Count(effect =>
                effect.GetEffectStateName() == "CardEffectAddCardUpgradeToUnitsPerStatusEffect") != 1)
                throw new InvalidOperationException("Original Solgard Stasis ability definition changed.");
            CardData steward = data.FindCardData(StewardId) ??
                throw new InvalidOperationException("Train Steward is missing.");
            CharacterData unit = steward.GetSpawnCharacterData() ??
                throw new InvalidOperationException("Train Steward has no summon definition.");
            AccessTools.Field(typeof(CharacterData), "unitAbility").SetValue(unit, ability);
            var statuses = unit.GetStartingStatusEffects().Where(status => status.statusId != "stygian_blessing")
                .Concat(new[] { new StatusEffectStackData { statusId = "stygian_blessing", count = 2 } }).ToArray();
            AccessTools.Field(typeof(CharacterData), "startingStatusEffects").SetValue(unit, statuses);
            CardManager cards = managers.GetCardManager()!;
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == StewardId))
                card.Setup(steward, save);
            if (!cards.GetBelowHandSize()) throw new InvalidOperationException("Status-upgrade fixture needs a free hand slot.");
            CardState added = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            if (added == null) throw new InvalidOperationException("Status-upgrade fixture failed to add Train Steward.");
            stewardCardId = FullBattleTrace.Active!.CardId(added);
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager()!, true);
            log.LogInfo("STATUS-UPGRADE-PREPARED originalAbility=" + AbilityId + " steward=" + stewardCardId +
                " blessing=2 cooldown=" + ability.GetCooldownAtSpawn());
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (activated) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            PlayCardAction[] plays = BattleActionModel.EnumerateSupportedPlays(source).ToArray();
            PlayCardAction? ability = plays.FirstOrDefault(action => action.ActivatorUnitId > 0 &&
                source.Spawn.Train.Context?.FindCard(action.CardInstanceId)?.DataId == AbilityId);
            if (ability != null)
            {
                abilityCardId = ability.CardInstanceId;
                BattleActionResult predicted = BattleActionModel.PlayCard(source, ability);
                logger!.LogInfo("STATUS-UPGRADE-ABILITY supported=" + predicted.Supported +
                    " actor=" + ability.ActivatorUnitId + " reason=" + predicted.Reason);
                if (predicted.Supported) { activated = true; return ability; }
            }
            PlayCardAction? steward = plays.FirstOrDefault(action => action.CardInstanceId == stewardCardId);
            if (steward != null) return steward;
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }
    }
}
