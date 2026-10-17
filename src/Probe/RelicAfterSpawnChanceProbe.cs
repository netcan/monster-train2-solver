using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicAfterSpawnChanceProbe
    {
        internal sealed class TestRecord
        {
            public int UnitId { get; set; }
            public string Trigger { get; set; } = string.Empty;
            public int RoomIndex { get; set; }
            public string Team { get; set; } = string.Empty;
            public bool IsDestroyed { get; set; }
            public bool Untouchable { get; set; }
            public int SpellWeaknessBefore { get; set; }
            public int SpellWeaknessAfter { get; set; }
            public CombatStatus[] StatusesBefore { get; set; } = System.Array.Empty<CombatStatus>();
            public CombatStatus[] StatusesAfter { get; set; } = System.Array.Empty<CombatStatus>();
            public uint[] BattleRngBefore { get; set; } = System.Array.Empty<uint>();
            public uint[] BattleRngAfter { get; set; } = System.Array.Empty<uint>();
            public bool Result { get; set; }
        }

        internal sealed class ApplyRecord
        {
            public int UnitId { get; set; }
            public int RoomIndex { get; set; }
            public int SpellWeaknessBefore { get; set; }
            public int SpellWeaknessAfter { get; set; }
            public CombatStatus[] StatusesBefore { get; set; } = System.Array.Empty<CombatStatus>();
            public CombatStatus[] StatusesAfter { get; set; } = System.Array.Empty<CombatStatus>();
            public uint[] BattleRngBefore { get; set; } = System.Array.Empty<uint>();
            public uint[] BattleRngAfter { get; set; } = System.Array.Empty<uint>();
            public bool Completed { get; set; }
        }

        internal static readonly List<TestRecord> Tests = new List<TestRecord>();
        internal static readonly List<ApplyRecord> Applications = new List<ApplyRecord>();

        [HarmonyPatch(typeof(RelicEffectChanceAddEffectAfterSpawn), nameof(RelicEffectChanceAddEffectAfterSpawn.TestCharacterTriggerEffect))]
        private static class TestPatch
        {
            private static void Prefix(RelicEffectChanceAddEffectAfterSpawn __instance,
                CharacterTriggerRelicEffectParams __0, out TestRecord? __state)
            {
                __state = null;
                if (!IsObservedOwner(__instance) ||
                    FullBattleTrace.Active == null) return;
                CharacterState actor = __0.characterState;
                FullBattleTrace trace = FullBattleTrace.Active;
                __state = new TestRecord
                {
                    UnitId = trace.UnitId(actor), Trigger = __0.trigger.ToString(),
                    RoomIndex = actor.GetSpawnPoint()?.GetRoomOwner().OrNull()?.GetRoomIndex() ?? -1,
                    Team = actor.GetTeamType().ToString(), IsDestroyed = actor.IsDestroyed,
                    Untouchable = actor.HasStatusEffect("untouchable"),
                    SpellWeaknessBefore = actor.GetStatusEffectStacks("spell weakness"),
                    StatusesBefore = Statuses(actor),
                    BattleRngBefore = Rng()
                };
                Tests.Add(__state);
            }

            private static void Postfix(TestRecord? __state, bool __result)
            {
                if (__state == null) return;
                CharacterState actor = FullBattleTrace.Active!.KnownUnits
                    .First(unit => FullBattleTrace.Active.TryGetUnitId(unit, out int id) && id == __state.UnitId);
                __state.Result = __result;
                __state.SpellWeaknessAfter = actor.GetStatusEffectStacks("spell weakness");
                __state.StatusesAfter = Statuses(actor);
                __state.BattleRngAfter = Rng();
            }
        }

        [HarmonyPatch(typeof(RelicEffectChanceAddEffectAfterSpawn), nameof(RelicEffectChanceAddEffectAfterSpawn.ApplyCharacterTriggerEffect))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectChanceAddEffectAfterSpawn __instance,
                CharacterTriggerRelicEffectParams __0, ref IEnumerator __result)
            {
                if (!IsObservedOwner(__instance) ||
                    FullBattleTrace.Active == null) return;
                __result = Wrap(__result, __0.characterState);
            }
        }

        private static IEnumerator Wrap(IEnumerator native, CharacterState actor)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            var record = new ApplyRecord
            {
                UnitId = trace.UnitId(actor),
                RoomIndex = actor.GetSpawnPoint()?.GetRoomOwner().OrNull()?.GetRoomIndex() ?? -1,
                SpellWeaknessBefore = actor.GetStatusEffectStacks("spell weakness"),
                StatusesBefore = Statuses(actor),
                BattleRngBefore = Rng()
            };
            Applications.Add(record);
            try
            {
                while (native.MoveNext()) yield return native.Current;
                record.Completed = true;
            }
            finally
            {
                (native as System.IDisposable)?.Dispose();
                record.SpellWeaknessAfter = actor.GetStatusEffectStacks("spell weakness");
                record.StatusesAfter = Statuses(actor);
                record.BattleRngAfter = Rng();
            }
        }

        private static bool IsObservedOwner(RelicEffectChanceAddEffectAfterSpawn effect)
        {
            string? id = effect.SourceRelicState?.GetRelicDataID();
            return OriginalChanceForSpellWeaknessScenario.Prepared && id == OriginalChanceForSpellWeaknessScenario.DataId ||
                OriginalChanceToCleanseScenario.Prepared && id == OriginalChanceToCleanseScenario.DataId;
        }

        private static CombatStatus[] Statuses(CharacterState actor)
        {
            var statuses = new List<CharacterState.StatusEffectStack>();
            actor.GetStatusEffects(ref statuses, includeZeroStacks: true);
            return statuses.Select(status => BattleActionProbe.Status(status.State.GetStatusId(), status.Count)).ToArray();
        }

        private static uint[] Rng() => RngCalibration.Words(RandomManager.GetState(RngId.Battle));
    }
}
