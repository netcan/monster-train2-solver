using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalImpOlateScenario
    {
        internal const string Name = "original-imp-olate";
        internal const string CardDataId = "e6e77821-f0c2-4d76-bc7f-2efece0e065f";
        internal const string ImpCardDataId = "d76be2d0-f851-4d43-bdc0-7c7a7779724f";
        internal const string ImpSubtype = "SubtypesData_Imp_0f9b989f-15b5-4b16-8378-5d8ed8691e7c";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static int ImpWitnessId { get; private set; }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData card = all.FindCardData(CardDataId)!;
            CardTraitData[] sourceTraits = card.GetTraits().ToArray();
            if (card.name != "ImpOlate" || card.GetCardType() != CardType.Spell || card.GetEffects().Count != 1 ||
                card.GetEffects()[0].GetEffectStateName() != "CardEffectDamage" || sourceTraits.Length != 1 ||
                sourceTraits[0].GetTraitStateName() != "CardTraitScalingAddDamage" || sourceTraits[0].GetParamInt() != 25 ||
                sourceTraits[0].GetParamInt2() != 0 || sourceTraits[0].GetParamInt3() != 0 || sourceTraits[0].GetParamBool() ||
                sourceTraits[0].GetParamStr() != "" || sourceTraits[0].GetParamFloat() != 1 || sourceTraits[0].GetUseScalingParams() ||
                sourceTraits[0].GetParamTrackedValue().ToString() != "SubtypeInDeck" || sourceTraits[0].GetParamEntryDuration().ToString() != "ThisTurn")
                throw new InvalidOperationException($"Original Imp Olate definition changed: asset={card.name}, type={card.GetCardType()}, effects={string.Join(",", card.GetEffects().Select(effect => effect.GetEffectStateName()))}, traits={string.Join(",", sourceTraits.Select(trait => trait.GetTraitStateName()))}.");
            log.LogInfo($"ORIGINAL-IMP-OLATE-DEFINITION cost={card.GetCost()} costType={card.GetCostType()} target={card.GetEffects()[0].GetTargetMode()} traitInt={sourceTraits[0].GetParamInt()} int2={sourceTraits[0].GetParamInt2()} int3={sourceTraits[0].GetParamInt3()} bool={sourceTraits[0].GetParamBool()} str={sourceTraits[0].GetParamStr()} float={sourceTraits[0].GetParamFloat()} scaling={sourceTraits[0].GetUseScalingParams()} tracked={sourceTraits[0].GetParamTrackedValue()} duration={sourceTraits[0].GetParamEntryDuration()}.");

            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0);
            Set(unit, "attackDamage", 9999);
            Set(unit, "health", 9999);
            Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            foreach (CardState state in cards.GetAllCards(new List<CardState>()).Where(state => state.GetCardDataID() == steward.GetID()))
                state.Setup(steward, save);
            foreach (CardState state in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(state);
            cards.GetDrawPile().Clear();
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardData imp = all.FindCardData(ImpCardDataId) ?? throw new InvalidOperationException("Original Imp Olate requires the native starter Imp card.");
            if (imp.GetCardType() != CardType.Monster || imp.GetSpawnCharacterData()?.GetSubtypes().Any(subtype => subtype.Key == ImpSubtype) != true)
                throw new InvalidOperationException("Original Imp Olate scaling witness is not an original Imp card.");
            CardState impWitness = cards.AddNewCard(imp, CardPile.DeckPileTop, false, false, animate: false);
            if (impWitness == null || !cards.GetDrawPile().Contains(impWitness))
                throw new InvalidOperationException("Original Imp Olate Imp witness did not enter the draw pile.");
            ImpWitnessId = FullBattleTrace.Active!.CardId(impWitness);
            CardState[] created = { Add(steward.GetID()), Add(CardDataId) };
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo($"ORIGINAL-IMP-OLATE-PREPARED unchanged original spell with native Imp witness={ImpWitnessId} subtype={ImpSubtype} and one high-attack protector.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0) return null;
            if (index == 0)
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == 0);
            if (source.Spawn.Turn < 1 || !source.Spawn.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Enemy)) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == 0);
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
