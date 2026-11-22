#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalOpeningUnitsScenario
    {
        private const string Prefix = "original-opening-units:";
        private static readonly string Modifier = Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? "";
        private static readonly HashSet<string> activatedUnits = new HashSet<string>(StringComparer.Ordinal);
        private static string[] cardIds = Array.Empty<string>();

        internal static bool Enabled => Modifier.StartsWith(Prefix, StringComparison.Ordinal);
        internal static bool ActivateAbilities => Enabled &&
            Environment.GetEnvironmentVariable("MT2_PROBE_OPENING_UNIT_ABILITIES") == "1";
        internal static bool Prepared { get; private set; }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var cards = managers.GetCardManager();
            var combat = managers.GetCombatManager();
            if (!combat.IsPlacementPhase || combat.GetTurnCount() != 0 || cards.GetDiscardPile().Count != 0)
                throw new InvalidOperationException("Expected original placement before recording opening units.");

            cardIds = Modifier.Substring(Prefix.Length).Split(',');
            if (cardIds.Length is < 1 or > 4 || cardIds.Distinct(StringComparer.Ordinal).Count() != cardIds.Length ||
                cardIds.Any(id => !Guid.TryParseExact(id, "D", out Guid parsed) || parsed.ToString() != id))
                throw new InvalidOperationException("An original opening needs one to four distinct native unit IDs.");

            var stock = cards.GetHand().ToArray();
            var all = managers.GetSaveManager().GetAllGameData();
            var definitions = cardIds.Select(id =>
            {
                CardData? card = all.FindCardData(id);
                if (card == null || card.GetID() != id || card.GetCardType() != CardType.Monster ||
                    !card.ShouldAppearInLogbook() || !card.ShouldCountTowardsMastery() || card.IsArchived ||
                    card.GetCostType() != CardData.CostType.Default || card.GetCost() < 0 ||
                    card.IsUnitAbility() || card.IsRoomAbility() || card.GetTraits().Any() || card.GetCardTriggers().Any() ||
                    stock.Any(instance => instance.GetCardDataID() == id))
                    throw new InvalidOperationException("Expected a distinct fixed-cost original logbook unit: " + id);

                CardEffectData[] effects = card.GetEffects().ToArray();
                CardPlayRule rule = BattleActionProbe.Definition(card);
                if (effects.Length != 1 || effects[0].GetEffectStateName() != nameof(CardEffectSpawnMonster) ||
                    effects[0].GetTargetMode() != TargetMode.Room || rule.Effect != "SpawnMonster" ||
                    rule.Summon != null || rule.SpawnUnit?.Modifiers?.IsChampion != false || rule.ExternalInteractions.Count != 0)
                    throw new InvalidOperationException("Expected a single ordinary original unit spawn: " + id);
                if (ActivateAbilities && rule.SpawnUnit?.Ability?.HasAbility != true)
                    throw new InvalidOperationException("Expected an original unit with a native ability: " + id);
                return card;
            }).ToArray();

            foreach (CardData definition in definitions)
                if (cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false) == null)
                    throw new InvalidOperationException("Original unit failed to enter deployment hand: " + definition.name);

            if (cards.GetHand().Count != stock.Length + definitions.Length ||
                stock.Any(card => !cards.GetHand().Contains(card)))
                throw new InvalidOperationException("Original deployment hand lost a stock card.");

            string? roomCorruption = Environment.GetEnvironmentVariable("MT2_PROBE_OPENING_UNIT_CORRUPTION");
            if (roomCorruption != null)
            {
                int amount = int.Parse(roomCorruption, CultureInfo.InvariantCulture);
                RoomManager rooms = managers.GetRoomManager()!;
                for (int index = 0; index < 3; index++)
                {
                    RoomState room = rooms.GetRoom(index);
                    var setup = room.AdjustCorruption(combat, managers.GetRelicManager(), amount, amount,
                        skipTriggersAndNotifications: true);
                    while (setup.MoveNext()) { }
                    if (room.GetCurrentCorruption() != amount)
                        throw new InvalidOperationException("The native opening corruption control did not settle in room " + index);
                }
            }
            Prepared = true;
            log.LogInfo("ORIGINAL-OPENING-UNITS prepared " + definitions.Length + " original units before deployment capture.");
        }

        internal static PlayCardAction? Choose(BattleTurnState decision, PlayCardAction? fallback)
        {
            if (decision.Spawn.Turn != 0)
            {
                if (!ActivateAbilities || activatedUnits.Count == cardIds.Length) return fallback;
                var context = decision.Spawn.Train.Context!;
                var pendingActors = decision.Spawn.Train.Rooms.SelectMany(room => room.Units
                    .Where(unit => unit.Ability?.HasAbility == true)
                    .Select(unit => (Unit: unit, Room: room.RoomIndex, Card: context.FindCard(unit.SpawnerCardId))))
                    .Where(actor => actor.Card != null && cardIds.Contains(actor.Card.DataId) && !activatedUnits.Contains(actor.Card.DataId))
                    .ToDictionary(actor => actor.Unit.Id, actor => (actor.Room, actor.Card!.DataId));
                if (pendingActors.Count == 0) return fallback;
                var activations = BattleActionModel.EnumerateSupportedPlays(decision)
                    .Where(play => pendingActors.ContainsKey(play.ActivatorUnitId)).ToArray();
                PlayCardAction? activation = activations.FirstOrDefault();
                if (activation == null) return fallback;
                var owner = pendingActors[activation.ActivatorUnitId];
                if (activation.TargetSpawnPointIndex >= 0)
                {
                    int activator = activation.ActivatorUnitId;
                    activation = activations.FirstOrDefault(play => play.ActivatorUnitId == activator && play.RoomIndex != owner.Room &&
                        decision.Spawn.Train.Rooms.Any(room => room.RoomIndex == play.RoomIndex && room.Units.All(unit => !unit.IsPyre))) ?? activation;
                }
                activatedUnits.Add(owner.DataId);
                return activation;
            }

            var hand = decision.Spawn.Train.Context!.Cards.Hand;
            if (hand.Any(card => !cardIds.Contains(card.DataId)))
                return BattleActionModel.ChooseUnitAndJunkPlay(decision, card => !cardIds.Contains(card.DataId)) ??
                    throw new InvalidOperationException("The original stock deployment could not finish.");

            foreach (string dataId in cardIds)
            {
                CardToken? card = hand.FirstOrDefault(instance => instance.DataId == dataId);
                if (card == null) continue;
                PlayCardAction? action = BattleActionModel.ChooseUnitAndJunkPlay(decision, instance => instance.InstanceId == card.InstanceId);
                if (action != null) return action;

                BattleActionEnumeration frontier = BattleActionModel.EnumerateActions(decision, card.InstanceId, includeAbilities: false);
                string reasons = string.Join(" | ", frontier.UnsupportedAttempts.Select(attempt => attempt.Reason).Distinct());
                throw new InvalidOperationException("The original unit has no supported deployment action: " + dataId +
                    " (energy=" + decision.Energy + ", frontier=" + frontier.SupportedCandidates.Count +
                    (reasons.Length == 0 ? ")" : ", unsupported=" + reasons + ")"));
            }
            return null;
        }
    }
}
#endif
