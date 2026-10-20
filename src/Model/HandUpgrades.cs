using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class HandUpgradeModel
    {
        public static RoomCombatResult Apply(RoomCombatState source, CardUpgradeModifier upgrade, string lifetime,
            BattlePlayRules? definitions, IReadOnlyList<CardUpgradeMaskRule>? filters = null)
        {
            string? validation = RoomCombatModel.Validate(source);
            if (validation != null) return Unsupported(validation);
            if (source.Context?.CardInstances == null || definitions == null)
                return Unsupported("Hand upgrades require card instance state and trait definitions.");
            if (upgrade.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", upgrade.ExternalInteractions));
            if (lifetime != "Permanent" && lifetime != "TemporaryUntilEndOfBattle")
                return Unsupported("Unimplemented hand upgrade lifetime.");
            CombatContext context = source.Context;
            foreach (CardToken token in context.Cards.Hand)
            {
                CardInstanceState? card = context.FindCard(token.InstanceId);
                if (card == null || card.DataId != token.DataId)
                    return Unsupported("Missing or mismatched upgraded card instance.");
                if (filters?.Count > 0)
                {
                    if (card.MaskDescriptor == null) return Unsupported("Hand upgrade filters require branch-owned card metadata.");
                    CardUpgradeMaskCard mask;
                    try { mask = CardBranchMaskModel.Resolve(card); }
                    catch (InvalidOperationException error) { return Unsupported(error.Message); }
                    bool allSubtypes = context.Relics?.Any(relic => relic.EffectTypes.Contains("RelicEffectMonstersAreAllSubtypes")) == true;
                    if (!filters.All(filter => CardUpgradeMaskModel.FilterCard(filter, mask, allSubtypes))) continue;
                }
                CardPlayRule? rule = definitions.Cards.FirstOrDefault(item => item.DataId == card.DataId);
                if (rule == null) return Unsupported("Missing upgraded card trait definition.");
                var interactions = rule.UpgradeInteractions ?? rule.ExternalInteractions;
                if (interactions.Count > 0) return Unsupported(string.Join("; ", interactions));
                string? exhaustError = lifetime == "Permanent" ? CardExhaustModel.PermanentUpgradeError(card, upgrade) : null;
                if (exhaustError != null) return Unsupported(exhaustError);
                // The CardUpgradeData overload constructs a distinct object for
                // each hand card; only existing queue originals can be shared.
                CardUpgradeModifier created = upgrade.WithSharedReference(null);
                CardUpgradeModifier applied = lifetime == "Permanent" ? CardMagicPowerModel.Scale(created, CardMagicPowerModel.Multiplier(card)) : created;
                context = CardMagicPowerModel.Refresh(context, card.WithModifiers(
                    lifetime == "Permanent" ? UnitModifierModel.Add(card.Permanent, applied) : card.Permanent,
                    lifetime == "TemporaryUntilEndOfBattle" ? UnitModifierModel.Add(card.Temporary, applied) : card.Temporary), out _);
            }
            return new RoomCombatResult(new RoomCombatState(source.RoomIndex, source.Deployment, source.Units,
                source.ExternalInteractions, context, source.Preview, source.ActiveCompanionBossIds),
                RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        }
        private static RoomCombatResult Unsupported(string reason) =>
            new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), reason);
    }
}
