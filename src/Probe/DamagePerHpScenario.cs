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
        internal const string ArmorName = "damage-per-hp-armor";
        internal const string LastAttackerName = "damage-per-hp-last-attacker";

        internal static void Prepare(AllGameManagers managers, ManualLogSource log, string scenario)
        {
            bool lastAttacker = scenario == LastAttackerName;
            bool includeArmor = scenario == ArmorName;
            SaveManager save = managers.GetSaveManager();
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            CardState[] spells = owned
                .Where(card => card.GetCardType() == CardType.Spell &&
                    card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            if (spells.Length == 0) throw new InvalidOperationException("DamagePerHp fixture requires the owned rearrangement spell.");

            CardData data = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            var effects = data.GetEffects(); effects.Clear();
            var damage = new CardEffectData("CardEffectDamagePerHp", null!, Team.Type.Monsters);
            damage.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            AccessTools.Field(typeof(CardEffectData), "paramInt").SetValue(damage, 1);
            if (lastAttacker || includeArmor)
            {
                AccessTools.Field(typeof(CardEffectData), "paramBool").SetValue(damage, lastAttacker);
                AccessTools.Field(typeof(CardEffectData), "paramBool2").SetValue(damage, includeArmor);
                if (includeArmor) AddArmorToOwnedMonsters(save, owned);
            }
            effects.Add(damage);
            AccessTools.Field(typeof(CardData), "targetless").SetValue(data, false);
            AccessTools.Field(typeof(CardData), "targetsRoom").SetValue(data, true);
            foreach (CardState card in spells) card.Setup(data, save);
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("DAMAGE-PER-HP-PREPARED lastAttacker=" + lastAttacker + " armor=" + includeArmor + ".");
        }

        private static void AddArmorToOwnedMonsters(SaveManager save, CardState[] owned)
        {
            CharacterData[] definitions = owned.Where(card => card.GetCardType() == CardType.Monster)
                .Select(card => save.GetAllGameData().FindCardData(card.GetCardDataID()))
                .Where(data => data != null)
                .Select(data => data!.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData())
                .Distinct().ToArray();
            if (definitions.Length == 0) throw new InvalidOperationException("DamagePerHp last-attacker fixture requires an owned monster.");
            foreach (CharacterData data in definitions)
            {
                List<StatusEffectStackData> statuses = (data.GetStartingStatusEffects() ?? Array.Empty<StatusEffectStackData>())
                    .Where(status => status.statusId != "armor").ToList();
                statuses.Add(new StatusEffectStackData { statusId = "armor", count = 50 });
                AccessTools.Field(typeof(CharacterData), "startingStatusEffects").SetValue(data, statuses.ToArray());
            }
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
                    PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play =>
                    {
                        if (!cardIds.Contains(play.CardInstanceId) || play.TargetUnitId <= 0) return false;
                        CombatUnit? unit = source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                            .FirstOrDefault(item => item.Id == play.TargetUnitId && item.Team == CombatTeam.Player);
                        if (unit == null) return false;
                        string dataId = source.Spawn.Train.Context!.Cards.Hand.Single(card => card.InstanceId == play.CardInstanceId).DataId;
                        CardActionEffect effect = source.PlayRules!.Cards.Single(rule => rule.DataId == dataId)
                            .Effects.Single(item => item.Type == "DamagePerHp");
                        return (!effect.DamagePerHpLastAttacker || unit.LastAttackerId is > 0) &&
                            (!effect.DamagePerHpIncludeArmor || unit.Statuses.Any(status => status.Id == "armor" && status.Stacks > 0));
                    });
                    if (action != null) return action;
                }
            }
            bool hasCapturedTargetRequirement = source.PlayRules != null && source.Spawn.Train.Context != null &&
                source.Spawn.Train.Context.Cards.Hand.Any(card => source.PlayRules.Cards.Any(rule => rule.DataId == card.DataId &&
                    rule.Effects.Any(effect => effect.Type == "DamagePerHp" &&
                        (effect.DamagePerHpLastAttacker || effect.DamagePerHpIncludeArmor))));
            return hasCapturedTargetRequirement
                ? BattleActionModel.ChooseUnitAndJunkPlay(source)
                : BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }
    }
}
