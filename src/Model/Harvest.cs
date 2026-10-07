using System;
using System.Collections.Generic;

namespace MonsterTrain2Poju.Model
{
    internal static class HarvestModel
    {
        internal static readonly string[] Kinds = { "OnAnyHeroDeathOnFloor", "OnAnyMonsterDeathOnFloor", "OnAnyUnitDeathOnFloor" };
        private const string Prefix = "#PhysicalHarvest:";
        internal static IEnumerable<string> Stages(CombatUnit dying)
        {
            foreach (string kind in new[] { dying.Team == CombatTeam.Enemy ? Kinds[0] : Kinds[1], Kinds[2] })
                foreach (CombatTeam team in new[] { CombatTeam.Player, CombatTeam.Enemy })
                    yield return Prefix + kind + ":" + team;
        }
        internal static bool Stage(string stage, out string kind, out CombatTeam team)
        {
            kind = ""; team = default;
            if (!stage.StartsWith(Prefix, StringComparison.Ordinal)) return false;
            string[] fields = stage.Substring(Prefix.Length).Split(':');
            kind = fields[0]; team = (CombatTeam)Enum.Parse(typeof(CombatTeam), fields[1]); return true;
        }
        internal static int Count(CombatUnit dying, string kind) =>
            kind == Kinds[1] || kind == Kinds[2] ? Math.Max(1, dying.Status("horde")?.Stacks ?? 0) : 1;
    }
}
