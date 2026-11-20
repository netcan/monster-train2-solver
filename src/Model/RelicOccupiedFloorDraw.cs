using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicOccupiedFloorDrawModel
    {
        internal const string Effect = "RelicEffectAdditionalDrawForEnemyOccupiedFloors";
        internal static bool Has(CombatContext? context) => context?.Relics?.Any(owner => owner.EffectTypes.Contains(Effect)) == true;
        internal static string? Validate(CombatRelicState owner, RelicResourceEffect rule) =>
            owner.IsCovenant.HasValue && owner.DisallowedInPlacementPhase.HasValue && rule.Amount == 0 &&
            !rule.CanApplyInPreviewMode && rule.RequiredMutatorId == null && rule.DrawValue.HasValue &&
            !float.IsNaN(rule.DrawValue.Value) && !float.IsInfinity(rule.DrawValue.Value) &&
            rule.DrawValue.Value >= 0 && rule.DrawValue.Value < 2147483648f && rule.DrawValue.Value == Math.Floor(rule.DrawValue.Value) &&
            (!rule.DrawMutationOrder.HasValue || rule.DrawMutationOrder >= 0)
                ? null : "Missing initialized enemy-occupied draw value or registration metadata.";

        internal static RelicResourceResult Count(TrainCombatState train)
        {
            var context = train.Context!; var metadata = context.RoomCapacities;
            if (metadata == null || context.SpawnPoints == null || metadata.Count == 0 ||
                metadata.Select(room => room.RoomIndex).Distinct().Count() != metadata.Count ||
                !metadata.Select(room => room.RoomIndex).OrderBy(id => id).SequenceEqual(train.Rooms.Select(room => room.RoomIndex).OrderBy(id => id)))
                return Fail("Enemy-occupied draw requires complete physical rooms and Pyre metadata.");
            var actors = train.Rooms.SelectMany(room => room.Units).Concat(context.Enchantments?.RetainedUnits.Select(actor => actor.Unit) ?? Array.Empty<CombatUnit>())
                .GroupBy(actor => actor.Id).ToDictionary(group => group.Key, group => group.First());
            int count = 0;
            foreach (var room in metadata.Where(room => !room.IsPyre))
            {
                var group = context.SpawnPoints.SelectedGroup(room.RoomIndex, CombatTeam.Enemy);
                if (group == null) return Fail("Missing enemy-occupied draw spawn points.");
                bool living = false;
                foreach (int id in group.Occupants.Where(id => id > 0).Distinct())
                {
                    if (!actors.TryGetValue(id, out var actor)) return Fail("Missing physically registered enemy draw actor.");
                    // Native includes outer-train anchors but excludes all actors
                    // whose first PostCombat trigger contains DespawnCharacter, even if silenced.
                    if (actor.Health > 0 && actor.DeathState?.IsDespawned != true &&
                        actor.Triggers.FirstOrDefault(trigger => trigger.Kind == "PostCombat")?.Effects.Any(effect => effect.Type == "CardEffectDespawnCharacter") != true)
                        living = true;
                }
                if (living) count++;
            }
            return new RelicResourceResult(context, count);
        }

        internal static RelicResourceResult RefreshOne(TrainCombatState train, int ownerIndex, int effectIndex,
            string callback, CombatUnit? actor = null, bool preview = false, bool placement = false)
        {
            var context = train.Context!; var owner = context.Relics![ownerIndex];
            var effect = owner.ResourceEffects!.Single(rule => rule.EffectIndex == effectIndex);
            string? error = Validate(owner, effect); if (error != null) return Fail(error);
            bool character = callback is "Birth" or "Killed";
            if (!character && callback is not ("EndTurn" or "AfterDraw")) return Fail("Unknown enemy-occupied draw callback.");
            if (character && (actor == null || actor.Team == CombatTeam.Enemy && context.EnergyState == null))
                return Fail("Enemy-occupied character callback requires its actor and current phase.");
            if (character && (actor!.Team != CombatTeam.Enemy || context.EnergyState?.Phase != "MonsterTurn"))
                return new RelicResourceResult(context, 0);
            // CharacterAdded has no preview gate. Killed has no placement gate.
            if ((callback != "Birth" && preview) || (callback != "Killed" && placement && owner.DisallowedInPlacementPhase == true))
                return new RelicResourceResult(context, 0);
            var condition = RelicConditionModel.Evaluate(context, effect.Conditions);
            if (!condition.Supported) return Fail(condition.UnsupportedReason!);
            context = condition.Context;
            if (!condition.Passed) return new RelicResourceResult(context, 0);
            var count = Count(CardSpellModel.WithContext(train, context));
            if (!count.Supported) return count;
            var effects = context.Relics![ownerIndex].ResourceEffects!.Select(rule => rule.EffectIndex == effectIndex ? rule.WithDrawValue(count.Value) : rule).ToArray();
            var owners = context.Relics.ToArray(); owners[ownerIndex] = owners[ownerIndex].WithResourceEffects(effects);
            return new RelicResourceResult(context.WithRelics(owners), count.Value);
        }

        internal static RelicResourceResult RefreshRoomOne(RoomCombatState room, int ownerIndex, int effectIndex,
            string callback, CombatUnit? actor = null)
        {
            var context = room.Context!; var world = context.Enchantments;
            if (world == null) return Fail("Enemy-occupied draw callback requires the complete train world.");
            var train = new TrainCombatState(world.Rooms.Select(floor => floor.RoomIndex == room.RoomIndex ? room :
                new RoomCombatState(floor.RoomIndex, floor.Deployment, floor.Units, floor.ExternalInteractions, context,
                    room.Preview, floor.ActiveCompanionBossIds)).ToArray(), world.Movement, world.EnemySlotsPerRoom, context);
            return RefreshOne(train, ownerIndex, effectIndex, callback, actor, room.Preview, room.Deployment);
        }

        private static RelicResourceResult Fail(string reason) => new(null, 0, reason);
    }
}
