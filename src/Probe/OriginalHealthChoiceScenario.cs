using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalHealthChoiceScenario
    {
        internal const string Name = "original-health-choice-relic";
        internal const string OwnerId = "ac208c49-610a-43e7-80cc-e9680cf84fcd";
        internal const string Gear = "fc119000-0000-4000-8000-000000000401";
        internal const string Wound = "fc119000-0000-4000-8000-000000000403";
        internal const string Nudge = "fc119000-0000-4000-8000-000000000404";
        internal const string Heal = "fc119000-0000-4000-8000-000000000405";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var data = save.GetAllGameData(); var cards = managers.GetCardManager();
            if (Prepared || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Health choice calibration requires isolated deployment.");
            var owned = cards.GetAllCards(new List<CardState>()).ToArray();
            var stewards = owned.Select(card => data.FindCardData(card.GetCardDataID())!).Where(card =>
                card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true).Distinct().Take(2).ToArray();
            if (stewards.Length != 2) throw new InvalidOperationException("Two Steward carriers required.");
            for (int index = 0; index < 2; index++)
            {
                var unit = stewards[index].GetSpawnCharacterData()!;
                Set(unit, "health", index == 0 ? 80 : 13); Set(unit, "attackDamage", index == 0 ? 20 : 2);
                Set(unit, "size", 1); Set(unit, "equipmentLimit", 3);
                Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
                Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "unitAbility", null); Set(stewards[index], "cost", 0);
            }
            var gear = UnityEngine.Object.Instantiate(data.GetAllCardData().First(card => card.GetCardType() == CardType.Equipment &&
                card.GetEffects().Count == 1 && card.GetEffects()[0].GetEffectStateName() == "CardEffectAttachEquipment"));
            Configure(gear, "PojuHealthChoiceGear", Gear);
            var upgrade = DynamicUpgradeScenario.Upgrade("PojuHealthChoiceGearUpgrade", "fc119000-0000-4000-8000-000000000402", 1, 0, 0, 0, "armor", 0);
            Set(upgrade, "statusEffectUpgrades", new List<StatusEffectStackData>());
            var attach = Effect("CardEffectAttachEquipment", 0); Set(attach, "paramCardUpgradeData", upgrade);
            Set(gear, "effects", new List<CardEffectData> { attach });
            var spell = data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Count == 1 && card.GetEffects()[0].GetEffectStateName() == "CardEffectDamage");
            CardData Spell(string name, string id, string type, int amount)
            {
                var clone = UnityEngine.Object.Instantiate(spell); Configure(clone, name, id);
                Set(clone, "effects", new List<CardEffectData> { Effect(type, amount) }); return clone;
            }
            var wound = Spell("PojuHealthChoiceWound", Wound, "CardEffectDamage", 6);
            var nudge = Spell("PojuHealthChoiceNudge", Nudge, "CardEffectDamage", 1);
            var heal = Spell("PojuHealthChoiceHeal", Heal, "CardEffectHeal", 12);
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).AddRange(new[] { gear, wound, nudge, heal });
            foreach (var card in owned) card.Setup(data.FindCardData(card.GetCardDataID())!, save);
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            foreach (var definition in new[] { stewards[0], stewards[1], gear, gear, gear, gear, wound, nudge, heal })
                if (cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false) == null)
                    throw new InvalidOperationException("Health choice carrier creation failed.");
            var relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == OwnerId);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId)) throw new InvalidOperationException("Owner already acquired.");
            save.AddRelic(relic); Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-HEALTH-CHOICE-PREPARED unchanged HairOfTheHybrid; HP 13/7/6 carriers and original 50-percent equipment callback.");
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            var context = state.Spawn.Train.Context!; var plays = BattleActionModel.EnumerateSupportedPlays(state);
            string? Data(int id) => context.FindCard(id)?.DataId;
            var room = state.Spawn.Train.Rooms[0];
            foreach (int maximum in new[] { 80, 13 })
            {
                if (room.Units.Any(unit => unit.Team == CombatTeam.Player && unit.MaxHealth == maximum)) continue;
                var summon = plays.FirstOrDefault(play => play.RoomIndex == 0 && play.PlayerPosition == room.Units.Count(unit => unit.Team == CombatTeam.Player) &&
                    state.PlayRules!.Cards.FirstOrDefault(rule => rule.DataId == Data(play.CardInstanceId))?.SpawnUnit?.MaxHealth == maximum);
                if (summon != null) return summon;
            }
            var strong = room.Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player && unit.MaxHealth == 80);
            if (strong?.EquipmentCards?.Count == 0)
            {
                var equip = plays.FirstOrDefault(play => Data(play.CardInstanceId) == Gear && play.TargetUnitId == strong.Id);
                if (equip != null) return equip;
            }
            var fragile = room.Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player && unit.MaxHealth == 13);
            if (fragile != null)
            {
                bool Played(string data) => context.Statistics!.Values.Any(value => value.Type == "TimesPlayed" && value.Duration == "ThisBattle" &&
                    value.Value > 0 && context.FindCard(value.CardId)?.DataId == data);
                int equipped = fragile.EquipmentCards!.Count;
                string? wanted = !Played(Wound) ? Wound : equipped == 0 ? Gear : !Played(Nudge) ? Nudge : equipped == 1 ? Gear :
                    !Played(Heal) ? Heal : equipped == 2 ? Gear : null;
                var action = plays.FirstOrDefault(play => wanted != null && Data(play.CardInstanceId) == wanted && play.TargetUnitId == fragile.Id);
                if (action != null) return action;
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(state, card => Data(card.InstanceId) != Gear && Data(card.InstanceId) != Wound &&
                Data(card.InstanceId) != Nudge && Data(card.InstanceId) != Heal);
        }
        private static void Configure(CardData card, string key, string id)
        {
            card.name = key; Set(card, "id", id); Set(card, "cost", 0);
            Set(card, "traits", new List<CardTraitData>()); Set(card, "triggers", new List<CardTriggerEffectData>());
            Set(card, "effectTriggers", new List<CharacterTriggerData>()); Set(card, "startingUpgrades", new List<CardUpgradeData>());
        }
        private static CardEffectData Effect(string type, int amount)
        {
            var effect = new CardEffectData(type, null!, Team.Type.Monsters);
            effect.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(effect, "paramInt", amount); return effect;
        }
        private static void Set(object target, string field, object? value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
