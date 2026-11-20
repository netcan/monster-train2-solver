using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicRelentlessDrawModel
    {
        internal const string Effect = "RelicEffectAdditionalDrawDuringRelentless";
        internal static bool Has(CombatContext? context) => context?.Relics?.Any(owner => owner.EffectTypes.Contains(Effect)) == true;
        internal static string? Validate(CombatRelicState owner, RelicResourceEffect rule) =>
            owner.IsCovenant.HasValue && owner.DisallowedInPlacementPhase.HasValue && !rule.CanApplyInPreviewMode &&
            rule.RequiredMutatorId == null && rule.Amount == rule.ParamInt && rule.DrawValue.HasValue &&
            (rule.DrawValue == 0 || rule.DrawValue == (float)rule.Amount) && (!rule.DrawMutationOrder.HasValue || rule.DrawMutationOrder >= 0)
                ? null : "Missing initialized Relentless draw amount, cache or registration.";

        internal static string? ValidateBeforeDrawOrder(CombatContext context)
        {
            if (!Has(context)) return null;
            bool cachedDrawSeen = false;
            foreach (string type in context.Relics!.SelectMany(owner => owner.EffectTypes))
            {
                if (type == Effect || type == RelicTurnDrawModel.FullHealth) cachedDrawSeen = true;
                else if (cachedDrawSeen && (type == RelicResourceModel.StartTurnHeal || type == RelicResourceModel.StartTurnRoomEffect ||
                    type == RelicResourceModel.PileStartTurn || type == RelicSoloUnitUpgradeModel.Effect))
                    return "Later before-draw health, upgrade or room callbacks with Relentless draw require ordered native integration.";
            }
            return null;
        }

        internal static RelicResourceResult Query(TrainCombatState train)
        {
            var context = train.Context!; var rooms = context.RoomCapacities;
            if (rooms == null || rooms.Count == 0 || context.SpawnPoints == null ||
                rooms.Select(room => room.RoomIndex).Distinct().Count() != rooms.Count ||
                !rooms.Select(room => room.RoomIndex).OrderBy(id => id).SequenceEqual(train.Rooms.Select(room => room.RoomIndex).OrderBy(id => id)))
                return Fail("Relentless draw requires complete physical rooms and Pyre metadata.");
            var actors = train.Rooms.SelectMany(room => room.Units).Concat(context.Enchantments?.RetainedUnits.Select(actor => actor.Unit) ?? Array.Empty<CombatUnit>())
                .GroupBy(actor => actor.Id).ToDictionary(group => group.Key, group => group.First());
            foreach (var room in rooms.Where(room => !room.IsPyre).OrderBy(room => room.RoomIndex))
            foreach (var team in new[] { CombatTeam.Enemy, CombatTeam.Player })
            {
                var group = context.SpawnPoints.SelectedGroup(room.RoomIndex, team);
                if (group == null) return Fail("Missing Relentless draw physical group.");
                foreach (int id in group.Occupants.Where(id => id > 0).Distinct())
                {
                    if (!actors.TryGetValue(id, out var actor)) return Fail("Missing physically registered Relentless draw actor.");
                    // GetCharactersWithStatus does not filter dead, spawning or
                    // post-combat-despawn actors. InSameRoom currently returns true.
                    if ((actor.StatusRegistry ?? actor.Statuses).FirstOrDefault(status => string.Equals(status.Id, "relentless", StringComparison.OrdinalIgnoreCase))?.Stacks > 0) return new RelicResourceResult(context, 1);
                }
            }
            return new RelicResourceResult(context, 0);
        }

        internal static RelicResourceResult RefreshOne(TrainCombatState train, int ownerIndex, int effectIndex,
            bool placement = false, bool preview = false, bool startOfCombat = false)
        {
            var context = train.Context!; var owner = context.Relics![ownerIndex];
            var effect = owner.ResourceEffects!.Single(rule => rule.EffectIndex == effectIndex);
            string? error = Validate(owner, effect); if (error != null) return Fail(error);
            if (preview || (startOfCombat ? owner.DisallowedInPlacementPhase == placement : placement && owner.DisallowedInPlacementPhase == true))
                return new RelicResourceResult(context, 0);
            var condition = RelicConditionModel.Evaluate(context, effect.Conditions);
            if (!condition.Supported) return Fail(condition.UnsupportedReason!);
            context = condition.Context; if (!condition.Passed) return new RelicResourceResult(context, 0);
            var query = Query(CardSpellModel.WithContext(train, context)); if (!query.Supported) return query;
            float value = query.Value != 0 ? effect.Amount : 0;
            var effects = context.Relics![ownerIndex].ResourceEffects!.Select(rule => rule.EffectIndex == effectIndex ? rule.WithDrawValue(value) : rule).ToArray();
            var owners = context.Relics.ToArray(); owners[ownerIndex] = owners[ownerIndex].WithResourceEffects(effects);
            return new RelicResourceResult(context.WithRelics(owners), 0);
        }

        // Unity/Mono's Mathf.RoundToInt converts this one int-to-float overflow
        // boundary to Int32.MinValue. Other values retain the captured float precision.
        internal static int RoundedValue(float value) => value >= 2147483648f ? int.MinValue : (int)Math.Round(value, MidpointRounding.ToEven);
        private static RelicResourceResult Fail(string reason) => new(null, 0, reason);
    }
}
