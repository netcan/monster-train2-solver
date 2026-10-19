using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalTitansGratitudeScenario
    {
        internal const string Name = "original-titans-gratitude";
        internal const string CardId = "9de23f57-7471-4c57-9b2e-b47f17e5f278";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData spell = all.FindCardData(CardId) ??
                throw new InvalidOperationException("Original Titan's Gratitude CardData lookup failed: " + CardId);
            IReadOnlyList<CardEffectData> effects = spell.GetEffects();
            CardTraitData[] traits = spell.GetTraits().ToArray();
            if (spell.name != "TitansGratitude" || spell.GetCardType() != CardType.Spell || spell.GetCost() != 1 ||
                effects.Count != 2 || effects[0].GetEffectStateName() != "CardEffectDamage" ||
                effects[0].GetTargetMode().ToString() != "FrontInRoom" ||
                !effects[0].GetTargetTeamType().HasFlag(Team.Type.Heroes) ||
                effects[0].GetTargetTeamType().HasFlag(Team.Type.Monsters) || effects[0].GetParamInt() != 60 ||
                effects[1].GetEffectStateName() != "CardEffectRandomDiscard" ||
                effects[1].GetTargetMode().ToString() != "Hand" || effects[1].GetParamInt() != 1 ||
                traits.Length != 1 || traits[0].GetTraitStateName() != "CardTraitStrongerMagicPower" ||
                traits[0].GetParamInt() != 5 || traits[0].GetParamInt2() != 0 || traits[0].GetParamInt3() != 0 ||
                traits[0].GetUseScalingParams() || traits[0].GetParamTrackedValue().ToString() != "SubtypeInDeck" ||
                traits[0].GetParamEntryDuration().ToString() != "ThisTurn" || spell.GetCardTriggers().Count != 0)
                throw new InvalidOperationException("Original Titan's Gratitude definition changed.");

            CardData steward = all.FindCardData(StewardId) ??
                throw new InvalidOperationException("Original Train Steward CardData lookup failed: " + StewardId);
            CharacterData unit = steward.GetSpawnCharacterData() ??
                throw new InvalidOperationException("Train Steward spawn data lookup failed.");
            Set(steward, "cost", 0); Set(unit, "attackDamage", 9999); Set(unit, "health", 9999); Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            foreach (CardState existing in cards.GetAllCards(new List<CardState>()).Where(item => item.GetCardDataID() == steward.GetID()))
                existing.Setup(steward, save);
            foreach (CardState existing in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(existing);
            cards.GetDrawPile().Clear();

            CardState anchor = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState target = cards.AddNewCard(spell, CardPile.HandPile, false, false, animate: false);
            CardState discarded = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            if (anchor == null || target == null || discarded == null ||
                !cards.GetHand().Contains(anchor) || !cards.GetHand().Contains(target) || !cards.GetHand().Contains(discarded))
                throw new InvalidOperationException("Original Titan's Gratitude scenario could not add its controlled cards.");
            CardIds = new[] { anchor, target, discarded }.Select(FullBattleTrace.Active!.CardId).ToArray();

            save.SetForgePoints(0);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-TITANS-GRATITUDE-PREPARED unchanged one-cost spell with its original stronger-magic trait, one enemy-front target and one controlled random-discard candidate.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
            {
                var summon = new PlayCardAction(ids[0], 0, playerPosition: 0);
                if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
            }
            if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0 &&
                context.Cards.Hand.Any(item => item.InstanceId == ids[1]) && context.Cards.Hand.Any(item => item.InstanceId == ids[2]))
            {
                PlayCardAction? play = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
                    action.CardInstanceId == ids[1] && source.Spawn.Train.Rooms.Any(room => room.RoomIndex == action.RoomIndex &&
                        room.Units.Count(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0) == 1));
                return play != null && BattleActionModel.PlayCard(source, play).Supported ? play : null;
            }
            return null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
