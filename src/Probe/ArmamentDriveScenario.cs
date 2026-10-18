using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class ArmamentDriveScenario
    {
        internal const string Name = "armament-drive";
        internal const string DataId = "954f3138-3e3a-4369-9d4b-46adb7b1585c";
        internal const string EquipmentId = "b776421c-915c-4c3d-87dc-0fe1a9e8584c";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static string UnitId(int index) => "f15d0002-0000-4000-8000-00000000000" + index;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); CardManager cards = managers.GetCardManager()!;
            AllGameData data = save.GetAllGameData();
            CardData steward = data.GetAllCardData().First(card => card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data);
            for (int index = 1; index <= 2; index++)
            {
                CardData card = UnityEngine.Object.Instantiate(steward); card.name = "PojuArmamentUnit" + index;
                Set(card, "id", UnitId(index)); Set(card, "cost", 0); Set(card, "costType", CardData.CostType.Default);
                Set(card, "traits", new List<CardTraitData>()); Set(card, "startingUpgrades", new List<CardUpgradeData>());
                CharacterData unit = UnityEngine.Object.Instantiate(steward.GetSpawnCharacterData()); unit.name = card.name + "Character";
                Set(unit, "attackDamage", 60); Set(unit, "health", 100); Set(unit, "size", 1); Set(unit, "unitAbility", null!);
                Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
                var spawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters);
                spawn.Cheat_SetTargetMode(TargetMode.Room); Set(spawn, "paramCharacterData", unit);
                Set(card, "effects", new List<CardEffectData> { spawn }); definitions.Add(card);
                cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false);
            }
            CardIds = Enumerable.Range(0, 4).Select(_ => FullBattleTrace.Active!.CardId(
                cards.AddNewCard(data.FindCardData(DataId), CardPile.HandPile, false, false, animate: false))).ToArray();
            managers.GetPlayerManager().AddEnergy(10);
            Prepared = true;
            log.LogInfo("ARMAMENT-DRIVE-PREPARED four unchanged originals: empty room, two unequipped, one equipped and all equipped; generated original Crafted Equipment is played between casts.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            CombatContext context = source.Spawn.Train.Context!;
            var plays = BattleActionModel.EnumerateSupportedPlays(source);
            bool Played(int index) => context.Statistics!.Value(CardIds[index], "TimesPlayed", "ThisBattle") > 0;
            PlayCardAction? Cast(int index, int room) => plays.FirstOrDefault(play => play.CardInstanceId == CardIds[index] && play.RoomIndex == room);
            if (!Played(0)) return Cast(0, 2);
            foreach (int index in new[] { 1, 2 })
            {
                var summon = plays.FirstOrDefault(play => play.RoomIndex == 0 && context.FindCard(play.CardInstanceId)!.DataId == UnitId(index));
                if (summon != null) return summon;
            }
            if (!Played(1)) return Cast(1, 0);
            CombatUnit[] units = source.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player &&
                context.FindCard(unit.SpawnerCardId)?.DataId is string id && (id == UnitId(1) || id == UnitId(2))).ToArray();
            int equipped = units.Count(unit => unit.EquipmentCards!.Count > 0);
            if (equipped == 1 && !Played(2)) return Cast(2, 0);
            if (equipped < 2)
                return plays.FirstOrDefault(play => context.FindCard(play.CardInstanceId)!.DataId == EquipmentId &&
                    units.Any(unit => unit.Id == play.TargetUnitId && unit.EquipmentCards!.Count == 0));
            if (!Played(3)) return Cast(3, 0);
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }
        private static void Set(object target, string field, object value) =>
            (AccessTools.Field(target.GetType(), field) ?? throw new MissingFieldException(target.GetType().Name, field)).SetValue(target, value);
    }
}
