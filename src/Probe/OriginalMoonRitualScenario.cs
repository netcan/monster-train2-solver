using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalMoonRitualScenario
    {
        internal const string Name = "original-moon-ritual";
        internal const string CardId = "b1fe0ba1-fcce-4764-9f4c-54b77d20b665";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData ritual = all.FindCardData(CardId) ??
                throw new InvalidOperationException("Original Moon Ritual CardData lookup failed: " + CardId);
            IReadOnlyList<CardEffectData> effects = ritual.GetEffects();
            if (ritual.name != "MoonRitual" || ritual.GetCost() != 0 || effects.Count != 2 ||
                effects[0].GetEffectStateName() != "CardEffectAddStatusEffect" ||
                effects[0].GetTargetMode().ToString() != "DropTargetCharacter" ||
                !effects[0].GetTargetTeamType().HasFlag(Team.Type.Monsters) || effects[0].GetParamStatusEffects().Length != 1 ||
                effects[0].GetParamStatusEffects()[0].statusId != "conduit" || effects[0].GetParamStatusEffects()[0].count != 1 ||
                effects[1].GetEffectStateName() != "CardEffectAdvanceMoonPhase" || effects[1].GetParamInt() != 1 ||
                effects[1].GetTargetMode().ToString() != "Room" || !effects[1].GetTargetTeamType().HasFlag(Team.Type.Monsters) ||
                ritual.GetTraits().Count != 0 || ritual.GetCardTriggers().Count != 0)
                throw new InvalidOperationException("Original Moon Ritual definition changed.");

            CardData steward = all.FindCardData(StewardId) ??
                throw new InvalidOperationException("Original Train Steward CardData lookup failed: " + StewardId);
            CharacterData unit = steward.GetSpawnCharacterData() ??
                throw new InvalidOperationException("Train Steward spawn data lookup failed.");
            Set(steward, "cost", 0); Set(unit, "attackDamage", 9999); Set(unit, "health", 9999); Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID()))
                card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();

            CardState anchor = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState spell = cards.AddNewCard(ritual, CardPile.HandPile, false, false, animate: false);
            if (anchor == null || spell == null || !cards.GetHand().Contains(anchor) || !cards.GetHand().Contains(spell))
                throw new InvalidOperationException("Original Moon Ritual scenario could not add its controlled cards.");
            CardIds = new[] { anchor, spell }.Select(FullBattleTrace.Active!.CardId).ToArray();

            save.SetForgePoints(0);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            if (!(bool)AccessTools.Field(typeof(PlayerManager), "isMoonPhaseTrackingActive")
                .GetValue(managers.GetPlayerManager())!)
                throw new InvalidOperationException("Adding Original Moon Ritual did not activate moon-phase tracking.");
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-MOON-RITUAL-PREPARED unchanged zero-cost card, friendly Train Steward target, Conduit and one Moon Phase advance.");
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
                PlayCardAction? ritual = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
                    action.CardInstanceId == ids[1] && action.TargetUnitId == target.Id);
                if (ritual != null && BattleActionModel.PlayCard(source, ritual).Supported) return ritual;
            }
            return null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
