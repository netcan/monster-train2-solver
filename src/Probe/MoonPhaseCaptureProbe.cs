using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    // Player captures use native signatures and the same transition validator as
    // search. Development fixture switches are not evidence of live capability.
    internal static class MoonPhaseCaptureProbe
    {
        internal static bool Enabled => AdvisorRuntime.PlayerSession || RoomMoonCallbackCalibration.Enabled ||
            // The isolation probe and live advisor compile separate copies of
            // AdvisorRuntime. A resumed campaign has no clan override, so the
            // probe must also read the explicit player-advisor launch setting.
            Environment.GetEnvironmentVariable("MT2_PROBE_PLAYER_ADVISOR") == "1" ||
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MT2_PROBE_PRIMARY_CLAN")) ||
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MT2_PROBE_SECONDARY_CLAN"));
        internal static bool CaptureMasks => Enabled &&
            AllGameManagers.Instance?.GetPlayerManager()?.GetHasMoonPhaseCardEffect() == true;

        private static readonly HashSet<string> Effects = new HashSet<string>(StringComparer.Ordinal)
        {
            "CardEffectAdvanceMoonPhase", "CardEffectSetMoonPhase", "CardEffectAddCardUpgradeToUnitsOnMoonPhase"
        };
        private static readonly HashSet<string> Traits = new HashSet<string>(StringComparer.Ordinal)
        {
            "CardTraitScalingAdvanceMoonPhase", "CardTraitScalingMagicPowerOnMoonPhase", "CardTraitAddCardUpgradeOnMoonPhase"
        };

        internal static string? Validate(AllGameManagers managers, TrainCombatState train)
        {
            CardManager cards = managers.GetCardManager()!;
            foreach (CardState card in managers.GetSaveManager().GetDeckState().Concat(cards.GetHand())
                .Concat(cards.GetDrawPile()).Concat(cards.GetDiscardPile()).Distinct())
            {
                foreach (CardEffectState effect in card.GetEffectStates())
                    if (effect.GetCardEffect() is IMoonPhaseCardEffect moon && !Effects.Contains(moon.GetType().Name))
                        return "Unmodeled native moon-phase effect: " + moon.GetType().Name;
                foreach (CardTraitState trait in card.GetTraitStates())
                    if (trait is IMoonPhaseCardTrait && !Traits.Contains(trait.GetType().Name))
                        return "Unmodeled native moon-phase trait: " + trait.GetType().Name;
            }
            // Includes unit callbacks, prior masks, card upgrades and relics;
            // unknown callback payloads remain unsupported by model validation.
            return MoonPhaseTransitionModel.Validate(train,
                new CardActionEffect("AdvanceMoonPhase", "Room", 1, false, false, Array.Empty<CombatStatus>()),
                0, playerActionOnly: false);
        }
    }
}
