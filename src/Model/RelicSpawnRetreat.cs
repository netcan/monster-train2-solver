using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicSpawnRetreatModel
    {
        internal const string DataId = "f7b3141b-e803-4128-86cd-d1bf4827d943";
        internal const string AssetKey = "ChanceRetreatEnemiesOnSpawn";
        internal static readonly IReadOnlyList<string> ExcludedSubtypes = Array.AsReadOnly(new[] {
            "SubtypesData_BigBossT1_50952308-e0f3-4acb-b0cc-e0d662b868b8",
            "SubtypesData_BigBoss2_e3e6f1a2-34a7-4493-b9d7-a123e182241e",
            "SubtypesData_BigBossT3_f7449215-5071-4671-909c-a02e2cceb821",
            "SubtypesData_BossEntropy" });
        internal static string? Validate(CombatRelicState owner)
        {
            RelicAfterSpawnChanceRule? rule = owner.AfterSpawnChanceRules?.Count == 1 ? owner.AfterSpawnChanceRules[0] : null;
            return owner.DataId == DataId && owner.AssetKey == AssetKey && owner.IsCovenant == false &&
                owner.DisallowedInPlacementPhase == false && owner.EffectTypes.SequenceEqual(new[] { RelicAfterSpawnChanceStatusModel.Effect }) &&
                rule?.EffectIndex == 0 && rule.ChancePercent == 100 && rule.RoomRestriction == 1 && rule.TargetTeam == "Heroes" &&
                rule.HasRequiredSubtype && rule.RequiredSubtypeIsNone && rule.RequiredSubtype == "SubtypesData_None" &&
                rule.ExcludedSubtypes.SequenceEqual(ExcludedSubtypes) && rule.ConditionCount == 0 && rule.CanApplyInPreviewMode &&
                rule.CardEffectCount == 1 && rule.CardEffectHasOnlyCapturedPayload && rule.CardEffectPayloadIssues.Count == 0 &&
                rule.CardEffect == "CardEffectFloorRearrange" && rule.CardEffectTarget == "BackInRoom" &&
                rule.CardEffectTeam == "Heroes" && rule.CardEffectTargetCardType == "Spell" && rule.CardEffectParamMultiplier == 0 &&
                rule.CardEffectParamInt == 0 && rule.CardEffectAdditionalParamInt == 0 && !rule.CardEffectUsesRange &&
                !rule.CardEffectParamBool && !rule.CardEffectParamBool2 && rule.StatusEffects.Count == 0 && rule.TriggeredInRooms?.Count == 4
                ? null : "Missing or changed original ChanceRetreatEnemiesOnSpawn definition or four-room runtime state.";
        }

        internal static bool Test(RoomCombatState? room, CombatUnit? actor, string trigger, CombatRelicState owner)
        {
            if (trigger != "AfterSpawnEnchant" || actor == null || actor.DeathState?.IsDestroyed == true ||
                actor.Team != CombatTeam.Enemy || actor.Status("untouchable") != null || room == null) return false;
            if (owner.AfterSpawnChanceRules![0].ExcludedSubtypes.Any(actor.Subtypes.Contains)) return false;
            if (room.RoomIndex < 0 || room.RoomIndex >= 4 || owner.AfterSpawnChanceRules![0].TriggeredInRooms![room.RoomIndex]) return false;
            return Back(room) != null;
        }

        private static CombatUnit? Back(RoomCombatState room) => room.Units.LastOrDefault(unit => unit.Team == CombatTeam.Enemy &&
            unit.Health > 0 && unit.DeathState?.IsDestroyed != true && unit.Status("untouchable") == null);

        internal static RoomCombatResult Apply(RoomCombatState source, int actorId, string trigger, bool managed)
        {
            CombatRelicState? owner = source.Context?.Relics?.SingleOrDefault(item => item.DataId == DataId);
            if (owner == null) return Match(source);
            string? error = Validate(owner); if (error != null) return Fail(error);
            CombatUnit? actor = source.Units.FirstOrDefault(unit => unit.Id == actorId);
            if (managed && !Test(source, actor, trigger, owner)) return Match(source);
            if (source.RoomIndex < 0 || source.RoomIndex >= 4) return Fail("Retreat requires an ordinary native room index.");
            if (source.Units.Any(unit => unit.Health <= 0) ||
                source.Context?.Enchantments?.Rooms.SelectMany(room => room.Units).Any(unit => unit.Health <= 0) == true)
                return Fail("Retreat dead-character cleanup requires native retained-target evidence.");
            bool[] rooms = owner.AfterSpawnChanceRules![0].TriggeredInRooms!.ToArray(); rooms[source.RoomIndex] = true;
            CombatContext context = source.Context!.WithRelics(source.Context.Relics!.Select(item => item.DataId == DataId
                ? item.WithAfterSpawnChanceRules(new[] { item.AfterSpawnChanceRules![0].WithTriggeredInRooms(rooms) }) : item).ToArray());
            RoomCombatState staged = source.WithContext(context);
            CombatUnit? target = Back(staged);
            return target == null ? Match(staged) : CardSpellModel.Apply(staged,
                new[] { new CardActionEffect("FloorRearrange", "DropTargetCharacter", 0, true, false, Array.Empty<CombatStatus>()) }, target.Id);
        }

        internal static TrainCombatResult Apply(TrainCombatState source, int actorId, string trigger, bool managed)
        {
            if (source.Context?.Relics?.Any(owner => owner.DataId == DataId) == true &&
                source.Rooms.SelectMany(item => item.Units).Any(unit => unit.Health <= 0))
                return new(null, RoomOutcome.Unsupported, Array.Empty<RoomCombatResult>(), "Retreat train-wide dead-character cleanup requires native evidence.");
            RoomCombatState? room = source.Rooms.FirstOrDefault(item => item.Units.Any(unit => unit.Id == actorId));
            if (room == null) return new(null, RoomOutcome.Unsupported, Array.Empty<RoomCombatResult>(), "Missing retreat actor room.");
            RoomCombatResult applied = Apply(room, actorId, trigger, managed);
            if (!applied.Supported) return new(null, RoomOutcome.Unsupported, new[] { applied }, applied.UnsupportedReason);
            TrainCombatState train = source; RelicDamageQueueModel.Import(ref train, applied.State!);
            return new(train, applied.Outcome, new[] { applied });
        }

        internal static CombatContext Reset(CombatContext source) => source.Relics?.Any(owner => owner.DataId == DataId) != true
            ? source : source.WithRelics(source.Relics.Select(owner => owner.DataId == DataId
                ? owner.WithAfterSpawnChanceRules(new[] { owner.AfterSpawnChanceRules![0].WithTriggeredInRooms(new bool[4]) }) : owner).ToArray());
        private static RoomCombatResult Match(RoomCombatState source) => new(source, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static RoomCombatResult Fail(string error) => new(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
    }
}
