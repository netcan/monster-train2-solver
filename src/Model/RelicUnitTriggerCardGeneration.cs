using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class RelicUnitTriggerCardGenerationModel
    {
        public const string Effect = "RelicEffectAddBattleCardToHandOnUnitTrigger";
        public const string AddSnackOnKillId = "b6548209-3ccf-4deb-9896-f07d5df3dab9";
        public const string AddSnackOnKillKey = "AddSnackOnKill";
        private static readonly string[] PoolIds =
        {
            "0347419a-2e3a-492d-a054-f6e340be3db2", "0347419a-2e3a-492d-a054-f6e340be3db2",
            "0347419a-2e3a-492d-a054-f6e340be3db2", "0347419a-2e3a-492d-a054-f6e340be3db2",
            "0347419a-2e3a-492d-a054-f6e340be3db2", "e16040b1-eac0-4769-bc89-90bc2eb31944",
            "e16040b1-eac0-4769-bc89-90bc2eb31944", "e16040b1-eac0-4769-bc89-90bc2eb31944",
            "e16040b1-eac0-4769-bc89-90bc2eb31944", "e16040b1-eac0-4769-bc89-90bc2eb31944",
            "9c8d3e46-1758-4ade-a4b4-cd2bf649a4d6", "9c8d3e46-1758-4ade-a4b4-cd2bf649a4d6",
            "9c8d3e46-1758-4ade-a4b4-cd2bf649a4d6", "9c8d3e46-1758-4ade-a4b4-cd2bf649a4d6",
            "b24a8e4f-294f-4d24-9bc2-9c14474d83d1", "b24a8e4f-294f-4d24-9bc2-9c14474d83d1",
            "b24a8e4f-294f-4d24-9bc2-9c14474d83d1", "b24a8e4f-294f-4d24-9bc2-9c14474d83d1",
            "536e6de0-7ccf-412a-9a4e-ec55ab3988e5"
        };

        public static bool IsEffect(string type) => type == Effect;

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect)
        {
            RelicUnitResourceRule? trigger = effect.UnitTrigger;
            CardGenerationRule? cards = effect.CardGeneration;
            RelicConditionState[] conditions = effect.Conditions.ToArray();
            if (owner.DataId != AddSnackOnKillId || owner.AssetKey != AddSnackOnKillKey || owner.IsCovenant != false ||
                owner.DisallowedInPlacementPhase != false || !owner.EffectTypes.SequenceEqual(new[] { Effect }) ||
                effect.EffectIndex != 0 || effect.Type != Effect || effect.CanApplyInPreviewMode ||
                effect.RequiredMutatorId != null || effect.ParamInt != 2 || effect.ParamInt2 != 0 ||
                effect.ParamMinInt != 0 || effect.Amount != 2 || trigger == null || trigger.Trigger != "OnDeath" ||
                trigger.Team != "Heroes" || trigger.Subtype != null || trigger.RequiredStatuses.Count != 0 ||
                conditions.Length != 1 || !ValidCondition(conditions[0]) || cards == null ||
                cards.Destination != "HandPile" || cards.Count != 2 ||
                !cards.Pool.Select(card => card.DataId).SequenceEqual(PoolIds) || cards.SkipDuplicateInHand ||
                cards.RequireHandSpace || cards.CopyModifiers || cards.IgnoreTemporaryModifiers || cards.Upgrade != null ||
                cards.DiscardCopyUpgrades.Count != 0 || cards.ExternalInteractions.Count != 0 ||
                cards.DeterministicPoolSelection || cards.RequireCopyableSource || cards.UpgradeIsPermanent ||
                cards.Permanent != false || cards.SelectedPoolIndex.HasValue || cards.PoolEligibility != null)
                return "Missing or unsupported original AddSnackOnKill payload.";
            return null;
        }

        public static CardGenerationResult ApplyOne(CombatContext source, string ownerId, int effectIndex,
            CombatUnit actor, string trigger, bool alive, int paramInt = 0, bool placement = false, bool preview = false)
        {
            CombatRelicState[] owners = source.Relics?.Where(owner => owner.DataId == ownerId).ToArray() ?? Array.Empty<CombatRelicState>();
            if (owners.Length != 1) return Fail("Unit-trigger card generation requires one original owner.");
            CombatRelicState owner = owners[0];
            RelicResourceEffect[] effects = owner.ResourceEffects?.Where(effect => effect.EffectIndex == effectIndex && IsEffect(effect.Type)).ToArray()
                ?? Array.Empty<RelicResourceEffect>();
            if (effects.Length != 1) return Fail("Missing native unit-trigger card generation effect index.");
            RelicResourceEffect effect = effects[0];
            string? error = Validate(owner, effect);
            if (error != null) return Fail(error);
            if (placement && owner.DisallowedInPlacementPhase == true || preview && !effect.CanApplyInPreviewMode)
                return new CardGenerationResult(source, Array.Empty<CardToken>());

            RelicConditionEvaluation evaluation = RelicConditionModel.Evaluate(source, effect.Conditions);
            if (!evaluation.Supported) return Fail(evaluation.UnsupportedReason!);
            if (!evaluation.Passed || trigger != effect.UnitTrigger!.Trigger ||
                actor.Team != CombatTeam.Enemy || trigger == "OnDeath" && alive && paramInt != 1)
                return new CardGenerationResult(evaluation.Context, Array.Empty<CardToken>());

            CardGenerationResult generated = CardGenerationModel.Apply(evaluation.Context, effect.CardGeneration!);
            if (!generated.Supported) return generated;
            CombatContext context = generated.Context!;
            int notifications = generated.AddedCards.Count > 0 ? 2 : 1;
            for (int index = 0; index < notifications; index++)
            {
                CombatRelicState current = context.Relics!.Single(item => item.DataId == ownerId);
                RelicResourceEffect currentEffect = current.ResourceEffects!.Single(item => item.EffectIndex == effectIndex);
                RelicConditionRecord recorded = RelicConditionModel.Record(context, currentEffect.Conditions);
                if (!recorded.Supported) return Fail(recorded.UnsupportedReason!);
                CombatRelicState replaced = current.WithResourceEffects(current.ResourceEffects!.Select(item =>
                    item.EffectIndex == effectIndex ? item.WithConditions(recorded.Conditions) : item).ToArray());
                context = recorded.Context.WithRelics(recorded.Context.Relics!.Select(item =>
                    item.DataId == ownerId ? replaced : item).ToArray());
            }
            return new CardGenerationResult(context, generated.AddedCards);
        }

        private static bool ValidCondition(RelicConditionState condition)
        {
            CardStatisticQuery query = condition.Query;
            return condition.TrackTriggerCount && condition.Comparator == 6 && condition.Value == 1 &&
                !condition.AllowMultiple && query.Type == "SubtypeInDeck" && query.Duration == "ThisTurn" &&
                query.TypeMatchDataIds == null && query.SubtypeMatchDataIds == null && query.SubtypeIsNone &&
                query.Subtype == null && query.SpecificCardDataId == null && query.SourceRawCost == null &&
                !query.VariableCost && query.TargetTeam == "None" && !query.CurrentCostCardModifiersOnly &&
                !query.CurrentCostSourceIsSpell;
        }

        private static CardGenerationResult Fail(string error) => new(null, Array.Empty<CardToken>(), error);
    }
}
