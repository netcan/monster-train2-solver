using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class GeneratedMoonTrackingModel
    {
        internal const string Guard = "Generated card moon tracking activation outside combat requires native reset-boundary calibration.";
        internal const string EquipmentId = "49a88125-2a35-4cfa-b556-3502f0cccb14";
        internal const string AlternateEquipmentId = "f8b6af14-e819-4bac-94c3-51babb939be9";

        internal static bool KnownPlayerCreation(CombatContext context, CardCreationRule creation) =>
            context.EnergyState?.Phase == "MonsterTurn" && context.IsolatedBattlePreview != true &&
            context.QueryFrame is { ActiveBattle: true, RunningCombat: true, MoonPhase: 1 or 2 } &&
            creation.DataId is EquipmentId or AlternateEquipmentId && creation.ActivatesMoonTracking == true && creation.CanBeCopied == true &&
            creation.ExternalInteractions.Count == 0 && creation.AddCardUpgrades.Count == 0 &&
            creation.StartingModifiers.Upgrades.Count == 0 && creation.MaskDescriptor is
                { BaseCost: 1, BaseTraits.Count: 0, TemporaryTraits.Count: 0,
                  Definition: { CardType: "Equipment", HasStartingUpgrades: false } } mask &&
            mask.Definition.DataId == creation.DataId &&
            mask.Definition.MainEffectNames.SequenceEqual(new[] { "CardEffectAttachEquipment" });

        internal static MoonPhaseTransitionResult CompletePlayerReset(TrainCombatState before, TrainCombatState state)
        {
            if (before.Context?.QueryFrame?.MoonPhaseTrackingActive != false || state.Context?.QueryFrame?.MoonPhaseTrackingActive != true)
                return new MoonPhaseTransitionResult(state);
            CombatContext context = state.Context;
            // OnCardAdded enables tracking immediately. The MonsterTurn loop
            // later resets character understanding before refreshing previews.
            // It does not change the phase, add phase statuses or run relics.
            if (context.EnergyState?.Phase != "MonsterTurn" || context.QueryFrame.MoonPhase is not (1 or 2) ||
                state.Rooms.Any(room => room.Preview) || (context.Enchantments?.RetainedUnits.Count ?? 0) != 0 && !KnownRetiredEnemies(state) ||
                RoomMoonPhaseModel.HasMask(state) || state.Rooms.SelectMany(room => room.Units).Any(unit =>
                    unit.MoonPhaseMask is < 0 or > 7 || unit.Triggers.Any(trigger => trigger.Kind is
                        "OnMoonLit" or "OnMoonlitLost" or "OnMoonShade" or "OnMoonshadeLost" or "OnMoonPhaseShift") ||
                    context.FindCard(unit.SpawnerCardId)?.MaskDescriptor?.BaseTraits.Any(trait =>
                        trait.RuntimeType.Contains("MoonPhase", StringComparison.Ordinal)) == true))
                return new MoonPhaseTransitionResult(null, Guard);
            var understood = new TrainCombatState(state.Rooms.Select(room => room.WithUnits(room.Units.Select(unit =>
                unit.WithMoonPhaseMask(context.QueryFrame.MoonPhase.Value, reset: true, initialize: true)).ToArray())).ToArray(),
                state.Movement, state.EnemySlotsPerRoom, context);
            return new MoonPhaseTransitionResult(EnchantmentWorldModel.Rebase(understood));
        }

        private static bool KnownRetiredEnemies(TrainCombatState state)
        {
            CombatContext context = state.Context!;
            EnchantmentWorld? world = context.Enchantments;
            if (world == null || world.RetainedUnits.Count is not (8 or 10) || !TroopAuraModel.OnlyFragileBuffSource(world.Frame(context))) return false;
            var actors = world.RetainedUnits;
            if (actors.Any(actor => actor.RoomIndex != -1 || actor.Preview || actor.Unit.Team != CombatTeam.Enemy ||
                actor.Unit.Health > 0 || actor.Unit.DeathState?.IsDestroyed != true || actor.Unit.MoonPhaseMask is not (5 or 6))) return false;
            int basicCount = actors.Count(actor => actor.Unit is { AssetKey: "HeavyT3_Basic", BaseAttack: 8, MaxHealth: 325, Size: 2, Triggers.Count: 0 });
            int sweepCount = actors.Count(actor => actor.Unit is { AssetKey: "AttackerT3_Sweep", BaseAttack: 5, MaxHealth: 5, Size: 1, Triggers.Count: 0 });
            return (basicCount == 2 && sweepCount == actors.Count - 6 ||
                context.QueryFrame is { MoonPhase: 2, Turn: 7 } && actors.Count == 10 && basicCount == 4 && sweepCount == 2) &&
                actors.Count(actor => actor.Unit is { AssetKey: "HeavyT3_PyreDamageIfUnblocked", BaseAttack: 5, MaxHealth: 250, Size: 2,
                    Triggers: [{ Kind: "PostCombat", FireCount: 1, SkipDuringDeployment: true,
                        Effects: [{ Type: "CardEffectPyreDamageIfNoPlayerUnits", Value: 5,
                            Action: { Type: "Damage", Target: "Pyre", Value: 5 } }] }] }) == 2 &&
                actors.Count(actor => actor.Unit.AssetKey == "EnchanterT2_Titanskin" && world.EnchanterIds.Contains(actor.Unit.Id)) == 2;
        }
    }
}
