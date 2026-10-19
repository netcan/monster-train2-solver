using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalDeadlyVariantScenario
    {
        internal const string Name = "original-deadly-variant";
        internal const string CardId = "5efb679b-6035-4d64-ab80-f29daad1fdf4";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData variant = all.FindCardData(CardId) ??
                throw new InvalidOperationException("Original Deadly Variant CardData lookup failed: " + CardId);
            IReadOnlyList<CardEffectData> effects = variant.GetEffects();
            StatusEffectStackData[] parameters = effects.Count == 1 ? effects[0].GetParamStatusEffects() : Array.Empty<StatusEffectStackData>();
            if (variant.name != "DeadlyVariant" || variant.GetCardType() != CardType.Spell || variant.GetCost() != 2 ||
                effects.Count != 1 || effects[0].GetEffectStateName() != "CardEffectAddStatusEffectPerOtherEffect" ||
                effects[0].GetTargetMode().ToString() != "DropTargetCharacter" ||
                !effects[0].GetTargetTeamType().HasFlag(Team.Type.Heroes) ||
                !effects[0].GetTargetTeamType().HasFlag(Team.Type.Monsters) || effects[0].GetParamInt() != 5 ||
                !effects[0].GetParamBool() || parameters.Length != 2 || parameters[0].statusId != "decay" ||
                parameters[0].count != 1 || parameters[1].statusId != "debuff" || parameters[1].count != 5 ||
                !variant.GetTraits().Select(trait => trait.GetTraitStateName()).SequenceEqual(new[] { "CardTraitExhaustState" }) ||
                variant.GetCardTriggers().Count != 0)
                throw new InvalidOperationException("Original Deadly Variant definition changed.");

            CardData steward = all.FindCardData(StewardId) ??
                throw new InvalidOperationException("Original Train Steward CardData lookup failed: " + StewardId);
            CharacterData unit = steward.GetSpawnCharacterData() ??
                throw new InvalidOperationException("Train Steward spawn data lookup failed.");
            Set(steward, "cost", 0); Set(unit, "attackDamage", 9999); Set(unit, "health", 9999); Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", new[] { new StatusEffectStackData { statusId = "debuff", count = 1 } });
            foreach (CardState existing in cards.GetAllCards(new List<CardState>()).Where(item => item.GetCardDataID() == steward.GetID()))
                existing.Setup(steward, save);
            foreach (CardState existing in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(existing);
            cards.GetDrawPile().Clear();

            CardState anchor = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState spell = cards.AddNewCard(variant, CardPile.HandPile, false, false, animate: false);
            if (anchor == null || spell == null || !cards.GetHand().Contains(anchor) || !cards.GetHand().Contains(spell))
                throw new InvalidOperationException("Original Deadly Variant scenario could not add its controlled cards.");
            CardIds = new[] { anchor, spell }.Select(FullBattleTrace.Active!.CardId).ToArray();

            save.SetForgePoints(0);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-DEADLY-VARIANT-PREPARED unchanged two-cost spell; selected Train Steward carries one counted Debuff for five Decay.");
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
