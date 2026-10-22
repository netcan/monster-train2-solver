using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class TrueFinalBossOrdinaryAllyScenario
    {
        internal sealed class Record
        {
            public string Kind { get; set; } = "";
            public int AnchorCardId { get; set; }
            public int CardId { get; set; }
            public string CardDataId { get; set; } = "";
            public string UpgradeKind { get; set; } = "";
            public int CommandCardId { get; set; }
            public string CommandCardDataId { get; set; } = "";
            public int CommandDrawIndex { get; set; } = -1;
            public CardPlayRule? CommandDefinition { get; set; }
            public CardPlayRule Definition { get; set; } = null!;
            public BattleTurnState Before { get; set; } = null!;
            public BattleTurnState Actual { get; set; } = null!;
        }
        internal static string Kind => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_ORDINARY_ALLY") ?? "";
        internal static bool Enabled => Kind.Length > 0;
        internal static string UpgradeKind => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_ORDINARY_ALLY_UPGRADE") ?? "";
        internal static Record? Evidence { get; private set; }
        internal static bool Prepare()
        {
            if (!Enabled || Evidence != null) return false;
            AllGameManagers managers = AllGameManagers.Instance;
            if (!TrueFinalBossTurnProbe.Enabled || managers.GetSaveManager().PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Ordinary ally calibration requires isolated original TFB deployment.");
            CardManager cards = managers.GetCardManager();
            FullBattleTrace trace = FullBattleTrace.Active!;
            string id = Kind == "Steward" ? "38e70e6a-20f3-4e36-8d31-c5110be07f87" : Kind == "Warrior" ?
                "5de24977-6800-4015-a20e-9949e79e057c" : Kind == "Animus" ? "e66178fd-b8e7-401c-b570-7832840ea902" :
                Kind == "Avenger" ? "69fcbe85-a5b7-4a83-bf6d-21888604d99e" : throw new InvalidOperationException("Unknown original ally.");
            var row = new Record { Kind = Kind, UpgradeKind = UpgradeKind, CardDataId = id, Before = trace.CaptureDecision(),
                AnchorCardId = trace.CardId(cards.GetHand().First(card => card.GetSpawnCharacterData()?.GetAssetKey() == "TrainStewardBig")) };
            CardData data = managers.GetSaveManager().GetAllGameData().FindCardData(id)!;
            row.Definition = BattleActionProbe.Definition(data);
            CardState added = cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false);
            foreach (string kind in UpgradeKind == "Both" ? new[] { "Health", "Damage" } : UpgradeKind.Length == 0 ? Array.Empty<string>() : new[] { UpgradeKind })
            {
                var upgrade = new CardUpgradeState();
                upgrade.Setup(TrueFinalBossAllyUpgradeScenario.Definition(managers.GetSaveManager().GetAllGameData(), kind));
                added.ApplyPermanentUpgrade(upgrade, managers.GetSaveManager(), ignoreUpgradeAnimation: true);
            }
            row.CardId = trace.CardId(added);
            if (TrueFinalBossLethalScenario.CommandedAttack || TrueFinalBossLethalScenario.PyreCommand)
            {
                bool pyre = TrueFinalBossLethalScenario.PyreCommand;
                CardData command = managers.GetSaveManager().GetAllGameData().FindCardData(pyre ? OriginalPyreshotScenario.CardDataId :
                    "1737fa0b-7f37-41e8-9183-ed9e6a391556")!;
                if (command.name != (pyre ? "Pyreshot" : "Eagleheart") || command.GetCost() != (pyre ? 3 : 2) || command.GetCardType() != CardType.Spell ||
                    !command.GetEffects().Select(effect => effect.GetEffectStateName()).SequenceEqual(pyre ? new[] { "CardEffectAttackWithUnit" } : new[] {
                        "CardEffectFloorRearrange", "CardEffectAddStatusEffect", "CardEffectAttackWithUnit" }))
                    throw new InvalidOperationException("Original command signature changed.");
                CardState spell = cards.AddNewCard(command, CardPile.HandPile, false, false, animate: false);
                row.CommandCardId = trace.CardId(spell); row.CommandCardDataId = command.GetID();
                row.CommandDefinition = BattleActionProbe.Definition(command);
                cards.MoveCardFromHandToTopOfDeck(spell);
                // Deployment reshuffles draw + discarded hand before the first
                // normal draw. Arrange only the initial pile, preserving RNG.
                BattleTurnState prepared = trace.CaptureDecision();
                BattleTurnState summoned = BattleActionModel.EnumerateActions(prepared, row.CardId, includeAbilities: false)
                    .SupportedCandidates.First(candidate => candidate.Action.RoomIndex == 0 && candidate.Action.PlayerPosition == 0).Result.State!;
                CardCycleState cycle = CardCycleModel.DiscardHand(summoned.Spawn.Train.Context!.Cards).State!;
                int count = cycle.Draw.Count + cycle.Discard.Count;
                row.CommandDrawIndex = cycle.Rng.Shuffle(Enumerable.Range(0, count).ToArray()).Items
                    .Skip(count - 5).Last(index => index < cycle.Draw.Count);
                var deck = (List<CardState>)AccessTools.Field(typeof(CardManager), "pileDeck").GetValue(cards);
                int originalIndex = deck.IndexOf(spell);
                CardState displaced = deck[row.CommandDrawIndex];
                deck[row.CommandDrawIndex] = spell; deck[originalIndex] = displaced;
            }
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            row.Actual = trace.CaptureDecision();
            Evidence = row;
            return true;
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            bool terminal = TrueFinalBossLethalScenario.PaidSpell || TrueFinalBossLethalScenario.OrdinaryCombat;
            foreach (int id in terminal ? new[] { Evidence!.CardId } : new[] { Evidence!.AnchorCardId, Evidence.CardId })
                if (state.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0)
                    return BattleActionModel.EnumerateActions(state, id, includeAbilities: false).SupportedActions
                        .First(action => action.RoomIndex == 0 && action.PlayerPosition == (!terminal && id == Evidence.CardId ? 1 : 0));
            return null;
        }
    }
}
