using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Card OnKill runs inside CheckDeathCardTriggers, after a possible Undying
    // revival and before final death/removal. It is separate from character Slay.
    public static class CardKillTriggerModel
    {
        public static bool Supported(CardLifecycleTrigger trigger)
        {
            if (trigger.Type != "OnKill" || trigger.ExternalInteractions.Count != 0 || trigger.Actions.Count != 1 ||
                trigger.Actions[0].AllowEnemy || trigger.Actions[0].AllowPlayer || trigger.Actions[0].Range != null) return false;
            CardActionEffect effect = trigger.Actions[0];
            if (trigger.Effects.SequenceEqual(new[] { "CardEffectGainEnergy" }))
                return effect.Type == "GainEnergy" && effect.Target is "None" or "Room" && effect.Value > 0;
            CardGenerationRule? generation = effect.Generation;
            return trigger.Effects.SequenceEqual(new[] { "CardEffectAddBattleCard" }) && effect.Type == "Generate" && effect.Target == "Hand" &&
                generation != null && generation.Destination == "HandPile" && generation.Count > 0 && generation.Pool.Count > 0 &&
                generation.ExternalInteractions.Count == 0 && generation.Permanent != true && generation.Upgrade == null &&
                !generation.CopyModifiers && !generation.RequireCopyableSource && generation.OnlyUnequippedTargets == null;
        }

        internal static string? Validate(CardInstanceState card)
        {
            var triggers = card.MaskDescriptor?.KillTriggers;
            if (triggers == null || triggers.Count == 0) return null;
            if (card.MaskDescriptor!.Definition.CardType != "Spell" || triggers.Any(trigger => !Supported(trigger)))
                return "Uncalibrated card OnKill payload; only fixed spell energy rewards and hand-pool generation are supported.";
            return null;
        }

        internal static CombatContext Apply(CombatContext context, CardInstanceState card, bool preview, bool deployment,
            int attackerUnitId, out string? error)
        {
            error = Validate(card);
            if (error != null) return context;
            var triggers = card.MaskDescriptor?.KillTriggers;
            if (triggers == null || triggers.Count == 0) return context;
            if (attackerUnitId > 0)
            { error = "Character-attributed card OnKill requires captured trigger fire-count rules."; return context; }
            if (preview || context.AllScenarioBossesDead == true) return context;
            foreach (var trigger in triggers)
            {
                if (deployment && !trigger.DeploymentAllowed.HasValue)
                { error = "Deployment card OnKill requires captured disallowed-trigger rules."; return context; }
                if (deployment && trigger.DeploymentAllowed == false) continue;
                CardActionEffect effect = trigger.Actions[0];
                if (effect.Type == "GainEnergy")
                {
                    error = EnergyModel.Validate(context);
                    if (error != null) return context;
                    context = EnergyModel.Apply(context, "GainEnergy", effect.Value);
                }
                else if (effect.Generation!.RequireHandSpace != true || context.Cards.Hand.Count < context.MaxHandSize)
                {
                    CardGenerationResult generated = CardGenerationModel.Apply(context, effect.Generation, card.InstanceId);
                    if (!generated.Supported) { error = generated.UnsupportedReason; return context; }
                    context = generated.Context!;
                }
                context = context.AfterCardEffects();
            }
            return context;
        }
    }
}
