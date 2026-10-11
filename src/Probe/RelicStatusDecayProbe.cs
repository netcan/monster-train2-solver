using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicStatusDecayProbe
    {
        internal sealed class Query
        {
            public bool RemoveAtContext { get; set; }
            public string StatusId { get; set; } = "";
            public int NativeTeam { get; set; }
            public int UnitId { get; set; }
            public bool Actual { get; set; }
            public string? SuppressingRelicId { get; set; }
            public bool Matrix { get; set; }
            public bool Preview { get; set; }
            public CombatRelicState[] Relics { get; set; } = null!;
        }
        internal static readonly List<Query> Queries = new List<Query>();
        private static readonly HashSet<string> seen = new HashSet<string>();
        private static bool matrix, capturing;

        internal static void Matrix()
        {
            if (!OriginalDecayRelicBatchScenario.Prepared || FullBattleTrace.Active == null) return;
            AllGameManagers managers = AllGameManagers.Instance!;
            matrix = true;
            try
            {
                foreach (CharacterState unit in FullBattleTrace.Active.KnownUnits.Where(unit => !unit.IsDestroyed && unit.IsAlive).ToArray())
                foreach (string id in new[] { "buff", "poison", "armor", "regen" })
                foreach (bool remove in new[] { false, true })
                    managers.GetRelicManager().GetShouldRemove(remove, id, unit, out RelicState _);
            }
            finally { matrix = false; }
        }

        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.GetShouldRemove))]
        private static class QueryPatch
        {
            private static void Postfix(bool removeAtContext, string statusId, CharacterState characterState,
                RelicState suppressingRelic, bool __result)
            {
                if (!OriginalDecayRelicBatchScenario.Prepared || capturing || FullBattleTrace.Active == null) return;
                capturing = true;
                try
                {
                    AllGameManagers managers = AllGameManagers.Instance!;
                    int team = (int)characterState.GetTeamType();
                    bool preview = managers.GetSaveManager().PreviewMode;
                    string? owner = suppressingRelic?.GetRelicDataID();
                    string key = removeAtContext + "/" + statusId + "/" + team + "/" + __result + "/" + owner + "/" + matrix + "/" + preview;
                    if (!seen.Add(key)) return;
                    Queries.Add(new Query { RemoveAtContext = removeAtContext, StatusId = statusId, NativeTeam = team,
                        UnitId = FullBattleTrace.Active.UnitId(characterState), Actual = __result, SuppressingRelicId = owner,
                        Matrix = matrix, Preview = preview, Relics = RelicProbe.Capture(managers) });
                }
                catch (Exception error) { FullBattleTrace.Active.CaptureFailure(error); }
                finally { capturing = false; }
            }
        }
    }
}
