using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalDeathroarScenario
    {
        internal const string Name = "original-deathroar";
        internal const string CardDataId = "73da2bc8-68a5-41b2-a639-754c2a98b6fd";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData spell = all.FindCardData(CardDataId) ?? throw new InvalidOperationException("Original Deathroar CardData lookup failed.");
            CardEffectData[] effects = spell.GetEffects().ToArray();
            CardTraitData[] traits = spell.GetTraits().ToArray();
            CardEffectData transfer = effects.SingleOrDefault(effect => effect.GetEffectStateName() == "CardEffectTransferAllStatusEffects")
                ?? throw new InvalidOperationException("Original Deathroar has no unique all-status transfer effect.");
            StatusEffectStackData[] statuses = transfer.GetParamStatusEffects().ToArray();
            if (spell.name != "Deathroar" || spell.GetCardType() != CardType.Spell || spell.GetCost() != 3 || effects.Length != 6 ||
                effects[0] != transfer || effects.Skip(1).Any(effect => effect.GetEffectStateName() != "CardEffectDamage") ||
                effects[0].GetTargetMode() != TargetMode.DropTargetCharacter || effects[1].GetTargetMode() != TargetMode.DropTargetCharacter ||
                effects.Skip(2).Any(effect => effect.GetTargetMode() != TargetMode.LastTargetedCharacters) ||
                effects.Skip(1).Any(effect => effect.GetParamInt() != 5) || statuses.Length != 1 || statuses[0].statusId != "pyregel" ||
                statuses[0].count != 1 || traits.Length != 0)
                throw new InvalidOperationException($"Original Deathroar definition changed: name={spell.name}, type={spell.GetCardType()}, cost={spell.GetCost()}, effects=[{string.Join(";", effects.Select(effect => $"{effect.GetEffectStateName()}:{effect.GetTargetMode()}:{effect.GetParamInt()}:{effect.GetParamBool()}:{effect.GetAdditionalParamInt()}:{effect.GetParamBool2()}:{string.Join(",", effect.GetParamStatusEffects().Select(status => status.statusId + ":" + status.count))}"))}], traits={traits.Length}.");
            log.LogInfo($"ORIGINAL-DEATHROAR-DEFINITION cost={spell.GetCost()} costType={spell.GetCostType()} effects=[{string.Join(";", effects.Select(effect => $"{effect.GetEffectStateName()}:{effect.GetTargetMode()}:{effect.GetTargetTeamType()}:{effect.GetParamInt()}:{effect.GetParamBool()}:{effect.GetAdditionalParamInt()}:{effect.GetParamBool2()}:{string.Join(",", effect.GetParamStatusEffects().Select(status => status.statusId + ":" + status.count))}"))}].");

            CardData steward = all.FindCardData(StewardId) ?? throw new InvalidOperationException("Train Steward CardData lookup failed.");
            CharacterData unit = steward.GetSpawnCharacterData() ?? throw new InvalidOperationException("Train Steward has no spawn character.");
            Set(steward, "cost", 0);
            Set(unit, "attackDamage", 9999);
            Set(unit, "health", 9999);
            Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", new[] { new StatusEffectStackData { statusId = statuses[0].statusId, count = 3 } });
            foreach (CardState state in cards.GetAllCards(new List<CardState>()).Where(state => state.GetCardDataID() == steward.GetID()))
                state.Setup(steward, save);
            foreach (CardState state in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(state);
            cards.GetDrawPile().Clear();
            cards.GetDiscardPile().Clear();
            cards.GetExhaustedPile().Clear();

            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(StewardId), Add(StewardId), Add(StewardId), Add(CardDataId) };
            if (created.Any(card => card == null || !cards.GetHand().Contains(card)))
                throw new InvalidOperationException("Original Deathroar scenario could not add its controlled cards.");
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-DEATHROAR-PREPARED unchanged damage/status-transfer spell, positive source status and separate floor control.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            for (int index = 0; index < ids.Length; index++)
            {
                if (context.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") != 0 ||
                    !context.Cards.Hand.Any(card => card.InstanceId == ids[index])) continue;
                int room = index == 2 ? 1 : 0;
                if (index == 3)
                {
                    if (source.Spawn.Turn < 1) return null;
                    CombatUnit? target = source.Spawn.Train.Rooms[0].Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player &&
                        !unit.IsPyre && unit.SpawnerCardId == ids[1]);
                    return target == null ? null : new PlayCardAction(ids[index], room, targetUnitId: target.Id, playerPosition: -1);
                }
                int position = index == 1 ? 1 : 0;
                PlayCardAction[] plays = BattleActionModel.EnumerateSupportedPlays(source)
                    .Where(play => play.CardInstanceId == ids[index] && play.RoomIndex == room && play.PlayerPosition == position).ToArray();
                return plays.FirstOrDefault();
            }
            return null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
