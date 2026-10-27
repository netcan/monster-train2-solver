using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class GeneratedMoonTrackingModel
    {
        internal const string Guard = "Generated card moon tracking activation outside combat requires native reset-boundary calibration.";
        internal const string EquipmentId = "49a88125-2a35-4cfa-b556-3502f0cccb14";

        internal static bool KnownPlayerCreation(CombatContext context, CardCreationRule creation) =>
            context.EnergyState?.Phase == "MonsterTurn" && context.IsolatedBattlePreview != true &&
            context.QueryFrame is { ActiveBattle: true, RunningCombat: true, MoonPhase: 1 or 2 } &&
            creation.DataId == EquipmentId && creation.ActivatesMoonTracking == true && creation.CanBeCopied == true &&
            creation.ExternalInteractions.Count == 0 && creation.AddCardUpgrades.Count == 0 &&
            creation.StartingModifiers.Upgrades.Count == 0 && creation.MaskDescriptor is
                { BaseCost: 1, BaseTraits.Count: 0, TemporaryTraits.Count: 0,
                  Definition: { DataId: EquipmentId, CardType: "Equipment", HasStartingUpgrades: false } } mask &&
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
                state.Rooms.Any(room => room.Preview) || (context.Enchantments?.RetainedUnits.Count ?? 0) != 0 ||
                RoomMoonPhaseModel.HasMask(state) || state.Rooms.SelectMany(room => room.Units).Any(unit =>
                    unit.MoonPhaseMask is not (null or 0 or 4) || unit.Triggers.Any(trigger => trigger.Kind is
                        "OnMoonLit" or "OnMoonlitLost" or "OnMoonShade" or "OnMoonshadeLost" or "OnMoonPhaseShift") ||
                    context.FindCard(unit.SpawnerCardId)?.MaskDescriptor?.BaseTraits.Any(trait =>
                        trait.RuntimeType.Contains("MoonPhase", StringComparison.Ordinal)) == true))
                return new MoonPhaseTransitionResult(null, Guard);
            var understood = new TrainCombatState(state.Rooms.Select(room => room.WithUnits(room.Units.Select(unit =>
                unit.WithMoonPhaseMask(context.QueryFrame.MoonPhase.Value, reset: true, initialize: true)).ToArray())).ToArray(),
                state.Movement, state.EnemySlotsPerRoom, context);
            return new MoonPhaseTransitionResult(EnchantmentWorldModel.Rebase(understood));
        }
    }
}
