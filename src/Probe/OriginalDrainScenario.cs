using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalDrainScenario
    {
        internal const string Name = "original-drain";
        internal const string CardId = "6fe6bf99-7e77-4191-9136-fbbecc223bdf";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData drain = all.FindCardData(CardId) ??
                throw new InvalidOperationException("Original Drain CardData lookup failed: " + CardId);
            IReadOnlyList<CardEffectData> effects = drain.GetEffects();
            if (drain.name != "Drain" || drain.GetCardType() != CardType.Spell || drain.GetCost() != 1 ||
                effects.Count != 2 || effects[0].GetEffectStateName() != "CardEffectAddStatusEffect" ||
                effects[0].GetTargetMode().ToString() != "DropTargetCharacter" ||
                !effects[0].GetTargetTeamType().HasFlag(Team.Type.Heroes) ||
                !effects[0].GetTargetTeamType().HasFlag(Team.Type.Monsters) || effects[0].GetParamStatusEffects().Length != 1 ||
                effects[0].GetParamStatusEffects()[0].statusId != "debuff" || effects[0].GetParamStatusEffects()[0].count != 3 ||
                effects[1].GetEffectStateName() != "CardEffectRandomDiscard" ||
                effects[1].GetTargetMode().ToString() != "Hand" || effects[1].GetParamInt() != 1 ||
                drain.GetTraits().Count != 0 || drain.GetCardTriggers().Count != 0)
                throw new InvalidOperationException("Original Drain definition changed.");

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
            CardState spell = cards.AddNewCard(drain, CardPile.HandPile, false, false, animate: false);
            CardState discarded = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            if (anchor == null || spell == null || discarded == null ||
                !cards.GetHand().Contains(anchor) || !cards.GetHand().Contains(spell) || !cards.GetHand().Contains(discarded))
                throw new InvalidOperationException("Original Drain scenario could not add its controlled cards.");
            CardIds = new[] { anchor, spell, discarded }.Select(FullBattleTrace.Active!.CardId).ToArray();

            save.SetForgePoints(0);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-DRAIN-PREPARED unchanged one-cost spell, friendly debuff target and one eligible controlled random discard.");
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
                CombatUnit? target = source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .FirstOrDefault(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0 && unit.SpawnerCardId == ids[0]);
                if (target == null) return null;
                PlayCardAction? play = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
                    action.CardInstanceId == ids[1] && action.TargetUnitId == target.Id);
                if (play != null && BattleActionModel.PlayCard(source, play).Supported) return play;
            }
            return null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
