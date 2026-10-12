using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DamageOverflowScenario
    {
        internal const string Name = "damage-overflow";
        private const string Trait = "CardTraitDamageOverflow";

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new System.Collections.Generic.List<CardState>()).ToArray();
            CardState[] spells = owned.Where(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            if (spells.Length == 0) throw new InvalidOperationException("Damage overflow fixture requires an owned rearrangement spell.");

            CardData data = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            data.GetEffects().Clear();
            var damage = new CardEffectData("CardEffectDamage", null!, Team.Type.Heroes);
            damage.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            AccessTools.Field(typeof(CardEffectData), "paramInt").SetValue(damage, 20);
            data.GetEffects().Add(damage);
            var overflow = new CardTraitData();
            overflow.Setup(Trait);
            overflow.SetParamInt(0);
            data.GetTraits().Clear();
            data.GetTraits().Add(overflow);
            AccessTools.Field(typeof(CardData), "targetless").SetValue(data, false);
            AccessTools.Field(typeof(CardData), "targetsRoom").SetValue(data, true);
            AccessTools.Field(typeof(CardData), "cost").SetValue(data, 0);
            foreach (CardState card in spells) card.Setup(data, save);
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("DAMAGE-OVERFLOW-PREPARED friendly-team damage=20.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            CombatContext? context = source.Spawn.Train.Context;
            if (context?.CardInstances == null || source.PlayRules == null) return null;
            var cards = context.Cards.Hand.Where(token => context.CardInstances.Any(card => card.InstanceId == token.InstanceId &&
                card.MaskDescriptor?.BaseTraits.Any(trait => trait.RuntimeType == Trait) == true))
                .Select(card => card.InstanceId).ToHashSet();
            if (cards.Count == 0) return null;

            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
            {
                if (!cards.Contains(action.CardInstanceId) || action.TargetUnitId <= 0) return false;
                RoomCombatState? room = source.Spawn.Train.Rooms.FirstOrDefault(item => item.RoomIndex == action.RoomIndex);
                CombatUnit? target = room?.Units.FirstOrDefault(unit => unit.Id == action.TargetUnitId && unit.Team == CombatTeam.Enemy);
                if (room == null || target == null || target.Health <= 0 ||
                    target.Statuses.Any(status => status.Id is "armor" or "damage shield" or "titanskin" or "untouchable") ||
                    !room.Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Id != target.Id)) return false;
                CardToken token = context.Cards.Hand.Single(card => card.InstanceId == action.CardInstanceId);
                CardActionEffect? damage = source.PlayRules.Cards.Single(rule => rule.DataId == token.DataId).Effects
                    .FirstOrDefault(effect => effect.Type == "Damage");
                return damage != null && damage.Value > target.Health;
            });
        }
    }
}
