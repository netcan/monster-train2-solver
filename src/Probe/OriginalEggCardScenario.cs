using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalEggCardScenario
    {
        internal const string Name = "original-egg-card";
        private const string CardId = "057f7f77-df4b-4981-9e6d-533a6e667f39";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            CardData card = save.GetAllGameData().FindCardData(CardId)!;
            CharacterData unit = card.GetSpawnCharacterData()!;
            if (card.GetAssetKey() != "KinhostVessel" || card.GetCardType() != CardType.Monster ||
                unit.GetAssetKey() != "MonsterKinhostVessel" ||
                unit.GetStartingStatusEffects().Single(status => status.statusId == "hatch").count != 8 ||
                !unit.GetTriggers().Any(trigger => trigger.GetTrigger() == CharacterTriggerData.Trigger.OnRemoveHatch) ||
                !unit.GetTriggers().Any(trigger => trigger.GetTrigger() == CharacterTriggerData.Trigger.OnHatched))
                throw new InvalidOperationException("Original Kinhost Vessel definition changed.");

            foreach (CardEffectData summon in unit.GetTriggers().SelectMany(trigger => trigger.GetEffects())
                .Where(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster"))
                TriggeredSummonProbe.Definition(summon);

            RoomState top = managers.GetRoomManager()!.GetRoom(2);
            object stats = AccessTools.Field(typeof(RoomState), "corruptionStats").GetValue(top);
            AccessTools.Field(stats.GetType(), "maxCorruption").SetValue(stats, 8);
            AccessTools.Field(stats.GetType(), "currentCorruption").SetValue(stats, 8);
            if (cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false) == null)
                throw new InvalidOperationException("Original Kinhost Vessel could not enter the initial hand.");

            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-EGG-CARD-PREPARED unchanged Kinhost Vessel in hand; upper room Corruption 8/8.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (source.Spawn.Train.Context?.Cards.Hand.All(card => card.DataId != CardId) != false) return null;
            return BattleActionModel.EnumerateSupportedPlays(source)
                .FirstOrDefault(action => action.RoomIndex == 2 &&
                    source.Spawn.Train.Context.FindCard(action.CardInstanceId)?.DataId == CardId);
        }
    }
}
