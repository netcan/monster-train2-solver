using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static partial class RelicResourceModel
    {
        public static (int BaseMaxHealth, int Health, int ModifiedAttack) AdjustPyreForDeckChange(
            int baseMaxHealth, int maxHealth, int health, int modifiedAttack,
            int deckDelta, int healthPerCard, int attackPerCard)
        {
            int healthDelta = unchecked(deckDelta * healthPerCard);
            int newBaseMaximum = Math.Max(1, unchecked(baseMaxHealth + healthDelta));
            int newMaximum = unchecked(maxHealth - baseMaxHealth + newBaseMaximum);
            // A reduction only lowers the cap. Any raw HP above that cap is
            // invisible, and a later increase would still fill the new cap.
            int newHealth = Math.Min(newMaximum, Math.Max(0, unchecked(health + Math.Max(0, healthDelta))));
            int newAttack = Math.Max(0, unchecked(modifiedAttack + deckDelta * attackPerCard));
            return (newBaseMaximum, newHealth, newAttack);
        }
    }

    public static class RelicDeckPyreStatsModel
    {
        public const string Effect = "RelicEffectAddPyreAttackAndHealthPerCardInDeck";
        public const string CardCopies = "RelicEffectGetCardCopiesOnDeck";

        private static int Tier(string id) => id switch
        {
            "7254e0f3-890a-43d9-b767-76df34b8fa05" => 1,
            "7ec532ca-5126-4a49-9895-67f33426f0c9" => 2,
            "490d737b-fa0d-4f16-bb90-4ff22af11530" => 3,
            _ => 0
        };

        internal static bool IsOriginal(CombatRelicState owner)
        {
            int tier = Tier(owner.DataId);
            return tier != 0 && owner.AssetKey == "ThikT" + tier &&
                owner.EffectTypes.SequenceEqual(new[] { CardCopies, Effect });
        }

        internal static bool Present(CombatContext? context) =>
            context?.Relics?.Any(owner => owner.EffectTypes.Contains(Effect)) == true;

        internal static bool AllowsHealthAboveCharacterMaximum(CombatContext? context, CombatUnit pyre) =>
            pyre.IsPyre && Present(context) && RelicResourceModel.GetPyreMaxHealth(context!) is
                { Supported: true } maximum && pyre.Health <= maximum.Value;

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect rule)
        {
            int tier = Tier(owner.DataId);
            if (!IsOriginal(owner) || rule.ParamInt != (tier == 3 ? 2 : 0) || rule.ParamInt2 != tier + 2 ||
                rule.Amount != 0 || rule.RequiredMutatorId != null || rule.Conditions.Count != 0 ||
                !rule.LastKnownDeckCount.HasValue || rule.LastKnownDeckCount.Value < 0)
                return "Missing or changed original Thik deck-change payload.";
            return null;
        }

        internal static CombatContext? DeckChanged(CombatContext source, out string? error)
        {
            error = null;
            if (!Present(source)) return source;
            error = RelicModel.Validate(source);
            if (error != null) return null;
            if (source.Statistics?.DeckCards == null)
            {
                error = "Thik requires the captured permanent deck membership.";
                return null;
            }

            int deckCount = source.Statistics.DeckCards.Count;
            CombatContext context = source;
            IReadOnlyList<CombatRelicState> owners = source.Relics!;
            for (int ownerIndex = 0; ownerIndex < owners.Count; ownerIndex++)
            {
                CombatRelicState owner = owners[ownerIndex];
                if (!owner.EffectTypes.Contains(Effect)) continue;
                RelicResourceEffect rule = owner.ResourceEffects!.Single(effect => effect.Type == Effect);
                if (source.Enchantments?.Preview == true && !rule.CanApplyInPreviewMode) continue;
                int delta = deckCount - rule.LastKnownDeckCount!.Value;
                if (delta == 0) continue;

                BattleResourceState raw = context.Resources!;
                RelicResourceResult maximum = RelicResourceModel.GetPyreMaxHealth(context);
                if (!maximum.Supported)
                {
                    error = maximum.UnsupportedReason;
                    return null;
                }
                EnchantmentWorld? world = context.Enchantments;
                CombatUnit? pyre = LivePyre(world);
                if (pyre == null)
                {
                    error = "Native Thik deck changes require the captured live Pyre.";
                    return null;
                }

                var updated = RelicResourceModel.AdjustPyreForDeckChange(raw.BasePyreMaxHealth, maximum.Value,
                    pyre.Health, raw.PyreModifiedBaseAttack, delta, rule.ParamInt2, rule.ParamInt);
                context = context.WithResources(raw.WithBasePyreMaxHealth(updated.BaseMaxHealth)
                    .WithPyreModifiedBaseAttack(updated.ModifiedAttack));
                if (pyre.Health != updated.Health)
                {
                    CombatUnit adjustedPyre = pyre.WithHealth(updated.Health);
                    context = context.WithEnchantments(new EnchantmentWorld(
                        world!.Rooms.Select(room => ReplaceHealth(room, adjustedPyre)).ToArray(),
                        world.Movement, world.EnemySlotsPerRoom, world.RetainedUnits, world.EnchanterIds,
                        world.AllowUpdates, world.Updating, world.Preview, world.TestRng,
                        world.AutomaticLifecycle, world.TemporaryPreviewUnitIds));
                }

                var rules = owner.ResourceEffects!.Select(effect => effect == rule ? rule.WithLastKnownDeckCount(deckCount) : effect).ToArray();
                var relics = context.Relics!.ToArray();
                relics[ownerIndex] = owner.WithResourceEffects(rules);
                context = context.WithRelics(relics);
            }
            return context;
        }

        internal static CombatContext? RemoveCard(CombatContext context, int cardId, out string? error)
        {
            BattleStatistics? statistics = context.Statistics;
            if (statistics?.DeckCards?.Contains(cardId) == true)
            {
                if (context.Resources != null)
                    context = context.WithResources(context.Resources.WithVisibleDeckCount(context.Resources.VisibleDeckCount - 1));
                context = context.WithStatistics(statistics.RemoveDeckCard(cardId));
            }
            return DeckChanged(context, out error);
        }

        internal static CombatUnit? ChangedPyre(CombatContext? before, CombatContext? after)
        {
            if (ReferenceEquals(before?.Enchantments, after?.Enchantments) || ReferenceEquals(before?.Relics, after?.Relics) ||
                !Present(before) || !Present(after)) return null;
            var prior = before!.Relics!.SelectMany(owner => owner.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
                .Where(rule => rule.Type == Effect);
            var next = after!.Relics!.SelectMany(owner => owner.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
                .Where(rule => rule.Type == Effect);
            if (prior.SequenceEqual(next)) return null;
            CombatUnit? pyre = LivePyre(after.Enchantments);
            return pyre != null && LivePyre(before.Enchantments)?.Health != pyre.Health ? pyre : null;
        }

        internal static RoomCombatState ReplaceHealth(RoomCombatState room, CombatUnit pyre) =>
            room.Units.Any(unit => unit.Id == pyre.Id && unit.Health != pyre.Health)
                ? room.WithUnits(room.Units.Select(unit => unit.Id == pyre.Id ? unit.WithHealth(pyre.Health) : unit).ToArray())
                : room;

        internal static void UpdateRemoteHealth(RoomCombatState[] rooms, int changedRoom,
            CombatContext? before, CombatContext? after)
        {
            CombatUnit? pyre = ChangedPyre(before, after);
            if (pyre == null) return;
            for (int index = 0; index < rooms.Length; index++)
                if (rooms[index].RoomIndex != changedRoom)
                    rooms[index] = ReplaceHealth(rooms[index], pyre);
        }

        private static CombatUnit? LivePyre(EnchantmentWorld? world) => world?.Rooms.SelectMany(room => room.Units)
            .SingleOrDefault(unit => unit.IsPyre && unit.Health > 0 && unit.DeathState?.IsDestroyed != true);
    }
}
