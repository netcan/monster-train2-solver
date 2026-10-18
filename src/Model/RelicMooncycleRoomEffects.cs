using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicMooncycleRoomEffectRule
    {
        public int Mode { get; }
        public int ParamMaxInt { get; }
        public float ParamFloat { get; }
        public bool ParamBool { get; }
        public bool ParamBool2 { get; }
        public bool ParamBool3 { get; }
        public bool ParamBool4 { get; }
        public IReadOnlyList<CardActionEffect> Actions { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public bool UsesNestedMultipliers { get; }
        public int TriggerCount { get; }
        public RelicMooncycleRoomEffectRule(int mode, int paramMaxInt, float paramFloat,
            bool paramBool, bool paramBool2, bool paramBool3, bool paramBool4,
            IReadOnlyList<CardActionEffect> actions, IReadOnlyList<string> externalInteractions,
            bool usesNestedMultipliers, int triggerCount)
        {
            Mode = mode; ParamMaxInt = paramMaxInt; ParamFloat = paramFloat; ParamBool = paramBool;
            ParamBool2 = paramBool2; ParamBool3 = paramBool3; ParamBool4 = paramBool4;
            Actions = Array.AsReadOnly(actions.ToArray()); ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
            UsesNestedMultipliers = usesNestedMultipliers; TriggerCount = triggerCount;
        }
    }

    internal static class RelicMooncycleRoomEffectModel
    {
        internal const string Effect = "RelicEffectAddEffectOnMooncycle";
        internal const string DataId = "8b20402a-100b-4f26-8cb5-47bed13cde3d";
        internal const string AssetKey = "MooncycleFaeAddMagicPower";
        internal const string UpgradeId = "6922ecb5-0835-451c-8ca7-8348d2e66079";
        internal const string FaeSubtype = "SubtypesData_Fae";
        internal static string? Validate(CombatRelicState owner, RelicResourceEffect resource)
        {
            RelicMooncycleRoomEffectRule? rule = resource.MooncycleRoomEffect;
            if (owner.DataId != DataId || owner.AssetKey != AssetKey || owner.IsCovenant != false || owner.DisallowedInPlacementPhase != false ||
                !owner.EffectTypes.SequenceEqual(new[] { Effect }) || resource.Type != Effect || resource.EffectIndex != 0 ||
                resource.ParamInt != 1 || resource.ParamInt2 != 0 || resource.ParamMinInt != 0 || resource.ParamFloat != 0 ||
                resource.Amount != 1 || resource.CanApplyInPreviewMode || resource.RequiredMutatorId != null || resource.Conditions.Count != 0 ||
                rule == null || rule.Mode != 1 || rule.ParamMaxInt != 0 || rule.ParamFloat != 0 || rule.ParamBool || rule.ParamBool2 ||
                rule.ParamBool3 || rule.ParamBool4 || rule.UsesNestedMultipliers || rule.TriggerCount != 0 ||
                rule.ExternalInteractions.Count != 0 || rule.Actions.Count != 2)
                return "Missing or changed original MooncycleFaeAddMagicPower definition.";
            for (int i = 0; i < rule.Actions.Count; i++)
            {
                CardActionEffect action = rule.Actions[i];
                if (action.Type != (i == 0 ? "UnitUpgrade" : "AddStatus") || action.Target != "Room" || action.AllowEnemy || !action.AllowPlayer ||
                    action.Value != 0 || action.Range != null || action.Filters?.Health != "Both" || action.Filters.IgnoreBosses ||
                    action.Filters.Subtype != FaeSubtype || action.Filters.RequiredStatuses.Count != 0 || action.Filters.ExcludedStatuses.Count != 0 ||
                    action.Filters.ExcludedSubtypes.Count != 0 || action.Tests?.ShouldTest != true || action.Tests.StrictTargets || action.Tests.FailToCast ||
                    action.Tests.CancelSubsequent || action.Tests.MustHaveValidTargets || action.Tests.CanPlayAfterBossDead != true)
                    return "Changed original mooncycle friendly Fae targeting or effect tests.";
            }
            CardActionEffect upgrade = rule.Actions[0], status = rule.Actions[1];
            CardUpgradeModifier? value = upgrade.Upgrade;
            if (upgrade.Lifetime != "TemporaryUntilEndOfBattle" || upgrade.ScaleUpgrade || upgrade.Statuses.Count != 0 ||
                value?.DataId != UpgradeId || value.AssetKey != "MooncycleFaeAddMagicPower_UpgradeData" ||
                value.Stats.Damage != 5 || value.Stats.Health != 0 || value.Stats.Cost != 0 || value.Stats.Heal != 0 || value.Stats.Size != 0 ||
                value.Stats.XCost != 0 || value.Stats.EquipmentLimit != 0 || value.Stats.UpgradeSlotCount != 0 || value.Unique ||
                value.ExcludeFromClones || value.RemoveOnDiscard || value.UnhealedHealth != 0 || value.DamageBuff != 0 ||
                value.Statuses.Count != 0 || value.ExternalInteractions.Count != 0 || value.TriggerUpgrades?.Count > 0 ||
                value.AbilityUpgrade != null || value.Lifecycle?.AddedTraits.Count > 0 || value.Lifecycle?.RemovedRuntimeTypes.Count > 0 ||
                value.Lifecycle?.ReplacedAssets.Count > 0 || value.Lifecycle?.TraitsModified.Count > 0 || value.Lifecycle?.Triggers.Count > 0 ||
                status.Statuses.Count != 1 || status.Statuses[0].Id != "conduit" || status.Statuses[0].Stacks != 1)
                return "Changed original mooncycle temporary +5 upgrade or Conduit payload.";
            return null;
        }

        internal static MoonPhaseTransitionResult Apply(TrainCombatState source, bool preview = false, bool placement = false)
        {
            if (source.Context?.Relics?.Any(owner => owner.EffectTypes.Contains(Effect)) != true) return new(source);
            TrainCombatState state = source;
            foreach (CombatRelicState owner in source.Context.Relics)
                foreach (RelicResourceEffect resource in owner.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
                {
                    if (resource.Type != Effect) continue;
                    string? error = Validate(owner, resource); if (error != null) return new(null, error);
                    if (preview && !resource.CanApplyInPreviewMode || placement && owner.DisallowedInPlacementPhase == true) continue;
                    RoomCombatState? pyre = state.Rooms.SingleOrDefault(room => room.Units.Any(unit => unit.IsPyre));
                    if (pyre == null) return new(null, "Mooncycle room effects require the native Pyre boundary.");
                    foreach (int room in state.Rooms.Where(room => room.RoomIndex < pyre.RoomIndex).OrderBy(room => room.RoomIndex).Select(room => room.RoomIndex))
                    {
                        TrainSpellResult applied = CardSpellModel.Apply(state, room, resource.MooncycleRoomEffect!.Actions, 0,
                            otherPiles: state.Context?.OtherPiles);
                        if (!applied.Supported) return new(null, applied.UnsupportedReason);
                        if (applied.Outcome != RoomOutcome.Exchanged) return new(null, "Unmodeled terminal mooncycle room callback.");
                        state = applied.State!;
                    }
                }
            return new(state);
        }
    }
}
