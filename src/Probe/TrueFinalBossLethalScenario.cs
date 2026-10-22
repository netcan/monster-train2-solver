using System;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    // Original definitions in an isolated save, with explicit owner HP and
    // optional Dominion shield controls; paid mode uses the normal card path.
    internal static class TrueFinalBossLethalScenario
    {
        internal sealed class Record
        {
            public BattleTurnState Before { get; set; } = null!;
            public BattleTurnState Actual { get; set; } = null!;
            public int[] OwnerIds { get; set; } = Array.Empty<int>();
            public int[] Debuffs { get; set; } = Array.Empty<int>();
            public bool RefreshPreview { get; set; }
            public string PaidSpellTarget { get; set; } = "";
            public int RemovedSpellShield { get; set; }
        }
        internal static Record? Evidence { get; private set; }
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_LETHAL") == "1";
        internal static string PaidSpellTarget => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_LETHAL_SPELL_TARGET") ?? "";
        internal static bool PaidSpell => PaidSpellTarget.Length > 0;
        internal static string TargetAsset => PaidSpellTarget == "Dominion" ? "Boss_Level9_Dominion_V2" :
            PaidSpellTarget == "Savagery" ? "Boss_Level9_Savagery_V2" :
            "Boss_Level9_Entropy_" + (PaidSpellTarget == "EntropyBot" ? "Bot" : PaidSpellTarget == "EntropyMid" ? "Mid" : "Top") + "_V2";
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (state.Spawn.Turn == 0) return null;
            CombatUnit target = state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.AssetKey == TargetAsset);
            BattleActionEnumeration choices = BattleActionModel.EnumerateActions(state, includeAbilities: false);
            PlayCardAction? selected = choices.SupportedActions.FirstOrDefault(action => action.TargetUnitId == target.Id &&
                state.PlayRules!.Cards.Any(rule => rule.DataId == state.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.DataId &&
                    rule.CardType == "Spell" && rule.Effects.Any(effect => effect.Type == "Damage")));
            return selected ?? throw new InvalidOperationException("Missing an original legal paid damage spell for " + PaidSpellTarget);
        }
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
            var row = new Record { Before = trace.CaptureDecision(), OwnerIds = owners.Select(trace.UnitId).ToArray(), PaidSpellTarget = PaidSpellTarget,
                RefreshPreview = Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_LETHAL_PREVIEW") == "1",
                Debuffs = owners.Select(unit => unit.GetHP() - 1).ToArray() };
            if (!row.Debuffs.SequenceEqual(new[] { 1554, 2749, 3499 }))
                throw new InvalidOperationException("Lethal control requires unchanged original owner HP.");
            for (int index = 0; index < owners.Length; index++) owners[index].DebuffHP(row.Debuffs[index]);
            if (PaidSpellTarget == "Dominion")
            {
                row.RemovedSpellShield = 4;
                owners[0].RemoveStatusEffect("spell shield", row.RemovedSpellShield);
            }
            row.Actual = trace.CaptureDecision();
            Evidence = row;
            if (row.RefreshPreview) managers.GetCombatManager().ToggleCombatPreviewFromExternalSource();
            return true;
        }
    }
}
