using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSmeltedTreasureScenario
    {
        internal const string Name = "original-smelted-treasure";
        internal const string CardId = "a1049085-eba0-4118-9546-ca20d680c2b8";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData treasure = all.FindCardData(CardId) ??
                throw new InvalidOperationException("Original Smelted Treasure CardData lookup failed: " + CardId);
            IReadOnlyList<CardEffectData> effects = treasure.GetEffects();
            if (treasure.name != "SmeltedTreasure" || treasure.GetCardType() != CardType.Spell || treasure.GetCost() != 1 ||
                effects.Count != 2 || effects[0].GetEffectStateName() != "CardEffectRewardGold" ||
                effects[0].GetTargetMode().ToString() != "Room" || effects[0].GetParamInt() != -50 || !effects[0].GetParamBool() ||
                effects[1].GetEffectStateName() != "CardEffectAddStatusEffect" ||
                effects[1].GetTargetMode().ToString() != "DropTargetCharacter" ||
                !effects[1].GetTargetTeamType().HasFlag(Team.Type.Monsters) || effects[1].GetParamStatusEffects().Length != 1 ||
                effects[1].GetParamStatusEffects()[0].statusId != "armor" || effects[1].GetParamStatusEffects()[0].count != 50 ||
                treasure.GetTraits().Count != 0 || treasure.GetCardTriggers().Count != 0)
                throw new InvalidOperationException("Original Smelted Treasure definition changed.");

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
            CardState spell = cards.AddNewCard(treasure, CardPile.HandPile, false, false, animate: false);
            if (anchor == null || spell == null || !cards.GetHand().Contains(anchor) || !cards.GetHand().Contains(spell))
                throw new InvalidOperationException("Original Smelted Treasure scenario could not add its controlled cards.");
            CardIds = new[] { anchor, spell }.Select(FullBattleTrace.Active!.CardId).ToArray();

            save.SetGold(100, isTotalReset: true);
            save.SetForgePoints(0);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-SMELTED-TREASURE-PREPARED unchanged one-cost card, 100 gold, one friendly Train Steward and 50 armor purchase.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
            {
                var summon = new PlayCardAction(ids[0], 0, playerPosition: 0);
                if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
            }
            if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0)
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
