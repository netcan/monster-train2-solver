using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalEagerConscriptScenario
    {
        internal const string Name = "original-eager-conscript";
        internal const string CardId = "0c8ff108-478c-4c1c-9c49-f6a297b4e0d8";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            CharacterData unit = steward.GetSpawnCharacterData();
            CardData eager = all.FindCardData(CardId)!;
            CardEffectData effect = eager.GetEffects().Single();
            StatusEffectStackData[] statuses = effect.GetParamStatusEffects();
            if (eager.name != "EagerConscript" || eager.GetCardType() != CardType.Spell || eager.GetCost() != 1 ||
                effect.GetEffectStateName() != "CardEffectSpawnMonsterOrAddStatusEffect" || effect.GetTargetMode() != TargetMode.Room ||
                effect.GetTargetTeamType() != Team.Type.Monsters || effect.GetTargetCharacterSubtype()?.Key != "SubtypesData_Horde" ||
                effect.GetParamInt() != 1 || statuses.Length != 1 || statuses[0].statusId != "horde" || statuses[0].count != 1 ||
                effect.GetParamCharacterData()?.name != "MonsterFunguy")
                throw new InvalidOperationException("Original Eager Conscript definition changed.");

            Set(steward, "cost", 0);
            Set(unit, "attackDamage", 60);
            Set(unit, "health", 500);
            Set(unit, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID()))
                card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();

            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false);
            Add(steward);
            Add(eager);
            Add(eager);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-EAGER-CONSCRIPT-PREPARED unchanged 1-cost Spell; first play spawns a Horde Funguy and second adds Horde.");
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
