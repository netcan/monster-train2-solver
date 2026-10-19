using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalStarterFrostbiteDiscardScenario
    {
        internal const string Name = "original-starter-frostbite-discard";
        internal const string CardId = "6b91f902-deb4-4519-863b-0c8a7f5d871c";
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
                throw new InvalidOperationException("Original Starter Frostbite Discard CardData lookup failed: " + CardId);
            IReadOnlyList<CardEffectData> effects = spell.GetEffects();
            StatusEffectStackData[] parameters = effects.Count == 2 ? effects[0].GetParamStatusEffects() : Array.Empty<StatusEffectStackData>();
            if (spell.name != "StarterFrostbiteDiscard" || spell.GetCardType() != CardType.Spell || spell.GetCost() != 0 ||
                effects.Count != 2 || effects[0].GetEffectStateName() != "CardEffectAddStatusEffect" ||
                effects[0].GetTargetMode().ToString() != "FrontInRoom" ||
                !effects[0].GetTargetTeamType().HasFlag(Team.Type.Heroes) ||
                effects[0].GetTargetTeamType().HasFlag(Team.Type.Monsters) || parameters.Length != 1 ||
                parameters[0].statusId != "poison" || parameters[0].count != 8 ||
                effects[1].GetEffectStateName() != "CardEffectRandomDiscard" ||
                effects[1].GetTargetMode().ToString() != "Hand" || effects[1].GetParamInt() != 1 ||
                spell.GetTraits().Count != 0 || spell.GetCardTriggers().Count != 0)
                throw new InvalidOperationException("Original Starter Frostbite Discard definition changed.");

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
                throw new InvalidOperationException("Original Starter Frostbite Discard scenario could not add its controlled cards.");
            CardIds = new[] { anchor, target, discarded }.Select(FullBattleTrace.Active!.CardId).ToArray();

            save.SetForgePoints(0);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-STARTER-FROSTBITE-DISCARD-PREPARED unchanged zero-cost spell, enemy-front Poison target and one eligible controlled random discard.");
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
