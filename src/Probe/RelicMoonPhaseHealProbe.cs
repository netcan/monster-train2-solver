using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicMoonPhaseHealProbe
    {
        internal sealed class UnitHealth
        {
            public int UnitId { get; set; }
            public int RoomIndex { get; set; }
            public int TargetOrder { get; set; }
            public string Team { get; set; } = "";
            public int Health { get; set; }
            public int MaxHealth { get; set; }
            public bool IsPyre { get; set; }
            public bool IsDead { get; set; }
            public bool IsDestroyed { get; set; }
            public bool IsTargetable { get; set; }
            public bool CanBeHealed { get; set; }
        }

        internal sealed class Operation
        {
            public string OwnerDataId { get; set; } = "";
            public int EffectIndex { get; set; }
            public string Phase { get; set; } = "";
            public string SourceTeam { get; set; } = "";
            public string TargetMode { get; set; } = "";
            public int HealthAmount { get; set; }
            public int MoonPhaseMask { get; set; }
            public UnitHealth[] Before { get; set; } = System.Array.Empty<UnitHealth>();
            public UnitHealth[]? After { get; set; }
        }

        internal static readonly List<Operation> Records = new List<Operation>();

        private static IEnumerator Wrap(IEnumerator native, RelicEffectHealOnMoonPhase effect)
        {
            Operation? record = null;
            try
            {
                AllGameManagers managers = AllGameManagers.Instance!;
                RelicState owner = Current(managers).Single(relic => relic.GetEffects().Any(item => ReferenceEquals(item, effect)));
                int effectIndex = owner.GetEffects().FindIndex(item => ReferenceEquals(item, effect));
                record = new Operation
                {
                    OwnerDataId = owner.GetRelicDataID(),
                    EffectIndex = effectIndex,
                    Phase = managers.GetPlayerManager().CurrentMoonPhase.ToString(),
                    SourceTeam = AccessTools.Field(effect.GetType(), "targetTeam").GetValue(effect).ToString()!,
                    TargetMode = AccessTools.Field(effect.GetType(), "targetMode").GetValue(effect).ToString()!,
                    HealthAmount = (int)AccessTools.Field(effect.GetType(), "healthAmount").GetValue(effect),
                    MoonPhaseMask = (int)AccessTools.Field(effect.GetType(), "moonPhaseMask").GetValue(effect),
                    Before = CaptureUnits(managers)
                };
                Records.Add(record);
            }
            catch (System.Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
            try
            {
                while (native.MoveNext()) yield return native.Current;
            }
            finally
            {
                if (record != null)
                {
                    try { record.After = CaptureUnits(AllGameManagers.Instance!); }
                    catch (System.Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
                }
            }
        }

        private static UnitHealth[] CaptureUnits(AllGameManagers managers)
        {
            var characters = new List<CharacterState>();
            RoomManager roomManager = managers.GetRoomManager()!;
            HeroManager heroManager = managers.GetHeroManager()!;
            MonsterManager monsterManager = managers.GetMonsterManager()!;
            int rooms = roomManager.GetNumRooms();
            for (int room = rooms - 1; room >= 0; room--)
            {
                heroManager.AddCharactersInRoomToList(characters, room);
                monsterManager.AddCharactersInRoomToList(characters, room);
            }
            int targetOrder = 0;
            return characters.Distinct().Select(character =>
            {
                var statuses = new List<CharacterState.StatusEffectStack>();
                character.GetStatusEffects(ref statuses);
                return new UnitHealth
                {
                    UnitId = FullBattleTrace.Active!.UnitId(character),
                    RoomIndex = character.GetCurrentRoomIndex(),
                    TargetOrder = targetOrder++,
                    Team = character.GetTeamType().ToString(),
                    Health = character.GetHP(),
                    MaxHealth = character.GetMaxHP(),
                    IsPyre = character.IsPyreHeart(),
                    IsDead = character.IsDead,
                    IsDestroyed = character.IsDestroyed,
                    IsTargetable = !character.HasStatusEffect("untouchable") &&
                        statuses.All(status => status.State.GetUnitIsTargetable(inCombat: false)),
                    CanBeHealed = character.GetCanBeHealed()
                };
            }).OrderBy(unit => unit.UnitId).ToArray();
        }

        private static RelicState[] Current(AllGameManagers managers)
        {
            var relics = new List<RelicState>();
            AccessTools.Method(typeof(RelicManager), "GetCurrentRelics").Invoke(managers.GetRelicManager(), new object[] { relics });
            return relics.ToArray();
        }

        [HarmonyPatch(typeof(RelicEffectHealOnMoonPhase), nameof(RelicEffectHealOnMoonPhase.ApplyEffectMoonPhase))]
        private static class HealOnMoonPhasePatch
        {
            private static void Postfix(RelicEffectHealOnMoonPhase __instance, ref IEnumerator __result)
            {
                if (OriginalMoonPhaseHealRelicScenario.Prepared ||
                    OriginalMoonCardUpgradesScenario.Prepared && OriginalMoonCardUpgradesScenario.PhaseHealRelic)
                    __result = Wrap(__result, __instance);
            }
        }
    }
}
