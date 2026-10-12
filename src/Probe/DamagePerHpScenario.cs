using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DamagePerHpScenario
    {
        internal const string Name = "damage-per-hp";

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] spells = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Where(card => card.GetCardType() == CardType.Spell &&
                    card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            if (spells.Length == 0) throw new InvalidOperationException("DamagePerHp fixture requires the owned rearrangement spell.");

            CardData data = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            var effects = data.GetEffects(); effects.Clear();
            var damage = new CardEffectData("CardEffectDamagePerHp", null!, Team.Type.Monsters);
            damage.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            AccessTools.Field(typeof(CardEffectData), "paramInt").SetValue(damage, 1);
            effects.Add(damage);
            AccessTools.Field(typeof(CardData), "targetless").SetValue(data, false);
            AccessTools.Field(typeof(CardData), "targetsRoom").SetValue(data, true);
            foreach (CardState card in spells) card.Setup(data, save);
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("DAMAGE-PER-HP-PREPARED selected-friendly-unit HP multiplier targeting opposing front.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (source.PlayRules != null && source.Spawn.Train.Context != null)
            {
                var cardIds = source.Spawn.Train.Context.Cards.Hand
                    .Where(card => source.PlayRules.Cards.Any(rule => rule.DataId == card.DataId &&
                        rule.Effects.Any(effect => effect.Type == "DamagePerHp")))
                    .Select(card => card.InstanceId).ToHashSet();
                if (cardIds.Count > 0)
                {
                    PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(source)
                        .FirstOrDefault(play => cardIds.Contains(play.CardInstanceId) && play.TargetUnitId > 0 &&
                            source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                                .Any(unit => unit.Id == play.TargetUnitId && unit.Team == CombatTeam.Player));
                    if (action != null) return action;
                }
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }
    }
}
