using System;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    // Original definitions and normal combat; only the three independent HP
    // owners are explicitly reduced in an isolated save.
    internal static class TrueFinalBossLethalScenario
    {
        internal sealed class Record
        {
            public BattleTurnState Before { get; set; } = null!;
            public BattleTurnState Actual { get; set; } = null!;
            public int[] OwnerIds { get; set; } = Array.Empty<int>();
            public int[] Debuffs { get; set; } = Array.Empty<int>();
            public bool RefreshPreview { get; set; }
        }
        internal static Record? Evidence { get; private set; }
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_LETHAL") == "1";
        internal static bool Prepare()
        {
            if (!Enabled || Evidence != null) return false;
            AllGameManagers managers = AllGameManagers.Instance;
            if (managers.GetCombatManager().GetTurnCount() != 1) return false;
            if (!TrueFinalBossTurnProbe.Enabled || managers.GetSaveManager().PreviewMode)
                throw new InvalidOperationException("Lethal control requires the original isolated live TFB route.");
            FullBattleTrace trace = FullBattleTrace.Active!;
            string[] assets = { "Boss_Level9_Dominion_V2", "Boss_Level9_Savagery_V2", "Boss_Level9_Entropy_Bot_V2" };
            CharacterState[] owners = assets.Select(asset => managers.GetHeroManager().GetCharactersReadOnly()
                .Single(unit => unit.GetSourceCharacterData().GetAssetKey() == asset)).ToArray();
            var row = new Record { Before = trace.CaptureDecision(), OwnerIds = owners.Select(trace.UnitId).ToArray(),
                RefreshPreview = Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_LETHAL_PREVIEW") == "1",
                Debuffs = owners.Select(unit => unit.GetHP() - 1).ToArray() };
            if (!row.Debuffs.SequenceEqual(new[] { 1554, 2749, 3499 }))
                throw new InvalidOperationException("Lethal control requires unchanged original owner HP.");
            for (int index = 0; index < owners.Length; index++) owners[index].DebuffHP(row.Debuffs[index]);
            row.Actual = trace.CaptureDecision();
            Evidence = row;
            if (row.RefreshPreview) managers.GetCombatManager().ToggleCombatPreviewFromExternalSource();
            return true;
        }
    }
}
