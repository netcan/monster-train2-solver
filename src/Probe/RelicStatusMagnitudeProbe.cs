using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicStatusMagnitudeProbe
    {
        internal sealed class Query
        {
            public string StatusId { get; set; } = "";
            public string CharacterTeam { get; set; } = "";
            public int NativeTeam { get; set; }
            public int Actual { get; set; }
            public bool Matrix { get; set; }
            public bool Preview { get; set; }
            public CombatRelicState[] Relics { get; set; } = null!;
            public CombatContext? Context { get; set; }
        }

        internal static readonly List<Query> Queries = new List<Query>();
        private static readonly HashSet<string> seen = new HashSet<string>();
        private static bool matrix;
        private static bool capturing;
        private static bool? conditionalRules;

        internal static void Matrix()
        {
            if (!OriginalMagnitudeRelicBatchScenario.Prepared || FullBattleTrace.Active == null) return;
            conditionalRules = null;
            matrix = true;
            try
            {
                RelicManager manager = AllGameManagers.Instance!.GetRelicManager();
                foreach (Team.Type team in new[] { Team.Type.None, Team.Type.Heroes, Team.Type.Monsters, (Team.Type)255 })
                foreach (string status in new[] { "buff", "spikes", "regen", "mageblade", "armor" })
                    manager.GetModifiedStatusMagnitudePerStack(status, team);
            }
            finally { matrix = false; }
        }

        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.GetModifiedStatusMagnitudePerStack))]
        private static class QueryPatch
        {
            private static void Postfix(RelicManager __instance, string statusId, Team.Type characterTeam, int __result)
            {
                if (!OriginalMagnitudeRelicBatchScenario.Prepared || capturing || FullBattleTrace.Active == null) return;
                capturing = true;
                try
                {
                    AllGameManagers managers = AllGameManagers.Instance!;
                    bool preview = managers.GetSaveManager().PreviewMode;
                    // Unconditional originals need only their captured ordered
                    // rules. Conditional effects also require their query frame;
                    // the guard excludes magnitude getters called by capture.
                    if (!conditionalRules.HasValue)
                    {
                        var nativeRelics = new List<RelicState>();
                        AccessTools.Method(typeof(RelicManager), "GetCurrentRelics").Invoke(__instance, new object[] { nativeRelics });
                        conditionalRules = nativeRelics.SelectMany(relic => relic.GetEffects())
                            .Where(effect => effect is RelicEffectModifyStatusMagnitude)
                            .Any(effect => RelicCardModifierProbe.Conditions(effect).Length != 0);
                    }
                    CombatContext? context = conditionalRules.Value ? FullBattleTrace.Active.CaptureContext() : null;
                    string key = statusId + "/" + (int)characterTeam + "/" + __result + "/" + matrix + "/" + preview +
                        (context == null ? "" : "/" + JsonConvert.SerializeObject(context));
                    if (!seen.Add(key)) return;
                    CombatRelicState[] relics = context?.Relics?.ToArray() ?? RelicProbe.Capture(managers);
                    Queries.Add(new Query { StatusId = statusId, CharacterTeam = characterTeam.ToString(), NativeTeam = (int)characterTeam,
                        Actual = __result, Matrix = matrix, Preview = preview, Relics = relics,
                        Context = context });
                }
                catch (Exception error) { FullBattleTrace.Active.CaptureFailure(error); }
                finally { capturing = false; }
            }
        }
    }
}
