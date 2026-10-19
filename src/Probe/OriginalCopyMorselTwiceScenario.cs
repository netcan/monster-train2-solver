using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalCopyMorselTwiceScenario
    {
        internal const string Name = "original-copy-morsel-twice";
        internal const string CardId = "e4c1814d-3c45-47aa-afdd-cb3fbe8e821d";
        internal const string MorselCardId = "536e6de0-7ccf-412a-9a4e-ec55ab3988e5";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            CardData morsel = all.FindCardData(MorselCardId)!;
            CardData copy = all.FindCardData(CardId)!;
            CharacterData stewardUnit = steward.GetSpawnCharacterData();
            CharacterData morselUnit = morsel.GetSpawnCharacterData()!;
            CardEffectData effect = copy.GetEffects().Single();
            if (copy.name != "CopyMorselTwice" || copy.GetCardType() != CardType.Spell || copy.GetCost() != 1 ||
                effect.GetEffectStateName() != "CardEffectCopyUnits" || effect.GetTargetMode() != TargetMode.DropTargetCharacter ||
                effect.GetTargetTeamType() != Team.Type.Monsters || effect.GetTargetCharacterSubtype()?.Key != "SubtypesData_Snack" ||
                effect.GetParamInt() != 3 || effect.GetParamBool() || morsel.name != "MorselMiner" ||
                morsel.GetCardType() != CardType.Monster || morsel.GetCost() != 0 ||
                !morselUnit.GetSubtypes().Any(subtype => subtype.Key == "SubtypesData_Snack"))
                throw new InvalidOperationException("Original Copy Morsel Twice or Morsel Miner definition changed.");

            Set(steward, "cost", 0);
            Set(stewardUnit, "attackDamage", 60);
            Set(stewardUnit, "health", 500);
            Set(stewardUnit, "size", 1);
            Set(morselUnit, "attackDamage", 60);
            Set(morselUnit, "health", 500);
            Set(morselUnit, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID()))
                card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();

            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false);
            Add(steward);
            Add(morsel);
            Add(copy);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-COPY-MORSEL-TWICE-PREPARED unchanged 1-cost Spell; one Snack target produces three native copies.");
        }

        [HarmonyPatch(typeof(CardManager), nameof(CardManager.CopyCardState))]
        private static class CopiedCardIdentityPatch
        {
            private static void Postfix(CardState __result)
            {
                if (Enabled && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    FullBattleTrace.Active!.CardId(__result);
            }
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
