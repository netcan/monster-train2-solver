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
            bool copyOnBirth = RoomSpawnCopyModel.Active(room);
            UnitGraftRule? graft = definition.SpawnUnit?.Modifiers?.Graft;
            if (count != 1 || context is not { Enchantments: null, LifeLinks: null } ||
                context.Relics?.Any(relic => relic.EffectTypes.Any(effect =>
                    effect != "RelicEffectNull" && effect != RelicUnitSpawnCountModel.Effect &&
                    (copyOnBirth || !CanApplyFirstUnitShield(relic)))) == true ||
                copyOnBirth && !RoomSpawnCopyModel.OriginalMirrorsOnly(room) ||
                definition.SpawnUnit is not { Triggers.Count: 0 } ||
                room.Units.Any(unit => unit.Triggers.Any(trigger => trigger.Kind is "OnSpawn" or "OnUnscaledSpawn" or
                    "OnSpawnNotFromCard" or "OnSpawnLate" or "AfterSpawnEnchant" or "CardMonsterPlayed")))
                return Fail("Additional paid births require an ordinary primary birth and supported callback contract.");
            if (graft != null && (!copyOnBirth ||
                context.FindCard(playedCardId) is not
                    { GraftedEquipmentCardId: int originalGraftId, MaskDescriptor: { PermanentGraft: true, Purified: false } } ||
                context.FindCard(originalGraftId) is not { EquippedUnitId: > 0 } originalGraft ||
                originalGraft.DataId != graft.Card.DataId))
                return Fail("Additional paid graft births require the original settled Hall graft contract.");
            CardCreationRule? creation = catalog?.Cards.SingleOrDefault(card => card.DataId == definition.DataId);
            CombatStatus? cardless = context.StatusRules.SingleOrDefault(status => status.Id == "cardless");
            if (creation == null || cardless == null || context.NextCardId is <= 0 or >= int.MaxValue - 1)
                return Fail("Additional paid births require captured source-card creation, Cardless and free card identities.");

            int equipmentId = graft == null ? 0 : context.NextCardId;
            CombatContext source = equipmentId == 0 ? context : context.WithNextCardId(equipmentId + 1);
            CardGenerationResult spawner = CardGenerationModel.CloneDetached(source, creation, playedCardId, keepGraftAlias: graft != null);
            if (!spawner.Supported) return Fail(spawner.UnsupportedReason!);
            return UnitBirthModel.SpawnAdditionalCardUnit(room.WithContext(spawner.Context), definition,
                spawner.AddedCards.Single().InstanceId, position, cardless.WithStacks(1),
                copyOnBirth ? AttachThenCopy : null);

            RoomCombatResult AttachThenCopy(RoomCombatState born, int id)
            {
                RoomCombatResult attached = graft == null ? new RoomCombatResult(born, RoomOutcome.Exchanged, 0, new List<CombatEvent>()) :
                    UnitGraftModel.AttachReserved(born, id, equipmentId, graft);
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

        private static bool CanApplyFirstUnitShield(CombatRelicState owner) =>
            owner is { IsCovenant: false, DisallowedInPlacementPhase: false,
                EffectTypes: [RelicModel.AddStatusOnSpawn],
                SpawnStatuses: [{ TargetPlayer: true, TargetEnemy: false, SubtypeIsNone: true, SubtypeIsPyre: false,
                    ExcludedSubtypes.Count: 0, RestrictedRoom: null, HpPercentAsStacks: 0, SourceKind: "",
                    RequireGraft: false, ApplyToTeam: false, CharacterAssetKeys.Count: 0,
                    EnergyGain: 0, DrawCount: 0, Statuses: [{ Id: "damage shield", Stacks: 2 }],
                    Conditions: [{ TrackTriggerCount: true, Comparator: 6, Value: 1, AllowMultiple: false,
                        Query: { Type: "SubtypeInDeck", Duration: "ThisTurn", SubtypeIsNone: true } }] }] };

        private static UnitBirthResult Fail(string reason) => new UnitBirthResult(Failure(reason));

        private static RoomCombatResult Failure(string reason) =>
            new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), reason);
    }
}
