using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicPileSpawnUpgradeRule
    {
        public string Pile { get; }
        public int Threshold { get; }
        public string Team { get; }
        public string Subtype { get; }
        public CardUpgradeModifier Upgrade { get; }
        public RelicPileSpawnUpgradeRule(string pile, int threshold, string team, string subtype, CardUpgradeModifier upgrade)
        { Pile = pile; Threshold = threshold; Team = team; Subtype = subtype; Upgrade = upgrade; }
    }

    internal static class RelicPileSpawnUpgradeModel
    {
        internal const string Effect = "RelicEffectAddUnitUpgradePerPile";
        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect)
        {
            var rule = effect.PileSpawnUpgrade;
            return owner.IsCovenant.HasValue && owner.DisallowedInPlacementPhase.HasValue && rule != null &&
                rule.Pile is "DeckPile" or "DeckPileTop" or "DeckPileRandom" or "HandPile" or "DiscardPile" or
                    "ExhaustedPile" or "KeepInStandBy" or "EatenPile" or "None" &&
                rule.Team is "Monsters" or "Heroes" or "Both" && rule.Subtype != null &&
                rule.Upgrade != null && !string.IsNullOrEmpty(rule.Upgrade.DataId) && rule.Upgrade.ExternalInteractions.Count == 0 &&
                !effect.CanApplyInPreviewMode && effect.RequiredMutatorId == null
                ? null : "Missing native pile-count birth upgrade definition.";
        }
        internal static int Applications(int visible, int threshold, string targetTeam, string team, bool subtype) =>
            subtype && (team == "None" || targetTeam == "Both" || targetTeam == team) && threshold > 0
                ? Math.Max(0, visible / threshold) : 0;
        internal static int? VisibleCount(CombatContext context, string pile) => pile switch
        {
            "DeckPile" or "DeckPileTop" or "DeckPileRandom" => context.Resources?.VisibleDeckCount,
            "HandPile" => context.Cards.Hand.Count,
            "DiscardPile" => context.Cards.Discard.Count,
            "ExhaustedPile" => context.OtherPiles?.SingleOrDefault(value => value.Name == "Exhausted")?.Cards.Count,
            // GetCardCount's default returns zero, even when these piles contain cards.
            "KeepInStandBy" or "EatenPile" or "None" => 0,
            _ => null
        };
        internal static RoomCombatResult Apply(RoomCombatState source, int unitId, RelicResourceEffect effect, bool deferCallbacks)
        {
            CombatUnit? unit = source.Units.SingleOrDefault(actor => actor.Id == unitId);
            var rule = effect.PileSpawnUpgrade!;
            if (unit == null || source.Context == null) return Fail("Missing pile-count birth upgrade references.");
            var condition = RelicConditionModel.Evaluate(source.Context, effect.Conditions);
            if (!condition.Supported) return Fail(condition.UnsupportedReason!);
            var state = source.WithContext(condition.Context);
            if (!condition.Passed) return Match(state);
            bool subtype = rule.Subtype.Length == 0 ? unit.Team == CombatTeam.Enemy || !unit.IsPyre :
                OriginalAllSubtypesModel.MatchesCharacter(unit, rule.Subtype,
                    OriginalAllSubtypesModel.Active(state.Context, state.Deployment));
            string team = unit.Team == CombatTeam.Player ? "Monsters" : "Heroes";
            if (Applications(1, 1, rule.Team, team, subtype) == 0 || rule.Threshold <= 0) return Match(state);
            int? visible = VisibleCount(state.Context!, rule.Pile);
            if (!visible.HasValue) return Fail("Missing visible birth-upgrade pile count: " + rule.Pile);
            int count = Applications(visible.Value, rule.Threshold, rule.Team, team, subtype);
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>(); var events = new List<CombatEvent>();
            RoomOutcome outcome = RoomOutcome.Exchanged;
            for (int i = 0; i < count; i++)
            {
                // Each native yield applies the same upgrade state separately;
                // multiplying its numeric fields would lose uniqueness and callbacks.
                var applied = RoomCombatModel.ApplyDirectUnitUpgrade(state, unitId, rule.Upgrade, false, "", null, deferCallbacks: deferCallbacks);
                if (!applied.Supported) return applied;
                state = applied.State!; callbacks.AddRange(applied.PendingCallbacks); events.AddRange(applied.Events);
                if (applied.Outcome != RoomOutcome.Exchanged) outcome = applied.Outcome;
            }
            return new RoomCombatResult(state, outcome, 0, events, pendingCallbacks: callbacks);
        }
        private static RoomCombatResult Match(RoomCombatState state) => new(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static RoomCombatResult Fail(string error) => new(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
    }
}
