using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // A local seed estimate, never a legality filter. Resolve room magic power
    // in an immutable context and cap useful damage at the target's durability.
    internal sealed class TargetedDamagePolicy
    {
        private readonly BattleTurnState source;
        private readonly Dictionary<(int Card, int Room), (CombatContext Context, CardActionEffect Effect)?> damage = new();

        internal TargetedDamagePolicy(BattleTurnState source) { this.source = source; }

        internal double? TargetValue(PlayCardAction action, CardPlayRule rule, CombatUnit target)
        {
            if (target.Team != CombatTeam.Enemy || target.Health <= 0 || rule.CardType != "Spell" || rule.IsAnyAbility == true ||
                rule.OnCastEffects.Count != 0 || rule.Effects.Count(effect => effect.Type == "Damage") != 1 ||
                rule.Effects.Any(effect => effect.Target is not ("DropTargetCharacter" or "LastTargetedCharacters") ||
                    !(effect.Type == "Damage" && effect.AllowEnemy && !effect.AllowPlayer && effect.Value > 0 && effect.Range == null ||
                      effect.Type == "Heal" && effect.AllowPlayer && !effect.AllowEnemy))) return null;
            // Keep cheap pings in their existing finishing/shield-breaking
            // role even when the currently focused room adds magic power.
            // Reserve a card for larger targets only when its authored or
            // permanent damage already overkills the weakest visible enemy.
            CardPlayRule? authored = source.PlayRules?.Cards.FirstOrDefault(item => item.DataId == rule.DataId);
            CardInstanceState? owner = source.Spawn.Train.Context?.FindCard(action.CardInstanceId);
            int persistentDamage = authored?.Effects.FirstOrDefault(effect => effect.Type == "Damage")?.Value ??
                rule.Effects.Single(effect => effect.Type == "Damage").Value;
            if (owner != null) persistentDamage = CardModifierModel.UpgradedStat(persistentDamage, "Damage", true, owner.Permanent);
            int smallestEnemy = source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0).Min(unit => unit.Health);
            if (persistentDamage <= smallestEnemy) return null;
            var key = (action.CardInstanceId, action.RoomIndex);
            if (!damage.TryGetValue(key, out var selected))
            {
                CombatContext? context = source.Spawn.Train.Context;
                if (context == null || ConduitModel.Validate(context) != null || RoomSpellUpgradeModel.ValidateContext(context) != null)
                    selected = null;
                else
                {
                    if (context.QueryFrame != null)
                        context = context.WithQueryFrame(context.QueryFrame.With(selectedRoomIndex: action.RoomIndex));
                    CardInstanceState? card = context.FindCard(action.CardInstanceId);
                    CardPlayRule? original = source.PlayRules?.Cards.FirstOrDefault(item => item.DataId == rule.DataId);
                    CardPlayRule resolved = card != null && original != null ? CardModifierModel.Resolve(original, card) : rule;
                    selected = resolved.ExternalInteractions.Count > 0 ? null :
                        (context, resolved.Effects.Single(effect => effect.Type == "Damage"));
                }
                damage.Add(key, selected);
            }
            if (!selected.HasValue) return null;
            var value = selected.Value;
            var scaled = DamageScalingModel.Apply(value.Context, action.CardInstanceId, action.CardInstanceId,
                value.Effect.Value, selectedRoom: source.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex),
                targetStatuses: target.Statuses);
            if (!scaled.Supported) return null;
            // Shields can still be broken by a small spell. Keep that legal
            // fallback below actual injury; the full frontier retains every cast.
            double amount = Math.Max(0d, scaled.Damage);
            if (target.Statuses.Any(status => status.Stacks > 0 &&
                status.Id is "damage shield" or "spell shield" or "untouchable" or "immune")) amount = 0;
            else amount *= 1d + Math.Max(0, target.Status("spell weakness")?.Stacks ?? 0);
            double durability = target.Health + Math.Max(0d, target.Status("armor")?.Stacks ?? 0);
            double injury = Math.Min(amount, durability);
            double kill = amount >= durability ? 10d + Math.Max(0d, target.Attack) * target.Attacks * 2d : 0;
            return 1000d + injury + kill;
        }
    }
}
