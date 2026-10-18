using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalOverflowCardsScenario
    {
        internal const string Name = "original-overflow-cards";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 200); Set(unit, "size", 1);
            CharacterData guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack"); Set(guard, "health", 400);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            CardData weak = UnityEngine.Object.Instantiate(steward); weak.name = "PojuOverflowVictim"; Set(weak, "id", "ac110019-0000-4000-8000-000000000001");
            CharacterData victim = UnityEngine.Object.Instantiate(unit); victim.name = "PojuOverflowVictimCharacter";
            Set(victim, "id", "ac110019-0000-4000-8000-000000000002"); Set(victim, "attackDamage", 0); Set(victim, "health", 5);
            Set(victim, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            var spawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters); spawn.Cheat_SetTargetMode(TargetMode.Room);
            Set(spawn, "paramCharacterData", victim); Set(weak, "effects", new List<CardEffectData> { spawn }); definitions.Add(weak);
            CardData battery = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            battery.name = "PojuOverflowBattery"; Set(battery, "id", "ac110019-0000-4000-8000-000000000003");
            Set(battery, "cost", 0); Set(battery, "costType", CardData.CostType.Default); Set(battery, "targetless", true); Set(battery, "targetsRoom", true);
            Set(battery, "traits", new List<CardTraitData>()); Set(battery, "startingUpgrades", new List<CardUpgradeData>());
            Set(battery, "triggers", new List<CardTriggerEffectData>()); Set(battery, "effectTriggers", new List<CharacterTriggerData>());
            var energy = new CardEffectData("CardEffectGainEnergy", null!, Team.Type.None); energy.Cheat_SetTargetMode(TargetMode.Room); Set(energy, "paramInt", 10);
            Set(battery, "effects", new List<CardEffectData> { energy }); definitions.Add(battery);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(weak.GetID()), Add(weak.GetID()), Add(battery.GetID()),
                Add("b1129fb0-6bd0-4a9c-aab4-ef7b977c6fb2"), Add("b1129fb0-6bd0-4a9c-aab4-ef7b977c6fb2"), Add("817d1245-13e6-4a4c-9c90-26d2622a860e") };
            foreach (CardState card in created.Skip(4)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-OVERFLOW-CARDS-PREPARED original Pyreball/Pyreclasm Hero-team overflow, friendly primary victims, nonlethal control and outside-floor control.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 4 && source.Spawn.Turn < 1) return null;
            int room = index == 1 ? 1 : 0;
            if (index >= 5 && !source.Spawn.Train.Rooms.Single(value => value.RoomIndex == room).Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0)) return null;
            int owner = index == 5 ? 0 : index == 6 ? 2 : index == 7 ? 3 : -1;
            int target = owner < 0 ? 0 : source.Spawn.Train.Rooms.SelectMany(value => value.Units).Single(unit => unit.SpawnerCardId == ids[owner]).Id;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == room && play.TargetUnitId == target);
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
