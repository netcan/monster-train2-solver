using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicPactDrawRule
    {
        public string MainClanId { get; }
        public string SubClanId { get; }
        public bool MainPlayed { get; }
        public bool SubPlayed { get; }
        public bool BonusScheduled { get; }
        public bool DrawNextTurn { get; }

        public RelicPactDrawRule(string mainClanId, string subClanId, bool mainPlayed, bool subPlayed, bool bonusScheduled,
            bool drawNextTurn)
        {
            MainClanId = mainClanId; SubClanId = subClanId; MainPlayed = mainPlayed; SubPlayed = subPlayed;
            BonusScheduled = bonusScheduled; DrawNextTurn = drawNextTurn;
        }
    }

    internal static class RelicPactDrawModel
    {
        internal const string Effect = "RelicEffectDrawCardAfterEachClanPlayed";
        internal const string OwnerId = "17ed1b69-ffb7-436c-b87c-98c07aba0242";
        internal const string OwnerKey = "PyreHeartPact_ArtifactData";

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect) =>
            owner.DataId == OwnerId && owner.AssetKey == OwnerKey && owner.EffectTypes is [Effect] &&
            owner.IsCovenant == false && owner.DisallowedInPlacementPhase.HasValue && effect.EffectIndex == 0 &&
            effect.ParamInt2 == 1 && effect.Amount == 1 && !effect.CanApplyInPreviewMode && effect.RequiredMutatorId == null &&
            effect.Conditions.Count == 0 && effect.PactDraw is { DrawNextTurn: true } state &&
            !string.IsNullOrEmpty(state.MainClanId) && !string.IsNullOrEmpty(state.SubClanId) &&
            state.BonusScheduled == (state.MainPlayed && state.SubPlayed) ? null : "Missing or changed original Pact Pyre draw state.";

        internal static RelicFirstCardCostResult Played(CombatContext context, int ownerIndex, int cardId, CardPlayRule? card,
            bool placement, bool preview)
        {
            CombatRelicState owner = context.Relics![ownerIndex];
            if (preview || placement && owner.DisallowedInPlacementPhase == true) return new(context);
            RelicResourceEffect effect = owner.ResourceEffects!.Single(rule => rule.Type == Effect);
            RelicPactDrawRule state = effect.PactDraw!;
            if (state.BonusScheduled || card?.IsAnyAbility == true) return new(context);
            if (card?.IsAnyAbility != false || context.FindCard(cardId)?.MaskDescriptor is not CardMaskDescriptor descriptor)
                return new(context, "Pact Pyre draw requires original linked clan and ability classification.");
            string? clan = descriptor.Definition.LinkedClan;
            bool main = state.MainPlayed || clan == state.MainClanId;
            bool sub = state.SubPlayed || clan == state.SubClanId;
            if (main == state.MainPlayed && sub == state.SubPlayed) return new(context);
            bool schedule = main && sub;
            if (schedule)
            {
                if (context.Cards.BonusDraw == null) return new(context, "Pact Pyre draw requires the captured draw modifier.");
                context = context.WithCards(BonusDrawModel.Schedule(context.Cards, "", effect.Amount, null));
            }
            return new(Replace(context, ownerIndex, effect.WithPactDraw(new(state.MainClanId, state.SubClanId, main, sub, schedule, true))));
        }

        internal static CombatContext Reset(CombatContext context)
        {
            for (int index = 0; index < (context.Relics?.Count ?? 0); index++)
            {
                RelicResourceEffect? effect = context.Relics![index].ResourceEffects?.SingleOrDefault(rule => rule.Type == Effect);
                if (effect?.PactDraw is not { } state || !state.MainPlayed && !state.SubPlayed) continue;
                context = Replace(context, index, effect.WithPactDraw(new(state.MainClanId, state.SubClanId, false, false, false, true)));
            }
            return context;
        }

        private static CombatContext Replace(CombatContext context, int ownerIndex, RelicResourceEffect updated)
        {
            CombatRelicState[] owners = context.Relics!.ToArray();
            owners[ownerIndex] = owners[ownerIndex].WithResourceEffects([updated]);
            return context.WithRelics(owners);
        }
    }
}
