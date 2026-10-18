using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class UnitStatSwapModel
    {
        public static RoomCombatResult Apply(RoomCombatState source, int targetId, bool deferCallbacks = false,
            Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null)
        {
            string? error = RoomCombatModel.Validate(source);
            CombatUnit? target = source.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (error != null || target?.Modifiers == null || source.Context == null)
                return Unsupported(error ?? "Stat swapping requires a live target and captured modifier state.");
            if (target.Status("fixedattack") != null || target.Status("armorattack") != null || target.Status("equalizer") != null)
                return Unsupported("Stat swapping with attack replacement statuses requires native primary-field calibration.");
            int health = HealingModel.HealedHealth(target.Health, target.MaxHealth,
                unchecked(target.MaxHealth - target.Health), target.Modifiers.CanBeHealed, target.Statuses);
            target = target.WithHealth(health);
            source = source.WithUnits(source.Units.Select(unit => unit.Id == targetId ? target : unit).ToArray());
            int mageblade = unchecked((target.Modifiers!.MagebladeDamagePerMagicPower ?? 0) *
                (source.Context.MagicPower?.FirstOrDefault(power => power.RoomIndex == source.RoomIndex && power.Team == target.Team)?.Value ?? 0));
            int attack = Math.Max(0, unchecked(target.AttackBeforeStatuses + target.AttackFromStatuses +
                RelicStatusAttackModel.AdditionalAttack(target.Modifiers.StatusAttackModifiers, target.Team, target.Statuses) + mageblade +
                (target.IsPyre ? source.Context.Resources?.PyreModifiedBaseAttack ?? 0 : 0)));
            CombatEffect? healer = target.Triggers.Where(trigger => trigger.Kind == "PostCombatHealing")
                .SelectMany(trigger => trigger.Effects).FirstOrDefault(effect => effect.Type == "CardEffectHeal");
            int nextMaximum = healer?.Value ?? attack;
            var stats = new CardStatModifier(damage: unchecked(health - target.Modifiers.AttackDamage),
                health: unchecked(nextMaximum - target.MaxHealth));
            var lifecycle = new CardLifecycleUpgrade(new CardMaskUpgrade("", stats, Array.Empty<UpgradeMaskStatus>(),
                false, false, false, false), "", false, false, Array.Empty<CardTraitValue>(), Array.Empty<string>(),
                false, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<CardLifecycleTrigger>());
            var upgrade = new CardUpgradeModifier("", "", stats, Array.Empty<CombatStatus>(), false, false, false,
                0, unchecked(-target.Modifiers.DamageBuff), Array.Empty<string>(),
                maskMetadata: source.Context.CardInstances?.Any(card => card.MaskDescriptor != null) == true
                    ? new CardUpgradeMaskMetadata(Array.Empty<UpgradeMaskStatus>(), false, false, false, false) : null,
                lifecycle: lifecycle);
            RoomCombatResult applied = deferCallbacks
                ? UnitModifierModel.ApplyDirectDeferred(source, targetId, upgrade, refreshPyre)
                : UnitModifierModel.ApplyDirect(source, targetId, upgrade, refreshPyre: refreshPyre);
            if (!applied.Supported) return applied;
            RoomCombatState changed = applied.State!;
            if (!source.Preview && source.Context.IsolatedBattlePreview != true && target.SpawnerCardId > 0 && target.Modifiers.SpawnerMatchesDefinition)
            {
                CardInstanceState? card = changed.Context!.FindCard(target.SpawnerCardId);
                if (card == null) return Unsupported("Stat swapping lost its target's spawner card.");
                CombatContext context = changed.Context.WithCard(card.WithModifiers(card.Permanent, UnitModifierModel.Add(card.Temporary, upgrade)));
                changed = new RoomCombatState(changed.RoomIndex, changed.Deployment, changed.Units,
                    changed.ExternalInteractions, context, changed.Preview, changed.ActiveCompanionBossIds);
            }
            return new RoomCombatResult(changed, applied.Outcome, applied.Rounds, applied.Events.ToList(),
                pendingCallbacks: applied.PendingCallbacks, retainedUnits: applied.RetainedUnits);
        }
        private static RoomCombatResult Unsupported(string error) =>
            new(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
    }
}
