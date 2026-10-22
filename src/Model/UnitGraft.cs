using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class UnitGraftRule
    {
        public CardCreationRule Card { get; }
        public EquipmentDefinition Equipment { get; }
        public UnitGraftRule(CardCreationRule card, EquipmentDefinition equipment)
        { Card = card; Equipment = equipment; }
    }
    internal static class UnitGraftModel
    {
        internal static RoomCombatResult Apply(RoomCombatState source, int unitId)
        {
            CombatUnit? unit = source.Units.SingleOrDefault(value => value.Id == unitId);
            UnitGraftRule? rule = unit?.Modifiers?.Graft;
            if (rule == null) return new RoomCombatResult(source, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
            CombatContext context = source.Context!;
            CardInstanceState? spawner = context.FindCard(unit!.SpawnerCardId);
            if (source.Preview || spawner?.MaskDescriptor == null)
                return Fail("Grafted birth requires a captured primary source card.");
            if (spawner.MaskDescriptor.Purified && !spawner.GraftedEquipmentCardId.HasValue)
                return new RoomCombatResult(source, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
            int id;
            if (spawner.GraftedEquipmentCardId.HasValue) id = spawner.GraftedEquipmentCardId.Value;
            else
            {
                CardGenerationResult created = CardGenerationModel.CreateDetached(context, rule.Card);
                if (!created.Supported) return Fail(created.UnsupportedReason!);
                context = created.Context!; id = created.AddedCards.Single().InstanceId;
                context = context.WithCard(spawner.WithGraftedEquipment(id));
            }
            CardInstanceState? card = context.FindCard(id);
            if (card != null && !CardGraftedEquipmentModel.Present(card) && spawner.DataId == CardGraftedEquipmentModel.EventHostId &&
                rule.Card.DataId == CardGraftedEquipmentModel.EventEquipmentId && card.DataId == rule.Card.DataId)
            {
                try { card = CardGraftedEquipmentModel.AttachEventTrait(card); }
                catch (InvalidOperationException error) { return Fail(error.Message); }
                context = context.WithCard(card);
            }
            if (card == null || !CardGraftedEquipmentModel.Present(card))
                return Fail("Grafted birth requires a calibrated equipment trait and source reference.");
            if (card.MaskDescriptor!.BaseTraits.Concat(card.MaskDescriptor.TemporaryTraits).Any(trait => trait.GraftSevered == true))
                return new RoomCombatResult(source, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
            if (card.EquippedUnitId > 0) return Fail("Duplicated graft equipment requires independent native calibration.");
            if (context.CardInstances!.All(value => value.InstanceId != id))
                context = context.WithCardInstances(context.CardInstances.Concat(new[] { card }).ToArray());
            var definition = new CardPlayRule(card.DataId, rule.Card.DataId, 0, "Equipment", "Standby", null,
                Array.Empty<string>(), equipment: rule.Equipment, cardType: "Equipment");
            RoomCombatResult equipped = RoomCombatModel.ApplyEquipment(source.WithContext(context), unitId, id,
                new BattlePlayRules(Array.Empty<RoomPlayRule>(), new[] { definition }), grafted: true);
            if (!equipped.Supported) return equipped;
            context = EquipmentModel.BindTransferred(equipped.State!.Context!, id, unitId);
            return new RoomCombatResult(equipped.State.WithContext(context), equipped.Outcome, 0, equipped.Events.ToList(),
                pendingCallbacks: equipped.PendingCallbacks);
        }
        private static RoomCombatResult Fail(string error) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0,
            new List<CombatEvent>(), error);
    }
}
