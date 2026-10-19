using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalLazarusLeagueSpikeScenario
    {
        internal const string Name = "original-lazarus-league-spike";
        internal const string CardId = "7168ed36-de03-41db-a36f-79b54b121341";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            CharacterData unit = steward.GetSpawnCharacterData();
            CardData spike = all.FindCardData(CardId)!;
            CardEffectData effect = spike.GetEffects().Single();
            if (spike.name != "LazarusLeagueSpike" || spike.GetCardType() != CardType.Spell ||
                spike.GetCost() != 0 || spike.GetCostType() != CardData.CostType.ConsumeRemainingEnergy ||
                effect.GetEffectStateName() != "CardEffectAddStatusEffect" ||
                effect.GetTargetMode() != TargetMode.DropTargetCharacter ||
                effect.GetTargetTeamType() != (Team.Type.Heroes | Team.Type.Monsters) ||
                effect.GetParamStatusEffects().Single().statusId != "undying" ||
                effect.GetParamStatusEffects().Single().count != 0 ||
                spike.GetTraits().Count(trait => trait.GetTraitStateName() == "CardTraitScalingAddStatusEffect") != 1 ||
                spike.GetTraits().Count(trait => trait.GetTraitStateName() == "CardTraitExhaustState") != 1)
                throw new InvalidOperationException("Original Lazarus League Spike definition changed.");

            Set(steward, "cost", 0);
            Set(unit, "attackDamage", 60);
            Set(unit, "health", 500);
            Set(unit, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID()))
                card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();

            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward), Add(steward), Add(spike) };
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-LAZARUS-LEAGUE-SPIKE-PREPARED unchanged ConsumeRemainingEnergy Spell; four Ember adds four Undying and exhausts the card.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            if (ids.Length != 3) return null;
            CombatContext context = source.Spawn.Train.Context!;
            for (int index = 0; index < 2; index++)
                if (context.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                    return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            if (context.Statistics!.Value(ids[2], "TimesPlayed", "ThisBattle") != 0) return null;
            CombatUnit? target = source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .SingleOrDefault(unit => unit.SpawnerCardId == ids[0]);
            if (target == null) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play =>
                play.CardInstanceId == ids[2] && play.RoomIndex == 0 && play.TargetUnitId == target.Id);
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
