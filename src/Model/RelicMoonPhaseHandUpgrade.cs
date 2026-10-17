using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal sealed class RelicMoonPhaseHandUpgradeResult
    {
        internal CombatContext Context { get; }
        internal string? UnsupportedReason { get; }
        internal bool Supported => UnsupportedReason == null;
        internal RelicMoonPhaseHandUpgradeResult(CombatContext context, string? unsupportedReason = null)
        { Context = context; UnsupportedReason = unsupportedReason; }
    }

    internal static class RelicMoonPhaseHandUpgradeModel
    {
        internal const string OwnerId = "e96497a1-3c1e-4adf-b897-8ef09b931d1d";
        internal const string OwnerKey = "MoonPhaseRoomsGainMagicPower";

        internal static RelicMoonPhaseHandUpgradeResult Apply(CombatContext source, int moonPhase,
            bool deployment, bool preview)
        {
            CombatContext context = source;
            IReadOnlyList<CombatRelicState>? relics = context.Relics;
            if (relics == null) return new RelicMoonPhaseHandUpgradeResult(context);
            string? validation = RelicModel.Validate(relics);
            if (validation != null) return Fail(context, validation);
            for (int relicIndex = 0; relicIndex < relics.Count; relicIndex++)
            {
                CombatRelicState relic = relics[relicIndex];
                for (int effectIndex = 0; effectIndex < relic.EffectTypes.Count; effectIndex++)
                {
                    if (relic.EffectTypes[effectIndex] != RelicModel.ApplyCardUpgradeToHandEndOfTurnOnMoon) continue;
                    RelicCardModifier effect = relic.CardModifiers!.Single(item => item.EffectIndex == effectIndex);
                    RelicMoonPhaseHandUpgrade rule = effect.MoonPhaseHandUpgrade!;
                    if (relic.DisallowedInPlacementPhase == true && deployment || preview && !rule.CanApplyInPreviewMode ||
                        (rule.MoonPhaseMask & moonPhase) == 0) continue;

                    foreach (CardToken token in context.Cards.Hand)
                    {
                        CardInstanceState? card = context.FindCard(token.InstanceId);
                        if (card == null || card.DataId != token.DataId)
                            return Fail(context, "Moon-phase hand upgrade requires matching owned card instances.");
                        if (rule.Filters.Count > 0)
                        {
                            if (card.MaskDescriptor == null || card.MaskDescriptor.Definition.DataId != card.DataId)
                                return Fail(context, "Moon-phase hand upgrade filters require matching card mask metadata.");
                            CardUpgradeMaskCard mask;
                            try { mask = CardBranchMaskModel.Resolve(card); }
                            catch (InvalidOperationException error) { return Fail(context, error.Message); }
                            if (rule.Filters.Any(filter => !CardUpgradeMaskModel.FilterCard(filter, mask))) continue;
                        }

                        CardInstanceState changed = card;
                        bool uniqueExists = rule.Upgrade.Unique && card.Temporary.Upgrades.Any(item => item.DataId == rule.Upgrade.DataId);
                        if (!uniqueExists)
                        {
                            CardModifiers temporary = new CardModifiers(card.Temporary.Offsets,
                                card.Temporary.Upgrades.Concat(new[] { rule.Upgrade }).ToArray(),
                                card.Temporary.PersistentHealth, card.Temporary.ExternalInteractions);
                            changed = card.WithModifiers(card.Permanent, temporary);
                        }
                        try { context = CardMagicPowerModel.Refresh(context, changed, out _); }
                        catch (InvalidOperationException error) { return Fail(context, error.Message); }
                    }
                }
            }
            return new RelicMoonPhaseHandUpgradeResult(context);
        }

        private static RelicMoonPhaseHandUpgradeResult Fail(CombatContext context, string error) =>
            new RelicMoonPhaseHandUpgradeResult(context, error);
    }
}
