using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class HandUpgradeModel
    {
        public static RoomCombatResult Apply(RoomCombatState source, CardUpgradeModifier upgrade, string lifetime,
            BattlePlayRules? definitions)
        {
            string? validation = RoomCombatModel.Validate(source);
            if (validation != null) return Unsupported(validation);
            if (source.Context?.CardInstances == null || definitions == null)
                return Unsupported("Hand upgrades require card instance state and trait definitions.");
            if (upgrade.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", upgrade.ExternalInteractions));
            if (lifetime != "Permanent" && lifetime != "TemporaryUntilEndOfBattle")
                return Unsupported("Unimplemented hand upgrade lifetime.");
            CombatContext context = source.Context;
            var instances = context.CardInstances.ToDictionary(card => card.InstanceId);
            foreach (CardToken token in context.Cards.Hand)
            {
                if (!instances.TryGetValue(token.InstanceId, out CardInstanceState? card) || card.DataId != token.DataId)
                    return Unsupported("Missing or mismatched upgraded card instance.");
                CardPlayRule? rule = definitions.Cards.FirstOrDefault(item => item.DataId == card.DataId);
                if (rule == null) return Unsupported("Missing upgraded card trait definition.");
                var interactions = rule.UpgradeInteractions ?? rule.ExternalInteractions;
                if (interactions.Count > 0) return Unsupported(string.Join("; ", interactions));
                instances[card.InstanceId] = new CardInstanceState(card.InstanceId, card.DataId,
                    lifetime == "Permanent" ? UnitModifierModel.Add(card.Permanent, upgrade) : card.Permanent,
                    lifetime == "TemporaryUntilEndOfBattle" ? UnitModifierModel.Add(card.Temporary, upgrade) : card.Temporary,
                    card.LastPlayedCost, card.LastForgedAmount, card.PlayCount, card.ExternalInteractions, card.EffectCounters);
            }
            return new RoomCombatResult(new RoomCombatState(source.RoomIndex, source.Deployment, source.Units,
                source.ExternalInteractions, context.WithCardInstances(instances.Values.ToArray()), source.Preview),
                RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        }
        private static RoomCombatResult Unsupported(string reason) =>
            new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), reason);
    }
}
