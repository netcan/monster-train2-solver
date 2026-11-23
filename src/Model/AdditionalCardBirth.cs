using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class AdditionalCardBirthModel
    {
        internal static UnitBirthResult Apply(TrainCombatState train, RoomCombatState room, CardPlayRule definition,
            UnitCopyCatalog? catalog, int playedCardId, int position, int count)
        {
            CombatContext context = room.Context!;
            if (context.SpawnPoints?.FirstEmpty(room.RoomIndex, CombatTeam.Player) < 0)
                return new UnitBirthResult(new RoomCombatResult(room, RoomOutcome.Exchanged, 0, new List<CombatEvent>()));
            if (count != 1 || context is not { Enchantments: null, LifeLinks: null } ||
                context.Relics?.Any(relic => relic.EffectTypes.Any(effect =>
                    effect != "RelicEffectNull" && effect != RelicUnitSpawnCountModel.Effect)) == true ||
                !RoomSpawnCopyModel.OriginalMirrorsOnly(room) ||
                definition.SpawnUnit is not { Triggers.Count: 0, Modifiers.Graft: UnitGraftRule graft } ||
                context.FindCard(playedCardId) is not
                    { GraftedEquipmentCardId: int originalGraftId, MaskDescriptor: { PermanentGraft: true, Purified: false } } ||
                context.FindCard(originalGraftId) is not { EquippedUnitId: > 0 } originalGraft ||
                originalGraft.DataId != graft.Card.DataId ||
                room.Units.Any(unit => unit.Triggers.Any(trigger => trigger.Kind is "OnSpawn" or "OnUnscaledSpawn" or
                    "OnSpawnNotFromCard" or "OnSpawnLate" or "AfterSpawnEnchant" or "CardMonsterPlayed")))
                return Fail("Additional paid graft births require the original settled Hall birth and callback contract.");
            CardCreationRule? creation = catalog?.Cards.SingleOrDefault(card => card.DataId == definition.DataId);
            CombatStatus? cardless = context.StatusRules.SingleOrDefault(status => status.Id == "cardless");
            if (creation == null || cardless == null || context.NextCardId is <= 0 or >= int.MaxValue - 1)
                return Fail("Additional paid births require captured source-card creation, Cardless and free card identities.");

            int equipmentId = context.NextCardId;
            CardGenerationResult spawner = CardGenerationModel.CloneDetached(context.WithNextCardId(equipmentId + 1),
                creation, playedCardId, keepGraftAlias: true);
            if (!spawner.Supported) return Fail(spawner.UnsupportedReason!);
            return UnitBirthModel.SpawnAdditionalCardUnit(room.WithContext(spawner.Context), definition,
                spawner.AddedCards.Single().InstanceId, position, cardless.WithStacks(1), AttachThenCopy);

            RoomCombatResult AttachThenCopy(RoomCombatState born, int id)
            {
                RoomCombatResult attached = UnitGraftModel.AttachReserved(born, id, equipmentId, graft);
                if (!attached.Supported) return attached;
                RoomCombatState equipped = attached.State!;
                var frame = new TrainCombatState(train.Rooms.Select(item => item.RoomIndex == equipped.RoomIndex
                    ? equipped : item.WithContext(equipped.Context)).ToArray(), train.Movement, train.EnemySlotsPerRoom, equipped.Context);
                UnitCloneResult copied = RoomSpawnCopyModel.PaidBirth(frame, id, catalog, paidGraftSource: true);
                return copied.Supported
                    ? new RoomCombatResult(copied.State!.Rooms.Single(item => item.RoomIndex == equipped.RoomIndex),
                        copied.Outcome == RoomOutcome.Exchanged ? attached.Outcome : copied.Outcome,
                        0, attached.Events.Concat(copied.Events).ToList())
                    : Failure(copied.UnsupportedReason!);
            }
        }

        private static UnitBirthResult Fail(string reason) => new UnitBirthResult(Failure(reason));

        private static RoomCombatResult Failure(string reason) =>
            new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), reason);
    }
}
