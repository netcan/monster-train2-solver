using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalImpPressiveScenario
    {
        internal const string Name = "original-imp-pressive";
        internal const string CardDataId = "adfdfdab-18cf-4f2e-8664-f33dd9251ba4";
        internal const string ImpCardDataId = "d76be2d0-f851-4d43-bdc0-7c7a7779724f";
        internal const string ImpSubtype = "SubtypesData_Imp_0f9b989f-15b5-4b16-8378-5d8ed8691e7c";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData spell = all.FindCardData(CardDataId) ?? throw new InvalidOperationException("Original ImpPressive CardData lookup failed.");
            CardEffectData[] effects = spell.GetEffects().ToArray();
            CardTraitData[] traits = spell.GetTraits().ToArray();
            CardData imp = all.FindCardData(ImpCardDataId) ?? throw new InvalidOperationException("Original ImpPressive requires the native starter Imp.");
            if (spell.name != "ImpPressive" || spell.GetCardType() != CardType.Spell || effects.Length != 2 ||
                !effects.Select(effect => effect.GetEffectStateName()).SequenceEqual(new[] { "CardEffectSacrifice", "CardEffectDamage" }) ||
                traits.Length != 1 || traits[0].GetTraitStateName() != "CardTraitStrongerMagicPower" || traits[0].GetParamInt() != 5 ||
                imp.GetCardType() != CardType.Monster || imp.GetSpawnCharacterData()?.GetSubtypes().Any(subtype => subtype.Key == ImpSubtype) != true)
                throw new InvalidOperationException($"Original ImpPressive definition or native Imp subtype witness changed: name={spell.name}, type={spell.GetCardType()}, cost={spell.GetCost()}, effects=[{string.Join(";", effects.Select(effect => $"{effect.GetEffectStateName()}:{effect.GetTargetMode()}:{effect.GetParamInt()}:{effect.GetParamSubtype().Key}"))}], traits=[{string.Join(";", traits.Select(trait => $"{trait.GetTraitStateName()}:{trait.GetParamInt()}"))}], impType={imp.GetCardType()}, impSubtypes=[{string.Join(",", imp.GetSpawnCharacterData()?.GetSubtypes().Select(subtype => subtype.Key) ?? Enumerable.Empty<string>())}].");
            log.LogInfo($"ORIGINAL-IMP-PRESSIVE-DEFINITION cost={spell.GetCost()} costType={spell.GetCostType()} damage={effects[1].GetParamInt()} damageTarget={effects[1].GetTargetMode()} sacrificeTarget={effects[0].GetTargetMode()} sacrificeSubtype={effects[0].GetParamSubtype().Key} magicPower={traits[0].GetParamInt()}.");

            CardData steward = all.FindCardData(StewardId)!;
            CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0);
            Set(unit, "attackDamage", 9999);
            Set(unit, "health", 9999);
            Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            foreach (CardState state in cards.GetAllCards(new List<CardState>()).Where(state => state.GetCardDataID() == steward.GetID()))
                state.Setup(steward, save);
            foreach (CardState state in cards.GetAllCards(new List<CardState>()).Where(state => state.GetCardDataID() == imp.GetID()))
                state.Setup(imp, save);
            Set(imp, "cost", 0);
            foreach (CardState state in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(state);
            cards.GetDrawPile().Clear();
            cards.GetDiscardPile().Clear();
            cards.GetExhaustedPile().Clear();

            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(StewardId), Add(ImpCardDataId), Add(CardDataId) };
            if (created.Any(card => card == null || !cards.GetHand().Contains(card)))
                throw new InvalidOperationException("Original ImpPressive scenario could not add its controlled cards.");
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-IMP-PRESSIVE-PREPARED unchanged ImpPressive with a native starter Imp and a protected target floor.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            var context = source.Spawn.Train.Context!;
            if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[0] && play.RoomIndex == 0 && play.PlayerPosition == 0);
            if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0)
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[1] && play.RoomIndex == 0 && play.PlayerPosition == 1);
            if (context.Statistics.Value(ids[2], "TimesPlayed", "ThisBattle") != 0) return null;
            if (source.Spawn.Turn < 1 || !source.Spawn.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Enemy)) return null;
            CombatUnit? imp = source.Spawn.Train.Rooms[0].Units.SingleOrDefault(unit => unit.Team == CombatTeam.Player && unit.Subtypes.Contains(ImpSubtype));
            if (imp == null) return null;
            var action = new PlayCardAction(ids[2], 0, targetUnitId: imp.Id);
            return BattleActionModel.PlayCard(source, action).Supported ? action : null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
