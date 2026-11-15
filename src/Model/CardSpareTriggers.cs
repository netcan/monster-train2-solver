using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // CardEffectDamage calls OnSpare after damage/Undying have returned, using
    // the surviving target. It is separate from character hit and card kill.
    public static class CardSpareTriggerModel
    {
        public static bool Supported(CardLifecycleTrigger trigger)
        {
            if (trigger.Type != "OnSpare" || trigger.ExternalInteractions.Count != 0) return false;
            if (trigger.Effects.SequenceEqual(new[] { "CardTriggerEffectBuffSpellDamage" }))
                return trigger.Actions.Count == 1 && trigger.Actions[0] is { Type: "BuffSourceSpellDamage", Target: "Self",
                    Value: > 0, Lifetime: "SingleRun", Range: null, AllowEnemy: false, AllowPlayer: false };
            return trigger.Effects.SequenceEqual(new[] { "CardEffectRequireTriggeredCharacterTeam", "CardEffectGainEnergy" }) &&
                trigger.Actions.Count == 2 && trigger.Actions[0] is { Type: "RequireTriggeredTeam", Target: "Self", Range: null } gate &&
                (gate.AllowEnemy || gate.AllowPlayer) && gate.Tests?.ShouldTest == true &&
                gate.Tests.CancelSubsequent && trigger.Actions[1] is { Type: "GainEnergy", Target: "Self", Value: > 0, Range: null };
        }

        public static string? Validate(CardInstanceState card)
        {
            var triggers = card.MaskDescriptor?.SpareTriggers;
            if (triggers == null || triggers.Count == 0) return null;
            if (card.MaskDescriptor!.Definition.CardType != "Spell" || triggers.Any(trigger => !Supported(trigger)) ||
                card.MaskDescriptor.Definition.MainEffectNames.Any(type => type != "CardEffectDamage"))
                return "Card OnSpare requires calibrated ordinary damage spells and captured source-growth or team-gated energy payloads.";
            return null;
        }

        internal static CombatContext Apply(CombatContext context, CardInstanceState card, CombatUnit survivor,
            bool preview, bool deployment, out string? error)
        {
            error = Validate(card);
            if (error != null || preview || survivor.Health <= 0 || context.AllScenarioBossesDead == true) return context;
            foreach (CardLifecycleTrigger trigger in card.MaskDescriptor?.SpareTriggers ?? Array.Empty<CardLifecycleTrigger>())
            {
                if (deployment && !trigger.DeploymentAllowed.HasValue)
                { error = "Deployment card OnSpare requires captured deployment-trigger rules."; return context; }
                if (deployment && trigger.DeploymentAllowed == false) continue;
                foreach (CardActionEffect effect in trigger.Actions)
                {
                    if (effect.Type == "RequireTriggeredTeam")
                    {
                        if (!(survivor.Team == CombatTeam.Player ? effect.AllowPlayer : effect.AllowEnemy)) break;
                    }
                    else if (effect.Type == "BuffSourceSpellDamage")
                    {
                        CardInstanceState current = context.FindCard(card.InstanceId) ?? card;
                        context = context.WithCard(current.WithModifiers(current.Permanent.WithAdditionalDamage(effect.Value), current.Temporary));
                    }
                    else
                    {
                        error = EnergyModel.Validate(context);
                        if (error != null) return context;
                        context = EnergyModel.Apply(context, "GainEnergy", effect.Value);
                    }
                }
            }
            return context;
        }
    }
}
