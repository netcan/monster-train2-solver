using System;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    // Controlled isolated-save boundary, not an unchanged natural route.
    internal static class TrueFinalBossDormantScenario
    {
        internal sealed class Record
        {
            public BattleTurnState Before { get; set; } = null!;
            public BattleTurnState Actual { get; set; } = null!;
            public int SavageryId { get; set; }
            public int FrontId { get; set; }
            public int FrontHealth { get; set; }
        }
        internal static Record? Evidence { get; private set; }
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_DORMANT") == "1";
        internal static bool Prepare()
        {
            if (Evidence != null || !Enabled) return false;
            AllGameManagers managers = AllGameManagers.Instance;
            if (managers.GetCombatManager().GetTurnCount() != 1) return false;
            if (!TrueFinalBossTurnProbe.Enabled || managers.GetSaveManager().PreviewMode)
                throw new InvalidOperationException("Dormant Savagery control requires the isolated original live TFB route.");
            FullBattleTrace trace = FullBattleTrace.Active!;
            CharacterState savagery = managers.GetHeroManager().GetCharactersReadOnly().Single(unit =>
                unit.GetSourceCharacterData().GetAssetKey() == "Boss_Level9_Savagery_V2");
            if (savagery.GetHP() != 2750) throw new InvalidOperationException("Dormant control requires unchanged born Savagery HP.");
            var record = new Record { Before = trace.CaptureDecision(), SavageryId = trace.UnitId(savagery) };
            savagery.DebuffHP(2749);
            if (int.TryParse(Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_FRONT_HEALTH"), out int hp) && hp > 0)
            {
                CharacterState front = managers.GetMonsterManager().GetCharactersReadOnly().First(unit =>
                    unit.GetSourceCharacterData().GetAssetKey() == "TrainStewardBig" && unit.GetCurrentRoomIndex() == 0);
                if (hp > front.GetHP()) throw new InvalidOperationException("Front control must only debuff a living Steward.");
                record.FrontId = trace.UnitId(front); record.FrontHealth = hp;
                front.DebuffHP(front.GetHP() - hp);
            }
            record.Actual = trace.CaptureDecision();
            Evidence = record;
            return true; // let the game's usual preview/input settlement run before EndTurn
        }
    }
}
