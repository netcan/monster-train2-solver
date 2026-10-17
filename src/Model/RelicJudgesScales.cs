using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicJudgesScalesResult
    {
        public CombatContext? Context { get; }
        public IReadOnlyList<int> EligibleCardIds { get; }
        public IReadOnlyList<int> DiscardedCardIds { get; }
        public IReadOnlyList<int> DrawnCardIds { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Context != null;
        internal RelicJudgesScalesResult(CombatContext? context, IReadOnlyList<int>? eligible = null,
            IReadOnlyList<int>? discarded = null, IReadOnlyList<int>? drawn = null, string? error = null)
        {
            Context = context;
            EligibleCardIds = Array.AsReadOnly((eligible ?? Array.Empty<int>()).ToArray());
            DiscardedCardIds = Array.AsReadOnly((discarded ?? Array.Empty<int>()).ToArray());
            DrawnCardIds = Array.AsReadOnly((drawn ?? Array.Empty<int>()).ToArray());
            UnsupportedReason = error;
        }
    }

    public static class RelicJudgesScalesModel
    {
        public const string Effect = "RelicEffectChooseDiscardsDrawReplacementsOnPostOpeningDraw";
        public const string DataId = "3511a804-42be-465c-9d40-56b988c8a940";
        public const string AssetKey = "JudgesScales";

        public static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == Effect).Select(item => item.index).ToArray();
            RelicResourceEffect[] resources = (relic.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
                .Where(rule => rule.Type == Effect).ToArray();
            if (indices.Length != 1 || !indices.SequenceEqual(new[] { 0 }) || resources.Length != 1 ||
                relic.EffectTypes.Count != 1 || relic.DataId != DataId || relic.AssetKey != AssetKey ||
                relic.IsCovenant != false || relic.DisallowedInPlacementPhase != false)
                return "Unmodeled native Judges Scales owner.";
            return Validate(relic, resources[0]);
        }

        public static string? Validate(CombatRelicState relic, RelicResourceEffect resource)
        {
            if (resource.Type != Effect || resource.EffectIndex != 0 || resource.ParamInt != 0 ||
                resource.ParamInt2 != 2 || resource.ParamMinInt != 0 || resource.ParamFloat != 0 || resource.Amount != 0 ||
                resource.CanApplyInPreviewMode || resource.RequiredMutatorId != null || resource.Conditions.Count != 0 ||
                resource.OpeningDraw != null || resource.StartTurnRoomEffect != null || resource.StartTurnHeal != null ||
                resource.CardGeneration != null || resource.UnitTrigger != null || resource.PileStartTurn != null ||
                resource.MoonPhaseStatus != null)
                return "Judges Scales native post-opening discard definition changed.";
            if (relic.DataId != DataId || relic.AssetKey != AssetKey || relic.EffectTypes.Count != 1 ||
                relic.EffectTypes[0] != Effect || relic.DisallowedInPlacementPhase != false)
                return "Unmodeled native Judges Scales owner.";
            return null;
        }

        public static IReadOnlyList<int> EligibleCardIds(CombatContext source, out string? error)
        {
            error = null;
            var eligible = new List<int>();
            foreach (CardToken token in source.Cards.Hand)
            {
                CardInstanceState? instance = source.FindCard(token.InstanceId);
                if (instance?.MaskDescriptor == null)
                {
                    error = "Judges Scales requires branch-owned hand card traits.";
                    return Array.Empty<int>();
                }
                IReadOnlyList<string> traits;
                try { traits = CardBranchMaskModel.Resolve(instance).Traits; }
                catch (InvalidOperationException exception)
                {
                    error = "Judges Scales cannot resolve hand card " + token.InstanceId + ": " + exception.Message;
                    return Array.Empty<int>();
                }
                if (!traits.Contains("CardTraitInfusion", StringComparer.Ordinal) &&
                    !traits.Contains("CardTraitPersistent", StringComparer.Ordinal))
                    eligible.Add(token.InstanceId);
            }
            return Array.AsReadOnly(eligible.ToArray());
        }

        public static RelicJudgesScalesResult Apply(CombatContext source, BattlePlayRules? definitions,
            IReadOnlyList<int>? selectedCardIds = null, bool placement = false, bool preview = false)
        {
            CombatRelicState[] owners = (source.Relics ?? Array.Empty<CombatRelicState>())
                .Where(relic => relic.EffectTypes.Contains(Effect)).ToArray();
            if (owners.Length == 0) return new RelicJudgesScalesResult(source);
            if (owners.Length != 1) return Fail("Judges Scales requires one native owner.");
            CombatRelicState owner = owners[0];
            string? error = Validate(owner);
            if (error != null) return Fail(error);
            string? relicError = RelicModel.Validate(source);
            if (relicError != null) return Fail(relicError);
            if (preview && !owner.ResourceEffects![0].CanApplyInPreviewMode ||
                owner.DisallowedInPlacementPhase == placement)
                return new RelicJudgesScalesResult(source);

            IReadOnlyList<int> initialEligible = EligibleCardIds(source, out error);
            if (error != null) return Fail(error);
            if (initialEligible.Count == 0) return new RelicJudgesScalesResult(source, initialEligible);

            int[] choices = selectedCardIds?.ToArray() ?? new[] { initialEligible[0] };
            CombatContext context = source;
            var discarded = new List<int>();
            foreach (int selected in choices)
            {
                IReadOnlyList<int> eligible = EligibleCardIds(context, out error);
                if (error != null) return Fail(error);
                if (!eligible.Contains(selected)) return Fail("Judges Scales selected a missing or protected hand card.");
                HandRemovalResult removal = HandRemovalModel.Apply(context, 0, 0, 0, definitions,
                    context.OtherPiles, selectedCardIds: new[] { selected }, effectCounterType: null,
                    triggeredByCard: true, placement: placement, preview: preview);
                if (!removal.Supported) return Fail(removal.UnsupportedReason!);
                context = removal.Context!;
                discarded.Add(selected);
                IReadOnlyList<int> remaining = EligibleCardIds(context, out error);
                if (error != null) return Fail(error);
                if (remaining.Count == 0 && discarded.Count < choices.Length)
                    return Fail("Native Judges Scales would stop selecting after the last eligible card.");
            }

            var drawn = new List<int>();
            if (discarded.Count > 0)
            {
                TypedDrawResult replacement = NextDrawUpgradeModel.DrawCards(context, discarded.Count, 0, definitions);
                if (!replacement.Supported) return Fail(replacement.UnsupportedReason!);
                context = replacement.Context!;
                drawn.AddRange(replacement.DrawnCardIds);
            }
            return new RelicJudgesScalesResult(context, initialEligible, discarded, drawn);
        }

        private static RelicJudgesScalesResult Fail(string error) => new(null, error: error);
    }
}
