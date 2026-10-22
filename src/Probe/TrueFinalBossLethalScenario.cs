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
            public bool TargetOwnerOnly { get; set; }
            public bool OrdinaryCombat { get; set; }
        }
        internal static Record? Evidence { get; private set; }
        private static int paidCardId;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_LETHAL") == "1";
        internal static string PaidSpellTarget => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_LETHAL_SPELL_TARGET") ?? "";
        internal static bool PaidSpell => PaidSpellTarget.Length > 0;
        internal static bool CommandedAttack => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_COMMANDED_ATTACK") == "1";
        internal static bool PyreCommand => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_PYRE_COMMAND") == "1";
        internal static bool OrdinaryCombat => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_LETHAL_COMBAT") == "1";
        internal static bool TargetOwnerOnly => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_LETHAL_TARGET_ONLY") == "1";
        internal static string TargetAsset => PaidSpellTarget == "Dominion" ? "Boss_Level9_Dominion_V2" :
            PaidSpellTarget == "Savagery" ? "Boss_Level9_Savagery_V2" :
            "Boss_Level9_Entropy_" + (PaidSpellTarget == "EntropyBot" ? "Bot" : PaidSpellTarget == "EntropyMid" ? "Mid" : "Top") + "_V2";
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (state.Spawn.Turn == 0) return null;
            if (TargetOwnerOnly && paidCardId > 0 && state.Spawn.Train.Context!.Statistics!.Value(paidCardId, "TimesPlayed", "ThisBattle") > 0)
                return null;
            if (CommandedAttack || PyreCommand)
            {
                int card = TrueFinalBossOrdinaryAllyScenario.Evidence!.CardId;
                int unit = state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(actor => actor.SpawnerCardId == card).Id;
                int command = TrueFinalBossOrdinaryAllyScenario.Evidence.CommandCardId;
                if (!state.Spawn.Train.Context!.Cards.Hand.Any(token => token.InstanceId == command))
                    throw new InvalidOperationException("Prepared original command was not drawn.");
                // Native legality is the oracle even while a model guard is
                // being calibrated; the trace still counts unsupported results.
                return new PlayCardAction(command, 0, targetUnitId: PyreCommand ? 0 : unit);
            }
            CombatUnit target = state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.AssetKey == TargetAsset);
            BattleActionEnumeration choices = BattleActionModel.EnumerateActions(state, includeAbilities: false);
            PlayCardAction? selected = choices.SupportedActions.FirstOrDefault(action => action.TargetUnitId == target.Id &&
                state.PlayRules!.Cards.Any(rule => rule.DataId == state.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.DataId &&
                    rule.CardType == "Spell" && rule.Effects.Any(effect => effect.Type == "Damage")));
            if (selected == null) throw new InvalidOperationException("Missing an original legal paid damage spell for " + PaidSpellTarget);
            paidCardId = selected.CardInstanceId;
            return selected;
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
                TargetOwnerOnly = TargetOwnerOnly, OrdinaryCombat = OrdinaryCombat,
                RefreshPreview = Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_LETHAL_PREVIEW") == "1",
                Debuffs = owners.Select(unit => unit.GetHP() - 1).ToArray() };
            if (!row.Debuffs.SequenceEqual(new[] { 1554, 2749, 3499 }))
                throw new InvalidOperationException("Lethal control requires unchanged original owner HP.");
            if (TargetOwnerOnly)
            {
                int selected = PaidSpellTarget == "Dominion" ? 0 : PaidSpellTarget == "Savagery" ? 1 : 2;
                for (int index = 0; index < owners.Length; index++) if (index != selected) row.Debuffs[index] = 0;
            }
            for (int index = 0; index < owners.Length; index++) if (row.Debuffs[index] != 0) owners[index].DebuffHP(row.Debuffs[index]);
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
