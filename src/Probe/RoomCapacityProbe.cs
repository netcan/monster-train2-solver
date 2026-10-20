using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RoomCapacityProbe
    {
        internal static RoomCapacityState[] Capture(RoomManager rooms) => Enumerable.Range(0, rooms.GetNumRooms()).Select(index =>
        {
            RoomState room = rooms.GetRoom(index);
            return new RoomCapacityState(index, room.GetCapacityInfo(Team.Type.Monsters).max,
                room.GetCapacityInfo(Team.Type.Heroes).max, room.GetIsPyreRoom(), room.GetCurrentCorruption(),
                CaptureAttachments(room), room.GetMaxCorruption(), room.GetPermanentCorruption(), room.NumRoomAttachmentsAddedFromCard);
        }).ToArray();

        internal static int SpellCostDelta(RoomState room) => room.Attachments.Where(attachment => attachment.IsActive)
            .SelectMany(attachment => attachment.UpgradeStates).SelectMany(upgrade => upgrade.Item2.GetRoomModifierUpgrades())
            .Where(modifier => modifier.GetRoomStateModifierClassName() == "RoomStateSpellCostModifier")
            .Sum(modifier => modifier.GetParamInt());

        internal static bool SupportsAttachments(RoomState room) => CaptureAttachments(room) != null;

        private static RoomCapacityAttachmentState[]? CaptureAttachments(RoomState room)
        {
            var result = new List<RoomCapacityAttachmentState>();
            foreach (TrainRoomAttachmentState attachment in room.Attachments)
            {
                if (!attachment.IsActive || (int)attachment.AttachType != 0 || attachment.UpgradeStates.Count == 0 ||
                    attachment.CardState == null && attachment.UpgradeStates.Count != 1) return null;
                int delta = 0;
                RoomMoonPhaseRule? moon = null;
                RoomPostCombatRule? postCombat = null;
                RoomPreCombatRule? preCombat = null;
                var characterUpgrades = new List<CardUpgradeModifier>();
                var characterFilters = new List<CardUpgradeMaskRule>();
                var statusStacks = new List<RoomStatusStackRule>();
                string? spawnCopySubtype = null;
                foreach (var (state, definition) in attachment.UpgradeStates)
                {
                    RoomModifierData[] roomModifiers = definition.GetRoomModifierUpgrades().ToArray();
                    RoomModifierData[] capacityModifiers = roomModifiers.Where(modifier =>
                        modifier.GetRoomStateModifierClassName() == "RoomStateCapacityModifier").ToArray();
                    RoomModifierData[] spellCostModifiers = roomModifiers.Where(modifier =>
                        modifier.GetRoomStateModifierClassName() == "RoomStateSpellCostModifier").ToArray();
                    RoomModifierData[] energyModifiers = roomModifiers.Where(modifier =>
                        modifier.GetRoomStateModifierClassName() == "RoomStateEnergyModifier").ToArray();
                    var interactions = new List<string>();
                    statusStacks.AddRange(RoomStatusStackProbe.Rules(roomModifiers, interactions) ?? System.Array.Empty<RoomStatusStackRule>());
                    string? copiedSubtype = RoomSpawnCopyProbe.Rule(roomModifiers, interactions);
                    if (copiedSubtype != null) { if (spawnCopySubtype != null) return null; spawnCopySubtype = copiedSubtype; }
                    CardUpgradeMaskRule[] filters = state.GetFilters().Select(CardUpgradeMaskProbe.Rule).ToArray();
                    if (filters.Length > 0 && attachment.UpgradeStates.Count != 1) return null;
                    characterFilters.AddRange(filters);
                    RoomMoonPhaseRule? phase = RoomMoonPhaseProbe.Rule(definition, interactions);
                    if (phase != null) { if (moon != null) return null; moon = phase; }
                    RoomPostCombatRule? post = RoomPostCombatProbe.Rule(definition, interactions);
                    if (post != null) { if (postCombat != null || attachment.UpgradeStates.Count != 1) return null; postCombat = post; }
                    RoomPreCombatRule? pre = RoomPreCombatProbe.Rule(definition, interactions);
                    if (pre != null) { if (preCombat != null || attachment.UpgradeStates.Count != 1) return null; preCombat = pre; }
                    RoomSpellUpgradeProbe.Rule(roomModifiers, interactions);
                    bool supported = interactions.Count == 0 && (phase != null || post != null || pre != null || roomModifiers.Length == capacityModifiers.Length + spellCostModifiers.Length + energyModifiers.Length + roomModifiers.Count(RoomStatusStackProbe.Known) + roomModifiers.Count(RoomSpawnCopyProbe.Known) + roomModifiers.Count(RoomSpellUpgradeProbe.Known)) &&
                        capacityModifiers.Length <= 1 && spellCostModifiers.Length <= 1 && energyModifiers.Length <= 1 && energyModifiers.All(modifier => modifier.GetParamInt() == 1) &&
                        state.GetAttackDamageBuff() == 0 && state.GetAdditionalHP() == 0 &&
                        (state.GetAdditionalUnhealedHP() == 0 || attachment.CardState?.GetCardDataID() == "57921ae8-ca98-4a3b-a659-5e741d2e904a" && state.GetAdditionalUnhealedHP() == 20) && state.GetCostReduction() == 0 && state.GetAdditionalSize() == 0 &&
                        state.GetAdditionalEquipmentLimit() == 0 && state.GetStatusEffectUpgrades().All(status => RoomCapacityModel.SupportsCharacterStatus(status.statusId, status.count) && !status.fromPermanentUpgrade) &&
                        state.GetTraitDataUpgrades().Count == 0 && state.GetRemoveTraitUpgrades().Count == 0 &&
                        (attachment.CardState == null || state.GetTriggerUpgrades().Count == 0 || RoomAttachmentTriggerIdentity.Known(attachment.CardState.GetCardDataID())) && state.GetCardTriggerUpgrades().Count == 0 &&
                        state.GetUpgradesToRemove().Count == 0 && RoomCapacityModel.ValidateCharacterFilters(filters) == null &&
                        state.GetUnitAbilityUpgrade() == null && state.GetRoomAbilityUpgrade() == null && !state.IsUnique();
                    if (!supported) return null;
                    if (capacityModifiers.Length == 1) delta = unchecked(delta + capacityModifiers[0].GetParamInt());
                    characterUpgrades.Add(CardModifierProbe.Upgrade(state, rejectFilters: false,
                        filtersAlreadyApplied: true, roomAttachmentUnitUpgrade: true));
                }
                string dataId = attachment.CardState?.GetCardDataID() ?? attachment.UpgradeStates.Single().Item2.GetID();
                string assetKey = attachment.CardState == null ? attachment.UpgradeStates.Single().Item2.name :
                    AllGameManagers.Instance!.GetSaveManager().GetAllGameData().FindCardData(dataId)!.name;
                bool ignoreBosses = (bool)AccessTools.Field(typeof(TrainRoomAttachmentState), "_isIgnoreBosses").GetValue(attachment);
                int spellCostDelta = attachment.UpgradeStates.SelectMany(item => item.Item2.GetRoomModifierUpgrades())
                    .Where(modifier => modifier.GetRoomStateModifierClassName() == "RoomStateSpellCostModifier")
                    .Sum(modifier => modifier.GetParamInt());
                result.Add(new RoomCapacityAttachmentState(dataId, assetKey, (int)attachment.AttachType, delta,
                    characterUpgrades, attachment.TrainSide.HasFlag(Team.Type.Heroes),
                    attachment.TrainSide.HasFlag(Team.Type.Monsters), ignoreBosses,
                    attachment.CardState == null ? attachment.Guid : RoomAttachmentTriggerIdentity.Known(dataId) ? FullBattleTrace.Active!.UpgradeKey(attachment.Guid) : "", spellCostDelta, moon,
                    attachment.UpgradeStates.SelectMany(item => item.Item2.GetRoomModifierUpgrades())
                        .Where(modifier => modifier.GetRoomStateModifierClassName() == "RoomStateEnergyModifier").Sum(modifier => modifier.GetParamInt()), characterFilters, postCombat, preCombat,
                        statusStacks.Count == 0 ? null : statusStacks, spawnCopySubtype, RoomSpellUpgradeProbe.Capture(attachment)));
            }
            return result.ToArray();
        }
        internal static ScalingCapacityTrait[]? Traits(CardState card)
        {
            ScalingCapacityTrait[] traits = card.GetTraitStates().OfType<CardTraitScalingAdjustCapacity>().Select(trait =>
                new ScalingCapacityTrait(DamageScalingProbe.Query(trait.StatValueData,
                    (int)AccessTools.Field(typeof(CardState), "cost").GetValue(card), card.IsConsumeRemainingEnergyCostType()), trait.GetParamInt())).ToArray();
            return traits.Length == 0 ? null : traits;
        }
        internal static ScalingCapacityTrait[]? Creation(CardData card)
        {
            ScalingCapacityTrait[] traits = card.GetTraits().Where(data => data.GetTraitStateName() == "CardTraitScalingAdjustCapacity").Select(data =>
            {
                var trait = new CardTraitScalingAdjustCapacity(); trait.Setup(data, CardState.None);
                return new ScalingCapacityTrait(DamageScalingProbe.Query(trait.StatValueData, card.GetCost(),
                    card.GetCostType() == CardData.CostType.ConsumeRemainingEnergy), trait.GetParamInt());
            }).ToArray();
            return traits.Length == 0 ? null : traits;
        }
        internal sealed class Attempt
        {
            public int Maximum { get; set; }
            public int Requested { get; set; }
            public int Approved { get; set; }
            public bool Allowed { get; set; }
            public string Error { get; set; } = "";
        }
        internal sealed class Record
        {
            public int SourceCardId { get; set; }
            public CardActionEffect Effect { get; set; } = null!;
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public bool Completed { get; set; }
            public List<Attempt> Attempts { get; } = new List<Attempt>();
        }
        internal sealed class TestRecord
        {
            public RoomCombatState Before { get; set; } = null!;
            public CardActionEffect Effect { get; set; } = null!;
            public bool Actual { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        internal static readonly List<TestRecord> Tests = new List<TestRecord>();
        private static Record? current;
        [HarmonyPatch(typeof(CardEffectAdjustRoomCapacity), nameof(CardEffectAdjustRoomCapacity.ApplyEffect))]
        private static class ApplyPatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            { if (FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode) __result = Observe(__result, cardEffectState, cardEffectParams); }
        }
        private static IEnumerator Observe(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
        {
            FullBattleTrace trace = FullBattleTrace.Active!; Record? parent = current;
            RoomState room = AllGameManagers.Instance!.GetRoomManager()!.GetRoom(parameters.selectedRoom);
            CardState? owner = effect.GetParentCardState();
            var record = new Record { SourceCardId = owner == null ? 0 : trace.CardId(owner),
                Effect = UnitTriggerActionProbe.Capture(effect) ?? throw new NotSupportedException("Uncaptured room-capacity effect."), Before = CaptureAllUnits(trace, room) };
            Records.Add(record); current = record;
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); current = parent; record.After = CaptureAllUnits(trace, room); }
        }
        private static RoomCombatState CaptureAllUnits(FullBattleTrace trace, RoomState room)
        {
            RoomCombatState captured = trace.Capture(room); var units = new List<CharacterState>();
            room.AddCharactersToList(units, Team.Type.Heroes); room.AddCharactersToList(units, Team.Type.Monsters);
            return new RoomCombatState(captured.RoomIndex, captured.Deployment, units.Where(unit => !unit.IsDestroyed)
                .Select(unit => trace.CaptureUnit(unit)).ToArray(), captured.ExternalInteractions, captured.Context, captured.Preview);
        }
        [HarmonyPatch(typeof(RoomState), nameof(RoomState.CanAdjustCapacity))]
        private static class BoundsPatch
        {
            private static void Prefix(RoomState __instance, Team.Type team, int adjustAmount, out Attempt? __state)
            {
                __state = current == null ? null : new Attempt { Maximum = __instance.GetCapacityInfo(team).max, Requested = adjustAmount };
            }
            private static void Postfix(int adjustAmount, SpawnPointGroup.CapacityAdjustmentError error, bool __result, Attempt? __state)
            { if (__state != null && current != null) { __state.Approved = adjustAmount; __state.Allowed = __result; __state.Error = error.ToString(); current.Attempts.Add(__state); } }
        }
        [HarmonyPatch(typeof(CardEffectAdjustRoomCapacity), nameof(CardEffectAdjustRoomCapacity.TestEffect))]
        private static class TestPatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, bool __result)
            {
                FullBattleTrace? trace = FullBattleTrace.Active;
                if (trace == null || trace.NativeWon != null || AllGameManagers.Instance!.GetSaveManager().PreviewMode) return;
                Tests.Add(new TestRecord { Before = CaptureAllUnits(trace, AllGameManagers.Instance!.GetRoomManager()!.GetRoom(cardEffectParams.selectedRoom)),
                    Effect = UnitTriggerActionProbe.Capture(cardEffectState) ?? throw new NotSupportedException("Uncaptured room-capacity test."), Actual = __result });
            }
        }
    }
}
