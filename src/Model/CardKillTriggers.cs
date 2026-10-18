using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Card OnKill runs inside CheckDeathCardTriggers, after a possible Undying
    // revival and before final death/removal. It is separate from character Slay.
    public static class CardKillTriggerModel
    {
        public static bool Supported(CardLifecycleTrigger trigger) => trigger.Type == "OnKill" &&
            trigger.ExternalInteractions.Count == 0 && trigger.Effects.SequenceEqual(new[] { "CardEffectGainEnergy" }) &&
            trigger.Actions.Count == 1 && trigger.Actions[0].Type == "GainEnergy" &&
            trigger.Actions[0].Target is "None" or "Room" && !trigger.Actions[0].AllowEnemy && !trigger.Actions[0].AllowPlayer &&
            trigger.Actions[0].Value > 0 && trigger.Actions[0].Range == null;

        internal static string? Validate(CardInstanceState card)
        {
            var triggers = card.MaskDescriptor?.KillTriggers;
            if (triggers == null || triggers.Count == 0) return null;
            if (card.MaskDescriptor!.Definition.CardType != "Spell" || triggers.Any(trigger => !Supported(trigger)))
                return "Uncalibrated card OnKill payload; only fixed, targetless spell energy rewards are supported.";
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
            error = EnergyModel.Validate(context);
            if (error != null) return context;
            foreach (var trigger in triggers)
            {
                if (deployment && !trigger.DeploymentAllowed.HasValue)
                { error = "Deployment card OnKill requires captured disallowed-trigger rules."; return context; }
                if (deployment && trigger.DeploymentAllowed == false) continue;
                context = EnergyModel.Apply(context, "GainEnergy", trigger.Actions[0].Value);
                context = context.AfterCardEffects();
            }
            return context;
        }
    }
}
