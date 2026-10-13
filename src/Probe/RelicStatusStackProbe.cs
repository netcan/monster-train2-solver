using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicStatusStackProbe
    {
        private static readonly HashSet<string> seen = new HashSet<string>();
        internal static readonly List<Query> Queries = new List<Query>();
        internal sealed class Query
        {
            public string StatusId { get; set; } = "";
            public int Incoming { get; set; }
            public CombatTeam? TargetTeam { get; set; }
            public bool HasStatus { get; set; }
            public bool Permanent { get; set; }
            public bool Deferred { get; set; }
            public bool IncludeAdders { get; set; }
            public string? EffectType { get; set; }
            public CombatRelicState[] Relics { get; set; } = null!;
            public int Actual { get; set; }
            public bool Matrix { get; set; }
        }
        private static bool matrix;
        internal static void Matrix()
        {
            if (!OriginalStatusRelicBatchScenario.Prepared && !OriginalDecayRelicBatchScenario.Prepared) return;
            AllGameManagers managers = AllGameManagers.Instance!;
            var targets = FullBattleTrace.Active!.KnownUnits.Where(unit => !unit.IsDestroyed && unit.IsAlive)
                .Cast<CharacterState?>().Concat(new CharacterState?[] { null });
            string[] statuses = OriginalDecayRelicBatchScenario.Prepared ? ["buff", "poison", "armor", "regen"] :
                OriginalStatusRelicBatchScenario.BurnoutStacks ? ["armor", "valor", "pyregel", "spell weakness", "regen", "corrupt_poison", "ephemeral"] :
                OriginalStatusRelicBatchScenario.Prepared ? ["armor", "valor", "pyregel", "spell weakness", "regen", "corrupt_poison"] :
                ["armor", "valor", "pyregel", "spell weakness", "regen"];
            matrix = true;
            try
            {
                foreach (CharacterState? target in targets)
                foreach (string id in statuses)
                {
                    foreach (int count in new[] { -1, 0, 1, int.MaxValue }) Get(count, false, false, true, null);
                    Get(1, true, false, true, null); Get(0, true, true, true, null); Get(1, true, true, true, null);
                    Get(1, false, true, true, null); Get(1, false, false, false, null);
                    foreach (Type type in new[] { typeof(CardEffectMultiplyStatusEffect), typeof(CardEffectTransferAllStatusEffects), typeof(CardEffectTransferAllStatusEffectsFromUnit) })
                        Get(1, false, false, true, type);
                    void Get(int count, bool permanent, bool deferred, bool adders, Type? type)
                    {
                        var data = new StatusEffectStackData { statusId = id, count = count, fromPermanentUpgrade = permanent };
                        managers.GetRelicManager().GetStatusEffectStacksModifiedByRelics(data, target, type, adders, deferred);
                    }
                }
            }
            finally { matrix = false; }
        }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.GetStatusEffectStacksModifiedByRelics))]
        private static class QueryPatch
        {
            private static void Postfix(StatusEffectStackData statusEffectStackData, CharacterState? onCharacter,
                Type? fromEffectType, bool includeAdders, bool deferred, int __result)
            {
                if (!OriginalStatusRelicBatchScenario.Prepared && !OriginalDecayRelicBatchScenario.Prepared) return;
                CombatTeam? team = onCharacter == null ? (CombatTeam?)null : onCharacter.GetTeamType() == Team.Type.Monsters ? CombatTeam.Player : CombatTeam.Enemy;
                bool has = onCharacter?.HasStatusEffect(statusEffectStackData.statusId) == true;
                string key = statusEffectStackData.statusId + "/" + statusEffectStackData.count + "/" + team + "/" + has + "/" +
                    statusEffectStackData.fromPermanentUpgrade + "/" + deferred + "/" + includeAdders + "/" + fromEffectType?.Name + "/" + matrix;
                if (!seen.Add(key)) return;
                Queries.Add(new Query { StatusId = statusEffectStackData.statusId, Incoming = statusEffectStackData.count,
                    TargetTeam = team, HasStatus = has, Permanent = statusEffectStackData.fromPermanentUpgrade,
                    Deferred = deferred, IncludeAdders = includeAdders, EffectType = fromEffectType?.Name,
                    Relics = RelicProbe.Capture(AllGameManagers.Instance!), Actual = __result, Matrix = matrix });
            }
        }
    }
}
